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
	   and longest, worked out from how many runs took each number of seconds rather than from every run.  A run started manually counts, unless it started at a step other than the job's start step: it
	   didn't do the whole job.  PartRuns is how many of those were left out.
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

/*	The runs not started by a schedule - usually few.  A schedule always starts the job at its start step, so only these need their first step looked up.
	The temp table gives that lookup an accurate row count: joined straight to all the runs, the estimate ran to millions of rows and the query asked for a huge memory grant */
SELECT	JH.instance_id,
		JH.job_id,
		JH.RunDateTime,
		JH.FinishDateTime,
		JH.RunDurationSec
INTO #NotScheduled
FROM dbo.JobHistory JH
JOIN dbo.Jobs J ON J.InstanceID = JH.InstanceID AND J.job_id = JH.job_id
WHERE JH.InstanceID = @InstanceID
AND JH.step_id = 0
AND JH.run_status = 1
AND JH.RunDurationSec IS NOT NULL
AND JH.RunDateTime >= DATEADD(d, -@DurationDays, @ToDate)
AND JH.RunDateTime < @ToDate
AND (@category IS NULL OR J.category = @category)
/* Binary collation: a leading wildcard LIKE over every run's message is several times faster, and SQL Agent always writes this text in the same case */
AND ISNULL(JH.message, N'') COLLATE Latin1_General_BIN2 NOT LIKE N'%invoked by Schedule %'

/* How many of each job's successful runs took each number of seconds, over the @DurationDays days - a busy job's runs
   take a few different times, so this is a few rows for thousands of runs */
SELECT	JH.job_id,
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
GROUP BY JH.job_id, JH.RunDurationSec

/* Runs started at a step other than the job's start step didn't do the whole job, so their times are left out.  Only a
   run not started by a schedule can (#NotScheduled): its first step is the step history row with the lowest instance_id
   in the run.  The job's steps are as collected now - a step removed since isn't looked for */
;WITH FirstStep AS (
	/* One seek per step of the job, on all of the index's key - a busy job has many thousands of step rows, and seeking on
	   the job alone read them all for each run */
	SELECT	N.job_id,
			N.RunDurationSec,
			F.step_id
	FROM #NotScheduled N
	CROSS APPLY (
		SELECT TOP(1) St.step_id
		FROM dbo.JobSteps St
		CROSS APPLY (
			SELECT TOP(1) S.instance_id
			FROM dbo.JobHistory S
			WHERE S.InstanceID = @InstanceID
			AND S.job_id = N.job_id
			AND S.step_id = St.step_id
			AND S.RunDateTime >= N.RunDateTime
			AND S.RunDateTime <= N.FinishDateTime
			AND S.instance_id < N.instance_id
			ORDER BY S.instance_id
		) S
		WHERE St.InstanceID = @InstanceID
		AND St.job_id = N.job_id
		ORDER BY S.instance_id
	) F
)
SELECT	F.job_id,
		F.RunDurationSec,
		COUNT(*) AS Runs
INTO #PartRuns
FROM FirstStep F
JOIN dbo.Jobs J ON J.InstanceID = @InstanceID AND J.job_id = F.job_id
WHERE F.step_id <> J.start_step_id
GROUP BY F.job_id, F.RunDurationSec

UPDATE D
	SET D.Runs = D.Runs - P.Runs
FROM #Durations D
JOIN #PartRuns P ON P.job_id = D.job_id AND P.RunDurationSec = D.RunDurationSec

;WITH Ranked AS (
	/* Where each duration comes in the job's whole runs from shortest to longest: the runs up to and including it */
	SELECT	job_id,
			RunDurationSec,
			SUM(Runs) OVER (PARTITION BY job_id ORDER BY RunDurationSec ROWS UNBOUNDED PRECEDING) AS ToRow
	FROM #Durations
	WHERE Runs > 0
), Totals AS (
	SELECT	job_id,
			SUM(Runs) AS Runs,
			SUM(RunDurationSec * CAST(Runs AS FLOAT)) / SUM(Runs) AS AvgSec,
			MAX(RunDurationSec) AS MaxSec,
			/* PERCENTILE_CONT(0.95) is the run at place P from the shortest (counting from 0), and P's fraction of the way to the next */
			0.95E0 * (SUM(Runs) - 1) AS P
	FROM #Durations
	WHERE Runs > 0
	GROUP BY job_id
)
SELECT	J.job_id,
		ISNULL(T.Runs, 0) AS Runs,
		T.AvgSec,
		Lo.RunDurationSec + (T.P - FLOOR(T.P)) * (Hi.RunDurationSec - Lo.RunDurationSec) AS P95Sec,
		T.MaxSec,
		ISNULL(PR.Runs, 0) AS PartRuns
FROM (SELECT job_id FROM #Durations GROUP BY job_id) J
LEFT JOIN Totals T ON T.job_id = J.job_id
OUTER APPLY (SELECT TOP(1) R.RunDurationSec FROM Ranked R WHERE R.job_id = J.job_id AND R.ToRow >= FLOOR(T.P) + 1 ORDER BY R.RunDurationSec) Lo
OUTER APPLY (SELECT TOP(1) R.RunDurationSec FROM Ranked R WHERE R.job_id = J.job_id AND R.ToRow >= CEILING(T.P) + 1 ORDER BY R.RunDurationSec) Hi
OUTER APPLY (SELECT SUM(P.Runs) AS Runs FROM #PartRuns P WHERE P.job_id = J.job_id) PR

SELECT	JH.job_id,
		JH.RunDateTime,
		CASE WHEN JH.FinishDateTime > JH.RunDateTime THEN JH.FinishDateTime ELSE DATEADD(s, 1, JH.RunDateTime) END AS FinishDateTime,
		JH.run_status,
		/* What started the run, e.g. "Schedule 12 (Nightly).", "Start Sequence." or "User DOMAIN\name." - so the plan only moves the runs a moving schedule started.
		   Just that first sentence: the rest ("The last step to run was...") is the same on every run and was most of the data, a week of a busy instance's runs being hundreds of thousands of rows */
		CASE WHEN P.Pos > 0 THEN LEFT(I.Rest, ISNULL(NULLIF(CHARINDEX(N'.  ' COLLATE Latin1_General_BIN2, I.Rest), 0), 300)) END AS InvokedBy
FROM dbo.JobHistory JH
/* Binary collation, as for #NotScheduled: searching every run's message in the database's collation was most of this statement's time */
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
