using DBADash.DailyReport;
using DBADash.PerformanceInsights;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>The Performance tab's insights: dbo.DailyReport_Performance_Get over a window other than 24 hours, and dbo.PerformanceInsights_Get</summary>
    public partial class DailyReportSqlTests
    {
        /// <summary>Monday 09:00-12:00 UTC</summary>
        private static readonly DateTime InsightFrom = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime InsightTo = InsightFrom.AddHours(3);

        private static int AddInsightInstance(string name) => Scalar($@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'{name}',N'{name}',1,1,3,0,'{InsightFrom.AddHours(2):yyyy-MM-dd HH:mm:ss}');
SELECT CAST(SCOPE_IDENTITY() AS INT)");

        private static string Sql(DateTime d) => $"'{d:yyyy-MM-dd HH:mm:ss}'";

        [TestMethod]
        public void PerformanceGet_WindowHours_ReadsOnlyTheWindowAndTheSameHoursAWeekEarlier()
        {
            var id = AddInsightInstance("PerfWindow");
            // An hour before the window, the window's 3 hours, and the same 3 hours a week earlier
            foreach (var hour in new[] { InsightFrom.AddHours(-1), InsightFrom, InsightFrom.AddHours(1), InsightFrom.AddHours(2), InsightFrom.AddDays(-7), InsightFrom.AddDays(-7).AddHours(2) })
            {
                Exec($@"INSERT INTO dbo.CPU_60MIN(InstanceID,EventTime,SumSQLProcessCPU,SumSystemIdleCPU,SampleCount,MaxSQLProcessCPU,MaxOtherProcessCPU,MaxTotalCPU)
VALUES({id},{Sql(hour)},1200,4800,60,30,5,35)");
            }
            // Instance total IO for the first hour: 3,600 MB read and 1,800 MB written = 1.5 MB/s
            Exec($@"INSERT INTO dbo.DBIOStats_60MIN(InstanceID,DatabaseID,Drive,FileID,SnapshotDate,num_of_reads,num_of_writes,num_of_bytes_read,num_of_bytes_written,
	io_stall_read_ms,io_stall_write_ms,sample_ms_diff,MaxReadLatency,MaxWriteLatency,MaxLatency,MaxReadIOPs,MaxWriteIOPs,MaxIOPs,MaxReadMBsec,MaxWriteMBsec,MaxMBsec)
VALUES({id},-1,'*',-1,{Sql(InsightFrom)},1000,500,{3600L * 1024 * 1024},{1800L * 1024 * 1024},2000,1000,3600000,0,0,0,0,0,0,0,0,0)");

            var data = PerformanceDataRepository.Get(ConnectionString, id.ToString(), InsightTo, 3, 1, null);
            Assert.AreEqual(InsightFrom, data.WindowStartUtc);
            var hours = data.Instances.Single().Hours;
            CollectionAssert.AreEquivalent(new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 2) }, hours.Select(h => (h.Period, h.HourOffset)).ToList());
            var first = hours.Single(h => h.Period == 0 && h.HourOffset == 0);
            Assert.AreEqual(20, first.AvgCPU!.Value, 0.001, "60 samples at 20% (100 - idle 80)");
            Assert.AreEqual(1.5, PerformanceAnalyzer.HourlyValue("MBsec", first, 0)!.Value, 0.0001);
            Assert.IsNull(hours.Single(h => h.Period == 0 && h.HourOffset == 1).ReadBytes, "No IO row for the hour - no bytes");
        }

        [TestMethod]
        public void PerformanceGet_LongerThanAWeekWithBaseline_IsRefused()
        {
            var ex = Assert.ThrowsExactly<SqlException>(() => PerformanceDataRepository.Get(ConnectionString, "1", InsightTo, 169, 1, null));
            StringAssert.Contains(ex.Message, "at most 168");
            // Without a baseline it's fine
            PerformanceDataRepository.Get(ConnectionString, "1", InsightTo, 300, 0, null);
        }

        [TestMethod]
        public void PerformanceGet_PeriodShift_MovesThatWeekByWholeHours()
        {
            var id = AddInsightInstance("PerfShift");
            // An hour before the window's start a week earlier - only in period 1 when that week is shifted back an hour
            Exec($@"INSERT INTO dbo.CPU_60MIN(InstanceID,EventTime,SumSQLProcessCPU,SumSystemIdleCPU,SampleCount,MaxSQLProcessCPU,MaxOtherProcessCPU,MaxTotalCPU)
VALUES({id},{Sql(InsightFrom.AddDays(-7).AddHours(-1))},1200,4800,60,30,5,35)");

            var unshifted = PerformanceDataRepository.Get(ConnectionString, id.ToString(), InsightTo, 3, 1, null);
            Assert.AreEqual(0, unshifted.Instances.Single().Hours.Count);

            var shifted = PerformanceDataRepository.Get(ConnectionString, id.ToString(), InsightTo, 3, 1, null, periodShiftHours: new Dictionary<int, int> { { 1, -1 } });
            var hour = shifted.Instances.Single().Hours.Single();
            Assert.AreEqual((1, 0), (hour.Period, hour.HourOffset), "Counted from the shifted start, so it's the week's first hour");
            Assert.AreEqual(-1, shifted.PeriodShiftHours[1]);
        }

        [TestMethod]
        public void PerformanceGet_WithoutDetail_LeavesOutDeadlocksAndWaitTypes()
        {
            var id = AddInsightInstance("PerfDetail");
            var waitTypeId = Scalar("IF NOT EXISTS(SELECT 1 FROM dbo.WaitType WHERE WaitType = N'TEST_INSIGHTS_WAIT') INSERT INTO dbo.WaitType(WaitType) VALUES(N'TEST_INSIGHTS_WAIT'); SELECT WaitTypeID FROM dbo.WaitType WHERE WaitType = N'TEST_INSIGHTS_WAIT'");
            Exec($@"INSERT INTO dbo.Deadlocks(InstanceID,EventTime,DeadlockHash) VALUES({id},{Sql(InsightFrom.AddMinutes(5))},0x01)");
            Exec($@"INSERT INTO dbo.Waits_60MIN(InstanceID,SnapshotDate,WaitTypeID,waiting_tasks_count,wait_time_ms,signal_wait_time_ms,sample_ms_diff)
VALUES({id},{Sql(InsightFrom)},{waitTypeId},10,5000,0,3600000)");

            var detailed = PerformanceDataRepository.Get(ConnectionString, id.ToString(), InsightTo, 3, 0, null);
            Assert.AreEqual(1, detailed.Instances.Single().Hours.Sum(h => h.Deadlocks ?? 0));
            Assert.AreEqual(1, detailed.Instances.Single().WaitTypes.Count);

            var lines = PerformanceDataRepository.Get(ConnectionString, id.ToString(), InsightTo, 3, 0, null, detail: false);
            var instance = lines.Instances.Single();
            Assert.AreEqual(0, instance.Hours.Sum(h => h.Deadlocks ?? 0));
            Assert.AreEqual(0, instance.WaitTypes.Count);
            Assert.AreEqual(5000, instance.Hours.Single().WaitMs, "Total waits by hour are still read - the Waits chart's baseline line needs them");
        }

        [TestMethod]
        public void PerformanceInsightsGet_ReturnsWhatHappenedInTheRange()
        {
            var id = AddInsightInstance("PerfInsights");
            var job = Guid.NewGuid();
            var shortJob = Guid.NewGuid();
            Exec($@"
INSERT INTO Alert.ActiveAlerts(InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,UpdatedDate)
VALUES({id},1,'CPU','CPU',N'CPU 95%',N'CPU 95%',{Sql(InsightFrom.AddDays(-1))},{Sql(InsightFrom)}); /* Open since before the range */

INSERT INTO Alert.ActiveAlerts(InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,UpdatedDate,IsResolved,ResolvedDate)
VALUES({id},20,'Blocking','Blocking',N'Old',N'Old',{Sql(InsightFrom.AddDays(-2))},{Sql(InsightFrom.AddDays(-2))},1,{Sql(InsightFrom.AddDays(-1))}); /* Resolved before it - left out */

INSERT INTO Alert.ClosedAlerts(AlertID,InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,UpdatedDate,IsResolved,ResolvedDate,ClosedDate)
VALUES(900000 + {id},{id},11,'Deadlock','Deadlock',N'Deadlocks',N'Deadlocks',{Sql(InsightFrom.AddMinutes(10))},{Sql(InsightFrom.AddMinutes(10))},1,{Sql(InsightFrom.AddMinutes(40))},{Sql(InsightFrom.AddHours(1))});

INSERT INTO dbo.OfflineInstances(InstanceID,FirstFail,LastFail,IsCurrent,FailCount)
VALUES({id},{Sql(InsightFrom.AddMinutes(30))},{Sql(InsightFrom.AddMinutes(35))},0,3);

INSERT INTO dbo.AvailabilityGroupRoleChanges(InstanceID,AvailabilityGroupName,PreviousRole,NewRole,ChangedAfter,ChangedBefore)
VALUES({id},N'AG1',2,1,{Sql(InsightFrom.AddMinutes(50))},{Sql(InsightFrom.AddMinutes(51))});

INSERT INTO dbo.SysConfigHistory(InstanceID,configuration_id,value,new_value,value_in_use,new_value_in_use,ValidFrom,ValidTo)
VALUES({id},1539,8,4,8,4,{Sql(InsightFrom.AddDays(-30))},{Sql(InsightFrom.AddHours(1))});

INSERT INTO dbo.TraceFlags(InstanceID,TraceFlag,ValidFrom) VALUES({id},1117,{Sql(InsightFrom.AddHours(2))});

INSERT INTO dbo.Jobs(InstanceID,job_id,name,IsActive) VALUES({id},'{job}',N'IndexOptimize',1),({id},'{shortJob}',N'Quick',1);
INSERT INTO dbo.JobHistory(InstanceID,instance_id,job_id,step_id,run_status,RunDateTime,RunDurationSec)
VALUES({id},1,'{job}',0,1,{Sql(InsightFrom.AddHours(-1))},5400), /* Started before the range, ran into it */
	({id},2,'{job}',0,0,{Sql(InsightFrom.AddHours(2))},60), /* Failed */
	({id},3,'{shortJob}',0,1,{Sql(InsightFrom.AddHours(1))},30), /* Short and fine - left out */
	({id},4,'{job}',0,1,{Sql(InsightFrom.AddHours(-5))},600); /* Finished before the range - left out */

INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage) VALUES({Sql(InsightFrom.AddMinutes(5))},{id},'CPU',N'x'),({Sql(InsightFrom.AddMinutes(6))},{id},'CPU',N'y');
");

            var context = PerformanceInsightsRepository.GetContext(ConnectionString, id, InsightFrom, InsightTo);

            Assert.AreEqual(InsightFrom.AddHours(2), context.StartTimeUtc);
            Assert.AreEqual(2, context.Alerts.Count);
            Assert.IsTrue(context.Alerts.Single(a => a.AlertType == "CPU").IsOpen);
            Assert.IsFalse(context.Alerts.Single(a => a.AlertType == "Deadlock").IsOpen);
            Assert.AreEqual(1, context.Offline.Count);
            Assert.AreEqual("PRIMARY", context.RoleChanges.Single().NewRole);

            var maxdop = context.ConfigChanges.Single(c => c.ChangeType == "Configuration");
            Assert.AreEqual("max degree of parallelism", maxdop.Name);
            Assert.AreEqual("8", maxdop.OldValue);
            Assert.AreEqual("4", maxdop.NewValue);
            Assert.AreEqual("Trace flag 1117", context.ConfigChanges.Single(c => c.ChangeType == "Trace flag").Name);

            CollectionAssert.AreEquivalent(new[] { (5400, 1), (60, 0) }, context.Jobs.Select(j => (j.DurationSec, j.RunStatus)).ToList());
            Assert.IsTrue(context.Jobs[0].Failed, "Failed runs first");
            Assert.AreEqual(2, context.CollectionErrors.Single().Errors);

            // And the cards they make
            var cards = PerformanceInsightBuilder.Build(new PerformanceInsightsInput
            {
                InstanceID = id,
                Window = InsightWindow.For(InsightFrom, InsightTo),
                Context = context,
                NowUtc = InsightTo.AddHours(1)
            });
            foreach (var category in new[] { InsightCategory.Alerts, InsightCategory.Offline, InsightCategory.Restart, InsightCategory.Failover, InsightCategory.Jobs, InsightCategory.Changes, InsightCategory.DataCollection })
            {
                Assert.IsTrue(cards.Any(c => c.Category == category), $"No {category} card");
            }
        }

        [TestMethod]
        public void PerformanceInsightsGet_SummarisesTempdbContentionFromTheSnapshots()
        {
            var id = AddInsightInstance("PerfTempdb");
            string Row(DateTime at, int allocation, long allocationMs, int metadata, long metadataMs) =>
                $"({id},{Sql(at)},5,0,0,0,0,0,0,0,0,{allocation},{allocationMs},{metadata},{metadataMs})";
            Exec($@"INSERT INTO dbo.RunningQueriesSummary(InstanceID,SnapshotDateUTC,RunningQueries,BlockedQueries,BlockedQueriesWaitMs,MaxMemoryGrant,
	LongestRunningQueryMs,CriticalWaitCount,CriticalWaitTime,TempDBWaitCount,TempDBWaitTimeMs,
	TempDBAllocationWaitCount,TempDBAllocationWaitTimeMs,TempDBMetadataWaitCount,TempDBMetadataWaitTimeMs)
VALUES{Row(InsightFrom.AddMinutes(10), 4, 2000, 0, 0)},
	{Row(InsightFrom.AddMinutes(20), 6, 900, 2, 300)},
	{Row(InsightFrom.AddMinutes(30), 0, 0, 0, 0)},
	{Row(InsightFrom.AddMinutes(-30), 50, 99999, 50, 99999)}"); /* Before the range - left out */

            var t = PerformanceInsightsRepository.GetContext(ConnectionString, id, InsightFrom, InsightTo).Tempdb;

            Assert.IsNotNull(t);
            Assert.AreEqual(3, t.Snapshots);
            Assert.AreEqual(3, t.SnapshotsWithTypes);
            Assert.AreEqual(2, t.AllocationSnapshots);
            Assert.AreEqual(6, t.MaxAllocationWaiting);
            Assert.AreEqual(2000, t.MaxAllocationWaitMs);
            Assert.AreEqual(InsightFrom.AddMinutes(20), t.WorstAllocationSnapshotUtc, "The snapshot with the most waiting");
            Assert.AreEqual(1, t.MetadataSnapshots);
            Assert.AreEqual(2, t.MaxMetadataWaiting);
            Assert.AreEqual(InsightFrom.AddMinutes(20), t.WorstMetadataSnapshotUtc);
        }

        [TestMethod]
        public void PerformanceInsightsGetCounters_RunsForAnInstanceWithNoCounters()
        {
            var id = AddInsightInstance("PerfCounters");
            Assert.AreEqual(0, PerformanceInsightsRepository.GetCounters(ConnectionString, id, InsightFrom, InsightTo).Count);
            // With the time filter - the dbo.IDs table-valued parameters and offset
            Assert.AreEqual(0, PerformanceInsightsRepository.GetCounters(ConnectionString, id, InsightFrom, InsightTo, hours: new[] { 9, 10 }, daysOfWeek: new[] { 1, 2, 3, 4, 5 }, utcOffset: 60).Count);
        }

        [TestMethod]
        public void PerformanceInsightsGet_TraceFlagOnAndOffInTheRange_ShowsBoth()
        {
            // Turned on at 10:00 and off at 11:00 - TraceFlags_Upd has moved the row to TraceFlagHistory
            var id = AddInsightInstance("PerfTraceFlag");
            Exec($"INSERT INTO dbo.TraceFlagHistory(InstanceID,TraceFlag,ValidFrom,ValidTo) VALUES({id},4199,{Sql(InsightFrom.AddHours(1))},{Sql(InsightFrom.AddHours(2))})");
            var changes = PerformanceInsightsRepository.GetContext(ConnectionString, id, InsightFrom, InsightTo).ConfigChanges.Where(c => c.ChangeType == "Trace flag").ToList();
            CollectionAssert.AreEquivalent(new[] { ("On", InsightFrom.AddHours(1)), ("Off", InsightFrom.AddHours(2)) },
                changes.Select(c => (c.NewValue, c.ChangeDateUtc)).ToList());
        }
    }
}
