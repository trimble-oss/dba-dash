using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class DeadlocksSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "deadlocks-summary";

    public string Name => ToolName;

    public string Description => "Summarize deadlock counts by instance over the last 24 hours.";

    public string InputHint => "Use for questions about deadlocks, lock conflicts, and concurrency failures.";

    public string[] Keywords => ["deadlock", "deadlocks", "victim", "lock conflict"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                i.InstanceDisplayName,
                SUM(CAST(ROUND(((pc.Value_Total / NULLIF(pc.SampleCount, 0)) * 60.0), 0) AS BIGINT)) AS DeadlockCountEstimate,
                MAX(pc.SnapshotDate) AS LatestSnapshotDate
            FROM dbo.PerformanceCounters_60MIN pc
            INNER JOIN dbo.Counters c ON c.CounterID = pc.CounterID
            INNER JOIN dbo.Instances i ON i.InstanceID = pc.InstanceID
            WHERE pc.SnapshotDate >= DATEADD(HOUR, -24, SYSUTCDATETIME())
              AND c.counter_name = 'Number of Deadlocks/sec'
              AND c.object_name = 'Locks'
              AND c.instance_name = '_Total'
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY DeadlockCountEstimate DESC;
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
                    Source = "dbo.PerformanceCounters_60MIN + dbo.Counters + dbo.Instances",
                    Detail = "Derived deadlock/sec trend rolled up for last 24 hours."
                }
            ]
        };
    }
}
