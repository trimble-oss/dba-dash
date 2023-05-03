using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    internal static class Encryption
    {
        public static string UserEncryptString(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            byte[] data = Encoding.Unicode.GetBytes(value);
            byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static string UserDecryptString(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            byte[] encryptedData = Convert.FromBase64String(value);
            byte[] data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(data);
        }

        public static string MachineEncryptString(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            byte[] data = Encoding.Unicode.GetBytes(value);
            byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(encryptedData);
        }

        public static string MachineDecryptString(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            byte[] encryptedData = Convert.FromBase64String(value);
            byte[] data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
            return Encoding.Unicode.GetString(data);
        }
    }
}