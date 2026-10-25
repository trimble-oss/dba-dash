using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;

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

        public static readonly string JsonConfigPath = Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
        public EncryptionOptions EncryptionOption { get; set; }

        [JsonIgnore]
        public DBADashConnection DestinationConnection { get; set; }

        public int ConfigBackupRetentionDays { get; set; } = 7;

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

        /// <summary>
        /// Key the config was loaded or last saved with.  Reused on save while the password is unchanged so the salt and protected secrets stay the same.
        /// </summary>
        [JsonIgnore]
        public ConfigKey EncryptionKey { get; private set; }

        /// <summary>
        /// Config was loaded from a file encrypted using the legacy format.  It will be upgraded on save.
        /// </summary>
        [JsonIgnore]
        public bool HasLegacyEncryption { get; private set; }

        /// <summary>
        /// Random key used to protect secrets when the config isn't encrypted (Windows only), stored protected with DPAPI LocalMachine.  Null if the config is encrypted.
        /// </summary>
        public string MachineKey { get; set; }

        private ConfigKey machineKey;

        private ConfigKey GetOrCreateMachineKey()
        {
            if (!OperatingSystem.IsWindows()) return null;
            if (machineKey != null) return machineKey;
            machineKey = ConfigKey.CreateMachineKey(out var protectedKey);
            MachineKey = protectedKey;
            return machineKey;
        }

        /// <summary>
        /// Load the machine key before deserializing so secrets protected with it can be decrypted.  Null if there is no key or it was protected on a different machine.
        /// </summary>
        private static ConfigKey LoadMachineKey(string protectedKey)
        {
            if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(protectedKey)) return null;
            try
            {
                var key = ConfigKey.LoadMachineKey(protectedKey);
                SecretProtection.UseKey(key);
                return key;
            }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
            {
                Log.Warning(ex, "Unable to load the machine key from the config.  Secrets protected with it will need to be re-entered.");
                return null;
            }
        }

        /// <summary>
        /// Serialize the config.  Secrets are re-protected to match <see cref="EncryptionOption"/> first: with the config encryption key if the config is encrypted, otherwise the machine key on Windows.
        /// </summary>
        /// <param name="encryptionOption">Option to wrap the output in an <see cref="EncryptedConfig"/>.  Use the default to get the decrypted JSON for display.</param>
        /// <param name="password">Config encryption password.  Uses the saved password if not specified.</param>
        public string Serialize(EncryptionOptions encryptionOption = EncryptionOptions.Basic, string password = null)
        {
            var encrypt = encryptionOption == EncryptionOptions.Encrypt;
            ConfigKey key = null;
            if (encrypt || EncryptionOption == EncryptionOptions.Encrypt)
            {
                password ??= EncryptedConfig.GetPassword();
                if (password == null && encrypt)
                {
                    throw new ArgumentNullException(nameof(password), @"Password is not set");
                }
                if (password != null)
                {
                    key = ConfigKey.ForPassword(password, EncryptionKey ?? SecretProtection.DefaultKey);
                    EncryptionKey = key;
                    SecretProtection.UseKey(key);
                    // Secrets are protected with the password key so the machine key isn't needed
                    machineKey = null;
                    MachineKey = null;
                }
            }
            else if (machineKey == null && !string.IsNullOrEmpty(MachineKey) && !ContainsSensitive())
            {
                // The stored machine key can't be loaded here (the config was copied from another machine) and there's no
                // password to protect yet.  Keep it rather than replacing it, so the secrets that need it can still be
                // decrypted on the machine it came from.  A new key is created when a password is entered.
                SecretProtection.ClearDefaultKey();
            }
            else
            {
                key = GetOrCreateMachineKey();
                if (key != null)
                {
                    SecretProtection.UseKey(key);
                }
                else
                {
                    SecretProtection.ClearDefaultKey();
                }
            }
            // If the config is encrypted but the password isn't available, leave secrets as they are
            if (key != null || EncryptionOption == EncryptionOptions.Basic)
            {
                ProtectSecrets(key, new HashSet<string>(StringComparer.Ordinal));
            }

            var config = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
            });
            if (encrypt)
            {
                config = new EncryptedConfig(config, key).Serialize();
            }
            return config;
        }

        /// <summary>
        /// Protect secrets with <paramref name="key"/>, or DPAPI/obfuscation if null.  Secrets already in that format are left unchanged unless
        /// another connection has the same protected value (see <paramref name="seen"/>).
        /// </summary>
        protected virtual void ProtectSecrets(ConfigKey key, HashSet<string> seen)
        {
            DestinationConnection?.ProtectSecret(key, seen);
        }

        /// <summary>
        /// True if secrets aren't in the format that matches <see cref="EncryptionOption"/>.  Use to upgrade the config file.
        /// </summary>
        public bool RequiresSecretUpgrade()
        {
            if (EncryptionOption == EncryptionOptions.Encrypt)
            {
                return HasLegacyEncryption || EncryptionKey == null || SecretsNeedProtection(EncryptionKey);
            }
            if (OperatingSystem.IsWindows() && machineKey == null)
            {
                return ContainsSensitive();
            }
            return SecretsNeedProtection(machineKey);
        }

        protected virtual bool SecretsNeedProtection(ConfigKey key) => DestinationConnection?.SecretNeedsProtection(key) == true;

        /// <summary>
        /// The secrets in the config: passwords of SQL connections and the AWS secret key, whether or not they can be decrypted here.
        /// </summary>
        public virtual List<ConfigSecret> GetSecrets()
        {
            var list = new List<ConfigSecret>();
            if (DestinationConnection?.HasPassword == true)
            {
                list.Add(ConfigSecret.ForConnection(ConfigSecret.SecretTypes.Destination, DestinationConnection.ConnectionForPrint, DestinationConnection));
            }
            return list;
        }

        /// <summary>
        /// Descriptions of secrets that can't be decrypted on this machine, e.g. passwords from a config copied from another machine.
        /// </summary>
        public List<string> GetUnreadableSecrets() => GetSecrets().Where(s => !s.IsReadable).Select(s => s.ToString()).ToList();

        public static BasicConfig Deserialize(string json)
        {
            return Deserialize<BasicConfig>(json);
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
                File.Copy(JsonConfigPath, movePath, false);
            }
            File.WriteAllText(JsonConfigPath, config);
        }

        public void Save()
        {
            var backup = ConfigBackupRetentionDays > 0;
            Save(backup);
        }

        /// <summary>
        /// If the file already exists, make the filename unique by adding _1, _2, _3 etc.
        /// </summary>
        /// <param name="fullPath">Full path to the file</param>
        /// <returns></returns>
        public static string GetUniqueFilename(string fullPath)
        {
            var path = Path.GetDirectoryName(fullPath) ?? throw new Exception($"GetDirectoryName returned null for {fullPath}");
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

            Directory.EnumerateFiles(AppContext.BaseDirectory, "ServiceConfig.json*.backup*")
                .Where(file => (File.GetCreationTime(file)) < DateTime.Now.AddDays(-retentionDays))
                .ToList()
                .ForEach(file =>
                {
                    File.Delete(file);
                    Log.Information("Remove old config {file}", file);
                });
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
            ConfigKey key = null;
            var legacy = false;
            // Parsed once for the encryption check and the machine key.  Dates are left as strings - only string values are read from it.
            var root = JObject.Load(new JsonTextReader(new StringReader(config)) { DateParseHandling = DateParseHandling.None });
            string protectedMachineKey;
            if (IsConfigEncrypted(root))
            {
                if (string.IsNullOrEmpty(password))
                {
                    password = EncryptedConfig.GetPassword();
                }
                if (string.IsNullOrEmpty(password))
                {
                    throw new ConfigDecryptionError("Password not available for decryption of config file");
                }
                config = root.ToObject<EncryptedConfig>().GetConfig(password, out key);
                legacy = key == null;
                // Must be registered before deserializing so ¬=k1: secrets can be decrypted
                if (key != null) SecretProtection.UseKey(key);
                // An encrypted config doesn't need a machine key, so avoid parsing the decrypted config for it
                protectedMachineKey = config.Contains($"\"{nameof(MachineKey)}\"")
                    ? JObject.Parse(config).Value<string>(nameof(MachineKey))
                    : null;
            }
            else
            {
                protectedMachineKey = root.Value<string>(nameof(MachineKey));
            }
            var loadedMachineKey = LoadMachineKey(protectedMachineKey);
            var result = JsonConvert.DeserializeObject<T>(config);
            if (result != null)
            {
                result.EncryptionKey = key;
                result.HasLegacyEncryption = legacy;
                result.machineKey = loadedMachineKey;
            }
            return result;
        }

        public static bool IsConfigEncrypted(string config) => IsConfigEncrypted(JObject.Parse(config));

        private static bool IsConfigEncrypted(JObject config) => config.ContainsKey(nameof(EncryptedConfig.ProtectedConfig));

        public static bool IsConfigFileEncrypted()
        {
            if (!File.Exists(JsonConfigPath)) return false;
            string config = File.ReadAllText(JsonConfigPath);
            return IsConfigEncrypted(config);
        }

        /// <summary>
        /// True if the config has a secret that can be decrypted here (and so protected again) - a password on any
        /// destination or source, or the AWS secret key.
        /// </summary>
        public bool ContainsSensitive() => GetSecrets().Any(s => s.IsReadable && s.HasValue);

        public static bool ConfigExists => File.Exists(JsonConfigPath);
    }
}