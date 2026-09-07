/*
	Creates and starts the database scoped extended events session DBA Dash manages for deadlock
	collection on Azure SQL Database, if it isn't already there.  Only ever run for the reserved session
	name - DBA Dash never creates or alters a session it does not own.

	Azure SQL Database has no system_health session and no server scoped event sessions, so unlike the
	on-premises case there is nothing to fall back on: without this session there is nothing to read.

	sqlserver.database_xml_deadlock_report rather than sqlserver.xml_deadlock_report: the server scoped
	event is not available to a database scoped session.  It carries the same graph, wrapped in an event
	that also names the database and the deadlock cycle.

	ring_buffer rather than event_file: an event_file target on Azure SQL Database writes to blob storage,
	which needs a storage container and a database scoped credential that DBA Dash would have to be given.
	The ring buffer needs nothing.  What it gives up is persistence - the buffer is emptied when the
	session stops, which includes a failover - so a deadlock is only collected if a collection runs before
	the next restart.  Deadlocks are read every few minutes and the repository is the archive, so that
	window is small.

	@RingBufferKB sizes the buffer and is a service level setting.  Bigger bridges a longer outage and
	costs read time, since a ring buffer read costs what the buffer holds rather than what is new in it.
	Above 1MB SQL Server warns that a ring buffer's target_data can come back truncated, which loses the
	whole read rather than part of it, so the default is 1MB and going higher is a deliberate choice - the
	collection warns when it sees a truncated buffer.  The session's own MAX_MEMORY, which is the event
	buffering rather than the target, stays at the 4MB ceiling Azure SQL Database allows.

	MAX_DISPATCH_LATENCY is short - 3 seconds rather than the 30 the on-premises session uses.  It is the
	time a deadlock can sit in the session's memory buffer before it reaches the target, so it is both how
	stale a collection can be and, when the collection is set to empty the buffer after reading it, the
	window in which a deadlock can be lost to the stop.  Dispatching promptly costs nothing here because
	there is nothing else in the session: it carries deadlock reports and they are rare.
*/
DECLARE @SessionName SYSNAME = @Name
DECLARE @SQL NVARCHAR(MAX)

/*	Resizing.  A target's options cannot be altered in place, and ALTER EVENT SESSION will not take an ADD
	and a DROP in the same statement, so the session is dropped here and rebuilt by the block below at the
	configured size.

	Dropping the session rather than swapping its target is also the safer of the two: a failed ADD would
	leave the session running with no target to read and nothing would ever rebuild it, where a failed
	CREATE leaves no session at all, which is the state a fresh deployment starts in and the next
	collection fixes.

	Costs whatever the buffer was holding - at most one collection interval of deadlocks, and only on the
	run after the setting changed. */
IF EXISTS(SELECT 1
		FROM sys.database_event_sessions
		WHERE name = @SessionName)
BEGIN
	/*	The size the session was built with, read from the catalog rather than assumed.  NULL means its
		ring buffer has no explicit max_memory - which one DBA Dash created always has - so the session
		came from somewhere else and is left alone. */
	DECLARE @CurrentKB INT

	SELECT @CurrentKB = TRY_CAST(f.value AS INT)
	FROM sys.database_event_sessions AS s
	JOIN sys.database_event_session_targets AS t
		ON t.event_session_id = s.event_session_id
	JOIN sys.database_event_session_fields AS f
		ON f.event_session_id = t.event_session_id
		AND f.object_id = t.target_id
		AND f.name = 'max_memory'
	WHERE s.name = @SessionName
	AND t.name = 'ring_buffer'

	IF @CurrentKB IS NOT NULL
		AND @CurrentKB <> @RingBufferKB
	BEGIN
		SET @SQL = N'DROP EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON DATABASE'
		EXEC sp_executesql @SQL
	END
END

IF NOT EXISTS(SELECT 1
			FROM sys.database_event_sessions
			WHERE name = @SessionName)
BEGIN
	SET @SQL = N'CREATE EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON DATABASE
	ADD EVENT sqlserver.database_xml_deadlock_report
	ADD TARGET package0.ring_buffer(SET max_memory=(' + CAST(@RingBufferKB AS NVARCHAR(20)) + N'))
	WITH (MAX_MEMORY=4096 KB,
		EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
		MAX_DISPATCH_LATENCY=3 SECONDS,
		MAX_EVENT_SIZE=0 KB,
		MEMORY_PARTITION_MODE=NONE,
		TRACK_CAUSALITY=OFF,
		STARTUP_STATE=ON)'

	EXEC sp_executesql @SQL
END

/*	Started separately: the session can exist but be stopped, either because STARTUP_STATE was changed
	or because someone stopped it. */
IF NOT EXISTS(SELECT 1
			FROM sys.dm_xe_database_sessions
			WHERE name = @SessionName)
BEGIN
	SET @SQL = N'ALTER EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON DATABASE STATE = START'
	EXEC sp_executesql @SQL
END
