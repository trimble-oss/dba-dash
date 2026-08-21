CREATE PROC dbo.AvailabilityGroupInstances_Get(
    @InstanceID INT
)
AS
SET NOCOUNT ON
/* Returns the OTHER monitored instances that share an availability group with @InstanceID, so the ad-hoc XE trace
   UI can offer to trace every replica of the same AG(s) at once.

   Every monitored replica independently reports its own dbo.AvailabilityReplicas rows tagged with its own InstanceID
   but under the same group_id GUID, so siblings resolve with a self-join on group_id - no fragile server-name match. */
SELECT DISTINCT
       I.InstanceID,
       I.InstanceGroupName AS InstanceName
FROM dbo.AvailabilityReplicas ar1
JOIN dbo.AvailabilityReplicas ar2 ON ar1.group_id = ar2.group_id
JOIN dbo.Instances I ON I.InstanceID = ar2.InstanceID
WHERE ar1.InstanceID = @InstanceID
AND ar2.InstanceID <> @InstanceID
AND ISNULL(I.IsActive, 0) = 1
ORDER BY I.InstanceGroupName
