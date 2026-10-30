/*
	The queries seen waiting on a wait type in a window - which statements are behind PAGELATCH waits
	on tempdb, or LCK_M waits - for a performance analysis that wants to know.  Run by the AI service
	when the model asks; see DBADashAI PerformanceInvestigation.

	Only the @Snapshots snapshots with the most sessions waiting are read through the full running
	queries view.  @WaitType is a LIKE pattern, e.g. PAGELATCH% or LCK_M%; @TempDBOnly keeps waits on
	tempdb pages only, which is what separates tempdb contention from page latches on user tables.

		0  What was read
		1  Waiting queries - grouped by what they were running.  PageType says PFS, GAM or SGAM where
		                     the wait was on an allocation page.
*/
CREATE PROC AI.PerformanceInvestigationWaits_Get(
	@InstanceID INT,
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@WaitType NVARCHAR(60),
	@TempDBOnly BIT = 0,
	@Snapshots INT = 20,
	@Top INT = 10
)
AS
SET NOCOUNT ON

CREATE TABLE #Snapshots(SnapshotDateUTC DATETIME2(7) NOT NULL PRIMARY KEY, Sessions INT NOT NULL)

/* Ranked from the base table, which is cheap to filter, before the view is read for the few that matter. */
INSERT INTO #Snapshots(SnapshotDateUTC, Sessions)
SELECT TOP (@Snapshots) Q.SnapshotDateUTC, COUNT(*)
FROM dbo.RunningQueries Q
WHERE Q.InstanceID = @InstanceID
AND Q.SnapshotDateUTC >= @FromDate
AND Q.SnapshotDateUTC < @ToDate
AND Q.wait_type LIKE @WaitType
AND (@TempDBOnly = 0 OR Q.wait_resource LIKE '2:%')
GROUP BY Q.SnapshotDateUTC
ORDER BY COUNT(*) DESC

SELECT	(SELECT COUNT(DISTINCT Q.SnapshotDateUTC)
		FROM dbo.RunningQueries Q
		WHERE Q.InstanceID = @InstanceID
		AND Q.SnapshotDateUTC >= @FromDate
		AND Q.SnapshotDateUTC < @ToDate
		AND Q.wait_type LIKE @WaitType
		AND (@TempDBOnly = 0 OR Q.wait_resource LIKE '2:%')) AS SnapshotsWithWait,
		(SELECT COUNT(*) FROM #Snapshots) AS SnapshotsAnalysed,
		(SELECT MAX(Sessions) FROM #Snapshots) AS MaxSessionsWaiting

SELECT TOP (@Top)
		Q.database_name AS DatabaseName,
		Q.object_name AS ObjectName,
		LEFT(Q.text, 500) AS StatementText,
		Q.program_name AS ProgramName,
		Q.wait_type AS WaitType,
		Q.page_type AS PageType,
		Q.wait_object AS WaitObject,
		COUNT(DISTINCT Q.SnapshotDateUTC) AS Snapshots,
		COUNT(*) AS Sessions,
		MAX(Q.wait_time) AS MaxWaitMs,
		MAX(Q.tempdb_current_mb) AS MaxTempDBMB
FROM dbo.RunningQueriesInfo Q
JOIN #Snapshots S ON S.SnapshotDateUTC = Q.SnapshotDateUTC
WHERE Q.InstanceID = @InstanceID
AND Q.wait_type LIKE @WaitType
AND (@TempDBOnly = 0 OR Q.wait_resource LIKE '2:%')
GROUP BY Q.database_name, Q.object_name, LEFT(Q.text, 500), Q.program_name, Q.wait_type, Q.page_type, Q.wait_object
ORDER BY COUNT(*) DESC
