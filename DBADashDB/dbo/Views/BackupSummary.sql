CREATE VIEW dbo.BackupSummary
AS
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
	MIN(SnapshotAgeStatus) as SnapshotAgeStatus,
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
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As FullEncrypted,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As DiffEncrypted,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogEncrypted AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As LogEncrypted,	
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As FullChecksum,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As DiffChecksum,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogChecksum AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As LogChecksum,
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As FullCompressed,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsDiffCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As DiffCompressed,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsLogCompressed AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As LogCompressed,	
	CONCAT(SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN CAST(BS.IsFullPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As FullPasswordProtected,
	CONCAT(SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN CAST(BS.IsFullPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As DiffPasswordProtected,
	CONCAT(SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN CAST(BS.IsFullPasswordProtected AS INT) ELSE 0 END),'/',SUM(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN 1 ELSE 0 END))  As LogPasswordProtected,
	CASE WHEN MIN(CASE WHEN BS.SnapshotBackups ='Y' THEN 1 WHEN BS.SnapshotBackups IS NULL THEN 2 ELSE 0 END) = 1 THEN 'ALL'
		 WHEN MAX(CASE WHEN BS.SnapshotBackups IN('Y','(Y)') THEN 1 ELSE 0 END) = 1 THEN 'SOME'
		 ELSE 'NONE' END AS SnapshotBackups,
	SUM(CASE WHEN BS.ThresholdsConfiguredLevel='DATABASE' THEN 1 ELSE 0 END) AS DBThresholdConfiguration,
	CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS InstanceThresholdConfiguration,
	CAST(MAX(CAST(BS.IsPartnerBackup AS INT)) AS BIT) as IsPartnerBackup,
	MIN(CASE WHEN BS.FullBackupExcludedReason IS NULL THEN BS.LastFull ELSE NULL END) as OldestFull,
	MIN(CASE WHEN BS.DiffBackupExcludedReason IS NULL THEN BS.LastDiff ELSE NULL END) as OldestDiff,
	MIN(CASE WHEN BS.LogBackupExcludedReason IS NULL THEN BS.LastLog ELSE NULL END) as OldestLog,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.FullCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')' 
			FROM dbo.BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.FullBackupExcludedReason IS NULL 
			GROUP BY BSA.FullCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS FullCompressionAlgorithms,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.DiffCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')' 
			FROM dbo.BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.DiffBackupExcludedReason IS NULL 
			GROUP BY BSA.DiffCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)')
		,1,2,'') AS DiffCompressionAlgorithms,
	STUFF(
			(SELECT ', ' + ISNULL(BSA.LogCompressionAlgorithm,'NULL') + '(' + CAST(COUNT(*) AS NVARCHAR(MAX)) + ')' 
			FROM dbo.BackupStatus BSA
			WHERE BSA.InstanceID=BS.InstanceID
			AND BSA.LogBackupExcludedReason IS NULL 
			GROUP BY BSA.LogCompressionAlgorithm
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)')
		,1,2,'') AS LogCompressionAlgorithms,
	BS.ShowInSummary
FROM dbo.BackupStatus BS
LEFT JOIN dbo.BackupThresholds T ON BS.InstanceID = T.InstanceID AND T.DatabaseID = -1
GROUP BY BS.InstanceID,
	BS.Instance,
	BS.InstanceDisplayName,
	T.InstanceID,
	BS.ShowInSummary