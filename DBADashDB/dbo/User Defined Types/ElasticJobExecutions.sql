/* Columns in the order of the fourth result set of SQLElasticJobs.sql - table-valued parameters bind by ordinal */
CREATE TYPE dbo.ElasticJobExecutions AS TABLE(
	job_execution_id UNIQUEIDENTIFIER NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	job_version INT NULL,
	step_id INT NULL,
	is_active BIT NOT NULL,
	lifecycle NVARCHAR(50) NOT NULL,
	create_time DATETIME2(7) NOT NULL,
	start_time DATETIME2(7) NULL,
	end_time DATETIME2(7) NULL,
	current_attempts INT NULL,
	current_attempt_start_time DATETIME2(7) NULL,
	next_attempt_start_time DATETIME2(7) NULL,
	last_message NVARCHAR(4000) NULL,
	target_type NVARCHAR(128) NULL,
	target_id UNIQUEIDENTIFIER NULL,
	target_group_name NVARCHAR(128) NULL,
	target_server_name NVARCHAR(256) NULL,
	target_database_name NVARCHAR(128) NULL,
	target_elastic_pool_name NVARCHAR(128) NULL,
	step_name NVARCHAR(128) NULL
);
