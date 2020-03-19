CREATE  PROC [Report].[ProcStats](
	@InstanceID INT,
	@DatabaseID INT=NULL,
	@Proc SYSNAME=NULL,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL,
	@Measure VARCHAR(30)='TotalDuration',
	@DateAgg VARCHAR(20)='NONE',
	@IsFunction BIT=0,
	@UTCOffset INT=0
)
WITH EXECUTE AS OWNER
AS
SELECT @FromDate= DATEADD(mi, -@UTCOffset, @FromDate),
	@ToDate = DATEADD(mi, -@UTCOffset, @ToDate) 

IF @FromDate IS NULL
	SET @FromDate = CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,DATEADD(mi,-120,GETUTCDATE()),120),16,4,'0:00'),120) 
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()

DECLARE @TypeString NVARCHAR(MAX) = CASE WHEN @IsFunction=1 THEN 'Function' ELSE 'Proc' END
DECLARE @DateAggString NVARCHAR(MAX)
DECLARE @MeasureString NVARCHAR(MAX) 
SELECT @MeasureString = CASE WHEN @Measure IN('TotalCPU','AvgCPU','TotalDuration','AvgDuration','ExecutionCount','ExecutionsPerMin','AvgLogicalReads','AvgPhysicalReads','AvgWrites','TotalWrites','TotalLogicalReads','TotalPhysicalReads') THEN @Measure ELSE NULL END
SELECT @DateAggString = CASE @DateAgg WHEN 'NONE' THEN 'DATEADD(mi, @UTCOffset, PS.SnapshotDate)'
		WHEN '10MIN' THEN 'CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,DATEADD(mi, @UTCOffset, PS.SnapshotDate),120),16,4,''0:00''),120)'
		WHEN '60MIN' THEN 'DATEADD(mi, @UTCOffset, PS.SnapshotDate) '
		WHEN '1DAY' THEN 'CAST(DATEADD(mi, @UTCOffset, PS.SnapshotDate) AS DATE)' ELSE NULL END
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WITH agg AS (
SELECT ' + @DateAggString + ' as SnapshotDate,
       D.name AS DatabaseName,
	   D.DatabaseID,
       P.object_name,
	   SUM(PS.total_worker_time)/1000000.0 as TotalCPU,
	   SUM(PS.total_worker_time)/NULLIF(SUM(PS.execution_count),0)/1000000.0 as AvgCPU,
	   SUM(PS.execution_count) as ExecutionCount,
	   SUM(PS.execution_count)/(NULLIF(SUM(PeriodTime),0)/60000000.0) as ExecutionsPerMin,
	   SUM(PS.total_elapsed_time)/1000000.0 AS TotalDuration,
	   SUM(PS.total_elapsed_time)/NULLIF(SUM(PS.execution_count),0)/1000000.0 AS AvgDuration,
	   SUM(PS.total_logical_reads) as TotalLogicalReads,
	   SUM(PS.total_logical_reads)/NULLIF(SUM(PS.execution_count),0) as AvgLogicalReads,
	   SUM(PS.total_physical_reads) as TotalPhysicalReads,
	   SUM(PS.total_physical_reads)/NULLIF(SUM(PS.execution_count),0) as AvgPhysicalReads,
	   SUM(PS.total_logical_writes) as TotalWrites,
	   SUM(PS.total_logical_writes)/NULLIF(SUM(PS.execution_count),0) as AvgWrites
FROM dbo.' + @TypeString + 'Stats' + CASE WHEN @DateAgg IN('60MIN','1DAY') THEN '_60MIN' ELSE '' END + ' PS
    JOIN dbo.' + @TypeString + 's P ON PS.' + @TypeString + 'ID = P.' + @TypeString + 'ID
    JOIN dbo.Databases D ON D.DatabaseID = P.DatabaseID
WHERE D.InstanceID=@InstanceID 
AND PS.InstanceID = @InstanceID
AND D.IsActive=1
AND PS.SnapshotDate >= @FromDate 
AND PS.SnapshotDate< @ToDate
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID=@DatabaseID' END + '
' + CASE WHEN @Proc IS NULL THEN '' ELSE 'AND P.object_name=@Proc' END + '
GROUP BY ' + @DateAggString + ',D.Name,P.object_name,D.DatabaseID
)
, T AS (
	SELECT agg.*,
		' + @MeasureString + ' as Measure,
		ROW_NUMBER() OVER (PARTITION BY SnapshotDate ORDER BY ' + @MeasureString + ' DESC) ProcRank
	FROM agg
)
SELECT T.*
FROM T
WHERE ProcRank <=50'
PRINT @SQL
IF @SQL IS NOT NULL
BEGIN
EXEC sp_executesql @SQL,N'@InstanceID INT,@DatabaseID INT,@FromDate DATETIME,@ToDate DATETIME,@Proc SYSNAME,@UTCOffset INT',@InstanceID,@DatabaseID,@FromDate,@ToDate,@Proc,@UTCOffset
END 
ELSE
BEGIN
DECLARE  @results TABLE( [SnapshotDate] DATETIME, [DatabaseName] NVARCHAR(128),[DatabaseID] INT, [object_name] NVARCHAR(128), [TotalCPU] DECIMAL(29,9), [AvgCPU] DECIMAL(29,9), [ExecutionCount] BIGINT, [ExecutionsPerMin] DECIMAL(38,9), [TotalDuration] DECIMAL(29,9), [AvgDuration] DECIMAL(29,9), [TotalLogicalReads] BIGINT, [AvgLogicalReads] BIGINT, [TotalPhysicalReads] BIGINT, [AvgPhysicalReads] BIGINT, [TotalWrites] BIGINT, [AvgWrites] BIGINT, [Measure] DECIMAL(29,9), [ProcRank] BIGINT )
SELECT * FROM @Results
END