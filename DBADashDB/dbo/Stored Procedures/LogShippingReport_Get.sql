CREATE PROC dbo.LogShippingReport_Get(
		@InstanceIDs IDs READONLY,
		@InstanceID INT=NULL,
		@IncludeCritical BIT=1,
		@IncludeWarning BIT=1,
		@IncludeNA BIT=0,
		@IncludeOK BIT=0,
		@ShowHidden BIT=1
)
AS
/*
	Combined report used by the Log Shipping tab (LogShippingView).
	Returns two result sets:
		0 - Instance summary (dbo.LogShippingStatusSummary)
		1 - Database detail   (dbo.LogShippingStatus)
	@InstanceID is supplied when drilling down from the summary to a single instance.
*/

-- Drilling down to a single instance mirrors the legacy single-instance view: show all databases including hidden ones.
IF @InstanceID IS NOT NULL SET @ShowHidden=1

DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceID IS NOT NULL
BEGIN
	INSERT INTO @Instances(InstanceID) VALUES(@InstanceID)
END
ELSE IF EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT ID FROM @InstanceIDs
END
ELSE
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT InstanceID FROM dbo.Instances WHERE IsActive=1
END

/* Result set 0: Instance summary */
SELECT LS.InstanceID,
       LS.InstanceDisplayName,
       LS.Status,
       LS.StatusDescription,
       LS.LogShippedDBCount,
       LS.WarningCount,
       LS.CriticalCount,
       LS.MaxTotalTimeBehind,
       LS.MaxTotalTimeBehindDuration,
       LS.MinTotalTimeBehind,
       LS.MinTotalTimeBehindDuration,
       LS.AvgTotalTimeBehind,
       LS.AvgTotalTimeBehindDuration,
       LS.MaxLatencyOfLast,
       LS.MaxLatencyOfLastDuration,
       LS.MinLatencyOfLast,
       LS.MinLatencyOfLastDuration,
       LS.AvgLatencyOfLast,
       LS.AvgLatencyOfLastDuration,
       LS.MaxTimeSinceLast,
       LS.MaxTimeSinceLastDuration,
       LS.MinTimeSinceLast,
       LS.MinTimeSinceLastDuration,
       LS.AvgTimeSinceLast,
       LS.AvgTimeSinceLastDuration,
       LS.SnapshotAge,
       LS.SnapshotAgeDuration,
       LS.SnapshotAgeStatus,
       LS.MinDateOfLastBackupRestored,
       LS.MaxDateOfLastBackupRestored,
       LS.MinLastRestoreCompleted,
       LS.MaxLastRestoreCompleted,
       LS.InstanceLevelThreshold,
       LS.DatabaseLevelThresholds,
       CAST('Configure' AS NVARCHAR(20)) AS Configure
FROM dbo.LogShippingStatusSummary LS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LS.InstanceID)
AND (LS.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY LS.Status,LS.MaxTotalTimeBehind DESC

/* Result set 1: Database detail */
;WITH Statuses AS(
	SELECT 1 AS Status
	WHERE @IncludeCritical=1
	UNION ALL
	SELECT 2
	WHERE @IncludeWarning=1
	UNION ALL
	SELECT 3
	WHERE @IncludeNA=1
	UNION ALL
	SELECT 4
	WHERE @IncludeOK=1
)
SELECT LSS.InstanceID,
       LSS.DatabaseID,
       LSS.InstanceDisplayName,
       LSS.name,
       LSS.restore_date_utc AS restore_date,
       LSS.backup_start_date_utc AS backup_start_date,
       LSS.TimeSinceLast,
       LSS.TimeSinceLastDuration,
       LSS.LatencyOfLast,
       LSS.LatencyOfLastDuration,
       LSS.TotalTimeBehind,
       LSS.TotalTimeBehindDuration,
       LSS.SnapshotAge,
       LSS.SnapshotAgeDuration,
       LSS.SnapshotDate,
       LSS.Status,
       LSS.StatusDescription,
       f.FileName AS last_file,
       LSS.ThresholdConfiguredLevel,
       CASE WHEN LSS.SnapshotAge>1440 THEN 1 WHEN LSS.SnapshotAge>120 THEN 2 WHEN LSS.SnapshotAge<60 THEN 4 ELSE 3 END AS SnapshotAgeStatus,
       CAST('Configure' AS NVARCHAR(20)) AS Configure
FROM dbo.LogShippingStatus LSS
CROSS APPLY dbo.ParseFileName(LSS.last_file) f
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LSS.InstanceID)
AND EXISTS(SELECT 1 FROM Statuses s WHERE LSS.Status=s.Status)
AND (LSS.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY LSS.Status,LSS.TotalTimeBehind DESC
OPTION(RECOMPILE)
GO
