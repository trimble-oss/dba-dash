CREATE PROC [dbo].[DailyPartitions_Add](@TableName SYSNAME,@DaysInFuture INT=14)
AS
/* 
	Generic proc to handle adding new daily partitions to the partitioned tables.
	Relies on the consistent name format used for partitioned functions/schemes
*/
DECLARE @PartitionTable SYSNAME = 'dbo.' + QUOTENAME(@TableName)
DECLARE @PartitionFunction SYSNAME = 'PF_' + @TableName
DECLARE @PartitionScheme SYSNAME = 'PS_' + @TableName

IF NOT EXISTS(SELECT * FROM Sys.partition_functions WHERE name = @PartitionFunction)
BEGIN
	RAISERROR('Invalid table',11,1)
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