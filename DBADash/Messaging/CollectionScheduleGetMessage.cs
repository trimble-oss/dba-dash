using DBADashService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Returns an instance's schedule configuration as the service holds it, for the GUI's schedule dialog: the service
    /// level overrides, the instance's own overrides and any temporary schedules.  The repository only has the effective
    /// schedule, which can't tell a service level setting from a default, and has nothing for collections that are
    /// disabled.  Read-only, but gated like the changes it's used to make (<see cref="CollectionConfig.AllowScheduleChanges"/>).
    /// </summary>
    public class CollectionScheduleGetMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public const string LevelService = "Service";
        public const string LevelInstance = "Instance";
        public const string LevelTemporary = "Temporary";

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.IsScheduleChangesAllowed)
            {
                throw new Exception("Changing collection schedules is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            List<DBADashSource> sources;
            lock (cfg.SourceConnections)
            {
                sources = cfg.SourceConnections.Where(s => s.SourceConnection.Type == DBADashConnection.ConnectionType.SQL).ToList();
            }
            var source = sources.FirstOrDefault(s => string.Equals(s.ConnectionID, ConnectionID, StringComparison.InvariantCultureIgnoreCase))
                         ?? throw new ArgumentException($"Instance {ConnectionID} isn't collected by this service.");

            // Instance overrides are saved to the config file, so they can only be made for instances defined there (not
            // ones discovered at runtime).  Loaded from disk for the same reason the changes are applied to it, under the
            // lock schedule changes write it with so it's never read half written.
            CollectionConfig fileConfig;
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                fileConfig = BasicConfig.Load<CollectionConfig>();
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }
            var inConfig = fileConfig.SourceConnections
                .Where(s => !string.IsNullOrEmpty(s.ConnectionID))
                .Select(s => s.ConnectionID)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            // Every instance the service collects, so the GUI can show which would be affected by a change to the
            // service schedule - those without their own schedule for the collection.
            var serviceInstances = sources
                .Where(s => !string.IsNullOrEmpty(s.ConnectionID))
                .Select(s => new ServiceInstanceInfo
                {
                    ConnectionID = s.ConnectionID,
                    IsInConfig = inConfig.Contains(s.ConnectionID),
                    InstanceSchedules = s.CollectionSchedules ?? new CollectionSchedules(),
                    TemporarySchedules = TemporaryScheduleStore.Current.Get(s.ConnectionID) ?? new Dictionary<CollectionType, TemporarySchedule>(),
                    PausedUntilUtc = CollectionPauseStore.Current.IsPaused(s.ConnectionID, out var pausedUntil) ? pausedUntil : null
                });

            return BuildDataSet(cfg.CollectionSchedules, source.CollectionSchedules,
                TemporaryScheduleStore.Current.Get(ConnectionID), inConfig.Contains(ConnectionID), serviceInstances);
        }

        public static DataSet BuildDataSet(CollectionSchedules serviceSchedules, CollectionSchedules instanceSchedules,
            Dictionary<CollectionType, TemporarySchedule> temporarySchedules, bool isInConfig,
            IEnumerable<ServiceInstanceInfo> serviceInstances = null)
        {
            var dt = new DataTable("Schedules");
            dt.Columns.Add("Level", typeof(string));
            dt.Columns.Add("CollectionType", typeof(string));
            dt.Columns.Add("Schedule", typeof(string));
            dt.Columns.Add("RunOnServiceStart", typeof(bool));
            dt.Columns.Add("UntilUtc", typeof(DateTime));

            foreach (var (type, schedule) in serviceSchedules ?? new CollectionSchedules())
            {
                dt.Rows.Add(LevelService, type.ToString(), schedule.Schedule ?? string.Empty, schedule.RunOnServiceStart, DBNull.Value);
            }
            foreach (var (type, schedule) in instanceSchedules ?? new CollectionSchedules())
            {
                dt.Rows.Add(LevelInstance, type.ToString(), schedule.Schedule ?? string.Empty, schedule.RunOnServiceStart, DBNull.Value);
            }
            foreach (var (type, schedule) in temporarySchedules ?? new Dictionary<CollectionType, TemporarySchedule>())
            {
                dt.Rows.Add(LevelTemporary, type.ToString(), schedule.Schedule ?? string.Empty, schedule.RunOnServiceStart, schedule.UntilUtc);
            }

            var info = new DataTable("Instance");
            info.Columns.Add("IsInConfig", typeof(bool));
            info.Rows.Add(isInConfig);

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(info);
            if (serviceInstances != null)
            {
                AddServiceInstances(ds, serviceInstances);
            }
            // The reply is sent as XML, which by default writes a DateTime as local time and reads it back in the
            // receiver's time zone.  Keep the UTC values as they are, as for collections written to a folder or S3.
            DataSetSerialization.SetDateTimeKind(ds);
            return ds;
        }

        private static void AddServiceInstances(DataSet ds, IEnumerable<ServiceInstanceInfo> serviceInstances)
        {
            var instances = new DataTable("ServiceInstances");
            instances.Columns.Add("ConnectionID", typeof(string));
            instances.Columns.Add("IsInConfig", typeof(bool));
            instances.Columns.Add("PausedUntilUtc", typeof(DateTime));

            var schedules = new DataTable("ServiceInstanceSchedules");
            schedules.Columns.Add("ConnectionID", typeof(string));
            schedules.Columns.Add("Level", typeof(string));
            schedules.Columns.Add("CollectionType", typeof(string));
            schedules.Columns.Add("Schedule", typeof(string));
            schedules.Columns.Add("RunOnServiceStart", typeof(bool));
            schedules.Columns.Add("UntilUtc", typeof(DateTime));

            foreach (var instance in serviceInstances)
            {
                instances.Rows.Add(instance.ConnectionID, instance.IsInConfig, (object)instance.PausedUntilUtc ?? DBNull.Value);
                foreach (var (type, schedule) in instance.InstanceSchedules ?? new CollectionSchedules())
                {
                    schedules.Rows.Add(instance.ConnectionID, LevelInstance, type.ToString(), schedule.Schedule ?? string.Empty, schedule.RunOnServiceStart, DBNull.Value);
                }
                foreach (var (type, schedule) in instance.TemporarySchedules ?? new Dictionary<CollectionType, TemporarySchedule>())
                {
                    schedules.Rows.Add(instance.ConnectionID, LevelTemporary, type.ToString(), schedule.Schedule ?? string.Empty, schedule.RunOnServiceStart, schedule.UntilUtc);
                }
            }
            ds.Tables.Add(instances);
            ds.Tables.Add(schedules);
        }

        /// <summary>
        /// The instances the service collects, from a DataSet built by <see cref="BuildDataSet"/>.  Null if the service
        /// didn't send them (an older service).
        /// </summary>
        public static List<ServiceInstanceInfo> ReadServiceInstances(DataSet ds)
        {
            var instancesTable = ds.Tables["ServiceInstances"];
            var schedulesTable = ds.Tables["ServiceInstanceSchedules"];
            if (instancesTable == null || schedulesTable == null) return null;

            var instances = instancesTable.Rows.Cast<DataRow>()
                .Select(row => new ServiceInstanceInfo
                {
                    ConnectionID = (string)row["ConnectionID"],
                    IsInConfig = (bool)row["IsInConfig"],
                    PausedUntilUtc = row["PausedUntilUtc"] == DBNull.Value
                        ? null
                        : DateTime.SpecifyKind((DateTime)row["PausedUntilUtc"], DateTimeKind.Utc)
                })
                .ToDictionary(i => i.ConnectionID, StringComparer.InvariantCultureIgnoreCase);

            foreach (DataRow row in schedulesTable.Rows)
            {
                if (!instances.TryGetValue((string)row["ConnectionID"], out var instance) ||
                    !Enum.TryParse<CollectionType>((string)row["CollectionType"], out var type)) continue;
                var schedule = (string)row["Schedule"];
                var runOnServiceStart = (bool)row["RunOnServiceStart"];
                if ((string)row["Level"] == LevelTemporary)
                {
                    instance.TemporarySchedules[type] = new TemporarySchedule
                    {
                        Schedule = schedule,
                        RunOnServiceStart = runOnServiceStart,
                        UntilUtc = DateTime.SpecifyKind((DateTime)row["UntilUtc"], DateTimeKind.Utc)
                    };
                }
                else
                {
                    instance.InstanceSchedules[type] = new CollectionSchedule { Schedule = schedule, RunOnServiceStart = runOnServiceStart };
                }
            }
            return instances.Values.ToList();
        }

        /// <summary>The schedules at <paramref name="level"/> from a DataSet built by <see cref="BuildDataSet"/>.  Unknown collection types are skipped.</summary>
        public static CollectionSchedules ReadSchedules(DataSet ds, string level)
        {
            var schedules = new CollectionSchedules();
            foreach (DataRow row in ds.Tables["Schedules"]!.Rows)
            {
                if ((string)row["Level"] != level || !Enum.TryParse<CollectionType>((string)row["CollectionType"], out var type)) continue;
                schedules[type] = new CollectionSchedule { Schedule = (string)row["Schedule"], RunOnServiceStart = (bool)row["RunOnServiceStart"] };
            }
            return schedules;
        }

        /// <summary>The temporary schedules from a DataSet built by <see cref="BuildDataSet"/>.</summary>
        public static Dictionary<CollectionType, TemporarySchedule> ReadTemporarySchedules(DataSet ds)
        {
            var schedules = new Dictionary<CollectionType, TemporarySchedule>();
            foreach (DataRow row in ds.Tables["Schedules"]!.Rows)
            {
                if ((string)row["Level"] != LevelTemporary || !Enum.TryParse<CollectionType>((string)row["CollectionType"], out var type)) continue;
                schedules[type] = new TemporarySchedule
                {
                    Schedule = (string)row["Schedule"],
                    RunOnServiceStart = (bool)row["RunOnServiceStart"],
                    UntilUtc = DateTime.SpecifyKind((DateTime)row["UntilUtc"], DateTimeKind.Utc)
                };
            }
            return schedules;
        }
    }

    /// <summary>
    /// An instance the service collects, with what decides whether a change to the service schedule affects it.  Returned
    /// by <see cref="CollectionScheduleGetMessage"/>.
    /// </summary>
    public class ServiceInstanceInfo
    {
        public string ConnectionID { get; set; }

        /// <summary>Defined in ServiceConfig.json rather than discovered at runtime (e.g. an Azure DB found via the master connection).</summary>
        public bool IsInConfig { get; set; }

        /// <summary>The instance's own schedules.  A collection listed here doesn't follow the service schedule.</summary>
        public CollectionSchedules InstanceSchedules { get; set; } = new();

        /// <summary>Temporary schedules in effect for the instance.</summary>
        public Dictionary<CollectionType, TemporarySchedule> TemporarySchedules { get; set; } = new();

        /// <summary>When the instance's paused collections resume (UTC), or null if they aren't paused.</summary>
        public DateTime? PausedUntilUtc { get; set; }

        /// <summary>
        /// True if <paramref name="type"/> runs on the service schedule for this instance, so a change to the service
        /// schedule changes it.  A temporary schedule only delays that until it ends.
        /// </summary>
        public bool FollowsServiceSchedule(CollectionType type) => InstanceSchedules == null || !InstanceSchedules.ContainsKey(type);
    }
}
