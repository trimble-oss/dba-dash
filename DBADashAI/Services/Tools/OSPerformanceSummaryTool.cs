using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class OSPerformanceSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public OSPerformanceSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "os-performance-summary";

        public string Name => ToolName;

        public string Description => "Show CPU utilization trends, key SQL Server performance counters (PLE, batch requests, memory grants, etc.), and top stored procedures by resource consumption.";

        public string InputHint => "Use for questions about CPU usage, performance counters, PLE, batch requests, memory grants, compilations, top procedures by CPU/IO, or overall instance performance.";

        public string[] Keywords =>
        [
            "cpu", "cpu usage", "cpu utilization", "processor", "cpu history",
            "ple", "page life expectancy", "buffer pool",
            "batch request", "transactions per second", "tps",
            "compilation", "recompilation", "recompile",
            "memory grant", "grants pending", "grants outstanding",
            "lazy write", "free list stall",
            "performance counter", "perf counter",
            "top procedure", "top proc", "top cpu", "top io",
            "object execution", "proc stats", "procedure performance",
            "user connections", "connections",
            "checkpoint", "log flush",
            "overall performance", "instance performance", "how is the server"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_OSPerformance_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    cpuTrends = results.ElementAtOrDefault(0) ?? [],
                    performanceCounters = results.ElementAtOrDefault(1) ?? [],
                    topProcedures = results.ElementAtOrDefault(2) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.CPU + dbo.PerformanceCounters + dbo.Counters + dbo.ObjectExecutionStats + dbo.ObjectName",
                        Detail = "CPU utilization trends, key performance counters (PLE, batch requests, memory grants, etc.), and top stored procedures by CPU and elapsed time."
                    }
                ]
            };
        }
    }
}
