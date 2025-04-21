CREATE VIEW dbo.LogShippingStatusSummary 
AS
WITH Summary AS (
	SELECT LSS.InstanceID,
		   LSS.InstanceDisplayName,
		   MIN(LSS.Status) AS Status,
		   CASE MIN(LSS.Status) WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
		   COUNT(*) AS LogShippedDBCount,
		   SUM(CASE WHEN LSS.Status=2 THEN 1 ELSE 0 END) AS WarningCount,
		   SUM(CASE WHEN LSS.Status=1 THEN 1 ELSE 0 END) AS CriticalCount,
		   MAX(LSS.TotalTimeBehind) MaxTotalTimeBehind,
		   MIN(LSS.TotalTimeBehind) MinTotalTimeBehind,
		   AVG(LSS.TotalTimeBehind) AvgTotalTimeBehind,
		   MAX(LSS.LatencyOfLast) MaxLatencyOfLast,
		   MIN(LSS.LatencyOfLast) MinLatencyOfLast,
		   AVG(LSS.LatencyOfLast) AvgLatencyOfLast,
		   MAX(LSS.TimeSinceLast) AS MaxTimeSinceLast,
		   MIN(LSS.TimeSinceLast) AS MinTimeSinceLast,
		   AVG(LSS.TimeSinceLast) AS AvgTimeSinceLast,
		   MAX(LSS.SnapshotAge) AS SnapshotAge,
		   ISNULL(MIN(NULLIF(LSS.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
		   MIN(LSS.backup_start_date_utc) MinDateOfLastBackupRestored,
		   MAX(LSS.backup_start_date_utc) MaxDateOfLastBackupRestored,
		   MIN(LSS.restore_date_utc) MinLastRestoreCompleted,
		   MAX(LSS.restore_date_utc) MaxLastRestoreCompleted,
		   CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS InstanceLevelThreshold,
		   SUM(CASE WHEN LSS.ThresholdConfiguredLevel='Database' THEN 1 ELSE 0 END) AS DatabaseLevelThresholds,
		   LSS.ShowInSummary
	FROM dbo.LogShippingStatus LSS
	LEFT JOIN dbo.LogRestoreThresholds T ON LSS.InstanceID = T.InstanceID AND T.DatabaseID=-1
	WHERE LSS.Status<> 3
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
		ShowInSummary
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