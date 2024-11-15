CREATE TABLE Alert.NotificationChannelType(
	NotificationChannelTypeID TINYINT NOT NULL,
	NotificationChannelType VARCHAR(50) NOT NULL, -- e.g. webhook, google chat, slack, pager duty
	CONSTRAINT PK_NotificationChannelType PRIMARY KEY(NotificationChannelTypeID)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX NotificationChannelType_NotificationChannelType ON Alert.NotificationChannelType(NotificationChannelType)
