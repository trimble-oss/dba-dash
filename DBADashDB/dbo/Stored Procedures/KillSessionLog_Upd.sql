CREATE PROC dbo.KillSessionLog_Upd(
	@MessageGroupID UNIQUEIDENTIFIER,
	@Status VARCHAR(200)
)
AS
UPDATE dbo.KillSessionLog
SET status = @Status
WHERE MessageGroupID = @MessageGroupID
