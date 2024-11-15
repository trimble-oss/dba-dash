CREATE PROC Alert.CustomThreadKey_Add(
	@NotificationChannelID INT,
	@AlertID BIGINT,
	@ThreadKey NVARCHAR(256)
)
AS
DELETE Alert.CustomThreadKey
WHERE NotificationChannelID = @NotificationChannelID 
AND AlertID = @AlertID

INSERT INTO Alert.CustomThreadKey(
    AlertID,
	NotificationChannelID,
    ThreadKey
)
VALUES(   
	@AlertID,  
	@NotificationChannelID, 
    @ThreadKey
    )