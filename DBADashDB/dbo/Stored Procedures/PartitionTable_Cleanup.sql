CREATE PROC dbo.PartitionTable_Cleanup(
		@TableName SYSNAME,
		@SchemaName SYSNAME='dbo',
		@DaysToKeep INT
)
AS
SET NOCOUNT ON

DECLARE @QualifiedTableName SYSNAME = QUOTENAME(@SchemaName) + '.' + QUOTENAME(@TableName)
DECLARE @DeleteOlderThanDate DATETIME2
SET @DeleteOlderThanDate = DATEADD(d,-@DaysToKeep,GETUTCDATE())

IF NOT EXISTS(	SELECT 1
				FROM dbo.PartitionHelper PH 
				WHERE PH.TableName = @TableName
				AND PH.SchemaName = @SchemaName
)
BEGIN
	RAISERROR('Invalid table',11,1)
	RETURN
END

DECLARE @PartitionFunction SYSNAME
DECLARE @MaxPartition INT

/* Find oldest partition we can remove */
SELECT TOP(1) @MaxPartition=PH.partition_number,
		@PartitionFunction=PH.PartitionFunctionName
FROM dbo.PartitionHelper PH 
WHERE PH.TableName = @TableName
AND PH.SchemaName = @SchemaName
AND PH.UpperBound < @DeleteOlderThanDate
AND PH.UpperBound < DATEADD(d,-1,GETUTCDATE())
ORDER BY PH.partition_number DESC

IF @MaxPartition IS NULL
BEGIN
	PRINT 'Nothing to cleanup'
	RETURN
END

/* Truncate partitions */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = CONCAT('TRUNCATE TABLE ',@QualifiedTableName,' WITH(PARTITIONS (1 TO ', @MaxPartition, '))')

PRINT @SQL
EXEC sp_executesql @SQL

/* Remove partitions previously truncated */
DECLARE @lb DATETIME2
DECLARE @rows BIGINT
DECLARE cMerge CURSOR LOCAL FAST_FORWARD FOR 
	SELECT	LowerBound,
			rows
	FROM dbo.PartitionHelper PH 
	WHERE PH.TableName = @TableName
	AND PH.SchemaName = @SchemaName
	AND UpperBound < @DeleteOlderThanDate
	AND UpperBound < DATEADD(d,-1,GETUTCDATE())
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
	EXEC sp_executesql @SQL,N'@lb DATETIME2',@lb
END

CLOSE cMerge
DEALLOCATE cMerge