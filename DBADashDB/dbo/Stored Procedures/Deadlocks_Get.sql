CREATE PROC dbo.Deadlocks_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT=NULL,
	@Use60Min BIT=NULL,
	@Debug BIT=0
)
AS
SET NOCOUNT ON
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'Deadlocks.SnapshotDate'
			ELSE 'DG.DateGroup' END
SELECT @Use60Min = CASE WHEN @Use60Min IS NOT NULL THEN @Use60Min WHEN @DateGroupingMin>=60 THEN 1 ELSE 0 END


SET @SQL = N'
WITH Deadlocks AS (
	SELECT  PC.SnapshotDate,
			/* Convert deadlocks/sec to deadlock count */
			CAST(ROUND((' + CASE WHEN @Use60Min=1 THEN '(PC.Value_Total/PC.SampleCount)' ELSE 'PC.value' END + ' * (DATEDIFF(ms,LAG(PC.SnapshotDate) OVER (ORDER BY PC.SnapshotDate),PC.SnapshotDate)/1000.0)),0) AS BIGINT) AS DeadlockCount
	FROM dbo.PerformanceCounters' + CASE WHEN @Use60Min=1 THEN '_60MIN' ELSE '' END + ' PC
	JOIN dbo.Counters C ON PC.CounterID = C.CounterID
	WHERE PC.SnapshotDate >= @FromDate 
	AND PC.SnapshotDate < @ToDate
	AND PC.InstanceID = @InstanceID
	AND C.counter_name = ''Number of Deadlocks/sec''
	AND C.object_name = ''Locks''
	AND C.instance_name = ''_Total''
)
SELECT ' + @DateGroupingSQL + ' AS SnapshotDate,
		SUM(DeadlockCount) AS DeadlockCount
FROM Deadlocks
' + CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(Deadlocks.SnapshotDate,@DateGroupingMin) DG' END + '
WHERE DeadlockCount IS NOT NULL
GROUP BY ' + @DateGroupingSQL + '
ORDER BY SnapshotDate DESC'

IF @Debug=1
BEGIN
	EXEC dbo.PrintMax @SQL
END

EXEC sp_executesql @SQL, N'@InstanceID INT, @FromDate DATETIME2(2), @ToDate DATETIME2(2), @DateGroupingMin INT', @InstanceID, @FromDate, @ToDate, @DateGroupingMin

GO