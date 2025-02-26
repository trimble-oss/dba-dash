CREATE PROC Alert.NotificationChannel_Upd(
	@ChannelName NVARCHAR(100),
	@DisableFrom DATETIME=NULL,
	@DisableTo DATETIME=NULL,
	@NotificationChannelTypeID INT,
	@ChannelDetails NVARCHAR(MAX),
	@NotificationChannelID INT,
	@AcknowledgedNotification BIT=0
)
AS
UPDATE Alert.NotificationChannel
SET NotificationChannelTypeID = @NotificationChannelTypeID,
	ChannelName = @ChannelName,
	DisableFrom = @DisableFrom,
	DisableTo = @DisableTo,
	ChannelDetails = @ChannelDetails,
	AcknowledgedNotification = @AcknowledgedNotification
WHERE NotificationChannelID = @NotificationChannelID
