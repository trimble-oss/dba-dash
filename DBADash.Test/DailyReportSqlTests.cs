using DBADash.DailyReport;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>Daily report procs against a repository database created from the DBADashDB dacpac (LocalDB unless a server is set in the environment variable)</summary>
    [TestClass]
    public partial class DailyReportSqlTests
    {
        private static readonly string Database = "DBADashDailyReportTest_" + Guid.NewGuid().ToString("N");
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
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

        private static void Exec(string sql)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        private static int Scalar(string sql)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>An email server.  The SMTP settings don't matter here - nothing is sent</summary>
        private static int AddEmailServer(string name)
        {
            return Scalar($@"
INSERT INTO Alert.EmailServer(EmailServerName,ServerDetails)
VALUES(N'{name}',N'{{}}');
SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }

        private static DailyReportConfig NewReport(string name, int? emailServerID = null) => new()
        {
            ReportName = name,
            IsActive = true,
            ScheduleTime = TimeSpan.Zero,
            TimeZone = "UTC",
            ApplyToTagID = -1,
            ChangeThresholdPct = 50,
            EmailServerID = emailServerID,
            Recipients = "ops@example.com"
        };

        [TestMethod]
        public void Save_AddsReportWithItsServerAndRecipients()
        {
            var serverID = AddEmailServer("Save-Ops");
            var config = NewReport("Save-Morning", serverID);
            config.IncludePerformance = true;
            config.Recipients = "a@example.com, b@example.com" + Environment.NewLine + " A@example.com ;";

            var id = DailyReportRepository.Save(ConnectionString, config);

            Assert.IsTrue(id > 0);
            var saved = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.AreEqual("Save-Morning", saved.ReportName);
            Assert.IsTrue(saved.IncludePerformance);
            Assert.AreEqual("{All}", saved.ScopeDescription);
            Assert.AreEqual(serverID, saved.EmailServerID);
            Assert.AreEqual("Save-Ops", saved.EmailServerName);
            Assert.AreEqual("a@example.com; b@example.com", saved.Recipients, "Stored without blanks or duplicates, separated by \"; \"");
            CollectionAssert.AreEqual(new[] { "a@example.com", "b@example.com" }, saved.RecipientList);
        }

        [TestMethod]
        public void Save_UpdateChangesServerAndRecipients()
        {
            var first = AddEmailServer("Upd-First");
            var second = AddEmailServer("Upd-Second");
            var config = NewReport("Upd-Report", first);
            var id = DailyReportRepository.Save(ConnectionString, config);

            config = DailyReportRepository.GetReports(ConnectionString, id).Single();
            config.EmailServerID = second;
            config.Recipients = "dba@example.com";
            config.ChangeThresholdPct = 75;
            DailyReportRepository.Save(ConnectionString, config);

            var saved = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.AreEqual(second, saved.EmailServerID);
            Assert.AreEqual("Upd-Second", saved.EmailServerName);
            Assert.AreEqual("dba@example.com", saved.Recipients);
            Assert.AreEqual(75, saved.ChangeThresholdPct);
        }

        [TestMethod]
        public void Save_RejectsDuplicateName()
        {
            var serverID = AddEmailServer("Dup-Ops");
            DailyReportRepository.Save(ConnectionString, NewReport("Dup-Name", serverID));
            Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, NewReport("Dup-Name", serverID)));
        }

        [TestMethod]
        public void Save_RejectsUnknownTimeZone()
        {
            var serverID = AddEmailServer("Tz-Ops");
            var config = NewReport("Tz-Report", serverID);
            config.TimeZone = "Not/AZone";
            Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, config));
        }

        [TestMethod]
        public void Save_RequiresAnEmailServer()
        {
            var ex = Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, NewReport("No-Server")));
            StringAssert.Contains(ex.Message, "email server");
        }

        [TestMethod]
        public void Save_RequiresRecipients()
        {
            var config = NewReport("No-Recipients", AddEmailServer("NoRcpt-Ops"));
            config.Recipients = " ; ";
            var ex = Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, config));
            StringAssert.Contains(ex.Message, "email address");
        }

        [TestMethod]
        public void Claim_ReturnsDueReportOncePerDay()
        {
            var serverID = AddEmailServer("Claim-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Claim-Daily", serverID)); // 00:00 UTC has always passed
            CreatedYesterday(id);

            var first = DailyReportRepository.ClaimDueReports(ConnectionString);
            CollectionAssert.Contains(first, id);

            var second = DailyReportRepository.ClaimDueReports(ConnectionString);
            CollectionAssert.DoesNotContain(second, id, "A claimed report must not be sent again the same day");

            var saved = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.IsNotNull(saved.LastRunUTC);
        }

        /// <summary>A report created after today's send time waits for tomorrow, so tests that need one due today backdate it</summary>
        private static void CreatedYesterday(int id) =>
            Exec($"UPDATE dbo.DailyReport SET CreatedDate = DATEADD(d,-1,SYSUTCDATETIME()) WHERE DailyReportID = {id}");

        [TestMethod]
        public void Claim_NewReportAfterSendTimeWaitsForTomorrow()
        {
            var serverID = AddEmailServer("New-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("New-Report", serverID)); // created after 00:00 today
            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id);
        }

        [TestMethod]
        public void Claim_OnlyOnTheSelectedDays()
        {
            var serverID = AddEmailServer("Days-Ops");
            var today = DailyReportConfig.DayBit(DateTime.UtcNow.DayOfWeek); // the reports are in UTC
            var notToday = NewReport("Days-NotToday", serverID);
            notToday.DaysOfWeek = DailyReportConfig.AllDays & ~today;
            var notTodayID = DailyReportRepository.Save(ConnectionString, notToday);
            var todayOnly = NewReport("Days-Today", serverID);
            todayOnly.DaysOfWeek = today;
            var todayID = DailyReportRepository.Save(ConnectionString, todayOnly);
            CreatedYesterday(notTodayID);
            CreatedYesterday(todayID);

            var claimed = DailyReportRepository.ClaimDueReports(ConnectionString);
            CollectionAssert.DoesNotContain(claimed, notTodayID);
            CollectionAssert.Contains(claimed, todayID);
            Assert.AreEqual(today, DailyReportRepository.GetReports(ConnectionString, todayID).Single().DaysOfWeek);
        }

        [TestMethod]
        public void Claim_RepeatingReportOncePerSlot()
        {
            var serverID = AddEmailServer("Repeat-Ops");
            var report = NewReport("Repeat-Report", serverID);
            report.IntervalMinutes = 15;
            var id = DailyReportRepository.Save(ConnectionString, report);
            CreatedYesterday(id);

            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id);
            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Once per slot");
            DailyReportRepository.SetRunResult(ConnectionString, id, null); // the run finished

            // The last run was in an earlier slot - so the current one is due
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-16,SYSUTCDATETIME()) WHERE DailyReportID = {id}");
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Next slot");
        }

        [TestMethod]
        public void Claim_RepeatingReportStopsAtTheEndTime()
        {
            if (DateTime.UtcNow.TimeOfDay < TimeSpan.FromMinutes(20)) Assert.Inconclusive("Needs a run earlier today, after 00:00 UTC");
            var serverID = AddEmailServer("Window-Ops");
            var report = NewReport("Window-Report", serverID);
            report.IntervalMinutes = 15;
            report.WindowEndTime = TimeSpan.Zero; // one slot: 00:00
            var id = DailyReportRepository.Save(ConnectionString, report);
            CreatedYesterday(id);
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-16,SYSUTCDATETIME()) WHERE DailyReportID = {id}");

            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Today's only slot has run");
            Assert.AreEqual(TimeSpan.Zero, DailyReportRepository.GetReports(ConnectionString, id).Single().WindowEndTime);
        }

        [TestMethod]
        public void Claim_NotWhileTheLastRunIsInProgress()
        {
            var serverID = AddEmailServer("Busy-Ops");
            var report = NewReport("Busy-Report", serverID);
            report.IntervalMinutes = 15;
            var id = DailyReportRepository.Save(ConnectionString, report);
            CreatedYesterday(id);
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id);

            // The next slot is due, but the run hasn't recorded its result yet (another service is still sending it)
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-16,SYSUTCDATETIME()), RunningSinceUTC = DATEADD(mi,-16,SYSUTCDATETIME()) WHERE DailyReportID = {id}");
            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Still running");

            // A run that never finished stops blocking after an hour
            Exec($"UPDATE dbo.DailyReport SET RunningSinceUTC = DATEADD(mi,-61,SYSUTCDATETIME()) WHERE DailyReportID = {id}");
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Stale run");

            DailyReportRepository.SetRunResult(ConnectionString, id, null);
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM dbo.DailyReport WHERE DailyReportID = {id} AND RunningSinceUTC IS NOT NULL"), "Recording the result ends the run");
        }

        [TestMethod]
        public void Save_RejectsNoDaysAndBadInterval()
        {
            var serverID = AddEmailServer("Bad-Ops");
            var noDays = NewReport("Bad-NoDays", serverID);
            noDays.DaysOfWeek = 0;
            Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, noDays));
            var tooOften = NewReport("Bad-Interval", serverID);
            tooOften.IntervalMinutes = 5;
            Assert.ThrowsExactly<SqlException>(() => DailyReportRepository.Save(ConnectionString, tooOften));
        }

        [TestMethod]
        public void Save_KeepsTypeAndScheduleAndDropsEndTimeWithoutInterval()
        {
            var serverID = AddEmailServer("Type-Ops");
            var report = NewReport("Type-Report", serverID);
            report.ReportType = DailyReportType.Changes;
            report.WindowEndTime = new TimeSpan(18, 0, 0); // no interval, so no end time
            var id = DailyReportRepository.Save(ConnectionString, report);
            var saved = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.AreEqual(DailyReportType.Changes, saved.ReportType);
            Assert.IsNull(saved.IntervalMinutes);
            Assert.IsNull(saved.WindowEndTime);
        }

        [TestMethod]
        public void Claim_RetriesARunThatSentNothing()
        {
            var serverID = AddEmailServer("Retry-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Retry-Report", serverID));
            CreatedYesterday(id);
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id);
            DailyReportRepository.SetRunResult(ConnectionString, id, "SMTP down", retry: true);

            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Not before the retry delay");

            // Past the retry delay (claimed 20 minutes ago) - but if it's just after midnight UTC that's yesterday, so only check on the same day
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-20,SYSUTCDATETIME()) WHERE DailyReportID = {id} AND CAST(DATEADD(mi,-20,SYSUTCDATETIME()) AS DATE) = CAST(SYSUTCDATETIME() AS DATE)");
            if (Scalar($"SELECT CASE WHEN CAST(LastRunUTC AS DATE) = CAST(SYSUTCDATETIME() AS DATE) AND LastRunUTC < DATEADD(mi,-15,SYSUTCDATETIME()) THEN 1 ELSE 0 END FROM dbo.DailyReport WHERE DailyReportID = {id}") == 0)
            {
                Assert.Inconclusive("Too close to midnight UTC to test a same-day retry");
            }
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Retried after the delay");
            Assert.AreEqual(1, Scalar($"SELECT RetryAttempts FROM dbo.DailyReport WHERE DailyReportID = {id}"));
        }

        [TestMethod]
        public void Claim_RecoversARunThatDiedBeforeSending()
        {
            // The claim is backdated 61 minutes, which must still be today (after the 00:00 slot)
            if (DateTime.UtcNow.TimeOfDay < TimeSpan.FromMinutes(65)) Assert.Inconclusive("Needs a claim earlier today, after 00:00 UTC");
            var serverID = AddEmailServer("Died-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Died-Report", serverID));
            CreatedYesterday(id);
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id);

            // The service stopped mid-run: no result, nothing sent.  Once the claim is stale, today's slot is tried again
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-61,SYSUTCDATETIME()), RunningSinceUTC = DATEADD(mi,-61,SYSUTCDATETIME()) WHERE DailyReportID = {id}");
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Unfinished and unsent");
            Assert.AreEqual(1, Scalar($"SELECT RetryAttempts FROM dbo.DailyReport WHERE DailyReportID = {id}"));

            // The same, but the mail server accepted it before the service stopped: not sent twice
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-61,SYSUTCDATETIME()), RunningSinceUTC = DATEADD(mi,-61,SYSUTCDATETIME()), LastSentUTC = DATEADD(mi,-60,SYSUTCDATETIME()) WHERE DailyReportID = {id}");
            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Already sent");
        }

        [TestMethod]
        public void RunLock_SendNowAndScheduledRunsDontOverlap()
        {
            var serverID = AddEmailServer("Lock-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Lock-Report", serverID));
            CreatedYesterday(id);

            var lockUtc = DailyReportRepository.AcquireRunLock(ConnectionString, id);
            Assert.IsNotNull(lockUtc, "Free");
            Assert.IsNull(DailyReportRepository.AcquireRunLock(ConnectionString, id), "Held by the first Send now");
            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id, "The scheduled run waits");

            DailyReportRepository.ReleaseRunLock(ConnectionString, id, lockUtc!.Value);
            CollectionAssert.Contains(DailyReportRepository.ClaimDueReports(ConnectionString), id, "Claimed once the Send now has finished");
            Assert.IsNull(DailyReportRepository.AcquireRunLock(ConnectionString, id), "Held by the scheduled run");

            // Releasing an old lock doesn't clear the scheduled run's
            DailyReportRepository.ReleaseRunLock(ConnectionString, id, lockUtc.Value);
            Assert.AreEqual(1, Scalar($"SELECT COUNT(*) FROM dbo.DailyReport WHERE DailyReportID = {id} AND RunningSinceUTC IS NOT NULL"));
        }

        [TestMethod]
        public void Claim_DoesNotRetryAPartialSend()
        {
            // Backdating the last run 20 minutes must stay today, or it's in yesterday and today's 00:00 run is rightly due
            if (DateTime.UtcNow.TimeOfDay < TimeSpan.FromMinutes(25)) Assert.Inconclusive("Needs a run earlier today, after 00:00 UTC");
            var serverID = AddEmailServer("Partial-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Partial-Report", serverID));
            CreatedYesterday(id);
            DailyReportRepository.ClaimDueReports(ConnectionString);
            DailyReportRepository.SetRunResult(ConnectionString, id, "One channel failed", retry: false);
            Exec($"UPDATE dbo.DailyReport SET LastRunUTC = DATEADD(mi,-20,SYSUTCDATETIME()) WHERE DailyReportID = {id}");

            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id);
        }

        [TestMethod]
        public void Performance_ReadsHourlyRollupsForTodayAndBaselineWeeks()
        {
            var to = new DateTime(2026, 11, 12, 8, 30, 0, DateTimeKind.Utc); // rounded down to 08:00
            var instanceID = Scalar(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'PerfInst',N'PerfInst',1,1,3,0,GETUTCDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);");
            // Today 14:00 (hour offset 6 from 08:00 yesterday), and the same hour 1 week earlier.  Outside the window: 08:00 today
            Exec($@"
INSERT INTO dbo.CPU_60MIN(InstanceID,EventTime,SumSQLProcessCPU,SumSystemIdleCPU,SampleCount,MaxSQLProcessCPU,MaxOtherProcessCPU,MaxTotalCPU,CPU90,CPU100)
VALUES({instanceID},'2026-11-11T14:00:00',5400,600,60,95,0,95,0,60),
	({instanceID},'2026-11-04T14:00:00',1200,4800,60,20,0,20,0,0),
	({instanceID},'2026-11-12T08:00:00',6000,0,60,100,0,100,0,60);
INSERT INTO dbo.Deadlocks(InstanceID,EventTime,DeadlockHash) VALUES({instanceID},'2026-11-11T15:10:00',0x01),({instanceID},'2026-11-11T15:20:00',0x02);");

            var data = PerformanceDataRepository.Get(ConnectionString, instanceID.ToString(), to, 2);

            Assert.AreEqual(new DateTime(2026, 11, 12, 8, 0, 0), data.WindowEndUtc);
            var inst = data.Instances.Single();
            Assert.AreEqual("PerfInst", inst.Instance);
            var today = inst.Hours.Where(h => h.Period == 0).ToList();
            Assert.AreEqual(90, today.Single(h => h.HourOffset == 6).AvgCPU!.Value, 0.001);
            Assert.AreEqual(60, today.Single(h => h.HourOffset == 6).HighCPUSamples);
            Assert.AreEqual(2, today.Single(h => h.HourOffset == 7).Deadlocks);
            Assert.IsFalse(inst.Hours.Any(h => h.HourOffset >= 24), "The hour at the window end is outside it");
            Assert.AreEqual(20, inst.Hours.Single(h => h.Period == 1).AvgCPU!.Value, 0.001);
            Assert.AreEqual(90, data.Thresholds.CPUCritical);
        }

        [TestMethod]
        public void Save_KeepsSettings()
        {
            var serverID = AddEmailServer("Settings-Ops");
            var config = NewReport("Settings-Report", serverID);
            config.Settings.BaselineWeeks = 3;
            config.Settings.IncludeAISummary = true;
            config.Settings.MetricSettings["Deadlocks"] = new PerformanceMetricSetting { Enabled = false, MinChange = 2 };
            var id = DailyReportRepository.Save(ConnectionString, config);

            var saved = DailyReportRepository.GetReports(ConnectionString, id).Single().Settings;
            Assert.AreEqual(3, saved.BaselineWeeks);
            Assert.IsTrue(saved.IncludeAISummary);
            Assert.IsFalse(saved.IsEnabled("Deadlocks"));
        }

        [TestMethod]
        public void SendResult_RecordedWithoutTouchingTheSchedule()
        {
            var serverID = AddEmailServer("SendRes-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("SendRes-Report", serverID));

            DailyReportRepository.SetSendResult(ConnectionString, id, false, "Ops: not sent - SMTP down");
            var failed = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.IsNull(failed.LastSentUTC, "Not sent anywhere");
            Assert.IsNotNull(failed.LastSendAttemptUTC);
            Assert.AreEqual("Ops: not sent - SMTP down", failed.LastSendResult);

            DailyReportRepository.SetSendResult(ConnectionString, id, true, "Ops: accepted by the mail server for a@b.c");
            var sent = DailyReportRepository.GetReports(ConnectionString, id).Single();
            Assert.IsNotNull(sent.LastSentUTC);
            Assert.IsNull(sent.LastRunUTC, "A Send now is not the scheduled run - today's report still goes out");
        }

        [TestMethod]
        public void IssueState_NewOngoingResolved()
        {
            var serverID = AddEmailServer("State-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("State-Report", serverID));
            var day1 = new DateTime(2026, 11, 10, 8, 0, 0);
            var day2 = day1.AddDays(1);

            var first = IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("a", "A"), new ReportIssue("b", "B") }, day1, update: false);
            Assert.IsTrue(first.IsFirstRun);

            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("a", "A"), new ReportIssue("b", "B") }, day1, update: true);
            var second = IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("a", "A"), new ReportIssue("c", "C") }, day2, update: false);

            Assert.IsFalse(second.IsFirstRun);
            Assert.IsFalse(second.IsNew("a"));
            Assert.IsTrue(second.IsNew("c"));
            Assert.AreEqual(1, second.DaysOngoing("a"));
            Assert.AreEqual("B", second.Resolved.Single().Description);

            // A compare doesn't record: the same compare again gives the same answer
            Assert.IsTrue(IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("c", "C") }, day2, update: false).IsNew("c"));

            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("a", "A"), new ReportIssue("c", "C") }, day2, update: true);
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM dbo.DailyReportIssueState WHERE DailyReportID = {id} AND IssueKey = N'b'"), "Resolved issues are removed");
            Assert.AreEqual(1, Scalar($"SELECT COUNT(*) FROM dbo.DailyReportIssueState WHERE DailyReportID = {id} AND IssueKey = N'a' AND FirstSeenUTC = '2026-11-10T08:00:00'"), "Ongoing keeps first seen");
        }

        [TestMethod]
        public void Alerts_OncePerKeyOpenAndNotInformational()
        {
            Exec(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'AlertInst',N'AlertInst',1,1,3,0,GETUTCDATE())
DECLARE @ID INT = SCOPE_IDENTITY()
INSERT INTO Alert.ActiveAlerts(InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,GroupID,IsAcknowledged,IsResolved)
VALUES(@ID,5,'Counter','KeyA',N'first',N'not acknowledged','2026-11-13T08:00:00',0,0,0),
	(@ID,3,'Counter','KeyA',N'first',N'acknowledged','2026-11-13T07:00:00',1,1,0),   /* same alert, another group: acknowledged there */
	(@ID,2,'Counter','KeyB',N'first',N'resolved','2026-11-13T08:00:00',0,0,1),
	(@ID,35,'Counter','KeyC',N'first',N'informational','2026-11-13T08:00:00',0,0,0)");
            var id = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'AlertInst'");

            var alerts = DailyReportAlertRepository.Get(ConnectionString, id.ToString());

            var a = alerts.Single();
            Assert.AreEqual("KeyA", a.AlertKey);
            Assert.AreEqual(5, a.Priority, "The group that still needs attention");
            Assert.IsFalse(a.IsAcknowledged);
            Assert.AreEqual("not acknowledged", a.Message);
            Assert.AreEqual(new DateTime(2026, 11, 13, 7, 0, 0), a.TriggerUtc, "Open since the first group's trigger");
        }

        [TestMethod]
        public void IssueState_ReconciledKeyKeepsFirstSeenAcrossRuns()
        {
            var serverID = AddEmailServer("Recon-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Recon-Report", serverID));
            var drive = IssueHistory.ObjectKey("Drive space", "SQL1", "C:");
            var instanceOnly = IssueHistory.ObjectKey("Drive space", "SQL1", string.Empty);
            var day1 = new DateTime(2026, 11, 10, 8, 0, 0);
            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue(drive, "C:", 2) }, day1, update: true);

            // Described on the instance instead: recorded with the drive's first seen time, not this run's
            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue(instanceOnly, "Drive space", 2) }, day1.AddDays(1), update: true);
            var day3 = IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue(instanceOnly, "Drive space", 2) }, day1.AddDays(2), update: false);
            Assert.AreEqual(2, day3.DaysOngoing(instanceOnly));
        }

        [TestMethod]
        public void IssueState_KeepsKeysWhoseSourceCouldNotBeRead()
        {
            var serverID = AddEmailServer("Keep-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Keep-Report", serverID));
            var alert = IssueHistory.AlertKey("SQL1", "CPU");
            var run = new DateTime(2026, 11, 10, 8, 0, 0);
            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue(alert, "CPU", 10), new ReportIssue("chk|x|SQL1", "X", 1) }, run, update: true);

            // Alerts couldn't be read this time: they're kept; the check that went is removed
            IssueHistoryRepository.Compare(ConnectionString, id, Array.Empty<ReportIssue>(), run.AddHours(1), update: true, keepKeyPrefix: "alert|");
            Assert.AreEqual(1, Scalar($"SELECT COUNT(*) FROM dbo.DailyReportIssueState WHERE DailyReportID = {id} AND IssueKey = N'{alert}'"));
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM dbo.DailyReportIssueState WHERE DailyReportID = {id} AND IssueKey = N'chk|x|SQL1'"));
        }

        [TestMethod]
        public void Activity_LongRunningRequestsFromSnapshots()
        {
            Exec(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'ActInst',N'ActInst',1,1,3,0,GETUTCDATE())
DECLARE @ID INT = SCOPE_IDENTITY()
/* DBCC: started 01:00, seen at 01:10 and 01:40 - 40 minutes.  A 2 minute query and a sleeping session aren't long running activity */
INSERT INTO dbo.RunningQueries(InstanceID,SnapshotDateUTC,session_id,command,status,blocking_session_id,login_name,start_time_utc,last_request_start_time_utc,program_name,writes,reads,cpu_time)
VALUES(@ID,'2026-11-13T01:10:00',60,N'DBCC',N'running',0,N'sa','2026-11-13T01:00:00','2026-11-13T01:00:00',N'SQLAgent - TSQL JobStep (Job 0x0102030405060708090A0B0C0D0E0F10 : Step 2)',500,900,1000),
	(@ID,'2026-11-13T01:40:00',60,N'DBCC',N'running',0,N'sa','2026-11-13T01:00:00','2026-11-13T01:00:00',N'SQLAgent - TSQL JobStep (Job 0x0102030405060708090A0B0C0D0E0F10 : Step 2)',1200000,3400000,480000),
	(@ID,'2026-11-13T01:40:00',61,N'SELECT',N'running',0,N'app','2026-11-13T01:38:00','2026-11-13T01:38:00',N'App',0,10,5),
	(@ID,'2026-11-13T01:40:00',62,N'AWAITING COMMAND',N'sleeping',0,N'app','2026-11-13T00:00:00','2026-11-13T00:00:00',N'App',0,0,0)");
            var id = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'ActInst'");

            var rows = DailyReportActivityRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 12, 8, 0, 0), new DateTime(2026, 11, 13, 8, 0, 0));

            var dbcc = rows.Single();
            Assert.AreEqual("DBCC", dbcc.Command);
            Assert.AreEqual(2400, dbcc.DurationSec);
            Assert.AreEqual(1_200_000, dbcc.Writes, "The last snapshot's figures");
            Assert.AreEqual(2, dbcc.StepID);
            Assert.IsNull(dbcc.JobName, "The job isn't in dbo.Jobs here");

            // Counting only 09:00-17:00 (offsets 1-8 from 08:00): the 01:00 DBCC is left out before the limit, not after.  Its own hour counts
            var dayOnly = Enumerable.Range(1, 8).Select(h => (0, h)).ToHashSet();
            Assert.AreEqual(0, DailyReportActivityRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 12, 8, 0, 0), new DateTime(2026, 11, 13, 8, 0, 0), dayOnly).Count);
            var night = new HashSet<(int, int)> { (0, 17) };
            Assert.AreEqual(1, DailyReportActivityRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 12, 8, 0, 0), new DateTime(2026, 11, 13, 8, 0, 0), night).Count);
        }

        [TestMethod]
        public void Blockers_LeadBlockersFromSnapshots()
        {
            Exec(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'BlkInst',N'BlkInst',1,1,3,0,GETUTCDATE())
DECLARE @ID INT = SCOPE_IDENTITY()
INSERT INTO dbo.QueryText(sql_handle,text) VALUES(0x0B01,N'UPDATE dbo.Orders SET Status = ''secret'' WHERE ID = 7')
INSERT INTO dbo.BlockingSnapshotSummary(InstanceID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime,UTCOffset)
VALUES(@ID,'2026-11-13T09:10:00',3,0,0),(@ID,'2026-11-13T09:20:00',1,0,0)
/* 09:10: 70 (sleeping, open transaction) blocks 71, which blocks 72 - both are under 70.  73 'blocks' itself (parallel threads) and
   74 is blocked by 99, which wasn't captured: neither is a lead blocker.  09:20: 70 blocks 71 again */
INSERT INTO dbo.RunningQueries(InstanceID,SnapshotDateUTC,session_id,status,blocking_session_id,wait_time,login_name,last_request_start_time_utc,program_name,sql_handle,statement_start_offset,statement_end_offset)
VALUES(@ID,'2026-11-13T09:10:00',70,N'sleeping',0,NULL,N'app','2026-11-13T09:00:00',N'OrderApp',0x0B01,0,-1),
	(@ID,'2026-11-13T09:10:00',71,N'suspended',70,30000,N'app','2026-11-13T09:05:00',N'OrderApp',NULL,NULL,NULL),
	(@ID,'2026-11-13T09:10:00',72,N'suspended',71,45000,N'app','2026-11-13T09:05:00',N'OrderApp',NULL,NULL,NULL),
	(@ID,'2026-11-13T09:10:00',73,N'suspended',73,100,N'app','2026-11-13T09:05:00',N'OrderApp',NULL,NULL,NULL),
	(@ID,'2026-11-13T09:10:00',74,N'suspended',99,100,N'app','2026-11-13T09:05:00',N'OrderApp',NULL,NULL,NULL),
	(@ID,'2026-11-13T09:20:00',70,N'sleeping',0,NULL,N'app','2026-11-13T09:00:00',N'OrderApp',0x0B01,0,-1),
	(@ID,'2026-11-13T09:20:00',71,N'suspended',70,600000,N'app','2026-11-13T09:05:00',N'OrderApp',NULL,NULL,NULL)");
            var id = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'BlkInst'");

            var rows = DailyReportBlockerRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 13, 8, 0, 0), new DateTime(2026, 11, 14, 8, 0, 0));

            var lead = rows.Single();
            Assert.AreEqual(2, lead.Snapshots);
            Assert.AreEqual(2, lead.SleepingSnapshots);
            Assert.AreEqual(2, lead.MaxBlockedSessions, "71 and 72, down the chain");
            Assert.AreEqual(600000, lead.MaxBlockedWaitMs, "09:20's 10 minutes beats 09:10's 30s + 45s");
            Assert.AreEqual("OrderApp", lead.ProgramName);
            Assert.AreEqual(new DateTime(2026, 11, 13, 9, 10, 0), lead.FirstSeenUtc);
            Assert.AreEqual(new DateTime(2026, 11, 13, 9, 20, 0), lead.LastSeenUtc);
            Assert.AreEqual("UPDATE dbo.Orders SET Status = '?' WHERE ID = ?", DailyReportBlockerAnalyzer.Title(lead, forAi: false), "Captured as is, masked to show");

            // Counting only 10:00 onwards (offsets 2+ from 08:00): neither snapshot counts
            var later = Enumerable.Range(2, 22).Select(h => (0, h)).ToHashSet();
            Assert.AreEqual(0, DailyReportBlockerRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 13, 8, 0, 0), new DateTime(2026, 11, 14, 8, 0, 0), later).Count);
        }

        [TestMethod]
        public void Blockers_DeepChainsAndZeroHashes()
        {
            Exec(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'BlkDeep',N'BlkDeep',1,1,3,0,GETUTCDATE())
DECLARE @ID INT = SCOPE_IDENTITY()
INSERT INTO dbo.BlockingSnapshotSummary(InstanceID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime,UTCOffset)
VALUES(@ID,'2026-11-13T09:10:00',107,0,0)
/* Two lead blockers with no compiled plan (an all zero query_hash) running different statements: 200, at the head of a chain 120 deep
   (201 blocked by 200, 202 by 201...), and 400, blocking 401 */
INSERT INTO dbo.RunningQueries(InstanceID,SnapshotDateUTC,session_id,status,blocking_session_id,wait_time,login_name,last_request_start_time_utc,sql_handle,statement_start_offset,statement_end_offset,query_hash)
VALUES(@ID,'2026-11-13T09:10:00',200,N'running',0,NULL,N'app','2026-11-13T09:00:00',0x0B02,0,-1,0x0000000000000000),
	(@ID,'2026-11-13T09:10:00',400,N'running',0,NULL,N'app','2026-11-13T09:00:00',0x0B03,0,-1,0x0000000000000000),
	(@ID,'2026-11-13T09:10:00',401,N'suspended',400,1000,N'app','2026-11-13T09:05:00',NULL,NULL,NULL,NULL)
INSERT INTO dbo.RunningQueries(InstanceID,SnapshotDateUTC,session_id,status,blocking_session_id,wait_time,login_name,last_request_start_time_utc)
SELECT TOP(120) @ID,'2026-11-13T09:10:00',200 + ROW_NUMBER() OVER(ORDER BY (SELECT NULL)),N'suspended',199 + ROW_NUMBER() OVER(ORDER BY (SELECT NULL)),1000,N'app','2026-11-13T09:05:00'
FROM sys.all_objects");
            var id = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'BlkDeep'");

            var rows = DailyReportBlockerRepository.Get(ConnectionString, new[] { id }, new DateTime(2026, 11, 13, 8, 0, 0), new DateTime(2026, 11, 14, 8, 0, 0));

            Assert.AreEqual(2, rows.Count, "An all zero hash isn't shared - each statement is its own lead blocker");
            Assert.IsTrue(rows.All(r => r.QueryHash == null), "No hash rather than 0x0000000000000000");
            Assert.AreEqual(120, rows.Max(r => r.MaxBlockedSessions), "A chain deeper than the default recursion limit of 100");
        }

        [TestMethod]
        public void PerformanceScope_NarrowsTheReportsInstances()
        {
            Exec(@"
DECLARE @Prod INT, @DR INT
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'PerfScope-Env',N'Prod') SET @Prod = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'PerfScope-Env',N'DR') SET @DR = SCOPE_IDENTITY()
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'PerfProd',N'PerfProd',1,1,3,0,GETUTCDATE()),(N'PerfDR',N'PerfDR',1,1,3,0,GETUTCDATE())
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @Prod FROM dbo.Instances WHERE Instance = N'PerfProd'
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @DR FROM dbo.Instances WHERE Instance = N'PerfDR'");
            var prod = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'PerfProd'");
            var dr = Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'PerfDR'");
            var prodTag = Scalar("SELECT TagID FROM dbo.Tags WHERE TagName = N'PerfScope-Env' AND TagValue = N'Prod'");
            var report = new List<int> { prod, dr };

            CollectionAssert.AreEqual(report, DailyReportRepository.GetPerformanceInstanceIDs(ConnectionString, new DailyReportSettings(), report), "Same as the report");
            CollectionAssert.AreEqual(new List<int> { prod }, DailyReportRepository.GetPerformanceInstanceIDs(ConnectionString, new DailyReportSettings { PerformanceApplyToTagID = prodTag }, report));
            CollectionAssert.AreEqual(new List<int>(), DailyReportRepository.GetPerformanceInstanceIDs(ConnectionString, new DailyReportSettings { PerformanceApplyToTagID = prodTag }, new List<int> { dr }), "Never more than the report");
        }

        [TestMethod]
        public void IssueState_TracksStatusForWorseAndBetter()
        {
            var serverID = AddEmailServer("Sev-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Sev-Report", serverID));
            var run1 = new DateTime(2026, 11, 10, 8, 0, 0);
            IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("up", "Up", 2), new ReportIssue("down", "Down", 1), new ReportIssue("perf", "Perf") }, run1, update: true);

            var run2 = IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("up", "Up", 1), new ReportIssue("down", "Down", 2), new ReportIssue("perf", "Perf") }, run1.AddHours(1), update: false);
            Assert.IsTrue(run2.IsWorse("up", 1));
            Assert.IsFalse(run2.IsBetter("up", 1));
            Assert.IsTrue(run2.IsBetter("down", 2));
            Assert.IsFalse(run2.IsWorse("perf", null));
            Assert.IsFalse(run2.IsWorse("up", 2), "Same as before");
        }

        [TestMethod]
        public void IssueState_RunWithNoIssuesStillCountsAsARun()
        {
            var serverID = AddEmailServer("Empty-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Empty-Report", serverID));
            IssueHistoryRepository.Compare(ConnectionString, id, Array.Empty<ReportIssue>(), new DateTime(2026, 11, 10, 8, 0, 0), update: true);
            var next = IssueHistoryRepository.Compare(ConnectionString, id, new[] { new ReportIssue("z", "Z") }, new DateTime(2026, 11, 11, 8, 0, 0), update: false);
            Assert.IsFalse(next.IsFirstRun);
            Assert.IsTrue(next.IsNew("z"));
        }

        [TestMethod]
        public void TopQueries_ReadsObjectsAndAdHocQueries()
        {
            var instanceID = Scalar(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'TopQInst',N'TopQInst',1,1,3,0,GETUTCDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);");
            Exec($@"
DECLARE @DB INT, @Obj BIGINT, @Stmt BIGINT
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state,is_in_standby,recovery_model)
VALUES({instanceID},N'Sales',5,DATEADD(d,-30,GETUTCDATE()),150,0,0,0,0,0,0,0,0,0,1,0,0,1) SET @DB = SCOPE_IDENTITY()
INSERT INTO dbo.DBObjects(DatabaseID,ObjectType,object_id,SchemaName,ObjectName,IsActive) VALUES(@DB,'P',100,N'dbo',N'usp_Orders',1) SET @Obj = SCOPE_IDENTITY()
INSERT INTO dbo.ObjectExecutionStats_60MIN(InstanceID,ObjectID,SnapshotDate,PeriodTime,total_worker_time,total_elapsed_time,total_logical_reads,total_logical_writes,total_physical_reads,execution_count,IsCompile)
VALUES({instanceID},@Obj,'2026-11-11T14:00:00',3600000000,600000000,700000000,1000,0,0,50,0),
	({instanceID},@Obj,'2026-11-04T14:00:00',3600000000,60000000,70000000,1000,0,0,48,0)
INSERT INTO dbo.QueryStatements(InstanceID,StatementKey,StatementType,DatabaseID,statement_start_offset,statement_end_offset,query_hash,FirstSeen,LastSeen)
VALUES({instanceID},0x01,5,@DB,0,-1,0x0102030405060708,'2026-11-11','2026-11-11') SET @Stmt = SCOPE_IDENTITY()
INSERT INTO dbo.QueryStats_60MIN(InstanceID,SnapshotDate,StatementID,query_plan_hash,execution_count,total_worker_time,total_elapsed_time,total_logical_reads,total_logical_writes,total_physical_reads,total_clr_time,MaxPlanCount,IsCompile,IsOtherPlans,FirstPeriodStart,LastSnapshotDate)
VALUES({instanceID},'2026-11-11T15:00:00',@Stmt,0x01,10,20000000,20000000,500,7300,4200,0,1,0,0,'2026-11-11T15:00:00','2026-11-11T15:55:00')");

            var rows = TopQueryRepository.Get(ConnectionString, new[] { instanceID }, new DateTime(2026, 11, 12, 8, 0, 0), 1);
            var adhoc = rows.Single(r => r.Source == "Query");
            Assert.AreEqual(7300, adhoc.LogicalWrites, "For write latency");
            Assert.AreEqual(4200, adhoc.PhysicalReads, "For read latency");

            var proc = rows.Where(r => r.Source == "Object").ToList();
            Assert.AreEqual(600000, proc.Single(r => r.Period == 0).CPUMs, 0.001, "µs to ms");
            Assert.AreEqual(60000, proc.Single(r => r.Period == 1).CPUMs, 0.001);
            Assert.AreEqual("dbo.usp_Orders", proc[0].Name);
            Assert.AreEqual("Sales", proc[0].DatabaseName);
            var query = rows.Single(r => r.Source == "Query");
            Assert.AreEqual("0x0102030405060708", query.QueryHash);

            // Only the 14:00 hour (offset 6) counted: the procedure is still there, the 15:00 query isn't
            var hours = new HashSet<(int, int)> { (0, 6), (1, 6) };
            var counted = TopQueryRepository.Get(ConnectionString, new[] { instanceID }, new DateTime(2026, 11, 12, 8, 0, 0), 1, hours);
            Assert.AreEqual(600000, counted.Single(r => r.Source == "Object" && r.Period == 0).CPUMs, 0.001);
            Assert.IsFalse(counted.Any(r => r.Source == "Query"), "Outside the counted hours");
        }

        [TestMethod]
        public void Issues_BackupAgeUsesHoursUnderTwoDays()
        {
            Assert.AreEqual("45 minutes", Text("SELECT AgeText FROM dbo.DailyReport_AgeText(DATEADD(mi,-45,SYSUTCDATETIME()),SYSUTCDATETIME())"));
            Assert.AreEqual("5 hours", Text("SELECT AgeText FROM dbo.DailyReport_AgeText(DATEADD(mi,-330,SYSUTCDATETIME()),SYSUTCDATETIME())"));
            Assert.AreEqual("3 days", Text("SELECT AgeText FROM dbo.DailyReport_AgeText(DATEADD(hh,-80,SYSUTCDATETIME()),SYSUTCDATETIME())"));
        }

        private static string Text(string sql)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            return (string)cmd.ExecuteScalar();
        }

        [TestMethod]
        public void Claim_IgnoresInactiveReports()
        {
            var serverID = AddEmailServer("Inactive-Ops");
            var config = NewReport("Inactive-Report", serverID);
            config.IsActive = false;
            var id = DailyReportRepository.Save(ConnectionString, config);

            CollectionAssert.DoesNotContain(DailyReportRepository.ClaimDueReports(ConnectionString), id);
        }

        [TestMethod]
        public void RunResult_RecordsAndClearsError()
        {
            var serverID = AddEmailServer("Result-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Result-Report", serverID));

            DailyReportRepository.SetRunResult(ConnectionString, id, "SMTP down");
            Assert.AreEqual("SMTP down", DailyReportRepository.GetReports(ConnectionString, id).Single().LastRunError);

            DailyReportRepository.SetRunResult(ConnectionString, id, null);
            Assert.IsNull(DailyReportRepository.GetReports(ConnectionString, id).Single().LastRunError);
        }

        [TestMethod]
        public void Delete_RemovesReportAndIssueState()
        {
            var serverID = AddEmailServer("Del-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("Del-Report", serverID));
            Exec($"INSERT INTO dbo.DailyReportIssueState(DailyReportID,IssueKey,Description,FirstSeenUTC,LastSeenUTC) VALUES({id},N'K',N'D',SYSUTCDATETIME(),SYSUTCDATETIME())");

            DailyReportRepository.Delete(ConnectionString, id);

            Assert.AreEqual(0, DailyReportRepository.GetReports(ConnectionString, id).Count);
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM dbo.DailyReportIssueState WHERE DailyReportID = {id}"));
        }

        [TestMethod]
        public void DeletingEmailServer_RefusedWhileAReportUsesIt()
        {
            var serverID = AddEmailServer("SrvDel-Ops");
            var id = DailyReportRepository.Save(ConnectionString, NewReport("SrvDel-Report", serverID));

            var ex = Assert.ThrowsExactly<SqlException>(() => Exec($"EXEC Alert.EmailServer_Del @EmailServerID = {serverID}"));
            StringAssert.Contains(ex.Message, "daily reports");
            Assert.AreEqual(1, Scalar($"SELECT COUNT(*) FROM Alert.EmailServer WHERE EmailServerID = {serverID}"));
            Assert.AreEqual(1, Scalar($"SELECT ReportCount FROM (SELECT COUNT(*) AS ReportCount FROM dbo.DailyReport WHERE EmailServerID = {serverID}) X"));

            DailyReportRepository.Delete(ConnectionString, id);
            Exec($"EXEC Alert.EmailServer_Del @EmailServerID = {serverID}");
            Assert.AreEqual(0, Scalar($"SELECT COUNT(*) FROM Alert.EmailServer WHERE EmailServerID = {serverID}"), "Deleted once nothing uses it");
        }

        [TestMethod]
        public void InstanceScope_FollowsTags()
        {
            Exec(@"
DECLARE @Prod INT, @Dev INT
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Scope-Role',N'Prod') SET @Prod = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Scope-Role',N'Dev') SET @Dev = SCOPE_IDENTITY()
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'ScopeProd',N'ScopeProd',1,1,3,0,GETUTCDATE()),(N'ScopeDev',N'ScopeDev',1,1,3,0,GETUTCDATE()),(N'ScopeHidden',N'ScopeHidden',1,0,3,0,GETUTCDATE())
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID)
SELECT I.InstanceID, @Prod FROM dbo.Instances I WHERE I.Instance IN (N'ScopeProd',N'ScopeHidden')
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID)
SELECT I.InstanceID, @Dev FROM dbo.Instances I WHERE I.Instance = N'ScopeDev'");

            var prodTag = Scalar("SELECT TagID FROM dbo.Tags WHERE TagName = N'Scope-Role' AND TagValue = N'Prod'");
            var prodOnly = DailyReportRepository.GetInstanceIDs(ConnectionString, new DailyReportConfig { ApplyToTagID = prodTag });
            Assert.AreEqual(1, prodOnly.Count, "Hidden instances are left out of the report, as on the summary page");
            Assert.AreEqual(Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'ScopeProd'"), prodOnly.Single());

            var all = DailyReportRepository.GetInstanceIDs(ConnectionString, new DailyReportConfig { ApplyToTagID = -1 });
            Assert.IsTrue(all.Count >= 2);
            Assert.IsFalse(all.Contains(Scalar("SELECT InstanceID FROM dbo.Instances WHERE Instance = N'ScopeHidden'")));
        }
    }
}
