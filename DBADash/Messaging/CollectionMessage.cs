using DBADashService;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    // Concrete message type for triggering a collection
    public class CollectionMessage : MessageBase
    {
        public List<string> CollectionTypes { get; set; }

        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }

        /// <summary>
        /// When true, collection types whose schedule is disabled (no cron expression) for the target
        /// instance are run anyway rather than skipped.  Set when the user explicitly confirms they want
        /// to run an unscheduled collection after being warned (see <see cref="CollectionScheduleDisabledException"/>).
        /// </summary>
        public bool IgnoreDisabledSchedule { get; set; }

        /// <summary>
        /// Collection type names from the request that don't match any standard or custom collection for the
        /// target instance (e.g. a custom collection that has since been removed).  Populated by
        /// <see cref="CollectAsync"/>.  When some valid collections still ran these are surfaced as a warning
        /// by the caller rather than failing the whole request; when nothing valid was left to run an
        /// <see cref="UnknownCollectionTypeException"/> is thrown instead.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public List<string> UnknownCollectionTypes { get; private set; }

        public CollectionMessage(List<string> collectionTypes, string connectionID)
        {
            CollectionTypes = collectionTypes;
            ConnectionID = connectionID;
        }

        public CollectionMessage(IEnumerable<CollectionType> collectionTypes, string connectionID)
        {
            CollectionTypes = collectionTypes.Select(Enum.GetName).ToList();
            ConnectionID = connectionID;
        }

        public CollectionMessage(CollectionType collectionType, string connectionID)
        {
            CollectionTypes = new List<string> { Enum.GetName(collectionType) };
            ConnectionID = connectionID;
        }

        public CollectionMessage()
        {
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            if (IsExpired)
            {
                throw new Exception("Message expired");
            }
            return await CollectAsync(cfg, ConnectionID, DatabaseName, cancellationToken);
        }

        /// <summary>
        /// Runs the configured collection types against a single source connection.  When the collect
        /// agent ferries data via S3 (remote/relay scenario) the collected data is returned so the caller
        /// can relay it back; otherwise the data is written directly to all destinations and null is
        /// returned.  Shared by <see cref="Process"/> and <see cref="MultiCollectionMessage"/>.
        /// </summary>
        internal async Task<DataSet> CollectAsync(CollectionConfig cfg, string connectionID, string databaseName, CancellationToken cancellationToken)
        {
            using var op = Operation.Begin("Collection triggered from message {Id} to collect {types} from {instance}",
                Id,
                CollectionTypes,
                connectionID);
            var src = await cfg.GetSourceConnectionAsync(connectionID);

            var (standardCollections, customCollections, unknownCollections) = ParseCollectionTypes(src, cfg);
            UnknownCollectionTypes = unknownCollections;

            // Don't run collections whose schedule has been disabled for this instance - a manual trigger
            // shouldn't collect something the user has turned off.  Disabled collections are skipped and, if
            // nothing is left to run, a warning is reported back rather than silently doing nothing.  The user
            // can confirm the warning to re-send the message with IgnoreDisabledSchedule set, forcing them to run.
            if (!IgnoreDisabledSchedule)
            {
                SkipDisabledCollections(cfg, src, standardCollections, customCollections, connectionID);
            }

            // A requested collection type that doesn't exist for this instance (e.g. a custom collection that
            // has since been removed) shouldn't sink the whole request - run whatever's valid and warn about
            // the rest.  Only when nothing valid is left to run do we surface a warning rather than collecting.
            if (unknownCollections.Count > 0)
            {
                if (standardCollections.Count == 0 && customCollections.Count == 0)
                {
                    throw new UnknownCollectionTypeException(unknownCollections);
                }
                Log.Warning("Message {Id}: skipping unknown collection type(s) {unknown} for {instance}", Id, unknownCollections, connectionID);
            }

            var collector = await DBCollector.CreateAsync(src, cfg.ServiceName, true);
            collector.FailedLoginsBackfillMinutes = cfg.FailedLoginsBackfillMinutes ?? CollectionConfig.DefaultFailedLoginsBackfillMinutes;
            await collector.CollectAsync(standardCollections.ToArray());
            await collector.CollectAsync(customCollections);

            if (standardCollections.Contains(CollectionType.SchemaSnapshot))
            {
                if (string.IsNullOrEmpty(databaseName)) //
                {
                    // Snapshot all configured databases
                    // Written to destinations as usual.  Could be additional delay to process.
                    // It's done DB at a time to limit the size of the data being processed.
                    Log.Information("Message {Id} requested schema snapshots for {instance}", Id, connectionID);
                    await SchemaSnapshotDB.GenerateSchemaSnapshots(cfg, src);
                }
                else
                {
                    Log.Information("Message {Id} requested snapshot for database {DatabaseName} on {instance}", Id, databaseName, connectionID);
                    var schema = new SchemaSnapshotDB(src.SourceConnection, cfg.SchemaSnapshotOptions);
                    var dt = schema.SnapshotDB(databaseName, collector);
                    collector.Data.Tables.Add(dt);
                }
            }

            if (CollectAgent.S3Path != null)
            {
                op.Complete();
                return collector.Data;
            }
            else
            {
                var fileName = DBADashSource.GenerateFileName(src.SourceConnection.ConnectionForFileName);
                await DestinationHandling.WriteAllDestinationsAsync(collector.Data, src, fileName, cfg);
                if (collector.Exceptions.Any())
                {
                    throw new AggregateException(collector.Exceptions);
                }
                op.Complete();
                return null;
            }
        }

        /// <summary>
        /// Removes any requested collections whose schedule is disabled (an empty cron expression) for this
        /// source.  SchemaSnapshot is scheduled separately and is always allowed.  If every requested
        /// collection is disabled a <see cref="CollectionScheduleDisabledException"/> is thrown so the
        /// caller can report a warning; otherwise the disabled ones are skipped and the rest run as normal.
        /// </summary>
        private void SkipDisabledCollections(CollectionConfig cfg, DBADashSource src,
            List<CollectionType> standardCollections, Dictionary<string, CustomCollection> customCollections,
            string connectionID)
        {
            // Effective schedule = agent/config schedule overlaid with any per-source overrides (mirrors the
            // resolution the scheduler uses when deciding what to collect automatically).
            var schedule = (src.CollectionSchedules is { Count: > 0 })
                ? CollectionSchedules.Combine(cfg.GetSchedules(), src.CollectionSchedules)
                : cfg.GetSchedules();

            var disabled = new List<string>();

            standardCollections.RemoveAll(type =>
            {
                if (type == CollectionType.SchemaSnapshot) return false; // scheduled separately - always allowed
                // Only skip when the schedule is explicitly present but disabled (empty cron expression).
                // Types absent from the schedule map are left to run rather than wrongly treated as disabled.
                if (schedule.TryGetValue(type, out var s) && string.IsNullOrEmpty(s.NormalizedSchedule))
                {
                    disabled.Add(Enum.GetName(type));
                    return true;
                }
                return false;
            });

            foreach (var name in customCollections
                         .Where(c => string.IsNullOrEmpty(c.Value.NormalizedSchedule))
                         .Select(c => c.Key).ToList())
            {
                customCollections.Remove(name);
                disabled.Add(name);
            }

            if (disabled.Count == 0) return;

            if (standardCollections.Count == 0 && customCollections.Count == 0)
            {
                // Nothing left to run - surface a warning rather than silently completing.
                throw new CollectionScheduleDisabledException(disabled);
            }

            Log.Warning("Message {Id}: skipping disabled collection(s) {disabled} for {instance}", Id, disabled, connectionID);
        }

        private (List<CollectionType>, Dictionary<string, CustomCollection>, List<string>) ParseCollectionTypes(DBADashSource src, CollectionConfig cfg)
        {
            var standardCollections = new List<CollectionType>();
            var customCollections = new Dictionary<string, CustomCollection>();
            var unknownCollections = new List<string>();

            foreach (var type in CollectionTypes)
            {
                if (type is null)
                {
                    throw new ArgumentException("Collection type cannot be null");
                }
                var customCollectionName = type.StartsWith("UserData.", StringComparison.OrdinalIgnoreCase) ? type["UserData.".Length..] : type;
                if (string.Equals(type, "QueryPlans", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "QueryText", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("QueryPlan and QueryText are collected as part of the RunningQueries collection");
                }
                else if (string.Equals(type, "SlowQueriesStats", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("SlowQueriesStats is collected as part of the SlowQueries collection");
                }
                else if (string.Equals(type, "InternalPerformanceCounters", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("InternalPerformanceCounters collection can't be triggered manually");
                }
                else if (CollectionTypeLegacyNames.TryParse(type, out var collectionType))
                {
                    // Honors legacy renames (e.g. "DriversWMI" -> Drivers) via the central map.
                    standardCollections.Add(collectionType);
                }
                else if (src.CustomCollections.TryGetValue(customCollectionName, out var customCollection))
                {
                    customCollections.Add(customCollectionName, customCollection);
                }
                else if (cfg.CustomCollections.TryGetValue(customCollectionName, out var globalCustomCollection))
                {
                    customCollections.Add(customCollectionName, globalCustomCollection);
                }
                else
                {
                    // Unknown collection type (e.g. a custom collection that's no longer configured).  Collected
                    // here rather than thrown so the remaining valid collections can still run - the caller
                    // (CollectAsync) decides whether to warn-and-continue or, if nothing valid is left, fail.
                    unknownCollections.Add(type);
                }
            }

            return (standardCollections, customCollections, unknownCollections);
        }
    }

    /// <summary>
    /// Thrown when every collection requested by a message is disabled (no schedule) for the target
    /// instance, so nothing was run.  Reported back to the GUI as a warning rather than an error.
    /// </summary>
    public class CollectionScheduleDisabledException : Exception
    {
        public List<string> DisabledCollections { get; }

        public CollectionScheduleDisabledException(List<string> disabledCollections)
            : base(disabledCollections is { Count: 1 }
                ? $"Collection '{disabledCollections[0]}' is disabled (no schedule) for this instance - nothing was collected."
                : $"Collections [{string.Join(", ", disabledCollections)}] are disabled (no schedule) for this instance - nothing was collected.")
        {
            DisabledCollections = disabledCollections;
        }
    }

    /// <summary>
    /// Thrown when every collection type requested by a message is unknown for the target instance (e.g. all
    /// were custom collections that have since been removed), so nothing could be run.  Reported back to the
    /// GUI as a warning.  When only some requested types are unknown the valid ones still run and the unknown
    /// ones are surfaced as a warning instead (see <see cref="CollectionMessage.UnknownCollectionTypes"/>).
    /// </summary>
    public class UnknownCollectionTypeException : Exception
    {
        public List<string> UnknownCollectionTypes { get; }

        public UnknownCollectionTypeException(List<string> unknownCollectionTypes)
            : base(unknownCollectionTypes is { Count: 1 }
                ? $"Collection type '{unknownCollectionTypes[0]}' is not valid for this instance - nothing was collected."
                : $"Collection types [{string.Join(", ", unknownCollectionTypes)}] are not valid for this instance - nothing was collected.")
        {
            UnknownCollectionTypes = unknownCollectionTypes;
        }

        /// <summary>
        /// Describes unknown collection types that were skipped while other valid collections still ran.
        /// Used to surface a warning for the partial case (unlike the constructor message, which is for the
        /// all-unknown case where nothing was collected).
        /// </summary>
        public static string DescribeSkipped(List<string> unknownCollectionTypes) =>
            unknownCollectionTypes is { Count: 1 }
                ? $"Collection type '{unknownCollectionTypes[0]}' is not valid for this instance and was skipped."
                : $"Collection types [{string.Join(", ", unknownCollectionTypes)}] are not valid for this instance and were skipped.";
    }
}