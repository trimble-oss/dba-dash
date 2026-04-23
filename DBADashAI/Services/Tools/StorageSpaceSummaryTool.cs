using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class StorageSpaceSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public StorageSpaceSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "storage-space-summary";

        public string Name => ToolName;

        public string Description => "Show drive space with growth trends, database file space allocation/usage, and TempDB configuration audit.";

        public string InputHint => "Use for questions about drive space, disk growth, database file sizes, log file usage, autogrowth settings, or TempDB configuration.";

        public string[] Keywords =>
        [
            "storage", "drive space", "disk space", "free space", "drive growth", "disk growth",
            "database size", "database space", "file size", "file space", "data file", "log file",
            "autogrow", "auto grow", "auto-grow", "growth rate", "days until full",
            "tempdb", "temp db", "tempdb config", "tempdb files",
            "allocated", "used space", "log usage", "log full",
            "max size", "unlimited growth", "percent growth"
        ];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_StorageSpace_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    driveGrowthTrends = results.ElementAtOrDefault(0) ?? [],
                    databaseFileSpace = results.ElementAtOrDefault(1) ?? [],
                    tempDBConfig = results.ElementAtOrDefault(2) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.Drives + dbo.DriveSnapshot + dbo.DBFiles + dbo.Databases + dbo.TempDBConfiguration",
                        Detail = "Drive growth trends with estimated days until full, database file space allocation/usage, and TempDB configuration audit."
                    }
                ]
            };
        }
    }
}
