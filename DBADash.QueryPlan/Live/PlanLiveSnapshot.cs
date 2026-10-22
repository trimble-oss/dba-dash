using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Live
{
    /// <summary>Where an operator of a running query has got to.</summary>
    public enum PlanLiveState
    {
        /// <summary>Not run yet: no executions and no rows.</summary>
        NotStarted,

        /// <summary>Started and not finished, but nothing changed since the last capture - waiting on its inputs, or blocked.</summary>
        Waiting,

        /// <summary>Rows or executions went up since the last capture: rows are flowing through it now.</summary>
        Active,

        /// <summary>Done: it has reached the end of its input on every execution so far, and nothing changed since the last capture.</summary>
        Finished
    }

    /// <summary>
    /// The state of each operator of a running query at one capture, worked out from its counters and how they moved
    /// since the capture before.  Keyed by showplan node id.  Operators SQL Server doesn't measure - a Compute Scalar
    /// whose expressions are deferred - have no entry.
    /// </summary>
    public sealed class PlanLiveSnapshot
    {
        private readonly Dictionary<int, PlanLiveState> _states;

        private PlanLiveSnapshot(Dictionary<int, PlanLiveState> states, DateTime capturedUtc)
        {
            _states = states;
            CapturedUtc = capturedUtc;
        }

        public DateTime CapturedUtc { get; }

        public IReadOnlyDictionary<int, PlanLiveState> States => _states;

        /// <summary>True while any operator is moving rows, which is when the flow along the arrows is worth animating.</summary>
        public bool HasActive => _states.Values.Any(s => s == PlanLiveState.Active);

        public PlanLiveState? StateOf(int nodeId) => _states.TryGetValue(nodeId, out var state) ? state : null;

        /// <summary>
        /// The state of each operator in <paramref name="current"/>.  <paramref name="previous"/> is the capture before,
        /// which is what says whether an operator is moving rows now rather than only having moved them at some point -
        /// without it nothing is reported as active.
        /// </summary>
        public static PlanLiveSnapshot From(IEnumerable<PlanLiveCounters> current, IEnumerable<PlanLiveCounters>? previous, DateTime capturedUtc)
        {
            ArgumentNullException.ThrowIfNull(current);

            var before = previous?
                .GroupBy(c => c.NodeId)
                .ToDictionary(g => g.Key, g => (Rows: g.Sum(c => c.Rows), Executions: g.Sum(c => c.Executions)));

            var states = new Dictionary<int, PlanLiveState>();
            foreach (var node in current.GroupBy(c => c.NodeId))
            {
                var rows = node.Sum(c => c.Rows);
                var executions = node.Sum(c => c.Executions);
                var changed = before is not null &&
                              (!before.TryGetValue(node.Key, out var last) || last.Rows != rows || last.Executions != executions);

                states[node.Key] = StateOf(node.ToList(), rows, executions, changed);
            }

            return new PlanLiveSnapshot(states, capturedUtc);
        }

        private static PlanLiveState StateOf(IReadOnlyList<PlanLiveCounters> threads, long rows, long executions, bool changed)
        {
            // Standard profiling records when each operator was opened and closed, which is the real answer.  Lightweight
            // profiling leaves them at zero, and the counters are all there is to go on.
            var timed = threads.Any(t => t.OpenTimeMs > 0 || t.CloseTimeMs > 0);

            var notStarted = timed
                ? threads.All(t => !(t.OpenTimeMs > 0))
                : executions == 0 && rows == 0;
            if (notStarted) return PlanLiveState.NotStarted;

            // Changes win over finished: the inner side of a loop reaches the end of its input on every execution and
            // starts again for the next outer row, so between executions it looks finished while it is very much at work.
            if (changed) return PlanLiveState.Active;

            // A parallel operator can include a coordinator or worker row that was never opened. Its zero close time
            // is not unfinished work, so only threads that actually opened have to have closed.
            var finished = timed
                ? threads.Where(t => t.OpenTimeMs > 0).All(t => t.CloseTimeMs > 0)
                : executions > 0 && threads.Sum(t => t.EndOfScans) >= executions;

            return finished ? PlanLiveState.Finished : PlanLiveState.Waiting;
        }
    }
}
