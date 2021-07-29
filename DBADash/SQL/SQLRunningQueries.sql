DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT SYSUTCDATETIME() as SnapshotDateUTC,
	s.session_id,
	r.statement_start_offset,
	r.statement_end_offset,
	R.command,
	s.status,
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
	s.open_transaction_count,
	s.transaction_isolation_level,
	s.login_name,
	s.host_name,
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'database_id','ColumnID') IS NULL THEN 'r.database_id,' ELSE 's.database_id,' /* 2012+ */ END + ' 
	s.program_name,
	s.client_interface_name,
	DATEADD(mi,DATEDIFF(minute, GETDATE(),GETUTCDATE()), r.start_time) AS start_time_utc,
	DATEADD(mi,DATEDIFF(minute, GETDATE(),GETUTCDATE()), s.last_request_start_time) AS last_request_start_time_utc,
	ISNULL(r.sql_handle,c.most_recent_sql_handle) as sql_handle,
	r.plan_handle,
	r.query_hash,
	r.query_plan_hash
FROM sys.dm_exec_sessions s
INNER JOIN sys.dm_exec_connections C ON C.session_id= S.session_id
LEFT JOIN sys.dm_exec_requests r on s.session_id = r.session_id
WHERE s.is_user_process=1
' +CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL THEN '' ELSE 'AND (s.open_transaction_count > 0 OR r.session_id IS NOT NULL)' END + /* 2012+ */ '
AND s.session_id <> @@SPID
AND s.session_id > 0'
+ CASE WHEN SERVERPROPERTY('EditionID') = 1674378470 THEN 'AND s.database_id = DB_ID()' /* DB filter for Azure */ ELSE '' END 

EXEC sp_executesql @SQL