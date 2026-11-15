CREATE PROC dbo.PerformanceInsights_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3), /* UTC */
	@ToDate DATETIME2(3) /* UTC */
)
AS
/*
	What happened on an instance in a date range that isn't on the Performance tab's charts but can explain them, for the tab's insight cards.
	The performance figures themselves (and their usual for the time of day and day of week) come from dbo.DailyReport_Performance_Get, and
	the counters outside their thresholds from dbo.PerformanceCounterSummary_Get.  Everything here is read for one instance, by its
	(InstanceID, date) keys, so the cost depends on the date range, not on how much history is kept.

	Result sets:
		0  Instance       - display name and when SQL Server last started
		1  Alerts         - DBA Dash alerts open at any time in the range: still open, or closed after it started
		2  Offline        - times the instance couldn't be reached that overlap the range
		3  AG role changes
		4  Config changes - server configuration, trace flags enabled or removed, patching, and object changes (from the schema snapshots)
		5  Jobs           - agent jobs that ran in the range and failed, or ran for 5 minutes or more
		6  Collection errors - by source, in the range
		7  Tempdb contention - from the running queries snapshot summaries: how many snapshots had sessions waiting on tempdb allocation pages
		                       (PFS/GAM/SGAM) or other tempdb pages (metadata), the most waiting at once, and the snapshot with the most.  The
		                       counts are NULL in every snapshot from an agent too old to capture them - SnapshotsWithTypes is 0 then
*/
SET NOCOUNT ON

SELECT I.InstanceID,
	I.InstanceDisplayName,
	DATEADD(mi,I.UTCOffset,I.sqlserver_start_time) AS StartTimeUtc, /* Collected in the server's local time - as dbo.InstanceUptimeThresholds_Get */
	CAST(CASE WHEN I.EngineEdition = 5 THEN 1 ELSE 0 END AS BIT) AS IsAzureDB
FROM dbo.Instances I
WHERE I.InstanceID = @InstanceID

/* Alerts */
SELECT 'Active' AS Source,
	A.AlertType,
	A.AlertKey,
	A.Priority,
	LEFT(A.LastMessage,500) AS Message,
	A.TriggerDate,
	A.ResolvedDate,
	CAST(NULL AS DATETIME2) AS ClosedDate,
	A.IsResolved,
	A.IsAcknowledged,
	A.IsBlackout
FROM Alert.ActiveAlerts A
WHERE A.InstanceID = @InstanceID
AND A.TriggerDate < @ToDate
AND (A.IsResolved = 0 OR A.ResolvedDate >= @FromDate)
UNION ALL
SELECT 'Closed',
	A.AlertType,
	A.AlertKey,
	A.Priority,
	LEFT(A.LastMessage,500),
	A.TriggerDate,
	A.ResolvedDate,
	A.ClosedDate,
	A.IsResolved,
	A.IsAcknowledged,
	CAST(0 AS BIT)
FROM Alert.ClosedAlerts A
WHERE A.ClosedDate >= @FromDate
AND A.InstanceID = @InstanceID
AND A.TriggerDate < @ToDate
/* An alert resolved before the range started was still waiting to be closed - not open in the range */
AND (A.ResolvedDate IS NULL OR A.ResolvedDate >= @FromDate)

/* Offline */
SELECT O.FirstFail,
	O.LastFail,
	O.ClosedDate,
	O.IsCurrent,
	O.FailCount
FROM dbo.OfflineInstances O
WHERE O.InstanceID = @InstanceID
AND O.LastFail >= @FromDate
AND O.FirstFail < @ToDate

/* AG role changes */
SELECT RC.AvailabilityGroupName,
	RC.ReplicaServerName,
	RC.PreviousRoleDesc,
	RC.NewRoleDesc,
	RC.ChangedAfter,
	RC.ChangedBefore,
	RC.Source
FROM dbo.AvailabilityGroupRoleChanges RC
WHERE RC.InstanceID = @InstanceID
AND RC.ChangedBefore >= @FromDate
AND RC.ChangedBefore < @ToDate

/* Config changes */
SELECT 'Configuration' AS ChangeType,
	ISNULL(O.name, CONCAT('configuration_id ', H.configuration_id)) AS Name,
	CAST(H.value AS NVARCHAR(128)) AS OldValue,
	CAST(H.new_value AS NVARCHAR(128)) AS NewValue,
	H.ValidTo AS ChangeDate,
	CAST(NULL AS INT) AS Count
FROM dbo.SysConfigHistory H
LEFT JOIN dbo.SysConfigOptions O ON O.configuration_id = H.configuration_id
WHERE H.InstanceID = @InstanceID
AND H.ValidTo >= @FromDate
AND H.ValidTo < @ToDate
UNION ALL
SELECT 'Trace flag',
	CONCAT('Trace flag ', TF.TraceFlag),
	N'Off',
	N'On',
	TF.ValidFrom,
	NULL
FROM dbo.TraceFlags TF
WHERE TF.InstanceID = @InstanceID
AND TF.ValidFrom >= @FromDate
AND TF.ValidFrom < @ToDate
UNION ALL
/* Turned on in the range and since turned off - TraceFlags_Upd moves the row to TraceFlagHistory, as TraceFlagHistory_Get reads it */
SELECT 'Trace flag',
	CONCAT('Trace flag ', TFH.TraceFlag),
	N'Off',
	N'On',
	TFH.ValidFrom,
	NULL
FROM dbo.TraceFlagHistory TFH
WHERE TFH.InstanceID = @InstanceID
AND TFH.ValidFrom >= @FromDate
AND TFH.ValidFrom < @ToDate
UNION ALL
SELECT 'Trace flag',
	CONCAT('Trace flag ', TFH.TraceFlag),
	N'On',
	N'Off',
	TFH.ValidTo,
	NULL
FROM dbo.TraceFlagHistory TFH
WHERE TFH.InstanceID = @InstanceID
AND TFH.ValidTo >= @FromDate
AND TFH.ValidTo < @ToDate
UNION ALL
SELECT 'Patching',
	N'SQL Server version',
	CONCAT(P.OldVersion, N' ', P.OldProductLevel, N' ', P.OldProductUpdateLevel),
	CONCAT(P.NewVersion, N' ', P.NewProductLevel, N' ', P.NewProductUpdateLevel),
	P.ChangedDate,
	NULL
FROM dbo.SQLPatchingHistory P
WHERE P.InstanceID = @InstanceID
AND P.ChangedDate >= @FromDate
AND P.ChangedDate < @ToDate
UNION ALL
SELECT 'Schema',
	D.name,
	NULL,
	NULL,
	MAX(S.SnapshotDate),
	SUM(ISNULL(S.Created,0) + ISNULL(S.Modified,0) + ISNULL(S.Dropped,0))
FROM dbo.Databases D
JOIN dbo.DDLSnapshots S ON S.DatabaseID = D.DatabaseID
WHERE D.InstanceID = @InstanceID
AND S.SnapshotDate >= @FromDate
AND S.SnapshotDate < @ToDate
GROUP BY D.name
HAVING SUM(ISNULL(S.Created,0) + ISNULL(S.Modified,0) + ISNULL(S.Dropped,0)) > 0
ORDER BY ChangeDate

/* Jobs - the job outcome rows (step 0).  Runs are looked for from a day before the range, to catch a long job still running when it started */
SELECT TOP(50) J.name AS JobName,
	JH.RunDateTime,
	JH.RunDurationSec,
	JH.run_status AS RunStatus
FROM dbo.Jobs J
CROSS APPLY (SELECT TOP (2147483647) H.RunDateTime, H.RunDurationSec, H.run_status, H.FinishDateTime
			FROM dbo.JobHistory H WITH (FORCESEEK(IX_JobHistory_InstanceID_job_id_step_id_RunDateTime(InstanceID,job_id,step_id,RunDateTime)))
			WHERE H.InstanceID = J.InstanceID
			AND H.job_id = J.job_id
			AND H.step_id = 0
			AND H.RunDateTime >= DATEADD(d,-1,@FromDate)
			AND H.RunDateTime < @ToDate) JH
WHERE J.InstanceID = @InstanceID
AND JH.FinishDateTime > @FromDate
AND (JH.run_status <> 1 OR JH.RunDurationSec >= 300)
ORDER BY CASE WHEN JH.run_status IN(0,3) THEN 0 ELSE 1 END, JH.RunDurationSec DESC
OPTION(NO_PERFORMANCE_SPOOL)

/* Collection errors */
SELECT E.ErrorSource,
	COUNT(*) AS Errors,
	MIN(E.ErrorDate) AS FirstError,
	MAX(E.ErrorDate) AS LastError
FROM dbo.CollectionErrorLog E
WHERE E.InstanceID = @InstanceID
AND E.ErrorDate >= @FromDate
AND E.ErrorDate < @ToDate
GROUP BY E.ErrorSource
ORDER BY Errors DESC

/*
	Tempdb contention - one range seek on the summaries' (InstanceID, SnapshotDateUTC) key.  The worst snapshot of each kind is ranked with
	ROW_NUMBER over the same rows rather than read again with TOP(1) subqueries.  NULL counts (an older agent) sort last as they're lowest,
	and the rank-1 row only counts when it actually had sessions waiting.  Ties go to the earliest snapshot
*/
;WITH S AS (
	SELECT S.SnapshotDateUTC,
		S.TempDBAllocationWaitCount,
		S.TempDBAllocationWaitTimeMs,
		S.TempDBMetadataWaitCount,
		S.TempDBMetadataWaitTimeMs,
		ROW_NUMBER() OVER(ORDER BY S.TempDBAllocationWaitCount DESC, S.TempDBAllocationWaitTimeMs DESC, S.SnapshotDateUTC) AS AllocationRank,
		ROW_NUMBER() OVER(ORDER BY S.TempDBMetadataWaitCount DESC, S.TempDBMetadataWaitTimeMs DESC, S.SnapshotDateUTC) AS MetadataRank
	FROM dbo.RunningQueriesSummary S
	WHERE S.InstanceID = @InstanceID
	AND S.SnapshotDateUTC >= @FromDate
	AND S.SnapshotDateUTC < @ToDate
)
SELECT COUNT(*) AS Snapshots,
	COUNT(S.TempDBAllocationWaitCount) AS SnapshotsWithTypes,
	ISNULL(SUM(CASE WHEN S.TempDBAllocationWaitCount > 0 THEN 1 ELSE 0 END),0) AS AllocationSnapshots,
	MAX(S.TempDBAllocationWaitCount) AS MaxAllocationWaiting,
	MAX(S.TempDBAllocationWaitTimeMs) AS MaxAllocationWaitMs,
	ISNULL(SUM(CASE WHEN S.TempDBMetadataWaitCount > 0 THEN 1 ELSE 0 END),0) AS MetadataSnapshots,
	MAX(S.TempDBMetadataWaitCount) AS MaxMetadataWaiting,
	MAX(S.TempDBMetadataWaitTimeMs) AS MaxMetadataWaitMs,
	MAX(CASE WHEN S.AllocationRank = 1 AND S.TempDBAllocationWaitCount > 0 THEN S.SnapshotDateUTC END) AS WorstAllocationSnapshot,
	MAX(CASE WHEN S.MetadataRank = 1 AND S.TempDBMetadataWaitCount > 0 THEN S.SnapshotDateUTC END) AS WorstMetadataSnapshot
FROM S
