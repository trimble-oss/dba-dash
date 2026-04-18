using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DBADashAI.Services;

public sealed class OpenCodeChatClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const string SystemPrompt = """
        You are a senior SQL Server DBA with deep expertise in performance tuning, high availability, and production operations.
        You are reviewing live monitoring data to help an on-call DBA triage and prioritize issues.

        When responding, you MUST:
        - Lead with the most critical finding — what needs action RIGHT NOW vs. what can wait.
        - For each finding, provide: what is happening, why it likely happened (root cause hypothesis), and what the blast radius is if ignored.
        - Group findings by severity: CRITICAL, WARNING, INFORMATIONAL.
        - Call out patterns — if the same instance appears in multiple findings, say so explicitly.
        - For Availability Groups: always comment on replica sync state, failover risk, and RPO/RTO impact.
        - For restarts: distinguish between planned maintenance and unexpected/crash restarts where possible.
        - For blocking/waits: name the wait type if available and explain what workload it typically indicates.
        - For storage/backup risk: give a time estimate (e.g. "drive fills in ~6 hours at current growth rate").
        - End with a prioritized action list: what to do in the next 15 minutes, 1 hour, and 24 hours.
        - Include a confidence rating (High/Medium/Low) with a one-line reason.

        Format your response in markdown using these sections:
        ## 🔴 Immediate Actions Required
        ## ⚠️ Watch List (Next 1-4 Hours)
        ## 📋 Housekeeping (Next 24 Hours)
        ## Root Cause Analysis
        ## Prioritized Action Plan
        ## Confidence

        Be specific — always name the instance, database, or job. Never give generic advice.
        If data is missing that would change the analysis, say what you would check next.
        """;

    public Task<string> SummarizeAsync(string question, string toolName, object data, CancellationToken cancellationToken)
    {
        var payload = $"Question: {question}\nTool Used: {toolName}\nData (JSON): {JsonSerializer.Serialize(data)}\nAnswer in markdown with sections: ## Summary, ## Top Findings, ## Recommended Actions.";
        return SummarizeWithPromptAsync(payload, cancellationToken);
    }

    public async Task<string> SummarizeWithPromptAsync(string userPrompt, CancellationToken cancellationToken)
    {
        var provider = _configuration["AI:Provider"]?.Trim();

        var azureEndpoint = _configuration["AzureOpenAI:Endpoint"];
        var azureApiKey = _configuration["AzureOpenAI:ApiKey"];
        var azureDeployment = _configuration["AzureOpenAI:Deployment"];
        var azureApiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-15-preview";

        var anthropicBaseUrl = _configuration["Anthropic:BaseUrl"] ?? "https://api.anthropic.com";
        var anthropicApiKey = _configuration["Anthropic:ApiKey"];
        var anthropicModel = _configuration["Anthropic:Model"];
        var anthropicVersion = _configuration["Anthropic:Version"] ?? "2023-06-01";
        var anthropicMaxTokens = ParseInt(_configuration["Anthropic:MaxTokens"], 1024, 128, 4096);

        var baseUrl = _configuration["OpenCode:BaseUrl"];
        var apiKey = _configuration["OpenCode:ApiKey"];
        var model = _configuration["OpenCode:Model"];

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

            if (string.Equals(provider, "OpenCode", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
                {
                    return await SummarizeWithOpenCodeAsync(userPrompt, baseUrl, apiKey, model, cancellationToken);
                }
                return "AI summary is disabled. AI:Provider=OpenCode but OpenCode settings are incomplete.";
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

            if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
            {
                return await SummarizeWithOpenCodeAsync(userPrompt, baseUrl, apiKey, model, cancellationToken);
            }

            return "AI summary is disabled. Configure AzureOpenAI:* (recommended), Anthropic:* or OpenCode:* settings.";
        }
        catch (Exception ex)
        {
            return $"AI summary provider error: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private async Task<string> SummarizeWithAzureOpenAIAsync(
        string userPrompt,
        string endpoint,
        string apiKey,
        string deployment,
        string apiVersion,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var path = $"openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}";
        using var response = await client.PostAsync(path, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Azure OpenAI summary call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}";
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
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Remove("anthropic-version");

        var isAzureFoundryAnthropic = baseUrl.Contains(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase);
        if (isAzureFoundryAnthropic)
        {
            // Azure Foundry Anthropic uses api-key header AND anthropic-version
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", version);
        }
        else
        {
            // Native Anthropic endpoints use x-api-key + anthropic-version
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", version);
        }

        var payload = new
        {
            model,
            max_tokens = maxTokens,
            temperature = 0.1,
            system = SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("v1/messages", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Anthropic summary call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}";
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ExtractAnthropicSummary(document.RootElement, "No summary returned by Anthropic.");
    }

    private async Task<string> SummarizeWithOpenCodeAsync(
        string userPrompt,
        string baseUrl,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"OpenCode summary call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}";
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ExtractOpenAiStyleSummary(document.RootElement, "No summary returned by OpenCode.");
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

    private static int ParseInt(string? value, int defaultValue, int min, int max)
    {
        if (!int.TryParse(value, out var parsed)) return defaultValue;
        return Math.Clamp(parsed, min, max);
    }
}
