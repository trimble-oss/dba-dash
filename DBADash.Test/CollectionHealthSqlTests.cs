using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// Collection runs from end to end: a real collection (with the CPU and reads measured) against a source database, imported by
    /// DBImporter into a new repository, then the hourly rollup, current status, virtual counters and the Collection Health report
    /// read back.  The later tests feed dbo.CollectionRuns_Upd batches directly to cover failures, re-imports, out of order files,
    /// the hourly-only mode and an older service's durations.
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  Both databases are dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class CollectionHealthSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Suffix = Guid.NewGuid().ToString("N");
        private static readonly string Repository = "DBADashHealthRepoTest_" + Suffix;
        private static readonly string SourceDB = "DBADashHealthSrcTest_" + Suffix;
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string RepositoryConnectionString => ConnectionStringFor(Repository);
        private static string SourceConnectionString => ConnectionStringFor(SourceDB);

        private static DataSet collected = new();
        /// <summary>The runs as the importer sent them - with the import's outcome and cost, which the collected table doesn't get</summary>
        private static DataTable importedRuns = new();
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
        public static async Task ClassInitialize(TestContext _)
        {
            try
            {
                await using var master = new SqlConnection(MasterConnectionString);
                await master.OpenAsync();
                Execute(master, $"CREATE DATABASE [{SourceDB}]");
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
                return;
            }
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Repository, MessageAuthorizationSqlTests.FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);
            await using (var source = new SqlConnection(SourceConnectionString))
            {
                await source.OpenAsync();
                Execute(source, "CREATE PROC dbo.HealthTestCollection AS SELECT 1 AS Value");
            }

            var collector = await DBCollector.CreateAsync(new DBADashSource(SourceConnectionString) { ConnectionID = "HealthTest" }, "DBADashTest", disableRetry: true, logInternalPerformanceCounters: true);
            await collector.CollectAsync(new[] { CollectionType.Waits, CollectionType.TraceFlags });
            await collector.CollectAsync(new Dictionary<string, CustomCollection>
            {
                // Collected, but the repository has no UserData.HealthTest_Upd - so the import fails
                ["HealthTest"] = new() { ProcedureName = "dbo.HealthTestCollection" },
                // The procedure doesn't exist - the collection fails
                ["Missing"] = new() { ProcedureName = "dbo.DoesNotExist" }
            });
            collected = collector.Data;
            CollectionRunsTable.MarkSent(collected); // As DestinationHandling.WriteDBAsync - the importer is called directly here
            var importer = new DBImporter(collected, RepositoryConnectionString, DBADashAgent.GetCurrent());
            try
            {
                await importer.UpdateAsync();
            }
            catch (AggregateException)
            {
                // The UserData.HealthTest import fails - by design
            }
            importedRuns = importer.ImportedCollectionRuns!;
            await using var repo = new SqlConnection(RepositoryConnectionString);
            await repo.OpenAsync();
            instanceID = (int)Scalar(repo, "SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = N'HealthTest'");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                foreach (var db in new[] { Repository, SourceDB })
                {
                    Execute(master, $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");
                }
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        private static DataRow CollectedRun(string reference) =>
            importedRuns.Rows.Cast<DataRow>().Single(r => (string)r["Reference"] == reference && (string)r["Step"] == "");

        [TestMethod]
        public void EachCollectionRunIsRecorded()
        {
            foreach (var reference in new[] { "Instance", "Waits", "TraceFlags" })
            {
                var run = CollectedRun(reference);
                Assert.AreEqual((byte)CollectionRunStatus.Succeeded, run["Status"], reference);
                Assert.AreEqual((byte)1, run["Attempts"], reference);
                Assert.IsTrue((int)run["DurationMs"] >= 0, reference);
            }
            Assert.AreEqual("True", collected.Tables[CollectionRunsTable.TableName]!.ExtendedProperties[CollectionRunsTable.StoreDetailProperty]);
        }

        /// <summary>
        /// The service sets LogInternalPerformanceCounters after creating the collector - the batch must still be marked to keep
        /// each run, or no run is ever kept and every chart falls back to the hourly rollup
        /// </summary>
        [TestMethod]
        public async Task SettingLogInternalPerformanceCountersAfterCreationKeepsEachRun()
        {
            var collector = await DBCollector.CreateAsync(new DBADashSource(SourceConnectionString) { ConnectionID = "HealthTestLate" }, "DBADashTest", disableRetry: true);
            var runs = collector.Data.Tables[CollectionRunsTable.TableName]!;
            Assert.IsFalse(CollectionRunsTable.GetStoreDetail(runs));
            collector.LogInternalPerformanceCounters = true;
            Assert.IsTrue(CollectionRunsTable.GetStoreDetail(runs));
        }

        /// <summary>
        /// Collectors that open their own connections from a connection string (slow queries, deadlocks, the XE readers) are
        /// measured for the run in progress, and only while one is - the same code run for the GUI measures nothing
        /// </summary>
        [TestMethod]
        public async Task ConnectionsOpenedDuringARunAreMeasured()
        {
            async Task<SessionUsageMeasurement> QueryAsync()
            {
                await using var cn = new SqlConnection(SourceConnectionString);
                await cn.OpenAsync();
                await using var usage = await SessionUsageMeasurement.StartForRunAsync(cn);
                await using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.all_columns", cn);
                await cmd.ExecuteScalarAsync();
                return usage;
            }

            Assert.IsNull(await QueryAsync(), "No run in progress - nothing to measure for");
            var meter = new CollectionRunMeter();
            using (SessionUsageMeasurement.ForRun(meter))
            {
                Assert.IsNotNull(await QueryAsync());
            }
            Assert.IsTrue(meter.LogicalReads > 0, "The run's meter has the connection's reads");
            Assert.IsNull(await QueryAsync(), "Not after the run");
        }

        /// <summary>The collection's own session counters - measured with LogInternalPerformanceCounters on</summary>
        [TestMethod]
        public void CpuAndReadsAreMeasured()
        {
            var waits = CollectedRun("Waits");
            Assert.IsInstanceOfType<int>(waits["CpuMs"]);
            Assert.IsInstanceOfType<long>(waits["LogicalReads"]);
            Assert.IsTrue((long)waits["LogicalReads"] > 0, "Reading sys.dm_os_wait_stats does some reads");
        }

        [TestMethod]
        public void MissingCustomCollectionProcedureFails()
        {
            Assert.AreEqual((byte)CollectionRunStatus.Failed, CollectedRun("UserData.Missing")["Status"]);
        }

        [TestMethod]
        public void ImportFailureIsRecordedAgainstTheRun()
        {
            Assert.AreEqual((byte)CollectionRunStatus.ImportFailed, CollectedRun("UserData.HealthTest")["Status"]);
            using var cn = Open();
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.ImportFailedCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'UserData.HealthTest'")));
        }

        /// <summary>
        /// The batch is imported into every destination from the same DataSet - a failure importing into one repository
        /// mustn't be recorded against the run in the next
        /// </summary>
        [TestMethod]
        public void ImportOutcomeIsNotWrittenToTheSharedBatch()
        {
            var shared = collected.Tables[CollectionRunsTable.TableName]!;
            Assert.IsFalse(shared.Columns.Contains("ImportMs"));
            var run = shared.Rows.Cast<DataRow>().Single(r => (string)r["Reference"] == "UserData.HealthTest" && (string)r["Step"] == "");
            Assert.AreEqual((byte)CollectionRunStatus.Succeeded, run["Status"]);
        }

        [TestMethod]
        public void RunsAreRolledUpAndKept()
        {
            using var cn = Open();
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT SUM(H.SucceededCount) FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'Waits'")));
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT COUNT(*) FROM dbo.CollectionRuns H {0} WHERE R.Reference = 'Waits'")), "Each run is kept with LogInternalPerformanceCounters on");
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.CpuCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'Waits'")));
            Assert.AreEqual((byte)1, Scalar(cn, Sql("SELECT H.LastStatus FROM dbo.CollectionRunStatus H {0} WHERE R.Reference = 'Waits'")));
        }

        [TestMethod]
        public void ImportingTheSameFileAgainDoesNotCountTwice()
        {
            using var cn = Open();
            var before = Scalar(cn, "SELECT SUM(RunCount) FROM dbo.CollectionRuns_60MIN WHERE InstanceID = " + instanceID);
            var runs = importedRuns;
            var snapshotDate = (DateTime)collected.Tables["DBADash"]!.Rows[0]["SnapshotDateUTC"];
            ImportRuns(cn, runs, snapshotDate, storeDetail: true);
            Assert.AreEqual(before, Scalar(cn, "SELECT SUM(RunCount) FROM dbo.CollectionRuns_60MIN WHERE InstanceID = " + instanceID));
        }

        /// <summary>
        /// LogInternalPerformanceCounters turned on part way through an hour keeps only the later runs.  Those show as runs of
        /// their own straight away, and the earlier runs as the rest of the hour's rollup - nothing left out or counted twice.
        /// </summary>
        [TestMethod]
        public void HourWithSomeRunsKeptShowsThemWithTheRest()
        {
            using var cn = Open();
            var now = DateTime.UtcNow;
            var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-5);
            ImportRuns(cn, Runs(("PartialDetail", hour.AddMinutes(5), CollectionRunStatus.Succeeded)), hour.AddMinutes(5), storeDetail: false);
            ImportRuns(cn, Runs(("PartialDetail", hour.AddMinutes(35), CollectionRunStatus.Failed)), hour.AddMinutes(35), storeDetail: true);

            var range = $"'{hour:yyyy-MM-dd HH:mm:ss}', '{hour.AddHours(1):yyyy-MM-dd HH:mm:ss}'";
            var runs = $"FROM dbo.CollectionRunsBetweenDates({range},1) F JOIN dbo.CollectionRunReferences R ON R.ReferenceID = F.ReferenceID WHERE R.Reference = 'PartialDetail' AND F.InstanceID = {instanceID}";
            Assert.AreEqual(2, Scalar(cn, "SELECT SUM(F.RunCount) " + runs));
            Assert.AreEqual(1, Scalar(cn, "SELECT SUM(F.FailedCount) " + runs));
            Assert.AreEqual(1, Scalar(cn, "SELECT COUNT(*) " + runs + " AND F.IsDetail = 1 AND F.FailedCount = 1"), "The run kept, as a run of its own");
            Assert.AreEqual(1, Scalar(cn, "SELECT SUM(F.RunCount) " + runs + " AND F.IsDetail = 0 AND F.SucceededCount = 1"), "The run that wasn't, from the rollup");
            Assert.AreEqual(20L, Scalar(cn, "SELECT SUM(F.DurationTotalMs) " + runs), "Duration of both, once each");

            var counterID = (int)Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Failed' AND instance_name = N'PartialDetail'");
            Assert.AreEqual(0.5m, Scalar(cn, $"SELECT AVG(Value) FROM dbo.PerformanceCountersBetweenDates({range},{instanceID},{counterID},5)"), "One of the two runs failed");
            Assert.AreEqual(2, Scalar(cn, $"SELECT COUNT(*) FROM dbo.PerformanceCountersBetweenDates({range},{instanceID},{counterID},5)"), "The run kept, and the average of the rest");
        }

        /// <summary>Same names as the counters older versions logged, so alert rules and saved metrics still find them</summary>
        [TestMethod]
        public void VirtualCountersAreRegisteredAndRead()
        {
            using var cn = Open();
            var counterID = (int)Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Duration (ms)' AND instance_name = N'Waits' AND CounterType = 5");
            Assert.AreEqual(1, Scalar(cn, $"SELECT COUNT(*) FROM dbo.InstanceCounters WHERE InstanceID = {instanceID} AND CounterID = {counterID}"));
            Assert.AreEqual(1, Scalar(cn, $"SELECT COUNT(*) FROM dbo.PerformanceCountersBetweenDates(DATEADD(d,-1,SYSUTCDATETIME()),DATEADD(d,1,SYSUTCDATETIME()),{instanceID},{counterID},5)"));
            Assert.AreEqual(1, Scalar(cn, $"SELECT SUM(SampleCount) FROM dbo.PerformanceCountersBetweenDates_60MIN(DATEADD(d,-1,SYSUTCDATETIME()),DATEADD(d,1,SYSUTCDATETIME()),{instanceID},{counterID},5)"));
            foreach (var counter in new[] { "Collection CPU (ms)", "Collection Logical Reads", "Collection Failed" })
            {
                Assert.AreEqual(1, Scalar(cn, $"SELECT COUNT(*) FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'{counter}' AND instance_name = N'Waits' AND CounterType = 5"), counter);
            }
        }

        /// <summary>The Metrics tab leaves the collection counters out unless asked - Collection Health shows them</summary>
        [TestMethod]
        public void SummaryCanLeaveOutCollectionMetrics()
        {
            using var cn = Open();
            int CollectionCounters(bool show)
            {
                using var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("FromDate", DateTime.UtcNow.AddDays(-1));
                cmd.Parameters.AddWithValue("ToDate", DateTime.UtcNow.AddDays(1));
                cmd.Parameters.AddWithValue("ShowCollectionMetrics", show);
                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt.Rows.Cast<DataRow>().Count(r => (string)r["object_name"] == "DBADash" && ((string)r["counter_name"]).StartsWith("Collection "));
            }
            Assert.IsTrue(CollectionCounters(true) > 0);
            Assert.AreEqual(0, CollectionCounters(false));
        }

        [TestMethod]
        public void FailuresInARowAndStatus()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-50);
            ImportRuns(cn, Runs(("Failing", start, CollectionRunStatus.Succeeded)), start, false);
            ImportRuns(cn, Runs(("Failing", start.AddMinutes(1), CollectionRunStatus.Failed)), start.AddMinutes(1), false);
            ImportRuns(cn, Runs(("Failing", start.AddMinutes(2), CollectionRunStatus.ImportFailed)), start.AddMinutes(2), false);
            AssertStatus(cn, "Failing", consecutiveFailures: 2, lastSuccess: start);
            Assert.AreEqual(4, HealthStatus(cn, "Failing"), "Failing for 2 minutes - a blip - and too few runs for the failure rate");

            // An older file imported late counts in the rollup but doesn't move the status back
            ImportRuns(cn, Runs(("Failing", start.AddSeconds(30), CollectionRunStatus.Succeeded)), start.AddSeconds(30), false);
            AssertStatus(cn, "Failing", consecutiveFailures: 2, lastSuccess: start);

            ImportRuns(cn, Runs(("Failing", start.AddMinutes(3), CollectionRunStatus.Retried)), start.AddMinutes(3), false);
            AssertStatus(cn, "Failing", consecutiveFailures: 0, lastSuccess: start.AddMinutes(3));
            Assert.AreEqual(2, HealthStatus(cn, "Failing"), "Recovered, but 2 of 5 failed - under the critical 50%, over the warning 10%");
        }

        [TestMethod]
        public void OneFailureInTwentyIsOK()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-40);
            for (var i = 0; i < 20; i++)
            {
                var status = i == 5 ? CollectionRunStatus.Failed : CollectionRunStatus.Succeeded;
                ImportRuns(cn, Runs(("Recovered", start.AddMinutes(i), status)), start.AddMinutes(i), false);
            }
            Assert.AreEqual(4, HealthStatus(cn, "Recovered"), "5% failed - under the warning 10%");
        }

        /// <summary>A collection that runs every minute has to fail for a while before it shows</summary>
        [TestMethod]
        public void FrequentCollectionHasToFailForAWhile()
        {
            using var cn = Open();
            // An hour of successful runs first - the failure rate over the report's 24hrs stays low
            var start = DateTime.UtcNow.AddMinutes(-40);
            for (var i = 60; i >= 0; i--)
            {
                ImportRuns(cn, Runs(("EveryMinute", start.AddMinutes(-i), CollectionRunStatus.Succeeded)), start.AddMinutes(-i), false);
            }
            for (var i = 1; i <= 31; i++)
            {
                ImportRuns(cn, Runs(("EveryMinute", start.AddMinutes(i), CollectionRunStatus.Failed)), start.AddMinutes(i), false);
                var expected = i switch
                {
                    5 => 4,
                    12 => 2,
                    31 => 1,
                    _ => 0
                };
                if (expected == 0) continue;
                var row = HealthRow(cn, "EveryMinute");
                Assert.AreEqual(expected, Convert.ToInt32(row["Status"]), $"Failing for {i} minutes");
                if (expected == 1)
                {
                    StringAssert.StartsWith((string)row["StatusReason"], "Failed 31 times in a row, 31 min since it last succeeded");
                }
            }
        }

        /// <summary>One that runs daily shows on its first failure, and is critical on its third</summary>
        [TestMethod]
        public void InfrequentCollectionShowsOnItsFirstFailure()
        {
            using var cn = Open();
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("Daily", now.AddHours(-26), CollectionRunStatus.Succeeded)), now.AddHours(-26), false);
            ImportRuns(cn, Runs(("Daily", now.AddHours(-2), CollectionRunStatus.Failed)), now.AddHours(-2), false);
            var row = HealthRow(cn, "Daily");
            Assert.AreEqual(2, Convert.ToInt32(row["Status"]));
            Assert.AreEqual("Failed once, 24 hours since it last succeeded", row["StatusReason"]);
            ImportRuns(cn, Runs(("Daily", now.AddHours(-1), CollectionRunStatus.Failed)), now.AddHours(-1), false);
            ImportRuns(cn, Runs(("Daily", now.AddMinutes(-30), CollectionRunStatus.Failed)), now.AddMinutes(-30), false);
            Assert.AreEqual(1, HealthStatus(cn, "Daily"));
        }

        /// <summary>
        /// A weekly collection that failed at the weekend is listed when the report is checked days later over the last hour - the
        /// summary still counts it.  Last Failure links to the errors since it last succeeded.
        /// </summary>
        [TestMethod]
        public void FailingCollectionIsListedWithNoRunsInThePeriod()
        {
            using var cn = Open();
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("Weekly", now.AddDays(-8), CollectionRunStatus.Succeeded)), now.AddDays(-8), false);
            // In a batch that started 20 minutes before the run - its errors are logged with the batch's date
            ImportRuns(cn, Runs(("Weekly", now.AddDays(-3), CollectionRunStatus.Failed)), now.AddDays(-3).AddMinutes(-20), false);

            var row = Report(cn, "dbo.CollectionHealth_Get", "Weekly", now.AddHours(-1), now).Tables[0].Rows.Cast<DataRow>().Single();
            Assert.AreEqual(0, row["RunCount"], "No runs in the last hour");
            Assert.AreEqual(2, Convert.ToInt32(row["Status"]));
            Assert.AreEqual("Failed once, 5 days since it last succeeded", row["StatusReason"]);
            Assert.AreEqual(DBNull.Value, row["FailedPct"], "No runs to rate");
            Assert.AreEqual(0m, row["FailedPct24Hrs"], "The failure was before the summary's period");
            Assert.AreEqual(row["LastSuccessDate"], row["ErrorsFrom"], "Failing now - every failure since it last succeeded");
            Assert.AreEqual(((DateTime)row["LastFailureDate"]).AddHours(1), row["ErrorsTo"]);

            var grouped = Report(cn, groupByInstance: false, "Weekly").Tables[0].Rows.Cast<DataRow>().Single();
            Assert.AreEqual(2, Convert.ToInt32(grouped["Status"]), "Over all the instances too");

            ImportRuns(cn, Runs(("Weekly", now.AddMinutes(-5), CollectionRunStatus.Succeeded)), now.AddMinutes(-5), false);
            row = Report(cn, "dbo.CollectionHealth_Get", "Weekly", now.AddHours(-1), now).Tables[0].Rows.Cast<DataRow>().Single();
            Assert.AreEqual(4, Convert.ToInt32(row["Status"]), "Succeeded since");
            Assert.AreEqual(((DateTime)row["LastFailureDate"]).AddMinutes(-20), row["ErrorsFrom"], "Not failing now - from the start of the batch the last failure came in");
            Assert.AreEqual(0, Report(cn, "dbo.CollectionHealth_Get", "Weekly", now.AddDays(-2), now.AddDays(-1)).Tables[0].Rows.Count,
                "No longer failing, so not listed for a period it didn't run in");
        }

        /// <summary>
        /// The summary rates the last 24hrs whatever the report's period.  Failures a few hours ago show in Failed % 24hrs when the
        /// report is over the last hour.
        /// </summary>
        [TestMethod]
        public void FailedPctOverTheSummarysPeriod()
        {
            using var cn = Open();
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("Overnight", now.AddHours(-3), CollectionRunStatus.Failed)), now.AddHours(-3), false);
            ImportRuns(cn, Runs(("Overnight", now.AddHours(-3).AddMinutes(1), CollectionRunStatus.Failed)), now.AddHours(-3).AddMinutes(1), false);
            ImportRuns(cn, Runs(("Overnight", now.AddMinutes(-30), CollectionRunStatus.Succeeded)), now.AddMinutes(-30), false);

            var row = Report(cn, "dbo.CollectionHealth_Get", "Overnight", now.AddHours(-1), now).Tables[0].Rows.Cast<DataRow>().Single();
            Assert.AreEqual(0m, row["FailedPct"], "Nothing failed in the last hour");
            Assert.AreEqual(66.67m, row["FailedPct24Hrs"], "2 of the 3 runs since");
        }

        /// <summary>
        /// WMI errors from 2 or more collections on an instance suggest WMI isn't working there - a card at the top of the report,
        /// naming the instances where the report is for more than one.
        /// </summary>
        [TestMethod]
        public void WMIFailingForSeveralCollectionsSuggestsNoWMI()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'WMIHealth',N'WMIHealth',3,1)");
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("WMIDrives", now.AddMinutes(-20), CollectionRunStatus.Partial), ("WMIPerfmon", now.AddMinutes(-20), CollectionRunStatus.Failed), ("WMIWaits", now.AddMinutes(-20), CollectionRunStatus.Failed)), now.AddMinutes(-20), false, instance);
            Execute(cn, $@"INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
VALUES	(DATEADD(mi,-19,SYSUTCDATETIME()),{instance},'WMIDrives',N'Error collecting drives via WMI','Collect:WMI'),
		(DATEADD(mi,-19,SYSUTCDATETIME()),{instance},'WMIPerfmon',N'The WinRM client cannot process the request','Collect:WMI Win32_PerfFormattedData'),
		(DATEADD(mi,-19,SYSUTCDATETIME()),{instance},'WMIWaits',N'Timeout expired','Collect')");

            var report = Report(cn, groupByInstance: true, instance: instance);
            CollectionAssert.DoesNotContain(Columns(report), "Suggestion", "A card rather than a column on every row it's about");
            var card = Insights(report).Single();
            Assert.AreEqual("Warning: WMI failed for 2 collections (2 errors).  [Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for this instance on the Source tab of the service config tool.", card,
                "One instance - no need to name it");
            CollectionAssert.AreEqual(new[] { card }, Insights(Report(cn, groupByInstance: false, "WMIDrives", instance)), "Whatever the grouping");

            // Over two instances, the card names the one with WMI failing - then both
            var second = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'WMIHealth2',N'WMIHealth2',3,1)");
            ImportRuns(cn, Runs(("WMIDrives", now.AddMinutes(-20), CollectionRunStatus.Succeeded)), now.AddMinutes(-20), false, second);
            Assert.AreEqual(0, Insights(Report(cn, groupByInstance: true, instance: second)).Count, "No WMI errors, no card");
            StringAssert.StartsWith(Insights(Report(cn, groupByInstance: true, instances: new[] { instance, second })).Single(), "Warning: WMI failed for 2 collections (2 errors) on WMIHealth.  ");
            Execute(cn, $@"INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
VALUES	(DATEADD(mi,-19,SYSUTCDATETIME()),{second},'WMIDrives',N'Error collecting drives via WMI','Collect:WMI'),
		(DATEADD(mi,-19,SYSUTCDATETIME()),{second},'WMIPerfmon',N'WMI failed','Collect:WMI')");
            Assert.AreEqual("Warning: WMI failed for several collections on 2 instances: WMIHealth, WMIHealth2.  [Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for these instances on the Source tab of the service config tool.",
                Insights(Report(cn, groupByInstance: false, instances: new[] { instance, second })).Single());
        }

        /// <summary>A collection that has never worked has failed for as long as we know - only the runs count</summary>
        [TestMethod]
        public void NeverSucceededCountsTheRunsOnly()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-10);
            ImportRuns(cn, Runs(("NeverWorked", start, CollectionRunStatus.Failed)), start, false);
            Assert.AreEqual(2, HealthStatus(cn, "NeverWorked"));
            ImportRuns(cn, Runs(("NeverWorked", start.AddMinutes(1), CollectionRunStatus.Failed)), start.AddMinutes(1), false);
            ImportRuns(cn, Runs(("NeverWorked", start.AddMinutes(2), CollectionRunStatus.Failed)), start.AddMinutes(2), false);
            var row = HealthRow(cn, "NeverWorked");
            Assert.AreEqual(1, Convert.ToInt32(row["Status"]));
            Assert.AreEqual("Failed 3 times in a row and has never succeeded", row["StatusReason"]);
        }

        /// <summary>A high failure rate is critical while the collection is failing - once it has recovered, a warning</summary>
        [TestMethod]
        public void FailureRateIsCriticalOnlyWhileFailing()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-30);
            var failed = new[] { true, false, true, false, true, false, true, false, true, true };
            for (var i = 0; i < failed.Length; i++)
            {
                ImportRuns(cn, Runs(("Flapping", start.AddMinutes(i), failed[i] ? CollectionRunStatus.Failed : CollectionRunStatus.Succeeded)), start.AddMinutes(i), false);
            }
            var row = HealthRow(cn, "Flapping");
            Assert.AreEqual(1, Convert.ToInt32(row["Status"]), "6 of 10 failed, the latest too - never failing long enough in a row to count");
            Assert.AreEqual("60.0% of 10 runs failed", row["StatusReason"]);
            ImportRuns(cn, Runs(("Flapping", start.AddMinutes(10), CollectionRunStatus.Succeeded)), start.AddMinutes(10), false);
            Assert.AreEqual(2, HealthStatus(cn, "Flapping"), "Still over 50%, but the latest run succeeded");
        }

        [TestMethod]
        public void ThresholdsAreConfiguredAndInherited()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-10);
            ImportRuns(cn, Runs(("Configured", start, CollectionRunStatus.Succeeded)), start, false);
            ImportRuns(cn, Runs(("Configured", start.AddMinutes(1), CollectionRunStatus.Failed)), start.AddMinutes(1), false);
            Assert.AreEqual(4, HealthStatus(cn, "Configured"), "A blip with the defaults");

            SetThresholds(cn, -1, "Configured", ("CriticalConsecutiveFailures", 1), ("CriticalFailingMinutes", 0));
            var row = HealthRow(cn, "Configured");
            Assert.AreEqual(1, Convert.ToInt32(row["Status"]), "Critical on the first failure for this collection on every instance");
            Assert.AreEqual("Collection", row["ThresholdLevel"]);

            var thresholds = GetThresholds(cn, instanceID, "Configured");
            Assert.AreEqual(DBNull.Value, thresholds["CriticalConsecutiveFailures"], "Nothing set at this level");
            Assert.AreEqual(1, thresholds["InheritedCriticalConsecutiveFailures"], "From the collection's row");
            Assert.AreEqual(3, GetThresholds(cn, -1, "Configured")["InheritedCriticalConsecutiveFailures"], "Every instance inherits the default");
            Assert.AreEqual(10m, thresholds["InheritedWarningFailedPct"]);

            SetThresholds(cn, instanceID, "Configured", ("Disabled", true));
            row = HealthRow(cn, "Configured");
            Assert.AreEqual(3, Convert.ToInt32(row["Status"]), "Not monitored on this instance");
            Assert.AreEqual("Instance", row["ThresholdLevel"]);
            Assert.AreEqual(true, GetThresholds(cn, instanceID, "Configured")["Disabled"]);

            SetThresholds(cn, instanceID, "Configured");
            SetThresholds(cn, -1, "Configured");
            Assert.AreEqual(0, Scalar(cn, "SELECT COUNT(*) FROM dbo.CollectionHealthThresholds WHERE Reference = 'Configured'"), "Nothing set removes the row");
            Assert.AreEqual(4, HealthStatus(cn, "Configured"));
        }

        /// <summary>
        /// The summary's DBA Dash Errors status is collection health - for an instance of its own, so the other tests' failures
        /// don't count.  Acknowledging the errors clears failures in a row until the next failure.
        /// </summary>
        [TestMethod]
        public void SummaryStatusIsCollectionHealth()
        {
            using var cn = Open();
            var summaryInstance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'SummaryHealth',N'SummaryHealth',3,1)");
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("SummaryDaily", now.AddHours(-26), CollectionRunStatus.Succeeded)), now.AddHours(-26), false, summaryInstance);
            ImportRuns(cn, Runs(("SummaryDaily", now.AddHours(-2), CollectionRunStatus.Failed)), now.AddHours(-2), false, summaryInstance);
            ImportRuns(cn, Runs(("SummaryOK", now.AddHours(-2), CollectionRunStatus.Succeeded)), now.AddHours(-2).AddSeconds(1), false, summaryInstance);
            Assert.AreEqual(2, SummaryStatus(cn, summaryInstance), "The daily collection's first failure");
            Assert.AreEqual(1, Scalar(cn, $"SELECT CollectionErrorCount FROM dbo.Summary WHERE InstanceID = {summaryInstance}"), "The number shown is the collections at warning or critical");

            try
            {
                Execute(cn, "EXEC dbo.AcknowledgeErrors");
                Assert.AreEqual(4, SummaryStatus(cn, summaryInstance), "Acknowledged");
                var again = DateTime.UtcNow.AddSeconds(1);
                ImportRuns(cn, Runs(("SummaryDaily", again, CollectionRunStatus.Failed)), again, false, summaryInstance);
                Assert.AreEqual(2, SummaryStatus(cn, summaryInstance), "Failed again after it was acknowledged");
            }
            finally
            {
                Execute(cn, "DELETE dbo.Settings WHERE SettingName = 'ErrorAckDate'");
            }

            SetThresholds(cn, summaryInstance, "SummaryDaily", ("Disabled", true));
            Assert.AreEqual(4, SummaryStatus(cn, summaryInstance), "Not monitored - and the other collection is OK");
        }

        /// <summary>
        /// As the summary, a collection whose schedule is disabled and every collection on a paused instance are N/A in the
        /// report - with the runs still shown.  An instance of its own, so pausing it doesn't touch the other tests.
        /// </summary>
        [TestMethod]
        public void DisabledOrPausedCollectionsAreNA()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'MutedHealth',N'MutedHealth',3,1)");
            var now = DateTime.UtcNow;
            // One batch per time - a second file at the same time for the instance is taken as the same file again
            ImportRuns(cn, Runs(("MutedDisabled", now.AddHours(-26), CollectionRunStatus.Succeeded), ("MutedPaused", now.AddHours(-26), CollectionRunStatus.Succeeded)), now.AddHours(-26), false, instance);
            ImportRuns(cn, Runs(("MutedDisabled", now.AddHours(-2), CollectionRunStatus.Failed), ("MutedPaused", now.AddHours(-2), CollectionRunStatus.Failed)), now.AddHours(-2), false, instance);
            foreach (var reference in new[] { "MutedDisabled", "MutedPaused" })
            {
                Assert.AreEqual(2, HealthStatus(cn, reference, instance), reference);
            }
            Assert.AreEqual(2, SummaryStatus(cn, instance));

            Execute(cn, $"INSERT INTO dbo.ScheduleInfo(InstanceID,Reference,Schedule,RunOnServiceStart,IsInstanceOverride,SnapshotDate) VALUES({instance},'MutedDisabled','',0,1,SYSUTCDATETIME())");
            var row = HealthRow(cn, "MutedDisabled", instance);
            Assert.AreEqual(3, Convert.ToInt32(row["Status"]));
            Assert.AreEqual("Schedule disabled", row["StatusReason"]);
            Assert.AreEqual(1, Convert.ToInt32(row["FailedCount"]), "The runs are still shown");
            Assert.AreEqual(2, HealthStatus(cn, "MutedPaused", instance), "Only that collection");

            var pausedUntil = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc).AddHours(1);
            Execute(cn, $"UPDATE dbo.Instances SET CollectionsPausedUntil = '{pausedUntil:yyyy-MM-ddTHH:mm:ss}' WHERE InstanceID = {instance}");
            row = HealthRow(cn, "MutedPaused", instance);
            Assert.AreEqual(3, Convert.ToInt32(row["Status"]));
            Assert.AreEqual($"Collections paused until {pausedUntil:yyyy-MM-dd HH:mm} UTC", row["StatusReason"]);
            Assert.AreEqual(3, SummaryStatus(cn, instance), "The summary agrees - N/A, as every collection on the instance is paused");
        }

        /// <summary>
        /// Two batches for an instance with the same SnapshotDate - two schedules starting within the same 10ms - are both
        /// imported.  The same batch again (the same file) is only imported once.
        /// </summary>
        [TestMethod]
        public void BatchesWithTheSameSnapshotDateAreBothImported()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'SameTime',N'SameTime',3,1)");
            var snapshotDate = DateTime.UtcNow.AddMinutes(-5);
            var first = Guid.NewGuid();
            void Import(string reference, Guid batchId)
            {
                using var cmd = new SqlCommand("dbo.CollectionRuns_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", instance);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("CollectionRuns", Runs((reference, snapshotDate, CollectionRunStatus.Failed)));
                cmd.Parameters.AddWithValue("BatchID", batchId);
                cmd.ExecuteNonQuery();
            }
            Import("EveryMinute", first);
            Import("EveryFiveMinutes", Guid.NewGuid());
            Import("EveryMinute", first);
            int Runs60(string reference) => Convert.ToInt32(Scalar(cn, $"SELECT H.RunCount FROM dbo.CollectionRuns_60MIN H JOIN dbo.CollectionRunReferences R ON R.ReferenceID = H.ReferenceID WHERE H.InstanceID = {instance} AND R.Reference = '{reference}'"));
            Assert.AreEqual(1, Runs60("EveryFiveMinutes"), "A different batch at the same time");
            Assert.AreEqual(1, Runs60("EveryMinute"), "The same batch again isn't counted twice");
        }

        /// <summary>
        /// A 0% failure rate threshold means any failure - a collection without one is OK, as the summary has it (it only reads
        /// the rate for collections that failed or partly failed)
        /// </summary>
        [TestMethod]
        public void ZeroPercentRateThresholdNeedsAFailure()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-20);
            for (var i = 0; i < 6; i++)
            {
                ImportRuns(cn, Runs(("ZeroPct", start.AddMinutes(i), CollectionRunStatus.Succeeded)), start.AddMinutes(i).AddMilliseconds(500), false);
            }
            SetThresholds(cn, -1, "ZeroPct", ("WarningFailedPct", 0m), ("CriticalFailedPct", 0m));
            try
            {
                Assert.AreEqual(4, HealthStatus(cn, "ZeroPct"), "No failures to count");
                ImportRuns(cn, Runs(("ZeroPct", start.AddMinutes(7), CollectionRunStatus.Partial)), start.AddMinutes(7).AddMilliseconds(500), false);
                Assert.AreEqual(2, HealthStatus(cn, "ZeroPct"), "Any partial failure");
            }
            finally
            {
                SetThresholds(cn, -1, "ZeroPct");
            }
        }

        /// <summary>Over all the instances, the thresholds column says where some instances have thresholds of their own</summary>
        [TestMethod]
        public void GroupedThresholdLevelShowsInstanceOverrides()
        {
            using var cn = Open();
            var other = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'Grouped',N'Grouped',3,1)");
            var start = DateTime.UtcNow.AddMinutes(-10);
            ImportRuns(cn, Runs(("GroupedLevel", start, CollectionRunStatus.Succeeded)), start.AddMilliseconds(700), false);
            ImportRuns(cn, Runs(("GroupedLevel", start, CollectionRunStatus.Succeeded)), start, false, other);
            DataRow Grouped()
            {
                using var cmd = new SqlCommand("dbo.CollectionHealth_Get", cn) { CommandType = CommandType.StoredProcedure };
                var ids = new DataTable();
                ids.Columns.Add("ID", typeof(int));
                ids.Rows.Add(instanceID);
                ids.Rows.Add(other);
                cmd.Parameters.AddWithValue("InstanceIDs", ids).SqlDbType = SqlDbType.Structured;
                cmd.Parameters.AddWithValue("GroupByInstance", false);
                cmd.Parameters.AddWithValue("Reference", "GroupedLevel");
                var ds = new DataSet();
                new SqlDataAdapter(cmd).Fill(ds);
                return ds.Tables[0].Rows.Cast<DataRow>().Single();
            }
            Assert.AreEqual("Default", Grouped()["ThresholdLevel"]);
            SetThresholds(cn, other, "GroupedLevel", ("WarningFailingMinutes", 60));
            Assert.AreEqual("Varies by instance", Grouped()["ThresholdLevel"]);
        }

        /// <summary>Without each run kept, the counter falls back to the hour's average</summary>
        [TestMethod]
        public void HourlyOnlyCountersUseTheHourlyAverage()
        {
            using var cn = Open();
            var start = DateTime.UtcNow.AddMinutes(-5);
            var runs = Runs(("HourlyOnly", start, CollectionRunStatus.Succeeded), ("HourlyOnly", start.AddSeconds(1), CollectionRunStatus.Failed));
            runs.Rows[0]["DurationMs"] = 100;
            runs.Rows[1]["DurationMs"] = 300;
            ImportRuns(cn, runs, start, storeDetail: false);
            Assert.AreEqual(0, Scalar(cn, Sql("SELECT COUNT(*) FROM dbo.CollectionRuns H {0} WHERE R.Reference = 'HourlyOnly'")));
            var duration = (int)Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Duration (ms)' AND instance_name = N'HourlyOnly'");
            var failed = (int)Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Failed' AND instance_name = N'HourlyOnly'");
            Assert.AreEqual(200m, Scalar(cn, $"SELECT AVG(Value) FROM dbo.PerformanceCountersBetweenDates(DATEADD(mi,-30,SYSUTCDATETIME()),DATEADD(mi,30,SYSUTCDATETIME()),{instanceID},{duration},5)"));
            Assert.AreEqual(0.5m, Scalar(cn, $"SELECT AVG(Value) FROM dbo.PerformanceCountersBetweenDates(DATEADD(mi,-30,SYSUTCDATETIME()),DATEADD(mi,30,SYSUTCDATETIME()),{instanceID},{failed},5)"), "The failure rate");
        }

        /// <summary>
        /// An older service logs durations as internal performance counters - imported as runs without a status, keeping the counter
        /// it had.  Its other diagnostic counters are still ordinary performance counters.
        /// </summary>
        [TestMethod]
        public void OlderServiceDurationsKeepTheirCounter()
        {
            using var cn = Open();
            Execute(cn, "INSERT INTO dbo.Counters(object_name,counter_name,instance_name,CounterType) VALUES(N'DBADash',N'Collection Duration (ms)',N'QueryStats Plans',1)");
            var counterID = (int)Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Duration (ms)' AND instance_name = N'QueryStats Plans'");
            // Logged as an ordinary counter before the upgrade
            Execute(cn, $"INSERT INTO dbo.PerformanceCounters(InstanceID,CounterID,SnapshotDate,Value) VALUES({instanceID},{counterID},DATEADD(mi,-20,SYSUTCDATETIME()),900)");
            var snapshotDate = DateTime.UtcNow.AddMinutes(-3);
            using (var cmd = new SqlCommand("dbo.InternalPerformanceCounters_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                var dt = new DataTable();
                dt.Columns.Add("SnapshotDate", typeof(DateTime));
                dt.Columns.Add("object_name");
                dt.Columns.Add("counter_name");
                dt.Columns.Add("instance_name");
                dt.Columns.Add("cntr_value", typeof(decimal));
                dt.Columns.Add("cntr_type", typeof(int));
                dt.Rows.Add(snapshotDate, "DBADash", "Collection Duration (ms)", "QueryStats Plans", 1234.5m, 65792);
                dt.Rows.Add(snapshotDate, "DBADash", "Count of plans collected", "", 7m, 65792);
                dt.Rows.Add(snapshotDate, "DBADash", "Collection Duration (ms)", "UserData.Disk Check", 50m, 65792);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("InternalPerformanceCounters", dt);
                cmd.ExecuteNonQuery();
            }
            Assert.AreEqual(counterID, Scalar(cn, "SELECT CounterID FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Duration (ms)' AND instance_name = N'QueryStats Plans' AND CounterType = 5"));
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.RunCount - H.SucceededCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'QueryStats' AND R.Step = 'Plans'")), "A run without a status");
            Assert.AreEqual(1235m, Scalar(cn, $"SELECT Value FROM dbo.PerformanceCountersBetweenDates(DATEADD(mi,-30,SYSUTCDATETIME()),DATEADD(mi,30,SYSUTCDATETIME()),{instanceID},{counterID},5) WHERE SnapshotDate > DATEADD(mi,-10,SYSUTCDATETIME())"));
            Assert.AreEqual(900m, Scalar(cn, $"SELECT Value FROM dbo.PerformanceCountersBetweenDates(DATEADD(mi,-30,SYSUTCDATETIME()),DATEADD(mi,30,SYSUTCDATETIME()),{instanceID},{counterID},5) WHERE SnapshotDate < DATEADD(mi,-10,SYSUTCDATETIME())"), "The history from before the upgrade is still read");
            Assert.AreEqual(7m, Scalar(cn, "SELECT PC.Value FROM dbo.PerformanceCounters PC JOIN dbo.Counters C ON C.CounterID = PC.CounterID WHERE C.counter_name = N'Count of plans collected' AND C.CounterType = 1"));
            Assert.AreEqual("", Scalar(cn, Sql("SELECT R.Step FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'UserData.Disk Check'")), "A custom collection's name with a space isn't split into a step");
        }

        /// <summary>
        /// The report's Errors count leaves out retries, and its link lists the errors over the report's range with retries
        /// left out - the same rows.  Showing retries adds them.  An instance of its own, so the count is only these errors.
        /// </summary>
        [TestMethod]
        public void ErrorCountMatchesTheErrorList()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'ErrorLink',N'ErrorLink',3,1)");
            var now = DateTime.UtcNow;
            ImportRuns(cn, Runs(("ErrLink", now.AddHours(-1), CollectionRunStatus.Retried)), now.AddHours(-1), false, instance);
            Execute(cn, $@"INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
VALUES	(DATEADD(mi,-50,SYSUTCDATETIME()),{instance},'ErrLink',N'Failed','Collect'),
		(DATEADD(mi,-55,SYSUTCDATETIME()),{instance},'ErrLink',N'No context',NULL),
		(DATEADD(mi,-59,SYSUTCDATETIME()),{instance},'ErrLink',N'Retried','Collect[Retry]'),
		(DATEADD(mi,-58,SYSUTCDATETIME()),{instance},'ErrLink',N'Import retried','Import[Retry]'),
		(DATEADD(d,-2,SYSUTCDATETIME()),{instance},'ErrLink',N'Before the range','Collect')");

            Assert.AreEqual(2, Convert.ToInt32(HealthRow(cn, "ErrLink", instance)["ErrorCount"]), "Retries left out, the error without a context counted");
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow.AddMinutes(1);
            Assert.AreEqual(2, ErrorLog(cn, instance, from, to, includeRetries: false).Rows.Count, "The link lists what the count is of");
            Assert.AreEqual(4, ErrorLog(cn, instance, from, to, includeRetries: true).Rows.Count, "Retries shown");
            Assert.AreEqual(0, ErrorLog(cn, instance, from, DateTime.UtcNow.AddMinutes(-56), includeRetries: false).Rows.Count, "Up to the end of the range only");
            Assert.AreEqual(5, ErrorLog(cn, instance, null, null, includeRetries: true).Rows.Count, "Without a range, the last @Days - as before");
        }

        /// <summary>
        /// The ErrorLog summary splits the context into type (error/warning/retry) and stage (collect/WMI/import/other)
        /// columns, each set adding up to the total, and a count lists the same rows as the list filtered to it.  Errors
        /// that don't belong to an instance are "(No instance)", which the instance filter finds.
        /// </summary>
        [TestMethod]
        public void ErrorLogSummarySplitsTheContext()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'ErrSummary',N'ErrSummary',3,1)");
            Execute(cn, $@"INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
VALUES	(DATEADD(mi,-1,SYSUTCDATETIME()),{instance},'ServerExtraProperties',N'Latest',NULL),
		(DATEADD(mi,-10,SYSUTCDATETIME()),{instance},'ServerExtraProperties',N'Failed','Collect'),
		(DATEADD(mi,-11,SYSUTCDATETIME()),{instance},'ServerExtraProperties',N'Retried','Collect[Retry]'),
		(DATEADD(mi,-12,SYSUTCDATETIME()),{instance},'ServerExtraProperties',N'WMI failed','Collect:Win32_OperatingSystem WMI'),
		(DATEADD(mi,-13,SYSUTCDATETIME()),{instance},'Drives',N'Error collecting drives via WMI','Collect:WMI'),
		(DATEADD(mi,-14,SYSUTCDATETIME()),{instance},'SlowQueries',N'Not supported','Collect[Warning]'),
		(DATEADD(mi,-15,SYSUTCDATETIME()),{instance},'TableSize',N'Warning: some databases were excluded','Collect'),
		(DATEADD(mi,-16,SYSUTCDATETIME()),{instance},'Waits',N'Import failed','Import'),
		(DATEADD(mi,-17,SYSUTCDATETIME()),{instance},'Waits',N'Import retried','Import[Retry]'),
		(DATEADD(mi,-18,SYSUTCDATETIME()),{instance},'SchemaSnapshot',N'Snapshot failed','MyDatabase'),
		(DATEADD(mi,-19,SYSUTCDATETIME()),NULL,'Alert',N'Error processing alerts','AlertProcessing'),
		(DATEADD(d,-3,SYSUTCDATETIME()),{instance},'Waits',N'Before the range','Import')");

            var list = ErrorList(cn, "dbo.CollectionErrorLog_Get", instance).Rows.Cast<DataRow>().ToDictionary(r => (string)r["ErrorMessage"], r => $"{r["ErrorType"]}/{r["ErrorStage"]}");
            Assert.AreEqual("Error/Other", list["Latest"], "No context - an older service");
            Assert.AreEqual("Error/Collect", list["Failed"]);
            Assert.AreEqual("Retry/Collect", list["Retried"]);
            Assert.AreEqual("Error/WMI", list["WMI failed"]);
            Assert.AreEqual("Error/WMI", list["Error collecting drives via WMI"]);
            Assert.AreEqual("Warning/Collect", list["Not supported"]);
            Assert.AreEqual("Warning/Collect", list["Warning: some databases were excluded"], "A message that starts with Warning");
            Assert.AreEqual("Error/Import", list["Import failed"]);
            Assert.AreEqual("Retry/Import", list["Import retried"]);
            Assert.AreEqual("Error/Collect", list["Snapshot failed"], "A schema snapshot's context is the database");
            Assert.IsFalse(list.ContainsKey("Error processing alerts"), "Not this instance's");
            Assert.IsFalse(list.ContainsKey("Before the range"));

            var bySource = ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("GroupBy", "Source")).Rows.Cast<DataRow>().ToDictionary(r => (string)r["ErrorSource"]);
            Assert.AreEqual(6, bySource.Count, "A row per source");
            var sep = bySource["ServerExtraProperties"];
            Assert.AreEqual(4, sep["ErrorCount"]);
            Assert.AreEqual(3, sep["Errors"]);
            Assert.AreEqual(DBNull.Value, sep["Warnings"], "NULL rather than 0");
            Assert.AreEqual(1, sep["Retries"]);
            Assert.AreEqual(2, sep["Collect"]);
            Assert.AreEqual(1, sep["WMI"]);
            Assert.AreEqual(1, sep["Other"]);
            Assert.AreEqual("Latest", sep["LastMessage"]);
            Assert.AreEqual(1, sep["InstanceCount"]);
            foreach (var row in bySource.Values)
            {
                int Sum(params string[] cols) => cols.Sum(c => row[c] is int n ? n : 0);
                Assert.AreEqual(row["ErrorCount"], Sum("Errors", "Warnings", "Retries"), $"{row["ErrorSource"]}: the types add up to the total");
                Assert.AreEqual(row["ErrorCount"], Sum("Collect", "WMI", "Import", "Other"), $"{row["ErrorSource"]}: the stages add up to the total");
            }

            var byInstance = ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("GroupBy", "Instance")).Rows.Cast<DataRow>().Single();
            Assert.AreEqual(10, byInstance["ErrorCount"]);
            Assert.AreEqual(2, byInstance["Warnings"]);
            Assert.AreEqual(6, byInstance["SourceCount"]);
            Assert.AreEqual("Source", byInstance["DrillGroupBy"], "The instance link goes to its sources");
            CollectionAssert.DoesNotContain(ColumnNames(byInstance.Table), "InstanceCount", "Always 1 on a row per instance");
            CollectionAssert.DoesNotContain(ColumnNames(bySource["Waits"].Table), "SourceCount");
            StringAssert.StartsWith((string)byInstance["Suggestion"], "WMI failed for 2 collections (2 errors).", "WMI errors from ServerExtraProperties and Drives");
            Assert.AreEqual("https://dbadash.com/docs/help/wmi/", byInstance["HelpUrl"]);
            Assert.AreEqual(DBNull.Value, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("GroupBy", "Instance"), ("ErrorSource", "Drives")).Rows[0]["Suggestion"],
                "One collection's WMI errors could be that one query");
            CollectionAssert.DoesNotContain(ColumnNames(bySource["Drives"].Table), "Suggestion", "The card says it - a column would repeat it on each WMI source");
            const string oneInstanceCard = "Warning: WMI failed for 2 collections (2 errors).  [Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for this instance on the Source tab of the service config tool.";
            foreach (var groupBy in new[] { "Instance", "Source", "None" })
            {
                CollectionAssert.AreEqual(new[] { oneInstanceCard }, ErrorLogInsights(cn, instance, groupBy), $"A card grouped by {groupBy}");
            }
            Assert.AreEqual(0, ErrorLogInsights(cn, instance, "Source", ("ErrorSource", "Drives")).Count, "No card for one collection's WMI errors");

            // A second instance with WMI failing - over both, the card names them and a row per instance says which is which
            var second = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'ErrSummary2',N'ErrSummary2',3,1)");
            Execute(cn, $@"INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
VALUES	(DATEADD(mi,-5,SYSUTCDATETIME()),{second},'Drives',N'Error collecting drives via WMI','Collect:WMI'),
		(DATEADD(mi,-5,SYSUTCDATETIME()),{second},'PerfmonCounters',N'WMI failed','Collect:WMI')");
            using var both = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure };
            both.Parameters.AddWithValue("InstanceIDs", Ids(new[] { instance, second })).SqlDbType = SqlDbType.Structured;
            both.Parameters.AddWithValue("Days", 1);
            both.Parameters.AddWithValue("GroupBy", "Instance");
            var overBoth = new DataSet();
            new SqlDataAdapter(both).Fill(overBoth);
            Assert.AreEqual("Warning: WMI failed for several collections on 2 instances: ErrSummary, ErrSummary2.  [Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for these instances on the Source tab of the service config tool.",
                Insights(overBoth).Single());
            var instances = overBoth.Tables[0].Rows.Cast<DataRow>().ToDictionary(r => (string)r["InstanceDisplayName"]);
            StringAssert.StartsWith((string)instances["ErrSummary2"]["Suggestion"], "WMI failed for 2 collections (2 errors).");
            StringAssert.StartsWith((string)instances["ErrSummary"]["Suggestion"], "WMI failed for 2 collections (2 errors).");

            Assert.AreEqual(10, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("GroupBy", "None"), ("ErrorSource", ""), ("ErrorContext", ""), ("ErrorMessage", ""), ("InstanceDisplayName", ""), ("ErrorType", ""), ("ErrorStage", "")).Rows.Count,
                "A cleared text box is no filter");

            Assert.AreEqual(2, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("ErrorType", "Warning")).Rows.Count, "The Warnings count lists the warnings");
            Assert.AreEqual(2, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("ErrorStage", "WMI")).Rows.Count, "The WMI count lists the WMI errors");
            Assert.AreEqual(1, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("ErrorSource", "ServerExtraProperties"), ("ErrorType", "Retry")).Rows.Count);
            Assert.AreEqual(DBNull.Value, ErrorList(cn, "dbo.CollectionErrorLog_Get", instance, ("GroupBy", "Instance"), ("IncludeRetries", false)).Rows[0]["Retries"], "Retries hidden");

            var noInstance = ErrorList(cn, "dbo.CollectionErrorLog_Get", null, ("GroupBy", "Instance"), ("InstanceDisplayName", "(No instance)")).Rows.Cast<DataRow>().Single();
            Assert.AreEqual("(No instance)", noInstance["InstanceDisplayName"]);
            Assert.IsTrue((int)noInstance["Other"] >= 1, "Alert processing is the service's own work");
            Assert.AreEqual(noInstance["ErrorCount"], ErrorList(cn, "dbo.CollectionErrorLog_Get", null, ("InstanceDisplayName", "(No instance)")).Rows.Count, "The instance link lists them");
        }

        private static DataTable ErrorList(SqlConnection cn, string proc, int? instance, params (string Name, object Value)[] parameters)
        {
            using var cmd = new SqlCommand(proc, cn) { CommandType = CommandType.StoredProcedure };
            if (instance.HasValue) cmd.Parameters.AddWithValue("InstanceID", instance.Value);
            cmd.Parameters.AddWithValue("Days", 1);
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        private static List<string> ErrorLogInsights(SqlConnection cn, int instance, string groupBy, params (string Name, object Value)[] parameters)
        {
            using var cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instance);
            cmd.Parameters.AddWithValue("Days", 1);
            cmd.Parameters.AddWithValue("GroupBy", groupBy);
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            var ds = new DataSet();
            new SqlDataAdapter(cmd).Fill(ds);
            Assert.AreEqual(2, ds.Tables.Count, "The errors and the insights");
            return Insights(ds);
        }

        private static DataTable ErrorLog(SqlConnection cn, int instance, DateTime? from, DateTime? to, bool includeRetries)
        {
            using var cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instance);
            cmd.Parameters.AddWithValue("ErrorSource", "ErrLink");
            cmd.Parameters.AddWithValue("Days", 7);
            if (from.HasValue) cmd.Parameters.AddWithValue("FromDate", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("ToDate", to.Value);
            cmd.Parameters.AddWithValue("IncludeRetries", includeRetries);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        [TestMethod]
        public void ReportGroupsByInstanceOrCollection()
        {
            using var cn = Open();
            var byInstance = Report(cn, groupByInstance: true);
            CollectionAssert.Contains(Columns(byInstance), "InstanceDisplayName");
            CollectionAssert.DoesNotContain(Columns(byInstance), "InstanceCount");
            var waits = byInstance.Tables[0].Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "Waits");
            Assert.AreEqual(4, waits["Status"]);
            Assert.AreEqual(100m, waits["SucceededPct"]);

            var byCollection = Report(cn, groupByInstance: false);
            CollectionAssert.DoesNotContain(Columns(byCollection), "InstanceDisplayName");
            Assert.AreEqual(1, byCollection.Tables[0].Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "Waits")["InstanceCount"]);
            CollectionAssert.Contains(Columns(byCollection), "AvgImportLagSec", "Lag is per collection");
            Assert.AreEqual(5, byCollection.Tables.Count, "Grid, runs by hour, cost by hour, import lag by hour and insights");
            CollectionAssert.AreEqual(new[] { "InsightSeverity", "Insight" }, ColumnNames(byCollection.Tables[4]), "The insights, last");
            Assert.IsTrue(byCollection.Tables[1].Rows.Count > 0);
            Assert.IsTrue(byCollection.Tables[3].Rows.Count > 0);
            Assert.IsInstanceOfType<decimal>(waits["AvgImportMs"]);
            Assert.IsInstanceOfType<decimal>(waits["AvgImportLagSec"]);
        }

        /// <summary>The importer times each table and measures what it costs the repository, against the run it belongs to</summary>
        [TestMethod]
        public void ImportIsTimedAndMeasured()
        {
            foreach (var reference in new[] { "Instance", "Waits" })
            {
                var run = CollectedRun(reference);
                Assert.IsInstanceOfType<int>(run["ImportMs"], reference);
                Assert.IsInstanceOfType<int>(run["ImportCpuMs"], reference);
                Assert.IsInstanceOfType<long>(run["ImportLogicalReads"], reference);
            }
            Assert.AreEqual(DBNull.Value, CollectedRun("UserData.Missing")["ImportMs"], "Nothing to import");
            using var cn = Open();
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.ImportCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'Waits'")));
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.ImportCpuCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'Waits'")));
            Assert.IsInstanceOfType<int>(Scalar(cn, Sql("SELECT H.ImportMs FROM dbo.CollectionRuns H {0} WHERE R.Reference = 'Waits'")));
            Assert.AreEqual(1, Scalar(cn, "SELECT COUNT(*) FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Import Duration (ms)' AND instance_name = N'Waits' AND CounterType = 5"));
        }

        /// <summary>
        /// The file's lag is recorded against each run in it - the same for all of them - and as a counter.  Imported straight
        /// after it was sent, as for a SQL destination, so near zero - not the time collecting the batch took.
        /// </summary>
        [TestMethod]
        public void ImportLagIsRecordedForEachRun()
        {
            using var cn = Open();
            var lags = new List<int>();
            using (var cmd = new SqlCommand(Sql("SELECT H.ImportLagMs FROM dbo.CollectionRuns H {0} WHERE R.Reference IN('Instance','Waits','TraceFlags')"), cn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) lags.Add(rdr.GetInt32(0));
            }
            Assert.AreEqual(3, lags.Count);
            Assert.AreEqual(1, lags.Distinct().Count(), "One lag per file");
            Assert.IsTrue(lags[0] is >= 0 and < 5000, $"Imported straight after it was sent: {lags[0]}ms");
            Assert.AreEqual(1, Scalar(cn, Sql("SELECT H.ImportLagCount FROM dbo.CollectionRuns_60MIN H {0} WHERE R.Reference = 'Waits'")));
            Assert.AreEqual(1, Scalar(cn, "SELECT COUNT(*) FROM dbo.Counters WHERE object_name = N'DBADash' AND counter_name = N'Collection Import Lag (ms)' AND instance_name = N'Instance' AND CounterType = 5"));
        }

        /// <summary>Collected and imported clocks that disagree - an import "before" the collection - count as no lag</summary>
        [TestMethod]
        public void NegativeLagIsZero()
        {
            using var cn = Open();
            var runStart = DateTime.UtcNow.AddMinutes(-20).AddSeconds(Interlocked.Increment(ref twoRunsCalls));
            using (var cmd = new SqlCommand("dbo.CollectionRuns_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("SnapshotDate", runStart);
                cmd.Parameters.AddWithValue("CollectionRuns", Runs(("ClockSkew", runStart, CollectionRunStatus.Succeeded)));
                cmd.Parameters.AddWithValue("StoreDetail", true);
                cmd.Parameters.AddWithValue("SentDate", runStart);
                cmd.Parameters.AddWithValue("ImportStartDate", runStart.AddMinutes(-1));
                cmd.ExecuteNonQuery();
            }
            Assert.AreEqual(0, Scalar(cn, Sql("SELECT H.ImportLagMs FROM dbo.CollectionRuns H {0} WHERE R.Reference = 'ClockSkew'")));
        }

        /// <summary>
        /// The lag is from the batch being sent, not from its SnapshotDate - when collecting started, so it would count the
        /// time collecting the rest of the batch took.  A file without the sent time has no lag.
        /// </summary>
        [TestMethod]
        public void LagIsFromWhenTheBatchWasSent()
        {
            using var cn = Open();
            var instance = (int)Scalar(cn, "INSERT INTO dbo.Instances(Instance,ConnectionID,EngineEdition,IsActive) OUTPUT INSERTED.InstanceID VALUES(N'SentLag',N'SentLag',3,1)");
            var started = DateTime.UtcNow.AddMinutes(-30);
            void Import(DateTime snapshotDate, string reference, DateTime? sent)
            {
                using var cmd = new SqlCommand("dbo.CollectionRuns_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", instance);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("CollectionRuns", Runs((reference, snapshotDate, CollectionRunStatus.Succeeded)));
                cmd.Parameters.AddWithValue("StoreDetail", true);
                cmd.Parameters.AddWithValue("SentDate", (object?)sent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ImportStartDate", started.AddMinutes(12));
                cmd.ExecuteNonQuery();
            }
            // Collecting started at 0, the batch was sent at 10 minutes (to S3, say) and imported at 12
            Import(started, "SentLag", started.AddMinutes(10));
            Import(started.AddSeconds(1), "NoSentDate", null);
            Assert.AreEqual(120000, Scalar(cn, $"SELECT H.ImportLagMs FROM dbo.CollectionRuns H JOIN dbo.CollectionRunReferences R ON R.ReferenceID = H.ReferenceID WHERE H.InstanceID = {instance} AND R.Reference = 'SentLag'"));
            Assert.AreEqual(DBNull.Value, Scalar(cn, $"SELECT H.ImportLagMs FROM dbo.CollectionRuns H JOIN dbo.CollectionRunReferences R ON R.ReferenceID = H.ReferenceID WHERE H.InstanceID = {instance} AND R.Reference = 'NoSentDate'"));
        }

        /// <summary>
        /// The sent time goes with the file to S3 or a folder, and is kept when the file goes through the destinations again
        /// on its way into the repository - so the lag covers the time it waited there.
        /// </summary>
        [TestMethod]
        public void SentTimeIsKeptThroughAFile()
        {
            var ds = new DataSet("DBADash");
            ds.Tables.Add(Runs(("Waits", DateTime.UtcNow, CollectionRunStatus.Succeeded)));
            Assert.IsNull(CollectionRunsTable.GetSent(ds.Tables[CollectionRunsTable.TableName]!));
            var before = DateTime.UtcNow;
            CollectionRunsTable.MarkSent(ds);
            var sent = CollectionRunsTable.GetSent(ds.Tables[CollectionRunsTable.TableName]!);
            Assert.IsNotNull(sent);
            Assert.AreEqual(DateTimeKind.Utc, sent.Value.Kind);
            Assert.IsTrue(sent.Value >= before.AddMilliseconds(-1) && sent.Value <= DateTime.UtcNow);

            using var ms = new MemoryStream();
            ds.WriteXml(ms, XmlWriteMode.WriteSchema);
            ms.Position = 0;
            var read = new DataSet();
            read.ReadXml(ms);
            Thread.Sleep(20);
            CollectionRunsTable.MarkSent(read);
            Assert.AreEqual(sent, CollectionRunsTable.GetSent(read.Tables[CollectionRunsTable.TableName]!), "Kept, not the time it was read back");

            CollectionRunsTable.MarkSent(new DataSet()); // Nothing to mark - a heartbeat, say
        }

        private static int twoRunsCalls;

        /// <summary>
        /// Two runs 10 minutes apart in an hour well in the past, imported as separate files.  The files' times differ by a few
        /// seconds on each call - a file with the same time as one already imported is skipped as a re-import.
        /// </summary>
        private static DateTime ImportTwoRuns(SqlConnection cn, string reference, bool storeDetail)
        {
            var now = DateTime.UtcNow;
            var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-3);
            var offset = TimeSpan.FromSeconds(Interlocked.Increment(ref twoRunsCalls));
            ImportRuns(cn, Runs((reference, hour.AddMinutes(5) + offset, CollectionRunStatus.Succeeded)), hour.AddMinutes(5) + offset, storeDetail);
            ImportRuns(cn, Runs((reference, hour.AddMinutes(15) + offset, CollectionRunStatus.Failed)), hour.AddMinutes(15) + offset, storeDetail);
            return hour;
        }

        private static DataSet Report(SqlConnection cn, string procedure, string reference, DateTime from, DateTime to)
        {
            using var cmd = new SqlCommand(procedure, cn) { CommandType = CommandType.StoredProcedure };
            var ids = new DataTable();
            ids.Columns.Add("ID", typeof(int));
            ids.Rows.Add(instanceID);
            cmd.Parameters.AddWithValue("InstanceIDs", ids).SqlDbType = SqlDbType.Structured;
            cmd.Parameters.AddWithValue("Reference", reference);
            cmd.Parameters.AddWithValue("FromDate", from);
            cmd.Parameters.AddWithValue("ToDate", to);
            var ds = new DataSet();
            new SqlDataAdapter(cmd).Fill(ds);
            return ds;
        }

        private static int Buckets(DataTable chart) => chart.Rows.Cast<DataRow>().Select(r => (DateTime)r["SnapshotDate"]).Distinct().Count();

        /// <summary>With each run kept, the charts show buckets of a few minutes and the range is exact</summary>
        [TestMethod]
        public void RunsKeptGiveFinerChartsAndExactRanges()
        {
            using var cn = Open();
            var hour = ImportTwoRuns(cn, "Granular", storeDetail: true);
            var ds = Report(cn, "dbo.CollectionHealth_Get", "Granular", hour, hour.AddHours(1));
            Assert.AreEqual(2, Buckets(ds.Tables[1]), "5 minute buckets");
            Assert.AreEqual(2, ds.Tables[0].Rows[0]["RunCount"]);

            ds = Report(cn, "dbo.CollectionHealth_Get", "Granular", hour.AddMinutes(10), hour.AddHours(1));
            Assert.AreEqual(1, ds.Tables[0].Rows[0]["RunCount"], "Only the run after the start of the range");
        }

        /// <summary>Without the runs, the hourly rollup - one bucket, and the hour the range starts in counted whole</summary>
        [TestMethod]
        public void HourlyOnlyChartsAreHourly()
        {
            using var cn = Open();
            var hour = ImportTwoRuns(cn, "HourlyCharts", storeDetail: false);
            var ds = Report(cn, "dbo.CollectionHealth_Get", "HourlyCharts", hour, hour.AddHours(1));
            Assert.AreEqual(1, Buckets(ds.Tables[1]));
            ds = Report(cn, "dbo.CollectionHealth_Get", "HourlyCharts", hour.AddMinutes(10), hour.AddHours(1));
            Assert.AreEqual(2, ds.Tables[0].Rows[0]["RunCount"]);
        }

        [TestMethod]
        public void PerformanceReportShowsRunsOrHours()
        {
            using var cn = Open();
            var hour = ImportTwoRuns(cn, "PerfRuns", storeDetail: true);
            var ds = Report(cn, "dbo.CollectionPerformance_Get", "PerfRuns", hour, hour.AddHours(1));
            Assert.AreEqual(5, ds.Tables.Count, "Runs, duration, CPU, reads and runs by outcome - no import lag, it's the batch's");
            CollectionAssert.AreEquivalent(new[] { "Failed", "Succeeded" }, ds.Tables[0].Rows.Cast<DataRow>().Select(r => (string)r["Outcome"]).ToArray());
            Assert.IsTrue(ds.Tables[0].Rows.Cast<DataRow>().All(r => (string)r["Period"] == "Run"));
            Assert.AreEqual(2, Buckets(ds.Tables[1]), "Duration in 5 minute buckets");

            hour = ImportTwoRuns(cn, "PerfHours", storeDetail: false);
            ds = Report(cn, "dbo.CollectionPerformance_Get", "PerfHours", hour, hour.AddHours(1));
            var row = ds.Tables[0].Rows.Cast<DataRow>().Single();
            Assert.AreEqual("Hour", row["Period"]);
            Assert.AreEqual(2, row["RunCount"]);
            Assert.AreEqual(1, Convert.ToInt32(row["Status"]), "An hour with a failure");
        }

        #region Helpers

        private static string Sql(string format) => string.Format(format, $"JOIN dbo.CollectionRunReferences R ON R.ReferenceID = H.ReferenceID AND H.InstanceID = {instanceID}");

        private static List<string> Columns(DataSet ds) => ds.Tables[0].Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

        private static List<string> ColumnNames(DataTable dt) => dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

        private static DataTable Runs(params (string Reference, DateTime Start, CollectionRunStatus Status)[] runs)
        {
            var dt = CollectionRunsTable.Create();
            foreach (var (reference, start, status) in runs)
            {
                dt.Rows.Add(reference, "", start, (byte)status, (byte)1, 10, DBNull.Value, DBNull.Value);
            }
            CollectionRunsTable.EnsureImportColumns(dt);
            return dt;
        }

        private static void ImportRuns(SqlConnection cn, DataTable runs, DateTime snapshotDate, bool storeDetail, int? instance = null)
        {
            using var cmd = new SqlCommand("dbo.CollectionRuns_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instance ?? instanceID);
            cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
            cmd.Parameters.AddWithValue("CollectionRuns", runs);
            cmd.Parameters.AddWithValue("StoreDetail", storeDetail);
            cmd.Parameters.AddWithValue("BatchID", (object?)CollectionRunsTable.GetBatchId(runs) ?? DBNull.Value); // As the importer
            cmd.ExecuteNonQuery();
        }

        private static void AssertStatus(SqlConnection cn, string reference, int consecutiveFailures, DateTime lastSuccess)
        {
            Assert.AreEqual(consecutiveFailures, Scalar(cn, Sql($"SELECT H.ConsecutiveFailures FROM dbo.CollectionRunStatus H {{0}} WHERE R.Reference = '{reference}'")));
            var stored = (DateTime)Scalar(cn, Sql($"SELECT H.LastSuccessDate FROM dbo.CollectionRunStatus H {{0}} WHERE R.Reference = '{reference}'"));
            Assert.IsTrue(Math.Abs((stored - lastSuccess).TotalMilliseconds) < 20, $"Last success {stored:O}, expected {lastSuccess:O}");
        }

        private static int HealthStatus(SqlConnection cn, string reference, int? instance = null) => Convert.ToInt32(HealthRow(cn, reference, instance)["Status"]);

        private static DataRow HealthRow(SqlConnection cn, string reference, int? instance = null)
        {
            var ds = Report(cn, groupByInstance: true, reference, instance);
            return ds.Tables[0].Rows.Cast<DataRow>().Single();
        }

        private static void SetThresholds(SqlConnection cn, int instance, string reference, params (string Name, object Value)[] values)
        {
            using var cmd = new SqlCommand("dbo.CollectionHealthThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instance);
            cmd.Parameters.AddWithValue("Reference", reference);
            foreach (var (name, value) in values)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.ExecuteNonQuery();
        }

        private static DataRow GetThresholds(SqlConnection cn, int instance, string reference)
        {
            using var cmd = new SqlCommand("dbo.CollectionHealthThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instance);
            cmd.Parameters.AddWithValue("Reference", reference);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt.Rows.Cast<DataRow>().Single();
        }

        private static int SummaryStatus(SqlConnection cn, int instance)
        {
            Execute(cn, "EXEC dbo.Summary_Upd");
            return Convert.ToInt32(Scalar(cn, $"SELECT CollectionErrorStatus FROM dbo.Summary WHERE InstanceID = {instance}"));
        }

        private static DataSet Report(SqlConnection cn, bool groupByInstance, string? reference = null, int? instance = null, int[]? instances = null)
        {
            using var cmd = new SqlCommand("dbo.CollectionHealth_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceIDs", Ids(instances ?? new[] { instance ?? instanceID })).SqlDbType = SqlDbType.Structured;
            cmd.Parameters.AddWithValue("GroupByInstance", groupByInstance);
            if (reference != null) cmd.Parameters.AddWithValue("Reference", reference);
            var ds = new DataSet();
            new SqlDataAdapter(cmd).Fill(ds);
            return ds;
        }

        private static DataTable Ids(IEnumerable<int> ids)
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            foreach (var id in ids) dt.Rows.Add(id);
            return dt;
        }

        /// <summary>The cards for the report's insights result set (the last) - InsightSeverity: Insight</summary>
        private static List<string> Insights(DataSet ds) =>
            ds.Tables[ds.Tables.Count - 1].Rows.Cast<DataRow>().Select(r => $"{r["InsightSeverity"]}: {r["Insight"]}").ToList();

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(RepositoryConnectionString);
            cn.Open();
            return cn;
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }

        private static object Scalar(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            return cmd.ExecuteScalar();
        }

        #endregion Helpers
    }
}
