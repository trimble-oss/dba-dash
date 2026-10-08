using System.Text.Json;

namespace DBADashAI.Models
{
    /// <summary>
    /// A follow-up to a question asked through <see cref="AiAskRequest"/>.
    ///
    /// The service keeps no conversation, so this carries back what the first answer was built from -
    /// the original question, the tool data and the evidence, exactly as the /ask response returned
    /// them - along with everything said since.  The tools are deliberately not run again: the data
    /// would have moved on, and the model would be asked to reconcile its first answer with numbers
    /// it was never shown.  A question that needs fresh data is a new question.
    /// </summary>
    public class AiAskFollowUpRequest
    {
        /// <summary>Generous for a tool's data, which the prompt serializer trims to a far smaller budget anyway.</summary>
        public const int MaxDataLength = 2 * 1024 * 1024;

        public const int MaxEvidenceItems = 200;

        /// <summary>
        /// Evidence goes into the prompt as it comes, outside the serializer's budget, so it is bounded
        /// here.  What /ask produces is a few hundred characters per item.
        /// </summary>
        public const int MaxEvidenceLength = 32 * 1024;

        /// <summary>The question the first answer was about.</summary>
        public string OriginalQuestion { get; set; } = string.Empty;

        /// <summary>The tools that ran, in order, as the /ask response listed them in ToolRuns.</summary>
        public List<AiAskToolRun> ToolRuns { get; set; } = [];

        /// <summary>The /ask response's Data: one tool's result, or the multi-tool wrapper.</summary>
        public JsonElement Data { get; set; }

        /// <summary>The /ask response's ranked evidence.</summary>
        public List<AiEvidenceItem> Evidence { get; set; } = [];

        public string? ConfidenceLabel { get; set; }

        /// <summary>Everything said after the opening prompt, starting with the first answer - see <see cref="AiConversation"/>.</summary>
        public List<AiConversationTurn> History { get; set; } = [];

        public string Question { get; set; } = string.Empty;

        /// <summary>Override the configured AI model for this request.  Null = use configured default.</summary>
        public string? ModelOverride { get; set; }

        /// <summary>
        /// Checks the request, given the names of the tools this service has.  A tool name goes into the
        /// prompt, so one the service does not know is refused rather than passed on.
        /// </summary>
        public string? Validate(IReadOnlyCollection<string> knownTools)
        {
            // An explicit JSON null replaces the initializers above, and a null element survives into
            // the list.  Either would throw below, turning bad input into a 500 rather than a 400.
            if (ToolRuns is null || ToolRuns.Any(r => r is null))
            {
                return "ToolRuns must be a list of tool runs.";
            }

            if (Evidence is null || Evidence.Any(e => e is null))
            {
                return "Evidence must be a list of evidence items.";
            }

            if (History is null || History.Any(t => t is null))
            {
                return "History must be a list of conversation turns.";
            }

            if (string.IsNullOrWhiteSpace(OriginalQuestion))
            {
                return "OriginalQuestion is required.";
            }

            if (OriginalQuestion.Length > AiAskRequest.MaxQuestionLength)
            {
                return $"OriginalQuestion exceeds maximum length of {AiAskRequest.MaxQuestionLength} characters.";
            }

            if (History.Count == 0)
            {
                return "A follow-up needs the conversation it follows.";
            }

            if (ToolRuns.Count == 0)
            {
                return "ToolRuns is required.";
            }

            var unknown = ToolRuns.FirstOrDefault(r => !knownTools.Contains(r.Tool, StringComparer.OrdinalIgnoreCase));
            if (unknown is not null)
            {
                return "ToolRuns names a tool this service does not have.";
            }

            if (Data.ValueKind == JsonValueKind.Undefined)
            {
                return "Data is required.";
            }

            if (Data.GetRawText().Length > MaxDataLength)
            {
                return $"Data exceeds {MaxDataLength} characters.";
            }

            if (Evidence.Count > MaxEvidenceItems)
            {
                return $"Evidence exceeds {MaxEvidenceItems} items.";
            }

            if (Evidence.Sum(e => (e.Source?.Length ?? 0) + (e.Detail?.Length ?? 0)) > MaxEvidenceLength)
            {
                return $"Evidence exceeds {MaxEvidenceLength} characters.";
            }

            if (ConfidenceLabel is { Length: > 20 })
            {
                return "ConfidenceLabel is not a confidence label.";
            }

            return AiConversation.Validate(History, Question);
        }

        /// <summary>
        /// The tool results the first answer was built from, put back together from the response that
        /// carried them.  A single tool's data is the response data itself; several tools' data sit in
        /// the wrapper's "tools" array, in the same order as <see cref="ToolRuns"/>.
        /// </summary>
        /// <returns>Null when the data is not the shape ToolRuns says it is.</returns>
        public List<AiToolExecutionResult>? ToToolResults()
        {
            if (ToolRuns.Count == 1)
            {
                return [Result(ToolRuns[0], Data)];
            }

            if (Data.ValueKind != JsonValueKind.Object
                || !TryGetProperty(Data, "tools", out var tools)
                || tools.ValueKind != JsonValueKind.Array
                || tools.GetArrayLength() != ToolRuns.Count)
            {
                return null;
            }

            var results = new List<AiToolExecutionResult>();
            var i = 0;
            foreach (var tool in tools.EnumerateArray())
            {
                if (tool.ValueKind != JsonValueKind.Object || !TryGetProperty(tool, "data", out var data))
                {
                    return null;
                }

                results.Add(Result(ToolRuns[i++], data));
            }

            return results;

            static AiToolExecutionResult Result(AiAskToolRun run, JsonElement data) => new()
            {
                Tool = run.Tool,
                Data = data,
                RowCount = run.RowCount,
                ExecutionMs = run.ExecutionMs
            };
        }

        /// <summary>The wrapper is serialized with default (Pascal case) names; accept either case.</summary>
        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
