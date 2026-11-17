CREATE PROC Alert.EmailServer_Get(
	@EmailServerID INT=NULL
)
AS
SELECT	ES.EmailServerID,
		ES.EmailServerName,
		ES.ServerDetails,
		(SELECT COUNT(*) FROM Alert.NotificationChannel NC WHERE NC.EmailServerID = ES.EmailServerID) AS ChannelCount,
		(SELECT COUNT(*) FROM dbo.DailyReport DR WHERE DR.EmailServerID = ES.EmailServerID) AS ReportCount
FROM Alert.EmailServer ES
WHERE @EmailServerID IS NULL OR ES.EmailServerID = @EmailServerID
ORDER BY ES.EmailServerName
