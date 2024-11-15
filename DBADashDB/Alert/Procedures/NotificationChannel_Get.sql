CREATE PROC Alert.NotificationChannel_Get(
	@NotificationChannelID INT=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT	NC.NotificationChannelID,
		NC.NotificationChannelTypeID,
		NC.ChannelName,
		NCT.NotificationChannelType,
		NC.DisableFrom,
		NC.DisableTo,
		CASE WHEN NC.DisableFrom < GETUTCDATE() AND (NC.DisableTo>GETUTCDATE() OR NC.DisableTo IS NULL) THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsActive,
		NC.ChannelDetails,
		S.ScheduleCount,
		CASE WHEN EXISTS(
			SELECT 1
			FROM Alert.NotificationChannelSchedule NCS
			WHERE NCS.NotificationChannelID = NC.NotificationChannelID
			AND Monday=1
			AND Tuesday=1
			AND Wednesday=1
			AND Thursday=1
			AND Friday=1
			AND Saturday=1
			AND Sunday=1
			AND TimeFrom IS NULL
			AND TimeTo IS NULL
			) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END Has247Schedule,
		STUFF((SELECT DISTINCT '', '' + CASE WHEN NCS.ApplyToTagID = -1 THEN ''{All}'' ELSE T.TagName + '':'' + T.TagValue END
			FROM Alert.NotificationChannelSchedule NCS
			LEFT JOIN dbo.Tags T ON T.TagID = NCS.ApplyToTagID
			WHERE NCS.NotificationChannelID = NC.NotificationChannelID
			FOR XML PATH(''''),TYPE).value(''.'',''NVARCHAR(MAX)''),1,2,'''') AS Tags,
		LastFailedNotification,
		LastSucceededNotification,
		FailedNotificationCount,
		SucceededNotificationCount,
		LastFailure,
		CASE WHEN LastSucceededNotification IS NULL AND LastFailedNotification IS NULL THEN 3 
			WHEN LastFailedNotification > ISNULL(LastSucceededNotification,''19000101'') OR LastSucceededNotification IS NULL THEN 1 
			WHEN LastFailedNotification > DATEADD(d,-3,SYSUTCDATETIME()) THEN 2  ELSE 4 END AS LastFailureStatus
FROM Alert.NotificationChannel NC 
JOIN Alert.NotificationChannelType NCT ON NC.NotificationChannelTypeID = NCT.NotificationChannelTypeID
OUTER APPLY(SELECT COUNT(*) AS ScheduleCount
			FROM Alert.NotificationChannelSchedule NCS
			WHERE NCS.NotificationChannelID = NC.NotificationChannelID
			) S
' + CASE WHEN @NotificationChannelID IS NULL THEN '' ELSE 'WHERE NC.NotificationChannelID = @NotificationChannelID' END 

EXEC sp_executesql @SQL,N'@NotificationChannelID INT',@NotificationChannelID

