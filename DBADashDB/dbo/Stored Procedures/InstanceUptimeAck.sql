CREATE PROC dbo.InstanceUptimeAck(
	@InstanceID INT=NULL
)
AS
IF @InstanceID IS NULL OR @InstanceID =-1
BEGIN
	UPDATE dbo.Instances
	SET UptimeAckDate = GETUTCDATE() 
END
ELSE
BEGIN
	UPDATE dbo.Instances
	SET UptimeAckDate = GETUTCDATE() 
	WHERE InstanceID = @InstanceID
END