/*
	Elastic jobs - jobs.jobs in the job database of an elastic job agent.  The agent's job database is the instance (an Azure DB).
	LastSucceeded, LastFailed and StepLastFailed are maintained from the executions as they complete (see dbo.ElasticJobs_Upd).
	Times are UTC.
*/
CREATE TABLE dbo.ElasticJobs(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	job_name NVARCHAR(128) NOT NULL,
	job_version INT NULL,
	description NVARCHAR(512) NULL,
	enabled BIT NOT NULL,
	schedule_interval_type NVARCHAR(50) NULL,
	schedule_interval_count INT NULL,
	schedule_start_time DATETIME2(2) NULL,
	schedule_end_time DATETIME2(2) NULL,
	IsActive BIT NOT NULL,
	SnapshotCreatedDate DATETIME2(2) NOT NULL,
	SnapshotUpdatedDate DATETIME2(2) NOT NULL,
	LastSucceeded DATETIME2(2) NULL,
	LastFailed DATETIME2(2) NULL,
	StepLastFailed DATETIME2(2) NULL,
	AckDate DATETIME2(2) NULL,
	CONSTRAINT PK_ElasticJobs PRIMARY KEY CLUSTERED (InstanceID, job_id),
	CONSTRAINT FK_ElasticJobs_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
