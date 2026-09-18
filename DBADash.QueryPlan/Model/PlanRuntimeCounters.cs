using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// One thread's actual counters for an operator, straight from RunTimeCountersPerThread.
    ///
    /// Kept per thread rather than only aggregated because the interesting question about a parallel
    /// operator is usually how evenly the work was spread, and an average cannot answer it.
    /// </summary>
    public sealed class PlanThreadCounters
    {
        public int Thread { get; internal set; }

        public long ActualRows { get; internal set; }

        /// <summary>
        /// Rows read before the predicate was applied.  Absent before SQL Server 2016 SP1, and the
        /// gap between this and <see cref="ActualRows"/> is the residual predicate cost.
        /// </summary>
        public long? ActualRowsRead { get; internal set; }

        public long ActualExecutions { get; internal set; }

        /// <summary>Wall clock time for this thread, in milliseconds.</summary>
        public long? ActualElapsedMs { get; internal set; }

        public long? ActualCpuMs { get; internal set; }

        public long? ActualScans { get; internal set; }

        public long? ActualLogicalReads { get; internal set; }

        public long? ActualPhysicalReads { get; internal set; }

        public long? ActualReadAheads { get; internal set; }

        public long? ActualLobLogicalReads { get; internal set; }

        public long? ActualLobPhysicalReads { get; internal set; }

        /// <summary>Row or Batch, where showplan reported it.</summary>
        public string? ActualExecutionMode { get; internal set; }

        /// <summary>Batch mode row batches, present only for batch mode operators.</summary>
        public long? Batches { get; internal set; }
    }

    /// <summary>
    /// What actually happened at an operator, aggregated across its threads, with the per thread
    /// detail kept alongside.
    ///
    /// Only present on an actual plan.  An estimated plan has no runtime information at all, and the
    /// difference matters enough that the property is null rather than zeroed: zero rows and no
    /// measurement are not the same thing, and drawing them the same way would invent a row count
    /// that was never observed.
    /// </summary>
    public sealed class PlanRuntimeCounters
    {
        internal PlanRuntimeCounters(IReadOnlyList<PlanThreadCounters> threads)
        {
            Threads = threads;

            ActualRows = threads.Sum(t => t.ActualRows);
            ActualExecutions = threads.Sum(t => t.ActualExecutions);
            ActualRowsRead = threads.Any(t => t.ActualRowsRead.HasValue)
                ? threads.Sum(t => t.ActualRowsRead ?? 0)
                : null;

            // Elapsed time is wall clock and the threads ran at the same time, so the operator took
            // as long as its slowest thread.  Summing would report several seconds for an operator
            // that finished in one.
            ActualElapsedMs = Max(threads, t => t.ActualElapsedMs);

            // CPU is the opposite: every thread burned its own, so the cost to the server is the sum.
            ActualCpuMs = Sum(threads, t => t.ActualCpuMs);

            ActualScans = Sum(threads, t => t.ActualScans);
            ActualLogicalReads = Sum(threads, t => t.ActualLogicalReads);
            ActualPhysicalReads = Sum(threads, t => t.ActualPhysicalReads);
            ActualReadAheads = Sum(threads, t => t.ActualReadAheads);
            ActualLobLogicalReads = Sum(threads, t => t.ActualLobLogicalReads);
            ActualLobPhysicalReads = Sum(threads, t => t.ActualLobPhysicalReads);
            Batches = Sum(threads, t => t.Batches);

            // Thread 0 is the coordinator and does no work in a parallel branch, so counting it would
            // report a serial operator as having run on one thread and a four thread one as five.
            WorkerThreadCount = threads.Count(t => t.Thread != 0);

            ActualExecutionMode = threads
                .Select(t => t.ActualExecutionMode)
                .FirstOrDefault(mode => !string.IsNullOrEmpty(mode));
        }

        public IReadOnlyList<PlanThreadCounters> Threads { get; }

        /// <summary>Rows produced, summed over every thread and every execution.</summary>
        public long ActualRows { get; }

        /// <summary>
        /// Rows examined before the predicate, where SQL Server reported it.  A large gap to
        /// <see cref="ActualRows"/> is work done and thrown away - usually a missing index or a
        /// predicate that could not seek.
        /// </summary>
        public long? ActualRowsRead { get; }

        public long ActualExecutions { get; }

        /// <summary>The slowest thread's wall clock time - see the constructor for why not the sum.</summary>
        public long? ActualElapsedMs { get; }

        /// <summary>CPU across every thread.</summary>
        public long? ActualCpuMs { get; }

        public long? ActualScans { get; }

        public long? ActualLogicalReads { get; }

        public long? ActualPhysicalReads { get; }

        public long? ActualReadAheads { get; }

        public long? ActualLobLogicalReads { get; }

        public long? ActualLobPhysicalReads { get; }

        public long? Batches { get; }

        /// <summary>Threads that did work, excluding the coordinator.  Zero for a serial operator.</summary>
        public int WorkerThreadCount { get; }

        /// <summary>
        /// How many times the operator ran, counting all the threads of one parallel execution as a
        /// single run.
        ///
        /// <see cref="ActualExecutions"/> is the sum over threads, so a parallel operator that ran
        /// once at DOP 4 reports four executions.  That is the right number for some questions and
        /// exactly the wrong one for scaling an estimate: showplan's row estimate for a parallel
        /// operator is already the total across its threads, so multiplying by four compares the
        /// estimate against four times the rows that were ever expected, and every parallel operator
        /// in the plan looks like it over-estimated by the degree of parallelism.
        /// </summary>
        public long LogicalExecutions =>
            WorkerThreadCount > 1 ? Math.Max(1, ActualExecutions / WorkerThreadCount) : ActualExecutions;

        /// <summary>Row or Batch.  Batch mode on a row store operator is worth noticing.</summary>
        public string? ActualExecutionMode { get; }

        /// <summary>
        /// How unevenly the rows were spread across threads: the busiest thread's share divided by
        /// an even share, so 1.0 is perfectly balanced and 4.0 on a four thread operator means one
        /// thread did all of it.  Null for a serial operator, where there is nothing to compare.
        ///
        /// This is the number that explains a parallel plan running no faster than a serial one, and
        /// it is invisible in the totals.
        /// </summary>
        public double? ThreadSkew
        {
            get
            {
                if (WorkerThreadCount < 2 || ActualRows == 0) return null;

                var busiest = Threads.Where(t => t.Thread != 0).Max(t => t.ActualRows);
                var evenShare = ActualRows / (double)WorkerThreadCount;
                return evenShare == 0 ? null : busiest / evenShare;
            }
        }

        /// <summary>
        /// Batch mode ran this operator, which changes what its row counts mean and is not obvious
        /// from anything else on the node.
        /// </summary>
        public bool IsBatchMode =>
            string.Equals(ActualExecutionMode, "Batch", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Sum a counter over the threads, or null when no thread reported it.  Distinguishing "every
        /// thread said zero" from "the server did not measure it" is the whole point: the first is a
        /// fact about the query, the second is a fact about the plan format.
        /// </summary>
        private static long? Sum(
            IReadOnlyList<PlanThreadCounters> threads,
            Func<PlanThreadCounters, long?> selector)
        {
            long total = 0;
            var any = false;

            foreach (var thread in threads)
            {
                if (selector(thread) is not { } value) continue;
                total += value;
                any = true;
            }

            return any ? total : null;
        }

        private static long? Max(
            IReadOnlyList<PlanThreadCounters> threads,
            Func<PlanThreadCounters, long?> selector)
        {
            long? highest = null;

            foreach (var thread in threads)
            {
                if (selector(thread) is not { } value) continue;
                if (highest is null || value > highest) highest = value;
            }

            return highest;
        }
    }
}
