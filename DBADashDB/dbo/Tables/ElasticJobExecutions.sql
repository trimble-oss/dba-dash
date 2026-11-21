/*
	Elastic job executions - jobs.job_executions in the job database of an elastic job agent.

	A row is one of:
		The execution of the job: step_id = 0 (NULL in jobs.job_executions) and no target.
		The execution of a step against a target: step_id > 0.  Target type SqlDatabase runs the step's command against the database.
			SqlServer and SqlElasticPool are target group members, which are expanded into the databases they contain.

	Executions are updated in place as they progress (lifecycle, end_time...), so rows are upserted rather than appended.  The key is
	the execution, the step and the target (TargetKey - a hash of the target columns, as a member that is a server or pool targets many
	databases).  create_time is part of the key so rows are partitioned on it.  See dbo.ElasticJobOutcome for what the lifecycle means.
	Times are UTC.
*/
CREATE TABLE dbo.ElasticJobExecutions(
	InstanceID INT NOT NULL,
	create_time DATETIME2(2) NOT NULL,
	job_execution_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	TargetKey BINARY(16) NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	job_version INT NULL,
	is_active BIT NOT NULL,
	lifecycle NVARCHAR(50) NOT NULL,
	start_time DATETIME2(2) NULL,
	end_time DATETIME2(2) NULL,
	current_attempts INT NULL,
	current_attempt_start_time DATETIME2(2) NULL,
	next_attempt_start_time DATETIME2(2) NULL,
	last_message NVARCHAR(4000) NULL,
	target_type NVARCHAR(128) NULL,
	target_id UNIQUEIDENTIFIER NULL,
	target_group_name NVARCHAR(128) NULL,
	target_server_name NVARCHAR(256) NULL,
	target_database_name NVARCHAR(128) NULL,
	target_elastic_pool_name NVARCHAR(128) NULL,
	step_name NVARCHAR(128) NULL, /* As it was when the step ran.  NULL for the job execution. */
	CONSTRAINT PK_ElasticJobExecutions PRIMARY KEY CLUSTERED (InstanceID, create_time, job_execution_id, step_id, TargetKey) WITH (DATA_COMPRESSION = PAGE) ON PS_ElasticJobExecutions(create_time),
	CONSTRAINT FK_ElasticJobExecutions_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_ElasticJobExecutions(create_time);
GO
CREATE NONCLUSTERED INDEX IX_ElasticJobExecutions_InstanceID_job_id_step_id_create_time
ON dbo.ElasticJobExecutions(InstanceID, job_id, step_id, create_time)
INCLUDE(job_execution_id, is_active, lifecycle, start_time, end_time)
WITH (DATA_COMPRESSION = PAGE)
ON PS_ElasticJobExecutions(create_time);
GO
/* Executions still running - for the timeline, which shows them however long ago they started */
CREATE NONCLUSTERED INDEX IX_ElasticJobExecutions_Active
ON dbo.ElasticJobExecutions(InstanceID, create_time)
INCLUDE(job_id, job_execution_id, step_id, lifecycle, start_time, end_time, target_type, target_server_name, target_elastic_pool_name, target_database_name, step_name)
WHERE is_active = 1
WITH (DATA_COMPRESSION = PAGE)
ON PS_ElasticJobExecutions(create_time);
