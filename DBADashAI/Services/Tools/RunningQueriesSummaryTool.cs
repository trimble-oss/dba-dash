using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class RunningQueriesSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public RunningQueriesSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "running-queries-summary";

        public string Name => ToolName;

        public string Description => "Show running query trends and detailed long-running, blocked, or resource-heavy queries captured in recent snapshots.";

        public string InputHint => "Use for questions about currently running queries, long-running queries, blocked sessions with detail, memory grants, sleeping sessions with open transactions, or tempdb pressure from queries.";

        public string[] Keywords =>
        [
            "running quer", "long running", "long-running", "currently running",
            "active quer", "active session",
            "head blocker", "blocking session", "blocked by", "who is blocking",
            "memory grant", "granted memory", "resource_semaphore",
            "sleeping session", "idle session", "open transaction", "forgotten transaction",
            "tempdb pressure", "tempdb usage", "tempdb alloc",
            "session", "spid", "what is running",
            "elapsed time", "running for", "how long"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_RunningQueries_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    summaryTrends = results.ElementAtOrDefault(0) ?? [],
                    longRunningOrBlockedDetail = results.ElementAtOrDefault(1) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.RunningQueriesSummary + dbo.RunningQueries",
                        Detail = "Running query summary trends and detailed long-running/blocked/high-memory-grant queries from recent snapshots."
                    }
                ]
            };
        }
    }
}
