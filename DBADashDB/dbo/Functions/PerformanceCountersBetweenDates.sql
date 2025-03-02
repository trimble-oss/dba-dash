CREATE FUNCTION dbo.PerformanceCountersBetweenDates(
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@InstanceID INT,
	@CounterID INT,
	@CounterType TINYINT
)
RETURNS TABLE
AS
RETURN
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.Value
FROM dbo.PerformanceCounters PC
WHERE @CounterType = 1
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(2)) /* Important that date type matches for performance */
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(2)) /* Important that date type matches for performance */
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
SELECT	RQPC.InstanceID,
		RQPC.CounterID,
		RQPC.SnapshotDate,
		RQPC.Value
FROM dbo.RunningQueriesPerformanceCounters RQPC
WHERE @CounterType = 2
AND RQPC.SnapshotDate >= @FromDate 
AND RQPC.SnapshotDate < @ToDate
AND RQPC.InstanceID = @InstanceID
AND RQPC.CounterID = @CounterID