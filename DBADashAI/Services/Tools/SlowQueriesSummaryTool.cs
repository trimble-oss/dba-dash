using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class SlowQueriesSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public SlowQueriesSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "slow-queries-summary";

        public string Name => ToolName;

        public string Description => "Summarize highest-impact slow query groups for the last 24 hours.";

        public string InputHint => "Use for questions about slow queries, regressions, and expensive SQL execution.";

        public string[] Keywords => ["slow query", "slow queries", "top query", "top queries", "query", "queries", "regression", "regressed", "duration", "cpu", "expensive", "costly", "long running", "longest", "heaviest", "cpu intensive", "high cpu", "procedure", "stored proc", "sproc", "ad hoc", "ad-hoc", "execution count", "reads", "writes", "io", "query performance", "plan change", "plan regression", "what's consuming", "resource usage"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_SlowQueries_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
}
