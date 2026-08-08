CREATE PROC dbo.KillSessionLog_Get(
	@InstanceIDs IDs READONLY,
	@Days INT = 7
)
AS
/* Session detail is joined from the RunningQueries snapshot rather than duplicated in the log.  If the snapshot has
   been purged (RunningQueries retention) the detail columns return NULL, but the audit (who/when/outcome) remains. */
SELECT	I.InstanceID,
		I.InstanceGroupName,
		K.log_date,
		K.killed_by,
		K.session_id,
		RQ.login_name,
		RQ.host_name,
		RQ.program_name,
		RQ.database_name,
		RQ.command,
		RQ.status AS session_status,
		RQ.blocking_session_id,
		RQ.BlockCountRecursive, /* Sessions this session was blocking (directly or indirectly) */
		K.status,
		K.SnapshotDate,
		RQ.start_time_utc AS request_start_time,
		RQ.[text],
		RQ.batch_text,
		K.MessageGroupID
FROM dbo.KillSessionLog K
JOIN dbo.Instances I ON K.InstanceID = I.InstanceID
LEFT JOIN dbo.RunningQueriesInfo RQ
	ON RQ.InstanceID = K.InstanceID
	AND RQ.SnapshotDateUTC = K.SnapshotDate
	AND RQ.session_id = K.session_id
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
AND K.log_date >= DATEADD(d,-@Days,SYSUTCDATETIME())
ORDER BY K.log_date DESC
