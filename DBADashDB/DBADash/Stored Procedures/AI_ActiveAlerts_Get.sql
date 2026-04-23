CREATE PROC DBADash.AI_ActiveAlerts_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
SELECT TOP (@MaxRows)
	AA.AlertType,
	AA.IsResolved,
	AA.IsAcknowledged,
	AA.Priority,
	I.InstanceDisplayName,
	AA.AlertKey,
	AA.LastMessage,
	AA.TriggerDate,
	AA.UpdatedDate
FROM Alert.ActiveAlerts AA
JOIN dbo.Instances I ON I.InstanceID = AA.InstanceID
WHERE AA.IsBlackout = 0
  AND (@InstanceFilter IS NULL OR I.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (@HoursBack IS NULL OR AA.UpdatedDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME()))
ORDER BY AA.IsResolved,
		 AA.IsAcknowledged,
		 AA.Priority ASC,
		 I.InstanceDisplayName,
		 AA.TriggerDate DESC
