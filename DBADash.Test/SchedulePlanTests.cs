using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Test
{
    [TestClass]
    public class SchedulePlanTests
    {
        private static readonly DateTime Monday = new(2026, 11, 2);

        private static AgentSchedule Every15(string uid) => new()
        {
            Name = "Every 15",
            FrequencyType = AgentSchedule.Daily,
            FrequencyInterval = 1,
            FrequencySubdayType = AgentSchedule.SubdayMinutes,
            FrequencySubdayInterval = 15,
            ActiveStartDate = 20260101,
            ScheduleUid = uid
        };

        private static JobScheduleInput Job(string name, AgentSchedule schedule, int duration = 60) =>
            new() { JobId = Guid.NewGuid(), Name = name, ExpectedDurationSec = duration, Schedules = new[] { schedule } };

        /// <summary>Runs every 15 minutes over the day, as if they'd happened</summary>
        private static IReadOnlyList<JobRun> Ran(int durationSec) =>
            Enumerable.Range(0, 96).Select(i => new JobRun(Monday.AddMinutes(i * 15), Monday.AddMinutes(i * 15).AddSeconds(durationSec))).ToList();

        /// <summary>A and B share a schedule; C has its own; D shares A and B's but didn't run in the range</summary>
        private static (SchedulePlan Plan, JobScheduleInput A, JobScheduleInput B, JobScheduleInput C, JobScheduleInput D) Setup()
        {
            var shared = Every15("11111111-1111-1111-1111-111111111111");
            var a = Job("A", shared);
            var b = Job("B", shared);
            var c = Job("C", Every15("22222222-2222-2222-2222-222222222222"));
            var d = Job("D", shared);
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, b, c, d }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [b.JobId] = Ran(60), [c.JobId] = Ran(60) });
            plan.Included.UnionWith(plan.Jobs.Select(j => j.JobId));
            return (plan, a, b, c, d);
        }

        [TestMethod]
        public void MostOverlapping_IgnoresPairsNoMoveCanHelp_AndTakesOnlyEligibleJobs()
        {
            var (plan, a, b, c, _) = Setup();
            plan.Included.Clear();
            var report = new JobOverlapReport
            {
                Pairs = new[]
                {
                    // Thousands of overlaps no start times could avoid, and a few that could be
                    new JobPairOverlap { A = a, B = b, ActualOverlaps = 5000, Verdict = CycleVerdict.CannotSeparate },
                    new JobPairOverlap { A = b, B = c, ActualOverlaps = 3, Verdict = CycleVerdict.Collides }
                }
            };
            plan.IncludeMostOverlapping(report, 1);
            Assert.AreEqual(1, plan.Included.Count);
            Assert.IsTrue(plan.Included.Contains(b.JobId) || plan.Included.Contains(c.JobId), "B and C overlap in a way a move could help; A only in a way it couldn't");
            plan.Included.Clear();
            plan.IncludeMostOverlapping(report, 2, j => j != b);
            Assert.AreEqual(0, plan.Included.Count, "Without B no pair is left that a move could help");
        }

        [TestMethod]
        public void OnlyJobsThatRan_ButAllCountForSharing()
        {
            var (plan, a, _, c, d) = Setup();
            CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, plan.Jobs.Select(j => j.Name).ToList());
            plan.Included.Add(d.JobId);
            Assert.IsFalse(plan.IncludedJobs.Contains(d), "A job that didn't run can't be previewed");
            Assert.IsTrue(plan.SharesSchedule(a));
            Assert.IsFalse(plan.SharesSchedule(c));
            CollectionAssert.AreEquivalent(new[] { "B", "D" }, plan.SharedWith(a, a.Schedules[0]));
        }

        [TestMethod]
        public void SharedScheduleMovesTogether_UntilAJobHasItsOwnCopy()
        {
            var (plan, a, b, _, _) = Setup();
            Assert.IsTrue(plan.TrySetShift(a, 120, out _));
            Assert.AreEqual(120, plan.JobShift(b), "Moving A's schedule moves B");

            plan.SetOwnSchedule(b, true);
            Assert.AreEqual(120, plan.JobShift(b), "The copy starts where the shared schedule is");
            Assert.IsTrue(plan.TrySetShift(b, 300, out _));
            Assert.AreEqual(120, plan.JobShift(a));
            Assert.AreEqual(300, plan.JobShift(b));
            Assert.AreNotEqual(plan.ShiftedJob(a).Schedules[0].ScheduleUid, plan.ShiftedJob(b).Schedules[0].ScheduleUid, "The copy no longer counts as shared");

            plan.SetOwnSchedule(b, false);
            Assert.AreEqual(120, plan.JobShift(b), "Back on the shared schedule, where it is");

            plan.Reset();
            Assert.AreEqual(0, plan.JobShift(a));
            Assert.AreEqual(0, plan.OwnSchedule.Count);
        }

        [TestMethod]
        public void ManualMove_GivesASharedJobItsOwnCopy_AndDropsItWhenMovedBack()
        {
            var (plan, a, b, _, _) = Setup();
            Assert.IsTrue(plan.TryMoveJob(a, 90, ownCopy: true, out _));
            Assert.AreEqual(90, plan.JobShift(a));
            Assert.AreEqual(0, plan.JobShift(b), "B stays where it is");
            CollectionAssert.Contains(plan.OwnSchedule.ToList(), a.JobId);

            Assert.IsTrue(plan.TryMoveJob(a, 0, ownCopy: true, out _));
            Assert.AreEqual(0, plan.OwnSchedule.Count, "Back at the shared schedule's time, the copy isn't needed");
            var (changes, splits) = plan.Changes();
            Assert.AreEqual(0, changes.Count + splits.Count);
        }

        [TestMethod]
        public void ManualMove_KeepsACopyTheUserChose()
        {
            var (plan, a, _, _, _) = Setup();
            plan.SetOwnSchedule(a, true);
            Assert.IsTrue(plan.TryMoveJob(a, 60, ownCopy: true, out _));
            Assert.IsTrue(plan.TryMoveJob(a, 0, ownCopy: true, out _));
            CollectionAssert.Contains(plan.OwnSchedule.ToList(), a.JobId, "Ticked Own Schedule stays ticked");
        }

        [TestMethod]
        public void ManualMove_MovingTogether_MovesTheSharedSchedule()
        {
            var (plan, a, b, _, _) = Setup();
            Assert.IsTrue(plan.TryMoveJob(a, 90, ownCopy: false, out _));
            Assert.AreEqual(90, plan.JobShift(b));
            Assert.AreEqual(0, plan.OwnSchedule.Count);
        }

        [TestMethod]
        public void PairOverlaps_BeforeAndAfter()
        {
            // A and B share a schedule and overlap; C is on its own schedule at the same times, so overlaps both
            var (plan, a, b, c, _) = Setup();
            var before = plan.PairOverlaps(plan.IncludedJobs, Monday, Monday.AddDays(1));
            Assert.AreEqual(3, before.Count);
            Assert.IsTrue(before.All(p => p.Change == PairChange.Same));
            var ab = before.Single(p => p.A == a && p.B == b);
            Assert.AreEqual(96, ab.Actual.Count);
            Assert.AreEqual(96 * 60, ab.Actual.Seconds);
            Assert.AreEqual(96, ab.Predicted!.Value.Count, "Predicted from the schedules with the expected duration");

            // C moves clear of both; A moves on its own copy, clear of B but now partly over C's new time
            plan.TryMoveJob(c, 120, ownCopy: true, out _);
            plan.TryMoveJob(a, 150, ownCopy: true, out _);
            var after = plan.PairOverlaps(plan.IncludedJobs, Monday, Monday.AddDays(1));
            Assert.AreEqual(PairChange.Cleared, after.Single(p => p.A == a && p.B == b).Change);
            Assert.AreEqual(PairChange.Cleared, after.Single(p => p.A == b && p.B == c).Change);
            var ac = after.Single(p => p.A == a && p.B == c);
            Assert.AreEqual(PairChange.Less, ac.Change, "Still overlapping, but for 30 seconds a run instead of a minute");
            Assert.AreEqual(96 * 30, ac.Actual.Seconds);
            Assert.AreEqual(96 * 60, ac.ActualBefore.Seconds);
        }

        [TestMethod]
        public void PairOverlaps_NewOverlap()
        {
            var shared = Every15("44444444-4444-4444-4444-444444444444");
            var a = Job("A", shared);
            var b = Job("B", Every15("55555555-5555-5555-5555-555555555555") with { ActiveStartTime = 500 });
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, b }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [b.JobId] = Ran(60).Select(r => new JobRun(r.Start.AddMinutes(5), r.End.AddMinutes(5))).ToList() });
            Assert.AreEqual(0, plan.PairOverlaps(plan.Jobs, Monday, Monday.AddDays(1)).Count, "Clear to start with");
            plan.TryMoveJob(a, 300, ownCopy: true, out _);
            Assert.AreEqual(PairChange.New, plan.PairOverlaps(plan.Jobs, Monday, Monday.AddDays(1)).Single().Change);
        }

        [TestMethod]
        public void ActualRunsMoveByTheShift()
        {
            var (plan, _, _, c, _) = Setup();
            plan.TrySetShift(c, 300, out _);
            var moved = plan.ActualRuns(c, moved: true, Monday, Monday.AddHours(1));
            var asWas = plan.ActualRuns(c, moved: false, Monday, Monday.AddHours(1));
            Assert.AreEqual(asWas[0].Start.AddMinutes(5), moved[0].Start);
            Assert.AreEqual(4, asWas.Count);
        }

        [TestMethod]
        public void Overlaps_AndConcurrency()
        {
            var a = new List<JobRun> { new(Monday, Monday.AddMinutes(10)) };
            var b = new List<JobRun> { new(Monday.AddMinutes(5), Monday.AddMinutes(20)), new(Monday.AddMinutes(30), Monday.AddMinutes(40)) };
            var overlaps = SchedulePlan.FindOverlaps(new List<List<JobRun>> { a, b }, new[] { "A", "B" });
            Assert.AreEqual(1, overlaps.Count);
            CollectionAssert.AreEqual(new byte[] { PlanOverlaps.Avoidable, 0 }, overlaps.Flags[1]);
            Assert.AreEqual("B", overlaps.Detail(0, 0, 1).With[0].Single());
            var bDetail = overlaps.Detail(1, 0, 2);
            Assert.IsNull(bDetail.With[1]);
            Assert.AreEqual((Monday.AddMinutes(5), Monday.AddMinutes(10)), bDetail.Segments[0].Single());
            Assert.AreEqual((Monday.AddMinutes(5), Monday.AddMinutes(10)), overlaps.Detail(1, 0, 1).Segments[0].Single(), "Asked about one run of several");

            var (edges, peaks) = SchedulePlan.Concurrency(a.Concat(b), Monday, Monday.AddHours(1), slices: 6);
            Assert.AreEqual(7, edges.Length);
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 1, 0, 0 }, peaks);
        }

        [TestMethod]
        public void Overlaps_UnavoidableKeptApart()
        {
            var a = new List<JobRun> { new(Monday, Monday.AddMinutes(10)) };
            var b = new List<JobRun> { new(Monday.AddMinutes(5), Monday.AddMinutes(20)) };
            var c = new List<JobRun> { new(Monday.AddMinutes(8), Monday.AddMinutes(9)) };
            // A and C can't be kept apart; the rest can
            var overlaps = SchedulePlan.FindOverlaps(new List<List<JobRun>> { a, b, c }, new[] { "A", "B", "C" }, (i, j) => i == 0 && j == 2);
            Assert.AreEqual(3, overlaps.Count);
            Assert.AreEqual(2, overlaps.AvoidableCount);
            Assert.AreEqual((byte)(PlanOverlaps.Avoidable | PlanOverlaps.Unavoidable), overlaps.Flags[0][0]);
            Assert.AreEqual(PlanOverlaps.Avoidable, overlaps.Flags[1][0]);
            var aDetail = overlaps.Detail(0, 0, 1);
            CollectionAssert.AreEquivalent(new[] { "B", "C" }, aDetail.With[0]);
            Assert.AreEqual(1, aDetail.Segments[0].Count, "A's overlap with B");
            Assert.AreEqual((Monday.AddMinutes(8), Monday.AddMinutes(9)), aDetail.Unavoidable[0].Single());
            var cDetail = overlaps.Detail(2, 0, 1);
            Assert.AreEqual((Monday.AddMinutes(8), Monday.AddMinutes(9)), cDetail.Unavoidable[0].Single());
            Assert.AreEqual(1, cDetail.Segments[0].Count, "C's overlap with B");

            var all = SchedulePlan.FindOverlaps(new List<List<JobRun>> { a, b, c }, new[] { "A", "B", "C" });
            Assert.AreEqual(3, all.AvoidableCount);
            Assert.IsNull(all.Detail(0, 0, 1).Unavoidable[0]);
        }

        [TestMethod]
        public void Changes_CopyForTheJobThatLeaves_SharedScheduleKeptByTheRest()
        {
            var (plan, a, b, _, d) = Setup();
            plan.SetOwnSchedule(b, true);
            plan.TrySetShift(b, 300, out _);
            plan.TrySetShift(a, 120, out _);
            var (changes, splits) = plan.Changes();
            Assert.AreEqual(b.JobId, splits.Single().JobId);
            Assert.AreEqual(500, splits.Single().Proposed.ActiveStartTime);
            var change = changes.Single();
            Assert.AreEqual(200, change.Proposed.ActiveStartTime);
            CollectionAssert.AreEquivalent(new[] { a.JobId, d.JobId }, change.JobIds.ToList(), "D didn't run in the range but still uses the schedule");
        }

        [TestMethod]
        public void Changes_WhenEveryUserHasACopy_TheFirstKeepsTheSchedule()
        {
            var shared = Every15("33333333-3333-3333-3333-333333333333");
            var a = Job("A", shared);
            var b = Job("B", shared);
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, b }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [b.JobId] = Ran(60) });
            plan.SetOwnSchedule(a, true);
            plan.SetOwnSchedule(b, true);
            plan.TrySetShift(a, 60, out _);
            plan.TrySetShift(b, 120, out _);
            var (changes, splits) = plan.Changes();
            Assert.AreEqual(100, changes.Single().Proposed.ActiveStartTime, "A keeps the schedule, with its move");
            Assert.AreEqual(b.JobId, splits.Single().JobId);
        }

        [TestMethod]
        public async Task Suggest_GivesATiedJobItsOwnCopy()
        {
            // A and B share a schedule with D, which didn't run in the range.  Moving A or B must not move D.
            var (plan, a, b, c, d) = Setup();
            plan.Included.Remove(c.JobId);
            var suggestion = await plan.SuggestAsync(new ScheduleOptimizerOptions { From = Monday, SplitSharedSchedules = true });
            CollectionAssert.AreEquivalent(new[] { a.JobId, b.JobId }, suggestion.Tied.ToList());
            var applied = plan.Apply(suggestion);
            Assert.AreEqual(1, applied.Changed.Count, "One of A and B moves");
            var moved = applied.Changed.Single();
            CollectionAssert.Contains(plan.OwnSchedule.ToList(), moved);
            var (changes, splits) = plan.Changes();
            Assert.AreEqual(0, changes.Count, "The shared schedule - and so D - stays put");
            Assert.AreEqual(moved, splits.Single().JobId);
        }

        [TestMethod]
        public async Task Suggest_MovingTogether_PinsTiedJobs()
        {
            var (plan, a, b, c, _) = Setup();
            plan.Included.Remove(c.JobId);
            var suggestion = await plan.SuggestAsync(new ScheduleOptimizerOptions { From = Monday, SplitSharedSchedules = false });
            Assert.AreEqual(0, suggestion.Result.Shifts.Count, "A and B can only move with D, so they stay");
            Assert.IsNotNull(suggestion.Alternative, "And copies would help");
            Assert.AreEqual(0, plan.Apply(suggestion).Changed.Count);
            Assert.AreEqual(0, plan.JobShift(a) + plan.JobShift(b));
        }

        private static AgentSchedule DailyAt(string name, int time, string uid) => new()
        {
            Name = name,
            FrequencyType = AgentSchedule.Daily,
            FrequencyInterval = 1,
            FrequencySubdayType = AgentSchedule.SubdayAtTime,
            ActiveStartDate = 20260101,
            ActiveStartTime = time,
            ScheduleUid = uid
        };

        private static AgentSchedule AtAgentStart(string uid) => new() { Name = "Agent start", FrequencyType = AgentSchedule.AgentStart, ScheduleUid = uid };

        private static JobRun RanAt(int hour, int minute, int second = 0, string? invokedBy = null) =>
            new(Monday.AddHours(hour).AddMinutes(minute).AddSeconds(second), Monday.AddHours(hour).AddMinutes(minute).AddSeconds(second + 30)) { InvokedBy = invokedBy };

        /// <summary>DoNothing: runs at 02:00 (Nightly) and 14:00 (Afternoon), and when SQL Server Agent starts</summary>
        private static (SchedulePlan Plan, JobScheduleInput Job) MultiSchedule(params JobRun[] runs)
        {
            var job = new JobScheduleInput
            {
                JobId = Guid.NewGuid(),
                Name = "DoNothing",
                ExpectedDurationSec = 30,
                Schedules = new[]
                {
                    DailyAt("Nightly", 20000, "33333333-0000-0000-0000-000000000001"),
                    DailyAt("Afternoon", 140000, "33333333-0000-0000-0000-000000000002"),
                    AtAgentStart("33333333-0000-0000-0000-000000000003")
                }
            };
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { job }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [job.JobId] = runs });
            plan.Included.Add(job.JobId);
            return (plan, job);
        }

        [TestMethod]
        public void AgentStartSchedule_DoesntStopTheMove()
        {
            var (plan, job) = MultiSchedule(RanAt(2, 0));
            Assert.AreEqual(2, SchedulePlan.MovableSchedules(job).Count);
            Assert.IsTrue(plan.TrySetShift(job, 600, out var reason), reason);
            Assert.AreEqual(600, plan.JobShift(job));
            var (changes, splits) = plan.Changes();
            Assert.AreEqual(0, splits.Count);
            // Both timed schedules move by the same amount; the agent start schedule isn't touched
            CollectionAssert.AreEquivalent(new[] { "Nightly:21000", "Afternoon:141000" }, changes.Select(c => $"{c.Original.Name}:{c.Proposed.ActiveStartTime}").ToList());
        }

        [TestMethod]
        public void OnlyAgentStart_CantMove()
        {
            var job = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "Startup", Schedules = new[] { AtAgentStart("44444444-0000-0000-0000-000000000001") } };
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { job }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [job.JobId] = new[] { RanAt(9, 13) } });
            Assert.IsFalse(plan.TrySetShift(job, 600, out var reason));
            StringAssert.Contains(reason, "runs when SQL Server Agent starts");
        }

        [TestMethod]
        public void OnlyRunsAMovingScheduleStarted_Move()
        {
            var (plan, job) = MultiSchedule(
                RanAt(2, 0, invokedBy: "Schedule 5 (Nightly).  The last step to run was step 1 (Do nothing)."),
                RanAt(9, 13, invokedBy: "Start Sequence.  The last step to run was step 1 (Do nothing)."),
                RanAt(10, 0, invokedBy: "User DOMAIN\\someone.  The last step to run was step 1 (Do nothing)."),
                RanAt(14, 0, 30), // No history message: within a minute of Afternoon
                RanAt(16, 0, 20)); // No history message, and no schedule due then
            Assert.IsTrue(plan.TrySetShift(job, 600, out _));
            var moved = plan.ActualRuns(job, moved: true, Monday, Monday.AddDays(1));
            CollectionAssert.AreEqual(new[] { 600, 0, 0, 600, 0 }, moved.Select(r => r.MovedBySec).ToList());
            Assert.AreEqual(Monday.AddHours(2).AddMinutes(10), moved[0].Start);
            Assert.AreEqual(Monday.AddHours(9).AddMinutes(13), moved[1].Start, "Agent start runs stay where they ran");
            CollectionAssert.AreEqual(new string?[] { "Schedule Nightly", "Agent start", "Manual - DOMAIN\\someone", "Schedule Afternoon", null }, moved.Select(SchedulePlan.DescribeStart).ToList());
        }

        [TestMethod]
        public void MoveSchedule_MovesOnlyThatSchedule_AndTheRunsItStarted()
        {
            var (plan, job) = MultiSchedule(
                RanAt(2, 0, invokedBy: "Schedule 5 (Nightly).  The last step to run was step 1 (Do nothing)."),
                RanAt(14, 0, invokedBy: "Schedule 6 (Afternoon).  The last step to run was step 1 (Do nothing)."));
            var nightly = job.Schedules[0];
            Assert.IsTrue(plan.TryMoveSchedule(job, nightly, 600, ownCopy: true, out var reason), reason);
            Assert.AreEqual(600, plan.ScheduleShift(job, nightly));
            Assert.AreEqual(0, plan.ScheduleShift(job, job.Schedules[1]));
            Assert.AreEqual("Nightly +10m", plan.DescribeShift(job));
            CollectionAssert.AreEqual(new[] { 600, 0 }, plan.ActualRuns(job, moved: true, Monday, Monday.AddDays(1)).Select(r => r.MovedBySec).ToList());
            var (changes, _) = plan.Changes();
            Assert.AreEqual("Nightly:21000", changes.Select(c => $"{c.Original.Name}:{c.Proposed.ActiveStartTime}").Single());

            // The predicted runs say which of the job's own schedules starts them, and how far it moved
            var predicted = plan.PredictedRuns(job, moved: true, Monday, Monday.AddDays(1));
            CollectionAssert.AreEqual(new[] { "Nightly:600", "Afternoon:0" }, predicted.Select(r => $"{r.Schedule.Name}:{r.MovedBySec}").ToList());
            Assert.AreSame(nightly, predicted[0].Schedule);

            Assert.IsFalse(plan.TryMoveSchedule(job, job.Schedules[2], 600, ownCopy: true, out reason));
            StringAssert.Contains(reason, "runs when SQL Server Agent starts");
        }

        [TestMethod]
        public void MoveJobBy_KeepsSchedulesMovedApartThatFarApart()
        {
            var (plan, job) = MultiSchedule(RanAt(2, 0));
            Assert.IsTrue(plan.TryMoveSchedule(job, job.Schedules[0], 600, ownCopy: true, out _));
            Assert.IsTrue(plan.TryMoveJobBy(job, 300, ownCopy: true, out var reason), reason);
            Assert.AreEqual(900, plan.ScheduleShift(job, job.Schedules[0]));
            Assert.AreEqual(300, plan.ScheduleShift(job, job.Schedules[1]));
            // A typed move puts them back together
            Assert.IsTrue(plan.TryMoveJob(job, 60, ownCopy: true, out _));
            Assert.AreEqual("+1m", plan.DescribeShift(job));
        }

        [TestMethod]
        public void MoveSchedule_OfASharedSchedule_GivesItsOwnCopy()
        {
            var (plan, a, b, _, _) = Setup();
            Assert.IsTrue(plan.TryMoveSchedule(a, a.Schedules[0], 120, ownCopy: true, out _));
            Assert.AreEqual(120, plan.JobShift(a));
            Assert.AreEqual(0, plan.JobShift(b));
            CollectionAssert.Contains(plan.OwnSchedule.ToList(), a.JobId);
            Assert.IsTrue(plan.TryMoveSchedule(a, a.Schedules[0], 0, ownCopy: true, out _));
            Assert.AreEqual(0, plan.OwnSchedule.Count, "Back where the shared schedule is, the copy isn't needed");
        }

        [TestMethod]
        public void ClashesAdded_CountsStartsLostToTheJobsOwnSchedules()
        {
            var (plan, job) = MultiSchedule(RanAt(2, 0));
            Assert.AreEqual(0, plan.ClashesAdded(job, Monday, Monday.AddDays(2)));
            // Nightly moved onto Afternoon's time: one start lost each day
            Assert.IsTrue(plan.TryMoveSchedule(job, job.Schedules[0], 12 * 3600, ownCopy: true, out _));
            Assert.AreEqual(2, plan.ClashesAdded(job, Monday, Monday.AddDays(2)));
            // 10 seconds later: still running (30s runs)
            Assert.IsTrue(plan.TryMoveSchedule(job, job.Schedules[0], 12 * 3600 + 10, ownCopy: true, out _));
            Assert.AreEqual(2, plan.ClashesAdded(job, Monday, Monday.AddDays(2)));
            Assert.IsTrue(plan.TryMoveSchedule(job, job.Schedules[0], 12 * 3600 + 60, ownCopy: true, out _));
            Assert.AreEqual(0, plan.ClashesAdded(job, Monday, Monday.AddDays(2)));
        }

        [TestMethod]
        public void NotMovedReason_SaysWhyARunStays()
        {
            var (plan, job) = MultiSchedule(
                RanAt(2, 0, invokedBy: "Schedule 5 (Nightly).  The last step to run was step 1 (Do nothing)."),
                RanAt(9, 13, invokedBy: "Schedule 7 (Agent start).  The last step to run was step 1 (Do nothing)."),
                RanAt(10, 0, invokedBy: "User DOMAIN\\someone.  The last step to run was step 1 (Do nothing)."),
                RanAt(11, 0, invokedBy: "Alert 3 (Disk).  The last step to run was step 1 (Do nothing)."),
                RanAt(12, 0, invokedBy: "Schedule 9 (Gone).  The last step to run was step 1 (Do nothing)."),
                RanAt(16, 0, 20));
            var reasons = plan.ActualRuns(job, moved: false, Monday, Monday.AddDays(1)).Select(r => SchedulePlan.NotMovedReason(job, r)).ToList();
            Assert.IsNull(reasons[0], "Nightly moves");
            StringAssert.Contains(reasons[1], "runs when SQL Server Agent starts");
            StringAssert.StartsWith(reasons[2], "Started manually");
            StringAssert.Contains(reasons[3], "alert");
            StringAssert.Contains(reasons[4], "renamed or removed");
            StringAssert.StartsWith(reasons[5], "Not matched to a schedule");
        }

        [TestMethod]
        public void HistoryNamingAnotherSchedule_IsBelieved()
        {
            // Started at 02:00 by its agent start schedule (the server started then) - not moved, even though Nightly was due
            var (plan, job) = MultiSchedule(RanAt(2, 0, invokedBy: "Start Sequence."));
            Assert.IsTrue(plan.TrySetShift(job, 600, out _));
            Assert.AreEqual(0, plan.ActualRuns(job, moved: true, Monday, Monday.AddDays(1)).Single().MovedBySec);
        }

        [TestMethod]
        public void UnknownScheduleName_FallsBackToTime()
        {
            // Renamed since it ran: matched by when it started instead
            var (plan, job) = MultiSchedule(RanAt(14, 0, invokedBy: "Schedule 6 (Old name).  The last step to run was step 1 (x)."));
            Assert.IsTrue(plan.TrySetShift(job, -300, out _));
            var run = plan.ActualRuns(job, moved: true, Monday, Monday.AddDays(1)).Single();
            Assert.AreEqual("Afternoon", run.Schedule.Name);
            Assert.AreEqual(-300, run.MovedBySec);
        }

        private static JobRun RanAtTime(DateTime start, string? invokedBy = "Schedule 5 (Sched).") => new(start, start.AddSeconds(30)) { InvokedBy = invokedBy };

        private static (SchedulePlan Plan, JobScheduleInput Job) WithHistory(AgentSchedule now, IEnumerable<JobRun> runs, params ScheduleVersion[] versions)
        {
            var job = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "Changed", ExpectedDurationSec = 30, Schedules = new[] { now } };
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { job }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [job.JobId] = runs.ToList() },
                versions.Length == 0 ? null : new Dictionary<Guid, IReadOnlyList<ScheduleVersion>> { [job.JobId] = versions });
            return (plan, job);
        }

        [TestMethod]
        public void ScheduleMoved_BeforeTheChangeShowsWhatItWouldDoNow()
        {
            // Nightly at 02:00 was moved to 02:05 - collected at 01:00 on Tuesday.  Monday's run was at 02:00; Tuesday's at 02:05.
            const string uid = "55555555-0000-0000-0000-000000000001";
            var was = DailyAt("Sched", 20000, uid);
            var now = was with { ActiveStartTime = 20500 };
            var monday = RanAtTime(Monday.AddHours(2));
            var tuesday = RanAtTime(Monday.AddDays(1).AddHours(2).AddMinutes(5).AddSeconds(2));
            var (plan, job) = WithHistory(now, new[] { monday, tuesday },
                new ScheduleVersion(Monday.AddDays(-1), new[] { was }), new ScheduleVersion(Monday.AddDays(1).AddHours(1), new[] { now }));

            var change = plan.ScheduleChangeOf(job);
            Assert.AreEqual(Monday.AddDays(1).AddHours(1), change.Until, "No run between the versions fits it now, so the change was by the later one");
            Assert.AreEqual(20000, change.Schedules.Single().Was.ActiveStartTime);

            var shown = plan.ActualRuns(job, moved: false, Monday, Monday.AddDays(2));
            CollectionAssert.AreEqual(new[] { true, false }, shown.Select(r => r.Predicted).ToList());
            Assert.AreEqual(Monday.AddHours(2).AddMinutes(5), shown[0].Start, "Monday as the schedule would run it now");
            Assert.AreEqual(Monday.AddHours(2), plan.ReplacedRuns(job, Monday, Monday.AddDays(2)).Single().Start, "...with the run that happened kept for its outline");

            Assert.IsTrue(plan.TrySetShift(job, 120, out _));
            var moved = plan.ActualRuns(job, moved: true, Monday, Monday.AddDays(2));
            CollectionAssert.AreEqual(new[] { Monday.AddHours(2).AddMinutes(7), Monday.AddDays(1).AddHours(2).AddMinutes(7).AddSeconds(2) }, moved.Select(r => r.Start).ToList());
            Assert.AreEqual(20700, plan.Changes().Changes.Single().Proposed.ActiveStartTime, "The move starts from the schedule as it is now, as the script does");
        }

        [TestMethod]
        public void MoreFrequent_PredictedBeforeTheChange()
        {
            // Every 15 minutes, changed at 11:00 to every 10 - collected at 12:00.  The runs to 12:00 show when it changed.
            const string uid = "66666666-0000-0000-0000-000000000001";
            var was = Every15(uid) with { Name = "Sched" };
            var now = was with { FrequencySubdayInterval = 10 };
            var runs = Enumerable.Range(0, 44).Select(i => RanAtTime(Monday.AddMinutes(i * 15)))
                .Concat(Enumerable.Range(0, 78).Select(i => RanAtTime(Monday.AddHours(11).AddMinutes(i * 10))));
            var (plan, job) = WithHistory(now, runs,
                new ScheduleVersion(Monday.AddDays(-1), new[] { was }), new ScheduleVersion(Monday.AddHours(12), new[] { now }));

            Assert.AreEqual(Monday.AddHours(11), plan.ScheduleChangeOf(job).Until, "11:00 fits both; 10:45 doesn't fit every 10 minutes");
            var shown = plan.ActualRuns(job, moved: false, Monday, Monday.AddDays(1));
            Assert.AreEqual(66, shown.Count(r => r.Predicted), "Every 10 minutes to 11:00");
            Assert.AreEqual(78, shown.Count(r => !r.Predicted));
            Assert.AreEqual(44, plan.ReplacedRuns(job, Monday, Monday.AddDays(1)).Count);
        }

        [TestMethod]
        public void MoveOfAWholeInterval_KeptAsTheSmallestThatDoesTheSame()
        {
            var (plan, _, _, c, _) = Setup();
            Assert.IsTrue(plan.TrySetShift(c, 20 * 60, out _));
            Assert.AreEqual(300, plan.JobShift(c), "Every 15 minutes all day: +20m runs at the same times as +5m");
            Assert.AreEqual(500, plan.Changes().Changes.Single().Proposed.ActiveStartTime, "...the start time the script sets");
            Assert.IsTrue(plan.TrySetShift(c, -10 * 60, out _));
            Assert.AreEqual(300, plan.JobShift(c), "-10m is +5m too");
            Assert.IsTrue(plan.TrySetShift(c, 7 * 60, out _));
            Assert.AreEqual(420, plan.JobShift(c), "Within half an interval, as it is");
            Assert.IsTrue(plan.TrySetShift(c, 15 * 60, out _));
            Assert.AreEqual(0, plan.JobShift(c), "A whole interval is no move");
        }

        [TestMethod]
        public void EquivalentShift_OnlyForSchedulesThatRepeatAllDay()
        {
            var every15 = Every15(null!);
            var every10 = every15 with { FrequencySubdayInterval = 10 };
            Assert.AreEqual(600, AgentScheduleShifter.EquivalentShift(new[] { every15, every10 }, 40 * 60), "Every 10 and every 15 repeat together every 30: +40m is +10m");
            var workingHours = every15 with { ActiveStartTime = 80000, ActiveEndTime = 180000 };
            Assert.AreEqual(1200, AgentScheduleShifter.EquivalentShift(new[] { workingHours }, 1200), "08:00 to 18:00 moves its window: +20m is +20m");
            Assert.AreEqual(1200, AgentScheduleShifter.EquivalentShift(new[] { DailyAt("Nightly", 20000, null!) }, 1200));
        }

        [TestMethod]
        public void MoreFrequent_OldRunsFitTheNewSchedule()
        {
            // Every 10 minutes, changed at 11:00 to every 5 - every old run fits the new schedule too.  11:05 is the first only every 5 explains.
            const string uid = "66666666-0000-0000-0000-000000000002";
            var was = Every15(uid) with { Name = "Sched", FrequencySubdayInterval = 10 };
            var now = was with { FrequencySubdayInterval = 5 };
            var runs = Enumerable.Range(0, 66).Select(i => RanAtTime(Monday.AddMinutes(i * 10)))
                .Concat(Enumerable.Range(0, 12).Select(i => RanAtTime(Monday.AddHours(11).AddMinutes(i * 5))));
            var (plan, job) = WithHistory(now, runs,
                new ScheduleVersion(Monday.AddDays(-1), new[] { was }), new ScheduleVersion(Monday.AddHours(12), new[] { now }));
            Assert.AreEqual(Monday.AddHours(11), plan.ScheduleChangeOf(job).Until);
            var shown = plan.ActualRuns(job, moved: false, Monday, Monday.AddHours(12));
            Assert.AreEqual(132, shown.Count(r => r.Predicted), "Every 5 minutes to 11:00, predicted");
            Assert.AreEqual(12, shown.Count(r => !r.Predicted), "...then as they ran");
        }

        [TestMethod]
        public void LessFrequent_ByTheTimeItWasCollected()
        {
            // Every 10 minutes, changed at 11:00 to every 30 - every new run fits the old schedule, so only 10:50 (the last only every 10 explains) narrows it
            const string uid = "66666666-0000-0000-0000-000000000003";
            var was = Every15(uid) with { Name = "Sched", FrequencySubdayInterval = 10 };
            var now = was with { FrequencySubdayInterval = 30 };
            var runs = Enumerable.Range(0, 66).Select(i => RanAtTime(Monday.AddMinutes(i * 10)))
                .Concat(Enumerable.Range(0, 2).Select(i => RanAtTime(Monday.AddHours(11).AddMinutes(i * 30))));
            var (plan, job) = WithHistory(now, runs,
                new ScheduleVersion(Monday.AddDays(-1), new[] { was }), new ScheduleVersion(Monday.AddHours(12), new[] { now }));
            Assert.AreEqual(Monday.AddHours(12), plan.ScheduleChangeOf(job).Until, "No run only every 30 explains: by the time it was collected");
            var shown = plan.ActualRuns(job, moved: false, Monday, Monday.AddHours(12));
            Assert.AreEqual(24, shown.Count, "Every 30 minutes all morning, the runs between 11:00 and 12:00 predicted the same as they ran");
            Assert.IsTrue(shown.All(r => r.Predicted));
        }

        [TestMethod]
        public void Unchanged_RunsAsTheyHappened()
        {
            const string uid = "77777777-0000-0000-0000-000000000001";
            var now = DailyAt("Sched", 20000, uid);
            var (plan, job) = WithHistory(now, new[] { RanAtTime(Monday.AddHours(2)) },
                new ScheduleVersion(Monday.AddDays(-3), new[] { now with { Name = "Renamed since" } }), new ScheduleVersion(Monday.AddDays(-1), new[] { now }));
            Assert.IsNull(plan.ScheduleChangeOf(job), "A new name isn't a change of timing");
            Assert.IsFalse(plan.ActualRuns(job, moved: false, Monday, Monday.AddDays(1)).Single().Predicted);
        }

        [TestMethod]
        public void ChangedAfterTheRange_AllPredicted_AndManualRunsKept()
        {
            // The range is last week; the schedule changed since (the latest version collected in it isn't what it is now)
            const string uid = "88888888-0000-0000-0000-000000000001";
            var was = DailyAt("Sched", 20000, uid);
            var now = was with { ActiveStartTime = 30000 };
            var (plan, job) = WithHistory(now, new[] { RanAtTime(Monday.AddHours(2)), RanAtTime(Monday.AddHours(9), "User DOMAIN\\someone.") },
                new ScheduleVersion(Monday.AddDays(-1), new[] { was }));
            Assert.AreEqual(DateTime.MaxValue, plan.ScheduleChangeOf(job).Until);
            var shown = plan.ActualRuns(job, moved: false, Monday, Monday.AddDays(1));
            CollectionAssert.AreEqual(new[] { Monday.AddHours(3), Monday.AddHours(9) }, shown.Select(r => r.Start).ToList(), "Predicted at 03:00; the manual run is kept");
            CollectionAssert.AreEqual(new[] { true, false }, shown.Select(r => r.Predicted).ToList());
        }

        [TestMethod]
        public void NoHistory_RunsThatDontFitGiveItAway()
        {
            // Ran at 02:00 on Monday and Tuesday; 02:05 from Wednesday, as the schedule is now
            var now = DailyAt("Sched", 20500, "99999999-0000-0000-0000-000000000001");
            var (plan, job) = WithHistory(now, new[] { RanAtTime(Monday.AddHours(2)), RanAtTime(Monday.AddDays(1).AddHours(2)), RanAtTime(Monday.AddDays(2).AddHours(2).AddMinutes(5)) });
            var change = plan.ScheduleChangeOf(job);
            Assert.IsTrue(change.Inferred);
            Assert.AreEqual(Monday.AddDays(2).AddHours(2).AddMinutes(5), change.Until, "From the first run that fits after the last that doesn't");
        }

        [TestMethod]
        public void NoHistory_ALateRunIsntAChange()
        {
            // Every day at 02:05; Tuesday's run started 3 minutes late
            var now = DailyAt("Sched", 20500, "99999999-0000-0000-0000-000000000002");
            var (plan, job) = WithHistory(now, Enumerable.Range(0, 5).Select(d => RanAtTime(Monday.AddDays(d).AddHours(2).AddMinutes(d == 1 ? 8 : 5))));
            Assert.IsNull(plan.ScheduleChangeOf(job));
        }

        [TestMethod]
        public void NoHistory_OneTimeScheduleRunsArentAChange()
        {
            var daily = DailyAt("Sched", 20500, "99999999-0000-0000-0000-000000000003");
            var once = new AgentSchedule { Name = "Once", FrequencyType = AgentSchedule.Once, ActiveStartDate = 20261103, ActiveStartTime = 150000, ScheduleUid = "99999999-0000-0000-0000-000000000004" };
            var job = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "Changed", ExpectedDurationSec = 30, Schedules = new[] { daily, once } };
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { job }, new Dictionary<Guid, IReadOnlyList<JobRun>>
            {
                [job.JobId] = new[] { RanAtTime(Monday.AddHours(2).AddMinutes(5)), RanAtTime(Monday.AddDays(1).AddHours(15), "Schedule 9 (Once)."), RanAtTime(Monday.AddDays(1).AddHours(2).AddMinutes(5)) }
            });
            Assert.IsNull(plan.ScheduleChangeOf(job));
        }

        [TestMethod]
        public void LatestRunAtOrBefore_SameTimesAsRunTimes()
        {
            var schedules = new[]
            {
                Every15(null!) with { ActiveStartTime = 700 },
                Every15(null!) with { FrequencySubdayInterval = 10, ActiveStartTime = 220000, ActiveEndTime = 20000 },
                Every15(null!) with { FrequencySubdayType = AgentSchedule.SubdaySeconds, FrequencySubdayInterval = 10 },
                DailyAt("Nightly", 233000, null!),
                DailyAt("Every 3 days", 120000, null!) with { FrequencyInterval = 3 },
                new AgentSchedule { FrequencyType = AgentSchedule.Weekly, FrequencyInterval = 2 | 16, FrequencyRecurrenceFactor = 1, FrequencySubdayType = AgentSchedule.SubdayHours, FrequencySubdayInterval = 2, ActiveStartDate = 20260101, ActiveStartTime = 80000, ActiveEndTime = 180000 }
            };
            foreach (var schedule in schedules)
            {
                var runs = AgentScheduleProjector.RunTimes(schedule, Monday.AddDays(-2), Monday.AddDays(5)).ToList();
                for (var t = Monday; t < Monday.AddDays(4); t = t.AddSeconds(617))
                {
                    var expected = runs.Where(r => r <= t && r >= t.Date.AddDays(-1)).Select(r => (DateTime?)r).LastOrDefault();
                    Assert.AreEqual(expected, AgentScheduleProjector.LatestRunAtOrBefore(schedule, t), $"{schedule.FrequencyType}/{schedule.FrequencySubdayInterval} at {t}");
                }
            }
        }

        [TestMethod]
        public void Apply_RefusedMove_DropsTheCopyItWasGiven()
        {
            // A shares 22:00 to 23:30 every 15 minutes with D, which isn't previewed.  An hour later would cross midnight.
            var window = Every15("44444444-1111-0000-0000-000000000001") with { ActiveStartTime = 220000, ActiveEndTime = 233000 };
            var a = Job("A", window);
            var d = Job("D", window);
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, d }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [d.JobId] = Ran(60) });
            plan.Included.Add(a.JobId);
            var result = new ScheduleOptimizerResult
            {
                Shifts = new Dictionary<Guid, int> { [a.JobId] = 3600 },
                OwnSchedule = Array.Empty<Guid>(),
                ConflictSecBefore = 100,
                ConflictSecAfter = 0
            };
            var applied = plan.Apply(new PlanSuggestion(new[] { a }, result, null, new HashSet<Guid> { a.JobId }));
            Assert.AreEqual(1, applied.Refused.Count);
            Assert.AreEqual(0, plan.OwnSchedule.Count, "Not moving, so no copy of the shared schedule");
            Assert.AreEqual(0, plan.Changes().Splits.Count);
        }

        [TestMethod]
        public void SnapshotAndRestore_UndoTheMovesAndCopies()
        {
            var (plan, a, b, _, _) = Setup();
            var start = plan.Snapshot();
            Assert.IsFalse(start.HasChanges);
            Assert.IsTrue(plan.TryMoveJob(a, 90, ownCopy: true, out _));
            var moved = plan.Snapshot();
            Assert.IsFalse(moved.SameAs(start));
            Assert.IsTrue(plan.IsMoved(a));
            Assert.IsFalse(plan.IsMoved(b), "A moved on its own copy");

            plan.Restore(start);
            Assert.AreEqual(0, plan.JobShift(a));
            Assert.AreEqual(0, plan.OwnSchedule.Count);
            Assert.IsTrue(plan.Snapshot().SameAs(start));

            plan.Restore(moved);
            Assert.AreEqual(90, plan.JobShift(a));
            CollectionAssert.Contains(plan.OwnSchedule.ToList(), a.JobId);
            // The copy was made for the move, so moving back drops it - as it did before the undo
            Assert.IsTrue(plan.TryMoveJob(a, 0, ownCopy: true, out _));
            Assert.AreEqual(0, plan.OwnSchedule.Count);
        }

        [TestMethod]
        public void ScheduleUid_MatchedWhateverItsCase()
        {
            var lower = Every15("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var a = Job("A", lower);
            var b = Job("B", lower with { ScheduleUid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE" });
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, b }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [b.JobId] = Ran(60) });
            Assert.IsTrue(plan.SharesSchedule(a));
            Assert.IsTrue(plan.TrySetShift(a, 120, out _));
            Assert.AreEqual(120, plan.JobShift(b), "One schedule, however its schedule_uid was written");
            Assert.IsNotNull(JobOverlapAnalyzer.SharedSchedule(a, b));
            Assert.AreEqual(1, plan.Changes().Changes.Count);
        }

        [TestMethod]
        public void Runs_CachedUntilTheJobMoves()
        {
            var (plan, a, _, c, _) = Setup();
            var first = plan.ActualRuns(c, moved: true, Monday, Monday.AddDays(1));
            Assert.AreSame(first, plan.ActualRuns(c, moved: true, Monday, Monday.AddDays(1)), "Asked again, the same runs");
            plan.TrySetShift(a, 300, out _);
            Assert.AreSame(first, plan.ActualRuns(c, moved: true, Monday, Monday.AddDays(1)), "Another job's move doesn't change C's runs");
            plan.TrySetShift(c, 300, out _);
            var moved = plan.ActualRuns(c, moved: true, Monday, Monday.AddDays(1));
            Assert.AreNotSame(first, moved);
            Assert.AreEqual(first[0].Start.AddMinutes(5), moved[0].Start);
        }

        [TestMethod]
        public void Apply_MovesEachScheduleFromWhereItWas()
        {
            // A has only the shared schedule; B has a nightly one of its own first.  A was moved +5m, which moved the shared
            // schedule for B too - so B's schedules are at different shifts when the suggestion moves both jobs a minute
            var shared = Every15("44444444-2222-0000-0000-000000000001");
            var a = Job("A", shared);
            var b = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "B", ExpectedDurationSec = 60, Schedules = new[] { DailyAt("Nightly", 20000, "44444444-2222-0000-0000-000000000002"), shared } };
            var plan = new SchedulePlan();
            plan.SetJobs(new[] { a, b }, new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Ran(60), [b.JobId] = Ran(60) });
            plan.Included.UnionWith(new[] { a.JobId, b.JobId });
            Assert.IsTrue(plan.TrySetShift(a, 300, out _));
            Assert.AreEqual(0, plan.JobShift(b), "B's first schedule hasn't moved");

            var result = new ScheduleOptimizerResult
            {
                Shifts = new Dictionary<Guid, int> { [a.JobId] = 60, [b.JobId] = 60 },
                OwnSchedule = Array.Empty<Guid>(),
                ConflictSecBefore = 100,
                ConflictSecAfter = 0
            };
            plan.Apply(new PlanSuggestion(new[] { a, b }, result, null, new HashSet<Guid>()));
            Assert.AreEqual(360, plan.JobShift(a), "The shared schedule moves a minute once, from +5m - not put back to B's +1m");
            CollectionAssert.AreEquivalent(new[] { "Nightly:20100", "Every 15:600" }, plan.Changes().Changes.Select(c => $"{c.Original.Name}:{c.Proposed.ActiveStartTime}").ToList());
        }
    }
}
