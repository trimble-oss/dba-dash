CREATE PROC dbo.PartitionTable_Cleanup(
		@TableName SYSNAME,
		@SchemaName SYSNAME='dbo',
		@DaysToKeep INT
)
AS
DECLARE @PartitionedTable SYSNAME = QUOTENAME(@SchemaName) + '.' + QUOTENAME(@TableName)

IF NOT EXISTS(	SELECT 1
				FROM sys.indexes i 
				INNER JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
				WHERE i.type IN (0,1)
				AND i.object_id = OBJECT_ID(@PartitionedTable)
				)
BEGIN
	RAISERROR('Invalid table',11,1);
	RETURN;
END
DECLARE @PartitionFunction SYSNAME

SELECT TOP(1) @PartitionFunction = pf.name
FROM sys.indexes i
JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf  ON pf.function_id = ps.function_id
WHERE i.object_id = OBJECT_ID(@PartitionedTable);

IF (@PartitionFunction IS NULL)
BEGIN
	RAISERROR('Invalid table',11,1)
	RETURN
END

DECLARE @DeleteOlderThanDate DATETIME2(3)
SET @DeleteOlderThanDate = DATEADD(d,-@DaysToKeep,GETUTCDATE())

DECLARE @MaxPartition INT
/* Find oldest partition we can remove */
SELECT @MaxPartition=MAX(partition_number)
FROM dbo.PartitionBoundaryHelper(@PartitionFunction,@PartitionedTable)
WHERE ub<@DeleteOlderThanDate
AND ub < DATEADD(d,-1,GETUTCDATE())

IF @MaxPartition IS NULL
BEGIN
	PRINT 'Nothing to cleanup'
	RETURN
END

/* Truncate partitions */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = CONCAT('TRUNCATE TABLE ',@PartitionedTable,' WITH(PARTITIONS (1 TO ', @MaxPartition, '))')

PRINT @SQL
EXEC sp_executesql @SQL

/* Remove partitions previously truncated */
DECLARE @lb DATETIME2(3)
DECLARE @rows BIGINT
DECLARE cMerge CURSOR LOCAL FAST_FORWARD FOR 
	SELECT	lb,
			rows
	FROM dbo.PartitionBoundaryHelper(@PartitionFunction,@PartitionedTable)
	WHERE ub < @DeleteOlderThanDate
	AND ub < DATEADD(d,-1,GETUTCDATE())
	AND partition_number>1

OPEN cMerge
WHILE 1=1
BEGIN
	FETCH NEXT FROM cMerge INTO @lb,@rows
	IF @@FETCH_STATUS<> 0
		BREAK
	IF @rows >0
	BEGIN
		RAISERROR('Partition management error. Expected row count 0 before MERGE',11,1)
		RETURN
	END
	SET @SQL = CONCAT('ALTER PARTITION FUNCTION ',QUOTENAME(@PartitionFunction),'() MERGE RANGE (@lb);')
	EXEC sp_executesql @SQL,N'@lb DATETIME2(3)',@lb
END

CLOSE cMerge
DEALLOCATE cMerge