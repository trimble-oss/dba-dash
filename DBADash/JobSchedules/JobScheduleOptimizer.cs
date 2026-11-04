using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DBADash.JobSchedules
{
    public sealed record ScheduleOptimizerOptions
    {
        /// <summary>Start of the time the runs are compared over, in server time - midnight</summary>
        public DateTime From { get; init; }

        /// <summary>The furthest a job may be moved either way</summary>
        public int MaxShiftSec { get; init; } = 3600;

        /// <summary>Moves are multiples of this</summary>
        public int StepSec { get; init; } = 60;

        /// <summary>Space to leave after a run before another job starts</summary>
        public int MinGapSec { get; init; }

        /// <summary>
        /// Let jobs that share a schedule move apart: a job that moves away from the others gets a copy of the schedule of
        /// its own (<see cref="ScheduleOptimizerResult.OwnSchedule"/>).  Otherwise jobs that share a schedule move together,
        /// as moving the schedule moves them all.
        /// </summary>
        public bool SplitSharedSchedules { get; init; }

        /// <summary>
        /// A move has to save at least this much overlap a day (and <see cref="MinGainFraction"/> of the overlap it's saving
        /// from), so a schedule isn't changed - or split - for a few seconds' gain
        /// </summary>
        public int MinGainSecPerDay { get; init; } = 60;

        /// <summary>The share of a job's overlap a move has to save, as well as <see cref="MinGainSecPerDay"/></summary>
        public double MinGainFraction { get; init; } = 0.05;

        /// <summary>
        /// Jobs expected to be running at least this share of the time are left out: nothing can be moved clear of them, and
        /// the short gaps between their runs would draw moves for no real gain
        /// </summary>
        public double AlwaysRunningFraction { get; init; } = 0.9;
    }

    public sealed class ScheduleOptimizerResult
    {
        /// <summary>Seconds to move each job by, from where it is now.  Only jobs that move.</summary>
        public IReadOnlyDictionary<Guid, int> Shifts { get; init; }

        /// <summary>Jobs that need their own copy of a schedule they share to make their move.  The others sharing it keep it.</summary>
        public IReadOnlyCollection<Guid> OwnSchedule { get; init; }

        /// <summary>Jobs left out because they're running nearly all the time (<see cref="ScheduleOptimizerOptions.AlwaysRunningFraction"/>)</summary>
        public IReadOnlyCollection<Guid> Ignored { get; init; }

        /// <summary>Time the jobs' runs (with the gap) overlap over the period compared, before and after the moves</summary>
        public double ConflictSecBefore { get; init; }

        public double ConflictSecAfter { get; init; }

        /// <summary>Of <see cref="ConflictSecAfter"/>, the time between jobs still sharing a schedule - they start together, so start times can't help</summary>
        public double SharedConflictSecAfter { get; init; }

        /// <summary>Each job's overlap time with the others, before and after the moves</summary>
        public IReadOnlyDictionary<Guid, double> JobConflictSecBefore { get; init; }

        public IReadOnlyDictionary<Guid, double> JobConflictSecAfter { get; init; }

        /// <summary>The period compared: a day when every job runs every day, otherwise a week</summary>
        public TimeSpan Period { get; init; }

        /// <summary>Why a job couldn't be kept clear, and what might help, by job</summary>
        public IReadOnlyDictionary<Guid, IReadOnlyList<string>> Notes { get; init; }

        /// <summary>The moves reduce the overlap</summary>
        public bool HasBenefit => Shifts.Count > 0 && ConflictSecAfter < ConflictSecBefore - Tolerance;

        internal const double Tolerance = 0.001;
    }

    /// <summary>
    /// Suggests start times for a set of jobs so their runs overlap as little as possible, moving them as little as
    /// possible.
    ///
    /// <para>Moving a schedule's start time moves its runs by the same amount, so each candidate move is tried by sliding
    /// the job's projected runs rather than projecting them again.  The search is coordinate descent: each job in turn
    /// takes the move (in <see cref="ScheduleOptimizerOptions.StepSec"/> steps, smallest first) that most reduces its
    /// overlap with the others where they are, until a pass changes nothing.  That finds a good arrangement, not always
    /// the best one.  Then any move that no longer helps is undone, so every move left earns its place.  A move has to
    /// save a meaningful amount (<see cref="ScheduleOptimizerOptions.MinGainSecPerDay"/>,
    /// <see cref="ScheduleOptimizerOptions.MinGainFraction"/>) to be made at all.</para>
    /// <para>Jobs running nearly all the time are left out of the scoring: every other job overlaps them wherever it goes,
    /// and the brief gaps their projection has between runs would otherwise draw moves that gain nothing real.</para>
    /// <para>Jobs that share a schedule start together, and moving the schedule moves them all - so they move as one unless
    /// <see cref="ScheduleOptimizerOptions.SplitSharedSchedules"/> lets them part.  When they may, each moves on its own,
    /// then rejoins the others where that's no worse, so as few as possible need a schedule of their own.</para>
    /// <para>Pairs on fixed cycles that no start times can keep apart (the gcd rule in <see cref="JobOverlapAnalyzer"/>)
    /// get a note suggesting a cadence that would allow it.</para>
    /// </summary>
    public static class JobScheduleOptimizer
    {
        private const int MaxPasses = 10;
        private const double Tolerance = ScheduleOptimizerResult.Tolerance;

        /// <summary>Intervals offered when suggesting a different cadence: those that divide a day, from a minute to a day</summary>
        public static readonly int[] StandardCadencesSec = new[] { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60, 120, 180, 240, 360, 480, 720, 1440 }.Select(m => m * 60).ToArray();

        /// <param name="jobs">The jobs, with their schedules where they are now</param>
        /// <param name="pinned">Jobs to leave where they are</param>
        /// <param name="options">Limits on the moves</param>
        /// <param name="cancellationToken">Stops the search</param>
        public static ScheduleOptimizerResult Optimize(IReadOnlyList<JobScheduleInput> jobs, ISet<Guid> pinned, ScheduleOptimizerOptions options, CancellationToken cancellationToken = default)
        {
            var n = jobs.Count;
            var step = Math.Max(1, options.StepSec);
            var maxShift = Math.Max(0, options.MaxShiftSec) / step * step;
            var gap = TimeSpan.FromSeconds(Math.Max(0, options.MinGapSec));
            var everyDay = jobs.All(j => j.Schedules.Where(AgentScheduleShifter.IsMovable).All(AgentScheduleShifter.IsEveryDay));
            var period = everyDay ? TimeSpan.FromDays(1) : TimeSpan.FromDays(7);
            var from = options.From;
            var to = from + period;

            // Jobs that share a schedule, directly or through another job
            var sharing = SharingGroups(jobs);
            var units = options.SplitSharedSchedules
                ? Enumerable.Range(0, n).Select(i => new[] { i }).ToList()
                : Enumerable.Range(0, n).GroupBy(i => sharing[i]).Select(g => g.ToArray()).ToList();
            var unitOf = new int[n];
            for (var u = 0; u < units.Count; u++)
            {
                foreach (var i in units[u]) unitOf[i] = u;
            }

            // Projected over the period and as far again as a job can move, so a slid list still covers the period
            var pad = TimeSpan.FromSeconds(maxShift);
            var projected = jobs.Select(j => JobOverlapAnalyzer.ProjectRuns(j, from - pad, to + pad, out _)).ToList();
            // A job running nearly all the time overlaps everything wherever it's moved: leave its runs out of the scoring
            var ignored = new HashSet<int>(Enumerable.Range(0, n).Where(i =>
                projected[i].Sum(r => Math.Max(0, ((r.End < to ? r.End : to) - (r.Start > from ? r.Start : from)).TotalSeconds)) >= options.AlwaysRunningFraction * period.TotalSeconds));
            List<JobRun> WithGap(IEnumerable<JobRun> runs) => runs.Select(r => r with { End = r.End + gap }).ToList();
            var baseRuns = projected.Select((runs, i) => ignored.Contains(i) ? new List<JobRun>() : WithGap(runs)).ToList();
            // A job with runs a schedule that doesn't move starts (one-time) can't just be slid: those runs stay, and which
            // starts Agent skips can change - its runs are worked out again for each move tried
            var hasFixed = projected.Select(runs => runs.Any(r => r.Schedule != null && !AgentScheduleShifter.IsMovable(r.Schedule))).ToArray();
            var reprojected = new Dictionary<(int Job, int Shift), List<JobRun>>();

            // The job's runs over the period with its movable schedules moved by shift
            List<JobRun> RunsAt(int i, int shift)
            {
                if (!hasFixed[i] || ignored.Contains(i) || shift == 0) return Slide(baseRuns[i], shift, from, to);
                if (reprojected.TryGetValue((i, shift), out var runs)) return runs;
                var moved = new JobScheduleInput
                {
                    JobId = jobs[i].JobId,
                    Name = jobs[i].Name,
                    Enabled = jobs[i].Enabled,
                    ExpectedDurationSec = jobs[i].ExpectedDurationSec,
                    // Candidates only offers moves every movable schedule can make
                    Schedules = jobs[i].Schedules.Select(s => AgentScheduleShifter.IsMovable(s) && AgentScheduleShifter.TryShift(s, shift, out var shifted, out _, changeDays: false) ? shifted : s).ToList()
                };
                return reprojected[(i, shift)] = Slide(WithGap(JobOverlapAnalyzer.ProjectRuns(moved, from - pad, to + pad, out _)), 0, from, to);
            }
            // The least a move must save, from an overlap of conflict seconds
            double MinGain(double conflict) => Math.Max(Tolerance, Math.Max(options.MinGainSecPerDay * period.TotalDays, options.MinGainFraction * conflict));
            var candidates = units.Select(u => Candidates(u.Select(i => jobs[i]).ToList(), u.Any(i => pinned.Contains(jobs[i].JobId)), maxShift, step)).ToList();
            var current = new int[n];
            var slid = Enumerable.Range(0, n).Select(i => RunsAt(i, 0)).ToList();

            double Overlap(List<JobRun> a, List<JobRun> b) => JobOverlapAnalyzer.CountOverlaps(a, b).Seconds;

            double JobConflict(int i) => Enumerable.Range(0, n).Where(j => j != i).Sum(j => Overlap(slid[i], slid[j]));

            // A unit's overlap with jobs outside it, were it moved by shift.  Overlaps inside a unit don't change as it moves.
            double UnitConflict(int u, int shift)
            {
                double total = 0;
                foreach (var i in units[u])
                {
                    var runs = shift == current[i] ? slid[i] : RunsAt(i, shift);
                    for (var j = 0; j < n; j++)
                    {
                        if (unitOf[j] != u) total += Overlap(runs, slid[j]);
                    }
                }
                return total;
            }

            void Move(int u, int shift)
            {
                foreach (var i in units[u])
                {
                    current[i] = shift;
                    slid[i] = RunsAt(i, shift);
                }
            }

            double TotalConflict()
            {
                double total = 0;
                for (var i = 0; i < n; i++)
                {
                    for (var j = i + 1; j < n; j++) total += Overlap(slid[i], slid[j]);
                }
                return total;
            }

            var before = TotalConflict();
            var jobBefore = Enumerable.Range(0, n).ToDictionary(i => jobs[i].JobId, JobConflict);
            for (var pass = 0; pass < MaxPasses; pass++)
            {
                var improved = false;
                // The units in the most conflict move first
                foreach (var u in Enumerable.Range(0, units.Count).OrderByDescending(u => UnitConflict(u, current[units[u][0]])))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (candidates[u].Count < 2) continue;
                    var now = current[units[u][0]];
                    var best = UnitConflict(u, now);
                    if (best == 0) continue;
                    var required = best - MinGain(best);
                    var bestShift = now;
                    foreach (var shift in candidates[u])
                    {
                        if (shift == now) continue;
                        var conflict = UnitConflict(u, shift);
                        // Strictly better only, and by enough: candidates come smallest move first, so a tie keeps the smaller move
                        if (conflict <= required && conflict < best - Tolerance)
                        {
                            best = conflict;
                            bestShift = shift;
                            if (best == 0) break;
                        }
                    }
                    if (bestShift == now) continue;
                    Move(u, bestShift);
                    improved = true;
                }
                if (!improved) break;
            }

            // A move made early on can stop helping (enough) once others have moved: undo those, the biggest first
            foreach (var u in Enumerable.Range(0, units.Count).Where(u => current[units[u][0]] != 0).OrderByDescending(u => Math.Abs(current[units[u][0]])))
            {
                var unmoved = UnitConflict(u, 0);
                if (unmoved - UnitConflict(u, current[units[u][0]]) < MinGain(unmoved)) Move(u, 0);
            }

            var own = new HashSet<Guid>();
            if (options.SplitSharedSchedules)
            {
                foreach (var group in Enumerable.Range(0, n).GroupBy(i => sharing[i]).Where(g => g.Count() > 1).Select(g => g.ToList()))
                {
                    // Jobs that moved away from the others rejoin them unless staying apart saves enough, so fewer need their own schedule
                    var keeper = Keeper(group, current);
                    foreach (var i in group.Where(i => current[i] != keeper))
                    {
                        if (pinned.Contains(jobs[i].JobId) && keeper != 0) continue;
                        if (!jobs[i].Schedules.Where(AgentScheduleShifter.IsMovable).All(s => AgentScheduleShifter.TryShift(s, keeper, out _, out _, changeDays: false))) continue;
                        var joined = UnitConflict(unitOf[i], keeper);
                        if (joined - UnitConflict(unitOf[i], current[i]) < MinGain(joined)) Move(unitOf[i], keeper);
                    }
                    keeper = Keeper(group, current);
                    foreach (var i in group.Where(i => current[i] != keeper)) own.Add(jobs[i].JobId);
                }
            }

            double shared = 0;
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    if (sharing[i] == sharing[j] && current[i] == current[j]) shared += Overlap(slid[i], slid[j]);
                }
            }

            var shifts = new Dictionary<Guid, int>();
            for (var i = 0; i < n; i++)
            {
                if (current[i] != 0) shifts[jobs[i].JobId] = current[i];
            }
            var notes = CycleNotes(jobs, current, options.MinGapSec).ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var i in ignored)
            {
                notes[jobs[i].JobId] = new[] { "Running nearly all the time, so left out when suggesting start times: no start time keeps other jobs clear of it" };
            }
            return new ScheduleOptimizerResult
            {
                Shifts = shifts,
                OwnSchedule = own,
                Ignored = ignored.Select(i => jobs[i].JobId).ToList(),
                ConflictSecBefore = before,
                ConflictSecAfter = TotalConflict(),
                SharedConflictSecAfter = shared,
                JobConflictSecBefore = jobBefore,
                JobConflictSecAfter = Enumerable.Range(0, n).ToDictionary(i => jobs[i].JobId, JobConflict),
                Period = period,
                Notes = notes
            };
        }

        /// <summary>
        /// For each job, a number shared by the jobs it shares a schedule with - directly, or through another job that
        /// shares one with each.  Matched by schedule_uid on enabled schedules.
        /// </summary>
        public static int[] SharingGroups(IReadOnlyList<JobScheduleInput> jobs)
        {
            var parent = Enumerable.Range(0, jobs.Count).ToArray();
            int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
            var firstUser = new Dictionary<Guid, int>();
            for (var i = 0; i < jobs.Count; i++)
            {
                if (!jobs[i].Enabled) continue;
                foreach (var uid in jobs[i].Schedules.Where(s => s.Enabled && s.Uid.HasValue).Select(s => s.Uid!.Value))
                {
                    if (firstUser.TryGetValue(uid, out var other)) parent[Find(i)] = Find(other);
                    else firstUser[uid] = i;
                }
            }
            return Enumerable.Range(0, jobs.Count).Select(Find).ToArray();
        }

        /// <summary>The move most of the jobs sharing a schedule make - the one the schedule itself takes.  The smaller on a tie.</summary>
        private static int Keeper(List<int> group, int[] shifts) =>
            group.GroupBy(i => shifts[i]).OrderByDescending(g => g.Count()).ThenBy(g => Math.Abs(g.Key)).ThenBy(g => g.Key).First().Key;

        /// <summary>The moves to try for jobs that move together, smallest first.  Only 0 when they're pinned or can't move.</summary>
        private static List<int> Candidates(List<JobScheduleInput> jobs, bool pinned, int maxShift, int step)
        {
            var list = new List<int> { 0 };
            // Agent start, idle and one-time schedules stay where they are, so don't limit the move
            var enabled = jobs.SelectMany(j => j.Schedules.Where(AgentScheduleShifter.IsMovable)).ToList();
            if (pinned || jobs.Any(j => !j.Enabled) || jobs.Any(j => !j.Schedules.Any(AgentScheduleShifter.IsMovable))) return list;
            // A move of a whole cycle or more is the same as a smaller one - when they all repeat at the same interval
            var cycles = jobs.Select(JobOverlapAnalyzer.GetCycle).ToList();
            long? cyclePeriod = cycles.All(c => c.HasValue) && cycles.Select(c => c!.Value.PeriodSec).Distinct().Count() == 1 ? cycles[0]!.Value.PeriodSec : null;
            if (cycles.All(c => c.HasValue))
            {
                step = FineStep(step, cycles.Select(c => c!.Value.PeriodSec).Aggregate(ScheduleMath.Gcd));
            }
            var limit = cyclePeriod is { } p ? Math.Min(maxShift, (int)Math.Min(int.MaxValue, p / 2)) : maxShift;
            for (var shift = step; shift <= limit; shift += step)
            {
                foreach (var s in new[] { shift, -shift })
                {
                    if (cyclePeriod is { } cp && s == -shift && shift * 2 == cp) continue;
                    if (enabled.All(schedule => AgentScheduleShifter.TryShift(schedule, s, out _, out _, changeDays: false))) list.Add(s);
                }
            }
            return list;
        }

        /// <summary>
        /// The step to move jobs that repeat every <paramref name="periodSec"/> by.  Moves of a whole step can't change where
        /// a job runs in a cycle of a step or two - every minute, it's where it was after any whole-minute move - so within
        /// a cycle of less than 4 steps, a round number of seconds that gives at least 12 places in it.
        /// </summary>
        private static int FineStep(int step, long periodSec)
        {
            if (periodSec >= 4L * step) return step;
            foreach (var s in new[] { 30, 20, 15, 10, 5, 2 })
            {
                if (s < step && periodSec % s == 0 && periodSec / s >= 12) return s;
            }
            return 1;
        }

        /// <summary>The runs moved by <paramref name="shift"/> seconds, keeping those that touch the period</summary>
        private static List<JobRun> Slide(List<JobRun> runs, int shift, DateTime from, DateTime to)
        {
            var delta = TimeSpan.FromSeconds(shift);
            var list = new List<JobRun>(runs.Count);
            foreach (var r in runs)
            {
                var moved = new JobRun(r.Start + delta, r.End + delta);
                if (moved.End > from && moved.Start < to) list.Add(moved);
            }
            return list;
        }

        /// <summary>For pairs on fixed cycles that start times can't keep apart, why, and a cadence that would allow it</summary>
        private static IReadOnlyDictionary<Guid, IReadOnlyList<string>> CycleNotes(IReadOnlyList<JobScheduleInput> jobs, int[] shifts, int gapSec)
        {
            var notes = new Dictionary<Guid, List<string>>();
            void Add(Guid id, string note)
            {
                if (!notes.TryGetValue(id, out var list)) notes[id] = list = new List<string>();
                list.Add(note);
            }

            var cycles = jobs.Select(JobOverlapAnalyzer.GetCycle).ToList();
            for (var i = 0; i < jobs.Count; i++)
            {
                if (cycles[i] is not { } ci || jobs[i].ExpectedDurationSec is not { } di) continue;
                var needI = Math.Max(1, di) + gapSec;
                if (needI >= ci.PeriodSec)
                {
                    Add(jobs[i].JobId, $"Runs for as long as its interval ({Format(ci.PeriodSec)}), so it's always running");
                    continue;
                }
                for (var j = i + 1; j < jobs.Count; j++)
                {
                    if (cycles[j] is not { } cj || jobs[j].ExpectedDurationSec is not { } dj) continue;
                    var needJ = Math.Max(1, dj) + gapSec;
                    if (needJ >= cj.PeriodSec) continue;
                    if (needI + needJ <= ScheduleMath.Gcd(ci.PeriodSec, cj.PeriodSec)) continue;
                    var why = WhyNotApart(jobs[i].Name, ci.PeriodSec, needI, jobs[j].Name, cj.PeriodSec, needJ);
                    Add(jobs[i].JobId, $"Can't be kept apart from {jobs[j].Name} by start times: {why}");
                    Add(jobs[j].JobId, $"Can't be kept apart from {jobs[i].Name} by start times: {why}");
                }
            }
            return notes.ToDictionary(n => n.Key, n => (IReadOnlyList<string>)n.Value);
        }

        /// <summary>
        /// For two jobs on fixed cycles that start times can't keep apart: that they line up too often for both runs, and a
        /// standard interval for whichever job it changes least that would allow it - "they line up every 30 minutes and
        /// together need 35m.  Running B every 20 minutes instead of every 30 minutes would allow it"
        /// </summary>
        /// <param name="needA">A's run time, with any gap to leave after it</param>
        public static string WhyNotApart(string nameA, long periodA, long needA, string nameB, long periodB, long needB)
        {
            var text = $"they line up every {Format(ScheduleMath.Gcd(periodA, periodB))} and together need {ScheduleMath.Span(needA + needB)}";
            var forB = SuggestCadence(periodA, needA, periodB, needB);
            var forA = SuggestCadence(periodB, needB, periodA, needA);
            var changeB = forB.HasValue && (!forA.HasValue || Math.Abs(forB.Value - periodB) / (double)periodB <= Math.Abs(forA.Value - periodA) / (double)periodA);
            return text + (changeB ? $".  Running {nameB} every {Format(forB!.Value)} instead of every {Format(periodB)} would allow it"
                : forA.HasValue ? $".  Running {nameA} every {Format(forA.Value)} instead of every {Format(periodA)} would allow it"
                : "");
        }

        /// <summary>
        /// A standard interval for the job that runs every <paramref name="periodSec"/> so it can be kept apart from the
        /// other job: the closest to its interval now, the less frequent on a tie.  Null when none would do.
        /// </summary>
        /// <param name="otherPeriodSec">The other job's interval</param>
        /// <param name="otherNeedSec">The other job's run time with the gap</param>
        /// <param name="periodSec">The job's interval now</param>
        /// <param name="needSec">The job's run time with the gap</param>
        public static int? SuggestCadence(long otherPeriodSec, long otherNeedSec, long periodSec, long needSec) =>
            StandardCadencesSec
                .Where(p => p != periodSec && p > needSec && ScheduleMath.Gcd(otherPeriodSec, p) >= otherNeedSec + needSec)
                .OrderBy(p => Math.Abs(p - periodSec))
                .ThenByDescending(p => p)
                .Select(p => (int?)p)
                .FirstOrDefault();

        /// <summary>An interval, as in "every 5 minutes"</summary>
        private static string Format(long seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (seconds % 3600 == 0) return seconds == 3600 ? "hour" : $"{ts.TotalHours:0} hours";
            if (seconds % 60 == 0) return seconds == 60 ? "minute" : $"{ts.TotalMinutes:0} minutes";
            return $"{seconds} seconds";
        }
    }
}
