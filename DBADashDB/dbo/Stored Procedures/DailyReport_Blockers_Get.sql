CREATE PROC dbo.DailyReport_Blockers_Get(
	@InstanceIDs VARCHAR(MAX), /* The instances listed in the performance section with a blocking finding - only those are read */
	@FromUTC DATETIME2(3),
	@ToUTC DATETIME2(3),
	@MaxPerInstance INT=10,
	@Hours VARCHAR(MAX)=NULL /* "0:hour offset,..." from @FromUTC - only snapshots in these hours (e.g. 09:00-17:00 local).  NULL: all of them */
)
AS
/*
	The lead blockers behind a daily report's blocking finding, from the blocking snapshots: in each snapshot with blocking, the sessions
	at the head of a blocking chain (not blocked themselves, blocking at least one other), with the sessions blocked under them directly
	or further down the chain (dbo.RunningQueriesBlockingRecursiveStats, as the running queries grid's root blockers).  The queries using
	the most CPU don't explain blocking - a session can hold locks while using next to none, or while sleeping with an open transaction -
	so this is what's listed for it instead.

	Lead blockers are grouped across snapshots by what they were running: the procedure the statement belongs to, otherwise the query_hash,
	otherwise the statement (sql_handle and offsets), and for a session with no text, its program.  For each, the snapshots it led a chain
	in, the most sessions blocked under it in one snapshot, the most blocked wait under it in one snapshot, and how many of those snapshots
	it was sleeping in (an open transaction left by the application).  The text, program and object come from the snapshot it blocked the
	most sessions in.

	The statement text is returned as it was captured: the caller (DBADash.DailyReport.DailyReportBlockerAnalyzer) masks its literal values
	before it's shown, and never passes it to the AI service.

	A blocked session whose blocker wasn't captured in the snapshot has no lead blocker to name, so it isn't counted.  Each instance's
	snapshots are read with a seek on (InstanceID, SnapshotDateUTC) - see dbo.DailyReport_Performance_Get for the TOP and NO_PERFORMANCE_SPOOL.
	No indexes are created on the temp tables after they're created, so they can be cached.
*/
SET NOCOUNT ON

CREATE TABLE #Instances(InstanceID INT PRIMARY KEY)
INSERT INTO #Instances(InstanceID)
SELECT DISTINCT TRY_CAST(value AS INT)
FROM STRING_SPLIT(@InstanceIDs,',')
WHERE TRY_CAST(value AS INT) IS NOT NULL

CREATE TABLE #IncludedHours(HourOffset INT PRIMARY KEY)
INSERT INTO #IncludedHours(HourOffset)
SELECT DISTINCT TRY_CAST(SUBSTRING(value, CHARINDEX(':', value) + 1, 10) AS INT)
FROM STRING_SPLIT(@Hours, ',')
WHERE LEFT(value, CHARINDEX(':', value)) = '0:'
AND TRY_CAST(SUBSTRING(value, CHARINDEX(':', value) + 1, 10) AS INT) IS NOT NULL

/* The snapshots with blocking in the window */
CREATE TABLE #Snapshots(
	InstanceID INT NOT NULL,
	SnapshotDateUTC DATETIME2(7) NOT NULL,
	PRIMARY KEY(InstanceID, SnapshotDateUTC)
)
INSERT INTO #Snapshots(InstanceID, SnapshotDateUTC)
SELECT T.InstanceID, B.SnapshotDateUTC
FROM #Instances T
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.BlockingSnapshotSummary B WITH (FORCESEEK(IX_BlockingSnapshotSummary_InstanceID_SnapshotDate(InstanceID,SnapshotDateUTC)))
			WHERE B.InstanceID = T.InstanceID AND B.SnapshotDateUTC >= @FromUTC AND B.SnapshotDateUTC < @ToUTC) B
WHERE B.BlockedSessionCount > 0
AND (@Hours IS NULL OR EXISTS(SELECT 1 FROM #IncludedHours H WHERE H.HourOffset = DATEDIFF(hh, @FromUTC, B.SnapshotDateUTC)))
OPTION(NO_PERFORMANCE_SPOOL)

/* One row per lead blocker per snapshot, with what it was running and the sessions blocked under it */
CREATE TABLE #Leads(
	InstanceID INT NOT NULL,
	SnapshotDateUTC DATETIME2(7) NOT NULL,
	session_id SMALLINT NOT NULL,
	BlockedSessions INT NOT NULL,
	BlockedWaitMs BIGINT NOT NULL,
	GroupKey NVARCHAR(300) COLLATE DATABASE_DEFAULT NOT NULL,
	DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	ObjectName NVARCHAR(257) COLLATE DATABASE_DEFAULT NULL,
	QueryHash VARCHAR(18) COLLATE DATABASE_DEFAULT NULL,
	ProgramName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	IsSleeping BIT NOT NULL,
	sql_handle VARBINARY(64) NULL,
	statement_start_offset INT NULL,
	statement_end_offset INT NULL
)
INSERT INTO #Leads(InstanceID, SnapshotDateUTC, session_id, BlockedSessions, BlockedWaitMs, GroupKey, DatabaseName, ObjectName, QueryHash, ProgramName, IsSleeping,
	sql_handle, statement_start_offset, statement_end_offset)
SELECT Q.InstanceID, Q.SnapshotDateUTC, Q.session_id, stat.BlockCountRecursive, stat.BlockWaitTimeRecursiveMs,
	CONCAT(CAST(Q.database_id AS NVARCHAR(10)), '|',
		CASE WHEN O.ObjectID IS NOT NULL THEN 'O:' + CAST(O.ObjectID AS NVARCHAR(20))
			/* A request without a compiled plan has an all zero hash - no hash, rather than one shared by every such request */
			WHEN NULLIF(Q.query_hash, 0x0000000000000000) IS NOT NULL THEN 'H:' + CONVERT(NVARCHAR(18), Q.query_hash, 1)
			WHEN Q.sql_handle IS NOT NULL THEN 'S:' + CONVERT(NVARCHAR(130), Q.sql_handle, 1) + ':' + CAST(ISNULL(Q.statement_start_offset, -1) AS NVARCHAR(11)) + ':' + CAST(ISNULL(Q.statement_end_offset, -1) AS NVARCHAR(11))
			ELSE 'P:' + ISNULL(Q.program_name, N'') END),
	ISNULL(objD.name, D.name),
	O.SchemaName + '.' + O.ObjectName,
	CONVERT(VARCHAR(18), NULLIF(Q.query_hash, 0x0000000000000000), 1),
	Q.program_name,
	CASE WHEN Q.status = 'sleeping' THEN 1 ELSE 0 END,
	Q.sql_handle,
	Q.statement_start_offset,
	Q.statement_end_offset
FROM #Snapshots S
JOIN dbo.RunningQueries Q ON Q.InstanceID = S.InstanceID AND Q.SnapshotDateUTC = S.SnapshotDateUTC
CROSS APPLY dbo.RunningQueriesBlockingRecursiveStats(Q.InstanceID, Q.SnapshotDateUTC, Q.session_id) stat
LEFT JOIN dbo.Databases D ON D.InstanceID = Q.InstanceID AND D.database_id = Q.database_id AND D.IsActive = 1
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
LEFT JOIN dbo.Databases objD ON objD.InstanceID = Q.InstanceID AND objD.database_id = QT.dbid AND objD.IsActive = 1
OUTER APPLY(SELECT TOP(1) O.ObjectID, O.SchemaName, O.ObjectName
			FROM dbo.DBObjects O
			WHERE O.DatabaseID = objD.DatabaseID
			AND O.object_id = QT.object_id
			ORDER BY O.IsActive DESC, O.ObjectName DESC) O
/* A lead blocker: not blocked itself, blocking at least one other - as IsRootBlocker in dbo.RunningQueriesInfo */
WHERE Q.blocking_session_id = 0
AND stat.BlockCount > 0
/* The function's recursive CTE walks each chain down from its lead: a chain deeper than the default 100 would fail the whole proc.  It
   can't loop - a lead isn't blocked, and each session has one blocker per snapshot (the key), so no cycle is reachable from a lead */
OPTION(MAXRECURSION 0)

;WITH Grouped AS (
	SELECT InstanceID,
		GroupKey,
		COUNT(DISTINCT SnapshotDateUTC) AS Snapshots,
		COUNT(DISTINCT CASE WHEN IsSleeping = 1 THEN SnapshotDateUTC END) AS SleepingSnapshots,
		MAX(BlockedSessions) AS MaxBlockedSessions,
		MAX(BlockedWaitMs) AS MaxBlockedWaitMs,
		MIN(SnapshotDateUTC) AS FirstSeenUTC,
		MAX(SnapshotDateUTC) AS LastSeenUTC
	FROM #Leads
	GROUP BY InstanceID, GroupKey
),
Ranked AS (
	SELECT G.*, ROW_NUMBER() OVER(PARTITION BY G.InstanceID ORDER BY G.Snapshots DESC, G.MaxBlockedSessions DESC, G.MaxBlockedWaitMs DESC) AS Rnk
	FROM Grouped G
),
/* The snapshot it blocked the most sessions in, for its text, program and object */
Example AS (
	SELECT L.*, ROW_NUMBER() OVER(PARTITION BY L.InstanceID, L.GroupKey ORDER BY L.BlockedSessions DESC, L.SnapshotDateUTC DESC) AS ExampleRank
	FROM #Leads L
)
SELECT R.InstanceID,
	E.DatabaseName,
	E.ObjectName,
	E.QueryHash,
	E.ProgramName,
	LEFT(SUBSTRING(QT.text, ISNULL((NULLIF(E.statement_start_offset,-1)/2)+1,0),
		ISNULL((NULLIF(NULLIF(E.statement_end_offset,-1),0) - NULLIF(E.statement_start_offset,-1))/2+1,2147483647)), 4000) AS StatementText,
	R.Snapshots,
	R.SleepingSnapshots,
	R.MaxBlockedSessions,
	R.MaxBlockedWaitMs,
	R.FirstSeenUTC,
	R.LastSeenUTC
FROM Ranked R
JOIN Example E ON E.InstanceID = R.InstanceID AND E.GroupKey = R.GroupKey AND E.ExampleRank = 1
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = E.sql_handle
WHERE R.Rnk <= @MaxPerInstance
ORDER BY R.InstanceID, R.Rnk
