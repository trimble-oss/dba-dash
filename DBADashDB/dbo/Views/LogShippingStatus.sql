CREATE VIEW dbo.LogShippingStatus
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
	CASE WHEN excl.DatabaseName IS NOT NULL THEN 'N/A (Excluded)' WHEN nd.IsNewDatabase=1 THEN 'N/A (New Database)' WHEN chk.Status = 1 THEN 'Critical' WHEN chk.Status = 2 THEN 'Warning' WHEN chk.Status = 3 THEN 'N/A' WHEN chk.Status = 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
	LR.last_file,
	D.state_desc,
	CASE WHEN cfg.InstanceID=D.InstanceID AND cfg.DatabaseID=D.DatabaseID THEN 'Database' WHEN cfg.InstanceID = D.InstanceID THEN 'Instance' ELSE 'Root' END AS ThresholdConfiguredLevel,
	I.ShowInSummary,
	CAST(0 AS BIT) AS IsMissing,
	pri.PrimaryInstanceID,
	pri.PrimaryInstanceDisplayName,
	CASE WHEN excl.DatabaseName IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsExcluded,
	CASE WHEN excl.DatabaseName IS NULL THEN NULL ELSE CONCAT(excl.DatabaseName,CASE WHEN excl.InstanceID = -1 THEN ' (Root)' ELSE ' (Instance)' END) END AS ExcludedBy,
	nd.IsNewDatabase
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
/* Database exclusions at instance or root level.  Only % is treated as a wildcard. [ and _ are escaped as they are valid in database names */
OUTER APPLY(SELECT TOP(1) X.InstanceID,
					X.DatabaseName
			FROM dbo.LogShippingExclusions X
			WHERE (X.InstanceID = D.InstanceID OR X.InstanceID = -1)
			AND D.name LIKE REPLACE(REPLACE(X.DatabaseName,'[','[[]'),'_','[_]')
			ORDER BY X.InstanceID DESC,X.DatabaseName
			) excl
/* Created within the new database exclude period.  Excluded databases take precedence */
OUTER APPLY(SELECT CASE WHEN excl.DatabaseName IS NULL AND utc.create_date_utc > DATEADD(mi,-cfg.NewDatabaseExcludePeriodMin,GETUTCDATE()) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsNewDatabase) nd
OUTER APPLY(SELECT CASE WHEN excl.DatabaseName IS NOT NULL THEN 3
	WHEN nd.IsNewDatabase=1 THEN 3
	WHEN l.TimeSinceLast >cfg.TimeSinceLastCriticalThreshold THEN 1
	WHEN l.TimeSinceLast IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast IS NULL AND cfg.LatencyCriticalThreshold IS NOT NULL THEN 1
	WHEN l.LatencyOfLast > cfg.LatencyCriticalThreshold THEN 1
	WHEN l.TimeSinceLast >cfg.TimeSinceLastWarningThreshold THEN 2
	WHEN l.LatencyOfLast > cfg.LatencyWarningThreshold THEN 2
	WHEN cfg.LatencyCriticalThreshold IS NULL AND cfg.TimeSinceLastCriticalThreshold IS NULL AND cfg.LatencyWarningThreshold IS NULL AND cfg.TimeSinceLastWarningThreshold IS NULL  THEN 3
	ELSE 4 END AS Status) chk
/* Primary instance for the database if the instance is configured as a secondary in dbo.LogShippingPairs */
OUTER APPLY(SELECT TOP(1) P.PrimaryInstanceID,
					PI.InstanceDisplayName AS PrimaryInstanceDisplayName
			FROM dbo.LogShippingPairs P
			JOIN dbo.Instances PI ON P.PrimaryInstanceID = PI.InstanceID
			JOIN dbo.Databases PD ON PD.InstanceID = P.PrimaryInstanceID AND PD.name = D.name
			WHERE P.SecondaryInstanceID = I.InstanceID
			AND PI.IsActive=1
			AND PD.IsActive=1
			ORDER BY PI.InstanceDisplayName
			) pri
WHERE (D.state =1 OR D.is_in_standby=1)
AND D.IsActive=1
AND I.IsActive=1
AND D.recovery_model<>3
AND NOT EXISTS(SELECT 1 FROM dbo.DatabaseMirroring DM WHERE DM.DatabaseID = D.DatabaseID AND DM.InstanceID = D.InstanceID)
UNION ALL
/*
	Databases on the primary instance of a configured primary/secondary pair (dbo.LogShippingPairs) that are not in a
	restoring/standby state on the secondary.  Reported against the secondary instance.
	System databases, snapshots, databases not online on the primary and databases in SIMPLE recovery are ignored.
	Excluded databases (dbo.LogShippingExclusions) are reported with N/A status.
*/
SELECT SI.InstanceID,
	NULL AS DatabaseID,
	SI.Instance,
	SI.InstanceDisplayName,
	PD.name,
	NULL AS restore_date,
	NULL AS restore_date_utc,
	NULL AS backup_start_date,
	NULL AS backup_start_date_utc,
	NULL AS TimeSinceLast,
	NULL AS TimeSinceLastDuration,
	NULL AS LatencyOfLast,
	NULL AS LatencyOfLastDuration,
	NULL AS TotalTimeBehind,
	NULL AS TotalTimeBehindDuration,
	l.SnapshotAge,
	SnapshotAgeHD.HumanDuration AS SnapshotAgeDuration,
	SSD.SnapshotDate,
	SSD.Status AS SnapshotAgeStatus,
	chk.Status,
	CASE WHEN excl.DatabaseName IS NOT NULL THEN 'Missing (Excluded)' WHEN nd.IsNewDatabase=1 THEN 'Missing (New Database)' ELSE 'Missing' END AS StatusDescription,
	NULL AS last_file,
	SD.state_desc,
	CASE WHEN cfg.InstanceID = SI.InstanceID THEN 'Instance' ELSE 'Root' END AS ThresholdConfiguredLevel,
	SI.ShowInSummary,
	CAST(1 AS BIT) AS IsMissing,
	P.PrimaryInstanceID,
	PI.InstanceDisplayName AS PrimaryInstanceDisplayName,
	CASE WHEN excl.DatabaseName IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsExcluded,
	CASE WHEN excl.DatabaseName IS NULL THEN NULL ELSE CONCAT(excl.DatabaseName,CASE WHEN excl.InstanceID = -1 THEN ' (Root)' ELSE ' (Instance)' END) END AS ExcludedBy,
	nd.IsNewDatabase
FROM dbo.LogShippingPairs P
JOIN dbo.Instances PI ON P.PrimaryInstanceID = PI.InstanceID
JOIN dbo.Instances SI ON P.SecondaryInstanceID = SI.InstanceID
JOIN dbo.Databases PD ON PD.InstanceID = P.PrimaryInstanceID
LEFT JOIN dbo.CollectionDatesStatus SSD ON SSD.InstanceID = SI.InstanceID AND SSD.Reference='LogRestores'
/* Database with the same name on the secondary (name is unique per instance) */
LEFT JOIN dbo.Databases SD ON SD.InstanceID = SI.InstanceID AND SD.name = PD.name AND SD.IsActive=1
OUTER APPLY(SELECT TOP(1) T.*
			FROM dbo.LogRestoreThresholds T
			WHERE (T.InstanceID = SI.InstanceID OR T.InstanceID = -1)
			AND T.DatabaseID = -1
			ORDER BY T.InstanceID DESC
			) cfg
OUTER APPLY(SELECT DATEADD(mi,PI.UTCOffset,PD.create_date) AS create_date_utc) AS utc
OUTER APPLY(SELECT DATEDIFF_BIG(s,SSD.SnapshotDate,GETUTCDATE())/60.0 AS SnapshotAge) l
OUTER APPLY dbo.SecondsToHumanDuration(l.SnapshotAge*60.0) AS SnapshotAgeHD
/* Database exclusions at instance or root level.  Only % is treated as a wildcard. [ and _ are escaped as they are valid in database names */
OUTER APPLY(SELECT TOP(1) X.InstanceID,
					X.DatabaseName
			FROM dbo.LogShippingExclusions X
			WHERE (X.InstanceID = SI.InstanceID OR X.InstanceID = -1)
			AND PD.name LIKE REPLACE(REPLACE(X.DatabaseName,'[','[[]'),'_','[_]')
			ORDER BY X.InstanceID DESC,X.DatabaseName
			) excl
/* Created within the new database exclude period.  Excluded databases take precedence */
OUTER APPLY(SELECT CASE WHEN excl.DatabaseName IS NULL AND utc.create_date_utc > DATEADD(mi,-cfg.NewDatabaseExcludePeriodMin,GETUTCDATE()) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsNewDatabase) nd
OUTER APPLY(SELECT CASE WHEN excl.DatabaseName IS NOT NULL THEN 3
					WHEN nd.IsNewDatabase=1 THEN 3
					ELSE 1 END AS Status) chk
WHERE PI.IsActive=1
AND SI.IsActive=1
AND PD.IsActive=1
AND PD.database_id > 4
AND PD.source_database_id IS NULL
AND PD.state = 0
AND ISNULL(PD.is_in_standby,0) = 0
AND PD.recovery_model<>3
/* Missing from the secondary or not restoring/in standby there (e.g. ONLINE) */
AND NOT (ISNULL(SD.state,0) = 1 OR ISNULL(SD.is_in_standby,0) = 1)