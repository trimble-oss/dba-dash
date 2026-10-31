namespace DBADashAI.Models
{
    /// <summary>An object the deadlock touched, as it was defined at the time.</summary>
    public class AiDeadlockObjectDefinition
    {
        public string Name { get; set; } = string.Empty;

        public string? Database { get; set; }

        public string? ObjectType { get; set; }

        /// <summary>
        /// When the schema snapshot the definition came from was taken.  Snapshots run on a schedule,
        /// so this is the last one before the deadlock rather than the moment of it - which matters
        /// if the model is asked whether a change caused the problem.
        /// </summary>
        public DateTime? AsAt { get; set; }

        public string? Ddl { get; set; }

        /// <summary>True when the caller cut the definition short to keep the request a sensible size.</summary>
        public bool Truncated { get; set; }
    }
}
