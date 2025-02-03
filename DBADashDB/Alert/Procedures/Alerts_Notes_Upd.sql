CREATE PROC Alert.Alerts_Notes_Upd(
	@AlertID BIGINT,
	@Notes NVARCHAR(MAX)=NULL
)
AS
UPDATE Alert.ActiveAlerts 
	SET Notes = @Notes
WHERE AlertID = @AlertID

IF @@ROWCOUNT=0
BEGIN
	UPDATE Alert.ClosedAlerts 
		SET Notes = @Notes
	WHERE AlertID = @AlertID
END