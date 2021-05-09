CREATE PROC dbo.JobSteps_Upd(
	@InstanceID INT,
	@JobSteps dbo.JobSteps READONLY,
	@SnapshotDate DATETIME
)
AS
SET XACT_ABORT ON
BEGIN TRAN

DELETE JS 
FROM dbo.JobSteps JS
WHERE InstanceID = @InstanceID
AND NOT EXISTS(SELECT 1 
			FROM @JobSteps t 
			WHERE t.step_id = JS.step_id 
			AND t.job_id = JS.job_id
			)

UPDATE JS 
SET step_id = t.step_id,
	step_name = t.step_name,
	subsystem = t.subsystem,
	command =t.command,
	cmdexec_success_code = t.cmdexec_success_code,
	on_success_action=t.on_success_action,
	on_success_step_id=t.on_success_step_id,
	on_fail_action=t.on_fail_action,
	on_fail_step_id	=t.on_fail_step_id,
	database_name=t.database_name,
	database_user_name=t.database_user_name,
	retry_attempts=t.retry_attempts,
	retry_interval	=t.retry_interval,
	output_file_name=t.output_file_name,
	proxy_name=t.proxy_name
FROM dbo.JobSteps JS 
JOIN @JobSteps t ON JS.step_id = t.step_id AND JS.job_id = t.job_id
WHERE JS.InstanceID = @InstanceID

INSERT INTO dbo.JobSteps(
	InstanceID,
	job_id,
	step_id,
	step_name,
	subsystem,
	command,
	cmdexec_success_code,
	on_success_action,
	on_success_step_id,
	on_fail_action,
	on_fail_step_id,
	database_name,
	database_user_name,
	retry_attempts,
	retry_interval,
	output_file_name,
	proxy_name)
SELECT @InstanceID,
	job_id,
	step_id,
	step_name,
	subsystem,
	command,
	cmdexec_success_code,
	on_success_action,
	on_success_step_id,
	on_fail_action,
	on_fail_step_id,
	database_name,
	database_user_name,
	retry_attempts,
	retry_interval,
	output_file_name,
	proxy_name
FROM @JobSteps t
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.JobSteps JS 
				WHERE JS.step_id = t.step_id 
				AND JS.job_id = t.job_id 
				AND JS.InstanceID = @InstanceID
				)
COMMIT