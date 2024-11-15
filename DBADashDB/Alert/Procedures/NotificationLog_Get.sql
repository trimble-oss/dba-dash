CREATE PROC Alert.NotificationLog_Get(
	@AlertID BIGINT,
	@FailedOnly BIT=0
)
AS
SELECT	NL.NotificationDate,
		NCT.NotificationChannelType,
		NC.ChannelName,
		CASE WHEN NL.ErrorMessage IS NULL THEN 'OK' ELSE 'Error' END AS Status,
		NL.ErrorMessage,
		NL.NotificationMessage
FROM Alert.NotificationLog NL
JOIN Alert.NotificationChannel NC ON NL.NotificationChannelID = NC.NotificationChannelID
JOIN Alert.NotificationChannelType NCT ON NC.NotificationChannelTypeID = NCT.NotificationChannelTypeID
WHERE NL.AlertID = @AlertID
AND (NL.ErrorMessage IS NOT NULL OR @FailedOnly=0)
ORDER BY NL.NotificationDate DESC