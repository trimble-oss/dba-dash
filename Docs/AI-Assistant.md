# DBA Dash AI Assistant

This document covers the DBA Dash AI Assistant end-to-end:
- what it is and how it works,
- how to configure Azure Foundry / Azure OpenAI,
- how to run `DBADashAI.dll`,
- how the GUI connects to the API,
- supported endpoints and troubleshooting.

## 1) Overview

The AI Assistant is split into two parts:

1. **DBADashGUI** (`DBADash.exe`)
   - Hosts the AI Assistant user interface.
   - Sends requests to the AI API over HTTP (default `http://localhost:5055`).

2. **DBADashAI** (`DBADashAI.dll`)
   - Executes DBA-oriented SQL tools against the DBA Dash repository database.
   - Optionally calls Azure OpenAI (recommended) or OpenCode to generate concise markdown summaries.
   - Exposes API endpoints under `/api/ai/*`.

## 2) Why Azure Foundry / Azure OpenAI is needed

The SQL tools can return structured findings, but **LLM summarization** is what turns raw evidence into a concise operator-friendly answer (Summary / Findings / Actions / Confidence).

Without Azure OpenAI (or OpenCode), API responses still work, but summary text will be disabled/fallback.

Azure OpenAI is recommended because it gives:
- enterprise deployment model and controls,
- stable model endpoint with API key auth,
- predictable integration for the DBA Dash AI service.

## 3) Architecture and request flow

1. User asks a question in GUI AI panel.
2. GUI posts to `POST /api/ai/ask`.
3. API validates request, selects one or more tools, runs SQL queries.
4. Evidence is ranked, confidence is scored.
5. If enabled, API sends a prompt + evidence to Azure OpenAI for markdown summary generation.
6. API returns structured JSON + summary to GUI.

## 4) Prerequisites

- DBA Dash repository database reachable from the machine running `DBADashAI.dll`.
- Valid SQL connection string for `ConnectionStrings:Repository`.
- .NET runtime compatible with project target (workspace is currently targeting .NET 10).
- Optional but recommended: Azure OpenAI deployment in Azure Foundry.

## 5) Configure DBADashAI (`DBADashAI/appsettings.json`)

`DBADashAI` reads settings from `DBADashAI/appsettings.json`.

### Required for data access

```json
{
  "ConnectionStrings": {
    "Repository": "Server=<server>;Database=<db>;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;"
  }
}
```

### AI settings (feedback and runbooks)

```json
{
  "AI": {
    "FeedbackStorePath": "",
    "RunbookBaseUrl": ""
  }
}
```

- `FeedbackStorePath`: optional explicit storage path for feedback persistence.
- `RunbookBaseUrl`: optional base URL for runbook links in risk/digest outputs.

### Azure OpenAI (recommended)

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com",
    "ApiKey": "<key>",
    "Deployment": "<deployment-name>",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

All of `Endpoint`, `ApiKey`, and `Deployment` must be present to enable Azure summary generation.

### OpenCode (optional fallback)

```json
{
  "OpenCode": {
    "BaseUrl": "",
    "ApiKey": "",
    "Model": ""
  }
}
```

OpenCode is used only if Azure settings are not fully configured.

### Provider selection

You can force a specific summary provider with:

```json
{
  "AI": {
    "Provider": "AzureOpenAI"
  }
}
```

Supported values:
- `AzureOpenAI`
- `Anthropic`
- `OpenCode`
- empty/omitted (auto-fallback order)

If omitted, provider fallback order is:
1. AzureOpenAI (if fully configured)
2. Anthropic (if fully configured)
3. OpenCode (if fully configured)

### Anthropic (optional)

```json
{
  "Anthropic": {
    "BaseUrl": "https://api.anthropic.com",
    "ApiKey": "",
    "Model": "",
    "Version": "2023-06-01",
    "MaxTokens": "1024"
  }
}
```

Required for Anthropic summaries:
- `Anthropic:ApiKey`
- `Anthropic:Model`

Optional:
- `BaseUrl` (default `https://api.anthropic.com`)
- `Version` (default `2023-06-01`)
- `MaxTokens` (default `1024`)

### Anthropic on Azure Foundry

If your Foundry admin provides values like:
- **Target URI**: `https://<resource>.services.ai.azure.com/anthropic/v1/messages`
- **API key**
- **deployment/model name** (example: `claude-sonnet-4-6`)

Map them to `DBADashAI/appsettings.json` as:

```json
{
  "AI": {
    "Provider": "Anthropic"
  },
  "Anthropic": {
    "BaseUrl": "https://<resource>.services.ai.azure.com/anthropic/",
    "ApiKey": "<api-key>",
    "Model": "<deployment-name>",
    "Version": "2023-06-01",
    "MaxTokens": "1024"
  }
}
```

Notes:
- Use the **base** Anthropic URL ending in `/anthropic/` (not `/v1/messages`) for `BaseUrl`.
- `Model` should be your Foundry deployment/model name.
- `AI:Provider` should be set to `Anthropic` to force that provider.

## 6) Configure GUI to reach API (`DBADashGUI/App.config`)

GUI app settings include:

```xml
<add key="AIApiBaseUrl" value="http://localhost:5055" />
<add key="AIToolName" value="" />
<add key="AIMaxRows" value="50" />
<add key="AIIncludeSummary" value="true" />
<add key="AIShowJson" value="false" />
```

- `AIApiBaseUrl`: where GUI sends AI requests.
- `AIToolName`: optional fixed tool override (empty = auto routing).
- `AIMaxRows`: max rows pulled by tools.
- `AIIncludeSummary`: whether API asks LLM for summary text.
- `AIShowJson`: shows/hides raw JSON panel in GUI.

## 7) How to run `DBADashAI.dll`

From the `DBADashAI` output folder:

```powershell
dotnet DBADashAI.dll --urls http://localhost:5055
```

Then run `DBADash.exe` and ensure GUI points to same base URL (`AIApiBaseUrl`).

### Health check

```http
GET /api/ai/health
```

Expected: HTTP 200 with JSON like `{ "status": "ok", ... }`.

## 8) API endpoints

- `GET /api/ai/health`
  - service health.

- `GET /api/ai/tools`
  - available tools and metadata.

- `POST /api/ai/ask`
  - main ask endpoint.
  - request model includes question, tool override, summary toggle, and max rows.

- `POST /api/ai/proactive-digest`
  - proactive digest summary/risk generation.

- `POST /api/ai/feedback`
  - submit helpful / not-helpful feedback.

- `GET /api/ai/feedback`
  - recent feedback records.

## 9) Tooling currently wired

Current tool registrations include:
- active alerts summary,
- blocking summary,
- agent job alerts,
- waits summary,
- deadlocks summary,
- slow queries summary,
- backups risk summary,
- drives risk summary,
- configuration drift summary.

The intent router can select one or more tools based on question and settings.

## 10) Azure Foundry setup checklist (practical)

1. Create or use an existing Azure OpenAI resource in your subscription.
2. Deploy a chat-capable model (e.g., your org-approved model/deployment).
3. Capture:
   - endpoint URL,
   - API key,
   - deployment name.
4. Put those values under `AzureOpenAI` in `appsettings.json`.
5. Restart `DBADashAI.dll`.
6. Verify:
   - `GET /api/ai/health` succeeds,
   - ask request returns a non-empty `summary`.

## 11) Security notes

- Treat API keys as secrets; do not commit them to source control.
- Limit who can read local config files with secrets.
- Use least privilege on repository DB access.
- Prefer environment-specific config management for production.

## 12) Troubleshooting

### GUI says AI request failed / cannot connect
- Confirm `DBADashAI.dll` is running.
- Confirm URL match: `AIApiBaseUrl` vs `--urls` argument.
- Check firewall or loopback restrictions.

### Summary missing
- Check `AIIncludeSummary=true` in GUI config.
- Ensure Azure settings are fully populated (Endpoint+ApiKey+Deployment) or valid OpenCode settings.
- Review API response body for error detail.

### SQL tool errors
- Validate `ConnectionStrings:Repository`.
- Validate DB reachability and permissions.
- Ensure repository schema objects exist and are accessible.

### Slow responses
- Reduce `AIMaxRows`.
- Validate SQL performance of repository DB.
- Monitor model latency and token volume.

## 13) Local dev workflow

For fast UI iteration, run from normal debug outputs:
- GUI: `DBADashBuild\Debug\DBADash.exe`
- API: `DBADashAI\bin\Debug\net10.0\DBADashAI.dll`

Create packaged test folders only when you need a snapshot/handoff build.

---

If this doc is moved to the website docs later, keep config keys and endpoint names aligned with code (`Program.cs`, `OpenCodeChatClient.cs`, `SqlToolExecutor.cs`, and GUI appSettings).
