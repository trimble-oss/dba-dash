CREATE PROC dbo.Backups_Get(
		@InstanceIDs VARCHAR(MAX)=NULL,
		@IncludeCritical BIT=1,
		@IncludeWarning BIT=1,
		@IncludeNA BIT=0,
		@IncludeOK BIT=0
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

WITH BackupStatuses AS(
	SELECT 1 AS BackupStatus
	WHERE @IncludeCritical=1
	UNION ALL
	SELECT 2
	WHERE @IncludeWarning=1
	UNION ALL
	SELECT 3
	WHERE @IncludeNA=1
	UNION ALL
	SELECT 4 
	WHERE @IncludeOK=1
)
SELECT InstanceID,
       DatabaseID,
       Instance,
       name,
       recovery_model,
	   recovery_model_desc,
       LastFull,
       LastDiff,
       LastLog,
       LastFG,
       LastFGDiff,
       LastPartial,
       LastPartialDiff,
       FullBackupStatus,
       LogBackupStatus,
       DiffBackupStatus,
       LogBackupWarningThreshold,
       LogBackupCriticalThreshold,
       FullBackupWarningThreshold,
       FullBackupCriticalThreshold,
       DiffBackupWarningThreshold,
       DiffBackupCriticalThreshold,
       ConsiderPartialBackups,
       ConsiderFGBackups,
	   SnapshotDate,
	   SnapshotAge,
	   ThresholdsConfiguredLevel,
	   CASE WHEN SnapshotAge>1440 THEN 1 WHEN BS.SnapshotAge>120 THEN 2 WHEN BS.SnapshotAge<60 THEN 4 ELSE 3 END AS SnapshotAgeStatus,
	   create_date_utc
FROM dbo.BackupStatus BS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = BS.InstanceID)
AND EXISTS(SELECT 1 FROM BackupStatuses s WHERE BS.BackupStatus=s.BackupStatus)
ORDER BY FullBackupStatus