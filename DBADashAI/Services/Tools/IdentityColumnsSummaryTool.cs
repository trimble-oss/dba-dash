using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class IdentityColumnsSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public IdentityColumnsSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "identity-columns-summary";

        public string Name => ToolName;

        public string Description => "Show identity columns that are at risk of exhaustion, with percent used, growth rate, and estimated days remaining.";

        public string InputHint => "Use for questions about identity column exhaustion, INT overflow risk, identity range, or capacity planning for auto-increment columns.";

        public string[] Keywords =>
        [
            "identity", "identity column", "identity exhaustion", "identity overflow",
            "int overflow", "bigint", "smallint", "tinyint",
            "auto increment", "autoincrement", "seed", "increment",
            "running out of", "range", "max value", "max int",
            "2147483647", "primary key exhaust"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_IdentityColumns_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.IdentityColumnsInfo (view over dbo.IdentityColumns + dbo.IdentityColumnsHistory)",
                        Detail = "Identity columns at >50% used or in warning/critical status, with growth rate and estimated exhaustion date."
                    }
                ]
            };
        }
    }
}
