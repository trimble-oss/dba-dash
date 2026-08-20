using System.Collections.Generic;
using System.Linq;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for the non-destructive ring_buffer <b>watch</b> diff (<see cref="RingBufferWatchDiff"/>), which detects
    /// new events across successive reads of a running session's ring buffer without flushing it.
    /// </summary>
    [TestClass]
    public class XESessionWatchTests
    {
        private static string Event(string ts, string name = "sql_batch_completed") =>
            $"<event name=\"{name}\" package=\"sqlserver\" timestamp=\"{ts}\">" +
            $"<data name=\"duration\"><value>10</value></data></event>";

        private static string RingBuffer(params string[] events) =>
            "<RingBufferTarget truncated=\"0\">" + string.Concat(events) + "</RingBufferTarget>";

        [TestMethod]
        public void Apply_FirstRead_ReturnsAllEvents_AndPopulatesSeen()
        {
            var data = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:02.000Z"));

            var (fresh, seen) = RingBufferWatchDiff.Apply(data, null);

            Assert.AreEqual(2, fresh.Count);
            Assert.AreEqual(2, seen.Count);
        }

        [TestMethod]
        public void Apply_SecondRead_OnlyReturnsNewEvents()
        {
            var first = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:02.000Z"));
            var (_, seen1) = RingBufferWatchDiff.Apply(first, null);

            // The buffer still holds the first two events plus a third.
            var second = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:02.000Z"),
                Event("2024-01-01T00:00:03.000Z"));

            var (fresh, seen2) = RingBufferWatchDiff.Apply(second, seen1);

            Assert.AreEqual(1, fresh.Count, "only the genuinely new event is returned");
            Assert.AreEqual(3, seen2.Count);
        }

        [TestMethod]
        public void Apply_NoNewEvents_ReturnsEmpty()
        {
            var data = RingBuffer(Event("2024-01-01T00:00:01.000Z"));
            var (_, seen1) = RingBufferWatchDiff.Apply(data, null);

            var (fresh, _) = RingBufferWatchDiff.Apply(data, seen1);

            Assert.AreEqual(0, fresh.Count);
        }

        [TestMethod]
        public void Apply_RolledBuffer_OldEventsDropped_NewEventsReturned()
        {
            var first = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:02.000Z"));
            var (_, seen1) = RingBufferWatchDiff.Apply(first, null);

            // Ring buffer rolled: the first event aged out, two new ones arrived.
            var second = RingBuffer(Event("2024-01-01T00:00:02.000Z"), Event("2024-01-01T00:00:03.000Z"),
                Event("2024-01-01T00:00:04.000Z"));

            var (fresh, _) = RingBufferWatchDiff.Apply(second, seen1);

            Assert.AreEqual(2, fresh.Count);
        }

        [TestMethod]
        public void Apply_WithinRead_DuplicatesPreserved()
        {
            // Two distinct events that happen to be byte-identical are both real events - both returned on first sight.
            var data = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:01.000Z"));

            var (fresh, seen) = RingBufferWatchDiff.Apply(data, null);

            Assert.AreEqual(2, fresh.Count, "within a read, identical events are both emitted");
            Assert.AreEqual(1, seen.Count, "they collapse to one hash in the seen set");
        }

        [TestMethod]
        public void Apply_AcrossReads_ByteIdenticalNewEvent_Suppressed_DocumentedLimitation()
        {
            // A genuinely new event whose XML is byte-identical to one already seen collides and is suppressed - the
            // accepted trade-off for a tail that never flushes the session.
            var first = RingBuffer(Event("2024-01-01T00:00:01.000Z"));
            var (_, seen1) = RingBufferWatchDiff.Apply(first, null);

            var second = RingBuffer(Event("2024-01-01T00:00:01.000Z"), Event("2024-01-01T00:00:01.000Z"));
            var (fresh, _) = RingBufferWatchDiff.Apply(second, seen1);

            Assert.AreEqual(0, fresh.Count);
        }

        [TestMethod]
        public void Apply_EmptyOrInvalidXml_ReturnsEmpty()
        {
            Assert.AreEqual(0, RingBufferWatchDiff.Apply(null, null).NewEvents.Count);
            Assert.AreEqual(0, RingBufferWatchDiff.Apply("", null).NewEvents.Count);
            Assert.AreEqual(0, RingBufferWatchDiff.Apply("<not-valid", null).NewEvents.Count);
        }

        [TestMethod]
        public void Apply_FreshEvents_ShredIntoRows()
        {
            var data = RingBuffer(Event("2024-01-01T00:00:01.000Z", "rpc_completed"));

            var (fresh, _) = RingBufferWatchDiff.Apply(data, null);
            var table = XETraceShredder.ShredElements(fresh);

            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("rpc_completed", table.Rows[0]["event_type"]);
            Assert.IsTrue(table.Columns.Contains("duration"));
        }
    }
}
