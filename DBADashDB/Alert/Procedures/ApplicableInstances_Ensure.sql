CREATE PROC Alert.ApplicableInstances_Ensure
AS
/*
	Called by each alert proc.  Alert.Alerts_Upd resolves the rule instances & blackouts once for the whole run and flags it in session context
	with the run's start time.  An alert proc run on its own resolves them first, so it never evaluates against data from a previous run.

	The flag is only trusted for 10 minutes: Alerts_Upd clears it when it finishes, but an error that aborts the batch part way through
	would leave it set for the session (pooled connections are reset when reused, which clears it).  An alert run longer than that just
	resolves again - extra work, not stale data.
*/
IF TRY_CAST(SESSION_CONTEXT(N'DBADash.AlertRun') AS DATETIME2) IS NULL
	OR TRY_CAST(SESSION_CONTEXT(N'DBADash.AlertRun') AS DATETIME2) < DATEADD(mi,-10,SYSUTCDATETIME())
BEGIN
	EXEC Alert.ApplicableInstances_Upd
END
