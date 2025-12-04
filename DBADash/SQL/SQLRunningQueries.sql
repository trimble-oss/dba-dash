/*
DECLARE @CollectSessionWaits BIT 
DECLARE @CollectTranBeginTime BIT 
DECLARE @CollectTempDB BIT
DECLARE @CollectTaskWaits BIT
DECLARE @CollectCursors BIT
SELECT @CollectSessionWaits =0,@CollectTranBeginTime = 1, @CollectTempDB=1, @CollectTaskWaits=1
*/

DECLARE @HasOpenTranCount BIT
DECLARE @UTCOffset INT 
DECLARE @SnapshotDateUTC DATETIME
SELECT	@HasOpenTranCount =CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END,
		@UTCOffset = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT),
		@SnapshotDateUTC = GETUTCDATE()

DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = CAST(N'
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
	' AS NVARCHAR(MAX)) + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL THEN 'r.open_transaction_count,' ELSE 's.open_transaction_count,' END + '
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
	DATEADD(mi,@UTCOffset, s.login_time) AS login_time_utc,
	DATEADD(mi,@UTCOffset, s.last_request_end_time) AS last_request_end_time_utc,
	s.context_info,
	' + CASE WHEN @CollectTranBeginTime=1 OR @HasOpenTranCount=0 THEN 'DATEADD(mi,@UTCOffset, t.transaction_begin_time)' ELSE 'CAST(NULL AS DATETIME)' END + ' AS transaction_begin_time_utc,
	' + CASE WHEN @CollectTranBeginTime=1 OR @HasOpenTranCount=0 THEN 'ISNULL(t.is_implicit_transaction,CAST(0 AS BIT))' ELSE 'CAST(NULL AS BIT)' END + ' AS is_implicit_transaction,
	r.total_elapsed_time,
	' + CASE WHEN @CollectTempDB=1 THEN 'tempdb_alloc.sum_tempdb_alloc_page_count' ELSE 'CAST(NULL AS BIGINT)' END + ' AS tempdb_alloc_page_count,
	' + CASE WHEN @CollectTempDB=1 THEN 'tempdb_alloc.sum_tempdb_dealloc_page_count' ELSE 'CAST(NULL AS BIGINT)' END + ' AS tempdb_dealloc_page_count,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_type_1' ELSE 'CAST(NULL AS NVARCHAR(60))' END + ' AS task_wait_type_1,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_time_1' ELSE 'CAST(NULL AS BIGINT)' END + ' AS task_wait_time_1,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_type_2' ELSE 'CAST(NULL AS NVARCHAR(60))' END + ' AS task_wait_type_2,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_time_2' ELSE 'CAST(NULL AS BIGINT)' END + ' AS task_wait_time_2,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_type_3' ELSE 'CAST(NULL AS NVARCHAR(60))' END + ' AS task_wait_type_3,
	' + CASE WHEN @CollectTaskWaits=1 THEN 'oswt_pivot.task_wait_time_3' ELSE 'CAST(NULL AS BIGINT)' END + ' AS task_wait_time_3,
	' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_requests'),'dop','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS dop' ELSE 'r.dop' END + '
FROM sys.dm_exec_sessions s
' + CASE WHEN @CollectTranBeginTime=1 OR @HasOpenTranCount=0 THEN '
LEFT HASH JOIN (
		/*	
			Get oldest transaction for the session.
			With MARS there might be more than one tran associated with a session.  
			We could get the tran associated with the request joining on transaction_id from sys.dm_exec_requests. 
			This way we get the tran begin time even if there is no active request.
		*/
		SELECT	ST.session_id,
				MIN(AT.transaction_begin_time) AS transaction_begin_time,
				CAST(MAX(CASE WHEN AT.name = ''implicit_transaction'' THEN 1 ELSE 0 END) AS BIT) AS is_implicit_transaction
		FROM sys.dm_tran_session_transactions ST 
		INNER HASH JOIN sys.dm_tran_active_transactions AT ON ST.transaction_id = AT.transaction_id
		GROUP BY ST.session_id
	) AS t ON s.session_id = t.session_id' ELSE '' END + '
INNER HASH JOIN sys.dm_exec_connections c ON c.session_id= s.session_id
LEFT HASH JOIN sys.dm_exec_requests r on s.session_id = r.session_id' 
+ CASE WHEN @CollectTempDB=1 THEN '
LEFT HASH JOIN (	SELECT	tsu.request_id,
							tsu.session_id,
							SUM(user_objects_alloc_page_count+internal_objects_alloc_page_count) AS sum_tempdb_alloc_page_count,
							SUM(user_objects_dealloc_page_count+internal_objects_dealloc_page_count) AS sum_tempdb_dealloc_page_count
					FROM sys.dm_db_task_space_usage tsu
					GROUP BY tsu.request_id,tsu.session_id
				) AS tempdb_alloc
		ON r.session_id = tempdb_alloc.session_id
		AND r.request_id = tempdb_alloc.request_id' ELSE '' END + '
' + CASE WHEN @CollectTaskWaits=1 THEN N'
LEFT HASH JOIN(
			/* Pivot top 3 wait types by total wait time for each session */	
			SELECT	oswt_ranked.session_id,
					MAX(CASE WHEN rnum=1 THEN oswt_ranked.wait_type ELSE NULL END) AS task_wait_type_1,
					MAX(CASE WHEN rnum=1 THEN oswt_ranked.wait_duration_ms ELSE NULL END) AS task_wait_time_1,
					MAX(CASE WHEN rnum=2 THEN oswt_ranked.wait_type ELSE NULL END) AS task_wait_type_2,
					MAX(CASE WHEN rnum=2 THEN oswt_ranked.wait_duration_ms ELSE NULL END) AS task_wait_time_2,
					MAX(CASE WHEN rnum=3 THEN oswt_ranked.wait_type ELSE NULL END) AS task_wait_type_3,
					MAX(CASE WHEN rnum=3 THEN oswt_ranked.wait_duration_ms ELSE NULL END) AS task_wait_time_3
			FROM (		/* Rank wait types by total wait time for each session */
						SELECT	oswt_group.session_id,
								oswt_group.wait_type,
								oswt_group.wait_duration_ms,
								ROW_NUMBER() OVER(PARTITION BY oswt_group.session_id ORDER BY oswt_group.wait_duration_ms DESC) rnum
						FROM ( /* Aggregate task wait times by wait type for each session */
								SELECT		oswt.session_id,
											oswt.wait_type,
											SUM(oswt.wait_duration_ms) wait_duration_ms		
								FROM sys.dm_os_waiting_tasks AS oswt
								WHERE oswt.session_id IS NOT NULL
								/* Should be safe to filter out, potentially reducing the amount of work */
								AND wait_type NOT IN(''DISPATCHER_QUEUE_SEMAPHORE'',''HADR_WORK_QUEUE'',''LOGMGR_QUEUE'',''BROKER_TASK_STOP'')
								GROUP BY	oswt.session_id,
											oswt.wait_type
							) oswt_group
					) oswt_ranked
			WHERE oswt_ranked.rnum <= 3
			GROUP BY oswt_ranked.session_id
			) AS oswt_pivot
			ON oswt_pivot.session_id = r.session_id
' ELSE N'' END + N'
WHERE s.is_user_process=1
' +CASE WHEN @HasOpenTranCount = 0 
		-- For older instances
		THEN 'AND (t.transaction_begin_time IS NOT NULL OR r.session_id IS NOT NULL)' 
		-- For 2012+
		ELSE 'AND (s.open_transaction_count > 0 OR r.session_id IS NOT NULL)' END + '
AND s.session_id <> @@SPID
AND s.session_id > 0'
+ CASE WHEN SERVERPROPERTY('EngineEdition') = 5 THEN 'AND s.database_id = DB_ID()' /* DB filter for Azure */ ELSE '' END 

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
IF @CollectCursors=1
BEGIN
	SELECT	@SnapshotDateUTC AS SnapshotDateUTC,
			session_id,
			name,
			properties,
			sql_handle,
			statement_start_offset,
			statement_end_offset,
			plan_generation_num,
			DATEADD(mi,@UTCOffset,creation_time) AS creation_time_utc,
			is_open,
			is_async_population,
			is_close_on_commit,
			fetch_status,
			fetch_buffer_size,
			fetch_buffer_start,
			ansi_position,
			worker_time,
			reads,
			writes,
			dormant_duration
	FROM sys.dm_exec_cursors (0) c
END
