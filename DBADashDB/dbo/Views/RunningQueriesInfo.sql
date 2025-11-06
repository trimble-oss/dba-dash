CREATE VIEW dbo.RunningQueriesInfo
AS
SELECT Q.InstanceID,
    I.InstanceDisplayName,
    HD.HumanDuration AS [Duration],
    QT.text AS batch_text,
	SUBSTRING(QT.text,ISNULL((NULLIF(Q.statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(NULLIF(Q.statement_end_offset,-1),0) - NULLIF(Q.statement_start_offset,-1))/2+1,2147483647)) AS text,
	CAST(DECOMPRESS(QP.query_plan_compresed) AS NVARCHAR(MAX)) query_plan,
	ISNULL(QT.object_id,QP.object_id) AS object_id,
	O.SchemaName + '.' +  O.ObjectName AS object_name,
    O.ObjectID AS DBADashObjectID,
    O.SchemaName,
    O.ObjectName,
    Q.SnapshotDateUTC,
    Q.session_id,
    Q.command,
    Q.status,
    Q.wait_time,
    Q.wait_type,
    sessionW.TopSessionWaits,
    Q.blocking_session_id,
    H.BlockingHierarchy,
    RQBRS.BlockCountRecursive, 
	RQBRS.BlockWaitTimeRecursiveMs,
    BlockWaitTimeRecursive.HumanDuration AS BlockWaitTimeRecursive,
	RQBRS.BlockCount,
    CASE WHEN RQBRS.BlockCount>0 AND Q.blocking_session_id=0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsRootBlocker,
	RQBRS.BlockWaitTimeMs,
    BlockWaitTime.HumanDuration AS BlockWaitTime,
    Q.cpu_time,
    Q.logical_reads,
    Q.reads,
    Q.writes,
    Q.granted_query_memory*8 granted_query_memory_kb,
    Q.percent_complete,
    Q.open_transaction_count,
    CASE Q.transaction_isolation_level WHEN 0 THEN 'Unspecified'
        WHEN 1 THEN 'ReadUncomitted'
        WHEN 2 THEN 'ReadCommitted'
        WHEN 3 THEN 'Repeatable'
        WHEN 4 THEN 'Serializable'
        WHEN 5 THEN 'Snapshot'
        ELSE CAST(Q.transaction_isolation_level AS VARCHAR(30)) END AS transaction_isolation_level,
    Q.login_name,
    Q.host_name,
    Q.database_id,
    ISNULL(objD.name,D.name) AS database_name,
	CONCAT(D.name,', ' + NULLIF(objD.name,D.name))  AS database_names,
    Q.program_name,
    jid.job_id,
	J.name AS job_name,
    Q.client_interface_name,
    Q.start_time_utc,
    Q.last_request_start_time_utc,
    Q.last_request_end_time_utc,
    LastRequestDuration.HumanDuration as last_request_duration,
    CASE WHEN status = 'sleeping' THEN DATEDIFF_BIG(ms,Q.last_request_end_time_utc,Q.SnapshotDateUTC)/1000.0 ELSE NULL END AS sleeping_session_idle_time_sec,
    CASE WHEN status = 'sleeping' THEN TimeSinceLastRequestEnd.HumanDuration ELSE NULL END AS sleeping_session_idle_time,
    CONVERT(CHAR(90),Q.sql_handle,1) AS sql_handle,
    CONVERT(CHAR(90),Q.plan_handle,1) AS plan_handle,
    CONVERT(CHAR(18),Q.query_hash,1) AS query_hash,
    CONVERT(CHAR(18),Q.query_plan_hash,1) AS query_plan_hash,
    Q.sql_handle AS sql_handle_bin,
    Q.plan_handle AS plan_handle_bin,
    Q.query_hash AS query_hash_bin,
    Q.query_plan_hash AS query_plan_hash_bin,
    calc.Duration AS [Duration (ms)],
    Q.wait_resource,
    waitR.wait_resource_type,
    waitR.wait_database_id,
    waitR.wait_file_id,
    waitR.wait_page_id,
    waitR.wait_object_id,
    waitR.wait_index_id,
    waitR.wait_hobt,
    waitR.wait_hash,
    waitR.wait_slot,
    waitR.wait_is_compile,
    waitR.page_type,
	CASE WHEN waitR.wait_database_id=2 THEN 'tempdb' ELSE waitD.name END AS wait_db,
	waitO.SchemaName + '.' + waitO.ObjectName AS wait_object,
	waitF.filegroup_name + ' | ' + waitF.name AS wait_file,
    Q.login_time_utc,
    CASE WHEN QP.query_plan_compresed IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS has_plan,
    Q.statement_start_offset,
    Q.statement_end_offset,
    Q.context_info,
    Q.transaction_begin_time_utc,
    calc.transaction_duration_ms,
    TranHD.HumanDuration AS transaction_duration,
    Q.is_implicit_transaction,
    D.is_query_store_on,
    CASE WHEN Q.tempdb_alloc_page_count < Q.tempdb_dealloc_page_count THEN 0 ELSE (Q.tempdb_alloc_page_count - Q.tempdb_dealloc_page_count) / 128.0 END AS tempdb_current_mb,
    Q.tempdb_alloc_page_count /128.0 AS tempdb_allocations_mb 
FROM dbo.RunningQueries Q
JOIN dbo.Instances I ON Q.InstanceID = I.InstanceID
CROSS APPLY(SELECT 	/* 
						If the total_elapsed_time and calculated duration are within 500ms or the calculated duration is negative and total_elapsed_time is less than 30 seconds, use total_elapsed_time.  
						total_elapsed_time might offer better precision, but if it differs too mucg from the calculated duration, it might contain an error. #1491.  
						For sleeping sesions, start_time will be NULL and the calculated duration will be based on last request start time
					*/
				   CASE WHEN (Q.start_time_utc > Q.SnapshotDateUTC AND Q.total_elapsed_time < 30000) 
								OR ABS(DATEDIFF_BIG(ms,Q.start_time_utc,Q.SnapshotDateUTC)-CAST(Q.total_elapsed_time AS BIGINT)) < CAST(500 AS BIGINT) THEN CAST(Q.total_elapsed_time AS BIGINT) 
						WHEN Q.start_time_utc < Q.SnapshotDateUTC OR Q.start_time_utc IS NULL  THEN DATEDIFF_BIG(ms,ISNULL(Q.start_time_utc,Q.last_request_start_time_utc),Q.SnapshotDateUTC) 
						ELSE 0 END AS Duration,
                   CASE WHEN Q.transaction_begin_time_utc<Q.SnapshotDateUTC OR Q.transaction_begin_time_utc IS NULL THEN DATEDIFF_BIG(ms,Q.transaction_begin_time_utc,Q.SnapshotDateUTC) ELSE 0 END AS transaction_duration_ms) calc
CROSS APPLY dbo.MillisecondsToHumanDuration (calc.Duration) HD
CROSS APPLY dbo.SplitWaitResource(Q.wait_resource) waitR
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
LEFT JOIN dbo.QueryPlans QP ON QP.plan_handle = Q.plan_handle AND QP.query_plan_hash = Q.query_plan_hash AND QP.statement_start_offset = Q.statement_start_offset AND QP.statement_end_offset = Q.statement_end_offset
LEFT JOIN dbo.Databases D ON Q.database_id =D.database_id AND D.IsActive=1 AND D.InstanceID = Q.InstanceID
LEFT JOIN dbo.Databases objD ON ISNULL(QT.dbid,QP.dbid) =objD.database_id AND objD.IsActive=1 AND objD.InstanceID = Q.InstanceID
OUTER APPLY(SELECT TOP(1) * 
			FROM dbo.DBObjects O 
			WHERE objD.DatabaseID = O.DatabaseID 
			AND O.object_id = ISNULL(QT.object_id,QP.object_id)
			ORDER  BY O.IsActive DESC,O.ObjectName DESC) O 
LEFT JOIN dbo.Databases waitD ON waitD.InstanceID = Q.InstanceID AND waitR.wait_database_id = waitD.database_id AND waitD.IsActive=1
OUTER APPLY(SELECT TOP(1) *
			FROM dbo.DBObjects waitO
			WHERE waitR.wait_object_id = waitO.object_id 
			AND waitO.DatabaseID = waitD.DatabaseID 
			ORDER  BY O.IsActive DESC,O.ObjectName DESC
			) waitO
LEFT JOIN dbo.DBFiles waitF ON waitF.DatabaseID = waitD.DatabaseID AND waitF.file_id = waitR.wait_file_id
OUTER APPLY (SELECT CASE WHEN program_name LIKE 'SQLAgent - TSQL JobStep (Job 0x%' THEN TRY_CAST(TRY_CONVERT(BINARY(16),SUBSTRING(program_name, 30,34),1) AS UNIQUEIDENTIFIER)  ELSE NULL END AS job_id) jid
LEFT JOIN dbo.Jobs J ON J.job_id = jid.job_id AND J.InstanceID = Q.InstanceID
OUTER APPLY(SELECT STUFF((SELECT TOP(3) ', ' + WT.WaitType + ' (' + CAST(SW.wait_time_ms AS VARCHAR(MAX)) +'ms)'
		FROM dbo.SessionWaits SW 
		JOIN dbo.WaitType WT ON SW.WaitTypeID = WT.WaitTypeID
		WHERE SW.InstanceID = Q.InstanceID 
		AND SW.SnapshotDateUTC = Q.SnapshotDateUTC 
		AND SW.session_id = Q.session_id 
		AND SW.login_time_utc = Q.login_time_utc
		ORDER BY SW.wait_time_ms DESC
		FOR XML PATH('')),1,1,'') AS TopSessionWaits
		) sessionW
OUTER APPLY dbo.RunningQueriesBlockingRecursiveStats(Q.InstanceID,Q.SnapshotDateUTC,Q.session_id) AS RQBRS
OUTER APPLY dbo.RunningQueriesBlockingHierarchy(Q.InstanceID,Q.SnapshotDateUTC,Q.blocking_session_id) AS H
CROSS APPLY dbo.MillisecondsToHumanDuration (RQBRS.BlockWaitTimeMs) AS BlockWaitTime
CROSS APPLY dbo.MillisecondsToHumanDuration (RQBRS.BlockWaitTimeRecursiveMs) AS BlockWaitTimeRecursive
CROSS APPLY dbo.MillisecondsToHumanDuration (DATEDIFF_BIG(ms,Q.last_request_end_time_utc,Q.SnapshotDateUTC)) AS TimeSinceLastRequestEnd
CROSS APPLY dbo.MillisecondsToHumanDuration (DATEDIFF_BIG(ms,Q.last_request_start_time_utc,Q.last_request_end_time_utc)) AS LastRequestDuration
CROSS APPLY dbo.MillisecondsToHumanDuration (calc.transaction_duration_ms) TranHD