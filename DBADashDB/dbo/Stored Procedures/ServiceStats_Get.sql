CREATE PROC dbo.ServiceStats_Get(
	@InstanceIDs IDs READONLY, /* The services that collect or import these instances.  Empty for every service */
	@DBADashAgentID INT = NULL, /* One service.  NULL for every service in @InstanceIDs' scope */
	@FromDate DATETIME2(2) = NULL, /* UTC.  Default: a day before @ToDate */
	@ToDate DATETIME2(2) = NULL, /* UTC.  Default: now */
	@DateGroupingMin INT = NULL /* Bucket size in minutes.  0 for each sample.  NULL to suit the range - about a hundred points */
)
AS
/*
	The DBA Dash service's own resource usage over time - see dbo.ServiceStats - for the Service Stats report.  Service is the
	host and service name, to tell the series apart.

	Result 1: a row per service in scope - its version and path even without samples - summarising the date range: the latest value of a measure that's a level (memory, threads,
	connections), with its min/max or average where how it moved matters, and totals for counts (GCs, restarts, skipped).
	Result 2: a row per service per time bucket.
	Result 3: the work queue's length by priority, a row per bucket per series - for a stacked chart.  The series is the
	priority, prefixed with the service where there's more than one.

	CpuPct is the share of the host's processors the service used over the bucket: 100 is every core busy.  MaxCpuPct is the
	busiest sample in the bucket.
	WorkingSetPerSQLConnectionMB is the working set over the SQL connections the service is handling: memory that grows with
	the estate keeps it level, while a leak or a new cost raises it.
	Rates - per minute, per hour, % of time - are over the time the samples cover rather than the range or bucket, so they
	mean the same whatever the range, and a service that was down for part of it isn't averaged over the gap.  GCPausePct is
	the share of that time the process was paused for garbage collection.  QueueSkippedPct is the share of scheduled
	collection attempts that weren't queued because the previous run of the same one hadn't finished.  SampledPct (summary) is the
	share of the range the samples cover - less than 100 is a service that was stopped or couldn't report for some of it.
	QueueLength is the collections waiting in the work queue, all priorities.  QueueWaitAvgMs is the average time a started
	item waited for a worker.
	Memory, thread, connection and queue length values are the latest in the bucket, with the bucket's highest working set,
	longest queues and longest wait alongside.  Restarts counts samples in the bucket whose process start differs from the
	sample before.
*/
SET NOCOUNT ON

SET @ToDate = ISNULL(@ToDate,SYSUTCDATETIME())
SET @FromDate = ISNULL(@FromDate,DATEADD(d,-1,@ToDate))

IF @DateGroupingMin IS NULL
BEGIN
	/* Samples are a minute apart by default, so a range up to a couple of hours is shown sample by sample */
	DECLARE @RangeMins INT = DATEDIFF(MINUTE,@FromDate,@ToDate)
	SET @DateGroupingMin = CASE
		WHEN @RangeMins <= 120 THEN 0
		WHEN @RangeMins <= 500 THEN 5
		WHEN @RangeMins <= 1500 THEN 15
		WHEN @RangeMins <= 6000 THEN 60
		WHEN @RangeMins <= 36000 THEN 360
		ELSE 1440 END
END
SET @DateGroupingMin = CASE WHEN @DateGroupingMin < 0 THEN 0 ELSE @DateGroupingMin END

CREATE TABLE #Agents(DBADashAgentID INT PRIMARY KEY)

IF NOT EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO #Agents(DBADashAgentID)
	SELECT DBADashAgentID
	FROM dbo.DBADashAgent
	WHERE (DBADashAgentID = @DBADashAgentID OR @DBADashAgentID IS NULL)
END
ELSE
BEGIN
	/* The service that collects an instance, and the one that imports it where they differ (an S3 or folder destination) */
	INSERT INTO #Agents(DBADashAgentID)
	SELECT A.AgentID
	FROM dbo.Instances I
	JOIN @InstanceIDs T ON T.ID = I.InstanceID
	CROSS APPLY(VALUES(I.CollectAgentID),(I.ImportAgentID)) A(AgentID)
	WHERE A.AgentID IS NOT NULL
	AND (A.AgentID = @DBADashAgentID OR @DBADashAgentID IS NULL)
	GROUP BY A.AgentID
END;

/* Each service's process start as of the range start, so a restart before its first sample in the range is counted */
SELECT	AG.DBADashAgentID,
		P.ProcessStartDate
INTO #Prev
FROM #Agents AG
CROSS APPLY(SELECT TOP(1) SS.ProcessStartDate
			FROM dbo.ServiceStats SS
			WHERE SS.DBADashAgentID = AG.DBADashAgentID
			AND SS.SnapshotDate < @FromDate
			ORDER BY SS.SnapshotDate DESC) P;

SELECT	SS.DBADashAgentID,
			SS.SnapshotDate,
			SS.ProcessStartDate,
			SS.IntervalMs,
			SS.CpuMs,
			SS.ProcessorCount,
			100.0 * SS.CpuMs / NULLIF(CAST(SS.IntervalMs AS BIGINT) * SS.ProcessorCount,0) AS CpuPct,
			SS.WorkingSetKB,
			SS.PeakWorkingSetKB,
			SS.PrivateBytesKB,
			SS.ManagedHeapKB,
			SS.GCCommittedKB,
			SS.Gen0Collections,
			SS.Gen1Collections,
			SS.Gen2Collections,
			SS.GCPauseMs,
			SS.ThreadCount,
			SS.ThreadPoolThreads,
			SS.ThreadPoolQueueLength,
			SS.HandleCount,
			SS.SourceConnections,
			SS.SQLConnections,
			SS.DiscoveredConnections,
			SS.OfflineInstances,
			SS.QueueLengthHigh + SS.QueueLengthNormal + SS.QueueLengthLow AS QueueLength,
			SS.QueueLengthHigh,
			SS.QueueLengthNormal,
			SS.QueueLengthLow,
			SS.Workers,
			SS.BusyWorkers,
			SS.MaxBusyWorkers,
			SS.QueueItemsStarted,
			SS.QueueSkipped,
			SS.QueueWaitTotalMs,
			SS.QueueWaitMaxMs,
			SS.QueueItemsQueued,
			/* The part of the sample's interval inside the range - the first can start before it */
			DATEDIFF_BIG(MILLISECOND,
				CASE WHEN DATEADD(MILLISECOND,-SS.IntervalMs,SS.SnapshotDate) < @FromDate THEN @FromDate ELSE DATEADD(MILLISECOND,-SS.IntervalMs,SS.SnapshotDate) END,
				SS.SnapshotDate) AS CoveredMs,
			DG.DateGroup AS Bucket,
			ROW_NUMBER() OVER(PARTITION BY SS.DBADashAgentID, DG.DateGroup ORDER BY SS.SnapshotDate DESC) AS LatestInBucket,
			ROW_NUMBER() OVER(PARTITION BY SS.DBADashAgentID ORDER BY SS.SnapshotDate DESC) AS Latest,
			CASE WHEN SS.ProcessStartDate <> ISNULL(LAG(SS.ProcessStartDate) OVER(PARTITION BY SS.DBADashAgentID ORDER BY SS.SnapshotDate),P.ProcessStartDate) THEN 1 ELSE 0 END AS IsRestart
INTO #S
FROM dbo.ServiceStats SS
JOIN #Agents AG ON AG.DBADashAgentID = SS.DBADashAgentID
LEFT JOIN #Prev P ON P.DBADashAgentID = SS.DBADashAgentID
CROSS APPLY dbo.DateGroupingMins(SS.SnapshotDate,@DateGroupingMin) DG
WHERE SS.SnapshotDate >= @FromDate
AND SS.SnapshotDate < @ToDate;

/*	Summary - every service in scope, with its version and path whether or not it has samples in the range (they're only
	collected with LogInternalPerformanceCounters on).  Current values are the latest sample in the range. */
SELECT	AG.DBADashAgentID,
		A.AgentHostName + '\' + A.AgentServiceName AS Service,
		A.AgentHostName,
		A.AgentServiceName,
		A.AgentVersion,
		A.AgentPath,
		CAST(MAX(S.SnapshotDate) AS DATETIME2(2)) AS LastSample,
		CAST(MAX(CASE WHEN S.Latest = 1 THEN S.ProcessStartDate END) AS DATETIME2(2)) AS ProcessStartDate,
		SUM(S.IsRestart) AS Restarts,
		CAST(100.0 * SUM(CAST(S.CpuMs AS BIGINT)) / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT) * S.ProcessorCount),0) AS DECIMAL(9,2)) AS AvgCpuPct,
		CAST(MAX(S.CpuPct) AS DECIMAL(9,2)) AS MaxCpuPct,
		MAX(CASE WHEN S.Latest = 1 THEN S.ProcessorCount END) AS ProcessorCount,
		CAST(MAX(CASE WHEN S.Latest = 1 THEN S.WorkingSetKB END) / 1024.0 AS DECIMAL(19,1)) AS WorkingSetMB,
		CAST(MIN(S.WorkingSetKB) / 1024.0 AS DECIMAL(19,1)) AS MinWorkingSetMB,
		CAST(MAX(S.WorkingSetKB) / 1024.0 AS DECIMAL(19,1)) AS MaxWorkingSetMB,
		CAST(MAX(CASE WHEN S.Latest = 1 THEN S.WorkingSetKB / 1024.0 / NULLIF(S.SQLConnections,0) END) AS DECIMAL(19,2)) AS WorkingSetPerSQLConnectionMB,
		CAST(MIN(S.WorkingSetKB / 1024.0 / NULLIF(S.SQLConnections,0)) AS DECIMAL(19,2)) AS MinWorkingSetPerSQLConnectionMB,
		CAST(MAX(S.WorkingSetKB / 1024.0 / NULLIF(S.SQLConnections,0)) AS DECIMAL(19,2)) AS MaxWorkingSetPerSQLConnectionMB,
		CAST(MAX(CASE WHEN S.Latest = 1 THEN S.PrivateBytesKB END) / 1024.0 AS DECIMAL(19,1)) AS PrivateBytesMB,
		CAST(MAX(CASE WHEN S.Latest = 1 THEN S.ManagedHeapKB END) / 1024.0 AS DECIMAL(19,1)) AS ManagedHeapMB,
		CAST(MAX(S.ManagedHeapKB) / 1024.0 AS DECIMAL(19,1)) AS MaxManagedHeapMB,
		CAST(SUM(S.Gen2Collections) * 3600000.0 / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(19,1)) AS Gen2CollectionsPerHour,
		CAST(100.0 * SUM(S.GCPauseMs) / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(9,2)) AS GCPausePct,
		MAX(CASE WHEN S.Latest = 1 THEN S.ThreadCount END) AS ThreadCount,
		MAX(S.ThreadCount) AS MaxThreadCount,
		MAX(S.ThreadPoolQueueLength) AS MaxThreadPoolQueueLength,
		MAX(CASE WHEN S.Latest = 1 THEN S.HandleCount END) AS HandleCount,
		MAX(S.HandleCount) AS MaxHandleCount,
		MAX(CASE WHEN S.Latest = 1 THEN S.SourceConnections END) AS SourceConnections,
		MAX(CASE WHEN S.Latest = 1 THEN S.SQLConnections END) AS SQLConnections,
		MIN(S.SQLConnections) AS MinSQLConnections,
		MAX(S.SQLConnections) AS MaxSQLConnections,
		MAX(CASE WHEN S.Latest = 1 THEN S.DiscoveredConnections END) AS DiscoveredConnections,
		MAX(CASE WHEN S.Latest = 1 THEN S.OfflineInstances END) AS OfflineInstances,
		MAX(CASE WHEN S.Latest = 1 THEN S.QueueLength END) AS QueueLength,
		CAST(AVG(S.QueueLength * 1.0) AS DECIMAL(19,1)) AS AvgQueueLength,
		MAX(S.QueueLength) AS MaxQueueLength,
		MAX(CASE WHEN S.Latest = 1 THEN S.Workers END) AS Workers,
		MAX(S.MaxBusyWorkers) AS MaxBusyWorkers,
		/* Per minute of sampled time, so it means the same whatever the range or bucket - and a service that was down for part of it isn't averaged over the gap */
		CAST(SUM(S.QueueItemsStarted) * 60000.0 / NULLIF(SUM(CASE WHEN S.QueueItemsStarted IS NOT NULL THEN CAST(S.IntervalMs AS BIGINT) END),0) AS DECIMAL(19,1)) AS QueueItemsStartedPerMin,
		CAST(100.0 * SUM(S.QueueSkipped) / NULLIF(SUM(S.QueueItemsQueued) + SUM(S.QueueSkipped),0) AS DECIMAL(9,2)) AS QueueSkippedPct,
		SUM(S.QueueWaitTotalMs) / NULLIF(SUM(S.QueueItemsStarted),0) AS QueueWaitAvgMs,
		MAX(S.QueueWaitMaxMs) AS QueueWaitMaxMs,
		/* The share of the range the samples cover, each clipped to the range.  Capped at 100 against clock adjustments. */
		CAST(CASE WHEN 100.0 * SUM(S.CoveredMs) / NULLIF(DATEDIFF_BIG(MILLISECOND,@FromDate,@ToDate),0) > 100 THEN 100
			ELSE ISNULL(100.0 * SUM(S.CoveredMs) / NULLIF(DATEDIFF_BIG(MILLISECOND,@FromDate,@ToDate),0),0) END AS DECIMAL(9,1)) AS SampledPct,
		COUNT(S.SnapshotDate) AS Samples
FROM #Agents AG
JOIN dbo.DBADashAgent A ON A.DBADashAgentID = AG.DBADashAgentID
LEFT JOIN #S S ON S.DBADashAgentID = AG.DBADashAgentID
GROUP BY AG.DBADashAgentID, A.AgentHostName, A.AgentServiceName, A.AgentVersion, A.AgentPath
ORDER BY Service;

SELECT	S.DBADashAgentID,
		A.AgentHostName + '\' + A.AgentServiceName AS Service,
		A.AgentHostName,
		A.AgentServiceName,
		CAST(S.Bucket AS DATETIME2(2)) AS SnapshotDate,
		CAST(100.0 * SUM(CAST(S.CpuMs AS BIGINT)) / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT) * S.ProcessorCount),0) AS DECIMAL(9,2)) AS CpuPct,
		CAST(MAX(S.CpuPct) AS DECIMAL(9,2)) AS MaxCpuPct,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.ProcessorCount END) AS ProcessorCount,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.WorkingSetKB END) / 1024.0 AS DECIMAL(19,1)) AS WorkingSetMB,
		CAST(MAX(S.WorkingSetKB) / 1024.0 AS DECIMAL(19,1)) AS MaxWorkingSetMB,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.PeakWorkingSetKB END) / 1024.0 AS DECIMAL(19,1)) AS PeakWorkingSetMB,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.PrivateBytesKB END) / 1024.0 AS DECIMAL(19,1)) AS PrivateBytesMB,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.ManagedHeapKB END) / 1024.0 AS DECIMAL(19,1)) AS ManagedHeapMB,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.GCCommittedKB END) / 1024.0 AS DECIMAL(19,1)) AS GCCommittedMB,
		CAST(SUM(S.Gen0Collections) * 3600000.0 / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(19,1)) AS Gen0CollectionsPerHour,
		CAST(SUM(S.Gen1Collections) * 3600000.0 / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(19,1)) AS Gen1CollectionsPerHour,
		CAST(SUM(S.Gen2Collections) * 3600000.0 / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(19,1)) AS Gen2CollectionsPerHour,
		CAST(100.0 * SUM(S.GCPauseMs) / NULLIF(SUM(CAST(S.IntervalMs AS BIGINT)),0) AS DECIMAL(9,2)) AS GCPausePct,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.ThreadCount END) AS ThreadCount,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.ThreadPoolThreads END) AS ThreadPoolThreads,
		MAX(S.ThreadPoolQueueLength) AS MaxThreadPoolQueueLength,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.HandleCount END) AS HandleCount,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.SourceConnections END) AS SourceConnections,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.SQLConnections END) AS SQLConnections,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.DiscoveredConnections END) AS DiscoveredConnections,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.WorkingSetKB / 1024.0 / NULLIF(S.SQLConnections,0) END) AS DECIMAL(19,2)) AS WorkingSetPerSQLConnectionMB,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.OfflineInstances END) AS OfflineInstances,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.QueueLength END) AS QueueLength,
		MAX(S.QueueLength) AS MaxQueueLength,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.QueueLengthHigh END) AS QueueLengthHigh,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.QueueLengthNormal END) AS QueueLengthNormal,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.QueueLengthLow END) AS QueueLengthLow,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.Workers END) AS Workers,
		MAX(CASE WHEN S.LatestInBucket = 1 THEN S.BusyWorkers END) AS BusyWorkers,
		MAX(S.MaxBusyWorkers) AS MaxBusyWorkers,
		/* Per minute of sampled time, so it means the same whatever the range or bucket - and a service that was down for part of it isn't averaged over the gap */
		CAST(SUM(S.QueueItemsStarted) * 60000.0 / NULLIF(SUM(CASE WHEN S.QueueItemsStarted IS NOT NULL THEN CAST(S.IntervalMs AS BIGINT) END),0) AS DECIMAL(19,1)) AS QueueItemsStartedPerMin,
		CAST(100.0 * SUM(S.QueueSkipped) / NULLIF(SUM(S.QueueItemsQueued) + SUM(S.QueueSkipped),0) AS DECIMAL(9,2)) AS QueueSkippedPct,
		SUM(S.QueueWaitTotalMs) / NULLIF(SUM(S.QueueItemsStarted),0) AS QueueWaitAvgMs,
		MAX(S.QueueWaitMaxMs) AS QueueWaitMaxMs,
		CAST(MAX(CASE WHEN S.LatestInBucket = 1 THEN S.ProcessStartDate END) AS DATETIME2(2)) AS ProcessStartDate,
		SUM(S.IsRestart) AS Restarts,
		COUNT(*) AS Samples
INTO #Buckets
FROM #S S
JOIN dbo.DBADashAgent A ON A.DBADashAgentID = S.DBADashAgentID
GROUP BY S.DBADashAgentID, A.AgentHostName, A.AgentServiceName, S.Bucket;

SELECT *
FROM #Buckets
ORDER BY DBADashAgentID, SnapshotDate;

/* As the latest sample in the bucket has them - a stack that adds up to QueueLength */
DECLARE @MultipleServices BIT = CASE WHEN (SELECT COUNT(DISTINCT DBADashAgentID) FROM #Buckets) > 1 THEN 1 ELSE 0 END

SELECT	B.SnapshotDate,
		CASE WHEN @MultipleServices = 1 THEN B.Service + ' - ' ELSE '' END + P.Priority AS Series,
		P.QueueLength
FROM #Buckets B
CROSS APPLY(VALUES('High',B.QueueLengthHigh,1),('Normal',B.QueueLengthNormal,2),('Low',B.QueueLengthLow,3)) P(Priority,QueueLength,Sort)
WHERE P.QueueLength IS NOT NULL
ORDER BY B.SnapshotDate, B.DBADashAgentID, P.Sort;
