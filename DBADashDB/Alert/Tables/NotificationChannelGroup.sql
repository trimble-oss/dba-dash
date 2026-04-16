CREATE TABLE Alert.NotificationChannelGroup(
	GroupID INT IDENTITY(1,1) NOT NULL,	-- 0 is reserved as the default group sentinel
	GroupName NVARCHAR(100) NOT NULL,
	CONSTRAINT PK_NotificationChannelGroup PRIMARY KEY(GroupID),
	CONSTRAINT UQ_NotificationChannelGroup_GroupName UNIQUE(GroupName)
)
