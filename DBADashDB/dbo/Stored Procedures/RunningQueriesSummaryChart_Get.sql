/*
	Running queries summary chart for an instance: one row per bar.
	Where the range has no more than @MaxBars snapshots each snapshot gets its own bar (@BucketSeconds returned as 0).
	Otherwise snapshots are grouped into time buckets.  The stacked bars are averages over the snapshots in the bucket (sum / snapshot count) so they
	keep their meaning at any bucket size and aren't shrunk by missing snapshots.  The peak snapshot (most running queries) is returned for each bucket
	so it can be marked on the chart and loaded directly.
	Line metrics are the MAX per bucket.  Wait times are how long each query had been waiting when the snapshot was taken, so one long wait appears
	in many snapshots and summing them would count it again for each snapshot.
*/
CREATE PROC dbo.RunningQueriesSummaryChart_Get(
	@InstanceID INT,
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@MaxBars INT = 200,
	@BucketSeconds INT = NULL OUTPUT
)
AS
SET NOCOUNT ON

DECLARE @SnapshotCount INT

SELECT @SnapshotCount = COUNT(*)
FROM dbo.RunningQueriesSummary
WHERE InstanceID = @InstanceID
AND SnapshotDateUTC >= @FromDate
AND SnapshotDateUTC < @ToDate

IF @SnapshotCount <= @MaxBars
BEGIN
	SET @BucketSeconds = 0
END
ELSE
BEGIN
	DECLARE @MinBucketSeconds BIGINT = CEILING(DATEDIFF_BIG(SECOND, @FromDate, @ToDate) / CAST(@MaxBars AS FLOAT))

	SELECT TOP(1) @BucketSeconds = B.Seconds
	FROM (VALUES(60),(120),(300),(600),(900),(1800),(3600),(7200),(10800),(14400),(21600),(43200),(86400),(172800),(604800)) B(Seconds)
	WHERE B.Seconds >= @MinBucketSeconds
	ORDER BY B.Seconds

	IF @BucketSeconds IS NULL
		SET @BucketSeconds = CAST(CEILING(@MinBucketSeconds / 86400.0) * 86400 AS INT)
END

/* Buckets are aligned to a fixed point in time so a bucket covers the same period whichever range it's viewed in */
DECLARE @Anchor DATETIME2(7) = '20000101'

;WITH S AS (
	SELECT	S.SnapshotDateUTC,
			CASE WHEN @BucketSeconds = 0 THEN S.SnapshotDateUTC
				ELSE DATEADD(SECOND, CAST(DATEDIFF_BIG(SECOND, @Anchor, S.SnapshotDateUTC) / NULLIF(@BucketSeconds, 0) * @BucketSeconds AS INT), @Anchor)
				END AS BucketStartUTC,
			S.RunningQueries,
			S.BlockedQueries,
			ISNULL(S.SleepingSessionsCount, 0) AS SleepingSessions,
			/* Sleeping sessions with an open transaction are included in RunningQueries but don't have a running request */
			CASE WHEN S.RunningQueries - S.BlockedQueries - ISNULL(S.SleepingSessionsCount, 0) < 0 THEN 0
				ELSE S.RunningQueries - S.BlockedQueries - ISNULL(S.SleepingSessionsCount, 0) END AS NotBlocked,
			S.SumMemoryGrant,
			S.MaxMemoryGrant,
			S.TempDBWaitTimeMs,
			S.BlockedQueriesWaitMs,
			S.LongestRunningQueryMs,
			/*	OldestTransactionMs is NULL where no session had an open transaction, and TempDBCurrentPageCount is NULL for a snapshot with no sessions.  Both
				are also NULL where the collector is older than the column.  So where any snapshot in the range has a value, NULL is 0 (the line drops to 0
				rather than joining up the values either side of it).  Where none do, the column wasn't collected and stays NULL - no line rather than a 0 one.
				The window MAX is over a 0/1 CASE so it doesn't raise the NULL elimination warning.
			*/
			CASE WHEN MAX(CASE WHEN S.OldestTransactionMs IS NULL THEN 0 ELSE 1 END) OVER() = 1 THEN ISNULL(S.OldestTransactionMs, 0) END AS OldestTransactionMs,
			CASE WHEN MAX(CASE WHEN S.TempDBCurrentPageCount IS NULL THEN 0 ELSE 1 END) OVER() = 1 THEN ISNULL(S.TempDBCurrentPageCount, 0) END AS TempDBCurrentPageCount,
			S.CriticalWaitTime
	FROM dbo.RunningQueriesSummary S
	WHERE S.InstanceID = @InstanceID
	AND S.SnapshotDateUTC >= @FromDate
	AND S.SnapshotDateUTC < @ToDate
),
R AS (
	SELECT	S.*,
			ROW_NUMBER() OVER(PARTITION BY S.BucketStartUTC ORDER BY S.RunningQueries DESC, S.SnapshotDateUTC DESC) AS PeakRank
	FROM S
),
/*	Nullable columns (NULL for snapshots collected before they were added) are aggregated through ISNULL/NULLIF rather than MAX(column)
	so the proc doesn't raise "Null value is eliminated by an aggregate" warnings.  NULL for a bucket where every snapshot is NULL.
*/
A AS (
	SELECT	R.BucketStartUTC,
			COUNT(*) AS SnapshotCount,
			AVG(CAST(R.NotBlocked AS FLOAT)) AS AvgNotBlocked,
			AVG(CAST(R.BlockedQueries AS FLOAT)) AS AvgBlocked,
			AVG(CAST(R.SleepingSessions AS FLOAT)) AS AvgSleeping,
			AVG(CAST(R.RunningQueries AS FLOAT)) AS AvgRunning,
			MAX(R.RunningQueries) AS PeakRunning,
			MAX(R.BlockedQueries) AS MaxBlocked,
			NULLIF(MAX(ISNULL(R.SumMemoryGrant, -1)), -1) * 8 / 1024.0 AS SumMemoryGrantMB,
			MAX(R.MaxMemoryGrant) * 8 / 1024.0 AS MaxMemoryGrantMB,
			MAX(R.TempDBWaitTimeMs) AS TempDBWaitTimeMs,
			MAX(R.BlockedQueriesWaitMs) AS BlockedQueriesWaitMs,
			MAX(R.LongestRunningQueryMs) / 1000.0 AS LongestRunningQuerySec,
			NULLIF(MAX(ISNULL(R.OldestTransactionMs, -1)), -1) / 1000.0 AS OldestTransactionSec,
			NULLIF(MAX(ISNULL(R.TempDBCurrentPageCount, -1)), -1) / 128.0 AS TempDBCurrentMB,
			MAX(R.CriticalWaitTime) AS CriticalWaitTimeMs
	FROM R
	GROUP BY R.BucketStartUTC
)
SELECT	A.BucketStartUTC,
		CASE WHEN @BucketSeconds = 0 THEN A.BucketStartUTC ELSE DATEADD(SECOND, @BucketSeconds, A.BucketStartUTC) END AS BucketEndUTC,
		/* Where the bar is drawn - the middle of the bucket */
		CASE WHEN @BucketSeconds = 0 THEN A.BucketStartUTC ELSE DATEADD(MILLISECOND, @BucketSeconds * 500, A.BucketStartUTC) END AS BucketMidUTC,
		A.SnapshotCount,
		A.AvgNotBlocked,
		A.AvgBlocked,
		A.AvgSleeping,
		A.AvgRunning,
		A.PeakRunning,
		/* The busiest snapshot in the bucket, joined rather than picked out with MAX(CASE...) which would raise the NULL warning */
		P.SnapshotDateUTC AS PeakSnapshotDateUTC,
		P.BlockedQueries AS PeakBlocked,
		A.MaxBlocked,
		A.SumMemoryGrantMB,
		A.MaxMemoryGrantMB,
		A.TempDBWaitTimeMs,
		A.BlockedQueriesWaitMs,
		A.LongestRunningQuerySec,
		A.OldestTransactionSec,
		A.TempDBCurrentMB,
		A.CriticalWaitTimeMs
FROM A
JOIN R AS P ON P.BucketStartUTC = A.BucketStartUTC AND P.PeakRank = 1
ORDER BY A.BucketStartUTC
