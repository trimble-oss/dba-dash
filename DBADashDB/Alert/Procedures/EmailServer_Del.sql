CREATE PROC Alert.EmailServer_Del(
	@EmailServerID INT
)
AS
/* A server that channels or daily reports still use is kept.  They need to be moved to another server first */
IF EXISTS(SELECT 1 FROM Alert.NotificationChannel WHERE EmailServerID = @EmailServerID)
BEGIN
	RAISERROR('This email server is used by one or more notification channels. Change those channels to another email server before deleting it.',16,1)
	RETURN
END
IF EXISTS(SELECT 1 FROM dbo.DailyReport WHERE EmailServerID = @EmailServerID)
BEGIN
	RAISERROR('This email server is used by one or more daily reports. Change those reports to another email server before deleting it.',16,1)
	RETURN
END

DELETE FROM Alert.EmailServer WHERE EmailServerID = @EmailServerID
