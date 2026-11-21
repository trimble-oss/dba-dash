/*
	A row for each completed elastic job execution, in the shape of dbo.ElasticJobStats_60MIN - for charts grouped by less than an hour.
	Counts the same executions as dbo.ElasticJobs_Upd does for the hourly stats.
	create_time is for a range on the index - RunDateTime (when it started, or was created if it never did) can't use one.  An execution
	starts after it's created, so RunDateTime < @ToDate means create_time < @ToDate, and readers allow a day for it to start.
*/
CREATE VIEW dbo.ElasticJobStats_RAW
AS
SELECT	E.InstanceID,
		E.job_id,
		E.step_id,
		ISNULL(E.start_time,E.create_time) AS RunDateTime,
		CAST(O.IsFailed AS INT) AS FailedCount,
		CAST(O.IsSucceeded AS INT) AS SucceededCount,
		CAST(O.IsSkipped AS INT) AS SkippedCount,
		CASE WHEN E.step_id > 0 AND E.current_attempts > 1 THEN E.current_attempts - 1 ELSE 0 END AS RetryCount,
		CASE WHEN D.DurationSec IS NULL THEN 0 ELSE 1 END AS TimedCount,
		CAST(ISNULL(D.DurationSec,0) AS BIGINT) AS RunDurationSec,
		D.DurationSec AS MaxRunDurationSec,
		D.DurationSec AS MinRunDurationSec,
		E.create_time
FROM dbo.ElasticJobExecutions E
CROSS APPLY dbo.ElasticJobOutcome(E.lifecycle,E.is_active) O
CROSS APPLY(SELECT DATEDIFF(s,E.start_time,E.end_time) AS DurationSec) D
WHERE E.is_active = 0
AND (E.step_id = 0 OR E.target_type = N'SqlDatabase' OR O.IsFailed = 1)
