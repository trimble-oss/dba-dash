/*
	Long running queries and long open transactions in a window, for a performance analysis.  Run by
	the AI service when the model asks; see DBADashAI PerformanceInvestigation.

	Reads the snapshots with the longest running query and those with the oldest open transaction -
	@Snapshots of each - through the full running queries view.  A query or transaction is included
	from 30 seconds; grouped by what it was running, so one seen in many snapshots is one row.
*/
CREATE PROC AI.PerformanceInvestigationLongRunning_Get(
	@InstanceID INT,
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@Snapshots INT = 10,
	@Top INT = 10
)
AS
SET NOCOUNT ON

CREATE TABLE #Snapshots(SnapshotDateUTC DATETIME2(7) NOT NULL PRIMARY KEY)

INSERT INTO #Snapshots(SnapshotDateUTC)
SELECT L.SnapshotDateUTC
FROM (	SELECT TOP (@Snapshots) S.SnapshotDateUTC
		FROM dbo.RunningQueriesSummary S
		WHERE S.InstanceID = @InstanceID
		AND S.SnapshotDateUTC >= @FromDate
		AND S.SnapshotDateUTC < @ToDate
		ORDER BY S.LongestRunningQueryMs DESC
	) L
UNION
SELECT T.SnapshotDateUTC
FROM (	SELECT TOP (@Snapshots) S.SnapshotDateUTC
		FROM dbo.RunningQueriesSummary S
		WHERE S.InstanceID = @InstanceID
		AND S.SnapshotDateUTC >= @FromDate
		AND S.SnapshotDateUTC < @ToDate
		AND S.OldestTransactionMs IS NOT NULL
		ORDER BY S.OldestTransactionMs DESC
	) T

SELECT TOP (@Top)
		Q.database_name AS DatabaseName,
		Q.object_name AS ObjectName,
		LEFT(Q.text, 500) AS StatementText,
		Q.program_name AS ProgramName,
		Q.status AS Status,
		COUNT(DISTINCT Q.SnapshotDateUTC) AS Snapshots,
		MAX(T.ElapsedMs) AS MaxElapsedMs,
		MAX(Q.transaction_duration_ms) AS MaxTransactionMs,
		MAX(Q.cpu_time) AS MaxCpuMs,
		MAX(Q.logical_reads) AS MaxLogicalReads,
		MAX(Q.granted_query_memory_kb) AS MaxGrantKB,
		MAX(Q.tempdb_current_mb) AS MaxTempDBMB,
		MAX(Q.wait_type) AS WaitType,
		MAX(Q.dop) AS MaxDop
FROM dbo.RunningQueriesInfo Q
JOIN #Snapshots S ON S.SnapshotDateUTC = Q.SnapshotDateUTC
/*	The view's corrected duration rather than total_elapsed_time, which can be anomalous.  Only for active
	requests: for a sleeping session it is the time since its last batch, and the open transaction is what
	matters there.  Missing values count as 0 so a sleeping session's transaction age still orders it. */
CROSS APPLY(SELECT CASE WHEN Q.status = 'sleeping' THEN NULL ELSE Q.[Duration (ms)] END AS ElapsedMs) T
CROSS APPLY(SELECT CASE WHEN ISNULL(Q.transaction_duration_ms, 0) > ISNULL(T.ElapsedMs, 0) THEN ISNULL(Q.transaction_duration_ms, 0) ELSE ISNULL(T.ElapsedMs, 0) END AS LongestMs) L
WHERE Q.InstanceID = @InstanceID
AND L.LongestMs >= 30000
GROUP BY Q.database_name, Q.object_name, LEFT(Q.text, 500), Q.program_name, Q.status
ORDER BY MAX(L.LongestMs) DESC
