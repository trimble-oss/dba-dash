using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.JobSchedules
{
    /// <summary>One execution of a job, actual or projected</summary>
    public readonly record struct JobRun(DateTime Start, DateTime End)
    {
        public double Seconds => (End - Start).TotalSeconds;

        /// <summary>How an actual run ended (msdb run_status: 0 failed, 1 succeeded, 2 retry, 3 cancelled).  Null for a projected run.</summary>
        public int? Status { get; init; }

        /// <summary>
        /// What started an actual run, from its history message ("invoked by ..."): "Schedule 12 (Nightly).  The last step
        /// ...", "Start Sequence." or "User DOMAIN\name."  Null when it isn't known.
        /// </summary>
        public string InvokedBy { get; init; }

        /// <summary>The job's schedule that started an actual run, as <see cref="SchedulePlan"/> works it out.  Null when none did, or it isn't known.</summary>
        public AgentSchedule Schedule { get; init; }

        /// <summary>How far a plan moved the run, in seconds: 0 when it stays where it was</summary>
        public int MovedBySec { get; init; }

        /// <summary>
        /// A run the job's schedules as they are now predict, shown in place of the runs that happened before they changed
        /// (<see cref="SchedulePlan.ScheduleChangeOf"/>)
        /// </summary>
        public bool Predicted { get; init; }
    }

    /// <summary>A job's schedules and how long it's expected to take</summary>
    public sealed class JobScheduleInput
    {
        public Guid JobId { get; init; }
        public string Name { get; init; }
        public bool Enabled { get; init; } = true;
        public IReadOnlyList<AgentSchedule> Schedules { get; init; } = Array.Empty<AgentSchedule>();

        /// <summary>Null when there's no history to estimate it from</summary>
        public int? ExpectedDurationSec { get; init; }
    }

    /// <summary>A job that repeats at a fixed interval: the interval, when in it the job starts and how long it runs</summary>
    public readonly record struct JobCycle(long PeriodSec, long OffsetSec, bool AllDay);

    public enum CycleVerdict
    {
        /// <summary>One of the jobs doesn't repeat at a fixed interval, or has no expected duration</summary>
        NotComparable,

        /// <summary>The current start times keep the runs apart</summary>
        Clear,

        /// <summary>The runs meet from time to time, but different start times would keep them apart</summary>
        Collides,

        /// <summary>No start times keep the runs apart: the cycles only share an interval shorter than both runs together</summary>
        CannotSeparate,

        /// <summary>A job runs for as long as its interval (or longer), so it's always running</summary>
        AlwaysRunning
    }

    public sealed class JobOverlapSummary
    {
        public JobScheduleInput Job { get; init; }

        /// <summary>Why the job's runs can't be worked out in advance.  Null when they can.</summary>
        public string NotProjectableReason { get; init; }

        public int ProjectedRuns { get; init; }

        /// <summary>The job would start more often than <see cref="JobOverlapAnalyzer.MaxRunsPerJob"/> in the period</summary>
        public bool ProjectionTruncated { get; init; }

        public JobCycle? Cycle { get; init; }

        /// <summary>Share of the time the job is expected to be running (0-1).  Null with no expected duration.</summary>
        public double? BusyFraction { get; init; }

        public int ActualRuns { get; init; }

        /// <summary>Actual runs that overlapped a run of another of the jobs</summary>
        public int ActualRunsOverlapping { get; init; }

        public int PredictedRunsOverlapping { get; init; }
    }

    public sealed class JobPairOverlap
    {
        public JobScheduleInput A { get; init; }
        public JobScheduleInput B { get; init; }
        public int ActualOverlaps { get; init; }
        public double ActualOverlapSec { get; init; }
        public int PredictedOverlaps { get; init; }
        public double PredictedOverlapSec { get; init; }
        public CycleVerdict Verdict { get; init; }

        /// <summary>The interval the two cycles share (the gcd of their periods) - the room there is to fit both runs</summary>
        public long? CommonCycleSec { get; init; }

        /// <summary>One or both jobs run only part of the day, so the cycle comparison is a sufficient rather than exact test</summary>
        public bool PartOfDay { get; init; }

        /// <summary>An enabled schedule both jobs use, so they start together whatever its start time.  Null when they share none.</summary>
        public string SharedSchedule { get; init; }
    }

    public sealed class JobOverlapReport
    {
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public IReadOnlyList<JobOverlapSummary> Jobs { get; init; }

        /// <summary>Pairs that overlapped, are predicted to, or whose cycles say they collide or can't be kept apart</summary>
        public IReadOnlyList<JobPairOverlap> Pairs { get; init; }
    }

    /// <summary>
    /// Compares what a set of jobs' schedules predict with what actually ran: which pairs of jobs overlap, how often, and
    /// whether moving their start times could keep them apart.
    ///
    /// <para>Two jobs that repeat every P1 and P2 seconds, starting at offsets o1 and o2 and running for d1 and d2, have
    /// start differences of (o2 - o1) plus any multiple of g = gcd(P1, P2).  They never overlap when (o2 - o1) mod g lies
    /// between d1 and g - d2, and no offsets can manage that when d1 + d2 > g.</para>
    /// </summary>
    public static class JobOverlapAnalyzer
    {
        /// <summary>Projection stops here for a job - a job every 10 seconds for a month is 259,200 runs</summary>
        public const int MaxRunsPerJob = 200_000;

        /// <summary>
        /// When the job is expected to run in the period, across all its enabled schedules, each run with the schedule that
        /// starts it (<see cref="JobRun.Schedule"/>).  Like SQL Server Agent, a start that comes while the previous run is
        /// still going is skipped.  A run that started before the period and is still going at its start is included, as it
        /// overlaps and holds off the starts due then.  Empty for a disabled job.
        /// </summary>
        public static List<JobRun> ProjectRuns(JobScheduleInput job, DateTime from, DateTime to, out bool truncated) =>
            ProjectRuns(job, from, to, out truncated, out _);

        /// <param name="clashes">
        /// Starts lost to another of the job's schedules: due while a run another schedule started is still going, or at the
        /// same time as another schedule.  A start lost to a run of its own schedule (a job that runs longer than its
        /// interval) isn't counted.
        /// </param>
        public static List<JobRun> ProjectRuns(JobScheduleInput job, DateTime from, DateTime to, out bool truncated, out int clashes)
        {
            truncated = false;
            clashes = 0;
            var runs = new List<JobRun>();
            if (!job.Enabled) return runs;
            var duration = TimeSpan.FromSeconds(Math.Max(1, job.ExpectedDurationSec ?? 0));
            var schedules = job.Schedules.Where(s => s.Enabled).ToList();
            // From a run's length before the period, for a run still going when it starts.  (A job that runs longer than its
            // interval would need its whole history to know which starts it skips - it's taken from here.)
            var lookback = from - duration;
            var starts = schedules.Select(s => AgentScheduleProjector.RunTimes(s, lookback, to)).ToList();
            var busyUntil = DateTime.MinValue;
            AgentSchedule running = null;
            foreach (var (start, index) in Merge(starts))
            {
                if (start < busyUntil)
                {
                    if (!ReferenceEquals(schedules[index], running) && start >= from) clashes++;
                    continue;
                }
                running = schedules[index];
                busyUntil = start + duration;
                if (busyUntil <= from) continue;
                if (runs.Count == MaxRunsPerJob)
                {
                    truncated = true;
                    break;
                }
                runs.Add(new JobRun(start, busyUntil) { Schedule = running });
            }
            return runs;
        }

        /// <summary>
        /// Merges ascending sequences into one, with the index of the sequence each time came from.  A time two sequences
        /// share comes once from each - the first starts the run and the other is skipped as it's running.
        /// </summary>
        private static IEnumerable<(DateTime Time, int Index)> Merge(List<IEnumerable<DateTime>> sequences)
        {
            if (sequences.Count == 1)
            {
                foreach (var t in sequences[0]) yield return (t, 0);
                yield break;
            }
            var enumerators = sequences.Select((s, i) => (Enumerator: s.GetEnumerator(), Index: i)).Where(e => e.Enumerator.MoveNext()).ToList();
            while (enumerators.Count > 0)
            {
                var next = enumerators.MinBy(e => (e.Enumerator.Current, e.Index));
                yield return (next.Enumerator.Current, next.Index);
                if (!next.Enumerator.MoveNext()) enumerators.Remove(next);
            }
        }

        /// <summary>
        /// How many pairs of runs overlap, and for how long in total.  Each list is in start order with no run of a job
        /// overlapping another run of the same job - true of Agent history, and of <see cref="ProjectRuns"/>.
        /// <paramref name="aOverlapping"/> and <paramref name="bOverlapping"/> (one flag per run) are set for the runs that overlap.
        /// </summary>
        public static (int Count, double Seconds) CountOverlaps(IReadOnlyList<JobRun> a, IReadOnlyList<JobRun> b, bool[] aOverlapping = null, bool[] bOverlapping = null)
        {
            var sink = new CountSink { AOverlapping = aOverlapping, BOverlapping = bOverlapping };
            Sweep(a, b, ref sink);
            return (sink.Count, sink.Seconds);
        }

        /// <summary>Told of each overlap <see cref="Sweep{TSink}"/> finds: run a of the first list, run b of the second, and when they overlap</summary>
        internal interface IOverlapSink
        {
            void Add(int a, int b, DateTime start, DateTime end);
        }

        /// <summary>
        /// Every pair of runs, one from each list, that overlap - each list in start order with no run of a job overlapping
        /// another run of the same job.  The sink is a struct so the optimizer, which sweeps a great many times, makes no
        /// delegate calls.
        /// </summary>
        internal static void Sweep<TSink>(IReadOnlyList<JobRun> a, IReadOnlyList<JobRun> b, ref TSink sink) where TSink : struct, IOverlapSink
        {
            var first = 0;
            for (var i = 0; i < a.Count; i++)
            {
                var run = a[i];
                // b's runs end in start order too, so the first that ends after this run starts only moves forward
                while (first < b.Count && b[first].End <= run.Start) first++;
                for (var j = first; j < b.Count && b[j].Start < run.End; j++)
                {
                    var overlapStart = run.Start > b[j].Start ? run.Start : b[j].Start;
                    var overlapEnd = run.End < b[j].End ? run.End : b[j].End;
                    if (overlapEnd > overlapStart) sink.Add(i, j, overlapStart, overlapEnd);
                }
            }
        }

        private struct CountSink : IOverlapSink
        {
            public int Count;
            public double Seconds;
            public bool[] AOverlapping;
            public bool[] BOverlapping;

            public void Add(int a, int b, DateTime start, DateTime end)
            {
                Count++;
                Seconds += (end - start).TotalSeconds;
                if (AOverlapping != null) AOverlapping[a] = true;
                if (BOverlapping != null) BOverlapping[b] = true;
            }
        }

        /// <summary>
        /// The job's cycle when it has one enabled schedule that recurs at times of day, and that repeats at a fixed interval.
        /// An agent start or CPU idle schedule beside it doesn't count: it has no time of day.  Nor does a one-time
        /// schedule - often one left from an ad-hoc run long ago - as it's a single run, not part of the cadence.
        /// </summary>
        public static JobCycle? GetCycle(JobScheduleInput job)
        {
            if (!job.Enabled) return null;
            var enabled = job.Schedules.Where(AgentScheduleShifter.IsMovable).ToList();
            if (enabled.Count != 1) return null;
            if (!AgentScheduleProjector.TryGetCycle(enabled[0], out var period, out var offset, out var allDay)) return null;
            // Running longer than its interval, it starts at the first time due after each run ends: every so many intervals.
            // That cycle only holds all day when it fits a whole number of times in a day - otherwise it shifts day to day.
            if (job.ExpectedDurationSec is { } duration && duration > period)
            {
                period *= (duration + period - 1) / period;
                if (allDay && 86400 % period != 0) return null;
            }
            return new JobCycle(period, offset, allDay);
        }

        /// <summary>Whether the start times of two cyclic jobs keep their runs apart, and whether any could</summary>
        public static CycleVerdict CompareCycles(JobCycle a, long aDurationSec, JobCycle b, long bDurationSec, out long commonCycleSec)
        {
            commonCycleSec = ScheduleMath.Gcd(a.PeriodSec, b.PeriodSec);
            aDurationSec = Math.Max(1, aDurationSec);
            bDurationSec = Math.Max(1, bDurationSec);
            if (aDurationSec >= a.PeriodSec || bDurationSec >= b.PeriodSec) return CycleVerdict.AlwaysRunning;
            if (aDurationSec + bDurationSec > commonCycleSec) return CycleVerdict.CannotSeparate;
            var delta = ((b.OffsetSec - a.OffsetSec) % commonCycleSec + commonCycleSec) % commonCycleSec;
            return delta >= aDurationSec && delta <= commonCycleSec - bDurationSec ? CycleVerdict.Clear : CycleVerdict.Collides;
        }

        /// <summary>The name of an enabled schedule both jobs use (matched by schedule_uid), or null</summary>
        public static string SharedSchedule(JobScheduleInput a, JobScheduleInput b)
        {
            if (!a.Enabled || !b.Enabled) return null;
            return a.Schedules
                .Where(s => s.Enabled && s.Uid.HasValue)
                .FirstOrDefault(s => b.Schedules.Any(o => o.Enabled && o.Uid == s.Uid))?.Name;
        }

        /// <param name="jobs">The jobs to compare</param>
        /// <param name="actualRuns">What each job actually ran in the period, by job id, in start order.  Jobs without an entry ran nothing.</param>
        /// <param name="from">Start of the period, in the server's local time like the schedules</param>
        /// <param name="to">End of the period</param>
        public static JobOverlapReport Analyze(IReadOnlyList<JobScheduleInput> jobs, IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> actualRuns, DateTime from, DateTime to)
        {
            var periodSec = (to - from).TotalSeconds;
            var projected = new List<JobRun>[jobs.Count];
            var truncated = new bool[jobs.Count];
            var actual = new IReadOnlyList<JobRun>[jobs.Count];
            var cycles = new JobCycle?[jobs.Count];
            var actualFlags = new bool[jobs.Count][];
            var predictedFlags = new bool[jobs.Count][];
            for (var i = 0; i < jobs.Count; i++)
            {
                projected[i] = ProjectRuns(jobs[i], from, to, out truncated[i]);
                actual[i] = actualRuns.TryGetValue(jobs[i].JobId, out var runs) ? runs : Array.Empty<JobRun>();
                cycles[i] = GetCycle(jobs[i]);
                actualFlags[i] = new bool[actual[i].Count];
                predictedFlags[i] = new bool[projected[i].Count];
            }

            var pairs = new List<JobPairOverlap>();
            for (var i = 0; i < jobs.Count; i++)
            {
                for (var j = i + 1; j < jobs.Count; j++)
                {
                    var (actualCount, actualSec) = CountOverlaps(actual[i], actual[j], actualFlags[i], actualFlags[j]);
                    // A duration of 1 second stands in for an unknown one, which would predict nothing worth showing
                    var (predictedCount, predictedSec) = jobs[i].ExpectedDurationSec.HasValue && jobs[j].ExpectedDurationSec.HasValue
                        ? CountOverlaps(projected[i], projected[j], predictedFlags[i], predictedFlags[j])
                        : (0, 0d);
                    var verdict = CycleVerdict.NotComparable;
                    long? common = null;
                    if (cycles[i] is { } ci && cycles[j] is { } cj && jobs[i].ExpectedDurationSec is { } di && jobs[j].ExpectedDurationSec is { } dj)
                    {
                        verdict = CompareCycles(ci, di, cj, dj, out var g);
                        common = g;
                    }
                    var shared = SharedSchedule(jobs[i], jobs[j]);
                    if (actualCount == 0 && predictedCount == 0 && shared == null && verdict is not (CycleVerdict.Collides or CycleVerdict.CannotSeparate)) continue;
                    pairs.Add(new JobPairOverlap
                    {
                        A = jobs[i],
                        B = jobs[j],
                        ActualOverlaps = actualCount,
                        ActualOverlapSec = actualSec,
                        PredictedOverlaps = predictedCount,
                        PredictedOverlapSec = predictedSec,
                        Verdict = verdict,
                        CommonCycleSec = common,
                        PartOfDay = cycles[i] is { AllDay: false } || cycles[j] is { AllDay: false },
                        SharedSchedule = shared
                    });
                }
            }

            var summaries = new List<JobOverlapSummary>();
            for (var i = 0; i < jobs.Count; i++)
            {
                var reason = !jobs[i].Enabled ? "Job is disabled"
                    : jobs[i].Schedules.All(s => !s.Enabled) ? "No enabled schedule"
                    : jobs[i].Schedules.Where(s => s.Enabled).Select(s => AgentScheduleProjector.IsProjectable(s, out var r) ? null : r).FirstOrDefault(r => r != null);
                double? busy = null;
                if (jobs[i].ExpectedDurationSec is { } d)
                {
                    busy = cycles[i] is { AllDay: true } c
                        ? Math.Min(1, (double)Math.Max(1, d) / c.PeriodSec)
                        : periodSec > 0 ? Math.Min(1, projected[i].Sum(r => r.Seconds) / periodSec) : 0;
                }
                summaries.Add(new JobOverlapSummary
                {
                    Job = jobs[i],
                    NotProjectableReason = reason,
                    ProjectedRuns = projected[i].Count,
                    ProjectionTruncated = truncated[i],
                    Cycle = cycles[i],
                    BusyFraction = busy,
                    ActualRuns = actual[i].Count,
                    ActualRunsOverlapping = actualFlags[i].Count(f => f),
                    PredictedRunsOverlapping = predictedFlags[i].Count(f => f)
                });
            }

            return new JobOverlapReport
            {
                From = from,
                To = to,
                Jobs = summaries,
                Pairs = pairs.OrderByDescending(p => p.ActualOverlaps)
                    .ThenByDescending(p => p.PredictedOverlaps)
                    .ThenBy(p => p.Verdict == CycleVerdict.Collides ? 0 : 1)
                    .ToList()
            };
        }
    }
}
