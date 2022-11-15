using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DBADashConfig.Test
{
    public class DeInitialize
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (File.Exists(Initialize.ServiceConfigPath))
            {
                File.Delete(Initialize.ServiceConfigPath);
            }
        }
    }
}
