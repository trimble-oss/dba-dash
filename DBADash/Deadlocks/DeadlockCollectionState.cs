using System;
using System.Collections.Generic;
using DBADash.XE;

namespace DBADash.Deadlocks
{
    /// <summary>
    /// What the deadlock collection has to remember between runs, per monitored instance.
    ///
    /// <para>Service-local rather than repository state, deliberately: in collect-to-S3 mode the service has
    /// no repository to read.  Losing it on restart is therefore expected and safe - the next run falls back
    /// to a bounded read and the repository's dedup on DeadlockHash absorbs whatever comes back twice.</para>
    /// </summary>
    public sealed class DeadlockCollectionState
    {
        private readonly object cursorLock = new();

        private FileTargetCursor cursor = FileTargetCursor.None;

        /// <summary>
        /// Resume position in the event_file target, so each run reads only what is new.  Not used by the
        /// ring_buffer path, which has no equivalent.
        ///
        /// <para>Locked rather than left as an auto-property because two threads reach it: the collection
        /// thread advances it after every batch, and <see cref="DeadlockCursorStore"/>'s flush timer reads it
        /// to write the file.  <see cref="FileTargetCursor"/> is three fields wide, so an unsynchronized read
        /// can tear - pairing one read's file name with another's offset, which is a position that may well
        /// exist and so would not be caught by the stale-cursor recovery in
        /// <see cref="EventFileTraceReader"/>.</para>
        /// </summary>
        public FileTargetCursor Cursor
        {
            get { lock (cursorLock) { return cursor; } }
            set { lock (cursorLock) { cursor = value; } }
        }

        /// <summary>
        /// Hashes of the deadlocks the previous ring_buffer read saw, so an unchanged buffer doesn't resend
        /// its whole contents every run.  Replaced (not added to) after each read, so it stays bounded by the
        /// size of the buffer: a graph that ages out of the buffer also leaves this set, and cannot come back.
        /// Unused by the event_file path, where the cursor does the job.
        /// </summary>
        public HashSet<string> SeenHashes { get; set; } = new(StringComparer.Ordinal);
    }
}
