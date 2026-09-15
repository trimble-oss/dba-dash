/*
	Creates and starts the extended events session DBA Dash manages for deadlock collection, if it isn't
	already there.  Only ever run for the reserved session name - DBA Dash never creates or alters a
	session it does not own, including system_health.

	Why a dedicated session rather than reading system_health: the cost of reading an event file is
	dominated by opening and seeking the file set, not by the number of new events, so reading
	system_health costs seconds per collection however little has happened, and gets worse as its files
	grow.  A session holding nothing but deadlock reports is tiny, and reading it costs milliseconds.

	What it gives up is history: a new session starts empty, where system_health already holds whatever
	the instance has done recently.

	event_file rather than ring_buffer: it survives a restart of the instance, and it is what the
	resume cursor reads.  The file name is unqualified so SQL Server puts it in the instance's own log
	directory, next to system_health's, rather than DBA Dash choosing a path it cannot verify.

	Sized small deliberately.  Deadlocks are rare and the collection reads every few minutes, so the
	files exist to bridge a service outage, not to be an archive - the repository is the archive.

	Azure SQL Managed Instance is the exception, and gets a ring buffer.  A session created there can only
	have an event_file in blob storage - the filename has to be an https:// URL, and an unqualified one
	fails with Msg 40538 - which needs a storage container and a credential DBA Dash would have to be given.  It is otherwise the Azure SQL Database session one scope
	up, and the reasoning in the Azure script applies: @RingBufferKB sizes the buffer, a session built at
	a different size is dropped and rebuilt, and MAX_DISPATCH_LATENCY is short because it is the window a
	ring buffer flush can lose a deadlock in.
*/
DECLARE @SessionName SYSNAME = @Name
DECLARE @SQL NVARCHAR(MAX)
DECLARE @IsManagedInstance BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) = 8 THEN 1 ELSE 0 END

/*	Resizing a ring buffer - see the Azure script for why this is a drop rather than an ALTER.

	Managed instance only, since that is the only place this script builds a ring buffer.  Anywhere else a
	ring buffer on a session of this name was put there by someone other than DBA Dash, and dropping it
	would destroy their session rather than resize ours. */
IF @IsManagedInstance = 1
	AND EXISTS(SELECT 1
		FROM sys.server_event_sessions
		WHERE name = @SessionName)
BEGIN
	DECLARE @CurrentKB INT

	SELECT @CurrentKB = TRY_CAST(f.value AS INT)
	FROM sys.server_event_sessions AS s
	JOIN sys.server_event_session_targets AS t
		ON t.event_session_id = s.event_session_id
	JOIN sys.server_event_session_fields AS f
		ON f.event_session_id = t.event_session_id
		AND f.object_id = t.target_id
		AND f.name = 'max_memory'
	WHERE s.name = @SessionName
	AND t.name = 'ring_buffer'

	IF @CurrentKB IS NOT NULL
		AND @CurrentKB <> @RingBufferKB
	BEGIN
		SET @SQL = N'DROP EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER'
		EXEC sp_executesql @SQL
	END
END

IF NOT EXISTS(SELECT 1
			FROM sys.server_event_sessions
			WHERE name = @SessionName)
BEGIN
	IF @IsManagedInstance = 1
	BEGIN
		SET @SQL = N'CREATE EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER
		ADD EVENT sqlserver.xml_deadlock_report
		ADD TARGET package0.ring_buffer(SET max_memory=(' + CAST(@RingBufferKB AS NVARCHAR(20)) + N'))
		WITH (MAX_MEMORY=4096 KB,
			EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
			MAX_DISPATCH_LATENCY=3 SECONDS,
			MAX_EVENT_SIZE=0 KB,
			MEMORY_PARTITION_MODE=NONE,
			TRACK_CAUSALITY=OFF,
			STARTUP_STATE=ON)'
	END
	ELSE
	BEGIN
		SET @SQL = N'CREATE EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER
		ADD EVENT sqlserver.xml_deadlock_report
		ADD TARGET package0.event_file(SET filename=N''' + REPLACE(@SessionName, '''', '''''') + N'.xel'', max_file_size=(10), max_rollover_files=(4))
		WITH (MAX_MEMORY=4096 KB,
			EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
			MAX_DISPATCH_LATENCY=30 SECONDS,
			MAX_EVENT_SIZE=0 KB,
			MEMORY_PARTITION_MODE=NONE,
			TRACK_CAUSALITY=OFF,
			STARTUP_STATE=ON)'
	END

	EXEC sp_executesql @SQL
END

/*	Started separately: the session can exist but be stopped, either because STARTUP_STATE was changed
	or because someone stopped it. */
IF NOT EXISTS(SELECT 1
			FROM sys.dm_xe_sessions
			WHERE name = @SessionName)
BEGIN
	SET @SQL = N'ALTER EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER STATE = START'
	EXEC sp_executesql @SQL
END
