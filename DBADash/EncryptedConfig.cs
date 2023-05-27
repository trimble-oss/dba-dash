using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Newtonsoft.Json;

namespace DBADash
{
    public class EncryptedConfig
    {
        private static readonly string tempKey = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.TempKey");
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
                throw new AggregateException("Failed to decrypt config.  Check that the password is correct", ex);
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
                throw new ArgumentNullException("Password", "Password is not set");
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SavePassword(password);
            }
            if (createTempKey)
            {
                var encryptedPass = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? password.MachineEncryptString()
                    : password.EncryptString(random);
                File.WriteAllText(tempKey, encryptedPass);
            }
        }

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
}