CREATE TABLE Alert.NotificationLog(
	AlertID BIGINT NOT NULL,
	NotificationChannelID INT NOT NULL,
	NotificationDate DATETIME2 NOT NULL CONSTRAINT DF_Alert_NotificationLog_NotificationDate DEFAULT(SYSUTCDATETIME()),
	ErrorMessage NVARCHAR(MAX) NULL,
	NotificationMessage NVARCHAR(MAX) NULL,
	CONSTRAINT PK_Alert_NotificationLog PRIMARY KEY(AlertID,NotificationChannelID,NotificationDate)
)