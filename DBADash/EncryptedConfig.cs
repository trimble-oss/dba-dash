using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Newtonsoft.Json;

namespace DBADash
{
    public class EncryptedConfig
    {
        private static readonly string tempKey = Path.Combine(AppContext.BaseDirectory, "ServiceConfig.TempKey");
        private static readonly string random = "Ys8A#QJowc#h#5te4QjumXv4aWYN9F";
        private static readonly string cred = "DBADash_Config_" + EncryptText.GetShortHash(AppContext.BaseDirectory.ToLowerInvariant());

        /// <summary>
        /// Current format: AES-GCM with a PBKDF2 key (see <see cref="ConfigKey"/>).  Null for the legacy format.
        /// </summary>
        public const int CurrentVersion = 2;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Version { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Salt { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Iterations { get; set; }

        public string ProtectedConfig { get; set; }

        public EncryptedConfig()
        {
        }

        public string GetConfig()
        {
            var password = GetPassword();
            return GetConfig(password);
        }

        public string GetConfig(string password) => GetConfig(password, out _);

        /// <param name="password">Config encryption password</param>
        /// <param name="key">Key used to decrypt the config.  Null if the config is in the legacy format.</param>
        public string GetConfig(string password, out ConfigKey key)
        {
            key = null;
            // Read from the file - an absurd value would hang the load in PBKDF2
            if (Version is not null && Iterations is < ConfigKey.MinIterations or > ConfigKey.MaxIterations)
            {
                throw new ConfigDecryptionError($"Invalid Iterations value {Iterations} in the encrypted config.  It should be between {ConfigKey.MinIterations} and {ConfigKey.MaxIterations}.  The config file might be corrupt.");
            }
            try
            {
                if (Version is null) return ProtectedConfig.DecryptString(password);
                key = ConfigKey.Derive(password, Convert.FromBase64String(Salt), Iterations ?? ConfigKey.DefaultIterations);
                return key.DecryptConfig(ProtectedConfig);
            }
            catch (Exception ex)
            {
                key = null;
                throw new ConfigDecryptionError("Failed to decrypt config.  Check that the password is correct", ex);
            }
        }

        public EncryptedConfig(string config, string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password), @"Password is not set");
            }
            SetConfig(config, ConfigKey.Create(password));
        }

        public EncryptedConfig(string config, ConfigKey key)
        {
            SetConfig(config, key);
        }

        public EncryptedConfig(string config) : this(config, GetPassword())
        {
        }

        private void SetConfig(string config, ConfigKey key)
        {
            Version = CurrentVersion;
            Salt = Convert.ToBase64String(key.Salt);
            Iterations = key.Iterations;
            ProtectedConfig = key.EncryptConfig(config);
        }

        public static string GetPassword()
        {
            string password = null;
            if (File.Exists(tempKey))
            {
                password = File.ReadAllText(tempKey);
                password = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? password.MachineDecryptString() : password.DecryptString(random);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                password = Creds.GetPassword(cred);
            }
            return password;
        }

        public static void SetPassword(string password, bool createTempKey)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new Exception("Password is required");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SavePassword(password);
            }
            if (createTempKey)
            {
                CreateTemporaryKey(password);
            }
        }

        public static void CreateTemporaryKey(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new Exception("Password is required");
            }
            var encryptedPass = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? password.MachineEncryptString()
                : password.EncryptString(random);
            File.WriteAllText(tempKey, encryptedPass);
        }

        public static void CreateTemporaryKey() => CreateTemporaryKey(GetPassword());

        [SupportedOSPlatform("windows")]
        public static void SavePassword(string password)
        {
            Creds.SetPassword(cred, password);
        }

        public static void ClearPassword()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Creds.Remove(cred);
            }
            if (File.Exists(tempKey))
            {
                File.Delete(tempKey);
            }
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static EncryptedConfig Deserialize(string config)
        {
            return JsonConvert.DeserializeObject<EncryptedConfig>(config);
        }

        public static bool HasTempKey => File.Exists(tempKey);

        [SupportedOSPlatform("windows")]
        public static bool ValidateKeyMatch()
        {
            var credsPwd = Creds.GetPassword(cred);
            var keyPassword = GetPassword();
            return credsPwd == keyPassword;
        }

        [SupportedOSPlatform("windows")]
        public static void DeleteTempKey(bool validate)
        {
            if (File.Exists(tempKey))
            {
                if (validate && !ValidateKeyMatch())
                {
                    throw new Exception("Password validation against temp key failed");
                }
                File.Delete(tempKey);
            }
        }
    }

    public class ConfigDecryptionError : Exception
    {
        public ConfigDecryptionError()
        {
        }

        public ConfigDecryptionError(string message)
            : base(message)
        {
        }

        public ConfigDecryptionError(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}