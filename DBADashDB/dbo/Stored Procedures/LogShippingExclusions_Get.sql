CREATE PROC dbo.LogShippingExclusions_Get(
	@InstanceID INT=NULL /* -1 for root.  NULL for all */
)
AS
/*
	Log shipping database exclusions.  Excluded databases are reported with N/A status.
	Exclusions apply to databases on the secondary (restoring/standby) and to databases missing from the secondary (log shipping pairs).
*/
SELECT X.InstanceID,
	CASE WHEN X.InstanceID = -1 THEN '{Root}' ELSE I.InstanceDisplayName END AS Instance,
	X.DatabaseName
FROM dbo.LogShippingExclusions X
LEFT JOIN dbo.Instances I ON X.InstanceID = I.InstanceID
WHERE (X.InstanceID = @InstanceID OR @InstanceID IS NULL)
ORDER BY X.InstanceID,X.DatabaseName
