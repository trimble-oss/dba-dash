CREATE PROC dbo.AvailabilityGroupRoleChanges_Get(
	@InstanceIDs IDs READONLY,
	@Days INT=30,
	@IncludeOtherReplicas BIT=0 /* Include role changes reported by other monitored replicas of the same availability groups */
)
AS
SET NOCOUNT ON
SELECT	RC.InstanceID,
		I.InstanceDisplayName AS Instance,
		RC.AvailabilityGroupName AS [Availability Group],
		RC.ReplicaServerName AS [Replica Server],
		CASE WHEN RC.NewRole = 1 THEN 'Became Primary'
			WHEN RC.PreviousRole = 1 THEN 'No Longer Primary'
			ELSE CONCAT(RC.PreviousRoleDesc,' => ',RC.NewRoleDesc) END AS [Change],
		RC.PreviousRoleDesc AS [Previous Role],
		RC.NewRoleDesc AS [New Role],
		RC.PreviousSnapshotDate AS [Changed After],
		RC.SnapshotDate AS [Changed Before],
		HD.HumanDuration AS [Detection Window],
		RC.Source
FROM dbo.AvailabilityGroupRoleChanges RC
JOIN dbo.Instances I ON I.InstanceID = RC.InstanceID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,RC.PreviousSnapshotDate,RC.SnapshotDate)) HD
WHERE RC.SnapshotDate >= DATEADD(d,-@Days,GETUTCDATE())
AND (
	EXISTS(
		SELECT 1
		FROM @InstanceIDs T
		WHERE T.ID = RC.InstanceID
	)
	OR (
		@IncludeOtherReplicas = 1
		AND EXISTS(
			SELECT 1
			FROM dbo.AvailabilityReplicas AR
			JOIN @InstanceIDs T ON T.ID = AR.InstanceID
			WHERE AR.group_id = RC.group_id
		)
	)
)
ORDER BY RC.SnapshotDate DESC,
		RC.AvailabilityGroupName,
		I.InstanceDisplayName
