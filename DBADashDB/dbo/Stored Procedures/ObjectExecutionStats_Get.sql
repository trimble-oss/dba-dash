CREATE PROC dbo.ObjectExecutionStats_Get(
	@Instance SYSNAME=NULL,
	@DatabaseID INT=NULL,
	@ObjectName SYSNAME=NULL,
	@SchemaName SYSNAME=NULL,
	@FromDateUTC DATETIME2(3)=NULL,
	@ToDateUTC DATETIME2(3)=NULL,
	@Measure VARCHAR(30)='TotalDuration',
	@UTCOffset INT=0,
	@InstanceID INT=NULL,
	@ObjectID BIGINT=NULL,
	@DateGroupingMin INT=NULL,
	/* 
		ObjectExecutionCounts table doesn't store zero executions counts.  This option will generate zero rows where we have object execution data collected for the instance but not for the specified stored proc.
	*/
	@ZeroFill BIT=1,
	@Debug BIT=0,
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET NOCOUNT ON
SET DATEFIRST 1 /* Start week on Monday */
CREATE TABLE #results(
    SnapshotDate DATETIME,
    DatabaseName NVARCHAR(128),
    DatabaseID INT,
	ObjectID BIGINT,
    object_name NVARCHAR(128),
    TotalCPU DECIMAL(29, 9),
    AvgCPU DECIMAL(29, 9),
	cpu_ms_per_sec DECIMAL(29, 9),
    ExecutionCount BIGINT,
    ExecutionsPerMin DECIMAL(38, 9),
    TotalDuration DECIMAL(29, 9),
    AvgDuration DECIMAL(29, 9),
	duration_ms_per_sec DECIMAL(29, 9),
    TotalLogicalReads BIGINT,
    AvgLogicalReads BIGINT,
    TotalPhysicalReads BIGINT,
    AvgPhysicalReads BIGINT,
    TotalWrites BIGINT,
    AvgWrites BIGINT,
	MaxExecutionsPerMin DECIMAL(29, 9),
    Measure DECIMAL(29, 9),
    ProcRank BIGINT,
	TotalMeasure DECIMAL(29, 9)
);

IF @Instance IS NULL AND @InstanceID IS NULL
BEGIN
	RAISERROR('Instance not specified',11,1);
	RETURN;
END

IF @FromDateUTC IS NULL
	SET @FromDateUTC = CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,DATEADD(mi,-120,GETUTCDATE()),120),16,4,'0:00'),120) 
IF @ToDateUTC IS NULL
	SET @ToDateUTC = GETUTCDATE()

DECLARE @DateAggString NVARCHAR(MAX)
DECLARE @MeasureString NVARCHAR(MAX) 
SELECT @MeasureString = CASE WHEN @Measure IN('TotalCPU','AvgCPU','TotalDuration','AvgDuration','ExecutionCount','ExecutionsPerMin','AvgLogicalReads','AvgPhysicalReads','AvgWrites','TotalWrites','TotalLogicalReads','TotalPhysicalReads','MaxExecutionsPerMin','cpu_ms_per_sec','duration_ms_per_sec') THEN @Measure ELSE NULL END
SELECT @DateAggString = CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin =0 THEN 'DATEADD(mi, @UTCOffset, OES.SnapshotDate)'
		 ELSE 'DG.DateGroup' END
DECLARE @SQL NVARCHAR(MAX)

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')


SET @SQL = N'
WITH agg AS (
	SELECT ' + @DateAggString + N' as SnapshotDate,
		   D.name AS DatabaseName,
		   D.DatabaseID,
		   O.ObjectID,
		   O.SchemaName + ''.'' + O.objectname as object_name,
		   SUM(OES.total_worker_time)/1000000.0 as TotalCPU,
		   SUM(OES.total_worker_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0 as AvgCPU,
		   SUM(total_worker_time)/1000.0 / MAX(SUM(OES.PeriodTime)/1000000.0) OVER() cpu_ms_per_sec,
		   SUM(OES.execution_count) as ExecutionCount,
		   SUM(OES.execution_count)/(NULLIF(SUM(OES.PeriodTime),0)/60000000.0) as ExecutionsPerMin,
		   SUM(OES.total_elapsed_time)/1000000.0 AS TotalDuration,
		   SUM(OES.total_elapsed_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0 AS AvgDuration,
		   SUM(OES.total_elapsed_time)/1000.0 / MAX(SUM(OES.PeriodTime)/1000000.0) OVER() duration_ms_per_sec,
		   SUM(OES.total_logical_reads) as TotalLogicalReads,
		   SUM(OES.total_logical_reads)/NULLIF(SUM(OES.execution_count),0) as AvgLogicalReads,
		   SUM(OES.total_physical_reads) as TotalPhysicalReads,
		   SUM(OES.total_physical_reads)/NULLIF(SUM(OES.execution_count),0) as AvgPhysicalReads,
		   SUM(OES.total_logical_writes) as TotalWrites,
		   SUM(OES.total_logical_writes)/NULLIF(SUM(OES.execution_count),0) as AvgWrites,
		   MAX(MaxExecutionsPerMin) as MaxExecutionsPerMin
	FROM dbo.ObjectExecutionStats' + CASE WHEN @DateGroupingMin>=60 THEN N'_60MIN' ELSE N'' END + N' OES
		' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin =0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, OES.SnapshotDate),@DateGroupingMin) DG' END + '
		JOIN dbo.DBObjects O ON OES.ObjectID = O.ObjectID
		JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
		JOIN dbo.Instances I ON D.InstanceID = I.InstanceID AND OES.InstanceID = I.InstanceID
	WHERE D.IsActive=1
	' + CASE WHEN @Instance IS NOT NULL THEN N'AND I.Instance = @Instance' ELSE '' END + N'
	' + CASE WHEN @InstanceID IS NOT NULL THEN N'AND I.InstanceID = @InstanceID' ELSE '' END + N'
	AND OES.SnapshotDate >= @FromDate 
	AND OES.SnapshotDate< @ToDate
	' + CASE WHEN @DatabaseID IS NULL THEN N'' ELSE N'AND D.DatabaseID=@DatabaseID' END + N'
	' + CASE WHEN @ObjectName IS NULL THEN N'' ELSE N'AND O.objectname=@ObjectName' END + N'
	' + CASE WHEN @ObjectID IS NULL THEN N'' ELSE N'AND OES.ObjectID = @ObjectID' END + N'
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	GROUP BY ' + @DateAggString + N',D.Name,O.objectname,D.DatabaseID,O.SchemaName,O.ObjectID
)
, T AS (
	SELECT agg.*,
		' + @MeasureString + N' as Measure,
		ROW_NUMBER() OVER (PARTITION BY SnapshotDate ORDER BY ' + @MeasureString + N' DESC) ProcRank,
		SUM(' + @MeasureString + N') OVER(PARTITION BY ObjectID) TotalMeasure
	FROM agg
)
SELECT T.*
FROM T
WHERE ProcRank <=20'

IF @Debug=1
BEGIN
	PRINT @SQL
END

IF @SQL IS NOT NULL
BEGIN
	INSERT INTO #results
	(
	    SnapshotDate,
	    DatabaseName,
	    DatabaseID,
	    ObjectID,
	    object_name,
	    TotalCPU,
	    AvgCPU,
	    cpu_ms_per_sec,
	    ExecutionCount,
	    ExecutionsPerMin,
	    TotalDuration,
	    AvgDuration,
	    duration_ms_per_sec,
	    TotalLogicalReads,
	    AvgLogicalReads,
	    TotalPhysicalReads,
	    AvgPhysicalReads,
	    TotalWrites,
	    AvgWrites,
	    MaxExecutionsPerMin,
	    Measure,
	    ProcRank,
	    TotalMeasure
	)
	EXEC sp_executesql @SQL,N'@Instance SYSNAME,
							@DatabaseID INT,
							@FromDate DATETIME2(3),
							@ToDate DATETIME2(3),
							@ObjectName SYSNAME,
							@SchemaName SYSNAME,
							@UTCOffset INT,
							@InstanceID INT,
							@ObjectID BIGINT,
							@DateGroupingMin INT',
							@Instance,
							@DatabaseID,
							@FromDateUTC,
							@ToDateUTC,
							@ObjectName,
							@SchemaName,
							@UTCOffset,
							@InstanceID,
							@ObjectID,
							@DateGroupingMin

END 

IF @ZeroFill=1 AND @ObjectID IS NOT NULL
BEGIN;
	DECLARE @DateGroups TABLE(
		DateGroup DATETIME2(3) NOT NULL PRIMARY KEY
	)
	/*	Get all the date groups where we have data collected for the instance
		We could generate the date groups using a table of numbers which would be more efficient, but this method doesn't generate dates if collections are missing.
		Collections could be missing if @ToDateUTC is a date in the future or the data hasn't been loaded yet. It could also be missing if the agent was stopped or the SQL instance wasn't running.
		It would be wrong to generate zero values in these situations.
	*/
	DECLARE @DateGroupSQL NVARCHAR(MAX)
	SET @DateGroupSQL=N'	
	SELECT ' + CASE WHEN @DateGroupingMin >0 THEN 'DG.DateGroup' ELSE 'DATEADD(mi, @UTCOffset, OES.SnapshotDate)' END + '
	FROM dbo.ObjectExecutionStats' + CASE WHEN @DateGroupingMin>=60 THEN N'_60MIN' ELSE N'' END + N' OES
	' + CASE WHEN @DateGroupingMin >0 THEN 'CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, OES.SnapshotDate),@DateGroupingMin) DG' ELSE '' END + '
	WHERE OES.SnapshotDate>=@FromDateUTC
	AND OES.SnapshotDate < @ToDateUTC
	AND OES.InstanceID = @InstanceID
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	GROUP BY ' + CASE WHEN @DateGroupingMin >0 THEN 'DG.DateGroup' ELSE 'DATEADD(mi, @UTCOffset, OES.SnapshotDate)' END

	IF @Debug=1
	BEGIN
		PRINT @DateGroupSQL
	END

	INSERT INTO @DateGroups
	(
	    DateGroup
	)
	EXEC sp_executesql @DateGroupSQL,
						N'@Instance SYSNAME,
						@DatabaseID INT,
						@FromDateUTC DATETIME2(3),
						@ToDateUTC DATETIME2(3),
						@ObjectName SYSNAME,
						@SchemaName SYSNAME,
						@UTCOffset INT,
						@InstanceID INT,
						@ObjectID BIGINT,
						@DateGroupingMin INT',
						@Instance,
						@DatabaseID,
						@FromDateUTC,
						@ToDateUTC,
						@ObjectName,
						@SchemaName,
						@UTCOffset,
						@InstanceID,
						@ObjectID,
						@DateGroupingMin
	
	INSERT INTO #results
	(
		SnapshotDate,
		DatabaseName,
		DatabaseID,
		ObjectID,
		object_name,
		TotalCPU,
		AvgCPU,
		cpu_ms_per_sec,
		ExecutionCount,
		ExecutionsPerMin,
		TotalDuration,
		AvgDuration,
		duration_ms_per_sec,
		TotalLogicalReads,
		AvgLogicalReads,
		TotalPhysicalReads,
		AvgPhysicalReads,
		TotalWrites,
		AvgWrites,
		MaxExecutionsPerMin,
		Measure,
		ProcRank,
		TotalMeasure
	)
	SELECT DG.DateGroup AS SnapshotDate,
			D.name AS DatabaseName,
			D.DatabaseID,
			O.ObjectID,
			O.SchemaName + '.' + O.ObjectName AS object_name,
			0 AS TotalCPU,
			0 AS AvgCPU,
			0 AS cpu_ms_per_sec,
			0 AS ExecutionCount,
			0 AS ExecutionsPerMin,
			0 AS TotalDuration,
			0 AS AvgDuration,
			0 AS duration_ms_per_sec,
			0 AS TotalLogicalReads,
			0 AS AvgLogicalReads,
			0 AS TotalPhysicalReads,
			0 AS AvgPhysicalReads,
			0 AS TotalWrites,
			0 AS AvgWrites,
			0 AS MaxExecutionsPerMin,
			0 AS Measure,
			0 AS ProcRank,
			0 AS TotalMeasure
	FROM dbo.DBObjects O
	JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
	CROSS JOIN @DateGroups DG
	WHERE O.ObjectID= @ObjectID
	AND NOT EXISTS(SELECT 1 FROM #results R WHERE DG.DateGroup = R.SnapshotDate)
END

SELECT SnapshotDate,
       DatabaseName,
       DatabaseID,
       ObjectID,
       object_name,
       TotalCPU,
       AvgCPU,
       cpu_ms_per_sec,
       ExecutionCount,
       ExecutionsPerMin,
       TotalDuration,
       AvgDuration,
       duration_ms_per_sec,
       TotalLogicalReads,
       AvgLogicalReads,
       TotalPhysicalReads,
       AvgPhysicalReads,
       TotalWrites,
       AvgWrites,
       MaxExecutionsPerMin,
       Measure,
       ProcRank,
       TotalMeasure 
FROM #results
ORDER BY DatabaseName,object_name,SnapshotDate