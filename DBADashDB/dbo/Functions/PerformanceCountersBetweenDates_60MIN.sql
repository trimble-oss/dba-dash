CREATE FUNCTION dbo.PerformanceCountersBetweenDates_60MIN(
	@FromDate DATETIME2(7),
	@ToDate DATETIME2(7),
	@InstanceID INT,
	@CounterID INT,
	@CounterType TINYINT
)
RETURNS TABLE
AS
RETURN
/* Standard performance counters*/
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.Value_Total,
		PC.Value_Min,
		PC.Value_Max,
		PC.SampleCount
FROM dbo.PerformanceCounters_60MIN PC
WHERE @CounterType IN(1,5) /* 5: the DBADash collection counters were ordinary counters (1) before they were read from the collection runs - their history before then is here */
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(2))
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* Running Queries performance counters. No 60 MIN aggregation available so fake schema. */
SELECT	RQPC.InstanceID,
		RQPC.CounterID,
		RQPC.SnapshotDate,
		RQPC.Value AS Value_Total,
		RQPC.Value AS Value_Min,
		RQPC.Value AS Value_Max,
		1 AS SampleCount
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
		PC.value*SampleCount AS Value_Total,
		NULL AS Value_Min,
		PC.max_value AS Value_Max,
		PC.SampleCount
FROM dbo.AzureDBElasticPoolResourceStatsCounters_60MIN PC
WHERE @CounterType = 3
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(3))
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(3))
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* Azure DB Resource Stats performance counters. */
SELECT	PC.InstanceID,
		PC.CounterID,
		PC.SnapshotDate,
		PC.value*SampleCount AS Value_Total,
		NULL AS Value_Min,
		PC.max_value AS Value_Max,
		PC.SampleCount
FROM dbo.AzureDBResourceStatsCounters_60MIN PC
WHERE @CounterType = 4
AND PC.SnapshotDate >= CAST(@FromDate AS DATETIME2(3))
AND PC.SnapshotDate < CAST(@ToDate AS DATETIME2(3))
AND PC.InstanceID = @InstanceID
AND PC.CounterID = @CounterID
UNION ALL
/* DBADash collection counters from the hourly rollup of collection runs.  Collection Failed is 1 for a failed run and
   0 otherwise, so its average is the failure rate. */
SELECT	H.InstanceID,
		C.CounterID,
		H.SnapshotDate,
		M.Value_Total,
		M.Value_Min,
		M.Value_Max,
		M.SampleCount
FROM dbo.Counters C
JOIN dbo.CollectionRunReferences R ON R.Name = C.instance_name
JOIN dbo.CollectionRuns_60MIN H ON H.ReferenceID = R.ReferenceID
CROSS APPLY(SELECT	FailedCount = H.FailedCount + H.ImportFailedCount,
					KnownCount = H.SucceededCount + H.WarningCount + H.RetriedCount + H.PartialCount + H.FailedCount + H.ImportFailedCount
			) F
CROSS APPLY(SELECT CAST(H.DurationTotalMs AS DECIMAL(28,9)), CAST(H.DurationMinMs AS DECIMAL(28,9)), CAST(H.DurationMaxMs AS DECIMAL(28,9)), H.DurationCount
			WHERE C.counter_name = N'Collection Duration (ms)'
			UNION ALL
			SELECT CAST(H.CpuTotalMs AS DECIMAL(28,9)), CAST(H.CpuMinMs AS DECIMAL(28,9)), CAST(H.CpuMaxMs AS DECIMAL(28,9)), H.CpuCount
			WHERE C.counter_name = N'Collection CPU (ms)'
			UNION ALL
			SELECT CAST(H.ReadsTotal AS DECIMAL(28,9)), CAST(H.ReadsMin AS DECIMAL(28,9)), CAST(H.ReadsMax AS DECIMAL(28,9)), H.ReadsCount
			WHERE C.counter_name = N'Collection Logical Reads'
			UNION ALL
			SELECT CAST(H.ImportTotalMs AS DECIMAL(28,9)), CAST(H.ImportMinMs AS DECIMAL(28,9)), CAST(H.ImportMaxMs AS DECIMAL(28,9)), H.ImportCount
			WHERE C.counter_name = N'Collection Import Duration (ms)'
			UNION ALL
			SELECT CAST(H.ImportCpuTotalMs AS DECIMAL(28,9)), CAST(H.ImportCpuMinMs AS DECIMAL(28,9)), CAST(H.ImportCpuMaxMs AS DECIMAL(28,9)), H.ImportCpuCount
			WHERE C.counter_name = N'Collection Import CPU (ms)'
			UNION ALL
			SELECT CAST(H.ImportReadsTotal AS DECIMAL(28,9)), CAST(H.ImportReadsMin AS DECIMAL(28,9)), CAST(H.ImportReadsMax AS DECIMAL(28,9)), H.ImportReadsCount
			WHERE C.counter_name = N'Collection Import Logical Reads'
			UNION ALL
			SELECT CAST(H.ImportLagTotalMs AS DECIMAL(28,9)), CAST(H.ImportLagMinMs AS DECIMAL(28,9)), CAST(H.ImportLagMaxMs AS DECIMAL(28,9)), H.ImportLagCount
			WHERE C.counter_name = N'Collection Import Lag (ms)'
			UNION ALL
			SELECT	CAST(F.FailedCount AS DECIMAL(28,9)),
					CAST(CASE WHEN F.FailedCount = F.KnownCount THEN 1 ELSE 0 END AS DECIMAL(28,9)),
					CAST(CASE WHEN F.FailedCount > 0 THEN 1 ELSE 0 END AS DECIMAL(28,9)),
					F.KnownCount
			WHERE C.counter_name = N'Collection Failed'
			) M(Value_Total,Value_Min,Value_Max,SampleCount)
WHERE @CounterType = 5
AND C.CounterID = @CounterID
AND C.object_name = N'DBADash'
AND H.InstanceID = @InstanceID
AND H.SnapshotDate >= CAST(@FromDate AS DATETIME2(2))
AND H.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
AND M.SampleCount > 0