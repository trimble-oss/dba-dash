CREATE PROC dbo.CollectionPerformance_Get(
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2) = NULL, /* UTC */
	@ToDate DATETIME2(2) = NULL, /* UTC */
	@Reference VARCHAR(100) = NULL, /* The collection.  NULL for all of them */
	@Step VARCHAR(27) = NULL, /* A step that follows the collection (Text, Plans...).  '' for the collection itself, NULL for both */
	@DateGroupingMin INT = NULL, /* Chart buckets.  NULL picks from the range - finer than an hour only where each run is kept */
	@DetailMaxHours INT = 48, /* Read each run (where kept) for ranges up to this long.  Longer ranges read the hourly rollup only */
	@MaxRows INT = 5000 /* Rows in the grid - the latest */
)
AS
/*
	A collection's performance over time - drilled into from the Collection Health report.
	Each run where it was kept (LogInternalPerformanceCounters on) and the range is short enough, otherwise by the hour - see
	dbo.CollectionRunsBetweenDates.

	Result sets
	0	Runs (or hours) - latest first, up to @MaxRows
	1	Duration over time - collecting and importing, average and worst
	2	CPU over time - collecting (on the monitored instance) and importing (in the repository)
	3	Logical reads over time - collecting and importing
	4	Runs over time by outcome

	No import lag chart: the lag is the batch's (file's), the same for every collection in it, and how backed up the
	destination is rather than anything about the collection.  Collection Health charts it over all the collections.
	The grid has each run's.
*/
SET NOCOUNT ON

SET @ToDate = ISNULL(@ToDate,SYSUTCDATETIME())
SET @FromDate = ISNULL(@FromDate,DATEADD(d,-1,@ToDate))
DECLARE @RangeMins INT = DATEDIFF(mi,@FromDate,@ToDate)
DECLARE @UseDetail BIT = CASE WHEN @RangeMins <= @DetailMaxHours * 60 THEN 1 ELSE 0 END
IF @DateGroupingMin IS NULL
BEGIN
	SELECT @DateGroupingMin = DateGroupingMin
	FROM dbo.ChartDateGroupingMin(@RangeMins,200) /* As the CPU and IO charts - 1 minute buckets for an hour */
END

CREATE TABLE #Instances(
	InstanceID INT NOT NULL PRIMARY KEY
)

IF EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO #Instances(InstanceID)
	SELECT I.InstanceID
	FROM dbo.Instances I
	JOIN @InstanceIDs T ON T.ID = I.InstanceID
	WHERE I.IsActive = 1
END
ELSE
BEGIN
	INSERT INTO #Instances(InstanceID)
	SELECT InstanceID
	FROM dbo.Instances
	WHERE IsActive = 1
END

SELECT	F.*,
		R.Name,
		DATEADD(mi,DATEDIFF(mi,0,F.SnapshotDate) / @DateGroupingMin * @DateGroupingMin,0) AS Bucket
INTO #Source
FROM dbo.CollectionRunsBetweenDates(@FromDate,@ToDate,@UseDetail) F
JOIN #Instances I ON I.InstanceID = F.InstanceID
JOIN dbo.CollectionRunReferences R ON R.ReferenceID = F.ReferenceID
WHERE (R.Reference = @Reference OR @Reference IS NULL)
AND (R.Step = @Step OR @Step IS NULL)

/* Only the hourly rollup - buckets smaller than an hour would leave gaps */
IF @DateGroupingMin < 60 AND NOT EXISTS(SELECT 1 FROM #Source WHERE IsDetail = 1)
BEGIN
	UPDATE #Source
		SET Bucket = DATEADD(hh,DATEDIFF(hh,0,SnapshotDate),0)
END

/* 0 - Runs, or hours where the runs weren't kept.  Status as the other DBA Dash statuses: 1 critical, 2 warning, 3 N/A, 4 OK */
SELECT TOP(@MaxRows)
		S.InstanceID,
		I.InstanceDisplayName,
		S.Name,
		S.SnapshotDate,
		CASE WHEN S.IsDetail = 1 THEN 'Run' ELSE 'Hour' END AS Period,
		CASE	WHEN S.IsDetail = 0 THEN NULL
				WHEN S.SucceededCount = 1 THEN 'Succeeded'
				WHEN S.WarningCount = 1 THEN 'Warning'
				WHEN S.RetriedCount = 1 THEN 'Retried'
				WHEN S.PartialCount = 1 THEN 'Partial'
				WHEN S.FailedCount = 1 THEN 'Failed'
				WHEN S.ImportFailedCount = 1 THEN 'Import Failed'
				ELSE 'Unknown' END AS Outcome,
		CASE	WHEN S.FailedCount + S.ImportFailedCount > 0 THEN 1
				WHEN S.WarningCount + S.RetriedCount + S.PartialCount > 0 THEN 2
				WHEN S.SucceededCount > 0 THEN 4
				ELSE 3 END AS Status,
		S.RunCount,
		S.FailedCount + S.ImportFailedCount AS FailedCount,
		S.RetriedCount + S.PartialCount AS RetriedOrPartialCount,
		S.DurationTotalMs * 1.0 / NULLIF(S.DurationCount,0) AS AvgDurationMs,
		S.DurationMaxMs AS MaxDurationMs,
		S.CpuTotalMs * 1.0 / NULLIF(S.CpuCount,0) AS AvgCpuMs,
		S.CpuMaxMs AS MaxCpuMs,
		S.ReadsTotal * 1.0 / NULLIF(S.ReadsCount,0) AS AvgLogicalReads,
		S.ImportTotalMs * 1.0 / NULLIF(S.ImportCount,0) AS AvgImportMs,
		S.ImportMaxMs AS MaxImportMs,
		S.ImportCpuTotalMs * 1.0 / NULLIF(S.ImportCpuCount,0) AS AvgImportCpuMs,
		S.ImportReadsTotal * 1.0 / NULLIF(S.ImportReadsCount,0) AS AvgImportLogicalReads,
		S.ImportLagTotalMs / 1000.0 / NULLIF(S.ImportLagCount,0) AS AvgImportLagSec,
		S.ImportLagMaxMs / 1000.0 AS MaxImportLagSec
FROM #Source S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
ORDER BY S.SnapshotDate DESC, I.InstanceDisplayName, S.Name

SELECT	Bucket AS SnapshotDate,
		SUM(SucceededCount + WarningCount) AS SucceededCount,
		SUM(RetriedCount + PartialCount) AS RetriedOrPartialCount,
		SUM(FailedCount + ImportFailedCount) AS FailedCount,
		SUM(DurationTotalMs) * 1.0 / NULLIF(SUM(DurationCount),0) AS AvgDurationMs,
		MAX(DurationMaxMs) AS MaxDurationMs,
		SUM(ImportTotalMs) * 1.0 / NULLIF(SUM(ImportCount),0) AS AvgImportMs,
		MAX(ImportMaxMs) AS MaxImportMs,
		SUM(CpuTotalMs) * 1.0 / NULLIF(SUM(CpuCount),0) AS AvgCpuMs,
		MAX(CpuMaxMs) AS MaxCpuMs,
		SUM(ImportCpuTotalMs) * 1.0 / NULLIF(SUM(ImportCpuCount),0) AS AvgImportCpuMs,
		SUM(ReadsTotal) * 1.0 / NULLIF(SUM(ReadsCount),0) AS AvgLogicalReads,
		SUM(ImportReadsTotal) * 1.0 / NULLIF(SUM(ImportReadsCount),0) AS AvgImportLogicalReads
INTO #Buckets
FROM #Source
GROUP BY Bucket

/* 1 - Duration */
SELECT B.SnapshotDate, M.Measure, M.Value
FROM #Buckets B
CROSS APPLY(VALUES	('Collect avg',B.AvgDurationMs),
					('Collect max',B.MaxDurationMs),
					('Import avg',B.AvgImportMs),
					('Import max',B.MaxImportMs)
			) M(Measure,Value)
WHERE M.Value IS NOT NULL
ORDER BY B.SnapshotDate, M.Measure

/* 2 - CPU */
SELECT B.SnapshotDate, M.Measure, M.Value
FROM #Buckets B
CROSS APPLY(VALUES	('Collect avg',B.AvgCpuMs),
					('Collect max',B.MaxCpuMs),
					('Import avg',B.AvgImportCpuMs)
			) M(Measure,Value)
WHERE M.Value IS NOT NULL
ORDER BY B.SnapshotDate, M.Measure

/* 3 - Logical reads */
SELECT B.SnapshotDate, M.Measure, M.Value
FROM #Buckets B
CROSS APPLY(VALUES	('Collect avg',B.AvgLogicalReads),
					('Import avg',B.AvgImportLogicalReads)
			) M(Measure,Value)
WHERE M.Value IS NOT NULL
ORDER BY B.SnapshotDate, M.Measure

/* 4 - Runs by outcome */
SELECT B.SnapshotDate, M.Outcome, M.RunCount
FROM #Buckets B
CROSS APPLY(VALUES	('Succeeded',B.SucceededCount),
					('Retried / Partial',B.RetriedOrPartialCount),
					('Failed',B.FailedCount)
			) M(Outcome,RunCount)
ORDER BY B.SnapshotDate, M.Outcome

