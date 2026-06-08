using System.Text;
using System.Text.Json;

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

        public async Task<string> SummarizeWithPromptAsync(string userPrompt, CancellationToken cancellationToken, string? modelOverride = null)
        {
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
                if (string.Equals(provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(azureEndpoint)
                        && !string.IsNullOrWhiteSpace(azureApiKey)
                        && !string.IsNullOrWhiteSpace(azureDeployment))
                    {
                        return await SummarizeWithAzureOpenAIAsync(userPrompt, azureEndpoint, azureApiKey, azureDeployment, azureApiVersion, cancellationToken);
                    }
                    return "AI summary is disabled. AI:Provider=AzureOpenAI but AzureOpenAI settings are incomplete.";
                }

                if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(anthropicApiKey) && !string.IsNullOrWhiteSpace(anthropicModel))
                    {
                        return await SummarizeWithAnthropicAsync(userPrompt, anthropicBaseUrl, anthropicApiKey, anthropicModel, anthropicVersion, anthropicMaxTokens, cancellationToken);
                    }
                    return "AI summary is disabled. AI:Provider=Anthropic but Anthropic settings are incomplete.";
                }

                if (!string.IsNullOrWhiteSpace(azureEndpoint)
                    && !string.IsNullOrWhiteSpace(azureApiKey)
                    && !string.IsNullOrWhiteSpace(azureDeployment))
                {
                    return await SummarizeWithAzureOpenAIAsync(userPrompt, azureEndpoint, azureApiKey, azureDeployment, azureApiVersion, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(anthropicApiKey) && !string.IsNullOrWhiteSpace(anthropicModel))
                {
                    return await SummarizeWithAnthropicAsync(userPrompt, anthropicBaseUrl, anthropicApiKey, anthropicModel, anthropicVersion, anthropicMaxTokens, cancellationToken);
                }

                return "AI summary is disabled. Configure AzureOpenAI:* or Anthropic:* settings.";
            }
            catch (Exception ex)
            {
                // Log the full exception (including stack trace, server names, and any connection
                // details) server-side only.  Return a correlation ID so an admin can locate the
                // log entry without exposing internal infrastructure details to the caller.
                var errorId = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError(ex, "AI provider call failed. ErrorId={ErrorId}, Provider={Provider}", errorId, provider ?? "auto");
                return $"AI provider error (ErrorId={errorId}). Check the service logs for details.";
            }
        }

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

        private async Task<string> SummarizeWithAzureOpenAIAsync(
            string userPrompt,
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

            var payload = new
            {
                model = deployment,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userPrompt }
                }
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
                return $"Azure OpenAI summary call failed: {(int)response.StatusCode} {StatusHint((int)response.StatusCode)} (ErrorId={errorId}). Check the service logs for details.";
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return ExtractOpenAiStyleSummary(document.RootElement, "No summary returned by Azure OpenAI.");
        }

        private async Task<string> SummarizeWithAnthropicAsync(
            string userPrompt,
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
                ["messages"] = new object[]
                {
                    new { role = "user", content = userPrompt }
                }
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
                return $"Anthropic summary call failed: {(int)response.StatusCode} {StatusHint((int)response.StatusCode)} (ErrorId={errorId}). Check the service logs for details.";
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return ExtractAnthropicSummary(document.RootElement, "No summary returned by Anthropic.");
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
