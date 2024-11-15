CREATE PROC Alert.NotificationChannel_Upd(
	@ChannelName NVARCHAR(100),
	@DisableFrom DATETIME=NULL,
	@DisableTo DATETIME=NULL,
	@NotificationChannelTypeID INT,
	@ChannelDetails NVARCHAR(MAX),
	@NotificationChannelID INT
)
AS
UPDATE Alert.NotificationChannel
SET NotificationChannelTypeID = @NotificationChannelTypeID,
	ChannelName = @ChannelName,
	DisableFrom = @DisableFrom,
	DisableTo = @DisableTo,
	ChannelDetails = @ChannelDetails
WHERE NotificationChannelID = @NotificationChannelID
