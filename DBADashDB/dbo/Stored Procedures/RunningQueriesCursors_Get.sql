CREATE PROC dbo.RunningQueriesCursors_Get(
		@InstanceID INT,
		@SnapshotDateUTC DATETIME2,
		@SessionID SMALLINT
)
AS
SELECT 		c.InstanceID,
			c.SnapshotDateUTC,
			c.session_id,
			c.name,
			QT.text,
			SUBSTRING(QT.text,ISNULL((NULLIF(c.statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(NULLIF(c.statement_end_offset,-1),0) - NULLIF(c.statement_start_offset,-1))/2+1,2147483647)) AS statement,
			c.properties,
			c.sql_handle,
			c.statement_start_offset,
			c.statement_end_offset,
			c.plan_generation_num,
			c.creation_time_utc,
			c.is_open,
			c.is_async_population,
			c.is_close_on_commit,
			c.fetch_status,
			CONCAT(c.fetch_status,' - ',CASE WHEN c.fetch_status = 0 THEN 'The FETCH statement was successful.'
				 WHEN c.fetch_status = -1 THEN 'The FETCH statement failed or the row was beyond the result set.'
				 WHEN c.fetch_status = -2 THEN 'The row fetched is missing.'
				 WHEN c.fetch_status = -9 THEN 'The cursor is not performing a fetch operation.'
				 ELSE 'Unknown' END) AS fetch_status_desc,
			c.fetch_buffer_size,
			c.fetch_buffer_start,
			c.ansi_position,
			c.worker_time/1000.0 as worker_time_ms,
			HDWorkerTime.HumanDuration AS worker_time,
			c.reads,
			c.writes,
			c.dormant_duration AS dormant_duration_ms,
			HDDormant.HumanDuration AS dormant_duration
FROM dbo.RunningQueriesCursors c
LEFT JOIN dbo.QueryText QT ON c.sql_handle = QT.sql_handle
OUTER APPLY dbo.MillisecondsToHumanDuration(dormant_duration) AS HDDormant
OUTER APPLY dbo.MillisecondsToHumanDuration(worker_time/1000) AS HDWorkerTime
WHERE c.SnapshotDateUTC = @SnapshotDateUTC
AND c.InstanceID = @InstanceID
AND (c.session_id = @SessionID OR @SessionID IS NULL)
