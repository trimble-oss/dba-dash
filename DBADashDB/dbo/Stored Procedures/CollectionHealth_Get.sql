CREATE PROC dbo.CollectionHealth_Get(
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2) = NULL, /* UTC.  Exact where each run is kept, otherwise rounded down to the hour (see dbo.CollectionRunsBetweenDates) */
	@ToDate DATETIME2(2) = NULL, /* UTC */
	@DateGroupingMin INT = NULL, /* Chart buckets.  NULL picks from the range - finer than an hour only where each run is kept */
	@DetailMaxHours INT = 48, /* Read each run (where kept) for ranges up to this long.  Longer ranges read the hourly rollup only */
	@GroupByInstance BIT = 1, /* 0 to show each collection once, over all the instances selected */
	@IncludeSteps BIT = 1, /* The steps that follow some collections - fetching text and plans for QueryStats, say */
	@Reference VARCHAR(100) = NULL, /* Restrict to one collection (and its steps with @Step NULL) */
	@Step VARCHAR(27) = NULL,
	@IncludeCritical BIT = 1,
	@IncludeWarning BIT = 1,
	@IncludeNA BIT = 1,
	@IncludeOK BIT = 1
)
AS
/*
	How well each collection is working over a period: how many runs succeeded, failed or needed a retry, how many
	have failed in a row now, and what the collection costs - duration, and CPU and logical reads on the monitored
	instance where the service measures them (LogInternalPerformanceCounters).

	Status is from the failures in a row now and the failure rate over the period rather than whether the last attempt
	failed - dbo.CollectionHealthStatus, as the summary, with the thresholds configured in dbo.CollectionHealthThresholds.
	N/A where there are no runs with a status (only durations from an older service version), the collection's health
	isn't monitored, its schedule is disabled or the instance's collections are paused - as the summary, with the runs still
	shown.  Over all the instances (@GroupByInstance = 0), the worst of the instances.

	A collection failing now is listed even with no runs over the period, so a short period can't hide what the summary counts.
	FailedPct24Hrs is the failure rate over the summary's period (dbo.CollectionHealthRateFrom), for when the summary's status
	is from a rate the report's period doesn't show.

	What to do about the instances' errors over the period, where there's something to suggest (dbo.CollectionErrorSuggestion),
	is a card at the top of the report rather than a column - one card for all the instances (dbo.CollectionErrorInsight).

	Each run is read where it was kept (LogInternalPerformanceCounters on) and the range is short enough (@DetailMaxHours), so
	the charts can show buckets of a few minutes.  Otherwise the hourly rollup - charts by the hour or more.  Should some
	instances keep each run and others not, the hourly figures land in the bucket at the start of each hour.

	Result sets
	0	A row per collection - per instance, or over all of them with @GroupByInstance = 0
	1	Runs by date bucket and outcome over all the collections selected, for a chart
	2	Total collect and import duration and CPU by date bucket over all the collections selected, for a chart
	3	Import lag (sent to a destination to import) by date bucket over the collections selected, for a chart
	4	Insights - a card at the top of the report for each row (CustomReport.ShowInsights)
*/
SET NOCOUNT ON

SET @ToDate = ISNULL(@ToDate,SYSUTCDATETIME())
SET @FromDate = ISNULL(@FromDate,DATEADD(d,-1,@ToDate))
DECLARE @FromHour DATETIME2(2) = DATEADD(hh,DATEDIFF(hh,0,@FromDate),0)
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

CREATE TABLE #References(
	ReferenceID INT NOT NULL PRIMARY KEY,
	Reference VARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
	Step VARCHAR(27) COLLATE DATABASE_DEFAULT NOT NULL,
	Name NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL
)

INSERT INTO #References(ReferenceID,Reference,Step,Name)
SELECT ReferenceID, Reference, Step, Name
FROM dbo.CollectionRunReferences
WHERE (Step = '' OR @IncludeSteps = 1)
AND (Reference = @Reference OR @Reference IS NULL)
AND (Step = @Step OR @Step IS NULL)

SELECT	F.*
INTO #Source
FROM dbo.CollectionRunsBetweenDates(@FromDate,@ToDate,@UseDetail) F
JOIN #Instances I ON I.InstanceID = F.InstanceID
JOIN #References R ON R.ReferenceID = F.ReferenceID

/* Only the hourly rollup - buckets smaller than an hour would leave gaps */
IF @DateGroupingMin < 60 AND NOT EXISTS(SELECT 1 FROM #Source WHERE IsDetail = 1)
BEGIN
	SET @DateGroupingMin = 60
END

/* Runs over the period, per instance and collection */
SELECT	H.InstanceID,
		H.ReferenceID,
		SUM(H.RunCount) AS RunCount,
		SUM(H.SucceededCount) AS SucceededCount,
		SUM(H.WarningCount) AS WarningCount,
		SUM(H.RetriedCount) AS RetriedCount,
		SUM(H.PartialCount) AS PartialCount,
		SUM(H.FailedCount) AS FailedCount,
		SUM(H.ImportFailedCount) AS ImportFailedCount,
		SUM(H.DurationCount) AS DurationCount,
		SUM(H.DurationTotalMs) AS DurationTotalMs,
		MAX(H.DurationMaxMs) AS DurationMaxMs,
		SUM(H.CpuCount) AS CpuCount,
		SUM(H.CpuTotalMs) AS CpuTotalMs,
		MAX(H.CpuMaxMs) AS CpuMaxMs,
		SUM(H.ReadsCount) AS ReadsCount,
		SUM(H.ReadsTotal) AS ReadsTotal,
		MAX(H.ReadsMax) AS ReadsMax,
		SUM(H.ImportCount) AS ImportCount,
		SUM(H.ImportTotalMs) AS ImportTotalMs,
		MAX(H.ImportMaxMs) AS ImportMaxMs,
		SUM(H.ImportCpuCount) AS ImportCpuCount,
		SUM(H.ImportCpuTotalMs) AS ImportCpuTotalMs,
		SUM(H.ImportReadsCount) AS ImportReadsCount,
		SUM(H.ImportReadsTotal) AS ImportReadsTotal,
		SUM(H.ImportLagCount) AS ImportLagCount,
		SUM(H.ImportLagTotalMs) AS ImportLagTotalMs,
		MAX(H.ImportLagMaxMs) AS ImportLagMaxMs
INTO #Runs
FROM #Source H
GROUP BY H.InstanceID, H.ReferenceID

/* Collections failing now are listed whatever the period, as the summary counts them.  A weekly collection that failed at the
   weekend has no runs in the last hour - no runs over the period, so its status is from the failures in a row. */
INSERT INTO #Runs(InstanceID,ReferenceID,RunCount,SucceededCount,WarningCount,RetriedCount,PartialCount,FailedCount,ImportFailedCount)
SELECT	S.InstanceID,
		S.ReferenceID,
		0,0,0,0,0,0,0
FROM dbo.CollectionRunStatus S
JOIN #Instances I ON I.InstanceID = S.InstanceID
JOIN #References R ON R.ReferenceID = S.ReferenceID
WHERE S.ConsecutiveFailures > 0
AND NOT EXISTS(	SELECT 1
				FROM #Runs Run
				WHERE Run.InstanceID = S.InstanceID
				AND Run.ReferenceID = S.ReferenceID
				)

/* The failure rate over the summary's period (dbo.CollectionHealthRateFrom) - the report's period can be shorter.  As the
   summary, only read for collections that failed or partly failed in it: the rest had no failures. */
DECLARE @RateFrom DATETIME2(2)
SELECT @RateFrom = RateFrom
FROM dbo.CollectionHealthRateFrom()

SELECT	Run.InstanceID,
		Run.ReferenceID,
		SUM(H.SucceededCount + H.WarningCount + H.RetriedCount + H.PartialCount + H.FailedCount + H.ImportFailedCount) AS KnownCount,
		SUM(H.FailedCount + H.ImportFailedCount) AS FailedCount
INTO #SummaryRates
FROM #Runs Run
JOIN dbo.CollectionRunStatus S ON S.InstanceID = Run.InstanceID AND S.ReferenceID = Run.ReferenceID
JOIN dbo.CollectionRuns_60MIN H ON H.InstanceID = Run.InstanceID AND H.ReferenceID = Run.ReferenceID
WHERE (S.LastFailureDate >= @RateFrom OR S.LastPartialDate >= @RateFrom)
AND H.SnapshotDate >= @RateFrom
GROUP BY Run.InstanceID, Run.ReferenceID

/* Errors logged for each collection over the period.  Steps log their errors against the collection they follow. */
SELECT	E.InstanceID,
		E.ErrorSource,
		COUNT(*) AS ErrorCount,
		COUNT(CASE WHEN C.ErrorStage = 'WMI' THEN 1 END) AS WMIErrorCount
INTO #Errors
FROM dbo.CollectionErrorLog E
JOIN #Instances I ON I.InstanceID = E.InstanceID
CROSS APPLY dbo.CollectionErrorClassify(E.ErrorSource,E.ErrorContext,E.ErrorMessage) C
WHERE E.ErrorDate >= @FromDate
AND E.ErrorDate < @ToDate
AND ISNULL(E.ErrorContext,'') NOT LIKE '%[[]Retry%]' /* As dbo.CollectionErrorLog_Get with @IncludeRetries = 0, so the link shows these */
GROUP BY E.InstanceID, E.ErrorSource

/* The instances there's something to suggest for (dbo.CollectionErrorSuggestion) - for the card at the end */
SELECT	W.InstanceID,
		W.WMISourceCount,
		W.WMIErrorCount
INTO #Suggestions
FROM (	SELECT	InstanceID,
				COUNT(*) AS WMISourceCount,
				SUM(WMIErrorCount) AS WMIErrorCount
		FROM #Errors
		WHERE WMIErrorCount > 0
		GROUP BY InstanceID
		) W
CROSS APPLY dbo.CollectionErrorSuggestion(W.WMISourceCount,W.WMIErrorCount) S
WHERE S.Suggestion IS NOT NULL

SELECT	Run.InstanceID,
		Run.ReferenceID,
		R.Reference,
		R.Step,
		R.Name,
		Run.RunCount,
		Run.SucceededCount,
		Run.WarningCount,
		Run.RetriedCount,
		Run.PartialCount,
		Run.FailedCount,
		Run.ImportFailedCount,
		Run.DurationCount,
		Run.DurationTotalMs,
		Run.DurationMaxMs,
		Run.CpuCount,
		Run.CpuTotalMs,
		Run.CpuMaxMs,
		Run.ReadsCount,
		Run.ReadsTotal,
		Run.ReadsMax,
		Run.ImportCount,
		Run.ImportTotalMs,
		Run.ImportMaxMs,
		Run.ImportCpuCount,
		Run.ImportCpuTotalMs,
		Run.ImportReadsCount,
		Run.ImportReadsTotal,
		Run.ImportLagCount,
		Run.ImportLagTotalMs,
		Run.ImportLagMaxMs,
		ISNULL(S.ConsecutiveFailures,0) AS ConsecutiveFailures,
		S.LastStatus,
		S.LastRunDate,
		S.LastSuccessDate,
		S.LastFailureDate,
		S.LastDurationMs,
		CASE WHEN R.Step = '' THEN ISNULL(E.ErrorCount,0) END AS ErrorCount,
		K.KnownCount,
		CASE WHEN M.MutedReason IS NOT NULL THEN 3 ELSE HS.Status END AS Status,
		COALESCE(M.MutedReason,HS.StatusReason) AS StatusReason, /* Not ISNULL - that would cut the reason to the muted reason's length */
		HS.ThresholdLevel,
		/* No status (an older service) has no rate.  Otherwise no failures read is none. */
		CASE WHEN S.InstanceID IS NOT NULL THEN ISNULL(SR.KnownCount,0) END AS SummaryKnownCount,
		CASE WHEN S.InstanceID IS NOT NULL THEN ISNULL(SR.FailedCount,0) END AS SummaryFailedCount,
		/* The errors for the last failure, for the Last Failure link: from the last success while it's failing now (every
		   failure in a row), otherwise from the start of the batch the last failure came in - errors are logged with the
		   batch's SnapshotDate, which can be well before the run started (dbo.CollectionRunsBatch has it, for 14 days; 10
		   minutes before the run where it's gone).  To an hour after the run, to allow for how long it took. */
		CASE WHEN S.ConsecutiveFailures > 0 AND S.LastSuccessDate IS NOT NULL THEN S.LastSuccessDate
			ELSE ISNULL((	SELECT MAX(B.SnapshotDate)
							FROM dbo.CollectionRunsBatch B
							WHERE B.InstanceID = S.InstanceID
							AND B.SnapshotDate <= S.LastFailureDate
							AND B.SnapshotDate > DATEADD(d,-1,S.LastFailureDate)
							),DATEADD(mi,-10,S.LastFailureDate)) END AS ErrorsFrom,
		DATEADD(hh,1,S.LastFailureDate) AS ErrorsTo
INTO #Detail
FROM #Runs Run
JOIN #References R ON R.ReferenceID = Run.ReferenceID
JOIN dbo.Instances I ON I.InstanceID = Run.InstanceID
LEFT JOIN dbo.CollectionRunStatus S ON S.InstanceID = Run.InstanceID AND S.ReferenceID = Run.ReferenceID
LEFT JOIN #SummaryRates SR ON SR.InstanceID = Run.InstanceID AND SR.ReferenceID = Run.ReferenceID
LEFT JOIN #Errors E ON E.InstanceID = Run.InstanceID AND E.ErrorSource = R.Reference AND R.Step = '' /* Steps log their errors against the collection */
CROSS APPLY(SELECT Run.SucceededCount + Run.WarningCount + Run.RetriedCount + Run.PartialCount + Run.FailedCount + Run.ImportFailedCount AS KnownCount) K
CROSS APPLY dbo.CollectionHealthStatus(Run.InstanceID,R.Reference,ISNULL(S.ConsecutiveFailures,0),S.LastStatus,S.LastRunDate,S.LastSuccessDate,
										K.KnownCount,Run.FailedCount + Run.ImportFailedCount,Run.PartialCount) HS
/* As the summary: paused instances and collections whose schedule is disabled are N/A.  Steps follow their collection's schedule. */
OUTER APPLY(SELECT	CASE	WHEN I.CollectionsPausedUntil > SYSUTCDATETIME() THEN CONCAT(N'Collections paused until ',CONVERT(NVARCHAR(16),I.CollectionsPausedUntil,120),N' UTC')
							WHEN EXISTS(SELECT 1
										FROM dbo.ScheduleInfo SI
										WHERE SI.InstanceID = Run.InstanceID
										AND SI.Reference = R.Reference
										AND SI.IsEnabled = 0
										) THEN N'Schedule disabled'
							END AS MutedReason
			) M

CREATE TABLE #Result(
	InstanceID INT NULL,
	InstanceDisplayName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	InstanceCount INT NOT NULL,
	FailingInstanceCount INT NOT NULL,
	Reference VARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
	Step VARCHAR(27) COLLATE DATABASE_DEFAULT NOT NULL,
	Name NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	RunCount INT NOT NULL,
	KnownCount INT NOT NULL,
	SucceededCount INT NOT NULL,
	WarningCount INT NOT NULL,
	RetriedCount INT NOT NULL,
	PartialCount INT NOT NULL,
	FailedCount INT NOT NULL,
	ImportFailedCount INT NOT NULL,
	ConsecutiveFailures INT NOT NULL,
	LastStatus TINYINT NULL,
	LastRunDate DATETIME2(2) NULL,
	LastSuccessDate DATETIME2(2) NULL,
	LastFailureDate DATETIME2(2) NULL,
	LastDurationMs INT NULL,
	AvgDurationMs DECIMAL(19,2) NULL,
	MaxDurationMs INT NULL,
	TotalDurationMs BIGINT NULL,
	AvgCpuMs DECIMAL(19,2) NULL,
	MaxCpuMs INT NULL,
	TotalCpuMs BIGINT NULL,
	AvgLogicalReads DECIMAL(19,2) NULL,
	MaxLogicalReads BIGINT NULL,
	TotalLogicalReads BIGINT NULL,
	AvgImportMs DECIMAL(19,2) NULL,
	MaxImportMs INT NULL,
	TotalImportMs BIGINT NULL,
	AvgImportCpuMs DECIMAL(19,2) NULL,
	TotalImportCpuMs BIGINT NULL,
	AvgImportLogicalReads DECIMAL(19,2) NULL,
	TotalImportLogicalReads BIGINT NULL,
	AvgImportLagSec DECIMAL(19,1) NULL,
	MaxImportLagSec DECIMAL(19,1) NULL,
	ErrorCount INT NULL,
	Status INT NOT NULL,
	StatusReason NVARCHAR(200) COLLATE DATABASE_DEFAULT NULL,
	ThresholdLevel VARCHAR(30) COLLATE DATABASE_DEFAULT NULL,
	SummaryKnownCount INT NULL,
	SummaryFailedCount INT NULL,
	ErrorsFrom DATETIME2(2) NULL,
	ErrorsTo DATETIME2(2) NULL
)

IF @GroupByInstance = 1
BEGIN
	INSERT INTO #Result
	SELECT	D.InstanceID,
			I.InstanceDisplayName,
			1,
			CASE WHEN D.ConsecutiveFailures > 0 THEN 1 ELSE 0 END,
			D.Reference,
			D.Step,
			D.Name,
			D.RunCount,
			D.SucceededCount + D.WarningCount + D.RetriedCount + D.PartialCount + D.FailedCount + D.ImportFailedCount,
			D.SucceededCount,
			D.WarningCount,
			D.RetriedCount,
			D.PartialCount,
			D.FailedCount,
			D.ImportFailedCount,
			D.ConsecutiveFailures,
			D.LastStatus,
			D.LastRunDate,
			D.LastSuccessDate,
			D.LastFailureDate,
			D.LastDurationMs,
			D.DurationTotalMs * 1.0 / NULLIF(D.DurationCount,0),
			D.DurationMaxMs,
			D.DurationTotalMs,
			D.CpuTotalMs * 1.0 / NULLIF(D.CpuCount,0),
			D.CpuMaxMs,
			D.CpuTotalMs,
			D.ReadsTotal * 1.0 / NULLIF(D.ReadsCount,0),
			D.ReadsMax,
			D.ReadsTotal,
			D.ImportTotalMs * 1.0 / NULLIF(D.ImportCount,0),
			D.ImportMaxMs,
			D.ImportTotalMs,
			D.ImportCpuTotalMs * 1.0 / NULLIF(D.ImportCpuCount,0),
			D.ImportCpuTotalMs,
			D.ImportReadsTotal * 1.0 / NULLIF(D.ImportReadsCount,0),
			D.ImportReadsTotal,
			D.ImportLagTotalMs / 1000.0 / NULLIF(D.ImportLagCount,0),
			D.ImportLagMaxMs / 1000.0,
			D.ErrorCount,
			D.Status,
			D.StatusReason,
			D.ThresholdLevel,
			D.SummaryKnownCount,
			D.SummaryFailedCount,
			D.ErrorsFrom,
			D.ErrorsTo
	FROM #Detail D
	JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
END
ELSE
BEGIN
	INSERT INTO #Result
	SELECT	NULL,
			NULL,
			COUNT(*),
			COUNT(CASE WHEN D.ConsecutiveFailures > 0 THEN 1 END),
			D.Reference,
			D.Step,
			D.Name,
			SUM(D.RunCount),
			SUM(D.SucceededCount + D.WarningCount + D.RetriedCount + D.PartialCount + D.FailedCount + D.ImportFailedCount),
			SUM(D.SucceededCount),
			SUM(D.WarningCount),
			SUM(D.RetriedCount),
			SUM(D.PartialCount),
			SUM(D.FailedCount),
			SUM(D.ImportFailedCount),
			MAX(D.ConsecutiveFailures),
			NULL,
			MAX(D.LastRunDate),
			MAX(D.LastSuccessDate),
			MAX(D.LastFailureDate),
			NULL,
			SUM(D.DurationTotalMs) * 1.0 / NULLIF(SUM(D.DurationCount),0),
			MAX(D.DurationMaxMs),
			SUM(D.DurationTotalMs),
			SUM(D.CpuTotalMs) * 1.0 / NULLIF(SUM(D.CpuCount),0),
			MAX(D.CpuMaxMs),
			SUM(D.CpuTotalMs),
			SUM(D.ReadsTotal) * 1.0 / NULLIF(SUM(D.ReadsCount),0),
			MAX(D.ReadsMax),
			SUM(D.ReadsTotal),
			SUM(D.ImportTotalMs) * 1.0 / NULLIF(SUM(D.ImportCount),0),
			MAX(D.ImportMaxMs),
			SUM(D.ImportTotalMs),
			SUM(D.ImportCpuTotalMs) * 1.0 / NULLIF(SUM(D.ImportCpuCount),0),
			SUM(D.ImportCpuTotalMs),
			SUM(D.ImportReadsTotal) * 1.0 / NULLIF(SUM(D.ImportReadsCount),0),
			SUM(D.ImportReadsTotal),
			SUM(D.ImportLagTotalMs) / 1000.0 / NULLIF(SUM(D.ImportLagCount),0),
			MAX(D.ImportLagMaxMs) / 1000.0,
			SUM(D.ErrorCount),
			ISNULL(MIN(NULLIF(D.Status,3)),3), /* The worst of the instances */
			NULL,
			/* Where the instances' thresholds come from - one level for all of them, or Varies by instance where some have their own */
			CASE WHEN MIN(D.ThresholdLevel) = MAX(D.ThresholdLevel) THEN MAX(D.ThresholdLevel) ELSE 'Varies by instance' END,
			SUM(D.SummaryKnownCount),
			SUM(D.SummaryFailedCount),
			MIN(D.ErrorsFrom),
			MAX(D.ErrorsTo)
	FROM #Detail D
	GROUP BY D.ReferenceID, D.Reference, D.Step, D.Name
END

;WITH Calc AS(
	SELECT	R.*,
			CAST(100.0 * (R.FailedCount + R.ImportFailedCount) / NULLIF(R.KnownCount,0) AS DECIMAL(5,2)) AS FailedPct,
			CAST(100.0 * (R.KnownCount - R.FailedCount - R.ImportFailedCount) / NULLIF(R.KnownCount,0) AS DECIMAL(5,2)) AS SucceededPct,
			/* 0 rather than blank where nothing failed in the period, even with no runs in it (a weekly collection) */
			CASE WHEN R.SummaryFailedCount = 0 THEN CAST(0 AS DECIMAL(5,2))
				 ELSE CAST(100.0 * R.SummaryFailedCount / NULLIF(R.SummaryKnownCount,0) AS DECIMAL(5,2)) END AS FailedPct24Hrs
	FROM #Result R
)
SELECT	C.*
INTO #WithStatus
FROM Calc C

DELETE #WithStatus
WHERE NOT (	(Status = 1 AND @IncludeCritical = 1)
			OR (Status = 2 AND @IncludeWarning = 1)
			OR (Status = 3 AND @IncludeNA = 1)
			OR (Status = 4 AND @IncludeOK = 1)
			)

/* Instance columns per instance, instance counts per collection */
SELECT	CASE WHEN @GroupByInstance = 1 THEN InstanceID END AS InstanceID,
		InstanceDisplayName,
		InstanceCount,
		FailingInstanceCount,
		Reference,
		Step,
		Name,
		Status,
		StatusReason,
		RunCount,
		SucceededPct,
		FailedPct,
		FailedPct24Hrs,
		SucceededCount,
		WarningCount,
		RetriedCount,
		PartialCount,
		FailedCount,
		ImportFailedCount,
		RunCount - KnownCount AS UnknownCount,
		ConsecutiveFailures,
		LastStatus,
		LastRunDate,
		LastSuccessDate,
		LastFailureDate,
		LastDurationMs,
		AvgDurationMs,
		MaxDurationMs,
		TotalDurationMs,
		AvgCpuMs,
		MaxCpuMs,
		TotalCpuMs,
		AvgLogicalReads,
		MaxLogicalReads,
		TotalLogicalReads,
		AvgImportMs,
		MaxImportMs,
		TotalImportMs,
		AvgImportCpuMs,
		TotalImportCpuMs,
		AvgImportLogicalReads,
		TotalImportLogicalReads,
		AvgImportLagSec,
		MaxImportLagSec,
		ErrorCount,
		ThresholdLevel,
		ErrorsFrom, /* For the Last Failure link */
		ErrorsTo,
		CAST(1 AS BIT) AS DrillGroupByInstance /* For the drill-down from a collection to its instances */
INTO #Output
FROM #WithStatus

IF @GroupByInstance = 1
BEGIN
	ALTER TABLE #Output DROP COLUMN InstanceCount, FailingInstanceCount, DrillGroupByInstance
END
ELSE
BEGIN
	ALTER TABLE #Output DROP COLUMN InstanceID, InstanceDisplayName, LastStatus, LastDurationMs, StatusReason
END

SELECT *
FROM #Output
ORDER BY Status, FailedPct DESC, Name

SELECT	B.SnapshotDate,
		SUM(H.SucceededCount + H.WarningCount) AS SucceededCount,
		SUM(H.RetriedCount + H.PartialCount) AS RetriedOrPartialCount,
		SUM(H.FailedCount + H.ImportFailedCount) AS FailedCount,
		SUM(H.DurationTotalMs) AS TotalDurationMs,
		SUM(H.CpuTotalMs) AS TotalCpuMs,
		SUM(H.ImportTotalMs) AS TotalImportMs,
		SUM(H.ImportCpuTotalMs) AS TotalImportCpuMs
INTO #Hourly
FROM #Source H
CROSS APPLY(SELECT DATEADD(mi,DATEDIFF(mi,0,H.SnapshotDate) / @DateGroupingMin * @DateGroupingMin,0) AS SnapshotDate) B
GROUP BY B.SnapshotDate

/* Runs by date bucket and how they went, over everything selected */
SELECT	H.SnapshotDate,
		M.Outcome,
		M.RunCount
FROM #Hourly H
CROSS APPLY(VALUES	('Succeeded',H.SucceededCount),
					('Retried / Partial',H.RetriedOrPartialCount),
					('Failed',H.FailedCount)
			) M(Outcome,RunCount)
ORDER BY H.SnapshotDate, M.Outcome

/* What the collections cost by date bucket, over everything selected - collecting on the monitored instances and importing
   into the repository.  CPU is only measured with LogInternalPerformanceCounters on. */
SELECT	H.SnapshotDate,
		M.Measure,
		M.Value
FROM #Hourly H
CROSS APPLY(VALUES	('Collect (sec)',H.TotalDurationMs / 1000.0),
					('Collect CPU (sec)',H.TotalCpuMs / 1000.0),
					('Import (sec)',H.TotalImportMs / 1000.0),
					('Import CPU (sec)',H.TotalImportCpuMs / 1000.0)
			) M(Measure,Value)
WHERE M.Value IS NOT NULL
ORDER BY H.SnapshotDate, M.Measure

/* Import lag (sent to a destination to import) by date bucket - average over the runs of the collections selected, and the worst */
SELECT	L.SnapshotDate,
		M.Measure,
		M.Value
FROM (	SELECT	B.SnapshotDate,
				SUM(L.ImportLagTotalMs) / 1000.0 / NULLIF(SUM(L.ImportLagCount),0) AS AvgLagSec,
				MAX(L.ImportLagMaxMs) / 1000.0 AS MaxLagSec
		FROM #Source L
		CROSS APPLY(SELECT DATEADD(mi,DATEDIFF(mi,0,L.SnapshotDate) / @DateGroupingMin * @DateGroupingMin,0) AS SnapshotDate) B
		WHERE L.ImportLagCount > 0
		GROUP BY B.SnapshotDate
		) L
CROSS APPLY(VALUES	('Avg lag (sec)',L.AvgLagSec),
					('Max lag (sec)',L.MaxLagSec)
			) M(Measure,Value)
ORDER BY L.SnapshotDate, M.Measure

/* Insights: what to do about the instances' errors, a card at the top of the report.  The instances are named where the
   report is for more than one - up to 5, so the card doesn't grow with the number of instances. */
DECLARE @InsightInstances NVARCHAR(MAX)
IF (SELECT COUNT(*) FROM #Instances) > 1
BEGIN
	SELECT @InsightInstances = STUFF((	SELECT TOP(5) N', ' + I.InstanceDisplayName
										FROM #Suggestions SG
										JOIN dbo.Instances I ON I.InstanceID = SG.InstanceID
										ORDER BY I.InstanceDisplayName
										FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,N'')
			+ CASE WHEN (SELECT COUNT(*) FROM #Suggestions) > 5 THEN CONCAT(N' and ',(SELECT COUNT(*) FROM #Suggestions) - 5,N' more') ELSE N'' END
END

SELECT	I.InsightSeverity,
		I.Insight
FROM (	SELECT	COUNT(*) AS InstanceCount,
				MAX(WMISourceCount) AS WMISourceCount,
				MAX(WMIErrorCount) AS WMIErrorCount
		FROM #Suggestions
		) SG
CROSS APPLY dbo.CollectionErrorInsight(SG.InstanceCount,@InsightInstances,SG.WMISourceCount,SG.WMIErrorCount) I
