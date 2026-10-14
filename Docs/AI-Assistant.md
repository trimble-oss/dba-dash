# DBA Dash AI Assistant

The AI Assistant analyses your DBA Dash repository data and answers natural-language questions about alerts, performance, waits, backups, blocking, slow queries, and more.

## How it works

Two components work together:

| Component | Role |
|---|---|
| **DBADashAI** (`DBADashAI.dll`) | ASP.NET Core web service. Runs SQL tools against the repository DB, optionally calls an LLM to summarise findings, and exposes `/api/ai/*` endpoints. |
| **DBADash GUI** | Discovers the AI service, calls the API, and renders the response in the AI Assistant tab. |

---

## Deployment modes

### Local mode (default — recommended for getting started)

Run `DBADashAI` on the same machine as the GUI. It binds to `localhost` only and requires no authentication — the loopback interface is the security boundary.

- No certificates required
- No API key required
- Not registered in the repository database
- Only the GUI on that machine can use it

**`appsettings.json` — leave `Registration:ServiceUrl` empty:**

```json
{
  "Registration": {
    "ServiceUrl": "",
    "Port": 5055
  }
}
```

### Shared/repository mode

Deploy `DBADashAI` on a central server accessible to all GUI clients. Set `Registration:ServiceUrl` to the URL that clients will connect to. The service binds to all interfaces, registers that URL in the repository database, and enforces API key authentication.

**`appsettings.json`:**

```json
{
  "Registration": {
    "ServiceUrl": "https://aiserver.corp.com",
    "Port": 5055
  },
  "Security": {
    "Enabled": true,
    "RequireHttps": true
  }
}
```

GUI clients discover the URL and API key automatically from the repository database — no client-side configuration needed.

---

## Configuration reference (`appsettings.json`)

All AI service configuration lives in `appsettings.json` in the `DBADashAI` folder. `ServiceConfig.json` is not used by the AI service except to inherit the repository connection string when they are co-located.

### Repository connection string

Required. Tells the AI service which DBA Dash database to query.

```json
{
  "ConnectionStrings": {
    "Repository": "Server=sql-server;Database=DBADashDB;Integrated Security=true;Encrypt=true;"
  }
}
```

If `DBADashAI` is deployed in the same folder as the collection service, this is inherited automatically from `ServiceConfig.json` and does not need to be set here.

### AI provider

At least one provider must be configured for the LLM summary to work. Without a provider the service still runs — tool data is returned but the `summary` field will be empty.

The **AI Service** tab in the DBADashServiceConfig tool edits these settings for you. It writes them to `appsettings.local.json`, which overrides `appsettings.json` and isn't replaced by upgrades, and encrypts API keys with DPAPI.

| Provider | Authentication | Model choice in the GUI |
|---|---|---|
| Azure OpenAI | API key | No - the deployment decides the model |
| Anthropic | API key | Yes - the models listed in the repository |
| Amazon Bedrock | AWS credentials (recommended) or Bedrock API key | Yes - the models available in the region |
| Ollama | None (optional key for a proxy) | Yes - the models installed on the server |

Where there's a choice, the GUI's **Model** picker shows each model's name with the ID to configure it by in brackets, e.g. *EU Anthropic Claude Sonnet 4.6 (eu.anthropic.claude-sonnet-4-6)*. The first entry is the configured model. A model picked there only affects your own requests; to change the default for everyone, set the provider's `Model` setting.

#### Azure OpenAI

```json
{
  "AI": { "Provider": "AzureOpenAI" },
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com",
    "ApiKey": "<key>",
    "Deployment": "<deployment-name>",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

#### Anthropic (direct or via Azure Foundry)

```json
{
  "AI": { "Provider": "Anthropic" },
  "Anthropic": {
    "BaseUrl": "https://api.anthropic.com",
    "ApiKey": "<key>",
    "Model": "claude-sonnet-4-6",
    "Version": "2023-06-01",
    "MaxTokens": 1024
  }
}
```

For Anthropic via **Azure Foundry**, set `BaseUrl` to the base Foundry URL (ending in `/anthropic/`):

```json
{
  "Anthropic": {
    "BaseUrl": "https://<resource>.services.ai.azure.com/anthropic/",
    "ApiKey": "<foundry-api-key>",
    "Model": "<deployment-name>"
  }
}
```

#### Amazon Bedrock

```json
{
  "AI": { "Provider": "Bedrock" },
  "Bedrock": {
    "Region": "us-east-1",
    "Model": "us.anthropic.claude-sonnet-4-6",
    "Profile": "",
    "ApiKey": "",
    "MaxTokens": ""
  }
}
```

Bedrock can be called with AWS credentials or with a Bedrock API key:

- **AWS credentials (recommended).** Leave `ApiKey` blank. The service takes credentials from `Profile` if it's set, or otherwise from the default AWS credential chain: an IAM role (EC2 instance profile, ECS task role, EKS web identity), the `AWS_*` environment variables, or the default profile of the account the service runs as. For each call it makes a short-term Bedrock API key from those credentials, valid for at most 12 hours or until the credentials expire - the same key AWS's `aws-bedrock-token-generator` libraries make. On premises, IAM Roles Anywhere through a profile's `credential_process` gives short-term credentials without a stored secret. The credentials need permission to call Bedrock, e.g. the `AmazonBedrockLimitedAccess` managed policy.
- **API key.** Set `ApiKey` to an [Amazon Bedrock API key](https://docs.aws.amazon.com/bedrock/latest/userguide/api-keys.html). It takes priority over AWS credentials. AWS recommends API keys for exploration only, and short-term keys from the console expire within 12 hours, so a service needs a long-term key.

The other settings:

- `Model` is a model ID or inference profile ID. Most current models can only be called through an inference profile - the ID with a geographic prefix such as `us.` or `eu.` - and calling the base model ID gives a validation error. An application inference profile is given by its ARN. Use the bedrock-runtime model ID from the model card, not the bedrock-mantle one (e.g. `openai.gpt-oss-120b-1:0`, not `openai.gpt-oss-120b`).
- Any chat model that supports the Converse API works (Anthropic Claude, Amazon Nova, Meta Llama, Mistral, ...). The GUI's model picker lists the inference profiles and on-demand text models available in the region, with the ID to use in brackets.
- `MaxTokens` optionally caps the answer length. Leave blank for the model's default.
- Listing models needs the `bedrock:ListInferenceProfiles` and `bedrock:ListFoundationModels` permissions; without them the picker is hidden and the configured `Model` is used.

To find model IDs outside the GUI, use the model card in the Bedrock console ("Programmatic Access" table, bedrock-runtime row) or the AWS CLI:

```bash
aws bedrock list-inference-profiles --region eu-west-2 --query "inferenceProfileSummaries[].inferenceProfileId"
```

```bash
aws bedrock list-foundation-models --region eu-west-2 --by-output-modality TEXT --by-inference-type ON_DEMAND --query "modelSummaries[].modelId"
```

An inference profile ID only works in a region whose profile list includes it - not every model has a `us.`/`eu.`/`global.` profile.

**AWS credentials and the Windows service.** The default credential chain is that of the account the service runs as. On EC2 the instance role just works. Elsewhere, either run the service as an account whose `%USERPROFILE%\.aws` holds the profile, or set the `AWS_SHARED_CREDENTIALS_FILE` / `AWS_CONFIG_FILE` environment variables for the service and name the profile in `Profile`. IAM Identity Center (SSO) profiles aren't supported - they need an interactive sign-in.

**IAM permissions.** For AWS credentials (an instance profile, task role or profile), grant the role:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "UseShortTermBedrockKeys",
      "Effect": "Allow",
      "Action": "bedrock:CallWithBearerToken",
      "Resource": "*",
      "Condition": { "StringEquals": { "bedrock:bearerTokenType": "SHORT_TERM" } }
    },
    {
      "Sid": "InvokeModels",
      "Effect": "Allow",
      "Action": "bedrock:InvokeModel",
      "Resource": [
        "arn:aws:bedrock:*::foundation-model/*",
        "arn:aws:bedrock:*:<account-id>:inference-profile/*",
        "arn:aws:bedrock:*:<account-id>:application-inference-profile/*"
      ]
    },
    {
      "Sid": "ListModelsForThePicker",
      "Effect": "Allow",
      "Action": [ "bedrock:ListFoundationModels", "bedrock:ListInferenceProfiles" ],
      "Resource": "*"
    }
  ]
}
```

- `bedrock:CallWithBearerToken` lets the role use the short-term key the service makes from its credentials. Without it every call is refused, even with `InvokeModel` allowed. Check that no service control policy denies it.
- `bedrock:InvokeModel` is all the Converse API needs (the service doesn't stream). The foundation-model ARN has a region wildcard because a geo or global inference profile routes to other regions, and the permission is needed in each of them.
- The List actions only fill the GUI's Model picker.
- To allow specific models only, list each inference profile ARN together with its foundation-model ARN in every region the profile covers - see [Prerequisites for inference profiles](https://docs.aws.amazon.com/bedrock/latest/userguide/cross-region-inference-prereq.html). The `AmazonBedrockLimitedAccess` managed policy also works, but grants far more than the service needs.
- Model access is separate from IAM: some models need a one-off step in the Bedrock console (a use-case form for Anthropic, a subscription for Marketplace models). Do that as an admin rather than giving the role Marketplace permissions.

With an API key instead, the IAM user the key belongs to needs the same `InvokeModel` and List permissions, and `bedrock:CallWithBearerToken` for `LONG_TERM` keys.

#### Ollama (local models)

```json
{
  "AI": { "Provider": "Ollama" },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:14b",
    "ContextLength": 32768,
    "MaxTokens": "",
    "TimeoutSeconds": 240,
    "ApiKey": ""
  }
}
```

- `Model` must be a model already pulled onto the Ollama server (`ollama pull qwen3:14b`, `ollama list`). The GUI's model picker lists the installed models.
- `ContextLength` is the context window requested from Ollama (`num_ctx`). Ollama's own default (typically 4096 tokens) is too small for deadlock and query plan analysis. Larger values use more GPU/system memory. A prompt that doesn't fit is reported as too large rather than silently truncated (this relies on Ollama honouring `truncate: false` - use a current Ollama release).
- `MaxTokens` optionally caps the answer length (`num_predict`). Leave blank for no limit - for reasoning models the limit includes the model's thinking.
- `TimeoutSeconds` is how long to wait for an answer (default 240). A model that takes longer is reported as timed out. The GUI waits up to 300 seconds for the service, so values above about 270 only help other callers.
- `ApiKey` is optional: Ollama doesn't use one, but it's sent as a bearer token if set (for an authenticating reverse proxy or ollama.com).

Small models (e.g. 3B) are fine for checking the setup but tend to miss findings; a 14B-class model or larger gives much better analysis.

If `AI:Provider` is not set, the service auto-selects the first fully-configured provider in the order: AzureOpenAI → Anthropic. Bedrock and Ollama are only used when `AI:Provider` is `Bedrock` or `Ollama`.

### Registration

```json
{
  "Registration": {
    "ServiceUrl": "",
    "Port": 5055
  }
}
```

| Key | Description |
|---|---|
| `ServiceUrl` | The URL registered in the repository DB for GUI discovery. Empty = local mode (loopback binding, no auth, no DB registration). |
| `Port` | Port to listen on. Used in both modes. |

### Security

Only applies in shared/repository mode (`ServiceUrl` set). In local mode, auth is automatically disabled.

```json
{
  "Security": {
    "Enabled": true,
    "RequireHttps": false,
    "KeyRotationDays": 30,
    "KeyGracePeriodHours": 24
  }
}
```

| Key | Description |
|---|---|
| `Enabled` | Enables authentication in shared/repository mode. API key authentication is the current authentication model. |
| `RequireHttps` | Redirects HTTP requests to HTTPS. Strongly recommended in shared/repository mode. |
| `KeyRotationDays` | Automatically rotates the shared API key after the specified number of days. Set `0` or less to disable rotation. |
| `KeyGracePeriodHours` | Grace period for previous keys during rotation. |

**⚠️ HTTPS in shared mode** — **Strongly recommended.** Without HTTPS, API keys are transmitted in plain text and can be intercepted on the network. Set `RequireHttps: true` and configure your binding via a reverse proxy (recommended) or Kestrel certificates. The health endpoint (`/api/ai/health`) is always unauthenticated for load-balancer probes.

### Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DBADashAI": "Information"
    }
  }
}
```

### Azure Key Vault (optional)

Store secrets in Key Vault instead of `appsettings.json`. Use `--` to represent hierarchy in secret names (e.g. `AzureOpenAI--ApiKey`).

```json
{
  "KeyVault": {
    "VaultUri": "https://<vault>.vault.azure.net/",
    "ManagedIdentityClientId": ""
  }
}
```

---

## Installing as a Windows service

Use the **AI Service** tab in the DBADashServiceConfig tool, or run `sc.exe` manually:

```powershell
sc.exe create DBADashAI `
    binPath="\"C:\Program Files\dotnet\dotnet.exe\" \"C:\DBADash\DBADashAI\DBADashAI.dll\"" `
    start= auto
sc.exe start DBADashAI
```

Configure the service on the AI Service tab, or by editing `appsettings.local.json` (or `appsettings.json`) in the `DBADashAI` folder, before or after installation. Restart the service to apply changes.

---

## GUI discovery

The GUI uses the following priority order to locate the AI service:

1. **Local probe** — always checks `http://localhost:5055` (or the URL in user settings) first. If reachable, uses it with no authentication. The health endpoint returns a repository fingerprint; if the local service is pointed at a different database than the GUI is connected to, a warning is shown and the tab is disabled.
2. **Repository database** — if no local service responds, reads `AI.ServiceConfig` for the shared service URL and API key registered by the central deployment.

If neither source is available the AI Assistant tab is hidden.

> **Role requirement** — Even when a service is reachable, the AI Assistant tab is only shown to users who are admins (**db_owner** or **sysadmin** on the repository database) or members of the **AIUser** role. If you can reach the service but the tab is still missing, verify that your repository login has one of these roles assigned.

---

## API endpoints

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/ai/health` | None | Service health + repository fingerprint |
| `GET /api/ai/diagnostics` | Required | Configuration summary (keys redacted; for Bedrock, whether an API key or AWS credentials are used) |
| `GET /api/ai/tools` | Required | Available tools and metadata |
| `GET /api/ai/examples` | Required | Example questions grouped by category |
| `GET /api/ai/models` | Required | Available LLM models (for Bedrock, the models available in the region; for Ollama, the models installed on the server) |
| `POST /api/ai/ask` | Required | Ask a question |
| `POST /api/ai/proactive-digest` | Required | Proactive risk digest |
| `POST /api/ai/feedback` | Required | Submit helpful/not-helpful feedback |
| `GET /api/ai/feedback` | Required | Recent feedback |
| `GET /api/ai/test-anthropic` | Required | Test Anthropic provider connectivity and configuration |

---

## Troubleshooting

### AI Assistant tab is not shown

- In local mode: verify the service is running — `Invoke-RestMethod http://localhost:5055/api/ai/health`
- In shared mode: check `AI.ServiceConfig` exists and `IsActive = 1` (heartbeat within 2 minutes)
- Verify that your repository login is **db_owner** or **sysadmin** on the repository database, or a member of the **AIUser** role — the tab is hidden for users who have neither

### "AI service is connected to a different repository"

The local service's `ConnectionStrings:Repository` points to a different database than the GUI is connected to. Either update the AI service connection string or switch the GUI to the matching repository.

### Summary is empty

The LLM provider is not configured or unreachable. Check `AI:Provider` and the corresponding provider section. Use `GET /api/ai/diagnostics` (authenticated) to inspect the resolved configuration.

### Bedrock errors

| Message | Cause |
|---|---|
| `The provided model identifier is invalid` (400) | The model ID isn't one bedrock-runtime knows in this region. Common causes: a bedrock-mantle ID (no version suffix, e.g. `openai.gpt-oss-120b` instead of `openai.gpt-oss-120b-1:0`), or an inference profile prefix (`eu.`, `us.`, `global.`) that doesn't exist for the model. Pick the model from the GUI's Model picker to get an ID that works. |
| `Invocation of model ID ... with on-demand throughput isn't supported` (400) | The model can only be called through an inference profile - use its `us.`/`eu.`/`global.` ID. |
| `You don't have access to the model` / 403 | The credentials or API key lack `bedrock:InvokeModel` for the model, or the model needs a marketplace subscription or use-case form in the Bedrock console. |
| `Bedrock:ApiKey is not set and no AWS credentials were found` | No API key is set, and the service account has no AWS credentials (or `Profile` names a profile it can't find). See *AWS credentials and the Windows service* above. |
| `Bedrock:Region must be an AWS region name` | `Region` is blank or mistyped - it must look like `eu-west-2`. |

The service log records Bedrock's full response for every failed call, with the ErrorId shown in the GUI.

### 401 Unauthorized

Shared mode only. The GUI retrieves the API key from the database automatically. If it fails, check that `AI.ServiceConfig` has a valid key and the GUI has read access to that table.

### Port conflict

Change `Registration:Port` in `appsettings.json` and restart. In local mode the GUI will probe `http://localhost:{port}` — update user settings in the GUI Options if using a non-default port.
