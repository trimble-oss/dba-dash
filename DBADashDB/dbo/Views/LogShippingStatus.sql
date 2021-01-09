
CREATE VIEW [dbo].[LogShippingStatus] 
AS
SELECT I.InstanceID,
	D.DatabaseID,
	I.Instance,
	D.name,
	LR.restore_date,
	LR.backup_start_date,
	l.TimeSinceLast,
	l.LatencyOfLast,
	l.TotalTimeBehind,
	DATEDIFF(mi,SSD.SnapshotDate,GETUTCDATE()) AS SnapshotAge,
	SSD.SnapshotDate AS LogRestoresDate,
	chk.Status,
	CASE chk.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' END AS StatusDescription,
	LR.last_file,
	D.state_desc,
	CASE WHEN cfg.InstanceID=D.InstanceID AND cfg.DatabaseID=D.DatabaseID THEN 'Database' WHEN cfg.InstanceID = D.InstanceID THEN 'Instance' ELSE 'Root' END AS ThresholdConfiguredLevel
FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
JOIN dbo.CollectionDates SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='LogRestores'
LEFT JOIN dbo.LogRestores LR ON LR.DatabaseID = D.DatabaseID
OUTER APPLY(SELECT TOP(1) T.* 
			FROM dbo.LogRestoreThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY InstanceID DESC,DatabaseID DESC
			) cfg
OUTER APPLY(SELECT DATEDIFF(mi,restore_date,GETUTCDATE()) AS TimeSinceLast,
					DATEDIFF(mi,backup_start_date,restore_date) AS LatencyOfLast,
					DATEDIFF(mi,backup_start_date,GETUTCDATE()) AS TotalTimeBehind) l
OUTER APPLY(SELECT CASE WHEN l.TimeSinceLast >cfg.TimeSinceLastCriticalThreshold THEN 1
	WHEN l.TimeSinceLast IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast IS NULL AND cfg.LatencyCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast> cfg.LatencyCriticalThreshold THEN 1
	WHEN l.TimeSinceLast >cfg.TimeSinceLastWarningThreshold THEN 2
	WHEN l.LatencyOfLast > cfg.LatencyWarningThreshold THEN 2
	WHEN cfg.LatencyCriticalThreshold IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NULL AND cfg.LatencyWarningThreshold IS NULL AND cfg.TimeSinceLastWarningThreshold IS NULL  THEN 3
	ELSE 4 END AS Status) chk
WHERE (D.state =1 OR D.is_in_standby=1)
AND D.IsActive=1
AND I.IsActive=1
AND D.create_date < DATEADD(d,-1,GETUTCDATE())
