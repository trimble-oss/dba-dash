CREATE   PROC dbo.PerformanceCounters_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(7),
	@PerformanceCounters dbo.PerformanceCounters READONLY,
	@Internal BIT=0
)
AS
SET XACT_ABORT ON
DECLARE @Ref NVARCHAR(128)='PerformanceCounters'

/* Insert any new performance counters into dbo.Counters table */
INSERT INTO dbo.Counters
(
    object_name,
    counter_name,
    instance_name
)
SELECT	RTRIM(object_name),
		RTRIM(counter_name),
		RTRIM(instance_name) 
FROM @PerformanceCounters
WHERE cntr_type <> 1073939712 /* PERF_LARGE_RAW_BASE.  Excluding this counter as it's just used in calculations for another counter */
EXCEPT 
SELECT	object_name,
		counter_name,
		instance_name 
FROM dbo.Counters WITH(UPDLOCK);

/* Associate performance counters collected with this SQL Instance */
INSERT INTO dbo.InstanceCounters(InstanceID,CounterID)
SELECT	@InstanceID,
		C.CounterID
FROM @PerformanceCounters pc 
JOIN dbo.Counters C ON C.counter_name = pc.counter_name 
					AND C.instance_name = pc.instance_name
					AND C.object_name = pc.object_name
EXCEPT 
SELECT	InstanceID,
		CounterID 
FROM dbo.InstanceCounters
WHERE InstanceID =@InstanceID;

/* Update the date of counter last collection for this SQL Instance */
UPDATE IC 
SET IC.UpdatedDate = @SnapshotDate
FROM dbo.InstanceCounters IC 
WHERE InstanceID = @InstanceID
AND EXISTS(	SELECT 1 
			FROM @PerformanceCounters pc 
			JOIN dbo.Counters C ON C.counter_name = pc.counter_name 
								AND C.instance_name = pc.instance_name 
								AND C.object_name = pc.object_name
			WHERE C.CounterID= IC.CounterID
			AND pc.cntr_type <> 1073939712 /* PERF_LARGE_RAW_BASE. Excluding this counter as it's just used in calculations for another counter  */
		)

DECLARE @PC TABLE(
	InstanceID INT NOT NULL,
	CounterID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	SnapshotDate60 DATETIME2(2) NOT NULL,
	Value DECIMAL (28, 9) NULL,
	PRIMARY KEY(InstanceID,CounterID,SnapshotDate)
) 

/* 
	Performance counter values require some calculation from the raw values collected based on the counter type.
	PERF_COUNTER_LARGE_RAWCOUNT can be used directly.
	PERF_LARGE_RAW_FRACTION needs to be divided by it's associated base counter.
	PERF_COUNTER_BULK_COUNT is a rate metric.  It needs to be subtracted from the previous collected value and divided by the number of seconds since the last collection.
	PERF_AVERAGE_BULK has an associated base counter.  The values of both counters are cumulative so you need to subtract the current value from the previous collected value.  The difference for PERF_AVERAGE_BULK is divided by the difference for it's associated base value.
	
	More details can be found here:
	https://techcommunity.microsoft.com/t5/sql-server-support-blog/interpreting-the-counter-values-from-sys-dm-os-performance/ba-p/317824
	
	Note: There is a CounterMapping table that associates the PERF_LARGE_RAW_FRACTION & PERF_AVERAGE_BULK counter types with the associated PERF_LARGE_RAW_BASE.  The naming of the base counter usually has the same name with " Base" appended, but this isn't consistent.
	Staging.PerformanceCounters holds the previous collected values
*/
INSERT INTO @PC
(
    InstanceID,
    CounterID,
    SnapshotDate,
	SnapshotDate60,
    Value
)
SELECT @InstanceID AS InstanceID,
	C.CounterID,
	B.SnapshotDate,
	DG.DateGroup, /* 60Min Date Grouping */
	CASE WHEN B.cntr_type = 65792  /* PERF_COUNTER_LARGE_RAWCOUNT - specific handling for Batch Resp Statistics.  This gives is number of batches by duration.  We don't want the total number - it's better to show the difference between the last collection */
			  AND B.object_name='Batch Resp Statistics' 
			  AND B.cntr_value >= A.cntr_value 
			  THEN B.cntr_value - A.cntr_value 
		WHEN B.cntr_type = 65792  /* PERF_COUNTER_LARGE_RAWCOUNT */
			AND B.object_name<>'Batch Resp Statistics' 
			THEN B.cntr_value 
		WHEN B.cntr_type = 272696576 /* PERF_COUNTER_BULK_COUNT */
			AND B.cntr_value>=A.cntr_value /* Sanity check: Current value should be larger than the previous value collected */
			THEN (B.cntr_value-A.cntr_value) / (DATEDIFF(ms,A.SnapshotDate,B.SnapshotDate)/1000.0)
		WHEN B.cntr_type=537003264 /* PERF_LARGE_RAW_FRACTION */
			THEN B.cntr_value*100.0 / NULLIF(Bbase.cntr_value,0)
		WHEN B.cntr_type=1073874176 /* PERF_AVERAGE_BULK */
			AND Bbase.cntr_value>=Abase.cntr_value /* Sanity check: Current value should be larger than the previous value collected */
			AND B.cntr_value>=A.cntr_value
			THEN ISNULL((B.cntr_value-A.cntr_value*1.0) / NULLIF(Bbase.cntr_value-Abase.cntr_value,0),0)
	ELSE NULL END AS Value
FROM @PerformanceCounters B /* B = Current Collection */
CROSS APPLY dbo.DateGroupingMins(B.SnapshotDate,60) DG /* Date groups of 60min intervals to be used for 60Min aggregate table insert later */
JOIN dbo.Counters C ON C.counter_name = B.counter_name AND C.object_name = B.object_name AND C.instance_name = B.instance_name
/* Map counters to the associated base counter */
LEFT JOIN dbo.CounterMapping M 	ON B.object_name = M.object_name 
								AND B.counter_name = M.counter_name 
								AND B.cntr_type IN(1073874176,	/* PERF_AVERAGE_BULK		*/
													537003264	/* PERF_LARGE_RAW_FRACTION	*/
													)  
/* Bbase = Current collection base counter */
LEFT JOIN @PerformanceCounters Bbase ON Bbase.object_name = M.object_name 
										AND Bbase.counter_name = M.base_counter_name 
										AND Bbase.instance_name = B.instance_name 
										AND Bbase.cntr_type=1073939712	/* PERF_LARGE_RAW_BASE		*/
										AND B.cntr_type IN(1073874176,	/* PERF_AVERAGE_BULK		*/
															537003264	/* PERF_LARGE_RAW_FRACTION	*/
															) 
/* A = Previous collection */
LEFT JOIN Staging.PerformanceCounters A ON A.object_name = b.object_name 
											AND A.counter_name = B.counter_name 
											AND A.instance_name = B.instance_name 
											AND B.cntr_type = A.cntr_type 
											AND A.InstanceID = @InstanceID
											AND A.SnapshotDate< B.SnapshotDate
/* ABase = Previous collection base counter */
LEFT JOIN Staging.PerformanceCounters Abase ON Abase.object_name = M.object_name 
											AND Abase.counter_name = M.base_counter_name 
											AND Abase.instance_name = B.instance_name 
											AND A.cntr_type = 1073874176	/* PERF_AVERAGE_BULK */
											AND Abase.InstanceID = @InstanceID 
											AND Abase.cntr_type=1073939712	/* PERF_LARGE_RAW_BASE */
											AND ABase.SnapshotDate <B.SnapshotDate
WHERE B.cntr_type IN(65792,		/*	PERF_COUNTER_LARGE_RAWCOUNT */
					272696576,	/*  PERF_COUNTER_BULK_COUNT		*/
					537003264,	/*  PERF_LARGE_RAW_FRACTION		*/
					1073874176	/*	PERF_LARGE_RAW_BASE			*/
					) 
AND NOT EXISTS(SELECT 1 
				FROM dbo.PerformanceCounters PC 
				WHERE PC.SnapshotDate = CAST(B.SnapshotDate as DATETIME2(2)) 
				AND PC.InstanceID = @InstanceID 
				AND PC.CounterID=C.CounterID
				)
AND (DATEDIFF(mi,A.SnapshotDate,B.SnapshotDate)< 1440 /* Don't insert if the time between collections is more than 1 day */
		OR A.SnapshotDate IS NULL /* OK to insert if there is no previous collection. e.g. PERF_COUNTER_LARGE_RAWCOUNT which doesn't require calculation */
		)
		
BEGIN TRAN

INSERT INTO dbo.PerformanceCounters
(
    InstanceID,
    CounterID,
    SnapshotDate,
    Value
)
SELECT PC.InstanceID,
       PC.CounterID,
	   PC.SnapshotDate,
       PC.Value	
FROM @PC PC
WHERE Value IS NOT NULL;

WITH T AS (
	SELECT PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate60,
		SUM(PC.Value) as Value_Total,
		MIN(PC.Value) as Value_Min,
		MAX(PC.Value) as Value_Max,
		COUNT(*) as SampleCount  
	FROM @PC PC
	WHERE PC.Value IS NOT NULL
	GROUP BY PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate60
)
UPDATE PC60
	SET PC60.Value_Total += T.Value_Total,
		PC60.Value_Min = CASE WHEN T.Value_Min < PC60.Value_Min THEN T.Value_Min ELSE PC60.Value_Min END,
		PC60.Value_Max = CASE WHEN T.Value_Max > PC60.Value_Max THEN T.Value_Max ELSE PC60.Value_Max END,
		PC60.SampleCount += T.SampleCount
FROM dbo.PerformanceCounters_60MIN PC60
JOIN T ON	T.InstanceID = PC60.InstanceID 
			AND T.CounterID = PC60.CounterID 
			AND T.SnapshotDate60 = PC60.SnapshotDate

INSERT INTO dbo.PerformanceCounters_60MIN
(
    InstanceID,
    CounterID,
    SnapshotDate,
    Value_Total,
    Value_Min,
    Value_Max,
    SampleCount
)
SELECT PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate60,
		SUM(PC.Value),
		MIN(PC.Value),
		MAX(PC.Value),
		COUNT(*)  
FROM @PC PC
WHERE Value IS NOT NULL
AND NOT EXISTS(	SELECT 1 
				FROM dbo.PerformanceCounters_60MIN PC60 
				WHERE PC60.InstanceID = PC.InstanceID 
				AND PC60.CounterID = PC.CounterID 
				AND PC60.SnapshotDate = PC.SnapshotDate60
				)
GROUP BY PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate60

IF @Internal=0
BEGIN

	DELETE Staging.PerformanceCounters 
	WHERE InstanceID = @InstanceID
	
	INSERT INTO Staging.PerformanceCounters(
		   InstanceID,
		   SnapshotDate,
		   object_name,
		   counter_name,
		   instance_name,
		   cntr_value,
		   cntr_type
		   )
	SELECT @InstanceID,
		   SnapshotDate,
		   object_name,
		   counter_name,
		   instance_name,
		   cntr_value,
		   cntr_type
	FROM @PerformanceCounters
	WHERE (cntr_type <> 65792 /* PERF_COUNTER_LARGE_RAWCOUNT.  Value used as is without any calculation required */
			OR object_name='Batch Resp Statistics' /* Batch Resp Statistics is a special case for PERF_COUNTER_LARGE_RAWCOUNT where we want the difference from the previous collection */
			)
END

COMMIT
IF @Internal=0
BEGIN
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
								 @Reference = @Ref,
								 @SnapshotDate = @SnapshotDate
END