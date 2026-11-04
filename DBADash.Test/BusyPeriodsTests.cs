using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class BusyPeriodsTests
    {
        private static readonly DateTime Monday = new(2026, 11, 2);

        private static readonly DayOfWeek[] Weekdays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        /// <summary>
        /// Busy periods as dbo.BusyPeriods_Get expands them: from <paramref name="fromTime"/> to <paramref name="toTime"/> on
        /// each of the given days (every day when none are given), crossing midnight when toTime is earlier - over a week
        /// either side of <see cref="Monday"/>
        /// </summary>
        internal static BusyPeriods Busy(DateTime around, TimeSpan fromTime, TimeSpan toTime, params DayOfWeek[] days)
        {
            var intervals = new List<BusyInterval>();
            for (var day = around.AddDays(-7); day < around.AddDays(14); day = day.AddDays(1))
            {
                if (days.Length > 0 && !days.Contains(day.DayOfWeek)) continue;
                var end = toTime > fromTime ? day + toTime : day.AddDays(1) + toTime;
                intervals.Add(new BusyInterval(day + fromTime, end));
            }
            return new BusyPeriods(intervals);
        }

        private static JobScheduleInput Job(int subdayType, int subdayInterval, int time, int durationSec, int frequencyType = AgentSchedule.Daily, int frequencyInterval = 1) => new()
        {
            JobId = Guid.NewGuid(),
            Name = "Job",
            ExpectedDurationSec = durationSec,
            Schedules = new[]
            {
                new AgentSchedule
                {
                    FrequencyType = frequencyType, FrequencyInterval = frequencyInterval, FrequencyRecurrenceFactor = 1, FrequencySubdayType = subdayType,
                    FrequencySubdayInterval = subdayInterval, ActiveStartDate = 20260101, ActiveStartTime = time
                }
            }
        };

        [TestMethod]
        public void CanAvoid_OnceADayOrLessOften()
        {
            var busy = Busy(Monday, TimeSpan.FromHours(8), TimeSpan.FromHours(18), Weekdays);
            Assert.IsTrue(busy.CanAvoid(Job(AgentSchedule.SubdayAtTime, 0, 90000, 1800)), "Daily");
            Assert.IsTrue(busy.CanAvoid(Job(AgentSchedule.SubdayAtTime, 0, 90000, 1800, AgentSchedule.Weekly, 2)), "Weekly");
        }

        [TestMethod]
        public void CanAvoid_LongCycleCan_ShortCycleCant()
        {
            var busy = Busy(Monday, TimeSpan.FromHours(8), TimeSpan.FromHours(18), Weekdays);
            Assert.IsTrue(busy.CanAvoid(Job(AgentSchedule.SubdayHours, 12, 0, 600)), "12h between runs leaves 2h outside a 10h busy stretch");
            Assert.IsFalse(busy.CanAvoid(Job(AgentSchedule.SubdayHours, 6, 0, 600)), "A 6h cycle can't skip a 10h busy stretch");
            Assert.IsFalse(busy.CanAvoid(Job(AgentSchedule.SubdayMinutes, 1, 0, 30)), "Every minute");
            Assert.IsFalse(BusyPeriods.None.CanAvoid(Job(AgentSchedule.SubdayAtTime, 0, 90000, 60)), "No busy periods, nothing to avoid");
        }

        [TestMethod]
        public void Intervals_MergedWhereTheyTouch()
        {
            var busy = new BusyPeriods(new[]
            {
                new BusyInterval(Monday.AddHours(13), Monday.AddHours(18)),
                new BusyInterval(Monday.AddHours(8), Monday.AddHours(12)),
                new BusyInterval(Monday.AddHours(12), Monday.AddHours(14))
            });
            Assert.AreEqual(1, busy.Intervals.Count);
            Assert.AreEqual(10 * 3600, busy.LongestStretchSec);
        }

        [TestMethod]
        public void CrossingMidnight_LongestStretch()
        {
            var busy = Busy(Monday, TimeSpan.FromHours(22), TimeSpan.FromHours(6));
            Assert.AreEqual(8 * 3600, busy.LongestStretchSec);
        }

        [TestMethod]
        public void Measure_CountsRunsAndSeconds()
        {
            var busy = Busy(Monday, TimeSpan.FromHours(8), TimeSpan.FromHours(18), Weekdays);
            var runs = new List<JobRun>
            {
                new(Monday.AddHours(7.5), Monday.AddHours(8.5)), // half an hour in
                new(Monday.AddHours(12), Monday.AddHours(12.25)), // all in
                new(Monday.AddHours(19), Monday.AddHours(20)), // clear
                new(Monday.AddDays(5).AddHours(12), Monday.AddDays(5).AddHours(13)) // Saturday: clear
            };
            var flags = new bool[runs.Count];
            var amount = busy.Measure(runs, flags);
            Assert.AreEqual(2, amount.Runs);
            Assert.AreEqual(2700, amount.Seconds);
            CollectionAssert.AreEqual(new[] { true, true, false, false }, flags);
        }
    }
}
