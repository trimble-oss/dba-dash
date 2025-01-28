CREATE PROC Settings_Add(
	@Reset BIT = 0
)
AS
DECLARE @Defaults TABLE(
	SettingName VARCHAR(100) PRIMARY KEY,
	SettingValue SQL_VARIANT
)

INSERT INTO @Defaults(SettingName,SettingValue)
VALUES	('MemoryDumpCriticalThresholdHrs',48),
		('MemoryDumpWarningThresholdHrs',168),
		('CPUCriticalThreshold',90),
		('CPUWarningThreshold',75),
		('CPULowThreshold',50),
		('ReadLatencyCriticalThreshold',50),
		('ReadLatencyWarningThreshold',10),
		('ReadLatencyGoodThreshold',10),
		('WriteLatencyCriticalThreshold',50),
		('WriteLatencyWarningThreshold',10),
		('WriteLatencyGoodThreshold',10),
		('MinIOPsThreshold',100),
		('CriticalWaitCriticalThreshold',1000),
		('CriticalWaitWarningThreshold',10),
		('SummaryCacheDurationSec',300),
		('HardDeleteThresholdDays',NULL),
		('GUISummaryCacheDuration',NULL),
		('GUIDefaultCommandTimeout',NULL),
		('GUISummaryCommandTimeout',NULL),
		('GUIDrivePerformanceMaxDrives',NULL),
		('IdleWarningThresholdForSleepingSessionWithOpenTran',NULL),
		('IdleCriticalThresholdForSleepingSessionWithOpenTran',NULL),
		('GUICellToolTipMaxLength',NULL),
		('ExcludeClosedAlertsWithNotesFromPurge',CAST(1 AS BIT))

/* If reset, remove any customized settings to be re-inserted with defaults */
IF @Reset=1
BEGIN
	DELETE S 
	FROM dbo.Settings S
	WHERE EXISTS(SELECT 1 
				FROM @Defaults D 
				WHERE D.SettingName=S.SettingName
				)
END

INSERT INTO dbo.Settings(SettingName,SettingValue)
SELECT T.SettingName,T.SettingValue 
FROM @Defaults T
WHERE NOT EXISTS(
	SELECT 1 
	FROM dbo.Settings S 
	WHERE S.SettingName = T.SettingName
	)
AND T.SettingValue IS NOT NULL;

/* Other 'settings' that we just want to initialize to a date */
INSERT INTO dbo.Settings(SettingName,SettingValue)
SELECT SettingName,CAST('19000101' AS DATETIME) 
FROM (VALUES('PurgeCollectionErrorLog_StartDate'),
			('PurgeCollectionErrorLog_CompletedDate'),
			('PurgeQueryText_StartDate'),
			('PurgeQueryText_CompletedDate'),
			('PurgeQueryPlans_StartDate'),
			('PurgeQueryPlans_CompletedDate'),
			('PurgePartitions_StartDate'),
			('PurgePartitions_CompletedDate'),
			('MemoryDumpAckDate'),
			('PurgeBlockingSnapshotSummary_CompletedDate'),
			('PurgeBlockingSnapshotSummary_StartDate'),
			('PurgeClosedAlerts_StartDate'),
			('PurgeClosedAlerts_CompletedDate')
	  ) T(SettingName)
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.Settings S
				WHERE S.SettingName=T.SettingName);