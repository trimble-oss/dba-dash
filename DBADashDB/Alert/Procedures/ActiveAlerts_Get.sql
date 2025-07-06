CREATE PROC Alert.ActiveAlerts_Get(
	@InstanceIDs IDs READONLY,
	@InstanceID INT=NULL
)
AS
/* 
	Returns all alerts that haven't been closed.
*/
DECLARE @AllInstances BIT
SELECT @AllInstances = CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END

SELECT  AA.AlertID,
		AA.InstanceID,
		I.InstanceDisplayName, 
		AA.Priority,
		CONCAT(AA.Priority,' - ',
			CASE WHEN AA.Priority =0 THEN 'Critical'
				WHEN AA.Priority>=1 AND AA.Priority<=10 THEN 'High'
				WHEN AA.Priority>=11 AND AA.Priority<=20 THEN 'Medium'
				WHEN AA.Priority>=21 AND AA.Priority<=30 THEN 'Low'
				WHEN AA.Priority>=31 AND AA.Priority<=254 THEN 'Informational'
				ELSE 'Success' END)
			+ CASE WHEN AA.IsResolved=1 THEN ' (Resolved)'
				WHEN AA.IsAcknowledged=1 THEN ' (Acknowledged)'
				WHEN AA.IsBlackout=1 THEN ' (Blackout)' ELSE '' END AS PriorityDescription,
		AA.Priority,
		AA.AlertType,
		AA.AlertKey,
		AA.FirstMessage,
		AA.LastMessage,
		AA.TriggerDate,
		AlertDuration.HumanDuration AS AlertDuration,
		AA.UpdatedDate,
		LastUpdate.HumanDuration AS TimeSinceLastUpdate,
		AA.FirstNotification,
		AA.LastNotification,
		LastNotification.HumanDuration AS TimeSinceLastNotification,
		AA.UpdateCount,
		AA.ResolvedCount,
		AA.NotificationCount,
		AA.FailedNotificationCount,
		AA.IsAcknowledged,
		AA.IsResolved,
		AA.ResolvedDate,
		AA.IsBlackout,
		CASE WHEN AA.IsResolved=1 THEN 4
			WHEN AA.IsAcknowledged=1 THEN 5
			WHEN AA.IsBlackout=1 THEN 3
			WHEN AA.Priority =0 THEN 1
			WHEN AA.Priority>=1 AND AA.Priority<=10 THEN 1
			WHEN AA.Priority>=11 AND AA.Priority<=20 THEN 2
			WHEN AA.Priority>=21 AND AA.Priority<=30 THEN 6
			WHEN AA.Priority>=31 AND AA.Priority<=254 THEN 7
			ELSE 4 END AS AlertStatus,
		AA.Notes,
		AA.RuleID,
		R.Notes AS RuleNotes,
		ROW_NUMBER() OVER(ORDER BY AA.IsResolved,
								AA.IsBlackout,
								AA.IsAcknowledged,
								AA.Priority,
								I.InstanceDisplayName,
								AA.AlertKey) AS DefaultSortOrder
FROM Alert.ActiveAlerts AA
LEFT JOIN Alert.Rules R ON AA.RuleID = R.RuleID
JOIN dbo.Instances I ON I.InstanceID = AA.InstanceID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,AA.TriggerDate,CASE WHEN AA.IsResolved=1 THEN AA.ResolvedDate ELSE SYSUTCDATETIME() END)) AS AlertDuration
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,AA.UpdatedDate,SYSUTCDATETIME())) AS LastUpdate
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,AA.LastNotification,SYSUTCDATETIME())) AS LastNotification
WHERE (AA.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (EXISTS(SELECT 1 
			FROM @InstanceIDs T 
			WHERE I.InstanceID = T.ID
			) OR @AllInstances=1)
ORDER BY DefaultSortOrder