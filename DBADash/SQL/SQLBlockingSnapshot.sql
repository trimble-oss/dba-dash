DECLARE @MinWaitTimeMs INT 
SET @MinWaitTimeMs= 1000
DECLARE @DBIDTable NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
DECLARE @OpenTransactionFilter NVARCHAR(MAX)
SELECT @DBIDTable= CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'database_id','ColumnId') IS NULL THEN 'R.' ELSE 'S.' END,
		-- Improves performance for instances with a large number of connections.  2012+.  To be blocked or causing blocking there should be an open transaction.
		@OpenTransactionFilter = CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'open_transaction_count','ColumnId') IS NULL THEN '' ELSE 'AND S.open_transaction_count>0' END

SET @SQL =N'
	DECLARE @UTCOffset INT 
	SELECT @UTCOffset= DATEDIFF(mi,GETDATE(),GETUTCDATE());
	WITH R AS (
		SELECT GETUTCDATE() SnapshotDateUTC,
			@UTCOffset AS UTCOffset,
			S.session_id,
			ISNULL(R.blocking_session_id,0) AS blocking_session_id,
			ISNULL(Rtxt.text,Ctxt.text) AS Txt,
			DATEADD(mi,@UTCOffset,ISNULL(R.start_time,S.last_request_start_time)) as start_time_utc,
			R.command,
			' + @DBIDTable + 'database_id,
			DB_NAME(' + @DBIDTable + 'database_id) database_name,
			S.host_name,
			S.program_name,
			R.wait_time,
			S.login_name,
			R.wait_resource,
			R.status,
			R.wait_type,
			S.status as session_status,
			S.transaction_isolation_level,
			/* Use blocking_session_total_wait to filter for sessions involved in the blocking process (including session causing the blocking) */
			SUM(CASE WHEN R.blocking_session_id>0 THEN R.wait_time ELSE 0 END) OVER(PARTITION BY ISNULL(NULLIF(R.blocking_session_id,0),S.session_id)) AS blocking_session_total_wait,
			/* Get the total wait time for all blocked sessions */
			SUM(CASE WHEN R.blocking_session_id>0 THEN R.wait_time ELSE 0 END) OVER() AS total_blocking_wait
		FROM sys.dm_exec_sessions S
		INNER JOIN sys.dm_exec_connections C ON C.session_id= S.session_id
		LEFT JOIN sys.dm_exec_requests as R ON S.session_id = R.session_id
		OUTER APPLY sys.dm_exec_sql_text(R.sql_handle) AS Rtxt
		OUTER APPLY sys.dm_exec_sql_text(C.most_recent_sql_handle) AS Ctxt
		WHERE S.session_id>0
		AND ((R.command <>''BRKR TASK'' AND R.database_id>0) OR R.command IS NULL)
		AND S.is_user_process=1
		' + @OpenTransactionFilter + '
	)
	SELECT SnapshotDateUTC,
		UTCOffset,
		session_id,
		blocking_session_id,
		Txt,
		start_time_utc,
		command,
		database_id,
		database_name,
		host_name,
		program_name,
		wait_time,
		login_name,
		wait_resource,
		status,
		wait_type,
		session_status,
		transaction_isolation_level
	FROM R	
	WHERE blocking_session_total_wait > 0 /* Either session is blocked or causing blocking */
	AND total_blocking_wait > @MinWaitTimeMs /* Total wait time for blocked sessions exceeds threshold */'

EXEC sp_executesql @SQL,N'@MinWaitTimeMs INT',@MinWaitTimeMs
