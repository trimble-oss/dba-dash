using DBADashAI.Models;
using System.Text;

namespace DBADashAI.Services;

public sealed class AiSummaryFormatter
{
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

        foreach (var tool in toolResults)
        {
            sb.AppendLine($"- Tool: {tool.Tool}");
            sb.AppendLine($"  RowCount: {tool.RowCount}");
            sb.AppendLine($"  ExecutionMs: {tool.ExecutionMs}");
            sb.AppendLine($"  Data: {System.Text.Json.JsonSerializer.Serialize(tool.Data)}");
            sb.AppendLine();
        }

        sb.AppendLine("Ranked Evidence (highest quality first):");
        foreach (var ev in rankedEvidence.Take(12))
        {
            sb.AppendLine($"- Rank {ev.Rank}, Score {ev.Score:0.00}: {ev.Source} | {ev.Detail}");
        }

        sb.AppendLine();
        sb.AppendLine("Response requirements:");
        sb.AppendLine("- Use markdown headings exactly: ## Summary, ## Top Findings, ## Recommended Actions, ## Confidence");
        sb.AppendLine("- Only use facts present in tool data and ranked evidence");
        sb.AppendLine("- Do not include evidence citation tokens like '(Evidence #1)' in the final response");
        sb.AppendLine("- If data is insufficient, explicitly state uncertainty");
        sb.AppendLine("- Keep response concise and actionable for on-call DBAs");
        sb.AppendLine("- In ## Confidence include label and one sentence on key confidence drivers");
        sb.AppendLine("- When available, include specific instance/server names, alert keys, and complaint details (e.g., last message)");

        return sb.ToString();
    }
}
