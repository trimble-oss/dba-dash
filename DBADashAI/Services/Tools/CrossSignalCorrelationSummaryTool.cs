using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class CrossSignalCorrelationSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "cross-signal-correlation-summary";

    public string Name => ToolName;

    public string Description => "Correlate multiple risk signals across alerts, workload pressure, reliability, and capacity by instance.";

    public string InputHint => "Use for systemic risk, multi-signal correlation, and root-cause cluster questions.";

    public string[] Keywords => ["correlation", "cross-signal", "systemic", "cluster", "root cause", "multi signal", "overall risk", "top risks"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string alertsSql = """
            SELECT TOP 500
                i.InstanceDisplayName,
                aa.AlertKey,
                aa.Priority,
                aa.IsResolved,
                aa.UpdatedDate
            FROM Alert.ActiveAlerts aa
            INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
            WHERE i.IsActive = 1
              AND aa.UpdatedDate >= DATEADD(DAY,-7,SYSUTCDATETIME())
            ORDER BY aa.UpdatedDate DESC;
            """;

        const string waitsSql = """
            SELECT TOP 500
                i.InstanceDisplayName,
                SUM(w.wait_time_ms)/1000.0 AS TotalWaitSec
            FROM dbo.Waits_60MIN w
            INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
            INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
            WHERE w.SnapshotDate >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND wt.IsExcluded = 0
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY TotalWaitSec DESC;
            """;

        const string blockingSql = """
            SELECT TOP 500
                i.InstanceDisplayName,
                SUM(bs.BlockedWaitTime) AS BlockedWaitMs
            FROM dbo.BlockingSnapshotSummary bs
            INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
            WHERE bs.SnapshotDateUTC >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY BlockedWaitMs DESC;
            """;

        const string deadlockSql = """
            SELECT TOP 500
                i.InstanceDisplayName,
                SUM(CAST(ROUND(((pc.Value_Total / NULLIF(pc.SampleCount,0))*60.0),0) AS BIGINT)) AS DeadlockCountEstimate
            FROM dbo.PerformanceCounters_60MIN pc
            INNER JOIN dbo.Counters c ON c.CounterID = pc.CounterID
            INNER JOIN dbo.Instances i ON i.InstanceID = pc.InstanceID
            WHERE pc.SnapshotDate >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND c.counter_name = 'Number of Deadlocks/sec'
              AND c.object_name = 'Locks'
              AND c.instance_name = '_Total'
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY DeadlockCountEstimate DESC;
            """;

        const string driveSql = """
            SELECT TOP 500
                ds.InstanceDisplayName,
                ds.Status,
                ds.PctFreeSpace
            FROM dbo.DriveStatus ds
            WHERE ds.Status IN (1,2)
            ORDER BY ds.Status ASC, ds.PctFreeSpace ASC;
            """;

        var alertRows = await sql.QueryAsync(alertsSql, Math.Max(request.MaxRows, 150), cancellationToken);
        var waitsRows = await sql.QueryAsync(waitsSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var blockingRows = await sql.QueryAsync(blockingSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var deadlockRows = await sql.QueryAsync(deadlockSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var driveRows = await sql.QueryAsync(driveSql, Math.Max(request.MaxRows, 100), cancellationToken);

        var instances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddInstances(IEnumerable<Dictionary<string, object?>> rows)
        {
            foreach (var r in rows)
            {
                var i = Get(r, "InstanceDisplayName");
                if (!string.IsNullOrWhiteSpace(i)) instances.Add(i);
            }
        }

        AddInstances(alertRows);
        AddInstances(waitsRows);
        AddInstances(blockingRows);
        AddInstances(deadlockRows);
        AddInstances(driveRows);

        var correlated = instances.Select(instance =>
        {
            var unresolvedAlerts = alertRows.Count(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase)
                                                     && (Get(r, "IsResolved") == "0" || string.Equals(Get(r, "IsResolved"), "False", StringComparison.OrdinalIgnoreCase)));
            var p1Alerts = alertRows.Count(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase)
                                             && Get(r, "Priority") == "1");
            var waits = waitsRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                 .Sum(r => ToDecimal(Get(r, "TotalWaitSec")));
            var blocking = blockingRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                       .Sum(r => ToDecimal(Get(r, "BlockedWaitMs"))) / 1000m;
            var deadlocks = deadlockRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                        .Sum(r => ToDecimal(Get(r, "DeadlockCountEstimate")));
            var driveRisk = driveRows.Count(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase));

            var signalCount = 0;
            if (unresolvedAlerts > 0 || p1Alerts > 0) signalCount++;
            if (waits > 0 || blocking > 0) signalCount++;
            if (deadlocks > 0) signalCount++;
            if (driveRisk > 0) signalCount++;

            var riskScore = (p1Alerts * 5m) + (unresolvedAlerts * 2m) + waits + blocking + (deadlocks * 5m) + (driveRisk * 3m);

            return new
            {
                Instance = instance,
                SignalCount = signalCount,
                UnresolvedAlerts = unresolvedAlerts,
                Priority1Alerts = p1Alerts,
                TotalWaitSec = Math.Round(waits, 2),
                BlockedWaitSec = Math.Round(blocking, 2),
                DeadlockCount = deadlocks,
                DriveRiskCount = driveRisk,
                RiskScore = Math.Round(riskScore, 2)
            };
        })
        .OrderByDescending(x => x.SignalCount)
        .ThenByDescending(x => x.RiskScore)
        .Take(25)
        .ToList();

        return new AiToolResult
        {
            RowCount = alertRows.Count + waitsRows.Count + blockingRows.Count + deadlockRows.Count + driveRows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                correlated,
                alertRows,
                waitsRows,
                blockingRows,
                deadlockRows,
                driveRows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "ActiveAlerts + Waits + Blocking + Deadlocks + DriveStatus",
                    Detail = "Cross-signal correlation to identify systemic risk clusters by instance"
                }
            ]
        };
    }

    private static decimal ToDecimal(string value)
    {
        return decimal.TryParse(value, out var d) ? d : 0m;
    }

    private static string Get(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }
}
