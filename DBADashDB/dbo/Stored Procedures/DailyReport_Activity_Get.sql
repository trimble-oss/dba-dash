CREATE PROC dbo.DailyReport_Activity_Get(
	@InstanceIDs VARCHAR(MAX), /* The instances listed in the performance section - only those are read */
	@FromUTC DATETIME2(3),
	@ToUTC DATETIME2(3),
	@MinMinutes INT=5, /* Requests seen running for at least this long */
	@MaxPerInstance INT=10, /* Per instance and per measure: the longest, and the most CPU, reads and writes */
	@Hours VARCHAR(MAX)=NULL /* "0:hour offset,..." from @FromUTC - only requests seen in these hours (e.g. 09:00-17:00 local).  NULL: all of them */
)
AS
/*
	Long running requests from the running query snapshots, for a daily report's performance section: the work the query stats don't
	show - DBCC CHECKDB, backups, index maintenance - which is often what's behind a latency or CPU finding.

	A request is a session_id and start_time_utc seen across snapshots.  Its figures are the last snapshot's (they're cumulative for the
	request), its duration is from its start to the last snapshot it was seen in.  Only what was running when a snapshot was taken is seen,
	so it's for long operations rather than short bursts.  Sleeping sessions and requests that only wait (WAITFOR) are left out.

	When the report counts only certain hours (@Hours), a request has to have been seen in one of them - before the limit, so a report for
	09:00-17:00 isn't left with only overnight maintenance that the caller would then filter out.  The candidates are the top @MaxPerInstance
	by each of duration, CPU, reads and writes, so the caller can rank them by the finding's resource rather than getting the longest only.

	Each instance's snapshots are read with a seek on (InstanceID, SnapshotDateUTC) - see dbo.DailyReport_Performance_Get for the TOP and
	NO_PERFORMANCE_SPOOL.
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

CREATE TABLE #Requests(
	InstanceID INT NOT NULL,
	session_id SMALLINT NOT NULL,
	start_time_utc DATETIME NOT NULL,
	LastSeenUTC DATETIME2(7) NOT NULL,
	PRIMARY KEY(InstanceID, session_id, start_time_utc)
)

INSERT INTO #Requests(InstanceID, session_id, start_time_utc, LastSeenUTC)
SELECT T.InstanceID, Q.session_id, Q.start_time_utc, MAX(Q.SnapshotDateUTC)
FROM #Instances T
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.RunningQueries Q WITH (FORCESEEK(PK_RunningQueries(InstanceID,SnapshotDateUTC)))
			WHERE Q.InstanceID = T.InstanceID AND Q.SnapshotDateUTC >= @FromUTC AND Q.SnapshotDateUTC < @ToUTC) Q
/* One row at most: HourOffset is the key */
LEFT JOIN #IncludedHours H ON H.HourOffset = DATEDIFF(hh, @FromUTC, Q.SnapshotDateUTC)
WHERE Q.start_time_utc IS NOT NULL
AND ISNULL(Q.status,'') <> 'sleeping'
AND ISNULL(Q.command,'') NOT IN ('WAITFOR','AWAITING COMMAND')
AND ISNULL(Q.wait_type,'') NOT LIKE '%WAITFOR%'
GROUP BY T.InstanceID, Q.session_id, Q.start_time_utc
HAVING DATEDIFF(mi, Q.start_time_utc, MAX(Q.SnapshotDateUTC)) >= @MinMinutes
/* Seen in a counted hour, when only some are counted */
AND (@Hours IS NULL OR COUNT(H.HourOffset) > 0)
OPTION(NO_PERFORMANCE_SPOOL)

;WITH Detail AS (
	SELECT R.InstanceID,
		R.start_time_utc,
		R.LastSeenUTC,
		DATEDIFF(s, R.start_time_utc, R.LastSeenUTC) AS DurationSec,
		Q.command,
		D.name AS DatabaseName,
		J.name AS JobName,
		/* SQLAgent - TSQL JobStep (Job 0x... : Step 2) */
		CASE WHEN jid.job_id IS NOT NULL AND CHARINDEX(': Step ', Q.program_name) > 0 THEN TRY_CAST(SUBSTRING(Q.program_name, CHARINDEX(': Step ', Q.program_name) + 7, CHARINDEX(')', Q.program_name + ')', CHARINDEX(': Step ', Q.program_name)) - CHARINDEX(': Step ', Q.program_name) - 7) AS INT) END AS StepID,
		Q.program_name,
		Q.cpu_time,
		Q.reads,
		Q.writes,
		Q.logical_reads,
		LEFT(SUBSTRING(QT.text, ISNULL((NULLIF(Q.statement_start_offset,-1)/2)+1,0),
			ISNULL((NULLIF(NULLIF(Q.statement_end_offset,-1),0) - NULLIF(Q.statement_start_offset,-1))/2+1,2147483647)), 300) AS StatementText,
		ROW_NUMBER() OVER(PARTITION BY R.InstanceID ORDER BY DATEDIFF(s, R.start_time_utc, R.LastSeenUTC) DESC) AS DurationRank,
		ROW_NUMBER() OVER(PARTITION BY R.InstanceID ORDER BY Q.cpu_time DESC) AS CPURank,
		ROW_NUMBER() OVER(PARTITION BY R.InstanceID ORDER BY Q.reads DESC) AS ReadsRank,
		ROW_NUMBER() OVER(PARTITION BY R.InstanceID ORDER BY Q.writes DESC) AS WritesRank
	FROM #Requests R
	JOIN dbo.RunningQueries Q ON Q.InstanceID = R.InstanceID AND Q.SnapshotDateUTC = R.LastSeenUTC AND Q.session_id = R.session_id
	LEFT JOIN dbo.Databases D ON D.InstanceID = Q.InstanceID AND D.database_id = Q.database_id AND D.IsActive = 1
	LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
	OUTER APPLY (SELECT CASE WHEN Q.program_name LIKE 'SQLAgent - TSQL JobStep (Job 0x%' THEN TRY_CAST(TRY_CONVERT(BINARY(16),SUBSTRING(Q.program_name, 30,34),1) AS UNIQUEIDENTIFIER) END AS job_id) jid
	LEFT JOIN dbo.Jobs J ON J.job_id = jid.job_id AND J.InstanceID = Q.InstanceID
)
SELECT InstanceID,
	start_time_utc AS StartUTC,
	LastSeenUTC,
	DurationSec,
	command AS Command,
	DatabaseName,
	JobName,
	StepID,
	program_name AS ProgramName,
	cpu_time AS CPUMs,
	reads AS Reads,
	writes AS Writes,
	logical_reads AS LogicalReads,
	StatementText
FROM Detail
WHERE DurationRank <= @MaxPerInstance
OR CPURank <= @MaxPerInstance
OR ReadsRank <= @MaxPerInstance
OR WritesRank <= @MaxPerInstance
ORDER BY InstanceID, DurationSec DESC
