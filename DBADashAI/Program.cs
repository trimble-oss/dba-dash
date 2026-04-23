using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using DBADash;
using DBADashAI.Models;
using DBADashAI.Services;
using DBADashAI.Services.Tools;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

LoadSettingsFromServiceConfig(builder.Configuration);

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    var managedIdentityClientId = builder.Configuration["KeyVault:ManagedIdentityClientId"];
    var credentialOptions = new DefaultAzureCredentialOptions();

    if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
    {
        credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
    }

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential(credentialOptions),
        new AzureKeyVaultConfigurationOptions());
}

builder.Host.UseWindowsService();
builder.Services.AddHttpClient();
builder.Services.AddScoped<SqlToolExecutor>();
builder.Services.AddScoped<AiChatClient>();
builder.Services.AddScoped<AiRequestTelemetryService>();
builder.Services.AddScoped<AiIntentRouter>();
builder.Services.AddScoped<AiConfidenceScorer>();
builder.Services.AddScoped<AiSummaryFormatter>();
builder.Services.AddScoped<AiEvidenceRanker>();
builder.Services.AddSingleton<AiFeedbackStore>();
builder.Services.AddScoped<AiRcaTemplateService>();
builder.Services.AddScoped<AiRunbookLinkService>();
builder.Services.AddScoped<AiRiskForecastService>();

builder.Services.AddScoped<IAiTool, ActiveAlertsSummaryTool>();
builder.Services.AddScoped<IAiTool, BlockingSummaryTool>();
builder.Services.AddScoped<IAiTool, AgentJobAlertsTool>();
builder.Services.AddScoped<IAiTool, WaitsSummaryTool>();
builder.Services.AddScoped<IAiTool, DeadlocksSummaryTool>();
builder.Services.AddScoped<IAiTool, SlowQueriesSummaryTool>();
builder.Services.AddScoped<IAiTool, BackupsRiskSummaryTool>();
builder.Services.AddScoped<IAiTool, DrivesRiskSummaryTool>();
builder.Services.AddScoped<IAiTool, ConfigDriftSummaryTool>();
builder.Services.AddScoped<IAiTool, InstanceMetadataSummaryTool>();
builder.Services.AddScoped<IAiTool, AgDrRiskSummaryTool>();
builder.Services.AddScoped<IAiTool, WorkloadPressureSummaryTool>();
builder.Services.AddScoped<IAiTool, ReliabilityRiskSummaryTool>();
builder.Services.AddScoped<IAiTool, CapacityForecastSummaryTool>();
builder.Services.AddScoped<IAiTool, ConfigRiskDriftSummaryTool>();
builder.Services.AddScoped<IAiTool, ConfigCurrentSummaryTool>();
builder.Services.AddScoped<IAiTool, DbConfigSummaryTool>();
builder.Services.AddScoped<IAiTool, RunningQueriesSummaryTool>();
builder.Services.AddScoped<IAiTool, IdentityColumnsSummaryTool>();
builder.Services.AddScoped<IAiTool, StorageSpaceSummaryTool>();
builder.Services.AddScoped<IAiTool, TableSizeSummaryTool>();
builder.Services.AddScoped<IAiTool, OSPerformanceSummaryTool>();
builder.Services.AddScoped<IAiTool, OperationalHygieneSummaryTool>();
builder.Services.AddScoped<IAiTool, CrossSignalCorrelationSummaryTool>();

var app = builder.Build();

app.MapGet("/api/ai/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.MapGet("/api/ai/diagnostics", (IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var provider = config["AI:Provider"] ?? "(not set - auto)";
    var repoCs = config.GetConnectionString("Repository") ?? "(not set)";
    var repoSafe = System.Text.RegularExpressions.Regex.Replace(repoCs, @"(?i)(password|pwd|api.?key)\s*=\s*[^;]+", "$1=***");

    var azureEndpoint = config["AzureOpenAI:Endpoint"] ?? "";
    var azureKey = string.IsNullOrWhiteSpace(config["AzureOpenAI:ApiKey"]) ? "(not set)" : "***set***";
    var azureDeployment = config["AzureOpenAI:Deployment"] ?? "(not set)";

    var anthropicUrl = config["Anthropic:BaseUrl"] ?? "(not set)";
    var anthropicKey = string.IsNullOrWhiteSpace(config["Anthropic:ApiKey"]) ? "(not set)" : "***set***";
    var anthropicModel = config["Anthropic:Model"] ?? "(not set)";
    var anthropicVersion = config["Anthropic:Version"] ?? "(not set)";

    return Results.Ok(new
    {
        provider,
        repository = repoSafe,
        azureOpenAI = new { endpoint = azureEndpoint, apiKey = azureKey, deployment = azureDeployment },
        anthropic = new { baseUrl = anthropicUrl, apiKey = anthropicKey, model = anthropicModel, version = anthropicVersion },
        utc = DateTime.UtcNow
    });
});

app.MapGet("/api/ai/test-anthropic", async (IConfiguration config, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var baseUrl = (config["Anthropic:BaseUrl"] ?? "https://api.anthropic.com").TrimEnd('/') + "/";
    var apiKey = config["Anthropic:ApiKey"] ?? "";
    var model = config["Anthropic:Model"] ?? "";
    var version = config["Anthropic:Version"] ?? "2023-06-01";
    var isFoundry = baseUrl.Contains(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
    {
        return Results.BadRequest(new { error = "Anthropic:ApiKey or Anthropic:Model not configured." });
    }

    try
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("anthropic-version", version);
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var payload = new
        {
            model,
            max_tokens = 64,
            messages = new[] { new { role = "user", content = "Reply with only: OK" } }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("v1/messages", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return Results.Ok(new
        {
            statusCode = (int)response.StatusCode,
            success = response.IsSuccessStatusCode,
            baseUrl,
            model,
            version,
            isFoundry,
            responseBody = body
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { error = ex.Message, baseUrl, model, version, isFoundry });
    }
});

app.MapPost("/api/ai/feedback", (
    AiFeedbackRequest request,
    AiFeedbackStore store,
    ILoggerFactory loggerFactory) =>
{
    var validationError = request.Validate();
    if (!string.IsNullOrWhiteSpace(validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }

    store.Add(request);

    var logger = loggerFactory.CreateLogger("AiFeedback");
    logger.LogInformation("AI feedback received. RequestId={RequestId}, Helpful={Helpful}, Category={Category}",
        request.RequestId,
        request.IsHelpful,
        request.Category ?? string.Empty);

    return Results.Ok(new { status = "saved" });
});

app.MapGet("/api/ai/feedback", (AiFeedbackStore store) =>
{
    var records = store.GetRecent(200);
    return Results.Ok(records);
});

app.MapGet("/api/ai/tools", (IEnumerable<IAiTool> tools) =>
{
    var result = tools.Select(t => new
    {
        t.Name,
        t.Description,
        t.InputHint,
        t.Keywords
    });

    return Results.Ok(result);
});

app.MapGet("/api/ai/examples", async (SqlToolExecutor sql, CancellationToken cancellationToken) =>
{
    var rows = await sql.QueryNoParamsAsync("DBADash.AIExampleQuestions_Get", cancellationToken);

    var grouped = rows
        .GroupBy(r => r.TryGetValue("Category", out var c) && c is not null ? c.ToString() ?? "" : "")
        .Where(g => !string.IsNullOrWhiteSpace(g.Key))
        .Select(g => new
        {
            category = g.Key,
            questions = g.Select(r => r.TryGetValue("Question", out var q) && q is not null ? q.ToString() ?? "" : "")
                         .Where(q => !string.IsNullOrWhiteSpace(q))
                         .ToList()
        })
        .OrderBy(g => g.category)
        .ToList();

    return Results.Ok(grouped);
});

app.MapGet("/api/ai/models", async (SqlToolExecutor sql, CancellationToken cancellationToken) =>
{
    try
    {
        var rows = await sql.QueryNoParamsAsync("DBADash.AIModels_Get", cancellationToken);
        var models = rows.Select(r => new
        {
            modelName = r.TryGetValue("ModelName", out var m) && m is not null ? m.ToString() : "",
            displayName = r.TryGetValue("DisplayName", out var d) && d is not null ? d.ToString() : ""
        }).Where(m => !string.IsNullOrWhiteSpace(m.modelName)).ToList();

        return Results.Ok(models);
    }
    catch
    {
        return Results.Ok(Array.Empty<object>());
    }
});

app.MapPost("/api/ai/ask", async (
    AiAskRequest request,
    IEnumerable<IAiTool> tools,
    AiIntentRouter router,
    AiChatClient aiChat,
    AiRequestTelemetryService telemetry,
    AiConfidenceScorer confidenceScorer,
    AiSummaryFormatter summaryFormatter,
    AiEvidenceRanker evidenceRanker,
    AiRcaTemplateService rcaTemplateService,
    CancellationToken cancellationToken) =>
{
    var validationError = request.Validate();
    if (!string.IsNullOrWhiteSpace(validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }

    var requestId = Guid.NewGuid().ToString("N");
    var totalSw = telemetry.Start(requestId, request.Question, request.ToolName);

    try
    {
        var selectedTools = router.SelectTools(request, tools);
        if (selectedTools.Count == 0)
        {
            return Results.BadRequest(new { error = "No tool available." });
        }

        var toolResults = new List<AiToolExecutionResult>();
        var allEvidence = new List<AiEvidenceItem>();

        foreach (var tool in selectedTools)
        {
            var toolSw = Stopwatch.StartNew();
            var result = await tool.RunAsync(request, cancellationToken);
            toolSw.Stop();

            toolResults.Add(new AiToolExecutionResult
            {
                Tool = tool.Name,
                Data = result.Data,
                Evidence = result.Evidence,
                RowCount = result.RowCount,
                ExecutionMs = toolSw.ElapsedMilliseconds
            });
            allEvidence.AddRange(result.Evidence);
        }

        var primaryTool = selectedTools[0].Name;
        var rankedEvidence = evidenceRanker.Rank(allEvidence);
        var confidence = confidenceScorer.Score(toolResults, rankedEvidence);

        var response = new AiAskResponse
        {
            RequestId = requestId,
            Tool = selectedTools.Count == 1 ? primaryTool : "multi-tool",
            Data = selectedTools.Count == 1
                ? toolResults[0].Data
                : new
                {
                    generatedUtc = DateTime.UtcNow,
                    tools = toolResults.Select(tr => new
                    {
                        tr.Tool,
                        tr.RowCount,
                        tr.ExecutionMs,
                        tr.Data
                    })
                },
            Evidence = rankedEvidence,
            ToolExecutionMs = toolResults.Sum(t => t.ExecutionMs),
            TotalExecutionMs = totalSw.ElapsedMilliseconds,
            ConfidenceScore = confidence.score,
            ConfidenceLabel = confidence.label
        };

        if (request.IncludeAiSummary)
        {
            var selectedToolNames = toolResults.Select(t => t.Tool).ToList();
            var rcaTemplate = rcaTemplateService.GetTemplate(request.Question, selectedToolNames);
            var prompt = summaryFormatter.BuildSummaryPayload(request.Question, toolResults, rankedEvidence, response.ConfidenceLabel, rcaTemplate);
            response.Summary = await aiChat.SummarizeWithPromptAsync(prompt, cancellationToken, request.ModelOverride);
        }

        totalSw.Stop();
        response.TotalExecutionMs = totalSw.ElapsedMilliseconds;

        telemetry.Complete(
            requestId,
            response.Tool,
            toolResults.Sum(r => r.RowCount),
            response.ToolExecutionMs,
            response.TotalExecutionMs,
            response.ConfidenceScore,
            response.ConfidenceLabel);

        return Results.Ok(response);
    }
    catch (OperationCanceledException)
    {
        telemetry.Fail(requestId, new TimeoutException("AI request cancelled or timed out."));
        return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
    }
    catch (Exception ex)
    {
        telemetry.Fail(requestId, ex);
        return Results.Problem(
            title: "AI request failed",
            detail: $"RequestId={requestId}. {ex.Message}",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/ai/proactive-digest", async (
    IEnumerable<IAiTool> tools,
    AiRiskForecastService forecastService,
    AiConfidenceScorer confidenceScorer,
    AiChatClient aiChat,
    CancellationToken cancellationToken) =>
{
    var requestId = Guid.NewGuid().ToString("N");

    var selected = tools.Where(t => t.Name is ActiveAlertsSummaryTool.ToolName
        or BackupsRiskSummaryTool.ToolName
        or DrivesRiskSummaryTool.ToolName
        or WaitsSummaryTool.ToolName
        or BlockingSummaryTool.ToolName
        or ConfigDriftSummaryTool.ToolName)
        .ToList();

    var request = new AiAskRequest
    {
        Question = "Generate proactive risk forecast digest for DBA operations.",
        IncludeAiSummary = false,
        MaxRows = 50
    };

    var toolResults = new List<AiToolExecutionResult>();
    var evidence = new List<AiEvidenceItem>();

    foreach (var tool in selected)
    {
        var sw = Stopwatch.StartNew();
        var result = await tool.RunAsync(request, cancellationToken);
        sw.Stop();

        toolResults.Add(new AiToolExecutionResult
        {
            Tool = tool.Name,
            Data = result.Data,
            Evidence = result.Evidence,
            RowCount = result.RowCount,
            ExecutionMs = sw.ElapsedMilliseconds
        });
        evidence.AddRange(result.Evidence);
    }

    var risks = forecastService.BuildForecasts(toolResults);
    var confidence = confidenceScorer.Score(toolResults, evidence);

    var summaryPrompt = $"""
        Build a concise proactive DBA digest with headings:
        ## Summary
        ## Top Risks Next 24h
        ## Preventive Actions
        ## Confidence

        Forecast JSON:
        {System.Text.Json.JsonSerializer.Serialize(risks)}

        Confidence Label: {confidence.label}
        """;

    var summary = await aiChat.SummarizeWithPromptAsync(summaryPrompt, cancellationToken);

    var response = new AiProactiveDigestResponse
    {
        RequestId = requestId,
        GeneratedUtc = DateTime.UtcNow,
        Risks = risks,
        Summary = summary,
        ConfidenceScore = confidence.score,
        ConfidenceLabel = confidence.label
    };

    return Results.Ok(response);
});

app.Run();

static void LoadSettingsFromServiceConfig(ConfigurationManager config)
{
    CollectionConfig serviceConfig;
    try
    {
        // Look for ServiceConfig.json in our directory first, then parent directory
        // (AI service is typically deployed as a subfolder: DBADash\DBADashAI\)
        var configPath = BasicConfig.JsonConfigPath;
        if (!File.Exists(configPath))
        {
            // AppContext.BaseDirectory may have trailing slash — normalize before getting parent
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parentDir = Path.GetDirectoryName(baseDir);
            if (parentDir != null)
            {
                var parentPath = Path.Combine(parentDir, "ServiceConfig.json");
                if (File.Exists(parentPath))
                {
                    configPath = parentPath;
                }
            }
        }

        if (!File.Exists(configPath))
        {
            return;
        }

        var json = File.ReadAllText(configPath);
        serviceConfig = CollectionConfig.Deserialize(json);
    }
    catch
    {
        return;
    }

    var destination = serviceConfig.DestinationConnection?.ConnectionString;
    if (!string.IsNullOrWhiteSpace(destination))
    {
        config["ConnectionStrings:Repository"] = destination;
    }

    config["AI:Provider"] = serviceConfig.AIProvider ?? string.Empty;

    config["AzureOpenAI:Endpoint"] = serviceConfig.AzureOpenAIEndpoint ?? string.Empty;
    config["AzureOpenAI:ApiKey"] = serviceConfig.AzureOpenAIApiKeyDecrypted ?? string.Empty;
    config["AzureOpenAI:Deployment"] = serviceConfig.AzureOpenAIDeployment ?? string.Empty;
    config["AzureOpenAI:ApiVersion"] = string.IsNullOrWhiteSpace(serviceConfig.AzureOpenAIApiVersion)
        ? "2024-02-15-preview"
        : serviceConfig.AzureOpenAIApiVersion;

    config["Anthropic:BaseUrl"] = serviceConfig.AnthropicBaseUrl ?? string.Empty;
    config["Anthropic:ApiKey"] = serviceConfig.AnthropicApiKeyDecrypted ?? string.Empty;
    config["Anthropic:Model"] = serviceConfig.AnthropicModel ?? string.Empty;
    config["Anthropic:Version"] = string.IsNullOrWhiteSpace(serviceConfig.AnthropicVersion)
        ? "2023-06-01"
        : serviceConfig.AnthropicVersion;
    config["Anthropic:MaxTokens"] = string.IsNullOrWhiteSpace(serviceConfig.AnthropicMaxTokens)
        ? "1024"
        : serviceConfig.AnthropicMaxTokens;
}
