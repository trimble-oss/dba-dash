CREATE PROC Databases_Get(@InstanceID INT)
AS
SELECT DatabaseID,Name
FROM dbo.Databases
WHERE InstanceID= @InstanceID
AND IsActive=1
ORDER BY Name