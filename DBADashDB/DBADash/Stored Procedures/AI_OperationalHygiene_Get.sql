CREATE PROC DBADash.AI_OperationalHygiene_Get(
	@MaxRows INT = 500,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 720
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
	aa.ResolvedDate,
	aa.LastMessage
FROM Alert.ActiveAlerts aa
INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
WHERE i.IsActive = 1
  AND aa.UpdatedDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY aa.UpdatedDate DESC
