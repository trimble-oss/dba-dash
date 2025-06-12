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

        public string ProtectedConfig { get; set; }

        public EncryptedConfig()
        {
        }

        public string GetConfig()
        {
            var password = GetPassword();
            return GetConfig(password);
        }

        public string GetConfig(string password)
        {
            try
            {
                return ProtectedConfig.DecryptString(password);
            }
            catch (Exception ex)
            {
                throw new ConfigDecryptionError("Failed to decrypt config.  Check that the password is correct", ex);
            }
        }

        public EncryptedConfig(string config, string password)
        {
            SetConfig(config, password);
        }

        public EncryptedConfig(string config)
        {
            var password = GetPassword();
            SetConfig(config, password);
        }

        private void SetConfig(string config, string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password), @"Password is not set");
            }
            ProtectedConfig = config.EncryptString(password);
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