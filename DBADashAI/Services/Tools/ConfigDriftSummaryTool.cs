using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class ConfigDriftSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public ConfigDriftSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "config-drift-summary";

        public string Name => ToolName;

        public string Description => "Summarize recent instance/database configuration drift.";

        public string InputHint => "Use for questions about configuration changes, drift, and change-correlation with incidents.";

        public string[] Keywords => ["config", "configuration", "drift", "changed", "change", "trace flag", "option", "setting", "settings", "sp_configure", "sys config", "maxdop", "max degree", "cost threshold", "max memory", "min memory", "who changed", "what changed", "recent changes", "modification"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_ConfigDrift_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.SysConfigHistory + dbo.SysConfigOptions + dbo.Instances",
                        Detail = "Instance-level configuration changes in last 7 days."
                    }
                ]
            };
        }
    }
}
