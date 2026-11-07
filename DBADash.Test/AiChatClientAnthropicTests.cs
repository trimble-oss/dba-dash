using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The Anthropic provider, against a fake server.  Every request goes to <see cref="FakeHandler"/>;
    /// no API key is needed.
    /// </summary>
    [TestClass]
    public class AiChatClientAnthropicTests
    {
        private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, content));
                return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            }
        }

        private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private static IConfiguration Config(Dictionary<string, string?>? extra = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["AI:Provider"] = "Anthropic",
                ["Anthropic:BaseUrl"] = "https://api.anthropic.com",
                ["Anthropic:ApiKey"] = "anthropic-key",
                ["Anthropic:Model"] = "claude-sonnet-4-6",
                ["Anthropic:Version"] = "2023-06-01"
            };
            foreach (var kv in extra ?? new()) values[kv.Key] = kv.Value;
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static (AiChatClient Client, FakeHandler Handler) Create(IConfiguration config, HttpStatusCode status, string body)
        {
            var handler = new FakeHandler(status, body);
            var client = new AiChatClient(config,
                new FakeFactory(handler),
                NullLogger<AiChatClient>.Instance,
                new SystemPromptLoader(config, NullLogger<SystemPromptLoader>.Instance),
                new AiDataDetailSetting());
            return (client, handler);
        }

        private static readonly AiConversationTurn[] Question =
            { new() { Role = AiConversationTurn.User, Content = "Why did this deadlock?" } };

        private const string Answer = """{"content":[{"type":"thinking","thinking":""},{"type":"text","text":"Lock order."}],"stop_reason":"end_turn"}""";

        [TestMethod]
        [DataRow("claude-sonnet-4-6")]
        [DataRow("claude-opus-4-7")]
        [DataRow("claude-sonnet-5-5")]
        [DataRow("claude-opus-5-5")]
        [DataRow("my-foundry-deployment")]
        public async Task Request_SendsNoSamplingOrThinkingSettings_WhateverTheModel(string model)
        {
            // Issue #2269: newer models answer 400 to temperature, and a model name - a deployment name
            // on Azure Foundry - is no way to tell which.
            var (client, handler) = Create(Config(new() { ["Anthropic:Model"] = model }), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Lock order.", result.Text);
            var (request, body) = handler.Requests.Single();
            Assert.AreEqual("https://api.anthropic.com/v1/messages", request.RequestUri!.ToString());
            Assert.AreEqual("anthropic-key", request.Headers.GetValues("x-api-key").Single());
            Assert.AreEqual("2023-06-01", request.Headers.GetValues("anthropic-version").Single());

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            Assert.AreEqual(model, root.GetProperty("model").GetString());
            Assert.IsFalse(root.TryGetProperty("temperature", out _));
            Assert.IsFalse(root.TryGetProperty("top_p", out _));
            Assert.IsFalse(root.TryGetProperty("top_k", out _));
            Assert.IsFalse(root.TryGetProperty("thinking", out _));
            Assert.AreEqual(AiChatClient.DefaultAnthropicMaxTokens, root.GetProperty("max_tokens").GetInt32());
        }

        [TestMethod]
        [DataRow("2000", 2000)]
        [DataRow("1000000", AiChatClient.MaxAnthropicMaxTokens)]
        [DataRow("", AiChatClient.DefaultAnthropicMaxTokens)]
        public async Task MaxTokens_IsHonouredWithinTheCap(string setting, int expected)
        {
            var (client, handler) = Create(Config(new() { ["Anthropic:MaxTokens"] = setting }), HttpStatusCode.OK, Answer);

            await client.ChatAsync(Question, CancellationToken.None);

            using var json = JsonDocument.Parse(handler.Requests.Single().Body);
            Assert.AreEqual(expected, json.RootElement.GetProperty("max_tokens").GetInt32());
        }

        [TestMethod]
        public async Task StoppedWhileThinking_SaysToRaiseMaxTokens_RatherThanReturningAnEmptySummary()
        {
            const string thinkingOnly = """{"content":[{"type":"thinking","thinking":""}],"stop_reason":"max_tokens"}""";
            var (client, _) = Create(Config(new() { ["Anthropic:MaxTokens"] = "1024" }), HttpStatusCode.OK, thinkingOnly);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            StringAssert.Contains(result.Text, "Anthropic:MaxTokens (1024)");
        }

        [TestMethod]
        public async Task StoppedWhileThinking_AtTheCap_DoesNotSayToRaiseMaxTokens()
        {
            const string thinkingOnly = """{"content":[{"type":"thinking","thinking":""}],"stop_reason":"max_tokens"}""";
            // Over the cap is clamped to it, so this is the same as setting the cap.
            var (client, _) = Create(Config(new() { ["Anthropic:MaxTokens"] = "50000" }), HttpStatusCode.OK, thinkingOnly);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            StringAssert.Contains(result.Text, AiChatClient.MaxAnthropicMaxTokens.ToString());
            Assert.IsFalse(result.Text.Contains("raise", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task Retries_StayInsideTheClientTimeout_RatherThanGettingItEachTime()
        {
            // A transient failure, then a backoff of 1s: with half a second for the whole call, the retry
            // is never sent.  Per attempt, it would have been - and the total could pass the GUI's wait.
            var handler = new FakeHandler(HttpStatusCode.ServiceUnavailable, "{}");
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(500) };
            var started = DateTime.UtcNow;

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => AiChatClient.PostWithRetryAsync(
                http,
                new Uri("https://api.anthropic.com/v1/messages"),
                () => new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"),
                CancellationToken.None));

            Assert.AreEqual(1, handler.Requests.Count);
            Assert.IsTrue(DateTime.UtcNow - started < TimeSpan.FromSeconds(3));
        }

        [TestMethod]
        public async Task StoppedMidAnswer_KeepsTheAnswer_AndSaysItWasCutShort()
        {
            const string cut = """{"content":[{"type":"text","text":"Lock order, because"}],"stop_reason":"max_tokens"}""";
            var (client, _) = Create(Config(), HttpStatusCode.OK, cut);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.IsTrue(result.Success);
            StringAssert.StartsWith(result.Text, "Lock order, because");
            StringAssert.Contains(result.Text, "cut short");
        }

        [TestMethod]
        public async Task Refusal_IsAFailure_NotAnAnswer()
        {
            const string refusal = """{"content":[],"stop_reason":"refusal","stop_details":{"type":"refusal","category":null,"explanation":"Declined."}}""";
            var (client, _) = Create(Config(), HttpStatusCode.OK, refusal);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            StringAssert.Contains(result.Text, "declined");
            StringAssert.Contains(result.Text, "Declined.");
        }

        [TestMethod]
        public async Task BadRequest_IncludesTheProvidersReason()
        {
            const string error = """{"type":"error","error":{"type":"invalid_request_error","message":"`temperature` is deprecated for this model."},"request_id":"req_1"}""";
            var (client, _) = Create(Config(), HttpStatusCode.BadRequest, error);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            Assert.AreEqual(400, result.ProviderStatusCode);
            StringAssert.Contains(result.Text, "`temperature` is deprecated for this model.");
            Assert.IsFalse(result.Text.Contains("check model/deployment name"));
        }

        [TestMethod]
        public async Task Unauthorized_LeavesTheProvidersMessageOut()
        {
            const string error = """{"type":"error","error":{"type":"authentication_error","message":"invalid x-api-key"}}""";
            var (client, _) = Create(Config(), HttpStatusCode.Unauthorized, error);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            Assert.IsFalse(result.Text.Contains("invalid x-api-key"));
        }
    }
}
