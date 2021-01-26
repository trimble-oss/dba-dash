CREATE PROC [Report].[Databases_Get](@Instance SYSNAME)
AS
SELECT NULL AS DatabaseID,'{ALL}' AS Name
UNION ALL
SELECT d.DatabaseID,d.name 
FROM dbo.Databases d
JOIN dbo.Instances I ON I.InstanceID = d.InstanceID
WHERE I.Instance=@Instance
AND d.IsActive=1
AND I.IsActive=1
ORDER BY Name