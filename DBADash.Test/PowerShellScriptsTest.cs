using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Diagnostics;
using System.IO;
using static DBADashConfig.Test.Helper;
namespace DBADashConfig.Test
{

    [TestClass]
    public class PowerShellScriptsTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Assert.IsFalse(File.Exists(Initialize.ServiceConfigPath));
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
            string dest = builder.ToString();
            var psi = new ProcessStartInfo("powershell", String.Format("-File \"Set-DBADashDestination.ps1\" -ConnectionString \"{0}\" -SkipValidation", dest));
            Helper.RunProcess(psi);

            string json = Helper.GetConfigJson();
            var cfg = DBADash.CollectionConfig.Deserialize(json);
            Console.WriteLine(cfg.DestinationConnection.ConnectionString);
            Console.WriteLine(dest);
            Assert.IsTrue(cfg.DestinationConnection.ConnectionString == builder.ToString(), "Destination connection doesn't match what was specified");

            Console.WriteLine(json);
            Assert.IsTrue(json.Contains(builder.DataSource), "Json should contain data source");
            Assert.IsFalse(json.Contains(builder.Password), "Plain text password found in json");
        }


        [TestMethod]
        [DataRow(-1, false, int.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, "")]
        [DataRow(999, true, 2, 1000, 1001, 8000, "*")]
        [DataRow(2000, true, 1, 2000, 2001, 6400, "DB1,DB2,DB3")]
        public void AddDBADashSourceTest(int slowQueryThreshold, bool planCollectionEnabled, int planCollectionCountThreshold, int planCollectionCPUThreshold, int planCollectionDurationThreshold, int PlanCollectionMemoryGrantThreshold, string schemaSnapshotDBs)
        {
            // Add connection
            var cnt = Helper.GetConnectionCount();
            var builder = GetRandomConnectionString();
            string source = builder.ToString();
            var psi = new ProcessStartInfo("powershell", String.Format("-File \"Add-DBADashSource.ps1\" -ConnectionString \"{0}\" --SkipValidation", source));
            RunProcess(psi);

            // Read the new config from disk
            var json = GetConfigJson();
            Console.WriteLine(json);
            Assert.IsTrue(json.Contains(builder.DataSource), "Json should contain data source");
            Assert.IsFalse(json.Contains(builder.Password), "Plain text password found in json");

            // Convert config to CollectionConfig object for additional checks
            var cfg = DBADash.CollectionConfig.Deserialize(json);
            var newCnt = cfg.SourceConnections.Count;
            Assert.IsTrue(newCnt == cnt + 1, string.Format("Expected {0} source connections instead of {1}", cnt + 1, newCnt));
            var src = cfg.GetSourceFromConnectionString(builder.ConnectionString);
            Assert.IsTrue(src.NoWMI == false, "source expected NoWMI=false");

            // Update the connection and validate it's been updated
            builder.Password = Guid.NewGuid().ToString();
            psi = new ProcessStartInfo("powershell", String.Format("-File \"Add-DBADashSource.ps1\" -ConnectionString \"{0}\" -SkipValidation --Replace -NoWMI -SlowQueryThresholdMs {1}", source, slowQueryThreshold));
            if (!string.IsNullOrEmpty(schemaSnapshotDBs))
            {
                psi.Arguments += String.Format(" -SchemaSnapshotDBs \"{0}\"", schemaSnapshotDBs);
            }
            if (planCollectionEnabled)
            {
                psi.Arguments += " -PlanCollectionEnabled";
                psi.Arguments += string.Format(" -PlanCollectionCountThreshold {0}", planCollectionCountThreshold);
                psi.Arguments += string.Format(" -PlanCollectionCPUThreshold {0}", planCollectionCPUThreshold);
                psi.Arguments += string.Format(" -PlanCollectionDurationThreshold {0}", planCollectionDurationThreshold);
                psi.Arguments += string.Format(" -PlanCollectionMemoryGrantThreshold {0}", PlanCollectionMemoryGrantThreshold);
            }

            RunProcess(psi);

            // Read config from disk again
            json = GetConfigJson();
            Console.WriteLine(json);
            cfg = DBADash.CollectionConfig.Deserialize(json);

            newCnt = cfg.SourceConnections.Count;
            // This time we should be replacing the existing connection - so the count will be the same
            Assert.IsTrue(newCnt == cnt + 1, string.Format("Expected {0} source connections instead of {1}", cnt + 1, newCnt));
            src = cfg.GetSourceFromConnectionString(builder.ConnectionString);
            Console.WriteLine(src.SlowQueryThresholdMs);
            // Check updates worked
            Assert.IsTrue(src.SlowQueryThresholdMs == slowQueryThreshold, "SlowQueryThresholdMs Test");
            Assert.IsTrue(src.NoWMI == true, "NoWMI Test");
            Assert.IsTrue(src.PlanCollectionEnabled == planCollectionEnabled, "PlanCollectionEnabled Test");
            Assert.IsTrue(src.PlanCollectionCountThreshold == planCollectionCountThreshold, "PlanCollectionCountThreshold Test");
            Assert.IsTrue(src.PlanCollectionCPUThreshold == planCollectionCPUThreshold, "PlanCollectionCountThreshold Test");
            Assert.IsTrue(src.PlanCollectionMemoryGrantThreshold == PlanCollectionMemoryGrantThreshold, "PlanCollectionMemoryGrantThreshold Test");
            if (string.IsNullOrEmpty(schemaSnapshotDBs))
            {
                Assert.IsTrue(string.IsNullOrEmpty(src.SchemaSnapshotDBs), "SchemaSnaspshotDBs Test");
            }
            else
            {
                Assert.IsTrue(src.SchemaSnapshotDBs == schemaSnapshotDBs, "SchemaSnaspshotDBs Test");
            }
        }

    }


}