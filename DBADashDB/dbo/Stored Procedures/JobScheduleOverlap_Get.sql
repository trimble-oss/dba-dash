CREATE PROC dbo.JobScheduleOverlap_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@category NVARCHAR(128)=NULL,
	@DurationDays INT=14
)
AS
/*
	What the GUI needs to compare an instance's job schedules with what actually ran (Schedule overlap on the job timeline):

	1. The instance's jobs with their latest scripted definition (compressed DDL - the schedules are parsed from it) and
	   the instance's UTC offset, as schedules are in the server's local time.  Every job, whatever @category: a job in
	   another category can share a schedule, and moving the schedule moves it too.  InCategory marks the ones asked for.
	2. How long each job takes: successful executions over the @DurationDays days to @ToDate - average, 95th percentile
	   and longest, worked out from how many runs took each number of seconds rather than from every run.  The runs a
	   schedule started, as those are what the plan moves and predicts: a run started by hand, an alert or agent start can
	   be a rerun, a test or start part way through the job.  A job no schedule started in the period goes by all its runs
	   (FromScheduled = 0).  OtherRuns is how many runs not started by a schedule were left out.
	3. Every execution that overlaps @FromDate to @ToDate (job outcome rows), to count the overlaps that really happened,
	   with how it ended and what started it (the first sentence of the "invoked by" part of the outcome message).  Not in order.
	   Executions still running have no outcome row yet and are left out.  The 1 day lookback on the start limits the
	   partitions scanned, as in JobTimeline_Get.
	4. Each job's scripted definitions in force over the executions in 3 (the one current at the 1 day lookback, and any
	   collected since), so the GUI can tell when a schedule changed - a run before then didn't run to the schedule as it
	   is now.  SnapshotDate is when DBA Dash collected the version: the change was made between it and the version before.
*/
SET NOCOUNT ON
SET @DurationDays = CASE WHEN ISNULL(@DurationDays, 0) < 1 THEN 14 ELSE @DurationDays END

SELECT	J.job_id,
		J.name,
		CAST(J.enabled AS BIT) AS enabled,
		J.category,
		D.DDL,
		I.UTCOffset,
		CAST(CASE WHEN @category IS NULL OR J.category = @category THEN 1 ELSE 0 END AS BIT) AS InCategory
FROM dbo.Jobs J
JOIN dbo.Instances I ON I.InstanceID = J.InstanceID
LEFT JOIN dbo.DDL D ON D.DDLID = J.DDLID
WHERE J.InstanceID = @InstanceID
AND J.IsActive = 1
ORDER BY J.name

/* How many of each job's successful runs took each number of seconds, over the @DurationDays days, by whether a
   schedule started them - a busy job's runs take a few different times, so this is a few rows for thousands of runs */
SELECT	JH.job_id,
		/* Binary collation: a leading wildcard LIKE over every run's message is several times faster, and SQL Agent always
		   writes this text in the same case.  On a server whose Agent writes its messages in another language, no run
		   matches, and every job goes by all its runs */
		CAST(CASE WHEN ISNULL(JH.message, N'') COLLATE Latin1_General_BIN2 LIKE N'%invoked by Schedule %' THEN 1 ELSE 0 END AS BIT) AS Scheduled,
		JH.RunDurationSec,
		COUNT(*) AS Runs
INTO #Durations
FROM dbo.JobHistory JH
JOIN dbo.Jobs J ON J.InstanceID = JH.InstanceID AND J.job_id = JH.job_id
WHERE JH.InstanceID = @InstanceID
AND JH.step_id = 0
AND JH.run_status = 1
AND JH.RunDurationSec IS NOT NULL
AND JH.RunDateTime >= DATEADD(d, -@DurationDays, @ToDate)
AND JH.RunDateTime < @ToDate
AND (@category IS NULL OR J.category = @category)
GROUP BY JH.job_id,
		CAST(CASE WHEN ISNULL(JH.message, N'') COLLATE Latin1_General_BIN2 LIKE N'%invoked by Schedule %' THEN 1 ELSE 0 END AS BIT),
		JH.RunDurationSec

;WITH Used AS (
	/* The runs a schedule started - or every run, for a job no schedule started */
	SELECT	D.job_id,
			D.Scheduled,
			D.RunDurationSec,
			D.Runs
	FROM #Durations D
	WHERE D.Scheduled = 1
	OR NOT EXISTS(SELECT 1 FROM #Durations S WHERE S.job_id = D.job_id AND S.Scheduled = 1)
), Ranked AS (
	/* Where each duration comes in the job's runs from shortest to longest: the runs up to and including it */
	SELECT	job_id,
			RunDurationSec,
			SUM(Runs) OVER (PARTITION BY job_id ORDER BY RunDurationSec ROWS UNBOUNDED PRECEDING) AS ToRow
	FROM Used
), Totals AS (
	SELECT	job_id,
			SUM(Runs) AS Runs,
			SUM(RunDurationSec * CAST(Runs AS FLOAT)) / SUM(Runs) AS AvgSec,
			MAX(RunDurationSec) AS MaxSec,
			/* PERCENTILE_CONT(0.95) is the run at place P from the shortest (counting from 0), and P's fraction of the way to the next */
			0.95E0 * (SUM(Runs) - 1) AS P,
			CAST(MAX(CAST(Scheduled AS INT)) AS BIT) AS FromScheduled
	FROM Used
	GROUP BY job_id
)
SELECT	T.job_id,
		T.Runs,
		T.AvgSec,
		Lo.RunDurationSec + (T.P - FLOOR(T.P)) * (Hi.RunDurationSec - Lo.RunDurationSec) AS P95Sec,
		T.MaxSec,
		T.FromScheduled,
		A.Runs - T.Runs AS OtherRuns
FROM Totals T
CROSS APPLY (SELECT SUM(D.Runs) AS Runs FROM #Durations D WHERE D.job_id = T.job_id) A
OUTER APPLY (SELECT TOP(1) R.RunDurationSec FROM Ranked R WHERE R.job_id = T.job_id AND R.ToRow >= FLOOR(T.P) + 1 ORDER BY R.RunDurationSec) Lo
OUTER APPLY (SELECT TOP(1) R.RunDurationSec FROM Ranked R WHERE R.job_id = T.job_id AND R.ToRow >= CEILING(T.P) + 1 ORDER BY R.RunDurationSec) Hi

SELECT	JH.job_id,
		JH.RunDateTime,
		CASE WHEN JH.FinishDateTime > JH.RunDateTime THEN JH.FinishDateTime ELSE DATEADD(s, 1, JH.RunDateTime) END AS FinishDateTime,
		JH.run_status,
		/* What started the run, e.g. "Schedule 12 (Nightly).", "Start Sequence." or "User DOMAIN\name." - so the plan only moves the runs a moving schedule started.
		   Just that first sentence: the rest ("The last step to run was...") is the same on every run and was most of the data, a week of a busy instance's runs being hundreds of thousands of rows */
		CASE WHEN P.Pos > 0 THEN LEFT(I.Rest, ISNULL(NULLIF(CHARINDEX(N'.  ' COLLATE Latin1_General_BIN2, I.Rest), 0), 300)) END AS InvokedBy
FROM dbo.JobHistory JH
/* Binary collation, as for #Durations: searching every run's message in the database's collation was most of this statement's time */
CROSS APPLY (SELECT CHARINDEX(N'invoked by ' COLLATE Latin1_General_BIN2, JH.message COLLATE Latin1_General_BIN2) AS Pos) P
CROSS APPLY (SELECT SUBSTRING(JH.message, P.Pos + 11, 300) COLLATE Latin1_General_BIN2 AS Rest) I
WHERE JH.InstanceID = @InstanceID
AND JH.step_id = 0
AND JH.RunDateTime >= DATEADD(d, -1, @FromDate)
AND JH.RunDateTime < @ToDate
AND JH.FinishDateTime > @FromDate
AND (@category IS NULL OR EXISTS(SELECT 1 FROM dbo.Jobs J WHERE J.InstanceID = JH.InstanceID AND J.job_id = JH.job_id AND J.category = @category))
/* No ORDER BY: the GUI orders each job's runs itself, and sorting every run took a large memory grant */

SELECT	H.job_id,
		H.SnapshotDate,
		D.DDL
FROM dbo.JobDDLHistory H
JOIN dbo.DDL D ON D.DDLID = H.DDLID
WHERE H.InstanceID = @InstanceID
AND H.SnapshotDate < @ToDate
AND H.SnapshotDate >= ISNULL((
		SELECT MAX(P.SnapshotDate)
		FROM dbo.JobDDLHistory P
		WHERE P.InstanceID = H.InstanceID
		AND P.job_id = H.job_id
		AND P.SnapshotDate <= DATEADD(d, -1, @FromDate)
	), '19000101')
AND EXISTS(SELECT 1 FROM dbo.Jobs J WHERE J.InstanceID = H.InstanceID AND J.job_id = H.job_id AND J.IsActive = 1 AND (@category IS NULL OR J.category = @category))
ORDER BY H.job_id, H.SnapshotDate
