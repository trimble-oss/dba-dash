using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class DbConfigSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public DbConfigSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "db-config-summary";

        public string Name => ToolName;

        public string Description => "Show database-level configuration: scoped configs that differ from defaults, recent scoped config changes, and recent database option changes (recovery model, compatibility level, auto_shrink, etc.).";

        public string InputHint => "Use for questions about database-level settings, recovery model changes, compatibility level, auto_shrink, parameter sniffing, cardinality estimation, or database option changes.";

        public string[] Keywords =>
        [
            "database config", "database configuration", "database setting", "database option",
            "recovery model", "compat", "compatibility level", "compatibility_level",
            "auto_shrink", "auto shrink", "auto_close", "auto close",
            "parameter sniffing", "cardinality estimation", "legacy cardinality",
            "query_optimizer_hotfixes", "query optimizer hotfixes",
            "db option", "db config", "db setting", "database level",
            "scoped config", "scoped configuration", "database scoped",
            "page verify", "torn_page", "checksum",
            "trustworthy", "is_trustworthy",
            "read committed snapshot", "rcsi", "snapshot isolation",
            "ansi", "quoted identifier", "arithabort"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_DBConfigCurrent_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    nonDefaultScopedConfigs = results.ElementAtOrDefault(0) ?? [],
                    recentScopedConfigChanges = results.ElementAtOrDefault(1) ?? [],
                    recentDbOptionChanges = results.ElementAtOrDefault(2) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.DBConfig + dbo.DBConfigHistory + dbo.DBOptionsHistory",
                        Detail = "Database-scoped configs differing from defaults, recent scoped config changes, and recent database option changes."
                    }
                ]
            };
        }
    }
}
