/*
	Elastic job execution stats over time for a job (step 0) or one of its steps - the chart on the Elastic Job Stats tab.
	The same shape as dbo.JobStats_Get.

	Grouped by whole hours, the whole hours inside the window come from dbo.ElasticJobStats_60MIN and only the part hours at either end
	from the executions (dbo.ElasticJobStats_RAW), as dbo.RollupRanges splits it.  An hour of the rollup is exactly the executions that
	started in it, so the counts are exact for close to the cost of reading the rollup alone.  Grouped by less than an hour, it's all
	read from the executions - unless the period is older than they're kept for, when it's grouped by the hour instead.  A part hour
	whose executions have been purged is left out.

	Executions are read on a range of create_time as well as when they started, which no index covers - see dbo.ElasticJobStats_RAW.
*/
CREATE PROC dbo.ElasticJobStats_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT OUT
)
AS
SET NOCOUNT ON
SELECT @DateGroupingMin = CASE WHEN EXISTS(	SELECT 1
											FROM dbo.DataRetention
											WHERE SchemaName = 'dbo'
											AND TableName = 'ElasticJobExecutions'
											AND RetentionDays > 0
											AND @FromDate < DATEADD(d,-RetentionDays,GETUTCDATE())
											AND @DateGroupingMin < 60
											) THEN 60
							ELSE @DateGroupingMin END

/* Whole hours from the rollup where the grouping is of whole hours, otherwise all from the executions */
DECLARE @HourFrom DATETIME2(3), @HourTo DATETIME2(3)
SELECT	@HourFrom = HourFrom,
		@HourTo = HourTo
FROM dbo.RollupRanges(@FromDate, @ToDate, CASE WHEN @DateGroupingMin >= 60 AND @DateGroupingMin % 60 = 0 THEN NULL ELSE 0 END)

SELECT	G.DateGroup,
		SUM(JS.FailedCount) AS FailedCount,
		SUM(JS.SucceededCount) AS SucceededCount,
		SUM(JS.SkippedCount) AS SkippedCount,
		SUM(JS.RetryCount) AS RetryCount,
		SUM(JS.RunDurationSec) / NULLIF(SUM(JS.TimedCount),0) AS AvgDurationSec,
		MAX(JS.MaxRunDurationSec) AS MaxDurationSec,
		MIN(JS.MinRunDurationSec) AS MinDurationSec,
		SUM(JS.RunDurationSec) AS TotalDurationSec
FROM (	/* The part hour at the start of the window, from the executions */
		SELECT R.RunDateTime, R.FailedCount, R.SucceededCount, R.SkippedCount, R.RetryCount, R.TimedCount, R.RunDurationSec, R.MaxRunDurationSec, R.MinRunDurationSec
		FROM dbo.ElasticJobStats_RAW R
		WHERE R.InstanceID = @InstanceID
		AND R.job_id = @JobID
		AND R.step_id = @StepID
		AND R.RunDateTime >= @FromDate
		AND R.RunDateTime < @HourFrom
		AND R.create_time >= DATEADD(d,-1,@FromDate)
		AND R.create_time < @HourFrom
		UNION ALL
		/* The whole hours, from the rollup */
		SELECT H.RunDateTime, H.FailedCount, H.SucceededCount, H.SkippedCount, H.RetryCount, H.TimedCount, H.RunDurationSec, H.MaxRunDurationSec, H.MinRunDurationSec
		FROM dbo.ElasticJobStats_60MIN H
		WHERE H.InstanceID = @InstanceID
		AND H.job_id = @JobID
		AND H.step_id = @StepID
		AND H.RunDateTime >= @HourFrom
		AND H.RunDateTime < @HourTo
		UNION ALL
		/* The part hour at the end */
		SELECT R.RunDateTime, R.FailedCount, R.SucceededCount, R.SkippedCount, R.RetryCount, R.TimedCount, R.RunDurationSec, R.MaxRunDurationSec, R.MinRunDurationSec
		FROM dbo.ElasticJobStats_RAW R
		WHERE R.InstanceID = @InstanceID
		AND R.job_id = @JobID
		AND R.step_id = @StepID
		AND R.RunDateTime >= @HourTo
		AND R.RunDateTime < @ToDate
		AND R.create_time >= DATEADD(d,-1,@HourTo)
		AND R.create_time < @ToDate
	) JS
CROSS APPLY dbo.DateGroupingMins(JS.RunDateTime,@DateGroupingMin) G /* 0 passes the time through - each execution */
GROUP BY G.DateGroup
ORDER BY G.DateGroup
OPTION(RECOMPILE)
GO
GRANT EXECUTE
	ON OBJECT::[dbo].[ElasticJobStats_Get] TO [Reports]
	AS [dbo];
