namespace DBADashAI.Models
{
    /// <summary>
    /// A deadlock handed over for analysis.  Unlike <see cref="AiAskRequest"/> this is not a question
    /// answered from the repository - the caller already holds the artifact and everything worth
    /// knowing about it, so the service's job is to reason over what it is given.
    ///
    /// The shape matches what the viewer shows the user before they submit: the two must stay the
    /// same, or the preview stops being a preview.
    /// </summary>
    public class AiDeadlockAnalysisRequest
    {
        /// <summary>A deadlock graph is a few KB; a runaway one is a bug, not a big deadlock.</summary>
        public const int MaxGraphXmlLength = 256 * 1024;

        /// <summary>The deadlock graph XML.</summary>
        public string GraphXml { get; set; } = string.Empty;

        /// <summary>Groups occurrences of one deadlock, and is what a cached answer is keyed on.</summary>
        public string? Signature { get; set; }

        public string? Instance { get; set; }

        /// <summary>One line per participant, as the viewer summarises them.</summary>
        public List<string> Participants { get; set; } = new();

        public List<string> Objects { get; set; } = new();

        public List<string> Modules { get; set; } = new();

        /// <summary>
        /// What the caller's own rules already established.  Passed on so the model builds on the
        /// findings the user can already see rather than rediscovering or contradicting them.
        /// </summary>
        public List<string> Findings { get; set; } = new();

        /// <summary>
        /// Definitions of the objects involved, as they were when the deadlock happened.  Optional:
        /// they come from schema snapshots, which are not always enabled, and the caller can choose
        /// not to send them.
        /// </summary>
        public List<AiDeadlockObjectDefinition> Schema { get; set; } = new();

        /// <summary>
        /// What kind of request this is - which is to say, what the model was given to reason from.
        /// Recorded with the analysis so a reader can tell whether an older answer was produced
        /// without the object definitions that are sent now.
        /// </summary>
        public string? PayloadVersion { get; set; }

        /// <summary>Recorded with the analysis, so it is possible to see where one came from.</summary>
        public int? InstanceId { get; set; }

        /// <summary>Override the configured AI model for this request.  Null = use configured default.</summary>
        public string? ModelOverride { get; set; }

        /// <summary>
        /// A ceiling on the schema regardless of what the caller sends.  The caller already caps each
        /// definition, but a deadlock across a dozen wide tables would still add up to more context
        /// than the answer improves by.
        /// </summary>
        public const int MaxSchemaLength = 64 * 1024;

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(GraphXml))
            {
                return "GraphXml is required.";
            }

            if (GraphXml.Length > MaxGraphXmlLength)
            {
                return $"GraphXml exceeds {MaxGraphXmlLength} characters.";
            }

            return Schema.Sum(s => s.Ddl?.Length ?? 0) > MaxSchemaLength
                ? $"Schema exceeds {MaxSchemaLength} characters."
                : null;
        }
    }
}
