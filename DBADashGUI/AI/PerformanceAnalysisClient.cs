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
        private static readonly HttpClient Client = AiServerCertificateValidator.CreateClient(AIServiceTimeouts.Request);

        internal sealed class Result
        {
            public string? Analysis { get; init; }

            public string? Model { get; init; }

            public DateTime? GeneratedUtc { get; init; }

            /// <summary>The conversation the answer belongs to, minted by the service on the first turn.</summary>
            public Guid? ConversationId { get; init; }

            public string? Error { get; init; }

            public bool Success => Error is null;
        }

        internal static string ToJson(
            PerformanceAnalysisPayload payload,
            string? modelOverride = null,
            AiConversation? conversation = null,
            string? question = null)
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
                    modelOverride,
                    conversationId = conversation?.Id,
                    history,
                    question
                },
                new JsonSerializerOptions { WriteIndented = true });
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
            string? modelOverride = null)
        {
            try
            {
                AiServerCertificateValidator.SetPinnedThumbprint(service);
                var baseUrl = service.ServiceUrl
                              ?? ConfigurationManager.AppSettings["AIApiBaseUrl"]
                              ?? "http://localhost:5055";

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/ai/analyse-performance")
                {
                    Content = new StringContent(ToJson(payload, modelOverride, conversation, question), Encoding.UTF8, "application/json")
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
                    ConversationId = root.TryGetProperty("conversationId", out var id) && id.TryGetGuid(out var conversationId) ? conversationId : null
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The client's own timeout, not the reader closing the window.
                return new Result { Error = $"No answer from the AI service within {AIServiceTimeouts.Request.TotalSeconds:0} seconds." };
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
