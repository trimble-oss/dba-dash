CREATE TYPE dbo.RunningJobs AS TABLE(
	job_id UNIQUEIDENTIFIER NOT NULL,
	run_requested_date_utc DATETIME  NULL,
	run_requested_source NVARCHAR(128) NULL,
	queued_date_utc DATETIME NULL,
	start_execution_date_utc DATETIME NULL,
	last_executed_step_id INT NULL,
	last_executed_step_date_utc DATETIME NULL,
	SnapshotDate DATETIME NOT NULL,
	current_execution_step_id INT NULL,
	current_execution_step_name NVARCHAR(128) NULL,
	current_retry_attempt INT NULL,
	current_execution_status INT NULL,
	PRIMARY KEY (job_id)
)