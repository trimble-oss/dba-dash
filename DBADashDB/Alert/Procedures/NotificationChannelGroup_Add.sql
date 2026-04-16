CREATE PROC Alert.NotificationChannelGroup_Add(
	@GroupName NVARCHAR(100),
	@GroupID INT OUT
)
AS
INSERT INTO Alert.NotificationChannelGroup(GroupName)
VALUES(@GroupName)

SET @GroupID = SCOPE_IDENTITY()
