﻿CREATE VIEW dbo.LogShippingStatus
AS
SELECT I.InstanceID,
	D.DatabaseID,
	I.Instance,
	I.InstanceDisplayName,
	D.name,
	LR.restore_date,
	utc.restore_date_utc,
	LR.backup_start_date,
	utc.backup_start_date_utc,
	l.TimeSinceLast,
	TimeSinceLastHD.HumanDuration AS TimeSinceLastDuration,
	l.LatencyOfLast,
	LatencyOfLastHD.HumanDuration as LatencyOfLastDuration,
	l.TotalTimeBehind,
	TotalTimeBehindHD.HumanDuration AS TotalTimeBehindDuration,
	l.SnapshotAge,
	SnapshotAgeHD.HumanDuration AS SnapshotAgeDuration,
	SSD.SnapshotDate,
	SSD.Status AS SnapshotAgeStatus,
	chk.Status,
	CASE WHEN utc.create_date_utc > DATEADD(mi,-cfg.NewDatabaseExcludePeriodMin,GETUTCDATE()) THEN 'N/A (New Database)' WHEN chk.Status = 1 THEN 'Critical' WHEN chk.Status = 2 THEN 'Warning' WHEN chk.Status = 3 THEN 'N/A' WHEN chk.Status = 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
	LR.last_file,
	D.state_desc,
	CASE WHEN cfg.InstanceID=D.InstanceID AND cfg.DatabaseID=D.DatabaseID THEN 'Database' WHEN cfg.InstanceID = D.InstanceID THEN 'Instance' ELSE 'Root' END AS ThresholdConfiguredLevel,
	I.ShowInSummary
FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
JOIN dbo.CollectionDatesStatus SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='LogRestores'
LEFT JOIN dbo.LogRestores LR ON LR.DatabaseID = D.DatabaseID AND LR.InstanceID = D.InstanceID
OUTER APPLY(SELECT TOP(1) T.* 
			FROM dbo.LogRestoreThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY InstanceID DESC,DatabaseID DESC
			) cfg
OUTER APPLY(SELECT DATEADD(mi,ISNULL(NULLIF(-LR.backup_time_zone,127)*15,I.UTCOffset),LR.backup_start_date) AS backup_start_date_utc,
				DATEADD(mi,I.UTCOffset,LR.restore_date) AS restore_date_utc,
				DATEADD(mi,I.UTCOffset,D.create_date) AS create_date_utc
				) AS utc
OUTER APPLY(SELECT DATEDIFF_BIG(s,utc.restore_date_utc,GETUTCDATE())/60.0 AS TimeSinceLast,
					DATEDIFF_BIG(s,utc.backup_start_date_utc,restore_date_utc)/60.0 AS LatencyOfLast,
					DATEDIFF_BIG(s,utc.backup_start_date_utc,GETUTCDATE())/60.0 AS TotalTimeBehind,
					DATEDIFF_BIG(s,SSD.SnapshotDate,GETUTCDATE())/60.0 AS SnapshotAge) l
OUTER APPLY dbo.SecondsToHumanDuration(l.TimeSinceLast*60.0) AS TimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(l.LatencyOfLast*60.0) AS LatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(l.TotalTimeBehind*60.0) AS TotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(l.SnapshotAge*60.0) AS SnapshotAgeHD
OUTER APPLY(SELECT CASE WHEN utc.create_date_utc > DATEADD(mi,-cfg.NewDatabaseExcludePeriodMin,GETUTCDATE()) THEN 3
	WHEN l.TimeSinceLast >cfg.TimeSinceLastCriticalThreshold THEN 1
	WHEN l.TimeSinceLast IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast IS NULL AND cfg.LatencyCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast > cfg.LatencyCriticalThreshold THEN 1
	WHEN l.TimeSinceLast >cfg.TimeSinceLastWarningThreshold THEN 2
	WHEN l.LatencyOfLast > cfg.LatencyWarningThreshold THEN 2
	WHEN cfg.LatencyCriticalThreshold IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NULL AND cfg.LatencyWarningThreshold IS NULL AND cfg.TimeSinceLastWarningThreshold IS NULL  THEN 3
	ELSE 4 END AS Status) chk
WHERE (D.state =1 OR D.is_in_standby=1)
AND D.IsActive=1
AND I.IsActive=1
AND D.recovery_model<>3
AND NOT EXISTS(SELECT 1 FROM dbo.DatabaseMirroring DM WHERE DM.DatabaseID = D.DatabaseID AND DM.InstanceID = D.InstanceID)