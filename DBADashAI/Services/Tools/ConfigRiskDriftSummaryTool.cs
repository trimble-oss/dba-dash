using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class ConfigRiskDriftSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "config-risk-drift-summary";

    public string Name => ToolName;

    public string Description => "Summarize high-risk configuration drift and change hotspots by instance.";

    public string InputHint => "Use for config drift risk, patch/config change correlation, and change review questions.";

    public string[] Keywords => ["drift", "config risk", "configuration change", "trace flag", "patch", "changed", "change risk"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 400
                i.InstanceDisplayName,
                o.name AS ConfigName,
                h.value AS PreviousValue,
                h.new_value AS NewValue,
                h.ValidTo AS ChangedUtc,
                o.description
            FROM dbo.SysConfigHistory h
            INNER JOIN dbo.SysConfigOptions o ON o.configuration_id = h.configuration_id
            INNER JOIN dbo.Instances i ON i.InstanceID = h.InstanceID
            WHERE h.ValidTo >= DATEADD(DAY,-14,SYSUTCDATETIME())
              AND i.IsActive = 1
            ORDER BY h.ValidTo DESC;
            """;

        var rows = await sql.QueryAsync(sqlText, Math.Max(request.MaxRows, 150), cancellationToken);

        static int RiskWeight(string configName)
        {
            if (configName.Contains("max degree of parallelism", StringComparison.OrdinalIgnoreCase)) return 5;
            if (configName.Contains("cost threshold for parallelism", StringComparison.OrdinalIgnoreCase)) return 5;
            if (configName.Contains("max server memory", StringComparison.OrdinalIgnoreCase)) return 5;
            if (configName.Contains("query", StringComparison.OrdinalIgnoreCase)) return 4;
            if (configName.Contains("trace", StringComparison.OrdinalIgnoreCase)) return 4;
            return 2;
        }

        var byInstance = rows
            .GroupBy(r => Get(r, "InstanceDisplayName"))
            .Select(g => new
            {
                Instance = g.Key,
                ChangeCount = g.Count(),
                DistinctConfigsChanged = g.Select(x => Get(x, "ConfigName")).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                RiskScore = g.Sum(x => RiskWeight(Get(x, "ConfigName")))
            })
            .OrderByDescending(x => x.RiskScore)
            .ThenByDescending(x => x.ChangeCount)
            .ToList();

        return new AiToolResult
        {
            RowCount = rows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                byInstance,
                rows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "dbo.SysConfigHistory + dbo.SysConfigOptions + dbo.Instances",
                    Detail = "Configuration drift over 14 days with weighted risk scoring"
                }
            ]
        };
    }

    private static string Get(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }
}
