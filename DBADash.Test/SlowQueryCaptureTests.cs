using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DBADash.SlowQueries;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DBADash.Test
{
    /// <summary>
    /// The slow query capture modes - see <see cref="DBADashSource.SlowQueryCaptureModes"/>.  Everything here is
    /// pure, so none of it needs an instance to run against.
    /// </summary>
    [TestClass]
    public class SlowQueryCaptureTests
    {
        private const string SqlConnection = "Data Source=SQL1;Integrated Security=SSPI";

        private static DBADashSource Source(DBADashSource.SlowQueryCaptureModes mode, bool keepRunning = false) =>
            new()
            {
                SourceConnection = new DBADashConnection(SqlConnection),
                SlowQueryThresholdMs = 1000,
                SlowQueryCaptureMode = mode,
                KeepSlowQueryXESessionRunning = keepRunning
            };

        [TestMethod]
        public void ConfigWithoutTheSetting_KeepsTheRingBuffer()
        {
            // A config written before the modes existed has to carry on exactly as it did.
            var source = JsonConvert.DeserializeObject<DBADashSource>(
                "{\"ConnectionString\":\"" + SqlConnection + "\",\"SlowQueryThresholdMs\":1000}");

            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.RingBuffer, source!.SlowQueryCaptureMode);
            Assert.AreEqual(DBADashSource.DefaultSlowQueryEventFileMaxSizeMB, source.SlowQueryEventFileMaxSizeMB);
            Assert.AreEqual(DBADashSource.DefaultSlowQueryEventFileMaxRolloverFiles, source.SlowQueryEventFileMaxRolloverFiles);
            Assert.IsFalse(source.KeepSlowQueryXESessionRunning);
            Assert.IsTrue(source.UseDualEventSession, "The ring buffer's own default is unchanged");
        }

        [TestMethod]
        public void Mode_IsWrittenAsItsName()
        {
            var json = JsonConvert.SerializeObject(Source(DBADashSource.SlowQueryCaptureModes.EventFile));
            StringAssert.Contains(json, "\"SlowQueryCaptureMode\":\"EventFile\"");

            var roundTripped = JsonConvert.DeserializeObject<DBADashSource>(json);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.EventFile, roundTripped!.SlowQueryCaptureMode);
        }

        [TestMethod]
        [DataRow(false, false, DBADashSource.SlowQueryCaptureModes.EventFile)]
        [DataRow(true, false, DBADashSource.SlowQueryCaptureModes.RingBuffer)]
        [DataRow(false, true, DBADashSource.SlowQueryCaptureModes.RingBuffer)]
        public void EventFile_FallsBackToTheRingBufferOnAzure(bool isAzureDB, bool isManagedInstance,
            DBADashSource.SlowQueryCaptureModes expected)
        {
            var source = Source(DBADashSource.SlowQueryCaptureModes.EventFile);
            Assert.AreEqual(expected, source.GetEffectiveSlowQueryCaptureMode(isAzureDB, isManagedInstance));
        }

        [TestMethod]
        public void ExistingSession_IsReadOnAzureToo()
        {
            var source = Source(DBADashSource.SlowQueryCaptureModes.ExistingSession);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.ExistingSession,
                source.GetEffectiveSlowQueryCaptureMode(true, false));
        }

        [TestMethod]
        public void KeepRunning_OnlyAppliesInEventFileMode()
        {
            // In any other mode the managed session is a leftover from a mode switch and is cleaned up on stop.
            Assert.IsTrue(Source(DBADashSource.SlowQueryCaptureModes.EventFile, true).IsSlowQueryXESessionKeptRunning);
            Assert.IsFalse(Source(DBADashSource.SlowQueryCaptureModes.EventFile).IsSlowQueryXESessionKeptRunning);
            Assert.IsFalse(Source(DBADashSource.SlowQueryCaptureModes.RingBuffer, true).IsSlowQueryXESessionKeptRunning);
            Assert.IsFalse(Source(DBADashSource.SlowQueryCaptureModes.ExistingSession, true).IsSlowQueryXESessionKeptRunning);

            // Switching capture off leaves nothing to keep running for.
            var captureOff = Source(DBADashSource.SlowQueryCaptureModes.EventFile, true);
            captureOff.SlowQueryThresholdMs = -1;
            Assert.IsFalse(captureOff.IsSlowQueryXESessionKeptRunning);
        }

        [TestMethod]
        [DataRow("DBADash_1", true)]
        [DataRow("dbadash_2", true)]
        [DataRow(" DBADash_SlowQueries ", true)]
        [DataRow("MySlowQueries", false)]
        [DataRow("DBADash_SlowQueries2", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void ReservedSessionNames_AreRecognisedWhateverTheirCase(string? sessionName, bool expected)
        {
            // These sessions are dropped by DBA Dash itself, so reading one as an existing session can't work.
            Assert.AreEqual(expected, DBADashSource.IsReservedSlowQueryXESessionName(sessionName!));
        }

        [TestMethod]
        public void NonSqlConnection_AlwaysRingBuffer()
        {
            var source = new DBADashSource
            {
                SourceConnection = new DBADashConnection(@"C:\Temp\DBADash"),
                SlowQueryCaptureMode = DBADashSource.SlowQueryCaptureModes.EventFile,
                SlowQueryXESessionName = "X"
            };
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.RingBuffer, source.SlowQueryCaptureMode);
            Assert.AreEqual(string.Empty, source.SlowQueryXESessionName);
        }

        private static XElement Event(string name, long duration, string appName = "App", string? objectName = null) =>
            XElement.Parse(
                $"<event name=\"{name}\" package=\"sqlserver\" timestamp=\"2026-10-01T10:00:00.123Z\">" +
                $"<data name=\"duration\"><value>{duration}</value></data>" +
                (objectName == null ? "" : $"<data name=\"object_name\"><value>{objectName}</value></data>") +
                "<data name=\"cpu_time\"><value>5</value></data>" +
                "<data name=\"batch_text\"><value>SELECT 1</value></data>" +
                $"<action name=\"client_app_name\" package=\"sqlserver\"><value>{appName}</value></action>" +
                "<action name=\"database_id\" package=\"sqlserver\"><value>7</value></action>" +
                "</event>");

        [TestMethod]
        public void ExistingSessionFilter_AppliesTheThreshold()
        {
            Assert.IsTrue(SlowQueryCollector.IsSlowQueryEvent(Event("sql_batch_completed", 1_000_001), 1_000_000));
            Assert.IsFalse(SlowQueryCollector.IsSlowQueryEvent(Event("sql_batch_completed", 1_000_000), 1_000_000),
                "Over the threshold, as the managed sessions' [duration]>(n) predicate says");
        }

        [TestMethod]
        public void ExistingSessionFilter_MatchesTheManagedSessionsExclusions()
        {
            Assert.IsFalse(SlowQueryCollector.IsSlowQueryEvent(Event("sql_batch_completed", 5_000_000, "DBADashXE"), 0));
            Assert.IsFalse(SlowQueryCollector.IsSlowQueryEvent(Event("rpc_completed", 5_000_000, objectName: "sp_readrequest"), 0));
            Assert.IsTrue(SlowQueryCollector.IsSlowQueryEvent(Event("rpc_completed", 5_000_000, objectName: "usp_Work"), 0));
        }

        [TestMethod]
        public void ExistingSessionFilter_OnlyTakesCompletedQueryEvents()
        {
            Assert.IsFalse(SlowQueryCollector.IsSlowQueryEvent(Event("wait_completed", 5_000_000), 0));
            Assert.IsFalse(SlowQueryCollector.IsSlowQueryEvent(Event("xml_deadlock_report", 5_000_000), 0));
            foreach (var name in SlowQueryCollector.ExistingSessionEventNames)
            {
                Assert.IsTrue(SlowQueryCollector.IsSlowQueryEvent(Event(name, 5_000_000), 0), name);
            }
        }

        [TestMethod]
        public void EventFileEvents_ShredLikeTheRingBuffer()
        {
            // The event file modes shred event by event; the result has to be the table the ring buffer gives,
            // so the import can't tell the modes apart.
            var events = new[] { Event("sql_batch_completed", 2_000_000), Event("rpc_completed", 3_000_000, objectName: "usp_Work") };
            var ringBuffer = new XElement("RingBufferTarget", events.Select(e => new XElement(e)));

            var fromRingBuffer = XETools.XEStrToDT(ringBuffer, out _);
            var fromEvents = XETools.XEEventsToDT(events);

            CollectionAssert.AreEqual(
                fromRingBuffer.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName + ":" + c.DataType).ToList(),
                fromEvents.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName + ":" + c.DataType).ToList());
            Assert.AreEqual(2, fromEvents.Rows.Count);
            for (var r = 0; r < fromEvents.Rows.Count; r++)
            {
                CollectionAssert.AreEqual(fromRingBuffer.Rows[r].ItemArray, fromEvents.Rows[r].ItemArray);
            }
            Assert.AreEqual(2_000_000L, fromEvents.Rows[0]["duration"]);
            Assert.AreEqual("usp_Work", fromEvents.Rows[1]["object_name"]);
        }

        [TestMethod]
        public void CursorStore_RoundTripsPositions()
        {
            var json = SlowQueryCursorStore.Serialize(new[]
            {
                new KeyValuePair<string, FileTargetCursor>("SQL1|DBADash_SlowQueries",
                    new FileTargetCursor(@"C:\Log\DBADash_SlowQueries_0_1.xel", 4096, 2)),
                new KeyValuePair<string, FileTargetCursor>("SQL2|DBADash_SlowQueries", FileTargetCursor.None)
            });

            var restored = SlowQueryCursorStore.Deserialize(json);

            Assert.AreEqual(1, restored.Count, "An instance with no position has nothing worth keeping");
            var cursor = restored["sql1|dbadash_slowqueries"];
            Assert.AreEqual(@"C:\Log\DBADash_SlowQueries_0_1.xel", cursor.FileName);
            Assert.AreEqual(4096, cursor.Offset);
            Assert.AreEqual(2, cursor.ConsumedAtOffset);
            Assert.AreEqual(0, SlowQueryCursorStore.Deserialize("null").Count);
        }

        [TestMethod]
        public void CursorStore_RunThatNeverCommitsLeavesThePositionWhereItWas()
        {
            var key = "SlowQueryCaptureTests|" + System.Guid.NewGuid();
            var first = SlowQueryCursorStore.GetPending(key);
            first.Cursor = new FileTargetCursor("a.xel", 10, 1);
            SlowQueryCursorStore.Commit(key, first);

            var uncommitted = SlowQueryCursorStore.GetPending(key);
            uncommitted.Cursor = new FileTargetCursor("a.xel", 20, 1);

            Assert.AreEqual(10, SlowQueryCursorStore.GetPending(key).Cursor.Offset);
        }
    }
}
