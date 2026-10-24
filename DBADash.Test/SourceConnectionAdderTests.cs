using DBADashService;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// Adding source connections - the options shared by the config tool and the GUI, and the check/apply steps.  The checks
    /// skip connecting to the instance (validate: false), so no SQL instance is needed.
    /// </summary>
    [TestClass]
    public class SourceConnectionAdderTests
    {
        private static CollectionConfig NewConfig() => new() { Destination = "Data Source=Repo;Integrated Security=True;Initial Catalog=DBADashDB" };

        [TestMethod]
        public void GetConnectionString_BuildsWindowsAuthForInstanceName()
        {
            var cs = SourceConnectionOptions.GetConnectionString("SQL1\\INST");
            StringAssert.Contains(cs, "Data Source=SQL1\\INST");
            StringAssert.Contains(cs, "Integrated Security=True");
            StringAssert.Contains(cs, "Application Name=DBADash");

            const string full = "Data Source=SQL1;User ID=dba;Password=x";
            Assert.AreEqual(full, SourceConnectionOptions.GetConnectionString(full));
            Assert.AreEqual("\\\\server\\share", SourceConnectionOptions.GetConnectionString("\\\\server\\share"));
        }

        [TestMethod]
        public void CreateSource_AppliesOptionsAndRoundTrips()
        {
            var options = new SourceConnectionOptions
            {
                NoWMI = true,
                SlowQueryThresholdMs = 2000,
                DeadlockXESessionName = DBADashSource.SystemHealthXESessionName,
                QueryStatsTopN = 25,
                QueryStatsPlansPerCollection = 10,
                SchemaSnapshotDBs = "DB1",
                SlowQueryXESessionName = string.Empty,
                CollectCursors = true,
                IOCollectionLevel = DBADashSource.IOCollectionLevels.Drive,
                RunningQueryPlanThreshold = PlanCollectionThreshold.DefaultThreshold
            };
            var src = options.CreateSource("SQL1");

            Assert.IsTrue(src.NoWMI);
            Assert.AreEqual(2000, src.SlowQueryThresholdMs);
            Assert.AreEqual(DBADashSource.SystemHealthXESessionName, src.DeadlockXESessionName);
            Assert.AreEqual(25, src.QueryStatsTopN);
            Assert.AreEqual("DB1", src.SchemaSnapshotDBs);
            Assert.IsTrue(src.CollectCursors);
            Assert.AreEqual(DBADashSource.IOCollectionLevels.Drive, src.IOCollectionLevel);

            var back = SourceConnectionOptions.FromSource(src);
            Assert.AreEqual(options.Serialize(), back.Serialize());
        }

        [TestMethod]
        public void CreateSource_NoCustomCollectionsForFolder()
        {
            var options = new SourceConnectionOptions
            {
                CustomCollections = new() { { "Test", new CustomCollection { ProcedureName = "dbo.Test" } } }
            };
            Assert.IsNull(options.CreateSource("C:\\Temp\\Import").CustomCollections);
            Assert.AreEqual(1, options.CreateSource("SQL1").CustomCollections.Count);
        }

        [TestMethod]
        public void Validate_SlowQueryExistingSession()
        {
            var options = new SourceConnectionOptions
            {
                SlowQueryThresholdMs = 1000,
                SlowQueryCaptureMode = DBADashSource.SlowQueryCaptureModes.ExistingSession
            };
            Assert.IsNotNull(options.Validate()); // No name
            options.SlowQueryXESessionName = DBADashSource.ManagedSlowQueryXESessionName;
            Assert.IsNotNull(options.Validate()); // Reserved
            options.SlowQueryXESessionName = "MySession";
            Assert.IsNull(options.Validate());
            options.SlowQueryThresholdMs = -1; // Off - name doesn't matter
            options.SlowQueryXESessionName = null;
            Assert.IsNull(options.Validate());
        }

        [TestMethod]
        public async Task CheckAndApply_AddsNewConnection()
        {
            var cfg = NewConfig();
            var src = new SourceConnectionOptions().CreateSource("SQL1");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);

            Assert.IsFalse(check.IsInvalid);
            Assert.AreEqual("SQL1", check.ConnectionID);
            Assert.AreEqual(ExistingConnectionStates.None, check.Existing);

            var result = SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions());
            Assert.AreEqual(SourceConnectionAddOutcomes.Added, result.Outcome);
            Assert.AreSame(src, cfg.SourceConnections.Single());
        }

        [TestMethod]
        public async Task CheckAndApply_ExistingConnectionUpdatedOnlyWhenDecided()
        {
            var cfg = NewConfig();
            var existing = new SourceConnectionOptions().CreateSource("SQL1");
            existing.ConnectionID = "CustomID";
            existing.SlowQueryEventFileMaxSizeMB = 77;
            existing.CollectionSchedules = new CollectionSchedules { { CollectionType.Jobs, new CollectionSchedule { Schedule = "600" } } };
            cfg.SourceConnections.Add(existing);

            var src = new SourceConnectionOptions { NoWMI = true }.CreateSource("SQL1");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);
            Assert.AreEqual(ExistingConnectionStates.Configured, check.Existing);
            Assert.AreEqual("CustomID", check.ExistingConnectionID);

            var skipped = SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions());
            Assert.AreEqual(SourceConnectionAddOutcomes.Skipped, skipped.Outcome);
            Assert.AreSame(existing, cfg.SourceConnections.Single());

            var updated = SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions { UpdateExisting = true });
            Assert.AreEqual(SourceConnectionAddOutcomes.Updated, updated.Outcome);
            var replaced = cfg.SourceConnections.Single();
            Assert.AreSame(src, replaced);
            Assert.IsTrue(replaced.NoWMI);
            Assert.AreEqual("CustomID", replaced.ConnectionID); // Kept, so it stays the same instance in the repository
            Assert.AreEqual(77, replaced.SlowQueryEventFileMaxSizeMB);
            Assert.AreEqual("600", replaced.CollectionSchedules[CollectionType.Jobs].Schedule);
        }

        [TestMethod]
        public async Task Apply_UnvalidatedConnectionNeedsDecision()
        {
            var cfg = NewConfig();
            var src = new SourceConnectionOptions().CreateSource("SQL1");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);
            check.ValidationError = "Login failed"; // As if the connection had been attempted

            Assert.AreEqual(SourceConnectionAddOutcomes.Skipped, SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions()).Outcome);
            Assert.AreEqual(SourceConnectionAddOutcomes.Added,
                SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions { AddUnvalidated = true }).Outcome);
        }

        [TestMethod]
        public async Task Apply_DefaultDeadlockSchedule()
        {
            var cfg = NewConfig();
            // Deadlocks disabled in the service schedule
            cfg.CollectionSchedules = new CollectionSchedules { { CollectionType.Deadlocks, new CollectionSchedule { Schedule = string.Empty } } };
            var src = new SourceConnectionOptions { DeadlockXESessionName = DBADashSource.ManagedDeadlockXESessionName }.CreateSource("SQL1");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);
            Assert.IsTrue(check.DeadlockScheduleMissing);

            SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions { UseDefaultDeadlockSchedule = true });
            Assert.AreEqual(CollectionSchedules.DeadlocksSchedule, src.CollectionSchedules[CollectionType.Deadlocks].Schedule);
            Assert.IsTrue(SourceConnectionAdder.IsDeadlockCollectionScheduled(cfg, src));

            var off = new SourceConnectionOptions { DeadlockXESessionName = string.Empty }.CreateSource("SQL2");
            Assert.IsFalse((await SourceConnectionAdder.CheckAsync(cfg, off, validate: false)).DeadlockScheduleMissing);
        }

        [TestMethod]
        public async Task Check_InvalidConnection()
        {
            var cfg = NewConfig();
            var src = new DBADashSource("x");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);
            Assert.IsTrue(check.IsInvalid);
            Assert.AreEqual(SourceConnectionAddOutcomes.Skipped,
                SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions { AddUnvalidated = true }).Outcome);
            Assert.AreEqual(0, cfg.SourceConnections.Count);
        }
    }

    /// <summary>Checking a connection that's really made - against LocalDB, or the instance in DBADASH_TEST_SQL.</summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class SourceConnectionAdderSqlTests
    {
        private static string ConnectionString =>
            Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs
                ? cs
                : new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }.ConnectionString;

        [TestMethod]
        public async Task Check_ConnectsAndReportsInstance()
        {
            var cfg = new CollectionConfig { Destination = "Data Source=Repo;Integrated Security=True;Initial Catalog=DBADashDB" };
            var src = new SourceConnectionOptions { SlowQueryThresholdMs = 1000 }.CreateSource(ConnectionString);
            SourceConnectionCheck check;
            try
            {
                check = await SourceConnectionAdder.CheckAsync(cfg, src);
            }
            catch (SqlException ex)
            {
                Assert.Inconclusive($"SQL instance isn't available: {ex.Message}");
                return;
            }
            if (!check.Validated && Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is not { Length: > 0 })
            {
                Assert.Inconclusive($"LocalDB isn't available: {check.ValidationError}");
            }

            Assert.IsTrue(check.Validated, check.ValidationError);
            Assert.IsFalse(string.IsNullOrEmpty(check.ConnectionID));
            Assert.AreEqual(check.ConnectionID, src.ConnectionID);
            Assert.IsFalse(string.IsNullOrEmpty(check.ProductVersion));
            Assert.IsFalse(string.IsNullOrEmpty(check.LoginName));
            Assert.AreEqual(!check.XESupported, check.SlowQueryWillBeDisabled);
        }
    }
}
