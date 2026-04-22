using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class OperationalHygieneSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public OperationalHygieneSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "operational-hygiene-summary";

        public string Name => ToolName;

        public string Description => "Summarize alert hygiene debt such as resolved-unacknowledged and aging alert backlog.";

        public string InputHint => "Use for monitoring process hygiene, acknowledgment debt, and stale alert workflow questions.";

        public string[] Keywords => ["hygiene", "acknowledge", "acknowledgment", "acknowledged", "unacknowledged", "stale alert", "stale", "backlog", "resolved unacknowledged", "alert debt", "cleanup", "clean up", "housekeeping", "noise", "noisy", "ignored", "old alert", "overdue alert", "open alert", "pending", "unresolved"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_OperationalHygiene_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);

            var resolvedUnacked = rows
                .Where(r => (Get(r, "IsResolved") == "1" || string.Equals(Get(r, "IsResolved"), "True", StringComparison.OrdinalIgnoreCase))
                            && (Get(r, "IsAcknowledged") == "0" || string.Equals(Get(r, "IsAcknowledged"), "False", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var unresolved = rows
                .Where(r => Get(r, "IsResolved") == "0" || string.Equals(Get(r, "IsResolved"), "False", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var byInstance = rows
                .GroupBy(r => Get(r, "InstanceDisplayName"))
                .Select(g => new
                {
                    Instance = g.Key,
                    ResolvedUnacknowledged = g.Count(x => (Get(x, "IsResolved") == "1" || string.Equals(Get(x, "IsResolved"), "True", StringComparison.OrdinalIgnoreCase))
                                                          && (Get(x, "IsAcknowledged") == "0" || string.Equals(Get(x, "IsAcknowledged"), "False", StringComparison.OrdinalIgnoreCase))),
                    UnresolvedCount = g.Count(x => Get(x, "IsResolved") == "0" || string.Equals(Get(x, "IsResolved"), "False", StringComparison.OrdinalIgnoreCase)),
                    TotalRecentAlerts = g.Count()
                })
                .OrderByDescending(x => x.ResolvedUnacknowledged)
                .ThenByDescending(x => x.UnresolvedCount)
                .ToList();

            return new AiToolResult
            {
                RowCount = rows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    summary = new
                    {
                        totalRecentAlerts = rows.Count,
                        resolvedUnacknowledgedCount = resolvedUnacked.Count,
                        unresolvedCount = unresolved.Count
                    },
                    byInstance,
                    resolvedUnacknowledgedRows = resolvedUnacked,
                    unresolvedRows = unresolved,
                    rows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "Alert.ActiveAlerts + dbo.Instances",
                        Detail = "Operational hygiene indicators over last 30 days (resolved-unacknowledged backlog and unresolved debt)"
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
