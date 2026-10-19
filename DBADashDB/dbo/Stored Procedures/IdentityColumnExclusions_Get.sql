CREATE PROC dbo.IdentityColumnExclusions_Get(
	@InstanceID INT=NULL, /* -1 for root.  NULL for all */
	@DatabaseID INT=NULL /* -1 for root or instance level.  NULL for all */
)
AS
/*
	Identity column table exclusions.  Excluded tables are reported with N/A status.
	ObjectName is matched against the table name and schema.table.  % can be used as a wildcard.
*/
SELECT X.InstanceID,
	X.DatabaseID,
	CASE WHEN X.InstanceID = -1 THEN '{Root}' ELSE I.InstanceDisplayName END AS Instance,
	D.name AS DB,
	X.ObjectName
FROM dbo.IdentityColumnExclusions X
LEFT JOIN dbo.Instances I ON X.InstanceID = I.InstanceID
LEFT JOIN dbo.Databases D ON X.DatabaseID = D.DatabaseID
WHERE (X.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (X.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
ORDER BY X.InstanceID,X.DatabaseID,X.ObjectName
