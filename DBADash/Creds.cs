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
        private static string userKeyFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash");

        [SupportedOSPlatform("windows")]
        public static void SetPassword(string target, string password)
        {
            string fileName = target + ".key";
            string filePath = Path.Combine(userKeyFolder, fileName);
            Directory.CreateDirectory(userKeyFolder);
            File.WriteAllText(filePath, password.UserEncryptString());
        }

        [SupportedOSPlatform("windows")]
        public static void Remove(string target)
        {
            string fileName = target + ".key";
            string filePath = Path.Combine(userKeyFolder, fileName);
            File.Delete(filePath);
        }

        [SupportedOSPlatform("windows")]
        public static string GetPassword(string target)
        {
            string fileName = target + ".key";
            string filePath = Path.Combine(userKeyFolder, fileName);
            if (File.Exists(filePath))
            {
                string password = File.ReadAllText(filePath);
                return password.UserDecryptString();
            }
            else
            {
                return null;
            }
        }
    }
}