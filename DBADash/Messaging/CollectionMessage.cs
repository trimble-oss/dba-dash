using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Serilog;
using static Microsoft.SqlServer.Management.SqlParser.Metadata.MetadataInfoProvider;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Azure;
using SerilogTimings;

namespace DBADash.Messaging
{
    // Concrete message type for triggering a collection
    public class CollectionMessage : MessageBase
    {
        public List<string> CollectionTypes { get; set; }

        public string ConnectionID { get; set; }

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

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle)
        {
            if (IsExpired)
            {
                throw new Exception("Message expired");
            }
            using var op = Operation.Begin("Collection triggered from message {handle} to collect {types} from {instance}",
                handle,
                CollectionTypes,
                ConnectionID);
            var src = GetSourceConnection(ConnectionID, cfg);

            var (standardCollections, customCollections) = ParseCollectionTypes(src, cfg);

            if (standardCollections.Contains(CollectionType.SchemaSnapshot))
            {
                // Written to destinations as usual.  Could be additional delay to process.
                // It's done DB at a time to limit the size of the data being processed.
                await SchemaSnapshotDB.GenerateSchemaSnapshots(cfg, src);
            }

            var collector = new DBCollector(src, cfg.ServiceName, true);
            collector.Collect(standardCollections.ToArray());
            collector.Collect(customCollections);

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

        public static DBADashSource GetSourceConnection(string connectionID, CollectionConfig cfg)
        {
            var src = cfg.SourceConnections.FirstOrDefault(s => string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
            if (src != null) // We have a match on ConnectionID
            {
                return src;
            }
            else if (connectionID.Contains('|') && cfg.ScanForAzureDBs) // We don't have a match but ConnectionID looks like an AzureDB connection.
            {
                // Try to find the master connection for this AzureDB
                var masterInstanceName = connectionID.Split('|')[0] + "|master";
                var masterSrc = cfg.SourceConnections.FirstOrDefault(s => string.Equals(s.ConnectionID, masterInstanceName, StringComparison.InvariantCultureIgnoreCase));
                if (masterSrc != null)
                {
                    // Master connection found. Create a copy with the correct database name
                    src = masterSrc.DeepCopy();
                    src.ConnectionID = null;
                    var builder = new SqlConnectionStringBuilder(masterSrc.SourceConnection.ConnectionString)
                    {
                        InitialCatalog = connectionID.Split('|')[1]
                    };
                    src.SourceConnection.ConnectionString = builder.ToString();
                    var collector = new DBCollector(src, cfg.ServiceName);
                    src.ConnectionID = collector.ConnectionID;
                    // Double check that the generated ConnectionID matches the one we're looking for & return the connection
                    if (string.Equals(src.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return src;
                    }
                }
            }
            throw new ArgumentException($"Unable to find instance with ConnectionID {connectionID}");
        }
    }
}