CREATE VIEW dbo.BackupStatus
AS
WITH B AS(
	SELECT B.DatabaseID,
			MAX(CASE WHEN B.type='D' THEN B.backup_start_date_utc ELSE NULL END) AS LastFull,
			MAX(CASE WHEN B.type='I' THEN B.backup_start_date_utc ELSE NULL END) AS LastDiff,
			MAX(CASE WHEN B.type='L' THEN B.backup_start_date_utc ELSE NULL END) AS LastLog,
			MAX(CASE WHEN B.type='F' THEN B.backup_start_date_utc ELSE NULL END) AS LastFG,
			MAX(CASE WHEN B.type='G' THEN B.backup_start_date_utc ELSE NULL END) AS LastFGDiff,
			MAX(CASE WHEN B.type='P' THEN B.backup_start_date_utc ELSE NULL END) AS LastPartial,
			MAX(CASE WHEN B.type='Q' THEN B.backup_start_date_utc ELSE NULL END) AS LastPartialDiff,
			MAX(CASE WHEN B.type='D' THEN B.BackupDuration ELSE NULL END) AS LastFullDuration,
			MAX(CASE WHEN B.type='D' THEN B.BackupDurationSec ELSE NULL END) AS LastFullDurationSec,
			MAX(CASE WHEN B.type='D' THEN B.BackupMBsec ELSE NULL END) AS FullBackupMBsec,
			MAX(CASE WHEN B.type='D' THEN B.BackupWriteMBsec ELSE NULL END) AS FullBackupWriteMBsec,
			MAX(CASE WHEN B.type='D' THEN B.BackupSizeGB ELSE NULL END) AS FullBackupSizeGB,
			MAX(CASE WHEN B.type='D' THEN B.BackupSizeCompressedGB ELSE NULL END) AS FullBackupSizeCompressedGB,
			MAX(CASE WHEN B.type='D' THEN B.CompressionSavingPct ELSE NULL END) AS FullCompressionSavingPct,
			MAX(CASE WHEN B.type='I' THEN B.BackupDuration ELSE NULL END) AS LastDiffDuration,
			MAX(CASE WHEN B.type='I' THEN B.BackupDurationSec ELSE NULL END) AS LastDiffDurationSec,
			MAX(CASE WHEN B.type='I' THEN B.BackupMBsec ELSE NULL END) AS DiffBackupMBsec,
			MAX(CASE WHEN B.type='I' THEN B.BackupWriteMBsec ELSE NULL END) AS DiffBackupWriteMBsec,
			MAX(CASE WHEN B.type='I' THEN B.BackupSizeGB ELSE NULL END) AS DiffBackupSizeGB,
			MAX(CASE WHEN B.type='I' THEN B.BackupSizeCompressedGB ELSE NULL END) AS DiffBackupSizeCompressedGB,
			MAX(CASE WHEN B.type='I' THEN B.CompressionSavingPct ELSE NULL END) AS DiffCompressionSavingPct,
			MAX(CASE WHEN B.type IN('D','F','P') THEN CAST(B.is_damaged AS INT) ELSE 0 END) IsFullDamaged,
			MAX(CASE WHEN B.type IN('I','G','Q') THEN CAST(B.is_damaged AS INT) ELSE 0 END) IsDiffDamaged,
			MAX(CASE WHEN B.type IN('L') THEN CAST(B.is_damaged AS INT) ELSE 0 END) IsLogDamaged,
			CAST(MAX(CASE WHEN B.type ='D' THEN CAST(B.has_backup_checksums AS INT) ELSE NULL END) AS BIT) as IsFullChecksum,
			CAST(MAX(CASE WHEN B.type ='I' THEN CAST(B.has_backup_checksums AS INT) ELSE NULL END) AS BIT) as IsDiffChecksum,
			CAST(MAX(CASE WHEN B.type ='L' THEN CAST(B.has_backup_checksums AS INT) ELSE NULL END) AS BIT) as IsLogChecksum,		
			CAST(MAX(CASE WHEN B.type ='D' THEN CAST(B.is_password_protected AS INT) ELSE NULL END) AS BIT) as IsFullPasswordProtected,
			CAST(MAX(CASE WHEN B.type ='I' THEN CAST(B.is_password_protected AS INT) ELSE NULL END) AS BIT) as IsDiffPasswordProtected,
			CAST(MAX(CASE WHEN B.type ='L' THEN CAST(B.is_password_protected AS INT) ELSE NULL END) AS BIT) as IsLogPasswordProtected,
			CAST(MAX(CASE WHEN B.type ='D' THEN CAST(B.IsEncrypted AS INT) ELSE NULL END) AS BIT) as IsFullEncrypted,
			CAST(MAX(CASE WHEN B.type ='I' THEN CAST(B.IsEncrypted AS INT) ELSE NULL END) AS BIT) as IsDiffEncrypted,
			CAST(MAX(CASE WHEN B.type ='L' THEN CAST(B.IsEncrypted AS INT) ELSE NULL END) AS BIT) as IsLogEncrypted,			
			CAST(MAX(CASE WHEN B.type ='D' THEN CAST(B.IsCompressed AS INT) ELSE NULL END) AS BIT) as IsFullCompressed,
			CAST(MAX(CASE WHEN B.type ='I' THEN CAST(B.IsCompressed AS INT) ELSE NULL END) AS BIT) as IsDiffCompressed,
			CAST(MAX(CASE WHEN B.type ='L' THEN CAST(B.IsCompressed AS INT) ELSE NULL END) AS BIT) as IsLogCompressed,	
			CASE WHEN MIN(CAST(B.is_snapshot AS INT))=1 THEN 'Y' WHEN MAX(CAST(B.is_snapshot AS INT))=1 THEN '(Y)' ELSE 'N' END AS SnapshotBackups,
			CAST(MAX(CAST(B.IsPartnerBackup AS INT)) AS BIT) AS IsPartnerBackup,
			MAX(CASE WHEN B.type ='D' THEN B.compression_algorithm ELSE NULL END) as FullCompressionAlgorithm,
			MAX(CASE WHEN B.type ='I' THEN B.compression_algorithm ELSE NULL END) as DiffCompressionAlgorithm,
			MAX(CASE WHEN B.type ='L' THEN B.compression_algorithm ELSE NULL END) as LogCompressionAlgorithm	
	FROM dbo.LastBackup B
	GROUP BY B.DatabaseID
)
SELECT I.InstanceID,
	d.DatabaseID,
	I.Instance,
	I.InstanceDisplayName,
	d.name,
	D.recovery_model,
	CASE D.recovery_model WHEN 1 THEN 'FULL' WHEN 2 THEN 'BULK_LOGGED' WHEN 3 THEN 'SIMPLE' ELSE '???' END AS recovery_model_desc,
	B.LastFull,
	B.LastDiff,
	B.LastLog,
	B.LastFG,
	B.LastFGDiff,
	B.LastPartial,
	B.LastPartialDiff,
	chk.FullBackupStatus,
	chk.LogBackupStatus,
	chk.DiffBackupStatus,
	CASE WHEN chk.FullBackupStatus=1 OR chk.DiffBackupStatus=1 OR chk.LogBackupStatus=1 THEN 1
		WHEN chk.FullBackupStatus=2 OR chk.DiffBackupStatus=2 OR chk.LogBackupStatus=2 THEN 2
		WHEN chk.FullBackupStatus=4 OR chk.DiffBackupStatus=4 OR chk.LogBackupStatus=4 THEN 4
		ELSE 3 END AS BackupStatus,
	cfg.LogBackupWarningThreshold,
    cfg.LogBackupCriticalThreshold,
    cfg.FullBackupWarningThreshold,
    cfg.FullBackupCriticalThreshold,
    cfg.DiffBackupWarningThreshold,
    cfg.DiffBackupCriticalThreshold,
    cfg.ConsiderPartialBackups,
    cfg.ConsiderFGBackups,
	CASE WHEN cfg.InstanceID = D.InstanceID AND cfg.DatabaseID = D.DatabaseID THEN 'Database' WHEN cfg.InstanceID=D.InstanceID THEN 'Instance' ELSE 'Root' END AS ThresholdsConfiguredLevel,
	SSD.SnapshotDate,
	HD.HumanDuration AS SnapshotAge,
	SSD.Status as SnapshotAgeStatus,
 	d.create_date,
	DATEADD(mi,I.UTCOffset,d.create_date) AS create_date_utc,
	I.UTCOffset,
    B.LastFullDuration,
    B.LastFullDurationSec,
    B.[FullBackupMBsec],
    B.FullBackupWriteMBsec,
    B.FullBackupSizeGB,
    B.FullBackupSizeCompressedGB,
	B.FullCompressionSavingPct,
    B.LastDiffDuration,
    B.LastDiffDurationSec,
    B.DiffBackupMBsec,
    B.DiffBackupWriteMBsec,
    B.DiffBackupSizeGB,
    B.DiffBackupSizeCompressedGB,
	B.DiffCompressionSavingPct,
	B.IsFullDamaged,
	B.IsDiffDamaged,
	B.IsLogDamaged,
	B.IsFullChecksum,
	B.IsDiffChecksum,
	B.IsLogChecksum,
	B.IsFullPasswordProtected,
	B.IsDiffPasswordProtected,
	B.IsLogPasswordProtected,
	B.IsFullCompressed,
	B.IsDiffCompressed,
	B.IsLogCompressed,
	B.IsFullEncrypted,
	B.IsDiffEncrypted,
	B.IsLogEncrypted,
	B.SnapshotBackups,
	B.IsPartnerBackup,
	f.FullBackupExcludedReason,
	f.DiffBackupExcludedReason,
	f.LogBackupExcludedReason,
	B.FullCompressionAlgorithm,
	B.DiffCompressionAlgorithm,
	B.LogCompressionAlgorithm,
	I.ShowInSummary
FROM dbo.Databases d 
JOIN dbo.Instances I ON d.InstanceID = I.InstanceID
JOIN dbo.CollectionDatesStatus SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='Backups'
LEFT JOIN B ON d.DatabaseID = B.DatabaseID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,SSD.SnapshotDate,GETUTCDATE())) HD
OUTER APPLY(SELECT TOP(1) T.*,
				CASE WHEN EXISTS(SELECT * FROM STRING_SPLIT(T.ExcludedDatabases,',') SS WHERE d.name LIKE SS.value) THEN CAST(1 AS BIT) ELSE CAST(0 as BIT) END as IsExcludedByName,
				CASE WHEN DATEADD(mi,I.UTCOffset,d.create_date) > DATEADD(mi,-T.MinimumAge,GETUTCDATE()) THEN CAST(1 AS BIT) ELSE CAST(0 as BIT) END IsExcludedByDate
			FROM dbo.BackupThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY InstanceID DESC,DatabaseID DESC
			) cfg
OUTER APPLY(SELECT STUFF(CONCAT(CASE WHEN d.source_database_id IS NOT NULL THEN ', Snapshot' ELSE NULL END,
								CASE WHEN d.state NOT IN(0,4,5) THEN ', ' + d.state_desc ELSE NULL END,
								CASE WHEN d.is_in_standby=1 THEN ', Standby' ELSE NULL END,
								CASE WHEN d.name = 'tempdb' THEN ', tempdb' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByName=1 THEN ', name' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByDate=1 THEN ', created_date' ELSE NULL END,
								CASE WHEN cfg.FullBackupWarningThreshold IS NULL AND cfg.FullBackupCriticalThreshold IS NULL THEN ', No Threshold' END
								),1,2,'') as FullBackupExcludedReason,
					STUFF(CONCAT(CASE WHEN d.source_database_id IS NOT NULL THEN ', Snapshot' ELSE NULL END,
								CASE WHEN d.state NOT IN(0,4,5) THEN ', ' + d.state_desc ELSE NULL END,
								CASE WHEN d.is_in_standby=1 THEN ', Standby' ELSE NULL END,
								CASE WHEN d.name = 'tempdb' THEN ', tempdb' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByName=1 THEN ', name' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByDate=1 THEN ', created_date' ELSE NULL END,
								CASE WHEN d.name = 'master' THEN ', master' ELSE NULL END,
								CASE WHEN cfg.DiffBackupWarningThreshold IS NULL AND cfg.DiffBackupCriticalThreshold IS NULL THEN ', No Threshold' END
								),1,2,'') as DiffBackupExcludedReason,
					STUFF(CONCAT(CASE WHEN d.source_database_id IS NOT NULL THEN ', Snapshot' ELSE NULL END,
								CASE WHEN d.state NOT IN(0,4,5) THEN ', ' + d.state_desc ELSE NULL END,
								CASE WHEN d.is_in_standby=1 THEN ', Standby' ELSE NULL END,
								CASE WHEN d.name = 'tempdb' THEN ', tempdb' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByName=1 THEN ', name' ELSE NULL END,
								CASE WHEN cfg.IsExcludedByDate=1 THEN ', created_date' ELSE NULL END,
								CASE WHEN d.recovery_model=3 THEN ', SIMPLE' ELSE NULL END,
								CASE WHEN cfg.LogBackupWarningThreshold IS NULL AND cfg.LogBackupCriticalThreshold IS NULL THEN ', No Threshold' END
								),1,2,'') as LogBackupExcludedReason,
					DATEDIFF(mi,
								CASE WHEN cfg.ConsiderFGBackups=1 AND (LastFG> LastFull OR LastFull IS NULL) AND (cfg.ConsiderPartialBackups=0 OR LastPartial<LastFG OR LastPartial IS NULL) THEN LastFG
									WHEN cfg.ConsiderPartialBackups=1 AND (LastPartial>LastFull OR LastFull IS NULL) THEN LastPartial
								ELSE LastFull END,
							GETUTCDATE()) AS FullBackupAge,
					DATEDIFF(mi,
								CASE WHEN cfg.ConsiderFGBackups=1 AND (LastFGDiff> LastDiff OR LastDiff IS NULL) AND (cfg.ConsiderPartialBackups=0 OR LastPartialDiff<LastFGDiff OR LastPartialDiff IS NULL) THEN LastFGDiff
									WHEN cfg.ConsiderPartialBackups=1 AND (LastPartialDiff>LastDiff OR LastDiff IS NULL) THEN LastPartialDiff
								ELSE LastDiff END,
							GETUTCDATE()) DiffBackupAge,
					DATEDIFF(mi,LastLog,GETUTCDATE()) LogBackupAge
			) f
OUTER APPLY(SELECT CASE WHEN f.FullBackupExcludedReason IS NOT NULL THEN 3
						WHEN B.IsFullDamaged=1 THEN 1
						WHEN ISNULL(f.FullBackupAge,cfg.FullBackupCriticalThreshold) >= cfg.FullBackupCriticalThreshold THEN 1
						WHEN f.FullBackupAge>=cfg.FullBackupWarningThreshold THEN 2
						ELSE 4 END AS FullBackupStatus,
					CASE WHEN f.DiffBackupExcludedReason IS NOT NULL THEN 3
						WHEN B.IsDiffDamaged=1 THEN 1
						WHEN ISNULL(f.DiffBackupAge,cfg.DiffBackupCriticalThreshold) >= cfg.DiffBackupCriticalThreshold THEN 1
						WHEN f.DiffBackupAge>=cfg.DiffBackupWarningThreshold THEN 2
					ELSE 4 END AS DiffBackupStatus,
					CASE WHEN LogBackupExcludedReason IS NOT NULL THEN 3
						WHEN B.IsLogDamaged=1 THEN 1
						WHEN ISNULL(f.LogBackupAge,cfg.LogBackupCriticalThreshold) >= cfg.LogBackupCriticalThreshold THEN 1
						WHEN f.LogBackupAge>=cfg.LogBackupWarningThreshold THEN 2
					ELSE 4 END AS LogBackupStatus
			) AS chk
WHERE d.IsActive=1
AND I.IsActive=1
AND I.EngineEdition NOT IN(5,8) -- Exclude AzureDB and Azure MI