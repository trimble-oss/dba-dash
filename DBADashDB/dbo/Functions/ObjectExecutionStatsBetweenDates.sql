/*
	The object execution stats rows for an instance between two dates, reading whole hours from
	dbo.ObjectExecutionStats_60MIN and the part hours at either end from dbo.ObjectExecutionStats.  @HourFrom & @HourTo
	come from dbo.RollupRanges - each range is its own seek on the clustered key.

	Called with a single instance as a variable, each seek can be a parallel range scan.  Applied per row of a table of
	instances it runs as a serial seek per instance - FORCESEEK keeps it from scanning the table and joining instead,
	which a large estimate for many instances could otherwise choose.
*/
CREATE FUNCTION dbo.ObjectExecutionStatsBetweenDates(
	@InstanceID INT,
	@From DATETIME2(3),
	@HourFrom DATETIME2(3),
	@HourTo DATETIME2(3),
	@ToExclusive DATETIME2(3)
)
RETURNS TABLE
AS
RETURN
/* The part hour at the start of the window, from the raw rows */
SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
	   OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
	   OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin,
	   OES.PeriodStartTime, OES.PeriodEndTime
FROM dbo.ObjectExecutionStats OES WITH (FORCESEEK)
WHERE OES.InstanceID = @InstanceID
AND OES.SnapshotDate >= @From
AND OES.SnapshotDate < @HourFrom
UNION ALL
/* The whole hours, from the rollup */
SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
	   OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
	   OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin,
	   OES.PeriodStartTime, OES.PeriodEndTime
FROM dbo.ObjectExecutionStats_60MIN OES WITH (FORCESEEK)
WHERE OES.InstanceID = @InstanceID
AND OES.SnapshotDate >= @HourFrom
AND OES.SnapshotDate < @HourTo
UNION ALL
/* The part hour at the end */
SELECT OES.InstanceID, OES.ObjectID, OES.SnapshotDate, OES.PeriodTime, OES.total_worker_time,
	   OES.total_elapsed_time, OES.total_logical_reads, OES.total_logical_writes,
	   OES.total_physical_reads, OES.execution_count, OES.MaxExecutionsPerMin,
	   OES.PeriodStartTime, OES.PeriodEndTime
FROM dbo.ObjectExecutionStats OES WITH (FORCESEEK)
WHERE OES.InstanceID = @InstanceID
AND OES.SnapshotDate >= @HourTo
AND OES.SnapshotDate < @ToExclusive
