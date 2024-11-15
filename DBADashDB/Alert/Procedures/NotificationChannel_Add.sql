CREATE PROC Alert.NotificationChannel_Add(
	@ChannelName NVARCHAR(100),
	@DisableFrom DATETIME=NULL,
	@DisableTo DATETIME=NULL,
	@NotificationChannelTypeID INT,
	@ChannelDetails NVARCHAR(MAX),
	@NotificationChannelID INT OUT
)
AS
INSERT INTO Alert.NotificationChannel(	
	NotificationChannelTypeID,
	ChannelName,
	DisableFrom,
	DisableTo,
	ChannelDetails
	)
VALUES(@NotificationChannelTypeID,@ChannelName,@DisableFrom,@DisableTo,@ChannelDetails);

SET @NotificationChannelID = SCOPE_IDENTITY();
GO

