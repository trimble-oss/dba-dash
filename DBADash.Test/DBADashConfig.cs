using DBADashConfig.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADash;

namespace DBADashConfig.Test
{
    [TestClass]
    public class DBADashConfigTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Helper.CleanupConfig();
            Assert.IsFalse(File.Exists(Helper.ServiceConfigPath));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Console.WriteLine("Inside ClassCleanup");
            BasicConfig.ClearOldConfigBackups(1);
        }

        [TestMethod]
        [DataRow(5)]
        [DataRow(60)]
        public void SetSetConfigFileBackupRetentionTest(int RetentionDays)
        {
            var psi = new ProcessStartInfo("DBADashConfig",
                $"-a SetConfigFileBackupRetention --RetentionDays {RetentionDays}");
            Helper.RunProcess(psi);

            var json = Helper.GetConfigJson();
            var cfg = DBADash.CollectionConfig.Deserialize(json);
            Assert.AreEqual(RetentionDays, cfg.ConfigBackupRetentionDays);
        }

        [TestMethod]
        [DataRow("Test Encryption!")]
        public void EncryptTest(string password)
        {
            var psi = new ProcessStartInfo("DBADashConfig",
                $"-a Encrypt --EncryptionPassword \"{password}\"");
            Helper.RunProcess(psi);
            var json = Helper.GetConfigJson();
            Assert.IsTrue(BasicConfig.IsConfigFileEncrypted(), "Config file should be encrypted");

            psi = new ProcessStartInfo("DBADashConfig",
                $"-a Decrypt");
            Helper.RunProcess(psi);

            Assert.IsFalse(BasicConfig.IsConfigFileEncrypted(), "Config file should not be encrypted");
            File.Delete("ServiceConfig.TempKey");
        }

        [TestMethod]
        [DataRow("DBADashUnitTest")]
        public void ServiceNameTest(string serviceName)
        {
            var psi = new ProcessStartInfo("DBADashConfig",
                $"-a SetServiceName --ServiceName \"{serviceName}\"");
            Helper.RunProcess(psi);
            var json = Helper.GetConfigJson();

            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(serviceName, cfg.ServiceName);
        }
    }
}