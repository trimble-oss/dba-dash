/*
	What the lifecycle of an elastic job execution (jobs.job_executions) means, in SQL Agent job history terms where that helps reuse.

	run_status, as for SQL Agent job history: 0 = Failed, 1 = Succeeded, 2 = Retry, 3 = Canceled (or skipped), 4 = In Progress.
		Failed, TimedOut           0 - failed.  An unknown lifecycle of an execution that is no longer active is also treated as failed.
		Succeeded,
		SucceededWithSkipped       1 - succeeded.  "With skipped": some of the executions beneath it were skipped.
		WaitingForRetry            2 - failed an attempt and is waiting to try again.
		Canceled, Skipped          3 - Skipped means another execution of the step was still running against the target.
		Created, InProgress,
		WaitingForChildJobExecutions, or anything else that is active    4 - in progress.

	IsFailed counts canceled executions as failures, as SQL Agent job stats count canceled jobs.  Skipped executions are neither
	failures nor successes.
*/
CREATE FUNCTION dbo.ElasticJobOutcome(
	@lifecycle NVARCHAR(50),
	@is_active BIT
)
RETURNS TABLE
AS
RETURN
SELECT	CAST(R.run_status AS INT) AS run_status,
		CAST(CASE WHEN @is_active = 0 AND R.run_status IN(0,3) AND @lifecycle <> N'Skipped' THEN 1 ELSE 0 END AS BIT) AS IsFailed,
		CAST(CASE WHEN @is_active = 0 AND R.run_status = 1 THEN 1 ELSE 0 END AS BIT) AS IsSucceeded,
		CAST(CASE WHEN @is_active = 0 AND @lifecycle = N'Skipped' THEN 1 ELSE 0 END AS BIT) AS IsSkipped,
		CASE WHEN @lifecycle = N'Skipped' THEN 'Skipped' /* Not canceled - the previous execution was still running */
			ELSE CASE R.run_status WHEN 0 THEN 'Failed' WHEN 1 THEN 'Succeeded' WHEN 2 THEN 'Retry' WHEN 3 THEN 'Canceled' ELSE 'In Progress' END
			END AS run_status_description
FROM (
	SELECT CASE	WHEN @lifecycle IN(N'Succeeded',N'SucceededWithSkipped') THEN 1
				WHEN @lifecycle IN(N'Failed',N'TimedOut') THEN 0
				WHEN @lifecycle IN(N'Canceled',N'Skipped') THEN 3
				WHEN @lifecycle = N'WaitingForRetry' THEN 2
				WHEN @is_active = 1 THEN 4
				ELSE 0 END AS run_status
	) R
