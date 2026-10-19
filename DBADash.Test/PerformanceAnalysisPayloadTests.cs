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

        [TestMethod]
        public void Numbers_KeepTheDigitsWorthReading()
        {
            Assert.AreEqual("1235", PerformanceAnalysisTable.Num(1234.5678));
            Assert.AreEqual("12.3", PerformanceAnalysisTable.Num(12.34));
            Assert.AreEqual("0.012", PerformanceAnalysisTable.Num(0.0123));
            Assert.AreEqual("0", PerformanceAnalysisTable.Num(0));
            Assert.AreEqual(string.Empty, PerformanceAnalysisTable.Num(null));
        }
    }
}
