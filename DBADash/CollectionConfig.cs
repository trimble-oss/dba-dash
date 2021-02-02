using DBADashService;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using static DBADash.DBADashConnection;

namespace DBADash
{

    public class CollectionConfig
    {
        public Int32 ServiceThreads=-1;
        private string _secretKey;
        private bool wasEncryptionPerformed = false;
        private string myString = "g&hAs2&mVOLwE6DqO!I5";
        public SchemaSnapshotDBOptions SchemaSnapshotOptions=null;
        public bool BinarySerialization { get; set; } = false;
        public bool ScanForAzureDBs { get; set; } = false;
        public Int32 ScanForAzureDBsInterval { get; set; } = 3600;
        public string ServiceName { get; set; } = "DBADashService";

        public bool AutoUpdateDatabase { get; set; }

        public List<DBADashSource> SourceConnections = new List<DBADashSource>();

        public CollectionConfig()
        {
            DestinationConnection = new DBADashConnection();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,

            });
        }

        public static CollectionConfig Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<CollectionConfig>(json);
        }

        public string AWSProfile { get; set; }

        public string AccessKey { get; set; }
        public string SecretKey
        {
            get
            {
                return _secretKey;
            }
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
                    _secretKey = "¬=!" + EncryptText.EncryptString(value, myString);
                    wasEncryptionPerformed = true;
                }
            }
        }


        public string GetSecretKey()
        {

            if (_secretKey != null && _secretKey.StartsWith("¬=!"))
            {
                return EncryptText.DecryptString(_secretKey.Substring(3), myString);
            }
            else
            {
                return _secretKey;
            }

        }

        [JsonIgnore]
        public DBADashConnection DestinationConnection { get; set; }

        private string defaultMaintenanceCron = " 0 0 0 ? * * *";

        public string MaintenanceScheduleCron { get; set; }

        public string GetMaintenanceCron()
        {
            if (MaintenanceScheduleCron == null || MaintenanceScheduleCron == "")
            {
                return defaultMaintenanceCron;
            }
            else
            {
                return MaintenanceScheduleCron;
            }
        }


        // Note if destination is SQL connection string, password is encrypted.  Use GetDestination() to return with real password
        public string Destination
        {
            get
            {
                return DestinationConnection.EncryptedConnectionString;
            }
            set
            {
                DestinationConnection = new DBADashConnection(value);
            }
        }

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

       public DBADashSource GetSourceFromConnectionString(string connectionString,bool? isAzure=null)
        {
            var findConnection = new DBADashConnection(connectionString);
            foreach (var s in SourceConnections)
            {

                if (s.SourceConnection.Type == findConnection.Type)
                {
                    if (s.SourceConnection.Type == ConnectionType.SQL && s.SourceConnection.DataSource() == findConnection.DataSource()) {
                        // normally we can treat as same connection if we just vary by initial catalog.  For AzureDB, a different DB is a different instance
                        if (s.SourceConnection.InitialCatalog() == findConnection.InitialCatalog()) {
                            return s;
                        }
                        else
                        {
                            if(isAzure == null)
                            {
                                isAzure = findConnection.IsAzureDB();
                            }
                            if (isAzure==false)
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
            }
            return null;
        }

        public bool SourceExists(string connectionString,bool? isAzure=null)
        {
            if (GetSourceFromConnectionString(connectionString,isAzure) != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public void AddConnections(List<DBADashSource> connections)
        {
            SourceConnections.AddRange(connections);
        }

        public List<DBADashSource> AddAzureDBs()
        {
            var newConnections = GetNewAzureDBConnections();
            SourceConnections.AddRange(newConnections);
            return newConnections;
        }

        public List<DBADashSource> GetNewAzureDBConnections()
        {
            var newConnections = new List<DBADashSource>();
            foreach (var cfg in SourceConnections)
            {
                if (cfg.SourceConnection.InitialCatalog() == "master" && cfg.SourceConnection.IsAzureDB())
                {
                    try
                    {
                        newConnections.AddRange(GetNewAzureDBConnections(cfg));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(string.Format("Error getting AzureDB connections from master '{0}': {1}", cfg.SourceConnection.ConnectionForPrint, ex.Message));
                    }
                }
            }
            return newConnections;
        }

        public List<DBADashSource> GetNewAzureDBConnections(DBADashSource masterConnection)
        {
            var newConnections = new List<DBADashSource>();
            SqlConnection cn = new SqlConnection(masterConnection.SourceConnection.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", cn);
                var rdr = cmd.ExecuteReader();
                var builder = new SqlConnectionStringBuilder(masterConnection.SourceConnection.ConnectionString);
                
                while (rdr.Read())
                {
                    builder.InitialCatalog = rdr.GetString(0);
                    DBADashSource dbCn = new DBADashSource(builder.ConnectionString);
                    dbCn.Schedules = masterConnection.Schedules;
                    dbCn.SlowQueryThresholdMs = masterConnection.SlowQueryThresholdMs;
                    if (masterConnection.SchemaSnapshotDBs == "*")
                    {
                        dbCn.SchemaSnapshotDBs = masterConnection.SchemaSnapshotDBs;
                        dbCn.SchemaSnapshotCron = masterConnection.SchemaSnapshotCron;
                        dbCn.SchemaSnapshotOnServiceStart = masterConnection.SchemaSnapshotOnServiceStart;
                    }
                    if (!SourceExists(dbCn.SourceConnection.ConnectionString,true))
                    {
                        newConnections.Add(dbCn);
                        Console.WriteLine("Adding AzureDB Connection:" + builder.DataSource + "|" + builder.InitialCatalog);
                    }
                }
            }
            return newConnections;
        }

        public void AddAzureDBs(DBADashSource masterConnection)
        {
            try
            {
                Console.WriteLine("Add azure DBs from " + masterConnection.SourceConnection.ConnectionForPrint);
                SourceConnections.AddRange(GetNewAzureDBConnections(masterConnection));
            }
            catch(Exception ex)
            {
                Console.WriteLine(string.Format("Error getting AzureDB connections from master '{0}': {1}", masterConnection.SourceConnection.ConnectionForPrint, ex.Message));
            }

        }
    }
}