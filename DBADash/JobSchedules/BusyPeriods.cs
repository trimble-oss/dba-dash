using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.JobSchedules
{
    /// <summary>One stretch of time an instance is busy - e.g. Monday 08:00 to 18:00 - in the same time as the runs it's compared with</summary>
    public readonly record struct BusyInterval(DateTime Start, DateTime End);

    /// <summary>Runs that fall in busy hours, and the time they spend there</summary>
    public readonly record struct BusyAmount(int Runs, double Seconds)
    {
        public bool Any => Runs > 0;
    }

    /// <summary>
    /// The times an instance is busy (dbo.BusyPeriod, expanded over a range by dbo.BusyPeriods_Get), and which jobs ought to
    /// keep out of them.
    ///
    /// <para>Only jobs whose runs can all fit outside busy hours are held to them: a job that runs once a day or less often
    /// (daily, weekly, monthly), or that repeats less often than the longest busy stretch with room to run between.  A job
    /// that runs every minute is busy in busy hours whatever its start time, so counting its runs there would only be noise -
    /// and would pull the optimizer towards moves that gain nothing.</para>
    /// </summary>
    public sealed class BusyPeriods
    {
        /// <summary>No busy periods: nothing is counted and no job is held to them</summary>
        public static readonly BusyPeriods None = new(Array.Empty<BusyInterval>());

        /// <summary>The intervals in start order, merged where they touch or overlap so time isn't counted twice</summary>
        public IReadOnlyList<BusyInterval> Intervals { get; }

        /// <summary>The longest merged stretch, in seconds - a job has to repeat less often than this (and run in what's left) to keep clear of busy hours</summary>
        public double LongestStretchSec { get; }

        public bool IsEmpty => Intervals.Count == 0;

        // The intervals as runs, for JobOverlapAnalyzer's sweep
        private readonly List<JobRun> _asRuns;

        public BusyPeriods(IEnumerable<BusyInterval> intervals)
        {
            var merged = new List<BusyInterval>();
            foreach (var interval in intervals.Where(i => i.End > i.Start).OrderBy(i => i.Start))
            {
                if (merged.Count > 0 && interval.Start <= merged[^1].End)
                {
                    var last = merged[^1];
                    if (interval.End > last.End) merged[^1] = last with { End = interval.End };
                }
                else
                {
                    merged.Add(interval);
                }
            }
            Intervals = merged;
            LongestStretchSec = merged.Count == 0 ? 0 : merged.Max(i => (i.End - i.Start).TotalSeconds);
            _asRuns = merged.Select(i => new JobRun(i.Start, i.End)).ToList();
        }

        /// <summary>
        /// Whether the job's runs can all be put outside busy hours by its start times, so it's held to them.  True for a job
        /// whose movable schedules each run once on the days they run, or that repeats less often than the longest busy
        /// stretch with time for a run between (<see cref="JobOverlapAnalyzer.GetCycle"/>).  False with no busy periods.
        /// </summary>
        public bool CanAvoid(JobScheduleInput job)
        {
            if (IsEmpty || !job.Enabled) return false;
            var movable = job.Schedules.Where(AgentScheduleShifter.IsMovable).ToList();
            if (movable.Count == 0) return false;
            // Once a day or less often: the start time can be anywhere in the day
            if (movable.All(s => s.SubdayStepSeconds == 0)) return true;
            var duration = Math.Max(1, job.ExpectedDurationSec ?? 0);
            return JobOverlapAnalyzer.GetCycle(job) is { } cycle && cycle.PeriodSec - LongestStretchSec >= duration;
        }

        /// <summary>
        /// How many of the runs touch busy hours, and for how long in total.  The runs are one job's, in start order with none
        /// overlapping another - as <see cref="JobOverlapAnalyzer.ProjectRuns(JobScheduleInput, DateTime, DateTime, out bool)"/> and <see cref="SchedulePlan.ActualRuns"/> give them.
        /// </summary>
        /// <param name="inBusy">Set for each run that touches busy hours, when given - one flag per run</param>
        public BusyAmount Measure(IReadOnlyList<JobRun> runs, bool[] inBusy = null)
        {
            if (IsEmpty || runs.Count == 0) return default;
            var flags = inBusy ?? new bool[runs.Count];
            var (_, seconds) = JobOverlapAnalyzer.CountOverlaps(runs, _asRuns, flags);
            return new BusyAmount(flags.Count(f => f), seconds);
        }

        /// <summary>The seconds the runs spend in busy hours - the optimizer's cost, without counting runs</summary>
        internal double Seconds(IReadOnlyList<JobRun> runs) => IsEmpty || runs.Count == 0 ? 0 : JobOverlapAnalyzer.CountOverlaps(runs, _asRuns).Seconds;
    }
}
