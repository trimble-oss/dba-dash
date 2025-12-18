using DBADash;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            var cfg = CollectionConfig.Deserialize(json);
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
                "-a Decrypt");
            Helper.RunProcess(psi);

            Assert.IsFalse(BasicConfig.IsConfigFileEncrypted(), "Config file should not be encrypted");
            File.Delete("ServiceConfig.TempKey");
        }

        [TestMethod]
        [DataRow("SQL1", "Data Source=SQL1;Integrated Security=SSPI", false, false, true, false, true, false, true, false, true)]
        [DataRow("SQL2", "Data Source=SQL2;Integrated Security=SSPI", true, false, false, true, false, true, false, true, true)]
        [DataRow("SQL3", "Data Source=SQL3;Integrated Security=SSPI", false, true, false, false, true, true, false, true, false)]
        public void AddConnectionTest(string connectionID, string connectionString, bool persistXE, bool useDualSession, bool tempdb, bool tranBeginTime, bool collectSessionWaits, bool writeToSecondaryDest, bool scriptAgentJobs, bool taskWaits, bool collectCursors)
        {
            const bool skipValidation = true;
            var args =
                $"-a Add -c \"{connectionString}\" --PersistXESessions {persistXE} --UseDualEventSession {useDualSession} --SkipValidation {skipValidation} --ConnectionID {connectionID} --CollectTranBeginTime {tranBeginTime} --CollectTempDB {tempdb} --WriteToSecondaryDestinations {writeToSecondaryDest} --ScriptAgentJobs {scriptAgentJobs} --CollectTaskWaits {taskWaits} --CollectCursors {collectCursors}";
            if (!collectSessionWaits)
            {
                args += " --NoCollectSessionWaits";
            }
            var psi = new ProcessStartInfo("DBADashConfig", args);
            Helper.RunProcess(psi);
            var json = Helper.GetConfigJson();
            var cfg = BasicConfig.Load<CollectionConfig>();

            var conn = cfg.SourceConnections.FirstOrDefault(c => c.ConnectionID == connectionID);
            Assert.IsTrue(cfg.SourceConnections.Any(c => c.ConnectionID == connectionID), "Test Connection exists");
            Assert.IsNotNull(conn);
            Assert.AreEqual(conn.UseDualEventSession, useDualSession, "Test UseDualEventSession");
            Assert.AreEqual(conn.PersistXESessions, persistXE, "Test PersistXESessions");
            Assert.AreEqual(conn.CollectTempDB, tempdb, "Test CollectTempDB");
            Assert.AreEqual(conn.CollectTranBeginTime, tranBeginTime, "Test CollectTranBeginTime");
            Assert.AreEqual(conn.CollectSessionWaits, collectSessionWaits, "Test CollectSessionWaits");
            Assert.AreEqual(conn.WriteToSecondaryDestinations, writeToSecondaryDest, "Test WriteToSecondaryDestinations");
            Assert.AreEqual(conn.ScriptAgentJobs, scriptAgentJobs, "Test ScriptAgentJobs");
            Assert.AreEqual(conn.CollectTaskWaits, taskWaits, "Test CollectTaskWaits");
            Assert.AreEqual(conn.CollectCursors, collectCursors, "Test CollectCursors");

            // test removal
            args = $"-a Remove --ConnectionID {connectionID}";
            psi = new ProcessStartInfo("DBADashConfig", args);
            Helper.RunProcess(psi);
            json = Helper.GetConfigJson();
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.IsFalse(cfg.SourceConnections.Any(c => c.ConnectionID == connectionID), "Test connection doesn't exist after removal");
        }

        [TestMethod]
        [DataRow("SQL4", "Data Source=SQL4;Integrated Security=SSPI")]
        public void AddConnectionTestDefaults(string connectionID, string connectionString)
        {
            const bool skipValidation = true;
            var psi = new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation {skipValidation} --ConnectionID {connectionID}");
            Helper.RunProcess(psi);
            var json = Helper.GetConfigJson();
            var cfg = BasicConfig.Load<CollectionConfig>();

            var conn = cfg.SourceConnections.FirstOrDefault(c => c.ConnectionID == connectionID);
            Assert.IsNotNull(conn);
            Assert.AreEqual(conn.UseDualEventSession, true);
            Assert.AreEqual(conn.PersistXESessions, false);
        }

        [TestMethod]
        [DataRow(null, "TestAccessKey", "TestSecretKey")]
        [DataRow("default", null, null)]
        public void SetAWS(string profile, string accessKey, string secretKey)
        {
            var psi = new ProcessStartInfo("DBADashConfig",
                $"-a SetAWS --AWSProfile \"{profile}\" --AWSAccessKey \"{accessKey}\" --AWSSecretKey \"{secretKey}\"");
            Helper.RunProcess(psi);
            var json = Helper.GetConfigJson();
            var cfg = BasicConfig.Load<CollectionConfig>();

            Assert.AreEqual(cfg.AWSProfile, profile);
            Assert.AreEqual(cfg.AccessKey, accessKey);
            Assert.AreEqual(cfg.GetSecretKey(), secretKey);
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

        [TestMethod]
        [DataRow(new[] { "C:\\Test", "C:\\Test2", "C:\\Test3" })]
        public void AddRemoveDestinationTest(string[] connectionStrings)
        {
            foreach (var connectionString in connectionStrings)
            {
                var cfg = BasicConfig.Load<CollectionConfig>();
                var expectedCnt = cfg.AllDestinations.Count + 1;
                var psi = new ProcessStartInfo("DBADashConfig",
                    $"-a AddDestination -c \"{connectionString}\" --SkipValidation");
                Helper.RunProcess(psi);

                cfg = BasicConfig.Load<CollectionConfig>();
                Assert.AreEqual(expectedCnt, cfg.AllDestinations.Count);
            }

            foreach (var connectionString in connectionStrings)
            {
                var cfg = BasicConfig.Load<CollectionConfig>();
                var expectedCnt = cfg.AllDestinations.Count - 1;
                var psi = new ProcessStartInfo("DBADashConfig",
                    $"-a RemoveDestination -c \"{connectionString}\"");
                Helper.RunProcess(psi);
                cfg = BasicConfig.Load<CollectionConfig>();
                Assert.AreEqual(expectedCnt, cfg.AllDestinations.Count);
            }
        }
    }
}