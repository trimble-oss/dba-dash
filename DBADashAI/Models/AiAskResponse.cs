namespace DBADashAI.Models
{
    public class AiAskResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public string Tool { get; set; } = string.Empty;

        public object Data { get; set; } = new();

        public string? Summary { get; set; }

        public List<AiEvidenceItem> Evidence { get; set; } = [];

        public long ToolExecutionMs { get; set; }

        public long TotalExecutionMs { get; set; }

        public double ConfidenceScore { get; set; }

        public string ConfidenceLabel { get; set; } = "Low";
    }
}
