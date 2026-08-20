using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class XETraceReaderTests
    {
        private const string File1 = @"C:\Log\DBADash_AdHoc_0_1.xel";
        private const string File2 = @"C:\Log\DBADash_AdHoc_0_2.xel";

        private static RawXEvent Ev(string file, long offset, string name = "sql_batch_completed") =>
            new(file, offset, $"<event name=\"{name}\" package=\"sqlserver\" timestamp=\"2024-01-01T00:00:00.000Z\" />");

        // ---- FileTargetCursorReader.Apply -------------------------------------------------------

        [TestMethod]
        public void Apply_InitialRead_ReturnsAllRows_AndSetsCursor()
        {
            var rows = new List<RawXEvent> { Ev(File1, 100), Ev(File1, 100), Ev(File1, 200) };

            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, FileTargetCursor.None);

            Assert.AreEqual(3, newEvents.Count);
            Assert.AreEqual(File1, cursor.FileName);
            Assert.AreEqual(200, cursor.Offset);
            Assert.AreEqual(1, cursor.ConsumedAtOffset); // one event at the final offset 200
        }

        [TestMethod]
        public void Apply_Resume_SkipsBoundaryEventsAlreadyConsumed()
        {
            // Last read ended at offset 200 having consumed 2 events there.  The re-read returns those 2 again.
            var prior = new FileTargetCursor(File1, 200, 2);
            var rows = new List<RawXEvent> { Ev(File1, 200), Ev(File1, 200) };

            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, prior);

            Assert.AreEqual(0, newEvents.Count, "both boundary events were already emitted");
            Assert.AreEqual(200, cursor.Offset);
            Assert.AreEqual(2, cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void Apply_Resume_NewEventInSameBuffer_EmittedAndCursorGrows()
        {
            var prior = new FileTargetCursor(File1, 200, 2);
            // Same buffer/offset now has a third event.
            var rows = new List<RawXEvent> { Ev(File1, 200), Ev(File1, 200), Ev(File1, 200) };

            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, prior);

            Assert.AreEqual(1, newEvents.Count);
            Assert.AreEqual(200, cursor.Offset);
            Assert.AreEqual(3, cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void Apply_Resume_NewBuffer_SkipsOldBoundary_EmitsNew()
        {
            var prior = new FileTargetCursor(File1, 200, 2);
            var rows = new List<RawXEvent>
            {
                Ev(File1, 200), Ev(File1, 200), // boundary - already consumed
                Ev(File1, 300), Ev(File1, 300)  // new buffer
            };

            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, prior);

            Assert.AreEqual(2, newEvents.Count);
            Assert.IsTrue(newEvents.All(e => e.Offset == 300));
            Assert.AreEqual(300, cursor.Offset);
            Assert.AreEqual(2, cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void Apply_EmptyRead_KeepsCursorUnchanged()
        {
            var prior = new FileTargetCursor(File1, 200, 2);

            var (newEvents, cursor) = FileTargetCursorReader.Apply(new List<RawXEvent>(), prior);

            Assert.AreEqual(0, newEvents.Count);
            Assert.AreEqual(File1, cursor.FileName);
            Assert.AreEqual(200, cursor.Offset);
            Assert.AreEqual(2, cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void Apply_FileRollover_EmitsEventsFromNewFile()
        {
            var prior = new FileTargetCursor(File1, 500, 1);
            var rows = new List<RawXEvent>
            {
                Ev(File1, 500),  // boundary in old file
                Ev(File2, 100),  // rolled into a new file
                Ev(File2, 100)
            };

            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, prior);

            Assert.AreEqual(2, newEvents.Count);
            Assert.IsTrue(newEvents.All(e => e.FileName == File2));
            Assert.AreEqual(File2, cursor.FileName);
            Assert.AreEqual(100, cursor.Offset);
            Assert.AreEqual(2, cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void Apply_NullRows_ReturnsEmpty_AndKeepsCursor()
        {
            var prior = new FileTargetCursor(File1, 10, 1);

            var (newEvents, cursor) = FileTargetCursorReader.Apply(null, prior);

            Assert.AreEqual(0, newEvents.Count);
            Assert.AreEqual(10, cursor.Offset);
        }

        // ---- XETraceShredder --------------------------------------------------------------------

        [TestMethod]
        public void Shred_BatchCompletedEvent_MapsDataAndActions()
        {
            var xml =
                "<event name=\"sql_batch_completed\" package=\"sqlserver\" timestamp=\"2024-01-01T00:00:00.000Z\">" +
                "<data name=\"duration\"><value>1500</value></data>" +
                "<data name=\"cpu_time\"><value>1000</value></data>" +
                "<data name=\"logical_reads\"><value>50</value></data>" +
                "<data name=\"batch_text\"><value>SELECT 1</value></data>" +
                "<action name=\"client_app_name\" package=\"sqlserver\"><value>SQLCMD</value></action>" +
                "<action name=\"session_id\" package=\"sqlserver\"><value>55</value></action>" +
                "</event>";
            var events = new List<RawXEvent> { new(File1, 100, xml) };

            var dt = XETraceShredder.Shred(events);

            Assert.AreEqual(1, dt.Rows.Count);
            var r = dt.Rows[0];
            Assert.AreEqual("sql_batch_completed", r["event_type"]);
            Assert.AreEqual(1500L, Convert.ToInt64(r["duration"]));
            Assert.AreEqual(1000L, Convert.ToInt64(r["cpu_time"]));
            Assert.AreEqual("SELECT 1", r["batch_text"]);
            Assert.AreEqual("SQLCMD", r["client_app_name"]);
            Assert.AreEqual(55, Convert.ToInt32(r["session_id"]));
        }

        [TestMethod]
        public void Shred_MultipleEvents_ProducesRowPerEvent()
        {
            var events = new List<RawXEvent> { Ev(File1, 100), Ev(File1, 100, "rpc_completed") };

            var dt = XETraceShredder.Shred(events);

            Assert.AreEqual(2, dt.Rows.Count);
        }

        [TestMethod]
        public void Shred_Empty_ReturnsBaseSchemaWithNoRows()
        {
            var dt = XETraceShredder.Shred(new List<RawXEvent>());

            Assert.AreEqual(0, dt.Rows.Count);
            // Dynamic schema: only the always-present base columns exist until events add their own fields.
            Assert.IsTrue(dt.Columns.Contains("event_type"));
            Assert.IsTrue(dt.Columns.Contains("timestamp"));
        }

        [TestMethod]
        public void Shred_ErrorReported_CapturesEventSpecificFields()
        {
            // error_reported has none of the *_completed fields but carries error_number/severity/message.
            var xml =
                "<event name=\"error_reported\" package=\"sqlserver\" timestamp=\"2024-01-01T00:00:00.000Z\">" +
                "<data name=\"error_number\"><value>208</value></data>" +
                "<data name=\"severity\"><value>16</value></data>" +
                "<data name=\"message\"><value>Invalid object name 'x'.</value></data>" +
                "</event>";
            var dt = XETraceShredder.Shred(new List<RawXEvent> { new(File1, 100, xml) });

            Assert.AreEqual(1, dt.Rows.Count);
            var r = dt.Rows[0];
            Assert.AreEqual("error_reported", r["event_type"]);
            Assert.AreEqual(208L, Convert.ToInt64(r["error_number"]));
            Assert.AreEqual(16L, Convert.ToInt64(r["severity"]));
            Assert.AreEqual("Invalid object name 'x'.", r["message"]);
        }

        [TestMethod]
        public void Shred_XmlTypedField_PreservesNestedPlanXml()
        {
            // query_post_execution_showplan carries showplan_xml as an xml-typed value: the <value> element holds
            // nested XML (the ShowPlanXML tree), not text.  The shredder must preserve the markup, not strip it.
            var xml =
                "<event name=\"query_post_execution_showplan\" package=\"sqlserver\" timestamp=\"2024-01-01T00:00:00.000Z\">" +
                "<data name=\"showplan_xml\"><type name=\"xml\" package=\"package0\" /><value>" +
                "<ShowPlanXML xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\">" +
                "<BatchSequence><Batch><Statements /></Batch></BatchSequence>" +
                "</ShowPlanXML>" +
                "</value></data>" +
                "</event>";

            var dt = XETraceShredder.Shred(new List<RawXEvent> { new(File1, 100, xml) });

            Assert.AreEqual(1, dt.Rows.Count);
            var plan = dt.Rows[0]["showplan_xml"] as string;
            Assert.IsFalse(string.IsNullOrWhiteSpace(plan), "showplan_xml must not be blank");
            StringAssert.Contains(plan, "ShowPlanXML");
            StringAssert.Contains(plan, "BatchSequence");
        }

        [TestMethod]
        public void Shred_SkipsNullOrEmptyEventData()
        {
            var events = new List<RawXEvent> { new(File1, 100, null), new(File1, 100, ""), Ev(File1, 100) };

            var dt = XETraceShredder.Shred(events);

            Assert.AreEqual(1, dt.Rows.Count);
        }

        // ---- RingBufferTraceReader guards -------------------------------------------------------

        [TestMethod]
        [DataRow("bad name")]
        [DataRow("bad]name")]
        [DataRow("")]
        public void RingBufferReader_InvalidSessionName_Throws(string name)
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new RingBufferTraceReader("Server=x;", name, databaseScoped: false));
        }

        [TestMethod]
        public void RingBufferReader_ValidSessionName_Constructs()
        {
            var reader = new RingBufferTraceReader("Server=x;", "DBADash_AdHoc", databaseScoped: false);
            Assert.IsNotNull(reader);
        }
    }
}
