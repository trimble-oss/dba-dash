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
        [DataRow("SQL1", "Data Source=SQL1;Integrated Security=SSPI", false, false, true, false, true, false, true, false, true, -1, "", -1, -1)]
        [DataRow("SQL2", "Data Source=SQL2;Integrated Security=SSPI", true, false, false, true, false, true, false, true, true, 100, "master", 10, 2)]
        [DataRow("SQL3", "Data Source=SQL3;Integrated Security=SSPI", false, true, false, false, true, true, false, true, false, 50, "db1,db2", 100, 5)]
        public void AddConnectionTest(string connectionID, string connectionString, bool persistXE, bool useDualSession, bool tempdb, bool tranBeginTime, bool collectSessionWaits, bool writeToSecondaryDest, bool scriptAgentJobs, bool taskWaits, bool collectCursors, int tableSizeCollectionThresholdMB, string tableSizeDatabases, int tableSizeMaxTableThreshold, int tableSizeMaxDatabaseThreshold)
        {
            const bool skipValidation = true;
            var args =
                $"-a Add -c \"{connectionString}\" --PersistXESessions {persistXE} --UseDualEventSession {useDualSession} --SkipValidation {skipValidation} --ConnectionID {connectionID} --CollectTranBeginTime {tranBeginTime} --CollectTempDB {tempdb} --WriteToSecondaryDestinations {writeToSecondaryDest} --ScriptAgentJobs {scriptAgentJobs} --CollectTaskWaits {taskWaits} --CollectCursors {collectCursors}";
            if (!collectSessionWaits)
            {
                args += " --NoCollectSessionWaits";
            }
            if (tableSizeCollectionThresholdMB != -1)
            {
                args += $" --TableSizeCollectionThresholdMB {tableSizeCollectionThresholdMB}";
            }
            if (!string.IsNullOrEmpty(tableSizeDatabases))
            {
                args += $" --TableSizeDatabases \"{tableSizeDatabases}\"";
            }
            if (tableSizeMaxTableThreshold != -1)
            {
                args += $" --TableSizeMaxTableThreshold {tableSizeMaxTableThreshold}";
            }
            if (tableSizeMaxDatabaseThreshold != -1)
            {
                args += $" --TableSizeMaxDatabaseThreshold {tableSizeMaxDatabaseThreshold}";
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
            Assert.AreEqual(conn.TableSizeCollectionThresholdMB, tableSizeCollectionThresholdMB == -1 ? null : (int?)tableSizeCollectionThresholdMB, "Test TableSizeCollectionThresholdMB");
            // Treat null and empty string as equivalent for TableSizeDatabases
            Assert.AreEqual(tableSizeDatabases ?? string.Empty, conn.TableSizeDatabases ?? string.Empty, "Test TableSizeDatabases");
            Assert.AreEqual(conn.TableSizeMaxTableThreshold, tableSizeMaxTableThreshold == -1 ? null : (int?)tableSizeMaxTableThreshold, "Test TableSizeMaxTableThreshold");
            Assert.AreEqual(conn.TableSizeMaxDatabaseThreshold, tableSizeMaxDatabaseThreshold == -1 ? null : (int?)tableSizeMaxDatabaseThreshold, "Test TableSizeMaxDatabaseThreshold");

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
            Assert.AreEqual(true, conn.UseDualEventSession);
            Assert.AreEqual(false, conn.PersistXESessions);
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
        public void SetPerfmonCountersGlobalTest()
        {
            var defaultCount = PerfmonCounter.DefaultCounters().Count;

            // Enable defaults
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", "-a SetPerfmonCounters --PerfmonDefaults"));
            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(defaultCount, cfg.PerfmonCounters.Count, "Defaults should populate the global list");

            // Add a specific counter on top of the defaults
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetPerfmonCounters --PerfmonDefaults --PerfmonCounters \"Win32_PerfRawData_PerfProc_Process:HandleCount:_Total\""));
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(defaultCount + 1, cfg.PerfmonCounters.Count, "Specific counter should be added on top of defaults");
            var added = cfg.PerfmonCounters.Single(c => c.WmiProperty == "HandleCount");
            Assert.AreEqual("Win32_PerfRawData_PerfProc_Process", added.WmiClass);
            Assert.AreEqual("_Total", added.InstanceName);
            Assert.AreEqual(0, added.CounterType, "Counter type is resolved by the collector, not stored by the CLI");

            // Clear
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", "-a SetPerfmonCounters --PerfmonClear"));
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(0, cfg.PerfmonCounters.Count, "Clear should empty the global list");
        }

        [TestMethod]
        public void SetPerfmonCountersInvalidSpecFailsTest()
        {
            // A non-raw class must be rejected: the CLI logs the error and exits with a non-zero code.
            var psi = new ProcessStartInfo("DBADashConfig",
                "-a SetPerfmonCounters --PerfmonCounters \"Win32_PerfFormattedData_PerfOS_Processor:PercentProcessorTime\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new Exception("Process is NULL");
            p.WaitForExit();
            Assert.AreEqual(1, p.ExitCode, "Invalid (non-raw) counter class should fail with a non-zero exit code");
        }

        [TestMethod]
        [DataRow("PerfmonConn1", "Data Source=PerfmonConn1;Integrated Security=SSPI")]
        public void SetPerfmonCountersPerConnectionTest(string connectionID, string connectionString)
        {
            var defaultCount = PerfmonCounter.DefaultCounters().Count;

            // Arrange: add a source connection to attach the override to.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID}"));

            // A fresh connection inherits the global list (null override).
            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNull(conn.PerfmonCounters, "New connection should inherit (null) by default");

            // Custom override: defaults for this connection only.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetPerfmonCounters -c \"{connectionString}\" --PerfmonDefaults"));
            conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNotNull(conn.PerfmonCounters);
            Assert.AreEqual(defaultCount, conn.PerfmonCounters.Count, "Per-connection defaults");

            // Disabled for this instance (empty, not null).
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetPerfmonCounters -c \"{connectionString}\" --PerfmonClear"));
            conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNotNull(conn.PerfmonCounters, "Clear should be empty, not null (disabled != inherit)");
            Assert.AreEqual(0, conn.PerfmonCounters.Count);

            // Back to inherit (null).
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetPerfmonCounters -c \"{connectionString}\" --PerfmonInherit"));
            conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNull(conn.PerfmonCounters, "Inherit should reset the override to null");

            // Cleanup
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
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