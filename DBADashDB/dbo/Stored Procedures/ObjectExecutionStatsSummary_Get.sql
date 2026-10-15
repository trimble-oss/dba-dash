CREATE   PROC dbo.ObjectExecutionStatsSummary_Get(
		@FromDate DATETIME2(3),
		@ToDate DATETIME2(4),
		@CompareFrom DATETIME2(3)=NULL,
		@CompareTo DATETIME2(3)=NULL,
		@Instance SYSNAME=NULL,
		@InstanceGroupName SYSNAME=NULL,
		@InstanceID INT=NULL,
		@DatabaseID INT=NULL,
		@Types VARCHAR(200)=NULL, -- 'P,FN,TR,TA,PC,X'
		@Use60MIN BIT=NULL, /* NULL: whole hours from the hourly rollup, part hours from the raw rows (raw rows only for a time of day or weekday filter with a part hour @UTCOffset).  0: raw rows only.  1: hourly rollup only */
		@Use60MINCompare BIT=NULL, /* As @Use60MIN, for the compare period */
		@ObjectID BIGINT=NULL,
		@ObjectName SYSNAME=NULL,
		@Debug BIT=0,
		@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
		@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
		@UTCOffset INT=0, /* Used for filtering on hours & weekday in current timezone */
		@InstanceIDs IDs READONLY
)
AS
SET NOCOUNT ON
SET DATEFIRST 1 /* Start week on Monday */

/*
	Whole hours come from dbo.ObjectExecutionStats_60MIN and only the part hours at either end of the window from
	dbo.ObjectExecutionStats.  An hour in the rollup is exactly the raw rows whose snapshot falls in it, so the two
	together give the raw rows' figures for close to the cost of reading the rollup alone.  Choosing one table for
	the whole window either read every raw row of a long window or, for a window that didn't start on the hour,
	left out the part hour - all of it, for a window that started after the last whole hour.

	Each period is split into three half open ranges by dbo.RollupRanges.  Each range is a seek per instance on the
	clustered key, as in dbo.QueryStats_Get, which has the measurements behind the shape.
*/
DECLARE @HasDaysOfWeek BIT = CASE WHEN EXISTS(SELECT 1 FROM @DaysOfWeek) THEN 1 ELSE 0 END
DECLARE @HasHours BIT = CASE WHEN EXISTS(SELECT 1 FROM @Hours) THEN 1 ELSE 0 END
DECLARE @HasInstanceIDs BIT = CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN 1 ELSE 0 END

CREATE TABLE #Scope (InstanceID INT NOT NULL PRIMARY KEY);

INSERT INTO #Scope (InstanceID)
SELECT I.InstanceID
FROM dbo.Instances I
WHERE (@InstanceID IS NULL OR I.InstanceID = @InstanceID)
AND (@Instance IS NULL OR I.Instance = @Instance)
AND (@InstanceGroupName IS NULL OR I.InstanceGroupName = @InstanceGroupName)
AND (@HasInstanceIDs = 0 OR EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID));

CREATE TABLE #Facts (
	Period TINYINT NOT NULL, /* 0 = base, 1 = compare */
	InstanceID INT NOT NULL,
	ObjectID BIGINT NOT NULL,
	PeriodTime BIGINT NOT NULL,
	total_worker_time BIGINT NOT NULL,
	total_elapsed_time BIGINT NOT NULL,
	total_logical_reads BIGINT NOT NULL,
	total_logical_writes BIGINT NOT NULL,
	total_physical_reads BIGINT NOT NULL,
	execution_count BIGINT NOT NULL,
	MaxExecutionsPerMin DECIMAL(19,6) NULL,
	PRIMARY KEY (Period, ObjectID)
);

DECLARE @Period TINYINT = 0,
		@From DATETIME2(3),
		@To DATETIME2(3),
		@Use BIT,
		@HourFrom DATETIME2(3),
		@HourTo DATETIME2(3)

WHILE @Period <= 1
BEGIN
	SELECT @From = CASE WHEN @Period = 0 THEN @FromDate ELSE @CompareFrom END,
		   @To = CASE WHEN @Period = 0 THEN @ToDate ELSE @CompareTo END,
		   @Use = CASE WHEN @Period = 0 THEN @Use60MIN ELSE @Use60MINCompare END

	/*
		An hourly row is filtered on the local time of its hour's start.  With an offset that isn't a whole number of
		hours every hourly row straddles two local hours, and the one at local midnight two days, so a time of day or
		weekday filter can only be applied exactly to the raw rows.
	*/
	IF @Use IS NULL AND @UTCOffset % 60 <> 0 AND (@HasHours = 1 OR @HasDaysOfWeek = 1)
		SET @Use = 0

	IF @From IS NOT NULL AND @To IS NOT NULL
	BEGIN
		/* @To is exclusive */
		SELECT @HourFrom = HourFrom, @HourTo = HourTo FROM dbo.RollupRanges(@From, @To, @Use)

		IF @Debug = 1
			PRINT CONCAT('Period ', @Period, ': raw [', @From, ', ', @HourFrom, ') hourly [', @HourFrom, ', ', @HourTo, ') raw [', @HourTo, ', ', @To, ')')

		INSERT INTO #Facts (Period, InstanceID, ObjectID, PeriodTime, total_worker_time, total_elapsed_time, total_logical_reads,
							total_logical_writes, total_physical_reads, execution_count, MaxExecutionsPerMin)
		SELECT @Period,
			   F.InstanceID,
			   F.ObjectID,
			   SUM(F.PeriodTime),
			   SUM(F.total_worker_time),
			   SUM(F.total_elapsed_time),
			   SUM(F.total_logical_reads),
			   SUM(F.total_logical_writes),
			   SUM(F.total_physical_reads),
			   SUM(F.execution_count),
			   MAX(F.MaxExecutionsPerMin)
		FROM (/* The part hour at the start of the window, from the raw rows */
			  SELECT R.*
			  FROM #Scope T
			  CROSS APPLY (SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
								  OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
								  OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin
						   FROM dbo.ObjectExecutionStats OES WITH (FORCESEEK)
						   WHERE OES.InstanceID = T.InstanceID
						   AND OES.SnapshotDate >= @From
						   AND OES.SnapshotDate < @HourFrom) R
			  UNION ALL
			  /* The whole hours, from the rollup */
			  SELECT H.*
			  FROM #Scope T
			  CROSS APPLY (SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
								  OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
								  OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin
						   FROM dbo.ObjectExecutionStats_60MIN OES WITH (FORCESEEK)
						   WHERE OES.InstanceID = T.InstanceID
						   AND OES.SnapshotDate >= @HourFrom
						   AND OES.SnapshotDate < @HourTo) H
			  UNION ALL
			  /* The part hour at the end */
			  SELECT R.*
			  FROM #Scope T
			  CROSS APPLY (SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
								  OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
								  OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin
						   FROM dbo.ObjectExecutionStats OES WITH (FORCESEEK)
						   WHERE OES.InstanceID = T.InstanceID
						   AND OES.SnapshotDate >= @HourTo
						   AND OES.SnapshotDate < @To) R) F
		WHERE (@ObjectID IS NULL OR F.ObjectID = @ObjectID)
		AND (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
		AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, F.SnapshotDate))))
		GROUP BY F.InstanceID, F.ObjectID
		OPTION (RECOMPILE);
	END

	SET @Period += 1
END;

WITH agg AS (
	SELECT	F.Period,
			F.InstanceID,
			F.ObjectID,
			I.ConnectionID,
			D.name as DB,
			O.SchemaName,
			O.ObjectName,
			O.ObjectType,
			OT.TypeDescription,
			F.total_elapsed_time/1000000.0 as total_duration_sec,
			F.total_elapsed_time/1000.0 / MAX(F.PeriodTime/1000000.0) OVER(PARTITION BY F.Period) duration_ms_per_sec,
			F.total_elapsed_time/1000000.0/F.execution_count as avg_duration_sec,
			F.execution_count,
			F.execution_count / MAX(F.PeriodTime/60000000.0) OVER(PARTITION BY F.Period) execs_per_min,
			F.total_worker_time/1000000.0 as total_cpu_sec,
			F.total_worker_time/1000.0 / MAX(F.PeriodTime/1000000.0) OVER(PARTITION BY F.Period) cpu_ms_per_sec,
			F.total_worker_time/1000000.0 / F.execution_count as avg_cpu_sec,
			F.total_physical_reads,
			F.total_logical_reads,
			F.total_logical_writes as total_writes,
			F.total_logical_writes/F.execution_count as avg_writes,
			F.total_physical_reads/F.execution_count as avg_physical_reads,
			F.total_logical_reads/F.execution_count as avg_logical_reads,
			MAX(F.PeriodTime/1000000.0) OVER(PARTITION BY F.Period) as period_time_sec,
			F.MaxExecutionsPerMin as max_execs_per_min
	FROM #Facts F
	JOIN dbo.Instances I ON F.InstanceID = I.InstanceID
	JOIN dbo.DBObjects O ON F.ObjectID = O.ObjectID
	JOIN dbo.Databases D ON O.DatabaseID = D.DatabaseID AND D.InstanceID = I.InstanceID
	JOIN dbo.ObjectType OT ON OT.ObjectType = O.ObjectType
	WHERE (@DatabaseID IS NULL OR D.DatabaseID = @DatabaseID)
	AND (@ObjectName IS NULL OR O.ObjectName = @ObjectName)
	AND (@Types IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@Types,',') ss WHERE ss.value = O.ObjectType))
),
base AS (
	SELECT *
	FROM agg
	WHERE Period = 0
),
compare as(
	SELECT InstanceID,
		ObjectID,
		ConnectionID,
		DB,
		SchemaName,
		ObjectName,
		ObjectType,
		TypeDescription,
		total_duration_sec as compare_total_duration_sec,
		duration_ms_per_sec as compare_duration_ms_per_sec,
		avg_duration_sec as compare_avg_duration_sec,
		execution_count as compare_execution_count,
		execs_per_min as compare_execs_per_min,
		total_cpu_sec as compare_total_cpu_sec,
		cpu_ms_per_sec as compare_cpu_ms_per_sec,
		avg_cpu_sec as compare_avg_cpu_sec,
		total_physical_reads as compare_total_physical_reads,
		total_logical_reads as compare_total_logical_reads,
		total_writes as compare_total_writes,
		avg_writes as compare_avg_writes,
		avg_physical_reads as compare_avg_physical_reads,
		avg_logical_reads as compare_avg_logical_reads,
		period_time_sec as compare_period_time_sec,
		max_execs_per_min as compare_max_execs_per_min
	FROM agg
	WHERE Period = 1
)
SELECT ISNULL(base.InstanceID,compare.InstanceID) as InstanceID,
		ISNULL(base.ObjectID,compare.ObjectID) as ObjectID,
		ISNULL(base.ConnectionID,compare.ConnectionID) as ConnectionID,
		ISNULL(base.DB,compare.DB) as DB,
		ISNULL(base.SchemaName,compare.SchemaName) as SchemaName,
		ISNULL(base.ObjectName,compare.ObjectName) as ObjectName,
		ISNULL(base.ObjectType,compare.ObjectType) as ObjectType,
		ISNULL(base.TypeDescription,compare.TypeDescription) as TypeDescription,
		total_duration_sec,
		duration_ms_per_sec,
		avg_duration_sec,
		execution_count,
		execs_per_min,
		max_execs_per_min,
		total_cpu_sec,
		cpu_ms_per_sec,
		avg_cpu_sec,
		total_physical_reads,
		total_logical_reads,
		total_writes,
		avg_writes,
		avg_physical_reads,
		avg_logical_reads,
		period_time_sec,
		compare_total_duration_sec,
		compare_duration_ms_per_sec,
		compare_avg_duration_sec,
		compare_execution_count,
		compare_execs_per_min,
		compare_max_execs_per_min,
		compare_total_cpu_sec,
		compare_cpu_ms_per_sec,
		compare_avg_cpu_sec,
		compare_total_physical_reads,
		compare_total_logical_reads,
		compare_total_writes,
		compare_avg_writes,
		compare_avg_physical_reads,
		compare_avg_logical_reads,
		compare_period_time_sec,
		(avg_duration_sec-compare_avg_duration_sec)/compare_avg_duration_sec as diff_avg_duration_pct,
		(avg_cpu_sec-compare_avg_cpu_sec)/compare_avg_cpu_sec as diff_avg_cpu_pct
FROM base
FULL JOIN compare on base.ObjectID = compare.ObjectID
