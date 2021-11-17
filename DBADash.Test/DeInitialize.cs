using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
