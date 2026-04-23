using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class AgDrRiskSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public AgDrRiskSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "ag-dr-risk-summary";

        public string Name => ToolName;

        public string Description => "Summarize Availability Group replica status, database mirroring, and backup/DR risks for active instances.";

        public string InputHint => "Use for AG health, failover stability, replica risk, RPO/backup risk, mirroring status, send queue, redo queue, and secondary lag questions.";

        public string[] Keywords => ["ag", "availability group", "availability groups", "replica", "replicas", "failover", "fail over", "dr", "disaster recovery", "rpo", "rto", "backup risk", "hadr", "synchronous", "asynchronous", "sync state", "secondary", "primary", "listener", "always on", "alwayson", "lag", "redo", "send queue", "log send", "mirroring", "mirror", "witness", "partner"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_AgDrRisk_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            var agAlertRows = results.ElementAtOrDefault(0) ?? [];
            var hadrRows = results.ElementAtOrDefault(1) ?? [];
            var mirroringRows = results.ElementAtOrDefault(2) ?? [];
            var backupRows = results.ElementAtOrDefault(3) ?? [];

            var agByInstance = agAlertRows
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
                RowCount = agAlertRows.Count + hadrRows.Count + mirroringRows.Count + backupRows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    summary = new
                    {
                        agAlerts = agAlertRows.Count,
                        hadrReplicaRows = hadrRows.Count,
                        mirroringRows = mirroringRows.Count,
                        backupRiskRows = backupRows.Count,
                        instancesWithAgAlerts = agByInstance.Count,
                        instancesWithBackupRisk = backupByInstance.Count
                    },
                    agByInstance,
                    backupByInstance,
                    agAlertRows,
                    hadrRows,
                    mirroringRows,
                    backupRows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "Alert.ActiveAlerts (AG) + dbo.DatabasesHADR + dbo.AvailabilityReplicas + dbo.AvailabilityGroups + dbo.DatabaseMirroring + dbo.BackupStatus",
                        Detail = "AG alerts, replica sync state/queue metrics, database mirroring status, and backup risk indicators for full DR posture assessment."
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
}
