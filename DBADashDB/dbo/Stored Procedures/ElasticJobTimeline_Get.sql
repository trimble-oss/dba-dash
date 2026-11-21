/*
	Elastic job executions for the timeline - the same columns as dbo.JobTimeline_Get, so the timeline chart takes them as it does
	SQL Agent jobs.  run_status is from dbo.ElasticJobOutcome.

	Executions that ran at any time in the period: started before @ToDate and finished (or still running) after @FromDate.  An
	execution that is still running ends at the time of the last collection (@AsOf, or now if that's earlier) - or where it started,
	if that's later (the collector's clock and the job database's can differ).
	Executions created more than a day before @FromDate are left out to limit what is read, as for SQL Agent jobs - except those
	still running, which would otherwise be missing from the timeline however long they've been running.

	@IncludeSteps adds the executions of each step against its targets - the databases the step ran against, and any other target
	that failed.  A step's executions against many databases run at the same time, so its row has a bar for each.
	@MergeGapSec, @MinDurationSec and @ProblemsOnly are as for dbo.JobTimeline_Get.
	@MaxRows: merge (with @MergeGapSec) only where there are more executions than this - so the GUI gets each execution when there
	aren't too many to draw, and merged spans when there are, in one call rather than fetching them all to find out.
	instance_id (the SQL Agent job history ID) is always NULL.  job_execution_id identifies a single execution.  Unmerged, each step
	execution also has its target, so the timeline can show a row per target beneath the step, whether it's still active (is_active -
	run_status is its outcome so far, Failed for a target waiting for a retry, say), and its start in UTC (RunDateTimeUtc).
*/
CREATE PROC dbo.ElasticJobTimeline_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@IncludeSteps BIT=0,
	@IncludeOutcome BIT=1,
	@job_id UNIQUEIDENTIFIER=NULL,
	@MergeGapSec INT=0,
	@MinDurationSec INT=0,
	@ProblemsOnly BIT=0,
	@MaxRows INT=NULL
)
AS
SET NOCOUNT ON
DECLARE @AsOf DATETIME2(2)
SET @MergeGapSec = ISNULL(@MergeGapSec,0)
SET @MinDurationSec = ISNULL(@MinDurationSec,0)

/* Running executions end where they were last seen running */
SELECT @AsOf = CASE WHEN CD.SnapshotDate IS NULL OR CD.SnapshotDate > SYSUTCDATETIME() THEN CAST(SYSUTCDATETIME() AS DATETIME2(2)) ELSE CAST(CD.SnapshotDate AS DATETIME2(2)) END
FROM (VALUES(1)) T(n)
LEFT JOIN dbo.CollectionDates CD ON CD.InstanceID = @InstanceID AND CD.Reference = 'ElasticJobs'

SELECT	J.job_name AS name,
		E.job_id,
		CAST(NULL AS INT) AS instance_id,
		E.job_execution_id,
		CASE WHEN E.step_id = 0 THEN N'Outcome' ELSE COALESCE(S.step_name,E.step_name,N'Step ' + CAST(E.step_id AS NVARCHAR(10))) END AS step_name, /* Current name first, so a renamed step stays one row */
		E.step_id,
		B.RunDateTime,
		DATEDIFF(s,B.RunDateTime,B.FinishDateTime) AS RunDurationSec,
		CASE WHEN B.FinishDateTime <= B.RunDateTime THEN DATEADD(s,1,B.RunDateTime) ELSE B.FinishDateTime END AS FinishDateTime,
		O.run_status,
		1 AS Executions,
		E.lifecycle,
		E.target_type,
		E.target_server_name,
		E.target_elastic_pool_name,
		E.target_database_name,
		E.is_active,
		B.RunDateTime AS RunDateTimeUtc /* Stays UTC when RunDateTime is shown in the app's time zone - for durations across a DST change */
INTO #Timeline
FROM (
	SELECT	E.job_id, E.job_execution_id, E.step_id, E.lifecycle, E.is_active, E.create_time, E.start_time, E.end_time,
			E.target_type, E.target_server_name, E.target_elastic_pool_name, E.target_database_name, E.step_name
	FROM dbo.ElasticJobExecutions E
	WHERE E.InstanceID = @InstanceID
	AND E.create_time >= DATEADD(d,-1,@FromDate)
	AND E.create_time < @ToDate
	UNION ALL
	/* Still running, from before the lookback.  Uses the filtered index of active executions. */
	SELECT	E.job_id, E.job_execution_id, E.step_id, E.lifecycle, E.is_active, E.create_time, E.start_time, E.end_time,
			E.target_type, E.target_server_name, E.target_elastic_pool_name, E.target_database_name, E.step_name
	FROM dbo.ElasticJobExecutions E
	WHERE E.InstanceID = @InstanceID
	AND E.is_active = 1
	AND E.create_time < DATEADD(d,-1,@FromDate)
	) E
JOIN dbo.ElasticJobs J ON J.InstanceID = @InstanceID AND J.job_id = E.job_id
LEFT JOIN dbo.ElasticJobSteps S ON S.InstanceID = @InstanceID AND S.job_id = E.job_id AND S.step_id = E.step_id
CROSS APPLY dbo.ElasticJobOutcome(E.lifecycle,E.is_active) O
CROSS APPLY(SELECT ISNULL(E.start_time,E.create_time) AS RunDateTime) R
CROSS APPLY(SELECT	R.RunDateTime,
					CASE WHEN E.is_active = 0 THEN COALESCE(E.end_time,E.start_time,E.create_time)
						WHEN @AsOf > R.RunDateTime THEN @AsOf
						ELSE R.RunDateTime END AS FinishDateTime
			) B
WHERE B.RunDateTime < @ToDate
AND B.FinishDateTime >= @FromDate
AND (@job_id IS NULL OR E.job_id = @job_id)
AND (
		(@IncludeOutcome = 1 AND E.step_id = 0)
		OR (@IncludeSteps = 1 AND E.step_id > 0 AND (E.target_type = N'SqlDatabase' OR O.IsFailed = 1))
	)
AND (@ProblemsOnly = 0 OR O.run_status IN(0,2,3))
AND DATEDIFF(s,B.RunDateTime,B.FinishDateTime) >= @MinDurationSec
OPTION(RECOMPILE)
DECLARE @Rows INT = @@ROWCOUNT

IF @MaxRows IS NOT NULL AND @Rows <= @MaxRows
BEGIN
	SET @MergeGapSec = 0 /* Few enough to show each one */
END

IF @MergeGapSec > 0
BEGIN
	/* Merge executions of the same job and step that overlap or are within @MergeGapSec of each other (gaps and islands) */
	;WITH Ordered AS (
		SELECT	*,
				MAX(FinishDateTime) OVER (PARTITION BY name, step_id ORDER BY RunDateTime, FinishDateTime ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) AS PrevFinish
		FROM #Timeline
	),
	Islands AS (
		SELECT	*,
				SUM(CASE WHEN PrevFinish IS NOT NULL AND DATEADD(s, @MergeGapSec, PrevFinish) >= RunDateTime THEN 0 ELSE 1 END)
					OVER (PARTITION BY name, step_id ORDER BY RunDateTime, FinishDateTime ROWS UNBOUNDED PRECEDING) AS Island
		FROM Ordered
	)
	SELECT	name,
			job_id,
			CAST(NULL AS INT) AS instance_id,
			CASE WHEN COUNT(*) = 1 THEN MAX(CAST(job_execution_id AS BINARY(16))) END AS job_execution_id_bin,
			MAX(step_name) AS step_name,
			step_id,
			MIN(RunDateTime) AS RunDateTime,
			CAST(AVG(CAST(RunDurationSec AS BIGINT)) AS INT) AS RunDurationSec, /* Summed as BIGINT - 20,000 runs of 30 hours overflow an INT */
			MAX(FinishDateTime) AS FinishDateTime,
			CASE MAX(CASE run_status WHEN 0 THEN 5 WHEN 2 THEN 4 WHEN 3 THEN 3 WHEN 4 THEN 2 ELSE 1 END) WHEN 5 THEN 0 WHEN 4 THEN 2 WHEN 3 THEN 3 WHEN 2 THEN 4 ELSE 1 END AS run_status,
			SUM(Executions) AS Executions,
			/* Runs by status, so the merged row's worst status isn't taken as the status of every run in it */
			SUM(CASE WHEN run_status = 0 THEN Executions ELSE 0 END) AS FailedRuns,
			SUM(CASE WHEN run_status = 1 THEN Executions ELSE 0 END) AS SucceededRuns,
			SUM(CASE WHEN run_status = 2 THEN Executions ELSE 0 END) AS RetryRuns,
			SUM(CASE WHEN run_status = 3 THEN Executions ELSE 0 END) AS CancelledRuns,
			SUM(CASE WHEN run_status = 4 THEN Executions ELSE 0 END) AS RunningRuns
	INTO #Merged
	FROM Islands
	GROUP BY name, job_id, step_id, Island

	SELECT	name,
			job_id,
			instance_id,
			CAST(job_execution_id_bin AS UNIQUEIDENTIFIER) AS job_execution_id,
			step_name,
			step_id,
			RunDateTime,
			RunDurationSec,
			FinishDateTime,
			run_status,
			Executions,
			FailedRuns,
			SucceededRuns,
			RetryRuns,
			CancelledRuns,
			RunningRuns
	FROM #Merged
	ORDER BY name,
			step_id,
			RunDateTime
END
ELSE
BEGIN
	SELECT	name,
			job_id,
			instance_id,
			job_execution_id,
			step_name,
			step_id,
			RunDateTime,
			RunDurationSec,
			FinishDateTime,
			run_status,
			Executions,
			lifecycle,
			target_type,
			target_server_name,
			target_elastic_pool_name,
			target_database_name,
			is_active,
			RunDateTimeUtc
	FROM #Timeline
	ORDER BY name,
			step_id,
			RunDateTime
END
GO
GRANT EXECUTE
	ON OBJECT::[dbo].[ElasticJobTimeline_Get] TO [Reports]
	AS [dbo];
