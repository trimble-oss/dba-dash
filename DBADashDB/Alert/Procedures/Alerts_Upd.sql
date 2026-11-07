CREATE PROC Alert.Alerts_Upd
AS
/* 
	Updates all alerts.  Called based on alert polling frequency & startup delay in config tool.  
*/
SET NOCOUNT ON
SET DATEFIRST 1

/* 
	If blackout period is in effect, update any currently active alerts rather than resolving the alert
	Note: new alerts won't be generated during the blackout
*/
UPDATE AA 
	SET LastMessage = IIF(BP.IsBlackout=1,'Blackout Period','Blackout Period Ended.  Pending...'),
	UpdateCount+=1,
	UpdatedDate = SYSUTCDATETIME(),
	IsBlackout = BP.IsBlackout
FROM Alert.ActiveAlerts AA
OUTER APPLY Alert.IsBlackoutPeriod(AA.InstanceID,AA.AlertKey) BP
WHERE AA.IsResolved=0
AND AA.IsBlackout <> BP.IsBlackout

/* All instances & all alerts blacked out - active alerts were marked as blackout above, so there's nothing for the alert procs to do */
IF EXISTS(
		SELECT 1
		FROM Alert.BlackoutPeriodsInEffect_Get() BP
		WHERE BP.ApplyToInstanceID = -1
		AND BP.ApplyToTagID = -1
		AND BP.AlertKey = N'%'
	)
BEGIN
	PRINT 'Blackout period in effect for all instances. No alerts will be processed.'
END
ELSE
BEGIN

/*
	Resolve tag-scoped rules & blackout periods once for all the alert procs below.
	The run is flagged in session context with its start time - see Alert.ApplicableInstances_Ensure.
*/
EXEC Alert.ApplicableInstances_Upd
DECLARE @RunStarted DATETIME2 = SYSUTCDATETIME()
EXEC sp_set_session_context @key = N'DBADash.AlertRun', @value = @RunStarted

EXEC Alert.CPUAlert_Upd
EXEC Alert.WaitAlert_Upd
EXEC Alert.CounterAlert_Upd
EXEC Alert.AGAlert_Upd
EXEC Alert.DriveSpaceAlert_Upd
EXEC Alert.CollectionDatesAlert_Upd
EXEC Alert.AgentJobAlert_Upd
EXEC Alert.OfflineAlert_Upd
EXEC Alert.RestartAlert_Upd
EXEC Alert.DatabaseStatusAlert_Upd
EXEC Alert.BackupAlert_Upd
EXEC Alert.AgentNotRunningAlert_Upd
EXEC Alert.CustomSqlAlert_Upd
EXEC Alert.FailedLoginsAlert_Upd
EXEC Alert.DatabaseMailAlert_Upd
EXEC Alert.AzureDBSpaceAlert_Upd
EXEC Alert.ElasticPoolSpaceAlert_Upd
EXEC Alert.ServiceBrokerAlert_Upd

EXEC sp_set_session_context @key = N'DBADash.AlertRun', @value = NULL

END

/* Close Alerts that have been resolved for a period of time */

DECLARE @AlertAutoCloseThresholdMins INT =1440

SELECT @AlertAutoCloseThresholdMins = ISNULL(TRY_CAST(SettingValue AS INT),@AlertAutoCloseThresholdMins)
FROM dbo.Settings
WHERE SettingName = 'AlertAutoCloseThresholdMins'

DECLARE @AlertIDs BigIDs

INSERT INTO @AlertIDs
SELECT AlertID 
FROM Alert.ActiveAlerts
WHERE (
	(IsResolved=1 AND ResolvedDate < DATEADD(mi,-@AlertAutoCloseThresholdMins,SYSUTCDATETIME()))
	OR UpdatedDate < DATEADD(mi,-@AlertAutoCloseThresholdMins,SYSUTCDATETIME())
	)

EXEC Alert.ClosedAlerts_Add @AlertIDs=@AlertIDs