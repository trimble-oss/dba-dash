using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class CapacityForecastSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public CapacityForecastSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "capacity-forecast-summary";

        public string Name => ToolName;

        public string Description => "Summarize capacity risk signals for drives, memory, and backup growth pressure.";

        public string InputHint => "Use for capacity runway, free space pressure, and growth risk questions.";

        public string[] Keywords => ["capacity", "runway", "growth", "growing", "free space", "disk", "drive", "memory", "forecast", "fill up", "out of space", "running out", "trend", "projection", "predict", "memory pressure", "memory usage", "paging", "low memory", "high memory", "percent free", "space left", "days remaining", "when will", "storage", "volume"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_CapacityForecast_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            var driveRows = results.Count > 0 ? results[0] : new List<Dictionary<string, object?>>();
            var memoryRows = results.Count > 1 ? results[1] : new List<Dictionary<string, object?>>();

            var criticalDrives = driveRows.Count(r => Get(r, "Status") == "1");
            var warningDrives = driveRows.Count(r => Get(r, "Status") == "2");
            var lowFreeInstances = driveRows
                .Where(r => decimal.TryParse(Get(r, "PctFreeSpace"), out var pct) && pct < 15m)
                .Select(r => Get(r, "InstanceDisplayName"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var highMemoryPressure = memoryRows
                .Where(r => decimal.TryParse(Get(r, "MemoryUtilizationPercent"), out var pct) && pct >= 90m)
                .Select(r => Get(r, "InstanceDisplayName"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return new AiToolResult
            {
                RowCount = driveRows.Count + memoryRows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    summary = new
                    {
                        criticalDrives,
                        warningDrives,
                        instancesWithLowFreeSpace = lowFreeInstances,
                        instancesWithHighMemoryPressure = highMemoryPressure
                    },
                    driveRows,
                    memoryRows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.DriveStatus + dbo.MemoryUsage + dbo.Instances",
                        Detail = "Capacity risk signals from storage and memory utilization snapshots"
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
