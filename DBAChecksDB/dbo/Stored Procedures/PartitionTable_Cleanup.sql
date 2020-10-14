CREATE PROC [dbo].[PartitionTable_Cleanup](@TableName SYSNAME,@DaysToKeep INT)
AS
IF(SELECT COUNT(*) 
	FROM sys.tables t
	JOIN sys.schemas s ON s.schema_id = t.schema_id
	WHERE t.name = @TableName
	AND s.name IN('dbo','switch')) <> 2
BEGIN
	RAISERROR('Invalid table',11,1);
	RETURN;
END
DECLARE @PartitionFunction SYSNAME

SELECT TOP(1) @PartitionFunction = pf.name
FROM sys.indexes i
JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf  ON pf.function_id = ps.function_id
WHERE i.object_id = OBJECT_ID(@TableName);

IF (@PartitionFunction IS NULL)
BEGIN
	RAISERROR('Invalid table',11,1)
	RETURN
END

DECLARE @PartitionedTable SYSNAME = 'dbo.' + QUOTENAME(@TableName)
DECLARE @SwitchTable SYSNAME = 'Switch.' + QUOTENAME(@TableName)
DECLARE @SQL NVARCHAR(MAX) = N'
DECLARE @DeleteOlderThanDate DATETIME2(3)
SET @DeleteOlderThanDate = DATEADD(d,-@DaysToKeep,GETUTCDATE())
WHILE 1=1
BEGIN;
	DECLARE @UpperBound DATETIME2(3);
	DECLARE @LowBound DATETIME2(3);

	-- get upper/lower bounds for partition number 2
	SELECT @LowBound = lb,
			@UpperBound = ub
	FROM dbo.PartitionBoundaryHelper(@PartitionFunction,@PartitionedTable)
	WHERE partition_number= 2

	-- delete oldest partition if it contains data less than the specified date.
	-- Note: Upper bound is start of next partition.  The partition we remove will have data only older than this date.
	IF @UpperBound <= @DeleteOlderThanDate
	BEGIN
	   -- Swap out the first "catch all" partition (should be empty).
	   ALTER TABLE ' + @PartitionedTable + '
			 SWITCH PARTITION 1 TO ' + @SwitchTable + '
		-- empty table
	   TRUNCATE TABLE ' + @SwitchTable + ';
	   -- Swap out the oldest partition.  
	   ALTER TABLE ' + @PartitionedTable + '
			 SWITCH PARTITION 2 TO ' + @SwitchTable + ';
	   -- empty table
	   TRUNCATE TABLE ' + @SwitchTable + ';
	   -- Remove oldest partition (merge with "catch all" partition.  Both partitions are empty so this is a metadata operation)
	   ALTER PARTITION FUNCTION ' + QUOTENAME(@PartitionFunction) + '()
			 MERGE RANGE (@LowBound);
	END
	ELSE
	BEGIN
		BREAK;
	END
END;'

EXEC sp_executesql @SQL,N'@PartitionFunction SYSNAME,@PartitionedTable SYSNAME,@DaysToKeep INT',@PartitionFunction,@PartitionedTable,@DaysToKeep