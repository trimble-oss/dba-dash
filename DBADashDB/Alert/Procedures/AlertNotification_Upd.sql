CREATE PROC Alert.AlertNotification_Upd(
	@AlertID INT,
	@NotificationChannelID INT,
	@NotificationMessage NVARCHAR(MAX),
	@ErrorMessage NVARCHAR(MAX)=NULL
)
AS
SET XACT_ABORT ON
/*
	Called when an alert notification is sent to a notification channel.
*/
DECLARE @CurrentDate DATETIME2 = SYSUTCDATETIME()

BEGIN TRAN

INSERT INTO Alert.NotificationLog(AlertID,NotificationChannelID,ErrorMessage,NotificationDate,NotificationMessage)
VALUES(@AlertID,@NotificationChannelID,@ErrorMessage,@CurrentDate,@NotificationMessage)

DECLARE @Count INT
DECLARE @FailedCount INT
/* Get the notification count for this channel */
SELECT @Count = COUNT(*),
	   @FailedCount =SUM(CASE WHEN ErrorMessage IS NULL THEN 0 ELSE 1 END)
FROM Alert.NotificationLog
WHERE AlertID = @AlertID
AND NotificationChannelID = @NotificationChannelID


IF @ErrorMessage IS NULL
BEGIN
	UPDATE Alert.NotificationChannel
		SET LastSucceededNotification = @CurrentDate,
		SucceededNotificationCount+=1
	WHERE NotificationChannelID = @NotificationChannelID
END
ELSE
BEGIN
	UPDATE Alert.NotificationChannel
		SET LastFailedNotification = @CurrentDate,
		FailedNotificationCount+=1,
		LastFailure = @ErrorMessage
	WHERE NotificationChannelID = @NotificationChannelID
END

UPDATE Alert.ActiveAlerts 
	SET LastNotification = SYSUTCDATETIME(),
	FirstNotification = ISNULL(FirstNotification,SYSUTCDATETIME()),
	NotificationCount = (SELECT MAX(cnt) FROM (VALUES(NotificationCount),(@Count)) T(cnt)), /* Notification count represents the max for each channel */
	FailedNotificationCount = (SELECT MAX(cnt) FROM (VALUES(FailedNotificationCount),(@FailedCount)) T(cnt)) /* Failed Notification count represents the max failures for each channel */
WHERE AlertID = @AlertID;

COMMIT