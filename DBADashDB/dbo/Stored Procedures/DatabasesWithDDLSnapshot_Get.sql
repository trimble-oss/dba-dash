CREATE PROC dbo.DatabasesWithDDLSnapshot_Get(
	@Instance NVARCHAR(128)
)
AS
SELECT D.DatabaseID,D.name
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
AND I.Instance = @Instance
AND EXISTS(SELECT 1
			FROM dbo.DDLSnapshots SS 
			WHERE SS.DatabaseID = D.DatabaseID
			)
ORDER BY D.name