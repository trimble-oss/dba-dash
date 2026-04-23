using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class WorkloadPressureSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public WorkloadPressureSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "workload-pressure-summary";

        public string Name => ToolName;

        public string Description => "Correlate waits, blocking, deadlocks, and slow query pressure by instance.";

        public string InputHint => "Use for workload pressure, lock contention, query regressions, and broad performance triage.";

        public string[] Keywords => ["pressure", "contention", "blocking", "wait", "deadlock", "slow query", "regression", "bottleneck", "triage", "workload", "load", "stress", "under pressure", "overwhelmed", "saturated", "struggling", "degraded", "degradation", "throughput", "latency", "response time", "what's happening", "whats happening", "first thing to check", "where to start"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_WorkloadPressure_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            var waitsRows = results.Count > 0 ? results[0] : new List<Dictionary<string, object?>>();
            var blockingRows = results.Count > 1 ? results[1] : new List<Dictionary<string, object?>>();
            var deadlockRows = results.Count > 2 ? results[2] : new List<Dictionary<string, object?>>();
            var slowRows = results.Count > 3 ? results[3] : new List<Dictionary<string, object?>>();

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
}
