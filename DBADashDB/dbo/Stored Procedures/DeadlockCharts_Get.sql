/*
	The chart view of the same data dbo.DeadlockSummary_Get shows as grids.  Seven result sets, one per
	chart, so every grouping is on screen at once rather than behind a picker - the point of the charts
	is to see which dimension explains the deadlocks, and that comparison cannot be made one pie at a
	time:

		0  Over time    - deadlock and victim counts per time bucket.  Answers "is this getting worse",
		                  which no amount of grouping does.
		1  Signature    - share per deadlock pattern.
		2  Application
		3  Database
		4  Login
		5  Host
		6  Procedure

	Every pie result uses the same GroupValue / Deadlocks column names, so the six share one shape and
	are told apart by their chart title rather than by bespoke column names.

	Scope, date range and filters come from dbo.DeadlockScope, shared with the grid report so the two
	can never disagree about what they are showing.

	Pies are capped: two hundred slices communicate nothing, so the top @TopN are returned and the tail
	is rolled into one "Other" slice, which keeps the total honest.
*/
CREATE PROC dbo.DeadlockCharts_Get(
	@InstanceIDs IDs READONLY,
	@InstanceID INT = NULL,
	@FromDate DATETIME2(3) = NULL,
	@ToDate DATETIME2(3) = NULL,
	@Signature VARCHAR(18) = NULL,
	/* 0 = no bucketing.  NULL derives a bucket from the range - see below. */
	@DateGroupingMin INT = NULL,
	/* Slices before the tail is rolled into "Other". */
	@TopN INT = 10,
	@ApplicationName NVARCHAR(128) = NULL,
	@DatabaseName sysname = NULL,
	@LoginName sysname = NULL,
	@HostName sysname = NULL,
	@ProcedureName NVARCHAR(776) = NULL,
	@ObjectName NVARCHAR(776) = NULL,
	/*	Group the processes that were rolled back rather than everything that took part.  "Which
		application is losing" is a different question from "which application is involved", and both are
		worth asking - a busy application can appear in every deadlock while never being the victim. */
	@VictimsOnly BIT = 0
)
AS
SET NOCOUNT ON

/*	Scoped to the instances in context, and the same way #Scope is below - see the fuller note in
	dbo.DeadlockSummary_Get.  A global test would go quiet as soon as any one instance in the estate
	collected deadlocks. */
IF NOT EXISTS(	SELECT 1
				FROM dbo.CollectionDates CD
				JOIN @InstanceIDs T ON T.ID = CD.InstanceID
				WHERE CD.Reference = 'Deadlocks'
				AND (T.ID = @InstanceID OR @InstanceID IS NULL))
BEGIN
	SELECT 'Enable the Deadlocks collection in the service config tool to see data here, or use Trigger Collection to read this instance''s system_health session now.' AS Message
	RETURN
END

SET @VictimsOnly = ISNULL(@VictimsOnly, 0)
SET @TopN = CASE WHEN @TopN IS NULL OR @TopN < 1 THEN 10 ELSE @TopN END

/*	A date range is required by dbo.DeadlockScope - see the fuller note in dbo.DeadlockSummary_Get.
	NULL becomes an open bound here rather than being passed through, so it keeps its meaning. */
SET @FromDate = ISNULL(@FromDate, '19000101')
SET @ToDate = ISNULL(@ToDate, '99991231 23:59:59.999')

DECLARE @SignatureBin BINARY(8) = CASE WHEN @Signature IS NULL THEN NULL ELSE CONVERT(BINARY(8), @Signature, 1) END

DECLARE @HasProcessFilter BIT = CASE WHEN @ApplicationName IS NOT NULL
									OR @DatabaseName IS NOT NULL
									OR @LoginName IS NOT NULL
									OR @HostName IS NOT NULL
									OR @ProcedureName IS NOT NULL
								THEN 1 ELSE 0 END

CREATE TABLE #Scope(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	Signature BINARY(8) NULL,
	VictimCount SMALLINT NULL,
	PRIMARY KEY(InstanceID, EventTime, DeadlockHash)
)

INSERT INTO #Scope(InstanceID, EventTime, DeadlockHash, Signature, VictimCount)
SELECT	InstanceID,
		EventTime,
		DeadlockHash,
		Signature,
		VictimCount
FROM dbo.DeadlockScope(@InstanceIDs, @InstanceID, @FromDate, @ToDate, @SignatureBin, @ApplicationName,
					@DatabaseName, @LoginName, @HostName, @ProcedureName, @ObjectName, @VictimsOnly,
					@HasProcessFilter)
/*	Optional filters, so the plan is compiled for the ones actually supplied rather than for every
	combination at once - see the note in dbo.DeadlockSummary_Get. */
OPTION(RECOMPILE)

/*	Every dimension, unpivoted, in one pass over the process rows rather than five near-identical
	queries.  DISTINCT because the unit being counted is the deadlock, not the process: two processes
	from one application in one deadlock is one deadlock involving that application. */
CREATE TABLE #DimensionValue(
	Dimension VARCHAR(20) NOT NULL,
	GroupValue NVARCHAR(776) NULL,
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL
)

INSERT INTO #DimensionValue(Dimension, GroupValue, InstanceID, EventTime, DeadlockHash)
SELECT DISTINCT
		V.Dimension,
		V.GroupValue,
		S.InstanceID,
		S.EventTime,
		S.DeadlockHash
FROM #Scope S
JOIN dbo.DeadlockProcesses P ON P.InstanceID = S.InstanceID AND P.EventTime = S.EventTime AND P.DeadlockHash = S.DeadlockHash
								AND (@VictimsOnly = 0 OR P.IsVictim = 1)
LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
CROSS APPLY(VALUES
		('Application', CAST(P.ClientApp AS NVARCHAR(776))),
		('Database', CAST(DB.name AS NVARCHAR(776))),
		('Login', CAST(P.LoginName AS NVARCHAR(776))),
		('Host', CAST(P.HostName AS NVARCHAR(776))),
		('Procedure', CAST(P.ProcedureName AS NVARCHAR(776)))
	) V(Dimension, GroupValue)

/*	Rank within each dimension once.  GroupValue is nulled for anything past @TopN so the six result
	sets below are a three line group-by rather than the same nested ranking repeated six times. */
SELECT	Dimension,
		CASE WHEN Rnk <= @TopN THEN GroupValue END AS GroupValue,
		Deadlocks
INTO #Ranked
FROM (
	SELECT	Dimension,
			ISNULL(GroupValue, '(none)') AS GroupValue,
			COUNT(*) AS Deadlocks,
			ROW_NUMBER() OVER (PARTITION BY Dimension ORDER BY COUNT(*) DESC, ISNULL(GroupValue, '(none)')) AS Rnk
	FROM #DimensionValue
	GROUP BY Dimension, ISNULL(GroupValue, '(none)')
	) R

/*	Bucket the time axis to roughly a hundred points across whatever range is in context, so the chart
	stays readable at an hour and at a year without the caller having to say which. */
IF @DateGroupingMin IS NULL
BEGIN
	DECLARE @RangeMins INT
	SELECT @RangeMins = DATEDIFF(MINUTE, MIN(EventTime), MAX(EventTime)) FROM #Scope

	SET @DateGroupingMin = CASE
		WHEN @RangeMins IS NULL OR @RangeMins <= 0 THEN 0
		WHEN @RangeMins <= 100 THEN 1
		WHEN @RangeMins <= 500 THEN 5
		WHEN @RangeMins <= 1500 THEN 15
		WHEN @RangeMins <= 6000 THEN 60
		WHEN @RangeMins <= 36000 THEN 360
		ELSE 1440 END
END

/*	A negative width has no meaning and would bucket into the wrong side of the epoch. */
SET @DateGroupingMin = CASE WHEN @DateGroupingMin < 0 THEN 0 ELSE @DateGroupingMin END

/*	0 - Over time.  Victims alongside deadlocks, so the chart can show both series.

	EventTimeEnd is the exclusive end of the bucket, carried so that clicking a point can drill into the
	deadlocks it counted: the bucket width is derived here and the caller has no other way to know it.
	dbo.DeadlockScope treats @ToDate as exclusive, so the pair selects exactly the bucket's deadlocks.
	An ungrouped chart (@DateGroupingMin = 0, a range of a minute or less) has a point per event rather
	than per bucket, so a minute is used as the width - the whole range is inside one minute anyway.*/
SELECT	DG.DateGroup AS EventTime,
		COUNT(*) AS Deadlocks,
		SUM(CAST(S.VictimCount AS INT)) AS Victims,
		DATEADD(MINUTE, CASE WHEN @DateGroupingMin < 1 THEN 1 ELSE @DateGroupingMin END, DG.DateGroup) AS EventTimeEnd
FROM #Scope S
CROSS APPLY dbo.DateGroupingMins(S.EventTime, @DateGroupingMin) DG
GROUP BY DG.DateGroup
ORDER BY DG.DateGroup

/*	1 - Signature.  Ranked separately from the rest: it comes off the deadlock rather than its
	processes, so it is not one of the unpivoted dimensions. */
SELECT	ISNULL(T.GroupValue, 'Other') AS GroupValue,
		SUM(T.Deadlocks) AS Deadlocks
FROM (
	SELECT	CASE WHEN R.Rnk <= @TopN THEN CONVERT(VARCHAR(18), R.Signature, 1) END AS GroupValue,
			R.Deadlocks
	FROM (
		SELECT	S.Signature,
				COUNT(*) AS Deadlocks,
				ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, S.Signature) AS Rnk
		FROM #Scope S
		GROUP BY S.Signature
		) R
	) T
GROUP BY ISNULL(T.GroupValue, 'Other')
ORDER BY Deadlocks DESC

/* 2 - Application */
SELECT ISNULL(GroupValue, 'Other') AS GroupValue, SUM(Deadlocks) AS Deadlocks
FROM #Ranked WHERE Dimension = 'Application'
GROUP BY ISNULL(GroupValue, 'Other') ORDER BY Deadlocks DESC

/* 3 - Database */
SELECT ISNULL(GroupValue, 'Other') AS GroupValue, SUM(Deadlocks) AS Deadlocks
FROM #Ranked WHERE Dimension = 'Database'
GROUP BY ISNULL(GroupValue, 'Other') ORDER BY Deadlocks DESC

/* 4 - Login */
SELECT ISNULL(GroupValue, 'Other') AS GroupValue, SUM(Deadlocks) AS Deadlocks
FROM #Ranked WHERE Dimension = 'Login'
GROUP BY ISNULL(GroupValue, 'Other') ORDER BY Deadlocks DESC

/* 5 - Host */
SELECT ISNULL(GroupValue, 'Other') AS GroupValue, SUM(Deadlocks) AS Deadlocks
FROM #Ranked WHERE Dimension = 'Host'
GROUP BY ISNULL(GroupValue, 'Other') ORDER BY Deadlocks DESC

/* 6 - Procedure */
SELECT ISNULL(GroupValue, 'Other') AS GroupValue, SUM(Deadlocks) AS Deadlocks
FROM #Ranked WHERE Dimension = 'Procedure'
GROUP BY ISNULL(GroupValue, 'Other') ORDER BY Deadlocks DESC
