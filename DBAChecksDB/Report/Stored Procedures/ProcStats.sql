CREATE  PROC Report.ProcStats(@InstanceID INT,@DatabaseID INT=NULL,@Proc SYSNAME=NULL,@FromDate DATETIME=NULL,@ToDate DATETIME=NULL,@Measure VARCHAR(30)='TotalDuration',@DateAgg VARCHAR(20)='NONE')
AS
IF @FromDate IS NULL
	SET @FromDate = CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,DATEADD(mi,-60,GETUTCDATE()),120),16,4,'0:00'),120) 
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()

DECLARE @DateAggString NVARCHAR(MAX)
DECLARE @MeasureString NVARCHAR(MAX) 
SELECT @MeasureString = CASE WHEN @Measure IN('TotalCPU','AvgCPU','TotalDuration','AvgDuration','ExecutionCount','ExecutionsPerMin','AvgLogicalReads','AvgPhysicalReads','AvgWrites','TotalWrites','TotalLogicalReads','TotalPhysicalReads') THEN @Measure ELSE NULL END
SELECT @DateAggString = CASE @DateAgg WHEN 'NONE' THEN 'PS.SnapshotDate'
		WHEN '10MIN' THEN 'CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,PS.SnapshotDate,120),16,4,''0:00''),120)'
		WHEN '60MIN' THEN 'CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,PS.SnapshotDate,120),15,5,''00:00''),120)'
		WHEN '1DAY' THEN 'CAST(PS.SnapshotDate AS DATE)' ELSE NULL END
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WITH agg AS (
SELECT P.ProcID,
	   ' + @DateAggString + ' as SnapshotDate,
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
FROM dbo.ProcStats PS
    JOIN dbo.Procs P ON PS.ProcID = P.ProcID
    JOIN dbo.Databases D ON D.DatabaseID = P.DatabaseID
WHERE D.InstanceID=@InstanceID 
AND D.IsActive=1
AND PS.SnapshotDate >= @FromDate
AND PS.SnapshotDate< @ToDate
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID=@DatabaseID' END + '
' + CASE WHEN @Proc IS NULL THEN '' ELSE 'AND P.object_name=@Proc' END + '
GROUP BY ' + @DateAggString + ',P.ProcID,D.Name,P.object_name,D.DatabaseID
)
, T AS (
	SELECT agg.*,
		' + @MeasureString + ' as Measure,
		ROW_NUMBER() OVER (PARTITION BY SnapshotDate ORDER BY ' + @MeasureString + ' DESC) ProcRank
	FROM agg
)
SELECT T.*
FROM T
WHERE ProcRank <100'
PRINT @SQL
IF @SQL IS NOT NULL
BEGIN
EXEC sp_executesql @SQL,N'@InstanceID INT,@DatabaseID INT,@FromDate DATETIME,@ToDate DATETIME,@Proc SYSNAME',@InstanceID,@DatabaseID,@FromDate,@ToDate,@Proc
END 
ELSE
BEGIN
DECLARE  @results TABLE( [ProcID] int, [SnapshotDate] datetime, [DatabaseName] nvarchar(128),[DatabaseID] INT, [object_name] nvarchar(128), [TotalCPU] decimal(29,9), [AvgCPU] decimal(29,9), [ExecutionCount] bigint, [ExecutionsPerMin] decimal(38,9), [TotalDuration] decimal(29,9), [AvgDuration] decimal(29,9), [TotalLogicalReads] bigint, [AvgLogicalReads] bigint, [TotalPhysicalReads] bigint, [AvgPhysicalReads] bigint, [TotalWrites] bigint, [AvgWrites] bigint, [Measure] decimal(29,9), [ProcRank] bigint )
SELECT * FROM @Results
END