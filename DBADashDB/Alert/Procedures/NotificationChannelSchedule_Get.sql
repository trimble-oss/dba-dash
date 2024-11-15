CREATE PROC Alert.NotificationChannelSchedule_Get(
	@NotificationChannelID INT
)
AS
SELECT  NCS.NotificationChannelScheduleID,
        NCS.NotificationChannelID,
        NCS.AlertNotificationLevel,
        NCS.RetriggerThresholdMins,
        NCS.ApplyToTagID,
        T.TagName,
        T.TagValue,
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
LEFT JOIN dbo.Tags T ON NCS.ApplyToTagID = T.TagID
WHERE NCS.NotificationChannelID = @NotificationChannelID


