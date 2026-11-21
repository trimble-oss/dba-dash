/*
	Execution history of an elastic job.

	@StepID 0 (the default) is the job's executions - one row each.  NULL is every row: the job's executions and the executions of its
	steps against each target.  A step ID is that step's executions against each target.
	@job_execution_id: the executions of the steps of one job execution, against each target.  Rows of an execution share its
	job_execution_id.  Where none do, the job's step executions created while it ran are returned instead.  An ID that isn't a job
	execution's (a step's execution against a target) returns the rows with that ID.
	@FailedOnly leaves out executions that succeeded, or are in progress.

	Executions that are still running have their duration so far.  Times are UTC.
*/
CREATE PROC dbo.ElasticJobHistory_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT=0,
	@job_execution_id UNIQUEIDENTIFIER=NULL,
	@Top INT=500,
	@FailedOnly BIT=0
)
AS
SET NOCOUNT ON
DECLARE @AsOf DATETIME2(2) = SYSUTCDATETIME()
DECLARE @ExecutionFrom DATETIME2(2)
DECLARE @ExecutionTo DATETIME2(2)
DECLARE @ByExecutionID BIT = 0

IF @job_execution_id IS NOT NULL
BEGIN
	SET @StepID = NULL
	SELECT TOP(1)	@ExecutionFrom = E.create_time,
					@ExecutionTo = ISNULL(E.end_time,@AsOf)
	FROM dbo.ElasticJobExecutions E
	WHERE E.InstanceID = @InstanceID
	AND E.job_id = @JobID
	AND E.step_id = 0
	AND E.job_execution_id = @job_execution_id

	IF @ExecutionFrom IS NULL
	BEGIN
		/* Not the ID of a job execution - the ID of a step's execution against a target, say.  The rows with that ID. */
		SELECT TOP(1) @ExecutionFrom = E.create_time
		FROM dbo.ElasticJobExecutions E
		WHERE E.InstanceID = @InstanceID
		AND E.job_id = @JobID
		AND E.job_execution_id = @job_execution_id
		ORDER BY E.create_time

		SET @ByExecutionID = 1
	END
	ELSE IF EXISTS(	SELECT 1
					FROM dbo.ElasticJobExecutions E
					WHERE E.InstanceID = @InstanceID
					AND E.job_id = @JobID
					AND E.step_id > 0
					AND E.job_execution_id = @job_execution_id
					AND E.create_time >= DATEADD(d,-1,@ExecutionFrom)
					)
	BEGIN
		SET @ByExecutionID = 1
	END
END

SELECT TOP(@Top)
		E.InstanceID,
		J.job_name AS name,
		E.job_id,
		E.job_execution_id,
		E.step_id,
		CASE WHEN E.step_id = 0 THEN N'(Job)' ELSE COALESCE(E.step_name,S.step_name,N'Step ' + CAST(E.step_id AS NVARCHAR(10))) END AS step_name, /* The name it ran with */
		E.target_type,
		/* Not in jobs.job_executions as it is documented to be - the target group of the step instead */
		CASE WHEN E.step_id > 0 THEN ISNULL(E.target_group_name,S.target_group_name) END AS target_group_name,
		E.target_server_name,
		E.target_elastic_pool_name,
		E.target_database_name,
		E.lifecycle,
		O.run_status,
		O.run_status_description,
		E.is_active,
		E.create_time,
		E.start_time,
		E.end_time,
		D.DurationSec,
		HD.HumanDuration AS Duration,
		E.current_attempts,
		E.next_attempt_start_time,
		E.last_message,
		E.job_version
FROM dbo.ElasticJobExecutions E
JOIN dbo.ElasticJobs J ON J.InstanceID = E.InstanceID AND J.job_id = E.job_id
LEFT JOIN dbo.ElasticJobSteps S ON S.InstanceID = E.InstanceID AND S.job_id = E.job_id AND S.step_id = E.step_id
CROSS APPLY dbo.ElasticJobOutcome(E.lifecycle,E.is_active) O
CROSS APPLY(SELECT DATEDIFF(s,E.start_time,CASE WHEN E.is_active = 1 THEN @AsOf ELSE E.end_time END) AS DurationSec) D
CROSS APPLY dbo.SecondsToHumanDuration(D.DurationSec) HD
WHERE E.InstanceID = @InstanceID
AND E.job_id = @JobID
AND (@StepID IS NULL OR E.step_id = @StepID)
AND (@job_execution_id IS NULL
	OR (@ByExecutionID = 1 AND E.job_execution_id = @job_execution_id AND E.create_time >= DATEADD(d,-1,@ExecutionFrom))
	OR (@ByExecutionID = 0 AND E.step_id > 0 AND E.create_time >= @ExecutionFrom AND E.create_time <= @ExecutionTo)
	)
AND (@FailedOnly = 0 OR O.run_status IN(0,2,3))
ORDER BY E.create_time DESC, E.step_id, E.target_server_name, E.target_database_name
OPTION(RECOMPILE)
GO
GRANT EXECUTE
	ON OBJECT::[dbo].[ElasticJobHistory_Get] TO [Reports]
	AS [dbo];
