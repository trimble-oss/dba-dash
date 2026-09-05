namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// A process listed as an owner of, or a waiter on, a deadlock resource.
    /// </summary>
    public sealed class DeadlockResourceParticipant
    {
        /// <summary>The process id referenced by the owner/waiter entry (e.g. "process1e0e8c8c58").</summary>
        public string ProcessId { get; internal set; } = string.Empty;

        /// <summary>
        /// The process this entry refers to, or null when the graph references a process that is not
        /// present in the process-list.  That happens with truncated graphs (the system_health ring
        /// buffer can drop processes), so consumers must handle null rather than assume resolution.
        /// </summary>
        public DeadlockProcess? Process { get; internal set; }

        /// <summary>Lock mode held (owners) or requested (waiters), e.g. "X", "S", "U".</summary>
        public string? Mode { get; internal set; }

        /// <summary>Waiters only - e.g. "wait", "convert".</summary>
        public string? RequestType { get; internal set; }
    }
}
