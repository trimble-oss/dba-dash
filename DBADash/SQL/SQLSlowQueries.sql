/* Setup - create event sessions if they don't exist */
DECLARE @SQL NVARCHAR(MAX)
DECLARE @EventSessionTemplate NVARCHAR(MAX) = N'CREATE EVENT SESSION [{EventSessionName}] ON SERVER
	ADD EVENT sqlserver.rpc_completed(
		ACTION(sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_id,sqlserver.username,sqlserver.session_id,sqlserver.context_info' + CASE WHEN @CollectGroupIDAndPoolID=1 THEN ',sqlserver.session_resource_group_id,sqlserver.session_resource_pool_id' ELSE '' END + ')
		WHERE ([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(MAX)) + ') AND ([sqlserver].[client_app_name]<>N''DBADashXE'' AND [object_name]<>N''sp_readrequest''))),
	ADD EVENT sqlserver.sql_batch_completed(
		ACTION(sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_id,sqlserver.username,sqlserver.session_id,sqlserver.context_info' + CASE WHEN @CollectGroupIDAndPoolID=1 THEN ',sqlserver.session_resource_group_id,sqlserver.session_resource_pool_id' ELSE '' END + ')
		WHERE ([duration]>(' + CAST(@SlowQueryThreshold AS NVARCHAR(MAX)) + ') AND ([sqlserver].[client_app_name]<>N''DBADashXE'')))
	ADD TARGET package0.ring_buffer' + CASE WHEN @MaxTargetMemory > 0 THEN N' (SET max_memory=' + CAST(@MaxTargetMemory AS NVARCHAR(MAX)) + N')' ELSE N'' END + '
	WITH (MAX_MEMORY=' + CAST(@MaxMemory AS NVARCHAR(MAX)) + ' KB,EVENT_RETENTION_MODE=ALLOW_MULTIPLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=2 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)'


-- create the fisrt event session if it doesn't already exist
IF NOT EXISTS(SELECT 1 
			FROM sys.server_event_sessions
			WHERE name = 'DBADash_1'
			)
BEGIN
	SET @SQL = REPLACE(@EventSessionTemplate,'{EventSessionName}','DBADash_1')
	EXEC sp_executesql @SQL
END

IF @UseDualSession=1
BEGIN
	--create the second event session if it doesn't already exist (in overlap/dual session mode)
	IF NOT EXISTS(SELECT 1 
				FROM sys.server_event_sessions
				WHERE name = 'DBADash_2'
				) 
	BEGIN
		SET @SQL = REPLACE(@EventSessionTemplate,'{EventSessionName}','DBADash_2')
		EXEC sp_executesql @SQL
	END
END
ELSE
BEGIN
	-- Remove the second event session if it exists and we are not running (single session mode)
	IF EXISTS(SELECT 1 
				FROM sys.server_event_sessions
				WHERE name = 'DBADash_2'
				) 
	BEGIN
		DROP EVENT SESSION DBADash_2 ON SERVER;
	END

END
-- ensure the session is started
IF NOT EXISTS(
	SELECT * 
	FROM sys.dm_xe_sessions
	WHERE name = 'DBADash_1'
	)
BEGIN
	ALTER EVENT SESSION DBADash_1
	ON SERVER
	STATE = START
END

IF @UseDualSession=1
BEGIN
	-- Only need to start the second session in overlap mode.  Provides a period of time where both sessions are running to reduce the chances of missing events.
	IF NOT EXISTS(
		SELECT * 
		FROM sys.dm_xe_sessions
		WHERE name = 'DBADash_2'
		)
	BEGIN
		ALTER EVENT SESSION DBADash_2
		ON SERVER
		STATE = START
	END
	-- Allow time for events to be dispatched
	WAITFOR DELAY '00:00:01'
END

-- Get events from the oldest session
DECLARE @target_data NVARCHAR(MAX)
DECLARE @OldestSession SYSNAME
-- Return ring buffer from oldest session
SELECT TOP(1) @target_data= t.target_data, @OldestSession=s.name
FROM sys.dm_xe_sessions AS s 
JOIN sys.dm_xe_session_targets AS t 
    ON t.event_session_address = s.address
WHERE s.name IN('DBADash_1','DBADash_2')
and t.target_name='ring_buffer'
ORDER BY s.create_time 

-- Return captured events
SELECT @target_data as target_data

IF @UseDualSession=1
BEGIN 
	-- In dual session mode we stop the oldest event and leave the one just started running
	IF @OldestSession='DBADash_1'
	BEGIN
		ALTER EVENT SESSION DBADash_1
		ON SERVER
		STATE = STOP
	END
	ELSE IF @OldestSession='DBADash_2'
	BEGIN
		ALTER EVENT SESSION DBADash_2
		ON SERVER
		STATE = STOP
	END
END
ELSE
BEGIN
	-- In single session mode, stop/start the session to flush the data from the ring buffer (reduces the amount of data we capture on next run)
	ALTER EVENT SESSION DBADash_1
	ON SERVER
	STATE = STOP

	ALTER EVENT SESSION DBADash_1
	ON SERVER
	STATE = START
END