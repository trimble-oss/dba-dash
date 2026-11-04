using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class JobScheduleOptimizerTests
    {
        private static readonly DateTime Monday = new(2026, 11, 2);

        private static JobScheduleInput Every(string name, int minutes, int durationSec, int startTime = 0) => new()
        {
            JobId = Guid.NewGuid(),
            Name = name,
            ExpectedDurationSec = durationSec,
            Schedules = new[]
            {
                new AgentSchedule
                {
                    FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdayMinutes,
                    FrequencySubdayInterval = minutes, ActiveStartDate = 20260101, ActiveStartTime = startTime
                }
            }
        };

        private static JobScheduleInput DailyAt(string name, int time, int durationSec) => new()
        {
            JobId = Guid.NewGuid(),
            Name = name,
            ExpectedDurationSec = durationSec,
            Schedules = new[]
            {
                new AgentSchedule { FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartDate = 20260101, ActiveStartTime = time }
            }
        };

        private static ScheduleOptimizerResult Optimize(IReadOnlyList<JobScheduleInput> jobs, int maxShift = 3600, int step = 60, int gap = 0, params Guid[] pinned) =>
            JobScheduleOptimizer.Optimize(jobs, new HashSet<Guid>(pinned), new ScheduleOptimizerOptions { From = Monday, MaxShiftSec = maxShift, StepSec = step, MinGapSec = gap });

        /// <summary>Overlaps over a day with the suggested moves applied to the real schedules</summary>
        private static int OverlapsAfter(IReadOnlyList<JobScheduleInput> jobs, ScheduleOptimizerResult result)
        {
            var moved = jobs.Select(j => new JobScheduleInput
            {
                JobId = j.JobId,
                ExpectedDurationSec = j.ExpectedDurationSec,
                Schedules = j.Schedules.Select(s => AgentScheduleShifter.TryShift(s, result.Shifts.GetValueOrDefault(j.JobId), out var shifted, out _) ? shifted : throw new AssertFailedException("Suggested an impossible move")).ToList()
            }).ToList();
            var runs = moved.Select(j => JobOverlapAnalyzer.ProjectRuns(j, Monday, Monday.AddDays(1), out _)).ToList();
            var total = 0;
            for (var i = 0; i < runs.Count; i++)
            {
                for (var k = i + 1; k < runs.Count; k++) total += JobOverlapAnalyzer.CountOverlaps(runs[i], runs[k]).Count;
            }
            return total;
        }

        [TestMethod]
        public void SeparatesCyclesWithTheSmallestMove()
        {
            var jobs = new[] { Every("Log backup", 10, 60), Every("ETL", 15, 60) };
            var result = Optimize(jobs);
            Assert.IsTrue(result.ConflictSecBefore > 0);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(1, result.Shifts.Count, "Only one job needs to move");
            Assert.AreEqual(60, Math.Abs(result.Shifts.Values.Single()), "A minute is enough: the runs take 1m each in a 5m shared cycle");
            Assert.AreEqual(0, OverlapsAfter(jobs, result));
        }

        [TestMethod]
        public void AgentStartScheduleBeside_StillMoved()
        {
            var etl = Every("ETL", 15, 60);
            var withStart = new JobScheduleInput
            {
                JobId = etl.JobId,
                Name = etl.Name,
                ExpectedDurationSec = 60,
                Schedules = etl.Schedules.Append(new AgentSchedule { Name = "Agent start", FrequencyType = AgentSchedule.AgentStart }).ToList()
            };
            var logBackup = Every("Log backup", 10, 60);
            var result = Optimize(new[] { logBackup, withStart }, pinned: logBackup.JobId);
            Assert.IsTrue(result.Shifts.ContainsKey(withStart.JobId), "The agent start schedule doesn't stop the timed one moving");
            Assert.AreEqual(0, result.ConflictSecAfter);
        }

        [TestMethod]
        public void OneTimeRun_StaysWhereItIs_WhenTheJobMoves()
        {
            // The clash is the one-time run at 01:00, which moving the job doesn't move - so no move clears it
            var pinned = DailyAt("Nightly", 10000, 60);
            var oneOff = new JobScheduleInput
            {
                JobId = Guid.NewGuid(),
                Name = "One-off",
                ExpectedDurationSec = 60,
                Schedules = new[]
                {
                    new AgentSchedule { Name = "Once", FrequencyType = AgentSchedule.Once, ActiveStartDate = 20261102, ActiveStartTime = 10000 },
                    new AgentSchedule { Name = "Noon", FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartDate = 20260101, ActiveStartTime = 120000 }
                }
            };
            var result = Optimize(new[] { pinned, oneOff }, pinned: pinned.JobId);
            Assert.IsTrue(result.ConflictSecBefore > 0);
            Assert.AreEqual(result.ConflictSecBefore, result.ConflictSecAfter, 0.001);
            Assert.AreEqual(0, result.Shifts.Count, "Moving the noon schedule doesn't help");
        }

        [TestMethod]
        public void PinnedJobsStay()
        {
            var jobs = new[] { Every("Log backup", 10, 60), Every("ETL", 15, 60) };
            var result = Optimize(jobs, pinned: jobs[1].JobId);
            Assert.IsFalse(result.Shifts.ContainsKey(jobs[1].JobId));
            Assert.IsTrue(result.Shifts.ContainsKey(jobs[0].JobId));
            Assert.AreEqual(0, result.ConflictSecAfter);

            var none = Optimize(jobs, pinned: new[] { jobs[0].JobId, jobs[1].JobId });
            Assert.AreEqual(0, none.Shifts.Count);
            Assert.AreEqual(none.ConflictSecBefore, none.ConflictSecAfter);
        }

        [TestMethod]
        public void DailyJobs_KeepsAGap()
        {
            var index = DailyAt("Index maintenance", 10000, 40 * 60);
            var stats = DailyAt("Update statistics", 12000, 15 * 60);
            var result = Optimize(new[] { index, stats });
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(20 * 60, result.Shifts.Values.Sum(Math.Abs), "Moved 20 minutes, one way or the other");
            Assert.AreEqual(TimeSpan.FromDays(1), result.Period);

            var withGap = Optimize(new[] { index, stats }, gap: 300);
            Assert.AreEqual(0, withGap.ConflictSecAfter);
            Assert.AreEqual(25 * 60, withGap.Shifts.Values.Sum(Math.Abs), "And 5 more for the gap");
        }

        [TestMethod]
        public void RespectsMaxShift()
        {
            var index = DailyAt("Index maintenance", 10000, 40 * 60);
            var stats = DailyAt("Update statistics", 12000, 15 * 60);
            var result = Optimize(new[] { index, stats }, maxShift: 600);
            Assert.IsTrue(result.Shifts.Values.All(s => Math.Abs(s) <= 600));
            Assert.IsTrue(result.ConflictSecAfter < result.ConflictSecBefore, "Still better, if not clear");
        }

        [TestMethod]
        public void CantSeparate_SuggestsACadence()
        {
            // 200s + 200s don't fit in the 5 minutes 10 and 15 minute cycles share
            var a = Every("Log backup", 10, 200);
            var b = Every("ETL", 15, 200);
            var result = Optimize(new[] { a, b });
            var note = result.Notes[b.JobId].Single();
            StringAssert.Contains(note, "line up every 5 minutes");
            // 20 minutes shares 10 with the log backup, enough for both; closer to 15 than 30, and less often than 10
            StringAssert.Contains(note, "Running ETL every 20 minutes instead of every 15 minutes would allow it");
            Assert.AreEqual(note.Replace("from Log backup", "from ETL"), result.Notes[a.JobId].Single());
        }

        [TestMethod]
        public void SuggestCadence_NoneFits()
        {
            Assert.IsNull(JobScheduleOptimizer.SuggestCadence(60, 50, 600, 50), "A job every minute leaves no room");
            Assert.AreEqual(1200, JobScheduleOptimizer.SuggestCadence(600, 200, 900, 200));
        }

        /// <summary>Jobs on one schedule (same schedule_uid), every 15 minutes</summary>
        private static JobScheduleInput[] Sharing(params string[] names)
        {
            var schedule = Every("x", 15, 60).Schedules[0] with { Name = "Every 15", ScheduleUid = Guid.NewGuid().ToString() };
            return names.Select(n => new JobScheduleInput { JobId = Guid.NewGuid(), Name = n, ExpectedDurationSec = 60, Schedules = new[] { schedule } }).ToArray();
        }

        private static ScheduleOptimizerResult Split(IReadOnlyList<JobScheduleInput> jobs, params Guid[] pinned) =>
            JobScheduleOptimizer.Optimize(jobs, new HashSet<Guid>(pinned), new ScheduleOptimizerOptions { From = Monday, SplitSharedSchedules = true });

        [TestMethod]
        public void AlreadyClear_NoMoves()
        {
            var jobs = new[] { Every("A", 15, 60), Every("B", 15, 60, startTime: 500) };
            var result = Optimize(jobs);
            Assert.AreEqual(0, result.ConflictSecBefore);
            Assert.AreEqual(0, result.Shifts.Count);
            Assert.IsFalse(result.HasBenefit);
        }

        [TestMethod]
        public void SharedSchedule_MovingTogetherCantHelp()
        {
            var jobs = Sharing("A", "B");
            var result = Optimize(jobs);
            Assert.IsTrue(result.ConflictSecBefore > 0);
            Assert.AreEqual(0, result.Shifts.Count, "Moving the schedule moves both, so no move helps");
            Assert.IsFalse(result.HasBenefit);
            Assert.AreEqual(result.ConflictSecBefore, result.SharedConflictSecAfter, "All the overlap is between jobs sharing the schedule");
            Assert.AreEqual(0, result.OwnSchedule.Count);
        }

        [TestMethod]
        public void SharedSchedule_SplitGivesOneJobItsOwnCopy()
        {
            var jobs = Sharing("A", "B");
            var result = Split(jobs);
            Assert.IsTrue(result.HasBenefit);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(0, result.SharedConflictSecAfter);
            Assert.AreEqual(1, result.OwnSchedule.Count, "One job leaves; the other keeps the schedule");
            Assert.AreEqual(result.OwnSchedule.Single(), result.Shifts.Keys.Single(), "The one that moves is the one with the copy");
            Assert.AreEqual(0, result.JobConflictSecAfter[jobs[0].JobId]);
            Assert.IsTrue(result.JobConflictSecBefore[jobs[0].JobId] > 0);
        }

        [TestMethod]
        public void SharedSchedule_SplitsAsFewAsNeeded()
        {
            // Three on one schedule need two copies; with a fourth on its own schedule, the shared three still move as few as possible
            var jobs = Sharing("A", "B", "C");
            var result = Split(jobs);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(2, result.OwnSchedule.Count);
            Assert.IsFalse(result.Shifts.Keys.Except(result.OwnSchedule).Any(), "The job keeping the schedule doesn't move");
        }

        [TestMethod]
        public void SharedSchedule_PinnedJobKeepsIt()
        {
            var jobs = Sharing("A", "B");
            var result = Split(jobs, jobs[0].JobId);
            Assert.AreEqual(jobs[1].JobId, result.OwnSchedule.Single());
            Assert.IsFalse(result.Shifts.ContainsKey(jobs[0].JobId));
        }

        [TestMethod]
        public void SharedSchedule_MovesAsOne()
        {
            // A and B share a schedule; C collides with both.  Moving together, A and B get the same move (or none).
            var jobs = Sharing("A", "B").Append(Every("C", 15, 60)).ToArray();
            var result = Optimize(jobs);
            Assert.AreEqual(result.Shifts.GetValueOrDefault(jobs[0].JobId), result.Shifts.GetValueOrDefault(jobs[1].JobId));
            Assert.IsTrue(result.HasBenefit, "C can be moved away from both");
            Assert.AreEqual(result.SharedConflictSecAfter, result.ConflictSecAfter, "What's left is A and B");
        }

        [TestMethod]
        public void SharingGroups_FollowsChains()
        {
            // B shares one schedule with A and another with C, so all three move together
            var ab = Sharing("A", "B");
            var bc = Sharing("B2", "C");
            var b = new JobScheduleInput { JobId = ab[1].JobId, Name = "B", ExpectedDurationSec = 60, Schedules = new[] { ab[1].Schedules[0], bc[0].Schedules[0] } };
            var groups = JobScheduleOptimizer.SharingGroups(new[] { ab[0], b, bc[1], Every("D", 10, 60) });
            Assert.AreEqual(groups[0], groups[1]);
            Assert.AreEqual(groups[1], groups[2]);
            Assert.AreNotEqual(groups[0], groups[3]);
            Assert.AreEqual("Every 15", JobOverlapAnalyzer.SharedSchedule(ab[0], ab[1]));
            Assert.IsNull(JobOverlapAnalyzer.SharedSchedule(ab[0], Every("D", 10, 60)));
        }

        [TestMethod]
        public void EveryMoveHelps()
        {
            // Undoing any one suggested move makes things worse - none is kept for nothing
            var jobs = new[] { Every("A", 15, 120), Every("B", 15, 120), Every("C", 30, 300), DailyAt("D", 20000, 60) };
            var result = Optimize(jobs, maxShift: 900);
            foreach (var id in result.Shifts.Keys)
            {
                var without = new ScheduleOptimizerResult { Shifts = result.Shifts.Where(s => s.Key != id).ToDictionary(s => s.Key, s => s.Value) };
                Assert.IsTrue(OverlapsAfter(jobs, without) > OverlapsAfter(jobs, result), $"The move for {jobs.First(j => j.JobId == id).Name} helps");
            }
        }

        [TestMethod]
        public void AlwaysRunningJob_IsLeftOut_AndDoesntDrawExtraMoves()
        {
            // Two jobs on one 5 minute schedule, beside one that runs 30 minutes every minute (nearly always running) and two
            // short frequent ones.  Only one of the pair needs to move; the always-running job's brief projected gaps used to
            // draw a second move for ~30 seconds a day.  Retry and BlitzWho start together every minute: one of them moves seconds.
            var pair = Sharing("Deadlock1", "Deadlock2");
            var longWait = Every("Long Wait", 1, 1801);
            var jobs = pair.Append(longWait).Append(Every("Retry", 1, 1)).Append(new JobScheduleInput
            {
                JobId = Guid.NewGuid(),
                Name = "BlitzWho",
                ExpectedDurationSec = 3,
                Schedules = new[] { new AgentSchedule { FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdaySeconds, FrequencySubdayInterval = 30, ActiveStartDate = 20260101 } }
            }).ToArray();
            var result = Split(jobs);
            Assert.AreEqual(longWait.JobId, result.Ignored.Single());
            StringAssert.Contains(result.Notes[longWait.JobId].Single(), "left out");
            var pairMoved = result.Shifts.Keys.Where(id => pair.Any(p => p.JobId == id)).ToList();
            Assert.AreEqual(1, pairMoved.Count, "One of the pair moves");
            Assert.AreEqual(2, result.Shifts.Count, "And one of Retry and BlitzWho");
            Assert.IsTrue(result.Shifts.Keys.Except(pairMoved).All(id => jobs.Single(j => j.JobId == id).Name is "Retry" or "BlitzWho"));
            Assert.AreEqual(1, result.OwnSchedule.Count);
            Assert.AreEqual(pairMoved.Single(), result.OwnSchedule.Single());
            Assert.IsTrue(result.HasBenefit);
        }

        [TestMethod]
        public void SmallGains_AreNotWorthAMove()
        {
            // 20 seconds a day of overlap isn't worth changing a schedule for
            var a = DailyAt("A", 10000, 60);
            var b = DailyAt("B", 10040, 60);
            var result = Optimize(new[] { a, b });
            Assert.AreEqual(20, result.ConflictSecBefore);
            Assert.AreEqual(0, result.Shifts.Count);
            Assert.IsFalse(result.HasBenefit);

            var any = JobScheduleOptimizer.Optimize(new[] { a, b }, new HashSet<Guid>(), new ScheduleOptimizerOptions { From = Monday, MinGainSecPerDay = 0, MinGainFraction = 0 });
            Assert.IsTrue(any.HasBenefit, "With no minimum, it moves");
        }

        [TestMethod]
        public void ThreeJobs_AllClear()
        {
            var jobs = new[] { Every("A", 15, 120), Every("B", 15, 120), Every("C", 30, 300), DailyAt("D", 20000, 60) };
            var result = Optimize(jobs, maxShift: 900);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(0, OverlapsAfter(jobs, result));
        }

        private static JobScheduleInput EverySeconds(string name, int seconds, int durationSec)
        {
            var job = Every(name, 1, durationSec);
            return new JobScheduleInput
            {
                JobId = job.JobId,
                Name = name,
                ExpectedDurationSec = durationSec,
                Schedules = new[] { job.Schedules[0] with { FrequencySubdayType = AgentSchedule.SubdaySeconds, FrequencySubdayInterval = seconds } }
            };
        }

        [TestMethod]
        public void EveryMinuteJobs_MoveInSeconds()
        {
            // Whole-minute moves leave a job that runs every minute where it was
            var jobs = new[] { Every("A", 1, 5), Every("B", 1, 5), Every("C", 1, 5), Every("D", 1, 5) };
            var result = Optimize(jobs);
            Assert.IsTrue(result.ConflictSecBefore > 0);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.AreEqual(3, result.Shifts.Count, "One stays where it is");
            Assert.IsTrue(result.Shifts.Values.All(s => s % 60 != 0 && Math.Abs(s) <= 30), "Moves are within the minute");
            Assert.AreEqual(0, OverlapsAfter(jobs, result));
        }

        [TestMethod]
        public void SecondsCycles_MixedWithMinutes_MostlyCleared()
        {
            var jobs = new[] { Every("A", 1, 5), Every("B", 1, 5), Every("C", 2, 10), EverySeconds("D", 30, 3), EverySeconds("E", 10, 1) };
            var result = Optimize(jobs);
            Assert.IsTrue(result.ConflictSecBefore > 0);
            Assert.IsTrue(result.HasBenefit);
            Assert.IsTrue(result.ConflictSecAfter <= result.ConflictSecBefore * 0.1, $"{result.ConflictSecAfter} of {result.ConflictSecBefore} left");
        }

        [TestMethod]
        public void LongerCycles_StillMoveInWholeMinutes()
        {
            var jobs = new[] { Every("A", 5, 60), Every("B", 5, 60), Every("C", 5, 60) };
            var result = Optimize(jobs);
            Assert.AreEqual(0, result.ConflictSecAfter);
            Assert.IsTrue(result.Shifts.Values.All(s => s % 60 == 0));
        }
    }
}
