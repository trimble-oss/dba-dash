CREATE PROC Alert.Notifications_Get
AS
/* 
	Gets a list of alert notifications that need to be sent out
*/
SET LANGUAGE British
DECLARE @AlertMaxNotificationCount INT=6

SELECT @AlertMaxNotificationCount  = ISNULL(TRY_CAST(SettingValue AS INT),@AlertMaxNotificationCount)
FROM dbo.Settings
WHERE SettingName = 'AlertMaxNotificationCount'

SELECT AA.AlertID,
		I.ConnectionID,
		I.InstanceDisplayName,
		AA.AlertKey,
		AA.LastMessage AS AlertDetails,
		AA.ResolvedDate,
		AA.IsResolved,
		AA.Priority,
		NC.NotificationChannelID,
		AA.NotificationCount,
		@AlertMaxNotificationCount AS AlertMaxNotificationCount,
		AA.TriggerDate,
		CASE WHEN AA.Escalated > AA.LastNotification THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsEscalated,
		CTK.ThreadKey AS CustomThreadKey,
		AA.AlertType,
		AA.IsAcknowledged
FROM Alert.ActiveAlerts AA
JOIN dbo.Instances I ON AA.InstanceID = I.InstanceID
CROSS JOIN Alert.NotificationChannel NC
LEFT JOIN Alert.CustomThreadKey CTK ON AA.AlertID = CTK.AlertID AND NC.NotificationChannelID = CTK.NotificationChannelID
WHERE (AA.UpdatedDate > AA.LastNotification OR AA.LastNotification IS NULL)
AND (AA.IsAcknowledged = 0 OR (AA.AcknowledgedDate > AA.LastNotification AND NC.AcknowledgedNotification=1))
AND AA.NotificationCount < @AlertMaxNotificationCount
AND (NC.DisableFrom IS NULL /* Not disabled */
	OR NC.DisableFrom>SYSUTCDATETIME() /* Disabled period hasn't started */
	OR NC.DisableTo < SYSUTCDATETIME() /* Disabled period has ended */
	)
AND EXISTS(
			SELECT 1
			FROM Alert.NotificationChannelSchedule NCS 
			OUTER APPLY(SELECT SYSDATETIMEOFFSET() AT TIME ZONE TimeZone AS CurrentTime) AS TZ
			OUTER APPLY(SELECT CAST(TZ.CurrentTime AS TIME) AS CurrentTimeAsTime) AS T
			WHERE NC.NotificationChannelID = NCS.NotificationChannelID
			AND CHOOSE(DATEPART(dw,TZ.CurrentTime),NCS.Monday,NCS.Tuesday,NCS.Wednesday,NCS.Thursday,NCS.Friday,NCS.Saturday,NCS.Sunday) = CAST(1 AS BIT)
			AND (
					(T.CurrentTimeAsTime >= NCS.TimeFrom OR NCS.TimeFrom IS NULL) 
					AND (T.CurrentTimeAsTime <= NCS.TimeTo OR NCS.TimeTo IS NULL)
					OR (
						/* Configured TimeTo is less than TimeFrom, crossing midnight boundary. e.g. 22:00 to 03:00. */
						NCS.TimeTo<NCS.TimeFrom
						/* Either >= TimeFrom or <= TimeTo is OK when midnight boundary is crossed*/
						AND (T.CurrentTimeAsTime >= NCS.TimeFrom OR T.CurrentTimeAsTime <= NCS.TimeTo )
					) 
				)
			AND NCS.AlertNotificationLevel>=AA.Priority
			AND (	/* Only notifiy if re-trigger threshold has passed,
						Or we haven't notified,
						Or it's the first time the issue has been resolved
						Or it's the first time the issue has been resolved and it's been re-triggered
						Or issue has escallated since last notification
					*/
					DATEDIFF(mi,AA.LastNotification,TZ.CurrentTime) >= NCS.RetriggerThresholdMins 
					OR AA.LastNotification IS NULL 
					OR (AA.IsResolved=1 AND AA.ResolvedCount=1)
					OR (AA.IsResolved=0 AND AA.ResolvedDate > AA.LastNotification AND AA.ResolvedCount=1)
					OR (AA.Escalated > AA.LastNotification)
					OR (AA.AcknowledgedDate > AA.LastNotification)
					)
			AND EXISTS(
					SELECT 1
					WHERE NCS.ApplyToTagID = -1
					UNION ALL
					SELECT 1 
					FROM dbo.InstanceIDsTags IT
					WHERE IT.InstanceID = I.InstanceID
					AND IT.TagID = NCS.ApplyToTagID
					UNION ALL
					SELECT 1 
					FROM dbo.InstanceTags IT
					WHERE IT.Instance = I.Instance
					AND IT.TagID = NCS.ApplyToTagID
			)
		)
AND AA.IsBlackout=0