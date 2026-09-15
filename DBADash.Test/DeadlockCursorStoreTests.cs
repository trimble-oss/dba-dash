using System;
using System.Collections.Generic;
using DBADash.Deadlocks;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// What decides whether a deadlock collection run is a first run - and so schedules the system_health backfill -
    /// what records the backfill as pending until it has run, and what keeps both across a restart.
    ///
    /// <para>The store is process wide, so every test takes a key of its own rather than sharing one.</para>
    /// </summary>
    [TestClass]
    public class DeadlockCursorStoreTests
    {
        private static string NewKey() => "Test|" + Guid.NewGuid();

        [TestMethod]
        public void AnInstanceWithNothingStored_IsOnItsFirstRun()
        {
            var key = NewKey();
            Assert.IsTrue(DeadlockCursorStore.GetPending(key).IsFirstRun);
            Assert.IsFalse(DeadlockCursorStore.IsBackfillPending(key));
        }

        [TestMethod]
        public void AScheduledBackfill_IsNotScheduledAgain_WhenTheRunNeverCommits()
        {
            // The run that schedules it may fail to reach a destination; the next run must not schedule a second.
            var key = NewKey();
            DeadlockCursorStore.MarkBackfillPending(key);

            var next = DeadlockCursorStore.GetPending(key);
            Assert.IsFalse(next.IsFirstRun);
            Assert.IsTrue(DeadlockCursorStore.IsBackfillPending(key));
            Assert.IsFalse(next.Cursor.HasValue, "The configured session is still read from the start, as a first run would");
        }

        [TestMethod]
        public void ACompletedBackfill_IsNoLongerPending_AndTheInstanceStaysPastItsFirstRun()
        {
            var key = NewKey();
            DeadlockCursorStore.MarkBackfillPending(key);

            DeadlockCursorStore.CompleteBackfill(key);

            Assert.IsFalse(DeadlockCursorStore.IsBackfillPending(key));
            Assert.IsFalse(DeadlockCursorStore.GetPending(key).IsFirstRun);
        }

        [TestMethod]
        public void ACommitAfterScheduling_DoesNotClearThePendingBackfill_AndACommitDoesNotRestoreACompletedOne()
        {
            // A run takes a copy of its state before the backfill finishes and commits it afterwards.  The pending
            // flag is kept apart from that state, so neither order of events puts it in the wrong place.
            var key = NewKey();
            DeadlockCursorStore.MarkBackfillPending(key);
            var run = DeadlockCursorStore.GetPending(key);

            DeadlockCursorStore.Commit(key, run);
            Assert.IsTrue(DeadlockCursorStore.IsBackfillPending(key), "A commit is not the backfill running");

            var runStartedBeforeCompletion = DeadlockCursorStore.GetPending(key);
            DeadlockCursorStore.CompleteBackfill(key);
            DeadlockCursorStore.Commit(key, runStartedBeforeCompletion);
            Assert.IsFalse(DeadlockCursorStore.IsBackfillPending(key), "A stale run must not schedule it again");
        }

        [TestMethod]
        public void MarkingABackfill_DoesNotOverwriteACommittedPosition()
        {
            var key = NewKey();
            var cursor = new FileTargetCursor(@"C:\XE\DBADash_Deadlocks_0_1.xel", 4096, 2);
            DeadlockCursorStore.Commit(key, new DeadlockCollectionState { Cursor = cursor });

            DeadlockCursorStore.MarkBackfillPending(key);

            var next = DeadlockCursorStore.GetPending(key);
            Assert.AreEqual(cursor.FileName, next.Cursor.FileName);
            Assert.AreEqual(cursor.Offset, next.Cursor.Offset);
            Assert.AreEqual(cursor.ConsumedAtOffset, next.Cursor.ConsumedAtOffset);
            Assert.IsFalse(DeadlockCursorStore.IsBackfillPending(key),
                "An instance with an entry is past its first run, so a run that thought otherwise is stale");
        }

        [TestMethod]
        public void AStaleFirstRun_DoesNotScheduleACompletedBackfillAgain()
        {
            // Two runs both start as a first run.  The first schedules the backfill, which runs and completes before
            // the second gets as far as marking it - the second must not schedule another.
            var key = NewKey();
            var first = DeadlockCursorStore.GetPending(key);
            var second = DeadlockCursorStore.GetPending(key);
            Assert.IsTrue(first.IsFirstRun && second.IsFirstRun);

            DeadlockCursorStore.MarkBackfillPending(key);
            DeadlockCursorStore.CompleteBackfill(key);
            DeadlockCursorStore.MarkBackfillPending(key);

            Assert.IsFalse(DeadlockCursorStore.IsBackfillPending(key));
        }

        [TestMethod]
        public void EntriesSurviveARestart_WithOrWithoutACursorOrAPendingBackfill()
        {
            // An entry with no cursor is the record that the instance is past its first run, and the pending flag is
            // what carries a queued backfill across a restart - the queue itself is in memory.
            var json = DeadlockCursorStore.Serialize(new[]
            {
                new KeyValuePair<string, DeadlockCursorStore.StoredPosition>("NoCursor",
                    new DeadlockCursorStore.StoredPosition(FileTargetCursor.None, false)),
                new KeyValuePair<string, DeadlockCursorStore.StoredPosition>("Pending",
                    new DeadlockCursorStore.StoredPosition(FileTargetCursor.None, true)),
                new KeyValuePair<string, DeadlockCursorStore.StoredPosition>("WithCursor",
                    new DeadlockCursorStore.StoredPosition(new FileTargetCursor(@"C:\XE\system_health_0_1.xel", 1024, 3), false))
            });

            var restored = DeadlockCursorStore.Deserialize(json);

            Assert.AreEqual(3, restored.Count);
            Assert.IsFalse(restored["NoCursor"].Cursor.HasValue);
            Assert.IsFalse(restored["NoCursor"].BackfillPending);
            Assert.IsTrue(restored["Pending"].BackfillPending);
            Assert.AreEqual(@"C:\XE\system_health_0_1.xel", restored["WithCursor"].Cursor.FileName);
            Assert.AreEqual(1024, restored["WithCursor"].Cursor.Offset);
            Assert.AreEqual(3, restored["WithCursor"].Cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void AFileWrittenBeforeThePendingFlag_ReadsAsNotPending()
        {
            // Those instances had their backfill, or predate it - none should be scheduled by an upgrade.
            var restored = DeadlockCursorStore.Deserialize(
                "{\"Old\":{\"FileName\":\"C:\\\\XE\\\\DBADash_Deadlocks_0_1.xel\",\"Offset\":10,\"ConsumedAtOffset\":1}}");

            Assert.IsFalse(restored["Old"].BackfillPending);
            Assert.AreEqual(10, restored["Old"].Cursor.Offset);
        }

        [TestMethod]
        public void AnEmptyFile_RestoresNothing()
        {
            Assert.AreEqual(0, DeadlockCursorStore.Deserialize("null").Count);
        }
    }
}
