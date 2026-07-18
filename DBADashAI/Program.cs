using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using DBADash;
using DBADashAI.Configuration;
using DBADashAI.Models;
using System.Text.Json;
using DBADashAI.Services;
using DBADashAI.Services.Authentication;
using DBADashAI.Services.Tools;
using Serilog;
using System.Diagnostics;

// Bootstrap Serilog from serilog.json before the host is built so that any
// startup errors (config load failures, port conflicts, etc.) are captured.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("serilog.DBADashAI.json", optional: false, reloadOnChange: true)
            .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ApplicationName", "DBADashAI")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .CreateLogger();

try
{
    Log.Information("DBADashAI starting up");
    RunApp(args);
    Log.Information("DBADashAI shut down cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "DBADashAI terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

return;

void RunApp(string[] args)
{

// When invoked with --check-runtime, exit immediately with code 0.
// This lets external callers (e.g. the service installer) verify that the
// required .NET runtime is present without starting the web host.
// If the runtime is missing the .NET host itself exits with code 150 before
// reaching this line.
if (args.Contains("--check-runtime")) return;

var builder = WebApplication.CreateBuilder(args);

// Load user-local overrides from appsettings.local.json if present.
// This file is never deployed from source (excluded from build output and .gitignore)
// so it is the safe place for real secrets, ports, and provider settings.
// It takes priority over appsettings.json but is overridden by environment variables,
// Azure Key Vault, and DPAPI-encrypted values.
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "appsettings.local.json"),
    optional: true,
    reloadOnChange: true);

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

// Decrypt any dpapi:-prefixed values stored in appsettings.json using DPAPI LocalMachine scope.
// This provider is added last so it only acts on values that were not already overridden by a
// higher-priority source (environment variables, Azure Key Vault, etc.).
builder.Configuration.AddDpApiSecrets();

// Mode is determined by Registration:ServiceUrl.
// If set  → shared/repository mode: bind to all interfaces, register in DB, auth is always enabled.
// If empty → local mode: bind loopback only, skip DB registration, auth disabled.
var registrationServiceUrl = builder.Configuration["Registration:ServiceUrl"];
var isLocalMode = string.IsNullOrWhiteSpace(registrationServiceUrl);

if (isLocalMode)
{
    // Bind to loopback only - not accessible from other machines.
    var port = builder.Configuration.GetValue<int?>("Registration:Port") ?? 5055;
    builder.WebHost.UseUrls($"http://localhost:{port}");
}
else
{
    // Shared mode: bind to all interfaces so remote GUI clients can connect.
    var port = builder.Configuration.GetValue<int?>("Registration:Port") ?? 5055;
    if (string.IsNullOrWhiteSpace(builder.Configuration["Urls"]))
    {
        builder.WebHost.UseUrls($"http://*:{port}");
    }
}

builder.Host.UseWindowsService();
builder.Host.UseSerilog();

// Security is mandatory in shared mode and intentionally disabled in local loopback-only mode.
var securityEnabled = !isLocalMode;

// Register API key manager for repository-based key storage
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApiKeyManager>();

// Register background service for service discovery
builder.Services.AddHostedService<ServiceRegistrationBackgroundService>();

if (securityEnabled)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = ApiKeyAuthenticationOptions.DefaultScheme;
        options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
    }).AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, options => { });

    builder.Services.AddAuthorization(options =>
    {
        // Require authentication on all endpoints by default.
        // Only endpoints explicitly marked with AllowAnonymous (e.g. /health) bypass this.
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}

// ToolSimilarityScorer is a singleton that builds TF-IDF vectors from tool metadata
// at startup. It accepts IServiceScopeFactory to safely resolve the scoped IAiTool
// registrations once during construction, then discards the scope.
builder.Services.AddSingleton<ToolSimilarityScorer>();
builder.Services.AddSingleton<SystemPromptLoader>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<SqlToolExecutor>();
builder.Services.AddScoped<AiChatClient>();
builder.Services.AddScoped<AiRequestTelemetryService>();
builder.Services.AddScoped<AiIntentRouter>();
builder.Services.AddScoped<AiConfidenceScorer>();
builder.Services.AddScoped<AiPromptDataSerializer>();
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
builder.Services.AddScoped<IAiTool, FailedLoginsSummaryTool>();
builder.Services.AddScoped<IAiTool, MemoryClerkSummaryTool>();
builder.Services.AddScoped<IAiTool, JobStepConfigSummaryTool>();

// Rate limiting: per-IP token bucket applied to costly AI endpoints.
// Configured via "RateLimiting" in appsettings.json; defaults allow short bursts
// while preventing sustained abuse that would incur AI API costs.
const string AiRateLimitPolicy = "ai-per-ip";
var rlOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

if (rlOptions.Enabled)
{
    builder.Services.AddRateLimiter(limiter =>
    {
        limiter.AddPolicy<string, AiRateLimitPartitioner>(AiRateLimitPolicy);
        limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        limiter.OnRejected = (ctx, _) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter =
                rlOptions.ReplenishmentPeriodSeconds.ToString();
            return ValueTask.CompletedTask;
        };
    });

    // Make the resolved options available so AiRateLimitPartitioner can read them.
    builder.Services.AddSingleton(rlOptions);
}

var app = builder.Build();

// Apply security middleware - ENABLED BY DEFAULT
var requireHttps = app.Configuration.GetValue<bool>("Security:RequireHttps");

if (securityEnabled)
{
    if (requireHttps)
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.Logger.LogInformation("Security enabled with API key authentication");
}
else
{
    if (isLocalMode)
        app.Logger.LogInformation("Running in local mode (loopback only) - authentication disabled");
    else
        app.Logger.LogWarning("Security is DISABLED - this is not recommended for production use");
}

if (rlOptions.Enabled)
{
    app.UseRateLimiter();
    app.Logger.LogInformation(
        "Rate limiting enabled: {TokenLimit} token bucket, {TokensPerPeriod} tokens/{ReplenishmentPeriodSeconds}s per IP, queue {QueueLimit}",
        rlOptions.TokenLimit, rlOptions.TokensPerPeriod, rlOptions.ReplenishmentPeriodSeconds, rlOptions.QueueLimit);
}

// Helper to conditionally apply authorization
RouteHandlerBuilder ApplyAuth(RouteHandlerBuilder builder) =>
    securityEnabled ? builder.RequireAuthorization() : builder;

// Helper to apply rate limiting only to costly AI endpoints (not health/tools/feedback)
RouteHandlerBuilder ApplyRateLimit(RouteHandlerBuilder builder) =>
    rlOptions.Enabled ? builder.RequireRateLimiting(AiRateLimitPolicy) : builder;

// Combined helper — auth + rate limit
RouteHandlerBuilder ApplyAuthAndRateLimit(RouteHandlerBuilder builder) =>
    ApplyRateLimit(ApplyAuth(builder));

// Unauthenticated health check endpoint - safe for monitoring/load balancers
// Returns basic status plus a repository fingerprint (SHA-256 of server|database)
// so GUI clients can verify the AI service is pointed at the same repository.
// The fingerprint is computed once (it queries the DB for its canonical identity)
// and cached so the health endpoint stays cheap under frequent probing. If the
// repository is unavailable when first computed the value is null, so it is retried
// lazily on subsequent health calls - a transient startup outage must not disable
// mismatch detection until the service is restarted.
var repositoryConnectionString = app.Configuration.GetConnectionString("Repository");
var cachedRepositoryFingerprint = DBADash.RepositoryFingerprint.GetFingerprint(repositoryConnectionString);
var fingerprintLock = new object();
app.MapGet("/api/ai/health", () =>
{
    if (cachedRepositoryFingerprint == null)
    {
        lock (fingerprintLock)
        {
            cachedRepositoryFingerprint ??= DBADash.RepositoryFingerprint.GetFingerprint(repositoryConnectionString);
        }
    }
    return Results.Ok(new
    {
        status = "healthy",
        service = "DBADashAI",
        version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        timestamp = DateTime.UtcNow,
        repositoryFingerprint = cachedRepositoryFingerprint
    });
}).AllowAnonymous();

ApplyAuth(app.MapGet("/api/ai/diagnostics", (IConfiguration config, IHttpClientFactory httpClientFactory, SystemPromptLoader systemPromptLoader) =>
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

    var registrationServiceUrlStatus = config["Registration:ServiceUrl"];
    var isLocalModeStatus = string.IsNullOrWhiteSpace(registrationServiceUrlStatus);
    var securityEnabledStatus = !isLocalModeStatus;

    return Results.Ok(new
    {
        provider,
        repository = repoSafe,
        azureOpenAI = new { endpoint = azureEndpoint, apiKey = azureKey, deployment = azureDeployment },
        anthropic = new { baseUrl = anthropicUrl, apiKey = anthropicKey, model = anthropicModel, version = anthropicVersion },
        security = new { enabled = securityEnabledStatus },
        systemPrompt = new { source = systemPromptLoader.Source, length = systemPromptLoader.Prompt.Length },
        utc = DateTime.UtcNow
    });
}));

ApplyAuth(app.MapGet("/api/ai/test-anthropic", async (IConfiguration config, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
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

        var requestUrl = new Uri(new Uri(baseUrl), "v1/messages");
        var payload = new
        {
            model,
            max_tokens = 64,
            messages = new[] { new { role = "user", content = "Reply with only: OK" } }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        // Build a fresh HttpRequestMessage so headers are scoped to this request only
        // and do not leak into future requests that reuse the same HttpClient instance.
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("x-api-key", AiChatClient.SanitizeHeaderValue(apiKey));
        request.Headers.Add("anthropic-version", AiChatClient.SanitizeHeaderValue(version));
        request.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
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
}));

ApplyAuth(app.MapPost("/api/ai/feedback", (
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
    logger.LogInformation("AI feedback received. RequestId={RequestId}, Helpful={Helpful}, Tool={ToolName}, Category={Category}",
        LogSanitizer.SanitizeForLog(request.RequestId),
        request.IsHelpful,
        LogSanitizer.SanitizeForLog(request.ToolName ?? "(unknown)"),
        LogSanitizer.SanitizeForLog(request.Category ?? string.Empty));

    return Results.Ok(new { status = "saved" });
}));

ApplyAuth(app.MapGet("/api/ai/feedback", (AiFeedbackStore store) =>
{
    var records = store.GetRecent(200);
    return Results.Ok(records);
}));

ApplyAuth(app.MapGet("/api/ai/tools", (IEnumerable<IAiTool> tools) =>
{
    var result = tools.Select(t => new
    {
        t.Name,
        t.Description,
        t.InputHint,
        t.Keywords
    });

    return Results.Ok(result);
}));

ApplyAuth(app.MapGet("/api/ai/examples", async (SqlToolExecutor sql, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
{
    try
    {
        var rows = await sql.QueryNoParamsAsync("AI.ExampleQuestions_Get", cancellationToken);

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
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger("AiExamples");
        logger.LogError(ex, "Failed to load example questions");
        return Results.Ok(Array.Empty<object>());
    }
}));

ApplyAuth(app.MapGet("/api/ai/models", async (SqlToolExecutor sql, CancellationToken cancellationToken) =>
{
    try
    {
        var rows = await sql.QueryNoParamsAsync("AI.Models_Get", cancellationToken);
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
}));

ApplyAuthAndRateLimit(app.MapPost("/api/ai/ask", async (
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
        var routing = router.SelectTools(request, tools);
        if (routing.Tools.Count == 0)
        {
            return Results.BadRequest(new { error = "No tool available." });
        }

        // Build a per-execution request that carries the resolved filters without
        // mutating the original caller-supplied request object.
        var resolvedRequest = new AiAskRequest
        {
            Question = request.Question,
            ToolName = request.ToolName,
            IncludeAiSummary = request.IncludeAiSummary,
            MaxRows = request.MaxRows,
            ModelOverride = request.ModelOverride,
            InstanceFilter = routing.InstanceFilter,
            HoursBack = routing.HoursBack
        };

        var toolResults = new List<AiToolExecutionResult>();
        var allEvidence = new List<AiEvidenceItem>();

        foreach (var tool in routing.Tools)
        {
            var toolSw = Stopwatch.StartNew();
            var result = await tool.RunAsync(resolvedRequest, cancellationToken);
            toolSw.Stop();

            toolResults.Add(new AiToolExecutionResult
            {
                Tool = tool.Name,
                Data = JsonSerializer.SerializeToElement(result.Data),
                Evidence = result.Evidence,
                RowCount = result.RowCount,
                ExecutionMs = toolSw.ElapsedMilliseconds
            });
            allEvidence.AddRange(result.Evidence);
        }

        var primaryTool = routing.Tools[0].Name;
        var rankedEvidence = evidenceRanker.Rank(allEvidence);
        var confidence = confidenceScorer.Score(toolResults, rankedEvidence);

        var response = new AiAskResponse
        {
            RequestId = requestId,
            Tool = routing.Tools.Count == 1 ? primaryTool : "multi-tool",
            Data = routing.Tools.Count == 1
                ? toolResults[0].Data
                : JsonSerializer.SerializeToElement(new
                {
                    generatedUtc = DateTime.UtcNow,
                    tools = toolResults.Select(tr => new
                    {
                        tr.Tool,
                        tr.RowCount,
                        tr.ExecutionMs,
                        tr.Data
                    })
                }),
            Evidence = rankedEvidence,
            ToolExecutionMs = toolResults.Sum(t => t.ExecutionMs),
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
}));

ApplyAuthAndRateLimit(app.MapPost("/api/ai/proactive-digest", async (
    IEnumerable<IAiTool> tools,
    AiRiskForecastService forecastService,
    AiConfidenceScorer confidenceScorer,
    AiChatClient aiChat,
    CancellationToken cancellationToken) =>
{
    var requestId = Guid.NewGuid().ToString("N");

    try
    {
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
                Data = JsonSerializer.SerializeToElement(result.Data),
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
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Proactive digest request {RequestId} failed.", requestId);
        return Results.Problem(
            title: "Proactive digest request failed",
            detail: $"RequestId={requestId}. {ex.Message}",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}));

app.Run();

static void LoadSettingsFromServiceConfig(ConfigurationManager config)
{
    // If a repository connection string is not already set in appsettings.json, try to
    // inherit it from ServiceConfig.json. This allows the AI service to be deployed
    // alongside the collection service without duplicating the connection string.
    if (!string.IsNullOrWhiteSpace(config.GetConnectionString("Repository")))
        return;

    try
    {
        var cfg = BasicConfig.Load<CollectionConfig>();
        var destination = cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL ? cfg.DestinationConnection.ConnectionString : string.Empty;
        if (!string.IsNullOrWhiteSpace(destination))
            config["ConnectionStrings:Repository"] = destination;
    }
    catch(Exception ex)
    {
        // ServiceConfig.json is optional - swallow and continue
        Log.Information("Error loading ServiceConfig.json for repository connection string inheritance: {Message}", ex.Message);
        }
}
} // end RunApp
