CREATE TABLE dbo.RunningJobs(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	run_requested_date_utc DATETIME  NULL,
	run_requested_source NVARCHAR(128) NULL,
	queued_date_utc DATETIME NULL,
	start_execution_date_utc DATETIME NOT NULL,
	last_executed_step_id INT NULL,
	last_executed_step_date_utc DATETIME NULL,
	SnapshotDate DATETIME NOT NULL,
	current_execution_step_id INT NULL,
	current_execution_step_name NVARCHAR(128) NULL,
	current_retry_attempt INT NULL,
	current_execution_status INT NULL,
	CONSTRAINT PK_RunningJobs PRIMARY KEY (InstanceID,job_id),
	CONSTRAINT FK_RunningJobs_Instances FOREIGN KEY (InstanceID)REFERENCES dbo.Instances(InstanceID)
)