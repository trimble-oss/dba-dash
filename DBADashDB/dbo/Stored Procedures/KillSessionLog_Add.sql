CREATE PROC dbo.KillSessionLog_Add(
	@MessageGroupID UNIQUEIDENTIFIER,
	@InstanceID INT,
	@session_id INT,
	@SnapshotDate DATETIME2(7),
	@killed_by NVARCHAR(256)
)
AS
/* Session detail (login, host, program, text, batch_text etc.) is not duplicated here - it is joined from
   dbo.RunningQueries via InstanceID + SnapshotDate + session_id when the log is read (KillSessionLog_Get). */
INSERT INTO dbo.KillSessionLog(MessageGroupID,InstanceID,session_id,SnapshotDate,killed_by)
VALUES(@MessageGroupID,@InstanceID,@session_id,@SnapshotDate,@killed_by)
