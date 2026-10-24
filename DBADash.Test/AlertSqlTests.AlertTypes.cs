using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    /*
     * A test per alert type: data for an instance that should alert ('Alert') and one that shouldn't ('OK' plus any
     * type specific near misses), then the instances left with an active alert.  DriveSpace, Backup and the Azure space
     * alerts are covered by the RuleID tests.
     */
    public partial class AlertSqlTests
    {
        /// <summary>Two instances - @Alert and @OK - with the defaults collection would give a healthy instance</summary>
        private const string TwoInstances = @"
DECLARE @Alert INT, @OK INT
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,scheduler_count,IsAgentRunning,sqlserver_start_time,DBMailStatus)
VALUES(N'Alert',N'Alert',1,1,3,0,4,1,DATEADD(d,-2,GETUTCDATE()),N'STARTED')
SET @Alert = SCOPE_IDENTITY()
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,scheduler_count,IsAgentRunning,sqlserver_start_time,DBMailStatus)
VALUES(N'OK',N'OK',1,1,3,0,4,1,DATEADD(d,-2,GETUTCDATE()),N'STARTED')
SET @OK = SCOPE_IDENTITY()
";

        /// <summary>Instances with an active (unresolved) alert of @Type</summary>
        private const string ActiveAlerts = @"
SELECT I.ConnectionID, AA.AlertKey, AA.LastMessage, AA.GroupID
FROM Alert.ActiveAlerts AA
JOIN dbo.Instances I ON I.InstanceID = AA.InstanceID
WHERE AA.AlertType = @Type
AND AA.IsResolved = 0
ORDER BY I.ConnectionID, AA.AlertKey";

        private sealed record ActiveAlert(string ConnectionID, string AlertKey, string Message, int GroupID);

        private static List<ActiveAlert> GetActiveAlerts(string sql)
        {
            var alerts = new List<ActiveAlert>();
            RunAndRollback(sql + ActiveAlerts, cmd =>
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    alerts.Add(new ActiveAlert((string)rdr["ConnectionID"], (string)rdr["AlertKey"], (string)rdr["LastMessage"], (int)rdr["GroupID"]));
                }
            });
            return alerts;
        }

        private static List<ActiveAlert> AssertAlerted(string sql, params string[] expectedConnectionIDs)
        {
            var alerts = GetActiveAlerts(sql);
            CollectionAssert.AreEquivalent(expectedConnectionIDs, alerts.Select(a => a.ConnectionID).ToList(),
                "Active alerts:\n" + string.Join("\n", alerts.Select(a => $"{a.ConnectionID} {a.AlertKey}: {a.Message}")));
            return alerts;
        }

        private static string Rule(string type, string alertKey, decimal? threshold = null, int? evaluationPeriodMins = null, string? details = null, int groupID = 0) =>
            $"INSERT INTO Alert.Rules(Type,AlertKey,Priority,ApplyToTagID,Threshold,EvaluationPeriodMins,IsActive,Details,GroupID) VALUES('{type}',N'{alertKey}',20,-1,{(threshold?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NULL")},{(evaluationPeriodMins?.ToString() ?? "NULL")},1,{(details == null ? "NULL" : "N'" + details.Replace("'", "''") + "'")},{groupID})\n";

        [TestMethod]
        public void OfflineAlertWaitsForEvaluationPeriod()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='OFFLINE', @Recent INT
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary) VALUES(N'Recent',N'Recent',1,1)
SET @Recent = SCOPE_IDENTITY()
INSERT INTO dbo.OfflineInstances(InstanceID,FirstFail,LastFail,IsCurrent,FailCount)
VALUES(@Alert,DATEADD(mi,-30,SYSUTCDATETIME()),SYSUTCDATETIME(),1,30),
	(@OK,DATEADD(mi,-90,SYSUTCDATETIME()),DATEADD(mi,-60,SYSUTCDATETIME()),0,30), /* Back online */
	(@Recent,DATEADD(mi,-2,SYSUTCDATETIME()),SYSUTCDATETIME(),1,2) /* Not offline for long enough yet */
" + Rule("OFFLINE", "Offline", evaluationPeriodMins: 10) + @"
EXEC Alert.OfflineAlert_Upd", "Alert");
        }

        [TestMethod]
        public void RestartAlertForRecentRestart()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='RESTART'
UPDATE dbo.Instances SET sqlserver_start_time = DATEADD(mi,-5,GETUTCDATE()) WHERE InstanceID = @Alert
" + Rule("RESTART", "Restart") + @"
EXEC Alert.RestartAlert_Upd", "Alert");
        }

        [TestMethod]
        public void CPUAlertAboveThreshold()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='CPU'
/* Alert at 95% total CPU, OK at 20% */
INSERT INTO dbo.CPU(InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU)
SELECT I.InstanceID, DATEADD(mi,-M.Mins,SYSUTCDATETIME()), 50, CASE WHEN I.InstanceID = @Alert THEN 5 ELSE 80 END
FROM (VALUES(@Alert),(@OK)) I(InstanceID)
CROSS JOIN (VALUES(1),(2),(3)) M(Mins)
" + Rule("CPU", "CPU", threshold: 90, evaluationPeriodMins: 5) + @"
EXEC Alert.CPUAlert_Upd", "Alert");
        }

        [TestMethod]
        public void AgentNotRunningAlertExcludesExpress()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='AgentNotRunning'
UPDATE dbo.Instances SET IsAgentRunning = 0 WHERE InstanceID = @Alert
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,IsAgentRunning) VALUES(N'Express',N'Express',1,1,4,0) /* No agent on Express */
" + Rule("AgentNotRunning", "AgentNotRunning") + @"
EXEC Alert.AgentNotRunningAlert_Upd", "Alert");
        }

        private const string FailedJobs = @"
DECLARE @Type VARCHAR(50)='AgentJob'
INSERT INTO dbo.Jobs(InstanceID,job_id,name,enabled,category,IsActive,LastSucceeded,LastFailed)
VALUES(@Alert,NEWID(),N'Backup job',1,N'Database Maintenance',1,DATEADD(d,-1,SYSUTCDATETIME()),DATEADD(mi,-10,SYSUTCDATETIME())),
	(@Alert,NEWID(),N'Noisy job',1,N'Database Maintenance',1,NULL,DATEADD(mi,-10,SYSUTCDATETIME())), /* Excluded by the rule */
	(@Alert,NEWID(),N'Old failure',1,N'Database Maintenance',1,NULL,DATEADD(mi,-90,SYSUTCDATETIME())), /* Failed more than an hour ago */
	(@OK,NEWID(),N'Backup job',1,N'Database Maintenance',1,DATEADD(mi,-5,SYSUTCDATETIME()),DATEADD(mi,-10,SYSUTCDATETIME())) /* Succeeded since */
";

        [TestMethod]
        public void AgentJobAlertForFailedJobs()
        {
            var alerts = AssertAlerted(TwoInstances + FailedJobs
                + Rule("AgentJob", "AgentJob", details: @"{""ExcludeJobName"":""Noisy%""}") + @"
EXEC Alert.AgentJobAlert_Upd", "Alert");
            Assert.AreEqual("Job:Backup job", alerts[0].AlertKey);
        }

        [TestMethod]
        public void AgentJobAlertResolvesWhenJobSucceeds()
        {
            AssertAlerted(TwoInstances + FailedJobs
                + Rule("AgentJob", "AgentJob", details: @"{""ExcludeJobName"":""Noisy%""}") + @"
EXEC Alert.AgentJobAlert_Upd
UPDATE dbo.Jobs SET LastSucceeded = SYSUTCDATETIME() WHERE InstanceID = @Alert AND name = N'Backup job'
EXEC Alert.AgentJobAlert_Upd");
        }

        [TestMethod]
        public void FailedLoginsAlertAtThreshold()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='FailedLogins'
INSERT INTO dbo.FailedLogins(InstanceID,LogDate,Uniqueifier)
SELECT @Alert, DATEADD(mi,-N.N,SYSUTCDATETIME()), 0 FROM (VALUES(1),(2),(3),(4),(5)) N(N)
UNION ALL
SELECT @OK, DATEADD(mi,-N.N,SYSUTCDATETIME()), 0 FROM (VALUES(1),(2),(3),(4)) N(N)
UNION ALL
SELECT @OK, DATEADD(mi,-90-N.N,SYSUTCDATETIME()), 0 FROM (VALUES(1),(2),(3),(4)) N(N) /* Outside the evaluation period */
" + Rule("FailedLogins", "FailedLogins", threshold: 5, evaluationPeriodMins: 60) + @"
EXEC Alert.FailedLoginsAlert_Upd", "Alert");
        }

        [TestMethod]
        public void WaitAlertAboveThreshold()
        {
            var alerts = AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='Wait', @WaitTypeID SMALLINT
SELECT @WaitTypeID = WaitTypeID FROM dbo.WaitType WHERE WaitType = N'LCK_M_X'
IF @WaitTypeID IS NULL
BEGIN
	INSERT INTO dbo.WaitType(WaitType) VALUES(N'LCK_M_X')
	SET @WaitTypeID = SCOPE_IDENTITY()
END
/* Snapshots a minute apart: Alert waits 60s per minute (~1000ms/sec), OK waits 100ms per minute */
INSERT INTO dbo.Waits(InstanceID,SnapshotDate,WaitTypeID,waiting_tasks_count,wait_time_ms,signal_wait_time_ms,sample_ms_diff)
SELECT I.InstanceID, CAST(DATEADD(mi,-M.Mins,SYSUTCDATETIME()) AS DATETIME2(2)), @WaitTypeID, 10, I.WaitMs, 0, 60000
FROM (VALUES(@Alert,60000),(@OK,100)) I(InstanceID,WaitMs)
CROSS JOIN (VALUES(1),(2),(3)) M(Mins)
" + Rule("Wait", "Wait - LCK_M_X", threshold: 500, evaluationPeriodMins: 5, details: @"{""WaitType"":""LCK_M_X"",""IsPerCore"":false}") + @"
EXEC Alert.WaitAlert_Upd", "Alert");
            Assert.AreEqual("Wait - LCK_M_X", alerts[0].AlertKey);
        }

        [TestMethod]
        public void CounterAlertAboveThreshold()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='Counter', @CounterID INT
INSERT INTO dbo.Counters(object_name,counter_name,instance_name,CounterType) VALUES(N'SQLServer:General Statistics',N'User Connections',N'',1)
SET @CounterID = SCOPE_IDENTITY()
INSERT INTO dbo.PerformanceCounters(InstanceID,CounterID,SnapshotDate,Value)
SELECT I.InstanceID, @CounterID, CAST(DATEADD(mi,-M.Mins,SYSUTCDATETIME()) AS DATETIME2(2)), I.Value
FROM (VALUES(@Alert,500),(@OK,10)) I(InstanceID,Value)
CROSS JOIN (VALUES(1),(2),(3)) M(Mins)
" + Rule("Counter", "SQLServer:General Statistics\\User Connections", threshold: 100, evaluationPeriodMins: 5,
                details: @"{""ComparisonSymbol"":"">="",""Aggregation"":""Avg"",""Counter"":{""ObjectName"":""SQLServer:General Statistics"",""CounterName"":""User Connections"",""InstanceName"":"""",""ApplyToAllInstances"":false}}") + @"
EXEC Alert.CounterAlert_Upd", "Alert");
        }

        private const string Databases = @"
INSERT INTO dbo.Databases(InstanceID,name,database_id,create_date,compatibility_level,is_auto_close_on,is_master_key_encrypted_by_server,is_published,is_subscribed,is_merge_published,is_distributor,is_sync_with_backup,is_broker_enabled,is_date_correlation_on,IsActive,state,is_in_standby,recovery_model)
VALUES(@Alert,N'DB1',5,GETDATE(),150,0,0,0,0,0,0,0,0,0,1,4,0,1), /* SUSPECT */
	(@Alert,N'DB2',6,GETDATE(),150,0,0,0,0,0,0,0,0,0,1,0,0,1),
	(@OK,N'DB1',5,GETDATE(),150,0,0,0,0,0,0,0,0,0,1,0,0,1),
	(@OK,N'DB2',6,GETDATE(),150,0,0,0,0,0,0,0,0,0,1,1,0,1) /* RESTORING - excluded by default */
";

        [TestMethod]
        public void DatabaseStatusAlertForSuspectDatabase()
        {
            var alerts = AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='DatabaseStatus'
" + Databases + Rule("DatabaseStatus", "DatabaseStatus") + @"
EXEC Alert.DatabaseStatusAlert_Upd", "Alert");
            Assert.AreEqual("DB:DB1", alerts[0].AlertKey);
        }

        [TestMethod]
        public void DatabaseStatusAlertResolvesWhenOnline()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='DatabaseStatus'
" + Databases + Rule("DatabaseStatus", "DatabaseStatus") + @"
EXEC Alert.DatabaseStatusAlert_Upd
UPDATE dbo.Databases SET state = 0 WHERE InstanceID = @Alert
EXEC Alert.DatabaseStatusAlert_Upd");
        }

        [TestMethod]
        public void DatabaseMailAlertExcludesDefaultStatuses()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='DatabaseMail'
UPDATE dbo.Instances SET DBMailStatus = N'STOPPED' WHERE InstanceID = @Alert
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,DBMailStatus) VALUES(N'NoXPs',N'NoXPs',1,1,N'15281|SQL Server blocked access to procedure') /* Excluded by default */
" + Rule("DatabaseMail", "DatabaseMail", details: @"{""ExcludedStatuses"":[]}") + @"
EXEC Alert.DatabaseMailAlert_Upd", "Alert");
        }

        [TestMethod]
        public void CollectionDatesAlertForOldCollection()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='CollectionDates'
INSERT INTO dbo.CollectionDates(InstanceID,Reference,SnapshotDate)
VALUES(@Alert,'Drives',DATEADD(mi,-120,SYSUTCDATETIME())),
	(@OK,'Drives',DATEADD(mi,-5,SYSUTCDATETIME()))
" + Rule("CollectionDates", "CollectionDates", threshold: 60, details: @"{""Reference"":""Drives"",""UseCriticalStatus"":false}") + @"
EXEC Alert.CollectionDatesAlert_Upd", "Alert");
        }

        [TestMethod]
        public void AGHealthAlertForUnhealthyDatabase()
        {
            var alerts = AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='AGHealth'
" + Databases + @"
INSERT INTO dbo.DatabasesHADR(InstanceID,DatabaseID,group_database_id,synchronization_state,synchronization_health)
SELECT D.InstanceID, D.DatabaseID, NEWID(), CASE WHEN D.InstanceID = @Alert AND D.name = N'DB1' THEN 0 ELSE 2 END, CASE WHEN D.InstanceID = @Alert AND D.name = N'DB1' THEN 0 ELSE 2 END
FROM dbo.Databases D
WHERE D.InstanceID IN(@Alert,@OK)
" + Rule("AGHealth", "AG Health") + @"
EXEC Alert.AGAlert_Upd", "Alert");
            StringAssert.Contains(alerts[0].Message, "DB1");
        }

        [TestMethod]
        public void CustomSqlAlertFromUserAlertProc()
        {
            var alerts = AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='CustomSql'
EXEC(N'CREATE PROC UserAlert.AlertTest AS SELECT InstanceID, N''Key1'', N''Custom message'' FROM dbo.Instances WHERE ConnectionID = N''Alert''')
" + Rule("CustomSql", "Custom", details: @"{""ProcName"":""AlertTest""}") + @"
EXEC Alert.CustomSqlAlert_Upd", "Alert");
            Assert.AreEqual("Custom:Key1", alerts[0].AlertKey);
            Assert.AreEqual("Custom message", alerts[0].Message);
        }

        [TestMethod]
        public void CustomSqlAlertCarriedForwardWhenProcFails()
        {
            AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='CustomSql'
EXEC(N'CREATE PROC UserAlert.AlertTest AS SELECT InstanceID, N''Key1'', N''Custom message'' FROM dbo.Instances WHERE ConnectionID = N''Alert''')
" + Rule("CustomSql", "Custom", details: @"{""ProcName"":""AlertTest""}") + @"
EXEC Alert.CustomSqlAlert_Upd
/* A transient error shouldn't resolve the alert */
EXEC(N'ALTER PROC UserAlert.AlertTest AS RAISERROR(''Transient failure'',16,1)')
EXEC Alert.CustomSqlAlert_Upd", "Alert");
        }

        [TestMethod]
        public void SQLAgentAlertUsesRuleGroup()
        {
            var alerts = AssertAlerted(TwoInstances + @"
DECLARE @Type VARCHAR(50)='SQLAgentAlert', @Now DATETIME2(3) = SYSUTCDATETIME()
" + Rule("SQLAgentAlert", "Alert: {AlertName}", details: @"{""MessageIDList"":[],""SeverityFrom"":17}", groupID: 5) + @"
DECLARE @Alerts dbo.Alerts
INSERT INTO @Alerts(id,name,message_id,severity,enabled,last_occurrence,occurrence_count)
VALUES(1,N'Severity 017',0,17,1,DATEADD(mi,-1,@Now),1),
	(2,N'Severity 016',0,16,1,DATEADD(mi,-1,@Now),1) /* Below SeverityFrom */
EXEC dbo.Alerts_Upd @Alerts = @Alerts, @InstanceID = @Alert, @SnapshotDate = @Now", "Alert");
            Assert.AreEqual("Alert: Severity 017", alerts[0].AlertKey);
            Assert.AreEqual(5, alerts[0].GroupID, "The alert should be sent to the rule's notification channel group");
        }
    }
}
