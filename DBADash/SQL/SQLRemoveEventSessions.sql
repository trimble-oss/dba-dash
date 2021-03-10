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