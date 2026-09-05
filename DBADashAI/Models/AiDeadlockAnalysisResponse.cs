namespace DBADashAI.Models
{
    /// <summary>The model's reading of a deadlock, with what it was asked about carried back for the caller to cache against.</summary>
    public class AiDeadlockAnalysisResponse
    {
        public string RequestId { get; set; } = string.Empty;

        /// <summary>Echoed so a caller can cache the answer against the pattern it belongs to.</summary>
        public string? Signature { get; set; }

        public string Analysis { get; set; } = string.Empty;

        /// <summary>The model that produced it, so an answer read later can be judged in context.</summary>
        public string? Model { get; set; }

        /// <summary>When the analysis was produced.</summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        public long TotalExecutionMs { get; set; }
    }
}
