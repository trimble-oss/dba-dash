CREATE PROC dbo.FlushPlanLog_Get(
	@InstanceIDs IDs READONLY,
	@Days INT = 7
)
AS
/* Session detail is joined from the RunningQueries snapshot rather than duplicated in the log.  If the snapshot has
   been purged (RunningQueries retention) the detail columns return NULL, but the audit (who/when/outcome) remains. */
SELECT	I.InstanceID,
		I.InstanceGroupName,
		F.log_date,
		F.flushed_by,
		F.session_id,
		RQ.login_name,
		RQ.host_name,
		RQ.program_name,
		RQ.database_name,
		RQ.command,
		RQ.status AS session_status,
		F.plan_handle,
		F.status,
		F.SnapshotDate,
		RQ.start_time_utc AS request_start_time,
		RQ.[text],
		RQ.batch_text,
		F.MessageGroupID
FROM dbo.FlushPlanLog F
JOIN dbo.Instances I ON F.InstanceID = I.InstanceID
LEFT JOIN dbo.RunningQueriesInfo RQ
	ON RQ.InstanceID = F.InstanceID
	AND RQ.SnapshotDateUTC = F.SnapshotDate
	AND RQ.session_id = F.session_id
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
AND F.log_date >= DATEADD(d,-@Days,SYSUTCDATETIME())
ORDER BY F.log_date DESC
