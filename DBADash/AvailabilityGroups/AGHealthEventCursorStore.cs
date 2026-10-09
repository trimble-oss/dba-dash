using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBADash.XE;

namespace DBADash.AvailabilityGroups
{
    /// <summary>
    /// Resume position in the AlwaysOn_health event file set for one instance.  Locked because the store's flush timer
    /// reads it from another thread, and the struct is wide enough to tear - see
    /// <see cref="Deadlocks.DeadlockCollectionState.Cursor"/>.
    /// </summary>
    public sealed class AGHealthEventCollectionState
    {
        private readonly object cursorLock = new();

        private FileTargetCursor cursor = FileTargetCursor.None;

        public FileTargetCursor Cursor
        {
            get { lock (cursorLock) { return cursor; } }
            set { lock (cursorLock) { cursor = value; } }
        }
    }

    /// <summary>
    /// Keeps each instance's AlwaysOn_health read position across service restarts.
    ///
    /// <para>The same approach as <see cref="SlowQueries.SlowQueryCursorStore"/>: a file beside the service rather than a
    /// repository table, because the service may be collecting to S3 or a folder; the state a run takes is a copy, made
    /// current only once the run's data has reached a destination; and a timer writes the file.  Losing the file costs
    /// one read from the start of the file set, and the repository discards events it already holds.</para>
    /// </summary>
    public static class AGHealthEventCursorStore
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "AGHealthEventCursors.json");

        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

        private static readonly ConcurrentDictionary<string, AGHealthEventCollectionState> States =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>What is on disk for each key, so an unmoved cursor doesn't dirty the file.</summary>
        private static readonly ConcurrentDictionary<string, FileTargetCursor> Persisted =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object SaveLock = new();

        private static volatile bool _loaded;

        private static int _dirty;

        private static Timer _flushTimer;

        private sealed class StoredCursor
        {
            public string FileName { get; set; }

            public long Offset { get; set; }

            public int ConsumedAtOffset { get; set; }
        }

        /// <summary>A detached copy of the state the next read for <paramref name="key"/> starts from.</summary>
        public static AGHealthEventCollectionState GetPending(string key)
        {
            EnsureLoaded();
            if (!States.TryGetValue(key, out var current)) return new AGHealthEventCollectionState();
            return new AGHealthEventCollectionState { Cursor = current.Cursor };
        }

        /// <summary>Makes <paramref name="state"/> the one the next run starts from.  No disk IO - see the class notes.</summary>
        public static void Commit(string key, AGHealthEventCollectionState state)
        {
            if (string.IsNullOrEmpty(key) || state == null) return;
            EnsureLoaded();
            States[key] = state;
            if (!state.Cursor.HasValue) return;
            if (Persisted.TryGetValue(key, out var written) && SamePosition(written, state.Cursor)) return;
            Interlocked.Exchange(ref _dirty, 1);
        }

        /// <summary>Writes moved cursors now rather than waiting for the timer.  For service shutdown.</summary>
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
                    foreach (var entry in Deserialize(File.ReadAllText(FilePath)))
                    {
                        States[entry.Key] = new AGHealthEventCollectionState { Cursor = entry.Value };
                        Persisted[entry.Key] = entry.Value;
                    }
                    Log.Debug("Restored {count} AlwaysOn_health read position(s) from {path}", States.Count, FilePath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not read AlwaysOn_health read positions from {path}; starting from the " +
                                    "beginning of each instance's event file set.", FilePath);
                }
                finally
                {
                    _loaded = true;
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
                    var written = new List<KeyValuePair<string, FileTargetCursor>>(States.Count);
                    foreach (var entry in States)
                    {
                        var cursor = entry.Value.Cursor;
                        if (cursor.HasValue) written.Add(new KeyValuePair<string, FileTargetCursor>(entry.Key, cursor));
                    }

                    // Written aside and moved into place, so a kill mid-write can't leave a truncated file.
                    var temp = FilePath + ".tmp";
                    File.WriteAllText(temp, Serialize(written));
                    File.Move(temp, FilePath, true);

                    foreach (var entry in written) Persisted[entry.Key] = entry.Value;
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not write AlwaysOn_health read positions to {path}.", FilePath);
                    return false;
                }
            }
        }

        internal static string Serialize(IEnumerable<KeyValuePair<string, FileTargetCursor>> positions)
        {
            var stored = new Dictionary<string, StoredCursor>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in positions)
            {
                if (!entry.Value.HasValue) continue;
                stored[entry.Key] = new StoredCursor
                {
                    FileName = entry.Value.FileName,
                    Offset = entry.Value.Offset,
                    ConsumedAtOffset = entry.Value.ConsumedAtOffset
                };
            }
            return JsonConvert.SerializeObject(stored, Formatting.Indented);
        }

        internal static Dictionary<string, FileTargetCursor> Deserialize(string json)
        {
            var positions = new Dictionary<string, FileTargetCursor>(StringComparer.OrdinalIgnoreCase);
            var stored = JsonConvert.DeserializeObject<Dictionary<string, StoredCursor>>(json);
            if (stored == null) return positions;
            foreach (var entry in stored)
            {
                if (entry.Value?.FileName == null) continue;
                positions[entry.Key] =
                    new FileTargetCursor(entry.Value.FileName, entry.Value.Offset, entry.Value.ConsumedAtOffset);
            }
            return positions;
        }
    }
}
