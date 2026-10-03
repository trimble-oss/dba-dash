IF EXISTS(SELECT 1 
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_1'
			)
BEGIN
	DROP EVENT SESSION DBADash_1 ON SERVER;
END
IF EXISTS(SELECT 1 
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_2'
			)
BEGIN
	DROP EVENT SESSION DBADash_2 ON SERVER;
END
/* Event file mode session.  Left running when configured to, so it captures while the service is down.
   Dropping it leaves its files in the log directory - SQL Server never deletes event files on a drop. */
IF @KeepSlowQuerySession = 0
	AND EXISTS(SELECT 1
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_SlowQueries'
			)
BEGIN
	DROP EVENT SESSION DBADash_SlowQueries ON SERVER;
END