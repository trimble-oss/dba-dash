using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class WorkloadPressureSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "workload-pressure-summary";

    public string Name => ToolName;

    public string Description => "Correlate waits, blocking, deadlocks, and slow query pressure by instance.";

    public string InputHint => "Use for workload pressure, lock contention, query regressions, and broad performance triage.";

    public string[] Keywords => ["pressure", "contention", "blocking", "wait", "deadlock", "slow query", "regression", "bottleneck", "triage"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string waitsSql = """
            SELECT TOP 300 i.InstanceDisplayName, wt.WaitType,
                   SUM(w.wait_time_ms)/1000.0 AS TotalWaitSec
            FROM dbo.Waits_60MIN w
            INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
            INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
            WHERE w.SnapshotDate >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND wt.IsExcluded = 0
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName, wt.WaitType
            ORDER BY TotalWaitSec DESC;
            """;

        const string blockingSql = """
            SELECT TOP 300 i.InstanceDisplayName,
                   SUM(bs.BlockedSessionCount) AS BlockedSessions,
                   SUM(bs.BlockedWaitTime) AS BlockedWaitMs
            FROM dbo.BlockingSnapshotSummary bs
            INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
            WHERE bs.SnapshotDateUTC >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY BlockedWaitMs DESC;
            """;

        const string deadlockSql = """
            SELECT TOP 300 i.InstanceDisplayName,
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

        const string slowSql = """
            SELECT TOP 300 i.InstanceDisplayName,
                   SUM(sq.Duration)/1000000.0 AS TotalDurationSec,
                   SUM(sq.cpu_time)/1000000.0 AS TotalCpuSec,
                   COUNT_BIG(*) AS SlowQueryExecCount
            FROM dbo.SlowQueries sq
            INNER JOIN dbo.Instances i ON i.InstanceID = sq.InstanceID
            WHERE sq.timestamp >= DATEADD(HOUR,-24,SYSUTCDATETIME())
              AND i.IsActive = 1
            GROUP BY i.InstanceDisplayName
            ORDER BY TotalDurationSec DESC;
            """;

        var waitsRows = await sql.QueryAsync(waitsSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var blockingRows = await sql.QueryAsync(blockingSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var deadlockRows = await sql.QueryAsync(deadlockSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var slowRows = await sql.QueryAsync(slowSql, Math.Max(request.MaxRows, 100), cancellationToken);

        var instanceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddInstances(IEnumerable<Dictionary<string, object?>> rows)
        {
            foreach (var r in rows)
            {
                var inst = Get(r, "InstanceDisplayName");
                if (!string.IsNullOrWhiteSpace(inst)) instanceSet.Add(inst);
            }
        }

        AddInstances(waitsRows);
        AddInstances(blockingRows);
        AddInstances(deadlockRows);
        AddInstances(slowRows);

        var pressureByInstance = instanceSet.Select(instance => new
        {
            Instance = instance,
            WaitSec = waitsRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                              .Sum(r => ToDecimal(Get(r, "TotalWaitSec"))),
            BlockedWaitMs = blockingRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                        .Sum(r => ToDecimal(Get(r, "BlockedWaitMs"))),
            DeadlockCount = deadlockRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                        .Sum(r => ToDecimal(Get(r, "DeadlockCountEstimate"))),
            SlowDurationSec = slowRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                      .Sum(r => ToDecimal(Get(r, "TotalDurationSec"))),
            SlowCpuSec = slowRows.Where(r => string.Equals(Get(r, "InstanceDisplayName"), instance, StringComparison.OrdinalIgnoreCase))
                                 .Sum(r => ToDecimal(Get(r, "TotalCpuSec")))
        })
        .Select(x => new
        {
            x.Instance,
            x.WaitSec,
            x.BlockedWaitMs,
            x.DeadlockCount,
            x.SlowDurationSec,
            x.SlowCpuSec,
            PressureScore = Math.Round(x.WaitSec + (x.BlockedWaitMs / 1000.0m) + (x.DeadlockCount * 5m) + x.SlowDurationSec + x.SlowCpuSec, 2)
        })
        .OrderByDescending(x => x.PressureScore)
        .Take(25)
        .ToList();

        return new AiToolResult
        {
            RowCount = waitsRows.Count + blockingRows.Count + deadlockRows.Count + slowRows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                pressureByInstance,
                waitsRows,
                blockingRows,
                deadlockRows,
                slowRows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "Waits + BlockingSnapshotSummary + PerformanceCounters_60MIN + SlowQueries",
                    Detail = "Cross-signal workload pressure correlation over the last 24 hours"
                }
            ]
        };
    }

    private static decimal ToDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    private static string Get(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }
}
