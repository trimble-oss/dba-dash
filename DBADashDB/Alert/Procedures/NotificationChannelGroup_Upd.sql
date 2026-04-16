CREATE PROC Alert.NotificationChannelGroup_Upd(
	@GroupID INT,
	@GroupName NVARCHAR(100)
)
AS
IF @GroupID = 0
BEGIN
	RAISERROR('The default notification channel group cannot be modified.',16,1)
	RETURN
END

UPDATE Alert.NotificationChannelGroup
SET GroupName = @GroupName
WHERE GroupID = @GroupID
