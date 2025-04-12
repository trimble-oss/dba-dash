using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SerilogTimings;

namespace DBADash.Messaging
{
    // Concrete message type for triggering a collection
    public class CollectionMessage : MessageBase
    {
        public List<string> CollectionTypes { get; set; }

        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }

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
            using var op = Operation.Begin("Collection triggered from message {Id} to collect {types} from {instance}",
                Id,
                CollectionTypes,
                ConnectionID);
            var src =await cfg.GetSourceConnectionAsync(ConnectionID);

            var (standardCollections, customCollections) = ParseCollectionTypes(src, cfg);

            var collector = await DBCollector.CreateAsync(src, cfg.ServiceName, true);
            await collector.CollectAsync(standardCollections.ToArray());
            await collector.CollectAsync(customCollections);

            if (standardCollections.Contains(CollectionType.SchemaSnapshot))
            {
                if (string.IsNullOrEmpty(DatabaseName)) //
                {
                    // Snapshot all configured databases
                    // Written to destinations as usual.  Could be additional delay to process.
                    // It's done DB at a time to limit the size of the data being processed.
                    Log.Information("Message {Id} requested schema snapshots for {instance}", Id, ConnectionID);
                    await SchemaSnapshotDB.GenerateSchemaSnapshots(cfg, src);
                }
                else
                {
                    Log.Information("Message {Id} requested snapshot for database {DatabaseName} on {instance}", Id, DatabaseName, ConnectionID);
                    var schema = new SchemaSnapshotDB(src.SourceConnection, cfg.SchemaSnapshotOptions);
                    var dt = schema.SnapshotDB(DatabaseName, collector);
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
                await DestinationHandling.WriteAllDestinations(collector.Data, src, fileName, cfg);
                op.Complete();
                return null;
            }
        }

        private (List<CollectionType>, Dictionary<string, CustomCollection>) ParseCollectionTypes(DBADashSource src, CollectionConfig cfg)
        {
            var standardCollections = new List<CollectionType>();
            var customCollections = new Dictionary<string, CustomCollection>();

            foreach (var type in CollectionTypes)
            {
                if (type is null)
                {
                    throw new ArgumentException("Collection type cannot be null");
                }
                var customCollectionName = type.StartsWith("UserData.", StringComparison.OrdinalIgnoreCase) ? type["UserData.".Length..] : type;
                if (string.Equals(type, "Drivers", StringComparison.OrdinalIgnoreCase))
                {
                    standardCollections.Add(CollectionType.DriversWMI);
                }
                else if (string.Equals(type, "QueryPlans", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "QueryText", StringComparison.OrdinalIgnoreCase))
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
                else if (Enum.TryParse<CollectionType>(type, out var collectionType))
                {
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
                    throw new ArgumentException($"Unknown collection type {type}");
                }
            }

            return (standardCollections, customCollections);
        }
    }
}