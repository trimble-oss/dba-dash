CREATE PROC dbo.PlanForcingLog_Upd(
	@MessageGroupID UNIQUEIDENTIFIER,
	@Status VARCHAR(200)
)
AS
UPDATE dbo.PlanForcingLog
SET status = @Status
WHERE MessageGroupID = @MessageGroupID