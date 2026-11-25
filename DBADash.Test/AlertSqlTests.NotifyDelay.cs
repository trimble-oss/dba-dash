using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

namespace DBADash.Test
{
    /*
     * A rule's Notify After delay holds notifications until the issue has persisted for that long, and its Resolve After
     * delay (defaulting to Notify After) keeps the alert open while the issue has only briefly cleared.  Without the
     * resolve delay an issue that keeps coming and going would restart the notify delay every time and never notify.
     * Time is simulated by moving the alert's dates back rather than waiting.
     */
    public partial class AlertSqlTests
    {
        [TestMethod]
        public void NoDelayNotifiesAndResolvesStraightAway()
        {
            RunDelayScenario(null, null, s =>
            {
                s.Fail();
                Assert.IsTrue(s.IsNotifying(), "Notifies as soon as the issue is seen");
                s.Notified();
                /* Notifications_Get needs UpdatedDate > LastNotification.  Without a wait they can share a SYSUTCDATETIME() tick (~15ms on Windows). */
                s.Wait(1);
                s.Clear();
                Assert.IsTrue(s.IsResolved, "Resolves as soon as the issue clears");
                Assert.IsTrue(s.IsNotifying(), "Notifies the resolution");
            });
        }

        [TestMethod]
        public void NotifyDelayHoldsUntilIssuePersists()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                Assert.IsTrue(s.IsPending, "Pending while waiting for the notify delay");
                Assert.IsFalse(s.IsNotifying());
                s.Wait(1);
                s.Fail();
                Assert.IsFalse(s.IsNotifying(), "Issue has only persisted for 1 of 2 mins");
                s.Wait(1);
                s.Fail();
                Assert.IsFalse(s.IsPending);
                Assert.IsTrue(s.IsNotifying(), "Issue has persisted for the notify delay");
            });
        }

        [TestMethod]
        public void BriefIssueResolvesWithoutNotification()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(1);
                s.Clear();
                Assert.IsTrue(s.IsResolving, "Resolving while waiting for the resolve delay");
                Assert.IsFalse(s.IsResolved);
                Assert.IsFalse(s.IsNotifying(), "Nothing to say while resolving");
                s.Wait(2);
                s.Clear();
                Assert.IsTrue(s.IsResolved, "Resolve delay defaults to the notify delay");
                Assert.AreEqual(1, s.ResolvedCount);
                Assert.IsFalse(s.IsNotifying(), "Nobody was told about the issue, so there's no resolution to send");
            });
        }

        [TestMethod]
        public void IssueThatKeepsClearingBrieflyStillNotifies()
        {
            RunDelayScenario(2, null, s =>
            {
                /* Fails on every other check - never 2 mins in a row */
                s.Fail();
                s.Wait(1);
                s.Clear();
                s.Wait(1);
                s.Fail();
                Assert.IsFalse(s.IsResolved);
                Assert.AreEqual(0, s.ResolvedCount, "Brief recoveries don't resolve the alert");
                Assert.IsTrue(s.IsNotifying(), "Issue started 2 mins ago and hasn't cleared for the resolve delay since");
            });
        }

        [TestMethod]
        public void ExplicitResolveDelayOverridesNotifyDelay()
        {
            RunDelayScenario(2, 0, s =>
            {
                s.Fail();
                s.Wait(1);
                s.Clear();
                Assert.IsTrue(s.IsResolved, "Resolve After = 0 resolves straight away");
                s.Wait(1);
                s.Fail();
                Assert.IsTrue(s.IsPending, "A new episode starts the notify delay again");
                Assert.IsFalse(s.IsNotifying());
            });
        }

        [TestMethod]
        public void ResolutionIsNotifiedAfterResolveDelay()
        {
            RunDelayScenario(1, 3, s =>
            {
                s.Fail();
                s.Wait(1);
                s.Fail();
                Assert.IsTrue(s.IsNotifying());
                s.Notified();
                s.Wait(1);
                s.Clear();
                Assert.IsTrue(s.IsResolving);
                Assert.IsFalse(s.IsNotifying(), "Not resolved yet");
                s.Wait(2);
                s.Clear();
                Assert.IsFalse(s.IsResolved, "Cleared for 2 of 3 mins");
                s.Wait(1);
                s.Clear();
                Assert.IsTrue(s.IsResolved);
                Assert.IsTrue(s.IsNotifying(), "Resolution of a notified alert is sent");
            });
        }

        [TestMethod]
        public void ResolvedAlertTriggeredAgainStartsNewEpisode()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(2);
                s.Fail();
                s.Notified();
                s.Clear();
                s.Wait(2);
                s.Clear();
                Assert.IsTrue(s.IsResolved);
                s.Notified();
                s.Wait(10);
                s.Fail();
                Assert.IsTrue(s.IsPending, "The notify delay applies to the new episode");
                Assert.IsFalse(s.IsNotifying());
                s.Wait(1);
                s.Clear();
                s.Wait(2);
                s.Clear();
                Assert.IsTrue(s.IsResolved);
                Assert.IsFalse(s.IsNotifying(), "This episode wasn't notified, so neither is its resolution");
            });
        }

        [TestMethod]
        public void ActiveAlertsGetShowsPendingAndResolving()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                Assert.AreEqual("10 - High (Pending)", s.PriorityDescription());
                s.Wait(2);
                s.Fail();
                Assert.AreEqual("10 - High", s.PriorityDescription());
                s.Clear();
                Assert.AreEqual("10 - High (Resolving)", s.PriorityDescription());
            });
        }

        /*
         * Alert procs that work out for themselves when their alerts are resolved use the same resolve delay.  Without it, a job that fails
         * on and off would resolve when it next succeeds and never get past the notify delay.
         */

        [TestMethod]
        public void JobThatFailsOnAndOffStillNotifies()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsPending);
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET LastSucceeded = DATEADD(s,1,LastFailed) WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Job succeeded - waiting for the resolve delay");
                Assert.IsFalse(s.IsNotifying());
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET LastFailed = DATEADD(s,1,LastSucceeded) WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsFalse(s.IsResolving);
                Assert.AreEqual(0, s.ResolvedCount);
                Assert.IsTrue(s.IsNotifying(), "First failure was 2 mins ago and the job hasn't stayed fixed since");
            }, "AgentJob", "AgentJob");
        }

        [TestMethod]
        public void JobFixedWithinNotifyDelayResolvesWithoutNotification()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET LastSucceeded = DATEADD(s,1,LastFailed) WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                s.Wait(2);
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsFalse(s.IsNotifying(), "Nobody was told the job failed, so there's no resolution to send");
            }, "AgentJob", "AgentJob");
        }

        [TestMethod]
        public void DatabaseStatusThatKeepsChangingStillNotifies()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec(@"INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state,is_in_standby,recovery_model)
VALUES(@InstanceID,N'DB1',5,GETDATE(),150,0,0,0,0,0,0,0,0,0,1,4,0,1) /* SUSPECT */
EXEC Alert.DatabaseStatusAlert_Upd");
                Assert.IsTrue(s.IsPending);
                s.Wait(1);
                s.Exec("UPDATE dbo.Databases SET state = 0 WHERE InstanceID = @InstanceID EXEC Alert.DatabaseStatusAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Database is online - waiting for the resolve delay");
                s.Wait(1);
                s.Exec("UPDATE dbo.Databases SET state = 4 WHERE InstanceID = @InstanceID EXEC Alert.DatabaseStatusAlert_Upd");
                Assert.AreEqual(0, s.ResolvedCount);
                Assert.IsTrue(s.IsNotifying());
                s.Notified();
                s.Wait(1);
                s.Exec("UPDATE dbo.Databases SET state = 0 WHERE InstanceID = @InstanceID EXEC Alert.DatabaseStatusAlert_Upd");
                s.Wait(2);
                s.Exec("EXEC Alert.DatabaseStatusAlert_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsTrue(s.IsNotifying(), "Resolution of a notified alert is sent");
            }, "DatabaseStatus", "DatabaseStatus");
        }

        [TestMethod]
        public void RenamedJobCountsAsFixed()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET LastSucceeded = DATEADD(s,1,LastFailed) WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving);
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET name = N'Renamed job' WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Nothing of that name is failing - still waiting for the resolve delay");
                Assert.IsFalse(s.IsNotifying(), "The job was fixed before it was renamed");
                s.Wait(1);
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsFalse(s.IsNotifying());
            }, "AgentJob", "AgentJob");
        }

        [TestMethod]
        public void ElasticJobThatFailsOnAndOffStillNotifies()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.ElasticJobs(InstanceID,job_id,job_name,enabled,IsActive,SnapshotCreatedDate,SnapshotUpdatedDate,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Purge',1,1,SYSUTCDATETIME(),SYSUTCDATETIME(),DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.ElasticJobAlert_Upd");
                Assert.IsTrue(s.IsPending);
                s.Wait(1);
                s.Exec("UPDATE dbo.ElasticJobs SET LastSucceeded = DATEADD(s,1,LastFailed) WHERE InstanceID = @InstanceID EXEC Alert.ElasticJobAlert_Upd");
                Assert.IsTrue(s.IsResolving);
                Assert.IsFalse(s.IsNotifying());
                s.Wait(1);
                s.Exec("UPDATE dbo.ElasticJobs SET LastFailed = DATEADD(s,1,LastSucceeded) WHERE InstanceID = @InstanceID EXEC Alert.ElasticJobAlert_Upd");
                Assert.IsFalse(s.IsResolving);
                Assert.AreEqual(0, s.ResolvedCount);
                Assert.IsTrue(s.IsNotifying());
            }, "ElasticJob", "ElasticJob");
        }

        /// <summary>A custom SQL proc that fails while its alert is resolving leaves the alert waiting - neither active again nor resolved</summary>
        [TestMethod]
        public void CustomSqlProcFailureLeavesResolvingAlertWaiting()
        {
            RunDelayScenario(2, null, s =>
            {
                /* Not CREATE OR ALTER - CI tests on SQL 2016 RTM */
                const string drop = "IF OBJECT_ID('UserAlert.DelayTest') IS NOT NULL DROP PROC UserAlert.DelayTest ";
                const string reporting = drop + "EXEC('CREATE PROC UserAlert.DelayTest AS SELECT InstanceID, N''K'', N''Issue'' FROM dbo.Instances WHERE Instance = N''SQL1''')";
                const string clear = drop + "EXEC('CREATE PROC UserAlert.DelayTest AS SELECT InstanceID, N''K'', N''Issue'' FROM dbo.Instances WHERE 1=0')";
                const string failing = drop + "EXEC('CREATE PROC UserAlert.DelayTest AS RAISERROR(''Test failure'',16,1)')";
                s.Exec(reporting + " EXEC Alert.CustomSqlAlert_Upd");
                s.Wait(2);
                s.Exec("EXEC Alert.CustomSqlAlert_Upd");
                Assert.IsTrue(s.IsNotifying());
                s.Notified();
                s.Exec(clear + " EXEC Alert.CustomSqlAlert_Upd");
                Assert.IsTrue(s.IsResolving);
                s.Wait(1);
                s.Exec(failing + " EXEC Alert.CustomSqlAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Not carried forward as active");
                Assert.IsFalse(s.IsNotifying());
                s.Wait(2);
                s.Exec("EXEC Alert.CustomSqlAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Not resolved while the proc can't say whether the issue is still clear");
                s.Wait(1500);
                s.Exec("EXEC Alert.Alerts_Upd");
                Assert.IsTrue(s.IsResolving, "Not closed as stale while the proc is failing");
                s.Exec(clear + " EXEC Alert.CustomSqlAlert_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsTrue(s.IsNotifying(), "Resolution of a notified alert is sent");
            }, "CustomSql", "Custom", @"{""ProcName"":""DelayTest""}");
        }

        /// <summary>Desktop notifications: an alert whose notify delay passed while it was resolving is new when the issue comes back</summary>
        [TestMethod]
        public void NewAlertsGetIncludesAlertThatCameBackAfterNotifyDelay()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(1);
                s.Clear();
                s.Wait(2);
                Assert.IsFalse(s.IsNewAlert(), "Resolving");
                s.Fail();
                Assert.IsTrue(s.IsNotifying());
                Assert.IsTrue(s.IsNewAlert(), "Came back after the notify delay passed");
            });
        }

        /// <summary>The bell and daily report leave out alerts held back by their rule's delays</summary>
        [TestMethod]
        public void AlertCountLeavesOutPendingAlerts()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                Assert.AreEqual(0, s.ForAttention());
                s.Wait(2);
                s.Fail();
                Assert.AreEqual(1, s.ForAttention());
                s.Clear();
                Assert.AreEqual(0, s.ForAttention(), "Resolving");
            });
        }

        [TestMethod]
        public void BlackoutWhileAlertIsPending()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(1);
                s.Exec("UPDATE dbo.Instances SET CollectionsPausedUntil = DATEADD(hh,1,SYSUTCDATETIME()) WHERE InstanceID = @InstanceID EXEC Alert.Alerts_Upd");
                Assert.IsTrue((bool)s.Value("IsBlackout"));
                s.Wait(3);
                s.Exec("EXEC Alert.Alerts_Upd");
                Assert.IsFalse(s.IsNotifying(), "Blackout");
                /* Blackout ends and the issue has cleared (no AG data) */
                s.Exec("UPDATE dbo.Instances SET CollectionsPausedUntil = NULL WHERE InstanceID = @InstanceID EXEC Alert.Alerts_Upd");
                Assert.IsFalse((bool)s.Value("IsBlackout"));
                Assert.IsTrue(s.IsResolving);
                Assert.IsFalse(s.IsNotifying());
                s.Wait(2);
                s.Exec("EXEC Alert.Alerts_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsFalse(s.IsNotifying(), "Never notified, so neither is the resolution");
            });
        }

        /// <summary>Alert.Alerts_Upd closes alerts that haven't been updated for a day.  A resolving alert is updated each time it's confirmed clear.</summary>
        [TestMethod]
        public void LongResolveDelayIsNotClosedAsStale()
        {
            RunDelayScenario(null, 3000, s =>
            {
                s.Fail();
                s.Clear();
                Assert.IsTrue(s.IsResolving);
                s.Wait(1500);
                s.Exec("EXEC Alert.Alerts_Upd"); /* No AG data - still clear */
                Assert.IsTrue(s.IsResolving, "Resolving for 1500 of 3000 mins");
            });
        }

        /// <summary>A disabled job isn't in the snapshot of recently failed jobs, so it mustn't be reported as a new failure every run</summary>
        [TestMethod]
        public void DisabledJobResolvesAfterResolveDelay()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET enabled = 0 WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving);
                s.Wait(1);
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving, "Not reported again once it's dropped out of the snapshot");
                s.Wait(1);
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolved);
                Assert.IsFalse(s.IsNotifying());
            }, "AgentJob", "AgentJob");
        }

        /// <summary>A job that's still failing but isn't reported again (the failure isn't new) mustn't stay resolving</summary>
        [TestMethod]
        public void JobRenamedAwayAndBackIsActiveAgain()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET name = N'Other name' WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsTrue(s.IsResolving);
                s.Wait(1);
                s.Exec("UPDATE dbo.Jobs SET name = N'Flaky job' WHERE InstanceID = @InstanceID EXEC Alert.AgentJobAlert_Upd");
                Assert.IsFalse(s.IsResolving, "The job is still failing");
                Assert.AreEqual(0, s.ResolvedCount);
                Assert.IsTrue(s.IsNotifying());
            }, "AgentJob", "AgentJob");
        }

        /// <summary>Desktop notifications: an episode that's already been shown isn't new again each time the issue comes back</summary>
        [TestMethod]
        public void NewAlertsGetDoesNotRepeatAnEpisode()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(2);
                s.Fail();
                Assert.IsTrue(s.IsNewAlert());
                s.Wait(1);
                s.Clear();
                s.Fail();
                Assert.IsFalse(s.IsResolving);
                Assert.IsFalse(s.IsNewAlert(), "Already shown for this episode");
            });
        }

        /// <summary>Rules that differ only by their delays aren't duplicates - e.g. when a notification channel group is deleted and its rules move to the default group</summary>
        [TestMethod]
        public void RulesDifferingByDelayAreNotDuplicates()
        {
            RunAndRollback(@"
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,IsActive) VALUES('AGHealth','AG HEALTH',10,-1,1)
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,IsActive,NotifyAfterMins) VALUES('AGHealth','AG HEALTH',10,-1,1,5)
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,IsActive,NotifyAfterMins,ResolveAfterMins) VALUES('AGHealth','AG HEALTH',10,-1,1,5,0)
SELECT COUNT(DISTINCT RuleHash) FROM Alert.Rules WHERE Type='AGHealth'", cmd => Assert.AreEqual(3, (int)cmd.ExecuteScalar()));
        }

        /// <summary>A job that keeps failing isn't reported again (the failure isn't new), but its pending alert mustn't be closed as stale before the notify delay passes</summary>
        [TestMethod]
        public void PendingJobAlertIsNotClosedAsStale()
        {
            RunDelayScenario(3000, null, s =>
            {
                s.Exec("INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed) VALUES(@InstanceID,NEWID(),N'Flaky job',1,N'Test',1,DATEADD(d,-1,SYSUTCDATETIME()),SYSUTCDATETIME())");
                s.Exec("EXEC Alert.AgentJobAlert_Upd");
                s.Wait(1500);
                s.Exec("EXEC Alert.Alerts_Upd");
                Assert.IsTrue(s.IsPending, "Pending for 1500 of 3000 mins");
            }, "AgentJob", "AgentJob");
        }

        /// <summary>Desktop notifications: an alert escalated while pending has no notification date until an alert run marks it due - it mustn't be returned without one</summary>
        [TestMethod]
        public void NewAlertsGetWaitsForNotifyReadyDate()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Exec("UPDATE Alert.ActiveAlerts SET Escalated = SYSUTCDATETIME() WHERE InstanceID = @InstanceID");
                s.Wait(2); /* Past the notify delay, but no alert run since */
                s.Exec("UPDATE Alert.ActiveAlerts SET Escalated = SYSUTCDATETIME() WHERE InstanceID = @InstanceID");
                Assert.IsFalse(s.IsNewAlert(), "Not due until an alert run marks it");
                s.Fail();
                Assert.IsTrue(s.IsNewAlert());
            });
        }

        /// <summary>Renumbering alerts that clash with closed alert IDs keeps their episode dates</summary>
        [TestMethod]
        public void FixIdentitySeedKeepsEpisodeDates()
        {
            RunDelayScenario(2, null, s =>
            {
                s.Fail();
                s.Wait(2);
                s.Fail();
                s.Clear();
                Assert.IsTrue(s.IsResolving);
                s.Exec(@"INSERT INTO Alert.ClosedAlerts(AlertID,InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,UpdatedDate,FirstNotification,LastNotification,
	UpdateCount,ResolvedCount,NotificationCount,FailedNotificationCount,IsAcknowledged,IsResolved,ResolvedDate,Notes,RuleID,GroupID,AcknowledgedDate)
SELECT AlertID,InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,TriggerDate,UpdatedDate,FirstNotification,LastNotification,
	UpdateCount,ResolvedCount,NotificationCount,FailedNotificationCount,IsAcknowledged,IsResolved,ResolvedDate,Notes,RuleID,GroupID,AcknowledgedDate
FROM Alert.ActiveAlerts
WHERE InstanceID = @InstanceID
EXEC Alert.ActiveAlerts_FixIdentitySeed");
                Assert.AreEqual(1, (int)s.Value("COUNT(*) OVER()"), "Renumbered, not lost");
                Assert.IsTrue((bool)s.Value("CAST(CASE WHEN AlertID NOT IN(SELECT AlertID FROM Alert.ClosedAlerts) THEN 1 ELSE 0 END AS BIT)"), "Renumbered");
                Assert.IsTrue(s.IsResolving, "ResolvingSince kept");
                Assert.IsTrue((bool)s.Value("CAST(CASE WHEN ActiveSince IS NOT NULL AND NotifyReadyDate IS NOT NULL THEN 1 ELSE 0 END AS BIT)"), "ActiveSince & NotifyReadyDate kept");
            });
        }

        private static void RunDelayScenario(int? notifyAfterMins, int? resolveAfterMins, Action<DelayScenario> steps, string alertType = "AGHealth", string ruleAlertKey = "AG HEALTH", string? details = null)
        {
            using var cn = new SqlConnection(ConnectionString);
            cn.Open();
            using var tran = cn.BeginTransaction();
            try
            {
                steps(new DelayScenario(cn, tran, notifyAfterMins, resolveAfterMins, alertType, ruleAlertKey, details));
            }
            finally
            {
                tran.Rollback();
            }
        }

        private sealed class DelayScenario
        {
            private readonly string AlertType;
            private readonly string AlertKey;
            private readonly SqlConnection cn;
            private readonly SqlTransaction tran;
            private readonly int instanceID;
            private readonly int ruleID;
            private readonly int channelID;

            public DelayScenario(SqlConnection cn, SqlTransaction tran, int? notifyAfterMins, int? resolveAfterMins, string alertType, string ruleAlertKey, string? details)
            {
                this.cn = cn;
                this.tran = tran;
                AlertType = alertType;
                AlertKey = ruleAlertKey;
                using var rdr = Command(@"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset) VALUES(N'SQL1',N'SQL1',1,1,3,0)
DECLARE @InstanceID INT = SCOPE_IDENTITY()
INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,IsActive,NotifyAfterMins,ResolveAfterMins,Details)
VALUES(@AlertType,@AlertKey,10,-1,1,@NotifyAfterMins,@ResolveAfterMins,@Details)
DECLARE @RuleID INT = SCOPE_IDENTITY()
INSERT INTO Alert.NotificationChannel(NotificationChannelTypeID,ChannelName,ChannelDetails)
SELECT MIN(NotificationChannelTypeID),N'Delay test',N'{}'
FROM Alert.NotificationChannelType
DECLARE @ChannelID INT = SCOPE_IDENTITY()
INSERT INTO Alert.NotificationChannelSchedule(NotificationChannelID,AlertNotificationLevel,ApplyToTagID)
VALUES(@ChannelID,41,-1)
SELECT @InstanceID,@RuleID,@ChannelID",
                    ("@AlertType", AlertType), ("@AlertKey", AlertKey),
                    ("@NotifyAfterMins", (object?)notifyAfterMins ?? DBNull.Value),
                    ("@ResolveAfterMins", (object?)resolveAfterMins ?? DBNull.Value),
                    ("@Details", (object?)details ?? DBNull.Value)).ExecuteReader();
                Assert.IsTrue(rdr.Read());
                instanceID = rdr.GetInt32(0);
                ruleID = rdr.GetInt32(1);
                channelID = rdr.GetInt32(2);
            }

            /// <summary>Run SQL with @InstanceID set - e.g. to change the monitored data and run an alert proc</summary>
            public void Exec(string sql) => Command(sql, ("@InstanceID", instanceID)).ExecuteNonQuery();

            /// <summary>Alert run where the issue is present</summary>
            public void Fail() => Upd(true);

            /// <summary>Alert run where the issue has cleared</summary>
            public void Clear() => Upd(false);

            private void Upd(bool isFailing) => Command(@"
DECLARE @AD Alert.AlertDetails
INSERT INTO @AD(InstanceID,Priority,AlertKey,Message,RuleID,GroupID)
SELECT @InstanceID,10,@AlertKey,N'Not healthy',@RuleID,0
WHERE @IsFailing=1
EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AD,@AlertType=@AlertType",
                ("@InstanceID", instanceID), ("@AlertKey", AlertKey), ("@RuleID", ruleID), ("@IsFailing", isFailing), ("@AlertType", AlertType)).ExecuteNonQuery();

            /// <summary>Move the alert's dates back - as if this many minutes have passed</summary>
            public void Wait(int mins) => Command(@"
UPDATE Alert.ActiveAlerts
	SET TriggerDate = DATEADD(mi,-@Mins,TriggerDate),
	UpdatedDate = DATEADD(mi,-@Mins,UpdatedDate),
	ActiveSince = DATEADD(mi,-@Mins,ActiveSince),
	ResolvingSince = DATEADD(mi,-@Mins,ResolvingSince),
	ResolvedDate = DATEADD(mi,-@Mins,ResolvedDate),
	FirstNotification = DATEADD(mi,-@Mins,FirstNotification),
	LastNotification = DATEADD(mi,-@Mins,LastNotification),
	NotifyReadyDate = DATEADD(mi,-@Mins,NotifyReadyDate),
	Escalated = DATEADD(mi,-@Mins,Escalated),
	DeEscalated = DATEADD(mi,-@Mins,DeEscalated)
WHERE InstanceID = @InstanceID", ("@Mins", mins), ("@InstanceID", instanceID)).ExecuteNonQuery();

            /// <summary>Is the alert in the list of notifications to send?</summary>
            public bool IsNotifying()
            {
                var alertID = AlertID;
                using var cmd = Command("Alert.Notifications_Get");
                cmd.CommandType = CommandType.StoredProcedure;
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (rdr.GetInt64(rdr.GetOrdinal("AlertID")) == alertID && rdr.GetInt32(rdr.GetOrdinal("NotificationChannelID")) == channelID) return true;
                }
                return false;
            }

            /// <summary>Is the alert in the GUI's desktop notifications, for a poll 30 seconds ago?</summary>
            public bool IsNewAlert()
            {
                using var cmd = Command("Alert.NewAlerts_Get", ("@FromDate", DateTime.UtcNow.AddSeconds(-30)));
                cmd.CommandType = CommandType.StoredProcedure;
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (rdr.GetString(rdr.GetOrdinal("AlertType")) == AlertType) return true;
                }
                return false;
            }

            /// <summary>Alerts needing attention, as the GUI's bell shows</summary>
            public int ForAttention()
            {
                using var cmd = Command("Alert.AlertCount_Get");
                cmd.CommandType = CommandType.StoredProcedure;
                using var rdr = cmd.ExecuteReader();
                Assert.IsTrue(rdr.Read());
                return rdr.GetInt32(rdr.GetOrdinal("ForAttention"));
            }

            /// <summary>Record that the notification was sent, as the service does</summary>
            public void Notified() => Command("EXEC Alert.AlertNotification_Upd @AlertID=@AlertID,@NotificationChannelID=@ChannelID,@NotificationMessage=N'Test'",
                ("@AlertID", AlertID), ("@ChannelID", channelID)).ExecuteNonQuery();

            public string PriorityDescription()
            {
                using var cmd = Command("Alert.ActiveAlerts_Get", ("@InstanceID", instanceID));
                cmd.CommandType = CommandType.StoredProcedure;
                using var rdr = cmd.ExecuteReader();
                Assert.IsTrue(rdr.Read());
                return rdr.GetString(rdr.GetOrdinal("PriorityDescription"));
            }

            private long AlertID => (long)Scalar("AlertID");
            public bool IsResolved => (bool)Scalar("IsResolved");
            public int ResolvedCount => (int)Scalar("ResolvedCount");
            public bool IsResolving => (bool)Scalar("CAST(CASE WHEN IsResolved=0 AND ResolvingSince IS NOT NULL THEN 1 ELSE 0 END AS BIT)");

            public bool IsPending
            {
                get
                {
                    using var cmd = Command("Alert.ActiveAlerts_Get", ("@InstanceID", instanceID));
                    cmd.CommandType = CommandType.StoredProcedure;
                    using var rdr = cmd.ExecuteReader();
                    Assert.IsTrue(rdr.Read());
                    return rdr.GetBoolean(rdr.GetOrdinal("IsPending"));
                }
            }

            public object Value(string column) => Scalar(column);

            private object Scalar(string expression) =>
                Command($"SELECT {expression} FROM Alert.ActiveAlerts WHERE InstanceID = @InstanceID AND AlertType = @AlertType",
                    ("@InstanceID", instanceID), ("@AlertType", AlertType)).ExecuteScalar() ?? throw new AssertFailedException("No active alert");

            private SqlCommand Command(string sql, params (string name, object value)[] parameters)
            {
                var cmd = new SqlCommand(sql, cn, tran) { CommandTimeout = 120 };
                foreach (var (name, value) in parameters)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }
                return cmd;
            }
        }
    }
}
