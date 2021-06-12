CREATE PROC dbo.ObjectType_Get
AS
SELECT ObjectType,TypeDescription
FROM dbo.ObjectType
ORDER BY TypeDescription