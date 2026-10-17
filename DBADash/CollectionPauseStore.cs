using System;
using System.Collections.Generic;
using System.IO;

namespace DBADash
{
    /// <summary>
    /// Instances whose scheduled collections are paused, and until when - set from the GUI via
    /// <see cref="Messaging.CollectionPauseMessage"/>.
    ///
    /// <para>Runtime state rather than config, so it's kept in its own file beside the service like
    /// <see cref="Deadlocks.DeadlockCursorStore"/> rather than in ServiceConfig.json: a pause is a temporary
    /// operational state that ends by itself, it shouldn't be backed up or shown in the config tool as a setting, and
    /// the config tool saving a copy of the config it loaded earlier mustn't be able to undo it.  It's still on disk so
    /// a pause survives a service restart - otherwise collections would resume while the repository still mutes the
    /// instance's alerts.</para>
    ///
    /// <para>Keyed by ConnectionID, so it covers instances the service discovers at runtime (Azure DBs, read
    /// replicas) as well as those in the config.  An expired pause is ignored rather than cleared, so collection
    /// resumes with nothing having to act.  Losing or corrupting the file just means nothing is paused - collections
    /// run as normal - so nothing here fails a collection.</para>
    /// </summary>
    public class CollectionPauseStore
    {
        /// <summary>The service's store, in a file beside the binary.</summary>
        public static readonly CollectionPauseStore Current = new(Path.Combine(AppContext.BaseDirectory, "CollectionPauses.json"));

        private readonly string filePath;
        private readonly object updateLock = new();

        /// <summary>ConnectionID -> pause end (UTC).  Replaced, never modified, so it can be read without a lock.</summary>
        private volatile Dictionary<string, DateTime> pauses;

        public CollectionPauseStore(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>True if scheduled collections for <paramref name="connectionID"/> are paused, with the time (UTC) they resume.</summary>
        public bool IsPaused(string connectionID, out DateTime pausedUntilUtc)
        {
            pausedUntilUtc = default;
            if (string.IsNullOrEmpty(connectionID)) return false;
            if (!GetPauses().TryGetValue(connectionID, out var until) || until <= DateTime.UtcNow) return false;
            pausedUntilUtc = until;
            return true;
        }

        /// <summary>
        /// Replaces the pauses with the result of <paramref name="change"/>, applied to a copy of the current ones, and
        /// writes the file.  Throws if the file can't be written, so the caller can report that the pause didn't take.
        /// </summary>
        public void Update(Func<Dictionary<string, DateTime>, Dictionary<string, DateTime>> change)
        {
            lock (updateLock)
            {
                var updated = new Dictionary<string, DateTime>(change(new Dictionary<string, DateTime>(GetPauses(), StringComparer.OrdinalIgnoreCase)) ?? new(),
                    StringComparer.OrdinalIgnoreCase);
                Save(updated);
                pauses = updated;
            }
        }

        private Dictionary<string, DateTime> GetPauses()
        {
            var current = pauses;
            if (current != null) return current;
            lock (updateLock)
            {
                return pauses ??= Load();
            }
        }

        private Dictionary<string, DateTime> Load()
        {
            var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, until) in JsonStateFile.Read<Dictionary<string, DateTime>>(filePath, "collection pauses") ?? new())
            {
                result[id] = DateTime.SpecifyKind(until.Kind == DateTimeKind.Local ? until.ToUniversalTime() : until, DateTimeKind.Utc);
            }
            return result;
        }

        private void Save(Dictionary<string, DateTime> updated) => JsonStateFile.Write(filePath, updated, updated.Count == 0);
    }
}
