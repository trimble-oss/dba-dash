using System;
using System.Collections.Generic;
using DBADash.Deadlocks;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// What decides whether a deadlock collection run is a first run - and so reads system_health as well - and
    /// what keeps that decision across a restart.
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
            Assert.IsTrue(DeadlockCursorStore.GetPending(NewKey()).IsFirstRun);
        }

        [TestMethod]
        public void AnAttemptedBackfill_IsNotRepeated_WhenTheRunNeverCommits()
        {
            // A timed out backfill, or one whose run failed to reach a destination, must not be read again on
            // every run that follows.
            var key = NewKey();
            DeadlockCursorStore.MarkBackfillAttempted(key);

            var next = DeadlockCursorStore.GetPending(key);
            Assert.IsFalse(next.IsFirstRun);
            Assert.IsFalse(next.Cursor.HasValue, "The configured session is still read from the start, as a first run would");
        }

        [TestMethod]
        public void MarkingABackfill_DoesNotOverwriteACommittedPosition()
        {
            var key = NewKey();
            var cursor = new FileTargetCursor(@"C:\XE\DBADash_Deadlocks_0_1.xel", 4096, 2);
            DeadlockCursorStore.Commit(key, new DeadlockCollectionState { Cursor = cursor });

            DeadlockCursorStore.MarkBackfillAttempted(key);

            var next = DeadlockCursorStore.GetPending(key);
            Assert.AreEqual(cursor.FileName, next.Cursor.FileName);
            Assert.AreEqual(cursor.Offset, next.Cursor.Offset);
            Assert.AreEqual(cursor.ConsumedAtOffset, next.Cursor.ConsumedAtOffset);
        }

        [TestMethod]
        public void AnEntryWithNoCursor_SurvivesARestart()
        {
            // The entry is the record that the instance is past its first run, so dropping it on the way through
            // the file would repeat the backfill after every service restart.
            var json = DeadlockCursorStore.Serialize(new[]
            {
                new KeyValuePair<string, FileTargetCursor>("NoCursor", FileTargetCursor.None),
                new KeyValuePair<string, FileTargetCursor>("WithCursor",
                    new FileTargetCursor(@"C:\XE\system_health_0_1.xel", 1024, 3))
            });

            var restored = DeadlockCursorStore.Deserialize(json);

            Assert.AreEqual(2, restored.Count);
            Assert.IsTrue(restored.ContainsKey("NoCursor"));
            Assert.IsFalse(restored["NoCursor"].HasValue);
            Assert.AreEqual(@"C:\XE\system_health_0_1.xel", restored["WithCursor"].FileName);
            Assert.AreEqual(1024, restored["WithCursor"].Offset);
            Assert.AreEqual(3, restored["WithCursor"].ConsumedAtOffset);
        }

        [TestMethod]
        public void AnEmptyFile_RestoresNothing()
        {
            Assert.AreEqual(0, DeadlockCursorStore.Deserialize("null").Count);
        }
    }
}
