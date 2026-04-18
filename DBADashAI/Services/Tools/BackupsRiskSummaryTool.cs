using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class BackupsRiskSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "backups-risk-summary";

    public string Name => ToolName;

    public string Description => "Summarize backup health risks by instance and database.";

    public string InputHint => "Use for questions about backup failures, missing backups, RPO/RTO, and recoverability risk.";

    public string[] Keywords => ["backup", "backups", "restore", "rpo", "rto", "recoverability"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                bs.InstanceDisplayName,
                bs.name AS DatabaseName,
                bs.BackupStatus,
                bs.FullBackupStatus,
                bs.DiffBackupStatus,
                bs.LogBackupStatus,
                bs.LastFull,
                bs.LastDiff,
                bs.LastLog,
                bs.SnapshotAge
            FROM dbo.BackupStatus bs
            WHERE bs.BackupStatus IN (1,2)
            ORDER BY bs.BackupStatus ASC, bs.SnapshotAge DESC;
            """;

        var rows = await sql.QueryAsync(sqlText, request.MaxRows, cancellationToken);
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
