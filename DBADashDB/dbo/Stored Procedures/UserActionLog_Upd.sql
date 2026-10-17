CREATE PROC dbo.UserActionLog_Upd(
	@MessageGroupID UNIQUEIDENTIFIER,
	@Status NVARCHAR(MAX)
)
AS
/* The outcome of an action sent by messaging, once the service replies */
UPDATE dbo.UserActionLog
SET status = @Status
WHERE MessageGroupID = @MessageGroupID
