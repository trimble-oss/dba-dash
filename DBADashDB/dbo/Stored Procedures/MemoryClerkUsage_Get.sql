CREATE PROC dbo.MemoryClerkUsage_Get(
	@InstanceID INT,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL,
	@MemoryClerkType NVARCHAR(60),
	@Mins INT=NULL,
	@Agg VARCHAR(20)='NONE', -- Options: AVG,MIN,MAX,NONE
	@Measure NVARCHAR(128)='pages_kb', -- Options: pages_kb,virtual_memory_reserved_kb,virtual_memory_committed_kb,awe_allocated_kb,shared_memory_reserved_kb,shared_memory_committed_kb
	@Debug BIT=0
)
AS
DECLARE @SQLAgg NVARCHAR(MAX)
DECLARE @MeasureValidated NVARCHAR(MAX) = CASE WHEN @Measure IN('pages_kb','virtual_memory_reserved_kb','virtual_memory_committed_kb','awe_allocated_kb','shared_memory_reserved_kb','shared_memory_committed_kb') THEN @Measure ELSE NULL END
DECLARE @AggValidated NVARCHAR(MAX) = CASE WHEN @Mins IS NULL OR @Mins<=0 THEN 'NONE' WHEN @Agg IN('AVG','MIN','MAX','NONE') THEN @Agg ELSE NULL END
IF @AggValidated ='NONE' AND @Mins>0
BEGIN;
	THROW 50001,'Invalid @Agg/@Mins combination',1;
	RETURN
END
IF @AggValidated IS NULL
BEGIN;
	THROW 50002,'Invalid @Agg',1;
	RETURN
END
IF @MeasureValidated IS NULL
BEGIN;
	THROW 50003,'Invalid @Measure',1;
	RETURN
END
DECLARE @MeasureSQL NVARCHAR(MAX) = CASE WHEN @AggValidated = 'NONE' THEN 'MU.' + QUOTENAME(@MeasureValidated)
									ELSE @AggValidated + '(MU.' + QUOTENAME(@MeasureValidated) + ') AS ' + QUOTENAME(@MeasureValidated) END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT MCT.MemoryClerkType, 
	   ' + CASE WHEN @AggValidated = 'NONE' THEN 'MU.SnapshotDate,' ELSE 'DG.DateGroup AS SnapshotDate,' END + '
	   ' + @MeasureSQL + '
FROM dbo.MemoryUsage MU
JOIN dbo.MemoryClerkType MCT ON MCT.MemoryClerkTypeID = MU.MemoryClerkTypeID
'+ CASE WHEN @AggValidated='NONE' THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(MU.SnapshotDate,@Mins) DG' END + '
WHERE MCT.MemoryClerkType = @MemoryClerkType
AND MU.SnapshotDate >= @FromDate
AND MU.SnapshotDate < @ToDate
AND MU.InstanceID = @InstanceID
' + CASE WHEN @AggValidated = 'NONE' THEN '' ELSE 'GROUP BY MCT.MemoryClerkType,DG.DateGroup' END + '
ORDER BY SnapshotDate'

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@MemoryClerkType NVARCHAR(60),@Mins INT',@InstanceID,@FromDate,@ToDate,@MemoryClerkType,@Mins