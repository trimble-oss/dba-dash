DECLARE @UTCOffset INT 
DECLARE @SnapshotDateUTC DATETIME
SET @UTCOffset = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT)
SET @SnapshotDateUTC = GETUTCDATE() 
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT @SnapshotDateUTC as SnapshotDateUTC,
	s.session_id,
	r.statement_start_offset,
	r.statement_end_offset,
	r.command,
	ISNULL(r.status,s.status) as status,
	r.wait_time,
	r.wait_type,
	r.wait_resource,
	ISNULL(r.blocking_session_id,0) as blocking_session_id,
	ISNULL(r.cpu_time,s.cpu_time) cpu_time,
	ISNULL(r.logical_reads,s.logical_reads) logical_reads,
	ISNULL(r.reads,s.reads) reads,
	ISNULL(r.writes,s.writes) writes,
	r.granted_query_memory,
	r.percent_complete,
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL THEN 'r.open_transaction_count,' ELSE 's.open_transaction_count,' END + '
	s.transaction_isolation_level,
	s.login_name,
	s.host_name,
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'database_id','ColumnID') IS NULL THEN 'r.database_id,' ELSE 's.database_id,' /* 2012+ */ END + ' 
	s.program_name,
	s.client_interface_name,
	DATEADD(mi,@UTCOffset, r.start_time) AS start_time_utc,
	DATEADD(mi,@UTCOffset, s.last_request_start_time) AS last_request_start_time_utc,
	ISNULL(r.sql_handle,c.most_recent_sql_handle) as sql_handle,
	r.plan_handle,
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_requests'),'query_hash','ColumnID') IS NULL THEN 'CAST(NULL AS BINARY(8)) AS query_hash,' ELSE 'r.query_hash,' END + '
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_requests'),'query_plan_hash','ColumnID') IS NULL THEN 'CAST(NULL AS BINARY(8)) AS query_plan_hash,' ELSE 'r.query_plan_hash,' END + '
	DATEADD(mi,@UTCOffset, s.login_time) AS login_time_utc
FROM sys.dm_exec_sessions s
INNER JOIN sys.dm_exec_connections c ON c.session_id= s.session_id
LEFT JOIN sys.dm_exec_requests r on s.session_id = r.session_id
WHERE s.is_user_process=1
' +CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL 
		-- For older instances
		THEN 'AND (r.session_id IS NOT NULL 
						OR EXISTS(SELECT 1 FROM sys.dm_tran_session_transactions t WHERE t.session_id = s.session_id)
				)' 
		-- For 2012+
		ELSE 'AND (s.open_transaction_count > 0 OR r.session_id IS NOT NULL)' END + '
AND s.session_id <> @@SPID
AND s.session_id > 0'
+ CASE WHEN SERVERPROPERTY('EngineEditionID') = 5 THEN 'AND s.database_id = DB_ID()' /* DB filter for Azure */ ELSE '' END 

EXEC sp_executesql @SQL,N'@UTCOffset INT,@SnapshotDateUTC DATETIME',@UTCOffset,@SnapshotDateUTC

IF @CollectSessionWaits=1 AND OBJECT_ID('sys.dm_exec_session_wait_stats') IS NOT NULL
BEGIN
	SELECT @SnapshotDateUTC AS SnapshotDateUTC,
			sws.session_id,
			sws.wait_type,
			sws.waiting_tasks_count,
			sws.wait_time_ms,
			sws.max_wait_time_ms,
			sws.signal_wait_time_ms,
			DATEADD(mi,@UTCOffset,s.login_time) login_time_utc
	FROM sys.dm_exec_session_wait_stats sws
	JOIN sys.dm_exec_sessions s ON sws.session_id = s.session_id 
	WHERE (s.open_transaction_count>0
			OR s.last_request_end_time>=DATEADD(s,-2,GETUTCDATE())
			OR EXISTS(SELECT 1 
					 FROM sys.dm_exec_requests r 
					 WHERE r.session_id = s.session_id
					 )
		  )
END