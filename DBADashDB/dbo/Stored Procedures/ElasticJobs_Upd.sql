/*
	Imports the Elastic Jobs collection - the jobs, steps, target group members and executions of an elastic job agent, from its job database.

	Jobs, steps and target group members (the definitions) are the full set, sent only when they've changed - @DefinitionsIncluded.  Jobs
	that are no longer there are marked inactive.  Steps and members that are no longer there are removed.  Collections can be imported
	out of order, so definitions from a collection older than the last imported (dbo.CollectionDates) are ignored.  Where that collection
	had no definitions, a change in them waits for the collector to send them again - at least hourly.  An older collection doesn't move
	the collection date back, so importing it again is ignored too.

	Executions are the ones created since a little before the previous collection, and any still running then.  They are updated
	in place in the job database as they progress, so they are upserted here.  Collecting a row again does no harm - only a change is
	written.  When an execution is first seen complete (inactive) it is added to dbo.ElasticJobStats_60MIN and to the last succeeded/failed
	dates of its job, so each execution is counted once however many times it's collected.

	Executions created before the retention of dbo.ElasticJobExecutions are not added if they're complete.  They would be counted again
	in the stats, having been purged since they were last imported - the first collection after a service restart goes back 7 days.  An
	execution still running is added however old, and its completion is recorded where it was stored running.  One purged while running
	is added again as running, and counted when it completes - unless it completes before the next import after the purge.
*/
CREATE PROC dbo.ElasticJobs_Upd(
	@InstanceID INT,
	@ElasticJobs dbo.ElasticJobs READONLY,
	@ElasticJobSteps dbo.ElasticJobSteps READONLY,
	@ElasticJobTargetGroupMembers dbo.ElasticJobTargetGroupMembers READONLY,
	@ElasticJobExecutions dbo.ElasticJobExecutions READONLY,
	@SnapshotDate DATETIME2(2),
	@DefinitionsIncluded BIT=1 /* 0: jobs, steps and target group members weren't sent as they haven't changed */
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30) = 'ElasticJobs'
DECLARE @RetentionDays INT

SELECT @RetentionDays = RetentionDays
FROM dbo.DataRetention
WHERE SchemaName = 'dbo'
AND TableName = 'ElasticJobExecutions'

DECLARE @MinCreateTime DATETIME2(2) = CASE WHEN ISNULL(@RetentionDays,0) > 0 THEN DATEADD(d,-@RetentionDays,SYSUTCDATETIME()) ELSE '19000101' END

/*
	The key an execution is stored under.  The job execution has no step (step_id 0) and no target.  A target group member that is a
	server or elastic pool targets many databases, so the target is all of its columns, hashed (the first 16 bytes of SHA2_256).
	Duplicate keys aren't expected - if there are any, the completed row wins.
*/
SELECT	job_execution_id,
		job_id,
		job_version,
		step_id,
		TargetKey,
		is_active,
		lifecycle,
		create_time,
		start_time,
		end_time,
		current_attempts,
		current_attempt_start_time,
		next_attempt_start_time,
		last_message,
		target_type,
		target_id,
		target_group_name,
		target_server_name,
		target_database_name,
		target_elastic_pool_name,
		step_name
INTO #Executions
FROM (
	SELECT	E.job_execution_id,
			E.job_id,
			E.job_version,
			ISNULL(E.step_id,0) AS step_id,
			K.TargetKey,
			E.is_active,
			E.lifecycle,
			CAST(E.create_time AS DATETIME2(2)) AS create_time,
			CAST(E.start_time AS DATETIME2(2)) AS start_time,
			CAST(E.end_time AS DATETIME2(2)) AS end_time,
			E.current_attempts,
			CAST(E.current_attempt_start_time AS DATETIME2(2)) AS current_attempt_start_time,
			CAST(E.next_attempt_start_time AS DATETIME2(2)) AS next_attempt_start_time,
			E.last_message,
			E.target_type,
			E.target_id,
			E.target_group_name,
			E.target_server_name,
			E.target_database_name,
			E.target_elastic_pool_name,
			E.step_name,
			ROW_NUMBER() OVER(PARTITION BY E.job_execution_id, ISNULL(E.step_id,0), K.TargetKey, CAST(E.create_time AS DATETIME2(2)) ORDER BY E.is_active, E.end_time DESC) AS rnum
	FROM @ElasticJobExecutions E
	CROSS APPLY(SELECT CAST(HASHBYTES('SHA2_256', CONCAT(E.target_type, N'|', CAST(E.target_id AS NCHAR(36)), N'|', E.target_group_name, N'|', E.target_server_name, N'|', E.target_database_name, N'|', E.target_elastic_pool_name)) AS BINARY(16)) AS TargetKey) K
	WHERE (
		E.create_time >= @MinCreateTime
		OR E.is_active = 1
		/* Completing an execution stored running */
		OR EXISTS(	SELECT 1
					FROM dbo.ElasticJobExecutions A
					WHERE A.InstanceID = @InstanceID
					AND A.create_time = CAST(E.create_time AS DATETIME2(2))
					AND A.job_execution_id = E.job_execution_id
					AND A.step_id = ISNULL(E.step_id,0)
					AND A.TargetKey = K.TargetKey
					AND A.is_active = 1
					)
		)
	) T
WHERE T.rnum = 1

/* Executions that changed.  WasActive is NULL for a new row.  Used to find the ones that completed with this import. */
CREATE TABLE #Changed(
	job_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	target_type NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	is_active BIT NOT NULL,
	lifecycle NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
	create_time DATETIME2(2) NOT NULL,
	start_time DATETIME2(2) NULL,
	end_time DATETIME2(2) NULL,
	current_attempts INT NULL,
	WasActive BIT NULL
)

DECLARE @OutOfOrder BIT = 0
DECLARE @ApplyDefinitions BIT

BEGIN TRAN

/* Older than the last collection imported - imported out of order */
IF EXISTS(
		SELECT 1
		FROM dbo.CollectionDates WITH(UPDLOCK,HOLDLOCK)
		WHERE InstanceID = @InstanceID
		AND Reference = @Ref
		AND SnapshotDate > @SnapshotDate
		)
BEGIN
	SET @OutOfOrder = 1
END

SET @ApplyDefinitions = CASE WHEN @DefinitionsIncluded = 1 AND @OutOfOrder = 0 THEN 1 ELSE 0 END

/* The definitions are only sent when they have changed.  Without them, the jobs are left as they are - an empty @ElasticJobs would mark them all deleted. */
IF @ApplyDefinitions = 1
BEGIN
	/* Jobs */
	UPDATE J
		SET J.job_name = T.job_name,
			J.job_version = T.job_version,
			J.description = T.description,
			J.enabled = T.enabled,
			J.schedule_interval_type = T.schedule_interval_type,
			J.schedule_interval_count = T.schedule_interval_count,
			J.schedule_start_time = CAST(T.schedule_start_time AS DATETIME2(2)),
			J.schedule_end_time = CAST(T.schedule_end_time AS DATETIME2(2)),
			J.IsActive = 1,
			J.SnapshotUpdatedDate = @SnapshotDate
	FROM dbo.ElasticJobs J
	JOIN @ElasticJobs T ON J.job_id = T.job_id
	WHERE J.InstanceID = @InstanceID

	INSERT INTO dbo.ElasticJobs(
		InstanceID,
		job_id,
		job_name,
		job_version,
		description,
		enabled,
		schedule_interval_type,
		schedule_interval_count,
		schedule_start_time,
		schedule_end_time,
		IsActive,
		SnapshotCreatedDate,
		SnapshotUpdatedDate
	)
	SELECT	@InstanceID,
			T.job_id,
			T.job_name,
			T.job_version,
			T.description,
			T.enabled,
			T.schedule_interval_type,
			T.schedule_interval_count,
			CAST(T.schedule_start_time AS DATETIME2(2)),
			CAST(T.schedule_end_time AS DATETIME2(2)),
			CAST(1 AS BIT),
			@SnapshotDate,
			@SnapshotDate
	FROM @ElasticJobs T
	WHERE NOT EXISTS(
				SELECT 1
				FROM dbo.ElasticJobs J
				WHERE J.InstanceID = @InstanceID
				AND J.job_id = T.job_id
				)

	/* Deleted from the agent.  Kept for their history. */
	UPDATE J
		SET J.IsActive = 0,
			J.SnapshotUpdatedDate = @SnapshotDate
	FROM dbo.ElasticJobs J
	WHERE J.InstanceID = @InstanceID
	AND J.IsActive = 1
	AND NOT EXISTS(
				SELECT 1
				FROM @ElasticJobs T
				WHERE T.job_id = J.job_id
				)
END

/* Executions of a job that has been deleted before it was collected */
INSERT INTO dbo.ElasticJobs(
	InstanceID,
	job_id,
	job_name,
	enabled,
	IsActive,
	SnapshotCreatedDate,
	SnapshotUpdatedDate
)
SELECT	@InstanceID,
		E.job_id,
		N'{Deleted job} ' + CAST(E.job_id AS NCHAR(36)),
		CAST(0 AS BIT),
		CAST(0 AS BIT),
		@SnapshotDate,
		@SnapshotDate
FROM #Executions E
WHERE NOT EXISTS(
			SELECT 1
			FROM dbo.ElasticJobs J
			WHERE J.InstanceID = @InstanceID
			AND J.job_id = E.job_id
			)
GROUP BY E.job_id

/* Steps and target group members - only sent with the jobs */
IF @ApplyDefinitions = 1
BEGIN
	/* Steps (current version of each job) */
	DELETE S
	FROM dbo.ElasticJobSteps S
	WHERE S.InstanceID = @InstanceID
	AND NOT EXISTS(
				SELECT 1
				FROM @ElasticJobSteps T
				WHERE T.job_id = S.job_id
				AND T.step_id = S.step_id
				)

	UPDATE S
		SET S.step_name = T.step_name,
			S.job_version = T.job_version,
			S.command_type = T.command_type,
			S.command_source = T.command_source,
			S.command = T.command,
			S.credential_name = T.credential_name,
			S.target_group_name = T.target_group_name,
			S.target_group_id = T.target_group_id,
			S.initial_retry_interval_seconds = T.initial_retry_interval_seconds,
			S.maximum_retry_interval_seconds = T.maximum_retry_interval_seconds,
			S.retry_interval_backoff_multiplier = T.retry_interval_backoff_multiplier,
			S.retry_attempts = T.retry_attempts,
			S.step_timeout_seconds = T.step_timeout_seconds,
			S.output_type = T.output_type,
			S.output_credential_name = T.output_credential_name,
			S.output_server_name = T.output_server_name,
			S.output_database_name = T.output_database_name,
			S.output_schema_name = T.output_schema_name,
			S.output_table_name = T.output_table_name,
			S.max_parallelism = T.max_parallelism
	FROM dbo.ElasticJobSteps S
	JOIN @ElasticJobSteps T ON T.job_id = S.job_id AND T.step_id = S.step_id
	WHERE S.InstanceID = @InstanceID
	/* Only where something changed - the steps are collected every time */
	AND EXISTS(
			SELECT S.step_name, S.job_version, S.command_type, S.command_source, S.command, S.credential_name, S.target_group_name, S.target_group_id,
					S.initial_retry_interval_seconds, S.maximum_retry_interval_seconds, S.retry_interval_backoff_multiplier, S.retry_attempts,
					S.step_timeout_seconds, S.output_type, S.output_credential_name, S.output_server_name, S.output_database_name,
					S.output_schema_name, S.output_table_name, S.max_parallelism
			EXCEPT
			SELECT T.step_name, T.job_version, T.command_type, T.command_source, T.command, T.credential_name, T.target_group_name, T.target_group_id,
					T.initial_retry_interval_seconds, T.maximum_retry_interval_seconds, T.retry_interval_backoff_multiplier, T.retry_attempts,
					T.step_timeout_seconds, T.output_type, T.output_credential_name, T.output_server_name, T.output_database_name,
					T.output_schema_name, T.output_table_name, T.max_parallelism
			)

	INSERT INTO dbo.ElasticJobSteps(
		InstanceID,
		job_id,
		step_id,
		step_name,
		job_version,
		command_type,
		command_source,
		command,
		credential_name,
		target_group_name,
		target_group_id,
		initial_retry_interval_seconds,
		maximum_retry_interval_seconds,
		retry_interval_backoff_multiplier,
		retry_attempts,
		step_timeout_seconds,
		output_type,
		output_credential_name,
		output_server_name,
		output_database_name,
		output_schema_name,
		output_table_name,
		max_parallelism
	)
	SELECT	@InstanceID,
			T.job_id,
			T.step_id,
			T.step_name,
			T.job_version,
			T.command_type,
			T.command_source,
			T.command,
			T.credential_name,
			T.target_group_name,
			T.target_group_id,
			T.initial_retry_interval_seconds,
			T.maximum_retry_interval_seconds,
			T.retry_interval_backoff_multiplier,
			T.retry_attempts,
			T.step_timeout_seconds,
			T.output_type,
			T.output_credential_name,
			T.output_server_name,
			T.output_database_name,
			T.output_schema_name,
			T.output_table_name,
			T.max_parallelism
	FROM @ElasticJobSteps T
	WHERE EXISTS( /* A step belongs to a job collected with it.  The FK would fail otherwise. */
				SELECT 1
				FROM @ElasticJobs J
				WHERE J.job_id = T.job_id
				)
	AND NOT EXISTS(
				SELECT 1
				FROM dbo.ElasticJobSteps S
				WHERE S.InstanceID = @InstanceID
				AND S.job_id = T.job_id
				AND S.step_id = T.step_id
				)

	/* Target group members */
	DELETE M
	FROM dbo.ElasticJobTargetGroupMembers M
	WHERE M.InstanceID = @InstanceID
	AND NOT EXISTS(
				SELECT 1
				FROM @ElasticJobTargetGroupMembers T
				WHERE T.target_group_id = M.target_group_id
				AND T.target_id = M.target_id
				)

	UPDATE M
		SET M.target_group_name = T.target_group_name,
			M.membership_type = T.membership_type,
			M.target_type = T.target_type,
			M.refresh_credential_name = T.refresh_credential_name,
			M.subscription_id = T.subscription_id,
			M.resource_group_name = T.resource_group_name,
			M.server_name = T.server_name,
			M.database_name = T.database_name,
			M.elastic_pool_name = T.elastic_pool_name,
			M.shard_map_name = T.shard_map_name
	FROM dbo.ElasticJobTargetGroupMembers M
	JOIN @ElasticJobTargetGroupMembers T ON T.target_group_id = M.target_group_id AND T.target_id = M.target_id
	WHERE M.InstanceID = @InstanceID
	AND EXISTS(
			SELECT M.target_group_name, M.membership_type, M.target_type, M.refresh_credential_name, M.subscription_id, M.resource_group_name,
					M.server_name, M.database_name, M.elastic_pool_name, M.shard_map_name
			EXCEPT
			SELECT T.target_group_name, T.membership_type, T.target_type, T.refresh_credential_name, T.subscription_id, T.resource_group_name,
					T.server_name, T.database_name, T.elastic_pool_name, T.shard_map_name
			)

	INSERT INTO dbo.ElasticJobTargetGroupMembers(
		InstanceID,
		target_group_id,
		target_id,
		target_group_name,
		membership_type,
		target_type,
		refresh_credential_name,
		subscription_id,
		resource_group_name,
		server_name,
		database_name,
		elastic_pool_name,
		shard_map_name
	)
	SELECT	@InstanceID,
			T.target_group_id,
			T.target_id,
			T.target_group_name,
			T.membership_type,
			T.target_type,
			T.refresh_credential_name,
			T.subscription_id,
			T.resource_group_name,
			T.server_name,
			T.database_name,
			T.elastic_pool_name,
			T.shard_map_name
	FROM @ElasticJobTargetGroupMembers T
	WHERE NOT EXISTS(
				SELECT 1
				FROM dbo.ElasticJobTargetGroupMembers M
				WHERE M.InstanceID = @InstanceID
				AND M.target_group_id = T.target_group_id
				AND M.target_id = T.target_id
				)
END

/* Executions */
UPDATE E
	SET E.job_version = T.job_version,
		E.is_active = T.is_active,
		E.lifecycle = T.lifecycle,
		E.start_time = T.start_time,
		E.end_time = T.end_time,
		E.current_attempts = T.current_attempts,
		E.current_attempt_start_time = T.current_attempt_start_time,
		E.next_attempt_start_time = T.next_attempt_start_time,
		E.last_message = T.last_message,
		E.step_name = T.step_name
OUTPUT	inserted.job_id,
		inserted.step_id,
		inserted.target_type,
		inserted.is_active,
		inserted.lifecycle,
		inserted.create_time,
		inserted.start_time,
		inserted.end_time,
		inserted.current_attempts,
		deleted.is_active
INTO #Changed(job_id,step_id,target_type,is_active,lifecycle,create_time,start_time,end_time,current_attempts,WasActive)
FROM dbo.ElasticJobExecutions E
JOIN #Executions T ON E.create_time = T.create_time
					AND E.job_execution_id = T.job_execution_id
					AND E.step_id = T.step_id
					AND E.TargetKey = T.TargetKey
WHERE E.InstanceID = @InstanceID
/* Complete is final.  An execution seen running in a collection imported out of order doesn't go back to running. */
AND NOT (E.is_active = 0 AND T.is_active = 1)
/* Only where something changed - active executions are collected every time */
AND EXISTS(
		SELECT E.job_version, E.is_active, E.lifecycle, E.start_time, E.end_time, E.current_attempts, E.current_attempt_start_time, E.next_attempt_start_time, E.last_message, E.step_name
		EXCEPT
		SELECT T.job_version, T.is_active, T.lifecycle, T.start_time, T.end_time, T.current_attempts, T.current_attempt_start_time, T.next_attempt_start_time, T.last_message, T.step_name
		)

INSERT INTO dbo.ElasticJobExecutions(
	InstanceID,
	create_time,
	job_execution_id,
	step_id,
	TargetKey,
	job_id,
	job_version,
	is_active,
	lifecycle,
	start_time,
	end_time,
	current_attempts,
	current_attempt_start_time,
	next_attempt_start_time,
	last_message,
	target_type,
	target_id,
	target_group_name,
	target_server_name,
	target_database_name,
	target_elastic_pool_name,
	step_name
)
OUTPUT	inserted.job_id,
		inserted.step_id,
		inserted.target_type,
		inserted.is_active,
		inserted.lifecycle,
		inserted.create_time,
		inserted.start_time,
		inserted.end_time,
		inserted.current_attempts,
		NULL
INTO #Changed(job_id,step_id,target_type,is_active,lifecycle,create_time,start_time,end_time,current_attempts,WasActive)
SELECT	@InstanceID,
		T.create_time,
		T.job_execution_id,
		T.step_id,
		T.TargetKey,
		T.job_id,
		T.job_version,
		T.is_active,
		T.lifecycle,
		T.start_time,
		T.end_time,
		T.current_attempts,
		T.current_attempt_start_time,
		T.next_attempt_start_time,
		T.last_message,
		T.target_type,
		T.target_id,
		T.target_group_name,
		T.target_server_name,
		T.target_database_name,
		T.target_elastic_pool_name,
		T.step_name
FROM #Executions T
WHERE NOT EXISTS(
			SELECT 1
			FROM dbo.ElasticJobExecutions E
			WHERE E.InstanceID = @InstanceID
			AND E.create_time = T.create_time
			AND E.job_execution_id = T.job_execution_id
			AND E.step_id = T.step_id
			AND E.TargetKey = T.TargetKey
			)

/*
	Executions that completed with this import - first seen inactive.
	A step's stats count the databases it ran against, and any other target that failed (a server or pool that couldn't be expanded
	into its databases).  Other targets of a step are the members that were expanded - the databases beneath them are what ran.
*/
SELECT	C.job_id,
		C.step_id,
		C.create_time,
		C.start_time,
		C.end_time,
		DATEDIFF(s,C.start_time,C.end_time) AS DurationSec,
		CASE WHEN C.step_id > 0 AND C.current_attempts > 1 THEN C.current_attempts - 1 ELSE 0 END AS Retries,
		O.IsFailed,
		O.IsSucceeded,
		O.IsSkipped
INTO #Completed
FROM #Changed C
CROSS APPLY dbo.ElasticJobOutcome(C.lifecycle,C.is_active) O
WHERE C.is_active = 0
AND ISNULL(C.WasActive,1) = 1
AND (C.step_id = 0 OR C.target_type = N'SqlDatabase' OR O.IsFailed = 1)

;WITH L AS (
	SELECT	job_id,
			MAX(CASE WHEN step_id = 0 AND IsFailed = 1 THEN ISNULL(end_time,create_time) END) AS LastFailed,
			MAX(CASE WHEN step_id = 0 AND IsSucceeded = 1 THEN ISNULL(end_time,create_time) END) AS LastSucceeded,
			MAX(CASE WHEN step_id > 0 AND IsFailed = 1 THEN ISNULL(end_time,create_time) END) AS StepLastFailed
	FROM #Completed
	GROUP BY job_id
)
UPDATE J
	SET J.LastFailed = (SELECT MAX(d) FROM (VALUES(L.LastFailed),(J.LastFailed)) V(d)),
		J.LastSucceeded = (SELECT MAX(d) FROM (VALUES(L.LastSucceeded),(J.LastSucceeded)) V(d)),
		J.StepLastFailed = (SELECT MAX(d) FROM (VALUES(L.StepLastFailed),(J.StepLastFailed)) V(d))
FROM dbo.ElasticJobs J
JOIN L ON J.job_id = L.job_id
WHERE J.InstanceID = @InstanceID

SELECT	C.job_id,
		C.step_id,
		DG.DateGroup AS RunDateTime,
		SUM(CAST(C.IsFailed AS INT)) AS FailedCount,
		SUM(CAST(C.IsSucceeded AS INT)) AS SucceededCount,
		SUM(CAST(C.IsSkipped AS INT)) AS SkippedCount,
		SUM(C.Retries) AS RetryCount,
		COUNT(C.DurationSec) AS TimedCount,
		ISNULL(SUM(CAST(C.DurationSec AS BIGINT)),0) AS RunDurationSec,
		MAX(C.DurationSec) AS MaxRunDurationSec,
		MIN(C.DurationSec) AS MinRunDurationSec
INTO #60
FROM #Completed C
CROSS APPLY dbo.DateGroupingMins(ISNULL(C.start_time,C.create_time),60) DG
GROUP BY C.job_id,
		C.step_id,
		DG.DateGroup

DECLARE @MinRunDateTime DATETIME2(2) = (SELECT MIN(RunDateTime) FROM #60)

UPDATE S
	SET S.FailedCount += T.FailedCount,
		S.SucceededCount += T.SucceededCount,
		S.SkippedCount += T.SkippedCount,
		S.RetryCount += T.RetryCount,
		S.TimedCount += T.TimedCount,
		S.RunDurationSec += T.RunDurationSec,
		S.MaxRunDurationSec = (SELECT MAX(c) FROM (VALUES (T.MaxRunDurationSec),(S.MaxRunDurationSec)) V(c)),
		S.MinRunDurationSec = (SELECT MIN(c) FROM (VALUES (T.MinRunDurationSec),(S.MinRunDurationSec)) V(c))
FROM dbo.ElasticJobStats_60MIN S
JOIN #60 T ON S.job_id = T.job_id AND S.step_id = T.step_id AND S.RunDateTime = T.RunDateTime
WHERE S.InstanceID = @InstanceID
AND S.RunDateTime >= @MinRunDateTime

INSERT INTO dbo.ElasticJobStats_60MIN(
	InstanceID,
	job_id,
	step_id,
	RunDateTime,
	FailedCount,
	SucceededCount,
	SkippedCount,
	RetryCount,
	TimedCount,
	RunDurationSec,
	MaxRunDurationSec,
	MinRunDurationSec
)
SELECT	@InstanceID,
		T.job_id,
		T.step_id,
		T.RunDateTime,
		T.FailedCount,
		T.SucceededCount,
		T.SkippedCount,
		T.RetryCount,
		T.TimedCount,
		T.RunDurationSec,
		T.MaxRunDurationSec,
		T.MinRunDurationSec
FROM #60 T
WHERE NOT EXISTS(
			SELECT 1
			FROM dbo.ElasticJobStats_60MIN S
			WHERE S.InstanceID = @InstanceID
			AND S.job_id = T.job_id
			AND S.step_id = T.step_id
			AND S.RunDateTime = T.RunDateTime
			)

/* Not moved back by an older collection */
IF @OutOfOrder = 0
BEGIN
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
								 @Reference = @Ref,
								 @SnapshotDate = @SnapshotDate
END

COMMIT
