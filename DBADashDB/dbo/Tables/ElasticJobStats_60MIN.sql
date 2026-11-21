/*
	Hourly elastic job execution stats, by the hour the execution started.  Added to by dbo.ElasticJobs_Upd as executions complete.
	step_id 0 is the job execution.  A step's row counts its executions against each target: the databases the step ran against, and
	members of the target group that failed (a server or pool that couldn't be expanded into its databases, say).

	FailedCount includes timed out and canceled executions, as SQL Agent job stats include canceled jobs.  SkippedCount is executions that
	didn't run because the previous execution of the step was still running against the target.
	RunDurationSec, MaxRunDurationSec and MinRunDurationSec are for the executions that started (TimedCount).
*/
CREATE TABLE dbo.ElasticJobStats_60MIN(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	RunDateTime DATETIME2(2) NOT NULL,
	FailedCount INT NOT NULL,
	SucceededCount INT NOT NULL,
	SkippedCount INT NOT NULL,
	RetryCount INT NOT NULL,
	TimedCount INT NOT NULL,
	RunDurationSec BIGINT NOT NULL,
	MaxRunDurationSec INT NULL,
	MinRunDurationSec INT NULL,
	CONSTRAINT PK_ElasticJobStats_60MIN PRIMARY KEY CLUSTERED (InstanceID, job_id, step_id, RunDateTime) WITH (DATA_COMPRESSION = PAGE) ON PS_ElasticJobStats_60MIN(RunDateTime)
) ON PS_ElasticJobStats_60MIN(RunDateTime);
