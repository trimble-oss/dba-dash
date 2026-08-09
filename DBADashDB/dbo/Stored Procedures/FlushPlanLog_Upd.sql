CREATE PROC dbo.FlushPlanLog_Upd(
	@MessageGroupID UNIQUEIDENTIFIER,
	@Status VARCHAR(200)
)
AS
UPDATE dbo.FlushPlanLog
SET status = @Status
WHERE MessageGroupID = @MessageGroupID
