CREATE PROC dbo.Blocking_Get(
	/* Supply @BlockingSnapshotID OR @InstanceID + @SnapshotDate */
	@BlockingSnapshotID INT=NULL,
	@blocking_session_id INT=0, /* Start with blocking_session_id 0 (root blocker) */
	@InstanceID INT=NULL,
	@SnapshotDate DATETIME2(7)=NULL
)
AS
IF @InstanceID IS NULL OR @SnapshotDate IS NULL
BEGIN
	SELECT @InstanceID = InstanceID,
			@SnapshotDate = SnapshotDateUTC
	FROM dbo.BlockingSnapshotSummary
	WHERE BlockingSnapshotID=@BlockingSnapshotID
END

SELECT Q.session_id,
	ISNULL(Q.blocking_session_id,0) AS blocking_session_id,
	SUBSTRING(QT.text,ISNULL((NULLIF(Q.statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(Q.statement_end_offset,-1) - NULLIF(Q.statement_start_offset,-1))/2+1,2147483647)) AS Txt,
	ISNULL(Q.start_time_utc,Q.last_request_start_time_utc) AS start_time_utc,
	Q.command,
	Q.database_id,
	D.name AS database_name,
	Q.host_name,
	Q.program_name,
	Q.wait_time,
	Q.login_name,
	Q.wait_resource,
	Q.status,
	stat.BlockCountRecursive,
	stat.BlockWaitTimeRecursiveMs AS WaitTimeRecursive,
	stat.BlockCount,
	stat.BlockWaitTimeMs as BlockWaitTime,
	Q.status AS session_status,
	CASE Q.transaction_isolation_level WHEN 0 THEN 'Unspecified'
		WHEN 1 THEN 'ReadUncomitted'
		WHEN 2 THEN 'ReadCommitted'
		WHEN 3 THEN 'Repeatable'
		WHEN 4 THEN 'Serializable'
		WHEN 5 THEN 'Snapshot'
		ELSE CAST(Q.transaction_isolation_level AS VARCHAR(30)) END as transaction_isolation_level
FROM dbo.RunningQueries Q 
LEFT JOIN dbo.QueryText QT ON QT.sql_handle = Q.sql_handle
LEFT JOIN dbo.Databases D ON Q.database_id = D.database_id AND D.InstanceID=@InstanceID AND D.IsActive=1
OUTER APPLY dbo.RunningQueriesBlockingRecursiveStats(@InstanceID,@SnapshotDate,Q.session_id) stat
WHERE Q.InstanceID = @InstanceID
AND Q.SnapshotDateUTC = @SnapshotDate
AND Q.blocking_session_id = @blocking_session_id
AND (stat.BlockCount >0 OR @blocking_session_id <> 0)
ORDER BY stat.BlockWaitTimeRecursiveMs DESC

