using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class CrossSignalCorrelationSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public CrossSignalCorrelationSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "cross-signal-correlation-summary";

        public string Name => ToolName;

        public string Description => "Correlate multiple risk signals across alerts, workload pressure, reliability, and capacity by instance.";

        public string InputHint => "Use for systemic risk, multi-signal correlation, and root-cause cluster questions.";

        public string[] Keywords => ["correlation", "cross-signal", "systemic", "cluster", "root cause", "multi signal", "overall risk", "top risks", "everything", "all signals", "big picture", "estate", "fleet", "across all", "worst", "most affected", "highest risk", "health check", "health score", "risk score", "combined", "pattern", "patterns", "what stands out", "overview"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_CrossSignalCorrelation_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            var alertRows = results.Count > 0 ? results[0] : new List<Dictionary<string, object?>>();
            var waitsRows = results.Count > 1 ? results[1] : new List<Dictionary<string, object?>>();
            var blockingRows = results.Count > 2 ? results[2] : new List<Dictionary<string, object?>>();
            var deadlockRows = results.Count > 3 ? results[3] : new List<Dictionary<string, object?>>();
            var driveRows = results.Count > 4 ? results[4] : new List<Dictionary<string, object?>>();

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
}
