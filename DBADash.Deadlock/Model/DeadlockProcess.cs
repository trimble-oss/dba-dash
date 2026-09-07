using System;
using System.Collections.Generic;

namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// A process (session/task) taking part in a deadlock, from an entry in the process-list.
    ///
    /// Note that a process is not the same thing as a session: a parallel query contributes several
    /// process entries sharing a <see cref="Spid"/> but differing by <see cref="Ecid"/>.
    /// </summary>
    public sealed class DeadlockProcess
    {
        /// <summary>The graph-internal id (e.g. "process1e0e8c8c58") used by owner/waiter references.</summary>
        public string Id { get; internal set; } = string.Empty;

        public int? Spid { get; internal set; }

        /// <summary>Execution context id.  0 is the coordinating task; non-zero indicates a parallel worker.</summary>
        public int? Ecid { get; internal set; }

        public string? Status { get; internal set; }

        public string? LoginName { get; internal set; }

        public string? HostName { get; internal set; }

        public int? HostPid { get; internal set; }

        public string? ClientApp { get; internal set; }

        /// <summary>Raw isolation level string, e.g. "read committed (2)".</summary>
        public string? IsolationLevel { get; internal set; }

        /// <summary>The lock mode this process holds or is requesting.</summary>
        public string? LockMode { get; internal set; }

        /// <summary>
        /// The raw wait_resource string, e.g. "KEY: 5:72057594045595648 (61a06abd401c)".  Left
        /// undecoded here - decoding requires a connection to the source instance.
        /// </summary>
        public string? WaitResource { get; internal set; }

        public TimeSpan? WaitTime { get; internal set; }

        public int? TransactionCount { get; internal set; }

        public string? TransactionName { get; internal set; }

        public DateTime? LastTransactionStarted { get; internal set; }

        public DateTime? LastBatchStarted { get; internal set; }

        public DateTime? LastBatchCompleted { get; internal set; }

        /// <summary>Transaction log bytes used by this process.</summary>
        public long? LogUsed { get; internal set; }

        /// <summary>DEADLOCK_PRIORITY of the process.</summary>
        public int? Priority { get; internal set; }

        /// <summary>
        /// The <c>clientoption1</c> bitmask: the SET options the session was running under, such as
        /// IMPLICIT_TRANSACTIONS, XACT_ABORT and ARITHABORT.  Carried and stored raw; the repository
        /// decodes it into option names at report time (dbo.DecodeClientOptions), which keeps the bits
        /// SQL Server does not name available and lets the naming be corrected without recollecting.
        /// </summary>
        public int? ClientOption1 { get; internal set; }

        /// <summary>The <c>clientoption2</c> bitmask.  See <see cref="ClientOption1"/>.</summary>
        public int? ClientOption2 { get; internal set; }

        public int? CurrentDatabaseId { get; internal set; }

        public string? CurrentDatabaseName { get; internal set; }

        /// <summary>Contents of the inputbuf element, trimmed.</summary>
        public string? InputBuffer { get; internal set; }

        /// <summary>Execution stack frames, outermost last as they appear in the graph.</summary>
        public IReadOnlyList<DeadlockFrame> ExecutionStack { get; internal set; } = Array.Empty<DeadlockFrame>();

        /// <summary>True when this process was chosen as a deadlock victim and rolled back.</summary>
        public bool IsVictim { get; internal set; }

        /// <summary>
        /// Short identifier for display, e.g. "SPID 52" or "SPID 52 (ecid 3)" for a parallel worker.
        /// Falls back to <see cref="Id"/> when the graph carries no spid.
        /// </summary>
        public string DisplayName =>
            Spid is null
                ? Id
                : Ecid is > 0
                    ? $"SPID {Spid} (ecid {Ecid})"
                    : $"SPID {Spid}";

        /// <summary>
        /// The execution stack frame most likely to be the interesting one - the innermost frame carrying
        /// statement text, falling back to the innermost frame that at least identifies the statement by
        /// sql handle.  The statement, the plan lookup and the Query Store lookup all key off this frame.
        /// </summary>
        public DeadlockFrame? PrimaryFrame
        {
            get
            {
                foreach (var frame in ExecutionStack)
                {
                    if (!string.IsNullOrWhiteSpace(frame.Sql)) return frame;
                }
                foreach (var frame in ExecutionStack)
                {
                    if (!string.IsNullOrWhiteSpace(frame.SqlHandle)) return frame;
                }
                return null;
            }
        }

        /// <summary>
        /// The statement most likely to be the interesting one - the innermost execution stack frame
        /// carrying text, falling back to the input buffer.
        /// </summary>
        public string? PrimaryStatement
        {
            get
            {
                var sql = PrimaryFrame?.Sql;
                if (!string.IsNullOrWhiteSpace(sql)) return sql;
                return string.IsNullOrWhiteSpace(InputBuffer) ? null : InputBuffer;
            }
        }
    }
}
