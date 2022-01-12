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
IF EXISTS(SELECT 1 
		FROM dbo.RunningQueriesSummary
		WHERE InstanceID = @InstanceID
		AND SnapshotDateUTC = @SnapshotDate
	)
BEGIN
	/* Blocking data now comes from running queries snapshot */
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
END
ELSE
BEGIN
	/* Legacy blocking capture */
	SELECT 
		   BSS.session_id,
		   BSS.blocking_session_id,
		   ISNULL(BSS.Txt,'') Txt,
		   BSS.start_time_utc,
		   BSS.command,
		   BSS.database_id,
		   BSS.database_name,
		   BSS.host_name,
		   BSS.program_name,
		   BSS.wait_time,
		   BSS.login_name,
		   BSS.wait_resource,
		   BSS.Status,
		   stat.BlockCountRecursive,
		   stat.BlockWaitTimeRecursive AS WaitTimeRecursive,
		   stat.BlockCount,
		   stat.BlockWaitTime,
		   BSS.session_status,
		   CASE BSS.transaction_isolation_level WHEN 0 THEN 'Unspecified' WHEN 1 THEN 'ReadUncommitted' WHEN 2 THEN 'ReadCommitted' WHEN 3 THEN 'RepeatableRead' WHEN 4 THEN 'Serializable' WHEN 5 THEN 'Snapshot' ELSE '?' END AS transaction_isolation_level
	FROM dbo.BlockingSnapshot BSS
	OUTER APPLY dbo.BlockingSnapshotRecursiveStats(BSS.BlockingSnapshotID,BSS.session_id,BSS.SnapshotDateUTC) stat
	WHERE BSS.BlockingSnapshotID = @BlockingSnapshotID
	AND (BSS.blocking_session_id=@blocking_session_id
			OR (@blocking_session_id=0
				/* 
				Session is not blocked by another sessionid in this snapshot. 
				(Either root blocker with blocking_session_id = 0, or we did not capture the root blocker)
				*/
				AND NOT EXISTS(SELECT 1 
							FROM dbo.BlockingSnapshot BSS2 
							WHERE BSS2.BlockingSnapshotID=BSS.BlockingSnapshotID
							AND BSS2.session_id=BSS.blocking_session_id
							)
				)
		)
	ORDER BY stat.BlockWaitTimeRecursive DESC
END