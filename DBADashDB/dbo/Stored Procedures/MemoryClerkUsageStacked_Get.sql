CREATE PROC dbo.MemoryClerkUsageStacked_Get(
	@InstanceID INT,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL,
	@Mins INT=NULL,
	@Agg VARCHAR(20)='NONE', -- Options: AVG,MIN,MAX,NONE
	@Measure NVARCHAR(128)='pages_kb', -- Options: pages_kb,virtual_memory_reserved_kb,virtual_memory_committed_kb,awe_allocated_kb,shared_memory_reserved_kb,shared_memory_committed_kb
	@Top INT=10, -- Clerks outside the top N (by usage over the period) are combined into {Other}
	@Debug BIT=0
)
AS
SET NOCOUNT ON
/* Memory usage over time for all memory clerks, for a stacked chart */
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
IF @FromDate IS NULL
BEGIN
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
END
IF @ToDate IS NULL
BEGIN
	SET @ToDate = GETUTCDATE()
END

/*	Collection only includes clerks above a threshold so a clerk can be missing from some snapshots in a date group.
	AVG/MIN over just the rows present would overstate it (e.g. a 10GB clerk in 1 of 60 snapshots would average 10GB rather than ~170MB) and the stack would exceed total memory.
	For AVG we SUM and divide by the number of snapshots in the date group.  For MIN a clerk missing from any snapshot in the group is treated as 0.
	The number of snapshots in a group is the MAX row count of any clerk in that group - some clerks (e.g. buffer pool) are always present.
	This uses the already aggregated data so it doesn't need another scan of dbo.MemoryUsage.
*/
DECLARE @MeasureCol NVARCHAR(MAX) = 'MU.' + QUOTENAME(@MeasureValidated)
DECLARE @ValueSQL NVARCHAR(MAX) = CASE @AggValidated
									WHEN 'NONE' THEN @MeasureCol
									WHEN 'AVG' THEN 'SUM(' + @MeasureCol + ')'
									ELSE @AggValidated + '(' + @MeasureCol + ')' END
DECLARE @AdjustedSQL NVARCHAR(MAX) = CASE @AggValidated
									WHEN 'AVG' THEN 'T.Value / MAX(T.Snapshots) OVER(PARTITION BY T.SnapshotDate)'
									WHEN 'MIN' THEN 'CASE WHEN T.Snapshots < MAX(T.Snapshots) OVER(PARTITION BY T.SnapshotDate) THEN 0 ELSE T.Value END'
									ELSE 'T.Value' END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WITH T AS (
	SELECT MU.MemoryClerkTypeID,
		' + CASE WHEN @AggValidated = 'NONE' THEN 'MU.SnapshotDate' ELSE 'DG.DateGroup' END + ' AS SnapshotDate,
		' + @ValueSQL + ' AS Value,
		' + CASE WHEN @AggValidated = 'NONE' THEN '1' ELSE 'COUNT(*)' END + ' AS Snapshots
	FROM dbo.MemoryUsage MU
	' + CASE WHEN @AggValidated='NONE' THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(MU.SnapshotDate,@Mins) DG' END + '
	WHERE MU.SnapshotDate >= @FromDate
	AND MU.SnapshotDate < @ToDate
	AND MU.InstanceID = @InstanceID
	' + CASE WHEN @AggValidated = 'NONE' THEN '' ELSE 'GROUP BY MU.MemoryClerkTypeID,DG.DateGroup' END + '
),
Adjusted AS (
	SELECT T.MemoryClerkTypeID,
		T.SnapshotDate,
		' + @AdjustedSQL + ' AS Value
	FROM T
),
ClerkTotals AS (
	SELECT A.MemoryClerkTypeID,
		A.SnapshotDate,
		A.Value,
		SUM(A.Value) OVER(PARTITION BY A.MemoryClerkTypeID) AS ClerkTotal
	FROM Adjusted A
),
/* Rank is calculated from T rather than joining back to it so dbo.MemoryUsage is only scanned once.  MemoryClerkTypeID breaks ties so we get exactly @Top clerks */
Ranked AS (
	SELECT CT.MemoryClerkTypeID,
		CT.SnapshotDate,
		CT.Value,
		CT.ClerkTotal,
		DENSE_RANK() OVER(ORDER BY CT.ClerkTotal DESC, CT.MemoryClerkTypeID) AS ClerkRank
	FROM ClerkTotals CT
)
SELECT ISNULL(MCT.MemoryClerkType,''{Other}'') AS MemoryClerkType,
	R.SnapshotDate,
	SUM(R.Value) AS ' + QUOTENAME(@MeasureValidated) + '
FROM Ranked R
LEFT JOIN dbo.MemoryClerkType MCT ON MCT.MemoryClerkTypeID = R.MemoryClerkTypeID AND R.ClerkRank <= @Top AND R.ClerkTotal > 0
GROUP BY ISNULL(MCT.MemoryClerkType,''{Other}''),
		CASE WHEN MCT.MemoryClerkTypeID IS NULL THEN 2147483647 ELSE R.ClerkRank END,
		R.SnapshotDate
ORDER BY CASE WHEN MCT.MemoryClerkTypeID IS NULL THEN 2147483647 ELSE R.ClerkRank END, R.SnapshotDate'

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@Mins INT,@Top INT',@InstanceID,@FromDate,@ToDate,@Mins,@Top
