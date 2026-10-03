using System;
using System.Collections.Generic;
using DBADash.XE;

namespace DBADash.SlowQueries
{
    /// <summary>
    /// What the slow query collection has to remember between runs, per monitored instance and session.  Only the
    /// event file and existing session modes have any: the default ring buffer mode empties its buffer on every
    /// read, so there is nothing to resume from.
    ///
    /// <para>Service-local rather than repository state, as for deadlocks - see
    /// <see cref="Deadlocks.DeadlockCollectionState"/>.  Losing it is safe: the next run reads from the start of
    /// the file set, and dbo.SlowQueries_Upd only inserts events newer than what it already holds.</para>
    /// </summary>
    public sealed class SlowQueryCollectionState
    {
        private readonly object cursorLock = new();

        private FileTargetCursor cursor = FileTargetCursor.None;

        /// <summary>
        /// Resume position in the event_file target.  Locked because the store's flush timer reads it from another
        /// thread, and the struct is wide enough to tear - see <see cref="Deadlocks.DeadlockCollectionState.Cursor"/>.
        /// </summary>
        public FileTargetCursor Cursor
        {
            get { lock (cursorLock) { return cursor; } }
            set { lock (cursorLock) { cursor = value; } }
        }

        /// <summary>
        /// The events the previous read of an existing session's ring buffer saw - a buffer DBA Dash may not empty,
        /// so every read returns everything it holds.  Replaced after each read, so it stays bounded by the buffer.
        /// Held in memory only: after a restart the buffer is sent once more and the repository discards it.
        /// </summary>
        public HashSet<string> SeenEvents { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The session's dropped_event_count at the previous read, so each run reports the events dropped since
        /// the last one rather than since the session started.  Null until the first read.  In memory only.
        /// </summary>
        public long? DroppedEventCount { get; set; }
    }
}
