using DBADashAI.Models;

namespace DBADashAI.Services
{
    public class AiConfidenceScorer
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

            // Tool breadth: up to +0.36 for 3 tools
            score += Math.Min(toolCount, 3) * 0.12;

            // Evidence quality: primary drivers of confidence
            score += Math.Min(evidenceCount, 8) * 0.03;  // up to +0.24
            score += topEvidenceScore * 0.18;
            score += avgEvidenceScore * 0.12;

            // Row count: minor modifier only when evidence is present, capped at +0.08.
            // High row count means more data was available, not that the analysis is better.
            if (evidenceCount > 0 && rows > 0)
            {
                score += rows switch
                {
                    > 50 => 0.08,
                    _ => 0.04
                };
            }

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
}
