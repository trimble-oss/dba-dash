/* Setup - create event sessions if they don't exist */
DECLARE @SQL NVARCHAR(MAX)
DECLARE @EventSessionTemplate NVARCHAR(MAX) = N'CREATE EVENT SESSION [{EventSessionName}] ON SERVER 
	ADD EVENT sqlserver.rpc_completed(
		ACTION(sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_id,sqlserver.username)
		WHERE ([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(MAX)) + ') AND ([sqlserver].[client_app_name]<>N''DBADashXE'' AND [object_name]<>N''sp_readrequest''))),
	ADD EVENT sqlserver.sql_batch_completed(
		ACTION(sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_id,sqlserver.username)
		WHERE ([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(MAX)) + ') AND ([sqlserver].[client_app_name]<>N''DBADashXE'')))
	ADD TARGET package0.ring_buffer
	WITH (MAX_MEMORY=' + CAST(@MaxMemory AS NVARCHAR(MAX)) + ' KB,EVENT_RETENTION_MODE=ALLOW_MULTIPLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=2 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)'

IF NOT EXISTS(SELECT 1 
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_1'
			)
BEGIN
	SET @SQL = REPLACE(@EventSessionTemplate,'{EventSessionName}','DBADash_1')
	EXEC sp_executesql @SQL
END
IF NOT EXISTS(SELECT 1 
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_2'
			)
BEGIN
	SET @SQL = REPLACE(@EventSessionTemplate,'{EventSessionName}','DBADash_2')
	EXEC sp_executesql @SQL
END

-- Start the session that is not running to create overlap
IF NOT EXISTS(
	SELECT * 
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_1'
	)
BEGIN
	ALTER EVENT SESSION DBADash_1
	ON SERVER
	State = START
END
IF NOT EXISTS(
	SELECT * 
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_2'
	)
BEGIN
	ALTER EVENT SESSION DBADash_2
	ON SERVER
	State = START
END

-- Allow time for events to be dispatched
WAITFOR DELAY '00:00:01'

DECLARE @target_data NVARCHAR(MAX)
DECLARE @OldestSession SYSNAME
-- Return ring buffer from oldest session
SELECT TOP(1) @target_data= target_data, @OldestSession=s.name
FROM sys.dm_xe_sessions AS s 
JOIN sys.dm_xe_session_targets AS t 
    ON t.event_session_address = s.address
WHERE s.name IN('DBADash_1','DBADash_2')
and t.target_name='ring_buffer'
ORDER BY s.create_time 

SELECT @target_data as target_data
-- 
IF @OldestSession='DBADash_1'
BEGIN
	ALTER EVENT SESSION DBADash_1
	ON SERVER
	State = STOP
END
ELSE IF @OldestSession='DBADash_2'
BEGIN
	ALTER EVENT SESSION DBADash_2
	ON SERVER
	State = STOP
END