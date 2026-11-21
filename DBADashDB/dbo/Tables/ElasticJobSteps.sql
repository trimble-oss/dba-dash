/*
	Steps of elastic jobs - jobs.jobsteps (the current version of each job) in the job database of an elastic job agent.
*/
CREATE TABLE dbo.ElasticJobSteps(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	step_name NVARCHAR(128) NOT NULL,
	job_version INT NULL,
	command_type NVARCHAR(50) NULL,
	command_source NVARCHAR(50) NULL,
	command NVARCHAR(MAX) NULL,
	credential_name NVARCHAR(128) NULL,
	target_group_name NVARCHAR(128) NULL,
	target_group_id UNIQUEIDENTIFIER NULL,
	initial_retry_interval_seconds INT NULL,
	maximum_retry_interval_seconds INT NULL,
	retry_interval_backoff_multiplier FLOAT NULL,
	retry_attempts INT NULL,
	step_timeout_seconds INT NULL,
	output_type NVARCHAR(50) NULL,
	output_credential_name NVARCHAR(128) NULL,
	output_server_name NVARCHAR(256) NULL,
	output_database_name NVARCHAR(128) NULL,
	output_schema_name NVARCHAR(128) NULL,
	output_table_name NVARCHAR(128) NULL,
	max_parallelism INT NULL,
	CONSTRAINT PK_ElasticJobSteps PRIMARY KEY CLUSTERED (InstanceID, job_id, step_id),
	CONSTRAINT FK_ElasticJobSteps_ElasticJobs FOREIGN KEY (InstanceID, job_id) REFERENCES dbo.ElasticJobs (InstanceID, job_id)
);
