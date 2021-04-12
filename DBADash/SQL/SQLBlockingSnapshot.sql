DECLARE @MinWaitTimeMs INT 
SET @MinWaitTimeMs= 1000

DECLARE @DBIDTable NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @DBIDTable= CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'database_id','ColumnId') IS NULL THEN 'R.' ELSE 'S.' END 
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
			R.Status,
			R.wait_type
		FROM sys.dm_exec_sessions S
		INNER JOIN sys.dm_exec_connections C ON C.session_id= S.session_id
		LEFT JOIN sys.dm_exec_requests as R ON S.session_id = R.session_id
		OUTER APPLY sys.dm_exec_sql_text(R.sql_handle) AS Rtxt
		OUTER APPLY sys.dm_exec_sql_text(C.most_recent_sql_handle) AS Ctxt
		WHERE S.session_id>0
		AND (R.command<>''BRKR TASK'' OR R.command IS NULL)
		AND S.is_user_process=1
	)
	SELECT *
	FROM R
	WHERE ( R.blocking_session_id > 0  AND R.wait_time> @MinWaitTimeMs
	OR EXISTS(SELECT * FROM R R2 WHERE R2.blocking_session_id = R.session_id AND R2.wait_time>@MinWaitTimeMs)
	)'

EXEC sp_executesql @SQL,N'@MinWaitTimeMs INT',@MinWaitTimeMs