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
    /// The Ollama provider, against a fake server.
    ///
    /// What matters most here is the request: Ollama answers an oversized prompt by quietly dropping
    /// the start of it unless it is told not to, and a model's default context window is too small
    /// for most of what this service sends.  Neither shows up as an error - only as a wrong answer.
    ///
    /// No Ollama server is needed: every request goes to <see cref="FakeHandler"/>, so these run the
    /// same on a build agent as on a machine with Ollama installed.
    /// </summary>
    [TestClass]
    public class AiChatClientOllamaTests
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

        /// <summary>What HttpClient throws when its own timeout expires, or the caller cancels.</summary>
        private sealed class CancellingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.",
                    new TimeoutException(), cancellationToken);
        }

        private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private static IConfiguration Config(Dictionary<string, string?>? extra = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["AI:Provider"] = "Ollama",
                ["Ollama:Model"] = "llama3.2:3b"
            };
            foreach (var kv in extra ?? new()) values[kv.Key] = kv.Value;
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static (AiChatClient Client, FakeHandler Handler) Create(IConfiguration config, HttpStatusCode status, string body)
        {
            var handler = new FakeHandler(status, body);
            return (Create(config, handler), handler);
        }

        private static AiChatClient Create(IConfiguration config, HttpMessageHandler handler) =>
            new(config,
                new FakeFactory(handler),
                NullLogger<AiChatClient>.Instance,
                new SystemPromptLoader(config, NullLogger<SystemPromptLoader>.Instance));

        private static readonly AiConversationTurn[] Question =
            { new() { Role = AiConversationTurn.User, Content = "Why did this deadlock?" } };

        private const string Answer = """{"model":"llama3.2:3b","message":{"role":"assistant","content":"Lock order."},"done":true,"done_reason":"stop"}""";

        [TestMethod]
        public async Task Request_GoesToNativeChatApi_WithContextWindowAndNoTruncation()
        {
            var (client, handler) = Create(Config(), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual("Lock order.", result.Text);
            var (request, body) = handler.Requests.Single();
            Assert.AreEqual("http://localhost:11434/api/chat", request.RequestUri!.ToString());
            Assert.IsNull(request.Headers.Authorization);

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            Assert.AreEqual("llama3.2:3b", root.GetProperty("model").GetString());
            Assert.IsFalse(root.GetProperty("stream").GetBoolean());
            Assert.IsFalse(root.GetProperty("truncate").GetBoolean());
            Assert.AreEqual(AiChatClient.DefaultOllamaContextLength, root.GetProperty("options").GetProperty("num_ctx").GetInt32());
            Assert.IsFalse(root.GetProperty("options").TryGetProperty("num_predict", out _));

            var messages = root.GetProperty("messages").EnumerateArray().ToList();
            Assert.AreEqual("system", messages[0].GetProperty("role").GetString());
            Assert.AreEqual("user", messages[1].GetProperty("role").GetString());
        }

        [TestMethod]
        public async Task Settings_BaseUrlContextMaxTokensKeyAndModelOverride_AreHonoured()
        {
            var config = Config(new()
            {
                ["Ollama:BaseUrl"] = "http://gpu-box:11434/",
                ["Ollama:ContextLength"] = "8192",
                ["Ollama:MaxTokens"] = "500",
                ["Ollama:ApiKey"] = "secret"
            });
            var (client, handler) = Create(config, HttpStatusCode.OK, Answer);

            await client.ChatAsync(Question, CancellationToken.None, modelOverride: "qwen3:8b");

            var (request, body) = handler.Requests.Single();
            Assert.AreEqual("http://gpu-box:11434/api/chat", request.RequestUri!.ToString());
            Assert.AreEqual("Bearer secret", request.Headers.Authorization!.ToString());

            using var json = JsonDocument.Parse(body);
            Assert.AreEqual("qwen3:8b", json.RootElement.GetProperty("model").GetString());
            var options = json.RootElement.GetProperty("options");
            Assert.AreEqual(8192, options.GetProperty("num_ctx").GetInt32());
            Assert.AreEqual(500, options.GetProperty("num_predict").GetInt32());
        }

        [TestMethod]
        public async Task NoModel_IsNotConfigured_AndNothingIsSent()
        {
            var (client, handler) = Create(Config(new() { ["Ollama:Model"] = "" }), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.NotConfigured, result.Failure);
            Assert.AreEqual(0, handler.Requests.Count);
        }

        [TestMethod]
        public async Task PromptOverContextWindow_IsTooLarge_WithOllamasOwnWords()
        {
            // As Ollama 0.35 sends it: the runner's error, serialized into the server's error string.
            const string body = """{"error":"{\"error\":{\"code\":400,\"message\":\"request (3630 tokens) exceeds the available context size (512 tokens), try increasing it\",\"type\":\"exceed_context_size_error\",\"n_prompt_tokens\":3630,\"n_ctx\":512}}"}""";
            var (client, _) = Create(Config(), HttpStatusCode.BadRequest, body);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.TooLarge, result.Failure);
            StringAssert.Contains(result.Text, "request (3630 tokens) exceeds the available context size (512 tokens)");
        }

        [TestMethod]
        public async Task MissingModel_IsAProviderFailure()
        {
            var (client, _) = Create(Config(), HttpStatusCode.NotFound, """{"error":"model 'nope:1b' not found"}""");

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            StringAssert.Contains(result.Text, "Ollama");
            StringAssert.Contains(result.Text, "404");
        }

        [TestMethod]
        public async Task SlowModel_IsATimeout_NotAGenericProviderError()
        {
            var client = Create(Config(), new CancellingHandler());

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Timeout, result.Failure);
            StringAssert.Contains(result.Text, "Ollama:TimeoutSeconds");
        }

        [TestMethod]
        public async Task CallerCancelling_IsNotAnswered()
        {
            // The endpoints turn this into a closed request; answering it as a failure would log an
            // error for something that is not one.
            var client = Create(Config(), new CancellingHandler());
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => client.ChatAsync(Question, cancelled.Token));
        }

        [TestMethod]
        public void InlineThinking_IsStrippedFromTheAnswer()
        {
            using var json = JsonDocument.Parse("""{"message":{"role":"assistant","content":"<think>\nhmm, locks\n</think>\n\nLock order."}}""");

            Assert.AreEqual("Lock order.", AiChatClient.ExtractOllamaSummary(json.RootElement, "fallback"));
        }

        [TestMethod]
        public async Task InstalledModels_AreListedByName()
        {
            const string tags = """{"models":[{"name":"tinyllama:latest"},{"name":"llama3.2:3b"}]}""";
            var (client, handler) = Create(Config(), HttpStatusCode.OK, tags);

            var models = await client.GetOllamaModelsAsync(CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "llama3.2:3b", "tinyllama:latest" }, models.ToArray());
            Assert.AreEqual("http://localhost:11434/api/tags", handler.Requests.Single().Request.RequestUri!.ToString());
        }

        [TestMethod]
        public void UnclosedThinking_IsNotPassedOffAsTheAnswer()
        {
            // Stopped by Ollama:MaxTokens before the model finished reasoning.
            using var json = JsonDocument.Parse("""{"message":{"role":"assistant","content":"<think>\nfirst, the locks"},"done_reason":"length"}""");

            Assert.AreEqual("fallback", AiChatClient.ExtractOllamaSummary(json.RootElement, "fallback"));
        }

        [TestMethod]
        public void TooLargeDetail_TakesAPlainStringError()
        {
            Assert.AreEqual("prompt too long", AiChatClient.TooLargeDetail("""{"error":"prompt too long"}"""));
        }

        [TestMethod]
        [DataRow("Ollama", "llama3.2:3b")]
        [DataRow("Anthropic", "claude-sonnet")]
        [DataRow("AzureOpenAI", "gpt")]
        [DataRow(null, "gpt")]
        public void ConfiguredModel_FollowsTheProvider(string? provider, string expected)
        {
            // The model stored with an analysis has to be the one that answered it, not whichever
            // provider happens to be configured first.
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = provider,
                ["Anthropic:Model"] = "claude-sonnet",
                ["AzureOpenAI:Deployment"] = "gpt",
                ["Ollama:Model"] = "llama3.2:3b"
            }).Build();

            Assert.AreEqual(expected, AiChatClient.ConfiguredModel(config));
        }
    }
}
