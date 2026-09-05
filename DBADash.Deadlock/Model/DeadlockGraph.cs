using System;
using System.Collections.Generic;

namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// A single parsed deadlock.  A source document may contain more than one - see
    /// <see cref="DeadlockParser"/>.
    ///
    /// This type is a pure description of what the graph said.  It carries no layout or rendering
    /// concerns: positioning lives in the layout layer and drawing in the renderer, so that this
    /// model stays usable headless (reporting, AI summaries, tests).
    /// </summary>
    public sealed class DeadlockGraph
    {
        private readonly Dictionary<string, DeadlockProcess> _processesById =
            new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<DeadlockProcess> Processes { get; internal set; } = Array.Empty<DeadlockProcess>();

        public IReadOnlyList<DeadlockResource> Resources { get; internal set; } = Array.Empty<DeadlockResource>();

        /// <summary>
        /// The processes SQL Server chose to roll back.  Usually one, but empty is possible when the
        /// graph does not name a victim, and a parallel deadlock can name more than one.
        /// </summary>
        public IReadOnlyList<DeadlockProcess> Victims { get; internal set; } = Array.Empty<DeadlockProcess>();

        /// <summary>
        /// True when this is an intra-query (parallel) deadlock rather than a deadlock between
        /// separate sessions.  Detected from parallelism resources such as exchangeEvent, or from
        /// several process entries sharing one spid.
        /// </summary>
        public bool IsParallel { get; internal set; }

        /// <summary>The XML of this deadlock element, so a viewer can show the original source.</summary>
        public string Xml { get; internal set; } = string.Empty;

        /// <summary>
        /// Roughly when the deadlock happened: the latest batch or transaction start among the
        /// participants.  The graph carries no timestamp of its own - the deadlock is detected some
        /// time after the last of these - so this is the closest the graph itself can say.
        ///
        /// Null when no participant carries either time.  Used to ask for the schema as it was then
        /// rather than as it is now.
        /// </summary>
        public DateTime? OccurredAt
        {
            get
            {
                DateTime? latest = null;

                foreach (var process in Processes)
                {
                    foreach (var candidate in new[] { process.LastBatchStarted, process.LastTransactionStarted })
                    {
                        if (candidate is { } value && (latest is null || value > latest)) latest = value;
                    }
                }

                return latest;
            }
        }

        /// <summary>Look up a process by its graph-internal id.  Returns null when not present.</summary>
        public DeadlockProcess? FindProcess(string? id) =>
            id is not null && _processesById.TryGetValue(id, out var p) ? p : null;

        internal void IndexProcesses()
        {
            _processesById.Clear();
            foreach (var p in Processes)
            {
                // A malformed graph could repeat an id; first one wins rather than throwing.
                _processesById.TryAdd(p.Id, p);
            }
        }
    }
}
