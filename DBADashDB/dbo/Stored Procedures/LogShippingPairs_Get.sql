CREATE PROC dbo.LogShippingPairs_Get
AS
/*
	Configured log shipping primary/secondary instance pairs used to detect databases missing from the secondary.
	Exclusions are configured separately at root/secondary instance level (dbo.LogShippingExclusions).
	Pairs for soft-deleted (inactive) instances are not returned.  They are removed when the instance is hard deleted or become visible again if the instance is restored.
*/
SELECT P.PrimaryInstanceID,
	PI.InstanceDisplayName AS PrimaryInstance,
	P.SecondaryInstanceID,
	SI.InstanceDisplayName AS SecondaryInstance
FROM dbo.LogShippingPairs P
JOIN dbo.Instances PI ON P.PrimaryInstanceID = PI.InstanceID
JOIN dbo.Instances SI ON P.SecondaryInstanceID = SI.InstanceID
WHERE PI.IsActive=1
AND SI.IsActive=1
ORDER BY PI.InstanceDisplayName,SI.InstanceDisplayName
