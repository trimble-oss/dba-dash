using DBADash;
using DBADashService;
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


        /// <summary>
        /// Runs the CLI expecting it to refuse, and returns nothing but the fact that it did.  The error
        /// paths call Environment.Exit(1), which <see cref="Helper.RunProcess"/> would not notice, so they
        /// are checked on the exit code the way the perfmon test above does.
        /// </summary>
        private static void AssertCliFails(string args, string because)
        {
            var psi = new ProcessStartInfo("DBADashConfig", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new Exception("Process is NULL");
            p.WaitForExit();
            Assert.AreEqual(1, p.ExitCode, because);
        }

        [TestMethod]
        [DataRow("DeadlockConn1", "Data Source=DeadlockConn1;Integrated Security=SSPI")]
        public void AddConnectionCaptureDeadlocksTest(string connectionID, string connectionString)
        {
            // --CaptureDeadlocks is the shorthand for the session DBA Dash creates and starts itself.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID} --CaptureDeadlocks"));

            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.AreEqual(DBADashSource.ManagedDeadlockXESessionName, conn.DeadlockXESessionName);
            Assert.IsTrue(conn.IsDeadlockCollectionEnabled);
            Assert.IsTrue(conn.IsDeadlockXESessionManaged, "The reserved name is the one DBA Dash may create");
            Assert.IsFalse(conn.FlushDeadlockXERingBuffer, "Flushing can lose a deadlock, so it is opt in");

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
        }

        [TestMethod]
        [DataRow("DeadlockConn2", "Data Source=DeadlockConn2;Integrated Security=SSPI")]
        public void AddConnectionDeadlockSessionNameTest(string connectionID, string connectionString)
        {
            // A named session is read and never altered - system_health included.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID} --DeadlockXESessionName system_health --FlushDeadlockXERingBuffer"));

            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.AreEqual(DBADashSource.SystemHealthXESessionName, conn.DeadlockXESessionName);
            Assert.IsTrue(conn.IsDeadlockCollectionEnabled);
            Assert.IsFalse(conn.IsDeadlockXESessionManaged, "A session DBA Dash did not create is read only");
            Assert.IsTrue(conn.FlushDeadlockXERingBuffer);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
        }

        [TestMethod]
        [DataRow("DeadlockConn3", "Data Source=DeadlockConn3;Integrated Security=SSPI")]
        public void AddConnectionDeadlockSessionNameWinsOverCaptureDeadlocksTest(string connectionID, string connectionString)
        {
            // Passing both is not a conflict: the shorthand names a session, and an explicit name says which.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID} --CaptureDeadlocks --DeadlockXESessionName MyOwnSession"));

            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.AreEqual("MyOwnSession", conn.DeadlockXESessionName);
            Assert.IsFalse(conn.IsDeadlockXESessionManaged);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
        }

        [TestMethod]
        [DataRow("DeadlockConn4", "Data Source=DeadlockConn4;Integrated Security=SSPI")]
        public void AddConnectionDeadlockDefaultIsOffTest(string connectionID, string connectionString)
        {
            // Deadlock collection is off unless asked for.  It reads an extended events session, so a default
            // of on would start reading one on every instance the moment the service was upgraded.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID}"));

            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.AreEqual(string.Empty, conn.DeadlockXESessionName);
            Assert.IsFalse(conn.IsDeadlockCollectionEnabled);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
        }

        [TestMethod]
        public void SetScheduleServiceLevelTest()
        {
            const string every5Min = "0 0/5 * * * ?";

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --CollectionType Deadlocks --Schedule \"{every5Min}\" --RunOnServiceStart true"));
            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.IsNotNull(cfg.CollectionSchedules);
            Assert.AreEqual(every5Min, cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule);
            Assert.IsTrue(cfg.CollectionSchedules[CollectionType.Deadlocks].RunOnServiceStart);

            // Only the collection named is written, so the rest still come from the shipped defaults.
            Assert.AreEqual(1, cfg.CollectionSchedules.Count, "One override, not a copy of every schedule");
            Assert.AreEqual(CollectionSchedules.DefaultSchedules[CollectionType.CPU].Schedule,
                cfg.GetSchedules()[CollectionType.CPU].Schedule, "Other collections keep their default");

            // RunOnServiceStart on its own keeps the schedule already in effect - writing a blank one would
            // read as disabled.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --RunOnServiceStart false"));
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(every5Min, cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule);
            Assert.IsFalse(cfg.CollectionSchedules[CollectionType.Deadlocks].RunOnServiceStart);

            // An empty schedule is how a collection is switched off, and is not the same as no override.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --Schedule \"\""));
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(string.Empty, cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule);

            // Clearing goes back to the shipped default rather than to nothing.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --ScheduleClear"));
            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.IsFalse(cfg.CollectionSchedules is { Count: > 0 }, "The last override leaves nothing behind");
            Assert.AreEqual(CollectionSchedules.DefaultSchedules[CollectionType.Deadlocks].Schedule,
                cfg.GetSchedules()[CollectionType.Deadlocks].Schedule);
        }

        [TestMethod]
        public void SetScheduleAcceptsIntervalInSecondsTest()
        {
            // The scheduler takes a whole number of seconds as well as a cron expression, so the CLI has to
            // accept one - rejecting it here would make a valid schedule unreachable from the command line.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --Schedule 30"));
            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual("30", cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --ScheduleClear"));
        }

        [TestMethod]
        [DataRow("ScheduleConn1", "Data Source=ScheduleConn1;Integrated Security=SSPI")]
        public void SetSchedulePerConnectionTest(string connectionID, string connectionString)
        {
            const string serviceLevel = "0 0/5 * * * ?";
            const string connectionLevel = "0 0/1 * * * ?";

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID} --CaptureDeadlocks"));
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --CollectionType Deadlocks --Schedule \"{serviceLevel}\""));

            // A fresh connection has no override and runs to the service level schedule.
            var conn = BasicConfig.Load<CollectionConfig>().SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNull(conn.CollectionSchedules, "New connection should inherit the service level schedule");

            // An override for this connection only.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule -c \"{connectionString}\" --CollectionType Deadlocks --Schedule \"{connectionLevel}\""));
            var cfg = BasicConfig.Load<CollectionConfig>();
            conn = cfg.SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNotNull(conn.CollectionSchedules);
            Assert.AreEqual(connectionLevel, conn.CollectionSchedules[CollectionType.Deadlocks].Schedule);
            Assert.AreEqual(serviceLevel, cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule,
                "The service level schedule is untouched");

            // The two levels combine the way the scheduler combines them: the override wins for this
            // collection, and everything else still comes from the service.
            var effective = CollectionSchedules.Combine(cfg.GetSchedules(), conn.CollectionSchedules);
            Assert.AreEqual(connectionLevel, effective[CollectionType.Deadlocks].Schedule);
            Assert.AreEqual(CollectionSchedules.DefaultSchedules[CollectionType.CPU].Schedule,
                effective[CollectionType.CPU].Schedule);

            // Back to inheriting.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule -c \"{connectionString}\" --CollectionType Deadlocks --ScheduleClear"));
            cfg = BasicConfig.Load<CollectionConfig>();
            conn = cfg.SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNull(conn.CollectionSchedules, "Clearing the last override removes it altogether");
            Assert.AreEqual(serviceLevel, cfg.CollectionSchedules[CollectionType.Deadlocks].Schedule,
                "Clearing a connection override leaves the service level schedule alone");

            // Cleanup
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --ScheduleClear"));
        }


        [TestMethod]
        public void SetScheduleClearLeavesOtherOverridesAloneTest()
        {
            // Clearing the last override drops the whole object, so the one case worth proving is that
            // clearing one of several does not: an override object holding nothing says nothing, but one
            // holding somebody else's schedule says a great deal.
            const string deadlocks = "0 0/5 * * * ?";
            const string cpu = "0 0/2 * * * ?";

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --CollectionType Deadlocks --Schedule \"{deadlocks}\""));
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --CollectionType CPU --Schedule \"{cpu}\""));

            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(2, cfg.CollectionSchedules.Count);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --ScheduleClear"));

            cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(1, cfg.CollectionSchedules.Count, "Only the collection named should be cleared");
            Assert.IsFalse(cfg.CollectionSchedules.ContainsKey(CollectionType.Deadlocks));
            Assert.AreEqual(cpu, cfg.CollectionSchedules[CollectionType.CPU].Schedule,
                "The other override must survive");

            // Cleanup
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType CPU --ScheduleClear"));
        }

        [TestMethod]
        public void SetScheduleClearWithNothingToClearIsHarmlessTest()
        {
            // Clearing something that was never overridden is not an error - it is the state the caller
            // asked for - and it must not rewrite the config to say so.
            const string cpu = "0 0/2 * * * ?";
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --CollectionType CPU --Schedule \"{cpu}\""));

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Deadlocks --ScheduleClear"));

            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.AreEqual(1, cfg.CollectionSchedules.Count);
            Assert.AreEqual(cpu, cfg.CollectionSchedules[CollectionType.CPU].Schedule);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType CPU --ScheduleClear"));
        }

        [TestMethod]
        [DataRow("ScheduleConn2", "Data Source=ScheduleConn2;Integrated Security=SSPI")]
        public void SetScheduleByConnectionIDTest(string connectionID, string connectionString)
        {
            // --ConnectionID is the other way to name a connection, and it resolves through a different
            // branch from -c.  Both have to reach the same source or an override lands on the service.
            const string connectionLevel = "0 0/1 * * * ?";

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a Add -c \"{connectionString}\" --SkipValidation true --ConnectionID {connectionID} --CaptureDeadlocks"));
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                $"-a SetSchedule --ConnectionID {connectionID} --CollectionType Deadlocks --Schedule \"{connectionLevel}\""));

            var cfg = BasicConfig.Load<CollectionConfig>();
            var conn = cfg.SourceConnections.Single(c => c.ConnectionID == connectionID);
            Assert.IsNotNull(conn.CollectionSchedules, "--ConnectionID should target the connection");
            Assert.AreEqual(connectionLevel, conn.CollectionSchedules[CollectionType.Deadlocks].Schedule);
            Assert.IsFalse(cfg.CollectionSchedules is { Count: > 0 },
                "The service level schedule should not have been touched");

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig", $"-a Remove --ConnectionID {connectionID}"));
        }

        [TestMethod]
        public void SetScheduleAcceptsLegacyCollectionTypeNameTest()
        {
            // The same legacy names the config file itself accepts, so a scripted call written before the
            // DriversWMI -> Drivers rename keeps working rather than failing as an unknown collection.
            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType DriversWMI --Schedule \"0 0 22 * * ?\""));

            var cfg = BasicConfig.Load<CollectionConfig>();
            Assert.IsTrue(cfg.CollectionSchedules.ContainsKey(CollectionType.Drivers),
                "The legacy name should be stored under the current member");
            Assert.AreEqual("0 0 22 * * ?", cfg.CollectionSchedules[CollectionType.Drivers].Schedule);

            Helper.RunProcess(new ProcessStartInfo("DBADashConfig",
                "-a SetSchedule --CollectionType Drivers --ScheduleClear"));
        }

        [TestMethod]
        public void SetScheduleInvalidInputFailsTest()
        {
            // Compared against what was there rather than against nothing: these tests share one config
            // file, so an absolute assertion would report a second failure for an earlier test's fault.
            var before = Helper.GetConfigJson();

            // A schedule the scheduler cannot parse would fail the service on start rather than here, which
            // is a long way from the mistake.
            AssertCliFails("-a SetSchedule --CollectionType Deadlocks --Schedule \"not a cron\"",
                "An unparseable schedule should be rejected");

            AssertCliFails("-a SetSchedule --CollectionType NotACollection --Schedule \"0 0/5 * * * ?\"",
                "An unknown collection type should be rejected");

            AssertCliFails("-a SetSchedule --Schedule \"0 0/5 * * * ?\"",
                "--CollectionType is required");

            AssertCliFails("-a SetSchedule --CollectionType Deadlocks",
                "Naming a collection without saying anything about its schedule does nothing");

            AssertCliFails("-a SetSchedule --CollectionType Deadlocks --ScheduleClear --Schedule \"0 0/5 * * * ?\"",
                "Clearing and setting at once is a contradiction");

            // None of the refusals should have written anything.
            Assert.AreEqual(before, Helper.GetConfigJson(), "A refused change must not be saved");
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