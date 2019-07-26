CREATE PROC Report.Databases_Get(@InstanceID INT)
AS
SELECT NULL AS DatabaseID,'{ALL}' AS Name
UNION ALL
SELECT DatabaseID,Name 
FROM dbo.Databases 
WHERE InstanceID=@InstanceID
AND IsActive=1