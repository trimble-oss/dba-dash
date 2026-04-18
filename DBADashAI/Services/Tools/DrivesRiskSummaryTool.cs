using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class DrivesRiskSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "drives-risk-summary";

    public string Name => ToolName;

    public string Description => "Summarize drive capacity risks across monitored instances.";

    public string InputHint => "Use for questions about storage pressure, free space risk, and drive growth concerns.";

    public string[] Keywords => ["drive", "disk", "storage", "free space", "capacity"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                ds.InstanceDisplayName,
                ds.Name AS DriveName,
                ds.Label,
                ds.TotalGB,
                ds.FreeGB,
                ds.PctFreeSpace,
                ds.Status,
                ds.SnapshotAgeMins
            FROM dbo.DriveStatus ds
            WHERE ds.Status IN (1,2)
            ORDER BY ds.Status ASC, ds.PctFreeSpace ASC;
            """;

        var rows = await sql.QueryAsync(sqlText, request.MaxRows, cancellationToken);
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
