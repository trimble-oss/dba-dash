CREATE PROC dbo.SpinlockStats_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2), /* UTC */
	@ToDate DATETIME2(2), /* UTC */
	@DateGroupingMin INT = NULL,
	@TopN INT = 10,
	@Measure VARCHAR(20) = 'Spins' /* Spins, Backoffs, Collisions, SleepTime */
)
AS
/*
	Spinlock stats for an instance over a date range.
	Result 0: Summary by spinlock
	Result 1: @Measure per second over time (as Value) for the top @TopN spinlocks by that measure, with the remainder rolled into "Other"

	Spins are the CPU burned spinning, so they show which spinlocks cost the most.  Backoffs are where spinning
	gave up and the thread slept - a sign the contention is bad enough to hurt, rather than just a busy spinlock.

	The collection is off by default.  If it has never run for the instance, a message is returned instead.
*/
SET NOCOUNT ON

IF @Measure NOT IN('Spins','Backoffs','Collisions','SleepTime')
BEGIN;
	THROW 50003,'Invalid @Measure',1;
END

IF NOT EXISTS(	SELECT 1
				FROM dbo.CollectionDates
				WHERE InstanceID = @InstanceID
				AND Reference = 'SpinlockStats'
				)
BEGIN
	SELECT	CASE WHEN EXISTS(	SELECT 1
								FROM dbo.ScheduleInfo
								WHERE InstanceID = @InstanceID
								AND Reference = 'SpinlockStats'
								AND IsEnabled = 1
								)
				THEN 'Spinlock stats collection is enabled.  Data will be shown here once it has been collected.'
				ELSE 'Enable the SpinlockStats collection in the service config tool to see data here.' END AS Message
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

CREATE TABLE #Spinlock(
	SpinlockID SMALLINT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	collisions BIGINT NOT NULL,
	spins BIGINT NOT NULL,
	sleep_time BIGINT NOT NULL,
	backoffs BIGINT NOT NULL,
	sample_ms_diff INT NOT NULL,
	/* The measure being charted, so ranking and the chart value don't need to repeat the CASE */
	measure BIGINT NULL,
	/* The chart bucket, which the peak rates in the summary are also taken over */
	DateGroup DATETIME2(2) NULL
)

IF @Use60MIN = 1
BEGIN
	INSERT INTO #Spinlock(SpinlockID,SnapshotDate,collisions,spins,sleep_time,backoffs,sample_ms_diff)
	SELECT SpinlockID,SnapshotDate,collisions,spins,sleep_time,backoffs,sample_ms_diff
	FROM dbo.SpinlockStats_60MIN
	WHERE InstanceID = @InstanceID
	AND SnapshotDate >= @FromDate
	AND SnapshotDate < @ToDate
END
ELSE
BEGIN
	INSERT INTO #Spinlock(SpinlockID,SnapshotDate,collisions,spins,sleep_time,backoffs,sample_ms_diff)
	SELECT SpinlockID,SnapshotDate,collisions,spins,sleep_time,backoffs,sample_ms_diff
	FROM dbo.SpinlockStats
	WHERE InstanceID = @InstanceID
	AND SnapshotDate >= @FromDate
	AND SnapshotDate < @ToDate
END

UPDATE S
SET S.measure = CASE @Measure WHEN 'Spins' THEN S.spins WHEN 'Backoffs' THEN S.backoffs WHEN 'Collisions' THEN S.collisions ELSE S.sleep_time END,
	S.DateGroup = DG.DateGroup
FROM #Spinlock S
CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate,@DateGroupingMin) DG

DECLARE @TotalSpins DECIMAL(38,3)
SELECT @TotalSpins = SUM(spins)
FROM #Spinlock

/*	Rates are per second of time the collection covers, as in the Waits report.  The collection is off by default, so
	it may be switched on and off around an investigation - dividing by the date range would understate the rate.
	Each row's sample_ms_diff is the time since the previous snapshot (an hour's worth of snapshots in the hourly table),
	and is the same for every row of a snapshot, so it's counted once per snapshot.  This covers gaps in collection,
	partial buckets at the ends of the range, and a long interval after the service or collection was stopped (which is
	averaged over its length rather than shown as a spike).
	A snapshot where no spinlock had any activity produces no rows, so its time isn't counted - in practice spinlocks
	are busy enough on any running instance for this not to matter.  */
SELECT	SnapshotDate,
		DateGroup,
		MAX(sample_ms_diff) AS sample_ms_diff
INTO #Snapshots
FROM #Spinlock
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

/*	Spinning burns CPU, so spins are also given per CPU - the same rate is a much bigger share of a small server.
	scheduler_count (user schedulers configured in the SQL Server process) rather than cpu_count (logical CPUs on the
	machine), as spinning happens on SQL Server's schedulers - consistent with the per core wait time in the Waits and
	Latches reports.  The two are usually the same, but cpu_count understates the rate where SQL Server can't use all
	of the machine's CPUs. */
DECLARE @CPUCount INT
SELECT @CPUCount = NULLIF(scheduler_count,0)
FROM dbo.Instances
WHERE InstanceID = @InstanceID

/*	Spins per ms per CPU above this is flagged as likely causing high CPU.  This is the threshold SQL Nexus uses
	(usp_Spinlock_HighCPU), including excluding the "dbcc execution" spinlocks.  Nexus divides by cpu_count. */
DECLARE @HighSpinsPerMsPerCPU INT = 10000

/*	Peak rates for each spinlock - the highest of its rates in the chart buckets, for the two measures that are
	highlighted.  The summary rates are averages over the whole range, so a burst of contention can be lost in them on a
	longer range. */
SELECT	B.SpinlockID,
		MAX(B.spins / (C.CoveredSec*1000) / @CPUCount) AS PeakSpinsPerMsPerCPU,
		MAX(B.sleep_time / C.CoveredSec) AS PeakSleepTimeMsPerSec
INTO #Peak
FROM (	SELECT	SpinlockID,
				DateGroup,
				SUM(spins) AS spins,
				SUM(sleep_time) AS sleep_time
		FROM #Spinlock
		GROUP BY SpinlockID,
				DateGroup
	) B
JOIN #Coverage C ON C.DateGroup = B.DateGroup
GROUP BY B.SpinlockID

/* 0 - Summary by spinlock.  Rates first - the totals are hidden in the report but can be added back by the user. */
SELECT	SL.SpinlockName,
		SUM(S.spins)/@CoveredSec AS SpinsPerSec,
		SUM(S.spins)/(@CoveredSec*1000)/@CPUCount AS SpinsPerMsPerCPU,
		CASE WHEN SUM(S.spins)/(@CoveredSec*1000)/@CPUCount > @HighSpinsPerMsPerCPU
				AND SL.SpinlockName NOT LIKE '%dbcc execution%'
			THEN 1 ELSE 0 END AS IsHighCPUSpinlock,
		P.PeakSpinsPerMsPerCPU,
		CASE WHEN P.PeakSpinsPerMsPerCPU > @HighSpinsPerMsPerCPU
				AND SL.SpinlockName NOT LIKE '%dbcc execution%'
			THEN 1 ELSE 0 END AS IsHighCPUSpinlockPeak,
		SUM(S.spins)/NULLIF(@TotalSpins,0) AS PctOfTotal,
		SUM(S.collisions)/@CoveredSec AS CollisionsPerSec,
		SUM(S.spins)*1.0/NULLIF(SUM(S.collisions),0) AS SpinsPerCollision,
		SUM(S.backoffs)/@CoveredSec AS BackoffsPerSec,
		SUM(S.sleep_time)/@CoveredSec AS SleepTimeMsPerSec,
		P.PeakSleepTimeMsPerSec,
		SUM(S.spins) AS Spins,
		SUM(S.collisions) AS Collisions,
		SUM(S.backoffs) AS Backoffs,
		SUM(S.sleep_time) AS SleepTimeMs,
		SL.Description
FROM #Spinlock S
JOIN dbo.Spinlock SL ON SL.SpinlockID = S.SpinlockID
JOIN #Peak P ON P.SpinlockID = S.SpinlockID
GROUP BY SL.SpinlockName,
		SL.Description,
		P.PeakSpinsPerMsPerCPU,
		P.PeakSleepTimeMsPerSec
ORDER BY SUM(S.measure) DESC

/*	1 - Over time.  Values are divided by the time the bucket's snapshots cover (see above) rather than the width of
	the bucket, so a bucket the collection only partly covers isn't understated. */
;WITH TopSpinlocks AS (
	SELECT TOP(@TopN) SpinlockID
	FROM #Spinlock
	GROUP BY SpinlockID
	HAVING SUM(measure) > 0
	ORDER BY SUM(measure) DESC
)
SELECT	S.DateGroup AS SnapshotDate,
		CASE WHEN T.SpinlockID IS NULL THEN 'Other' ELSE SL.SpinlockName END AS SpinlockName,
		SUM(S.measure) / MAX(C.CoveredSec) AS Value,
		SUM(S.spins) / MAX(C.CoveredSec) AS SpinsPerSec,
		SUM(S.collisions) / MAX(C.CoveredSec) AS CollisionsPerSec,
		SUM(S.backoffs) / MAX(C.CoveredSec) AS BackoffsPerSec,
		SUM(S.sleep_time) / MAX(C.CoveredSec) AS SleepTimeMsPerSec,
		SUM(S.spins)*1.0/NULLIF(SUM(S.collisions),0) AS SpinsPerCollision
FROM #Spinlock S
JOIN dbo.Spinlock SL ON SL.SpinlockID = S.SpinlockID
LEFT JOIN TopSpinlocks T ON T.SpinlockID = S.SpinlockID
JOIN #Coverage C ON C.DateGroup = S.DateGroup
GROUP BY S.DateGroup,
		CASE WHEN T.SpinlockID IS NULL THEN 'Other' ELSE SL.SpinlockName END
ORDER BY SnapshotDate,SpinlockName
