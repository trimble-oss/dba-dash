CREATE PROC dbo.DatabaseFinder_Get(
	@InstanceIDs IDs READONLY,
	@SearchString NVARCHAR(128)
)
AS
SELECT	I.InstanceID,
		D.DatabaseID,
		I.InstanceGroupName,
		D.name as DatabaseName
FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID 
WHERE EXISTS(SELECT 1 
			FROM @InstanceIDs T
			WHERE T.ID = I.InstanceID
			)
AND I.IsActive=1
AND D.IsActive=1
AND (D.name = @SearchString OR D.name LIKE @SearchString)
ORDER BY	InstanceGroupName,
			DatabaseName