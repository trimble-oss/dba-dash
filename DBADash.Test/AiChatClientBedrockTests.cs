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
    /// The Amazon Bedrock provider, against a fake server.
    ///
    /// Bedrock is called with an API key over the Converse API, so the request is plain JSON with a
    /// bearer token - which is what these check.  Every request goes to <see cref="FakeHandler"/>; no
    /// AWS account is needed.
    /// </summary>
    [TestClass]
    public class AiChatClientBedrockTests
    {
        /// <summary>Answers each request with the first response whose URL fragment it contains.</summary>
        private sealed class FakeHandler(params (string UrlContains, HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
        {
            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, content));
                var (_, status, body) = responses.First(r => request.RequestUri!.ToString().Contains(r.UrlContains, StringComparison.Ordinal));
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
                ["AI:Provider"] = "Bedrock",
                ["Bedrock:Region"] = "eu-west-2",
                ["Bedrock:ApiKey"] = "bedrock-key",
                ["Bedrock:Model"] = "eu.anthropic.claude-sonnet-4-6"
            };
            foreach (var kv in extra ?? new()) values[kv.Key] = kv.Value;
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static (AiChatClient Client, FakeHandler Handler) Create(IConfiguration config, params (string, HttpStatusCode, string)[] responses)
        {
            var handler = new FakeHandler(responses);
            var client = new AiChatClient(config,
                new FakeFactory(handler),
                NullLogger<AiChatClient>.Instance,
                new SystemPromptLoader(config, NullLogger<SystemPromptLoader>.Instance));
            return (client, handler);
        }

        private static (AiChatClient Client, FakeHandler Handler) Create(IConfiguration config, HttpStatusCode status, string body) =>
            Create(config, ("", status, body));

        private static readonly AiConversationTurn[] Question =
            { new() { Role = AiConversationTurn.User, Content = "Why did this deadlock?" } };

        private const string Answer = """{"output":{"message":{"role":"assistant","content":[{"text":"Lock order."}]}},"stopReason":"end_turn","usage":{"inputTokens":10,"outputTokens":3,"totalTokens":13}}""";

        [TestMethod]
        public async Task Request_GoesToConverse_WithBearerKeyAndSystemPrompt()
        {
            var (client, handler) = Create(Config(), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual("Lock order.", result.Text);
            var (request, body) = handler.Requests.Single();
            Assert.AreEqual("https://bedrock-runtime.eu-west-2.amazonaws.com/model/eu.anthropic.claude-sonnet-4-6/converse", request.RequestUri!.ToString());
            Assert.AreEqual("Bearer bedrock-key", request.Headers.Authorization!.ToString());

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("system")[0].GetProperty("text").GetString()));
            var message = root.GetProperty("messages").EnumerateArray().Single();
            Assert.AreEqual("user", message.GetProperty("role").GetString());
            Assert.AreEqual("Why did this deadlock?", message.GetProperty("content")[0].GetProperty("text").GetString());
            // Left to the model unless set: a fixed default would be too high for some models.
            Assert.IsFalse(root.TryGetProperty("inferenceConfig", out _));
        }

        [TestMethod]
        public async Task ModelOverrideAndMaxTokens_AreHonoured_AndAnArnStaysOneSegment()
        {
            const string arn = "arn:aws:bedrock:eu-west-2:123456789012:application-inference-profile/abc123";
            var (client, handler) = Create(Config(new() { ["Bedrock:MaxTokens"] = "2000" }), HttpStatusCode.OK, Answer);

            await client.ChatAsync(Question, CancellationToken.None, modelOverride: arn);

            var (request, body) = handler.Requests.Single();
            Assert.AreEqual("/model/" + Uri.EscapeDataString(arn) + "/converse", request.RequestUri!.AbsolutePath);
            using var json = JsonDocument.Parse(body);
            Assert.AreEqual(2000, json.RootElement.GetProperty("inferenceConfig").GetProperty("maxTokens").GetInt32());
        }

        [TestMethod]
        [DataRow("https://api.claude.aws.example.com/converse", "https://api.claude.aws.example.com/converse")]
        [DataRow("https://gw.example.com/bedrock/model/{model}/converse", "https://gw.example.com/bedrock/model/eu.anthropic.claude-sonnet-4-6/converse")]
        public async Task Url_SendsConverseThere_WithTheSameRequest(string url, string expected)
        {
            // A gateway with its own key: no region needed.
            var (client, handler) = Create(Config(new() { ["Bedrock:Url"] = url, ["Bedrock:Region"] = "" }), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual("Lock order.", result.Text);
            var (request, body) = handler.Requests.Single();
            Assert.AreEqual(expected, request.RequestUri!.ToString());
            Assert.AreEqual("Bearer bedrock-key", request.Headers.Authorization!.ToString());
            using var json = JsonDocument.Parse(body);
            Assert.AreEqual("Why did this deadlock?", json.RootElement.GetProperty("messages")[0].GetProperty("content")[0].GetProperty("text").GetString());
        }

        [TestMethod]
        public async Task Url_ListsNoModels_AndSendsNothing()
        {
            var (client, handler) = Create(Config(new() { ["Bedrock:Url"] = "https://api.claude.aws.example.com/converse" }), HttpStatusCode.OK, "{}");

            var models = await client.GetBedrockModelsAsync(CancellationToken.None);

            Assert.AreEqual(0, models.Count);
            Assert.AreEqual(0, handler.Requests.Count);
        }

        [TestMethod]
        public async Task Url_WithoutAKey_StillNeedsTheRegionToSignOne()
        {
            var config = Config(new() { ["Bedrock:Url"] = "https://gw.example.com/converse", ["Bedrock:ApiKey"] = "", ["Bedrock:Region"] = "" });
            var (client, handler) = Create(config, HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.NotConfigured, result.Failure);
            StringAssert.Contains(result.Text, "Bedrock:Region");
            Assert.AreEqual(0, handler.Requests.Count);
        }

        [TestMethod]
        [DataRow("Bedrock:Model", "")]
        [DataRow("Bedrock:Region", "")]
        [DataRow("Bedrock:Region", "evil.example.com/x")]
        [DataRow("Bedrock:Url", "api.claude.aws.example.com/converse")]
        [DataRow("Bedrock:Url", "ftp://gw.example.com/converse")]
        public async Task IncompleteSettings_AreNotConfigured_AndNothingIsSent(string key, string value)
        {
            var (client, handler) = Create(Config(new() { [key] = value }), HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.NotConfigured, result.Failure);
            Assert.AreEqual(0, handler.Requests.Count);
        }

        [TestMethod]
        public async Task NoKeyAndNoCredentials_IsNotConfigured_AndNothingIsSent()
        {
            var config = Config(new() { ["Bedrock:ApiKey"] = "", ["Bedrock:Profile"] = "dbadash-test-no-such-profile" });
            var (client, handler) = Create(config, HttpStatusCode.OK, Answer);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.NotConfigured, result.Failure);
            StringAssert.Contains(result.Text, "dbadash-test-no-such-profile");
            Assert.AreEqual(0, handler.Requests.Count);
        }

        /// <summary>
        /// Short-term keys made from AWS credentials, against keys made by AWS's own generator
        /// (aws-bedrock-token-generator 1.x for Python, on botocore) with the same credentials at the
        /// same moment.  Bedrock checks the signature, so only an exact match will be accepted.
        /// </summary>
        [TestMethod]
        [DataRow(null, "bedrock-api-key-YmVkcm9jay5hbWF6b25hd3MuY29tLz9BY3Rpb249Q2FsbFdpdGhCZWFyZXJUb2tlbiZYLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSURFWEFNUExFJTJGMjAyNjEwMTYlMkZldS13ZXN0LTIlMkZiZWRyb2NrJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNjEwMTZUMTIzNDU2WiZYLUFtei1FeHBpcmVzPTQzMjAwJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZYLUFtei1TaWduYXR1cmU9OWU4NmU1MWY5NTk0ZGQ3OTkzMTA3YjViZmY3ZTJlNTlkNGViZGExNzRkODYxM2ZkOWQ1YzA4YTZlMmZjMWFlZCZWZXJzaW9uPTE=")]
        [DataRow("FwoGZXIvYXdzEBYaDH+session/token==", "bedrock-api-key-YmVkcm9jay5hbWF6b25hd3MuY29tLz9BY3Rpb249Q2FsbFdpdGhCZWFyZXJUb2tlbiZYLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSURFWEFNUExFJTJGMjAyNjEwMTYlMkZldS13ZXN0LTIlMkZiZWRyb2NrJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNjEwMTZUMTIzNDU2WiZYLUFtei1FeHBpcmVzPTQzMjAwJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZYLUFtei1TZWN1cml0eS1Ub2tlbj1Gd29HWlhJdllYZHpFQllhREglMkJzZXNzaW9uJTJGdG9rZW4lM0QlM0QmWC1BbXotU2lnbmF0dXJlPWM1ZDdjMTIwZDM0ZmM0OTViNDE0MDVlMGZmMTQyNWNkYjIxOTM2MTM0NTZiOTZhMTlhNzFjMTFmZmNjODgzYmEmVmVyc2lvbj0x")]
        public void ShortTermKey_MatchesAwsTokenGenerator(string? sessionToken, string expected)
        {
            var token = BedrockToken.Create("AKIDEXAMPLE", "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY", sessionToken, "eu-west-2",
                new DateTime(2026, 10, 16, 12, 34, 56, DateTimeKind.Utc));

            Assert.AreEqual(expected, token);
        }

        /// <summary>Answers requests in turn, one response each.</summary>
        private sealed class SequenceHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
        {
            public List<string> Bodies { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
                var (status, body) = responses[Bodies.Count - 1];
                return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            }
        }

        private static AiChatClient Create(IConfiguration config, HttpMessageHandler handler) =>
            new(config, new FakeFactory(handler), NullLogger<AiChatClient>.Instance,
                new SystemPromptLoader(config, NullLogger<SystemPromptLoader>.Instance));

        [TestMethod]
        public async Task SystemPromptRefused_IsResentInTheFirstMessage()
        {
            var handler = new SequenceHandler(
                (HttpStatusCode.BadRequest, """{"message":"This model doesn't support system messages."}"""),
                (HttpStatusCode.OK, Answer));
            var config = Config();
            var client = Create(config, handler);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual("Lock order.", result.Text);
            Assert.AreEqual(2, handler.Bodies.Count);
            using var retry = JsonDocument.Parse(handler.Bodies[1]);
            Assert.IsFalse(retry.RootElement.TryGetProperty("system", out _));
            var text = retry.RootElement.GetProperty("messages")[0].GetProperty("content")[0].GetProperty("text").GetString()!;
            var systemPrompt = new SystemPromptLoader(config, NullLogger<SystemPromptLoader>.Instance).Prompt;
            StringAssert.StartsWith(text, systemPrompt);
            StringAssert.EndsWith(text, "Why did this deadlock?");
        }

        [TestMethod]
        public async Task OtherBadRequests_AreNotResent()
        {
            var handler = new SequenceHandler((HttpStatusCode.BadRequest, """{"message":"The provided model identifier is invalid."}"""));
            var client = Create(Config(), handler);

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            Assert.AreEqual(1, handler.Bodies.Count);
        }

        [TestMethod]
        public async Task InputTooLong_IsTooLarge_WithBedrocksOwnWords()
        {
            var (client, _) = Create(Config(), HttpStatusCode.BadRequest, """{"message":"Input is too long for requested model."}""");

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.TooLarge, result.Failure);
            StringAssert.Contains(result.Text, "Input is too long for requested model.");
        }

        [TestMethod]
        public async Task AccessDenied_IsAProviderFailure()
        {
            var (client, _) = Create(Config(), HttpStatusCode.Forbidden, """{"message":"You don't have access to the model with the specified model ID."}""");

            var result = await client.ChatAsync(Question, CancellationToken.None);

            Assert.AreEqual(AiChatFailure.Provider, result.Failure);
            StringAssert.Contains(result.Text, "Bedrock");
            StringAssert.Contains(result.Text, "403");
        }

        [TestMethod]
        public void ReasoningBlocks_AreLeftOutOfTheAnswer()
        {
            using var json = JsonDocument.Parse("""{"output":{"message":{"role":"assistant","content":[{"reasoningContent":{"reasoningText":{"text":"hmm, locks"}}},{"text":"Lock order."}]}}}""");

            Assert.AreEqual("Lock order.", AiChatClient.ExtractBedrockSummary(json.RootElement, "fallback"));
        }

        [TestMethod]
        public async Task Models_AreChatProfilesFirst_ThenActiveOnDemandModels()
        {
            const string application = """{"inferenceProfileSummaries":[{"inferenceProfileId":"abc123","inferenceProfileArn":"arn:aws:bedrock:eu-west-2:123456789012:application-inference-profile/abc123","inferenceProfileName":"DBA Dash","status":"ACTIVE","type":"APPLICATION","models":[{"modelArn":"arn:aws:bedrock:eu-west-2::foundation-model/anthropic.claude-sonnet-4-6"}]}]}""";
            // An embedding model's profile: listed by Bedrock alongside the rest, but it cannot answer.
            const string system = """{"inferenceProfileSummaries":[{"inferenceProfileId":"eu.anthropic.claude-sonnet-4-6","inferenceProfileName":"EU Anthropic Claude Sonnet 4.6","status":"ACTIVE","type":"SYSTEM_DEFINED","models":[{"modelArn":"arn:aws:bedrock:eu-west-1::foundation-model/anthropic.claude-sonnet-4-6"},{"modelArn":"arn:aws:bedrock:eu-west-2::foundation-model/anthropic.claude-sonnet-4-6"}]},{"inferenceProfileId":"eu.cohere.embed-v4:0","inferenceProfileName":"EU Cohere Embed v4","status":"ACTIVE","type":"SYSTEM_DEFINED","models":[{"modelArn":"arn:aws:bedrock:eu-west-2::foundation-model/cohere.embed-v4:0"}]}]}""";
            // Sonnet can only be called through a profile, so it is not listed as a base model too.
            const string foundation = """{"modelSummaries":[{"modelId":"anthropic.claude-sonnet-4-6","modelName":"Claude Sonnet 4.6","providerName":"Anthropic","inputModalities":["TEXT","IMAGE"],"inferenceTypesSupported":["INFERENCE_PROFILE"],"modelLifecycle":{"status":"ACTIVE"}},{"modelId":"mistral.mistral-large-2402-v1:0","modelName":"Mistral Large","providerName":"Mistral AI","inputModalities":["TEXT"],"inferenceTypesSupported":["ON_DEMAND"],"modelLifecycle":{"status":"ACTIVE"}},{"modelId":"old.model-v1","modelName":"Old","providerName":"X","inputModalities":["TEXT"],"inferenceTypesSupported":["ON_DEMAND"],"modelLifecycle":{"status":"LEGACY"}}]}""";
            var (client, handler) = Create(Config(),
                ("type=APPLICATION", HttpStatusCode.OK, application),
                ("type=SYSTEM_DEFINED", HttpStatusCode.OK, system),
                ("/foundation-models", HttpStatusCode.OK, foundation));

            var models = await client.GetBedrockModelsAsync(CancellationToken.None);

            CollectionAssert.AreEqual(new[]
            {
                "arn:aws:bedrock:eu-west-2:123456789012:application-inference-profile/abc123",
                "eu.anthropic.claude-sonnet-4-6",
                "mistral.mistral-large-2402-v1:0"
            }, models.Select(m => m.Id).ToArray());
            Assert.AreEqual("Mistral AI Mistral Large", models[2].Name);
            Assert.IsTrue(handler.Requests.All(r => r.Request.RequestUri!.Host == "bedrock.eu-west-2.amazonaws.com"));
            Assert.IsTrue(handler.Requests.All(r => r.Request.Headers.Authorization!.ToString() == "Bearer bedrock-key"));
        }

        [TestMethod]
        public void ConfiguredModel_IsBedrocksModel()
        {
            Assert.AreEqual("eu.anthropic.claude-sonnet-4-6", AiChatClient.ConfiguredModel(Config(new() { ["Anthropic:Model"] = "claude-sonnet" })));
        }
    }
}
