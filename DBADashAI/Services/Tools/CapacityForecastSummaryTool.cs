using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class CapacityForecastSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "capacity-forecast-summary";

    public string Name => ToolName;

    public string Description => "Summarize capacity risk signals for drives, memory, and backup growth pressure.";

    public string InputHint => "Use for capacity runway, free space pressure, and growth risk questions.";

    public string[] Keywords => ["capacity", "runway", "growth", "free space", "disk", "drive", "memory", "forecast", "fill up"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string driveSql = """
            SELECT TOP 300
                ds.InstanceDisplayName,
                ds.Name AS DriveName,
                ds.TotalGB,
                ds.FreeGB,
                ds.PctFreeSpace,
                ds.Status,
                ds.SnapshotAgeMins
            FROM dbo.DriveStatus ds
            WHERE ds.Status IN (1,2)
            ORDER BY ds.Status ASC, ds.PctFreeSpace ASC;
            """;

        const string memorySql = """
            SELECT TOP 300
                i.InstanceDisplayName,
                CAST(i.physical_memory_kb/1024.0/1024.0 AS DECIMAL(18,2)) AS PhysicalMemoryGB,
                mu.TotalServerMemoryGB,
                mu.TargetServerMemoryGB,
                mu.MemoryUtilizationPercent,
                mu.SnapshotDate
            FROM dbo.MemoryUsage mu
            INNER JOIN dbo.Instances i ON i.InstanceID = mu.InstanceID
            WHERE i.IsActive = 1
              AND mu.SnapshotDate >= DATEADD(HOUR,-24,SYSUTCDATETIME())
            ORDER BY mu.SnapshotDate DESC;
            """;

        var driveRows = await sql.QueryAsync(driveSql, Math.Max(request.MaxRows, 100), cancellationToken);
        var memoryRows = await sql.QueryAsync(memorySql, Math.Max(request.MaxRows, 100), cancellationToken);

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
