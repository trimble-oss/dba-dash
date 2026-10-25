using DBADashService;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Changes collection schedules on the service from the GUI - enable, disable or reschedule a collection, either for
    /// one instance (an instance level override) or for every instance the service collects (the service level schedule).
    /// Must be explicitly enabled on the service (<see cref="CollectionConfig.AllowScheduleChanges"/>).
    ///
    /// <para>The change is written to ServiceConfig.json, so it survives a restart and shows in the config tool, and is
    /// then applied to the running service via <see cref="CollectionConfig.RescheduleCollectionsAsync"/>.  The file is
    /// updated from a fresh copy read from disk rather than by saving the service's in-memory config: the service adds
    /// sources it discovers at runtime (Azure DBs, read replicas) to its in-memory config and those must not be
    /// persisted.</para>
    /// </summary>
    public class CollectionScheduleUpdateMessage : MessageBase
    {
        public enum ScheduleScopes
        {
            /// <summary>Override the schedule for a single instance (<see cref="ConnectionID"/>).</summary>
            Instance,

            /// <summary>Change the service level schedule, inherited by every instance without an override.</summary>
            Service
        }

        public ScheduleScopes Scope { get; set; }

        /// <summary>The instance to change when <see cref="Scope"/> is <see cref="ScheduleScopes.Instance"/>.</summary>
        public string ConnectionID { get; set; }

        /// <summary>
        /// The new schedule for each collection.  A null schedule removes the override so the collection inherits again:
        /// the service level schedule for an instance, the built-in default for the service.  An empty
        /// <see cref="CollectionSchedule.Schedule"/> disables the collection.
        /// </summary>
        public Dictionary<CollectionType, CollectionSchedule> Changes { get; set; } = new();

        /// <summary>
        /// True for a temporary change to an instance's schedule - e.g. enabling a collection that's normally off while
        /// investigating an issue.  It's kept in <see cref="TemporaryScheduleStore"/> rather than ServiceConfig.json and
        /// reverts by itself at <see cref="TemporaryUntilUtc"/>.  A null schedule in <see cref="Changes"/> ends the
        /// temporary schedule for that collection now.
        /// </summary>
        public bool Temporary { get; set; }

        /// <summary>When a temporary schedule reverts (UTC).  Required when <see cref="Temporary"/> sets a schedule.</summary>
        public DateTime? TemporaryUntilUtc { get; set; }

        /// <summary>Longest a temporary schedule can last.</summary>
        public static readonly TimeSpan MaxTemporary = TimeSpan.FromDays(31);

        /// <summary>
        /// Serializes access to ServiceConfig.json - two requests mustn't both read the file and then both write it, and a
        /// read mustn't see it half written (<see cref="CollectionScheduleGetMessage"/>).
        /// </summary>
        internal static readonly SemaphoreSlim ConfigFileLock = new(1, 1);

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.IsScheduleChangesAllowed)
            {
                throw new Exception("Changing collection schedules is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            Validate(DateTime.UtcNow);

            var derivedConnectionIDs = new List<string>();
            if (Temporary)
            {
                // Any instance the service collects, including those it discovered at runtime - nothing is written to the config
                bool isKnown;
                lock (cfg.SourceConnections)
                {
                    isKnown = cfg.SourceConnections.Exists(s => s.SourceConnection.Type == DBADashConnection.ConnectionType.SQL &&
                                                                string.Equals(s.ConnectionID, ConnectionID, StringComparison.InvariantCultureIgnoreCase));
                }
                if (!isKnown)
                {
                    throw new ArgumentException($"Instance {ConnectionID} isn't collected by this service.");
                }
                TemporaryScheduleStore.Current.Set(ConnectionID, GetTemporaryChanges());
            }
            else
            {
                await ConfigFileLock.WaitAsync(cancellationToken);
                try
                {
                    var fileConfig = BasicConfig.Load<CollectionConfig>();
                    if (Scope == ScheduleScopes.Instance)
                    {
                        // Telling configured sources from derived ones relies on every saved source having a ConnectionID
                        ConnectionChanges.ThrowIfMissingConnectionIDs(fileConfig);
                    }
                    ApplyTo(fileConfig);
                    fileConfig.Save();
                    // The running service's sources are added to by the Azure DB and read replica scans, so they're
                    // only changed under the lock those take
                    lock (cfg.SourceConnections)
                    {
                        ApplyTo(cfg);
                        if (Scope == ScheduleScopes.Instance)
                        {
                            derivedConnectionIDs = ApplyToDerivedSources(cfg.SourceConnections, fileConfig);
                        }
                    }
                }
                finally
                {
                    ConfigFileLock.Release();
                }
            }

            Log.Information("Collection schedule changed{temporary} via message {id} for {scope} {connectionID}: {changes}",
                Temporary ? $" temporarily until {TemporaryUntilUtc:u}" : string.Empty, Id, Scope,
                Scope == ScheduleScopes.Instance ? ConnectionID : cfg.ServiceName, DescribeChanges());

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = Temporary ? "Temporary schedule saved.  Rescheduling collections..." : "Schedule saved to ServiceConfig.json.  Rescheduling collections..."
            });

            if (cfg.RescheduleCollectionsAsync != null)
            {
                await cfg.RescheduleCollectionsAsync();
            }

            var failures = await ReportScheduleInfoAsync(cfg, derivedConnectionIDs, cancellationToken);
            var warning = MessageWarnings.ForScheduleInfoFailures(failures, "Collection Dates won't show the new schedule");
            return MessageWarnings.ToDataSet(warning == null ? null : new[] { warning });
        }

        /// <summary>
        /// Gives the sources the service discovered from the changed instance its new schedule, as they would have at the
        /// next service start: databases found through an Azure master connection and read replicas found from their
        /// primary are copies of it, taken when they were discovered.  Returns their ConnectionIDs.  Caller holds the
        /// lock on <paramref name="sources"/>.
        /// </summary>
        private List<string> ApplyToDerivedSources(List<DBADashSource> sources, CollectionConfig fileConfig)
        {
            var parent = sources.FirstOrDefault(s => string.Equals(s.ConnectionID, ConnectionID, StringComparison.InvariantCultureIgnoreCase));
            if (parent == null) return new List<string>();
            // Caller holds the lock on sources
            var configured = ConnectionChanges.GetConfiguredConnectionIDs(fileConfig);
            var derived = GetDerivedSources(sources, ConnectionID, configured);
            foreach (var source in derived)
            {
                source.CollectionSchedules = parent.CollectionSchedules == null ? null : CollectionSchedules.Combine(new CollectionSchedules(), parent.CollectionSchedules);
            }
            return derived.Select(s => s.ConnectionID).ToList();
        }

        /// <summary>
        /// The sources discovered at runtime from <paramref name="parentConnectionID"/> (so not in the config,
        /// <paramref name="configuredConnectionIDs"/>): the databases found through an Azure master connection
        /// (server|master -> server|db, and their read replicas), and read replicas found from their primary
        /// (ConnectionID|ReadOnly).  The read replica of a database that's in the config itself is derived from that
        /// database rather than master.
        /// </summary>
        public static List<DBADashSource> GetDerivedSources(IEnumerable<DBADashSource> sources, string parentConnectionID,
            ISet<string> configuredConnectionIDs)
        {
            const string masterSuffix = "|master";
            const string replicaSuffix = "|ReadOnly";
            var serverPrefix = parentConnectionID.EndsWith(masterSuffix, StringComparison.InvariantCultureIgnoreCase)
                ? parentConnectionID[..^"master".Length]
                : null;
            var replicaConnectionID = parentConnectionID + replicaSuffix;

            bool IsReplicaOfConfiguredDatabase(string connectionID) =>
                connectionID.EndsWith(replicaSuffix, StringComparison.InvariantCultureIgnoreCase)
                && configuredConnectionIDs.Contains(connectionID[..^replicaSuffix.Length]);

            return sources
                .Where(s => !string.IsNullOrEmpty(s.ConnectionID)
                            && !configuredConnectionIDs.Contains(s.ConnectionID)
                            && !string.Equals(s.ConnectionID, parentConnectionID, StringComparison.InvariantCultureIgnoreCase))
                .Where(s => (serverPrefix != null && s.ConnectionID.StartsWith(serverPrefix, StringComparison.InvariantCultureIgnoreCase)
                             && !IsReplicaOfConfiguredDatabase(s.ConnectionID))
                            || string.Equals(s.ConnectionID, replicaConnectionID, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
        }

        /// <summary>Throws if the request is malformed.  Doesn't depend on the service config, so it runs before anything is changed.</summary>
        public void Validate(DateTime utcNow)
        {
            if (Changes is not { Count: > 0 })
            {
                throw new ArgumentException("No schedule changes were requested.");
            }

            if (Scope == ScheduleScopes.Instance && string.IsNullOrEmpty(ConnectionID))
            {
                throw new ArgumentException("ConnectionID is required to change an instance schedule.");
            }

            if (Temporary)
            {
                if (Scope != ScheduleScopes.Instance)
                {
                    throw new ArgumentException("Temporary schedules apply to an instance, not the service.");
                }
                if (Changes.Values.Any(s => s != null))
                {
                    var until = TemporaryUntilUtc.HasValue ? DateTime.SpecifyKind(TemporaryUntilUtc.Value, DateTimeKind.Utc) : (DateTime?)null;
                    if (until == null || until <= utcNow)
                    {
                        throw new ArgumentException("A temporary schedule must end in the future.");
                    }
                    if (until - utcNow > MaxTemporary)
                    {
                        throw new ArgumentException($"A temporary schedule can last up to {MaxTemporary.TotalDays} days.");
                    }
                }
            }

            foreach (var (type, schedule) in Changes)
            {
                if (!IsConfigurable(type))
                {
                    throw new ArgumentException($"The schedule for {type} can't be changed.");
                }

                if (schedule != null && !IsValidSchedule(schedule.Schedule))
                {
                    throw new ArgumentException($"Invalid schedule for {type}: '{schedule.Schedule}'.  Use a cron expression or a time in seconds.");
                }
            }
        }

        /// <summary>
        /// True for a collection type that has a schedule the user can change.  ScheduleInfo is internal bookkeeping (it
        /// reports the schedule itself), as in the config tool.
        /// </summary>
        public static bool IsConfigurable(CollectionType type) =>
            type != CollectionType.ScheduleInfo && CollectionSchedules.DefaultSchedules.ContainsKey(type);

        /// <summary>
        /// Empty (disabled), a positive number of seconds, or a valid Quartz cron expression that fires at some point.
        /// Valid syntax isn't enough: Quartz refuses to schedule a trigger that can never fire (e.g. 30 February, or a
        /// year that has passed), which would leave collections unscheduled and stop the service starting.
        /// </summary>
        public static bool IsValidSchedule(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule)) return true;
            if (int.TryParse(schedule, out var seconds)) return seconds > 0;
            if (!CronExpression.TryParse(schedule.Trim(), out _)) return false;
            return new CronExpression(schedule.Trim()).GetNextValidTimeAfter(DateTimeOffset.UtcNow) != null;
        }

        /// <summary>
        /// Applies the changes to <paramref name="config"/>.  The schedules are replaced with a new collection rather than
        /// modified in place, as the running service reads them from other threads.  An override list left empty is set
        /// to null so the config file doesn't keep an empty section.
        /// </summary>
        public void ApplyTo(CollectionConfig config)
        {
            switch (Scope)
            {
                case ScheduleScopes.Service:
                    config.CollectionSchedules = Apply(config.CollectionSchedules);
                    break;

                case ScheduleScopes.Instance:
                    var source = config.SourceConnections.FirstOrDefault(s =>
                                     string.Equals(s.ConnectionID, ConnectionID, StringComparison.InvariantCultureIgnoreCase))
                                 ?? throw new ArgumentException($"Instance {ConnectionID} isn't defined in the service config.  It might have been discovered automatically (e.g. an Azure DB found via the master connection).  Change the schedule at service level instead, or add the instance to the service config.");
                    if (source.SourceConnection.Type != DBADashConnection.ConnectionType.SQL)
                    {
                        throw new ArgumentException($"{ConnectionID} isn't a SQL instance, so it has no collection schedule.");
                    }
                    source.CollectionSchedules = Apply(source.CollectionSchedules);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Scope), Scope, null);
            }
        }

        private CollectionSchedules Apply(CollectionSchedules current)
        {
            var updated = new CollectionSchedules();
            if (current != null)
            {
                foreach (var (type, schedule) in current)
                {
                    updated[type] = schedule;
                }
            }

            foreach (var (type, schedule) in Changes)
            {
                if (schedule == null)
                {
                    updated.Remove(type);
                }
                else
                {
                    updated[type] = new CollectionSchedule
                    {
                        Schedule = schedule.Schedule?.Trim() ?? string.Empty,
                        RunOnServiceStart = schedule.RunOnServiceStart
                    };
                }
            }

            return updated.Count == 0 ? null : updated;
        }

        /// <summary>
        /// The changes needed to go from overrides <paramref name="before"/> to <paramref name="after"/> (as edited in the
        /// schedule dialog), against <paramref name="baseSchedule"/> - the schedule inherited where there's no override.
        /// For a permanent change: the overrides that were added or changed, and null for those removed (so they inherit
        /// again).  For a temporary change there's nothing to remove - a temporary schedule can only set one - so it's the
        /// collections whose schedule ends up different, with the schedule they end up with.
        /// </summary>
        public static Dictionary<CollectionType, CollectionSchedule> GetChanges(CollectionSchedules baseSchedule,
            CollectionSchedules before, CollectionSchedules after, bool temporary)
        {
            before ??= new CollectionSchedules();
            after ??= new CollectionSchedules();
            var changes = new Dictionary<CollectionType, CollectionSchedule>();
            if (temporary)
            {
                var effectiveBefore = CollectionSchedules.Combine(baseSchedule, before);
                foreach (var (type, schedule) in CollectionSchedules.Combine(baseSchedule, after))
                {
                    if (!effectiveBefore.TryGetValue(type, out var was) || !IsSame(was, schedule))
                    {
                        changes[type] = schedule;
                    }
                }
                return changes;
            }

            foreach (var (type, schedule) in after)
            {
                if (!before.TryGetValue(type, out var was) || !IsSame(was, schedule))
                {
                    changes[type] = schedule;
                }
            }
            foreach (var type in before.Keys.Where(type => !after.ContainsKey(type)))
            {
                changes[type] = null;
            }
            return changes;
        }

        private static bool IsSame(CollectionSchedule a, CollectionSchedule b) =>
            a.RunOnServiceStart == b.RunOnServiceStart &&
            string.Equals(a.Schedule?.Trim() ?? string.Empty, b.Schedule?.Trim() ?? string.Empty, StringComparison.Ordinal);

        /// <summary>The changes as temporary schedules, ending at <see cref="TemporaryUntilUtc"/> (null to end one now).</summary>
        public Dictionary<CollectionType, TemporarySchedule> GetTemporaryChanges() =>
            Changes.ToDictionary(c => c.Key, c => c.Value == null
                ? null
                : new TemporarySchedule
                {
                    Schedule = c.Value.Schedule?.Trim() ?? string.Empty,
                    RunOnServiceStart = c.Value.RunOnServiceStart,
                    UntilUtc = DateTime.SpecifyKind(TemporaryUntilUtc!.Value, DateTimeKind.Utc)
                });

        private string DescribeChanges() =>
            string.Join(", ", Changes.Select(c => c.Value == null
                ? $"{c.Key}={(Temporary ? "End" : "Inherit")}"
                : $"{c.Key}='{c.Value.Schedule}'{(c.Value.RunOnServiceStart ? " (run on service start)" : string.Empty)}"));

        /// <summary>
        /// Reports the new schedule for the affected instances straight away, so the GUI shows the change without waiting
        /// for a service restart (ScheduleInfo is otherwise only reported at service start).  A failure for one instance
        /// doesn't fail the request - the schedule change itself has already been made - but is returned so the user can
        /// be told.
        /// </summary>
        private async Task<List<string>> ReportScheduleInfoAsync(CollectionConfig cfg, IReadOnlyCollection<string> derivedConnectionIDs,
            CancellationToken cancellationToken)
        {
            List<DBADashSource> sources;
            lock (cfg.SourceConnections)
            {
                sources = cfg.SourceConnections
                    .Where(s => s.SourceConnection.Type == DBADashConnection.ConnectionType.SQL)
                    .Where(s => Scope == ScheduleScopes.Service
                        ? IsAffectedByServiceChange(s)
                        : string.Equals(s.ConnectionID, ConnectionID, StringComparison.InvariantCultureIgnoreCase) ||
                          derivedConnectionIDs.Contains(s.ConnectionID, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            return await ScheduleInfoReporter.ReportAsync(sources, cfg, cancellationToken);
        }

        /// <summary>
        /// True if a change to the service schedule changes <paramref name="source"/>'s schedule - it follows the service
        /// schedule (has no schedule of its own) for at least one of the changed collections.
        /// </summary>
        public bool IsAffectedByServiceChange(DBADashSource source) =>
            source.CollectionSchedules == null || Changes.Keys.Any(type => !source.CollectionSchedules.ContainsKey(type));
    }
}
