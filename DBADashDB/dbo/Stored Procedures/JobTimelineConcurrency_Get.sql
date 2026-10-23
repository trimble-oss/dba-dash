CREATE PROC dbo.JobTimelineConcurrency_Get(
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@BucketSec INT,
	@category NVARCHAR(128)=NULL,
	@JobName NVARCHAR(128)=NULL,
	@MinDurationSec INT=0,
	@ProblemsOnly BIT=0
)
AS
/*
	How many jobs were running at once, for the band along the top of the job timeline - across every instance in
	@InstanceIDs, so it shows when work piles up (backups on shared storage all starting at 01:00, say).

	Executions are selected as in JobTimelineSummary_Get (job outcomes, plus jobs still running), with the same filters.
	Each one is +1 at its start and -1 at its finish (clipped to the period), and a running total over those events in
	time order gives the number running after each.  A finish sorts before a start at the same time, so a job starting
	as another ends isn't counted as overlapping it.

	The period is cut into @BucketSec slices - the GUI passes about a pixel's worth - and for each slice with any events
	this returns the most running at any moment in it (PeakRunning) and the number still running at its end (EndRunning).  A
	slice with no events has the previous slice's EndRunning throughout, which the caller fills in, so at most one row per
	slice comes back however many executions there are.
*/
SET NOCOUNT ON
DECLARE @AsOf DATETIME2(2) = CAST(GETUTCDATE() AS DATETIME2(2))
SET @BucketSec = CASE WHEN ISNULL(@BucketSec, 0) < 1 THEN 1 ELSE @BucketSec END
SET @MinDurationSec = ISNULL(@MinDurationSec, 0)

CREATE TABLE #Runs(
	RunDateTime DATETIME2(2) NOT NULL,
	FinishDateTime DATETIME2(2) NOT NULL
)

INSERT INTO #Runs(RunDateTime, FinishDateTime)
SELECT	JH.RunDateTime,
		CASE WHEN JH.FinishDateTime > JH.RunDateTime THEN JH.FinishDateTime ELSE DATEADD(s, 1, JH.RunDateTime) END
FROM @InstanceIDs I
JOIN dbo.Jobs J ON J.InstanceID = I.ID
JOIN dbo.JobHistory JH ON JH.InstanceID = J.InstanceID AND JH.job_id = J.job_id
WHERE JH.step_id = 0
AND JH.FinishDateTime >= @FromDate
AND JH.RunDateTime < @ToDate
AND JH.RunDateTime > DATEADD(d, -1, @FromDate)
AND JH.RunDurationSec >= @MinDurationSec
AND (@ProblemsOnly = 0 OR JH.run_status IN (0,2,3))
AND (@category IS NULL OR J.category = @category)
AND (@JobName IS NULL OR CHARINDEX(@JobName, J.name) > 0)
UNION ALL
SELECT	CAST(RJ.start_execution_date_utc AS DATETIME2(2)),
		CASE WHEN CAST(RJ.start_execution_date_utc AS DATETIME2(2)) >= OE.ObservedEnd THEN DATEADD(s, 1, CAST(RJ.start_execution_date_utc AS DATETIME2(2))) ELSE OE.ObservedEnd END
FROM @InstanceIDs I
JOIN dbo.RunningJobs RJ ON RJ.InstanceID = I.ID
JOIN dbo.Jobs J ON J.InstanceID = RJ.InstanceID AND J.job_id = RJ.job_id
CROSS APPLY (
		SELECT CASE WHEN RJ.SnapshotDate >= @AsOf THEN @AsOf ELSE CAST(RJ.SnapshotDate AS DATETIME2(2)) END AS ObservedEnd
	) OE
WHERE @ProblemsOnly = 0 /* running isn't a problem */
AND RJ.start_execution_date_utc IS NOT NULL
AND RJ.start_execution_date_utc < CAST(@ToDate AS DATETIME)
AND RJ.SnapshotDate >= CAST(@FromDate AS DATETIME)
AND (RJ.current_execution_status IS NULL OR RJ.current_execution_status <> 4) /* not idle */
AND DATEDIFF(s, RJ.start_execution_date_utc, OE.ObservedEnd) >= @MinDurationSec
AND (@category IS NULL OR J.category = @category)
AND (@JobName IS NULL OR CHARINDEX(@JobName, J.name) > 0)
AND NOT EXISTS (
		SELECT 1
		FROM dbo.JobHistory JH2
		WHERE JH2.InstanceID = RJ.InstanceID
		AND JH2.job_id = RJ.job_id
		AND JH2.step_id = 0
		AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		AND JH2.RunDateTime < @AsOf
	)

;WITH Events AS (
	SELECT CASE WHEN RunDateTime < @FromDate THEN @FromDate ELSE RunDateTime END AS EventTime, 1 AS Delta
	FROM #Runs
	UNION ALL
	SELECT CASE WHEN FinishDateTime > @ToDate THEN @ToDate ELSE FinishDateTime END, -1
	FROM #Runs
),
/*
	One change per instant: everything starting and finishing at the same time netted together.  The states part way
	through an instant last no time - a job starting as another finishes doesn't overlap it - and leaving them as separate
	rows with the same sort key made "the count at the end of a slice" depend on which of them happened to sort last.
*/
Changes AS (
	SELECT	EventTime,
			SUM(Delta) AS Delta
	FROM Events
	GROUP BY EventTime
),
Running AS (
	SELECT	EventTime,
			SUM(Delta) OVER (ORDER BY EventTime ROWS UNBOUNDED PRECEDING) AS Running
	FROM Changes
),
Buckets AS (
	/* Elapsed milliseconds rather than DATEDIFF(s), which counts second boundaries crossed - off by up to a second when
	   the period starts part way through one, putting an event near a slice boundary in the wrong slice */
	SELECT	DATEDIFF_BIG(ms, @FromDate, EventTime) / (CAST(@BucketSec AS BIGINT) * 1000) AS Bucket,
			EventTime,
			Running,
			LAG(Running, 1, 0) OVER (ORDER BY EventTime) AS Entering
	FROM Running
),
Ordered AS (
	SELECT	Bucket,
			EventTime,
			Running,
			Entering,
			DATEADD(s, CAST(Bucket AS INT) * @BucketSec, @FromDate) AS BucketStart,
			ROW_NUMBER() OVER (PARTITION BY Bucket ORDER BY EventTime DESC) AS FromEnd,
			ROW_NUMBER() OVER (PARTITION BY Bucket ORDER BY EventTime) AS FromStart
	FROM Buckets
)
SELECT	CAST(Bucket AS INT) AS Bucket,
		MIN(BucketStart) AS BucketStart,
		/* The most running at any moment in the slice: after each change in it, and the number running as it began - but
		   only if that lasted some time, i.e. the slice's first change isn't right at its start */
		CASE WHEN MAX(CASE WHEN FromStart = 1 AND EventTime > BucketStart THEN Entering END) > MAX(Running)
			THEN MAX(CASE WHEN FromStart = 1 AND EventTime > BucketStart THEN Entering END)
			ELSE MAX(Running)
		END AS PeakRunning,
		MAX(CASE WHEN FromEnd = 1 THEN Running END) AS EndRunning
FROM Ordered
GROUP BY Bucket
ORDER BY Bucket
