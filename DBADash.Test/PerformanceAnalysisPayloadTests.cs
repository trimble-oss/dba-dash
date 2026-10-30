using System;
using System.Data;
using System.Linq;
using DBADash.PerformanceAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// What a performance analysis sends.  The preview the reader approves is this payload, so what it
    /// leaves out matters as much as what it carries.
    /// </summary>
    [TestClass]
    public class PerformanceAnalysisPayloadTests
    {
        private static readonly DateTime To = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

        private static PerformanceAnalysisData Data(TimeSpan? length = null) => new()
        {
            Window = PerformanceAnalysisWindow.Create(To - (length ?? TimeSpan.FromHours(24)), To),
            Instance = "SQL01",
            InstanceId = 1
        };

        private static PerformanceAnalysisPayload Build(PerformanceAnalysisData data, PerformanceAnalysisOptions? options = null) =>
            PerformanceAnalysisPayload.Build(data, options ?? PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General), t => t, "UTC");

        private static string Section(PerformanceAnalysisPayload payload, PerformanceSection section) =>
            payload.Sections.Single(s => s.Section == section).Content;

        private static DataTable Waits(params (string Type, double MsPerSec)[] waits)
        {
            var table = new DataTable();
            table.Columns.Add("WaitType", typeof(string));
            table.Columns.Add("WaitTimeMsPerSec", typeof(double));
            table.Columns.Add("SignalWaitPct", typeof(double));
            table.Columns.Add("TotalWaitSec", typeof(double));
            table.Columns.Add("SignalWaitSec", typeof(double));
            table.Columns.Add("CriticalWait", typeof(int));
            foreach (var (type, msPerSec) in waits)
            {
                table.Rows.Add(type, msPerSec, 0.1, msPerSec, msPerSec / 10, type == "RESOURCE_SEMAPHORE" ? 1 : 0);
            }

            return table;
        }

        private static DataTable Cpu(DateTime from, params (double Sql, double Other, double Max)[] points)
        {
            var table = new DataTable();
            table.Columns.Add("EventTime", typeof(DateTime));
            table.Columns.Add("SQLProcessCPU", typeof(double));
            table.Columns.Add("OtherCPU", typeof(double));
            table.Columns.Add("MaxCPU", typeof(double));
            for (var i = 0; i < points.Length; i++)
            {
                table.Rows.Add(from.AddMinutes(30 * i), points[i].Sql, points[i].Other, points[i].Max);
            }

            return table;
        }

        [TestMethod]
        public void Window_LongerThanAWeek_KeepsItsLastWeek()
        {
            var window = PerformanceAnalysisWindow.Create(To.AddDays(-30), To);

            Assert.IsTrue(window.WasClamped);
            Assert.AreEqual(To, window.ToUtc);
            Assert.AreEqual(PerformanceAnalysisWindow.MaxLength, window.Length);
        }

        [TestMethod]
        public void Window_BucketsKeepASeriesToAboutFortyEightPoints()
        {
            Assert.AreEqual(2, PerformanceAnalysisWindow.ChooseBucketMinutes(TimeSpan.FromHours(1)));
            Assert.AreEqual(30, PerformanceAnalysisWindow.ChooseBucketMinutes(TimeSpan.FromHours(24)));
            Assert.AreEqual(240, PerformanceAnalysisWindow.ChooseBucketMinutes(TimeSpan.FromDays(7)));
        }

        [TestMethod]
        public void Baseline_IsAWeekEarlierInWholeHours()
        {
            // Whole hours, because the baseline comes from the hourly rollups: a part hour holds no hourly row.
            var window = PerformanceAnalysisWindow.Create(new DateTime(2026, 10, 18, 9, 20, 0, DateTimeKind.Utc), new DateTime(2026, 10, 18, 9, 40, 0, DateTimeKind.Utc));

            Assert.AreEqual(new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc), window.Baseline.FromUtc);
            Assert.AreEqual(new DateTime(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc), window.Baseline.ToUtc);
        }

        [TestMethod]
        public void Sections_LeftOut_AreNotSent()
        {
            var options = PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General);
            options[PerformanceSection.Memory].Include = false;

            var payload = Build(Data(), options);

            Assert.IsFalse(payload.Sections.Any(s => s.Section == PerformanceSection.Memory));
            Assert.IsFalse(payload.ToPreview().Contains("## Memory clerks"));
        }

        [TestMethod]
        public void LatchFocus_SendsLatchesAndSpinlocksWithCpuAndWaitsAsContext()
        {
            var sections = Build(Data(), PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.LatchesAndSpinlocks))
                .Sections.Select(s => s.Section).ToList();

            CollectionAssert.AreEqual(
                new[] { PerformanceSection.Context, PerformanceSection.CPU, PerformanceSection.Waits, PerformanceSection.Latches, PerformanceSection.Spinlocks },
                sections);
        }

        [TestMethod]
        public void Waits_AreRankedAndCappedAndComparedWithTheBaseline()
        {
            var data = Data();
            data.Waits = Waits(("WRITELOG", 40), ("PAGEIOLATCH_SH", 100), ("CXPACKET", 5));
            data.WaitsBaseline = Waits(("WRITELOG", 10), ("PAGEIOLATCH_SH", 100));
            var options = PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General);
            options[PerformanceSection.Waits].TopN = 2;

            var waits = Section(Build(data, options), PerformanceSection.Waits);

            var pageIo = waits.IndexOf("PAGEIOLATCH_SH |", StringComparison.Ordinal);
            var writeLog = waits.IndexOf("WRITELOG |", StringComparison.Ordinal);
            Assert.IsTrue(pageIo >= 0 && writeLog > pageIo, "Ranked by ms/sec, worst first");
            Assert.IsFalse(waits.Contains("CXPACKET |"), "Past the Top N");
            StringAssert.Contains(waits, "Top 2 of 3 wait types");
            StringAssert.Contains(waits, "| 10 | 4x");
            StringAssert.Contains(waits, "| 100 | 1x");
        }

        [TestMethod]
        public void Waits_NoBaselineHistory_IsNotAWeekWithoutWaits()
        {
            // A newly monitored instance has no week-earlier rows: that is no history, not a regression in every wait.
            var data = Data();
            data.Waits = Waits(("WRITELOG", 40), ("PAGEIOLATCH_SH", 100));
            data.WaitsBaseline = Waits();

            var waits = Section(Build(data), PerformanceSection.Waits);

            Assert.IsFalse(waits.Contains("new", StringComparison.Ordinal));
            StringAssert.Contains(waits, "Baseline: no wait stats were collected");
        }

        [TestMethod]
        public void Baseline_Off_LeavesOutEveryBaselineColumn()
        {
            var data = Data();
            data.Waits = Waits(("WRITELOG", 40));
            data.WaitsBaseline = Waits(("WRITELOG", 10));
            var options = PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General);
            options.IncludeBaseline = false;

            var payload = Build(data, options);

            Assert.IsNull(payload.Baseline);
            Assert.IsFalse(payload.ToPreview().Contains("baseline_", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Cpu_SummarisesTheWindowAndOnlySendsTheSeriesWhenAsked()
        {
            var data = Data();
            data.Cpu = Cpu(To.AddHours(-2), (10, 1, 20), (30, 1, 90), (20, 1, 40), (40, 1, 50));
            var options = PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General);
            options.IncludeBaseline = false;

            var cpu = Section(Build(data, options), PerformanceSection.CPU);
            StringAssert.Contains(cpu, "window | 25 | 1 | 26 |");
            StringAssert.Contains(cpu, "| 90");
            StringAssert.Contains(cpu, "Over time:");

            options[PerformanceSection.CPU].TimeSeries = false;
            Assert.IsFalse(Section(Build(data, options), PerformanceSection.CPU).Contains("Over time:"));
        }

        [TestMethod]
        public void Cpu_AveragesAreWeightedBySamples()
        {
            // A part bucket at the edge of the window holds fewer samples and counts for less.
            var data = Data();
            data.Cpu = Cpu(To.AddHours(-1), (10, 0, 10), (70, 0, 70));
            data.Cpu.Columns.Add("SampleCount", typeof(int));
            data.Cpu.Rows[0]["SampleCount"] = 30;
            data.Cpu.Rows[1]["SampleCount"] = 10;
            var options = PerformanceAnalysisOptions.Defaults(PerformanceAnalysisFocus.General);
            options.IncludeBaseline = false;

            StringAssert.Contains(Section(Build(data, options), PerformanceSection.CPU), "window | 25 | 0 | 25 |");
        }

        private static DataTable Blocking(params (int Sessions, long WaitMs)[] snapshots)
        {
            var table = new DataTable();
            table.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            table.Columns.Add("BlockedSessionCount", typeof(int));
            table.Columns.Add("BlockedWaitTime", typeof(long));
            for (var i = 0; i < snapshots.Length; i++)
            {
                table.Rows.Add(To.AddMinutes(-snapshots.Length + i), snapshots[i].Sessions, snapshots[i].WaitMs);
            }

            return table;
        }

        [TestMethod]
        public void Blocking_SnapshotsAreNotAdded()
        {
            // One session seen 10, 20 and 30 seconds into the same block is 30 seconds of blocking, not 60.
            var data = Data();
            data.Blocking = Blocking((1, 10_000), (1, 20_000), (1, 30_000));

            var blocking = Section(Build(data), PerformanceSection.Blocking);

            StringAssert.Contains(blocking, "at most 30 sec of blocked wait in one snapshot");
            Assert.IsFalse(blocking.Contains("60", StringComparison.Ordinal));
        }

        [TestMethod]
        public void LatchesNotCollected_SaysSoRatherThanSendingNothing()
        {
            var message = new DataTable();
            message.Columns.Add("Message", typeof(string));
            message.Rows.Add("Enable the LatchStats collection in the service config tool to see data here.");
            var data = Data();
            data.Latches = new DataSet();
            data.Latches.Tables.Add(message);

            StringAssert.Contains(Section(Build(data), PerformanceSection.Latches), "Not collected");
        }

        [TestMethod]
        public void SectionThatFailedToLoad_SaysWhyAndTheRestStillGo()
        {
            var data = Data();
            data.Errors[PerformanceSection.IO] = "Timeout expired.";
            data.Waits = Waits(("WRITELOG", 40));

            var payload = Build(data);

            StringAssert.Contains(Section(payload, PerformanceSection.IO), "Timeout expired.");
            StringAssert.Contains(Section(payload, PerformanceSection.Waits), "WRITELOG");
        }

        [TestMethod]
        public void ClampedWindow_IsSaidInTheNotes()
        {
            var payload = Build(Data(TimeSpan.FromDays(30)));

            Assert.IsTrue(payload.Notes.Any(n => n.Contains("only its last 7 days")));
        }

        private static DataTable Counters(params (string Object, string Counter, string Instance, double Avg)[] counters)
        {
            var table = new DataTable();
            table.Columns.Add("object_name", typeof(string));
            table.Columns.Add("counter_name", typeof(string));
            table.Columns.Add("instance_name", typeof(string));
            table.Columns.Add("AvgValue", typeof(double));
            foreach (var (obj, counter, instance, avg) in counters)
            {
                table.Rows.Add(obj, counter, instance, avg);
            }

            return table;
        }

        [TestMethod]
        public void Metrics_LeaveOutDBADashsOwnCounters()
        {
            // DBA Dash logs its own collection timings as counters; they are about the monitoring, not the instance.
            var data = Data();
            data.Metrics = Counters(
                ("SQLServer:Buffer Manager", "Page life expectancy", "", 3000),
                ("DBADash", "Collection Duration (ms)", "Alerts", 0.73));

            var metrics = Section(Build(data), PerformanceSection.Metrics);

            StringAssert.Contains(metrics, "Page life expectancy");
            Assert.IsFalse(metrics.Contains("Collection Duration"));
            StringAssert.Contains(metrics, "1 counters:");
        }

        [TestMethod]
        public void Metrics_OnlyInternalCounters_IsNoData()
        {
            var data = Data();
            data.Metrics = Counters(("DBADash", "Collection Duration (ms)", "Alerts", 0.73));

            StringAssert.Contains(Section(Build(data), PerformanceSection.Metrics), "No performance counters");
        }

        private static DataTable Objects(params (string Schema, string Name, double MsPerSec)[] objects)
        {
            var table = new DataTable();
            table.Columns.Add("DB", typeof(string));
            table.Columns.Add("SchemaName", typeof(string));
            table.Columns.Add("ObjectName", typeof(string));
            table.Columns.Add("duration_ms_per_sec", typeof(double));
            foreach (var (schema, name, msPerSec) in objects)
            {
                table.Rows.Add("DBADashDB", schema, name, msPerSec);
            }

            return table;
        }

        [TestMethod]
        public void ObjectExecution_LeavesOutTheMessagingReceiveAndSaysSo()
        {
            // It sits in WAITFOR (RECEIVE), so on a quiet instance it tops the list while costing nothing.
            var data = Data();
            data.ObjectExecution = Objects(("Messaging", "ReceiveMessageToService", 900), ("dbo", "Collect_Upd", 12));

            var objects = Section(Build(data), PerformanceSection.ObjectExecution);

            StringAssert.Contains(objects, "DBADashDB.dbo.Collect_Upd");
            Assert.IsFalse(objects.Contains("DBADashDB.Messaging.ReceiveMessageToService"));
            StringAssert.Contains(objects, "Left out: Messaging.ReceiveMessageToService");
            StringAssert.Contains(objects, "Top 1 of 1 objects");
        }

        [TestMethod]
        public void ObjectExecution_OnlyTheMessagingReceive_SaysItWasLeftOutRatherThanNotCollected()
        {
            var data = Data();
            data.ObjectExecution = Objects(("Messaging", "ReceiveMessageToService", 900));

            var objects = Section(Build(data), PerformanceSection.ObjectExecution);

            Assert.IsFalse(objects.Contains("were collected", StringComparison.Ordinal));
            StringAssert.Contains(objects, "Left out: Messaging.ReceiveMessageToService");
        }

        [TestMethod]
        public void ObjectExecution_NothingLeftOut_SaysNothingAboutIt()
        {
            var data = Data();
            data.ObjectExecution = Objects(("dbo", "Collect_Upd", 12));

            Assert.IsFalse(Section(Build(data), PerformanceSection.ObjectExecution).Contains("Left out"));
        }

        [TestMethod]
        public void CopiedPrompt_IsWhatTheServiceWouldAsk()
        {
            // A reader without access to the service pastes this into a tool of their own, so it has to be the same question.
            var data = Data();
            data.Waits = Waits(("WRITELOG", 40));
            foreach (var focus in new[] { PerformanceAnalysisFocus.General, PerformanceAnalysisFocus.LatchesAndSpinlocks })
            {
                var payload = Build(data, PerformanceAnalysisOptions.Defaults(focus));
                var request = new DBADashAI.Models.AiPerformanceAnalysisRequest
                {
                    Instance = payload.Instance,
                    Focus = payload.Focus.ToString(),
                    Window = payload.Window,
                    Baseline = payload.Baseline,
                    Notes = payload.Notes.ToList(),
                    Sections = payload.Sections.Select(s => new DBADashAI.Models.AiPerformanceAnalysisSection { Title = s.Title, Content = s.Content }).ToList()
                };

                Assert.AreEqual(new DBADashAI.Services.AiPerformancePromptBuilder().Build(request), payload.ToPrompt(), focus.ToString());
            }
        }

        private static DataTable RunningQueries(bool tempdbDetail, params (int Running, int Blocked, long BlockedWaitMs, int AllocWaiting, int MetaWaiting)[] snapshots)
        {
            var table = new DataTable();
            table.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            table.Columns.Add("RunningQueries", typeof(short));
            table.Columns.Add("BlockedQueries", typeof(short));
            table.Columns.Add("BlockedQueriesWaitMs", typeof(long));
            table.Columns.Add("LongestRunningQueryMs", typeof(long));
            table.Columns.Add("TempDBWaitCount", typeof(int));
            table.Columns.Add("TempDBAllocationWaitCount", typeof(int));
            table.Columns.Add("TempDBAllocationWaitTimeMs", typeof(long));
            table.Columns.Add("TempDBMetadataWaitCount", typeof(int));
            table.Columns.Add("TempDBMetadataWaitTimeMs", typeof(long));
            for (var i = 0; i < snapshots.Length; i++)
            {
                var s = snapshots[i];
                table.Rows.Add(To.AddMinutes(-snapshots.Length + i), (short)s.Running, (short)s.Blocked, s.BlockedWaitMs, 60_000L, s.AllocWaiting + s.MetaWaiting,
                    tempdbDetail ? s.AllocWaiting : DBNull.Value, tempdbDetail ? s.AllocWaiting * 50L : DBNull.Value,
                    tempdbDetail ? s.MetaWaiting : DBNull.Value, tempdbDetail ? s.MetaWaiting * 20L : DBNull.Value);
            }

            return table;
        }

        [TestMethod]
        public void RunningQueries_SummarisesBlockingAndTellsTempdbAllocationFromMetadata()
        {
            var data = Data();
            data.RunningQueries = RunningQueries(true, (10, 0, 0, 0, 0), (20, 4, 30_000, 6, 0), (30, 2, 5_000, 0, 3), (40, 0, 0, 0, 0));

            var running = Section(Build(data), PerformanceSection.RunningQueries);

            // 4 snapshots: avg 25 running, max 40, blocked in half, at most 4 blocked, 30 sec of blocked wait, longest query 60 sec.
            StringAssert.Contains(running, "window | 4 | 25 | 40 | 50 | 4 | 30 | 60 |");
            // Allocation in 1 of 4 snapshots, 6 waiting, 300ms; metadata in 1 of 4, 3 waiting, 60ms.
            StringAssert.Contains(running, "window | 25 | 6 | 300 | 25 | 3 | 60 |");
            StringAssert.Contains(running, "baseline | no data");
            StringAssert.Contains(running, "Over time");
        }

        [TestMethod]
        public void RunningQueries_OlderAgent_SaysTempdbDetailIsNotCaptured()
        {
            var data = Data();
            data.RunningQueries = RunningQueries(false, (10, 0, 0, 2, 0));

            StringAssert.Contains(Section(Build(data), PerformanceSection.RunningQueries), "not captured");
        }

        private static DataSet DeadlockCharts(params (DateTime Time, int Deadlocks)[] points)
        {
            var set = new DataSet();
            var overTime = set.Tables.Add();
            overTime.Columns.Add("EventTime", typeof(DateTime));
            overTime.Columns.Add("Deadlocks", typeof(int));
            overTime.Columns.Add("Victims", typeof(int));
            foreach (var (time, deadlocks) in points)
            {
                overTime.Rows.Add(time, deadlocks, deadlocks);
            }

            for (var i = 1; i <= 6; i++)
            {
                var group = set.Tables.Add();
                group.Columns.Add("GroupValue", typeof(string));
                group.Columns.Add("Deadlocks", typeof(int));
                if (points.Length > 0) group.Rows.Add(i == 2 ? "OrdersApp" : i == 6 ? "Sales.dbo.usp_UpdateOrder" : "Sales", points.Sum(p => p.Deadlocks));
            }

            return set;
        }

        [TestMethod]
        public void Deadlocks_GroupBySignatureAndCompareWithTheBaseline()
        {
            var data = Data();
            data.Deadlocks = DeadlockCharts((To.AddHours(-2), 30), (To.AddHours(-1), 10));
            data.DeadlocksBaseline = DeadlockCharts((To.AddDays(-7), 4));
            data.DeadlockSignatures = new DataTable();
            data.DeadlockSignatures.Columns.Add("Signature", typeof(string));
            data.DeadlockSignatures.Columns.Add("Occurrences", typeof(int));
            data.DeadlockSignatures.Columns.Add("ObjectNames", typeof(string));
            data.DeadlockSignatures.Rows.Add("0x0102030405060708", 40, "Sales.dbo.Orders");

            var deadlocks = Section(Build(data), PerformanceSection.Deadlocks);

            StringAssert.Contains(deadlocks, "Window: 40 deadlocks, 40 victims.  Baseline: 4 deadlocks (10x).");
            StringAssert.Contains(deadlocks, "0x0102030405060708 | 40 |");
            StringAssert.Contains(deadlocks, "Sales.dbo.Orders");
            StringAssert.Contains(deadlocks, "Deadlocks by application: OrdersApp 40");
            StringAssert.Contains(deadlocks, "Deadlocks by procedure: Sales.dbo.usp_UpdateOrder 40");
        }

        [TestMethod]
        public void Deadlocks_NotCollected_SaysSo()
        {
            var message = new DataTable();
            message.Columns.Add("Message", typeof(string));
            message.Rows.Add("Enable the Deadlocks collection in the service config tool to see data here.");
            var data = Data();
            data.Deadlocks = new DataSet();
            data.Deadlocks.Tables.Add(message);

            StringAssert.Contains(Section(Build(data), PerformanceSection.Deadlocks), "Not collected");
        }

        [TestMethod]
        public void Numbers_KeepTheDigitsWorthReading()
        {
            Assert.AreEqual("1235", PerformanceAnalysisTable.Num(1234.5678));
            Assert.AreEqual("12.3", PerformanceAnalysisTable.Num(12.34));
            Assert.AreEqual("0.012", PerformanceAnalysisTable.Num(0.0123));
            Assert.AreEqual("0", PerformanceAnalysisTable.Num(0));
            Assert.AreEqual(string.Empty, PerformanceAnalysisTable.Num(null));
        }

        [TestMethod]
        public void OffsetChanges_FindsAClockChangeToTheMinute()
        {
            // BST until 01:00 UTC on 2026-10-25, GMT after.
            var change = new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc);
            DateTime ToLocal(DateTime utc) => utc < change ? utc.AddHours(1) : utc;

            var changes = PerformanceAnalysisPayload.OffsetChanges(ToLocal, change.AddMinutes(-150), change.AddHours(2));
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(change, changes[0].FromUtc);
            Assert.AreEqual(60, changes[0].PreviousOffsetMinutes);
            Assert.AreEqual(0, changes[0].OffsetMinutes);

            Assert.AreEqual(0, PerformanceAnalysisPayload.OffsetChanges(ToLocal, change, change.AddDays(7)).Count);
        }
    }
}
