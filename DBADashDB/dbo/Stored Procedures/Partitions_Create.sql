CREATE PROC dbo.Partitions_Create(
	@TableName SYSNAME,
	@SchemaName SYSNAME='dbo',
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
DECLARE @Boundary DATETIME2

SELECT TOP(1)	@Boundary= PH.LowerBound, 
				@PartitionScheme= PH.PartitionSchemeName, 
				@PartitionFunction = PH.PartitionFunctionName
FROM dbo.PartitionHelper PH
WHERE PH.TableName = @TableName
AND PH.SchemaName = @SchemaName
ORDER BY PH.partition_number DESC

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

EXEC sp_executesql @SQL,N'@PeriodCount INT,@Boundary DATETIME2',@PeriodCount,@Boundary

COMMIT