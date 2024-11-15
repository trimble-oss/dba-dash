CREATE TYPE Alert.NotificationChannelSchedule AS TABLE(
	AlertNotificationLevel TINYINT NOT NULL, 
	RetriggerThresholdMins INT NOT NULL, 
	ApplyToTagID INT NOT NULL,
	TimeFrom TIME NULL,
	TimeTo TIME NULL,
	Monday BIT NOT NULL,
	Tuesday BIT NOT NULL,
	Wednesday BIT NOT NULL,
	Thursday BIT NOT NULL,
	Friday BIT NOT NULL, 
	Saturday BIT NOT NULL,
	Sunday BIT NOT NULL,
	TimeZone NVARCHAR(128) NOT NULL
)