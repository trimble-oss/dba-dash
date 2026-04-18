using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class ConfigDriftSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "config-drift-summary";

    public string Name => ToolName;

    public string Description => "Summarize recent instance/database configuration drift.";

    public string InputHint => "Use for questions about configuration changes, drift, and change-correlation with incidents.";

    public string[] Keywords => ["config", "configuration", "drift", "changed", "change", "trace flag", "option"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                i.InstanceDisplayName,
                o.name AS ConfigName,
                h.value AS PreviousValue,
                h.new_value AS NewValue,
                h.ValidTo AS ChangedUtc,
                o.description
            FROM dbo.SysConfigHistory h
            INNER JOIN dbo.SysConfigOptions o ON o.configuration_id = h.configuration_id
            INNER JOIN dbo.Instances i ON i.InstanceID = h.InstanceID
            WHERE h.ValidTo >= DATEADD(DAY, -7, SYSUTCDATETIME())
              AND i.IsActive = 1
            ORDER BY h.ValidTo DESC;
            """;

        var rows = await sql.QueryAsync(sqlText, request.MaxRows, cancellationToken);
        return new AiToolResult
        {
            RowCount = rows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                rows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "dbo.SysConfigHistory + dbo.SysConfigOptions + dbo.Instances",
                    Detail = "Instance-level configuration changes in last 7 days."
                }
            ]
        };
    }
}
