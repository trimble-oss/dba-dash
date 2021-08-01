CREATE PROC dbo.RunningQueries_Get(
	@InstanceID INT,
	@SnapshotDate DATETIME2=NULL OUT
)
AS
IF @SnapshotDate IS NULL
BEGIN
    SELECT TOP(1) @SnapshotDate=SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
    ORDER BY SnapshotDateUTC DESC
END

SELECT HD.HumanDuration as [Duration],
    QT.text AS batch_text,
	SUBSTRING(QT.text,ISNULL((NULLIF(Q.statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(NULLIF(Q.statement_end_offset,-1),0) - NULLIF(Q.statement_start_offset,-1))/2+1,2147483647)) AS text,
	CAST(DECOMPRESS(QP.query_plan_compresed) AS XML) query_plan,
	ISNULL(QT.object_id,QP.object_id) AS object_id,
	O.SchemaName + '.' +  O.ObjectName AS object_name,
    Q.SnapshotDateUTC,
    Q.session_id,
    Q.command,
    Q.status,
    Q.wait_time,
    Q.wait_type,
    Q.blocking_session_id,
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
        ELSE CAST(Q.transaction_isolation_level AS VARCHAR(30)) END as transaction_isolation_level,
    Q.login_name,
    Q.host_name,
    Q.database_id,
	CONCAT(D.name,', ' + NULLIF(objD.name,D.name))  AS database_name,
    Q.program_name,
    Q.client_interface_name,
    Q.start_time_utc,
    Q.last_request_start_time_utc,
    CONVERT(CHAR(90),Q.sql_handle,1) AS sql_handle,
    CONVERT(CHAR(90),Q.plan_handle,1) AS plan_handle,
    CONVERT(CHAR(18),Q.query_hash,1) AS query_hash,
    CONVERT(CHAR(18),Q.query_plan_hash,1) AS query_plan_hash,
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
	waitF.filegroup_name + ' | ' + waitF.name AS wait_file
FROM dbo.RunningQueries Q 
CROSS APPLY(SELECT DATEDIFF_BIG(ms,ISNULL(start_time_utc,last_request_start_time_utc),Q.SnapshotDateUTC) AS Duration) calc
CROSS APPLY dbo.MillisecondsToHumanDuration (calc.Duration) HD
CROSS APPLY dbo.SplitWaitResource(Q.wait_resource) waitR
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
LEFT JOIN dbo.QueryPlans QP ON QP.plan_handle = Q.plan_handle AND QP.query_plan_hash = Q.query_plan_hash AND QP.statement_start_offset = Q.statement_start_offset AND QP.statement_end_offset = Q.statement_end_offset
LEFT JOIN dbo.Databases D ON Q.database_id =D.database_id AND D.IsActive=1 AND D.InstanceID = Q.InstanceID
LEFT JOIN dbo.Databases objD ON ISNULL(QT.dbid,QP.dbid) =objD.database_id AND objD.IsActive=1 AND objD.InstanceID = Q.InstanceID
LEFT JOIN dbo.DBObjects O ON objD.DatabaseID = O.DatabaseID AND O.object_id = ISNULL(QT.object_id,QP.object_id)
LEFT JOIN dbo.Databases waitD ON waitD.InstanceID = Q.InstanceID AND waitR.wait_database_id = waitD.database_id AND waitD.IsActive=1
LEFT JOIN dbo.DBObjects waitO ON waitR.wait_object_id = waitO.object_id AND waitO.DatabaseID = waitD.DatabaseID 
LEFT JOIN dbo.DBFiles waitF ON waitF.DatabaseID = waitD.DatabaseID AND waitF.file_id = waitR.wait_file_id
WHERE Q.SnapshotDateUTC = @SnapshotDate
AND Q.InstanceID = @InstanceID
ORDER BY calc.Duration DESC