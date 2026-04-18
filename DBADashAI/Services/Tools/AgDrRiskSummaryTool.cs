using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class AgDrRiskSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "ag-dr-risk-summary";

    public string Name => ToolName;

    public string Description => "Summarize Availability Group and backup/DR risks for active instances.";

    public string InputHint => "Use for AG health, failover stability, replica risk, and RPO/backup risk questions.";

    public string[] Keywords => ["ag", "availability group", "replica", "failover", "dr", "rpo", "backup risk", "hadr"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string agSql = """
            SELECT TOP 300
                i.InstanceDisplayName,
                aa.AlertKey,
                aa.LastMessage,
                aa.IsResolved,
                aa.IsAcknowledged,
                aa.TriggerDate,
                aa.UpdatedDate
            FROM Alert.ActiveAlerts aa
            INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
            WHERE i.IsActive = 1
              AND (
                    aa.AlertKey LIKE '%AG%'
                 OR aa.AlertType LIKE '%Availability%'
                 OR aa.AlertType LIKE '%HADR%'
                  )
            ORDER BY aa.IsResolved ASC, aa.Priority ASC, aa.UpdatedDate DESC;
            """;

        const string backupSql = """
            SELECT TOP 300
                bs.InstanceDisplayName,
                bs.name AS DatabaseName,
                bs.BackupStatus,
                bs.FullBackupStatus,
                bs.LogBackupStatus,
                bs.LastFull,
                bs.LastLog,
                bs.SnapshotAge
            FROM dbo.BackupStatus bs
            WHERE bs.BackupStatus IN (1,2)
            ORDER BY bs.BackupStatus ASC, bs.SnapshotAge DESC;
            """;

        var agRows = await sql.QueryAsync(agSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var backupRows = await sql.QueryAsync(backupSql, Math.Max(request.MaxRows, 100), cancellationToken);

        var agByInstance = agRows
            .GroupBy(r => Get(r, "InstanceDisplayName"))
            .Select(g => new
            {
                Instance = g.Key,
                AlertCount = g.Count(),
                ActiveUnresolved = g.Count(x => string.Equals(Get(x, "IsResolved"), "False", StringComparison.OrdinalIgnoreCase) || Get(x, "IsResolved") == "0"),
                RecentMessage = g.Select(x => Get(x, "LastMessage")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty
            })
            .OrderByDescending(x => x.ActiveUnresolved)
            .ThenByDescending(x => x.AlertCount)
            .ToList();

        var backupByInstance = backupRows
            .GroupBy(r => Get(r, "InstanceDisplayName"))
            .Select(g => new
            {
                Instance = g.Key,
                BackupRiskCount = g.Count(),
                CriticalCount = g.Count(x => Get(x, "BackupStatus") == "1"),
                WarningCount = g.Count(x => Get(x, "BackupStatus") == "2")
            })
            .OrderByDescending(x => x.CriticalCount)
            .ThenByDescending(x => x.BackupRiskCount)
            .ToList();

        return new AiToolResult
        {
            RowCount = agRows.Count + backupRows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                summary = new
                {
                    agAlerts = agRows.Count,
                    backupRiskRows = backupRows.Count,
                    instancesWithAgAlerts = agByInstance.Count,
                    instancesWithBackupRisk = backupByInstance.Count
                },
                agByInstance,
                backupByInstance,
                agRows,
                backupRows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "Alert.ActiveAlerts (AG-related) + dbo.BackupStatus",
                    Detail = "Combines AG health/failover signals with backup risk indicators for DR posture"
                }
            ]
        };
    }

    private static string Get(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }
}
