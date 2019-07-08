
CREATE VIEW [dbo].[BackupStatus]
AS
WITH B AS(
	SELECT DatabaseID,
			MAX(CASE WHEN type='D' THEN LastBackup ELSE NULL END) AS LastFull,
			MAX(CASE WHEN type='I' THEN LastBackup ELSE NULL END) AS LastDiff,
			MAX(CASE WHEN type='L' THEN LastBackup ELSE NULL END) AS LastLog,
			MAX(CASE WHEN type='F' THEN LastBackup ELSE NULL END) AS LastFG,
			MAX(CASE WHEN type='G' THEN LastBackup ELSE NULL END) AS LastFGDiff,
			MAX(CASE WHEN type='P' THEN LastBackup ELSE NULL END) AS LastPartial,
			MAX(CASE WHEN type='P' THEN LastBackup ELSE NULL END) AS LastPartialDiff
	FROM dbo.Backups
	GROUP BY DatabaseID
)
SELECT I.InstanceID,
	d.DatabaseID,
	I.Instance,
	d.name,
	D.recovery_model,
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
	cfg.[LogBackupWarningThreshold],
    cfg.[LogBackupCriticalThreshold],
    cfg.[FullBackupWarningThreshold],
    cfg.[FullBackupCriticalThreshold],
    cfg.[DiffBackupWarningThreshold],
    cfg.[DiffBackupCriticalThreshold],
    cfg.[ConsiderPartialBackups],
    cfg.[ConsiderFGBackups]
FROM dbo.Databases d 
JOIN dbo.Instances I ON d.InstanceID = I.InstanceID
LEFT JOIN B ON d.DatabaseID = B.DatabaseID
OUTER APPLY(SELECT TOP(1) T.* 
			FROM dbo.BackupThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY InstanceID DESC,DatabaseID DESC
			) cfg
OUTER APPLY(SELECT DATEDIFF(mi,CASE WHEN cfg.ConsiderFGBackups=1 AND (LastFG> LastFull OR LastFull IS NULL) AND (cfg.ConsiderPartialBackups=0 OR LastPartial<LastFG OR LastPartial IS NULL) THEN LastFG
					WHEN cfg.ConsiderPartialBackups=1 AND (LastPartial>LastFull OR LastFull IS NULL) THEN LastPartial
					ELSE LastFull END,GETUTCDATE()) AS FullBackupAge,
					DATEDIFF(mi,CASE WHEN cfg.ConsiderFGBackups=1 AND (LastFGDiff> LastDiff OR LastDiff IS NULL) AND (cfg.ConsiderPartialBackups=0 OR LastPartialDiff<LastFGDiff OR LastPartialDiff IS NULL) THEN LastFGDiff
					WHEN cfg.ConsiderPartialBackups=1 AND (LastPartialDiff>LastFull OR LastDiff IS NULL) THEN LastPartialDiff
					ELSE LastDiff END,GETUTCDATE()) DiffBackupAge,DATEDIFF(mi,LastLog,GETUTCDATE()) LogBackupAge) f
OUTER APPLY(SELECT CASE WHEN ISNULL(f.FullBackupAge,cfg.FullBackupCriticalThreshold) >= cfg.FullBackupCriticalThreshold THEN 1
	WHEN f.FullBackupAge>=cfg.FullBackupWarningThreshold THEN 2
	WHEN cfg.FullBackupWarningThreshold IS NULL AND cfg.FullBackupCriticalThreshold IS NULL THEN 3
	ELSE 4 END AS FullBackupStatus,
	CASE WHEN d.name='master' THEN 4
	WHEN ISNULL(f.DiffBackupAge,cfg.DiffBackupCriticalThreshold) >= cfg.DiffBackupCriticalThreshold THEN 1
	WHEN f.DiffBackupAge>=cfg.DiffBackupWarningThreshold THEN 2
	WHEN cfg.DiffBackupWarningThreshold IS NULL AND cfg.DiffBackupCriticalThreshold IS NULL THEN 3
	ELSE 4 END AS DiffBackupStatus,
	CASE WHEN d.recovery_model=3 THEN 4
	WHEN ISNULL(f.LogBackupAge,cfg.LogBackupCriticalThreshold) >= cfg.LogBackupCriticalThreshold THEN 1
	WHEN f.LogBackupAge>=cfg.LogBackupWarningThreshold THEN 2
	WHEN cfg.LogBackupWarningThreshold IS NULL AND cfg.LogBackupCriticalThreshold IS NULL THEN 3
	ELSE 4 END AS LogBackupStatus) AS chk
WHERE d.IsActive=1
AND d.source_database_id IS NULL
AND d.state=0
AND d.is_in_standby=0
AND I.IsActive=1
AND d.name<>'tempdb'
