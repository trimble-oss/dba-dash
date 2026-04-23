using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class TableSizeSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public TableSizeSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "table-size-summary";

        public string Name => ToolName;

        public string Description => "Show largest tables and table growth trends with row count and space changes over time.";

        public string InputHint => "Use for questions about table sizes, largest tables, table growth, row counts, index space, or which tables are consuming the most storage.";

        public string[] Keywords =>
        [
            "table size", "table space", "largest table", "biggest table",
            "table growth", "row count", "row growth", "rows",
            "index size", "index space", "unused space",
            "which table", "growing table", "shrinking table",
            "space consumed", "space usage", "reserved space",
            "data pages", "index pages"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_TableSize_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    largestTables = results.ElementAtOrDefault(0) ?? [],
                    tableGrowth = results.ElementAtOrDefault(1) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.TableSize + dbo.ObjectName + dbo.Databases + dbo.Instances",
                        Detail = "Current largest tables with row counts and space breakdown, plus table growth trends over the specified time window."
                    }
                ]
            };
        }
    }
}
