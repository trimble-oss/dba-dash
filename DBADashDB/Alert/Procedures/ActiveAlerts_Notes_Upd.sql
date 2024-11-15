CREATE PROC Alert.ActiveAlerts_Notes_Upd(
	@AlertID BIGINT,
	@Notes NVARCHAR(MAX)=NULL
)
AS
UPDATE Alert.ActiveAlerts 
	SET Notes = @Notes
WHERE AlertID = @AlertID
