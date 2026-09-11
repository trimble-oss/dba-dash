using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBADash.XE;

namespace DBADash.Deadlocks
{
    /// <summary>
    /// Keeps each instance's deadlock read position across service restarts.
    ///
    /// <para>Without it a restart loses every cursor, and the next run of each instance's collection pages
    /// through that instance's whole event file set again.  The repository's dedup means nothing is stored
    /// twice, so that is wasted work rather than damage - but it is a full read per monitored instance every
    /// time the service is restarted, which during a run of config changes is often.</para>
    ///
    /// <para>A file beside the service rather than a table in the repository, because the service has no
    /// repository to write to when it collects to S3 or a folder - the same constraint that made the cursor
    /// service-local in the first place.</para>
    ///
    /// <para>Losing or corrupting the file costs one full read per instance, exactly as before, so nothing
    /// here fails a collection: every path falls back to starting from no cursor.  A cursor that survives a
    /// restart is also more likely to have gone stale than one a few minutes old, which is why
    /// <see cref="EventFileTraceReader"/> recovers from a resume position that no longer exists rather than
    /// trusting what is stored here.</para>
    ///
    /// <para>The file holds every instance, so writing it costs the same whether one cursor moved or all of
    /// them did.  <see cref="Save"/> therefore only records the cursor and marks the file out of date; a timer
    /// writes it.  Without that, an instance reading system_health - where the cursor advances on every run,
    /// deadlock or not, because that session is always emitting something - would rewrite the whole file on
    /// every collection, and a few hundred of them would do so a few hundred times per collection cycle, each
    /// time serializing every other instance's cursor too and holding the write lock while the collection
    /// threads queued behind it.</para>
    /// </summary>
    public static class DeadlockCursorStore
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "DeadlockCursors.json");

        /// <summary>How long a moved cursor may sit in memory before the file catches up.  A hard kill inside
        /// the window loses that much progress, which costs the affected instances one read from an older
        /// position - the same price as losing the file, and dedup discards what comes back.</summary>
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

        private static readonly ConcurrentDictionary<string, DeadlockCollectionState> States =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>What is already on disk for each instance, so a run that did not move the cursor does not
        /// rewrite an unchanged file.</summary>
        private static readonly ConcurrentDictionary<string, FileTargetCursor> Persisted =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object SaveLock = new();

        /// <summary>Set once the file has been read, not before: a caller that saw it set early would take a
        /// state with no cursor for an instance the file has a position for, and read that instance's whole
        /// event file set.  Volatile because it is read outside <see cref="SaveLock"/>.</summary>
        private static volatile bool _loaded;

        /// <summary>Set when a cursor has moved past what <see cref="Persisted"/> says is on disk, cleared by
        /// the write that takes it there.  Cleared before the write rather than after, so a cursor that moves
        /// while the file is being written is not mistaken for one the write already covered.</summary>
        private static int _dirty;

        private static Timer _flushTimer;

        /// <summary>What is written to disk - the cursor only.  The ring buffer's seen-hash set is deliberately
        /// not persisted: it is bounded by the buffer's current contents, so a stale one would suppress
        /// deadlocks that are still there.</summary>
        private sealed class StoredCursor
        {
            public string FileName { get; set; }

            public long Offset { get; set; }

            public int ConsumedAtOffset { get; set; }
        }

        /// <summary>
        /// The state an instance's next read starts from - a detached copy, not the live state that
        /// <see cref="Commit"/> registers.  The read advances the copy, so a run that never commits leaves
        /// the position where it was and the next run reads the same events again rather than stepping over
        /// deadlocks that were collected but never stored.  Dedup on DeadlockHash absorbs whatever that
        /// re-read brings back, so the cost of an unnecessary one is the read itself.
        /// </summary>
        public static DeadlockCollectionState GetPending(string connectionID)
        {
            EnsureLoaded();
            if (!States.TryGetValue(connectionID, out var current)) return new DeadlockCollectionState();
            return new DeadlockCollectionState
            {
                Cursor = current.Cursor,
                // Copied rather than shared: the ring_buffer read replaces this set, and an uncommitted run
                // must leave the previous run's set in place so anything it read but did not store is still
                // treated as unseen.
                SeenHashes = new HashSet<string>(current.SeenHashes, StringComparer.Ordinal)
            };
        }

        /// <summary>
        /// Makes the state a run reached the one the next run starts from, and records the cursor for the
        /// flush timer to write.  Called once the run's deadlocks have reached a destination - see
        /// <see cref="DBCollector.CommitDeadlockCursor"/> - so at the collection's own cadence rather than per
        /// batch, and does no disk IO of its own: the collection thread is not the place to write a file
        /// shared with every other instance.
        /// </summary>
        public static void Commit(string connectionID, DeadlockCollectionState state)
        {
            if (string.IsNullOrEmpty(connectionID) || state == null) return;
            EnsureLoaded();
            States[connectionID] = state;

            // The cursor is the only thing written, so a run that did not move it has nothing new to say.
            // Worth checking rather than always marking dirty: the ring_buffer path never sets a cursor at
            // all, and that is the path every Azure SQL Database source takes, so without this each of them
            // would keep the file permanently out of date and the timer would write it forever.
            if (Persisted.TryGetValue(connectionID, out var written) && SamePosition(written, state.Cursor)) return;
            Interlocked.Exchange(ref _dirty, 1);
        }

        /// <summary>
        /// Writes any moved cursors out now rather than waiting for the timer.  For service shutdown - the
        /// timer stops with the process, and without this the last flush interval's progress is lost on every
        /// clean stop rather than only on a kill.
        /// </summary>
        public static void Flush()
        {
            if (Interlocked.Exchange(ref _dirty, 0) == 0) return;
            if (!WriteFile()) Interlocked.Exchange(ref _dirty, 1);
        }

        private static void OnFlushTimer(object _) => Flush();

        private static bool SamePosition(FileTargetCursor a, FileTargetCursor b) =>
            a.HasValue == b.HasValue
            && a.FileName == b.FileName
            && a.Offset == b.Offset
            && a.ConsumedAtOffset == b.ConsumedAtOffset;

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (SaveLock)
            {
                if (_loaded) return;

                try
                {
                    if (!File.Exists(FilePath)) return;
                    var stored = JsonConvert.DeserializeObject<Dictionary<string, StoredCursor>>(
                        File.ReadAllText(FilePath));
                    if (stored == null) return;

                    foreach (var entry in stored)
                    {
                        if (entry.Value?.FileName == null) continue;
                        var cursor = new FileTargetCursor(entry.Value.FileName, entry.Value.Offset,
                            entry.Value.ConsumedAtOffset);
                        States[entry.Key] = new DeadlockCollectionState { Cursor = cursor };
                        Persisted[entry.Key] = cursor;
                    }
                    Log.Debug("Restored {count} deadlock read position(s) from {path}", States.Count, FilePath);
                }
                catch (Exception ex)
                {
                    // Starting from no cursor is the pre-existing behaviour and always safe, so a bad file is
                    // worth a warning and nothing more.
                    Log.Warning(ex, "Could not read deadlock read positions from {path}; starting from the " +
                                    "beginning of each instance's event file set.", FilePath);
                }
                finally
                {
                    // Set last, and under the lock, so no caller can take a state before the cursors are in
                    // place.  Set even when the read failed: starting from no cursor is the safe fallback, and
                    // retrying a bad file on every collection would only repeat the warning.
                    _loaded = true;

                    // Started here rather than in a static initializer so a process that never collects
                    // deadlocks - the GUI references this assembly too - never arms it.  A threading timer
                    // does not hold the process open, so nothing has to stop it on shutdown.
                    _flushTimer = new Timer(OnFlushTimer, null, FlushInterval, FlushInterval);
                }
            }
        }

        private static bool WriteFile()
        {
            lock (SaveLock)
            {
                try
                {
                    var stored = new Dictionary<string, StoredCursor>(StringComparer.OrdinalIgnoreCase);
                    // Every instance's cursor is taken, not only those with a position to store, because what
                    // this becomes is the record of what the file now says.  An instance with no cursor - the
                    // ring_buffer path - has to be in it, or nothing would ever mark it as up to date.
                    var written = new List<KeyValuePair<string, FileTargetCursor>>(States.Count);
                    foreach (var entry in States)
                    {
                        written.Add(new KeyValuePair<string, FileTargetCursor>(entry.Key, entry.Value.Cursor));
                        if (!entry.Value.Cursor.HasValue) continue;
                        stored[entry.Key] = new StoredCursor
                        {
                            FileName = entry.Value.Cursor.FileName,
                            Offset = entry.Value.Cursor.Offset,
                            ConsumedAtOffset = entry.Value.Cursor.ConsumedAtOffset
                        };
                    }

                    // Written to a temporary file and moved into place: a service killed mid-write would
                    // otherwise leave a truncated file, and the next start would discard every cursor rather
                    // than just the one being written.
                    var temp = FilePath + ".tmp";
                    File.WriteAllText(temp, JsonConvert.SerializeObject(stored, Formatting.Indented));
                    File.Move(temp, FilePath, true);

                    // Recorded only once the file is actually on disk, so a failed write leaves every instance
                    // looking unsaved and the next flush retries the lot.  All of them, not just whichever
                    // instance prompted the write: this write persisted their cursors as well, and treating
                    // them as unsaved would have each one dirty the file again for a position already in it.
                    foreach (var entry in written) Persisted[entry.Key] = entry.Value;
                    return true;
                }
                catch (Exception ex)
                {
                    // The cursor is still held in memory, so this run and the ones after it are unaffected -
                    // only a restart would go back to reading from the start.
                    Log.Warning(ex, "Could not write deadlock read positions to {path}.", FilePath);
                    return false;
                }
            }
        }
    }
}
