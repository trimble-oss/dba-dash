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
    DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT,
    DatabaseID INT,
	ObjectID BIGINT,
    object_name NVARCHAR(257) COLLATE DATABASE_DEFAULT,
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

/*
	The work is staged through temp tables rather than done in one statement.  Instances can have tens of thousands of
	objects, and a single statement grouped millions of rows by name strings and computed the per-period and per-object
	totals with windowed aggregates, which SQL Server runs through segment spools and large sorts that spill to tempdb.
	Here each stage groups by integer keys only, the totals are plain GROUP BYs, and names are joined only to the
	rows that are returned.
*/
CREATE TABLE #agg(
	DateGroup DATETIME2(7) NOT NULL,
	ObjectID BIGINT NOT NULL,
	total_elapsed_time BIGINT NOT NULL,
	execution_count BIGINT NOT NULL,
	total_logical_reads BIGINT NOT NULL,
	total_logical_writes BIGINT NOT NULL,
	total_physical_reads BIGINT NOT NULL,
	total_worker_time BIGINT NOT NULL,
	PeriodTime BIGINT NOT NULL,
	MaxExecutionsPerMin DECIMAL(38,9) NULL,
	PeriodStartTime DATETIME2(7) NULL,
	PeriodEndTime DATETIME2(3) NOT NULL
);

IF @MeasureString IS NOT NULL
BEGIN
	/*
		Whole hours come from dbo.ObjectExecutionStats_60MIN and only the part hours at either end of the window from
		dbo.ObjectExecutionStats, as in dbo.ObjectExecutionStatsSummary_Get.  The rollup is only used where the chart
		groups by an hour or more - a finer grouping needs the raw rows throughout.  An hourly row is filtered on the
		local time of its hour's start, so with an offset that isn't a whole number of hours a time of day or weekday
		filter can only be applied exactly to the raw rows.
	*/
	DECLARE @HasDaysOfWeek BIT = CASE WHEN EXISTS(SELECT 1 FROM @DaysOfWeek) THEN 1 ELSE 0 END
	DECLARE @HasHours BIT = CASE WHEN EXISTS(SELECT 1 FROM @Hours) THEN 1 ELSE 0 END
	DECLARE @Use60MIN BIT = CASE WHEN @DateGroupingMin >= 60 AND NOT (@UTCOffset % 60 <> 0 AND (@HasHours = 1 OR @HasDaysOfWeek = 1)) THEN NULL ELSE 0 END
	DECLARE @HourFrom DATETIME2(3), @HourTo DATETIME2(3)
	DECLARE @Mins INT = ISNULL(@DateGroupingMin,0)

	SELECT @HourFrom = HourFrom, @HourTo = HourTo FROM dbo.RollupRanges(@FromDateUTC, @ToDateUTC, @Use60MIN)

	IF @Debug = 1
		PRINT CONCAT('raw [', @FromDateUTC, ', ', @HourFrom, ') hourly [', @HourFrom, ', ', @HourTo, ') raw [', @HourTo, ', ', @ToDateUTC, ')')

	CREATE TABLE #Scope (InstanceID INT NOT NULL PRIMARY KEY);

	INSERT INTO #Scope (InstanceID)
	SELECT I.InstanceID
	FROM dbo.Instances I
	WHERE (@InstanceID IS NULL OR I.InstanceID = @InstanceID)
	AND (@Instance IS NULL OR I.Instance = @Instance)
	AND (@InstanceGroupName IS NULL OR I.InstanceGroupName = @InstanceGroupName)

	DECLARE @SingleInstanceID INT = CASE WHEN (SELECT COUNT(*) FROM #Scope) = 1 THEN (SELECT InstanceID FROM #Scope) END

	/*
		A database or object name filter narrows the objects to a short list, which the stats rows are checked against
		as they are read.  Otherwise the list would be every object on the instance - hundreds of thousands on some,
		most of which never ran - so the rows are aggregated first and objects in inactive databases removed after.
	*/
	DECLARE @FilterObjects BIT = CASE WHEN @DatabaseID IS NOT NULL OR @ObjectName IS NOT NULL THEN 1 ELSE 0 END

	CREATE TABLE #Objects (ObjectID BIGINT NOT NULL PRIMARY KEY);

	IF @FilterObjects = 1
	BEGIN
		INSERT INTO #Objects (ObjectID)
		SELECT O.ObjectID
		FROM #Scope T
		JOIN dbo.Databases D ON D.InstanceID = T.InstanceID
		JOIN dbo.DBObjects O ON O.DatabaseID = D.DatabaseID
		WHERE D.IsActive = 1
		AND (@DatabaseID IS NULL OR D.DatabaseID = @DatabaseID)
		AND (@ObjectName IS NULL OR O.ObjectName = @ObjectName)
		AND (@ObjectID IS NULL OR O.ObjectID = @ObjectID)
		OPTION (RECOMPILE)
	END

	/* Initial aggregation by time period & object */
	INSERT INTO #agg WITH (TABLOCK)
	(
		DateGroup,
		ObjectID,
		total_elapsed_time,
		execution_count,
		total_logical_reads,
		total_logical_writes,
		total_physical_reads,
		total_worker_time,
		PeriodTime,
		MaxExecutionsPerMin,
		PeriodStartTime,
		PeriodEndTime
	)
	SELECT DG.DateGroup,
		   F.ObjectID,
		   SUM(F.total_elapsed_time),
		   SUM(F.execution_count),
		   SUM(F.total_logical_reads),
		   SUM(F.total_logical_writes),
		   SUM(F.total_physical_reads),
		   SUM(F.total_worker_time),
		   SUM(F.PeriodTime),
		   MAX(F.MaxExecutionsPerMin),
		   MIN(F.PeriodStartTime),
		   MAX(F.PeriodEndTime)
	FROM (/* A single instance is read directly, so each seek can be a parallel range scan */
		  SELECT F.*
		  FROM dbo.ObjectExecutionStatsBetweenDates(@SingleInstanceID, @FromDateUTC, @HourFrom, @HourTo, @ToDateUTC) F
		  WHERE @SingleInstanceID IS NOT NULL
		  UNION ALL
		  SELECT F.*
		  FROM #Scope T
		  CROSS APPLY dbo.ObjectExecutionStatsBetweenDates(T.InstanceID, @FromDateUTC, @HourFrom, @HourTo, @ToDateUTC) F
		  WHERE @SingleInstanceID IS NULL) F
	CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, F.SnapshotDate),@Mins) DG
	WHERE (@FilterObjects = 0 OR EXISTS(SELECT 1 FROM #Objects O WHERE O.ObjectID = F.ObjectID))
	AND (@ObjectID IS NULL OR F.ObjectID = @ObjectID)
	AND (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
	AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
	GROUP BY DG.DateGroup, F.ObjectID
	OPTION (RECOMPILE);

	IF @FilterObjects = 0
	BEGIN
		/* Objects in inactive databases.  Checked once per object rather than once per row */
		DELETE A
		FROM #agg A
		WHERE A.ObjectID IN (
			SELECT X.ObjectID
			FROM (SELECT DISTINCT ObjectID FROM #agg) X
			WHERE NOT EXISTS(
				SELECT 1
				FROM dbo.DBObjects O
				JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
				JOIN #Scope T ON T.InstanceID = D.InstanceID
				WHERE O.ObjectID = X.ObjectID
				AND D.IsActive = 1
				)
			)
		OPTION (RECOMPILE);
	END

	/* The longest period time of any object in each time period, which per second/minute measures divide by */
	CREATE TABLE #period(
		DateGroup DATETIME2(7) NOT NULL PRIMARY KEY,
		PeriodTime BIGINT NOT NULL
	);
	INSERT INTO #period(DateGroup,PeriodTime)
	SELECT DateGroup,MAX(PeriodTime)
	FROM #agg
	GROUP BY DateGroup

	DECLARE @PeriodSeconds BIGINT
	SELECT @PeriodSeconds = NULLIF(DATEDIFF_BIG(s,MIN(PeriodStartTime),MAX(PeriodEndTime)),0)
	FROM #agg

	/* Totals for each object over the whole period.  Only objects with a positive total are ranked & returned */
	CREATE TABLE #obj(
		ObjectID BIGINT NOT NULL,
		TotalMeasure DECIMAL(38,9) NOT NULL,
		ProcRankTotal BIGINT NOT NULL
	);
	INSERT INTO #obj WITH (TABLOCK) (ObjectID,TotalMeasure,ProcRankTotal)
	SELECT ObjectID,
		TotalMeasure,
		DENSE_RANK() OVER (ORDER BY TotalMeasure DESC,ObjectID)
	FROM (
		SELECT ObjectID,
			CASE @MeasureString
				WHEN 'TotalDuration' THEN SUM(total_elapsed_time)/1000000.0
				WHEN 'TotalCPU' THEN SUM(total_worker_time)/1000000.0
				WHEN 'ExecutionCount' THEN SUM(execution_count)
				WHEN 'AvgCPU' THEN SUM(total_worker_time)/NULLIF(SUM(execution_count),0)/1000000.0
				WHEN 'AvgDuration' THEN SUM(total_elapsed_time)/NULLIF(SUM(execution_count),0)/1000000.0
				WHEN 'ExecutionsPerMin' THEN SUM(execution_count) * 60.0 / @PeriodSeconds
				WHEN 'AvgLogicalReads' THEN SUM(total_logical_reads)/NULLIF(SUM(execution_count),0)
				WHEN 'AvgPhysicalReads' THEN SUM(total_physical_reads)/NULLIF(SUM(execution_count),0)
				WHEN 'AvgWrites' THEN SUM(total_logical_writes)/NULLIF(SUM(execution_count),0)
				WHEN 'TotalWrites' THEN SUM(total_logical_writes)
				WHEN 'TotalLogicalReads' THEN SUM(total_logical_reads)
				WHEN 'TotalPhysicalReads' THEN SUM(total_physical_reads)
				WHEN 'MaxExecutionsPerMin' THEN MAX(MaxExecutionsPerMin)
				WHEN 'cpu_ms_per_sec' THEN SUM(total_worker_time)/1000.0 / @PeriodSeconds
				WHEN 'duration_ms_per_sec' THEN SUM(total_elapsed_time)/1000.0 / @PeriodSeconds
			END AS TotalMeasure
		FROM #agg
		GROUP BY ObjectID
	) T
	WHERE TotalMeasure > 0;

	/* Totals for each period & object in the top, and for "Other" */
	CREATE TABLE #regroup(
		DateGroup DATETIME2(7) NOT NULL,
		ObjectID BIGINT NOT NULL,
		total_elapsed_time BIGINT NOT NULL,
		execution_count BIGINT NOT NULL,
		total_logical_reads BIGINT NOT NULL,
		total_logical_writes BIGINT NOT NULL,
		total_physical_reads BIGINT NOT NULL,
		total_worker_time BIGINT NOT NULL,
		PeriodTime BIGINT NOT NULL,
		MaxExecutionsPerMin DECIMAL(38,9) NULL,
		PeriodStartTime DATETIME2(7) NULL,
		PeriodEndTime DATETIME2(3) NOT NULL,
		ProcRankPeriod BIGINT NOT NULL,
		ProcRankTotal BIGINT NOT NULL
	);

	/*
		Rank objects within each period on a narrow row, keeping only the rows that stay out of "Other": the top in the
		period and the top overall.  Sorting every column of #agg for the ranking spilled to tempdb.
	*/
	CREATE TABLE #kept(
		DateGroup DATETIME2(7) NOT NULL,
		ObjectID BIGINT NOT NULL,
		ProcRankPeriod BIGINT NOT NULL,
		PRIMARY KEY(DateGroup,ObjectID)
	);

	WITH Ranking AS (
		SELECT A.DateGroup,
			A.ObjectID,
			T.ProcRankTotal,
			ROW_NUMBER() OVER (PARTITION BY A.DateGroup
								ORDER BY CASE @MeasureString
											WHEN 'TotalDuration' THEN A.total_elapsed_time/1000000.0
											WHEN 'TotalCPU' THEN A.total_worker_time/1000000.0
											WHEN 'ExecutionCount' THEN A.execution_count
											WHEN 'AvgCPU' THEN A.total_worker_time/NULLIF(A.execution_count,0)/1000000.0
											WHEN 'AvgDuration' THEN A.total_elapsed_time/NULLIF(A.execution_count,0)/1000000.0
											WHEN 'ExecutionsPerMin' THEN A.execution_count / NULLIF(P.PeriodTime/60000000.0,0)
											WHEN 'AvgLogicalReads' THEN A.total_logical_reads/NULLIF(A.execution_count,0)
											WHEN 'AvgPhysicalReads' THEN A.total_physical_reads/NULLIF(A.execution_count,0)
											WHEN 'AvgWrites' THEN A.total_logical_writes/NULLIF(A.execution_count,0)
											WHEN 'TotalWrites' THEN A.total_logical_writes
											WHEN 'TotalLogicalReads' THEN A.total_logical_reads
											WHEN 'TotalPhysicalReads' THEN A.total_physical_reads
											WHEN 'MaxExecutionsPerMin' THEN A.MaxExecutionsPerMin
											WHEN 'cpu_ms_per_sec' THEN A.total_worker_time/1000.0 / NULLIF(P.PeriodTime/1000000.0,0)
											WHEN 'duration_ms_per_sec' THEN A.total_elapsed_time/1000.0 / NULLIF(P.PeriodTime/1000000.0,0)
										END DESC,
										A.ObjectID) AS ProcRankPeriod
		FROM #agg A
		JOIN #obj T ON T.ObjectID = A.ObjectID
		JOIN #period P ON P.DateGroup = A.DateGroup
	)
	INSERT INTO #kept(DateGroup,ObjectID,ProcRankPeriod)
	SELECT DateGroup,ObjectID,ProcRankPeriod
	FROM Ranking
	WHERE ProcRankTotal <= @Top OR ProcRankPeriod <= @Top

	/* Bucket objects outside the top into "Other" (ObjectID -1) */
	INSERT INTO #regroup
	(
		DateGroup,
		ObjectID,
		total_elapsed_time,
		execution_count,
		total_logical_reads,
		total_logical_writes,
		total_physical_reads,
		total_worker_time,
		PeriodTime,
		MaxExecutionsPerMin,
		PeriodStartTime,
		PeriodEndTime,
		ProcRankPeriod,
		ProcRankTotal
	)
	SELECT A.DateGroup,
			ISNULL(K.ObjectID,-1),
			SUM(A.total_elapsed_time),
			SUM(A.execution_count),
			SUM(A.total_logical_reads),
			SUM(A.total_logical_writes),
			SUM(A.total_physical_reads),
			SUM(A.total_worker_time),
			MAX(A.PeriodTime),
			MAX(A.MaxExecutionsPerMin),
			MIN(A.PeriodStartTime),
			MAX(A.PeriodEndTime),
			MIN(ISNULL(K.ProcRankPeriod,2147483647)),
			MIN(CASE WHEN K.ObjectID IS NULL THEN 2147483647 ELSE T.ProcRankTotal END)
	FROM #agg A
	JOIN #obj T ON T.ObjectID = A.ObjectID
	LEFT JOIN #kept K ON K.DateGroup = A.DateGroup AND K.ObjectID = A.ObjectID
	WHERE @IncludeOther=1 OR K.ObjectID IS NOT NULL
	GROUP BY A.DateGroup,
			ISNULL(K.ObjectID,-1)

	/* Return the results with names, calculating the measures over the "Other" bucket as a whole */
	SET @SQL = CONCAT(N'
	SELECT OES.DateGroup AS SnapshotDate,
			ISNULL(D.name,'''') AS DatabaseName,
			ISNULL(D.DatabaseID,-1) AS DatabaseID,
			OES.ObjectID,
			ISNULL(O.SchemaName + ''.'' + O.objectname,''{Other}'') AS object_name,
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
	FROM #regroup OES
	LEFT JOIN dbo.DBObjects O ON O.ObjectID = OES.ObjectID
	LEFT JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
	GROUP BY OES.DateGroup,
			D.name,
			D.DatabaseID,
			OES.ObjectID,
			O.SchemaName,
			O.objectname')

	IF @Debug=1
	BEGIN
		EXEC dbo.PrintMax @SQL
	END

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
	EXEC sp_executesql @SQL
END

/* #Scope and the ranges are set above, which only runs for a valid measure */
IF @ZeroFill=1 AND @ObjectID IS NOT NULL AND @MeasureString IS NOT NULL
BEGIN;
	DECLARE @DateGroups TABLE(
		DateGroup DATETIME2(3) NOT NULL PRIMARY KEY
	)
	/*	Get all the date groups where we have data collected for the instance
		We could generate the date groups using a table of numbers which would be more efficient, but this method doesn't generate dates if collections are missing.
		Collections could be missing if @ToDateUTC is a date in the future or the data hasn't been loaded yet. It could also be missing if the agent was stopped or the SQL instance wasn't running.
		It would be wrong to generate zero values in these situations.
	*/
	/*
		Read the same rollup & raw ranges as the data, so a part hour at either end of the window gets its zero too.  Only
		the object's own instance - another instance in the scope collecting while it wasn't would make zeros of a gap.
	*/
	DECLARE @ObjectInstanceID INT = (SELECT D.InstanceID
									 FROM dbo.DBObjects O
									 JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
									 WHERE O.ObjectID = @ObjectID
									 AND EXISTS(SELECT 1 FROM #Scope T WHERE T.InstanceID = D.InstanceID))

	INSERT INTO @DateGroups
	(
	    DateGroup
	)
	SELECT DG.DateGroup
	FROM dbo.ObjectExecutionStatsBetweenDates(@ObjectInstanceID, @FromDateUTC, @HourFrom, @HourTo, @ToDateUTC) F
	CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, F.SnapshotDate),@Mins) DG
	WHERE (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
	AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
	GROUP BY DG.DateGroup
	OPTION (RECOMPILE);

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
			1 AS ProcRankPeriod,
			1 AS ProcRankTotal
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