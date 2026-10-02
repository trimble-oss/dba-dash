CREATE PROC dbo.LatchStats_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2), /* UTC */
	@ToDate DATETIME2(2), /* UTC */
	@DateGroupingMin INT = NULL,
	@TopN INT = 10,
	@Measure VARCHAR(20) = 'WaitTime' /* WaitTime, WaitingRequests */
)
AS
/*
	Latch stats for an instance over a date range.
	Result 0: Summary by latch class
	Result 1: @Measure per second over time (as Value) for the top @TopN latch classes by that measure, with the remainder rolled into "Other"

	The collection is off by default.  If it has never run for the instance, a message is returned instead.
*/
SET NOCOUNT ON

IF @Measure NOT IN('WaitTime','WaitingRequests')
BEGIN;
	THROW 50003,'Invalid @Measure',1;
END

IF NOT EXISTS(	SELECT 1
				FROM dbo.CollectionDates
				WHERE InstanceID = @InstanceID
				AND Reference = 'LatchStats'
				)
BEGIN
	SELECT	CASE WHEN EXISTS(	SELECT 1
								FROM dbo.ScheduleInfo
								WHERE InstanceID = @InstanceID
								AND Reference = 'LatchStats'
								AND IsEnabled = 1
								)
				THEN 'Latch stats collection is enabled.  Data will be shown here once it has been collected.'
				ELSE 'Enable the LatchStats collection in the service config tool to see data here.' END AS Message
	RETURN
END

SET @TopN = ISNULL(NULLIF(@TopN,0),10)

/*	Bucket the time axis to roughly a hundred points across the range */
IF @DateGroupingMin IS NULL
BEGIN
	DECLARE @RangeMins INT = DATEDIFF(MINUTE, @FromDate, @ToDate)
	SET @DateGroupingMin = CASE
		WHEN @RangeMins <= 100 THEN 1
		WHEN @RangeMins <= 500 THEN 5
		WHEN @RangeMins <= 1500 THEN 15
		WHEN @RangeMins <= 6000 THEN 60
		WHEN @RangeMins <= 36000 THEN 360
		ELSE 1440 END
END
SET @DateGroupingMin = CASE WHEN @DateGroupingMin < 1 THEN 1 ELSE @DateGroupingMin END

/* The hourly aggregation is used where the buckets are an hour or more - nothing is lost by it at that grain */
DECLARE @Use60MIN BIT = CASE WHEN @DateGroupingMin >= 60 AND @DateGroupingMin % 60 = 0 THEN 1 ELSE 0 END

CREATE TABLE #Latch(
	LatchClassID SMALLINT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	waiting_requests_count BIGINT NOT NULL,
	wait_time_ms BIGINT NOT NULL,
	sample_ms_diff INT NOT NULL,
	/* The measure being charted, so ranking and the chart value don't need to repeat the CASE */
	measure BIGINT NULL,
	/* The chart bucket, which the peak rates in the summary are also taken over */
	DateGroup DATETIME2(2) NULL
)

IF @Use60MIN = 1
BEGIN
	INSERT INTO #Latch(LatchClassID,SnapshotDate,waiting_requests_count,wait_time_ms,sample_ms_diff)
	SELECT LatchClassID,SnapshotDate,waiting_requests_count,wait_time_ms,sample_ms_diff
	FROM dbo.LatchStats_60MIN
	WHERE InstanceID = @InstanceID
	AND SnapshotDate >= @FromDate
	AND SnapshotDate < @ToDate
END
ELSE
BEGIN
	INSERT INTO #Latch(LatchClassID,SnapshotDate,waiting_requests_count,wait_time_ms,sample_ms_diff)
	SELECT LatchClassID,SnapshotDate,waiting_requests_count,wait_time_ms,sample_ms_diff
	FROM dbo.LatchStats
	WHERE InstanceID = @InstanceID
	AND SnapshotDate >= @FromDate
	AND SnapshotDate < @ToDate
END

UPDATE L
SET L.measure = CASE @Measure WHEN 'WaitingRequests' THEN L.waiting_requests_count ELSE L.wait_time_ms END,
	L.DateGroup = DG.DateGroup
FROM #Latch L
CROSS APPLY dbo.DateGroupingMins(L.SnapshotDate,@DateGroupingMin) DG

DECLARE @TotalWaitMs DECIMAL(38,3)
SELECT @TotalWaitMs = SUM(wait_time_ms)
FROM #Latch

/*	Rates are per second of time the collection covers, as in the Waits report.  The collection is off by default, so
	it may be switched on and off around an investigation - dividing by the date range would understate the rate.
	Each row's sample_ms_diff is the time since the previous snapshot (an hour's worth of snapshots in the hourly table),
	and is the same for every row of a snapshot, so it's counted once per snapshot.  This covers gaps in collection,
	partial buckets at the ends of the range, and a long interval after the service or collection was stopped (which is
	averaged over its length rather than shown as a spike).
	A snapshot where no latch class was waited on produces no rows, so its time isn't counted.  That only happens on an
	instance with almost no latch activity, where it inflates rates that are tiny anyway.  */
SELECT	SnapshotDate,
		DateGroup,
		MAX(sample_ms_diff) AS sample_ms_diff
INTO #Snapshots
FROM #Latch
GROUP BY SnapshotDate,
		DateGroup

DECLARE @CoveredSec DECIMAL(38,3)
SELECT @CoveredSec = NULLIF(SUM(CAST(sample_ms_diff AS BIGINT)),0)/1000.0
FROM #Snapshots

/* Seconds covered by each chart bucket - for the chart, and for the peak rates in the summary */
SELECT	DateGroup,
		SUM(CAST(sample_ms_diff AS BIGINT))/1000.0 AS CoveredSec
INTO #Coverage
FROM #Snapshots
GROUP BY DateGroup

/*	Peak rate for each latch class - the highest of its rates in the chart buckets.  The summary rates are averages
	over the whole range, so a burst of contention can be lost in them on a longer range. */
SELECT	B.LatchClassID,
		MAX(B.wait_time_ms / C.CoveredSec) AS PeakWaitTimeMsPerSec
INTO #Peak
FROM (	SELECT	LatchClassID,
				DateGroup,
				SUM(wait_time_ms) AS wait_time_ms
		FROM #Latch
		GROUP BY LatchClassID,
				DateGroup
	) B
JOIN #Coverage C ON C.DateGroup = B.DateGroup
GROUP BY B.LatchClassID

/* Wait time is also given per core for context, as in the Waits report */
DECLARE @SchedulerCount INT
SELECT @SchedulerCount = NULLIF(scheduler_count,0)
FROM dbo.Instances
WHERE InstanceID = @InstanceID

/* 0 - Summary by latch class.  Rates first - the totals are hidden in the report but can be added back by the user. */
SELECT	LC.LatchClass,
		SUM(L.wait_time_ms)/@CoveredSec AS WaitTimeMsPerSec,
		P.PeakWaitTimeMsPerSec,
		SUM(L.wait_time_ms)/@CoveredSec/@SchedulerCount AS WaitTimeMsPerCorePerSec,
		SUM(L.wait_time_ms)/NULLIF(@TotalWaitMs,0) AS PctOfTotal,
		SUM(L.waiting_requests_count)/@CoveredSec AS WaitingRequestsPerSec,
		SUM(L.wait_time_ms)*1.0/NULLIF(SUM(L.waiting_requests_count),0) AS AvgWaitMs,
		S.max_wait_time_ms AS MaxWaitMs,
		SUM(L.wait_time_ms)/1000.0 AS TotalWaitSec,
		SUM(L.waiting_requests_count) AS WaitingRequests,
		LC.Description
FROM #Latch L
JOIN dbo.LatchClass LC ON LC.LatchClassID = L.LatchClassID
JOIN #Peak P ON P.LatchClassID = L.LatchClassID
LEFT JOIN Staging.LatchStats S ON S.InstanceID = @InstanceID AND S.latch_class = LC.LatchClass
GROUP BY LC.LatchClass,
		LC.Description,
		P.PeakWaitTimeMsPerSec,
		S.max_wait_time_ms
ORDER BY SUM(L.measure) DESC

/*	1 - Over time.  Values are divided by the time the bucket's snapshots cover (see above) rather than the width of
	the bucket, so a bucket the collection only partly covers isn't understated. */
;WITH TopClasses AS (
	SELECT TOP(@TopN) LatchClassID
	FROM #Latch
	GROUP BY LatchClassID
	HAVING SUM(measure) > 0
	ORDER BY SUM(measure) DESC
)
SELECT	L.DateGroup AS SnapshotDate,
		CASE WHEN T.LatchClassID IS NULL THEN 'Other' ELSE LC.LatchClass END AS LatchClass,
		SUM(L.measure) / MAX(C.CoveredSec) AS Value,
		SUM(L.wait_time_ms) / MAX(C.CoveredSec) AS WaitTimeMsPerSec,
		SUM(L.waiting_requests_count) / MAX(C.CoveredSec) AS WaitingRequestsPerSec,
		SUM(L.wait_time_ms)*1.0/NULLIF(SUM(L.waiting_requests_count),0) AS AvgWaitMs
FROM #Latch L
JOIN dbo.LatchClass LC ON LC.LatchClassID = L.LatchClassID
LEFT JOIN TopClasses T ON T.LatchClassID = L.LatchClassID
JOIN #Coverage C ON C.DateGroup = L.DateGroup
GROUP BY L.DateGroup,
		CASE WHEN T.LatchClassID IS NULL THEN 'Other' ELSE LC.LatchClass END
ORDER BY SnapshotDate,LatchClass
