using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class ReliabilityRiskSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public ReliabilityRiskSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "reliability-risk-summary";

        public string Name => ToolName;

        public string Description => "Summarize reliability risk from restarts, offline events, and job failures.";

        public string InputHint => "Use for stability and reliability questions, recurring incidents, and service interruptions.";

        public string[] Keywords => ["reliability", "unreliable", "unstable", "restart", "restarted", "reboot", "rebooted", "offline", "down", "went down", "availability", "job failure", "incident", "recurring", "crash", "crashed", "unexpected", "unplanned", "outage", "uptime", "downtime", "stability", "flapping", "intermittent", "keeps failing"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_ReliabilityRisk_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);

            var byInstance = rows
                .GroupBy(r => Get(r, "InstanceDisplayName"))
                .Select(g => new
                {
                    Instance = g.Key,
                    IncidentCount = g.Count(),
                    UnresolvedCount = g.Count(x => Get(x, "IsResolved") == "0" || string.Equals(Get(x, "IsResolved"), "False", StringComparison.OrdinalIgnoreCase)),
                    RestartCount = g.Count(x => Get(x, "AlertKey").Contains("RESTART", StringComparison.OrdinalIgnoreCase)),
                    OfflineCount = g.Count(x => Get(x, "AlertKey").Contains("OFFLINE", StringComparison.OrdinalIgnoreCase)),
                    JobFailureCount = g.Count(x => Get(x, "AlertType").Contains("AgentJob", StringComparison.OrdinalIgnoreCase)
                                               || Get(x, "AlertKey").Contains("Job", StringComparison.OrdinalIgnoreCase))
                })
                .Select(x => new
                {
                    x.Instance,
                    x.IncidentCount,
                    x.UnresolvedCount,
                    x.RestartCount,
                    x.OfflineCount,
                    x.JobFailureCount,
                    ReliabilityRiskScore = (x.UnresolvedCount * 3) + (x.OfflineCount * 3) + (x.RestartCount * 2) + x.JobFailureCount
                })
                .OrderByDescending(x => x.ReliabilityRiskScore)
                .ThenByDescending(x => x.IncidentCount)
                .ToList();

            return new AiToolResult
            {
                RowCount = rows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    byInstance,
                    rows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "Alert.ActiveAlerts + dbo.Instances",
                        Detail = "Reliability signals for restart/offline/job-failure patterns over the last 7 days"
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
