CREATE PROC dbo.DailyReport_Performance_Get(
	@InstanceIDs VARCHAR(MAX),
	@ToDate DATETIME2(3), /* UTC.  Rounded down to the hour: the report reads whole hours from the hourly rollups */
	@BaselineWeeks TINYINT=4,
	@Hours VARCHAR(MAX)=NULL /* "period:hour offset,..." - the hours the report counts (e.g. 09:00-17:00 local).  NULL: all of them */
)
AS
/*
	Performance for a daily report: the last 24 whole hours (period 0) and the same 24 hours 1 to @BaselineWeeks weeks
	earlier (periods 1..n), so the report can compare a day with the same weekday's usual figures rather than with one
	possibly unusual day.  Hourly rows, so the report can find the busiest hour as well as the day as a whole.

	Analysis (thresholds, baseline median, what's worth reporting) is done by the caller - DBADash.DailyReport.PerformanceAnalyzer.

	Result sets:
		0  Instances      - the instances, with IsAzureDB.  Azure DB CPU comes from the resource stats rollup as dbo.CPU isn't collected for it.
		1  Hours          - one row per instance, period and hour with any data.
		2  Wait types     - per instance and period, the wait types that are in the top 10 of any period for the instance, so a type
		                    that is new this week shows against a baseline of 0 rather than missing.  Only the @Hours hours, when given.
		                    The hours result set is always all 24 - the caller leaves hours out itself, and charts show them all.
		3  Thresholds     - dbo.PerformanceThresholds, the same thresholds the Performance Summary colours by.

	Every read is a seek per instance and period on (InstanceID, time), so the cost depends on the instances and days read, not on how much
	history is kept.  Each seek is a CROSS APPLY correlated to the instance and period, with FORCESEEK naming the key columns and a TOP inside:
	  - Plain FORCESEEK can be met by seeking on InstanceID alone and filtering the dates afterwards, which reads the instance's whole history
	    (seen as 12M rows read from Waits_60MIN for 96K wanted).
	  - Naming the columns without the TOP fails with error 8622: the optimizer turns the APPLY into a join it can't seek.  The TOP keeps it an
	    APPLY, run once per instance and period with that period's dates.
	  - NO_PERFORMANCE_SPOOL: with the TOP the optimizer adds a lazy spool under the APPLY that never rewinds (each instance and period is
	    read once) but writes every row to tempdb.
*/
SET NOCOUNT ON

SET @ToDate = DATETIMEFROMPARTS(YEAR(@ToDate),MONTH(@ToDate),DAY(@ToDate),DATEPART(hh,@ToDate),0,0,0)

CREATE TABLE #Instances(
	InstanceID INT PRIMARY KEY,
	IsAzureDB BIT NOT NULL
)
INSERT INTO #Instances(InstanceID,IsAzureDB)
SELECT I.InstanceID, CASE WHEN I.EngineEdition = 5 THEN 1 ELSE 0 END
FROM dbo.Instances I
WHERE I.InstanceID IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@InstanceIDs,','))

CREATE TABLE #Periods(
	Period TINYINT PRIMARY KEY,
	FromDate DATETIME2(3) NOT NULL,
	ToDate DATETIME2(3) NOT NULL,
	FromDate2 DATETIME2(2) NOT NULL,
	ToDate2 DATETIME2(2) NOT NULL
)
;WITH N AS (
	SELECT TOP(@BaselineWeeks+1) CAST(ROW_NUMBER() OVER(ORDER BY (SELECT NULL))-1 AS TINYINT) AS Period
	FROM sys.all_objects
)
INSERT INTO #Periods(Period,FromDate,ToDate,FromDate2,ToDate2)
SELECT Period,
	DATEADD(hh,-24,DATEADD(d,-7*Period,@ToDate)),
	DATEADD(d,-7*Period,@ToDate),
	DATEADD(hh,-24,DATEADD(d,-7*Period,@ToDate)),
	DATEADD(d,-7*Period,@ToDate)
FROM N

CREATE TABLE #IncludedHours(
	Period TINYINT NOT NULL,
	HourOffset INT NOT NULL,
	PRIMARY KEY(Period, HourOffset)
)
INSERT INTO #IncludedHours(Period, HourOffset)
SELECT DISTINCT TRY_CAST(LEFT(value, CHARINDEX(':', value) - 1) AS TINYINT), TRY_CAST(SUBSTRING(value, CHARINDEX(':', value) + 1, 10) AS INT)
FROM STRING_SPLIT(@Hours, ',')
WHERE CHARINDEX(':', value) > 1
AND TRY_CAST(LEFT(value, CHARINDEX(':', value) - 1) AS TINYINT) IS NOT NULL
AND TRY_CAST(SUBSTRING(value, CHARINDEX(':', value) + 1, 10) AS INT) IS NOT NULL

SELECT T.InstanceID,
	I.InstanceDisplayName,
	T.IsAzureDB
FROM #Instances T
JOIN dbo.Instances I ON I.InstanceID = T.InstanceID

/*
	Hourly figures.  Each source is aggregated to (instance, period, hour) on its own into #Hourly, then the rows are combined, so no source
	multiplies another and each is read once.
*/
CREATE TABLE #Hourly(
	InstanceID INT NOT NULL,
	Period TINYINT NOT NULL,
	HourOffset INT NOT NULL,
	AvgCPU DECIMAL(9,3) NULL,
	MaxCPU DECIMAL(9,3) NULL,
	HighCPUSamples BIGINT NULL,
	CPUSamples BIGINT NULL,
	Reads BIGINT NULL,
	Writes BIGINT NULL,
	ReadStallMs BIGINT NULL,
	WriteStallMs BIGINT NULL,
	IOSampleMs BIGINT NULL,
	WaitMs BIGINT NULL,
	CriticalWaitMs BIGINT NULL,
	LockWaitMs BIGINT NULL,
	WaitSampleMs BIGINT NULL,
	BlockingSnapshots INT NULL,
	MaxBlockedSessions INT NULL,
	MaxBlockedWaitMs BIGINT NULL,
	Deadlocks INT NULL,
	SlowQueries INT NULL,
	FailedSlowQueries INT NULL
)

INSERT INTO #Hourly(InstanceID,Period,HourOffset,AvgCPU,MaxCPU,HighCPUSamples,CPUSamples)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,C.EventTime),
	SUM(C.SumTotalCPU)*1.0 / NULLIF(SUM(C.SampleCount),0),
	MAX(C.MaxTotalCPU),
	/* Samples at 80% or more (the histogram buckets are 10% wide, CPU90 is 80-90%) and all samples, so the caller can work out time at high CPU */
	SUM(ISNULL(C.CPU90,0)+ISNULL(C.CPU100,0)),
	SUM(C.SampleCount)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.CPU_60MIN C WITH (FORCESEEK(PK_CPU_60MIN(InstanceID,EventTime)))
			WHERE C.InstanceID = T.InstanceID AND C.EventTime >= P.FromDate AND C.EventTime < P.ToDate) C
WHERE T.IsAzureDB = 0
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,C.EventTime)
OPTION(NO_PERFORMANCE_SPOOL)

/* Azure DB CPU comes from the resource stats rollup as dbo.CPU isn't collected for it */
INSERT INTO #Hourly(InstanceID,Period,HourOffset,AvgCPU,MaxCPU,HighCPUSamples,CPUSamples)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,A.end_time),
	AVG(A.avg_cpu_percent*1.0),
	MAX(A.max_cpu_percent),
	SUM(A.CPU90+A.CPU100),
	SUM(A.CPU10+A.CPU20+A.CPU30+A.CPU40+A.CPU50+A.CPU60+A.CPU70+A.CPU80+A.CPU90+A.CPU100)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.AzureDBResourceStats_60MIN A WITH (FORCESEEK(PK_AzureDBResourceStats_60MIN(InstanceID,end_time)))
			WHERE A.InstanceID = T.InstanceID AND A.end_time >= P.FromDate AND A.end_time < P.ToDate) A
WHERE T.IsAzureDB = 1
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,A.end_time)
OPTION(NO_PERFORMANCE_SPOOL)

/* Instance totals are the rows for database, file and drive -1, -1, '*' */
INSERT INTO #Hourly(InstanceID,Period,HourOffset,Reads,Writes,ReadStallMs,WriteStallMs,IOSampleMs)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate2,S.SnapshotDate),
	SUM(S.num_of_reads),
	SUM(S.num_of_writes),
	SUM(S.io_stall_read_ms),
	SUM(S.io_stall_write_ms),
	SUM(S.sample_ms_diff)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.DBIOStats_60MIN S WITH (FORCESEEK(PK_IOStats_60MIN(InstanceID,DatabaseID,FileID,Drive,SnapshotDate)))
			WHERE S.InstanceID = T.InstanceID AND S.DatabaseID = -1 AND S.FileID = -1 AND S.Drive = '*'
			AND S.SnapshotDate >= P.FromDate2 AND S.SnapshotDate < P.ToDate2) S
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate2,S.SnapshotDate)
OPTION(NO_PERFORMANCE_SPOOL)

INSERT INTO #Hourly(InstanceID,Period,HourOffset,WaitMs,CriticalWaitMs,LockWaitMs,WaitSampleMs)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate2,W.SnapshotDate),
	SUM(CASE WHEN WT.IsExcluded = 0 THEN W.wait_time_ms ELSE 0 END),
	SUM(CASE WHEN WT.IsCriticalWait = 1 THEN W.wait_time_ms ELSE 0 END),
	SUM(CASE WHEN WT.WaitType LIKE 'LCK%' THEN W.wait_time_ms ELSE 0 END),
	/* Every wait type in an hour has the hour's sample time */
	MAX(W.sample_ms_diff)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.Waits_60MIN W WITH (FORCESEEK(PK_Waits_60MIN(InstanceID,SnapshotDate)))
			WHERE W.InstanceID = T.InstanceID AND W.SnapshotDate >= P.FromDate2 AND W.SnapshotDate < P.ToDate2) W
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate2,W.SnapshotDate)
OPTION(NO_PERFORMANCE_SPOOL)

INSERT INTO #Hourly(InstanceID,Period,HourOffset,BlockingSnapshots,MaxBlockedSessions,MaxBlockedWaitMs)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,B.SnapshotDateUTC),
	COUNT(*),
	MAX(B.BlockedSessionCount),
	MAX(B.BlockedWaitTime)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.BlockingSnapshotSummary B WITH (FORCESEEK(IX_BlockingSnapshotSummary_InstanceID_SnapshotDate(InstanceID,SnapshotDateUTC)))
			WHERE B.InstanceID = T.InstanceID AND B.SnapshotDateUTC >= P.FromDate AND B.SnapshotDateUTC < P.ToDate) B
WHERE B.BlockedSessionCount > 0
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,B.SnapshotDateUTC)
OPTION(NO_PERFORMANCE_SPOOL)

INSERT INTO #Hourly(InstanceID,Period,HourOffset,Deadlocks)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,D.EventTime),
	COUNT(*)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.Deadlocks D WITH (FORCESEEK(PK_Deadlocks(InstanceID,EventTime)))
			WHERE D.InstanceID = T.InstanceID AND D.EventTime >= P.FromDate AND D.EventTime < P.ToDate) D
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,D.EventTime)
OPTION(NO_PERFORMANCE_SPOOL)

INSERT INTO #Hourly(InstanceID,Period,HourOffset,SlowQueries,FailedSlowQueries)
SELECT T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,Q.timestamp),
	COUNT(*),
	SUM(CASE WHEN Q.result <> '0 - OK' THEN 1 ELSE 0 END)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.SlowQueries Q WITH (FORCESEEK(PK_SlowQueries(InstanceID,timestamp)))
			WHERE Q.InstanceID = T.InstanceID AND Q.timestamp >= P.FromDate AND Q.timestamp < P.ToDate) Q
GROUP BY T.InstanceID, P.Period, DATEDIFF(hh,P.FromDate,Q.timestamp)
OPTION(NO_PERFORMANCE_SPOOL)

/* Each source filled its own columns, so there's at most one value per column for each hour */
SELECT InstanceID,
	Period,
	HourOffset,
	MAX(AvgCPU) AS AvgCPU,
	MAX(MaxCPU) AS MaxCPU,
	MAX(HighCPUSamples) AS HighCPUSamples,
	MAX(CPUSamples) AS CPUSamples,
	MAX(Reads) AS Reads,
	MAX(Writes) AS Writes,
	MAX(ReadStallMs) AS ReadStallMs,
	MAX(WriteStallMs) AS WriteStallMs,
	MAX(IOSampleMs) AS IOSampleMs,
	MAX(WaitMs) AS WaitMs,
	MAX(CriticalWaitMs) AS CriticalWaitMs,
	MAX(LockWaitMs) AS LockWaitMs,
	MAX(WaitSampleMs) AS WaitSampleMs,
	MAX(BlockingSnapshots) AS BlockingSnapshots,
	MAX(MaxBlockedSessions) AS MaxBlockedSessions,
	MAX(MaxBlockedWaitMs) AS MaxBlockedWaitMs,
	MAX(Deadlocks) AS Deadlocks,
	MAX(SlowQueries) AS SlowQueries,
	MAX(FailedSlowQueries) AS FailedSlowQueries
FROM #Hourly
GROUP BY InstanceID, Period, HourOffset
ORDER BY InstanceID, Period, HourOffset

/* Wait types by period, for the types in any period's top 10.  Totals are kept in #WaitTypes as they're read twice: to rank and to return */
CREATE TABLE #WaitTypes(
	InstanceID INT NOT NULL,
	Period TINYINT NOT NULL,
	WaitTypeID SMALLINT NOT NULL,
	WaitMs BIGINT NOT NULL,
	PRIMARY KEY(InstanceID, Period, WaitTypeID)
)
INSERT INTO #WaitTypes(InstanceID,Period,WaitTypeID,WaitMs)
SELECT T.InstanceID, P.Period, W.WaitTypeID,
	SUM(W.wait_time_ms)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.Waits_60MIN W WITH (FORCESEEK(PK_Waits_60MIN(InstanceID,SnapshotDate)))
			WHERE W.InstanceID = T.InstanceID AND W.SnapshotDate >= P.FromDate2 AND W.SnapshotDate < P.ToDate2) W
/* Applied after the seek, so it doesn't change how the rows are read */
WHERE (@Hours IS NULL OR EXISTS(SELECT 1 FROM #IncludedHours H WHERE H.Period = P.Period AND H.HourOffset = DATEDIFF(hh, P.FromDate2, W.SnapshotDate)))
GROUP BY T.InstanceID, P.Period, W.WaitTypeID
OPTION(NO_PERFORMANCE_SPOOL)

;WITH ranked AS (
	SELECT BT.InstanceID, BT.WaitTypeID, ROW_NUMBER() OVER(PARTITION BY BT.InstanceID, BT.Period ORDER BY BT.WaitMs DESC) AS rnk
	FROM #WaitTypes BT
	JOIN dbo.WaitType WT ON WT.WaitTypeID = BT.WaitTypeID
	WHERE WT.IsExcluded = 0
	AND BT.WaitMs > 0
)
, topTypes AS (
	SELECT DISTINCT InstanceID, WaitTypeID
	FROM ranked
	WHERE rnk <= 10
)
SELECT BT.InstanceID,
	BT.Period,
	WT.WaitType,
	WT.IsCriticalWait,
	BT.WaitMs
FROM #WaitTypes BT
JOIN topTypes TT ON TT.InstanceID = BT.InstanceID AND TT.WaitTypeID = BT.WaitTypeID
JOIN dbo.WaitType WT ON WT.WaitTypeID = BT.WaitTypeID
ORDER BY BT.InstanceID, BT.Period, BT.WaitMs DESC
OPTION(RECOMPILE)

SELECT CPUCriticalThreshold,
	CPUWarningThreshold,
	ReadLatencyCriticalThreshold,
	ReadLatencyWarningThreshold,
	WriteLatencyCriticalThreshold,
	WriteLatencyWarningThreshold,
	MinIOPsThreshold,
	CriticalWaitCriticalThreshold,
	CriticalWaitWarningThreshold
FROM dbo.PerformanceThresholds
