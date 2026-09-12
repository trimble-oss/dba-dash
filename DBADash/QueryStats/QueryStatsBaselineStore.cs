using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Serilog;

namespace DBADash.QueryStats
{
    /// <summary>
    /// Holds one <see cref="Baseline"/> per monitored connection, backed by a file beside the binary.
    ///
    /// <para>The same shape as <see cref="Deadlocks.DeadlockCursorStore"/>, and for the same reason: state
    /// that must survive a service restart, but that has no business being written to the monitored
    /// instance or carried through the repository.  A baseline lost to a deleted file costs the instance
    /// one interval of attribution, not its history.</para>
    ///
    /// <para>A collection moves the baseline on with <see cref="Apply"/> as soon as it has processed its read,
    /// before the data is written - the opposite of the deadlock and slow query cursors, which only move once
    /// the write has succeeded.  Those can afford to re-read, because the repository dedups what comes back.  A
    /// delta can't be deduped: one that covered an interval already stored would count that interval's work a
    /// second time, and a write can fail after part of it has landed - another collection's table, or another
    /// destination.  So a failed write costs its own interval instead, as a gap the coverage figures show.  A
    /// scheduled collection's data is still in the failed message file, and importing that later fills the gap
    /// exactly once.</para>
    ///
    /// <para>Everything from the read to <see cref="Apply"/> runs under <see cref="LockAsync"/>.  Two collections
    /// for one connection at once - a triggered collection on top of the scheduled one is enough - would diff
    /// against the same baseline and report the same interval twice.</para>
    /// </summary>
    public static class QueryStatsBaselineStore
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "QueryStatsBaselines");

        private static readonly ConcurrentDictionary<string, Baseline> Baselines = new(StringComparer.OrdinalIgnoreCase);

        private static readonly AsyncKeyedLocker<string> Locks = new();

        /// <summary>
        /// Exclusive use of a connection's baseline, held from the read of the plan cache until the result has
        /// been applied.  Per connection, so one instance's collection never waits on another's.
        ///
        /// <para>Case-insensitive like <see cref="Baselines"/>, by normalising the key rather than by giving the
        /// locker a case-insensitive comparer.  With the comparer, a key that differs only in case from the one
        /// held doesn't wait asynchronously: the call spins on its thread until the holder lets go.</para>
        /// </summary>
        public static async ValueTask<IDisposable> LockAsync(string connectionID) =>
            await Locks.LockAsync(connectionID.ToUpperInvariant()).ConfigureAwait(false);

        public static Baseline Get(string connectionID, int maxEntries)
        {
            var baseline = Baselines.GetOrAdd(connectionID, id =>
            {
                try
                {
                    return Baseline.Load(GetPath(id), maxEntries);
                }
                catch (Exception ex)
                {
                    // A corrupt or unreadable file is not worth failing a collection over.  Starting empty
                    // costs one interval, and the file is replaced by the next collection.
                    Log.Warning(ex, "Could not read the query stats baseline for {ConnectionID}. Starting a new one.", id);
                    return new Baseline { MaxEntries = maxEntries };
                }
            });
            baseline.MaxEntries = maxEntries;
            return baseline;
        }

        /// <summary>
        /// Move the connection's baseline on to a processed collection and persist it.  Called with
        /// <see cref="LockAsync"/> held, before the collection's data is written - see the class remarks.
        /// </summary>
        public static void Apply(string connectionID, Baseline baseline, QueryStatsResult result)
        {
            baseline.Apply(result);
            try
            {
                baseline.Save(GetPath(connectionID));
            }
            catch (Exception ex)
            {
                // The in-memory baseline is still correct, so the collection carries on.  A restart before the
                // next successful save would load the older file and report intervals that were already sent;
                // the repository refuses a collection that overlaps one it holds, so that costs an interval
                // rather than counting one twice.
                Log.Warning(ex, "Could not save the query stats baseline for {ConnectionID}.", connectionID);
            }
        }

        /// <summary>Remove a connection's baseline from memory and disk.  Used by tests.</summary>
        public static void Clear(string connectionID)
        {
            Baselines.TryRemove(connectionID, out _);
            var path = GetPath(connectionID);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Named from the id in upper case, so it is case-insensitive like <see cref="Baselines"/>.  A
        /// connection whose id changes only in case keeps its baseline rather than starting a new one.</summary>
        private static string GetPath(string connectionID) =>
            Path.Combine(Folder, SafeFileName(connectionID.ToUpperInvariant()) + ".bin");

        /// <summary>
        /// A connection id is a display string and can contain anything, so the file is named from a
        /// sanitised prefix for legibility plus a hash of the original for uniqueness.  Two connections
        /// whose sanitised names collide must not share a baseline.
        /// </summary>
        private static string SafeFileName(string connectionID)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(connectionID.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            if (cleaned.Length > 60) cleaned = cleaned[..60];

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(connectionID));
            return cleaned + "_" + Convert.ToHexString(hash, 0, 6);
        }
    }
}
