/*
	Elastic Jobs - collected from the job database of an elastic job agent, through the views in the jobs schema.
	The login needs membership of the jobs_reader role in the job database.

	Result sets, in the order of ElasticJobsTables.TableNames:
		Jobs, job steps (current version), target group members, job executions, then the time the collection ran with the hash
		of the definitions and whether they were returned.
	The columns of each are in the order of the matching table type in the repository (table-valued parameters bind by ordinal).
	The definitions (the first three) are only returned when they've changed since @DefinitionsHash - see below.

	The columns of the views aren't all as documented, and can change as the agent is updated - jobs.job_executions has no
	target_group_name, for one.  So each query is built from the columns listed below.  A column the view doesn't have is returned
	as NULL of the listed type, keeping the shape of the result.  Key columns (no type to fall back to) are required, so a view
	without one fails with an error that names the column rather than collecting something that can't be stored.

	Executions are updated in place as they progress (lifecycle, end_time...), so they are not collected
	incrementally by an ID as SQL Agent job history is.  Instead, executions created since @Since are collected each
	time.  The collector passes the time the previous collection ran (less a few minutes, for rows committed late), or
	the creation time of the oldest execution that was still active then if that's earlier - see
	DBCollector.ElasticJobsFromUtc.  So an execution is collected again until it has been seen complete.  The repository
	upserts them, so collecting a row twice does no harm.
	A single range on create_time rather than an OR across is_active/create_time/end_time, so the collection isn't a scan
	of the whole execution history on every run.  @AllActive (the first collection, or one run on demand) also collects
	every active execution, however old.
	All the times in the views are UTC.
*/
SET NOCOUNT ON
DECLARE @SnapshotDateUtc DATETIME2(7) = SYSUTCDATETIME()

/*	ViewName, the column, the expression that returns it (the column itself when NULL) and the type to return it as when the
	view doesn't have it.  No type: the column is required. */
DECLARE @Columns TABLE(
	Ord INT IDENTITY(1,1) PRIMARY KEY,
	ViewName NVARCHAR(128) NOT NULL,
	ColumnName SYSNAME NOT NULL,
	Expression NVARCHAR(400) NULL,
	MissingAs NVARCHAR(50) NULL
)
INSERT INTO @Columns(ViewName,ColumnName,Expression,MissingAs)
VALUES	(N'jobs.jobs',N'job_id',NULL,NULL),
		(N'jobs.jobs',N'job_name',NULL,NULL),
		(N'jobs.jobs',N'job_version',NULL,N'INT'),
		(N'jobs.jobs',N'description',NULL,N'NVARCHAR(512)'),
		(N'jobs.jobs',N'enabled',NULL,NULL),
		(N'jobs.jobs',N'schedule_interval_type',NULL,N'NVARCHAR(50)'),
		(N'jobs.jobs',N'schedule_interval_count',NULL,N'INT'),
		(N'jobs.jobs',N'schedule_start_time',NULL,N'DATETIME2(7)'),
		(N'jobs.jobs',N'schedule_end_time',NULL,N'DATETIME2(7)'),

		(N'jobs.jobsteps',N'job_id',NULL,NULL),
		(N'jobs.jobsteps',N'step_id',NULL,NULL),
		(N'jobs.jobsteps',N'step_name',NULL,NULL),
		(N'jobs.jobsteps',N'job_version',NULL,N'INT'),
		(N'jobs.jobsteps',N'command_type',NULL,N'NVARCHAR(50)'),
		(N'jobs.jobsteps',N'command_source',NULL,N'NVARCHAR(50)'),
		(N'jobs.jobsteps',N'command',NULL,N'NVARCHAR(MAX)'),
		(N'jobs.jobsteps',N'credential_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'target_group_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'target_group_id',NULL,N'UNIQUEIDENTIFIER'),
		(N'jobs.jobsteps',N'initial_retry_interval_seconds',NULL,N'INT'),
		(N'jobs.jobsteps',N'maximum_retry_interval_seconds',NULL,N'INT'),
		(N'jobs.jobsteps',N'retry_interval_backoff_multiplier',N'CAST(retry_interval_backoff_multiplier AS FLOAT)',N'FLOAT'),
		(N'jobs.jobsteps',N'retry_attempts',NULL,N'INT'),
		(N'jobs.jobsteps',N'step_timeout_seconds',NULL,N'INT'),
		(N'jobs.jobsteps',N'output_type',NULL,N'NVARCHAR(50)'),
		(N'jobs.jobsteps',N'output_credential_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'output_server_name',NULL,N'NVARCHAR(256)'),
		(N'jobs.jobsteps',N'output_database_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'output_schema_name',N'CAST(output_schema_name AS NVARCHAR(128))',N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'output_table_name',N'CAST(output_table_name AS NVARCHAR(128))',N'NVARCHAR(128)'),
		(N'jobs.jobsteps',N'max_parallelism',NULL,N'INT'),

		(N'jobs.target_group_members',N'target_group_id',NULL,NULL),
		(N'jobs.target_group_members',N'target_group_name',NULL,NULL),
		(N'jobs.target_group_members',N'target_id',NULL,NULL),
		(N'jobs.target_group_members',N'membership_type',N'CAST(membership_type AS NVARCHAR(50))',N'NVARCHAR(50)'),
		(N'jobs.target_group_members',N'target_type',NULL,N'NVARCHAR(128)'),
		(N'jobs.target_group_members',N'refresh_credential_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.target_group_members',N'subscription_id',NULL,N'UNIQUEIDENTIFIER'),
		(N'jobs.target_group_members',N'resource_group_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.target_group_members',N'server_name',NULL,N'NVARCHAR(256)'),
		(N'jobs.target_group_members',N'database_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.target_group_members',N'elastic_pool_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.target_group_members',N'shard_map_name',NULL,N'NVARCHAR(128)'),

		(N'jobs.job_executions',N'job_execution_id',NULL,NULL),
		(N'jobs.job_executions',N'job_id',NULL,NULL),
		(N'jobs.job_executions',N'job_version',NULL,N'INT'),
		(N'jobs.job_executions',N'step_id',NULL,NULL),
		(N'jobs.job_executions',N'is_active',NULL,NULL),
		(N'jobs.job_executions',N'lifecycle',NULL,NULL),
		(N'jobs.job_executions',N'create_time',NULL,NULL),
		(N'jobs.job_executions',N'start_time',NULL,N'DATETIME2(7)'),
		(N'jobs.job_executions',N'end_time',NULL,N'DATETIME2(7)'),
		(N'jobs.job_executions',N'current_attempts',NULL,N'INT'),
		(N'jobs.job_executions',N'current_attempt_start_time',NULL,N'DATETIME2(7)'),
		(N'jobs.job_executions',N'next_attempt_start_time',NULL,N'DATETIME2(7)'),
		(N'jobs.job_executions',N'last_message',N'CAST(LEFT(last_message,4000) AS NVARCHAR(4000))',N'NVARCHAR(4000)'),
		(N'jobs.job_executions',N'target_type',NULL,N'NVARCHAR(128)'),
		(N'jobs.job_executions',N'target_id',NULL,N'UNIQUEIDENTIFIER'),
		(N'jobs.job_executions',N'target_group_name',NULL,N'NVARCHAR(128)'), /* Documented, but not in the view */
		(N'jobs.job_executions',N'target_server_name',NULL,N'NVARCHAR(256)'),
		(N'jobs.job_executions',N'target_database_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.job_executions',N'target_elastic_pool_name',NULL,N'NVARCHAR(128)'),
		(N'jobs.job_executions',N'step_name',NULL,N'NVARCHAR(128)') /* The name the step had when it ran - it may since have been renamed or removed */

DECLARE @Select TABLE(ViewName NVARCHAR(128) PRIMARY KEY, SelectList NVARCHAR(MAX) NOT NULL)
INSERT INTO @Select(ViewName,SelectList)
SELECT	V.ViewName,
		STUFF((	SELECT N',' + CASE WHEN C.MissingAs IS NOT NULL AND COL_LENGTH(C.ViewName,C.ColumnName) IS NULL
								THEN N'CAST(NULL AS ' + C.MissingAs + N')'
								ELSE ISNULL(C.Expression,QUOTENAME(C.ColumnName)) END
						+ N' AS ' + QUOTENAME(C.ColumnName)
				FROM @Columns C
				WHERE C.ViewName = V.ViewName
				ORDER BY C.Ord
				FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,N'')
FROM (SELECT DISTINCT ViewName FROM @Columns) V

DECLARE @SQL NVARCHAR(MAX)

/*	The definitions - jobs, steps and target group members - change rarely, so they're only returned when they've changed since
	the ones the collector last sent (@DefinitionsHash, NULL to always return them).  A hash of the columns returned, in key order.
	Otherwise their result sets are empty, and DefinitionsIncluded says they weren't sent rather than that there are none. */
DECLARE @Hash VARBINARY(32)
SELECT @SQL = N'SELECT @Hash = HASHBYTES(''SHA2_256'', CONCAT(
	(SELECT ' + J.SelectList + N' FROM jobs.jobs ORDER BY job_id FOR JSON PATH, INCLUDE_NULL_VALUES), N''|'',
	(SELECT ' + S.SelectList + N' FROM jobs.jobsteps ORDER BY job_id, step_id FOR JSON PATH, INCLUDE_NULL_VALUES), N''|'',
	(SELECT ' + M.SelectList + N' FROM jobs.target_group_members ORDER BY target_group_id, target_id FOR JSON PATH, INCLUDE_NULL_VALUES)))'
FROM @Select J
CROSS JOIN @Select S
CROSS JOIN @Select M
WHERE J.ViewName = N'jobs.jobs'
AND S.ViewName = N'jobs.jobsteps'
AND M.ViewName = N'jobs.target_group_members'
EXEC sp_executesql @SQL,N'@Hash VARBINARY(32) OUTPUT',@Hash OUTPUT

DECLARE @DefinitionsIncluded BIT = CASE WHEN @DefinitionsHash IS NOT NULL AND @DefinitionsHash = @Hash THEN 0 ELSE 1 END

SELECT @SQL = N'SELECT ' + SelectList + N' FROM jobs.jobs WHERE @DefinitionsIncluded = 1' FROM @Select WHERE ViewName = N'jobs.jobs'
EXEC sp_executesql @SQL,N'@DefinitionsIncluded BIT',@DefinitionsIncluded

SELECT @SQL = N'SELECT ' + SelectList + N' FROM jobs.jobsteps WHERE @DefinitionsIncluded = 1' FROM @Select WHERE ViewName = N'jobs.jobsteps'
EXEC sp_executesql @SQL,N'@DefinitionsIncluded BIT',@DefinitionsIncluded

SELECT @SQL = N'SELECT ' + SelectList + N' FROM jobs.target_group_members WHERE @DefinitionsIncluded = 1' FROM @Select WHERE ViewName = N'jobs.target_group_members'
EXEC sp_executesql @SQL,N'@DefinitionsIncluded BIT',@DefinitionsIncluded

SELECT @SQL = N'SELECT ' + SelectList + N'
FROM jobs.job_executions
WHERE create_time >= @Since
OR (@AllActive = 1 AND is_active = 1)
OPTION(RECOMPILE) /* Embeds @AllActive, so the OR is gone when it''s 0 */'
FROM @Select
WHERE ViewName = N'jobs.job_executions'
EXEC sp_executesql @SQL,N'@Since DATETIME2(7),@AllActive BIT',@Since,@AllActive

SELECT	@SnapshotDateUtc AS SnapshotDateUtc,
		@Hash AS DefinitionsHash,
		@DefinitionsIncluded AS DefinitionsIncluded
