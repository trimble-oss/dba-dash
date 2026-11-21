/* Columns in the order of the first result set of SQLElasticJobs.sql - table-valued parameters bind by ordinal */
CREATE TYPE dbo.ElasticJobs AS TABLE(
	job_id UNIQUEIDENTIFIER NOT NULL,
	job_name NVARCHAR(128) NOT NULL,
	job_version INT NULL,
	description NVARCHAR(512) NULL,
	enabled BIT NOT NULL,
	schedule_interval_type NVARCHAR(50) NULL,
	schedule_interval_count INT NULL,
	schedule_start_time DATETIME2(7) NULL,
	schedule_end_time DATETIME2(7) NULL
);
