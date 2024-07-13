using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace DBADash
{
    public static class EncryptText
    {
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string initVector = "pemgail9uzpgzl88";

        // This constant is used to determine the keysize of the encryption algorithm
        private const int keysize = 256;

        //Encrypt
        public static string EncryptString(this string plainText, string passPhrase)
        {
            var initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (PasswordDeriveBytes password = new(passPhrase, null))
            {
                var keyBytes = password.GetBytes(keysize / 8);
                using (var symmetricKey = Aes.Create())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                    using (var memoryStream = new MemoryStream())
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        var cipherTextBytes = memoryStream.ToArray();
                        return Convert.ToBase64String(cipherTextBytes);
                    }
                }
            }
        }

        //Decrypt
        public static string DecryptString(this string cipherText, string passPhrase)
        {
            var initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            var cipherTextBytes = Convert.FromBase64String(cipherText);
            using (PasswordDeriveBytes password = new(passPhrase, null))
            {
                var keyBytes = password.GetBytes(keysize / 8);
                using (var symmetricKey = Aes.Create())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    using (MemoryStream memoryStream = new(cipherTextBytes))
                    using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new(cryptoStream))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        [SupportedOSPlatform("windows")]
        public static string EncryptString(this string value, DataProtectionScope scope)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (string.IsNullOrEmpty(value)) return value;
                var data = Encoding.Unicode.GetBytes(value);
                var encryptedData = ProtectedData.Protect(data, null, scope);
                return Convert.ToBase64String(encryptedData);
            }
            else
            {
                return value;
            }
        }

        [SupportedOSPlatform("windows")]
        public static string DecryptString(this string value, DataProtectionScope scope)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (string.IsNullOrEmpty(value)) return value;
                var encryptedData = Convert.FromBase64String(value);
                var data = ProtectedData.Unprotect(encryptedData, null, scope);
                return Encoding.Unicode.GetString(data);
            }
            else
            {
                return value;
            }
        }

        [SupportedOSPlatform("windows")]
        public static string UserEncryptString(this string value)
        {
            return EncryptString(value, DataProtectionScope.CurrentUser);
        }

        [SupportedOSPlatform("windows")]
        public static string UserDecryptString(this string value)
        {
            return DecryptString(value, DataProtectionScope.CurrentUser);
        }

        [SupportedOSPlatform("windows")]
        public static string MachineEncryptString(this string value)
        {
            return EncryptString(value, DataProtectionScope.LocalMachine);
        }

        [SupportedOSPlatform("windows")]
        public static string MachineDecryptString(this string value)
        {
            return DecryptString(value, DataProtectionScope.LocalMachine);
        }

        public static string GetHash(string input)=>GetShortHash(input, 64);

        public static string GetShortHash(string input, int length = 8)
        {
            if (length is < 0 or > 64) // SHA256 produces a 64-character hex string
            {
                throw new ArgumentOutOfRangeException(nameof(length), @"Length must be between 0 and 64.");
            }
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            var builder = new StringBuilder(length); // Initialize StringBuilder with the exact length needed
            for (var i = 0; i < bytes.Length && builder.Length < length; i++)
            {
                // Append each byte as a two-character hexadecimal string
                builder.Append(bytes[i].ToString("x2"));
                if (builder.Length > length) // If appending the last byte exceeds the desired length
                {
                    builder.Length = length; // Truncate the StringBuilder to the desired length
                }
            }

            return builder.ToString();
        }
    }
}