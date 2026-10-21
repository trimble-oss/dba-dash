using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADash.Test
{
    /// <summary>
    /// Import procs against a repository database whose collation isn't the server collation.  Temp tables take the server
    /// (tempdb) collation, so a character column without COLLATE DATABASE_DEFAULT can't be compared with a repository column
    /// (#2210).  <see cref="TempTableCollationTests"/> checks the definitions; these run the code paths that CI's end-to-end
    /// collection can't reach - it has no availability group to raise role changes.  Inconclusive where the server has the
    /// same collation as the repository, as there's nothing to conflict.
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  The database is dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class RepositoryCollationSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Database = "DBADashCollationTest_" + Guid.NewGuid().ToString("N");
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string ConnectionString => ConnectionStringFor(Database);

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
            string serverCollation;
            try
            {
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                using var cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('Collation') AS NVARCHAR(128))", master);
                serverCollation = (string)cmd.ExecuteScalar();
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
                return;
            }

            // The repository database as DBA Dash deploys it - with the DACPAC's collation, whatever the server collation is
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Database, MessageAuthorizationSqlTests.FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);

            using var cn = Open();
            using var check = new SqlCommand("SELECT CAST(DATABASEPROPERTYEX(DB_NAME(),'Collation') AS NVARCHAR(128))", cn);
            var repositoryCollation = (string)check.ExecuteScalar();
            if (repositoryCollation == serverCollation)
            {
                // Nothing to conflict.  CI's server collation is Latin1_General_BIN, so it's tested there.
                Assert.Inconclusive($"The repository and server collations are both {serverCollation}.  These tests need a server with a different collation to the repository.");
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                Execute(master, $"IF DB_ID('{Database}') IS NOT NULL BEGIN ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Database}]; END");
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        /// <summary>
        /// A failover of the local replica (PRIMARY_NORMAL => RESOLVING_NORMAL => SECONDARY_NORMAL) is recorded as a role change.
        /// This runs every statement in AGHealthEvents_Upd that compares the temp tables with repository columns.
        /// </summary>
        [TestMethod]
        public void AGHealthEventsRoleChangeImports()
        {
            using var cn = Open();
            using var cmd = new SqlCommand(@"
DECLARE @InstanceID INT
INSERT INTO dbo.Instances(Instance,ConnectionID) VALUES(N'SQL1',N'SQL1')
SET @InstanceID = SCOPE_IDENTITY()

DECLARE @group_id UNIQUEIDENTIFIER = NEWID(), @replica_id UNIQUEIDENTIFIER = NEWID()
INSERT INTO dbo.AvailabilityReplicas(InstanceID,replica_id,group_id,replica_server_name,is_local)
VALUES(@InstanceID,@replica_id,@group_id,N'SQL1',1)

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME()
DECLARE @Events dbo.AGHealthEvents
INSERT INTO @Events(EventTime,EventName,EventHash,group_id,AvailabilityGroupName,replica_id,ReplicaName,PreviousState,CurrentState)
VALUES(DATEADD(s,-20,@Now),N'availability_replica_state_change',HASHBYTES('SHA2_256','1'),@group_id,N'AG1',@replica_id,N'SQL1',N'RESOLVING_NORMAL',N'PRIMARY_NORMAL'),
	(DATEADD(s,-10,@Now),N'availability_replica_state_change',HASHBYTES('SHA2_256','2'),@group_id,N'AG1',@replica_id,N'SQL1',N'PRIMARY_NORMAL',N'RESOLVING_NORMAL'),
	(@Now,N'availability_replica_state_change',HASHBYTES('SHA2_256','3'),@group_id,N'AG1',@replica_id,N'SQL1',N'RESOLVING_NORMAL',N'SECONDARY_NORMAL')

EXEC dbo.AGHealthEvents_Upd @AGHealthEvents = @Events, @InstanceID = @InstanceID, @SnapshotDate = @Now

SELECT COUNT(*)
FROM dbo.AvailabilityGroupRoleChanges
WHERE InstanceID = @InstanceID
AND Source = 'XE'
AND PreviousRole = 1
AND NewRole = 2", cn);

            Assert.AreEqual(1, (int)cmd.ExecuteScalar());
        }

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            return cn;
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
