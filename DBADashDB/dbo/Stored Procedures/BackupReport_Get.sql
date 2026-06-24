CREATE PROC dbo.BackupReport_Get(
		@InstanceIDs IDs READONLY,
		@InstanceID INT=NULL,
		@IncludeCritical BIT=1,
		@IncludeWarning BIT=1,
		@IncludeNA BIT=0,
		@IncludeOK BIT=0,
		@ShowHidden BIT=1
)
AS
/*
	Combined report used by the Backups tab (BackupsView).
	Returns two result sets:
		0 - Instance summary (dbo.BackupSummary)
		1 - Database detail   (dbo.BackupStatus)
	@InstanceID is supplied when drilling down from the summary to a single instance.
*/

-- Drilling down to a single instance mirrors the legacy single-instance view: show all databases including hidden ones.
IF @InstanceID IS NOT NULL SET @ShowHidden=1

DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceID IS NOT NULL
BEGIN
	INSERT INTO @Instances(InstanceID) VALUES(@InstanceID)
END
ELSE IF EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT ID FROM @InstanceIDs
END
ELSE
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT InstanceID FROM dbo.Instances WHERE IsActive=1
END

/* Result set 0: Instance summary */
SELECT B.InstanceID,
       B.Instance,
       B.InstanceDisplayName,
       B.DatabaseCount,
       B.FullRecoveryCount,
       B.BulkLoggedCount,
       B.SimpleCount,
       B.FullOK,
       B.FullNA,
       B.FullWarning,
       B.FullCritical,
       B.DiffOK,
       B.DiffNA,
       B.DiffWarning,
       B.DiffCritical,
       B.LogOK,
       B.LogNA,
       B.LogWarning,
       B.LogCritical,
       B.SnapshotDate,
       B.SnapshotAge,
       B.SnapshotAgeStatus,
       B.FullBackupSizeGB,
       B.FullBackupMBsec,
       B.FullBackupSizeCompressedGB,
       B.FullCompressionSavingPct,
       B.FullBackupWriteMBsec,
       B.DiffBackupSizeGB,
       B.DiffBackupMBsec,
       B.DiffBackupSizeCompressedGB,
       B.DiffCompressionSavingPct,
       B.DiffBackupWriteMBsec,
       B.FullEncrypted,
       B.DiffEncrypted,
       B.LogEncrypted,
       B.FullChecksum,
       B.DiffChecksum,
       B.LogChecksum,
       B.FullCompressed,
       B.DiffCompressed,
       B.LogCompressed,
       B.FullPasswordProtected,
       B.DiffPasswordProtected,
       B.LogPasswordProtected,
       B.SnapshotBackups,
       B.DBThresholdConfiguration,
       B.InstanceThresholdConfiguration,
       B.IsPartnerBackup,
       B.OldestFull,
       B.OldestDiff,
       B.OldestLog,
       B.FullCompressionAlgorithms,
       B.DiffCompressionAlgorithms,
       B.LogCompressionAlgorithms,
       CAST('Configure' AS NVARCHAR(20)) AS Configure
FROM dbo.BackupSummary B
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = B.InstanceID)
AND (B.ShowInSummary = 1 OR @ShowHidden = 1)
ORDER BY B.Instance

/* Result set 1: Database detail */
;WITH BackupStatuses AS(
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
		BS.InstanceDisplayName,
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
		-- Partial / Filegroup backup status is only meaningful (and coloured) when the database is configured
		-- to consider those backup types, otherwise it is reported as N/A (3).  Computed here so the GUI can
		-- drive cell highlighting declaratively rather than with bespoke per-row code.
		CASE WHEN BS.ConsiderPartialBackups=1 THEN BS.FullBackupStatus ELSE 3 END AS PartialBackupStatus,
		CASE WHEN BS.ConsiderPartialBackups=1 THEN BS.DiffBackupStatus ELSE 3 END AS PartialDiffBackupStatus,
		CASE WHEN BS.ConsiderFGBackups=1 THEN BS.FullBackupStatus ELSE 3 END AS FGBackupStatus,
		CASE WHEN BS.ConsiderFGBackups=1 THEN BS.DiffBackupStatus ELSE 3 END AS FGDiffBackupStatus,
		BS.LogBackupWarningThreshold,
		BS.LogBackupCriticalThreshold,
		BS.FullBackupWarningThreshold,
		BS.FullBackupCriticalThreshold,
		BS.DiffBackupWarningThreshold,
		BS.DiffBackupCriticalThreshold,
		BS.ConsiderPartialBackups,
		BS.ConsiderFGBackups,
		BS.ConsiderCopyOnlyBackups,
		BS.ConsiderSnapshotBackups,
		BS.ConsiderFullBackupWithDiffThreshold,
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
		BS.LogBackupExcludedReason,
		BS.FullCompressionAlgorithm,
		BS.DiffCompressionAlgorithm,
		BS.LogCompressionAlgorithm,
		CAST('Configure' AS NVARCHAR(20)) AS Configure
FROM dbo.BackupStatus BS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = BS.InstanceID)
AND EXISTS(SELECT 1 FROM BackupStatuses s WHERE BS.BackupStatus=s.BackupStatus)
AND (BS.ShowInSummary = 1 OR @ShowHidden = 1)
ORDER BY BS.FullBackupStatus
OPTION(RECOMPILE)
GO
