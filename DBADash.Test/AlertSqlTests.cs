using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// Alert procs against a repository database.  CI's end-to-end collection doesn't create alert rules, so without these
    /// the alert procs only get compiled, never run - on any of the SQL versions or the binary server collation CI tests.
    /// Each test runs in a transaction that's rolled back, as rules apply to every instance and would affect the other tests.
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  The database is dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public partial class AlertSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Database = "DBADashAlertTest_" + Guid.NewGuid().ToString("N");
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
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Database, MessageAuthorizationSqlTests.FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                using var cmd = new SqlCommand($"IF DB_ID('{Database}') IS NOT NULL BEGIN ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Database}]; END", master);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        /// <summary>Details as the GUI saves them, for alert types that need more than the defaults to evaluate.</summary>
        private static readonly Dictionary<string, string> RuleDetails = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Wait"] = @"{""WaitType"":""LCK_M_X"",""IsPerCore"":false}",
            ["Counter"] = @"{""ComparisonSymbol"":"">="",""Aggregation"":""Avg"",""Counter"":{""ObjectName"":""CPU"",""CounterName"":""CPU Utilization"",""InstanceName"":"""",""ApplyToAllInstances"":false}}",
            ["BackupAlert"] = @"{""BackupType"":""Full"",""AlertMode"":""AgeSinceLastBackup""}",
            ["CollectionDates"] = @"{""UseCriticalStatus"":true}",
        };

        /// <summary>
        /// One rule of every alert type, then the alert processing the service runs.  Every proc gets past its "no rules"
        /// check and runs each statement, so a compile error, collation conflict or runtime error in any of them fails here.
        /// </summary>
        [TestMethod]
        public void AlertsUpdRunsEveryAlertType()
        {
            var types = AlertRegistrationTests.ReadAlertProcs().Values.ToList();
            var rules = string.Join(",\n", types.Select((t, i) =>
                $"('{t}','{t}',{i % 40},-1,1,1,{(RuleDetails.TryGetValue(t, out var details) ? "N'" + details.Replace("'", "''") + "'" : "NULL")})"));
            var messages = new List<string>();

            RunAndRollback($@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary) VALUES(N'SQL1',N'SQL1',1,1)
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details)
VALUES {rules}
EXEC Alert.Alerts_Upd", cmd => cmd.ExecuteNonQuery(), messages);

            var notProcessed = types.Where(t => !messages.Any(m => m.Equals($"Processing alerts of type {t}", StringComparison.OrdinalIgnoreCase))).ToList();
            Assert.AreEqual(0, notProcessed.Count, "Alert types that weren't processed: " + string.Join(", ", notProcessed) + "\nMessages:\n" + string.Join("\n", messages));
        }

        /*
         * The alert's priority and RuleID must come from the same rule, or the alert shows the wrong rule's notes and is
         * closed when that rule is deleted.  Each test has rule A (lower RuleID, priority 30) matching everything and
         * rule B (priority 10) matching a subset, so a separate MIN(Priority) & MIN(RuleID) would pair B's priority with A.
         */

        private const string AssertRuleB = @"
SELECT AA.Priority, AA.RuleID, @RuleB AS ExpectedRuleID
FROM Alert.ActiveAlerts AA
WHERE AA.InstanceID = @InstanceID
AND AA.AlertType = @Type
AND AA.IsResolved = 0";

        [TestMethod]
        public void DriveSpaceAlertRuleMatchesPriority()
        {
            AssertAlertUsesRuleB(@"
DECLARE @Type VARCHAR(50)='DriveSpace', @InstanceID INT, @RuleA INT, @RuleB INT
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary) VALUES(N'SQL1',N'SQL1',1,1)
SET @InstanceID = SCOPE_IDENTITY()
INSERT INTO dbo.CollectionDates(InstanceID,Reference,SnapshotDate) VALUES(@InstanceID,'Drives',SYSUTCDATETIME())
INSERT INTO dbo.Drives(InstanceID,Name,Capacity,FreeSpace,IsActive)
VALUES(@InstanceID,N'C:\',100000000000,10000000000,1), /* 10% free */
	(@InstanceID,N'D:\',100000000000,40000000000,1) /* 40% free */

INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'DRIVE SPACE',30,-1,50,1,'{""UseCriticalStatus"":false,""IsThresholdPercentage"":true}')
SET @RuleA = SCOPE_IDENTITY()
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'DRIVE SPACE',10,-1,20,1,'{""UseCriticalStatus"":false,""IsThresholdPercentage"":true}')
SET @RuleB = SCOPE_IDENTITY()

EXEC Alert.DriveSpaceAlert_Upd
" + AssertRuleB);
        }

        [TestMethod]
        public void BackupAlertRuleMatchesPriority()
        {
            AssertAlertUsesRuleB(@"
DECLARE @Type VARCHAR(50)='BackupAlert', @InstanceID INT, @RuleA INT, @RuleB INT
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset) VALUES(N'SQL1',N'SQL1',1,1,3,0)
SET @InstanceID = SCOPE_IDENTITY()
INSERT INTO dbo.CollectionDates(InstanceID,Reference,SnapshotDate) VALUES(@InstanceID,'Backups',SYSUTCDATETIME())
/* Never backed up and created well before both thresholds */
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state,is_in_standby,recovery_model)
VALUES(@InstanceID,N'DB1',5,DATEADD(d,-30,GETUTCDATE()),150,0,0,0,0,0,0,0,0,0,1,0,0,1),
	(@InstanceID,N'DB2',6,DATEADD(d,-30,GETUTCDATE()),150,0,0,0,0,0,0,0,0,0,1,0,0,1)

INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'BACKUP:Full',30,-1,1440,1,'{""BackupType"":""Full"",""AlertMode"":""AgeSinceLastBackup""}')
SET @RuleA = SCOPE_IDENTITY()
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'BACKUP:Full',10,-1,1440,1,'{""BackupType"":""Full"",""AlertMode"":""AgeSinceLastBackup"",""DatabaseName"":""DB1""}')
SET @RuleB = SCOPE_IDENTITY()

EXEC Alert.BackupAlert_Upd
" + AssertRuleB);
        }

        private const string AzureDBs = @"
INSERT INTO dbo.Instances(Instance,ConnectionID,EditionID,IsActive,ShowInSummary) VALUES(N'azsrv.database.windows.net',N'azsrv|master',1674378470,1,1)
SET @InstanceID = SCOPE_IDENTITY()
DECLARE @DB1 INT, @DB2 INT
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive)
VALUES(@InstanceID,N'DB1',5,GETDATE(),150,0,0,0,0,0,0,0,0,0,1)
SET @DB1 = SCOPE_IDENTITY()
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive)
VALUES(@InstanceID,N'DB2',6,GETDATE(),150,0,0,0,0,0,0,0,0,0,1)
SET @DB2 = SCOPE_IDENTITY()
/* DB1 95% used, DB2 40% used of a 1GB max size */
INSERT INTO dbo.DBFiles(DatabaseID,name,file_id,type,size,space_used,max_size,data_space_id,IsActive)
VALUES(@DB1,N'DB1',1,0,131072,124518,131072,1,1),
	(@DB2,N'DB2',1,0,131072,52429,131072,1,1)
";

        [TestMethod]
        public void AzureDBSpaceAlertRuleMatchesPriority()
        {
            AssertAlertUsesRuleB(@"
DECLARE @Type VARCHAR(50)='AzureDBSpace', @InstanceID INT, @RuleA INT, @RuleB INT
" + AzureDBs + @"
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'AzureDBSpace',30,-1,30,1,'{""UseCriticalStatus"":false,""SpaceMeasure"":""Used""}')
SET @RuleA = SCOPE_IDENTITY()
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'AzureDBSpace',10,-1,90,1,'{""UseCriticalStatus"":false,""SpaceMeasure"":""Used""}')
SET @RuleB = SCOPE_IDENTITY()

EXEC Alert.AzureDBSpaceAlert_Upd
" + AssertRuleB);
        }

        [TestMethod]
        public void ElasticPoolSpaceAlertRuleMatchesPriority()
        {
            AssertAlertUsesRuleB(@"
DECLARE @Type VARCHAR(50)='ElasticPoolSpace', @InstanceID INT, @RuleA INT, @RuleB INT, @P1 INT, @P2 INT
INSERT INTO dbo.Instances(Instance,ConnectionID,EditionID,IsActive,ShowInSummary) VALUES(N'azsrv.database.windows.net',N'azsrv|master',1674378470,1,1)
SET @InstanceID = SCOPE_IDENTITY()
INSERT INTO dbo.AzureDBElasticPool(InstanceID,elastic_pool_name) VALUES(@InstanceID,N'Pool1')
SET @P1 = SCOPE_IDENTITY()
INSERT INTO dbo.AzureDBElasticPool(InstanceID,elastic_pool_name) VALUES(@InstanceID,N'Pool2')
SET @P2 = SCOPE_IDENTITY()
INSERT INTO dbo.AzureDBElasticPoolResourceStats(PoolID,start_time,end_time,avg_storage_percent,avg_allocated_storage_percent,elastic_pool_storage_limit_mb)
VALUES(@P1,DATEADD(mi,-5,SYSUTCDATETIME()),SYSUTCDATETIME(),60,95,512000),
	(@P2,DATEADD(mi,-5,SYSUTCDATETIME()),SYSUTCDATETIME(),40,50,512000)

INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'ElasticPoolSpace',30,-1,30,1,'{""UseCriticalStatus"":false,""SpaceMeasure"":""Used""}')
SET @RuleA = SCOPE_IDENTITY()
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES(@Type,'ElasticPoolSpace',10,-1,55,1,'{""UseCriticalStatus"":false,""SpaceMeasure"":""Used""}')
SET @RuleB = SCOPE_IDENTITY()

EXEC Alert.ElasticPoolSpaceAlert_Upd
" + AssertRuleB);
        }

        private static void AssertAlertUsesRuleB(string sql)
        {
            var alerts = new List<(int Priority, int RuleID, int ExpectedRuleID)>();
            RunAndRollback(sql, cmd =>
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    alerts.Add((Convert.ToInt32(rdr["Priority"]), (int)rdr["RuleID"], (int)rdr["ExpectedRuleID"]));
                }
            });

            Assert.AreEqual(1, alerts.Count, "Expected a single alert for the instance");
            Assert.AreEqual(10, alerts[0].Priority, "Alert priority should come from the highest priority rule");
            Assert.AreEqual(alerts[0].ExpectedRuleID, alerts[0].RuleID, "Alert RuleID should be the rule that supplied the priority");
        }

        private static void RunAndRollback(string sql, Action<SqlCommand> execute, List<string>? messages = null)
        {
            using var cn = new SqlConnection(ConnectionString);
            if (messages != null)
            {
                cn.InfoMessage += (_, e) => messages.AddRange(e.Errors.Cast<SqlError>().Select(err => err.Message));
            }
            cn.Open();
            using var tran = cn.BeginTransaction();
            using var cmd = new SqlCommand(sql, cn, tran) { CommandTimeout = 120 };
            try
            {
                execute(cmd);
            }
            finally
            {
                tran.Rollback();
            }
        }
    }
}
