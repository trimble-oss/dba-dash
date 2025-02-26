CREATE PROC Alert.NotificationChannel_Add(
	@ChannelName NVARCHAR(100),
	@DisableFrom DATETIME=NULL,
	@DisableTo DATETIME=NULL,
	@NotificationChannelTypeID INT,
	@ChannelDetails NVARCHAR(MAX),
	@AcknowledgedNotification BIT=0,
	@NotificationChannelID INT OUT
)
AS
INSERT INTO Alert.NotificationChannel(	
	NotificationChannelTypeID,
	ChannelName,
	DisableFrom,
	DisableTo,
	ChannelDetails,
	AcknowledgedNotification
	)
VALUES(@NotificationChannelTypeID,@ChannelName,@DisableFrom,@DisableTo,@ChannelDetails,@AcknowledgedNotification);

SET @NotificationChannelID = SCOPE_IDENTITY();
GO

