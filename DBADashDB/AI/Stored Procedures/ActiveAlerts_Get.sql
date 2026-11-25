CREATE PROC AI.ActiveAlerts_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
SET NOCOUNT ON
SELECT TOP (@MaxRows)
	AA.AlertType,
	AA.IsResolved,
	AA.IsAcknowledged,
	DS.IsPending, /* Waiting for the issue to persist for the rule's notify delay - not notified yet */
	DS.IsResolving, /* Issue has cleared - waiting for the rule's resolve delay */
	AA.Priority,
	I.InstanceDisplayName,
	AA.AlertKey,
	AA.LastMessage,
	AA.TriggerDate,
	AA.UpdatedDate
FROM Alert.ActiveAlerts AA
JOIN dbo.Instances I ON I.InstanceID = AA.InstanceID
CROSS APPLY Alert.AlertDelayStatus_Get(AA.IsResolved,ISNULL(AA.ActiveSince,AA.TriggerDate),AA.ResolvingSince,AA.RuleID) DS
WHERE AA.IsBlackout = 0
  AND (@InstanceFilter IS NULL OR I.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (@HoursBack IS NULL OR AA.UpdatedDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME()))
ORDER BY AA.IsResolved,
		 AA.IsAcknowledged,
		 AA.Priority ASC,
		 I.InstanceDisplayName,
		 AA.TriggerDate DESC
