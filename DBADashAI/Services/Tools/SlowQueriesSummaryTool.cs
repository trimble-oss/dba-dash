using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class SlowQueriesSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "slow-queries-summary";

    public string Name => ToolName;

    public string Description => "Summarize highest-impact slow query groups for the last 24 hours.";

    public string InputHint => "Use for questions about slow queries, regressions, and expensive SQL execution.";

    public string[] Keywords => ["slow query", "slow queries", "top query", "query", "regression", "duration", "cpu"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                i.InstanceDisplayName,
                ISNULL(NULLIF(sq.object_name, ''), '{ad-hoc}') AS ObjectName,
                COUNT_BIG(*) AS ExecCount,
                SUM(sq.Duration) / 1000000.0 AS TotalDurationSec,
                SUM(sq.cpu_time) / 1000000.0 AS TotalCpuSec,
                SUM(sq.logical_reads + sq.writes) AS TotalIO,
                MAX(sq.timestamp) AS LastSeenUtc
            FROM dbo.SlowQueries sq
            INNER JOIN dbo.Instances i ON i.InstanceID = sq.InstanceID
            WHERE sq.timestamp >= DATEADD(HOUR, -24, SYSUTCDATETIME())
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName, ISNULL(NULLIF(sq.object_name, ''), '{ad-hoc}')
            ORDER BY TotalDurationSec DESC;
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
                    Source = "dbo.SlowQueries + dbo.Instances",
                    Detail = "Aggregated by instance/object for last 24 hours."
                }
            ]
        };
    }
}
