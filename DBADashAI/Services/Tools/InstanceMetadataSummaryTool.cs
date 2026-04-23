using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class InstanceMetadataSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public InstanceMetadataSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "instance-metadata-summary";

        public string Name => ToolName;

        public string Description => "Summarize SQL instance inventory metadata such as version, edition, CPU, and memory.";

        public string InputHint => "Use for estate inventory questions like SQL version counts, RAM thresholds, edition distribution, and host platform.";

        public string[] Keywords =>
        [
            "server", "servers", "instance", "instances", "inventory", "metadata", "version", "sql 2016", "sql 2017", "sql 2019", "sql 2022",
            "edition", "enterprise", "standard", "express", "ram", "gb", "cpu", "socket", "core", "cores", "platform",
            "how many", "list all", "what servers", "what instances", "host", "hosted", "azure sql", "managed instance",
            "sqlmi", "vm", "virtual machine", "physical", "engine edition", "product version", "hardware", "specs", "specification"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_InstanceMetadata_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);

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
}
