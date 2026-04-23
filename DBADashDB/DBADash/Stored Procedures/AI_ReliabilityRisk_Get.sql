CREATE PROC DBADash.AI_ReliabilityRisk_Get(
	@MaxRows INT = 400,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 168
)
AS
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	aa.AlertType,
	aa.AlertKey,
	aa.Priority,
	aa.IsResolved,
	aa.IsAcknowledged,
	aa.TriggerDate,
	aa.UpdatedDate,
	aa.LastMessage
FROM Alert.ActiveAlerts aa
INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
WHERE i.IsActive = 1
  AND aa.UpdatedDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (
		aa.AlertKey LIKE '%RESTART%'
	 OR aa.AlertKey LIKE '%OFFLINE%'
	 OR aa.AlertType LIKE '%AgentJob%'
	 OR aa.AlertKey LIKE '%Job%'
	  )
ORDER BY aa.IsResolved ASC, aa.Priority ASC, aa.UpdatedDate DESC
