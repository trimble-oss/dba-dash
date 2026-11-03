using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class ClockChangeTests
    {
        private static readonly DateTime Monday = new(2026, 10, 19);

        private static JobScheduleInput DailyJob(string name, int time) => new()
        {
            JobId = Guid.NewGuid(),
            Name = name,
            Schedules = new[]
            {
                new AgentSchedule
                {
                    Name = name, FrequencyType = AgentSchedule.Daily, FrequencyInterval = 1, FrequencySubdayType = AgentSchedule.SubdayAtTime,
                    ActiveStartDate = 20260101, ActiveStartTime = time, ScheduleUid = Guid.NewGuid().ToString()
                }
            }
        };

        /// <summary>A week of the job's daily runs as converted with the offset now: the first <paramref name="hourOutDays"/> an hour early</summary>
        private static IReadOnlyList<JobRun> Week(JobScheduleInput job, int hour, int hourOutDays, string name) =>
            Enumerable.Range(0, 7).Select(d =>
            {
                var start = Monday.AddDays(d).AddHours(d < hourOutDays ? hour - 1 : hour);
                return new JobRun(start, start.AddMinutes(5)) { InvokedBy = $"Schedule 3 ({name}).  The last step to run was step 1 (x)." };
            }).ToList();

        [TestMethod]
        public void RunsAnHourOutBeforeAChange_AreFound()
        {
            var a = DailyJob("Backup", 20000);
            var b = DailyJob("Index", 50000);
            var runs = new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Week(a, 2, 3, "Backup"), [b.JobId] = Week(b, 5, 3, "Index") };
            var change = ClockChanges.Find(new[] { a, b }, runs);
            Assert.IsNotNull(change);
            Assert.AreEqual(60, change.Value.Minutes);
            Assert.AreEqual(Monday.AddDays(3).AddHours(2), change.Value.Until, "From the first run that fits as converted");
            Assert.AreEqual(Monday.AddHours(2), change.Value.Correct(Monday.AddHours(1)));
            Assert.AreEqual(Monday.AddDays(4).AddHours(2), change.Value.Correct(Monday.AddDays(4).AddHours(2)));
        }

        [TestMethod]
        public void AllRunsAnHourOut_ChangedSinceThePeriod()
        {
            var a = DailyJob("Backup", 20000);
            var b = DailyJob("Index", 50000);
            var runs = new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Week(a, 2, 7, "Backup"), [b.JobId] = Week(b, 5, 7, "Index") };
            Assert.AreEqual(new ClockChange(DateTime.MaxValue, 60), ClockChanges.Find(new[] { a, b }, runs));
        }

        [TestMethod]
        public void OneJobMovedByAnHour_IsntAClockChange()
        {
            var a = DailyJob("Backup", 20000);
            var b = DailyJob("Index", 50000);
            var runs = new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Week(a, 2, 3, "Backup"), [b.JobId] = Week(b, 5, 0, "Index") };
            Assert.IsNull(ClockChanges.Find(new[] { a, b }, runs));
        }

        [TestMethod]
        public void NoChange()
        {
            var a = DailyJob("Backup", 20000);
            var b = DailyJob("Index", 50000);
            var runs = new Dictionary<Guid, IReadOnlyList<JobRun>> { [a.JobId] = Week(a, 2, 0, "Backup"), [b.JobId] = Week(b, 5, 0, "Index") };
            Assert.IsNull(ClockChanges.Find(new[] { a, b }, runs));
        }
    }
}
