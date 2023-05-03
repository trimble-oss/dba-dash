using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Navigation;
using Microsoft.Data.SqlClient;
using DBADash;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Microsoft.Web.WebView2.Core.Raw;
using ThirdParty.Json.LitJson;

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

        public bool Contains(string name)
        {
            return FindByName(name) != null;
        }

        public RepositoryConnection GetDefaultConnection()
        {
            var connection = this.FirstOrDefault(c => c.IsDefault);

            if (connection == null && this.Count > 0)
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
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
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
                MessageBox.Show("Error loading connections\n" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new RepositoryConnectionList();
            }
        }

        private static RepositoryConnection GetConfigRepositoryConnection()
        {
            if (!File.Exists(Common.JsonConfigPath))
            {
                return null;
            }
            string jsonConfig = System.IO.File.ReadAllText(Common.JsonConfigPath);
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
            using var cmd = new SqlCommand("SELECT @@SERVERNAME + '\\' + DB_NAME() as Name", cn);
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