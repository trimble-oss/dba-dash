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
		0 - Instance summary
		1 - Database detail
	@InstanceID is supplied when drilling down from the summary to a single instance.

	dbo.LogShippingStatus is evaluated once into #LogShippingStatus and both result sets are produced from it.
	The summary aggregation matches dbo.LogShippingStatusSummary - keep the two in sync.
	Durations are formatted only for the rows returned.
*/
SET NOCOUNT ON

-- Drilling down to a single instance mirrors the legacy single-instance view: show all databases including hidden ones.
IF @InstanceID IS NOT NULL SET @ShowHidden=1

/*
	Temp table rather than table variable: it has statistics and recompiles when the instance count changes significantly.
	Avoids OPTION(RECOMPILE) on the dbo.LogShippingStatus query, which is expensive to compile.
*/
CREATE TABLE #Instances(
	InstanceID INT PRIMARY KEY
)
IF @InstanceID IS NOT NULL
BEGIN
	INSERT INTO #Instances(InstanceID) VALUES(@InstanceID)
END
ELSE IF EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO #Instances(InstanceID)
	SELECT ID FROM @InstanceIDs
END
ELSE
BEGIN
	INSERT INTO #Instances(InstanceID)
	SELECT InstanceID FROM dbo.Instances WHERE IsActive=1
END

SELECT LSS.InstanceID,
	LSS.DatabaseID,
	LSS.InstanceDisplayName,
	LSS.name,
	LSS.restore_date_utc,
	LSS.backup_start_date_utc,
	LSS.TimeSinceLast,
	LSS.LatencyOfLast,
	LSS.TotalTimeBehind,
	LSS.SnapshotAge,
	LSS.SnapshotDate,
	LSS.SnapshotAgeStatus,
	LSS.Status,
	LSS.StatusDescription,
	LSS.last_file,
	LSS.ThresholdConfiguredLevel,
	LSS.IsMissing,
	LSS.PrimaryInstanceID,
	LSS.PrimaryInstanceDisplayName,
	LSS.state_desc,
	LSS.IsExcluded,
	LSS.ExcludedBy,
	LSS.ShowInSummary,
	LSS.IsNewDatabase
INTO #LogShippingStatus
FROM dbo.LogShippingStatus LSS
WHERE EXISTS(SELECT 1 FROM #Instances I WHERE I.InstanceID = LSS.InstanceID)
AND (LSS.ShowInSummary=1 OR @ShowHidden=1)

/* Result set 0: Instance summary.  Matches dbo.LogShippingStatusSummary */
;WITH Summary AS (
	SELECT LSS.InstanceID,
		   LSS.InstanceDisplayName,
		   ISNULL(MIN(NULLIF(LSS.Status,3)),3) AS Status,
		   SUM(CASE WHEN LSS.IsMissing=0 AND LSS.Status<>3 THEN 1 ELSE 0 END) AS LogShippedDBCount,
		   SUM(CASE WHEN LSS.Status=2 THEN 1 ELSE 0 END) AS WarningCount,
		   SUM(CASE WHEN LSS.Status=1 THEN 1 ELSE 0 END) AS CriticalCount,
		   MAX(CASE WHEN LSS.Status<>3 THEN LSS.TotalTimeBehind END) MaxTotalTimeBehind,
		   MIN(CASE WHEN LSS.Status<>3 THEN LSS.TotalTimeBehind END) MinTotalTimeBehind,
		   AVG(CASE WHEN LSS.Status<>3 THEN LSS.TotalTimeBehind END) AvgTotalTimeBehind,
		   MAX(CASE WHEN LSS.Status<>3 THEN LSS.LatencyOfLast END) MaxLatencyOfLast,
		   MIN(CASE WHEN LSS.Status<>3 THEN LSS.LatencyOfLast END) MinLatencyOfLast,
		   AVG(CASE WHEN LSS.Status<>3 THEN LSS.LatencyOfLast END) AvgLatencyOfLast,
		   MAX(CASE WHEN LSS.Status<>3 THEN LSS.TimeSinceLast END) AS MaxTimeSinceLast,
		   MIN(CASE WHEN LSS.Status<>3 THEN LSS.TimeSinceLast END) AS MinTimeSinceLast,
		   AVG(CASE WHEN LSS.Status<>3 THEN LSS.TimeSinceLast END) AS AvgTimeSinceLast,
		   MAX(LSS.SnapshotAge) AS SnapshotAge,
		   ISNULL(MIN(NULLIF(LSS.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
		   MIN(CASE WHEN LSS.Status<>3 THEN LSS.backup_start_date_utc END) MinDateOfLastBackupRestored,
		   MAX(CASE WHEN LSS.Status<>3 THEN LSS.backup_start_date_utc END) MaxDateOfLastBackupRestored,
		   MIN(CASE WHEN LSS.Status<>3 THEN LSS.restore_date_utc END) MinLastRestoreCompleted,
		   MAX(CASE WHEN LSS.Status<>3 THEN LSS.restore_date_utc END) MaxLastRestoreCompleted,
		   CASE WHEN T.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS InstanceLevelThreshold,
		   SUM(CASE WHEN LSS.ThresholdConfiguredLevel='Database' AND LSS.Status<>3 THEN 1 ELSE 0 END) AS DatabaseLevelThresholds,
		   SUM(CASE WHEN LSS.IsMissing=1 AND LSS.Status<>3 THEN 1 ELSE 0 END) AS MissingCount,
		   SUM(CASE WHEN LSS.IsNewDatabase=1 THEN 1 ELSE 0 END) AS NewDatabaseCount
	FROM #LogShippingStatus LSS
	LEFT JOIN dbo.LogRestoreThresholds T ON LSS.InstanceID = T.InstanceID AND T.DatabaseID=-1
	/*
		N/A rows are included so that an instance whose log shipped databases are all N/A (e.g. excluded or misconfigured thresholds)
		still appears in the summary with N/A status.  N/A rows are ignored by the other aggregates.
		Missing databases that are N/A (excluded) are ignored, other than new databases which are counted in NewDatabaseCount.
	*/
	WHERE (LSS.Status<> 3 OR LSS.IsNewDatabase=1 OR LSS.IsMissing=0)
	GROUP BY LSS.InstanceID,
			 LSS.InstanceDisplayName,
			 T.InstanceID
)
SELECT S.InstanceID,
       S.InstanceDisplayName,
       S.Status,
       CASE S.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
       S.LogShippedDBCount,
       S.WarningCount,
       S.CriticalCount,
       S.MaxTotalTimeBehind,
       MaxTotalTimeBehindHD.HumanDuration AS MaxTotalTimeBehindDuration,
       S.MinTotalTimeBehind,
       MinTotalTimeBehindHD.HumanDuration AS MinTotalTimeBehindDuration,
       S.AvgTotalTimeBehind,
       AvgTotalTimeBehindHD.HumanDuration AS AvgTotalTimeBehindDuration,
       S.MaxLatencyOfLast,
       MaxLatencyOfLastHD.HumanDuration AS MaxLatencyOfLastDuration,
       S.MinLatencyOfLast,
       MinLatencyOfLastHD.HumanDuration AS MinLatencyOfLastDuration,
       S.AvgLatencyOfLast,
       AvgLatencyOfLastHD.HumanDuration AS AvgLatencyOfLastDuration,
       S.MaxTimeSinceLast,
       MaxTimeSinceLastHD.HumanDuration AS MaxTimeSinceLastDuration,
       S.MinTimeSinceLast,
       MinTimeSinceLastHD.HumanDuration AS MinTimeSinceLastDuration,
       S.AvgTimeSinceLast,
       AvgTimeSinceLastHD.HumanDuration AS AvgTimeSinceLastDuration,
       S.SnapshotAge,
       SnapshotAgeHD.HumanDuration AS SnapshotAgeDuration,
       S.SnapshotAgeStatus,
       S.MinDateOfLastBackupRestored,
       S.MaxDateOfLastBackupRestored,
       S.MinLastRestoreCompleted,
       S.MaxLastRestoreCompleted,
       S.InstanceLevelThreshold,
       S.DatabaseLevelThresholds,
       CAST('Configure' AS NVARCHAR(20)) AS Configure,
       S.MissingCount,
       S.NewDatabaseCount,
       pri.PrimaryInstances
FROM Summary S
/* Primary instance(s) configured for the instance in dbo.LogShippingPairs.  NULL if not configured.  FOR XML PATH rather than STRING_AGG for SQL 2016 */
OUTER APPLY(SELECT STUFF((SELECT ', ' + PI.InstanceDisplayName
						FROM dbo.LogShippingPairs P
						JOIN dbo.Instances PI ON P.PrimaryInstanceID = PI.InstanceID
						WHERE P.SecondaryInstanceID = S.InstanceID
						AND PI.IsActive=1
						ORDER BY PI.InstanceDisplayName
						FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS PrimaryInstances
			) pri
OUTER APPLY dbo.SecondsToHumanDuration(S.MaxTimeSinceLast*60.0) AS MaxTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.MinTimeSinceLast*60.0) AS MinTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.AvgTimeSinceLast*60.0) AS AvgTimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.MaxLatencyOfLast*60.0) AS MaxLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.MinLatencyOfLast*60.0) AS MinLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.AvgLatencyOfLast*60.0) AS AvgLatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(S.MaxTotalTimeBehind*60.0) AS MaxTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(S.MinTotalTimeBehind*60.0) AS MinTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(S.AvgTotalTimeBehind*60.0) AS AvgTotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(S.SnapshotAge*60.0) AS SnapshotAgeHD
ORDER BY S.Status,S.MaxTotalTimeBehind DESC

/* Result set 1: Database detail */
SELECT LSS.InstanceID,
       LSS.DatabaseID,
       LSS.InstanceDisplayName,
       LSS.name,
       LSS.restore_date_utc AS restore_date,
       LSS.backup_start_date_utc AS backup_start_date,
       LSS.TimeSinceLast,
       TimeSinceLastHD.HumanDuration AS TimeSinceLastDuration,
       LSS.LatencyOfLast,
       LatencyOfLastHD.HumanDuration AS LatencyOfLastDuration,
       LSS.TotalTimeBehind,
       TotalTimeBehindHD.HumanDuration AS TotalTimeBehindDuration,
       LSS.SnapshotAge,
       SnapshotAgeHD.HumanDuration AS SnapshotAgeDuration,
       LSS.SnapshotDate,
       LSS.Status,
       LSS.StatusDescription,
       f.FileName AS last_file,
       LSS.ThresholdConfiguredLevel,
       CASE WHEN LSS.SnapshotAge>1440 THEN 1 WHEN LSS.SnapshotAge>120 THEN 2 WHEN LSS.SnapshotAge<60 THEN 4 ELSE 3 END AS SnapshotAgeStatus,
       /* No database level thresholds for databases missing from the secondary */
       CAST(CASE WHEN LSS.DatabaseID IS NULL THEN NULL ELSE 'Configure' END AS NVARCHAR(20)) AS Configure,
       LSS.IsMissing,
       LSS.PrimaryInstanceID,
       LSS.PrimaryInstanceDisplayName,
       LSS.state_desc,
       LSS.IsExcluded,
       LSS.ExcludedBy
FROM #LogShippingStatus LSS
CROSS APPLY dbo.ParseFileName(LSS.last_file) f
OUTER APPLY dbo.SecondsToHumanDuration(LSS.TimeSinceLast*60.0) AS TimeSinceLastHD
OUTER APPLY dbo.SecondsToHumanDuration(LSS.LatencyOfLast*60.0) AS LatencyOfLastHD
OUTER APPLY dbo.SecondsToHumanDuration(LSS.TotalTimeBehind*60.0) AS TotalTimeBehindHD
OUTER APPLY dbo.SecondsToHumanDuration(LSS.SnapshotAge*60.0) AS SnapshotAgeHD
WHERE (LSS.Status = 1 AND @IncludeCritical=1)
OR (LSS.Status = 2 AND @IncludeWarning=1)
OR (LSS.Status = 3 AND @IncludeNA=1)
OR (LSS.Status = 4 AND @IncludeOK=1)
ORDER BY LSS.Status,LSS.TotalTimeBehind DESC,LSS.InstanceDisplayName,LSS.name
GO
