using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class ConfigCurrentSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public ConfigCurrentSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "config-current-summary";

        public string Name => ToolName;

        public string Description => "Show current configuration values for key SQL Server settings across all instances. Use this to audit best practices and find misconfigurations.";

        public string InputHint => "Use for questions about current config values, best practice audits, what maxdop or max memory is set to, or cross-instance config comparison.";

        public string[] Keywords =>
        [
            "current config", "current configuration", "what is maxdop", "what is max memory", "configured",
            "best practice", "best practices", "misconfigured", "misconfiguration", "audit",
            "maxdop set to", "cost threshold set to", "max memory set to",
            "compare config", "config comparison", "config review", "config audit",
            "xp_cmdshell", "clr enabled", "ad hoc", "optimize for ad hoc",
            "backup compression", "fill factor", "blocked process threshold",
            "priority boost", "lightweight pooling", "remote admin connections"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_ConfigCurrent_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.SysConfig + dbo.SysConfigOptions + dbo.Instances",
                        Detail = "Current in-use configuration values for key settings across all active instances."
                    }
                ]
            };
        }
    }
}
