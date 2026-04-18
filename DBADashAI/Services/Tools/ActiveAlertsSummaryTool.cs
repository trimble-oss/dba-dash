using DBADashAI.Models;

namespace DBADashAI.Services.Tools;

public sealed class ActiveAlertsSummaryTool(SqlToolExecutor sql) : IAiTool
{
    public const string ToolName = "active-alerts-summary";

    public string Name => ToolName;

    public string Description => "Summarize active alerts by status and priority.";

    public string InputHint => "Use for questions about active alerts, priorities, incidents, and alert status.";

    public string[] Keywords => ["alert", "incident", "priority", "active alert"];

    public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
    {
        const string sqlText = """
            SELECT TOP 200
                AA.AlertType,
                AA.IsResolved,
                AA.IsAcknowledged,
                AA.Priority,
                I.InstanceDisplayName,
                AA.AlertKey,
                AA.LastMessage,
                AA.TriggerDate,
                AA.UpdatedDate
            FROM Alert.ActiveAlerts AA
            JOIN dbo.Instances I ON I.InstanceID = AA.InstanceID
            WHERE AA.IsBlackout = 0
            ORDER BY AA.IsResolved,
                     AA.IsAcknowledged,
                     AA.Priority ASC,
                     I.InstanceDisplayName,
                     AA.TriggerDate DESC;
            """;

        var rows = await sql.QueryAsync(sqlText, request.MaxRows, cancellationToken);

        var grouped = rows
            .GroupBy(r => new
            {
                AlertType = Get(r, "AlertType"),
                IsResolved = Get(r, "IsResolved"),
                IsAcknowledged = Get(r, "IsAcknowledged"),
                Priority = Get(r, "Priority")
            })
            .Select(g => new
            {
                g.Key.AlertType,
                g.Key.IsResolved,
                g.Key.IsAcknowledged,
                g.Key.Priority,
                AlertCount = g.Count(),
                Instances = g.Select(x => Get(x, "InstanceDisplayName")).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(10).ToList()
            })
            .OrderBy(g => int.TryParse(g.Priority, out var p) ? p : 999)
            .ThenByDescending(g => g.AlertCount)
            .ToList();

        return new AiToolResult
        {
            RowCount = rows.Count,
            Data = new
            {
                generatedUtc = DateTime.UtcNow,
                grouped,
                rows
            },
            Evidence =
            [
                new AiEvidenceItem
                {
                    Source = "Alert.ActiveAlerts + dbo.Instances",
                    Detail = "Includes InstanceDisplayName, AlertKey, LastMessage, and trigger/update timestamps"
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
