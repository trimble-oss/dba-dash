CREATE PROC dbo.ObjectExecutionStats_Get(
	@Instance SYSNAME=NULL,
	@InstanceGroupName SYSNAME=NULL,
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
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
	@Top INT=10,
	@IncludeOther BIT=1
)
AS
SET NOCOUNT ON
SET DATEFIRST 1 /* Start week on Monday */
CREATE TABLE #results(
    SnapshotDate DATETIME,
    DatabaseName NVARCHAR(128),
    DatabaseID INT,
	ObjectID BIGINT,
    object_name NVARCHAR(257),
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
	TotalMeasure DECIMAL(29, 9),
    ProcRankPeriod BIGINT,
	ProcRankTotal BIGINT
	
);

IF @Instance IS NULL AND @InstanceID IS NULL AND @InstanceGroupName IS NULL
BEGIN
	RAISERROR('Instance not specified',11,1);
	RETURN;
END

IF @FromDateUTC IS NULL
	SET @FromDateUTC = CONVERT(DATETIME,STUFF(CONVERT(VARCHAR,DATEADD(mi,-120,GETUTCDATE()),120),16,4,'0:00'),120) 
IF @ToDateUTC IS NULL
	SET @ToDateUTC = GETUTCDATE()

DECLARE @MeasureString NVARCHAR(MAX) 
SELECT @MeasureString = CASE WHEN @Measure IN('TotalCPU','AvgCPU','TotalDuration','AvgDuration','ExecutionCount','ExecutionsPerMin','AvgLogicalReads','AvgPhysicalReads','AvgWrites','TotalWrites','TotalLogicalReads','TotalPhysicalReads','MaxExecutionsPerMin','cpu_ms_per_sec','duration_ms_per_sec') 
						THEN @Measure ELSE NULL END

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

DECLARE @MeasureCol NVARCHAR(MAX) = CASE WHEN @MeasureString = 'TotalDuration' THEN 'SUM(OES.total_elapsed_time)/1000000.0' 
					WHEN @MeasureString = 'TotalCPU' THEN 'SUM(OES.total_worker_time)/1000000.0 '
					WHEN @MeasureString = 'ExecutionCount' THEN 'SUM(OES.execution_count)'
					WHEN @MeasureString = 'AvgCPU' THEN 'SUM(OES.total_worker_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0'
					WHEN @MeasureString = 'AvgDuration' THEN 'SUM(OES.total_elapsed_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0'
					WHEN @MeasureString = 'ExecutionsPerMin' THEN 'SUM(OES.execution_count) / NULLIF(MAX(SUM(OES.PeriodTime)/60000000.0) OVER(PARTITION BY DateGroup),0)'
					WHEN @MeasureString = 'AvgLogicalReads' THEN 'SUM(OES.total_logical_reads)/NULLIF(SUM(OES.execution_count),0)'
					WHEN @MeasureString = 'AvgPhysicalReads' THEN 'SUM(OES.total_physical_reads)/NULLIF(SUM(OES.execution_count),0) '
					WHEN @MeasureString = 'AvgWrites' THEN 'SUM(OES.total_logical_writes)/NULLIF(SUM(OES.execution_count),0) '
					WHEN @MeasureString = 'TotalWrites' THEN 'SUM(OES.total_logical_writes)'
					WHEN @MeasureString = 'TotalLogicalReads' THEN 'SUM(OES.total_logical_reads)'
					WHEN @MeasureString = 'TotalPhysicalReads' THEN 'SUM(OES.total_physical_reads)'
					WHEN @MeasureString = 'MaxExecutionsPerMin' THEN 'MAX(MaxExecutionsPerMin)'
					WHEN @MeasureString = 'cpu_ms_per_sec' THEN 'SUM(total_worker_time)/1000.0 / NULLIF(MAX(SUM(OES.PeriodTime)/1000000.0) OVER(PARTITION BY DateGroup),0)' 
					WHEN @MeasureString = 'duration_ms_per_sec' THEN 'SUM(OES.total_elapsed_time)/1000.0 / NULLIF(MAX(SUM(OES.PeriodTime)/1000000.0) OVER(PARTITION BY DateGroup),0)' 
		   END + ' AS Measure,'

DECLARE @TotalMeasureCol NVARCHAR(MAX) = CASE WHEN @MeasureString = 'TotalDuration' THEN 'SUM(SUM(OES.total_elapsed_time)) OVER(PARTITION BY OES.ObjectID)/1000000.0 ' 
					WHEN @MeasureString = 'TotalCPU' THEN 'SUM(SUM(OES.total_worker_time)) OVER(PARTITION BY OES.ObjectID) /1000000.0 '
					WHEN @MeasureString = 'ExecutionCount' THEN 'SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID) '
					WHEN @MeasureString = 'AvgCPU' THEN 'SUM(SUM(OES.total_worker_time)) OVER(PARTITION BY OES.ObjectID)/NULLIF(SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID),0)/1000000.0'
					WHEN @MeasureString = 'AvgDuration' THEN 'SUM(SUM(OES.total_elapsed_time)) OVER(PARTITION BY OES.ObjectID)/NULLIF(SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID),0)/1000000.0 '
					WHEN @MeasureString = 'ExecutionsPerMin' THEN 'SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID) * 60.0 / NULLIF(DATEDIFF_BIG(s,MIN(MIN(PeriodStartTime)) OVER(),MAX(MAX(PeriodEndTime)) OVER()),0) '
					WHEN @MeasureString = 'AvgLogicalReads' THEN 'SUM(SUM(OES.total_logical_reads)) OVER(PARTITION BY OES.ObjectID) /NULLIF(SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID),0) '
					WHEN @MeasureString = 'AvgPhysicalReads' THEN 'SUM(SUM(OES.total_physical_reads)) OVER(PARTITION BY OES.ObjectID) /NULLIF(SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID),0) '
					WHEN @MeasureString = 'AvgWrites' THEN 'SUM(SUM(OES.total_logical_writes)) OVER(PARTITION BY OES.ObjectID) /NULLIF(SUM(SUM(OES.execution_count)) OVER(PARTITION BY OES.ObjectID),0) '
					WHEN @MeasureString = 'TotalWrites' THEN 'SUM(SUM(OES.total_logical_writes)) OVER(PARTITION BY OES.ObjectID)'
					WHEN @MeasureString = 'TotalLogicalReads' THEN 'SUM(SUM(OES.total_logical_reads)) OVER(PARTITION BY OES.ObjectID)'
					WHEN @MeasureString = 'TotalPhysicalReads' THEN 'SUM(SUM(OES.total_physical_reads)) OVER(PARTITION BY OES.ObjectID)'
					WHEN @MeasureString = 'MaxExecutionsPerMin' THEN 'MAX(MAX(OES.MaxExecutionsPerMin)) OVER(PARTITION BY OES.ObjectID)'
					WHEN @MeasureString = 'cpu_ms_per_sec' THEN 'SUM(SUM(OES.total_worker_time)) OVER(PARTITION BY OES.ObjectID) /1000.0 / NULLIF(DATEDIFF_BIG(s,MIN(MIN(PeriodStartTime)) OVER(),MAX(MAX(PeriodEndTime)) OVER()),0)' 
					WHEN @MeasureString = 'duration_ms_per_sec' THEN 'SUM(SUM(OES.total_elapsed_time)) OVER(PARTITION BY OES.ObjectID) /1000.0 / NULLIF(DATEDIFF_BIG(s,MIN(MIN(PeriodStartTime)) OVER(),MAX(MAX(PeriodEndTime)) OVER()),0)' 
		   END + ' AS TotalMeasure,'

SET @SQL = CONCAT(N'
WITH agg AS (
	/* Initial Aggregation by time period & object */
	SELECT DG.DateGroup,
		   D.name AS DatabaseName,
		   D.DatabaseID,
		   OES.ObjectID,
		   O.SchemaName + ''.'' + O.objectname as object_name,
		   ', @MeasureCol, '
		   ', @TotalMeasureCol, '
		   SUM(OES.total_elapsed_time) AS total_elapsed_time,
		   SUM(OES.execution_count) AS execution_count,
		   SUM(OES.total_logical_reads) AS total_logical_reads,
		   SUM(OES.total_logical_writes) AS total_logical_writes,
		   SUM(OES.total_physical_reads) AS total_physical_reads,
		   SUM(OES.total_worker_time) AS total_worker_time,
		   SUM(OES.PeriodTime) AS PeriodTime,
		   MAX(MaxExecutionsPerMin) AS MaxExecutionsPerMin,
		   MIN(PeriodStartTime) AS PeriodStartTime,
		   MAX(PeriodEndTime) AS PeriodEndTime
	FROM dbo.ObjectExecutionStats' + CASE WHEN @DateGroupingMin>=60 THEN N'_60MIN' ELSE N'' END + N' OES
		' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin =0 THEN 'OUTER APPLY(SELECT DATEADD(mi, @UTCOffset, OES.SnapshotDate) AS DateGroup) DG' ELSE 'CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, OES.SnapshotDate),@DateGroupingMin) DG' END + '
		JOIN dbo.DBObjects O ON OES.ObjectID = O.ObjectID
		JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
		JOIN dbo.Instances I ON D.InstanceID = I.InstanceID AND OES.InstanceID = I.InstanceID
	WHERE D.IsActive=1
	', CASE WHEN @Instance IS NOT NULL THEN N'AND I.Instance = @Instance' ELSE '' END, N'
	', CASE WHEN @InstanceGroupName IS NOT NULL THEN N'AND I.InstanceGroupName = @InstanceGroupName' ELSE '' END, N'
	', CASE WHEN @InstanceID IS NOT NULL THEN N'AND I.InstanceID = @InstanceID' ELSE '' END, N'
	AND OES.SnapshotDate >= @FromDate 
	AND OES.SnapshotDate< @ToDate
	', CASE WHEN @DatabaseID IS NULL THEN N'' ELSE N'AND D.DatabaseID=@DatabaseID' END, N'
	', CASE WHEN @ObjectName IS NULL THEN N'' ELSE N'AND O.objectname=@ObjectName' END, N'
	', CASE WHEN @ObjectID IS NULL THEN N'' ELSE N'AND OES.ObjectID = @ObjectID' END, N'
	', CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END, '
	', CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN(' + @HoursCsv + ')' END, '
	GROUP BY DG.DateGroup,D.Name,O.objectname,D.DatabaseID,O.SchemaName,OES.ObjectID
)
, Ranking AS (
	/* Get top objects for each period & overall */
	SELECT agg.*,
		ROW_NUMBER() OVER (PARTITION BY DateGroup ORDER BY Measure DESC,ObjectID) ProcRankPeriod,
		DENSE_RANK() OVER (ORDER BY TotalMeasure DESC,ObjectID) ProcRankTotal
	FROM agg
	WHERE TotalMeasure>0
),
ReGrouping AS (
	/* Bucket objects outside top into "Other" category */
	SELECT  DateGroup,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN DatabaseName ELSE '''' END AS DatabaseName,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN DatabaseID ELSE -1 END AS DatabaseID,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN ObjectID ELSE -1 END AS ObjectID,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN object_name ELSE ''{Other}'' END AS object_name,
			SUM(total_elapsed_time) AS total_elapsed_time,
			SUM(execution_count) AS execution_count,
			SUM(total_logical_reads) AS total_logical_reads,
			SUM(total_logical_writes) AS total_logical_writes,
			SUM(total_physical_reads) AS total_physical_reads,
			SUM(total_worker_time) AS total_worker_time,
			MAX(PeriodTime) AS PeriodTime,
			MAX(MaxExecutionsPerMin) AS MaxExecutionsPerMin,
			MIN(PeriodStartTime) AS PeriodStartTime,
			MAX(PeriodEndTime) AS PeriodEndTime,
			MIN(CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN ProcRankPeriod ELSE 2147483647 END) AS ProcRankPeriod,
			MIN(CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN ProcRankTotal ELSE 2147483647 END) AS ProcRankTotal
	FROM Ranking
	GROUP BY  DateGroup,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN DatabaseName ELSE '''' END,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN DatabaseID ELSE -1 END,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN ObjectID ELSE -1 END,
			CASE WHEN ProcRankTotal <=@Top OR ProcRankPeriod<=@Top THEN object_name ELSE ''{Other}'' END
)
SELECT DateGroup AS SnapshotDate,
		DatabaseName,
		DatabaseID,
		ObjectID,
		object_name,
		SUM(OES.total_worker_time)/1000000.0 as TotalCPU,
		SUM(OES.total_worker_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0 as AvgCPU,
		SUM(total_worker_time)/1000.0 / NULLIF(MAX(SUM(OES.PeriodTime)/1000000.0) OVER(PARTITION BY DateGroup),0) cpu_ms_per_sec,
		SUM(OES.execution_count) as ExecutionCount,
		SUM(OES.execution_count)/ NULLIF(MAX(SUM(OES.PeriodTime)/60000000.0) OVER(PARTITION BY DateGroup),0) as ExecutionsPerMin,
		SUM(OES.total_elapsed_time)/1000000.0 AS TotalDuration,
		SUM(OES.total_elapsed_time)/NULLIF(SUM(OES.execution_count),0)/1000000.0 AS AvgDuration,
		SUM(OES.total_elapsed_time)/1000.0 / NULLIF(MAX(SUM(OES.PeriodTime)/1000000.0) OVER(PARTITION BY DateGroup),0) duration_ms_per_sec,
		SUM(OES.total_logical_reads) as TotalLogicalReads,
		SUM(OES.total_logical_reads)/NULLIF(SUM(OES.execution_count),0) as AvgLogicalReads,
		SUM(OES.total_physical_reads) as TotalPhysicalReads,
		SUM(OES.total_physical_reads)/NULLIF(SUM(OES.execution_count),0) as AvgPhysicalReads,
		SUM(OES.total_logical_writes) as TotalWrites,
		SUM(OES.total_logical_writes)/NULLIF(SUM(OES.execution_count),0) as AvgWrites,
		MAX(MaxExecutionsPerMin) as MaxExecutionsPerMin,
		', @MeasureCol,'
		', @TotalMeasureCol,'
		MIN(ProcRankPeriod) AS ProcRankPeriod,
		MIN(ProcRankTotal) AS ProcRankTotal
FROM ReGrouping OES
',CASE WHEN @IncludeOther=1 THEN '' ELSE 'WHERE ProcRankTotal <> 2147483647 ' END,'
GROUP BY DateGroup,
		DatabaseName,
		DatabaseID,
		ObjectID,
		object_name
ORDER BY ProcRankTotal,SnapshotDate')

IF @Debug=1
BEGIN
	EXEC dbo.PrintMax @SQL
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
	    TotalMeasure,
		ProcRankPeriod,
		ProcRankTotal
	)
	EXEC sp_executesql @SQL,N'@Instance SYSNAME,
							@InstanceGroupName SYSNAME,
							@DatabaseID INT,
							@FromDate DATETIME2(3),
							@ToDate DATETIME2(3),
							@ObjectName SYSNAME,
							@SchemaName SYSNAME,
							@UTCOffset INT,
							@InstanceID INT,
							@ObjectID BIGINT,
							@DateGroupingMin INT,
							@Top INT',
							@Instance,
							@InstanceGroupName,
							@DatabaseID,
							@FromDateUTC,
							@ToDateUTC,
							@ObjectName,
							@SchemaName,
							@UTCOffset,
							@InstanceID,
							@ObjectID,
							@DateGroupingMin,
							@Top

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
						@InstanceGroupName SYSNAME,
						@DatabaseID INT,
						@FromDateUTC DATETIME2(3),
						@ToDateUTC DATETIME2(3),
						@ObjectName SYSNAME,
						@SchemaName SYSNAME,
						@UTCOffset INT,
						@InstanceID INT,
						@ObjectID BIGINT,
						@DateGroupingMin INT,
						@Top INT',
						@Instance,
						@InstanceGroupName,
						@DatabaseID,
						@FromDateUTC,
						@ToDateUTC,
						@ObjectName,
						@SchemaName,
						@UTCOffset,
						@InstanceID,
						@ObjectID,
						@DateGroupingMin,
						@Top
	
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
		TotalMeasure,
		ProcRankPeriod,
		ProcRankTotal
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
			0 AS TotalMeasure,
			0 AS ProcRankPeriod,
			0 AS ProcRankTotal
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
       TotalMeasure,
	   ProcRankPeriod,
	   ProcRankTotal
FROM #results
ORDER BY ProcRankTotal,DatabaseName,object_name,SnapshotDate