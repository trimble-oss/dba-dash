#nullable enable
using DBADash.PerformanceAnalysis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Sends a performance analysis to the AI service, using the same discovery and API key path as the
    /// plan and deadlock viewers.
    ///
    /// The payload is serialised from the object the dialog previewed, so what was shown is what goes.
    /// </summary>
    internal static class PerformanceAnalysisClient
    {
        // Longer than the other viewers: an analysis that looks things up is several model calls in one request.
        // The service starts no lookup after 7 minutes (AI:PerformanceInvestigation:DeadlineSeconds), then can make
        // two more calls - the one in flight and the answer - so this allows for that and the repository time.
        private static readonly TimeSpan Timeout = AIServiceTimeouts.Request * 4;

        private static readonly HttpClient Client = AiServerCertificateValidator.CreateClient(Timeout);

        internal sealed class Result
        {
            public string? Analysis { get; init; }

            public string? Model { get; init; }

            public DateTime? GeneratedUtc { get; init; }

            /// <summary>The conversation the answer belongs to, minted by the service on the first turn.</summary>
            public Guid? ConversationId { get; init; }

            /// <summary>What the model looked up before it answered.</summary>
            public IReadOnlyList<AiConversation.Investigation> Investigations { get; init; } = Array.Empty<AiConversation.Investigation>();

            public string? Error { get; init; }

            public bool Success => Error is null;
        }

        internal static string ToJson(
            PerformanceAnalysisPayload payload,
            string? modelOverride = null,
            AiConversation? conversation = null,
            string? question = null,
            bool allowInvestigation = false)
        {
            // Everything said so far.  The figures are not in here: the service builds the opening question
            // from the payload below on every turn, so a follow-up cannot quietly change them.
            var history = (conversation?.ToHistory() ?? new List<AiConversation.WireTurn>())
                .Select(t => new { role = t.Role, content = t.Content })
                .ToList();

            return JsonSerializer.Serialize(
                new
                {
                    instance = payload.Instance,
                    instanceId = payload.InstanceId,
                    focus = payload.Focus.ToString(),
                    window = payload.Window,
                    baseline = payload.Baseline,
                    notes = payload.Notes,
                    sections = payload.Sections.Select(s => new { title = s.Title, content = s.Content }),
                    payloadVersion = payload.Version,
                    allowInvestigation,
                    fromUtc = payload.FromUtc,
                    toUtc = payload.ToUtc,
                    utcOffsetMinutes = payload.UtcOffsetMinutes,
                    utcOffsetChanges = payload.UtcOffsetChanges.Select(c => new { fromUtc = c.FromUtc, previousOffsetMinutes = c.PreviousOffsetMinutes, offsetMinutes = c.OffsetMinutes }),
                    modelOverride,
                    conversationId = conversation?.Id,
                    history,
                    question
                },
                new JsonSerializerOptions { WriteIndented = true });
        }

        private static List<AiConversation.Investigation> Investigations(JsonElement root)
        {
            var investigations = new List<AiConversation.Investigation>();
            if (!root.TryGetProperty("investigations", out var list) || list.ValueKind != JsonValueKind.Array) return investigations;

            foreach (var item in list.EnumerateArray())
            {
                string Text(string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

                // Both turns are needed to replay it; one without either would break the alternation the service checks.
                var request = Text("request");
                var result = Text("result");
                if (request.Length > 0 && result.Length > 0) investigations.Add(new AiConversation.Investigation(Text("summary"), request, result));
            }

            return investigations;
        }

        /// <summary>
        /// Asks about the window.  With no <paramref name="conversation"/> that is a first analysis; with one,
        /// and a <paramref name="question"/>, it is the next turn of the exchange on the reader's screen.
        /// </summary>
        internal static async Task<Result> AnalyseAsync(
            PerformanceAnalysisPayload payload,
            AIServiceDiscovery.ServiceInfo service,
            CancellationToken cancellationToken,
            AiConversation? conversation = null,
            string? question = null,
            string? modelOverride = null,
            bool allowInvestigation = false)
        {
            try
            {
                AiServerCertificateValidator.SetPinnedThumbprint(service);
                var baseUrl = service.ServiceUrl
                              ?? ConfigurationManager.AppSettings["AIApiBaseUrl"]
                              ?? "http://localhost:5055";

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/ai/analyse-performance")
                {
                    Content = new StringContent(ToJson(payload, modelOverride, conversation, question, allowInvestigation), Encoding.UTF8, "application/json")
                };

                var apiKey = service.ApiKey ?? await AIApiKeyProvider.GetApiKeyAsync();
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-API-Key", apiKey);

                using var response = await Client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result { Error = AIServiceErrors.Describe(response, body) };
                }

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                return new Result
                {
                    Analysis = root.TryGetProperty("analysis", out var analysis) ? analysis.GetString() : null,
                    Model = root.TryGetProperty("model", out var model) ? model.GetString() : null,
                    GeneratedUtc = root.TryGetProperty("generatedUtc", out var generated) && generated.TryGetDateTime(out var value) ? value : null,
                    ConversationId = root.TryGetProperty("conversationId", out var id) && id.TryGetGuid(out var conversationId) ? conversationId : null,
                    Investigations = Investigations(root)
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The client's own timeout, not the reader closing the window.
                return new Result { Error = $"No answer from the AI service within {Timeout.TotalSeconds:0} seconds." };
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
    }
}
