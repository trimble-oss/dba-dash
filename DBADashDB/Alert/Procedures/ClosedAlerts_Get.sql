CREATE PROC Alert.ClosedAlerts_Get(
	@InstanceIDs IDs READONLY,
	@InstanceID INT=NULL,
	@Top INT=1000
)
AS
DECLARE @AllInstances BIT
SELECT @AllInstances = CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END

SELECT  TOP(@Top)
		CA.AlertID,
		CA.InstanceID,
		I.InstanceDisplayName, 
		CA.Priority,
		CONCAT(CA.Priority,' - ',
			CASE WHEN CA.Priority =0 THEN 'Critical'
				WHEN CA.Priority>=1 AND CA.Priority<=10 THEN 'High'
				WHEN CA.Priority>=11 AND CA.Priority<=20 THEN 'Medium'
				WHEN CA.Priority>=21 AND CA.Priority<=30 THEN 'Low'
				WHEN CA.Priority>=31 AND CA.Priority<=254 THEN 'Informational'
				ELSE 'Success' END)
			+ CASE WHEN CA.IsResolved=1 THEN ' (Resolved)'
				WHEN CA.IsAcknowledged=1 THEN ' (Acknowledged)'
				ELSE '' END AS PriorityDescription,
		CA.Priority,
		CA.AlertType,
		CA.AlertKey,
		CA.FirstMessage,
		CA.LastMessage,
		CA.TriggerDate,
		AlertDuration.HumanDuration AS AlertDuration,
		CA.UpdatedDate,
		LastUpdate.HumanDuration AS TimeSinceLastUpdate,
		CA.FirstNotification,
		CA.LastNotification,
		LastNotification.HumanDuration AS TimeSinceLastNotification,
		CA.UpdateCount,
		CA.ResolvedCount,
		CA.NotificationCount,
		CA.FailedNotificationCount,
		CA.IsAcknowledged,
		CA.IsResolved,
		CA.ResolvedDate,
		CA.Notes,
		CA.ClosedDate,
		CA.RuleID,
		R.Notes AS RuleNotes
FROM Alert.ClosedAlerts CA
LEFT JOIN Alert.Rules R ON CA.RuleID = R.RuleID
JOIN dbo.Instances I ON I.InstanceID = CA.InstanceID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,CA.TriggerDate,CASE WHEN CA.IsResolved=1 THEN CA.ResolvedDate ELSE SYSUTCDATETIME() END)) AS AlertDuration
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,CA.UpdatedDate,SYSUTCDATETIME())) AS LastUpdate
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(SECOND,CA.LastNotification,SYSUTCDATETIME())) AS LastNotification
WHERE (CA.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (EXISTS(SELECT 1 
			FROM @InstanceIDs T 
			WHERE I.InstanceID = T.ID
			) OR @AllInstances=1)
ORDER BY CA.ClosedDate DESC
OPTION(RECOMPILE)