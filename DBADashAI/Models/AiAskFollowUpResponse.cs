namespace DBADashAI.Models
{
    public class AiAskFollowUpResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public long TotalExecutionMs { get; set; }
    }
}
