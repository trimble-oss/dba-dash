using System;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// The definition of an object the deadlock touched, as it stood when the deadlock happened.
    ///
    /// The graph names objects and modules but says nothing about their shape - which index the
    /// statement could have used, what the procedure does either side of the statement that
    /// deadlocked, whether the table is a heap.  That is most of what turns a description of a
    /// deadlock into advice about it, and a repository with schema snapshots already has it.
    /// </summary>
    public sealed class DeadlockObjectDefinition
    {
        /// <summary>Schema-qualified name, e.g. "dbo.usp_PostInvoice".</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>The database it lives in, since a deadlock can span more than one.</summary>
        public string Database { get; init; } = string.Empty;

        /// <summary>"Table", "Stored Procedure" and so on, as the repository describes it.</summary>
        public string ObjectType { get; init; } = string.Empty;

        /// <summary>
        /// When the snapshot the definition came from was taken.  Worth carrying: schema snapshots
        /// run on a schedule, so "as at the deadlock" is really "as at the last snapshot before it".
        /// </summary>
        public DateTime? AsAt { get; init; }

        public string Ddl { get; init; } = string.Empty;

        /// <summary>True when the definition was cut short to keep the request a sensible size.</summary>
        public bool Truncated { get; init; }
    }
}
