#nullable enable
using DBADash.Deadlock.Analysis;
using DBADashGUI.AI;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Sends a deadlock to the AI service for analysis, using the same discovery and API key path as
    /// the assistant: a service registered in the repository, or a local one on loopback.
    ///
    /// The payload is serialised from the object the viewer previewed, so what was shown is what
    /// goes.  Nothing is sent until someone presses the button.
    /// </summary>
    internal static class DeadlockAnalysisClient
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(180) };

        internal sealed class Result
        {
            public string? Analysis { get; init; }

            public string? Model { get; init; }

            /// <summary>When the analysis was produced.</summary>
            public DateTime? GeneratedUtc { get; init; }

            public string? Error { get; init; }

            public bool Success => Error is null;
        }

        /// <summary>The service to talk to, or null when none is configured or reachable.</summary>
        internal static async Task<AIServiceDiscovery.ServiceInfo?> FindServiceAsync()
        {
            try
            {
                var info = await AIServiceDiscovery.GetServiceInfoAsync();
                return info is { IsEnabled: true } ? info : null;
            }
            catch (Exception)
            {
                // Discovery reaching nothing is the normal answer for an estate with no AI service.
                return null;
            }
        }

        internal static string ToJson(
            DeadlockAnalysisPayload payload,
            int? instanceId = null,
            string? modelOverride = null) =>
            JsonSerializer.Serialize(
                new
                {
                    graphXml = payload.GraphXml,
                    signature = payload.Signature,
                    payloadVersion = payload.Version,
                    instanceId,
                    instance = payload.Instance,
                    participants = payload.Participants,
                    objects = payload.Objects,
                    modules = payload.Modules,
                    findings = payload.Findings,
                    schema = payload.Schema.Select(s => new
                    {
                        name = s.Name,
                        database = s.Database,
                        objectType = s.ObjectType,
                        asAt = s.AsAt,
                        ddl = s.Ddl,
                        truncated = s.Truncated
                    }),
                    modelOverride
                },
                new JsonSerializerOptions { WriteIndented = true });

        internal static async Task<Result> AnalyseAsync(
            DeadlockAnalysisPayload payload,
            AIServiceDiscovery.ServiceInfo service,
            CancellationToken cancellationToken,
            int? instanceId = null)
        {
            try
            {
                var baseUrl = service.ServiceUrl
                              ?? ConfigurationManager.AppSettings["AIApiBaseUrl"]
                              ?? "http://localhost:5055";

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/api/ai/analyse-deadlock")
                {
                    Content = new StringContent(
                        ToJson(payload, instanceId),
                        Encoding.UTF8,
                        "application/json")
                };

                var apiKey = service.ApiKey ?? await AIApiKeyProvider.GetApiKeyAsync();
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-API-Key", apiKey);

                using var response = await Client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result { Error = $"{(int)response.StatusCode} {response.ReasonPhrase}: {body}" };
                }

                using var document = JsonDocument.Parse(body);
                return new Result
                {
                    Analysis = Read(document, "analysis"),
                    Model = Read(document, "model"),
                    GeneratedUtc = document.RootElement.TryGetProperty("generatedUtc", out var generated) &&
                                   generated.TryGetDateTime(out var value)
                        ? value
                        : null
                };
            }
            catch (OperationCanceledException)
            {
                return new Result { Error = "Cancelled." };
            }
            catch (Exception ex)
            {
                return new Result { Error = ex.Message };
            }
        }

        private static string? Read(JsonDocument document, string property) =>
            document.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
