using DBADashService;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class CollectionConfig : BasicConfig
    {
        public Int32 ServiceThreads = -1;
        private string _secretKey;
        private bool wasEncryptionPerformed;
        private readonly string myString = "g&hAs2&mVOLwE6DqO!I5";
        public SchemaSnapshotDBOptions SchemaSnapshotOptions = null;
        public bool ScanForAzureDBs { get; set; } = true;
        public Int32 ScanForAzureDBsInterval { get; set; } = 3600;
        public string ServiceName { get; set; } = "DBADashService";

        public bool AutoUpdateDatabase { get; set; } = true;
        public bool LogInternalPerformanceCounters = false;
        public int? IdentityCollectionThreshold = null;
        public CollectionSchedules CollectionSchedules;
        public Dictionary<string, CustomCollection> CustomCollections { get; set; } = new();

        public string SummaryRefreshCron { get; set; }

        public int? ImportCommandTimeout { get; set; }
        public int? PurgeDataCommandTimeout { get; set; }
        public int? AddPartitionsCommandTimeout { get; set; }

        public bool EnableMessaging { get; set; } = true;

        public bool AllowPlanForcing { get; set; }

        public string AllowedScripts { get; set; }

        public string AllowedJobs { get; set; }

        public string ServiceSQSQueueUrl { get; set; }

        public bool ProcessAlerts { get; set; } = true;

        public int? AlertProcessingFrequencySeconds { get; set; }

        public int? AlertProcessingStartupDelaySeconds { get; set; }

        public const int DefaultAlertProcessingFrequencySeconds = 60;

        public const int DefaultAlertProcessingStartupDelaySeconds = 60;

        public CollectionSchedules GetSchedules()
        {
            if (CollectionSchedules == null)
            {
                return CollectionSchedules.DefaultSchedules;
            }
            else
            {
                return CollectionSchedules.CombineWithDefault();
            }
        }

        public List<DBADashSource> SourceConnections = new();

        public static CollectionConfig Deserialize(string json, string password = null)
        {
            return BasicConfig.Deserialize<CollectionConfig>(json, password);
        }

        public string AWSProfile { get; set; }

        public string AccessKey { get; set; }

        public string SecretKey
        {
            get => _secretKey;
            set
            {
                if (value == "")
                {
                    _secretKey = null;
                }
                else if (value == null || value.StartsWith("¬=!"))
                {
                    _secretKey = value;
                }
                else
                {
                    _secretKey = "¬=!" + value.EncryptString(myString);
                    wasEncryptionPerformed = true;
                }
            }
        }

        public string GetSecretKey()
        {
            if (_secretKey != null && _secretKey.StartsWith("¬=!"))
            {
                return _secretKey[3..].DecryptString(myString);
            }
            else
            {
                return _secretKey;
            }
        }

        private const string defaultMaintenanceCron = "0 0 0/12 1/1 * ? *";

        public string MaintenanceScheduleCron { get; set; }

        public string GetMaintenanceCron()
        {
            return MaintenanceScheduleCron is null or "" ? defaultMaintenanceCron : MaintenanceScheduleCron;
        }

        [JsonIgnore]
        public List<DBADashConnection> SecondaryDestinationConnections { get; set; } = new();

        // Required for serialization
        public string[] SecondaryDestinations
        {
            get
            {
                var encryptedStrings = new List<string>();
                foreach (var d in SecondaryDestinationConnections)
                {
                    encryptedStrings.Add(d.EncryptedConnectionString);
                }
                return encryptedStrings.ToArray();
            }
            set
            {
                SecondaryDestinationConnections = new List<DBADashConnection>();
                foreach (var s in value.Distinct().ToArray())
                {
                    SecondaryDestinationConnections.Add(new DBADashConnection(s));
                }
            }
        }

        [JsonIgnore]
        public List<DBADashConnection> AllDestinations
        {
            get
            {
                var all = new List<DBADashConnection>();
                if (!string.IsNullOrEmpty(Destination))
                {
                    all.Add(DestinationConnection);
                }
                all.AddRange(SecondaryDestinationConnections);
                return all;
            }
        }

        [JsonIgnore]
        public List<DBADashConnection> SQLDestinations => AllDestinations.Where(d => d.Type == ConnectionType.SQL).ToList();

        public bool WasEncrypted()
        {
            if (wasEncryptionPerformed)
            {
                return wasEncryptionPerformed;
            }
            if (DestinationConnection.WasEncrypted)
            {
                wasEncryptionPerformed = true;
            }
            else
            {
                foreach (var c in SourceConnections)
                {
                    if (c.SourceConnection.WasEncrypted)
                    {
                        wasEncryptionPerformed = true;
                    }
                }
            }
            return wasEncryptionPerformed;
        }

        public int? MessageThreads { get; set; }

        public void ValidateDestination()
        {
            ValidateDestination(DestinationConnection);
        }

        public static void ValidateDestination(DBADashConnection destination)
        {
            if (destination.Type == ConnectionType.Invalid)
            {
                throw new Exception("Invalid connection string");
            }
            else if (destination.Type == ConnectionType.SQL)
            {
                if (string.IsNullOrEmpty(destination.InitialCatalog()))
                {
                    throw new Exception("Provide a name for the DBADash repository database through the Initial Catalog property of the connection string");
                }
                // VersionStatus check will throw an error if there is an issue connecting to the DB or the DB isn't valid.
                var status = DBValidations.VersionStatus(destination.ConnectionString);
                if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    ValidateDestinationVersion(destination);
                }
                Log.Information("DB Version Status: {status}", status.VersionStatus);
            }
            else
            {
                destination.Validate();
            }
        }

        private static void ValidateDestinationVersion(DBADashConnection destination)
        {
            var master = destination.MasterConnection();
            var cInfo = master.ConnectionInfo;
            if (string.IsNullOrEmpty(cInfo.ServerName))
            {
                Log.Warning("@@SERVERNAME returned NULL for {connection}.  Consider fixing with sp_addserver", destination.ConnectionForPrint);
            }
            if (cInfo.MajorVersion < 13 && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlDatabase && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlManagedInstance) // 13=2016, 12 might be Azure DB which is OK
            {
                throw new Exception("DBA Dash repository database requires SQL 2016 SP1 or later");
            }
            else if (cInfo.MajorVersion == 13 && (new Version(cInfo.ProductVersion)).CompareTo(new Version("13.0.4001.0")) < 0 && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.Enterprise)
            { // Check if we are running SP1 for SQL 2016
                throw new Exception("DBA Dash repository database requires SQL 2016 SP1 or later.  Please upgrade to SP1 or later.");
            }
        }

        public DBADashSource GetSourceFromConnectionString(string connectionString, bool? isAzure = null)
        {
            var findConnection = new DBADashConnection(connectionString);
            foreach (var s in SourceConnections.Where(s => s.SourceConnection.Type == findConnection.Type))
            {
                if (s.SourceConnection.Type == ConnectionType.SQL && string.Equals(s.SourceConnection.DataSource(), findConnection.DataSource(), StringComparison.CurrentCultureIgnoreCase) && s.SourceConnection.ApplicationIntent() == findConnection.ApplicationIntent())
                {
                    // normally we can treat as same connection if we just vary by initial catalog.  For AzureDB, a different DB is a different instance
                    if (string.Equals(s.SourceConnection.InitialCatalog(), findConnection.InitialCatalog(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        return s;
                    }
                    else if ((new[] { "master", string.Empty }).Contains(s.SourceConnection.InitialCatalog().ToLower())
                             && (new[] { "master", string.Empty }).Contains(findConnection.InitialCatalog().ToLower()))
                    {
                        return s;
                    }
                    else
                    {
                        isAzure ??= findConnection.IsAzureDB();
                        if (isAzure == false)
                        {
                            return s;
                        }
                    }
                }
                else if (s.SourceConnection.ConnectionString == findConnection.ConnectionString)
                {
                    return s;
                }
            }
            return null;
        }

        public bool SourceExists(string connectionString, bool? isAzure = null)
        {
            return GetSourceFromConnectionString(connectionString, isAzure) != null;
        }

        public void AddConnections(List<DBADashSource> connections)
        {
            SourceConnections.AddRange(connections);
        }

        public List<DBADashSource> AddAzureDBs()
        {
            var newConnections = GetNewAzureDBConnections();
            lock (SourceConnections)
            {
                SourceConnections.AddRange(newConnections);
            }
            return newConnections;
        }

        public List<DBADashSource> AddAzureDBs(DBADashSource masterConnection)
        {
            var newConnections = GetNewAzureDBConnections(masterConnection);
            SourceConnections.AddRange(newConnections);
            return newConnections;
        }

        public List<DBADashSource> GetNewAzureDBConnections()
        {
            var newConnections = new List<DBADashSource>();
            foreach (var cfg in SourceConnections.Where(src => src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src)))
            {
                try
                {
                    if ((new[] { "master", String.Empty }).Contains(cfg.SourceConnection.InitialCatalog()) && cfg.SourceConnection.ConnectionInfo.IsAzureMasterDB)
                    {
                        newConnections.AddRange(GetNewAzureDBConnections(cfg));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Azure DB connections from master {connection}", cfg.SourceConnection.ConnectionForPrint);
                }
            }
            return newConnections;
        }

        public List<DBADashSource> GetNewAzureDBConnections(DBADashSource masterConnection)
        {
            var newConnections = new List<DBADashSource>();
            using (var cn = new SqlConnection(masterConnection.SourceConnection.ConnectionString))
            using (var cmd = new SqlCommand("SELECT name from sys.databases WHERE name <> 'master'", cn))
            {
                cn.Open();
                using var rdr = cmd.ExecuteReader();
                var builder = new SqlConnectionStringBuilder(masterConnection.SourceConnection.ConnectionString);

                while (rdr.Read())
                {
                    builder.InitialCatalog = rdr.GetString(0);
                    var dbCn = masterConnection.DeepCopy();
                    dbCn.SourceConnection.ConnectionString = builder.ConnectionString;
                    dbCn.ConnectionID = string.Empty;

                    if (!SourceExists(dbCn.SourceConnection.ConnectionString, true))
                    {
                        newConnections.Add(dbCn);
                        Log.Information("Add Azure DB connection {DataSource}|{DB}", builder.DataSource, builder.InitialCatalog);
                    }
                }
            }
            return newConnections;
        }

        public override bool ContainsSensitive()
        {
            if (!string.IsNullOrEmpty(SecretKey))
            {
                return true;
            }

            foreach (var src in SourceConnections)
            {
                if (src.SourceConnection.Type == ConnectionType.SQL && new SqlConnectionStringBuilder(src.SourceConnection.ConnectionString).Password.Length > 0)
                {
                    return true;
                }
            }

            return base.ContainsSensitive();
        }

        public DBADashConnection GetDestination(string hash)
        {
            return DestinationConnection.Hash == hash ? DestinationConnection : SecondaryDestinationConnections.FirstOrDefault(c => c.Hash == hash);
        }

        public async Task<DBADashSource> GetSourceConnectionAsync(string connectionID)
        {
            if (!string.IsNullOrEmpty(connectionID))
            {
                var src = SourceConnections.FirstOrDefault(s =>
                    string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
                if (src != null) // We have a match on ConnectionID
                {
                    return src;
                }
                else if
                    (connectionID.Contains('|') &&
                     ScanForAzureDBs) // We don't have a match but ConnectionID looks like an AzureDB connection.
                {
                    // Try to find the master connection for this AzureDB
                    var masterInstanceName = connectionID.Split('|')[0] + "|master";
                    var masterSrc = SourceConnections.FirstOrDefault(s =>
                        string.Equals(s.ConnectionID, masterInstanceName, StringComparison.InvariantCultureIgnoreCase));
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
                        var collector = await DBCollector.CreateAsync(src, ServiceName);
                        src.ConnectionID = collector.ConnectionID;
                        // Double check that the generated ConnectionID matches the one we're looking for & return the connection
                        if (string.Equals(src.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return src;
                        }
                    }
                }
            }

            throw new ArgumentException($"Unable to find instance with ConnectionID {connectionID}");
        }

        /// <summary>
        /// Get source connection from connection string or ConnectionID.  Returns null if not found
        /// </summary>
        public async Task<DBADashSource> FindSourceConnectionAsync(string connectionString, string connectionId)
        {
            var source = GetSourceFromConnectionString(connectionString);
            try
            {
                if (!string.IsNullOrEmpty(connectionId))
                {
                    source = await GetSourceConnectionAsync(connectionId);
                }
            }
            catch
            {
                // ignore
            }

            return source;
        }
    }
}