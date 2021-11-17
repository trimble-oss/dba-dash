using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBADashConfig.Test
{
    [TestClass]
    public class Initialize
    {
        public static string AppPath = String.Empty;
        public static string ServiceConfigPath = String.Empty;

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
