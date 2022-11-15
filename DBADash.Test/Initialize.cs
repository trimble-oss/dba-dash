using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;

namespace DBADashConfig.Test
{
    [TestClass]
    public class Initialize
    {
        public static string AppPath = string.Empty;
        public static string ServiceConfigPath = string.Empty;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Console.WriteLine("Begin Assembly Initialize");
            AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            ServiceConfigPath = Path.Combine(AppPath, "ServiceConfig.json");
            if (File.Exists(ServiceConfigPath))
            {
                Console.WriteLine("Delete existing config");
                File.Delete(ServiceConfigPath);
            }
        }
    }
}
