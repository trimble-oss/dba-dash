using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace DBADash.Test
{
    /// <summary>
    /// The Service Broker collection run against a database set up with a problem for each check, imported with dbo.ServiceBroker_Upd
    /// as DBImporter does, then the status views and report read back.  The collection is off by default so CI's end-to-end run
    /// wouldn't otherwise exercise these checks - on any of the SQL versions or the binary server collation CI tests.
    ///
    /// <para>The snapshot is imported 20 mins in the future so the age checks fire (15 min queue & transmission queue warning
    /// thresholds, 10 mins for activation that's notified without anything receiving) without the test waiting for them.</para>
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  Both databases are dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class ServiceBrokerSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Suffix = Guid.NewGuid().ToString("N");
        private static readonly string Repository = "DBADashSBRepoTest_" + Suffix;
        private static readonly string BrokerDB = "DBADashSBTest_" + Suffix;
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string RepositoryConnectionString => ConnectionStringFor(Repository);
        private static string BrokerConnectionString => ConnectionStringFor(BrokerDB);

        private static DataSet collected = new();
        private static int instanceID;
        private static int databaseID;

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
            CreateBrokerDatabase();
            collected = Collect();
            Import(collected);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                foreach (var db in new[] { Repository, BrokerDB })
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
        /// A queue for each queue check, a message to a service that doesn't exist (transmission queue) and a conversation ended with
        /// an error (ER on the initiator).  Waits for the queue whose activation procedure doesn't RECEIVE to be NOTIFIED.
        /// </summary>
        private static void CreateBrokerDatabase()
        {
            using (var master = new SqlConnection(MasterConnectionString))
            {
                master.Open();
                Execute(master, $"CREATE DATABASE [{BrokerDB}]");
                Execute(master, $"ALTER DATABASE [{BrokerDB}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE");
            }
            using var cn = new SqlConnection(BrokerConnectionString);
            cn.Open();
            Execute(cn, "CREATE PROC dbo.NoReceive AS RETURN");
            Execute(cn, @"
CREATE QUEUE dbo.InitiatorQueue
CREATE SERVICE InitiatorService ON QUEUE dbo.InitiatorQueue
/* Messages that sit waiting - no activation */
CREATE QUEUE dbo.WaitingQueue
CREATE SERVICE WaitingService ON QUEUE dbo.WaitingQueue([DEFAULT])
/* Activation that fires but nothing receives */
CREATE QUEUE dbo.NotReceivingQueue WITH ACTIVATION(STATUS = ON, PROCEDURE_NAME = dbo.NoReceive, MAX_QUEUE_READERS = 1, EXECUTE AS OWNER)
CREATE SERVICE NotReceivingService ON QUEUE dbo.NotReceivingQueue([DEFAULT])
/* Received messages are retained, so they're in the queue's internal table */
CREATE QUEUE dbo.RetentionQueue WITH RETENTION = ON
CREATE SERVICE RetentionService ON QUEUE dbo.RetentionQueue([DEFAULT])
/* Disabled by poison message handling (below), and disabled by hand with poison message handling off */
CREATE QUEUE dbo.PoisonQueue
CREATE SERVICE PoisonService ON QUEUE dbo.PoisonQueue([DEFAULT])
CREATE QUEUE dbo.SwitchedOffQueue WITH STATUS = OFF, POISON_MESSAGE_HANDLING(STATUS = OFF)
/* Same name in another schema - thresholds for one mustn't apply to the other */
EXEC('CREATE SCHEMA audit')
CREATE QUEUE audit.WaitingQueue
/* SqlDependency creates a queue per listener */
CREATE QUEUE dbo.[SqlQueryNotificationService-11111111-1111-1111-1111-111111111111]
CREATE QUEUE dbo.[SqlQueryNotificationService-22222222-2222-2222-2222-222222222222]");
            Execute(cn, @"
DECLARE @h UNIQUEIDENTIFIER, @i INT = 0
WHILE @i < 3
BEGIN
	BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE 'WaitingService' WITH ENCRYPTION = OFF;
	SEND ON CONVERSATION @h (0x01);
	BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE 'RetentionService' WITH ENCRYPTION = OFF;
	SEND ON CONVERSATION @h (0x01);
	SET @i += 1
END
BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE 'NotReceivingService' WITH ENCRYPTION = OFF;
SEND ON CONVERSATION @h (0x01);
/* Messages to services that don't exist stay in the transmission queue.  More target services than the collection returns individually. */
DECLARE @service NVARCHAR(256)
SET @i = 0
WHILE @i < 55
BEGIN
	SET @service = CONCAT(N'MissingService', @i)
	BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE @service WITH ENCRYPTION = OFF;
	SEND ON CONVERSATION @h (0x01);
	SET @i += 1
END;
/* One conversation group each, so this receives one of the three */
RECEIVE TOP(1) * FROM dbo.RetentionQueue;
/* The target ends with an error - the initiator's endpoint goes to ER */
BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE 'WaitingService' WITH ENCRYPTION = OFF;
SEND ON CONVERSATION @h (0x02);
SELECT TOP(1) @h = conversation_handle FROM dbo.WaitingQueue WHERE message_body = 0x02;
END CONVERSATION @h WITH ERROR = 50001 DESCRIPTION = N'Test';");

            // 5 rollbacks of a RECEIVE in a row - poison message handling disables the queue
            Execute(cn, @"
DECLARE @h UNIQUEIDENTIFIER
BEGIN DIALOG @h FROM SERVICE InitiatorService TO SERVICE 'PoisonService' WITH ENCRYPTION = OFF;
SEND ON CONVERSATION @h (0x01);");
            for (var i = 0; i < 5; i++)
            {
                Execute(cn, "BEGIN TRAN; RECEIVE TOP(1) * FROM dbo.PoisonQueue; ROLLBACK;");
            }
            Assert.AreEqual(false, Scalar(cn, "SELECT is_receive_enabled FROM sys.service_queues WHERE name = 'PoisonQueue'"), "Poison message handling disabled the queue");

            for (var i = 0; i < 60; i++)
            {
                if (Scalar(cn, "SELECT COUNT(*) FROM sys.dm_broker_queue_monitors WHERE database_id = DB_ID() AND queue_id = OBJECT_ID('dbo.NotReceivingQueue') AND state = 'NOTIFIED'") is 1) return;
                Thread.Sleep(1000);
            }
            Assert.Fail("NotReceivingQueue wasn't NOTIFIED within 60 seconds.");
        }

        /// <summary>The collection query as DBCollector runs it.  It visits every broker enabled database on the server - only the test database is imported.</summary>
        private static DataSet Collect(int maxDatabases = CollectionConfig.DefaultServiceBrokerMaxDatabases)
        {
            var ds = new DataSet();
            using var cn = new SqlConnection(MasterConnectionString);
            using var cmd = new SqlCommand(SqlStrings.ServiceBroker, cn) { CommandTimeout = 120 };
            cmd.Parameters.AddWithValue("MaxDatabases", maxDatabases);
            new SqlDataAdapter(cmd).Fill(ds);
            return ds;
        }

        /// <summary>
        /// Over the database limit the collection fails with an error saying how to change it, rather than visiting them all.  The server has
        /// at least msdb and the test database with Service Broker enabled.
        /// </summary>
        [TestMethod]
        public void DatabaseLimitFailsTheCollection()
        {
            var ex = Assert.ThrowsExactly<SqlException>(() => Collect(maxDatabases: 1));
            StringAssert.Contains(ex.Message, "over the limit of 1");
            StringAssert.Contains(ex.Message, "ServiceBrokerMaxDatabases");
        }

        [TestMethod]
        public void NoDatabaseLimit()
        {
            Assert.AreEqual(ServiceBrokerTables.TableNames.Length, Collect(maxDatabases: 0).Tables.Count);
        }

        /// <summary>As DBImporter: each result set to its table-valued parameter, only where it has rows.</summary>
        private static void Import(DataSet ds)
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            Execute(cn, $@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition) VALUES(N'SQL1',N'SQL1',1,1,3)
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state)
VALUES(SCOPE_IDENTITY(),N'{BrokerDB}',DB_ID(N'{BrokerDB}'),SYSUTCDATETIME(),160,0,0,0,0,0,0,0,1,0,1,0)");
            instanceID = (int)Scalar(cn, "SELECT InstanceID FROM dbo.Instances WHERE Instance = N'SQL1'");
            databaseID = (int)Scalar(cn, $"SELECT DatabaseID FROM dbo.Databases WHERE name = N'{BrokerDB}'");

            using var cmd = new SqlCommand("dbo.ServiceBroker_Upd", cn) { CommandType = CommandType.StoredProcedure };
            for (var i = 0; i < ServiceBrokerTables.TableNames.Length; i++)
            {
                if (ds.Tables[i].Rows.Count > 0) cmd.Parameters.AddWithValue(ServiceBrokerTables.TableNames[i], ds.Tables[i]);
            }
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("SnapshotDate", DateTime.UtcNow.AddMinutes(20));
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Table-valued parameters bind by ordinal, so a column out of place in the collection query or its user defined type would be
        /// imported into the wrong column - possibly without an error.  The names have to line up as well as the count.
        /// </summary>
        [TestMethod]
        public void CollectionColumnsMatchTableTypes()
        {
            Assert.AreEqual(ServiceBrokerTables.TableNames.Length, collected.Tables.Count, "Result sets returned by the collection");
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            for (var i = 0; i < ServiceBrokerTables.TableNames.Length; i++)
            {
                var typeColumns = new List<string>();
                using var cmd = new SqlCommand(@"
SELECT c.name
FROM sys.table_types tt
JOIN sys.columns c ON c.object_id = tt.type_table_object_id
WHERE tt.name = @name
AND tt.schema_id = SCHEMA_ID('dbo')
ORDER BY c.column_id", cn);
                cmd.Parameters.AddWithValue("name", ServiceBrokerTables.TableNames[i]);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) typeColumns.Add(rdr.GetString(0));
                }
                var collectedColumns = collected.Tables[i].Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                CollectionAssert.AreEqual(typeColumns, collectedColumns, $"{ServiceBrokerTables.TableNames[i]}: collection [{string.Join(",", collectedColumns)}] vs type [{string.Join(",", typeColumns)}]");
            }
        }

        [TestMethod]
        public void OldMessagesWaitingIsAWarning()
        {
            var queue = Queue("WaitingQueue");
            Assert.AreEqual(3L, queue["message_count"]);
            Assert.AreEqual(2, queue["QueueAgeStatus"], "Oldest message ~20 mins old against the 15 min warning threshold");
            Assert.AreEqual(2, queue["QueueStatus"]);
            StringAssert.Contains((string)queue["Issues"], "Oldest message has been waiting");
        }

        [TestMethod]
        public void ActivationNotReceivingIsAWarning()
        {
            var queue = Queue("NotReceivingQueue");
            Assert.AreEqual("NOTIFIED", queue["monitor_state"]);
            Assert.AreEqual(2, queue["QueueStatus"]);
            StringAssert.Contains((string)queue["Issues"], "Activation was notified");
        }

        /// <summary>
        /// Poison message handling disables receive and enqueue, the same as STATUS = OFF - one critical issue, not one for each, that names
        /// the likely cause and how to re-enable the queue.
        /// </summary>
        [TestMethod]
        public void PoisonMessageDisabledQueueIsCritical()
        {
            var queue = Queue("PoisonQueue");
            Assert.AreEqual(false, queue["is_enabled"]);
            Assert.AreEqual(1, queue["EnabledStatus"]);
            Assert.AreEqual(1, queue["QueueStatus"]);
            var issues = (string)queue["Issues"];
            StringAssert.StartsWith(issues, "Queue is disabled - ");
            StringAssert.Contains(issues, "poison message");
            StringAssert.Contains(issues, "ALTER QUEUE [dbo].[PoisonQueue] WITH STATUS = ON");
            Assert.AreEqual(1, issues.Split("Queue is disabled").Length - 1, "One disabled issue: " + issues);
        }

        /// <summary>With poison message handling off the queue can only have been disabled with ALTER QUEUE, so a poison message isn't suggested.</summary>
        [TestMethod]
        public void SwitchedOffQueueIsCritical()
        {
            var queue = Queue("SwitchedOffQueue");
            Assert.AreEqual(false, queue["is_enabled"]);
            Assert.AreEqual(1, queue["QueueStatus"]);
            var issues = (string)queue["Issues"];
            StringAssert.StartsWith(issues, "Queue is disabled (STATUS = OFF)");
            StringAssert.Contains(issues, "Poison message handling is off");
        }

        [TestMethod]
        public void EnabledQueue()
        {
            var queue = Queue("WaitingQueue");
            Assert.AreEqual(true, queue["is_enabled"]);
            Assert.AreEqual(4, queue["EnabledStatus"]);
        }

        [TestMethod]
        public void RetentionQueueDepthExcludesReceivedMessages()
        {
            var queue = Queue("RetentionQueue");
            Assert.AreEqual(true, queue["is_retention_enabled"]);
            Assert.AreEqual(2L, queue["message_count"], "3 sent, 1 received and retained");
        }

        [TestMethod]
        public void SqlDependencyQueuesAreRolledUp()
        {
            var rows = Query($"SELECT queue_name FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID} AND queue_name LIKE N'SqlQueryNotificationService-%'");
            Assert.AreEqual(1, rows.Rows.Count);
            Assert.AreEqual("SqlQueryNotificationService-*", rows.Rows[0]["queue_name"]);
        }

        [TestMethod]
        public void DatabaseChecks()
        {
            var db = Query($"SELECT * FROM dbo.ServiceBrokerDatabaseInfo WHERE DatabaseID = {databaseID}").Rows.Cast<DataRow>().Single();
            Assert.AreEqual(55L, db["transmission_queue_count"], "Messages to services that don't exist - including those beyond the groups returned individually");
            Assert.AreEqual(2, db["TransmissionQueueStatus"]);
            Assert.AreEqual(1L, db["error_count"], "Initiator of the conversation the target ended with an error");
            Assert.AreEqual(2, db["ErrorConversationStatus"]);
            Assert.AreEqual(2, db["DatabaseStatus"]);
        }

        [TestMethod]
        public void InstanceStatusIncludesQueuesAndDatabases()
        {
            var instance = Query($"SELECT * FROM dbo.ServiceBrokerInstanceInfo WHERE InstanceID = {instanceID}").Rows.Cast<DataRow>().Single();
            Assert.AreEqual(1, instance["ServiceBrokerStatus"], "The disabled queues are critical");
            // The status comes from the queues and database, so Issues says so rather than being blank
            var issues = (string)instance["Issues"];
            StringAssert.StartsWith(issues, "Queues: 2 critical, ");
            StringAssert.Contains(issues, " warning.");
            StringAssert.Contains(issues, "Databases: 1 warning.");
        }

        /// <summary>Over the conversation scan limit the collection returns the total with state '??' - counted as open as the states are unknown.</summary>
        [TestMethod]
        public void UncountedConversationsAreOpen()
        {
            var before = (long)Query($"SELECT conversation_count FROM dbo.ServiceBrokerDatabaseInfo WHERE DatabaseID = {databaseID}").Rows[0][0];
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using var cmd = new SqlCommand($@"
INSERT INTO dbo.ServiceBrokerConversations(InstanceID,DatabaseID,state,conversation_count,SnapshotDate)
SELECT TOP(1) InstanceID,DatabaseID,'??',2000000,SnapshotDate FROM dbo.ServiceBrokerConversations WHERE DatabaseID = {databaseID}
SELECT conversation_count, ConversationStatus FROM dbo.ServiceBrokerDatabaseInfo WHERE DatabaseID = {databaseID}", cn, tran);
            using (var rdr = cmd.ExecuteReader())
            {
                Assert.IsTrue(rdr.Read());
                Assert.AreEqual(before + 2000000, rdr.GetInt64(0));
                Assert.AreEqual(2, rdr.GetInt32(1), "Over the 100,000 open conversation warning threshold");
            }
            tran.Rollback();
        }

        [TestMethod]
        public void ReportReturnsEveryResultSet()
        {
            foreach (var forDatabase in new[] { false, true })
            {
                using var cn = new SqlConnection(RepositoryConnectionString);
                using var cmd = new SqlCommand("dbo.ServiceBrokerReport_Get", cn) { CommandType = CommandType.StoredProcedure };
                var ids = new DataTable();
                ids.Columns.Add("ID", typeof(int));
                ids.Rows.Add(instanceID);
                cmd.Parameters.Add(new SqlParameter("InstanceIDs", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = ids });
                if (forDatabase) cmd.Parameters.AddWithValue("DatabaseID", databaseID);
                var ds = new DataSet();
                new SqlDataAdapter(cmd).Fill(ds);
                Assert.AreEqual(7, ds.Tables.Count, $"Result sets (DatabaseID {(forDatabase ? "set" : "not set")})");
                Assert.AreEqual(8, ds.Tables[2].Rows.Count, "Queues: initiator, waiting (in 2 schemas), not receiving, retention, poison, switched off and the SqlDependency roll up");
            }
        }

        /// <summary>
        /// 55 target services: the largest 49 groups are returned individually and the rest combined, so the database total (checked in
        /// <see cref="DatabaseChecks"/>) and the oldest message stay complete.
        /// </summary>
        [TestMethod]
        public void TransmissionQueueGroupsAreLimited()
        {
            var groups = Query($"SELECT to_service_name, message_count FROM dbo.ServiceBrokerTransmissionQueue WHERE DatabaseID = {databaseID}");
            Assert.IsTrue(groups.Rows.Count is 50 or 51, $"Groups: {groups.Rows.Count}.  49 individually, then 1 or 2 for the rest depending whether they have a transmission status yet");
            Assert.AreEqual(55L, groups.Rows.Cast<DataRow>().Sum(r => (long)r["message_count"]));
            Assert.IsTrue(groups.Rows.Cast<DataRow>().Any(r => ((string)r["to_service_name"]).StartsWith("(") && ((string)r["to_service_name"]).Contains("other")), "Combined row for the remaining groups");
        }

        /// <summary>Queue names are only unique within a schema - a queue level threshold applies to that schema's queue only.</summary>
        [TestMethod]
        public void QueueThresholdsAreSchemaSpecific()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using var cmd = new SqlCommand($@"
EXEC dbo.ServiceBrokerThresholds_Upd @InstanceID = {instanceID}, @DatabaseID = {databaseID}, @schema_name = N'dbo', @queue_name = N'WaitingQueue',
	@QueueDepthWarningThreshold = 1, @QueueDepthCriticalThreshold = NULL, @QueueAgeWarningThreshold = NULL, @QueueAgeCriticalThreshold = NULL,
	@TransmissionQueueAgeWarningThreshold = NULL, @TransmissionQueueAgeCriticalThreshold = NULL, @ConversationCountWarningThreshold = NULL, @ConversationCountCriticalThreshold = NULL
SELECT schema_name, QueueDepthWarningThreshold, ThresholdConfigurationLevel FROM dbo.ServiceBrokerQueueInfo WHERE DatabaseID = {databaseID} AND queue_name = N'WaitingQueue'", cn, tran);
            var dt = new DataTable();
            using (var rdr = cmd.ExecuteReader()) dt.Load(rdr);
            var dbo = dt.Rows.Cast<DataRow>().Single(r => (string)r["schema_name"] == "dbo");
            var audit = dt.Rows.Cast<DataRow>().Single(r => (string)r["schema_name"] == "audit");
            Assert.AreEqual(1L, dbo["QueueDepthWarningThreshold"]);
            Assert.AreEqual("Queue", dbo["ThresholdConfigurationLevel"]);
            Assert.AreEqual(10000L, audit["QueueDepthWarningThreshold"], "Root threshold");
            Assert.AreEqual("Root", audit["ThresholdConfigurationLevel"]);
            tran.Rollback();
        }

        /// <summary>
        /// A collection's counter deltas cover the time since the previous collection, which can be longer than the chart's bucket.
        /// 1,800 SENDs over 30 minutes is 1/sec, whatever the bucket width.
        /// </summary>
        [TestMethod]
        public void ActivityRateUsesTimeSampled()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using var cmd = new SqlCommand($@"
DECLARE @SnapshotDate DATETIME2(2) = DATEADD(d,-1,SYSUTCDATETIME())
INSERT INTO dbo.ServiceBrokerStats(InstanceID,SnapshotDate,sample_ms_diff,sends)
VALUES({instanceID},@SnapshotDate,1800000,1800)
DECLARE @IDs dbo.IDs
INSERT INTO @IDs VALUES({instanceID})
DECLARE @From DATETIME2(2) = DATEADD(mi,-10,@SnapshotDate), @To DATETIME2(2) = DATEADD(mi,10,@SnapshotDate)
EXEC dbo.ServiceBrokerReport_Get @InstanceIDs = @IDs, @FromDate = @From, @ToDate = @To, @DateGroupingMin = 5", cn, tran);
            var ds = new DataSet();
            new SqlDataAdapter(cmd).Fill(ds);
            var sends = ds.Tables[5].Rows.Cast<DataRow>().Single(r => (string)r["Measure"] == "SENDs/sec");
            Assert.AreEqual(1.0, Convert.ToDouble(sends["ValuePerSec"]), 0.0001);
            tran.Rollback();
        }

        /// <summary>Top N is the number of lines on the queue depth chart - transmission queues are ranked with the queues, not added on top.</summary>
        [TestMethod]
        public void QueueDepthChartIsLimitedToTopN()
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            using var cmd = new SqlCommand("dbo.ServiceBrokerReport_Get", cn) { CommandType = CommandType.StoredProcedure };
            var ids = new DataTable();
            ids.Columns.Add("ID", typeof(int));
            ids.Rows.Add(instanceID);
            cmd.Parameters.Add(new SqlParameter("InstanceIDs", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = ids });
            cmd.Parameters.AddWithValue("TopN", 2);
            cmd.Parameters.AddWithValue("ToDate", DateTime.UtcNow.AddHours(1)); // The snapshot was imported in the future
            var ds = new DataSet();
            new SqlDataAdapter(cmd).Fill(ds);
            var series = ds.Tables[4].Rows.Cast<DataRow>().Select(r => (string)r["Queue"]).Distinct().ToList();
            Assert.AreEqual(2, series.Count, "Series: " + string.Join(", ", series));
            Assert.IsTrue(series.Any(s => s.StartsWith("Transmission Queue: ")), "The transmission queue (55 messages) is the deepest");
            Assert.IsTrue(series.Any(s => s.EndsWith(".dbo.WaitingQueue")), "Then the deepest queue (3 messages)");
        }

        /// <summary>
        /// A database the collection could not read is reported on its own, and the import leaves that database's queues, transmission
        /// queue and conversations as they were.  Activation counters for it are still stored, but the queue and conversation totals are
        /// NULL rather than 0 - a 0 would be a dip on the chart.  A snapshot that doesn't list it removes them, which is a queue being dropped.
        /// </summary>
        [TestMethod]
        public void FailedDatabaseKeepsCollectedState()
        {
            var activeBefore = (int)Query($"SELECT COUNT(*) FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID} AND IsActive = 1").Rows[0][0];
            var transmissionBefore = (int)Query($"SELECT COUNT(*) FROM dbo.ServiceBrokerTransmissionQueue WHERE DatabaseID = {databaseID}").Rows[0][0];
            var conversationsBefore = (int)Query($"SELECT COUNT(*) FROM dbo.ServiceBrokerConversations WHERE DatabaseID = {databaseID}").Rows[0][0];
            Assert.IsTrue(activeBefore > 0 && transmissionBefore > 0 && conversationsBefore > 0);

            var failed = new DataTable();
            failed.Columns.Add("database_name", typeof(string));
            failed.Rows.Add(BrokerDB);
            var counters = new DataTable();
            counters.Columns.Add("instance_name", typeof(string));
            counters.Columns.Add("counter_name", typeof(string));
            counters.Columns.Add("cntr_value", typeof(long));
            counters.Rows.Add(BrokerDB, "Tasks Aborted/sec", 15L);

            // SnapshotDate is datetime2(2). A datetime parameter keeps .003/.007 seconds, which do not equal that column after it is rounded, so the lookup misses the row.
            var snapshot = new DateTime(DateTime.UtcNow.AddHours(2).Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using (var setup = new SqlCommand(@"
DELETE Staging.ServiceBrokerCounters
WHERE InstanceID = @InstanceID AND instance_name = @DB AND counter_name = N'Tasks Aborted/sec'
INSERT INTO Staging.ServiceBrokerCounters(InstanceID,instance_name,counter_name,cntr_value,SnapshotDate)
VALUES(@InstanceID,@DB,N'Tasks Aborted/sec',10,DATEADD(mi,-5,@Snapshot))", cn, tran))
            {
                setup.Parameters.AddWithValue("InstanceID", instanceID);
                setup.Parameters.AddWithValue("DB", BrokerDB);
                setup.Parameters.AddWithValue("Snapshot", snapshot);
                setup.ExecuteNonQuery();
            }

            void ImportSnapshot(DateTime snapshotDate, bool databaseFailed)
            {
                using var cmd = new SqlCommand("dbo.ServiceBroker_Upd", cn, tran) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                if (databaseFailed)
                {
                    cmd.Parameters.AddWithValue(ServiceBrokerTables.DatabasesTableName, failed);
                    cmd.Parameters.AddWithValue(ServiceBrokerTables.CountersTableName, counters);
                }
                cmd.ExecuteNonQuery();
            }

            ImportSnapshot(snapshot, databaseFailed: true);

            var kept = new DataTable();
            using (var cmd = new SqlCommand($@"
SELECT
	(SELECT COUNT(*) FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID} AND IsActive = 1) AS ActiveQueues,
	(SELECT COUNT(*) FROM dbo.ServiceBrokerTransmissionQueue WHERE DatabaseID = {databaseID}) AS TransmissionGroups,
	(SELECT COUNT(*) FROM dbo.ServiceBrokerConversations WHERE DatabaseID = {databaseID}) AS ConversationStates,
	(SELECT transmission_queue_count FROM dbo.ServiceBrokerDatabaseStats WHERE DatabaseID = {databaseID} AND SnapshotDate = @Snapshot) AS TransmissionCount,
	(SELECT conversation_count FROM dbo.ServiceBrokerDatabaseStats WHERE DatabaseID = {databaseID} AND SnapshotDate = @Snapshot) AS ConversationCount,
	(SELECT tasks_aborted FROM dbo.ServiceBrokerDatabaseStats WHERE DatabaseID = {databaseID} AND SnapshotDate = @Snapshot) AS TasksAborted", cn, tran))
            {
                cmd.Parameters.AddWithValue("Snapshot", snapshot);
                using var rdr = cmd.ExecuteReader();
                kept.Load(rdr);
            }
            var row = kept.Rows[0];
            Assert.AreEqual(activeBefore, Convert.ToInt32(row["ActiveQueues"]));
            Assert.AreEqual(transmissionBefore, Convert.ToInt32(row["TransmissionGroups"]));
            Assert.AreEqual(conversationsBefore, Convert.ToInt32(row["ConversationStates"]));
            Assert.IsTrue(row["TransmissionCount"] is DBNull, "Transmission queue wasn't read");
            Assert.IsTrue(row["ConversationCount"] is DBNull, "Conversations weren't read");
            Assert.IsFalse(row.IsNull("TasksAborted"), "Activation counters are instance level and still diffed");
            Assert.AreEqual(5L, Convert.ToInt64(row["TasksAborted"]), "Activation counters are instance level and still diffed");

            ImportSnapshot(snapshot.AddHours(1), databaseFailed: false);
            Assert.AreEqual(0, (int)Scalar(cn, tran, $"SELECT COUNT(*) FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID} AND IsActive = 1"), "Not listed as failed, so the queues are gone");
            Assert.AreEqual(0, (int)Scalar(cn, tran, $"SELECT COUNT(*) FROM dbo.ServiceBrokerTransmissionQueue WHERE DatabaseID = {databaseID}"));
            Assert.AreEqual(0, (int)Scalar(cn, tran, $"SELECT COUNT(*) FROM dbo.ServiceBrokerConversations WHERE DatabaseID = {databaseID}"));
            tran.Rollback();
        }

        /// <summary>
        /// The import records when a queue is enabled or disabled.  Queues that haven't changed since they were first collected have no date -
        /// when they changed isn't known.  Re-imported with PoisonQueue enabled, in a transaction that's rolled back.
        /// </summary>
        [TestMethod]
        public void StatusChangedDateIsTracked()
        {
            Assert.IsTrue(Query($"SELECT status_changed_date FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID}").Rows.Cast<DataRow>().All(r => r[0] is DBNull), "Not changed since first collected");

            var queues = collected.Tables[0].Copy();
            var poison = queues.Rows.Cast<DataRow>().Single(r => (string)r["database_name"] == BrokerDB && (string)r["queue_name"] == "PoisonQueue");
            poison["is_receive_enabled"] = true;
            poison["is_enqueue_enabled"] = true;
            var snapshotDate = DateTime.UtcNow.AddHours(1);

            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            using (var cmd = new SqlCommand("dbo.ServiceBroker_Upd", cn, tran) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue(ServiceBrokerTables.QueuesTableName, queues);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.ExecuteNonQuery();
            }
            var dt = new DataTable();
            using (var cmd = new SqlCommand($"SELECT queue_name, status_changed_date FROM dbo.ServiceBrokerQueues WHERE DatabaseID = {databaseID}", cn, tran))
            using (var rdr = cmd.ExecuteReader()) dt.Load(rdr);
            tran.Rollback();

            foreach (DataRow row in dt.Rows)
            {
                if ((string)row["queue_name"] == "PoisonQueue")
                    Assert.IsTrue(row["status_changed_date"] is DateTime changed && Math.Abs((changed - snapshotDate).TotalSeconds) < 1, $"Enabled in this snapshot: {row["status_changed_date"]} vs {snapshotDate}");
                else
                    Assert.IsTrue(row["status_changed_date"] is DBNull, $"{row["queue_name"]} didn't change");
            }
        }

        private static string Key(string check) => $"ServiceBroker {check}";

        private static string QueueLine(string queue, string status) => $"\n{BrokerDB}.dbo.{queue} - {status}: ";

        /// <summary>
        /// Default settings.  Both disabled queues were disabled before they were first collected, so they're left out until PoisonQueue is marked
        /// as disabled recently.  NotReceivingQueue's activation warning alerts as the activation check defaults to warning or critical.  The queue
        /// & transmission queue ages are warnings, which default to critical only.
        /// </summary>
        [TestMethod]
        public void AlertOnRecentlyDisabledQueue()
        {
            var alerts = RunServiceBrokerAlerts("NULL");
            CollectionAssert.AreEquivalent(new[] { Key("Queue Activation") }, alerts.Keys, string.Join(", ", alerts.Keys));
            StringAssert.Contains(alerts[Key("Queue Activation")], QueueLine("NotReceivingQueue", "Warning") + "Activation was notified");

            alerts = RunServiceBrokerAlerts("NULL", $"UPDATE dbo.ServiceBrokerQueues SET status_changed_date = SYSUTCDATETIME() WHERE DatabaseID = {databaseID} AND queue_name = N'PoisonQueue'");
            CollectionAssert.AreEquivalent(new[] { Key("Queue Activation"), Key("Disabled Queues") }, alerts.Keys, string.Join(", ", alerts.Keys));
            var disabled = alerts[Key("Disabled Queues")];
            StringAssert.StartsWith(disabled, "Service Broker Disabled Queues (critical): 1");
            StringAssert.Contains(disabled, QueueLine("PoisonQueue", "Critical") + "Queue is disabled - ");
            Assert.IsFalse(disabled.Contains("SwitchedOffQueue"), disabled);
        }

        /// <summary>
        /// A queue disabled recently stays in the open alert after the recently disabled window has passed - the alert doesn't resolve while it's still
        /// disabled.  Time passing is simulated by moving the queue's status change and the alert's trigger date back 30 days.  A queue disabled before
        /// it was first collected still isn't added.
        /// </summary>
        [TestMethod]
        public void DisabledQueueAlertStaysActive()
        {
            var alerts = RunServiceBrokerAlerts("NULL",
                $"UPDATE dbo.ServiceBrokerQueues SET status_changed_date = SYSUTCDATETIME() WHERE DatabaseID = {databaseID} AND queue_name = N'PoisonQueue'",
                $@"UPDATE dbo.ServiceBrokerQueues SET status_changed_date = DATEADD(d,-30,status_changed_date) WHERE DatabaseID = {databaseID} AND queue_name = N'PoisonQueue'
UPDATE Alert.ActiveAlerts SET TriggerDate = DATEADD(d,-30,TriggerDate) WHERE AlertType = 'ServiceBroker'");
            CollectionAssert.Contains(alerts.Keys, Key("Disabled Queues"));
            StringAssert.Contains(alerts[Key("Disabled Queues")], QueueLine("PoisonQueue", "Critical"));
            Assert.IsFalse(alerts[Key("Disabled Queues")].Contains("SwitchedOffQueue"), alerts[Key("Disabled Queues")]);
        }

        [TestMethod]
        public void AlertOnAllDisabledQueues()
        {
            var alerts = RunServiceBrokerAlerts(@"N'{""RecentlyDisabledHours"":0,""QueueActivation"":""Off""}'");
            CollectionAssert.AreEquivalent(new[] { Key("Disabled Queues") }, alerts.Keys, string.Join(", ", alerts.Keys));
            var disabled = alerts[Key("Disabled Queues")];
            StringAssert.StartsWith(disabled, "Service Broker Disabled Queues (critical): 2");
            StringAssert.Contains(disabled, QueueLine("PoisonQueue", "Critical"));
            StringAssert.Contains(disabled, QueueLine("SwitchedOffQueue", "Critical"));
        }

        /// <summary>Each check has its own level and its own alert, listing what fails that check with that check's issue only.</summary>
        [TestMethod]
        public void AlertLevelPerCheck()
        {
            var alerts = RunServiceBrokerAlerts(@"N'{""QueueActivation"":""Off"",""QueueAge"":""WarningOrCritical"",""TransmissionQueue"":""WarningOrCritical"",""ErrorConversations"":""WarningOrCritical""}'");
            CollectionAssert.AreEquivalent(new[] { Key("Queue Oldest Message"), Key("Transmission Queue"), Key("Error Conversations") }, alerts.Keys, string.Join(", ", alerts.Keys));
            var age = alerts[Key("Queue Oldest Message")];
            StringAssert.StartsWith(age, "Service Broker Queue Oldest Message (warning): ");
            StringAssert.Contains(age, QueueLine("WaitingQueue", "Warning") + "Oldest message has been waiting ");
            Assert.IsFalse(age.Contains("PoisonQueue"), "Disabled before it was first collected: " + age);
            Assert.IsFalse(age.Contains("Activation"), "Only this check's issues: " + age);
            StringAssert.Contains(alerts[Key("Error Conversations")], $"\n{BrokerDB} - Warning: 1 conversations in the error state");
        }

        [TestMethod]
        public void AlertDatabaseFilter()
        {
            var alerts = RunServiceBrokerAlerts(@"N'{""QueueAge"":""WarningOrCritical"",""ErrorConversations"":""WarningOrCritical"",""DatabaseName"":""NoSuchDB%""}'");
            Assert.AreEqual(0, alerts.Count, string.Join(", ", alerts.Keys));
        }

        /// <summary>
        /// Adds a ServiceBroker rule with the details (a SQL literal), then for each of the SQL batches runs it followed by the alert proc.
        /// Returns the active (unresolved) alerts - key and message.  Rolled back.
        /// </summary>
        private static Dictionary<string, string> RunServiceBrokerAlerts(string details, params string[] batches)
        {
            using var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            void Run(string sql)
            {
                using var cmd = new SqlCommand(sql, cn, tran);
                cmd.ExecuteNonQuery();
            }
            Run($"INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,IsActive,Details) VALUES('ServiceBroker','ServiceBroker%',10,-1,NULL,1,{details})");
            foreach (var sql in batches.DefaultIfEmpty(""))
            {
                if (sql != "") Run(sql);
                Run("EXEC Alert.ServiceBrokerAlert_Upd");
            }
            var alerts = new Dictionary<string, string>();
            using (var cmd = new SqlCommand($"SELECT AlertKey, LastMessage FROM Alert.ActiveAlerts WHERE AlertType = 'ServiceBroker' AND IsResolved = 0 AND InstanceID = {instanceID}", cn, tran))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) alerts.Add(rdr.GetString(0), rdr.GetString(1));
            }
            tran.Rollback();
            return alerts;
        }


        private static DataRow Queue(string name) =>
            Query($"SELECT * FROM dbo.ServiceBrokerQueueInfo WHERE DatabaseID = {databaseID} AND schema_name = N'dbo' AND queue_name = N'{name}'").Rows.Cast<DataRow>().Single();

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

        private static object Scalar(SqlConnection cn, SqlTransaction tran, string sql)
        {
            using var cmd = new SqlCommand(sql, cn, tran);
            return cmd.ExecuteScalar()!;
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }
    }

    [TestClass]
    public class ServiceBrokerConfigTests
    {
        [TestMethod]
        public void MaxDatabasesDefaults()
        {
            Assert.AreEqual(CollectionConfig.DefaultServiceBrokerMaxDatabases, new CollectionConfig().GetServiceBrokerMaxDatabases(), "Unset uses the default");
            Assert.AreEqual(0, new CollectionConfig { ServiceBrokerMaxDatabases = 0 }.GetServiceBrokerMaxDatabases(), "0 is no limit");
            Assert.AreEqual(0, new CollectionConfig { ServiceBrokerMaxDatabases = -5 }.GetServiceBrokerMaxDatabases(), "Negative is treated as no limit");
            Assert.AreEqual(2000, new CollectionConfig { ServiceBrokerMaxDatabases = 2000 }.GetServiceBrokerMaxDatabases());
        }
    }
}
