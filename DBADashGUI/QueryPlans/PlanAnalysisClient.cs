#nullable enable
using DBADash.QueryPlan.Analysis;
using DBADashGUI.AI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Sends a query plan to the AI service for analysis, using the same discovery and API key path as
    /// the assistant and the deadlock viewer: a service registered in the repository, or a local one on
    /// loopback.
    ///
    /// The payload is serialised from the object the viewer previewed, so what was shown is what goes.
    /// Nothing is sent until someone presses the button.
    /// </summary>
    internal static class PlanAnalysisClient
    {
        // Plans are larger than deadlock graphs and the answers longer, so the wait can be longer too.
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(240) };

        internal sealed class Result
        {
            public string? Analysis { get; init; }

            public string? Model { get; init; }

            /// <summary>When the analysis was produced.</summary>
            public DateTime? GeneratedUtc { get; init; }

            /// <summary>
            /// The conversation the answer belongs to, as the service recorded it.  Sent back with each
            /// follow-up so the exchange is stored as one rather than as unrelated answers.
            /// </summary>
            public Guid? ConversationId { get; init; }


            /// <summary>
            /// Where in the conversation this answer sits, as the service numbered it.  Taken from the
            /// service rather than counted here, so the turn the viewer stores is the turn the service
            /// thinks it produced.
            /// </summary>
            public int? TurnNumber { get; init; }

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
            PlanAnalysisPayload payload,
            int? instanceId = null,
            string? modelOverride = null,
            AiConversation? conversation = null,
            string? question = null)
        {
            // Everything said about this plan so far.  Empty on a first analysis, which is what the
            // service reads as one.  The plan itself is not in here: the service builds the opening
            // question from the payload below, so a follow-up cannot quietly change what the first
            // answer was about.
            var history = (conversation?.ToHistory() ?? new List<AiConversation.WireTurn>())
                .Select(t => new { role = t.Role, content = t.Content })
                .ToList();

            return JsonSerializer.Serialize(
                new
                {
                    planXml = payload.PlanXml,
                    statementText = payload.StatementText,
                    statementTruncated = payload.StatementTruncated,
                    signature = payload.Signature,
                    planHash = payload.PlanHash,
                    payloadVersion = payload.Version,
                    instanceId,
                    instance = payload.Instance,
                    fileName = payload.FileName,
                    context = payload.Context,
                    statistics = payload.Statistics,
                    insights = payload.Insights,
                    missingIndexes = payload.MissingIndexes,
                    operators = payload.Operators,
                    waits = payload.Waits,
                    parameters = payload.Parameters,
                    objects = payload.Objects,
                    isActualPlan = payload.IsActualPlan,
                    modelOverride,
                    conversationId = conversation?.Id,
                    history,
                    question,
                    // A follow-up is kept on this machine, not by the service: it belongs to whoever
                    // asked it.  See AiLocalConversationStore.  The opening analysis is shared and
                    // stays the service's.
                    clientStoresTurn = question is not null
                },
                new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Asks about the plan.  With no <paramref name="conversation"/> that is a first analysis; with
        /// one, and a <paramref name="question"/>, it is the next turn of the exchange already on the
        /// reader's screen.
        /// </summary>
        internal static async Task<Result> AnalyseAsync(
            PlanAnalysisPayload payload,
            AIServiceDiscovery.ServiceInfo service,
            CancellationToken cancellationToken,
            int? instanceId = null,
            AiConversation? conversation = null,
            string? question = null)
        {
            try
            {
                var baseUrl = service.ServiceUrl
                              ?? ConfigurationManager.AppSettings["AIApiBaseUrl"]
                              ?? "http://localhost:5055";

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/api/ai/analyse-plan")
                {
                    Content = new StringContent(
                        ToJson(payload, instanceId, modelOverride: null, conversation, question),
                        Encoding.UTF8,
                        "application/json")
                };

                var apiKey = service.ApiKey ?? await AIApiKeyProvider.GetApiKeyAsync();
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-API-Key", apiKey);

                using var response = await Client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result { Error = $"{(int)response.StatusCode} {response.ReasonPhrase}: {Describe(body)}" };
                }

                using var document = JsonDocument.Parse(body);
                return new Result
                {
                    Analysis = Read(document, "analysis"),
                    Model = Read(document, "model"),
                    GeneratedUtc = document.RootElement.TryGetProperty("generatedUtc", out var generated) &&
                                   generated.TryGetDateTime(out var value)
                        ? value
                        : null,
                    ConversationId = document.RootElement.TryGetProperty("conversationId", out var id) &&
                                     id.TryGetGuid(out var conversationId)
                        ? conversationId
                        : null,
                    TurnNumber = document.RootElement.TryGetProperty("turnNumber", out var turn) &&
                                 turn.TryGetInt32(out var turnNumber)
                        ? turnNumber
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

        /// <summary>
        /// What the service said, rather than the envelope it said it in.  A refusal arrives as
        /// ProblemDetails, and what belongs in front of the reader is its detail - which for the one
        /// refusal they can do something about, a plan larger than the model will take, is the part
        /// saying what to do about it.
        /// </summary>
        private static string Describe(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "(no detail)";

            try
            {
                using var document = JsonDocument.Parse(body);

                foreach (var property in new[] { "detail", "error" })
                {
                    if (document.RootElement.TryGetProperty(property, out var value)
                        && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }
            }
            catch (JsonException)
            {
                // Something other than the service answering - a proxy, or a host that fell over
                // before the service saw the request.  Whatever it said is better than nothing.
            }

            return body;
        }

        private static string? Read(JsonDocument document, string property) =>
            document.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
