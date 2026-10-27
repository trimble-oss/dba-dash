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
		0 - Instance summary (aggregated from dbo.BackupStatus)
		1 - Database detail   (dbo.BackupStatus)
	@InstanceID is supplied when drilling down from the summary to a single instance.

	dbo.BackupStatus is evaluated once into #BackupStatus and both result sets are produced from it.
	BackupStatus is expensive to evaluate, so avoid referencing it more than once per call.
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

SELECT BS.*
INTO #BackupStatus
FROM dbo.BackupStatus BS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = BS.InstanceID)
AND (BS.ShowInSummary = 1 OR @ShowHidden = 1)
OPTION(RECOMPILE)

/* Result set 0: Instance summary */
SELECT BS.InstanceID,
	BS.Instance,
	BS.InstanceDisplayName,
	COUNT(*) AS DatabaseCount,
	SUM(CASE WHEN BS.recovery_model=1 THEN 1 ELSE 0 END) AS FullRecoveryCount,
	SUM(CASE WHEN BS.recovery_model=2 THEN 1 ELSE 0 END) AS BulkLoggedCount,
	SUM(CASE WHEN BS.recovery_model=3 THEN 1 ELSE 0 END) AS SimpleCount,
	SUM(CASE WHEN BS.FullBackupStatus=4 THEN 1 ELSE 0 END) AS FullOK,
	SUM(CASE WHEN BS.FullBackupStatus=3 THEN 1 ELSE 0 END) AS FullNA,
	SUM(CASE WHEN BS.FullBackupStatus=2 THEN 1 ELSE 0 END) AS FullWarning,
	SUM(CASE WHEN BS.FullBackupStatus=1 THEN 1 ELSE 0 END) AS FullCritical,
	SUM(CASE WHEN BS.DiffBackupStatus=4 THEN 1 ELSE 0 END) AS DiffOK,
	SUM(CASE WHEN BS.DiffBackupStatus=3 THEN 1 ELSE 0 END) AS DiffNA,
	SUM(CASE WHEN BS.DiffBackupStatus=2 THEN 1 ELSE 0 END) AS DiffWarning,
	SUM(CASE WHEN BS.DiffBackupStatus=1 THEN 1 ELSE 0 END) AS DiffCritical,
	SUM(CASE WHEN BS.LogBackupStatus=4 THEN 1 ELSE 0 END) AS LogOK,
	SUM(CASE WHEN BS.LogBackupStatus=3 THEN 1 ELSE 0 END) AS LogNA,
	SUM(CASE WHEN BS.LogBackupStatus=2 THEN 1 ELSE 0 END) AS LogWarning,
	SUM(CASE WHEN BS.LogBackupStatus=1 THEN 1 ELSE 0 END) AS LogCritical,
	MIN(BS.SnapshotDate) AS SnapshotDate,
	MIN(BS.SnapshotAge) AS SnapshotAge,
	MIN(BS.SnapshotAgeStatus) AS SnapshotAgeStatus,
	SUM(BS.FullBackupSizeGB) AS FullBackupSizeGB,
	SUM(BS.FullBackupSizeGB)*1024.0 /NULLIF(SUM(BS.LastFullDurationSec),0) AS FullBackupMBsec,
	SUM(BS.FullBackupSizeCompressedGB) AS FullBackupSizeCompressedGB,
	1.0-(SUM(BS.FullBackupSizeCompressedGB)/SUM(BS.FullBackupSizeGB)) AS FullCompressionSavingPct,
	SUM(BS.FullBackupSizeCompressedGB)*1024.0 /NULLIF(SUM(BS.LastFullDurationSec),0) AS FullBackupWriteMBsec,
	SUM(BS.DiffBackupSizeGB) AS DiffBackupSizeGB,
	SUM(BS.DiffBackupSizeGB)*1024.0 /NULLIF(SUM(BS.LastDiffDurationSec),0) AS DiffBackupMBsec,
	SUM(BS.DiffBackupSizeCompressedGB) AS DiffBackupSizeCompressedGB,
	1.0-(SUM(BS.DiffBackupSizeCompressedGB)/SUM(BS.DiffBackupSizeGB)) AS DiffCompressionSavingPct,
	SUM(BS.DiffBackupSizeCompressedGB)*1024.0 /NULLIF(SUM(BS.LastDiffDurationSec),0) AS DiffBackupWriteMBsec,
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS FullEncrypted,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS DiffEncrypted,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS LogEncrypted,
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS FullChecksum,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS DiffChecksum,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS LogChecksum,
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS FullCompressed,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS DiffCompressed,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS LogCompressed,
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS FullPasswordProtected,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS DiffPasswordProtected,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END)) AS LogPasswordProtected,
	CASE WHEN MIN(CASE WHEN BS.SnapshotBackups ='Y' THEN 1 WHEN BS.SnapshotBackups IS NULL THEN 2 ELSE 0 END) = 1 THEN 'ALL'
		 WHEN MAX(CASE WHEN BS.SnapshotBackups IN('Y','(Y)') THEN 1 ELSE 0 END) = 1 THEN 'SOME'
		 ELSE 'NONE' END AS SnapshotBackups,
	SUM(CASE WHEN BS.ThresholdsConfiguredLevel='DATABASE' THEN 1 ELSE 0 END) AS DBThresholdConfiguration,
	CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS InstanceThresholdConfiguration,
	CAST(MAX(CAST(BS.IsPartnerBackup AS INT)) AS BIT) AS IsPartnerBackup,
	MIN(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN BS.LastFull ELSE NULL END) AS OldestFull,
	MIN(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN BS.LastDiff ELSE NULL END) AS OldestDiff,
	MIN(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN BS.LastLog ELSE NULL END) AS OldestLog,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.FullCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')'
			FROM #BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.FullBackupExcludedReason IS NULL
			GROUP BY BSA.FullCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS FullCompressionAlgorithms,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.DiffCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')'
			FROM #BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.DiffBackupExcludedReason IS NULL
			GROUP BY BSA.DiffCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS DiffCompressionAlgorithms,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.LogCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')'
			FROM #BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.LogBackupExcludedReason IS NULL
			GROUP BY BSA.LogCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS LogCompressionAlgorithms,
	CAST('Configure' AS NVARCHAR(20)) AS Configure
FROM #BackupStatus BS
LEFT JOIN dbo.BackupThresholds T ON BS.InstanceID = T.InstanceID AND T.DatabaseID = -1
GROUP BY BS.InstanceID,
	BS.Instance,
	BS.InstanceDisplayName,
	T.InstanceID
ORDER BY BS.Instance

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
FROM #BackupStatus BS
WHERE EXISTS(SELECT 1 FROM BackupStatuses s WHERE BS.BackupStatus=s.BackupStatus)
ORDER BY BS.FullBackupStatus
GO
