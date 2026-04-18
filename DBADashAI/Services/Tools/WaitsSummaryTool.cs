using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class WaitsSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "waits-summary";

    public string Name => ToolName;

    public string Description => "Summarize top waits over the last 24 hours across active instances.";

    public string InputHint => "Use for questions about wait stats, bottlenecks, and SQL Server pressure sources.";

    public string[] Keywords => ["wait", "waits", "bottleneck", "pressure", "latch"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                i.InstanceDisplayName,
                wt.WaitType,
                wt.Description,
                SUM(w.wait_time_ms) / 1000.0 AS TotalWaitSec,
                SUM(w.signal_wait_time_ms) / 1000.0 AS SignalWaitSec,
                SUM(w.waiting_tasks_count) AS WaitingTasksCount
            FROM dbo.Waits_60MIN w
            INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
            INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
            WHERE w.SnapshotDate >= DATEADD(HOUR, -24, SYSUTCDATETIME())
              AND wt.IsExcluded = 0
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName, wt.WaitType, wt.Description
            ORDER BY TotalWaitSec DESC;
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
                    Source = "dbo.Waits_60MIN + dbo.WaitType + dbo.Instances",
                    Detail = "Top waits over the last 24 hours (excluded waits removed)."
                }
            ]
        };
    }
}
