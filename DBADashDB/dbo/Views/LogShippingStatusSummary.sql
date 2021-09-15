CREATE VIEW dbo.LogShippingStatusSummary 
AS
SELECT LSS.InstanceID,
       LSS.Instance,
	   MIN(LSS.Status) AS Status,
	   CASE MIN(LSS.Status) WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
	   COUNT(*) AS LogShippedDBCount,
	   SUM(CASE WHEN LSS.Status=2 THEN 1 ELSE 0 END) AS WarningCount,
	   SUM(CASE WHEN LSS.Status=1 THEN 1 ELSE 0 END) AS CriticalCount,
	   MAX(LSS.TotalTimeBehind) MaxTotalTimeBehind,
	   MAX(LSS.LatencyOfLast) MaxLatencyOfLast,
	   MAX(LSS.TimeSinceLast) AS TimeSinceLast,
	   MAX(LSS.SnapshotAge) AS SnapshotAge,
	   ISNULL(MIN(NULLIF(LSS.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
	   MIN(LSS.backup_start_date_utc) MinDateOfLastBackupRestored,
	   MIN(LSS.restore_date_utc) MinLastRestoreCompleted,
	   CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END as InstanceLevelThreshold,
	   SUM(CASE WHEN LSS.ThresholdConfiguredLevel='Database' THEN 1 ELSE 0 END) as DatabaseLevelThresholds
FROM dbo.LogShippingStatus LSS
LEFT JOIN dbo.LogRestoreThresholds T ON LSS.InstanceID = T.InstanceID AND T.DatabaseID=-1
WHERE LSS.Status<> 3
GROUP BY LSS.InstanceID,LSS.Instance,T.InstanceID