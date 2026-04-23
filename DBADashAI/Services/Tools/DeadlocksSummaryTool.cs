using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class DeadlocksSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public DeadlocksSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "deadlocks-summary";

        public string Name => ToolName;

        public string Description => "Summarize deadlock counts by instance over the last 24 hours.";

        public string InputHint => "Use for questions about deadlocks, lock conflicts, and concurrency failures.";

        public string[] Keywords => ["deadlock", "deadlocks", "victim", "lock conflict", "deadlocking", "deadlocked", "kill", "killed", "aborted", "chosen as victim", "deadlock graph", "lock cycle", "circular", "increasing deadlocks", "deadlock rate", "deadlock count"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_Deadlocks_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
}
