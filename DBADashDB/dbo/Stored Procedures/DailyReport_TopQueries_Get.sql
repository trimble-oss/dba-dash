CREATE PROC dbo.DailyReport_TopQueries_Get(
	@InstanceIDs VARCHAR(MAX),
	@ToDate DATETIME2(3), /* UTC.  Rounded down to the hour, as in dbo.DailyReport_Performance_Get */
	@BaselineWeeks TINYINT=4,
	@Top INT=20,
	@Hours VARCHAR(MAX)=NULL /* "period:hour offset,..." - only these hours are summed (e.g. 09:00-17:00 local).  NULL: all of them */
)
AS
/*
	The procedures and ad hoc queries behind a daily report's performance findings: for each instance, the candidates that are in the
	top @Top of the last 24 hours by CPU or by logical reads, with their figures for the last 24 hours (period 0) and the same 24 hours
	in each baseline week (periods 1..n), so the caller (DBADash.DailyReport.TopQueryAnalyzer) can rank them by how much they went up.

	Procedures, functions and triggers come from dbo.ObjectExecutionStats_60MIN.  Statements come from dbo.QueryStats_60MIN, ad hoc ones
	only (StatementType 0 and 5): a module's statements are already in its procedure's figures, so listing both would count them twice.
	The rollup rows (types 2-4) aren't queries.  Times are ms.
*/
SET NOCOUNT ON

SET @ToDate = DATETIMEFROMPARTS(YEAR(@ToDate),MONTH(@ToDate),DAY(@ToDate),DATEPART(hh,@ToDate),0,0,0)

CREATE TABLE #Instances(InstanceID INT PRIMARY KEY)
INSERT INTO #Instances(InstanceID)
SELECT DISTINCT TRY_CAST(value AS INT)
FROM STRING_SPLIT(@InstanceIDs,',')
WHERE TRY_CAST(value AS INT) IS NOT NULL

CREATE TABLE #Periods(
	Period TINYINT PRIMARY KEY,
	FromDate DATETIME2(3) NOT NULL,
	ToDate DATETIME2(3) NOT NULL
)
;WITH N AS (
	SELECT TOP(@BaselineWeeks+1) CAST(ROW_NUMBER() OVER(ORDER BY (SELECT NULL))-1 AS TINYINT) AS Period
	FROM sys.all_objects
)
INSERT INTO #Periods(Period,FromDate,ToDate)
SELECT Period, DATEADD(hh,-24,DATEADD(d,-7*Period,@ToDate)), DATEADD(d,-7*Period,@ToDate)
FROM N

/* The same format as dbo.DailyReport_Performance_Get's @Hours */
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

CREATE TABLE #Totals(
	Source VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	InstanceID INT NOT NULL,
	ID BIGINT NOT NULL,
	Period TINYINT NOT NULL,
	CPUMs DECIMAL(19,3) NOT NULL,
	LogicalReads BIGINT NOT NULL,
	PhysicalReads BIGINT NOT NULL,
	Executions BIGINT NOT NULL,
	PRIMARY KEY(Source, InstanceID, ID, Period)
)

INSERT INTO #Totals(Source, InstanceID, ID, Period, CPUMs, LogicalReads, PhysicalReads, Executions)
SELECT 'Object', T.InstanceID, OES.ObjectID, P.Period,
	SUM(OES.total_worker_time)/1000.0,
	SUM(OES.total_logical_reads),
	SUM(OES.total_physical_reads),
	SUM(OES.execution_count)
FROM #Instances T
CROSS JOIN #Periods P
/* Seek on (InstanceID, SnapshotDate) per instance and period - see dbo.DailyReport_Performance_Get for why the TOP and named columns */
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.ObjectExecutionStats_60MIN OES WITH (FORCESEEK(PK_ObjectExecutionStats_60MIN(InstanceID,SnapshotDate))) WHERE OES.InstanceID = T.InstanceID AND OES.SnapshotDate >= P.FromDate AND OES.SnapshotDate < P.ToDate) OES
WHERE (@Hours IS NULL OR EXISTS(SELECT 1 FROM #IncludedHours H WHERE H.Period = P.Period AND H.HourOffset = DATEDIFF(hh, P.FromDate, OES.SnapshotDate)))
GROUP BY T.InstanceID, OES.ObjectID, P.Period
OPTION(RECOMPILE, NO_PERFORMANCE_SPOOL)

INSERT INTO #Totals(Source, InstanceID, ID, Period, CPUMs, LogicalReads, PhysicalReads, Executions)
SELECT 'Query', T.InstanceID, QS.StatementID, P.Period,
	SUM(QS.total_worker_time)/1000.0,
	SUM(QS.total_logical_reads),
	SUM(QS.total_physical_reads),
	SUM(QS.execution_count)
FROM #Instances T
CROSS JOIN #Periods P
CROSS APPLY (SELECT TOP (2147483647) * FROM dbo.QueryStats_60MIN QS WITH (FORCESEEK(PK_QueryStats_60MIN(InstanceID,SnapshotDate))) WHERE QS.InstanceID = T.InstanceID AND QS.SnapshotDate >= P.FromDate AND QS.SnapshotDate < P.ToDate) QS
JOIN dbo.QueryStatements S ON S.StatementID = QS.StatementID
WHERE S.StatementType IN (0,5)
AND (@Hours IS NULL OR EXISTS(SELECT 1 FROM #IncludedHours H WHERE H.Period = P.Period AND H.HourOffset = DATEDIFF(hh, P.FromDate, QS.SnapshotDate)))
GROUP BY T.InstanceID, QS.StatementID, P.Period
OPTION(RECOMPILE, NO_PERFORMANCE_SPOOL)

;WITH Ranked AS (
	SELECT Source, InstanceID, ID,
		ROW_NUMBER() OVER(PARTITION BY InstanceID ORDER BY CPUMs DESC) AS CPURank,
		ROW_NUMBER() OVER(PARTITION BY InstanceID ORDER BY LogicalReads DESC) AS ReadsRank
	FROM #Totals
	WHERE Period = 0
)
SELECT T.Source,
	T.InstanceID,
	T.ID,
	T.Period,
	D.name AS DatabaseName,
	CASE WHEN T.Source = 'Object' THEN O.SchemaName + '.' + O.ObjectName ELSE S.StatementLabelShort END AS Name,
	O.ObjectType,
	CONVERT(VARCHAR(18), S.query_hash, 1) AS QueryHash,
	T.CPUMs,
	T.LogicalReads,
	T.PhysicalReads,
	T.Executions
FROM #Totals T
JOIN Ranked R ON R.Source = T.Source AND R.InstanceID = T.InstanceID AND R.ID = T.ID
LEFT JOIN dbo.DBObjects O ON T.Source = 'Object' AND O.ObjectID = T.ID
LEFT JOIN dbo.QueryStatements S ON T.Source = 'Query' AND S.StatementID = T.ID
LEFT JOIN dbo.Databases D ON D.DatabaseID = ISNULL(O.DatabaseID, S.DatabaseID)
WHERE R.CPURank <= @Top OR R.ReadsRank <= @Top
ORDER BY T.InstanceID, T.Source, T.ID, T.Period
