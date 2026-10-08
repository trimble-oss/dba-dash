/*
	Drives the deadlock markers on the performance tab's blocking chart.

	Two sources, picked per instance:

	Collected - where the Deadlocks collection is enabled, the markers count the rows it stored in
	dbo.Deadlocks.  These are real events with their own timestamps, so the count is exact, each bucket
	selects exactly the deadlocks it counted (FromDate/ToDate), and the chart agrees with the native
	Deadlocks report it drills into.  It can also be scoped to a database, which the counter cannot.

	Counter - otherwise, the "Number of Deadlocks/sec" performance counter converted back into a count.
	Needs nothing enabled, but the counter is engine wide.  On Azure SQL Database the engine is shared
	(elastic pool, internal activity), so it reports deadlocks that never touched the database in
	question - see https://github.com/trimble-oss/dba-dash/issues/1939.  The raw counter is still on the
	Metrics tab.

	The collected source is a range seek on the clustered key of dbo.Deadlocks (InstanceID, EventTime),
	partition eliminated on EventTime, and reads one row per deadlock rather than one per counter
	snapshot.  The database filter is a seek into dbo.DeadlockProcesses on the same key prefix.

	@IsCollected returns which source was used, so the chart can label it.  @IsCollectionEnabled says
	whether the collection is enabled whichever source was used: it decides which report the chart drills
	into.

	@PreferCollected = 0 uses the counter even where the collection is enabled.  The collection's history
	starts when it was switched on, so the counter is the way to see further back.
*/
CREATE PROC dbo.Deadlocks_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT=NULL,
	@Use60Min BIT=NULL,
	/* Collected source only.  The counter has no database dimension. */
	@DatabaseID INT=NULL,
	@PreferCollected BIT=1,
	@IsCollected BIT=NULL OUTPUT,
	@IsCollectionEnabled BIT=NULL OUTPUT,
	@Debug BIT=0
)
AS
SET NOCOUNT ON

/*	Enabled rather than "has rows": an instance that isn't deadlocking has none, and should show an empty
	chart rather than fall back to a counter that may be counting someone else's deadlocks. */
SET @IsCollectionEnabled = CASE WHEN EXISTS(
						SELECT 1
						FROM dbo.ScheduleInfo
						WHERE InstanceID = @InstanceID
						AND Reference = 'Deadlocks'
						AND IsEnabled = 1
						) THEN 1 ELSE 0 END

SET @IsCollected = CASE WHEN @IsCollectionEnabled = 1 AND ISNULL(@PreferCollected, 1) = 1 THEN 1 ELSE 0 END

IF @IsCollected = 1
BEGIN
	SET @DateGroupingMin = CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin < 0 THEN 0 ELSE @DateGroupingMin END
	/*	Ungrouped, a point per event would need a width for the drill-down's exclusive end, so a minute is used
		- as dbo.DeadlockCharts_Get does. */
	DECLARE @BucketMins INT = CASE WHEN @DateGroupingMin < 1 THEN 1 ELSE @DateGroupingMin END

	SELECT	DG.DateGroup AS SnapshotDate,
			DG.DateGroup AS PreviousSnapshotDate,
			CAST(COUNT(*) AS BIGINT) AS DeadlockCount,
			/* The window the point counted, start inclusive and end exclusive - what dbo.DeadlockScope expects. */
			DG.DateGroup AS FromDate,
			DATEADD(MINUTE, @BucketMins, DG.DateGroup) AS ToDate
	FROM dbo.Deadlocks D
	CROSS APPLY dbo.DateGroupingMins(D.EventTime, @BucketMins) DG
	WHERE D.InstanceID = @InstanceID
	AND D.EventTime >= @FromDate
	AND D.EventTime < @ToDate
	AND (@DatabaseID IS NULL
		OR EXISTS(	SELECT 1
					FROM dbo.DeadlockProcesses P
					WHERE P.InstanceID = D.InstanceID
					AND P.EventTime = D.EventTime
					AND P.DeadlockHash = D.DeadlockHash
					AND P.DatabaseID = @DatabaseID
					)
		)
	GROUP BY DG.DateGroup
	ORDER BY SnapshotDate DESC
	OPTION(RECOMPILE)

	RETURN
END

DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'Deadlocks.SnapshotDate'
			ELSE 'DG.DateGroup' END
SELECT @Use60Min = CASE WHEN @Use60Min IS NOT NULL THEN @Use60Min WHEN @DateGroupingMin>=60 THEN 1 ELSE 0 END


SET @SQL = N'
WITH Deadlocks AS (
	SELECT  PC.SnapshotDate,
			/* The snapshot date is the date the metric was collected.  The deadlock events occurred between PreviousSnapshotDate and SnapshotDate.  We can use this for filtering in sp_BlitzLock */
			LAG(PC.SnapshotDate) OVER (ORDER BY PC.SnapshotDate) AS PreviousSnapshotDate,
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
		MIN(PreviousSnapshotDate) AS PreviousSnapshotDate,
		SUM(DeadlockCount) AS DeadlockCount,
		/* Not known exactly for a counter - the caller derives the window from the snapshot dates */
		CAST(NULL AS DATETIME2(3)) AS FromDate,
		CAST(NULL AS DATETIME2(3)) AS ToDate
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
