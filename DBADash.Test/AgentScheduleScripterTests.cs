using DBADash.JobSchedules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class AgentScheduleScripterTests
    {
        private const string Uid = "11111111-2222-3333-4444-555555555555";

        private static readonly AgentSchedule Every15 = new()
        {
            Name = "Every 15 min",
            FrequencyType = AgentSchedule.Daily,
            FrequencyInterval = 1,
            FrequencySubdayType = AgentSchedule.SubdayMinutes,
            FrequencySubdayInterval = 15,
            ActiveStartTime = 0,
            ActiveEndTime = 235959,
            ScheduleUid = Uid
        };

        private static ScheduleChange Moved(AgentSchedule schedule, int seconds, params string[] jobs)
        {
            Assert.IsTrue(AgentScheduleShifter.TryShift(schedule, seconds, out var shifted, out _));
            return new ScheduleChange(schedule, shifted, jobs);
        }

        [TestMethod]
        public void Apply_ChecksThenUpdates()
        {
            var script = AgentScheduleScripter.Build(new[] { Moved(Every15, 120, "ETL load") }, "SQL01", new DateTime(2026, 11, 1, 12, 0, 0)).Script;
            StringAssert.Contains(script, $"INSERT INTO @Schedules VALUES('{Uid}', N'Every 15 min', 4, 1, 4, 15, 0, 0, {Every15.ActiveStartDate}, {Every15.ActiveEndDate}, 1, 0, 235959, 200, 235959)");
            // How often and when it runs must be as collected too, not just its times
            foreach (var column in new[] { "freq_type", "freq_interval", "freq_subday_type", "freq_subday_interval", "freq_relative_interval", "freq_recurrence_factor", "active_start_date", "active_end_date", "enabled" })
            {
                StringAssert.Contains(script, $"OR SS.{column} <> S.{column}");
            }
            StringAssert.Contains(script, "how often or when it runs has changed since this script was generated");
            StringAssert.Contains(script, "/* ETL load: 00:00:00 -> 00:02:00 */");
            StringAssert.Contains(script, "\tEXEC @rc = msdb.dbo.sp_update_schedule @schedule_id = @schedule_id, @active_start_time = 200, @active_end_time = 235959");
            StringAssert.Contains(script, "IF @rc <> 0 GOTO Failed");
            StringAssert.Contains(script, "RAISERROR('Nothing was changed");
            // The check comes before the transaction
            Assert.IsTrue(script.IndexOf("IF @Problems IS NOT NULL", StringComparison.Ordinal) < script.IndexOf("BEGIN TRANSACTION", StringComparison.Ordinal));
            Assert.IsFalse(script.Contains("THROW"), "RAISERROR for SQL 2008");
        }

        [TestMethod]
        public void OneScript_RollbackChoosesTheDirection()
        {
            var built = AgentScheduleScripter.Build(new[] { Moved(Every15, 120, "ETL load") }, "SQL01", DateTime.UtcNow);
            Assert.AreEqual(1, built.Changes);
            var script = built.Script;
            StringAssert.Contains(script, "DECLARE @Rollback BIT = 0 /* Change to 1 to undo the changes */");
            // Rolling back expects the new times and puts the original ones back
            StringAssert.Contains(script, $"IF @Rollback = 1{Environment.NewLine}\tUPDATE @Schedules SET expected_start_time = new_start_time");
            var apply = script.IndexOf($"IF @Rollback = 0{Environment.NewLine}BEGIN", StringComparison.Ordinal);
            var undo = script.IndexOf($"ELSE{Environment.NewLine}BEGIN", StringComparison.Ordinal);
            Assert.IsTrue(apply > 0 && undo > apply);
            Assert.IsTrue(script.IndexOf("@active_start_time = 200,", StringComparison.Ordinal) is var to and > 0 && to > apply && to < undo, "Applying moves it");
            Assert.IsTrue(script.IndexOf("@active_start_time = 0,", StringComparison.Ordinal) > undo, "Rolling back puts it back");
            StringAssert.Contains(script, "IF @Rollback = 0 PRINT 'Done: 1 schedule moved.' ELSE PRINT 'Rolled back: 1 schedule moved.'");
        }

        [TestMethod]
        public void SkipsUnchangedAndUnfindable()
        {
            var noUid = Every15 with { ScheduleUid = null, Name = "Mystery" };
            var scripts = AgentScheduleScripter.Build(new[]
            {
                new ScheduleChange(Every15, Every15, new[] { "Unchanged" }),
                Moved(noUid, 60, "Job X")
            }, "SQL01", DateTime.UtcNow);
            Assert.AreEqual(0, scripts.Changes);
            Assert.AreEqual("Mystery (Job X): no schedule_uid to find it by", scripts.Skipped.Single());
            StringAssert.Contains(scripts.Script, "PRINT 'No schedule changes.'");
            StringAssert.Contains(scripts.Script, "Not scripted: Mystery (Job X)");
            Assert.IsFalse(scripts.Script.Contains("@Rollback"));
        }

        [TestMethod]
        public void ListsTheJobsThatKeepTheSchedule()
        {
            var jobId = Guid.NewGuid();
            var change = Moved(Every15, 120, "ETL load") with { JobIds = new[] { jobId } };
            var scripts = AgentScheduleScripter.Build(new[] { change }, "SQL01", DateTime.UtcNow);
            StringAssert.Contains(scripts.Script, $"INSERT INTO @ScheduleJobs VALUES('{Uid}', '{jobId}')");
            StringAssert.Contains(scripts.Script, "which would move too");
        }

        [TestMethod]
        public void Split_AddsACopyAndRollbackRemovesIt()
        {
            var jobId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
            var newUid = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
            Assert.IsTrue(AgentScheduleShifter.TryShift(Every15, 300, out var copy, out _));
            var split = new ScheduleSplit(Every15, copy, jobId, "ETL load", newUid);
            Assert.AreEqual("Every 15 min (ETL load)", split.NewName);

            var built = AgentScheduleScripter.Build(Array.Empty<ScheduleChange>(), new[] { split }, "SQL01", DateTime.UtcNow);
            Assert.AreEqual(1, built.Changes);
            var script = built.Script;
            var undoAt = script.IndexOf($"ELSE{Environment.NewLine}BEGIN", StringComparison.Ordinal);
            var apply = script[..undoAt];
            var undo = script[undoAt..];
            StringAssert.Contains(script, "1 job given its own schedule");
            StringAssert.Contains(script, "/* ETL load: own copy of Every 15 min, 00:00:00 -> 00:05:00 */");
            StringAssert.Contains(apply, $"@active_start_time = 500, @active_end_time = 235959, @owner_login_name = @owner, @schedule_uid = '{newUid}', @schedule_id = @new_schedule_id OUTPUT");
            StringAssert.Contains(apply, $"EXEC @rc = msdb.dbo.sp_attach_schedule @job_id = '{jobId}', @schedule_id = @new_schedule_id");
            StringAssert.Contains(apply, $"EXEC @rc = msdb.dbo.sp_detach_schedule @job_id = '{jobId}', @schedule_id = @schedule_id");
            StringAssert.Contains(apply, "already exists - has this script already been run?");
            Assert.IsFalse(apply.Contains("delete_unused_schedule"), "The shared schedule stays for the other jobs");

            StringAssert.Contains(undo, $"EXEC @rc = msdb.dbo.sp_attach_schedule @job_id = '{jobId}', @schedule_id = @schedule_id");
            StringAssert.Contains(undo, $"EXEC @rc = msdb.dbo.sp_detach_schedule @job_id = '{jobId}', @schedule_id = @new_schedule_id, @delete_unused_schedule = 1");
            StringAssert.Contains(script, "not found - have the changes been made?");
            // The copy is deleted on rolling back, so it must be just as the changes left it
            StringAssert.Contains(script, $"\t\tOR SS.active_end_date <> SP.active_end_date{Environment.NewLine}\t\tOR SS.enabled <> SP.enabled)");
            Assert.IsFalse(undo.Contains("sp_add_schedule"));
        }

        [TestMethod]
        public void Split_ChecksTheSharedScheduleIsAsCollected()
        {
            Assert.IsTrue(AgentScheduleShifter.TryShift(Every15, 300, out var copy, out _));
            var split = new ScheduleSplit(Every15 with { ActiveStartDate = 20260101 }, copy, Guid.NewGuid(), "ETL load", Guid.NewGuid());
            var script = AgentScheduleScripter.Build(Array.Empty<ScheduleChange>(), new[] { split }, "SQL01", DateTime.UtcNow).Script;
            // The copy is made from these, so a change to any of them since stops the script
            StringAssert.Contains(script, ", 4, 1, 4, 15, 0, 0, 20260101, 99991231, 1, 0, 235959, 500, 235959)");
            foreach (var column in new[] { "freq_relative_interval", "freq_recurrence_factor", "active_start_date", "active_end_date", "enabled" })
            {
                StringAssert.Contains(script, $"OR SS.{column} <> SP.{column}");
            }
            StringAssert.Contains(script, "AND (SS.active_start_time <> SP.expected_start_time OR SS.active_end_time <> SP.expected_end_time))");
        }

        [TestMethod]
        public void EachLookup_FailsWhenTheScheduleIsGone()
        {
            var script = AgentScheduleScripter.Build(new[] { Moved(Every15, 120, "ETL load") }, "SQL01", DateTime.UtcNow).Script;
            // Otherwise @schedule_id would keep the previous step's schedule
            StringAssert.Contains(script, $"\tSET @schedule_id = NULL{Environment.NewLine}\tSELECT @schedule_id = schedule_id FROM msdb.dbo.sysschedules WHERE schedule_uid = '{Uid}'");
            StringAssert.Contains(script, "IF @schedule_id IS NULL BEGIN PRINT N'Every 15 min: not found'; GOTO Failed; END");
        }

        [TestMethod]
        public void Split_NameFitsSysname()
        {
            var split = new ScheduleSplit(Every15 with { Name = new string('s', 100) }, Every15, Guid.NewGuid(), new string('j', 100), Guid.NewGuid());
            Assert.AreEqual(128, split.NewName.Length);
        }

        [TestMethod]
        public void NamesAreEscaped()
        {
            var odd = Every15 with { Name = "O'Brien's */ schedule" };
            var scripts = AgentScheduleScripter.Build(new[] { Moved(odd, 60, "Job */ DROP", "Second job") }, "SQL*/01", DateTime.UtcNow);
            StringAssert.Contains(scripts.Script, "N'O''Brien''s */ schedule'");
            StringAssert.Contains(scripts.Script, "/* Job * / DROP, Second job: 00:00:00 -> 00:01:00 */");
            StringAssert.Contains(scripts.Script, "on SQL* /01");
        }
    }
}
