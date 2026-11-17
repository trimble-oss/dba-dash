CREATE PROC Alert.NotificationChannel_Add(
	@ChannelName NVARCHAR(100),
	@DisableFrom DATETIME=NULL,
	@DisableTo DATETIME=NULL,
	@NotificationChannelTypeID INT,
	@ChannelDetails NVARCHAR(MAX),
	@AcknowledgedNotification BIT=0,
	@GroupID INT=0,
	@EmailServerID INT=NULL,
	@NotificationChannelID INT OUT
)
AS
INSERT INTO Alert.NotificationChannel(	
	NotificationChannelTypeID,
	ChannelName,
	DisableFrom,
	DisableTo,
	ChannelDetails,
	AcknowledgedNotification,
	GroupID,
	EmailServerID
	)
VALUES(@NotificationChannelTypeID,@ChannelName,@DisableFrom,@DisableTo,@ChannelDetails,@AcknowledgedNotification,@GroupID,@EmailServerID);

SET @NotificationChannelID = SCOPE_IDENTITY();
GO

