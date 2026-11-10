CREATE FUNCTION dbo.CollectionRunsBetweenDates(
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@UseDetail BIT
)
RETURNS TABLE
AS
/*
	Collection runs between two dates in the shape of dbo.CollectionRuns_60MIN, so callers can add them up the same way:
	each run where it was kept (dbo.CollectionRuns - LogInternalPerformanceCounters on), as a row of its own, and the hourly
	rollup for the runs that weren't.  Where every run in an hour was kept, the range is exact - it can start part way
	through the hour.  The rollup for the hours the range starts and ends in is counted whole.

	An hour with only some of its runs kept - LogInternalPerformanceCounters turned on or off part way through it - returns
	the runs kept, each as a row of its own, and a row for the rest: the hour's rollup less the runs kept.  So turning the
	setting on shows each run from then on, rather than from the next hour, and nothing is counted twice or left out.  The
	rest can't have a minimum or maximum of their own, so that row has the hour's - right for the hour as a whole, which
	includes the runs kept.

	One pass over the rollup: the runs kept for each hour are added up once, then read again for the rows of their own.
	@UseDetail = 0 reads the hourly rollup only - quicker over a long range, where hourly is all a chart can show anyway.
	IsDetail says which a row is: SnapshotDate is the run's start for a run, the start of the hour for the rollup.
	Join to the instances wanted - this returns all of them.
*/
RETURN
SELECT	H.InstanceID,
		H.ReferenceID,
		V.SnapshotDate,
		V.IsDetail,
		V.RunCount,
		V.SucceededCount,
		V.WarningCount,
		V.RetriedCount,
		V.PartialCount,
		V.FailedCount,
		V.ImportFailedCount,
		V.DurationCount,
		V.DurationTotalMs,
		V.DurationMinMs,
		V.DurationMaxMs,
		V.CpuCount,
		V.CpuTotalMs,
		V.CpuMinMs,
		V.CpuMaxMs,
		V.ReadsCount,
		V.ReadsTotal,
		V.ReadsMin,
		V.ReadsMax,
		V.ImportCount,
		V.ImportTotalMs,
		V.ImportMinMs,
		V.ImportMaxMs,
		V.ImportCpuCount,
		V.ImportCpuTotalMs,
		V.ImportCpuMinMs,
		V.ImportCpuMaxMs,
		V.ImportReadsCount,
		V.ImportReadsTotal,
		V.ImportReadsMin,
		V.ImportReadsMax,
		V.ImportLagCount,
		V.ImportLagTotalMs,
		V.ImportLagMinMs,
		V.ImportLagMaxMs
FROM dbo.CollectionRuns_60MIN H
/* The runs kept for the hour, added up - all of them, not only those in the range, as they're taken off the hour's rollup */
CROSS APPLY(SELECT	COUNT(*) AS RunCount,
					COUNT(CASE WHEN D.Status = 1 THEN 1 END) AS SucceededCount,
					COUNT(CASE WHEN D.Status = 2 THEN 1 END) AS WarningCount,
					COUNT(CASE WHEN D.Status = 3 THEN 1 END) AS RetriedCount,
					COUNT(CASE WHEN D.Status = 4 THEN 1 END) AS PartialCount,
					COUNT(CASE WHEN D.Status = 5 THEN 1 END) AS FailedCount,
					COUNT(CASE WHEN D.Status = 6 THEN 1 END) AS ImportFailedCount,
					COUNT(D.DurationMs) AS DurationCount,
					SUM(CAST(D.DurationMs AS BIGINT)) AS DurationTotalMs,
					COUNT(D.CpuMs) AS CpuCount,
					SUM(CAST(D.CpuMs AS BIGINT)) AS CpuTotalMs,
					COUNT(D.LogicalReads) AS ReadsCount,
					SUM(D.LogicalReads) AS ReadsTotal,
					COUNT(D.ImportMs) AS ImportCount,
					SUM(CAST(D.ImportMs AS BIGINT)) AS ImportTotalMs,
					COUNT(D.ImportCpuMs) AS ImportCpuCount,
					SUM(CAST(D.ImportCpuMs AS BIGINT)) AS ImportCpuTotalMs,
					COUNT(D.ImportLogicalReads) AS ImportReadsCount,
					SUM(D.ImportLogicalReads) AS ImportReadsTotal,
					COUNT(D.ImportLagMs) AS ImportLagCount,
					SUM(CAST(D.ImportLagMs AS BIGINT)) AS ImportLagTotalMs
			FROM dbo.CollectionRuns D
			WHERE @UseDetail = 1
			AND D.InstanceID = H.InstanceID
			AND D.ReferenceID = H.ReferenceID
			AND D.SnapshotDate >= H.SnapshotDate
			AND D.SnapshotDate < DATEADD(hh,1,H.SnapshotDate)
			) K
CROSS APPLY(/* Each run kept */
			SELECT	CR.SnapshotDate,
					CAST(1 AS BIT) AS IsDetail,
					1 AS RunCount,
					CASE WHEN CR.Status = 1 THEN 1 ELSE 0 END AS SucceededCount,
					CASE WHEN CR.Status = 2 THEN 1 ELSE 0 END AS WarningCount,
					CASE WHEN CR.Status = 3 THEN 1 ELSE 0 END AS RetriedCount,
					CASE WHEN CR.Status = 4 THEN 1 ELSE 0 END AS PartialCount,
					CASE WHEN CR.Status = 5 THEN 1 ELSE 0 END AS FailedCount,
					CASE WHEN CR.Status = 6 THEN 1 ELSE 0 END AS ImportFailedCount,
					CASE WHEN CR.DurationMs IS NULL THEN 0 ELSE 1 END AS DurationCount,
					CAST(CR.DurationMs AS BIGINT) AS DurationTotalMs,
					CR.DurationMs AS DurationMinMs,
					CR.DurationMs AS DurationMaxMs,
					CASE WHEN CR.CpuMs IS NULL THEN 0 ELSE 1 END AS CpuCount,
					CAST(CR.CpuMs AS BIGINT) AS CpuTotalMs,
					CR.CpuMs AS CpuMinMs,
					CR.CpuMs AS CpuMaxMs,
					CASE WHEN CR.LogicalReads IS NULL THEN 0 ELSE 1 END AS ReadsCount,
					CR.LogicalReads AS ReadsTotal,
					CR.LogicalReads AS ReadsMin,
					CR.LogicalReads AS ReadsMax,
					CASE WHEN CR.ImportMs IS NULL THEN 0 ELSE 1 END AS ImportCount,
					CAST(CR.ImportMs AS BIGINT) AS ImportTotalMs,
					CR.ImportMs AS ImportMinMs,
					CR.ImportMs AS ImportMaxMs,
					CASE WHEN CR.ImportCpuMs IS NULL THEN 0 ELSE 1 END AS ImportCpuCount,
					CAST(CR.ImportCpuMs AS BIGINT) AS ImportCpuTotalMs,
					CR.ImportCpuMs AS ImportCpuMinMs,
					CR.ImportCpuMs AS ImportCpuMaxMs,
					CASE WHEN CR.ImportLogicalReads IS NULL THEN 0 ELSE 1 END AS ImportReadsCount,
					CR.ImportLogicalReads AS ImportReadsTotal,
					CR.ImportLogicalReads AS ImportReadsMin,
					CR.ImportLogicalReads AS ImportReadsMax,
					CASE WHEN CR.ImportLagMs IS NULL THEN 0 ELSE 1 END AS ImportLagCount,
					CAST(CR.ImportLagMs AS BIGINT) AS ImportLagTotalMs,
					CR.ImportLagMs AS ImportLagMinMs,
					CR.ImportLagMs AS ImportLagMaxMs
			FROM dbo.CollectionRuns CR
			WHERE K.RunCount > 0
			AND CR.InstanceID = H.InstanceID
			AND CR.ReferenceID = H.ReferenceID
			AND CR.SnapshotDate >= H.SnapshotDate
			AND CR.SnapshotDate < DATEADD(hh,1,H.SnapshotDate)
			AND CR.SnapshotDate >= @FromDate
			AND CR.SnapshotDate < @ToDate
			UNION ALL
			/* The runs that weren't kept - the whole hour where none were */
			SELECT	H.SnapshotDate,
					CAST(0 AS BIT),
					H.RunCount - K.RunCount,
					H.SucceededCount - K.SucceededCount,
					H.WarningCount - K.WarningCount,
					H.RetriedCount - K.RetriedCount,
					H.PartialCount - K.PartialCount,
					H.FailedCount - K.FailedCount,
					H.ImportFailedCount - K.ImportFailedCount,
					H.DurationCount - K.DurationCount,
					H.DurationTotalMs - ISNULL(K.DurationTotalMs,0),
					H.DurationMinMs,
					H.DurationMaxMs,
					H.CpuCount - K.CpuCount,
					H.CpuTotalMs - ISNULL(K.CpuTotalMs,0),
					H.CpuMinMs,
					H.CpuMaxMs,
					H.ReadsCount - K.ReadsCount,
					H.ReadsTotal - ISNULL(K.ReadsTotal,0),
					H.ReadsMin,
					H.ReadsMax,
					H.ImportCount - K.ImportCount,
					H.ImportTotalMs - ISNULL(K.ImportTotalMs,0),
					H.ImportMinMs,
					H.ImportMaxMs,
					H.ImportCpuCount - K.ImportCpuCount,
					H.ImportCpuTotalMs - ISNULL(K.ImportCpuTotalMs,0),
					H.ImportCpuMinMs,
					H.ImportCpuMaxMs,
					H.ImportReadsCount - K.ImportReadsCount,
					H.ImportReadsTotal - ISNULL(K.ImportReadsTotal,0),
					H.ImportReadsMin,
					H.ImportReadsMax,
					H.ImportLagCount - K.ImportLagCount,
					H.ImportLagTotalMs - ISNULL(K.ImportLagTotalMs,0),
					H.ImportLagMinMs,
					H.ImportLagMaxMs
			WHERE K.RunCount < H.RunCount
			) V
WHERE H.SnapshotDate >= DATEADD(hh,DATEDIFF(hh,0,@FromDate),0)
AND H.SnapshotDate < @ToDate