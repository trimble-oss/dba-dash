using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using ThirdParty.Json.LitJson;

namespace DBADash
{
    /// <summary>
    /// Basic config is used by GUI and is the base class for CollectionConfig used by service
    /// </summary>
    public class BasicConfig
    {
        public static readonly string JsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
        public bool EncryptConfig { get; set; }

        [JsonIgnore]
        public DBADashConnection DestinationConnection { get; set; }

        public BasicConfig()
        {
            DestinationConnection = new DBADashConnection();
        }

        // Note if destination is SQL connection string, password is encrypted.  Use GetDestination() to return with real password
        public string Destination
        {
            get => DestinationConnection.EncryptedConnectionString;
            set => DestinationConnection = new DBADashConnection(value);
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
            });
        }

        public static BasicConfig Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<BasicConfig>(json);
        }

        /// <summary>
        /// Save the config file to disk.  Config will be encrypted if encryption is specified.
        /// </summary>
        /// <param name="backup">Option to create a backup of the current config</param>
        public void Save(bool backup)
        {
            if (backup && File.Exists(JsonConfigPath))
            {
                File.Move(JsonConfigPath, JsonConfigPath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            string config = Serialize();
            if (EncryptConfig)
            {
                config = new EncryptedConfig(config).Serialize();
            }
            File.WriteAllText(JsonConfigPath, config);
        }

        /// <summary>
        /// Remove old config file backups
        /// </summary>
        /// <param name="retentionDays"></param>
        public static void ClearOldConfigBackups(int retentionDays)
        {
            if (retentionDays < 0) return;
            Log.Information("Remove configs older than {retentionDays} days", retentionDays);
            foreach (string file in Directory.GetFiles(AppContext.BaseDirectory, "ServiceConfig.json.backup*"))
            {
                var dateString = file[^14..];
                DateTime backupDate;
                if (!DateTime.TryParseExact(dateString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out backupDate)) continue;
                if (backupDate < DateTime.Now.AddDays(-retentionDays))
                {
                    Log.Information("Remove old config {file}", file);
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Deserialize config from disk.  If config is encrypted it will be decrypted
        /// </summary>
        /// <param name="password">Password to use for decryption</param>
        public static T Load<T>(string password = null) where T : BasicConfig, new()
        {
            if (string.IsNullOrEmpty(password))
            {
                password = EncryptedConfig.GetPassword();
            }
            if (!File.Exists(JsonConfigPath)) return new T();
            string config = File.ReadAllText(JsonConfigPath);
            if (IsConfigFileEncrypted())
            {
                if (password == null)
                {
                    throw new Exception("Password not available for decryption of config file");
                }
                config = EncryptedConfig.Deserialize(config).GetConfig(password);
            }
            return JsonConvert.DeserializeObject<T>(config);
        }

        public static bool IsConfigFileEncrypted()
        {
            if (!File.Exists(JsonConfigPath)) return false;
            string config = File.ReadAllText(JsonConfigPath);
            try
            {
                var cfg = EncryptedConfig.Deserialize(config);
                return cfg != null && !string.IsNullOrEmpty(cfg.ProtectedConfig);
            }
            catch
            {
                return false;
            }
        }

        public static bool ConfigExists => File.Exists(JsonConfigPath);
    }
}