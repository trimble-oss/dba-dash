CREATE TABLE Alert.CustomThreadKey(
	AlertID BIGINT NOT NULL,
	NotificationChannelID INT NOT NULL,	
	ThreadKey NVARCHAR(256) NOT NULL,
	CONSTRAINT PK_Alert_CustomThreadKey PRIMARY KEY(AlertID,NotificationChannelID)
)