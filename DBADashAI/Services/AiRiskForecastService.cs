using DBADashAI.Models;
using System.Text.Json;

namespace DBADashAI.Services;

public sealed class AiRiskForecastService(AiRunbookLinkService runbookLinkService)
{
    private readonly AiRunbookLinkService _runbookLinkService = runbookLinkService;

    public List<AiRiskForecastItem> BuildForecasts(IReadOnlyCollection<AiToolExecutionResult> toolResults)
    {
        var risks = new List<AiRiskForecastItem>();

        foreach (var tool in toolResults)
        {
            var rows = GetRows(tool.Data);
            if (rows.Count == 0) continue;

            if (tool.Tool == "backups-risk-summary")
            {
                risks.Add(new AiRiskForecastItem
                {
                    Category = "Backups",
                    Severity = "High",
                    Title = "Backup risk likely to impact recoverability",
                    Evidence = $"{rows.Count} warning/critical backup status records in latest snapshot.",
                    RecommendedAction = "Review failing backup chains and remediate critical databases first.",
                    ConfidenceScore = 0.82,
                    RunbookUrl = _runbookLinkService.Resolve("Backups")
                });
            }
            else if (tool.Tool == "drives-risk-summary")
            {
                risks.Add(new AiRiskForecastItem
                {
                    Category = "Storage",
                    Severity = "High",
                    Title = "Drive capacity pressure forecast",
                    Evidence = $"{rows.Count} storage warnings/critical drive rows detected.",
                    RecommendedAction = "Prioritize low-free-space drives and validate autogrowth safeguards.",
                    ConfidenceScore = 0.8,
                    RunbookUrl = _runbookLinkService.Resolve("Storage")
                });
            }
            else if (tool.Tool == "active-alerts-summary")
            {
                risks.Add(new AiRiskForecastItem
                {
                    Category = "Alerts",
                    Severity = "Medium",
                    Title = "Alert concentration indicates possible recurring instability",
                    Evidence = $"{rows.Count} grouped alert rows captured in current active alert summary.",
                    RecommendedAction = "Focus triage on recurring high-priority alert types and noisy channels.",
                    ConfidenceScore = 0.7,
                    RunbookUrl = _runbookLinkService.Resolve("Alerts")
                });
            }
            else if (tool.Tool == "waits-summary" || tool.Tool == "blocking-summary")
            {
                risks.Add(new AiRiskForecastItem
                {
                    Category = "Performance",
                    Severity = "Medium",
                    Title = "Concurrency/performance pressure trend",
                    Evidence = $"{rows.Count} rows from {tool.Tool} indicate sustained contention signals.",
                    RecommendedAction = "Review top waits/blockers and validate immediate mitigation runbooks.",
                    ConfidenceScore = 0.74,
                    RunbookUrl = _runbookLinkService.Resolve("Performance")
                });
            }
            else if (tool.Tool == "config-drift-summary")
            {
                risks.Add(new AiRiskForecastItem
                {
                    Category = "Configuration",
                    Severity = "Medium",
                    Title = "Recent configuration drift may alter stability",
                    Evidence = $"{rows.Count} recent configuration changes detected.",
                    RecommendedAction = "Validate high-impact changes and rollback risky deltas where required.",
                    ConfidenceScore = 0.68,
                    RunbookUrl = _runbookLinkService.Resolve("Configuration")
                });
            }
        }

        return risks
            .OrderByDescending(r => r.ConfidenceScore)
            .ThenByDescending(r => SeverityRank(r.Severity))
            .ToList();
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "High" => 3,
        "Medium" => 2,
        _ => 1
    };

    private static List<Dictionary<string, object?>> GetRows(object data)
    {
        try
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
            if (!doc.RootElement.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var rows = new List<Dictionary<string, object?>>();
            foreach (var row in rowsElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in row.EnumerateObject())
                {
                    d[prop.Name] = prop.Value.ToString();
                }
                rows.Add(d);
            }
            return rows;
        }
        catch
        {
            return [];
        }
    }
}
