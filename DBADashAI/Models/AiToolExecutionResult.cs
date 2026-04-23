namespace DBADashAI.Models
{
    public class AiToolExecutionResult
    {
        public string Tool { get; set; } = string.Empty;

        public object Data { get; set; } = new();

        public List<AiEvidenceItem> Evidence { get; set; } = [];

        public int RowCount { get; set; }

        public long ExecutionMs { get; set; }
    }
}
