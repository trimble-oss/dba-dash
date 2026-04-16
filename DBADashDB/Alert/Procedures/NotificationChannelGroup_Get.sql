CREATE PROC Alert.NotificationChannelGroup_Get
AS
SELECT GroupID,
	   GroupName,
	   (SELECT COUNT(*) FROM Alert.NotificationChannel NC WHERE NC.GroupID = G.GroupID) AS ChannelCount,
	   (SELECT COUNT(*) FROM Alert.Rules R WHERE R.GroupID = G.GroupID) AS RuleCount
FROM Alert.NotificationChannelGroup G
ORDER BY G.GroupName
