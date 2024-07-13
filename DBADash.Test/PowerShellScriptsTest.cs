using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Diagnostics;
using System.IO;
using DBADash;
using static DBADashConfig.Test.Helper;

namespace DBADashConfig.Test
{
    [DoNotParallelize]
    [TestClass]
    public class PowerShellScriptsTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            CleanupConfig();
            Assert.IsFalse(File.Exists(ServiceConfigPath));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Console.WriteLine("Inside ClassCleanup");
        }

        [TestMethod]
        public void SetDBADashDestinationTest()
        {
            var builder = GetRandomConnectionString();
            var dest = builder.ToString();
            var psi = new ProcessStartInfo("powershell",
                $"-File \"Set-DBADashDestination.ps1\" -ConnectionString \"{dest}\" -SkipValidation");
            RunProcess(psi);

            var json = GetConfigJson();
            var cfg = CollectionConfig.Deserialize(json);
            Console.WriteLine(cfg.DestinationConnection.ConnectionString);
            Console.WriteLine(dest);
            Assert.IsTrue(cfg.DestinationConnection.ConnectionString == builder.ToString(), "Destination connection doesn't match what was specified");

            Console.WriteLine(json);
            Assert.IsTrue(json.Contains(builder.DataSource), "Json should contain data source");
            Assert.IsFalse(json.Contains(builder.Password), "Plain text password found in json");
        }

        [TestMethod]
        [DataRow(-1, false, int.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, "", 1)]
        [DataRow(999, true, 2, 1000, 1001, 8000, "*", 2)]
        [DataRow(2000, true, 1, 2000, 2001, 6400, "DB1,DB2,DB3", 3)]
        public void AddDBADashSourceTest(int slowQueryThreshold, bool planCollectionEnabled, int planCollectionCountThreshold, int planCollectionCPUThreshold, int planCollectionDurationThreshold, int PlanCollectionMemoryGrantThreshold, string schemaSnapshotDBs, DBADashSource.IOCollectionLevels IOCollectionLevel)
        {
            // Add connection
            var cnt = GetConnectionCount();
            var builder = GetRandomConnectionString();
            var source = builder.ToString();
            var psi = new ProcessStartInfo("powershell",
                $"-File \"Add-DBADashSource.ps1\" -ConnectionString \"{source}\" --SkipValidation");
            RunProcess(psi);

            // Read the new config from disk
            var json = GetConfigJson();
            Console.WriteLine(json);
            Assert.IsTrue(json.Contains(builder.DataSource), "Json should contain data source");
            Assert.IsFalse(json.Contains(builder.Password), "Plain text password found in json");

            // Convert config to CollectionConfig object for additional checks
            var cfg = CollectionConfig.Deserialize(json);
            var newCnt = cfg.SourceConnections.Count;
            Assert.IsTrue(newCnt == cnt + 1, $"Expected {cnt + 1} source connections instead of {newCnt}");
            var src = cfg.GetSourceFromConnectionString(builder.ConnectionString);
            Assert.IsTrue(src.NoWMI == false, "source expected NoWMI=false");

            // Update the connection and validate it's been updated
            builder.Password = Guid.NewGuid().ToString();
            psi = new ProcessStartInfo("powershell",
                $"-File \"Add-DBADashSource.ps1\" -ConnectionString \"{source}\" -SkipValidation --Replace -NoWMI -SlowQueryThresholdMs {slowQueryThreshold}");
            if (!string.IsNullOrEmpty(schemaSnapshotDBs))
            {
                psi.Arguments += $" -SchemaSnapshotDBs \"{schemaSnapshotDBs}\"";
            }
            if (planCollectionEnabled)
            {
                psi.Arguments += " -PlanCollectionEnabled";
                psi.Arguments += $" -PlanCollectionCountThreshold {planCollectionCountThreshold}";
                psi.Arguments += $" -PlanCollectionCPUThreshold {planCollectionCPUThreshold}";
                psi.Arguments += $" -PlanCollectionDurationThreshold {planCollectionDurationThreshold}";
                psi.Arguments += $" -PlanCollectionMemoryGrantThreshold {PlanCollectionMemoryGrantThreshold}";
            }

            psi.Arguments += $" -IOCollectionLevel {(int)IOCollectionLevel}";
            RunProcess(psi);

            // Read config from disk again
            json = GetConfigJson();
            Console.WriteLine(json);
            cfg = CollectionConfig.Deserialize(json);

            newCnt = cfg.SourceConnections.Count;
            // This time we should be replacing the existing connection - so the count will be the same
            Assert.IsTrue(newCnt == cnt + 1, $"Expected {cnt + 1} source connections instead of {newCnt}");
            src = cfg.GetSourceFromConnectionString(builder.ConnectionString);
            Console.WriteLine(src.SlowQueryThresholdMs);
            // Check updates worked
            Assert.IsTrue(src.SlowQueryThresholdMs == slowQueryThreshold, "SlowQueryThresholdMs Test");
            Assert.IsTrue(src.NoWMI, "NoWMI Test");
            Assert.IsTrue(src.PlanCollectionEnabled == planCollectionEnabled, "PlanCollectionEnabled Test");
            Assert.IsTrue(src.PlanCollectionCountThreshold == planCollectionCountThreshold, "PlanCollectionCountThreshold Test");
            Assert.IsTrue(src.PlanCollectionCPUThreshold == planCollectionCPUThreshold, "PlanCollectionCountThreshold Test");
            Assert.IsTrue(src.PlanCollectionMemoryGrantThreshold == PlanCollectionMemoryGrantThreshold, "PlanCollectionMemoryGrantThreshold Test");
            Assert.IsTrue(src.IOCollectionLevel == IOCollectionLevel, "IOCollectionLevel Test");
            if (string.IsNullOrEmpty(schemaSnapshotDBs))
            {
                Assert.IsTrue(string.IsNullOrEmpty(src.SchemaSnapshotDBs), "SchemaSnapshotDBs Test");
            }
            else
            {
                Assert.IsTrue(src.SchemaSnapshotDBs == schemaSnapshotDBs, "SchemaSnapshotDBs Test");
            }
        }
    }
}