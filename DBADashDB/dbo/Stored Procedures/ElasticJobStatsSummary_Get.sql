/*
	Elastic job execution stats for the period, by job (step 0) and step - the grid on the Elastic Job Stats tab.  The same shape as
	dbo.JobStatsSummary_Get.  A step's counts are of its executions against each target.

	The whole hours inside the window come from dbo.ElasticJobStats_60MIN and only the part hours at either end from the executions
	(dbo.ElasticJobStats_RAW), as dbo.RollupRanges splits it.  An hour of the rollup is exactly the executions that started in it, so
	the counts are exact for close to the cost of reading the rollup alone.  A part hour whose executions have been purged is left out.
	@Use60MIN 1 reads it all from the rollup (part hours left out) and 0 all from the executions.

	Executions are read on a range of create_time as well as when they started, which no index covers - see dbo.ElasticJobStats_RAW.
*/
CREATE PROC dbo.ElasticJobStatsSummary_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER=NULL,
	@StepID INT=NULL,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@Use60MIN BIT=NULL
)
AS
SET NOCOUNT ON
IF @JobID = '00000000-0000-0000-0000-000000000000' SET @JobID = NULL

DECLARE @HourFrom DATETIME2(3), @HourTo DATETIME2(3)
SELECT	@HourFrom = HourFrom,
		@HourTo = HourTo
FROM dbo.RollupRanges(@FromDate, @ToDate, @Use60MIN)

;WITH S AS (
	/* The part hour at the start of the window, from the executions */
	SELECT R.job_id, R.step_id, R.FailedCount, R.SucceededCount, R.SkippedCount, R.RetryCount, R.TimedCount, R.RunDurationSec, R.MaxRunDurationSec, R.MinRunDurationSec
	FROM dbo.ElasticJobStats_RAW R
	WHERE R.InstanceID = @InstanceID
	AND R.RunDateTime >= @FromDate
	AND R.RunDateTime < @HourFrom
	AND R.create_time >= DATEADD(d,-1,@FromDate)
	AND R.create_time < @HourFrom
	UNION ALL
	/* The whole hours, from the rollup */
	SELECT H.job_id, H.step_id, H.FailedCount, H.SucceededCount, H.SkippedCount, H.RetryCount, H.TimedCount, H.RunDurationSec, H.MaxRunDurationSec, H.MinRunDurationSec
	FROM dbo.ElasticJobStats_60MIN H
	WHERE H.InstanceID = @InstanceID
	AND H.RunDateTime >= @HourFrom
	AND H.RunDateTime < @HourTo
	UNION ALL
	/* The part hour at the end */
	SELECT R.job_id, R.step_id, R.FailedCount, R.SucceededCount, R.SkippedCount, R.RetryCount, R.TimedCount, R.RunDurationSec, R.MaxRunDurationSec, R.MinRunDurationSec
	FROM dbo.ElasticJobStats_RAW R
	WHERE R.InstanceID = @InstanceID
	AND R.RunDateTime >= @HourTo
	AND R.RunDateTime < @ToDate
	AND R.create_time >= DATEADD(d,-1,@HourTo)
	AND R.create_time < @ToDate
),
T AS (
	SELECT	S.job_id,
			S.step_id,
			SUM(S.FailedCount) AS FailedCount,
			SUM(S.SucceededCount) AS SucceededCount,
			SUM(S.SkippedCount) AS SkippedCount,
			SUM(S.RetryCount) AS RetryCount,
			CAST(SUM(S.RunDurationSec) / NULLIF(SUM(S.TimedCount),0) AS INT) AS AvgDurationSec,
			MAX(S.MaxRunDurationSec) AS MaxDurationSec,
			MIN(S.MinRunDurationSec) AS MinDurationSec,
			SUM(S.RunDurationSec) AS TotalDurationSec
	FROM S
	WHERE (@JobID IS NULL OR S.job_id = @JobID)
	AND (@StepID IS NULL OR S.step_id = @StepID)
	GROUP BY S.job_id, S.step_id
)
SELECT	T.job_id,
		T.step_id,
		ISNULL(J.job_name,CAST(T.job_id AS NVARCHAR(128))) AS JobName,
		/* A step that has since been removed: the name it last ran with */
		CASE WHEN T.step_id = 0 THEN '{outcome}' ELSE COALESCE(step.step_name,LastRun.step_name,'Step ' + CAST(T.step_id AS NVARCHAR(10))) END AS JobStep,
		T.FailedCount,
		T.SucceededCount,
		T.SkippedCount,
		T.RetryCount,
		T.AvgDurationSec,
		T.MaxDurationSec,
		T.MinDurationSec,
		T.TotalDurationSec,
		HDAvg.HumanDuration AS AvgDuration,
		HDMax.HumanDuration AS MaxDuration,
		HDMin.HumanDuration AS MinDuration,
		HDTotal.HumanDuration AS TotalDuration,
		@InstanceID AS InstanceID
FROM T
LEFT JOIN dbo.ElasticJobs J ON J.InstanceID = @InstanceID AND J.job_id = T.job_id
LEFT JOIN dbo.ElasticJobSteps step ON step.InstanceID = @InstanceID AND step.job_id = T.job_id AND step.step_id = T.step_id
OUTER APPLY(SELECT TOP(1) E.step_name
			FROM dbo.ElasticJobExecutions E
			WHERE T.step_id > 0
			AND step.step_name IS NULL
			AND E.InstanceID = @InstanceID
			AND E.job_id = T.job_id
			AND E.step_id = T.step_id
			AND E.step_name IS NOT NULL
			ORDER BY E.create_time DESC) LastRun
CROSS APPLY dbo.SecondsToHumanDuration(T.AvgDurationSec) HDAvg
CROSS APPLY dbo.SecondsToHumanDuration(T.MaxDurationSec) HDMax
CROSS APPLY dbo.SecondsToHumanDuration(T.MinDurationSec) HDMin
CROSS APPLY dbo.SecondsToHumanDuration(T.TotalDurationSec) HDTotal
ORDER BY JobName, T.step_id
OPTION(RECOMPILE)
GO
GRANT EXECUTE
	ON OBJECT::[dbo].[ElasticJobStatsSummary_Get] TO [Reports]
	AS [dbo];
