using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class BlockingSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public BlockingSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "blocking-summary";

        public string Name => ToolName;

        public string Description => "Show recent blocking trends and top blocked instances.";

        public string InputHint => "Use for questions about blocking, blocked sessions, and wait impact.";

        public string[] Keywords => ["block", "blocking", "blocked", "blocked session", "blocked sessions", "lock", "locked", "locking", "head blocker", "lead blocker", "waiting", "wait time", "chain", "blocked process", "lock timeout", "lock escalation", "resource_semaphore", "suspension", "suspended", "how long blocked", "timeframe", "time frame", "when did", "how many blocked"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_Blocking_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
}
