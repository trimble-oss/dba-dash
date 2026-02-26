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

IF EXISTS(
		SELECT 1 
		FROM Alert.IsBlackoutPeriod(DEFAULT,DEFAULT) BP
		WHERE BP.IsBlackout=1
	)
BEGIN
	PRINT 'Blackout period in effect for all instances. No alerts will be processed.'
	RETURN;
END

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