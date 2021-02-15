CREATE PROC dbo.DatabasesByInstance_Get(@Instance SYSNAME) 
AS
SELECT D.DatabaseID,D.name,O.ObjectID,D.InstanceID
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.DBObjects O ON O.DatabaseID = D.DatabaseID AND O.ObjectType='DB'
WHERE I.IsActive=1
AND D.IsActive=1
AND D.source_database_id IS NULL
AND I.Instance = @Instance
ORDER BY D.name