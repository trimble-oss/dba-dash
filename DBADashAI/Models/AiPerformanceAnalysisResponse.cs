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

        /// <summary>
        /// What the model looked up before answering, in order.  The caller puts each one's request and
        /// result into the history ahead of the answer, so a follow-up is asked of a model that still
        /// knows what it found.
        /// </summary>
        public List<AiPerformanceInvestigation> Investigations { get; set; } = new();
    }

    /// <summary>One lookup the model asked for: what it said, what it was given back, and a line for the reader.</summary>
    public class AiPerformanceInvestigation
    {
        public string Summary { get; set; } = string.Empty;

        /// <summary>The model's turn that asked for it.</summary>
        public string Request { get; set; } = string.Empty;

        /// <summary>The turn that answered it.</summary>
        public string Result { get; set; } = string.Empty;
    }
}
