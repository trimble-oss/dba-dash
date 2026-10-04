using System.Text;
using System.Text.Json;
using DBADashAI.Models;

namespace DBADashAI.Services
{
    public class AiChatClient
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiChatClient> _logger;
        private readonly SystemPromptLoader _systemPromptLoader;

        public AiChatClient(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AiChatClient> logger, SystemPromptLoader systemPromptLoader)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _systemPromptLoader = systemPromptLoader;
        }

        private string SystemPrompt => _systemPromptLoader.Prompt;

        /// <summary>
        /// One question, one answer, for a caller that only displays it.
        ///
        /// The reason it can go on returning a string where <see cref="ChatAsync"/> cannot: these
        /// answers are handed straight back to whoever asked and never stored, so a sentence saying
        /// the provider refused is a perfectly good thing to show in place of one.  An analysis is
        /// stored, which is why that caller has to be able to tell the two apart.
        /// </summary>
        public async Task<string> SummarizeWithPromptAsync(string userPrompt, CancellationToken cancellationToken, string? modelOverride = null) =>
            (await ChatAsync(
                new[] { new AiConversationTurn { Role = AiConversationTurn.User, Content = userPrompt } },
                cancellationToken,
                modelOverride)).Text;

        /// <summary>
        /// Sends a conversation and returns the next answer.
        ///
        /// The service holds no conversation state: a caller with follow-up questions sends
        /// everything said so far on every turn.  Both providers take the same alternating
        /// user/assistant array, so the only difference between a first analysis and a tenth
        /// follow-up is the length of the list.
        /// </summary>
        public async Task<AiChatResult> ChatAsync(IReadOnlyList<AiConversationTurn> messages, CancellationToken cancellationToken, string? modelOverride = null)
        {
            if (messages.Count == 0)
            {
                return AiChatResult.Failed(AiChatFailure.NotConfigured, "Nothing was asked.");
            }

            var provider = _configuration["AI:Provider"]?.Trim();

            var azureEndpoint = _configuration["AzureOpenAI:Endpoint"];
            var azureApiKey = _configuration["AzureOpenAI:ApiKey"];
            var azureDeployment = _configuration["AzureOpenAI:Deployment"];
            var azureApiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-15-preview";

            var anthropicBaseUrl = _configuration["Anthropic:BaseUrl"] ?? "https://api.anthropic.com";
            var anthropicApiKey = _configuration["Anthropic:ApiKey"];
            var anthropicModel = modelOverride ?? _configuration["Anthropic:Model"];
            var anthropicVersion = _configuration["Anthropic:Version"] ?? "2023-06-01";
            var anthropicMaxTokens = ParseInt(_configuration["Anthropic:MaxTokens"], 1024, 128, 4096);

            try
            {
                if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
                {
                    var ollamaModel = modelOverride ?? _configuration["Ollama:Model"];
                    if (!string.IsNullOrWhiteSpace(ollamaModel))
                    {
                        return await SummarizeWithOllamaAsync(messages, OllamaBaseUrl(_configuration), ollamaModel, cancellationToken);
                    }
                    return AiChatResult.Failed(AiChatFailure.NotConfigured,
                        "AI summary is disabled. AI:Provider=Ollama but Ollama:Model is not set.");
                }

                if (string.Equals(provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(azureEndpoint)
                        && !string.IsNullOrWhiteSpace(azureApiKey)
                        && !string.IsNullOrWhiteSpace(azureDeployment))
                    {
                        return await SummarizeWithAzureOpenAIAsync(messages, azureEndpoint, azureApiKey, azureDeployment, azureApiVersion, cancellationToken);
                    }
                    return AiChatResult.Failed(AiChatFailure.NotConfigured,
                        "AI summary is disabled. AI:Provider=AzureOpenAI but AzureOpenAI settings are incomplete.");
                }

                if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(anthropicApiKey) && !string.IsNullOrWhiteSpace(anthropicModel))
                    {
                        return await SummarizeWithAnthropicAsync(messages, anthropicBaseUrl, anthropicApiKey, anthropicModel, anthropicVersion, anthropicMaxTokens, cancellationToken);
                    }
                    return AiChatResult.Failed(AiChatFailure.NotConfigured,
                        "AI summary is disabled. AI:Provider=Anthropic but Anthropic settings are incomplete.");
                }

                if (!string.IsNullOrWhiteSpace(azureEndpoint)
                    && !string.IsNullOrWhiteSpace(azureApiKey)
                    && !string.IsNullOrWhiteSpace(azureDeployment))
                {
                    return await SummarizeWithAzureOpenAIAsync(messages, azureEndpoint, azureApiKey, azureDeployment, azureApiVersion, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(anthropicApiKey) && !string.IsNullOrWhiteSpace(anthropicModel))
                {
                    return await SummarizeWithAnthropicAsync(messages, anthropicBaseUrl, anthropicApiKey, anthropicModel, anthropicVersion, anthropicMaxTokens, cancellationToken);
                }

                return AiChatResult.Failed(AiChatFailure.NotConfigured,
                    "AI summary is disabled. Configure AzureOpenAI:* or Anthropic:* settings, or set AI:Provider=Ollama and Ollama:Model.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller gave up.  There is nobody left to answer, and nothing went wrong here -
                // the endpoints report it as a closed request.
                throw;
            }
            catch (OperationCanceledException ex)
            {
                // HttpClient's own timeout: the provider is reachable but did not answer in time, which
                // for a local model usually means it is too slow for the request rather than broken.
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogWarning(ex, "AI provider call timed out. ErrorId={ErrorId}, Provider={Provider}", errorId, provider ?? "auto");
                var hint = string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase)
                    ? " Try a smaller or faster model, or raise Ollama:TimeoutSeconds."
                    : string.Empty;
                return AiChatResult.Failed(AiChatFailure.Timeout,
                    $"The AI provider did not answer in time (ErrorId={errorId}).{hint}", errorId);
            }
            catch (Exception ex)
            {
                // Log the full exception (including stack trace, server names, and any connection
                // details) server-side only.  Return a correlation ID so an admin can locate the
                // log entry without exposing internal infrastructure details to the caller.
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError(ex, "AI provider call failed. ErrorId={ErrorId}, Provider={Provider}", errorId, provider ?? "auto");
                return AiChatResult.Failed(AiChatFailure.Provider,
                    $"AI provider error (ErrorId={errorId}). Check the service logs for details.", errorId);
            }
        }

        /// <summary>
        /// The role as a provider will accept it.  Anything that is not an assistant turn is a user
        /// turn: both providers reject an unknown role outright, and a turn whose role got lost is
        /// still a turn the model needs to see.  Requests are validated before they reach here - this
        /// is the belt to that braces.
        /// </summary>
        private static string Role(AiConversationTurn turn) =>
            turn.IsAssistant ? AiConversationTurn.Assistant : AiConversationTurn.User;

        /// <summary>
        /// Strips any characters outside the printable ASCII range (0x20–0x7E) from a
        /// value before it is placed in an HTTP header. Non-ASCII bytes (e.g. a BOM or
        /// unicode quotes copy-pasted from a browser) cause HttpClient to throw immediately.
        /// </summary>
        internal static string SanitizeHeaderValue(string value)
        {
            var chars = value.ToCharArray();
            var clean = System.Array.FindAll(chars, c => c >= 0x20 && c <= 0x7E);
            return new string(clean);
        }

        private async Task<AiChatResult> SummarizeWithAzureOpenAIAsync(
            IReadOnlyList<AiConversationTurn> messages,
            string endpoint,
            string apiKey,
            string deployment,
            string apiVersion,
            CancellationToken cancellationToken)
        {
            // Ensure the base URI ends with '/' so Uri combination appends the path
            // rather than replacing the last path segment. This preserves the port and
            // any path prefix (e.g. a proxy at https://corp-proxy:8443/openai-gateway/).
            var baseUri = new Uri(endpoint.TrimEnd('/') + "/");
            var path = $"openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}";
            var requestUrl = new Uri(baseUri, path);

            var conversation = new List<object> { new { role = "system", content = SystemPrompt } };
            conversation.AddRange(messages.Select(m => (object)new { role = Role(m), content = m.Content }));

            var payload = new
            {
                model = deployment,
                messages = conversation
            };

            var client = _httpClientFactory.CreateClient();
            using var response = await PostWithRetryAsync(
                client,
                requestUrl,
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    req.Headers.Add("api-key", SanitizeHeaderValue(apiKey));
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    return req;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError("Azure OpenAI call failed. ErrorId={ErrorId}, Status={StatusCode}, URL={Url}, Body={Body}", LogSanitizer.SanitizeForLog(errorId), (int)response.StatusCode, requestUrl, LogSanitizer.TruncateAndSanitizeForLog(errorBody));
                return ProviderFailure("Azure OpenAI", (int)response.StatusCode, errorBody, errorId);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return AiChatResult.Answer(ExtractOpenAiStyleSummary(document.RootElement, "No summary returned by Azure OpenAI."));
        }

        private async Task<AiChatResult> SummarizeWithAnthropicAsync(
            IReadOnlyList<AiConversationTurn> messages,
            string baseUrl,
            string apiKey,
            string model,
            string version,
            int maxTokens,
            CancellationToken cancellationToken)
        {
            var requestUrl = new Uri(baseUrl.TrimEnd('/') + "/v1/messages");

            // Models like claude-opus-4-7 deprecate temperature and require adaptive thinking
            var isThinkingModel = model.Contains("opus-4-7", StringComparison.OrdinalIgnoreCase)
                               || model.Contains("mythos", StringComparison.OrdinalIgnoreCase);

            var payloadDict = new Dictionary<string, object>
            {
                ["model"] = model,
                ["max_tokens"] = maxTokens,
                ["system"] = SystemPrompt,
                ["messages"] = messages
                    .Select(m => (object)new { role = Role(m), content = m.Content })
                    .ToArray()
            };

            if (isThinkingModel)
                payloadDict["thinking"] = new { type = "adaptive" };
            else
                payloadDict["temperature"] = 0.1;

            var client = _httpClientFactory.CreateClient();
            using var response = await PostWithRetryAsync(
                client,
                requestUrl,
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    // Azure Foundry Anthropic and native Anthropic both use x-api-key + anthropic-version
                    req.Headers.Add("x-api-key", SanitizeHeaderValue(apiKey));
                    req.Headers.Add("anthropic-version", SanitizeHeaderValue(version));
                    req.Content = new StringContent(JsonSerializer.Serialize(payloadDict), Encoding.UTF8, "application/json");
                    return req;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError("Anthropic call failed. ErrorId={ErrorId}, Status={StatusCode}, Body={Body}", LogSanitizer.SanitizeForLog(errorId), (int)response.StatusCode, LogSanitizer.TruncateAndSanitizeForLog(errorBody));
                return ProviderFailure("Anthropic", (int)response.StatusCode, errorBody, errorId);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return AiChatResult.Answer(ExtractAnthropicSummary(document.RootElement, "No summary returned by Anthropic."));
        }

        internal const string DefaultOllamaBaseUrl = "http://localhost:11434";

        /// <summary>
        /// Ollama's own default context window is small (4096 tokens on most models), and a prompt
        /// built from a deadlock graph or a query plan is routinely several times that.  So the
        /// service always asks for a window of its own rather than inheriting whatever the model
        /// happens to default to.
        /// </summary>
        internal const int DefaultOllamaContextLength = 32768;

        /// <summary>
        /// Inside the GUI's wait for the service (300s, AIServiceTimeouts), so a slow model is reported
        /// as a timeout by the service rather than as a dropped connection by the client - with room
        /// left for the repository queries an Ask runs before the model is called.  Raising it past the
        /// GUI's wait only helps callers that wait longer than the GUI does.
        /// </summary>
        internal const int DefaultOllamaTimeoutSeconds = 240;

        internal static string OllamaBaseUrl(IConfiguration configuration) =>
            string.IsNullOrWhiteSpace(configuration["Ollama:BaseUrl"]) ? DefaultOllamaBaseUrl : configuration["Ollama:BaseUrl"]!;

        /// <summary>
        /// The model a request will use when the caller does not pick one: the one belonging to the
        /// configured provider.  With no provider set, the same order <see cref="ChatAsync"/> falls
        /// back through.
        /// </summary>
        public static string? ConfiguredModel(IConfiguration configuration)
        {
            var provider = configuration["AI:Provider"]?.Trim();

            if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
                return NullIfBlank(configuration["Ollama:Model"]);
            if (string.Equals(provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
                return NullIfBlank(configuration["AzureOpenAI:Deployment"]);
            if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
                return NullIfBlank(configuration["Anthropic:Model"]);

            return NullIfBlank(configuration["AzureOpenAI:Deployment"]) ?? NullIfBlank(configuration["Anthropic:Model"]);
        }

        private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        /// <summary>
        /// Ollama, over its native chat API rather than the OpenAI-compatible one.
        ///
        /// The native API is the only one that takes the context window per request (num_ctx), and
        /// the only one that can be told to refuse an oversized prompt (truncate=false).  Without it
        /// Ollama silently drops the start of the conversation to make it fit, which here is the system
        /// prompt and most of the artifact, and the answer that comes back is about something else.
        /// </summary>
        private async Task<AiChatResult> SummarizeWithOllamaAsync(
            IReadOnlyList<AiConversationTurn> messages,
            string baseUrl,
            string model,
            CancellationToken cancellationToken)
        {
            var requestUrl = new Uri(baseUrl.TrimEnd('/') + "/api/chat");
            var apiKey = _configuration["Ollama:ApiKey"];
            var contextLength = ParseInt(_configuration["Ollama:ContextLength"], DefaultOllamaContextLength, 2048, 1048576);
            var timeoutSeconds = ParseInt(_configuration["Ollama:TimeoutSeconds"], DefaultOllamaTimeoutSeconds, 30, 3600);

            var options = new Dictionary<string, object>
            {
                ["num_ctx"] = contextLength,
                ["temperature"] = 0.1
            };
            // Left unset, the answer runs until the model stops - which matters for a reasoning model,
            // whose thinking counts against the same limit.
            if (int.TryParse(_configuration["Ollama:MaxTokens"], out var maxTokens) && maxTokens > 0)
                options["num_predict"] = maxTokens;

            var conversation = new List<object> { new { role = "system", content = SystemPrompt } };
            conversation.AddRange(messages.Select(m => (object)new { role = Role(m), content = m.Content }));

            var payload = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = conversation,
                ["stream"] = false,
                ["truncate"] = false,
                ["options"] = options
            };

            // A local model can take minutes over a large plan, past HttpClient's default of 100s.
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            using var response = await PostWithRetryAsync(
                client,
                requestUrl,
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    // Ollama itself takes no key.  One is only needed behind a reverse proxy, or for ollama.com.
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        req.Headers.Add("Authorization", "Bearer " + SanitizeHeaderValue(apiKey));
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    return req;
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError("Ollama call failed. ErrorId={ErrorId}, Status={StatusCode}, URL={Url}, Body={Body}", LogSanitizer.SanitizeForLog(errorId), (int)response.StatusCode, requestUrl, LogSanitizer.TruncateAndSanitizeForLog(errorBody));
                return ProviderFailure("Ollama", (int)response.StatusCode, errorBody, errorId);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return AiChatResult.Answer(ExtractOllamaSummary(document.RootElement, "No summary returned by Ollama."));
        }

        /// <summary>
        /// The answer, without the reasoning.  Current Ollama returns a reasoning model's thinking in a
        /// field of its own, but some models - and older Ollama versions - leave it inline in
        /// &lt;think&gt; tags, and nobody asked to read it.  A model stopped by Ollama:MaxTokens while
        /// still thinking leaves the tag unclosed, and then there is no answer at all - only reasoning.
        /// </summary>
        internal static string ExtractOllamaSummary(JsonElement root, string fallbackMessage)
        {
            if (!root.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String)
            {
                return fallbackMessage;
            }

            var text = System.Text.RegularExpressions.Regex.Replace(
                content.GetString() ?? string.Empty,
                @"^\s*<think>.*?(</think>|$)",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

            return string.IsNullOrWhiteSpace(text) ? fallbackMessage : text;
        }

        /// <summary>
        /// The models Ollama has pulled, by name.  Unlike the hosted providers there is no fixed list to
        /// keep in the repository: what can be asked for is whatever is installed on that server.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetOllamaModelsAsync(CancellationToken cancellationToken)
        {
            var requestUrl = new Uri(OllamaBaseUrl(_configuration).TrimEnd('/') + "/api/tags");
            var apiKey = _configuration["Ollama:ApiKey"];

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("Authorization", "Bearer " + SanitizeHeaderValue(apiKey));

            // Only fills a drop-down, so an unreachable server should not hold the GUI up for long.
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();

            return models.EnumerateArray()
                .Where(m => m.ValueKind == JsonValueKind.Object && m.TryGetProperty("name", out _))
                .Select(m => m.GetProperty("name").GetString())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ExtractAnthropicSummary(JsonElement root, string fallbackMessage)
        {
            if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                return fallbackMessage;
            }

            var textParts = content.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object
                            && e.TryGetProperty("type", out var type)
                            && string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase)
                            && e.TryGetProperty("text", out _))
                .Select(e => e.GetProperty("text").GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var text = string.Join("\n", textParts);
            return string.IsNullOrWhiteSpace(text) ? fallbackMessage : text;
        }

        private static string ExtractOpenAiStyleSummary(JsonElement root, string fallbackMessage)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            {
                return fallbackMessage;
            }

            var first = choices[0];
            if (!first.TryGetProperty("message", out var message))
            {
                return fallbackMessage;
            }

            if (!message.TryGetProperty("content", out var content))
            {
                return fallbackMessage;
            }

            var text = content.ValueKind switch
            {
                JsonValueKind.String => content.GetString(),
                JsonValueKind.Array => string.Join("\n", content.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("text", out _))
                    .Select(e => e.GetProperty("text").GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
                _ => null
            };

            return string.IsNullOrWhiteSpace(text) ? fallbackMessage : text;
        }

        /// <summary>
        /// A provider that refused, turned into something the caller can act on.
        ///
        /// Everything else about a failed call stays as it was - a status code, a hint, and an
        /// ErrorId to find the logged body by - because there is nothing a caller can do with the
        /// detail of an expired key or an overloaded region except report it.
        /// </summary>
        private static AiChatResult ProviderFailure(string provider, int status, string body, string errorId)
        {
            if (!IsTooLarge(status, body))
            {
                return AiChatResult.Failed(
                    AiChatFailure.Provider,
                    $"{provider} summary call failed: {status} {StatusHint(status)} (ErrorId={errorId}). Check the service logs for details.",
                    errorId,
                    status);
            }

            var detail = TooLargeDetail(body);

            return AiChatResult.Failed(
                AiChatFailure.TooLarge,
                $"The request is larger than the model will accept{(detail is null ? string.Empty : ": " + detail)} (ErrorId={errorId}).",
                errorId,
                status);
        }

        /// <summary>
        /// Whether the refusal was about size.  It is the one provider failure the caller can do
        /// something about - send less - so it is worth telling apart from the rest.
        ///
        /// No provider gives it a status code of its own.  Anthropic answers 413 for a request over
        /// the transport limit but 400 for a prompt over the model's context window, Azure OpenAI
        /// answers 400 with the reason as a code in the body, and Ollama - told not to truncate -
        /// answers 400 naming the context size.  So the body is what decides, and it is matched
        /// loosely: the wording of these messages is not a contract.
        /// </summary>
        internal static bool IsTooLarge(int status, string body)
        {
            if (status == 413) return true;
            if (status != 400 || string.IsNullOrEmpty(body)) return false;

            return body.Contains("context_length_exceeded", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("exceed_context_size", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("exceeds the available context size", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("request_too_large", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("string_above_max_length", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("prompt is too long", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("maximum context length", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The provider's own words for it, which are the useful part: "prompt is too long: 250000
        /// tokens > 200000 maximum" tells the reader how much to cut, and anything written here
        /// could only guess at it.  Truncated and stripped of control characters like anything else
        /// that crosses back to a caller, and left out altogether when the body is not the shape
        /// the providers document.
        ///
        /// Ollama's error is a string rather than an object, and when it comes from the model runner
        /// that string is itself the documented shape, serialized a second time.
        /// </summary>
        internal static string? TooLargeDetail(string body, int depth = 0)
        {
            try
            {
                using var document = JsonDocument.Parse(body);

                if (!document.RootElement.TryGetProperty("error", out var error))
                    return null;

                string? message = null;
                if (error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String)
                {
                    message = messageElement.GetString();
                }
                else if (error.ValueKind == JsonValueKind.String)
                {
                    message = error.GetString();
                    if (depth == 0 && message is not null && message.TrimStart().StartsWith('{'))
                        return TooLargeDetail(message, depth + 1) ?? Clean(message);
                }

                return Clean(message);
            }
            catch (JsonException)
            {
                // A provider - or a proxy in front of one - answering with something other than the
                // documented shape.  The status and the ErrorId still say what happened.
            }

            return null;

            static string? Clean(string? message)
            {
                var text = LogSanitizer.TruncateAndSanitizeForLog(message, 300);
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
        }

        private static string StatusHint(int statusCode) => statusCode switch
        {
            400 => "(bad request — check model/deployment name)",
            401 => "(unauthorized — API key may be invalid or expired)",
            403 => "(forbidden — check permissions on the AI resource)",
            404 => "(not found — check endpoint URL and deployment name)",
            429 => "(rate limited — retry later)",
            500 => "(provider internal error)",
            502 => "(bad gateway — provider may be temporarily unavailable)",
            503 => "(service unavailable — provider may be overloaded)",
            504 => "(gateway timeout — provider did not respond in time)",
            _ => ""
        };

        private static int ParseInt(string? value, int defaultValue, int min, int max)
        {
            if (!int.TryParse(value, out var parsed)) return defaultValue;
            return Math.Clamp(parsed, min, max);
        }

        private const int MaxRetries = 3;

        private static bool IsTransient(HttpResponseMessage response)
        {
            var code = (int)response.StatusCode;
            return code is 429 or 500 or 502 or 503 or 504;
        }

        private static async Task<HttpResponseMessage> PostWithRetryAsync(
            HttpClient client,
            Uri requestUri,
            Func<HttpRequestMessage> requestFactory,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;

            for (var attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, cancellationToken);
                }

                // Each attempt gets a fresh HttpRequestMessage and HttpContent from the factory
                using var request = requestFactory();
                response = await client.SendAsync(request, cancellationToken);

                if (!IsTransient(response) || attempt == MaxRetries)
                    break;

                response.Dispose();
                response = null;
            }

            return response!;
        }
    }
}
