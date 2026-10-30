/*
	Blocking in a window, for a performance analysis that has found enough of it to want to know who
	was doing it.  The AI service runs it when the model asks - see DBADashAI PerformanceInvestigation.

	Reads the full running queries snapshots, which is expensive, so only the @Snapshots worst ones
	(by blocked wait, from dbo.RunningQueriesSummary) are read rather than every snapshot in the window.
	Grouped rather than listed: a blocker seen in twenty snapshots is one finding, not twenty rows.

		0  What was read  - snapshots with blocking in the window, and how many were analysed.
		1  Root blockers  - grouped by what they were running.  A sleeping root blocker is an
		                    application holding a transaction open between requests.
		2  Blocked        - what the blocked sessions were waiting on.
*/
CREATE PROC AI.PerformanceInvestigationBlocking_Get(
	@InstanceID INT,
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@Snapshots INT = 20,
	@Top INT = 10
)
AS
SET NOCOUNT ON

CREATE TABLE #Snapshots(SnapshotDateUTC DATETIME2(7) NOT NULL PRIMARY KEY)

INSERT INTO #Snapshots(SnapshotDateUTC)
SELECT TOP (@Snapshots) S.SnapshotDateUTC
FROM dbo.RunningQueriesSummary S
WHERE S.InstanceID = @InstanceID
AND S.SnapshotDateUTC >= @FromDate
AND S.SnapshotDateUTC < @ToDate
AND S.BlockedQueries > 0
ORDER BY S.BlockedQueriesWaitMs DESC

SELECT	(SELECT COUNT(*)
		FROM dbo.RunningQueriesSummary S
		WHERE S.InstanceID = @InstanceID
		AND S.SnapshotDateUTC >= @FromDate
		AND S.SnapshotDateUTC < @ToDate
		AND S.BlockedQueries > 0) AS SnapshotsWithBlocking,
		(SELECT COUNT(*) FROM #Snapshots) AS SnapshotsAnalysed

SELECT TOP (@Top)
		Q.database_name AS DatabaseName,
		Q.object_name AS ObjectName,
		LEFT(Q.text, 500) AS StatementText,
		Q.program_name AS ProgramName,
		Q.login_name AS LoginName,
		Q.status AS Status,
		COUNT(DISTINCT Q.SnapshotDateUTC) AS Snapshots,
		MIN(Q.SnapshotDateUTC) AS FirstSeenUTC,
		MAX(Q.SnapshotDateUTC) AS LastSeenUTC,
		MAX(Q.BlockCountRecursive) AS MaxSessionsBlocked,
		MAX(Q.BlockWaitTimeRecursiveMs) AS MaxBlockedWaitMs,
		MAX(Q.transaction_duration_ms) AS MaxTransactionMs,
		MAX(Q.open_transaction_count) AS MaxOpenTransactions,
		MAX(Q.wait_type) AS WaitType
FROM dbo.RunningQueriesInfo Q
JOIN #Snapshots S ON S.SnapshotDateUTC = Q.SnapshotDateUTC
WHERE Q.InstanceID = @InstanceID
AND Q.IsRootBlocker = 1
GROUP BY Q.database_name, Q.object_name, LEFT(Q.text, 500), Q.program_name, Q.login_name, Q.status
ORDER BY SUM(Q.BlockWaitTimeRecursiveMs) DESC

SELECT TOP (@Top)
		Q.wait_type AS WaitType,
		Q.wait_db AS WaitDatabase,
		Q.wait_object AS WaitObject,
		Q.object_name AS BlockedObjectName,
		COUNT(*) AS BlockedSessions,
		MAX(Q.wait_time) AS MaxWaitMs
FROM dbo.RunningQueriesInfo Q
JOIN #Snapshots S ON S.SnapshotDateUTC = Q.SnapshotDateUTC
WHERE Q.InstanceID = @InstanceID
AND Q.blocking_session_id <> 0
GROUP BY Q.wait_type, Q.wait_db, Q.wait_object, Q.object_name
ORDER BY SUM(CAST(Q.wait_time AS BIGINT)) DESC
