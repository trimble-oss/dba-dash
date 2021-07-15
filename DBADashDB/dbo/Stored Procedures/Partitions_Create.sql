CREATE PROC dbo.Partitions_Create(
	@TableName SYSNAME,
	@PeriodCount INT,
	/* Valid values : m (Month)
					  d (Day)
	*/
	@PeriodType CHAR(1)
)
AS
/* 
	Generic proc to handle adding new partitions to the partitioned tables.
	Creates daily or monthly partitions a specified number of periods into the future.
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
IF @PeriodType NOT IN('d','m') OR @PeriodType IS NULL
BEGIN
	RAISERROR('Invalid parameter value for @PeriodType.  Valid Values: d (Day), m (Month)',11,1)
	RETURN
END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
DECLARE @Boundary DATETIME2(3)
SELECT TOP(1) @Boundary= lb 
FROM dbo.PartitionBoundaryHelper(@PartitionFunction,@TableName)
ORDER BY partition_number DESC

SELECT @Boundary = CASE WHEN @Boundary IS NULL THEN DATEADD(' + @PeriodType + ', DATEDIFF(' + @PeriodType + ', 0, GETUTCDATE())-1, 0) 
					WHEN @Boundary < GETUTCDATE() THEN DATEADD(' + @PeriodType + ', DATEDIFF(' + @PeriodType + ', 0, GETUTCDATE()), 0) 
					ELSE @Boundary END

-- Repeat until we are specified periods in the future
WHILE DATEDIFF(' + @PeriodType + ', GETUTCDATE(), @Boundary) < @PeriodCount
BEGIN;
   -- Increase by 1 period and split partition
   SET @Boundary = DATEADD(' + @PeriodType + ', 1, @Boundary);

   ALTER PARTITION SCHEME ' + QUOTENAME(@PartitionScheme) + '
   NEXT USED [PRIMARY]

   ALTER PARTITION FUNCTION ' + QUOTENAME(@PartitionFunction) + '()
         SPLIT RANGE (@Boundary);
END;'

DECLARE @applock INT

SET XACT_ABORT ON
BEGIN TRAN
EXEC @applock = sp_getapplock @Resource = 'Partitioning',
                                 @LockMode = 'Exclusive',
                                 @LockOwner = 'Transaction',
                                 @LockTimeout = 5000;
IF NOT @applock IN(0,1)
BEGIN;
	THROW 50000,'sp_getapplock error',1;
	RETURN;
END

EXEC sp_executesql @SQL,N'@PartitionFunction SYSNAME,@TableName SYSNAME,@PeriodCount INT',@PartitionFunction,@TableName,@PeriodCount

COMMIT