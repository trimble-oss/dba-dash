using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class BlockingSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "blocking-summary";

    public string Name => ToolName;

    public string Description => "Show recent blocking trends and top blocked instances.";

    public string InputHint => "Use for questions about blocking, blocked sessions, and wait impact.";

    public string[] Keywords => ["block", "blocking", "blocked", "wait"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                i.InstanceDisplayName,
                bs.SnapshotDateUTC AS SnapshotDate,
                bs.BlockedSessionCount,
                bs.BlockedWaitTime
            FROM dbo.BlockingSnapshotSummary bs
            INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
            WHERE bs.SnapshotDateUTC >= DATEADD(HOUR, -24, SYSUTCDATETIME())
              AND i.IsActive = 1
            ORDER BY bs.SnapshotDateUTC DESC, bs.BlockedWaitTime DESC;
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
                    Source = "dbo.BlockingSnapshotSummary + dbo.Instances",
                    Detail = "Last 24 hours ordered by most recent and blocked wait time"
                }
            ]
        };
    }
}
