namespace DBADashAI.Models
{
    /// <summary>The model's reading of an instance's performance over a window.</summary>
    public class AiPerformanceAnalysisResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public string Analysis { get; set; } = string.Empty;

        /// <summary>The model that produced it, so an answer can be judged in context.</summary>
        public string? Model { get; set; }

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Echoed back so the caller sends it with the follow-ups, and minted here when the caller had none.</summary>
        public Guid ConversationId { get; set; }

        /// <summary>Which answer in the conversation this is - 1 for the opening analysis.</summary>
        public int TurnNumber { get; set; }

        public long TotalExecutionMs { get; set; }
    }
}
