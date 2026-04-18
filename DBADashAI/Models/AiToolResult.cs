namespace DBADashAI.Models;

public sealed class AiToolResult
{
    public object Data { get; set; } = new();

    public List<AiEvidenceItem> Evidence { get; set; } = [];

    public int RowCount { get; set; }
}
