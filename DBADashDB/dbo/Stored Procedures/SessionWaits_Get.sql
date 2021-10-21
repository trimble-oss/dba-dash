CREATE PROC dbo.SessionWaits_Get(
	@InstanceID INT,
	@SnapshotDateUTC DATETIME2(7),
	@LoginTimeUTC DATETIME=NULL,
	@SessionID SMALLINT=NULL
)
AS
SELECT SW.session_id,
		WT.WaitType, 
		SW.waiting_tasks_count,
        SW.wait_time_ms,
        SW.max_wait_time_ms,
        SW.signal_wait_time_ms,
		SW.signal_wait_time_ms*1.0/ NULLIF(SW.wait_time_ms,0) AS signal_wait_pct,
        wait_time_ms*1.0/NULLIF(SUM(SW.wait_time_ms)OVER(),0) AS wait_pct
FROM dbo.SessionWaits SW
JOIN dbo.WaitType WT ON SW.WaitTypeID = WT.WaitTypeID
WHERE (SW.session_id=@SessionID OR @SessionID IS NULL)
AND SW.SnapshotDateUTC=@SnapshotDateUTC
AND (SW.login_time_utc = @LoginTimeUTC OR @LoginTimeUTC IS NULL)
AND SW.InstanceID = @InstanceID
AND EXISTS(SELECT 1 
		FROM dbo.RunningQueries Q 
		WHERE Q.SnapshotDateUTC = SW.SnapshotDateUTC 
		AND Q.session_id = SW.session_id
		AND Q.login_time_utc = SW.login_time_utc
		AND Q.InstanceID = SW.InstanceID
		)
ORDER BY SW.wait_time_ms DESC