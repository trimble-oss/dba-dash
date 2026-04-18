using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class InstanceMetadataSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "instance-metadata-summary";

    public string Name => ToolName;

    public string Description => "Summarize SQL instance inventory metadata such as version, edition, CPU, and memory.";

    public string InputHint => "Use for estate inventory questions like SQL version counts, RAM thresholds, edition distribution, and host platform.";

    public string[] Keywords =>
    [
        "server", "servers", "instance", "instances", "inventory", "metadata", "version", "sql 2016", "sql 2017", "sql 2019", "sql 2022",
        "edition", "ram", "memory", "gb", "cpu", "socket", "core", "platform", "how many"
    ];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 1000
                I.InstanceDisplayName,
                I.ConnectionID,
                I.ProductVersion,
                I.ProductMajorVersion,
                I.Edition,
                I.EngineEdition,
                I.host_platform,
                I.cpu_count,
                I.socket_count,
                I.cores_per_socket,
                I.physical_memory_kb,
                CAST(I.physical_memory_kb / 1024.0 / 1024.0 AS DECIMAL(18,2)) AS PhysicalMemoryGB
            FROM dbo.Instances I
            WHERE I.IsActive = 1
            ORDER BY I.InstanceDisplayName;
            """;

        var rows = await sql.QueryAsync(sqlText, Math.Max(request.MaxRows, 200), cancellationToken);

        var byMajorVersion = rows
            .GroupBy(r => Get(r, "ProductMajorVersion"))
            .Select(g => new
            {
                ProductMajorVersion = string.IsNullOrWhiteSpace(g.Key) ? "Unknown" : g.Key,
                ServerCount = g.Select(x => Get(x, "ConnectionID")).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count()
            })
            .OrderBy(g => g.ProductMajorVersion)
            .ToList();

        var byEdition = rows
            .GroupBy(r => Get(r, "Edition"))
            .Select(g => new
            {
                Edition = string.IsNullOrWhiteSpace(g.Key) ? "Unknown" : g.Key,
                ServerCount = g.Select(x => Get(x, "ConnectionID")).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count()
            })
            .OrderByDescending(g => g.ServerCount)
            .ToList();

        var over16Gb = rows.Count(r => decimal.TryParse(Get(r, "PhysicalMemoryGB"), out var mem) && mem > 16m);
        var over32Gb = rows.Count(r => decimal.TryParse(Get(r, "PhysicalMemoryGB"), out var mem) && mem > 32m);
        var over64Gb = rows.Count(r => decimal.TryParse(Get(r, "PhysicalMemoryGB"), out var mem) && mem > 64m);

        return new AiToolResult
        {
            RowCount = rows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                summary = new
                {
                    totalActiveInstances = rows.Select(x => Get(x, "ConnectionID")).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count(),
                    serversWithMemoryOver16Gb = over16Gb,
                    serversWithMemoryOver32Gb = over32Gb,
                    serversWithMemoryOver64Gb = over64Gb
                },
                byMajorVersion,
                byEdition,
                rows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "dbo.Instances",
                    Detail = "Active instance inventory including ProductMajorVersion, Edition, CPU and physical memory metadata"
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
