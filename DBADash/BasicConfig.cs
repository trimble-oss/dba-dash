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
using Newtonsoft.Json.Linq;

namespace DBADash
{
    /// <summary>
    /// Basic config is used by GUI and is the base class for CollectionConfig used by service
    /// </summary>
    public class BasicConfig
    {
        public enum EncryptionOptions
        {
            Basic = 0,
            Encrypt = 1
        }

        public static readonly string JsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
        public EncryptionOptions EncryptionOption { get; set; }

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

        public string Serialize(EncryptionOptions encryptionOption = EncryptionOptions.Basic, string password = null)
        {
            var config = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
            });
            if (encryptionOption == EncryptionOptions.Encrypt)
            {
                password ??= EncryptedConfig.GetPassword();
                config = new EncryptedConfig(config, password).Serialize();
            }
            return config;
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
            var config = Serialize(EncryptionOption);
            if (backup && File.Exists(JsonConfigPath))
            {
                var movePath = GetUniqueFilename(JsonConfigPath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                File.Move(JsonConfigPath, movePath);
            }
            File.WriteAllText(JsonConfigPath, config);
        }

        /// <summary>
        /// If the file already exists, make the filename unique by adding _1, _2, _3 etc.
        /// </summary>
        /// <param name="fullPath">Full path to the file</param>
        /// <returns></returns>
        public static string GetUniqueFilename(string fullPath)
        {
            var path = Path.GetDirectoryName(fullPath);
            var filename = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            var newFullPath = fullPath;
            var counter = 1;
            while (File.Exists(newFullPath))
            {
                var tempFileName = $"{filename}_{counter}";
                newFullPath = Path.Combine(path, tempFileName + extension);
                counter++;
            }

            return newFullPath;
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
            if (!File.Exists(JsonConfigPath)) return new T();
            var config = File.ReadAllText(JsonConfigPath);
            return Deserialize<T>(config, password);
        }

        public static T Deserialize<T>(string config, string password = null) where T : BasicConfig, new()
        {
            if (IsConfigEncrypted(config))
            {
                if (string.IsNullOrEmpty(password))
                {
                    password = EncryptedConfig.GetPassword();
                }
                if (string.IsNullOrEmpty(password))
                {
                    throw new Exception("Password not available for decryption of config file");
                }
                config = EncryptedConfig.Deserialize(config).GetConfig(password);
            }
            return JsonConvert.DeserializeObject<T>(config);
        }

        public static bool IsConfigEncrypted(string config)
        {
            JObject jsonObject = JObject.Parse(config);
            return jsonObject.ContainsKey("ProtectedConfig");
        }

        public static bool IsConfigFileEncrypted()
        {
            if (!File.Exists(JsonConfigPath)) return false;
            string config = File.ReadAllText(JsonConfigPath);
            return IsConfigEncrypted(config);
        }

        public static bool ConfigExists => File.Exists(JsonConfigPath);
    }
}