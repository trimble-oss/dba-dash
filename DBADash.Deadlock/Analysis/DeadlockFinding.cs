using System;
using System.Collections.Generic;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// One thing worth saying about a deadlock: what the graph shows, and where that leads.
    ///
    /// Findings carry model references rather than formatted node ids so a viewer can take the
    /// reader to what the finding is about - selecting the process on the graph, or the row in a
    /// grid - without parsing the text back apart.
    /// </summary>
    public sealed class DeadlockFinding
    {
        public DeadlockFindingSeverity Severity { get; internal set; }

        /// <summary>A few words naming the pattern, e.g. "Objects locked in opposite order".</summary>
        public string Title { get; internal set; } = string.Empty;

        /// <summary>
        /// A sentence or two: what the graph shows, then what usually fixes it.  Written to be read
        /// by someone who has just been handed the deadlock and does not yet know the code.
        /// </summary>
        public string Detail { get; internal set; } = string.Empty;

        /// <summary>The processes the finding is about.  Empty when it is about the capture itself.</summary>
        public IReadOnlyList<DeadlockProcess> Processes { get; internal set; }
            = Array.Empty<DeadlockProcess>();

        /// <summary>The resources the finding is about.</summary>
        public IReadOnlyList<DeadlockResource> Resources { get; internal set; }
            = Array.Empty<DeadlockResource>();
    }
}
