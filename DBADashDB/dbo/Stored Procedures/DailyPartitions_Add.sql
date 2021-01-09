CREATE PROC [dbo].[DailyPartitions_Add](@TableName SYSNAME,@DaysInFuture INT=14)
AS
/* 
	Generic proc to handle adding new daily partitions to the partitioned tables.
*/
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
SELECT @Boundary = ISNULL(@Boundary,CAST(GETUTCDATE() AS DATE))
-- Repeat until we are @DaysInFuture in the future
WHILE DATEDIFF(day, GETUTCDATE(), @Boundary) < @DaysInFuture
BEGIN;
   -- Increase by a day and split partition
   SET @Boundary = DATEADD(day, 1, @Boundary);

   ALTER PARTITION SCHEME ' + QUOTENAME(@PartitionScheme) + '
   NEXT USED [PRIMARY]

   ALTER PARTITION FUNCTION ' + QUOTENAME(@PartitionFunction) + '()
         SPLIT RANGE (@Boundary);
END;'

EXEC sp_executesql @SQL,N'@PartitionFunction SYSNAME,@TableName SYSNAME,@DaysInFuture INT',@PartitionFunction,@TableName,@DaysInFuture