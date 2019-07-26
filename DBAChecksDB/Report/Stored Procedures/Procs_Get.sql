CREATE PROC [Report].[Procs_Get](@DatabaseID INT,@IsFunction BIT=0)
AS
SELECT NULL AS Value,'{ALL}' as Name
UNION ALL
SELECT DISTINCT object_name,object_name
FROM dbo.Procs
WHERE DatabaseID=@DatabaseID
AND @IsFunction=0
UNION ALL
SELECT DISTINCT object_name,object_name
FROM dbo.Functions
WHERE DatabaseID=@DatabaseID
AND @IsFunction=1