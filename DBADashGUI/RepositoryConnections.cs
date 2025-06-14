using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using DBADash;

namespace DBADashGUI
{
    internal class RepositoryConnectionList : SortedSet<RepositoryConnection>
    {
        public bool Remove(string name)
        {
            return base.Remove(FindByName(name));
        }

        public RepositoryConnection FindByName(string name)
        {
            return this.FirstOrDefault(c => c.Name.Equals(name));
        }

        public RepositoryConnection FindByConnectionString(string connectionString)
        {
            return this.FirstOrDefault(c => CompareConnections(c.ConnectionString, connectionString));
        }

        private bool CompareConnections(string connectionString1, string connectionString2)
        {
            var builder1 = new SqlConnectionStringBuilder(connectionString1);
            var builder2 = new SqlConnectionStringBuilder(connectionString2);
            return builder1.DataSource == builder2.DataSource
                   && builder1.InitialCatalog == builder2.InitialCatalog
                   && builder1.UserID == builder2.UserID
                   && builder1.IntegratedSecurity == builder2.IntegratedSecurity;
        }

        public bool Contains(string name)
        {
            return FindByName(name) != null;
        }

        public RepositoryConnection GetDefaultConnection()
        {
            var connection = this.FirstOrDefault(c => c.IsDefault);

            if (connection == null && Count > 0)
            {
                connection = this.First();
            }
            return connection;
        }

        private static string GetStorageFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "DBADashGUI");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "ConnectionStrings.json");
        }

        public void Save()
        {
            string filePath = GetStorageFilePath();
            if (Count == 0)
            {
                File.Delete(filePath);
            }
            else
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }

        public static RepositoryConnectionList GetRepositoryConnectionList()
        {
            try
            {
                string userPath = GetStorageFilePath();
                if (!File.Exists(userPath)) // Get config from app config if user config doesn't exist
                {
                    var connection = GetConfigRepositoryConnection();
                    if (connection == null) return new RepositoryConnectionList();
                    var list = new RepositoryConnectionList
                    {
                        connection
                    };
                    return list;
                }

                string json = File.ReadAllText(userPath);
                return JsonConvert.DeserializeObject<RepositoryConnectionList>(json);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading connections");
                return new RepositoryConnectionList();
            }
        }

        private static RepositoryConnection GetConfigRepositoryConnection()
        {
            if (!File.Exists(Common.JsonConfigPath))
            {
                return null;
            }
            string jsonConfig = File.ReadAllText(Common.JsonConfigPath);
            var cfg = BasicConfig.Deserialize(jsonConfig);
            if (cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL)
            {
                string connectionString = cfg.DestinationConnection.ConnectionString;
                var connection = new RepositoryConnection() { ConnectionString = connectionString, Name = "DBA Dash" };
                return connection;
            }
            else
            {
                return null;
            }
        }
    }

    public class RepositoryConnection : IComparable<RepositoryConnection>
    {
        public string EncryptedConnectionString { get; set; }

        [JsonIgnore]
        public Guid ConnectionID { get; set; } = Guid.NewGuid();

        public bool IsDefault { get; set; }

        public string GetDefaultName()
        {
            using var cn = new SqlConnection(ConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("SELECT ISNULL(@@SERVERNAME,ISNULL(CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(128)),'')) + '\\' + DB_NAME() as Name", cn);
            return (string)cmd.ExecuteScalar();
        }

        public void SetDefaultName()
        {
            Name = GetDefaultName();
        }

        public string Name { get; set; }

        [JsonIgnore]
        public string ConnectionString
        {
            get => EncryptedConnectionString.UserDecryptString();
            set => EncryptedConnectionString = value.UserEncryptString();
        }

        public int CompareTo(RepositoryConnection other)
        {
            if (other == null)
            {
                return 1;
            }

            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}