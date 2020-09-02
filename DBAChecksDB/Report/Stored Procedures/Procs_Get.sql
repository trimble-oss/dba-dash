CREATE PROC [Report].[Procs_Get](@DatabaseID INT,@IsFunction BIT=0)
AS
SELECT NULL AS Value,'{ALL}' as Name
UNION ALL
SELECT ObjectName,ObjectName 
FROM dbo.DBObjects
WHERE DatabaseID = @DatabaseID
AND ((ObjectType IN('P','PC','X') AND @IsFunction=0)
	OR ObjectType='FN' AND @IsFunction=1
	)