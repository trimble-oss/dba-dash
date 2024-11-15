CREATE PROC Alert.NotificationChannelSchedule_Upd(
	@NotificationChannelID INT,
	@Schedules Alert.NotificationChannelSchedule READONLY
)
AS 

DELETE NCS 
FROM Alert.NotificationChannelSchedule NCS
WHERE NCS.NotificationChannelID = @NotificationChannelID
AND EXISTS(SELECT	NCS.AlertNotificationLevel,
					NCS.RetriggerThresholdMins,
					NCS.ApplyToTagID,
					NCS.TimeFrom,
					NCS.TimeTo,
					NCS.TimeZone,
					NCS.Monday,
					NCS.Tuesday,
					NCS.Wednesday,
					NCS.Thursday,
					NCS.Friday,
					NCS.Saturday,
					NCS.Sunday
			EXCEPT 
			SELECT	S.AlertNotificationLevel,
					S.RetriggerThresholdMins,
					S.ApplyToTagID,
					S.TimeFrom,
					S.TimeTo,
					S.TimeZone,
					S.Monday,
					S.Tuesday,
					S.Wednesday,
					S.Thursday,
					S.Friday,
					S.Saturday,
					S.Sunday
			FROM @Schedules S
			)

INSERT INTO Alert.NotificationChannelSchedule(
		NotificationChannelID,
		AlertNotificationLevel,
		RetriggerThresholdMins,
		ApplyToTagID,
		TimeFrom,
		TimeTo,
		TimeZone,
		Monday,
		Tuesday,
		Wednesday,
		Thursday,
		Friday,
		Saturday,
		Sunday
)
SELECT	@NotificationChannelID,
		S.AlertNotificationLevel,
		S.RetriggerThresholdMins,
		S.ApplyToTagID,
		S.TimeFrom,
		S.TimeTo,
		S.TimeZone,
		S.Monday,
		S.Tuesday,
		S.Wednesday,
		S.Thursday,
		S.Friday,
		S.Saturday,
		S.Sunday
FROM @Schedules S
EXCEPT 
SELECT	@NotificationChannelID,
		NCS.AlertNotificationLevel,
		NCS.RetriggerThresholdMins,
		NCS.ApplyToTagID,
		NCS.TimeFrom,
		NCS.TimeTo,
		NCS.TimeZone,
		NCS.Monday,
		NCS.Tuesday,
		NCS.Wednesday,
		NCS.Thursday,
		NCS.Friday,
		NCS.Saturday,
		NCS.Sunday
FROM Alert.NotificationChannelSchedule NCS
WHERE NotificationChannelID = @NotificationChannelID