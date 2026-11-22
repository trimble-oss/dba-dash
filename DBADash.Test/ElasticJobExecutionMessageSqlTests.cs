using DBADash.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// <see cref="ElasticJobExecutionMessage"/> run against a stand-in for the job database of an elastic job agent: the jobs and
    /// job_executions views over tables, and jobs.sp_start_job/jobs.sp_stop_job that add an execution and cancel one as the agent's do
    /// (the agent cancels asynchronously - here it's immediate).
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  The database is dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class ElasticJobExecutionMessageSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string JobDB = "DBADashEJExecTest_" + Guid.NewGuid().ToString("N");
        private const string ConnectionID = "azsrv|jobs";

        private static readonly Guid PurgeJob = new("AAAAAAAA-0000-0000-0000-000000000001");
        private static readonly Guid IndexJob = new("BBBBBBBB-0000-0000-0000-000000000002");
        private static readonly Guid DeniedJob = new("CCCCCCCC-0000-0000-0000-000000000003");
        private static readonly Guid IdleJob = new("DDDDDDDD-0000-0000-0000-000000000004");
        private static readonly Guid TwiceJob = new("FFFFFFFF-0000-0000-0000-000000000005");
        private static readonly Guid OverlapJob = new("99999999-0000-0000-0000-000000000006");
        private static readonly Guid OverlapRunning = new("EEEEEEEE-0000-0000-0000-000000000003");
        private static readonly Guid IndexRunning = new("EEEEEEEE-0000-0000-0000-000000000001");
        private static readonly Guid IndexFailed = new("EEEEEEEE-0000-0000-0000-000000000002");

        private static string ConnectionStringFor(string database)
        {
            var builder = ServerConnectionString == null
                ? new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }
                : new SqlConnectionStringBuilder(ServerConnectionString);
            builder.InitialCatalog = database;
            return builder.ConnectionString;
        }

        private static string JobDBConnectionString => ConnectionStringFor(JobDB);

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            try
            {
                using var master = new SqlConnection(ConnectionStringFor("master"));
                master.Open();
                Execute(master, $"CREATE DATABASE [{JobDB}]");
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
                return;
            }
            using var cn = new SqlConnection(JobDBConnectionString);
            cn.Open();
            Execute(cn, "CREATE SCHEMA jobs");
            Execute(cn, @"
CREATE TABLE jobs.jobs_t(job_name NVARCHAR(128), job_id UNIQUEIDENTIFIER, job_version INT, description NVARCHAR(512), enabled BIT)
CREATE TABLE jobs.job_executions_t(job_execution_id UNIQUEIDENTIFIER, job_name NVARCHAR(128), job_id UNIQUEIDENTIFIER, job_version INT, step_name NVARCHAR(128), step_id INT, is_active BIT, lifecycle NVARCHAR(50), create_time DATETIME2(7), start_time DATETIME2(7), end_time DATETIME2(7), current_attempts INT, last_message NVARCHAR(MAX), target_type NVARCHAR(128), target_server_name NVARCHAR(256), target_database_name NVARCHAR(128))");
            Execute(cn, "CREATE VIEW jobs.jobs AS SELECT * FROM jobs.jobs_t");
            Execute(cn, "CREATE VIEW jobs.job_executions AS SELECT * FROM jobs.job_executions_t");
            Execute(cn, @"
CREATE PROC jobs.sp_start_job @job_name NVARCHAR(128), @job_execution_id UNIQUEIDENTIFIER = NULL OUTPUT
AS
DECLARE @job_id UNIQUEIDENTIFIER
SELECT @job_id = job_id FROM jobs.jobs_t WHERE job_name = @job_name
IF @job_id IS NULL
BEGIN
	RAISERROR('Job not found',16,1)
	RETURN
END
SET @job_execution_id = NEWID()
INSERT INTO jobs.job_executions_t(job_execution_id,job_name,job_id,job_version,step_id,is_active,lifecycle,create_time,current_attempts)
VALUES(@job_execution_id,@job_name,@job_id,1,NULL,1,'Created',SYSUTCDATETIME(),0)");
            Execute(cn, @"
CREATE PROC jobs.sp_stop_job @job_execution_id UNIQUEIDENTIFIER
AS
UPDATE jobs.job_executions_t SET is_active = 0, lifecycle = 'Canceled', end_time = SYSUTCDATETIME() WHERE job_execution_id = @job_execution_id AND is_active = 1");
            Execute(cn, $@"
DECLARE @Now DATETIME2(7) = SYSUTCDATETIME()
INSERT INTO jobs.jobs_t VALUES('PurgeLogs','{PurgeJob}',1,NULL,1),('IndexMaint','{IndexJob}',1,NULL,1),('DropEverything','{DeniedJob}',1,NULL,1),('Idle','{IdleJob}',1,NULL,1),('Twice','{TwiceJob}',1,NULL,1),('Overlap','{OverlapJob}',1,NULL,1)
INSERT INTO jobs.job_executions_t(job_execution_id,job_name,job_id,job_version,step_name,step_id,is_active,lifecycle,create_time,start_time,end_time,current_attempts,last_message,target_type,target_server_name,target_database_name)
VALUES('{IndexFailed}','IndexMaint','{IndexJob}',1,NULL,NULL,0,'Failed',DATEADD(hh,-2,@Now),DATEADD(hh,-2,@Now),DATEADD(hh,-1,@Now),0,'Job execution failed.',NULL,NULL,NULL),
	('{IndexFailed}','IndexMaint','{IndexJob}',1,'Rebuild',1,0,'Failed',DATEADD(hh,-2,@Now),DATEADD(hh,-2,@Now),DATEADD(hh,-1,@Now),1,REPLICATE(CAST(N'x' AS NVARCHAR(MAX)),5000),'SqlDatabase','srv1.database.windows.net','db1'),
	('{IndexRunning}','IndexMaint','{IndexJob}',1,'Rebuild',1,1,'InProgress',DATEADD(mi,-5,@Now),DATEADD(mi,-5,@Now),NULL,1,NULL,'SqlDatabase','srv1.database.windows.net','db1'),
	('{IndexRunning}','IndexMaint','{IndexJob}',1,NULL,NULL,1,'InProgress',DATEADD(mi,-5,@Now),DATEADD(mi,-5,@Now),NULL,0,NULL,NULL,NULL,NULL),
	('{IndexRunning}','IndexMaint','{IndexJob}',1,'Rebuild',1,1,'InProgress',DATEADD(mi,-5,@Now),DATEADD(mi,-5,@Now),NULL,1,NULL,'SqlDatabase','srv1.database.windows.net','db2'),
	/* Overlap: an execution running for a day, and a newer one that has finished */
	('{OverlapRunning}','Overlap','{OverlapJob}',1,NULL,NULL,1,'InProgress',DATEADD(d,-1,@Now),DATEADD(d,-1,@Now),NULL,0,NULL,NULL,NULL,NULL),
	(NEWID(),'Overlap','{OverlapJob}',1,NULL,NULL,0,'Succeeded',DATEADD(hh,-1,@Now),DATEADD(hh,-1,@Now),DATEADD(mi,-50,@Now),0,NULL,NULL,NULL,NULL)");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(ConnectionStringFor("master"));
                master.Open();
                Execute(master, $"IF DB_ID('{JobDB}') IS NOT NULL BEGIN ALTER DATABASE [{JobDB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{JobDB}]; END");
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        private static CollectionConfig Config(string allowedJobs = "*,-DropEverything")
        {
            var cfg = new CollectionConfig { AllowedJobs = allowedJobs };
            cfg.SourceConnections.Add(new DBADashSource(JobDBConnectionString) { ConnectionID = ConnectionID });
            return cfg;
        }

        private static ElasticJobExecutionMessage Message(Guid jobId, ElasticJobExecutionMessage.JobActions action, Guid? executionId = null) =>
            new() { ConnectionID = ConnectionID, JobId = jobId, JobAction = action, JobExecutionId = executionId };

        private static Task<DataSet> Process(ElasticJobExecutionMessage message, CollectionConfig? cfg = null) =>
            message.Process(cfg ?? Config(), Guid.NewGuid(), CancellationToken.None);

        [TestMethod]
        public async Task StartReturnsTheExecutionStarted()
        {
            var message = Message(PurgeJob, ElasticJobExecutionMessage.JobActions.StartJob);
            var ds = await Process(message);

            Assert.IsNotNull(message.JobExecutionId);
            Assert.AreEqual("PurgeLogs", ds.Tables[ElasticJobExecutionMessage.JobTableName]!.Rows[0]["job_name"]);
            var messages = ds.Tables[ElasticJobExecutionMessage.MessagesTableName]!;
            StringAssert.Contains((string)messages.Rows[0]["Message"], message.JobExecutionId.ToString());
            Assert.AreEqual(message.JobExecutionId, messages.Rows[0]["JobExecutionId"]);
            var executions = ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!;
            Assert.AreEqual(1, executions.Rows.Count);
            Assert.AreEqual(message.JobExecutionId, executions.Rows[0]["job_execution_id"]);
            Assert.AreEqual("Created", executions.Rows[0]["lifecycle"]);

            // Then stopped - by its ID, as the GUI does for an execution it started
            ds = await Process(Message(PurgeJob, ElasticJobExecutionMessage.JobActions.StopJob, message.JobExecutionId));
            Assert.AreEqual("Canceled", ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!.Rows[0]["lifecycle"]);
            Assert.AreEqual("Cancellation requested for 1 execution(s).", ds.Tables[ElasticJobExecutionMessage.MessagesTableName]!.Rows[0]["Message"]);
        }

        [TestMethod]
        public async Task StatusIsOfTheLatestExecutionWithTheJobsRowFirst()
        {
            var ds = await Process(Message(IndexJob, ElasticJobExecutionMessage.JobActions.StatusOnly));

            Assert.IsNull(ds.Tables[ElasticJobExecutionMessage.MessagesTableName]);
            var executions = ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!;
            Assert.AreEqual(3, executions.Rows.Count);
            Assert.IsTrue(executions.Rows.Cast<DataRow>().All(r => (Guid)r["job_execution_id"] == IndexRunning));
            Assert.AreEqual(DBNull.Value, executions.Rows[0]["step_id"]);
            // The view's own columns
            Assert.IsTrue(executions.Columns.Contains("target_database_name"));
            Assert.IsTrue(executions.Columns.Contains("job_name"));
        }

        /// <summary>An older execution still running after a newer one finished is the one shown - so it can be stopped</summary>
        [TestMethod]
        public async Task StatusPrefersAnActiveExecution()
        {
            var ds = await Process(Message(OverlapJob, ElasticJobExecutionMessage.JobActions.StatusOnly));

            var executions = ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!;
            Assert.AreEqual(OverlapRunning, executions.Rows[0]["job_execution_id"]);
            Assert.AreEqual(true, executions.Rows[0]["is_active"]);
        }

        [TestMethod]
        public async Task StatusOfAnExecutionTruncatesTheMessage()
        {
            var ds = await Process(Message(IndexJob, ElasticJobExecutionMessage.JobActions.StatusOnly, IndexFailed));

            var executions = ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!;
            Assert.AreEqual(2, executions.Rows.Count);
            Assert.AreEqual("Failed", executions.Rows[0]["lifecycle"]);
            Assert.AreEqual(4000, ((string)executions.Rows[1]["last_message"]).Length);
        }

        [TestMethod]
        public async Task StatusOfAJobThatHasNeverRunIsEmpty()
        {
            var ds = await Process(Message(IdleJob, ElasticJobExecutionMessage.JobActions.StatusOnly));

            Assert.AreEqual(0, ds.Tables[ElasticJobExecutionMessage.ExecutionsTableName]!.Rows.Count);
        }

        [TestMethod]
        public async Task StopIsRefusedForAnotherJobsExecution()
        {
            var ex = await Assert.ThrowsExactlyAsync<SqlException>(() => Process(Message(PurgeJob, ElasticJobExecutionMessage.JobActions.StopJob, IndexRunning)));

            Assert.AreEqual("The job isn't running.", ex.Message);
            Assert.AreEqual(true, Scalar($"SELECT is_active FROM jobs.job_executions_t WHERE job_execution_id = '{IndexRunning}' AND step_id IS NULL"));
        }

        [TestMethod]
        public async Task StopWhenNotRunningFails()
        {
            var ex = await Assert.ThrowsExactlyAsync<SqlException>(() => Process(Message(IdleJob, ElasticJobExecutionMessage.JobActions.StopJob)));

            Assert.AreEqual("The job isn't running.", ex.Message);
        }

        [TestMethod]
        public async Task StopWithoutAnExecutionStopsEveryActiveExecution()
        {
            var first = Message(TwiceJob, ElasticJobExecutionMessage.JobActions.StartJob);
            await Process(first);
            var second = Message(TwiceJob, ElasticJobExecutionMessage.JobActions.StartJob);
            await Process(second);

            var ds = await Process(Message(TwiceJob, ElasticJobExecutionMessage.JobActions.StopJob));

            Assert.AreEqual("Cancellation requested for 2 execution(s).", ds.Tables[ElasticJobExecutionMessage.MessagesTableName]!.Rows[0]["Message"]);
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM jobs.job_executions_t WHERE job_id = '{TwiceJob}' AND is_active = 1"));
        }

        [TestMethod]
        public async Task DeniedJobIsNotStarted()
        {
            var ex = await Assert.ThrowsExactlyAsync<Exception>(() => Process(Message(DeniedJob, ElasticJobExecutionMessage.JobActions.StartJob)));

            Assert.AreEqual($"Job execution is denied for DropEverything with id {DeniedJob}", ex.Message);
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM jobs.job_executions_t WHERE job_id = '{DeniedJob}'"));
        }

        [TestMethod]
        public async Task JobsAreMatchedByID()
        {
            var ds = await Process(Message(IndexJob, ElasticJobExecutionMessage.JobActions.StatusOnly), Config(IndexJob.ToString()));

            Assert.AreEqual("IndexMaint", ds.Tables[ElasticJobExecutionMessage.JobTableName]!.Rows[0]["job_name"]);
            await Assert.ThrowsExactlyAsync<Exception>(() => Process(Message(PurgeJob, ElasticJobExecutionMessage.JobActions.StatusOnly), Config(IndexJob.ToString())));
        }

        [TestMethod]
        public async Task DisabledWhenNoJobsAreAllowed()
        {
            var ex = await Assert.ThrowsExactlyAsync<Exception>(() => Process(Message(PurgeJob, ElasticJobExecutionMessage.JobActions.StartJob), Config("")));

            StringAssert.StartsWith(ex.Message, "Job execution is disabled");
        }

        [TestMethod]
        public async Task UnknownJobFails()
        {
            var unknown = Guid.NewGuid();
            var ex = await Assert.ThrowsExactlyAsync<Exception>(() => Process(Message(unknown, ElasticJobExecutionMessage.JobActions.StartJob)));

            Assert.AreEqual($"Elastic job {unknown} wasn't found in the job database", ex.Message);
        }

        private static object Scalar(string sql)
        {
            using var cn = new SqlConnection(JobDBConnectionString);
            cn.Open();
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
