using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class AgentJobAlertsTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "agent-job-alerts";

    public string Name => ToolName;

    public string Description => "List active or recently resolved agent job related alerts.";

    public string InputHint => "Use for questions about failed jobs, job alerts, and SQL Agent incidents.";

    public string[] Keywords => ["job", "agent", "failed", "failure"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                aa.AlertID,
                aa.AlertType,
                aa.AlertKey,
                aa.Priority,
                aa.IsResolved,
                aa.ResolvedDate,
                aa.UpdatedDate,
                aa.LastMessage,
                i.InstanceDisplayName,
                i.ConnectionID
            FROM Alert.ActiveAlerts aa
            INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
            WHERE (aa.AlertType LIKE '%AgentJob%' OR aa.AlertKey LIKE '%AgentJob%' OR aa.AlertKey LIKE '%Job%')
              AND aa.UpdatedDate >= DATEADD(DAY, -7, SYSUTCDATETIME())
            ORDER BY aa.IsResolved ASC, aa.Priority ASC, aa.UpdatedDate DESC;
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
                    Source = "Alert.ActiveAlerts + dbo.Instances",
                    Detail = "Agent-job related alerts from last 7 days"
                }
            ]
        };
    }
}
