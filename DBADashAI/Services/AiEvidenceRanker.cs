using DBADashAI.Models;

namespace DBADashAI.Services
{
    public class AiEvidenceRanker
    {
        public List<AiEvidenceItem> Rank(IEnumerable<AiEvidenceItem> evidenceItems)
        {
            var grouped = evidenceItems
                .Where(e => !string.IsNullOrWhiteSpace(e.Source))
                .GroupBy(
                    e => $"{e.Source.Trim()}||{e.Detail.Trim()}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var parts = g.Key.Split(new[] { "||" }, 2, StringSplitOptions.None);
                    var source = parts.Length > 0 ? parts[0] : string.Empty;
                    var detail = parts.Length > 1 ? parts[1] : string.Empty;

                    return new AiEvidenceItem
                    {
                        Source = source,
                        Detail = detail,
                        Score = Math.Round(CalculateScore(source, detail, g.Count()), 2)
                    };
                })
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.Source)
                .ThenBy(e => e.Detail)
                .ToList();

            for (var i = 0; i < grouped.Count; i++)
            {
                grouped[i].Rank = i + 1;
            }

            return grouped;
        }

        private static double CalculateScore(string source, string detail, int frequency)
        {
            var score = 0.3;

            if (source.Contains("+", StringComparison.OrdinalIgnoreCase)) score += 0.2;
            if (source.Contains("Alert", StringComparison.OrdinalIgnoreCase)) score += 0.1;
            if (source.Contains("Wait", StringComparison.OrdinalIgnoreCase)) score += 0.1;
            if (source.Contains("Backup", StringComparison.OrdinalIgnoreCase)) score += 0.1;
            if (detail.Contains("last 24", StringComparison.OrdinalIgnoreCase) || detail.Contains("last 7", StringComparison.OrdinalIgnoreCase)) score += 0.1;

            score += Math.Min(frequency, 5) * 0.04;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}