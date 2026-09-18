using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// Which figure an operator's elapsed and CPU time mean on the picture.
    /// </summary>
    public enum OperatorTimeMode
    {
        /// <summary>
        /// The time spent in the operator itself, with the time of its inputs taken out.  What
        /// ranking operators by time needs: the slow operator is the one that took the time, not
        /// every operator above it.
        /// </summary>
        Own,

        /// <summary>
        /// The figures exactly as SQL Server wrote them, which is what SSMS shows.  A row mode
        /// operator's time includes everything feeding it, and a batch mode operator's does not.
        /// </summary>
        AsReported
    }

    /// <summary>
    /// Works out each operator's own elapsed and CPU time from the runtime counters.
    ///
    /// SQL Server reports times two ways in one plan.  A batch mode operator reports its own time.
    /// A row mode operator's time runs from its first call to its last, and its inputs run inside
    /// those calls on the same thread, so the figure includes everything below it.  Ranked as they
    /// stand, the operators nearest the root always look the slowest, because each one contains the
    /// rest.  SQL Server 2022 can report own time for row mode too, and says so on the plan with
    /// <see cref="PlanStatement.ExclusiveProfileTimeActive"/>; then there is nothing to take off.
    ///
    /// The inputs' time is taken off thread by thread.  In a parallel branch each thread runs its own
    /// copy of the operator and its inputs, and taking the slowest input off the slowest parent pairs
    /// up threads that never had anything to do with each other.  The coordinator's thread 0 is left
    /// out of a parallel branch, where its elapsed time is the branch's wall clock rather than work.
    ///
    /// Two kinds of input need more than their own figure taking off:
    /// - A batch mode input reports only its own time, but its inputs ran on the same thread inside
    ///   the parent's calls too, so the whole batch region below it comes off.
    /// - An operator with no timings, like a Compute Scalar whose work was deferred, is looked through
    ///   to the operators under it.  Taking off nothing would leave them all in the parent.
    ///
    /// An exchange is where the thread numbers stop meaning the same thing.  The exchange's counters
    /// are for its consumer side, on the parent's threads, so they come off the parent as they
    /// stand.  Its input runs on another set of threads, which are numbered from one again, so under
    /// an exchange the branch is compared as a whole: the slowest thread of the exchange against the
    /// slowest thread of the branch.  An exchange's CPU is only ever its consumer side's own, so
    /// nothing is taken off that.
    ///
    /// Clock resolution and the per thread start up offsets mean a parent can report a millisecond or
    /// two less than its inputs, so every result is clamped at zero.
    /// </summary>
    internal static class PlanOperatorTiming
    {
        /// <summary>Set <see cref="PlanOperator.OwnElapsedMs"/> and <see cref="PlanOperator.OwnCpuMs"/> for every operator.</summary>
        public static void Apply(PlanOperator root, bool exclusiveTimes)
        {
            var elapsed = new Calculator(t => t.ActualElapsedMs, isElapsed: true);
            var cpu = new Calculator(t => t.ActualCpuMs, isElapsed: false);

            foreach (var node in root.DescendantsAndSelf())
            {
                if (exclusiveTimes)
                {
                    // The server has already done the subtraction.
                    node.OwnElapsedMs = node.Runtime?.ActualElapsedMs;
                    node.OwnCpuMs = node.Runtime?.ActualCpuMs;
                    continue;
                }

                node.OwnElapsedMs = elapsed.Own(node);
                node.OwnCpuMs = cpu.Own(node);
            }
        }

        /// <summary>One measure's own times, with each operator's per thread total remembered.</summary>
        private sealed class Calculator
        {
            private readonly Func<PlanThreadCounters, long?> _measure;
            private readonly bool _isElapsed;

            /// <summary>
            /// What each operator's subtree adds to its parent on each thread.  Remembered because
            /// every operator's parent asks for it, and a batch region or a chain of pass through
            /// operators would otherwise be walked again for each operator above it.
            /// </summary>
            private readonly Dictionary<PlanOperator, Dictionary<int, long>> _contributions = new();

            public Calculator(Func<PlanThreadCounters, long?> measure, bool isElapsed)
            {
                _measure = measure;
                _isElapsed = isElapsed;
            }

            public long? Own(PlanOperator node)
            {
                if (!IsTimed(node)) return null;

                var reported = _isElapsed ? node.Runtime!.ActualElapsedMs : node.Runtime!.ActualCpuMs;

                if (IsBatchMode(node)) return reported;

                if (IsExchange(node))
                {
                    if (!_isElapsed) return reported;

                    var branch = InputsByThread(node).Values.DefaultIfEmpty(0).Max();
                    return Math.Max(0, reported!.Value - branch);
                }

                var inputs = InputsByThread(node);
                var own = new List<long>();

                foreach (var thread in WorkingThreads(node))
                {
                    if (_measure(thread) is not { } value) continue;

                    inputs.TryGetValue(thread.Thread, out var fromInputs);
                    own.Add(Math.Max(0, value - fromInputs));
                }

                if (own.Count == 0) return reported;

                // Aggregated the same way as the reported figures: elapsed is the slowest thread,
                // because the threads ran side by side, and CPU is every thread's added together.
                return _isElapsed ? own.Max() : own.Sum();
            }

            /// <summary>The time the operator's inputs add to it on each thread, summed over the inputs.</summary>
            private Dictionary<int, long> InputsByThread(PlanOperator node)
            {
                var total = new Dictionary<int, long>();
                foreach (var child in node.Children) AddTo(total, Contribution(child));
                return total;
            }

            /// <summary>What an operator and everything under it adds to its parent's time on each thread.</summary>
            private Dictionary<int, long> Contribution(PlanOperator node)
            {
                if (_contributions.TryGetValue(node, out var known)) return known;

                Dictionary<int, long> result;

                if (!IsTimed(node))
                {
                    // Nothing to go on under an exchange either: its inputs' threads are another
                    // branch's, and adding them in would charge the parent for the wrong threads.
                    result = IsExchange(node) ? new Dictionary<int, long>() : InputsByThread(node);
                }
                else
                {
                    result = new Dictionary<int, long>();
                    foreach (var thread in WorkingThreads(node))
                    {
                        if (_measure(thread) is { } value) Add(result, thread.Thread, value);
                    }

                    // A batch mode operator's figure is its own, and the region under it ran inside
                    // the parent's calls as well.  A row mode figure, an exchange's included,
                    // already contains its inputs.
                    if (IsBatchMode(node)) AddTo(result, InputsByThread(node));
                }

                _contributions[node] = result;
                return result;
            }

            /// <summary>
            /// The threads whose times are the operator's work.  In a parallel branch thread 0 is the
            /// coordinator: it runs no executions, but can still carry an elapsed time that is the
            /// wall clock of the whole branch rather than anything it did.  Counted, it hands the
            /// operator the branch's entire duration.  On a serial operator thread 0 is the only
            /// thread, and the one doing the work.
            /// </summary>
            private static IEnumerable<PlanThreadCounters> WorkingThreads(PlanOperator node)
            {
                var threads = node.Runtime!.Threads;
                var parallel = threads.Any(t => t.Thread != 0);

                return parallel
                    ? threads.Where(t => t.Thread != 0 || t.ActualExecutions > 0)
                    : threads;
            }

            private bool IsTimed(PlanOperator node) =>
                node.Runtime is { } runtime && runtime.Threads.Any(t => _measure(t) is not null);

            private static bool IsBatchMode(PlanOperator node) =>
                node.Runtime?.IsBatchMode == true;

            private static bool IsExchange(PlanOperator node) =>
                node.Kind is PlanOperatorKind.GatherStreams
                    or PlanOperatorKind.RepartitionStreams
                    or PlanOperatorKind.DistributeStreams;

            private static void AddTo(Dictionary<int, long> total, Dictionary<int, long> more)
            {
                foreach (var (thread, value) in more) Add(total, thread, value);
            }

            private static void Add(Dictionary<int, long> total, int thread, long value)
            {
                total.TryGetValue(thread, out var existing);
                total[thread] = existing + value;
            }
        }
    }
}
