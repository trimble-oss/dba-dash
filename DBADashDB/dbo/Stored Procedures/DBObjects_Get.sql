CREATE PROC dbo.DBObjects_Get(@DatabaseID INT,@Types VARCHAR(MAX))
AS
SELECT O.ObjectID,O.ObjectType,O.SchemaName,O.ObjectName
FROM dbo.DBObjects O
WHERE O.DatabaseID=@DatabaseID
AND O.IsActive=1
AND EXISTS(SELECT 1 FROM STRING_SPLIT(@Types,',') ss WHERE ss.value = O.ObjectType)
ORDER BY O.SchemaName,O.ObjectName