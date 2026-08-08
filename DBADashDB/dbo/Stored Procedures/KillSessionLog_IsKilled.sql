CREATE PROC dbo.KillSessionLog_IsKilled(
	@InstanceID INT,
	@session_id INT,
	@SnapshotDate DATETIME2(7)
)
AS
/* Returns 1 if this session (in this snapshot) has already been successfully killed from DBA Dash, else nothing.
   Used by the session detail viewer to avoid offering to kill a session that has already been killed. */
SELECT TOP(1) 1 AS IsKilled
FROM dbo.KillSessionLog
WHERE InstanceID = @InstanceID
AND session_id = @session_id
AND SnapshotDate = @SnapshotDate
AND status = 'KILLED'
