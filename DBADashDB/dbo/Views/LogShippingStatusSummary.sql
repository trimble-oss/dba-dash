CREATE VIEW dbo.LogShippingStatusSummary 
AS
WITH Summary AS (
	SELECT LSS.InstanceID,
		   LSS.InstanceDisplayName,
		   ISNULL(MIN(NULLIF(LSS.Status,3)),3) AS Status,
		   CASE ISNULL(MIN(NULLIF(LSS.Status,3)),3) WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
		   SUM(CASE WHEN LSS.IsMissing=0 AND LSS.IsNewDatabase=0 THEN 1 ELSE 0 END) AS LogShippedDBCount,
		   SUM(CASE WHEN LSS.Status=2 THEN 1 ELSE 0 END) AS WarningCount,
		   SUM(CASE WHEN LSS.Status=1 THEN 1 ELSE 0 END) AS CriticalCount,
		   MAX(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TotalTimeBehind END) MaxTotalTimeBehind,
		   MIN(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TotalTimeBehind END) MinTotalTimeBehind,
		   AVG(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TotalTimeBehind END) AvgTotalTimeBehind,
		   MAX(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.LatencyOfLast END) MaxLatencyOfLast,
		   MIN(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.LatencyOfLast END) MinLatencyOfLast,
		   AVG(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.LatencyOfLast END) AvgLatencyOfLast,
		   MAX(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TimeSinceLast END) AS MaxTimeSinceLast,
		   MIN(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TimeSinceLast END) AS MinTimeSinceLast,
		   AVG(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.TimeSinceLast END) AS AvgTimeSinceLast,
		   MAX(LSS.SnapshotAge) AS SnapshotAge,
		   ISNULL(MIN(NULLIF(LSS.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
		   MIN(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.backup_start_date_utc END) MinDateOfLastBackupRestored,
		   MAX(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.backup_start_date_utc END) MaxDateOfLastBackupRestored,
		   MIN(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.restore_date_utc END) MinLastRestoreCompleted,
		   MAX(CASE WHEN LSS.IsNewDatabase=0 THEN LSS.restore_date_utc END) MaxLastRestoreCompleted,
		   CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS InstanceLevelThreshold,
		   SUM(CASE WHEN LSS.ThresholdConfiguredLevel='Database' AND LSS.IsNewDatabase=0 THEN 1 ELSE 0 END) AS DatabaseLevelThresholds,
		   LSS.ShowInSummary,
		   SUM(CASE WHEN LSS.IsMissing=1 AND LSS.IsNewDatabase=0 THEN 1 ELSE 0 END) AS MissingCount,
		   SUM(CASE WHEN LSS.IsNewDatabase=1 THEN 1 ELSE 0 END) AS NewDatabaseCount
	FROM dbo.LogShippingStatus LSS
	LEFT JOIN dbo.LogRestoreThresholds T ON LSS.InstanceID = T.InstanceID AND T.DatabaseID=-1
	/* New databases (N/A) are included for NewDatabaseCount only.  Other N/A databases are ignored */
	WHERE (LSS.Status<> 3 OR LSS.IsNewDatabase=1)
	GROUP BY LSS.InstanceID,
			 LSS.InstanceDisplayName,
			 T.InstanceID,
			 LSS.ShowInSummary
)
SELECT	InstanceID,
		InstanceDisplayName,
		Status,
		StatusDescription,
		LogShippedDBCount,
		WarningCount,
		CriticalCount,
		MaxTotalTimeBehind,
		MaxTotalTimeBehindHD.HumanDuration AS MaxTotalTimeBehindDuration,
		MinTotalTimeBehind,
		MinTotalTimeBehindHD.HumanDuration AS MinTotalTimeBehindDuration,
		AvgTotalTimeBehind,
		AvgTotalTimeBehindHD.HumanDuration AS AvgTotalTimeBehindDuration,
		MaxLatencyOfLast,
		MaxLatencyOfLastHD.HumanDuration AS MaxLatencyOfLastDuration,
		MinLatencyOfLast,
		MinLatencyOfLastHD.HumanDuration AS MinLatencyOfLastDuration,
		AvgLatencyOfLast,
		AvgLatencyOfLastHD.HumanDuration AS AvgLatencyOfLastDuration,
		MaxTimeSinceLast,
		MaxTimeSinceLastHD.HumanDuration AS MaxTimeSinceLastDuration,
		MinTimeSinceLast,
		MinTimeSinceLastHD.HumanDuration AS MinTimeSinceLastDuration,
		AvgTimeSinceLast,
		AvgTimeSinceLastHD.HumanDuration AS AvgTimeSinceLastDuration,
		SnapshotAge,
		SnapshotAgeHD.HumanDuration AS SnapshotAgeDuration,
		SnapshotAgeStatus,
		MinDateOfLastBackupRestored,
		MaxDateOfLastBackupRestored,
		MinLastRestoreCompleted,
		MaxLastRestoreCompleted,
		InstanceLevelThreshold,
		DatabaseLevelThresholds,
		ShowInSummary,
		MissingCount,
		NewDatabaseCount
FROM Summary
OUTER APPLY dbo.SecondsToHumanDuration(MaxTimeSinceLast*60.0) AS MaxTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(MinTimeSinceLast*60.0) AS MinTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(AvgTimeSinceLast*60.0) AS AvgTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(MaxLatencyOfLast*60.0) AS MaxLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(MinLatencyOfLast*60.0) AS MinLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(AvgLatencyOfLast*60.0) AS AvgLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(MaxTotalTimeBehind*60.0) AS MaxTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(MinTotalTimeBehind*60.0) AS MinTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(AvgTotalTimeBehind*60.0) AS AvgTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(SnapshotAge*60.0) AS SnapshotAgeHD