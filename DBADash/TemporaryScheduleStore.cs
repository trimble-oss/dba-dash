using DBADashService;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBADash
{
    /// <summary>A collection schedule that applies to an instance until a given time, then reverts to its normal schedule.</summary>
    public class TemporarySchedule
    {
        public string Schedule { get; set; }

        public bool RunOnServiceStart { get; set; }

        /// <summary>When the schedule reverts (UTC).</summary>
        public DateTime UntilUtc { get; set; }

        public bool IsActive(DateTime utcNow) => DateTime.SpecifyKind(UntilUtc, DateTimeKind.Utc) > utcNow;
    }

    /// <summary>
    /// Temporary schedule changes per instance - e.g. enabling a collection that's normally off while investigating an
    /// issue - set from the GUI via <see cref="Messaging.CollectionScheduleUpdateMessage"/>.  They take precedence over
    /// the instance and service schedules until they expire, and the service reschedules when one does.
    ///
    /// <para>Runtime state rather than config, like <see cref="CollectionPauseStore"/>: kept in its own file beside the
    /// service rather than ServiceConfig.json, so it's never mistaken for (or saved over by) the permanent schedule, and
    /// it survives a restart so a service restart doesn't end an investigation early.  A missing or corrupt file just
    /// means no temporary schedules apply.</para>
    /// </summary>
    public class TemporaryScheduleStore
    {
        /// <summary>The service's store, in a file beside the binary.</summary>
        public static readonly TemporaryScheduleStore Current = new(Path.Combine(AppContext.BaseDirectory, "TemporarySchedules.json"));

        private readonly string filePath;
        private readonly object updateLock = new();

        /// <summary>ConnectionID -> collection -> schedule.  Replaced, never modified, so it can be read without a lock.</summary>
        private volatile Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> schedules;

        public TemporaryScheduleStore(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>The temporary schedules in effect for <paramref name="connectionID"/>, or null if there are none.</summary>
        public Dictionary<CollectionType, TemporarySchedule> Get(string connectionID)
        {
            if (string.IsNullOrEmpty(connectionID) || !GetAll().TryGetValue(connectionID, out var forInstance)) return null;
            var now = DateTime.UtcNow;
            var active = forInstance.Where(s => s.Value.IsActive(now)).ToDictionary(s => s.Key, s => s.Value);
            return active.Count == 0 ? null : active;
        }

        /// <summary>The temporary schedules in effect for <paramref name="connectionID"/> as schedule overrides, or null.</summary>
        public CollectionSchedules GetOverrides(string connectionID)
        {
            var active = Get(connectionID);
            if (active == null) return null;
            var overrides = new CollectionSchedules();
            foreach (var (type, schedule) in active)
            {
                overrides[type] = new CollectionSchedule { Schedule = schedule.Schedule, RunOnServiceStart = schedule.RunOnServiceStart };
            }
            return overrides;
        }

        /// <summary>
        /// Sets (or with a null schedule, removes) temporary schedules for <paramref name="connectionID"/> and writes the
        /// file.  Throws if the file can't be written.  Expired schedules are kept for <see cref="RemoveExpired"/>, so
        /// the service still reports the instances they belonged to as reverted.
        /// </summary>
        public void Set(string connectionID, IReadOnlyDictionary<CollectionType, TemporarySchedule> changes)
        {
            lock (updateLock)
            {
                var updated = Copy(GetAll());
                updated.TryGetValue(connectionID, out var forInstance);
                forInstance ??= new Dictionary<CollectionType, TemporarySchedule>();
                foreach (var (type, schedule) in changes)
                {
                    if (schedule == null)
                    {
                        forInstance.Remove(type);
                    }
                    else
                    {
                        forInstance[type] = schedule;
                    }
                }
                if (forInstance.Count == 0)
                {
                    updated.Remove(connectionID);
                }
                else
                {
                    updated[connectionID] = forInstance;
                }
                Save(updated);
                schedules = updated;
            }
        }

        /// <summary>Removes every temporary schedule for <paramref name="connectionIDs"/> - e.g. connections that are no longer collected.</summary>
        public void Remove(IReadOnlyCollection<string> connectionIDs)
        {
            lock (updateLock)
            {
                var updated = Copy(GetAll());
                var removed = false;
                foreach (var id in connectionIDs)
                {
                    removed |= updated.Remove(id);
                }
                if (!removed) return;
                Save(updated);
                schedules = updated;
            }
        }

        /// <summary>
        /// Drops schedules that have expired, returning the ConnectionIDs they belonged to so the service can reschedule
        /// those instances and report their schedule.  Empty when nothing has expired.
        /// </summary>
        public List<string> RemoveExpired()
        {
            lock (updateLock)
            {
                var now = DateTime.UtcNow;
                var current = GetAll();
                var expired = current.Where(i => i.Value.Values.Any(s => !s.IsActive(now))).Select(i => i.Key).ToList();
                if (expired.Count == 0) return expired;
                var updated = CopyActive(current, now);
                try
                {
                    Save(updated);
                }
                catch (Exception ex)
                {
                    // Expired schedules are ignored anyway - the file just catches up on the next change
                    Log.Warning(ex, "Error removing expired temporary schedules from {file}", filePath);
                }
                schedules = updated;
                return expired;
            }
        }

        private static Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> CopyActive(
            Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> source, DateTime utcNow) =>
            Copy(source, s => s.IsActive(utcNow));

        private static Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> Copy(
            Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> source, Func<TemporarySchedule, bool> keep = null)
        {
            var copy = new Dictionary<string, Dictionary<CollectionType, TemporarySchedule>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, forInstance) in source)
            {
                var kept = forInstance.Where(s => keep == null || keep(s.Value)).ToDictionary(s => s.Key, s => s.Value);
                if (kept.Count > 0) copy[id] = kept;
            }
            return copy;
        }

        private Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> GetAll()
        {
            var current = schedules;
            if (current != null) return current;
            lock (updateLock)
            {
                return schedules ??= Load();
            }
        }

        private Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> Load()
        {
            var result = new Dictionary<string, Dictionary<CollectionType, TemporarySchedule>>(StringComparer.OrdinalIgnoreCase);
            var loaded = JsonStateFile.Read<Dictionary<string, Dictionary<CollectionType, TemporarySchedule>>>(filePath, "temporary schedules");
            foreach (var (id, forInstance) in loaded ?? new())
            {
                foreach (var schedule in forInstance.Values)
                {
                    schedule.UntilUtc = schedule.UntilUtc.Kind == DateTimeKind.Local
                        ? schedule.UntilUtc.ToUniversalTime()
                        : DateTime.SpecifyKind(schedule.UntilUtc, DateTimeKind.Utc);
                }
                result[id] = forInstance;
            }
            return result;
        }

        private void Save(Dictionary<string, Dictionary<CollectionType, TemporarySchedule>> updated) =>
            JsonStateFile.Write(filePath, updated, updated.Count == 0);
    }
}
