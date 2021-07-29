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

SELECT HD.[dd hh:mm:ss] as [Duration],
    QT.text AS batch_text,
	SUBSTRING(QT.text,ISNULL((NULLIF(Q.statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(Q.statement_end_offset,-1) - NULLIF(Q.statement_start_offset,-1))/2+1,2147483647)) AS text,
	CAST(DECOMPRESS(QP.query_plan_compresed) AS XML) query_plan,
    Q.SnapshotDateUTC,
    Q.session_id,
    Q.command,
    Q.status,
    Q.wait_time,
    Q.wait_type,
    Q.wait_resource,
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
    Q.program_name,
    Q.client_interface_name,
    Q.start_time_utc,
    Q.last_request_start_time_utc,
    CONVERT(CHAR(90),Q.sql_handle,1) AS sql_handle,
    CONVERT(CHAR(90),Q.plan_handle,1) AS plan_handle,
    CONVERT(CHAR(18),Q.query_hash,1) AS query_hash,
    CONVERT(CHAR(18),Q.query_plan_hash,1) AS query_plan_hash,
    calc.Duration AS [Duration (ms)]
FROM dbo.RunningQueries Q 
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
CROSS APPLY(SELECT DATEDIFF_BIG(ms,ISNULL(start_time_utc,last_request_start_time_utc),Q.SnapshotDateUTC) AS Duration) calc
CROSS APPLY dbo.SecondsToHumanDuration (calc.Duration/1000) HD
LEFT JOIN dbo.QueryPlans QP ON QP.plan_handle = Q.plan_handle AND QP.query_plan_hash = Q.query_plan_hash AND QP.statement_start_offset = Q.statement_start_offset AND QP.statement_end_offset = Q.statement_end_offset
WHERE Q.SnapshotDateUTC = @SnapshotDate
AND Q.InstanceID = @InstanceID
ORDER BY Duration DESC