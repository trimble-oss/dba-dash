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
SELECT	BS.InstanceID,
		BS.DatabaseID,
		BS.Instance,
		BS.name,
		BS.recovery_model,
		BS.recovery_model_desc,
		BS.LastFull,
		BS.LastDiff,
		BS.LastLog,
		BS.LastFG,
		BS.LastFGDiff,
		BS.LastPartial,
		BS.LastPartialDiff,
		BS.FullBackupStatus,
		BS.LogBackupStatus,
		BS.DiffBackupStatus,
		BS.BackupStatus,
		BS.LogBackupWarningThreshold,
		BS.LogBackupCriticalThreshold,
		BS.FullBackupWarningThreshold,
		BS.FullBackupCriticalThreshold,
		BS.DiffBackupWarningThreshold,
		BS.DiffBackupCriticalThreshold,
		BS.ConsiderPartialBackups,
		BS.ConsiderFGBackups,
		BS.ThresholdsConfiguredLevel,
		BS.SnapshotDate,
		BS.SnapshotAge,
		BS.create_date,
		BS.create_date_utc,
		BS.UTCOffset,
		BS.LastFullDuration,
		BS.LastFullDurationSec,
		BS.FullBackupMBsec,
		BS.FullBackupWriteMBsec,
		BS.FullBackupSizeGB,
		BS.FullBackupSizeCompressedGB,
		BS.FullCompressionSavingPct,
		BS.LastDiffDuration,
		BS.LastDiffDurationSec,
		BS.DiffBackupMBsec,
		BS.DiffBackupWriteMBsec,
		BS.DiffBackupSizeGB,
		BS.DiffBackupSizeCompressedGB,
		BS.DiffCompressionSavingPct,
		BS.IsFullDamaged,
		BS.IsDiffDamaged,
		BS.IsLogDamaged,
		BS.IsFullChecksum,
		BS.IsDiffChecksum,
		BS.IsLogChecksum,
		BS.IsFullPasswordProtected,
		BS.IsDiffPasswordProtected,
		BS.IsLogPasswordProtected,
		BS.IsFullEncrypted,
		BS.IsDiffEncrypted,
		BS.IsLogEncrypted,
		BS.SnapshotBackups,
		BS.SnapshotAgeStatus,
		BS.IsFullCompressed,
		BS.IsDiffCompressed,
		BS.IsLogCompressed,
		BS.IsPartnerBackup,
		BS.FullBackupExcludedReason,
		BS.DiffBackupExcludedReason,
		BS.LogBackupExcludedReason
FROM dbo.BackupStatus BS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = BS.InstanceID)
AND EXISTS(SELECT 1 FROM BackupStatuses s WHERE BS.BackupStatus=s.BackupStatus)
ORDER BY FullBackupStatus