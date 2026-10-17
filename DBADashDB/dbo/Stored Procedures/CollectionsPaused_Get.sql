CREATE PROC dbo.CollectionsPaused_Get(
	@InstanceIDs VARCHAR(MAX)
)
AS
/* Pause state for the Pause Collections dialog, with what's needed to send the pause to the collecting service */
SELECT I.InstanceID,
	I.ConnectionID,
	I.InstanceDisplayName,
	I.CollectAgentID,
	I.ImportAgentID,
	CASE WHEN IA.MessagingEnabled=1 AND CA.MessagingEnabled=1 AND CA.ScheduleChangesEnabled=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS ScheduleChangesEnabled,
	CASE WHEN I.CollectionsPausedUntil > SYSUTCDATETIME() THEN I.CollectionsPausedUntil END AS CollectionsPausedUntil
FROM dbo.Instances I
JOIN dbo.DBADashAgent IA ON I.ImportAgentID = IA.DBADashAgentID
JOIN dbo.DBADashAgent CA ON I.CollectAgentID = CA.DBADashAgentID
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') SS WHERE SS.value = I.InstanceID)
AND I.IsActive = 1
