CREATE PROC dbo.FlushPlanLog_Add(
	@MessageGroupID UNIQUEIDENTIFIER,
	@InstanceID INT,
	@session_id INT,
	@SnapshotDate DATETIME2(7),
	@plan_handle VARCHAR(130),
	@flushed_by NVARCHAR(256)
)
AS
/* Session detail (login, host, program, text, batch_text etc.) is not duplicated here - it is joined from
   dbo.RunningQueries via InstanceID + SnapshotDate + session_id when the log is read (FlushPlanLog_Get). */
INSERT INTO dbo.FlushPlanLog(MessageGroupID,InstanceID,session_id,SnapshotDate,plan_handle,flushed_by)
VALUES(@MessageGroupID,@InstanceID,@session_id,@SnapshotDate,@plan_handle,@flushed_by)
