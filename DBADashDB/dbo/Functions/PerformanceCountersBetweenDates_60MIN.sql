CREATE FUNCTION dbo.PerformanceCountersBetweenDates_60MIN(
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@InstanceID INT,
	@CounterID INT,
	@CounterType TINYINT
)
RETURNS TABLE
AS
RETURN
/* Standard performance counters*/
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.Value_Total,
		PC.Value_Min,
		PC.Value_Max,
		PC.SampleCount
FROM dbo.PerformanceCounters_60MIN PC
WHERE @CounterType = 1
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(2))
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* Runing Queries performance counters. No 60 MIN aggregation available so fake schema. */
SELECT	RQPC.InstanceID,
		RQPC.CounterID,
		RQPC.SnapshotDate,
		RQPC.Value AS Value_Total,
		RQPC.Value AS Value_Min,
		RQPC.Value AS Value_Max,
		1 AS SampleCount
FROM dbo.RunningQueriesPerformanceCounters RQPC
WHERE @CounterType = 2
AND RQPC.SnapshotDate >= @FromDate 
AND RQPC.SnapshotDate < @ToDate
AND RQPC.InstanceID = @InstanceID
AND RQPC.CounterID = @CounterID