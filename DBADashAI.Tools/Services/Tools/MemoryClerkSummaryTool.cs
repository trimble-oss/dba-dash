using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class MemoryClerkSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public MemoryClerkSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "memory-clerk-summary";

        public string Name => ToolName;

        public string Description => "Summarize SQL Server memory clerk usage per instance, showing the top memory consumers (buffer pool, plan cache, and other clerks) from the latest snapshot.";

        public string InputHint => "Use for questions about where memory is being used, memory clerk breakdown, buffer pool size, plan cache bloat, or which component is consuming the most memory.";

        public string[] Keywords => ["memory clerk", "memory clerks", "memory usage", "memory consumer", "buffer pool", "plan cache", "memory breakdown", "where is memory", "memory consumption", "memory allocation", "cache bloat", "stolen memory", "memory footprint", "ram usage", "memory pressure"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("AI.MemoryClerkUsage_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.MemoryUsage + dbo.MemoryClerkType",
                        Detail = "Top memory clerks by pages from the latest memory snapshot per instance, with percentage of total pages."
                    }
                ]
            };
        }
    }
}
