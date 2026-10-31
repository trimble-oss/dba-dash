namespace DBADashAI.Models
{
    /// <summary>
    /// One turn of a conversation about an artifact the caller holds - a deadlock graph, a query plan.
    ///
    /// The service keeps no conversation state.  A follow-up arrives carrying everything said so far,
    /// which is what makes a viewer able to reopen a conversation stored months ago and add to it, and
    /// what keeps the "you can see exactly what is sent" promise the artifact viewers make: the
    /// transcript on screen is the transcript on the wire.
    /// </summary>
    public class AiConversationTurn
    {
        /// <summary>"user" or "assistant".  Anything else is rejected - it goes straight to a provider.</summary>
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public const string User = "user";

        public const string Assistant = "assistant";

        public bool IsUser => string.Equals(Role, User, StringComparison.OrdinalIgnoreCase);

        public bool IsAssistant => string.Equals(Role, Assistant, StringComparison.OrdinalIgnoreCase);
    }
}
