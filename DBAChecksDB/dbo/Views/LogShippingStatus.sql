CREATE VIEW LogShippingStatus 
AS
SELECT I.InstanceID,
	I.Instance,
	D.name,
	LR.restore_date,
	LR.backup_start_date,
	l.TimeSinceLast,
	l.LatencyOfLast,
	l.TotalTimeBehind,
	DATEDIFF(mi,I.LogRestoreSnapshotDate,GETUTCDATE()) as SnapshotAge,
	I.LogRestoreSnapshotDate,
	chk.Status,
	CASE chk.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' END as StatusDescription
	FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.LogRestores LR ON LR.DatabaseID = D.DatabaseID
OUTER APPLY(SELECT TOP(1) T.* 
			FROM dbo.LogRestoreThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY InstanceID DESC,DatabaseID DESC
			) cfg
OUTER APPLY(SELECT DATEDIFF(mi,restore_date,GETUTCDATE()) as TimeSinceLast,
					DATEDIFF(mi,backup_start_date,restore_date) as LatencyOfLast,
					DATEDIFF(mi,backup_start_date,GETUTCDATE()) as TotalTimeBehind) l
OUTER APPLY(SELECT CASE WHEN l.TimeSinceLast >cfg.TimeSinceLastCriticalThreshold THEN 1
	WHEN l.LatencyOfLast> cfg.LatencyCriticalThreshold THEN 1
	WHEN l.TimeSinceLast IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast IS NULL AND cfg.LatencyCriticalThreshold IS NOT NULL THEN 1
	WHEN l.TimeSinceLast >cfg.TimeSinceLastWarningThreshold THEN 2
	WHEN l.LatencyOfLast > cfg.LatencyWarningThreshold THEN 2
	WHEN cfg.LatencyCriticalThreshold IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NULL AND cfg.LatencyWarningThreshold IS NULL AND cfg.TimeSinceLastWarningThreshold IS NULL  THEN 3
	ELSE 4 END as Status) chk
WHERE (D.state=1 OR D.is_in_standby=1)
