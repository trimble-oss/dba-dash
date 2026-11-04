using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class JobScheduleProjectionTests
    {
        private static readonly DateTime Monday = new(2026, 11, 2);

        private static AgentSchedule EveryMinutes(int minutes, int startTime = 0, int endTime = 235959) => new()
        {
            FrequencyType = AgentSchedule.Daily,
            FrequencyInterval = 1,
            FrequencySubdayType = AgentSchedule.SubdayMinutes,
            FrequencySubdayInterval = minutes,
            ActiveStartDate = 20260101,
            ActiveStartTime = startTime,
            ActiveEndTime = endTime
        };

        private static AgentSchedule DailyAt(int time, int everyDays = 1, int startDate = 20260101) => new()
        {
            FrequencyType = AgentSchedule.Daily,
            FrequencyInterval = everyDays,
            FrequencySubdayType = AgentSchedule.SubdayAtTime,
            ActiveStartDate = startDate,
            ActiveStartTime = time
        };

        private static List<DateTime> Runs(AgentSchedule s, DateTime from, DateTime to) =>
            AgentScheduleProjector.RunTimes(s, from, to).ToList();

        [TestMethod]
        public void EveryNMinutes_AllDay()
        {
            var runs = Runs(EveryMinutes(15), Monday, Monday.AddDays(1));
            Assert.AreEqual(96, runs.Count);
            Assert.AreEqual(Monday, runs[0]);
            Assert.AreEqual(Monday.AddHours(23).AddMinutes(45), runs[^1]);
        }

        [TestMethod]
        public void EveryNMinutes_WindowIncludesEndTime()
        {
            var runs = Runs(EveryMinutes(30, 80000, 180000), Monday, Monday.AddDays(1));
            Assert.AreEqual(21, runs.Count);
            Assert.AreEqual(Monday.AddHours(8), runs[0]);
            Assert.AreEqual(Monday.AddHours(18), runs[^1]);
        }

        [TestMethod]
        public void EveryNMinutes_WindowThroughMidnight()
        {
            var runs = Runs(EveryMinutes(60, 220000, 20000), Monday, Monday.AddDays(1));
            // 00:00, 01:00, 02:00 carried over from Sunday's window, then 22:00 and 23:00
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 22, 23 }, runs.Select(r => r.Hour).ToArray());
        }

        [TestMethod]
        public void EverySeconds()
        {
            var s = new AgentSchedule { FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdaySeconds, FrequencySubdayInterval = 10, ActiveStartDate = 20260101 };
            Assert.AreEqual(360, Runs(s, Monday, Monday.AddHours(1)).Count);
        }

        [TestMethod]
        public void EveryNDays_CountsFromStartDate()
        {
            // Starts Tuesday 3 Nov, every 2 days
            var runs = Runs(DailyAt(10000, 2, 20261103), Monday, Monday.AddDays(7));
            CollectionAssert.AreEqual(new[] { 3, 5, 7 }, runs.Select(r => r.Day).ToArray());
            Assert.IsTrue(runs.All(r => r.Hour == 1));
        }

        [TestMethod]
        public void ActiveDateRange_Respected()
        {
            var s = DailyAt(0, 1, 20261104) with { ActiveEndDate = 20261105 };
            CollectionAssert.AreEqual(new[] { 4, 5 }, Runs(s, Monday, Monday.AddDays(7)).Select(r => r.Day).ToArray());
        }

        [TestMethod]
        public void Weekly_DaysAndRecurrence()
        {
            var s = new AgentSchedule
            {
                FrequencyType = AgentSchedule.Weekly,
                FrequencyInterval = AgentScheduleProjector.DayBit(DayOfWeek.Monday) | AgentScheduleProjector.DayBit(DayOfWeek.Friday),
                FrequencyRecurrenceFactor = 2,
                FrequencySubdayType = AgentSchedule.SubdayAtTime,
                ActiveStartDate = 20261102,
                ActiveStartTime = 233000
            };
            var runs = Runs(s, Monday, Monday.AddDays(28));
            // Weeks 0 and 2 only
            CollectionAssert.AreEqual(new[] { new DateTime(2026, 11, 2), new DateTime(2026, 11, 6), new DateTime(2026, 11, 16), new DateTime(2026, 11, 20) },
                runs.Select(r => r.Date).ToArray());
            Assert.AreEqual(new TimeSpan(23, 30, 0), runs[0].TimeOfDay);
        }

        [TestMethod]
        public void Monthly_DayOfMonth_SkipsShortMonths()
        {
            var s = new AgentSchedule { FrequencyType = AgentSchedule.Monthly, FrequencyInterval = 31, FrequencyRecurrenceFactor = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartDate = 20260101 };
            var runs = Runs(s, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7, 8, 10, 12 }, runs.Select(r => r.Month).ToArray());
        }

        [TestMethod]
        public void MonthlyRelative_LastWeekdayAndFirstSunday()
        {
            var lastWeekday = new AgentSchedule { FrequencyType = AgentSchedule.MonthlyRelative, FrequencyInterval = 9, FrequencyRelativeInterval = 16, FrequencyRecurrenceFactor = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartDate = 20260101 };
            // Last weekday of October 2026 is Friday 30th (31st is a Saturday)
            Assert.AreEqual(new DateTime(2026, 10, 30), Runs(lastWeekday, new DateTime(2026, 10, 1), new DateTime(2026, 11, 1)).Single());

            var firstSunday = lastWeekday with { FrequencyInterval = 1, FrequencyRelativeInterval = 1 };
            Assert.AreEqual(new DateTime(2026, 11, 1), Runs(firstSunday, new DateTime(2026, 11, 1), new DateTime(2026, 12, 1)).Single());
        }

        [TestMethod]
        public void NotProjectable_AgentStartAndIdle()
        {
            Assert.IsFalse(AgentScheduleProjector.IsProjectable(new AgentSchedule { FrequencyType = AgentSchedule.AgentStart }, out var reason));
            Assert.IsNotNull(reason);
            Assert.IsFalse(AgentScheduleProjector.IsProjectable(new AgentSchedule { FrequencyType = AgentSchedule.Idle }, out _));
            Assert.AreEqual(0, Runs(new AgentSchedule { FrequencyType = AgentSchedule.Idle }, Monday, Monday.AddDays(1)).Count);
        }

        [TestMethod]
        public void DisabledSchedule_DoesNotRun()
        {
            Assert.AreEqual(0, Runs(EveryMinutes(5) with { Enabled = false }, Monday, Monday.AddDays(1)).Count);
        }

        [TestMethod]
        public void Once()
        {
            var s = new AgentSchedule { FrequencyType = AgentSchedule.Once, ActiveStartDate = 20261103, ActiveStartTime = 120000 };
            Assert.AreEqual(new DateTime(2026, 11, 3, 12, 0, 0), Runs(s, Monday, Monday.AddDays(7)).Single());
        }

        [TestMethod]
        public void Cycle_EveryNMinutes()
        {
            Assert.IsTrue(AgentScheduleProjector.TryGetCycle(EveryMinutes(15, 500), out var period, out var offset, out var allDay));
            Assert.AreEqual(900, period);
            Assert.AreEqual(300, offset);
            Assert.IsTrue(allDay);

            Assert.IsTrue(AgentScheduleProjector.TryGetCycle(EveryMinutes(15, 80000, 180000), out _, out _, out allDay));
            Assert.IsFalse(allDay);

            // 7 minutes doesn't divide a day, so the cadence restarts each day
            Assert.IsFalse(AgentScheduleProjector.TryGetCycle(EveryMinutes(7), out _, out _, out _));
        }

        [TestMethod]
        public void Cycle_EveryNDays_OffsetFollowsStartDate()
        {
            Assert.IsTrue(AgentScheduleProjector.TryGetCycle(DailyAt(10000, 2, 20000102), out var period, out var offset, out _));
            Assert.AreEqual(2 * 86400, period);
            Assert.AreEqual(3600, offset);
            Assert.IsTrue(AgentScheduleProjector.TryGetCycle(DailyAt(10000, 2, 20000103), out _, out offset, out _));
            Assert.AreEqual(86400 + 3600, offset);
        }

        [TestMethod]
        public void Cycle_MatchesProjection()
        {
            // Every projected run sits on the cycle
            foreach (var s in new[] { EveryMinutes(20, 1000), DailyAt(31500, 3, 20260105) })
            {
                Assert.IsTrue(AgentScheduleProjector.TryGetCycle(s, out var period, out var offset, out _));
                foreach (var run in Runs(s, Monday, Monday.AddDays(10)))
                {
                    Assert.AreEqual(offset, (long)(run - AgentScheduleProjector.Epoch).TotalSeconds % period, $"{s} at {run}");
                }
            }
        }

        [TestMethod]
        public void CompareCycles_Verdicts()
        {
            var every10 = new JobCycle(600, 0, true);
            var every15 = new JobCycle(900, 0, true);
            // gcd 300s.  Both start on the hour: collide.
            Assert.AreEqual(CycleVerdict.Collides, JobOverlapAnalyzer.CompareCycles(every10, 60, every15, 60, out var common));
            Assert.AreEqual(300, common);
            // Second one 2 minutes later: clear
            Assert.AreEqual(CycleVerdict.Clear, JobOverlapAnalyzer.CompareCycles(every10, 60, every15 with { OffsetSec = 120 }, 60, out _));
            // Runs of 200s + 200s don't fit in 300s
            Assert.AreEqual(CycleVerdict.CannotSeparate, JobOverlapAnalyzer.CompareCycles(every10, 200, every15, 200, out _));
            Assert.AreEqual(CycleVerdict.AlwaysRunning, JobOverlapAnalyzer.CompareCycles(every10, 600, every15, 10, out _));
        }

        [TestMethod]
        public void CompareCycles_AgreesWithProjection()
        {
            // Exhaustive check of the gcd rule against brute force projection for small cycles
            for (var offsetB = 0; offsetB < 600; offsetB += 30)
            {
                var a = new JobScheduleInput { JobId = Guid.NewGuid(), Schedules = new[] { EveryMinutes(10) }, ExpectedDurationSec = 90 };
                var b = new JobScheduleInput { JobId = Guid.NewGuid(), Schedules = new[] { EveryMinutes(15) with { FrequencySubdayType = AgentSchedule.SubdaySeconds, FrequencySubdayInterval = 900, ActiveStartTime = offsetB / 60 * 100 + offsetB % 60 } }, ExpectedDurationSec = 60 };
                var verdict = JobOverlapAnalyzer.CompareCycles(JobOverlapAnalyzer.GetCycle(a)!.Value, 90, JobOverlapAnalyzer.GetCycle(b)!.Value, 60, out _);
                var (count, _) = JobOverlapAnalyzer.CountOverlaps(
                    JobOverlapAnalyzer.ProjectRuns(a, Monday, Monday.AddDays(1), out _),
                    JobOverlapAnalyzer.ProjectRuns(b, Monday, Monday.AddDays(1), out _));
                Assert.AreEqual(verdict == CycleVerdict.Clear, count == 0, $"offset {offsetB}s: {verdict}, {count} overlaps");
            }
        }

        [TestMethod]
        public void ProjectRuns_SkipsStartsWhileRunning()
        {
            var job = new JobScheduleInput { Schedules = new[] { EveryMinutes(5) }, ExpectedDurationSec = 7 * 60 };
            var runs = JobOverlapAnalyzer.ProjectRuns(job, Monday, Monday.AddHours(1), out _);
            // 7 minute runs every 5 minutes start every 10 - the 23:55 run still going at midnight holds off the 00:00 start
            Assert.AreEqual(7, runs.Count);
            Assert.AreEqual(Monday.AddMinutes(-5), runs[0].Start);
            Assert.AreEqual(Monday.AddMinutes(5), runs[1].Start);
            Assert.AreEqual(Monday.AddMinutes(15), runs[2].Start);
        }

        [TestMethod]
        public void ProjectRuns_MergesSchedules()
        {
            var job = new JobScheduleInput { Schedules = new[] { DailyAt(10000), DailyAt(130000), DailyAt(10000) }, ExpectedDurationSec = 60 };
            var runs = JobOverlapAnalyzer.ProjectRuns(job, Monday, Monday.AddDays(1), out _);
            CollectionAssert.AreEqual(new[] { 1, 13 }, runs.Select(r => r.Start.Hour).ToArray());
            Assert.AreSame(job.Schedules[0], runs[0].Schedule, "Each run says which schedule starts it - the first, when two share a time");
            Assert.AreSame(job.Schedules[1], runs[1].Schedule);
            Assert.IsNull(JobOverlapAnalyzer.GetCycle(job), "More than one schedule has no single cycle");
        }

        [TestMethod]
        public void ProjectRuns_KeepsARunStillGoingAtTheStart()
        {
            // 30 minute runs at 23:50 and 00:10: the 23:50 run holds off the 00:10 start
            var job = new JobScheduleInput { Schedules = new[] { DailyAt(235000), DailyAt(1000) }, ExpectedDurationSec = 30 * 60 };
            var runs = JobOverlapAnalyzer.ProjectRuns(job, Monday, Monday.AddHours(1), out _, out var clashes);
            Assert.AreEqual(Monday.AddMinutes(-10), runs.Single().Start);
            Assert.AreEqual(1, clashes);
        }

        [TestMethod]
        public void Cycle_OfAJobRunningLongerThanItsInterval_IsWhenItReallyStarts()
        {
            // 7 minutes every 5 starts every 10 - room for a 2 minute job, not always running
            var overrun = new JobScheduleInput { Schedules = new[] { EveryMinutes(5) }, ExpectedDurationSec = 7 * 60 };
            Assert.AreEqual(600, JobOverlapAnalyzer.GetCycle(overrun)!.Value.PeriodSec);
            var other = new JobScheduleInput { Schedules = new[] { EveryMinutes(10) }, ExpectedDurationSec = 120 };
            Assert.AreNotEqual(CycleVerdict.AlwaysRunning, JobOverlapAnalyzer.CompareCycles(JobOverlapAnalyzer.GetCycle(overrun)!.Value, 7 * 60, JobOverlapAnalyzer.GetCycle(other)!.Value, 120, out _));
            // Back to back is always running
            var backToBack = new JobScheduleInput { Schedules = new[] { EveryMinutes(5) }, ExpectedDurationSec = 10 * 60 };
            Assert.AreEqual(CycleVerdict.AlwaysRunning, JobOverlapAnalyzer.CompareCycles(JobOverlapAnalyzer.GetCycle(backToBack)!.Value, 10 * 60, JobOverlapAnalyzer.GetCycle(other)!.Value, 120, out _));
        }

        [TestMethod]
        public void ProjectRuns_CountsClashesBetweenSchedules()
        {
            // 01:00 and 01:00 share a time; 01:00:30 comes while that run (60s) is going
            var job = new JobScheduleInput { Schedules = new[] { DailyAt(10000), DailyAt(10000), DailyAt(10030) }, ExpectedDurationSec = 60 };
            JobOverlapAnalyzer.ProjectRuns(job, Monday, Monday.AddDays(1), out _, out var clashes);
            Assert.AreEqual(2, clashes);
            // A run longer than its own interval isn't a clash between schedules
            var overrun = new JobScheduleInput { Schedules = new[] { EveryMinutes(5) }, ExpectedDurationSec = 7 * 60 };
            JobOverlapAnalyzer.ProjectRuns(overrun, Monday, Monday.AddHours(1), out _, out clashes);
            Assert.AreEqual(0, clashes);
        }

        [TestMethod]
        public void ScheduleMath_GcdModAndText()
        {
            Assert.AreEqual(300, ScheduleMath.Gcd(900, 600));
            Assert.AreEqual(55, ScheduleMath.Mod(-5, 60));
            Assert.AreEqual(55L, ScheduleMath.Mod(-5L, 60L));
            Assert.AreEqual("1h 5m 30s", ScheduleMath.Span(3930));
            Assert.AreEqual("0s", ScheduleMath.Span(0));
            Assert.AreEqual("-45s", ScheduleMath.Shift(-45));
            Assert.AreEqual("+2m", ScheduleMath.Shift(120));
            Assert.AreEqual("0", ScheduleMath.Shift(0));
        }

        [TestMethod]
        public void Describe_AsAgentPutsIt()
        {
            Assert.AreEqual("Daily every 1 day(s), every 15 minutes, between 00:05:00 and 23:59:59", EveryMinutes(15, 500).Describe());
            Assert.AreEqual("Daily every 1 day(s), at 01:00:00", DailyAt(10000).Describe());
            var relative = new AgentSchedule { FrequencyType = AgentSchedule.MonthlyRelative, FrequencyInterval = 2, FrequencyRelativeInterval = 16, FrequencyRecurrenceFactor = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartTime = 230000 };
            Assert.AreEqual("Last Monday of every 1 month(s), at 23:00:00", relative.Describe());
            Assert.AreEqual("When the SQL Server agent starts", new AgentSchedule { FrequencyType = AgentSchedule.AgentStart }.Describe());
        }

        [TestMethod]
        public void Cycle_IgnoresAOneTimeSchedule()
        {
            var once = new AgentSchedule { FrequencyType = AgentSchedule.Once, ActiveStartDate = 20250301, ActiveStartTime = 120000 };
            var job = new JobScheduleInput { Schedules = new[] { DailyAt(10000), once }, ExpectedDurationSec = 60 };
            Assert.AreEqual(86400, JobOverlapAnalyzer.GetCycle(job)?.PeriodSec, "A one-off left from long ago isn't part of the cadence");
        }

        [TestMethod]
        public void CountOverlaps_CountsPairsAndTime()
        {
            var a = new List<JobRun> { new(Monday, Monday.AddMinutes(10)), new(Monday.AddMinutes(20), Monday.AddMinutes(30)) };
            var b = new List<JobRun> { new(Monday.AddMinutes(5), Monday.AddMinutes(6)), new(Monday.AddMinutes(8), Monday.AddMinutes(25)), new(Monday.AddMinutes(30), Monday.AddMinutes(40)) };
            var aFlags = new bool[2];
            var bFlags = new bool[3];
            var (count, seconds) = JobOverlapAnalyzer.CountOverlaps(a, b, aFlags, bFlags);
            Assert.AreEqual(3, count);
            Assert.AreEqual((1 + 2 + 5) * 60, seconds);
            CollectionAssert.AreEqual(new[] { true, true }, aFlags);
            CollectionAssert.AreEqual(new[] { true, true, false }, bFlags, "Touching end to start isn't an overlap");
        }

        [TestMethod]
        public void Shift_AllDayInterval_MovesEveryRun()
        {
            foreach (var seconds in new[] { 125, -125, 25 * 60, -3 * 3600 })
            {
                Assert.IsTrue(AgentScheduleShifter.TryShift(EveryMinutes(10), seconds, out var shifted, out var reason), reason);
                Assert.AreEqual(235959, shifted.ActiveEndTime);
                var expected = Runs(EveryMinutes(10), Monday.AddDays(-1), Monday.AddDays(2)).Select(r => r.AddSeconds(seconds))
                    .Where(r => r >= Monday && r < Monday.AddDays(1));
                CollectionAssert.AreEqual(expected.ToList(), Runs(shifted, Monday, Monday.AddDays(1)), $"shift {seconds}");
            }
            Assert.IsTrue(AgentScheduleShifter.TryShift(EveryMinutes(10), 125, out var s, out _));
            Assert.AreEqual(205, s.ActiveStartTime, "Earliest equivalent start: 00:02:05");
        }

        [TestMethod]
        public void Shift_PartOfDayWindow()
        {
            Assert.IsTrue(AgentScheduleShifter.TryShift(EveryMinutes(30, 80000, 180000), 1800, out var shifted, out _));
            Assert.AreEqual(83000, shifted.ActiveStartTime);
            Assert.AreEqual(183000, shifted.ActiveEndTime);
            Assert.IsFalse(AgentScheduleShifter.TryShift(EveryMinutes(30, 80000, 180000), 7 * 3600, out shifted, out var reason));
            Assert.IsNotNull(reason);
            Assert.AreEqual(80000, shifted.ActiveStartTime, "Unchanged when refused");

            // Through midnight: 22:00-02:00 moved an hour later is 23:00-03:00, but not to start after midnight
            Assert.IsTrue(AgentScheduleShifter.TryShift(EveryMinutes(60, 220000, 20000), 3600, out shifted, out _));
            Assert.AreEqual((230000, 30000), (shifted.ActiveStartTime, shifted.ActiveEndTime));
            Assert.IsFalse(AgentScheduleShifter.TryShift(EveryMinutes(60, 220000, 20000), 3 * 3600, out _, out _));
        }

        [TestMethod]
        public void Shift_AtTime_PastMidnightOnlyForEveryDay()
        {
            Assert.IsTrue(AgentScheduleShifter.TryShift(DailyAt(233000), 3600, out var shifted, out _));
            Assert.AreEqual(3000, shifted.ActiveStartTime);
            Assert.IsTrue(AgentScheduleShifter.TryShift(DailyAt(3000), -3600, out shifted, out _));
            Assert.AreEqual(233000, shifted.ActiveStartTime);

            Assert.IsFalse(AgentScheduleShifter.TryShift(DailyAt(233000, 2), 3600, out _, out var reason, changeDays: false));
            Assert.IsNotNull(reason);
            var weekly = Weekly(2, 1, 233000);
            Assert.IsFalse(AgentScheduleShifter.TryShift(weekly, 3600, out _, out _, changeDays: false));
            Assert.IsTrue(AgentScheduleShifter.TryShift(weekly, -3600, out shifted, out _, changeDays: false));
            Assert.AreEqual(223000, shifted.ActiveStartTime);
        }

        private static AgentSchedule Weekly(int days, int everyWeeks, int time, int startDate = 20260101, int subdayMinutes = 0, int endTime = 235959) => new()
        {
            FrequencyType = AgentSchedule.Weekly,
            FrequencyInterval = days,
            FrequencyRecurrenceFactor = everyWeeks,
            FrequencySubdayType = subdayMinutes > 0 ? AgentSchedule.SubdayMinutes : AgentSchedule.SubdayAtTime,
            FrequencySubdayInterval = subdayMinutes,
            ActiveStartDate = startDate,
            ActiveStartTime = time,
            ActiveEndTime = endTime
        };

        /// <summary>Asserts the moved schedule runs at each of the original's run times moved by <paramref name="seconds"/></summary>
        /// <param name="from">Where to compare from - before the schedule's start date to check the first runs too</param>
        private static void AssertRunsMoved(AgentSchedule schedule, int seconds, DateTime? from = null)
        {
            Assert.IsTrue(AgentScheduleShifter.TryShift(schedule, seconds, out var shifted, out var reason), reason);
            AssertSameRuns(schedule, shifted, seconds, from ?? new DateTime(2026, 11, 1));
        }

        private static void AssertSameRuns(AgentSchedule schedule, AgentSchedule shifted, int seconds, DateTime from, string what = "")
        {
            var to = from.AddMonths(4);
            List<DateTime> Starts(AgentSchedule s, int offset) => JobOverlapAnalyzer.ProjectRuns(new JobScheduleInput { Schedules = new[] { s }, ExpectedDurationSec = 1 }, from.AddDays(-2), to.AddDays(2), out _)
                .Select(r => r.Start.AddSeconds(offset)).Where(t => t >= from && t < to).ToList();
            var expected = Starts(schedule, seconds);
            Assert.IsTrue(expected.Count > 0, what);
            CollectionAssert.AreEqual(expected, Starts(shifted, 0), what);
        }

        [TestMethod]
        public void Shift_PastMidnight_RunsFromTheStartDate()
        {
            // Every start weekday, days and direction: when a move is allowed, each run moves - the first ones after the
            // start date too (every 2 weeks Mon/Wed from Sunday the 4th, an hour earlier, still runs on the 4th and 6th)
            var masks = new[] { 1, 2, 64, 2 | 8, 1 | 64, 2 | 8 | 32, 127 };
            for (var start = 0; start < 7; start++)
            {
                var startDate = new DateTime(2026, 1, 4).AddDays(start);
                foreach (var weeks in new[] { 1, 2, 3 })
                foreach (var mask in masks)
                foreach (var (time, seconds) in new[] { (0, -3600), (0, -3 * 3600), (230000, 3600), (220000, 3 * 3600), (120000, 13 * 3600), (120000, -13 * 3600) })
                {
                    var schedule = Weekly(mask, weeks, time, startDate: startDate.Year * 10000 + startDate.Month * 100 + startDate.Day);
                    // A schedule that runs every day keeps its start date: only the run on the day before it is lost
                    if (AgentScheduleShifter.IsEveryDay(schedule) || !AgentScheduleShifter.TryShift(schedule, seconds, out var shifted, out _)) continue;
                    AssertSameRuns(schedule, shifted, seconds, startDate.AddDays(-7), $"from {startDate:ddd d}, every {weeks} weeks, days {mask}, {seconds / 3600}h");
                }
                foreach (var every in new[] { 2, 3 })
                {
                    var daily = DailyAt(0, every, startDate.Year * 10000 + startDate.Month * 100 + startDate.Day);
                    Assert.IsTrue(AgentScheduleShifter.TryShift(daily, -3600, out var shifted, out _));
                    AssertSameRuns(daily, shifted, -3600, startDate.AddDays(-7), $"from {startDate:ddd d}, every {every} days");
                }
            }
        }

        [TestMethod]
        public void Shift_PastMidnight_MovesTheDays()
        {
            AssertRunsMoved(Weekly(1, 1, 0), -3 * 3600); // Sunday 00:00 to Saturday 21:00
            AssertRunsMoved(Weekly(1 | 4 | 16, 1, 230000), 2 * 3600); // Sun/Tue/Thu 23:00 to Mon/Wed/Fri 01:00
            AssertRunsMoved(Weekly(64, 1, 220000), 3 * 3600); // Saturday into Sunday
            AssertRunsMoved(DailyAt(3000, 3), -3600);
            AssertRunsMoved(DailyAt(233000, 2), 3600);
            AssertRunsMoved(Weekly(2, 1, 0, subdayMinutes: 30, endTime: 20000), -3 * 3600); // A window, Monday 00:00-02:00 to Sunday 21:00-23:00

            Assert.IsTrue(AgentScheduleShifter.TryShift(Weekly(1, 1, 0), -3 * 3600, out var shifted, out _));
            Assert.AreEqual(64, shifted.FrequencyInterval);
            Assert.AreEqual(210000, shifted.ActiveStartTime);
        }

        [TestMethod]
        public void Shift_PastMidnight_EveryNWeeks()
        {
            // Every 2 weeks: Sunday a day earlier is Saturday of the week before, so the week it's counted from moves too -
            // whatever day it starts on
            AssertRunsMoved(Weekly(1, 2, 0, startDate: 20260104), -3 * 3600); // Starting on a Sunday
            AssertRunsMoved(Weekly(1, 2, 0, startDate: 20260107), -3 * 3600); // And mid week
            AssertRunsMoved(Weekly(64, 3, 230000, startDate: 20260107), 3600);
            AssertRunsMoved(Weekly(2 | 8, 2, 0, startDate: 20260107), -3600); // Mon/Wed to Sun/Tue, all in the same week

            Assert.IsFalse(AgentScheduleShifter.TryShift(Weekly(1 | 8, 2, 0), -3600, out _, out var reason), "Sunday leaves the week, Wednesday doesn't");
            StringAssert.Contains(reason, "every 2 weeks");
        }

        [TestMethod]
        public void Shift_PastMidnight_RefusedForMonthly()
        {
            var monthly = new AgentSchedule { FrequencyType = AgentSchedule.Monthly, FrequencyInterval = 1, FrequencyRecurrenceFactor = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime, ActiveStartDate = 20260101, ActiveStartTime = 0 };
            Assert.IsFalse(AgentScheduleShifter.TryShift(monthly, -3600, out _, out var reason));
            StringAssert.Contains(reason, "day of the month");
            Assert.IsTrue(AgentScheduleShifter.TryShift(monthly, 3600, out _, out _));
        }

        [TestMethod]
        public void Shift_RefusesWhatCantMove()
        {
            Assert.IsFalse(AgentScheduleShifter.TryShift(new AgentSchedule { FrequencyType = AgentSchedule.AgentStart }, 60, out _, out _));
            Assert.IsFalse(AgentScheduleShifter.TryShift(new AgentSchedule { FrequencyType = AgentSchedule.Once, ActiveStartDate = 20261103 }, 60, out _, out _));
            Assert.IsTrue(AgentScheduleShifter.TryShift(new AgentSchedule { FrequencyType = AgentSchedule.Idle }, 0, out _, out _), "No move is always fine");
        }

        [TestMethod]
        public void Analyze_ReportsActualAndPredicted()
        {
            var a = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "A", Schedules = new[] { EveryMinutes(10) }, ExpectedDurationSec = 120 };
            var b = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "B", Schedules = new[] { EveryMinutes(15) }, ExpectedDurationSec = 60 };
            var c = new JobScheduleInput { JobId = Guid.NewGuid(), Name = "C", Schedules = new[] { new AgentSchedule { FrequencyType = AgentSchedule.AgentStart } } };
            var actual = new Dictionary<Guid, IReadOnlyList<JobRun>>
            {
                [a.JobId] = new List<JobRun> { new(Monday, Monday.AddMinutes(2)) },
                [b.JobId] = new List<JobRun> { new(Monday.AddMinutes(1), Monday.AddMinutes(3)) }
            };
            var report = JobOverlapAnalyzer.Analyze(new[] { a, b, c }, actual, Monday, Monday.AddHours(1));

            var pair = report.Pairs.Single();
            Assert.AreEqual(1, pair.ActualOverlaps);
            Assert.AreEqual(60, pair.ActualOverlapSec);
            // Both run on the half hour: 00:00 and 00:30
            Assert.AreEqual(2, pair.PredictedOverlaps);
            Assert.AreEqual(CycleVerdict.Collides, pair.Verdict);
            Assert.AreEqual(300, pair.CommonCycleSec);

            var summaryA = report.Jobs.Single(j => j.Job == a);
            Assert.AreEqual(6, summaryA.ProjectedRuns);
            Assert.AreEqual(0.2, summaryA.BusyFraction!.Value, 1e-9);
            Assert.AreEqual(1, summaryA.ActualRunsOverlapping);
            Assert.IsNotNull(report.Jobs.Single(j => j.Job == c).NotProjectableReason);
        }
    }
}
