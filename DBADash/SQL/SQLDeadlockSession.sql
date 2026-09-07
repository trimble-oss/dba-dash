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
*/
DECLARE @SessionName SYSNAME = @Name

IF NOT EXISTS(SELECT 1
			FROM sys.server_event_sessions
			WHERE name = @SessionName)
BEGIN
	DECLARE @SQL NVARCHAR(MAX) = N'CREATE EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER
	ADD EVENT sqlserver.xml_deadlock_report
	ADD TARGET package0.event_file(SET filename=N''' + REPLACE(@SessionName, '''', '''''') + N'.xel'', max_file_size=(10), max_rollover_files=(4))
	WITH (MAX_MEMORY=4096 KB,
		EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
		MAX_DISPATCH_LATENCY=30 SECONDS,
		MAX_EVENT_SIZE=0 KB,
		MEMORY_PARTITION_MODE=NONE,
		TRACK_CAUSALITY=OFF,
		STARTUP_STATE=ON)'

	EXEC sp_executesql @SQL
END

/*	Started separately: the session can exist but be stopped, either because STARTUP_STATE was changed
	or because someone stopped it. */
IF NOT EXISTS(SELECT 1
			FROM sys.dm_xe_sessions
			WHERE name = @SessionName)
BEGIN
	DECLARE @StartSQL NVARCHAR(MAX) = N'ALTER EVENT SESSION ' + QUOTENAME(@SessionName) + N' ON SERVER STATE = START'
	EXEC sp_executesql @StartSQL
END
