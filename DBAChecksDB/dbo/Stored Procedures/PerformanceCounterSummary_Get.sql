CREATE PROC dbo.PerformanceCounterSummary_Get(@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@Search NVARCHAR(128)=NULL)
AS
WITH T AS (
SELECT C.CounterID,
       C.object_name,
       C.counter_name,
       C.instance_name,
	   PC.Value,
	   PC.SnapshotDate,
	   LAST_VALUE(PC.Value) OVER(PARTITION BY PC.InstanceID,PC.CounterID ORDER BY PC.SnapshotDate DESC) AS LastValue
FROM dbo.InstanceCounters IC
JOIN dbo.Counters C ON C.CounterID = IC.CounterID
JOIN dbo.PerformanceCounters PC ON PC.InstanceID = IC.InstanceID AND PC.CounterID = IC.CounterID
WHERE IC.InstanceID = @InstanceID
AND PC.SnapshotDate>=@FromDate
AND PC.SnapshotDate<@ToDate
AND (C.object_name LIKE '%' + @Search + '%'
	OR C.instance_name LIKE '%' + @Search + '%'
	OR C.counter_name LIKE '%' + @Search + '%'
	OR @Search IS NULL
	)
)
SELECT T.CounterID,
       T.object_name,
       T.counter_name,
       T.instance_name,
       MAX(T.Value) AS MaxValue,
	   MIN(T.Value) AS MinValue,
	   AVG(T.Value) AS AvgValue,
       MAX(T.LastValue) AS CurrentValue 
FROM T
GROUP BY  T.CounterID,
       T.object_name,
       T.counter_name,
       T.instance_name
ORDER BY T.object_name,T.counter_name,T.instance_name;