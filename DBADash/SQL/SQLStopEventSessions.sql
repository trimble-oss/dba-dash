IF EXISTS(
	SELECT * 
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_1'
	)
BEGIN
	ALTER EVENT SESSION DBADash_1
	ON SERVER
	State = STOP
END
IF EXISTS(
	SELECT * 
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_2'
	)
BEGIN
	ALTER EVENT SESSION DBADash_2
	ON SERVER
	State = STOP
END
/* Event file mode session.  Left running when configured to, so it captures while the service is down */
IF @KeepSlowQuerySession = 0
	AND EXISTS(
	SELECT *
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_SlowQueries'
	)
BEGIN
	ALTER EVENT SESSION DBADash_SlowQueries
	ON SERVER
	State = STOP
END