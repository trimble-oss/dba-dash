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
	   and longest.  A run started manually counts, unless it started at a step other than the job's start step: it
	   didn't do the whole job.  PartRuns is how many of those were left out.
	3. Every execution that overlaps @FromDate to @ToDate (job outcome rows), to count the overlaps that really happened,
	   with how it ended and what started it (the "invoked by" part of the outcome message).
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

SELECT	job_id,
		COUNT(DurationSec) AS Runs,
		AVG(CAST(DurationSec AS FLOAT)) AS AvgSec,
		MAX(P95Sec) AS P95Sec,
		MAX(DurationSec) AS MaxSec,
		SUM(CAST(PartRun AS INT)) AS PartRuns
FROM (
	/* A run started at a step other than the job's start step didn't do the whole job, so its duration is left out (PERCENTILE_CONT, AVG and MAX ignore NULLs) */
	SELECT	R.job_id,
			CASE WHEN R.PartRun = 0 THEN R.RunDurationSec END AS DurationSec,
			R.PartRun,
			PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY CASE WHEN R.PartRun = 0 THEN R.RunDurationSec END) OVER (PARTITION BY R.job_id) AS P95Sec
	FROM (
		SELECT	JH.job_id,
				JH.RunDurationSec,
				CASE WHEN FS.step_id <> J.start_step_id THEN 1 ELSE 0 END AS PartRun
		FROM dbo.JobHistory JH
		JOIN dbo.Jobs J ON J.InstanceID = JH.InstanceID AND J.job_id = JH.job_id
		/* The execution's first step.  A schedule always starts the job at its start step, so only the other runs (a user, an alert, agent start - or a message that doesn't say) are looked at */
		OUTER APPLY(SELECT TOP(1) S.step_id
					FROM dbo.JobHistory S
					WHERE ISNULL(JH.message, N'') NOT LIKE N'%invoked by Schedule %'
					AND S.InstanceID = JH.InstanceID
					AND S.job_id = JH.job_id
					AND S.step_id > 0
					AND S.RunDateTime >= JH.RunDateTime
					AND S.RunDateTime <= JH.FinishDateTime
					AND S.instance_id < JH.instance_id
					ORDER BY S.instance_id
					) FS
		WHERE JH.InstanceID = @InstanceID
		AND JH.step_id = 0
		AND JH.run_status = 1
		AND JH.RunDurationSec IS NOT NULL
		AND JH.RunDateTime >= DATEADD(d, -@DurationDays, @ToDate)
		AND JH.RunDateTime < @ToDate
		AND (@category IS NULL OR J.category = @category)
	) R
) T
GROUP BY job_id

SELECT	JH.job_id,
		JH.RunDateTime,
		CASE WHEN JH.FinishDateTime > JH.RunDateTime THEN JH.FinishDateTime ELSE DATEADD(s, 1, JH.RunDateTime) END AS FinishDateTime,
		JH.run_status,
		/* What started the run, e.g. "Schedule 12 (Nightly).  The last step...", "Start Sequence." or "User DOMAIN\name." - so the plan only moves the runs a moving schedule started */
		CASE WHEN CHARINDEX(N'invoked by ', JH.message) > 0 THEN SUBSTRING(JH.message, CHARINDEX(N'invoked by ', JH.message) + 11, 300) END AS InvokedBy
FROM dbo.JobHistory JH
WHERE JH.InstanceID = @InstanceID
AND JH.step_id = 0
AND JH.RunDateTime >= DATEADD(d, -1, @FromDate)
AND JH.RunDateTime < @ToDate
AND JH.FinishDateTime > @FromDate
AND (@category IS NULL OR EXISTS(SELECT 1 FROM dbo.Jobs J WHERE J.InstanceID = JH.InstanceID AND J.job_id = JH.job_id AND J.category = @category))
ORDER BY JH.job_id, JH.RunDateTime

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
