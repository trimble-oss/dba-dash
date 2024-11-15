CREATE TABLE Alert.NotificationChannel(
	NotificationChannelID INT IDENTITY(1,1),
	NotificationChannelTypeID TINYINT NOT NULL,
	ChannelName NVARCHAR(100) NOT NULL,
	DisableFrom DATETIME2 NULL,
	DisableTo DATETIME2 NULL,
	ChannelDetails NVARCHAR(MAX) MASKED WITH(FUNCTION = 'default()') NOT NULL,
	LastFailedNotification DATETIME2 NULL,
	LastSucceededNotification DATETIME2 NULL,
	FailedNotificationCount INT NOT NULL CONSTRAINT DF_Alert_NotificationChannel_FailedNotificationCount DEFAULT(0),
	SucceededNotificationCount INT NOT NULL CONSTRAINT DF_Alert_NotificationChannel_SucceededNotificationCount DEFAULT(0),
	LastFailure NVARCHAR(MAX) NULL,
	CONSTRAINT FK_NotificationChannel_NotificationChannelType FOREIGN KEY(NotificationChannelTypeID) REFERENCES Alert.NotificationChannelType(NotificationChannelTypeID),
	CONSTRAINT PK_NotificationChannel PRIMARY KEY(NotificationChannelID)
)
GO 
CREATE UNIQUE NONCLUSTERED INDEX IX_Alert_NotificationChannel ON Alert.NotificationChannel(ChannelName)