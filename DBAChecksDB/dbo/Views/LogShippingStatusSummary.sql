CREATE VIEW LogShippingStatusSummary 
AS
SELECT InstanceID,
       Instance,
	   MIN(LSS.Status) AS Status,
	   CASE MIN(LSS.Status) WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' END AS StatusDescription,
	   COUNT(*) AS LogShippedDBCount,
	   SUM(CASE WHEN Status=2 THEN 1 ELSE 0 END) AS WarningCount,
	   SUM(CASE WHEN Status=1 THEN 1 ELSE 0 END) AS CriticalCount,
	   MAX(LSS.TotalTimeBehind) MaxTotalTimeBehind,
	   MAX(LSS.LatencyOfLast) MaxLatencyOfLast,
	   MAX(LSS.TimeSinceLast) AS TimeSinceLast,
	   MAX(LSS.SnapshotAge) AS SnapshotAge,
	   MIN(LSS.backup_start_date) MinDateOfLastBackupRestored,
	   MIN(LSS.restore_date) MinLastRestoreCompleted
FROM dbo.LogShippingStatus LSS
WHERE LSS.Status<> 3
GROUP BY LSS.InstanceID,LSS.Instance