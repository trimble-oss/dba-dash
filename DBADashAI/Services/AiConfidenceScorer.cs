using DBADashAI.Models;

namespace DBADashAI.Services;

public sealed class AiConfidenceScorer
{
    public (double score, string label) Score(
        IReadOnlyCollection<AiToolExecutionResult> tools,
        IReadOnlyCollection<AiEvidenceItem> rankedEvidence)
    {
        if (tools.Count == 0)
        {
            return (0.1, "Low");
        }

        var toolCount = tools.Count;
        var rows = tools.Sum(t => t.RowCount);
        var evidenceCount = rankedEvidence.Count;
        var topEvidenceScore = rankedEvidence.Count == 0 ? 0 : rankedEvidence.Max(e => e.Score);
        var avgEvidenceScore = rankedEvidence.Count == 0 ? 0 : rankedEvidence.Average(e => e.Score);

        var score = 0.2; // base confidence when at least one tool ran
        score += Math.Min(toolCount, 3) * 0.12;
        score += Math.Min(evidenceCount, 8) * 0.03;
        score += (topEvidenceScore * 0.18);
        score += (avgEvidenceScore * 0.12);
        score += rows switch
        {
            > 200 => 0.22,
            > 50 => 0.16,
            > 0 => 0.08,
            _ => 0.0
        };

        score = Math.Clamp(score, 0.0, 1.0);
        var label = score switch
        {
            >= 0.8 => "High",
            >= 0.55 => "Medium",
            _ => "Low"
        };

        return (Math.Round(score, 2), label);
    }
}
