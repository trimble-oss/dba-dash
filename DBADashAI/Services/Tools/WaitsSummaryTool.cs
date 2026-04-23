using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class WaitsSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public WaitsSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "waits-summary";

        public string Name => ToolName;

        public string Description => "Summarize top waits over the last 24 hours across active instances.";

        public string InputHint => "Use for questions about wait stats, bottlenecks, and SQL Server pressure sources.";

        public string[] Keywords => ["wait", "waits", "wait type", "wait stat", "wait stats", "bottleneck", "pressure", "latch", "pageiolatch", "cxpacket", "sos_scheduler_yield", "writelog", "async_network_io", "resource_semaphore", "io stall", "signal wait", "what is it waiting on", "why slow", "why is it slow", "performance", "perf", "wait profile", "top waits"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_Waits_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
}
