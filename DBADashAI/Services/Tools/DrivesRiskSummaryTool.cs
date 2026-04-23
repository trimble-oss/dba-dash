using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class DrivesRiskSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public DrivesRiskSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "drives-risk-summary";

        public string Name => ToolName;

        public string Description => "Summarize drive capacity risks across monitored instances.";

        public string InputHint => "Use for questions about storage pressure, free space risk, and drive growth concerns.";

        public string[] Keywords => ["drive", "drives", "disk", "disks", "storage", "free space", "capacity", "volume", "mount", "full", "filling", "space", "gb free", "percent free", "low disk", "disk space", "c drive", "d drive", "e drive", "data drive", "log drive", "tempdb drive"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("DBADash.AI_DrivesRisk_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = rows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    rows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.DriveStatus",
                        Detail = "Rows where drive status is Critical(1) or Warning(2)."
                    }
                ]
            };
        }
    }
}
