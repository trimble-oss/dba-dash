using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    /*
     * Rules scoped to multiple tags: tags with different names must all match (AND), tags with the same name are
     * alternatives (OR).  Tag-scoped rules and blackouts are resolved once per alert run by Alert.ApplicableInstances_Upd.
     * Uses the restart alert as every instance below was restarted 5 mins ago - so the alerted instances are exactly
     * the instances the rule applies to.
     */
    public partial class AlertSqlTests
    {
        private const string TaggedInstances = @"
DECLARE @Type VARCHAR(50)='RESTART'
DECLARE @Prod INT, @UAT INT, @Dev INT, @APAC INT, @EMEA INT, @RuleID INT, @TagIDs VARCHAR(MAX)
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Role',N'Production') SET @Prod = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Role',N'UAT') SET @UAT = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Role',N'Dev') SET @Dev = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Region',N'APAC') SET @APAC = SCOPE_IDENTITY()
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'Region',N'EMEA') SET @EMEA = SCOPE_IDENTITY()

INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
SELECT V.Name, V.Name, 1, 1, 3, 0, DATEADD(mi,-5,GETUTCDATE())
FROM (VALUES(N'ProdAPAC'),(N'ProdEMEA'),(N'UATAPAC'),(N'DevAPAC'),(N'Untagged')) V(Name)

INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID)
SELECT I.InstanceID, T.TagID
FROM dbo.Instances I
JOIN (VALUES(N'ProdAPAC',@Prod),(N'ProdAPAC',@APAC),
			(N'ProdEMEA',@Prod),(N'ProdEMEA',@EMEA),
			(N'UATAPAC',@UAT),(N'UATAPAC',@APAC),
			(N'DevAPAC',@Dev),(N'DevAPAC',@APAC)) T(Instance,TagID) ON T.Instance = I.Instance
";

        /// <summary>SQL expression joining tag variables (e.g. "@Prod,@APAC") with commas.  CONCAT rather than CONCAT_WS as CI tests on SQL 2016.  The trailing '' allows a single variable.</summary>
        private static string TagList(string tagIDsExpression) => "CONCAT(" + string.Join(",',',", tagIDsExpression.Split(',')) + ",'')";

        /// <summary>Add a restart rule scoped to a comma-separated list of tag variables, via the proc the GUI uses</summary>
        private static string TagRule(string tagIDsExpression) => $@"
SET @TagIDs = {TagList(tagIDsExpression)}
EXEC Alert.Rule_Add @Type=@Type, @AlertKey=N'Restart', @Priority=20, @Threshold=NULL, @EvaluationPeriodMins=NULL, @Details=NULL, @ApplyToTagIDs=@TagIDs
SELECT @RuleID = MAX(RuleID) FROM Alert.Rules
";

        [TestMethod]
        public void MultiTagRuleRequiresEveryTagName()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod,@APAC") + @"
EXEC Alert.RestartAlert_Upd", "ProdAPAC");
        }

        [TestMethod]
        public void MultiTagRuleMatchesAnyValueOfTheSameTagName()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod,@UAT,@APAC") + @"
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "UATAPAC");
        }

        [TestMethod]
        public void MultiTagRuleThroughAlertsUpd()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod,@UAT,@EMEA") + @"
EXEC Alert.Alerts_Upd
IF SESSION_CONTEXT(N'DBADash.AlertRun') IS NOT NULL THROW 50000,'Alerts_Upd left the alert run flag set',1
", "ProdEMEA");
        }

        [TestMethod]
        public void SingleTagRuleThroughTagList()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod") + @"
IF NOT EXISTS(SELECT 1 FROM Alert.Rules WHERE RuleID = @RuleID AND ApplyToTagID = @Prod AND ApplyToTagIDs IS NULL) THROW 50000,'A single tag should be stored as ApplyToTagID',1
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "ProdEMEA");
        }

        [TestMethod]
        public void TagRuleReflectsTagChangesSinceTheLastRun()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod,@APAC") + @"
EXEC Alert.RestartAlert_Upd
/* ProdEMEA moves to APAC.  Run on its own, the alert proc must re-resolve the rule rather than use the previous run's match */
DELETE dbo.InstanceIDsTags WHERE TagID = @EMEA
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @APAC FROM dbo.Instances WHERE Instance = N'ProdEMEA'
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "ProdEMEA");
        }

        [TestMethod]
        public void MultiTagRuleCombinesAzureServerAndDatabaseTags()
        {
            AssertAlerted(TaggedInstances + @"
/* Region tag on the Azure server, Role tag on the database */
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'azsrv1',N'azsrv1|DB1',1,1,5,0,DATEADD(mi,-5,GETUTCDATE()))
INSERT INTO dbo.InstanceTags(Instance,TagID) VALUES(N'azsrv1',@APAC)
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @Prod FROM dbo.Instances WHERE ConnectionID = N'azsrv1|DB1'
" + TagRule("@Prod,@APAC") + @"
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "azsrv1|DB1");
        }

        [TestMethod]
        public void DeletedTagNeverWidensARule()
        {
            AssertAlerted(TaggedInstances + TagRule("@Prod,@APAC") + @"
DELETE dbo.InstanceIDsTags WHERE TagID = @APAC
DELETE dbo.Tags WHERE TagID = @APAC
EXEC Alert.RestartAlert_Upd");
        }

        [TestMethod]
        public void DeletedTagWithLeftoverAssignmentsNeverMatches()
        {
            /* InstanceIDsTags has no foreign key to Tags, so assignments can outlive a tag deleted outside the app */
            AssertAlerted(TaggedInstances + BlackoutVariables + Rule("RESTART", "Restart") + TagBlackout("@Prod,@EMEA") + TagRule("@Prod,@APAC") + @"
DELETE dbo.Tags WHERE TagID IN(@APAC,@EMEA)
IF EXISTS(SELECT 1 FROM dbo.Instances I CROSS APPLY Alert.TagScopeMatchesInstance(-2,@TagIDs,I.InstanceID) TM WHERE TM.IsMatch = 1) THROW 50000,'Deleted tag matched an instance',1
EXEC Alert.RestartAlert_Upd
IF EXISTS(SELECT 1 FROM Alert.RuleInstanceMatch WHERE RuleID = @RuleID) THROW 50000,'Deleted tag matched an instance in the alert run',1
/* The blackout (Production AND deleted EMEA) mutes nothing, so the all instances rule alerts everywhere */", "ProdAPAC", "ProdEMEA", "UATAPAC", "DevAPAC", "Untagged");
        }

        [TestMethod]
        public void TagsGetByIDIncludesTagsOnlyOnInactiveInstances()
        {
            /* The GUI loads a rule's tags by ID - a tag whose instances are all inactive must still show by name, not as deleted */
            var tags = new List<string>();
            RunAndRollback(TaggedInstances + @"
UPDATE dbo.Instances SET IsActive = 0 WHERE Instance = N'UATAPAC'
DECLARE @Tags TABLE(TagID INT, TagName NVARCHAR(50), TagValue NVARCHAR(128))
INSERT INTO @Tags EXEC dbo.Tags_Get
IF EXISTS(SELECT 1 FROM @Tags WHERE TagID = @UAT) THROW 50000,'Tags_Get without IDs should only return tags in use',1
SET @TagIDs = CONCAT(@UAT,',',@Prod,',999999')
EXEC dbo.Tags_Get @TagIDs = @TagIDs", cmd =>
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read()) tags.Add(rdr.GetString(1) + ":" + rdr.GetString(2));
            });
            CollectionAssert.AreEquivalent(new[] { "Role:Production", "Role:UAT" }, tags);
        }

        [TestMethod]
        public void BlackoutPeriodsStillApply()
        {
            AssertAlerted(TaggedInstances + Rule("RESTART", "Restart") + @"
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone) VALUES(@APAC,-1,N'%',N'UTC') /* Tag */
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone) SELECT -1, InstanceID, N'Rest%', N'UTC' FROM dbo.Instances WHERE Instance = N'Untagged' /* Instance + matching alert key */
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone,StartDate,EndDate) VALUES(-1,-1,N'%',N'UTC','2020-01-01','2020-01-02') /* Ended */
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone) VALUES(-1,-1,N'CPU',N'UTC') /* Different alert */
EXEC Alert.RestartAlert_Upd", "ProdEMEA");
        }

        [TestMethod]
        public void PausedCollectionsMuteAlerts()
        {
            AssertAlerted(TaggedInstances + Rule("RESTART", "Restart") + @"
UPDATE dbo.Instances SET CollectionsPausedUntil = DATEADD(hh,1,SYSUTCDATETIME()) WHERE Instance <> N'Untagged'
EXEC Alert.RestartAlert_Upd", "Untagged");
        }

        [TestMethod]
        public void TagListOrderDoesNotCreateADifferentRule()
        {
            RunAndRollback(TaggedInstances + TagRule("@APAC,@Prod") + @"
IF NOT EXISTS(SELECT 1 FROM Alert.Rules WHERE RuleID = @RuleID AND ApplyToTagID = -2 AND ApplyToTagIDs = CONCAT(@Prod,',',@APAC)) THROW 50000,'Tag list should be stored sorted',1
" + TagRule("@Prod,@APAC,@Prod"), cmd =>
            {
                var ex = Assert.ThrowsExactly<SqlException>(() => cmd.ExecuteNonQuery());
                Assert.IsTrue(ex.Number is 2601 or 2627, "Expected a duplicate rule error, got: " + ex.Message);
            });
        }

        [TestMethod]
        public void RulesGetDescribesMultipleTags()
        {
            string? description = null;
            RunAndRollback(TaggedInstances + TagRule("@Prod,@UAT,@APAC") + @"
DECLARE @Rules TABLE(RuleID INT, Type VARCHAR(50), AlertKey NVARCHAR(256), Priority TINYINT, PriorityDescription NVARCHAR(100), ApplyToTagID INT, Threshold DECIMAL(28,9),
	EvaluationPeriodMins INT, IsActive BIT, Details NVARCHAR(MAX), ApplyToTag NVARCHAR(MAX), ApplyToInstanceID INT, ApplyToInstance NVARCHAR(256), ApplyToHidden BIT,
	Notes NVARCHAR(MAX), GroupID INT, GroupName NVARCHAR(256), ApplyToTagIDs VARCHAR(800))
INSERT INTO @Rules EXEC Alert.Rules_Get @RuleID = @RuleID
SELECT ApplyToTag FROM @Rules", cmd => description = (string)cmd.ExecuteScalar());
            Assert.AreEqual("Region:APAC AND (Role:Production OR Role:UAT)", description);
        }

        /// <summary>SQL Agent alerts check a single instance as they're imported - that must agree with the per alert run match</summary>
        [TestMethod]
        public void SingleInstanceMatchAgreesWithAlertRunMatch()
        {
            int mismatches = -1;
            RunAndRollback(TaggedInstances + TagRule("@Prod,@APAC") + TagRule("@Prod,@UAT,@APAC") + TagRule("@EMEA") + TagRule("@Dev,@EMEA") + @"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'azsrv1',N'azsrv1|DB1',1,1,5,0,DATEADD(mi,-5,GETUTCDATE()))
INSERT INTO dbo.InstanceTags(Instance,TagID) VALUES(N'azsrv1',@APAC)
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @UAT FROM dbo.Instances WHERE ConnectionID = N'azsrv1|DB1'
EXEC Alert.ApplicableInstances_Upd
SELECT COUNT(*)
FROM Alert.Rules R
CROSS JOIN dbo.Instances I
CROSS APPLY Alert.TagScopeMatchesInstance(R.ApplyToTagID,R.ApplyToTagIDs,I.InstanceID) TM
WHERE R.ApplyToTagID <> -1
AND TM.IsMatch <> CASE WHEN EXISTS(SELECT 1 FROM Alert.RuleInstanceMatch M WHERE M.RuleID = R.RuleID AND M.InstanceID = I.InstanceID) THEN 1 ELSE 0 END",
                cmd => mismatches = (int)cmd.ExecuteScalar());
            Assert.AreEqual(0, mismatches);
        }

        [TestMethod]
        public void MatchingInstanceCountForTagPicker()
        {
            var counts = new List<string>();
            RunAndRollback(TaggedInstances + @"
/* Hidden instance - rules only apply to it with Apply To Hidden, so it's counted separately */
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset) VALUES(N'HiddenProdAPAC',N'HiddenProdAPAC',1,0,3,0)
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, T.TagID FROM dbo.Instances CROSS JOIN (VALUES(@Prod),(@APAC)) T(TagID) WHERE Instance = N'HiddenProdAPAC'
DECLARE @Counts TABLE(InstanceCount INT, HiddenInstanceCount INT, Seq INT IDENTITY)
DECLARE @List VARCHAR(MAX) = CONCAT(@Prod,',',@UAT,',',@APAC)
INSERT INTO @Counts(InstanceCount,HiddenInstanceCount) EXEC Alert.TagScopeMatchingInstanceCount_Get @ApplyToTagIDs = @List
INSERT INTO @Counts(InstanceCount,HiddenInstanceCount) EXEC Alert.TagScopeMatchingInstanceCount_Get @ApplyToTagIDs = ''
SELECT CONCAT(InstanceCount,'+',HiddenInstanceCount) FROM @Counts ORDER BY Seq", cmd =>
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read()) counts.Add(rdr.GetString(0));
            });
            CollectionAssert.AreEqual(new[] { "2+1", "5+1" }, counts, "(Production OR UAT) AND APAC = 2 instances + 1 hidden, no tags = all 5 + 1 hidden");
        }

        [TestMethod]
        public void TooManyTagsGivesAClearError()
        {
            RunAndRollback(@"
/* 200 TagIDs of 5 digits = over 800 characters */
DECLARE @List VARCHAR(MAX) = STUFF((SELECT ',' + CAST(10000 + n AS VARCHAR(10)) FROM (SELECT TOP(200) ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) n FROM sys.all_objects) x FOR XML PATH('')),1,1,'')
EXEC Alert.TagScopeMatchingInstanceCount_Get @ApplyToTagIDs = @List", cmd =>
            {
                var ex = Assert.ThrowsExactly<SqlException>(() => cmd.ExecuteNonQuery());
                StringAssert.Contains(ex.Message, "Too many tags selected");
            });
            RunAndRollback(@"
DECLARE @List VARCHAR(MAX) = STUFF((SELECT ',' + CAST(10000 + n AS VARCHAR(10)) FROM (SELECT TOP(200) ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) n FROM sys.all_objects) x FOR XML PATH('')),1,1,'')
EXEC Alert.Rule_Add @Type='RESTART', @AlertKey=N'Restart', @Priority=20, @Threshold=NULL, @EvaluationPeriodMins=NULL, @Details=NULL, @ApplyToTagIDs=@List", cmd =>
            {
                var ex = Assert.ThrowsExactly<SqlException>(() => cmd.ExecuteNonQuery());
                StringAssert.Contains(ex.Message, "Too many tags selected");
            });
        }

        [TestMethod]
        public void TagNamesGroupCaseInsensitively()
        {
            /* Role and role are the same tag name (OR), whatever the repository collation - matching how the GUI describes the scope */
            AssertAlerted(TaggedInstances + @"
DECLARE @Staging INT
INSERT INTO dbo.Tags(TagName,TagValue) VALUES(N'role',N'Staging') SET @Staging = SCOPE_IDENTITY()
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time) VALUES(N'StagingAPAC',N'StagingAPAC',1,1,3,0,DATEADD(mi,-5,GETUTCDATE()))
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, T.TagID FROM dbo.Instances CROSS JOIN (VALUES(@Staging),(@APAC)) T(TagID) WHERE Instance = N'StagingAPAC'
" + TagRule("@Prod,@Staging,@APAC") + @"
IF NOT EXISTS(SELECT 1 FROM Alert.TagScope_Describe(CONCAT(@Prod,',',@Staging,',',@APAC)) WHERE TagText = N'Region:APAC AND (Role:Production OR role:Staging)') THROW 50000,'Description should group Role and role',1
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "StagingAPAC");
        }

        /// <summary>SQL Agent alerts are matched to rules as they're imported, one instance at a time - not by the alert run</summary>
        [TestMethod]
        public void SqlAgentAlertImportUsesRuleTagScope()
        {
            AssertAlerted(TaggedInstances + BlackoutVariables + @"
SET @Type = 'SQLAgentAlert'
/* Also Production + APAC, but hidden / in an instance blackout */
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset) VALUES(N'HiddenProdAPAC',N'HiddenProdAPAC',1,0,3,0),(N'BlackoutProdAPAC',N'BlackoutProdAPAC',1,1,3,0)
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, T.TagID FROM dbo.Instances CROSS JOIN (VALUES(@Prod),(@APAC)) T(TagID) WHERE Instance IN(N'HiddenProdAPAC',N'BlackoutProdAPAC')
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone) SELECT -1, InstanceID, N'%', N'UTC' FROM dbo.Instances WHERE Instance = N'BlackoutProdAPAC'
" + TagBlackout("@Prod,@EMEA") + @"
SET @TagIDs = CONCAT(@Prod,',',@APAC,',',@EMEA,',',@UAT) /* (Production OR UAT) AND (APAC OR EMEA) */
EXEC Alert.Rule_Add @Type=@Type, @AlertKey=N'Alert: {AlertName}', @Priority=20, @Threshold=NULL, @EvaluationPeriodMins=NULL, @Details=N'{""MessageIDList"":[]}', @ApplyToTagIDs=@TagIDs

DECLARE @Alerts dbo.Alerts, @InstanceID INT, @Now DATETIME2(3) = SYSUTCDATETIME()
INSERT INTO @Alerts(id,name,message_id,severity,enabled,delay_between_responses,last_occurrence,include_event_description,occurrence_count,has_notification,category_id)
VALUES(1,N'Severity 17',0,17,1,0,@Now,0,1,0,0)
DECLARE cInstances CURSOR LOCAL FAST_FORWARD FOR SELECT InstanceID FROM dbo.Instances
OPEN cInstances
FETCH NEXT FROM cInstances INTO @InstanceID
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC dbo.Alerts_Upd @Alerts = @Alerts, @InstanceID = @InstanceID, @SnapshotDate = @Now
	FETCH NEXT FROM cInstances INTO @InstanceID
END
/* ProdEMEA is in the (Production AND EMEA) blackout, DevAPAC & Untagged are out of scope, plus the hidden & instance blackout instances */
", "ProdAPAC", "UATAPAC");
        }

        [TestMethod]
        public void AllInstancesBlackoutSkipsAlertProcs()
        {
            var messages = new List<string>();
            int alerts = -1;
            RunAndRollback(TaggedInstances + Rule("RESTART", "Restart") + @"
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone) VALUES(-1,-1,N'%',N'UTC')
EXEC Alert.Alerts_Upd
SELECT COUNT(*) FROM Alert.ActiveAlerts", cmd => alerts = (int)cmd.ExecuteScalar(), messages);
            Assert.AreEqual(0, alerts);
            Assert.IsTrue(messages.Any(m => m.Contains("Blackout period in effect for all instances")), "Messages:\n" + string.Join("\n", messages));
            Assert.IsFalse(messages.Any(m => m.StartsWith("Processing alerts of type")), "Alert procs shouldn't run during an all instances blackout");
        }

        [TestMethod]
        public void StaleAlertRunFlagIsIgnored()
        {
            /* A flag left by an aborted Alerts_Upd more than 10 mins ago must not stop a standalone alert proc resolving the rule */
            AssertAlerted(TaggedInstances + TagRule("@Prod,@APAC") + @"
DECLARE @Old DATETIME2 = DATEADD(hh,-1,SYSUTCDATETIME())
EXEC sp_set_session_context @key = N'DBADash.AlertRun', @value = @Old
EXEC Alert.RestartAlert_Upd
EXEC sp_set_session_context @key = N'DBADash.AlertRun', @value = NULL", "ProdAPAC");
        }

        [TestMethod]
        public void BlackoutDayOfWeekIgnoresDateFirst()
        {
            /* A blackout for today only must apply whatever the session's DATEFIRST */
            AssertAlerted(TaggedInstances + Rule("RESTART", "Restart") + @"
DECLARE @Day TINYINT = (DATEPART(dw,SYSUTCDATETIME()) + @@DATEFIRST - 2) % 7 + 1 /* Monday = 1 */
INSERT INTO Alert.BlackoutPeriod(ApplyToTagID,ApplyToInstanceID,AlertKey,TimeZone,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday)
VALUES(@APAC,-1,N'%',N'UTC',IIF(@Day=1,1,0),IIF(@Day=2,1,0),IIF(@Day=3,1,0),IIF(@Day=4,1,0),IIF(@Day=5,1,0),IIF(@Day=6,1,0),IIF(@Day=7,1,0))
SET DATEFIRST 7
EXEC Alert.RestartAlert_Upd
SET DATEFIRST 1", "ProdEMEA", "Untagged");
        }

        /// <summary>Add a blackout scoped to a comma-separated list of tag variables, via the proc the GUI uses</summary>
        private static string TagBlackout(string tagIDsExpression, string alertKey = "%") => $@"
SET @TagIDs = {TagList(tagIDsExpression)}
SET @BlackoutPeriodID = NULL
EXEC Alert.BlackoutPeriod_Add @StartDate=NULL, @EndDate=NULL, @AlertKey=N'{alertKey}', @ApplyToTagIDs=@TagIDs, @BlackoutPeriodID=@BlackoutPeriodID OUT
";

        private const string BlackoutVariables = @"
DECLARE @BlackoutPeriodID INT
";

        [TestMethod]
        public void MultiTagBlackoutMutesMatchingInstances()
        {
            AssertAlerted(TaggedInstances + BlackoutVariables + Rule("RESTART", "Restart") + TagBlackout("@Prod,@UAT,@APAC") + @"
EXEC Alert.RestartAlert_Upd", "ProdEMEA", "DevAPAC", "Untagged");
        }

        [TestMethod]
        public void MultiTagBlackoutThroughAlertsUpd()
        {
            AssertAlerted(TaggedInstances + BlackoutVariables + Rule("RESTART", "Restart") + TagBlackout("@Prod,@EMEA", "Rest%") + TagBlackout("@Dev,@APAC", "CPU") + @"
EXEC Alert.Alerts_Upd", "ProdAPAC", "UATAPAC", "DevAPAC", "Untagged");
        }

        [TestMethod]
        public void MultiTagBlackoutWithDeletedTagMutesNothing()
        {
            AssertAlerted(TaggedInstances + BlackoutVariables + Rule("RESTART", "Restart") + TagBlackout("@Prod,@APAC") + @"
DELETE dbo.InstanceIDsTags WHERE TagID = @APAC
DELETE dbo.Tags WHERE TagID = @APAC
EXEC Alert.RestartAlert_Upd", "ProdAPAC", "ProdEMEA", "UATAPAC", "DevAPAC", "Untagged");
        }

        [TestMethod]
        public void BlackoutTagListIsNormalized()
        {
            RunAndRollback(TaggedInstances + BlackoutVariables + TagBlackout("@Prod") + @"
IF NOT EXISTS(SELECT 1 FROM Alert.BlackoutPeriod WHERE BlackoutPeriodID = @BlackoutPeriodID AND ApplyToTagID = @Prod AND ApplyToTagIDs IS NULL) THROW 50000,'A single tag should be stored as ApplyToTagID',1
" + TagBlackout("@APAC,@Prod") + @"
IF NOT EXISTS(SELECT 1 FROM Alert.BlackoutPeriod WHERE BlackoutPeriodID = @BlackoutPeriodID AND ApplyToTagID = -2 AND ApplyToTagIDs = CONCAT(@Prod,',',@APAC)) THROW 50000,'Tag list should be stored sorted',1
/* Edit keeps the list */
DECLARE @List VARCHAR(MAX) = CONCAT(@UAT,',',@Prod)
EXEC Alert.BlackoutPeriod_Add @StartDate=NULL, @EndDate=NULL, @ApplyToTagIDs=@List, @BlackoutPeriodID=@BlackoutPeriodID OUT
IF NOT EXISTS(SELECT 1 FROM Alert.BlackoutPeriod WHERE BlackoutPeriodID = @BlackoutPeriodID AND ApplyToTagID = -2 AND ApplyToTagIDs = CONCAT(@Prod,',',@UAT)) THROW 50000,'Edited tag list not saved',1
" + TagBlackout("@UAT,@Prod,@UAT"), cmd =>
            {
                var ex = Assert.ThrowsExactly<SqlException>(() => cmd.ExecuteNonQuery());
                Assert.IsTrue(ex.Number is 2601 or 2627, "Expected a duplicate blackout error, got: " + ex.Message);
            });
        }

        /// <summary>Alert.IsBlackoutPeriod (active alerts, SQL Agent alert import) must agree with the per alert run Alert.ActiveBlackout</summary>
        [TestMethod]
        public void IsBlackoutPeriodAgreesWithActiveBlackout()
        {
            int mismatches = -1;
            RunAndRollback(TaggedInstances + BlackoutVariables + TagBlackout("@Prod,@APAC") + TagBlackout("@UAT,@Dev,@APAC", "Rest%") + TagBlackout("@EMEA", "CPU") + @"
INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,ShowInSummary,EngineEdition,UTCOffset,sqlserver_start_time)
VALUES(N'azsrv1',N'azsrv1|DB1',1,1,5,0,DATEADD(mi,-5,GETUTCDATE()))
INSERT INTO dbo.InstanceTags(Instance,TagID) VALUES(N'azsrv1',@APAC)
INSERT INTO dbo.InstanceIDsTags(InstanceID,TagID) SELECT InstanceID, @Prod FROM dbo.Instances WHERE ConnectionID = N'azsrv1|DB1'
SET DATEFIRST 1
EXEC Alert.ApplicableInstances_Upd
SELECT COUNT(*)
FROM dbo.Instances I
CROSS JOIN (VALUES(N'Restart'),(N'CPU'),(N'Other')) K(AlertKey)
CROSS APPLY Alert.IsBlackoutPeriod(I.InstanceID,K.AlertKey) BP
WHERE I.IsActive = 1
AND BP.IsBlackout <> CASE WHEN EXISTS(SELECT 1 FROM Alert.ActiveBlackout AB WHERE AB.InstanceID = I.InstanceID AND K.AlertKey LIKE AB.AlertKeyPattern) THEN 1 ELSE 0 END",
                cmd => mismatches = (int)cmd.ExecuteScalar());
            Assert.AreEqual(0, mismatches);
        }
    }
}
