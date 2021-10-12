CREATE VIEW dbo.JobStats_RAW
AS
SELECT jh.InstanceID,
	jh.job_id,
	jh.step_id,
	RunDateTime,
	SUM(CASE WHEN run_status IN(0,3) THEN 1 ELSE 0 END) as FailedCount,
	SUM(CASE WHEN run_status=1 THEN 1 ELSE 0 END) as SucceededCount,
	ISNULL(SUM(retries_attempted),0) as RetryCount,
	ISNULL(SUM(RunDurationSec),0) as RunDurationSec,
	ISNULL(MAX(RunDurationSec),0) as MaxRunDurationSec,
	ISNULL(MIN(RunDurationSec),0) as MinRunDurationSec
FROM dbo.JobHistory jh
GROUP BY InstanceID,
		job_id,
		step_id,
		jh.RunDateTime