using DBADashAI.Models;
using System.Text;

namespace DBADashAI.Services
{
    public class AiSummaryFormatter
    {
        private readonly AiPromptDataSerializer _dataSerializer;

        public AiSummaryFormatter(AiPromptDataSerializer dataSerializer)
        {
            _dataSerializer = dataSerializer;
        }

        public string BuildSummaryPayload(
            string userQuestion,
            IReadOnlyCollection<AiToolExecutionResult> toolResults,
            IReadOnlyCollection<AiEvidenceItem> rankedEvidence,
            string confidenceLabel,
            string rcaTemplate)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"User Question: {userQuestion}");
            sb.AppendLine($"Computed Confidence: {confidenceLabel}");
            sb.AppendLine($"RCA Template Guidance: {rcaTemplate}");
            sb.AppendLine();
            sb.AppendLine("Tool Results:");
            sb.Append(_dataSerializer.Serialize(toolResults));

            sb.AppendLine("Ranked Evidence (highest quality first):");
            foreach (var ev in rankedEvidence.Take(12))
            {
                sb.AppendLine($"- Rank {ev.Rank}, Score {ev.Score:0.00}: {ev.Source} | {ev.Detail}");
            }

            sb.AppendLine();
            sb.AppendLine("Response requirements:");
            sb.AppendLine("- Follow the FORMAT and SEVERITY AND VISUAL EMPHASIS rules from the system prompt exactly");
            sb.AppendLine("- Use the headings: ## Summary, ## Top Findings, ## Recommended Actions, ## Confidence (in that order, and no others)");
            sb.AppendLine("- Prefix each finding with a stoplight icon (🔴 Critical, 🟡 Warning, 🟢 Healthy/Informational) and order findings most-critical-first");
            sb.AppendLine("- Only use facts present in tool data and ranked evidence");
            sb.AppendLine("- Do not include evidence citation tokens like '(Evidence #1)' in the final response");
            sb.AppendLine("- If data is insufficient, explicitly state uncertainty");
            sb.AppendLine("- Keep response concise and actionable for on-call DBAs");
            sb.AppendLine("- In ## Confidence include label and one sentence on key confidence drivers");
            sb.AppendLine("- When available, include specific instance/server names, alert keys, and complaint details (e.g., last message)");
            sb.AppendLine();

            // Always included, not only on a follow-up: a follow-up rebuilds this prompt, and it has to
            // be the prompt the first answer was written against.
            sb.AppendLine(
                "The reader can ask follow-up questions after this answer.  Answer those directly and as briefly " +
                "as the question allows, without the four headings and without repeating what you have already " +
                "said - the whole conversation stays on their screen.  The tool data and evidence above are still " +
                "all you know, so where a follow-up asks for something they do not contain, say so and suggest a " +
                "new question that would fetch it rather than guessing.");

            return sb.ToString();
        }
    }
}