using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// The Elastic Jobs collection, run against a stand-in for the job database of an elastic job agent and imported with dbo.ElasticJobs_Upd
    /// as DBImporter does, then the status, report, history, timeline and stats procedures read back.  CI has no elastic job agent, so
    /// without this nothing but the alert proc would run - on any of the SQL versions or the binary server collation CI tests.
    ///
    /// <para>The stand-in has the jobs schema's four views with the columns of a real agent's - which aren't as documented:
    /// job_executions has no target_group_name, and has step_name and target_elastic_pool_name.  Data is in tables beneath them.  The
    /// collection runs three times, as the service would: the first goes back 7 days and collects every active execution (one has been
    /// running for 9 days), then executions finish, a new one starts and a step is removed, and each later collection starts from where
    /// the one before said to (<see cref="ElasticJobsTables.NextFromUtc"/>).</para>
    ///
    /// <para>What this can't cover is the agent itself - how its views perform, what jobs_reader lets the login see, or how real executions
    /// are laid out (the targets of an execution are assumed to share its job_execution_id).</para>
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  The databases are dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class ElasticJobsSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Suffix = Guid.NewGuid().ToString("N");
        private static readonly string Repository = "DBADashEJRepoTest_" + Suffix;
        private static readonly string JobDB = "DBADashEJAgentTest_" + Suffix;
        private static readonly string AppDB = "DBADashEJAppTest_" + Suffix;
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string RepositoryConnectionString => ConnectionStringFor(Repository);
        private static string JobDBConnectionString => ConnectionStringFor(JobDB);

        private static readonly Guid PurgeJob = new("AAAAAAAA-0000-0000-0000-000000000001");
        private static readonly Guid IndexJob = new("BBBBBBBB-0000-0000-0000-000000000002");
        private static readonly Guid LongRunningExecution = new("EEEEEEEE-0000-0000-0000-00000000000E");

        /// <summary>The first collection - before anything changed</summary>
        private static DataSet firstCollection = new();
        private static readonly List<DateTime> collectedFrom = new();
        private static DateTime longRunningCreated;
        private static int instanceID;

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
            CreateJobDatabase();
            CreateAppDatabase();
            AddInstance();

            // First collection: no watermark - back 7 days, and every active execution
            firstCollection = Collect(null);
            Import(firstCollection);
            var from = NextFrom(firstCollection);

            // Running executions finish, a new one starts and a step of PurgeLogs is removed
            using (var cn = new SqlConnection(JobDBConnectionString))
            {
                cn.Open();
                Execute(cn, $@"
UPDATE jobs.job_executions_t SET is_active = 0, lifecycle = 'Succeeded', end_time = SYSUTCDATETIME(), last_message = 'done' WHERE is_active = 1
INSERT INTO jobs.job_executions_t(job_execution_id,job_name,job_id,job_version,step_id,is_active,lifecycle,create_time,start_time,current_attempts)
VALUES(NEWID(),'PurgeLogs','{PurgeJob}',3,NULL,1,'InProgress',SYSUTCDATETIME(),SYSUTCDATETIME(),0)
DELETE jobs.jobsteps_t WHERE job_id = '{PurgeJob}' AND step_id = 2
UPDATE jobs.jobs_t SET job_version = 4 WHERE job_id = '{PurgeJob}'");
            }
            var second = Collect(from);
            Import(second);
            var third = Collect(NextFrom(second));
            Import(third);
            // The same data again - as a file imported twice would be
            Import(third);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                foreach (var db in new[] { Repository, JobDB, AppDB })
                {
                    Execute(master, $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");
                }
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        /// <summary>
        /// The jobs schema of an elastic job agent: views over tables holding the data.  Two jobs on a target group of a server (expanded
        /// into db1 and db2), with a database excluded and a shard map.
        /// <list type="bullet">
        /// <item>PurgeLogs runs every 15 mins: two executions done, the third running.  Its first execution's step 2 was skipped on db2, and
        /// step 1 was retried on db1 in its second.</item>
        /// <item>IndexMaint: a success 26 hours ago, a failure 2 hours ago (failed on db1), and an execution running for 9 days on bigdb.</item>
        /// </list>
        /// </summary>
        private static void CreateJobDatabase()
        {
            using (var master = new SqlConnection(MasterConnectionString))
            {
                master.Open();
                Execute(master, $"CREATE DATABASE [{JobDB}]");
            }
            using var cn = new SqlConnection(JobDBConnectionString);
            cn.Open();
            Execute(cn, "CREATE SCHEMA jobs");
            Execute(cn, @"
CREATE TABLE jobs.jobs_t(job_name NVARCHAR(128), job_id UNIQUEIDENTIFIER, job_version INT, description NVARCHAR(512), enabled BIT, schedule_interval_type NVARCHAR(50), schedule_interval_count INT, schedule_start_time DATETIME2(7), schedule_end_time DATETIME2(7))
CREATE TABLE jobs.jobsteps_t(job_name NVARCHAR(128), job_id UNIQUEIDENTIFIER, job_version INT, step_id INT, step_name NVARCHAR(128), command_type NVARCHAR(50), command_source NVARCHAR(50), command NVARCHAR(MAX), credential_name NVARCHAR(128), target_group_name NVARCHAR(128), target_group_id UNIQUEIDENTIFIER, initial_retry_interval_seconds INT, maximum_retry_interval_seconds INT, retry_interval_backoff_multiplier REAL, retry_attempts INT, step_timeout_seconds INT, output_type NVARCHAR(11), output_credential_name NVARCHAR(128), output_subscription_id UNIQUEIDENTIFIER, output_resource_group_name NVARCHAR(128), output_server_name NVARCHAR(256), output_database_name NVARCHAR(128), output_schema_name NVARCHAR(MAX), output_table_name NVARCHAR(MAX), max_parallelism INT)
CREATE TABLE jobs.target_group_members_t(target_group_name NVARCHAR(128), target_group_id UNIQUEIDENTIFIER, membership_type VARCHAR(20), target_type NVARCHAR(128), target_id UNIQUEIDENTIFIER, refresh_credential_name NVARCHAR(128), subscription_id UNIQUEIDENTIFIER, resource_group_name NVARCHAR(128), server_name NVARCHAR(128), database_name NVARCHAR(128), elastic_pool_name NVARCHAR(128), shard_map_name NVARCHAR(128))
CREATE TABLE jobs.job_executions_t(job_execution_id UNIQUEIDENTIFIER, job_name NVARCHAR(128), job_id UNIQUEIDENTIFIER, job_version INT, step_name NVARCHAR(128), step_id INT, is_active BIT, lifecycle NVARCHAR(50), create_time DATETIME2(7), start_time DATETIME2(7), end_time DATETIME2(7), current_attempts INT, current_attempt_start_time DATETIME2(7), next_attempt_start_time DATETIME2(7), last_message NVARCHAR(MAX), target_type NVARCHAR(128), target_id UNIQUEIDENTIFIER, target_subscription_id UNIQUEIDENTIFIER, target_resource_group_name NVARCHAR(128), target_server_name NVARCHAR(256), target_database_name NVARCHAR(128), target_elastic_pool_name NVARCHAR(128))");
            Execute(cn, "CREATE VIEW jobs.jobs AS SELECT * FROM jobs.jobs_t");
            Execute(cn, "CREATE VIEW jobs.jobsteps AS SELECT * FROM jobs.jobsteps_t");
            Execute(cn, "CREATE VIEW jobs.target_group_members AS SELECT * FROM jobs.target_group_members_t");
            Execute(cn, "CREATE VIEW jobs.job_executions AS SELECT * FROM jobs.job_executions_t");
            Execute(cn, $@"
DECLARE @TG UNIQUEIDENTIFIER = NEWID(), @SRV UNIQUEIDENTIFIER = NEWID(), @Now DATETIME2(7) = SYSUTCDATETIME()
INSERT INTO jobs.jobs_t VALUES('PurgeLogs','{PurgeJob}',3,'Purge old log rows',1,'Minutes',15,'20260101',NULL),
	('IndexMaint','{IndexJob}',1,'Nightly index maintenance',1,'Days',1,'20260101 01:00',NULL)
INSERT INTO jobs.jobsteps_t(job_name,job_id,job_version,step_id,step_name,command_type,command_source,command,credential_name,target_group_name,target_group_id,retry_attempts,step_timeout_seconds,output_type)
VALUES('PurgeLogs','{PurgeJob}',3,1,'Purge','TSql','Inline','DELETE dbo.Log','JobRun','AllDbs',@TG,10,43200,'Inline'),
	('PurgeLogs','{PurgeJob}',3,2,'Shrink stats','TSql','Inline','UPDATE STATISTICS dbo.Log','JobRun','AllDbs',@TG,10,43200,'Inline'),
	('IndexMaint','{IndexJob}',1,1,'Rebuild','TSql','Inline','EXEC dbo.IndexOptimize','JobRun','AllDbs',@TG,0,43200,'Inline')
INSERT INTO jobs.target_group_members_t(target_group_name,target_group_id,membership_type,target_type,target_id,server_name,database_name,shard_map_name)
VALUES('AllDbs',@TG,'Include','SqlServer',@SRV,'srv1.database.windows.net',NULL,NULL),
	('AllDbs',@TG,'Exclude','SqlDatabase',NEWID(),'srv1.database.windows.net','master',NULL),
	('AllDbs',@TG,'Include','SqlShardMap',NEWID(),'srv1.database.windows.net','shardmapdb','CustomerMap')

DECLARE @i INT = 0, @E UNIQUEIDENTIFIER, @S DATETIME2(7), @Active BIT
WHILE @i < 3
BEGIN
	SELECT @E = NEWID(), @S = DATEADD(mi,-15*(2-@i)-20,@Now), @Active = CASE WHEN @i = 2 THEN 1 ELSE 0 END
	INSERT INTO jobs.job_executions_t(job_execution_id,job_name,job_id,job_version,step_name,step_id,is_active,lifecycle,create_time,start_time,end_time,current_attempts,last_message,target_type,target_id,target_server_name,target_database_name)
	VALUES(@E,'PurgeLogs','{PurgeJob}',3,NULL,NULL,@Active,CASE WHEN @Active = 1 THEN 'InProgress' ELSE 'Succeeded' END,@S,@S,CASE WHEN @Active = 1 THEN NULL ELSE DATEADD(s,95,@S) END,0,NULL,NULL,NULL,NULL,NULL),
		(@E,'PurgeLogs','{PurgeJob}',3,'Purge',1,0,'Succeeded',@S,@S,DATEADD(s,5,@S),1,NULL,'SqlServer',@SRV,'srv1.database.windows.net',NULL),
		(@E,'PurgeLogs','{PurgeJob}',3,'Purge',1,0,'Succeeded',DATEADD(s,5,@S),DATEADD(s,6,@S),DATEADD(s,40,@S),CASE WHEN @i = 1 THEN 2 ELSE 1 END,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db1'),
		(@E,'PurgeLogs','{PurgeJob}',3,'Purge',1,0,'Succeeded',DATEADD(s,5,@S),DATEADD(s,6,@S),DATEADD(s,50,@S),1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db2'),
		(@E,'PurgeLogs','{PurgeJob}',3,'Shrink stats',2,@Active,CASE WHEN @Active = 1 THEN 'InProgress' ELSE 'Succeeded' END,DATEADD(s,51,@S),DATEADD(s,52,@S),CASE WHEN @Active = 1 THEN NULL ELSE DATEADD(s,90,@S) END,1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db1'),
		(@E,'PurgeLogs','{PurgeJob}',3,'Shrink stats',2,0,CASE WHEN @i = 0 THEN 'Skipped' ELSE 'Succeeded' END,DATEADD(s,51,@S),DATEADD(s,52,@S),DATEADD(s,60,@S),1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db2')
	SET @i += 1
END

DECLARE @E1 UNIQUEIDENTIFIER = NEWID(), @E2 UNIQUEIDENTIFIER = NEWID(), @S1 DATETIME2(7) = DATEADD(hh,-26,@Now), @S2 DATETIME2(7) = DATEADD(hh,-2,@Now), @S3 DATETIME2(7) = DATEADD(d,-9,@Now)
INSERT INTO jobs.job_executions_t(job_execution_id,job_name,job_id,job_version,step_name,step_id,is_active,lifecycle,create_time,start_time,end_time,current_attempts,last_message,target_type,target_id,target_server_name,target_database_name)
VALUES(@E1,'IndexMaint','{IndexJob}',1,NULL,NULL,0,'Succeeded',@S1,@S1,DATEADD(mi,30,@S1),0,'Job execution succeeded.',NULL,NULL,NULL,NULL),
	(@E1,'IndexMaint','{IndexJob}',1,'Rebuild',1,0,'Succeeded',@S1,DATEADD(s,1,@S1),DATEADD(mi,20,@S1),1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db1'),
	(@E2,'IndexMaint','{IndexJob}',1,NULL,NULL,0,'Failed',@S2,@S2,DATEADD(mi,40,@S2),0,'Job execution failed.',NULL,NULL,NULL,NULL),
	(@E2,'IndexMaint','{IndexJob}',1,'Rebuild',1,0,'Failed',@S2,DATEADD(s,1,@S2),DATEADD(mi,40,@S2),1,'Msg 1205: deadlock victim','SqlDatabase',@SRV,'srv1.database.windows.net','db1'),
	(@E2,'IndexMaint','{IndexJob}',1,'Rebuild',1,0,'Succeeded',@S2,DATEADD(s,1,@S2),DATEADD(mi,25,@S2),1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','db2'),
	('{LongRunningExecution}','IndexMaint','{IndexJob}',1,NULL,NULL,1,'InProgress',@S3,@S3,NULL,0,NULL,NULL,NULL,NULL,NULL),
	('{LongRunningExecution}','IndexMaint','{IndexJob}',1,'Rebuild',1,1,'InProgress',@S3,@S3,NULL,1,NULL,'SqlDatabase',@SRV,'srv1.database.windows.net','bigdb')");
            longRunningCreated = (DateTime)Scalar(cn, $"SELECT create_time FROM jobs.job_executions_t WHERE job_execution_id = '{LongRunningExecution}' AND step_id IS NULL");
        }

        /// <summary>An application's own jobs schema - tables named as the agent's views.  Not an elastic job database.</summary>
        private static void CreateAppDatabase()
        {
            using (var master = new SqlConnection(MasterConnectionString))
            {
                master.Open();
                Execute(master, $"CREATE DATABASE [{AppDB}]");
            }
            using var cn = new SqlConnection(ConnectionStringFor(AppDB));
            cn.Open();
            Execute(cn, "CREATE SCHEMA jobs");
            Execute(cn, @"
CREATE TABLE jobs.jobs(job_id INT PRIMARY KEY, name NVARCHAR(100))
CREATE TABLE jobs.jobsteps(id INT PRIMARY KEY)
CREATE TABLE jobs.target_group_members(id INT PRIMARY KEY)
CREATE TABLE jobs.job_executions(id INT PRIMARY KEY, job_id INT, status NVARCHAR(20))");
        }

        private static void AddInstance()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            Execute(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition) VALUES(N'azsrv.database.windows.net',N'azsrv|jobs',1,1,5)");
            instanceID = (int)Scalar(cn, "SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = N'azsrv|jobs'");
        }

        /// <summary>
        /// The collection query, with the parameters DBCollector gives it: no watermark is the first collection, which collects every
        /// active execution.  The definitions hash is the last one sent, as the collector holds it - definitions that haven't changed
        /// since aren't returned.
        /// </summary>
        private static DataSet Collect(DateTime? fromUtc)
        {
            collectedFrom.Add(fromUtc ?? DateTime.MinValue);
            var ds = new DataSet();
            using var cn = new SqlConnection(JobDBConnectionString);
            using var cmd = new SqlCommand(SqlStrings.ElasticJobs, cn) { CommandTimeout = 120 };
            cmd.Parameters.Add(new SqlParameter("Since", SqlDbType.DateTime2) { Value = fromUtc ?? DateTime.UtcNow.AddDays(-ElasticJobsTables.InitialLookbackDays) });
            cmd.Parameters.Add(new SqlParameter("AllActive", SqlDbType.Bit) { Value = fromUtc == null });
            cmd.Parameters.Add(new SqlParameter("DefinitionsHash", SqlDbType.VarBinary, 32) { Value = definitionsHash is null ? DBNull.Value : definitionsHash });
            new SqlDataAdapter(cmd).Fill(ds);
            if (DefinitionsIncluded(ds)) definitionsHash = (byte[])ds.Tables[ElasticJobsTables.SnapshotDateResultIndex].Rows[0]["DefinitionsHash"];
            collections.Add(ds);
            return ds;
        }

        /// <summary>The hash of the definitions last sent - see DBCollector.ElasticJobsDefinitionsHash</summary>
        private static byte[]? definitionsHash;

        private static readonly List<DataSet> collections = new();

        private static bool DefinitionsIncluded(DataSet ds) => (bool)ds.Tables[ElasticJobsTables.SnapshotDateResultIndex].Rows[0]["DefinitionsIncluded"];

        private static DateTime NextFrom(DataSet ds) =>
            ElasticJobsTables.NextFromUtc(ds.Tables[3], (DateTime)ds.Tables[ElasticJobsTables.SnapshotDateResultIndex].Rows[0]["SnapshotDateUtc"]);

        /// <summary>
        /// As DBImporter: each result set to its table-valued parameter, only where it has rows.  Definitions that weren't returned aren't
        /// sent (the collector leaves their tables out), and the import is told so.
        /// </summary>
        private static void Import(DataSet ds)
        {
            var definitionsIncluded = DefinitionsIncluded(ds);
            using var cn = new SqlConnection(RepositoryConnectionString);
            using var cmd = new SqlCommand("dbo.ElasticJobs_Upd", cn) { CommandType = CommandType.StoredProcedure };
            for (var i = 0; i < ElasticJobsTables.TableNames.Length; i++)
            {
                if (!definitionsIncluded && ElasticJobsTables.TableNames[i] != ElasticJobsTables.ExecutionsTableName) continue;
                if (ds.Tables[i].Rows.Count > 0) cmd.Parameters.AddWithValue(ElasticJobsTables.TableNames[i], ds.Tables[i]);
            }
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("SnapshotDate", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("DefinitionsIncluded", definitionsIncluded);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// The definitions are returned by the first collection (nothing sent yet) and the second (a job changed and a step was removed),
        /// but not the third - nothing changed.  Its import leaves the jobs, steps and members as they were, rather than taking the missing
        /// definitions to mean there are none.
        /// </summary>
        [TestMethod]
        public void DefinitionsOnlySentWhenChanged()
        {
            CollectionAssert.AreEqual(new[] { true, true, false }, collections.Select(DefinitionsIncluded).ToArray());
            var third = collections[2];
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(0, third.Tables[i].Rows.Count, ElasticJobsTables.TableNames[i]);
            }
            Assert.IsTrue(third.Tables[3].Rows.Count > 0, "Executions are always returned");
            Assert.AreEqual(2, Query($"SELECT 1 FROM dbo.ElasticJobs WHERE InstanceID = {instanceID} AND IsActive = 1").Rows.Count, "Jobs left as they were");
        }

        /// <summary>Unchanged definitions are sent again after an hour anyway, so a repository that lost them gets them back</summary>
        [TestMethod]
        public void DefinitionsSentAgainAfterAnHour()
        {
            var hash = new byte[] { 1, 2, 3 };
            var now = DateTime.UtcNow;
            Assert.AreSame(hash, ElasticJobsTables.DefinitionsHashToCompare(hash, now.AddMinutes(-ElasticJobsTables.DefinitionsRefreshMinutes + 1), now));
            Assert.IsNull(ElasticJobsTables.DefinitionsHashToCompare(hash, now.AddMinutes(-ElasticJobsTables.DefinitionsRefreshMinutes - 1), now));
            Assert.IsNull(ElasticJobsTables.DefinitionsHashToCompare(null, now, now), "Nothing sent yet");
            Assert.IsNull(ElasticJobsTables.DefinitionsHashToCompare(hash, null, now));
        }

        /// <summary>
        /// Table-valued parameters bind by ordinal, so a column out of place in the collection query or its user defined type would be
        /// imported into the wrong column - possibly without an error.  The names have to line up as well as the count.
        /// </summary>
        [TestMethod]
        public void CollectionColumnsMatchTableTypes()
        {
            Assert.AreEqual(ElasticJobsTables.TableNames.Length + 1, firstCollection.Tables.Count, "Result sets returned by the collection, then its snapshot date");
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            for (var i = 0; i < ElasticJobsTables.TableNames.Length; i++)
            {
                var typeColumns = new List<string>();
                using var cmd = new SqlCommand(@"
SELECT c.name
FROM sys.table_types tt
JOIN sys.columns c ON c.object_id = tt.type_table_object_id
WHERE tt.name = @name
AND tt.schema_id = SCHEMA_ID('dbo')
ORDER BY c.column_id", cn);
                cmd.Parameters.AddWithValue("@name", ElasticJobsTables.TableNames[i]);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) typeColumns.Add(rdr.GetString(0));
                }
                var collectedColumns = firstCollection.Tables[i].Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                CollectionAssert.AreEqual(typeColumns, collectedColumns, $"{ElasticJobsTables.TableNames[i]}: collection {string.Join(",", collectedColumns)} - type {string.Join(",", typeColumns)}");
            }
        }

        /// <summary>A column the agent's view doesn't have - target_group_name, though documented - is collected as NULL, keeping the shape</summary>
        [TestMethod]
        public void ColumnsMissingFromTheViewsAreNull()
        {
            var executions = firstCollection.Tables[3];
            Assert.IsTrue(executions.Rows.Count > 0);
            Assert.IsTrue(executions.AsEnumerable().All(r => r.IsNull("target_group_name")), "target_group_name isn't in jobs.job_executions");
            Assert.IsTrue(executions.AsEnumerable().Any(r => r["step_name"] as string == "Shrink stats"), "step_name is collected where the view has it");
            Assert.IsTrue(firstCollection.Tables[2].AsEnumerable().Any(r => r["shard_map_name"] as string == "CustomerMap"));
        }

        /// <summary>The four jobs objects must exist and be views - an application's own jobs tables aren't an elastic job database</summary>
        [TestMethod]
        public void DetectsElasticJobDatabase()
        {
            bool IsJobDatabase(string database)
            {
                using var cn = new SqlConnection(ConnectionStringFor(database));
                cn.Open();
                return (bool)Scalar(cn, DBCollector.IsElasticJobDatabaseSql);
            }
            Assert.IsTrue(IsJobDatabase(JobDB));
            Assert.IsFalse(IsJobDatabase(AppDB), "Tables named as the agent's views");
            Assert.IsFalse(IsJobDatabase(Repository), "No jobs schema");
        }

        /// <summary>
        /// The first collection had no watermark, so it collected the execution that has been running for 9 days although that's older than
        /// its 7 day lookback.  The next collection had to start from that execution's creation so it was seen finishing.  The one after,
        /// with nothing running from before, starts from a few minutes before it ran.
        /// </summary>
        [TestMethod]
        public void CollectionStartsFromTheOldestRunningExecution()
        {
            Assert.IsTrue(firstCollection.Tables[3].AsEnumerable().Any(r => (Guid)r["job_execution_id"] == LongRunningExecution), "Active from before the lookback");
            Assert.AreEqual(longRunningCreated, collectedFrom[1], "Second collection starts from the oldest active execution");
            Assert.IsTrue(collectedFrom[2] > DateTime.UtcNow.AddMinutes(-ElasticJobsTables.OverlapMinutes - 5), $"Third collection starts from a few minutes before the second ran: {collectedFrom[2]:O}");
            Assert.AreEqual("Succeeded", Query($"SELECT lifecycle FROM dbo.ElasticJobExecutions WHERE job_execution_id = '{LongRunningExecution}' AND step_id = 0").Rows[0][0], "Seen finishing");
        }

        /// <summary>
        /// Each execution is counted in the hourly stats once, when first seen complete - however many times it's collected (three collections
        /// and the last imported twice).  The executions are the record: dbo.ElasticJobStats_RAW counts them from scratch.
        /// </summary>
        [TestMethod]
        public void StatsCountEachExecutionOnce()
        {
            const string totals = @"
SELECT job_id, step_id, SUM(FailedCount) F, SUM(SucceededCount) S, SUM(SkippedCount) Sk, SUM(RetryCount) R, SUM(TimedCount) T, SUM(RunDurationSec) D
FROM {0}
WHERE InstanceID = {1}
GROUP BY job_id, step_id
ORDER BY job_id, step_id";
            var hourly = Query(string.Format(totals, "dbo.ElasticJobStats_60MIN", instanceID));
            var raw = Query(string.Format(totals, "dbo.ElasticJobStats_RAW", instanceID));
            Assert.AreEqual(Describe(raw), Describe(hourly));

            int Count(Guid job, int step, string column) => Convert.ToInt32(hourly.AsEnumerable().Single(r => (Guid)r["job_id"] == job && (int)r["step_id"] == step)[column]);
            Assert.AreEqual(3, Count(PurgeJob, 0, "S"), "PurgeLogs: 2 executions, and the one that was running");
            Assert.AreEqual(6, Count(PurgeJob, 1, "S"), "Step 1 against db1 and db2 - not the server it expanded");
            Assert.AreEqual(1, Count(PurgeJob, 1, "R"), "Retried once on db1");
            Assert.AreEqual(1, Count(PurgeJob, 2, "Sk"), "Skipped on db2");
            Assert.AreEqual(1, Count(IndexJob, 0, "F"));
            Assert.AreEqual(2, Count(IndexJob, 0, "S"), "Including the 9 day execution");
            Assert.AreEqual(1, Count(IndexJob, 1, "F"), "Failed against db1");
        }

        private static string Describe(DataTable dt) =>
            string.Join("\n", dt.AsEnumerable().Select(r => string.Join(",", r.ItemArray)));

        [TestMethod]
        public void JobsStepsAndMembers()
        {
            var jobs = Query($"SELECT job_name, job_version, IsActive, LastSucceeded, LastFailed, StepLastFailed FROM dbo.ElasticJobs WHERE InstanceID = {instanceID} ORDER BY job_name");
            Assert.AreEqual(2, jobs.Rows.Count);
            var index = jobs.Rows[0];
            Assert.AreEqual("IndexMaint", index["job_name"]);
            Assert.IsFalse(index.IsNull("LastFailed"));
            Assert.IsFalse(index.IsNull("StepLastFailed"));
            Assert.AreEqual(4, jobs.Rows[1]["job_version"], "PurgeLogs updated");
            Assert.AreEqual(2, Query($"SELECT 1 FROM dbo.ElasticJobSteps WHERE InstanceID = {instanceID}").Rows.Count, "PurgeLogs step 2 removed");
            Assert.AreEqual(3, Query($"SELECT 1 FROM dbo.ElasticJobTargetGroupMembers WHERE InstanceID = {instanceID}").Rows.Count);
        }

        /// <summary>
        /// IndexMaint failed about 80 mins ago, and has succeeded since (the 9 day execution).  The root thresholds make a failure in the last
        /// hour critical, and in the last 7 days a warning.  PurgeLogs is OK.  Also the job's last execution, the job status of the Azure DB in
        /// the summary, and the daily report's elastic jobs.
        /// </summary>
        [TestMethod]
        public void StatusAndReport()
        {
            var report = Query($@"
DECLARE @IDs dbo.IDs
INSERT INTO @IDs VALUES({instanceID})
EXEC dbo.ElasticJobsReport_Get @InstanceIDs = @IDs, @IncludeNA = 1, @IncludeOK = 1, @IncludeACK = 1");
            var index = report.AsEnumerable().Single(r => (string)r["name"] == "IndexMaint");
            var purge = report.AsEnumerable().Single(r => (string)r["name"] == "PurgeLogs");
            Assert.AreEqual(2, index["JobStatus"], "Failed in the last 7 days");
            Assert.AreEqual(0, index["IsLastFail"], "Succeeded since");
            Assert.AreEqual(4, purge["JobStatus"]);
            Assert.AreEqual(true, purge["IsRunning"]);
            Assert.AreEqual("Every 15 Minutes", purge["Schedule"]);
            Assert.AreEqual(1, index["JobStepFails24Hrs"]);

            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            Execute(cn, "EXEC dbo.Summary_Upd");
            Assert.AreEqual(2, Scalar(cn, $"SELECT JobStatus FROM dbo.Summary WHERE InstanceID = {instanceID}"), "An Azure DB's job status is its elastic jobs");
            var issues = Query($"EXEC dbo.DailyReport_Issues_Get @InstanceIDs = '{instanceID}'");
            Assert.AreEqual("IndexMaint", issues.AsEnumerable().Single(r => (string)r["CheckGroup"] == "Elastic jobs")["ObjectName"]);
        }

        /// <summary>A job's executions, and one execution's steps against each target - by the ID they share</summary>
        [TestMethod]
        public void History()
        {
            var executions = Query($"EXEC dbo.ElasticJobHistory_Get @InstanceID = {instanceID}, @JobID = '{IndexJob}'");
            Assert.AreEqual(3, executions.Rows.Count, "One row per execution");
            var failed = executions.AsEnumerable().Single(r => (string)r["lifecycle"] == "Failed");
            Assert.AreEqual("Failed", failed["run_status_description"]);

            var targets = Query($"EXEC dbo.ElasticJobHistory_Get @InstanceID = {instanceID}, @JobID = '{IndexJob}', @job_execution_id = '{failed["job_execution_id"]}'");
            CollectionAssert.AreEquivalent(new[] { "db1", "db2" }, targets.AsEnumerable().Where(r => (int)r["step_id"] > 0).Select(r => (string)r["target_database_name"]).ToList());
            Assert.IsTrue(targets.AsEnumerable().Where(r => (int)r["step_id"] > 0).All(r => r["target_group_name"] as string == "AllDbs"), "The step's target group, as the executions don't have it");

            var purge = Query($"EXEC dbo.ElasticJobHistory_Get @InstanceID = {instanceID}, @JobID = '{PurgeJob}', @StepID = NULL");
            Assert.IsTrue(purge.AsEnumerable().Any(r => (string)r["lifecycle"] == "Skipped" && (string)r["run_status_description"] == "Skipped"));
            Assert.IsTrue(purge.AsEnumerable().Any(r => (int)r["step_id"] == 2 && (string)r["step_name"] == "Shrink stats"), "A removed step by the name it ran with");
        }

        [TestMethod]
        public void Timeline()
        {
            var timeline = Query($@"
DECLARE @From DATETIME2(2) = DATEADD(d,-2,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME()
EXEC dbo.ElasticJobTimeline_Get @InstanceID = {instanceID}, @FromDate = @From, @ToDate = @To, @IncludeSteps = 1");
            Assert.IsTrue(timeline.Columns.Contains("target_database_name") && timeline.Columns.Contains("lifecycle"), "Targets, for a row per target beneath each step");
            // The 9 day execution has finished, so it's older than the timeline looks back (a day before the period), as for SQL Agent jobs
            Assert.AreEqual(2, timeline.AsEnumerable().Count(r => (string)r["name"] == "IndexMaint" && (int)r["step_id"] == 0), "IndexMaint's executions in the last 2 days");
            Assert.IsFalse(timeline.AsEnumerable().Any(r => r["target_type"] as string == "SqlServer"), "A server that was expanded into its databases isn't a bar of its own");
            Assert.IsTrue(timeline.AsEnumerable().All(r => (int)r["RunDurationSec"] >= 0));

            var merged = Query($@"
DECLARE @From DATETIME2(2) = DATEADD(d,-2,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME()
EXEC dbo.ElasticJobTimeline_Get @InstanceID = {instanceID}, @FromDate = @From, @ToDate = @To, @MergeGapSec = 3600");
            Assert.IsTrue(merged.Columns.Contains("FailedRuns"));
            Assert.AreEqual(1, merged.AsEnumerable().Count(r => (string)r["name"] == "PurgeLogs"), "PurgeLogs runs every 15 mins - one span");
        }

        /// <summary>@MaxRows: merged only where there are more executions than that - the GUI's one call, rather than fetching them all first</summary>
        [TestMethod]
        public void TimelineMergesOnlyOverMaxRows()
        {
            DataTable Timeline(int maxRows) => Query($@"
DECLARE @From DATETIME2(2) = DATEADD(d,-2,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME()
EXEC dbo.ElasticJobTimeline_Get @InstanceID = {instanceID}, @FromDate = @From, @ToDate = @To, @IncludeSteps = 1, @MergeGapSec = 3600, @MaxRows = {maxRows}");
            var few = Timeline(1000);
            Assert.IsFalse(few.Columns.Contains("FailedRuns"), "Under the limit - each execution, with its target");
            Assert.IsTrue(few.Columns.Contains("target_database_name"));
            Assert.IsTrue(Timeline(5).Columns.Contains("FailedRuns"), "Over the limit - merged");
        }

        [TestMethod]
        public void Stats()
        {
            var summary = Query($@"
DECLARE @From DATETIME2(2) = DATEADD(d,-2,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME()
EXEC dbo.ElasticJobStatsSummary_Get @InstanceID = {instanceID}, @FromDate = @From, @ToDate = @To");
            Assert.AreEqual("Shrink stats", summary.AsEnumerable().Single(r => (Guid)r["job_id"] == PurgeJob && (int)r["step_id"] == 2)["JobStep"], "A removed step by the name it last ran with");

            foreach (var grouping in new[] { 0, 15, 60, 120 }) // 0 and 15 read the executions, 60 and 120 the hourly stats with the part hours from the executions
            {
                var chart = Query($@"
DECLARE @From DATETIME2(2) = DATEADD(d,-2,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME(), @Grouping INT = {grouping}
EXEC dbo.ElasticJobStats_Get @InstanceID = {instanceID}, @JobID = '{PurgeJob}', @StepID = 0, @FromDate = @From, @ToDate = @To, @DateGroupingMin = @Grouping OUT");
                Assert.AreEqual(3, chart.AsEnumerable().Sum(r => Convert.ToInt32(r["SucceededCount"])), $"Grouped by {grouping} mins");
            }

            var steps = Query($"EXEC dbo.ElasticJobSteps_Get @InstanceID = {instanceID}, @JobID = '{PurgeJob}'");
            Assert.AreEqual(1, steps.Rows.Count);
        }

        /// <summary>
        /// Under 24 hours, not on the hour, the summary reads the executions (dbo.ElasticJobStats_RAW) rather than the hourly stats - bounded by
        /// create_time, so not every execution kept for the job database.  The same counts as the hourly stats give.
        /// </summary>
        [TestMethod]
        public void StatsSummaryForAShortPeriod()
        {
            var summary = Query($@"
DECLARE @From DATETIME2(2) = DATEADD(mi,-197,SYSUTCDATETIME()), @To DATETIME2(2) = SYSUTCDATETIME()
EXEC dbo.ElasticJobStatsSummary_Get @InstanceID = {instanceID}, @FromDate = @From, @ToDate = @To, @Use60MIN = 0");
            int Succeeded(Guid job, int step) => Convert.ToInt32(summary.AsEnumerable().Single(r => (Guid)r["job_id"] == job && (int)r["step_id"] == step)["SucceededCount"]);
            Assert.AreEqual(3, Succeeded(PurgeJob, 0));
            Assert.AreEqual(6, Succeeded(PurgeJob, 1));
            Assert.AreEqual(1, Convert.ToInt32(summary.AsEnumerable().Single(r => (Guid)r["job_id"] == IndexJob && (int)r["step_id"] == 0)["FailedCount"]), "Failed 2 hours ago");
            Assert.IsFalse(summary.AsEnumerable().Any(r => (Guid)r["job_id"] == IndexJob && Convert.ToInt32(r["SucceededCount"]) > 0 && (int)r["step_id"] == 0),
                "IndexMaint's successes started 26 hours and 9 days ago - outside the period");
        }

        /// <summary>
        /// The summary splits a window that doesn't start or end on the hour (dbo.RollupRanges): whole hours from the hourly stats, the part
        /// hours from the executions.  It agrees with reading it all from the executions.  The hourly stats are then inflated, in a
        /// transaction that's rolled back: only the split read sees it, which shows the whole hours come from the rollup.
        /// </summary>
        [TestMethod]
        public void WholeHoursComeFromTheRollup()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            var to = (DateTime)new SqlCommand("SELECT CAST(SYSUTCDATETIME() AS DATETIME2(2))", cn, tran).ExecuteScalar()!;
            var from = to.AddMinutes(-197); // Not on the hour, and spans whole hours

            DataTable Summary(bool? use60Min)
            {
                using var cmd = new SqlCommand("dbo.ElasticJobStatsSummary_Get", cn, tran) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@InstanceID", instanceID);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                cmd.Parameters.AddWithValue("@Use60MIN", use60Min.HasValue ? use60Min.Value : DBNull.Value);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                return dt;
            }
            int Count(DataTable dt, Guid job, string column) => Convert.ToInt32(dt.AsEnumerable().Single(r => (Guid)r["job_id"] == job && (int)r["step_id"] == 0)[column]);

            Assert.AreEqual(Describe(Summary(false)), Describe(Summary(null)), "Split - the same as reading the executions");
            Assert.AreEqual(3, Count(Summary(null), PurgeJob, "SucceededCount"));

            /* IndexMaint failed 2 hours ago: always in a whole hour of the window, wherever in the hour the test runs */
            using (var cmd = new SqlCommand($"UPDATE dbo.ElasticJobStats_60MIN SET FailedCount += 100 WHERE InstanceID = {instanceID} AND job_id = '{IndexJob}' AND step_id = 0 AND FailedCount > 0", cn, tran))
            {
                Assert.AreEqual(1, cmd.ExecuteNonQuery());
            }
            Assert.AreEqual(101, Count(Summary(null), IndexJob, "FailedCount"), "Whole hours from the rollup");
            Assert.AreEqual(1, Count(Summary(false), IndexJob, "FailedCount"), "The executions aren't changed");
            tran.Rollback();
        }

        /// <summary>Removing the instance's elastic job data - in a transaction that's rolled back, as the other tests read it</summary>
        [TestMethod]
        public void DeleteRemovesEverything()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using (var cmd = new SqlCommand($"INSERT INTO Alert.ElasticJobSnapshot(InstanceID,job_id) SELECT InstanceID, job_id FROM dbo.ElasticJobs WHERE InstanceID = {instanceID}; EXEC dbo.ElasticJobs_Del @InstanceID = {instanceID}, @DaysToKeep = 0, @BatchSize = 5", cn, tran))
            {
                cmd.ExecuteNonQuery();
            }
            foreach (var table in new[] { "dbo.ElasticJobExecutions", "dbo.ElasticJobStats_60MIN", "dbo.ElasticJobSteps", "dbo.ElasticJobTargetGroupMembers", "dbo.ElasticJobs", "Alert.ElasticJobSnapshot" })
            {
                using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {table} WHERE InstanceID = {instanceID}", cn, tran);
                Assert.AreEqual(0, cmd.ExecuteScalar(), table);
            }
            tran.Rollback();
        }

        private static DataTable Query(string sql)
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            var dt = new DataTable();
            new SqlDataAdapter(new SqlCommand(sql, cn)).Fill(dt);
            return dt;
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
