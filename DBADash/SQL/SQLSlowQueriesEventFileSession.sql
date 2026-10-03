/*
	Creates and starts the extended events session DBA Dash manages for slow query capture in event file mode,
	rebuilding it when its definition no longer matches the configuration.  Only ever run for the reserved session
	name - a session DBA Dash did not create is read and never altered.

	Why an event file rather than the ring buffer the default mode uses: the event file is read from a resume
	cursor, so each collection reads only what is new and the session never has to be stopped and started to
	empty it.  Nothing is lost in a stop/start window, a burst of slow queries spills to disk rather than being
	dropped from a full buffer, and with the session left running across a service restart the events from while
	the service was down are still there to collect.

	The events, actions and filters are the same as the ring buffer sessions' (see SQLSlowQueries.sql), so the
	data collected is the same whichever mode captured it.  The file name is unqualified so SQL Server puts it in
	the instance's own log directory, next to system_health's.

	Unlike the ring buffer sessions, which are only created when missing and so pick up a new threshold when the
	service drops them on shutdown, this session can be left running when the service stops.  The definition is
	therefore checked against the catalog: the threshold in each event's predicate, the resource governor actions,
	the target's file size and count, MAX_MEMORY and STARTUP_STATE.  A mismatch drops and recreates the session -
	the files it has written stay on disk and the resume cursor carries on reading them.

	Not run on every collection: the service runs it on an instance's first collection, after a configuration
	change, or when the session isn't running, and otherwise only checks the session is running.  See
	SlowQueryCollector.EnsureManagedSessionAsync.

	The ring buffer sessions from the default mode are not touched here.  They are cleaned up once per service
	lifetime by the collector (see SlowQueryCollector.RemoveLeftoverSessionsOnceAsync) rather than on every run, so
	another DBA Dash service can keep capturing the same instance in ring buffer mode alongside this one.
*/
DECLARE @SessionName SYSNAME = @Name
DECLARE @SQL NVARCHAR(MAX)
DECLARE @Actions NVARCHAR(MAX) = N'sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_id,sqlserver.username,sqlserver.session_id,sqlserver.context_info'
	+ CASE WHEN @CollectGroupIDAndPoolID = 1 THEN N',sqlserver.session_resource_group_id,sqlserver.session_resource_pool_id' ELSE N'' END
DECLARE @RpcPredicate NVARCHAR(MAX) = N'([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(30)) + N') AND ([sqlserver].[client_app_name]<>N''DBADashXE'' AND [object_name]<>N''sp_readrequest''))'
DECLARE @BatchPredicate NVARCHAR(MAX) = N'([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(30)) + N') AND ([sqlserver].[client_app_name]<>N''DBADashXE''))'
DECLARE @Rebuilt BIT = 0
DECLARE @Created BIT = 0

IF EXISTS(SELECT 1 FROM sys.server_event_sessions WHERE name = @SessionName)
BEGIN
	DECLARE @SessionID INT
	DECLARE @IsCurrent BIT = 1

	SELECT @SessionID = event_session_id
	FROM sys.server_event_sessions
	WHERE name = @SessionName
	AND max_memory = @MaxMemory
	AND startup_state = @StartupState

	IF @SessionID IS NULL
		SET @IsCurrent = 0

	/*	Exactly the two events, each filtered on this threshold.  Checked for the threshold term rather than
		compared whole: SQL Server normalizes the predicate it stores (redundant parentheses are removed), so the
		stored text isn't the text it was created with. */
	DECLARE @ThresholdTerm NVARCHAR(100) = N'[duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(30)) + N')'
	IF @IsCurrent = 1 AND (
		(SELECT COUNT(*) FROM sys.server_event_session_events WHERE event_session_id = @SessionID) <> 2
		OR (SELECT COUNT(*)
			FROM sys.server_event_session_events
			WHERE event_session_id = @SessionID
			AND name IN('rpc_completed','sql_batch_completed')
			AND CHARINDEX(@ThresholdTerm, CAST(predicate AS NVARCHAR(MAX))) > 0
			AND CHARINDEX(N'DBADashXE', CAST(predicate AS NVARCHAR(MAX))) > 0
			) <> 2
		)
		SET @IsCurrent = 0

	/* The resource governor actions come and go with resource governor being in use */
	IF @IsCurrent = 1
		AND @CollectGroupIDAndPoolID <> CASE WHEN EXISTS(
						SELECT 1
						FROM sys.server_event_session_actions
						WHERE event_session_id = @SessionID
						AND name = 'session_resource_group_id'
						) THEN 1 ELSE 0 END
		SET @IsCurrent = 0

	/* A single event_file target, sized as configured */
	IF @IsCurrent = 1 AND (
		(SELECT COUNT(*) FROM sys.server_event_session_targets WHERE event_session_id = @SessionID) <> 1
		OR NOT EXISTS(
			SELECT 1
			FROM sys.server_event_session_targets AS t
			JOIN sys.server_event_session_fields AS fs ON fs.event_session_id = t.event_session_id AND fs.object_id = t.target_id AND fs.name = 'max_file_size'
			JOIN sys.server_event_session_fields AS fr ON fr.event_session_id = t.event_session_id AND fr.object_id = t.target_id AND fr.name = 'max_rollover_files'
			WHERE t.event_session_id = @SessionID
			AND t.name = 'event_file'
			AND TRY_CAST(fs.value AS BIGINT) = @MaxFileSizeMB
			AND TRY_CAST(fr.value AS BIGINT) = @MaxRolloverFiles
			)
		)
		SET @IsCurrent = 0

	IF @IsCurrent = 0
	BEGIN
		SET @SQL = N'DROP EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER'
		EXEC sp_executesql @SQL
		SET @Rebuilt = 1
	END
END

IF NOT EXISTS(SELECT 1 FROM sys.server_event_sessions WHERE name = @SessionName)
BEGIN
	SET @SQL = N'CREATE EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER
	ADD EVENT sqlserver.rpc_completed(
		ACTION(' + @Actions + N')
		WHERE ' + @RpcPredicate + N'),
	ADD EVENT sqlserver.sql_batch_completed(
		ACTION(' + @Actions + N')
		WHERE ' + @BatchPredicate + N')
	ADD TARGET package0.event_file(SET filename=N''' + REPLACE(@SessionName, '''', '''''') + N'.xel'', max_file_size=(' + CAST(@MaxFileSizeMB AS NVARCHAR(20)) + N'), max_rollover_files=(' + CAST(@MaxRolloverFiles AS NVARCHAR(20)) + N'))
	WITH (MAX_MEMORY=' + CAST(@MaxMemory AS NVARCHAR(20)) + N' KB,
		EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
		MAX_DISPATCH_LATENCY=5 SECONDS,
		MAX_EVENT_SIZE=0 KB,
		MEMORY_PARTITION_MODE=NONE,
		TRACK_CAUSALITY=OFF,
		STARTUP_STATE=' + CASE WHEN @StartupState = 1 THEN N'ON' ELSE N'OFF' END + N')'
	EXEC sp_executesql @SQL
	SET @Created = 1
END

/*	Started separately: the session can exist but be stopped - the service stops rather than drops it when
	PersistXESessions is set, or someone stopped it by hand. */
IF NOT EXISTS(SELECT 1 FROM sys.dm_xe_sessions WHERE name = @SessionName)
BEGIN
	SET @SQL = N'ALTER EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER STATE = START'
	EXEC sp_executesql @SQL
END

SELECT @Created AS Created, @Rebuilt AS Rebuilt
