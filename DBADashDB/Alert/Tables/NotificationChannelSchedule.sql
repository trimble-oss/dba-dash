-- Allow alerts to be sent to channel during specific hours of the day & days of the week
-- Alert notification level can be different over weekend & out of hours for example
CREATE TABLE Alert.NotificationChannelSchedule(
	NotificationChannelScheduleID INT IDENTITY(1,1),
	NotificationChannelID INT NOT NULL,
	AlertNotificationLevel TINYINT NOT NULL CONSTRAINT DF_NotificationChannelSchedule_AlertNotificationLevel DEFAULT(1), 
	RetriggerThresholdMins INT NOT NULL CONSTRAINT DF_NotificationChannelSchedule_RetriggerThresholdMins DEFAULT(30), -- retrigger alert if it has not been resolved after this time
	ApplyToTagID INT NOT NULL,
	TimeFrom TIME NULL,
	TimeTo TIME NULL,
	Monday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Monday DEFAULT(CAST(1 AS BIT)),
	Tuesday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Tuesday DEFAULT(CAST(1 AS BIT)),
	Wednesday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Wednesday DEFAULT(CAST(1 AS BIT)),
	Thursday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelScheduleThursday DEFAULT(CAST(1 AS BIT)),
	Friday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Friday DEFAULT(CAST(1 AS BIT)),
	Saturday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Saturday DEFAULT(CAST(1 AS BIT)),
	Sunday BIT NOT NULL CONSTRAINT DF_Alert_NotificationChannelSchedule_Sunday DEFAULT(CAST(1 AS BIT)),
	TimeZone NVARCHAR(128) NOT NULL CONSTRAINT DF_NotificationChannelSchedule_TimeZone DEFAULT('UTC'),
	CONSTRAINT CK_Alert_NotificationChannelSchedule_TimeTo CHECK(TimeTo>TimeFrom),
	CONSTRAINT FK_NotificationChannelSchedule_NotificationChannel FOREIGN KEY(NotificationChannelID) REFERENCES Alert.NotificationChannel(NotificationChannelID)
)
