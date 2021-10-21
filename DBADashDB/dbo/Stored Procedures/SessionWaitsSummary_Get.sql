CREATE PROC dbo.SessionWaitsSummary_Get(
	@InstanceID INT,
	@SnapshotDateUTC DATETIME2(7)
)
AS
SELECT WT.WaitType, 
		SUM(SW.waiting_tasks_count) AS waiting_tasks_count,
        SUM(SW.wait_time_ms) AS wait_time_ms,
        MAX(SW.max_wait_time_ms) AS max_wait_time_ms,
        SUM(SW.signal_wait_time_ms) AS signal_wait_time_ms,
		SUM(SW.signal_wait_time_ms*1.0)/ NULLIF(SUM(SW.wait_time_ms),0) AS signal_wait_pct,
        SUM(wait_time_ms)*1.0/NULLIF(SUM(SUM(SW.wait_time_ms)) OVER(),0) AS wait_pct
FROM dbo.SessionWaits SW
JOIN dbo.WaitType WT ON SW.WaitTypeID = WT.WaitTypeID
WHERE SW.SnapshotDateUTC=@SnapshotDateUTC
AND SW.InstanceID = @InstanceID
AND EXISTS(SELECT 1 
		FROM dbo.RunningQueries Q 
		WHERE Q.SnapshotDateUTC = SW.SnapshotDateUTC 
		AND Q.session_id = SW.session_id
		AND Q.login_time_utc = SW.login_time_utc
		AND Q.InstanceID = SW.InstanceID
		)
GROUP BY WT.WaitType
ORDER BY wait_time_ms DESC