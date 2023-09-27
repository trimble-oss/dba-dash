DECLARE @UTCOffset INT
SET @UTCOffset = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT)

SELECT	ja.job_id,
		DATEADD(mi,@UTCOffset,ja.run_requested_date) AS run_requested_date_utc,
		ja.run_requested_source, 
		DATEADD(mi,@UTCOffset,ja.queued_date) AS queued_date_utc,
		DATEADD(mi,@UTCOffset,ja.start_execution_date) AS start_execution_date_utc, 
		ja.last_executed_step_id, 
		DATEADD(mi,@UTCOffset,ja.last_executed_step_date) AS last_executed_step_date_utc,
		GETUTCDATE() as SnapshotDate
FROM msdb.dbo.sysjobactivity ja
WHERE ja.session_id = (SELECT MAX(session_id) FROM msdb.dbo.syssessions)
AND ja.start_execution_date IS NOT NULL
AND ja.stop_execution_date IS NULL

EXEC msdb.dbo.sp_help_job @execution_status=0