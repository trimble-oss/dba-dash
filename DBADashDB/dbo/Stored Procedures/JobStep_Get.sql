CREATE PROC dbo.JobStep_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT
)
AS
SELECT J.name,
	JS.step_name,
	JS.subsystem,
	JS.command,
	JS.cmdexec_success_code,
	JS.on_success_action,
	JS.on_success_step_id,
	JS.on_fail_action,
	JS.on_fail_step_id,
	JS.database_name,
	JS.database_user_name,
	JS.retry_attempts,
	JS.retry_interval,
	JS.output_file_name,
	JS.proxy_name
FROM dbo.JobSteps JS 
JOIN dbo.Jobs J ON J.job_id = JS.job_id AND J.InstanceID = JS.InstanceID
WHERE JS.InstanceID = @InstanceID
AND JS.job_id = @JobID
AND JS.step_id = @StepID