using DBADash;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADash.Test
{
    /// <summary>
    /// The log shipping counts dbo.Summary_Upd keeps for the Summary tab: databases by status, and the furthest behind of those checked.
    /// Default thresholds (warning 24hrs, critical 48hrs).  Same server as <see cref="MessageAuthorizationSqlTests"/>: the
    /// <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/> connection string, or LocalDB.  The database is dropped afterwards.
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class SummaryLogShippingSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Repository = "DBADashSummaryLSTest_" + Guid.NewGuid().ToString("N");
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string RepositoryConnectionString => ConnectionStringFor(Repository);

        private static int logShippedInstance;
        private static int otherInstance;
        private static int pairedSecondaryInstance;

        private static string ConnectionStringFor(string database)
        {
            var builder = ServerConnectionString == null
                ? new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }
                : new SqlConnectionStringBuilder(ServerConnectionString);
            builder.InitialCatalog = database;
            return builder.ConnectionString;
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            try
            {
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
                return;
            }
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Repository, MessageAuthorizationSqlTests.FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);

            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            logShippedInstance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive,UTCOffset) OUTPUT INSERTED.InstanceID VALUES(N'LSSecondary',N'LSSecondary',3,1,0)");
            otherInstance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive,UTCOffset) OUTPUT INSERTED.InstanceID VALUES(N'NoLS',N'NoLS',3,1,0)");
            Execute(cn, $"INSERT INTO dbo.CollectionDates(InstanceID,Reference,SnapshotDate) VALUES({logShippedInstance},'LogRestores',SYSUTCDATETIME())");
            Execute(cn, $"INSERT INTO dbo.LogShippingExclusions(InstanceID,DatabaseName) VALUES({logShippedInstance},N'Excluded')");

            // Minutes since the last restore, and since the backup it restored was started
            AddRestoringDatabase(cn, logShippedInstance, "OK1", 10, 20);
            AddRestoringDatabase(cn, logShippedInstance, "OK2", 5, 15);
            AddRestoringDatabase(cn, logShippedInstance, "Warn", 30 * 60, 30 * 60 + 10);
            AddRestoringDatabase(cn, logShippedInstance, "Crit", 3 * 24 * 60, 3 * 24 * 60 + 10);
            AddRestoringDatabase(cn, logShippedInstance, "Excluded", 10 * 24 * 60, 10 * 24 * 60 + 10); // Further behind, but not checked
            AddRestoringDatabase(cn, logShippedInstance, "NoRestore", null, null); // Restoring, but no log restore recorded: critical with no time behind

            /*
                A secondary of two primaries.  Orders is on both and missing on the secondary: the view has a row for each primary, but it's one database.
                NoBackupInfo has a restore recorded without backup details: no time behind (critical on latency), but it isn't a database without a restore.
            */
            pairedSecondaryInstance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive,UTCOffset) OUTPUT INSERTED.InstanceID VALUES(N'LSPairedSecondary',N'LSPairedSecondary',3,1,0)");
            Execute(cn, $"INSERT INTO dbo.CollectionDates(InstanceID,Reference,SnapshotDate) VALUES({pairedSecondaryInstance},'LogRestores',SYSUTCDATETIME())");
            foreach (var primary in new[] { "LSPrimary1", "LSPrimary2" })
            {
                var primaryInstance = (int)Scalar(cn, $"INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive,UTCOffset) OUTPUT INSERTED.InstanceID VALUES(N'{primary}',N'{primary}',3,1,0)");
                Execute(cn, $"INSERT INTO dbo.LogShippingPairs(PrimaryInstanceID,SecondaryInstanceID) VALUES({primaryInstance},{pairedSecondaryInstance})");
                AddDatabase(cn, primaryInstance, "Orders", state: 0);
                AddDatabase(cn, primaryInstance, "NoBackupInfo", state: 0);
            }
            var noBackupInfo = AddDatabase(cn, pairedSecondaryInstance, "NoBackupInfo", state: 1);
            Execute(cn, $@"INSERT INTO dbo.LogRestores(InstanceID,DatabaseID,restore_date,backup_start_date,last_file)
VALUES({pairedSecondaryInstance},{noBackupInfo},DATEADD(mi,-10,SYSUTCDATETIME()),NULL,N'NoBackupInfo.trn')");

            Execute(cn, "EXEC dbo.Summary_Upd");
        }

        private static int AddDatabase(SqlConnection cn, int instanceID, string name, int state) => (int)Scalar(cn, $@"
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,
    is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state,is_in_standby,recovery_model)
OUTPUT INSERTED.DatabaseID
VALUES({instanceID},N'{name}',100 + (SELECT COUNT(*) FROM dbo.Databases),DATEADD(d,-30,GETUTCDATE()),150,0,0,0,0,0,0,0,0,0,1,{state},0,1)");

        private static void AddRestoringDatabase(SqlConnection cn, int instanceID, string name, int? restoredMinsAgo, int? backupMinsAgo)
        {
            var databaseID = AddDatabase(cn, instanceID, name, state: 1);
            if (restoredMinsAgo == null) return;
            Execute(cn, $@"INSERT INTO dbo.LogRestores(InstanceID,DatabaseID,restore_date,backup_start_date,last_file)
VALUES({instanceID},{databaseID},DATEADD(mi,-{restoredMinsAgo},SYSUTCDATETIME()),DATEADD(mi,-{backupMinsAgo},SYSUTCDATETIME()),N'{name}.trn')");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                Execute(master, $"IF DB_ID('{Repository}') IS NOT NULL BEGIN ALTER DATABASE [{Repository}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Repository}]; END");
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        [TestMethod]
        public void CountsByStatus()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("SELECT LogShippingStatus,LogShippingCriticalCount,LogShippingWarningCount,LogShippingOKCount,LogShippingNACount,LogShippingMaxTotalTimeBehind,LogShippingNoRestoreCount FROM dbo.Summary WHERE InstanceID = @InstanceID", cn);
            cmd.Parameters.AddWithValue("InstanceID", logShippedInstance);
            using var rdr = cmd.ExecuteReader();
            Assert.IsTrue(rdr.Read());
            Assert.AreEqual(1, rdr.GetInt32(0), "Worst status");
            Assert.AreEqual(2, rdr.GetInt32(1), "Critical (Crit + NoRestore)");
            Assert.AreEqual(1, rdr.GetInt32(6), "No log restore recorded");
            Assert.AreEqual(1, rdr.GetInt32(2), "Warning");
            Assert.AreEqual(2, rdr.GetInt32(3), "OK");
            Assert.AreEqual(1, rdr.GetInt32(4), "N/A (excluded)");
            // Crit's backup was started 3 days + 10 mins ago.  The excluded database is further behind but isn't checked
            Assert.AreEqual(3 * 24 * 60 + 10, (double)rdr.GetDecimal(5), 1.0, "Furthest behind (mins)");
        }

        [TestMethod]
        public void MissingOnSecondaryCountedOnceAndNoBackupInfoHasARestore()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            Assert.AreEqual(2, (int)Scalar(cn, $"SELECT COUNT(*) FROM dbo.LogShippingStatus WHERE InstanceID = {pairedSecondaryInstance} AND name = N'Orders'"), "The view has Orders once per primary");
            using var cmd = new SqlCommand("SELECT LogShippingStatus,LogShippingCriticalCount,LogShippingWarningCount,LogShippingOKCount,LogShippingNACount,LogShippingMaxTotalTimeBehind,LogShippingNoRestoreCount FROM dbo.Summary WHERE InstanceID = @InstanceID", cn);
            cmd.Parameters.AddWithValue("InstanceID", pairedSecondaryInstance);
            using var rdr = cmd.ExecuteReader();
            Assert.IsTrue(rdr.Read());
            Assert.AreEqual(1, rdr.GetInt32(0), "Worst status");
            Assert.AreEqual(2, rdr.GetInt32(1), "Critical: Orders (once) + NoBackupInfo");
            Assert.AreEqual(0, rdr.GetInt32(2), "Warning");
            Assert.AreEqual(0, rdr.GetInt32(3), "OK");
            Assert.AreEqual(0, rdr.GetInt32(4), "N/A");
            Assert.IsTrue(rdr.IsDBNull(5), "Neither has a time behind");
            Assert.AreEqual(1, rdr.GetInt32(6), "No log restore recorded: Orders only (once)");
        }

        [TestMethod]
        public void NoLogShipping()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("SELECT LogShippingStatus,LogShippingCriticalCount,LogShippingMaxTotalTimeBehind FROM dbo.Summary WHERE InstanceID = @InstanceID", cn);
            cmd.Parameters.AddWithValue("InstanceID", otherInstance);
            using var rdr = cmd.ExecuteReader();
            Assert.IsTrue(rdr.Read());
            Assert.AreEqual(3, rdr.GetInt32(0), "N/A without log shipping");
            Assert.IsTrue(rdr.IsDBNull(1));
            Assert.IsTrue(rdr.IsDBNull(2));
        }

        [TestMethod]
        public void SummaryGetReturnsCounts()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("dbo.Summary_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceIDs", logShippedInstance.ToString());
            using var rdr = cmd.ExecuteReader();
            Assert.IsTrue(rdr.Read());
            Assert.AreEqual(2, rdr.GetInt32(rdr.GetOrdinal("LogShippingCriticalCount")));
            Assert.AreEqual(1, rdr.GetInt32(rdr.GetOrdinal("LogShippingNoRestoreCount")));
            Assert.AreEqual(2, rdr.GetInt32(rdr.GetOrdinal("LogShippingOKCount")));
        }

        private static object Scalar(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            return cmd.ExecuteScalar()!;
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
