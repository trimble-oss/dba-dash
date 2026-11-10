CREATE FUNCTION dbo.PerformanceCountersBetweenDates(
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@InstanceID INT,
	@CounterID INT,
	@CounterType TINYINT
)
RETURNS TABLE
AS
RETURN
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.Value
FROM dbo.PerformanceCounters PC
WHERE @CounterType IN(1,5) /* 5: the DBADash collection counters were ordinary counters (1) before they were read from the collection runs - their history before then is here */
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(2)) /* Important that date type matches for performance */
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(2)) /* Important that date type matches for performance */
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* Running Queries performance counters. No 60 MIN aggregation available so fake schema. */
SELECT	RQPC.InstanceID,
		RQPC.CounterID,
		RQPC.SnapshotDate,
		RQPC.Value
FROM dbo.RunningQueriesPerformanceCounters RQPC
WHERE @CounterType = 2
AND RQPC.SnapshotDate >= @FromDate 
AND RQPC.SnapshotDate < @ToDate
AND RQPC.InstanceID = @InstanceID
AND RQPC.CounterID = @CounterID
UNION ALL
/* Azure DB Elastic Pool Resource Stats performance counters. */
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.value
FROM dbo.AzureDBElasticPoolResourceStatsCounters PC
WHERE @CounterType = 3
AND PC.SnapshotDate >= @FromDate
AND PC.SnapshotDate < @ToDate
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* Azure DB Resource Stats performance counters. */
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.value
FROM dbo.AzureDBResourceStatsCounters PC
WHERE @CounterType = 4
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(3))
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(3))
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* DBADash collection counters (CounterType 5), read from the collection runs.  One pass over the hourly rollup: each run kept
   for the hour (LogInternalPerformanceCounters on) as a value of its own, and the average of the runs that weren't - the
   hour's rollup less the runs kept, so the whole hour where none were.  Turning the setting on part way through an hour
   shows each run from then on, with the earlier runs as their average.  That average is dated from the start of the hour
   or of the range, whichever is later - so an hour the range starts part way through is still included.
   The runs kept for the hour are added up once, then read again for the values of their own. */
SELECT	H.InstanceID,
		C.CounterID,
		V.SnapshotDate,
		V.Value
FROM dbo.Counters C
JOIN dbo.CollectionRunReferences R ON R.Name = C.instance_name
JOIN dbo.CollectionRuns_60MIN H ON H.ReferenceID = R.ReferenceID
/* All the runs kept for the hour, not only those in the range - they're taken off the hour's rollup */
CROSS APPLY(SELECT	COUNT(*) AS RunCount,
					COUNT(CASE WHEN D.Status IN(5,6) THEN 1 END) AS FailedCount,
					COUNT(D.Status) AS KnownCount,
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
			WHERE D.InstanceID = H.InstanceID
			AND D.ReferenceID = R.ReferenceID /* R, not H: so the counter's collection is found first, and only its hours added up */
			AND D.SnapshotDate >= H.SnapshotDate
			AND D.SnapshotDate < DATEADD(hh,1,H.SnapshotDate)
			) K
CROSS APPLY(/* Each run kept */
			SELECT	CR.SnapshotDate,
					CAST(CASE C.counter_name
						WHEN N'Collection Duration (ms)' THEN CR.DurationMs
						WHEN N'Collection CPU (ms)' THEN CR.CpuMs
						WHEN N'Collection Logical Reads' THEN CR.LogicalReads
						WHEN N'Collection Failed' THEN CASE WHEN CR.Status IN(5,6) THEN 1 WHEN CR.Status IS NOT NULL THEN 0 END
						WHEN N'Collection Import Duration (ms)' THEN CR.ImportMs
						WHEN N'Collection Import CPU (ms)' THEN CR.ImportCpuMs
						WHEN N'Collection Import Logical Reads' THEN CR.ImportLogicalReads
						WHEN N'Collection Import Lag (ms)' THEN CR.ImportLagMs
						END AS DECIMAL(28,9)) AS Value
			FROM dbo.CollectionRuns CR
			WHERE K.RunCount > 0
			AND CR.InstanceID = H.InstanceID
			AND CR.ReferenceID = R.ReferenceID
			AND CR.SnapshotDate >= H.SnapshotDate
			AND CR.SnapshotDate < DATEADD(hh,1,H.SnapshotDate)
			AND CR.SnapshotDate >= CAST(@FromDate AS DATETIME2(2))
			AND CR.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
			UNION ALL
			/* The average of the runs that weren't kept */
			SELECT	CASE WHEN H.SnapshotDate < CAST(@FromDate AS DATETIME2(2)) THEN CAST(@FromDate AS DATETIME2(2)) ELSE H.SnapshotDate END,
					CAST(CASE C.counter_name
						WHEN N'Collection Duration (ms)' THEN (H.DurationTotalMs - ISNULL(K.DurationTotalMs,0)) * 1.0 / NULLIF(H.DurationCount - K.DurationCount,0)
						WHEN N'Collection CPU (ms)' THEN (H.CpuTotalMs - ISNULL(K.CpuTotalMs,0)) * 1.0 / NULLIF(H.CpuCount - K.CpuCount,0)
						WHEN N'Collection Logical Reads' THEN (H.ReadsTotal - ISNULL(K.ReadsTotal,0)) * 1.0 / NULLIF(H.ReadsCount - K.ReadsCount,0)
						WHEN N'Collection Failed' THEN (H.FailedCount + H.ImportFailedCount - K.FailedCount) * 1.0
														/ NULLIF(H.SucceededCount + H.WarningCount + H.RetriedCount + H.PartialCount + H.FailedCount + H.ImportFailedCount - K.KnownCount,0)
						WHEN N'Collection Import Duration (ms)' THEN (H.ImportTotalMs - ISNULL(K.ImportTotalMs,0)) * 1.0 / NULLIF(H.ImportCount - K.ImportCount,0)
						WHEN N'Collection Import CPU (ms)' THEN (H.ImportCpuTotalMs - ISNULL(K.ImportCpuTotalMs,0)) * 1.0 / NULLIF(H.ImportCpuCount - K.ImportCpuCount,0)
						WHEN N'Collection Import Logical Reads' THEN (H.ImportReadsTotal - ISNULL(K.ImportReadsTotal,0)) * 1.0 / NULLIF(H.ImportReadsCount - K.ImportReadsCount,0)
						WHEN N'Collection Import Lag (ms)' THEN (H.ImportLagTotalMs - ISNULL(K.ImportLagTotalMs,0)) * 1.0 / NULLIF(H.ImportLagCount - K.ImportLagCount,0)
						END AS DECIMAL(28,9))
			WHERE K.RunCount < H.RunCount
			) V
WHERE @CounterType = 5
AND C.CounterID = @CounterID
AND C.object_name = N'DBADash'
AND H.InstanceID = @InstanceID
AND H.SnapshotDate > DATEADD(hh,-1,CAST(@FromDate AS DATETIME2(2)))
AND H.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
AND V.Value IS NOT NULL