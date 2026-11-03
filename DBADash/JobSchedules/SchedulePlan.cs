using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.JobSchedules
{
    /// <summary>Where runs of previewed jobs overlap: per job and run, the other jobs it overlaps and when, and the pairs in all</summary>
    /// <param name="With">Per job, per run: the names of the jobs it overlaps.  Null for a run that overlaps none.</param>
    /// <param name="Segments">Per job, per run: when it overlaps another job.  Null for a run that overlaps none.</param>
    /// <param name="Count">Pairs of runs that overlap</param>
    public sealed record PlanOverlaps(List<List<string>[]> With, List<List<(DateTime Start, DateTime End)>[]> Segments, int Count);

    /// <summary>Suggested start times for the included jobs, before they're applied to the plan</summary>
    /// <param name="Included">The jobs the suggestion is for</param>
    /// <param name="Result">The suggestion</param>
    /// <param name="Alternative">The suggestion with copies of shared schedules allowed, when they weren't and might help.  Null otherwise.</param>
    /// <param name="Tied">Jobs sharing a schedule with a job that isn't included - moving them would move that job too</param>
    public sealed record PlanSuggestion(IReadOnlyList<JobScheduleInput> Included, ScheduleOptimizerResult Result, ScheduleOptimizerResult Alternative, IReadOnlySet<Guid> Tied);

    /// <summary>What applying a suggestion changed</summary>
    /// <param name="Changed">Jobs moved or given their own schedule</param>
    /// <param name="OwnSchedule">Jobs given their own copy of a shared schedule</param>
    /// <param name="Refused">Moves that couldn't be made: the job's name and why</param>
    public sealed record AppliedSuggestion(IReadOnlyList<Guid> Changed, IReadOnlyCollection<Guid> OwnSchedule, IReadOnlyList<string> Refused);

    /// <summary>How a pair's overlap changes with the plan's moves</summary>
    public enum PairChange
    {
        Same,

        /// <summary>Didn't overlap before the moves, does now</summary>
        New,

        /// <summary>Overlapped before the moves, doesn't now</summary>
        Cleared,

        More,
        Less
    }

    /// <summary>Overlaps and how much (seconds) of a count of pairs of runs</summary>
    public readonly record struct OverlapAmount(int Count, double Seconds)
    {
        public bool Any => Count > 0;
    }

    /// <summary>
    /// Where two previewed jobs overlap, with the plan's moves and before them: in the runs that happened in the range
    /// (moved as planned), and in the runs the schedules predict over it.  Predicted is null when either job has no
    /// expected duration.
    /// </summary>
    public sealed record PlanPairOverlap(JobScheduleInput A, JobScheduleInput B, OverlapAmount Actual, OverlapAmount ActualBefore, OverlapAmount? Predicted, OverlapAmount? PredictedBefore)
    {
        private double After => Actual.Seconds + (Predicted?.Seconds ?? 0);
        private double Before => ActualBefore.Seconds + (PredictedBefore?.Seconds ?? 0);

        public PairChange Change =>
            !ActualBefore.Any && !(PredictedBefore?.Any ?? false) && (Actual.Any || (Predicted?.Any ?? false)) ? PairChange.New
            : (ActualBefore.Any || (PredictedBefore?.Any ?? false)) && !Actual.Any && !(Predicted?.Any ?? false) ? PairChange.Cleared
            : After > Before + 0.5 ? PairChange.More
            : After < Before - 0.5 ? PairChange.Less
            : PairChange.Same;
    }

    /// <summary>
    /// What a move is kept against: a schedule by schedule_uid (<paramref name="CopyFor"/> empty), a job's own copy of a
    /// shared one (<paramref name="CopyFor"/> the job), or - with no schedule_uid - the job's schedule by
    /// <paramref name="Name"/>
    /// </summary>
    internal readonly record struct ScheduleKey(Guid Uid, Guid CopyFor, string Name);

    /// <summary>A plan's moves and copies at one moment - an undo step</summary>
    public sealed class PlanState
    {
        internal PlanState(Dictionary<ScheduleKey, int> shifts, HashSet<Guid> ownSchedule, HashSet<Guid> autoOwn)
        {
            Shifts = shifts;
            OwnSchedule = ownSchedule;
            AutoOwn = autoOwn;
        }

        internal IReadOnlyDictionary<ScheduleKey, int> Shifts { get; }
        internal IReadOnlySet<Guid> OwnSchedule { get; }
        internal IReadOnlySet<Guid> AutoOwn { get; }

        /// <summary>Jobs moved or given their own copy</summary>
        public bool HasChanges => Shifts.Count > 0 || OwnSchedule.Count > 0;

        /// <summary>The same moves and copies - nothing to undo between them</summary>
        public bool SameAs(PlanState other) =>
            other != null && Shifts.Count == other.Shifts.Count && Shifts.All(kv => other.Shifts.TryGetValue(kv.Key, out var v) && v == kv.Value)
            && OwnSchedule.SetEquals(other.OwnSchedule) && AutoOwn.SetEquals(other.AutoOwn);
    }

    /// <summary>A job's schedules as DBA Dash collected them at <paramref name="Collected"/> (server time) - one version of its scripted definition</summary>
    public sealed record ScheduleVersion(DateTime Collected, IReadOnlyList<AgentSchedule> Schedules);

    /// <summary>A schedule whose timing changed: as it was (null for one added) and as it is now (null for one gone or disabled)</summary>
    public sealed record ScheduleDifference(AgentSchedule Was, AgentSchedule Now);

    /// <summary>
    /// A job's schedules changed: the runs before <paramref name="Until"/> (server time; MaxValue for all of them) ran to
    /// <paramref name="Schedules"/> as they were.  <paramref name="Inferred"/> when there were no collected definitions to
    /// compare, and runs that don't fit the schedules now gave it away - so which schedules changed isn't known.
    /// </summary>
    public sealed record JobScheduleChange(DateTime Until, IReadOnlyList<ScheduleDifference> Schedules, bool Inferred);

    /// <summary>
    /// A plan of moves to agent job schedules: which jobs are previewed, how far each is moved, which get their own copy of
    /// a schedule they share, and the scripts to make it so.  Nothing is changed on the server.
    ///
    /// <para>Moves are kept per schedule (by schedule_uid), so a schedule shared by several jobs moves for all of them -
    /// unless a job is given its own copy, when its move is kept apart from the shared schedule's.  Only the jobs that ran
    /// in the range (<see cref="Jobs"/>) are previewed and moved, but every job (<see cref="AllJobs"/>) counts when
    /// deciding which schedules are shared.</para>
    /// <para>A job moves as a whole (<see cref="TryMoveJob"/>, <see cref="TryMoveJobBy"/>): all its movable schedules
    /// (<see cref="AgentScheduleShifter.IsMovable"/>) by the same amount - or one of them on its own
    /// (<see cref="TryMoveSchedule"/>), which can make a job's own schedules clash (<see cref="ClashesAdded"/>).  Its agent
    /// start, CPU idle, one-time and disabled schedules stay as they are and don't stop the move.  Of the runs that happened,
    /// only those a movable schedule started move, by that schedule's move (<see cref="AttributeRuns"/>) - one started by
    /// hand, at agent start, or that can't be tied to a schedule stays where it ran.</para>
    /// <para>The runs that happened only show what a job's schedules do now if they haven't changed since.  Before they last
    /// changed (<see cref="FindScheduleChange"/>) - moved, more or less often, other days - the runs they predict now are
    /// shown instead, with the runs that happened kept to show in outline (<see cref="ReplacedRuns"/>).</para>
    /// </summary>
    public sealed class SchedulePlan
    {
        /// <summary>How long after a schedule's time a run can start and still be taken as started by it, when its history doesn't say</summary>
        public const int StartMatchToleranceSec = 60;

        /// <summary>Seconds each schedule is moved by, keyed by <see cref="KeyOf"/></summary>
        private Dictionary<ScheduleKey, int> _shifts = new();

        /// <summary>schedule_uids of schedules more than one job uses</summary>
        private readonly HashSet<Guid> _sharedUids = new();

        /// <summary>
        /// The runs <see cref="ActualRuns"/> and <see cref="PredictedRuns"/> worked out, by job, range and the job's moves - the
        /// editor asks for the same ones over and over as a job is dragged
        /// </summary>
        private readonly Dictionary<(Guid JobId, bool Actual, DateTime From, DateTime To, string Moves), IReadOnlyList<JobRun>> _runCache = new();

        private const int MaxCachedRunLists = 5000;

        private HashSet<Guid> _ownSchedule = new();

        /// <summary>Jobs given their own copy only to make a manual move - dropped again if the job goes back to the shared schedule's time</summary>
        private HashSet<Guid> _autoOwn = new();

        private IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> _actualRuns = new Dictionary<Guid, IReadOnlyList<JobRun>>();

        /// <summary>Jobs whose schedules changed over the runs, by job</summary>
        private readonly Dictionary<Guid, JobScheduleChange> _changes = new();

        /// <summary>The runs and history <see cref="SetJobs"/> last matched to schedules</summary>
        private object _givenRuns;

        private object _givenHistory = new();

        /// <summary>Every job, with its schedules as they are</summary>
        public IReadOnlyList<JobScheduleInput> AllJobs { get; private set; } = Array.Empty<JobScheduleInput>();

        /// <summary>The jobs that ran in the range - the ones previewed and moved</summary>
        public IReadOnlyList<JobScheduleInput> Jobs { get; private set; } = Array.Empty<JobScheduleInput>();

        /// <summary>Jobs previewed</summary>
        public HashSet<Guid> Included { get; } = new();

        /// <summary>Jobs to leave where they are when suggesting start times</summary>
        public HashSet<Guid> Pinned { get; } = new();

        /// <summary>Jobs to give their own copy of the schedules they share, so they can move apart from the other jobs</summary>
        public IReadOnlyCollection<Guid> OwnSchedule => _ownSchedule;

        /// <summary>The previewed jobs, in <see cref="Jobs"/> order</summary>
        public List<JobScheduleInput> IncludedJobs => Jobs.Where(j => Included.Contains(j.JobId)).ToList();

        /// <summary>
        /// New data, or the same with a different expected duration: keeps the moves, and the previewed and pinned jobs
        /// that are still there.
        /// </summary>
        /// <param name="allJobs">Every job</param>
        /// <param name="actualRuns">What each job ran in the range, in start order.  Jobs without an entry didn't run.</param>
        /// <param name="history">Each job's collected definitions over the runs, to tell when its schedules changed.  Null or missing for a job to go by its runs alone.</param>
        public void SetJobs(IReadOnlyList<JobScheduleInput> allJobs, IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> actualRuns,
            IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleVersion>> history = null)
        {
            AllJobs = allJobs;
            // Expected durations, and so predictions, may have changed
            _runCache.Clear();
            // Which schedule started each run, and when schedules changed, don't depend on how long jobs take: given the same
            // runs and history again (another duration basis), keep what was worked out - matching every run is the costly part
            if (!ReferenceEquals(actualRuns, _givenRuns) || !ReferenceEquals(history, _givenHistory))
            {
                _givenRuns = actualRuns;
                _givenHistory = history;
                var byId = allJobs.ToDictionary(j => j.JobId);
                _actualRuns = actualRuns.ToDictionary(r => r.Key, r => byId.TryGetValue(r.Key, out var job) ? AttributeRuns(job, r.Value) : r.Value);
                _changes.Clear();
                foreach (var (jobId, runs) in _actualRuns)
                {
                    if (!byId.TryGetValue(jobId, out var job)) continue;
                    if (FindScheduleChange(job, runs, history?.GetValueOrDefault(jobId)) is { } change) _changes[jobId] = change;
                }
            }
            Jobs = allJobs.Where(j => actualRuns.ContainsKey(j.JobId)).ToList();
            Included.IntersectWith(Jobs.Select(j => j.JobId));
            _sharedUids.Clear();
            _sharedUids.UnionWith(allJobs
                .SelectMany(j => j.Schedules.Where(s => s.Uid.HasValue).Select(s => (Uid: s.Uid!.Value, j.JobId)))
                .Distinct()
                .GroupBy(x => x.Uid)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key));
            _ownSchedule.RemoveWhere(id => !Jobs.Any(j => j.JobId == id && SharesSchedule(j)));
            _autoOwn.IntersectWith(_ownSchedule);
        }

        /// <summary>Preview the jobs most involved in overlaps, up to <paramref name="max"/></summary>
        public void IncludeMostOverlapping(JobOverlapReport report, int max)
        {
            var score = new Dictionary<Guid, int>();
            var inRange = Jobs.Select(j => j.JobId).ToHashSet();
            foreach (var p in report.Pairs.Where(p => inRange.Contains(p.A.JobId) && inRange.Contains(p.B.JobId) && (p.PredictedOverlaps > 0 || p.ActualOverlaps > 0 || p.Verdict is CycleVerdict.Collides or CycleVerdict.CannotSeparate)))
            {
                var weight = p.PredictedOverlaps + p.ActualOverlaps + 1;
                score[p.A.JobId] = score.GetValueOrDefault(p.A.JobId) + weight;
                score[p.B.JobId] = score.GetValueOrDefault(p.B.JobId) + weight;
            }
            foreach (var id in score.OrderByDescending(s => s.Value).Take(max).Select(s => s.Key)) Included.Add(id);
        }

        /// <summary>Put every job back to its current start time and schedule</summary>
        public void Reset()
        {
            _shifts.Clear();
            _ownSchedule.Clear();
            _autoOwn.Clear();
        }

        /// <summary>The moves and copies as they are, for <see cref="Restore"/> to put back - an undo step</summary>
        public PlanState Snapshot() => new(new Dictionary<ScheduleKey, int>(_shifts), new HashSet<Guid>(_ownSchedule), new HashSet<Guid>(_autoOwn));

        /// <summary>Put the moves and copies back as <see cref="Snapshot"/> took them</summary>
        public void Restore(PlanState state)
        {
            _shifts = new Dictionary<ScheduleKey, int>(state.Shifts);
            _ownSchedule = new HashSet<Guid>(state.OwnSchedule);
            _autoOwn = new HashSet<Guid>(state.AutoOwn);
            // Jobs that left since the snapshot was taken can't keep a copy
            _ownSchedule.RemoveWhere(id => !Jobs.Any(j => j.JobId == id && SharesSchedule(j)));
            _autoOwn.IntersectWith(_ownSchedule);
        }

        public JobScheduleInput Job(Guid jobId) => Jobs.FirstOrDefault(j => j.JobId == jobId);

        public bool IsShared(AgentSchedule schedule) => schedule.Uid is { } uid && _sharedUids.Contains(uid);

        public bool SharesSchedule(JobScheduleInput job) => job.Schedules.Any(IsShared);

        /// <summary>The other jobs (of all of them) that use <paramref name="schedule"/></summary>
        public List<string> SharedWith(JobScheduleInput job, AgentSchedule schedule) =>
            schedule.Uid is not { } uid ? new List<string>() : AllJobs.Where(o => o != job && o.Schedules.Any(s => s.Uid == uid)).Select(o => o.Name).ToList();

        /// <summary>The job is to get its own copy of this schedule, which it shares</summary>
        public bool HasOwnCopy(Guid jobId, AgentSchedule schedule) => _ownSchedule.Contains(jobId) && IsShared(schedule);

        /// <summary>The schedule's move: shared by the jobs that use it, unless this job is getting its own copy</summary>
        private ScheduleKey KeyOf(Guid jobId, AgentSchedule schedule) =>
            schedule.Uid is not { } uid ? new ScheduleKey(Guid.Empty, jobId, schedule.Name)
            : HasOwnCopy(jobId, schedule) ? new ScheduleKey(uid, jobId, null)
            : SharedKey(schedule);

        private static ScheduleKey SharedKey(AgentSchedule schedule) => new(schedule.Uid!.Value, Guid.Empty, null);

        private static ScheduleKey OwnKey(Guid jobId, AgentSchedule schedule) => new(schedule.Uid!.Value, jobId, null);

        /// <summary>A schedule_uid for the job's copy of a shared schedule, so the copy doesn't count as shared - the same each time it's asked for</summary>
        private static Guid CopyUid(Guid uid, Guid jobId)
        {
            var a = uid.ToByteArray();
            var b = jobId.ToByteArray();
            for (var i = 0; i < a.Length; i++) a[i] ^= b[i];
            return new Guid(a);
        }

        /// <summary>
        /// Give the job its own copy of the schedules it shares, starting where the shared ones are - or put it back on the
        /// shared ones, where they are.
        /// </summary>
        public void SetOwnSchedule(JobScheduleInput job, bool own)
        {
            // Chosen rather than made for a move
            _autoOwn.Remove(job.JobId);
            if (own == _ownSchedule.Contains(job.JobId) || !SharesSchedule(job)) return;
            var shared = job.Schedules.Where(IsShared).ToList();
            if (own)
            {
                foreach (var schedule in shared)
                {
                    if (_shifts.TryGetValue(SharedKey(schedule), out var seconds)) _shifts[OwnKey(job.JobId, schedule)] = seconds;
                }
                _ownSchedule.Add(job.JobId);
                return;
            }
            // Back to where the shared schedules are - its other schedules stay where they've been moved to
            foreach (var schedule in shared) _shifts.Remove(OwnKey(job.JobId, schedule));
            _ownSchedule.Remove(job.JobId);
        }

        /// <summary>
        /// Move the job by <paramref name="seconds"/> from where it is now, as dragged or typed.  With <paramref name="ownCopy"/>,
        /// a job sharing a schedule gets its own copy so it moves on its own (the other jobs stay put) - a copy that's dropped
        /// again if the job is moved back to the shared schedule's time.  Without, the shared schedule moves, and every job
        /// using it with it.
        /// </summary>
        public bool TryMoveJob(JobScheduleInput job, int seconds, bool ownCopy, out string reason) =>
            WithCopyIfNeeded(job, ownCopy && SharesSchedule(job), () => TrySetShift(job, seconds, out var r) ? null : r, out reason);

        /// <summary>
        /// Move the job's movable schedules <paramref name="delta"/> further from where each is now, keeping any moved apart
        /// that distance apart - a drag of the whole job.  <paramref name="ownCopy"/> as for <see cref="TryMoveJob"/>.
        /// </summary>
        public bool TryMoveJobBy(JobScheduleInput job, int delta, bool ownCopy, out string reason) =>
            // Taken once any copy is made, which starts where the shared schedule is
            WithCopyIfNeeded(job, ownCopy && SharesSchedule(job), () => TryMoveBy(job, delta, new Dictionary<ScheduleKey, int>(_shifts), out var r) ? null : r, out reason);

        /// <summary>
        /// Move one of the job's schedules to <paramref name="seconds"/> from where it is now, leaving its other schedules
        /// where they are - a drag of one of its runs.  <paramref name="ownCopy"/> as for <see cref="TryMoveJob"/>, for a
        /// shared schedule.  The move kept is the smallest that runs it at the same times.
        /// </summary>
        public bool TryMoveSchedule(JobScheduleInput job, AgentSchedule schedule, int seconds, bool ownCopy, out string reason)
        {
            if (!job.Enabled || !AgentScheduleShifter.IsMovable(schedule))
            {
                reason = job.Enabled ? $"Schedule {schedule.Name} {FixedReason(schedule)}" : CantMoveReason(job);
                return false;
            }
            return WithCopyIfNeeded(job, ownCopy && IsShared(schedule), () =>
            {
                var shift = AgentScheduleShifter.EquivalentShift(new[] { schedule }, seconds);
                if (!AgentScheduleShifter.TryShift(schedule, shift, out _, out var r)) return r;
                var key = KeyOf(job.JobId, schedule);
                if (shift == 0) _shifts.Remove(key);
                else _shifts[key] = shift;
                return null;
            }, out reason);
        }

        /// <summary>
        /// Make a move (<paramref name="move"/> returns why it can't, or null) - first giving the job its own copy of the
        /// schedules it shares when <paramref name="copy"/> and it hasn't one, a copy that's dropped again when the move
        /// fails or puts the copy back where the shared schedule is.
        /// </summary>
        private bool WithCopyIfNeeded(JobScheduleInput job, bool copy, Func<string> move, out string reason)
        {
            var made = copy && !_ownSchedule.Contains(job.JobId);
            if (made) SetOwnSchedule(job, true);
            reason = move();
            if (reason != null)
            {
                if (made) SetOwnSchedule(job, false);
                return false;
            }
            if (made) _autoOwn.Add(job.JobId);
            if (_autoOwn.Contains(job.JobId) && job.Schedules.Where(IsShared).All(s => _shifts.GetValueOrDefault(OwnKey(job.JobId, s)) == _shifts.GetValueOrDefault(SharedKey(s))))
            {
                SetOwnSchedule(job, false);
            }
            return true;
        }

        /// <summary>Any of the job's schedules is moved - by its own move, or with another job on a schedule they share</summary>
        public bool IsMoved(JobScheduleInput job) => job.Schedules.Any(s => s.Enabled && _shifts.ContainsKey(KeyOf(job.JobId, s)));

        /// <summary>How far the job has been moved: its first movable schedule's shift - all of them, unless they've been moved apart</summary>
        public int JobShift(JobScheduleInput job)
        {
            var first = job.Schedules.FirstOrDefault(AgentScheduleShifter.IsMovable);
            return first != null ? ScheduleShift(job, first) : 0;
        }

        /// <summary>How far one of the job's schedules has been moved - 0 for one that doesn't move</summary>
        public int ScheduleShift(JobScheduleInput job, AgentSchedule schedule) =>
            AgentScheduleShifter.IsMovable(schedule) ? _shifts.GetValueOrDefault(KeyOf(job.JobId, schedule)) : 0;

        /// <summary>The job's move: "+5m", or when its schedules have been moved apart, those moved - "Nightly +10m"</summary>
        public string DescribeShift(JobScheduleInput job)
        {
            var moves = MovableSchedules(job).Select(s => (s.Name, Seconds: ScheduleShift(job, s))).ToList();
            return moves.Select(m => m.Seconds).Distinct().Count() <= 1
                ? ScheduleMath.Shift(moves.FirstOrDefault().Seconds)
                : string.Join(", ", moves.Where(m => m.Seconds != 0).Select(m => $"{m.Name} {ScheduleMath.Shift(m.Seconds)}"));
        }

        /// <summary>
        /// Starts the moves lose to the job's own schedules in the range, as predicted: two of its schedules moved so one is
        /// due while a run the other started is still going, or at the same time - Agent doesn't start a job that's running.
        /// 0 when the moves lose none (or win some back).
        /// </summary>
        public int ClashesAdded(JobScheduleInput job, DateTime from, DateTime to)
        {
            if (!IsMoved(job) || job.Schedules.Count(s => s.Enabled) < 2) return 0;
            JobOverlapAnalyzer.ProjectRuns(job, from, to, out _, out var before);
            JobOverlapAnalyzer.ProjectRuns(ShiftedJob(job), from, to, out _, out var after);
            return Math.Max(0, after - before);
        }

        /// <summary>The schedules moving the job moves - see <see cref="AgentScheduleShifter.IsMovable"/></summary>
        public static List<AgentSchedule> MovableSchedules(JobScheduleInput job) => job.Schedules.Where(AgentScheduleShifter.IsMovable).ToList();

        /// <summary>
        /// The job with its schedules moved as planned.  A schedule the job is getting its own copy of has a schedule_uid of
        /// its own, so it no longer counts as shared.
        /// </summary>
        public JobScheduleInput ShiftedJob(JobScheduleInput job) => new()
        {
            JobId = job.JobId,
            Name = job.Name,
            Enabled = job.Enabled,
            ExpectedDurationSec = job.ExpectedDurationSec,
            Schedules = job.Schedules.Select(s =>
            {
                var moved = s.Enabled && _shifts.TryGetValue(KeyOf(job.JobId, s), out var seconds) && AgentScheduleShifter.TryShift(s, seconds, out var shifted, out _)
                    ? shifted
                    : s;
                return HasOwnCopy(job.JobId, s) ? moved with { ScheduleUid = CopyUid(s.Uid!.Value, job.JobId).ToString() } : moved;
            }).ToList()
        };

        /// <summary>
        /// Move all of the job's movable schedules by <paramref name="seconds"/> from where they are now - or none of them.
        /// Its other schedules (agent start, idle, one-time, disabled) stay as they are.  The move kept is the smallest that
        /// runs them at the same times (<see cref="AgentScheduleShifter.EquivalentShift"/>): every 15 minutes all day, +20m
        /// is kept as +5m - the start time the script sets.
        /// </summary>
        public bool TrySetShift(JobScheduleInput job, int seconds, out string reason)
        {
            var movable = MovableSchedules(job);
            if (!job.Enabled || movable.Count == 0)
            {
                reason = CantMoveReason(job);
                return false;
            }
            seconds = AgentScheduleShifter.EquivalentShift(movable, seconds);
            foreach (var schedule in movable)
            {
                if (!AgentScheduleShifter.TryShift(schedule, seconds, out _, out reason))
                {
                    reason = (movable.Count > 1 ? $"Schedule {schedule.Name}: " : "") + reason;
                    return false;
                }
            }
            foreach (var schedule in movable)
            {
                if (seconds == 0) _shifts.Remove(KeyOf(job.JobId, schedule));
                else _shifts[KeyOf(job.JobId, schedule)] = seconds;
            }
            reason = null;
            return true;
        }

        /// <summary>
        /// Move each of the job's movable schedules <paramref name="delta"/> further than <paramref name="before"/> has it - or
        /// none of them.  Unlike <see cref="TrySetShift"/>, which puts them all at one shift, schedules at different shifts
        /// keep their distance apart.  Each move kept is the smallest that runs the schedule at the same times.
        /// </summary>
        private bool TryMoveBy(JobScheduleInput job, int delta, IReadOnlyDictionary<ScheduleKey, int> before, out string reason)
        {
            var movable = MovableSchedules(job);
            if (!job.Enabled || movable.Count == 0)
            {
                reason = CantMoveReason(job);
                return false;
            }
            var targets = movable
                .Select(s => (Schedule: s, Key: KeyOf(job.JobId, s)))
                .Select(t => (t.Schedule, t.Key, Seconds: AgentScheduleShifter.EquivalentShift(new[] { t.Schedule }, before.GetValueOrDefault(t.Key) + delta)))
                .ToList();
            foreach (var t in targets)
            {
                if (!AgentScheduleShifter.TryShift(t.Schedule, t.Seconds, out _, out reason))
                {
                    reason = (movable.Count > 1 ? $"Schedule {t.Schedule.Name}: " : "") + reason;
                    return false;
                }
            }
            foreach (var t in targets)
            {
                if (t.Seconds == 0) _shifts.Remove(t.Key);
                else _shifts[t.Key] = t.Seconds;
            }
            reason = null;
            return true;
        }

        private static string CantMoveReason(JobScheduleInput job) =>
            !job.Enabled ? "The job is disabled"
            : job.Schedules.All(s => !s.Enabled) ? "It has no enabled schedule"
            : "None of its schedules run at a time of day: " + string.Join(", ", job.Schedules.Where(s => s.Enabled).Select(s => $"{s.Name} ({FixedReason(s)})"));

        /// <summary>
        /// What the job ran that touches the range, in start order: moved as planned (<paramref name="moved"/>) or as it is
        /// now.  A run a movable schedule started moves by that schedule's shift (<see cref="JobRun.MovedBySec"/>); others
        /// stay where they ran.  Before the job's schedules last changed (<see cref="ScheduleChangeOf"/>) the runs that
        /// happened don't show what its schedules do now, so the runs they predict are shown instead
        /// (<see cref="JobRun.Predicted"/>) - and the runs that happened are left to <see cref="ReplacedRuns"/>.  Runs a user,
        /// an alert or agent start started are kept: they weren't down to the schedule.  Shared with later calls for the same
        /// job, range and moves, so not to be changed.
        /// </summary>
        public IReadOnlyList<JobRun> ActualRuns(JobScheduleInput job, bool moved, DateTime from, DateTime to) =>
            Cached(job, true, moved, from, to, () => WorkOutActualRuns(job, moved, from, to));

        /// <summary>The runs the job's schedules predict, with its planned moves (<paramref name="moved"/>) or as they are.  Shared, like <see cref="ActualRuns"/>.</summary>
        public IReadOnlyList<JobRun> PredictedRuns(JobScheduleInput job, bool moved, DateTime from, DateTime to) =>
            Cached(job, false, moved, from, to, () => Predict(job, moved, from, to));

        /// <summary>
        /// The runs the job's schedules predict, moved as planned or not, each with the job's own schedule that starts it
        /// (not the moved copy) and how far that's moved
        /// </summary>
        private List<JobRun> Predict(JobScheduleInput job, bool moved, DateTime from, DateTime to)
        {
            if (!moved) return JobOverlapAnalyzer.ProjectRuns(job, from, to, out _);
            // ShiftedJob keeps the schedules in order
            var shifted = ShiftedJob(job);
            var original = new Dictionary<object, AgentSchedule>(ReferenceEqualityComparer.Instance);
            for (var i = 0; i < job.Schedules.Count; i++) original[shifted.Schedules[i]] = job.Schedules[i];
            return JobOverlapAnalyzer.ProjectRuns(shifted, from, to, out _)
                .Select(r => original.TryGetValue(r.Schedule, out var s) ? r with { Schedule = s, MovedBySec = ScheduleShift(job, s) } : r)
                .ToList();
        }

        private IReadOnlyList<JobRun> Cached(JobScheduleInput job, bool actual, bool moved, DateTime from, DateTime to, Func<IReadOnlyList<JobRun>> workOut)
        {
            // A job's runs depend only on the moves of its own schedules
            var moves = moved ? string.Join(",", job.Schedules.Select(s => s.Enabled ? _shifts.GetValueOrDefault(KeyOf(job.JobId, s)) : 0)) : "";
            var key = (job.JobId, actual, from, to, moves);
            if (_runCache.TryGetValue(key, out var runs)) return runs;
            if (_runCache.Count >= MaxCachedRunLists) _runCache.Clear();
            return _runCache[key] = workOut();
        }

        private List<JobRun> WorkOutActualRuns(JobScheduleInput job, bool moved, DateTime from, DateTime to)
        {
            if (!_actualRuns.TryGetValue(job.JobId, out var actual)) return new List<JobRun>();
            var until = _changes.TryGetValue(job.JobId, out var change) ? change.Until : DateTime.MinValue;
            var runs = actual
                .Where(r => r.Start >= until || IsNotScheduled(r))
                .Select(r =>
                {
                    var seconds = moved && r.Schedule != null ? ScheduleShift(job, r.Schedule) : 0;
                    return r with { Start = r.Start.AddSeconds(seconds), End = r.End.AddSeconds(seconds), MovedBySec = seconds };
                });
            if (until > from)
            {
                var predicted = Predict(job, moved, from, until < to ? until : to).Select(r => r with { Predicted = true });
                runs = runs.Concat(predicted);
            }
            return runs
                .Where(r => r.End > from && r.Start < to)
                .OrderBy(r => r.Start)
                .ToList();
        }

        /// <summary>The runs that happened before the job's schedules last changed, as they ran - shown in outline where the runs its schedules predict now are shown</summary>
        public List<JobRun> ReplacedRuns(JobScheduleInput job, DateTime from, DateTime to)
        {
            if (!_actualRuns.TryGetValue(job.JobId, out var actual) || !_changes.TryGetValue(job.JobId, out var change)) return new List<JobRun>();
            return actual.Where(r => r.Start < change.Until && !IsNotScheduled(r) && r.End > from && r.Start < to).ToList();
        }

        /// <summary>When the job's schedules last changed, and how - null when they're as they were over the runs</summary>
        public JobScheduleChange ScheduleChangeOf(JobScheduleInput job) => _changes.GetValueOrDefault(job.JobId);

        /// <summary>
        /// When the job's schedules last changed to what they are now - so a run before then didn't run to them - and which
        /// schedules changed.  From its collected definitions (<paramref name="versions"/>): the change came between the last
        /// version that differs and the one after it, and the runs between them can show where.  A last version that differs
        /// means the schedules changed after the runs.  Without versions, runs their history says a schedule started that it
        /// wouldn't start now show it's changed since (<see cref="JobScheduleChange.Inferred"/>) - which misses a schedule made
        /// more frequent, as the runs that happened still fit it.  Null when they haven't changed.
        /// </summary>
        public static JobScheduleChange FindScheduleChange(JobScheduleInput job, IReadOnlyList<JobRun> runs, IReadOnlyList<ScheduleVersion> versions)
        {
            if (versions is { Count: > 0 })
            {
                var sorted = versions.OrderBy(v => v.Collected).ToList();
                var k = sorted.FindLastIndex(v => TimingDifferences(v.Schedules, job.Schedules).Count > 0);
                if (k < 0) return null;
                var differences = TimingDifferences(sorted[k].Schedules, job.Schedules);
                if (k == sorted.Count - 1) return new JobScheduleChange(DateTime.MaxValue, differences, false);
                var (after, before) = (sorted[k].Collected, sorted[k + 1].Collected);
                // Only a run that fits one and not the other tells them apart: made more often, the old runs fit the new
                // schedule too.  When a run only the new schedule explains shows it, the change came just before it - the
                // run before it fits both - and after the last run only the old schedule explains.  Otherwise by the time
                // the new version was collected: runs that fit both look the same either way.
                var old = sorted[k].Schedules.Where(AgentScheduleShifter.IsMovable).ToList();
                var now = MovableSchedules(job);
                var between = runs.Where(r => r.Start > after && r.Start <= before && !IsNotScheduled(r)).OrderBy(r => r.Start).ToList();
                var lastOld = between.FindLastIndex(r => Fits(old, r.Start) && !Fits(now, r.Start));
                var firstNew = between.FindIndex(lastOld + 1, r => Fits(now, r.Start) && !Fits(old, r.Start));
                var boundary = firstNew >= 0 ? between[Math.Max(lastOld + 1, firstNew - 1)].Start : before;
                return new JobScheduleChange(boundary, differences, false);
            }
            // Without versions: the runs a schedule the job moves started (as their history names it) that it wouldn't start
            // now.  A one-time schedule's run isn't one, and a run that started late is a one-off: a change puts most of the
            // runs before it out of step, so it takes two or more, most of those up to the last
            var movable = MovableSchedules(job);
            var named = runs
                .Select(r => (Run: r, Schedule: NamedSchedule(job, r)))
                .Where(x => x.Schedule != null && movable.Contains(x.Schedule))
                .OrderBy(x => x.Run.Start)
                .ToList();
            var fits = named.Select(x => StartedBy(x.Schedule, x.Run.Start)).ToList();
            var last = fits.FindLastIndex(f => !f);
            if (last < 0) return null;
            var misfits = fits.Take(last + 1).Count(f => !f);
            if (misfits < 2 || misfits * 2 <= last + 1) return null;
            var next = last + 1 < named.Count ? named[last + 1].Run.Start : named[last].Run.End;
            return new JobScheduleChange(next, Array.Empty<ScheduleDifference>(), true);
        }

        /// <summary>The job's schedule its history says started the run, when exactly one has that name</summary>
        internal static AgentSchedule NamedSchedule(JobScheduleInput job, JobRun run) => NamedSchedules(job, run) is { Count: 1 } named ? named[0] : null;

        /// <summary>
        /// The job's schedules with the name its history says started the run ("Schedule 12 (Nightly)") - none when it's been
        /// renamed since, several when the job has two of that name.  Null when the history doesn't name a schedule.
        /// </summary>
        private static List<AgentSchedule> NamedSchedules(JobScheduleInput job, JobRun run)
        {
            if (run.InvokedBy is null || InvokedBySchedule.Match(run.InvokedBy) is not { Success: true } match) return null;
            return job.Schedules.Where(s => string.Equals(s.Name, match.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private static bool Fits(IEnumerable<AgentSchedule> schedules, DateTime start) =>
            schedules.Any(s => StartedBy(s, start));

        /// <summary>The schedule would start a run at <paramref name="start"/> - it's due then, or up to <see cref="StartMatchToleranceSec"/> before</summary>
        internal static bool StartedBy(AgentSchedule schedule, DateTime start) =>
            AgentScheduleProjector.LatestRunAtOrBefore(schedule, start) is { } due && (start - due).TotalSeconds <= StartMatchToleranceSec;

        /// <summary>
        /// The enabled schedules whose timing differs between <paramref name="was"/> and <paramref name="now"/>, matched by
        /// schedule_uid (name when there isn't one): changed, added (Was null) or gone (Now null).  Names don't count.
        /// </summary>
        public static List<ScheduleDifference> TimingDifferences(IReadOnlyList<AgentSchedule> was, IReadOnlyList<AgentSchedule> now)
        {
            static string Key(AgentSchedule s) => s.Uid?.ToString() ?? "name:" + s.Name;
            static string Timing(AgentSchedule s) =>
                $"{s.FrequencyType}|{s.FrequencyInterval}|{s.FrequencySubdayType}|{s.FrequencySubdayInterval}|{s.FrequencyRelativeInterval}|" +
                $"{s.FrequencyRecurrenceFactor}|{s.ActiveStartDate}|{s.ActiveEndDate}|{s.ActiveStartTime}|{s.ActiveEndTime}";
            var before = was.Where(s => s.Enabled).GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
            var after = now.Where(s => s.Enabled).GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
            var list = new List<ScheduleDifference>();
            foreach (var key in before.Keys.Union(after.Keys))
            {
                var a = before.GetValueOrDefault(key);
                var b = after.GetValueOrDefault(key);
                if (a != null && b != null && Timing(a) == Timing(b)) continue;
                list.Add(new ScheduleDifference(a, b));
            }
            return list;
        }

        /// <summary>A user, an alert or agent start started it - not a schedule</summary>
        private static bool IsNotScheduled(JobRun run) => run.InvokedBy != null && IsKnownNonSchedule(run.InvokedBy);

        private static readonly Regex InvokedBySchedule = new(@"^Schedule (?<id>\d+) \((?<name>.*?)\)\.(\s|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Each run with the schedule that started it (<see cref="JobRun.Schedule"/>), where that's known.  Its history says:
        /// "Schedule 12 (Nightly)" is matched to the job's schedule of that name; agent start ("Start Sequence"), a user or an
        /// alert started it, and no schedule did.  When the history doesn't say (no message, another language, a schedule
        /// renamed since, or two of the job's schedules with that name), a run that starts up to
        /// <see cref="StartMatchToleranceSec"/> after one of the job's movable schedules is due is taken as started by it.
        /// Otherwise none - so the run isn't moved.
        /// </summary>
        public static IReadOnlyList<JobRun> AttributeRuns(JobScheduleInput job, IReadOnlyList<JobRun> runs)
        {
            var movable = MovableSchedules(job);
            var list = new List<JobRun>(runs.Count);
            foreach (var run in runs)
            {
                AgentSchedule schedule = null;
                var candidates = movable;
                if (NamedSchedules(job, run) is { } named)
                {
                    if (named.Count == 1) schedule = named[0];
                    else if (named.Count > 1) candidates = named.Where(AgentScheduleShifter.IsMovable).ToList();
                }
                else if (IsNotScheduled(run))
                {
                    candidates = new List<AgentSchedule>();
                }
                schedule ??= candidates.FirstOrDefault(s => StartedBy(s, run.Start));
                list.Add(run with { Schedule = schedule });
            }
            return list;
        }

        /// <summary>Started by agent start, a user or an alert - by name in English history messages</summary>
        private static bool IsKnownNonSchedule(string invokedBy) =>
            invokedBy.StartsWith("Start Sequence", StringComparison.OrdinalIgnoreCase)
            || invokedBy.StartsWith("User ", StringComparison.OrdinalIgnoreCase)
            || invokedBy.StartsWith("Alert", StringComparison.OrdinalIgnoreCase);

        /// <summary>What started a run, in a few words: "Schedule Nightly", "Agent start", "Manual - DOMAIN\name", or null when it isn't known</summary>
        public static string DescribeStart(JobRun run)
        {
            if (run.Schedule != null) return $"Schedule {run.Schedule.Name}";
            if (run.InvokedBy is not { } invokedBy) return null;
            if (invokedBy.StartsWith("Start Sequence", StringComparison.OrdinalIgnoreCase)) return "Agent start";
            var text = invokedBy.Split(new[] { ".  " }, 2, StringSplitOptions.None)[0].TrimEnd('.', ' ');
            if (text.StartsWith("User ", StringComparison.OrdinalIgnoreCase)) return "Manual - " + text[5..];
            return text.Length == 0 ? null : text;
        }

        /// <summary>
        /// Why moving the job leaves a run that happened where it ran: a person, an alert or agent start started it, a
        /// schedule that doesn't move did, or it couldn't be matched to a schedule.  Null for a run that moves with the job.
        /// </summary>
        public static string NotMovedReason(JobScheduleInput job, JobRun run)
        {
            if (run.Predicted) return null;
            if (run.Schedule != null) return AgentScheduleShifter.IsMovable(run.Schedule) ? null : $"Started by schedule {run.Schedule.Name}, which {FixedReason(run.Schedule)}";
            var invokedBy = run.InvokedBy ?? "";
            if (invokedBy.StartsWith("User ", StringComparison.OrdinalIgnoreCase)) return "Started manually - moving the job moves its schedules, and only the runs they start";
            if (invokedBy.StartsWith("Alert", StringComparison.OrdinalIgnoreCase)) return "Started by an alert, not a schedule";
            if (invokedBy.StartsWith("Start Sequence", StringComparison.OrdinalIgnoreCase)) return "Started when SQL Server Agent started, not by a schedule";
            if (NamedSchedules(job, run) is { Count: 0 }) return "Started by a schedule the job doesn't have now - renamed or removed since";
            return $"Not matched to a schedule: none of the job's schedules that move was due up to {StartMatchToleranceSec}s before it started";
        }

        /// <summary>Why moving a job leaves this schedule as it is</summary>
        public static string FixedReason(AgentSchedule schedule) =>
            !schedule.Enabled ? "disabled"
            : schedule.FrequencyType == AgentSchedule.AgentStart ? "runs when SQL Server Agent starts"
            : schedule.FrequencyType == AgentSchedule.Idle ? "runs when the CPU is idle"
            : schedule.FrequencyType == AgentSchedule.Once ? "runs once"
            : AgentScheduleProjector.IsProjectable(schedule, out var reason) ? null : reason.ToLowerInvariant();

        /// <summary>
        /// The pairs of <paramref name="jobs"/> that overlap in the range - with the plan's moves or before them - in the runs
        /// that happened (moved as planned) and the runs the schedules predict.  Pairs that overlap neither way are left out.
        /// </summary>
        public List<PlanPairOverlap> PairOverlaps(IReadOnlyList<JobScheduleInput> jobs, DateTime from, DateTime to)
        {
            var actual = jobs.Select(j => ActualRuns(j, moved: true, from, to)).ToList();
            var actualBefore = jobs.Select(j => ActualRuns(j, moved: false, from, to)).ToList();
            // A duration of 1 second stands in for an unknown one, which would predict nothing worth showing
            var predicted = jobs.Select(j => j.ExpectedDurationSec.HasValue ? PredictedRuns(j, moved: true, from, to) : null).ToList();
            var predictedBefore = jobs.Select(j => j.ExpectedDurationSec.HasValue ? PredictedRuns(j, moved: false, from, to) : null).ToList();

            static OverlapAmount Amount(IReadOnlyList<JobRun> a, IReadOnlyList<JobRun> b)
            {
                var (count, seconds) = JobOverlapAnalyzer.CountOverlaps(a, b);
                return new OverlapAmount(count, seconds);
            }

            var pairs = new List<PlanPairOverlap>();
            for (var i = 0; i < jobs.Count; i++)
            {
                for (var j = i + 1; j < jobs.Count; j++)
                {
                    var predictable = predicted[i] != null && predicted[j] != null;
                    var pair = new PlanPairOverlap(jobs[i], jobs[j],
                        Amount(actual[i], actual[j]), Amount(actualBefore[i], actualBefore[j]),
                        predictable ? Amount(predicted[i], predicted[j]) : null,
                        predictable ? Amount(predictedBefore[i], predictedBefore[j]) : null);
                    if (pair.Actual.Any || pair.ActualBefore.Any || (pair.Predicted?.Any ?? false) || (pair.PredictedBefore?.Any ?? false)) pairs.Add(pair);
                }
            }
            return pairs;
        }

        /// <summary>Where runs overlap.  Each job's runs are in start order and don't overlap each other.</summary>
        public static PlanOverlaps FindOverlaps(IReadOnlyList<IReadOnlyList<JobRun>> runs, IReadOnlyList<string> names)
        {
            var sink = new FindSink
            {
                With = runs.Select(r => new List<string>[r.Count]).ToList(),
                Segments = runs.Select(r => new List<(DateTime, DateTime)>[r.Count]).ToList(),
                Names = names
            };
            for (sink.I = 0; sink.I < runs.Count; sink.I++)
            {
                for (sink.J = sink.I + 1; sink.J < runs.Count; sink.J++) JobOverlapAnalyzer.Sweep(runs[sink.I], runs[sink.J], ref sink);
            }
            return new PlanOverlaps(sink.With, sink.Segments, sink.Total);
        }

        /// <summary>Notes each overlap between jobs I and J against both runs</summary>
        private struct FindSink : JobOverlapAnalyzer.IOverlapSink
        {
            public List<List<string>[]> With;
            public List<List<(DateTime Start, DateTime End)>[]> Segments;
            public IReadOnlyList<string> Names;
            public int I;
            public int J;
            public int Total;

            public void Add(int a, int b, DateTime start, DateTime end)
            {
                Total++;
                (With[I][a] ??= new List<string>()).Add(Names[J]);
                (With[J][b] ??= new List<string>()).Add(Names[I]);
                (Segments[I][a] ??= new List<(DateTime, DateTime)>()).Add((start, end));
                (Segments[J][b] ??= new List<(DateTime, DateTime)>()).Add((start, end));
            }
        }

        /// <summary>The most runs going at once in each of <paramref name="slices"/> slices of the period</summary>
        public static (DateTime[] Edges, int[] Peaks) Concurrency(IEnumerable<JobRun> runs, DateTime from, DateTime to, int slices = 720)
        {
            var sliceTicks = Math.Max(TimeSpan.TicksPerSecond, (to - from).Ticks / slices);
            var count = (int)Math.Ceiling((double)(to - from).Ticks / sliceTicks);
            var edges = Enumerable.Range(0, count + 1).Select(i => i == count ? to : from.AddTicks(i * sliceTicks)).ToArray();
            var peaks = new int[count];
            // Ends before starts at the same moment: one run ending as another starts isn't two at once
            var events = runs.SelectMany(r => new[] { (Time: r.Start, Delta: 1), (Time: r.End, Delta: -1) }).OrderBy(e => e.Time).ThenBy(e => e.Delta).ToList();
            var k = 0;
            var running = 0;
            for (var s = 0; s < count; s++)
            {
                // Where things stand at the slice's start - a run ending right on it doesn't count in it
                while (k < events.Count && events[k].Time <= edges[s]) running += events[k++].Delta;
                var peak = running;
                while (k < events.Count && events[k].Time < edges[s + 1])
                {
                    running += events[k++].Delta;
                    peak = Math.Max(peak, running);
                }
                peaks[s] = peak;
            }
            return (edges, peaks);
        }

        /// <summary>Of <paramref name="included"/>, the jobs sharing a schedule (that they haven't a copy of) with a job not included</summary>
        public HashSet<Guid> TiedJobs(IReadOnlyList<JobScheduleInput> included) =>
            included.Where(j => j.Schedules.Any(s => IsShared(s) && !HasOwnCopy(j.JobId, s) &&
                                                      AllJobs.Any(o => !included.Contains(o) && o.Schedules.Any(os => os.Uid == s.Uid))))
                .Select(j => j.JobId).ToHashSet();

        /// <summary>
        /// Suggest start times for the previewed jobs, from where they are in the plan.  Jobs sharing a schedule with a job
        /// not previewed get their own copy if they move - or, when copies aren't allowed, stay where they are.  Moving
        /// together, the suggestion with copies allowed comes too when overlap is left between jobs sharing a schedule.
        /// </summary>
        public async Task<PlanSuggestion> SuggestAsync(ScheduleOptimizerOptions options, CancellationToken cancellationToken = default)
        {
            var included = IncludedJobs;
            var shifted = included.Select(ShiftedJob).ToList();
            var tied = TiedJobs(included);
            var pinned = new HashSet<Guid>(Pinned);
            if (!options.SplitSharedSchedules) pinned.UnionWith(tied);
            var result = await Task.Run(() => JobScheduleOptimizer.Optimize(shifted, pinned, options, cancellationToken), cancellationToken);
            var alternative = !options.SplitSharedSchedules && result.SharedConflictSecAfter > 0
                ? await Task.Run(() => JobScheduleOptimizer.Optimize(shifted, pinned, options with { SplitSharedSchedules = true }, cancellationToken), cancellationToken)
                : null;
            return new PlanSuggestion(included, result, alternative, options.SplitSharedSchedules ? tied : new HashSet<Guid>());
        }

        /// <summary>
        /// Make a suggestion's moves, when it has a benefit.  A job that moves off a schedule a job not previewed uses gets
        /// its own copy, so that job stays put.
        /// </summary>
        public AppliedSuggestion Apply(PlanSuggestion suggestion)
        {
            var result = suggestion.Result;
            if (!result.HasBenefit) return new AppliedSuggestion(Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<string>());
            var own = result.OwnSchedule.Union(result.Shifts.Keys.Where(suggestion.Tied.Contains)).ToHashSet();
            var madeOwn = suggestion.Included.Where(j => own.Contains(j.JobId) && !_ownSchedule.Contains(j.JobId)).Select(j => j.JobId).ToHashSet();
            foreach (var job in suggestion.Included.Where(j => own.Contains(j.JobId))) SetOwnSchedule(job, true);
            // The suggestion moves each schedule from where it was suggested from - so a schedule two moving jobs share
            // moves once, and a job's schedules at different shifts (a shared one moved with another job) stay that far apart
            var before = new Dictionary<ScheduleKey, int>(_shifts);
            var refused = new List<string>();
            foreach (var (jobId, delta) in result.Shifts)
            {
                var job = suggestion.Included.First(j => j.JobId == jobId);
                if (TryMoveBy(job, delta, before, out var reason)) continue;
                refused.Add($"{job.Name} ({reason})");
                // It isn't moving, so it doesn't need the copy it was given to move with
                if (!madeOwn.Contains(jobId)) continue;
                SetOwnSchedule(job, false);
                own.Remove(jobId);
            }
            return new AppliedSuggestion(result.Shifts.Keys.Union(own).ToList(), own, refused);
        }

        /// <summary>
        /// Each moved schedule once, with the jobs that keep it - and each job getting its own copy of a shared schedule.
        /// When every job using a shared schedule is to get its own copy, the first keeps the schedule instead, so it isn't
        /// left with no jobs.  Jobs that didn't run in the range are counted as keeping a schedule they share.
        /// </summary>
        public (List<ScheduleChange> Changes, List<ScheduleSplit> Splits) Changes()
        {
            var users = AllJobs
                .SelectMany(j => j.Schedules.Where(s => s.Uid.HasValue).Select(s => (Uid: s.Uid!.Value, Job: j)))
                .GroupBy(x => x.Uid)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Job).Distinct().OrderBy(j => j.Name).ToList());
            var keeperByCopy = users
                .Where(u => u.Value.Count > 1 && u.Value.All(j => _ownSchedule.Contains(j.JobId)))
                .ToDictionary(u => u.Key, u => u.Value[0].JobId);
            bool Keeps(JobScheduleInput job, AgentSchedule schedule) => schedule.Uid is { } uid && keeperByCopy.GetValueOrDefault(uid) == job.JobId;

            var changes = new Dictionary<ScheduleKey, (AgentSchedule Original, int Seconds, List<JobScheduleInput> Jobs)>();
            var splits = new List<ScheduleSplit>();
            foreach (var job in AllJobs.OrderBy(j => j.Name))
            {
                foreach (var schedule in job.Schedules)
                {
                    var seconds = schedule.Enabled ? _shifts.GetValueOrDefault(KeyOf(job.JobId, schedule)) : 0;
                    if (schedule.Enabled && HasOwnCopy(job.JobId, schedule) && !Keeps(job, schedule))
                    {
                        if (AgentScheduleShifter.TryShift(schedule, seconds, out var copy, out _)) splits.Add(new ScheduleSplit(schedule, copy, job.JobId, job.Name, Guid.NewGuid()));
                        continue;
                    }
                    var key = schedule.Uid.HasValue ? SharedKey(schedule) : KeyOf(job.JobId, schedule);
                    if (!changes.TryGetValue(key, out var change)) changes[key] = change = (schedule, seconds, new List<JobScheduleInput>());
                    // The job keeping a schedule all its users were to leave sets its move
                    if (Keeps(job, schedule)) changes[key] = change = (schedule, seconds, change.Jobs);
                    change.Jobs.Add(job);
                }
            }
            var moved = changes.Values
                .Where(c => c.Seconds != 0)
                .Select(c => AgentScheduleShifter.TryShift(c.Original, c.Seconds, out var shifted, out _)
                    ? new ScheduleChange(c.Original, shifted, c.Jobs.Select(j => j.Name).ToList()) { JobIds = c.Jobs.Select(j => j.JobId).ToList() }
                    : null)
                .Where(c => c != null)
                .ToList();
            return (moved, splits);
        }
    }
}
