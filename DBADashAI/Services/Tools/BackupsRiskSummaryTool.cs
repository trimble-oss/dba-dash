using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class BackupsRiskSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public BackupsRiskSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "backups-risk-summary";

        public string Name => ToolName;

        public string Description => "Summarize backup health risks by instance and database.";

        public string InputHint => "Use for questions about backup failures, missing backups, RPO/RTO, and recoverability risk.";

        public string[] Keywords => ["backup", "backups", "restore", "rpo", "rto", "recoverability", "full backup", "log backup", "diff backup", "differential", "transaction log", "last backup", "backup status", "backup age", "no backup", "missing backup", "overdue", "recovery point", "backup risk", "data loss", "snapshot age"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_BackupsRisk_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.BackupStatus",
                        Detail = "Rows where backup status is Critical(1) or Warning(2)."
                    }
                ]
            };
        }
    }
}
