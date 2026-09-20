namespace DBADashAI.Models
{
    /// <summary>The model's reading of a query plan, with what it was asked about carried back for the caller to cache against.</summary>
    public class AiPlanAnalysisResponse
    {
        public string RequestId { get; set; } = string.Empty;

        /// <summary>Echoed so a caller can cache the answer against the query it belongs to.</summary>
        public string? Signature { get; set; }

        /// <summary>Echoed so a caller can tell an answer about this plan from one about the same query.</summary>
        public string? PlanHash { get; set; }

        public string Analysis { get; set; } = string.Empty;

        /// <summary>The model that produced it, so an answer read later can be judged in context.</summary>
        public string? Model { get; set; }

        /// <summary>When the analysis was produced.</summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The conversation this answer belongs to.  Echoed back so the caller sends it with the
        /// follow-ups, and minted here when the caller did not supply one.
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>Which answer in the conversation this is - 1 for the opening analysis.</summary>
        public int TurnNumber { get; set; }

        public long TotalExecutionMs { get; set; }
    }
}
