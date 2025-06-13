using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DBADashConfig.Test
{
    [TestClass]
    public class DeInitialize
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (File.Exists(Helper.ServiceConfigPath))
            {
                File.Delete(Helper.ServiceConfigPath);
            }
        }
    }
}