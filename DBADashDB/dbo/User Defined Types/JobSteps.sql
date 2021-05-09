CREATE TYPE dbo.JobSteps AS TABLE(
	job_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	step_name SYSNAME NOT NULL,
	subsystem NVARCHAR(40) NOT NULL,
	command NVARCHAR(MAX),
	cmdexec_success_code INT NOT NULL,
	on_success_action	TINYINT NOT NULL,
	on_success_step_id	INT NOT NULL,
	on_fail_action	TINYINT NOT NULL,
	on_fail_step_id	INT NOT NULL,
	database_name	SYSNAME NULL,
	database_user_name	SYSNAME NULL,
	retry_attempts	INT NOT NULL,
	retry_interval	INT NOT NULL,
	output_file_name	NVARCHAR(200) NULL,
	proxy_name	SYSNAME NULL
)