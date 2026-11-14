CREATE PROC Alert.NotificationChannel_Del(
	@NotificationChannelID INT
)
AS
SET XACT_ABORT ON

BEGIN TRAN
DELETE Alert.NotificationChannelSchedule 
WHERE NotificationChannelID = @NotificationChannelID

/* Daily reports that sent to this channel stop using it */
DELETE dbo.DailyReportChannel
WHERE NotificationChannelID = @NotificationChannelID

DELETE Alert.NotificationChannel
WHERE NotificationChannelID = @NotificationChannelID
COMMIT