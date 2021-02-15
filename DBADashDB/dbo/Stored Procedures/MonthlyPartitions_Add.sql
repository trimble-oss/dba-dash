CREATE PROC [dbo].[MonthlyPartitions_Add](@TableName SYSNAME,@MonthsInFuture INT=1)
AS
DECLARE @PartitionFunction SYSNAME
DECLARE @PartitionScheme SYSNAME

SELECT TOP(1) @PartitionScheme = ps.name,
       @PartitionFunction = pf.name
FROM sys.indexes i
JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf  ON pf.function_id = ps.function_id
WHERE i.object_id = OBJECT_ID(@TableName);

IF (@PartitionFunction IS NULL)
BEGIN
	RAISERROR('Invalid table %s',11,1,@TableName)
	RETURN
END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
DECLARE @Boundary DATETIME2(3)
SELECT TOP(1) @Boundary= lb 
FROM dbo.PartitionBoundaryHelper(@PartitionFunction,@TableName)
ORDER BY partition_number DESC
SELECT @Boundary = ISNULL(@Boundary,DATEADD(m, DATEDIFF(m, 0, GETUTCDATE())-1, 0))
-- Repeat until we are @MonthsInFuture in the future
WHILE DATEDIFF(m, GETUTCDATE(), @Boundary) < @MonthsInFuture
BEGIN;
   -- Increase by 1 month and split partition
   SET @Boundary = DATEADD(m, 1, @Boundary);

   ALTER PARTITION SCHEME ' + QUOTENAME(@PartitionScheme) + '
   NEXT USED [PRIMARY]

   ALTER PARTITION FUNCTION ' + QUOTENAME(@PartitionFunction) + '()
         SPLIT RANGE (@Boundary);
END;'


PRINT @SQL
EXEC sp_executesql @SQL,N'@PartitionFunction SYSNAME,@TableName SYSNAME,@MonthsInFuture INT',@PartitionFunction,@TableName,@MonthsInFuture