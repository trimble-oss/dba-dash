using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>One hop of a deadlock cycle: a process, the resource it waits on, and a process holding it.</summary>
    public readonly record struct DeadlockCycleStep(
        DeadlockProcess Waiter,
        DeadlockResource Resource,
        DeadlockProcess Owner);

    /// <summary>
    /// Finds the cycle in a deadlock graph - who is waiting on what, held by whom, back round to the
    /// start.  Both the layout (which draws the cycle as the shape of the picture) and the analysis
    /// (which describes it) need it, so it lives here rather than in either.
    /// </summary>
    public static class DeadlockCycle
    {
        /// <summary>
        /// The cycle, in order, or empty when none can be traced - which means a truncated capture,
        /// since a real deadlock always has one.
        ///
        /// Every owner of every contended resource is searched rather than the first: a resource can
        /// have several owners - two sessions holding S while a third asks for X is an everyday
        /// deadlock - and only one of them may be on the cycle.  Deadlock graphs are tiny, so the
        /// exhaustive search costs nothing.
        /// </summary>
        public static IReadOnlyList<DeadlockCycleStep> Find(DeadlockGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var waitsOn = graph.Processes.ToDictionary(
                p => p,
                p => graph.Resources.Where(r => r.Waiters.Any(w => ReferenceEquals(w.Process, p))).ToList());

            foreach (var start in StartCandidates(graph, waitsOn))
            {
                var path = new List<DeadlockCycleStep>();
                var onPath = new HashSet<DeadlockProcess>();

                if (Search(start)) return path;

                bool Search(DeadlockProcess process)
                {
                    if (!onPath.Add(process)) return false;

                    foreach (var resource in waitsOn[process])
                    {
                        foreach (var owner in OwnersOtherThan(resource, process))
                        {
                            path.Add(new DeadlockCycleStep(process, resource, owner));
                            if (ReferenceEquals(owner, start) || Search(owner)) return true;
                            path.RemoveAt(path.Count - 1);
                        }
                    }

                    onPath.Remove(process);
                    return false;
                }
            }

            return Array.Empty<DeadlockCycleStep>();
        }

        /// <summary>
        /// The processes holding a resource, other than the one waiting on it.  A process can own and
        /// wait on the same resource - that is a conversion deadlock, where it holds S and wants X -
        /// and following that self-edge would close a cycle of one that says nothing about who it is
        /// actually deadlocked with.
        /// </summary>
        private static IEnumerable<DeadlockProcess> OwnersOtherThan(DeadlockResource resource, DeadlockProcess waiter)
        {
            var seen = new HashSet<DeadlockProcess>();

            foreach (var participant in resource.Owners)
            {
                var owner = participant.Process;
                if (owner is null || ReferenceEquals(owner, waiter)) continue;
                if (seen.Add(owner)) yield return owner;
            }
        }

        /// <summary>
        /// Where to start looking.  A victim first - it is the process the reader cares about most,
        /// and starting there puts it at the head of the cycle - then any other waiting process, so a
        /// cycle the victim is not part of is still found.
        /// </summary>
        private static IEnumerable<DeadlockProcess> StartCandidates(
            DeadlockGraph graph,
            Dictionary<DeadlockProcess, List<DeadlockResource>> waitsOn)
        {
            var victims = new HashSet<DeadlockProcess>(graph.Victims);

            foreach (var process in graph.Victims.Concat(graph.Processes.Where(p => !victims.Contains(p))))
            {
                if (waitsOn.TryGetValue(process, out var waits) && waits.Count > 0) yield return process;
            }
        }
    }
}
