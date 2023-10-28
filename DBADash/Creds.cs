using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace DBADash

{
    internal class Creds
    {
        private static string UserKeyFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash");

        [SupportedOSPlatform("windows")]
        public static void SetPassword(string target, string password)
        {
            var fileName = target + ".key";
            var filePath = Path.Combine(UserKeyFolder, fileName);
            Directory.CreateDirectory(UserKeyFolder);
            File.WriteAllText(filePath, password.UserEncryptString());
        }

        [SupportedOSPlatform("windows")]
        public static void Remove(string target)
        {
            var fileName = target + ".key";
            var filePath = Path.Combine(UserKeyFolder, fileName);
            File.Delete(filePath);
        }

        [SupportedOSPlatform("windows")]
        public static string GetPassword(string target)
        {
            var fileName = target + ".key";
            var filePath = Path.Combine(UserKeyFolder, fileName);
            if (File.Exists(filePath))
            {
                var password = File.ReadAllText(filePath);
                return password.UserDecryptString();
            }
            else
            {
                return null;
            }
        }
    }
}