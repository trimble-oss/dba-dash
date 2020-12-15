CREATE PROC dbo.PerformanceCounter_Get(
	@InstanceID INT,
	@CounterID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2)
)
AS
SELECT CounterID,
       SnapshotDate,
       Value 
FROM dbo.PerformanceCounters
WHERE InstanceID =@InstanceID
AND CounterID = @CounterID
AND SnapshotDate>=@FromDate
AND SnapshotDate<@ToDate