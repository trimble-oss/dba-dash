CREATE PROC dbo.QueryStats_Get (
    @InstanceIDs dbo.IDs READONLY,
    @InstanceID INT = NULL,
    @DatabaseID INT = NULL,
    @FromDate DATETIME2(3) = NULL,
    @ToDate DATETIME2(3) = NULL,
    /* Family (one row per query shape), Statement (one row per statement), Plan (one row per plan shape) */
    @GroupBy VARCHAR(20) = 'Family',
    @QueryHash VARCHAR(20) = NULL,
    @StatementID BIGINT = NULL,
    /*
        The series label a chart slice was drilled into: either a qualified object name, or the short label
        the charts report builds from a statement's own text for a statement with no object.  Matched against
        both, so every slice that can be drilled into lands on the rows it counted.
    */
    @ObjectName NVARCHAR(260) = NULL,
    /*
        Leaves out the {Other queries}, {Other variants} and {Other databases} rows.  Off by default, deliberately: with them
        the figures add up to the work the instance did, and without them they are a top N that looks like a
        total.  Worth having when the rollups dominate and you want to read the named detail.
    */
    @ExcludeRollups BIT = 0,
    @Top INT = 100
)
AS
/*
    Statement level resource usage for a window.

    Two results.  The first is the workload, at whichever of the three grains the caller asked for.  The
    second is what the first cannot tell you on its own: how complete the window is.  The collection keeps
    the top families and rolls the rest up, and a plan cache row seen for the first time with an old compile
    time cannot be attributed to the interval at all - so a caller that shows the grid without the quality
    result is showing a number without its error bars.

    The rollup rows are returned alongside the detail rather than filtered out.  Their totals are what make
    the returned figures add up to the work the instance actually did, and hiding them would quietly turn a
    complete picture into a top N.

    Rates are calculated against the time the collection actually covered, taken from
    dbo.QueryStatsCollection, rather than against the requested window.  A window half of which the service
    was not collecting for would otherwise halve every rate in it.

    A row that includes an ad hoc shape is shown as the shape's template rather than as any one statement's
    text: the shape's numbers are every literal variant's together, and one variant's values would read as
    though they were what all of them ran with.  Every other row is shown as its own statement.
*/
SET NOCOUNT ON

IF @GroupBy NOT IN ('Family', 'Statement', 'Plan')
BEGIN
    RAISERROR('Invalid @GroupBy value.  Valid options: Family, Statement, Plan', 11, 1);
    RETURN;
END

/*
    A drill down carries its filter rather than its grain, so the grain follows from the filter: asking for
    one family's rows means its statements, and asking for one statement's rows means its plans.  This is
    what lets the grid link straight down the hierarchy with nothing but a column to parameter mapping.
*/
IF @StatementID IS NOT NULL
    SET @GroupBy = 'Plan'
ELSE IF @QueryHash IS NOT NULL
    SET @GroupBy = 'Statement'

SELECT @FromDate = ISNULL(@FromDate, DATEADD(hh, -1, SYSUTCDATETIME())),
       @ToDate = ISNULL(@ToDate, SYSUTCDATETIME())

/*
    Whole hours come from the hourly rollups and only the part hours at either end of the window from the raw
    rows.  An hour in dbo.QueryStats_60MIN is exactly the raw rows whose snapshot falls in it, so the two together
    give the raw rows' figures, for a twelfth of the reading wherever the window spans hours - and reach back as far
    as the rollup is kept rather than only as far as the raw rows.  Where a part hour's raw rows have aged out it is
    missing from the figures and from the covered time alike, so the rates stay right.

    The window is split into three half open ranges: [@FromDate, @HourFrom) and [@HourTo, @ToExclusive) from the
    raw rows, and [@HourFrom, @HourTo) from the rollup.  @HourFrom is the start of the first hour inside the window
    and @HourTo the end of the last.  A window with no whole hour inside it reads its raw rows as the first range,
    and the other two are empty.

    Each range is read on its own, a seek per instance on both columns of the clustered key, so the part hours
    cost their own rows and nothing more.  Each alternative was measured, and each read more:
      - The window with the whole hours excluded by an OR is two seeks only where the optimizer is given the
        values, as OPTION (RECOMPILE) does.  Otherwise it is one seek across the whole window with the OR as a
        residual, reading every raw row the rollup is there to save.
      - The two part hours as rows of a table of ranges leaves the optimizer to scan whole partitions for a range
        of minutes, or to seek on the instance alone and filter on the time afterwards.
    A range that is a pair of variables and an instance from the outer side of the APPLY gives it nothing to choose
    but the seek.  FORCESEEK keeps it from scanning partitions when many instances are in scope; it can always be
    met, since the instance alone is a seek, so it cannot fail the query.
*/
DECLARE @ToExclusive DATETIME2(3) = DATEADD(MILLISECOND, 1, @ToDate)
DECLARE @HourFrom DATETIME2(3) = (SELECT DateGroup FROM dbo.DateGroupingMins(@FromDate, 60))
DECLARE @HourTo DATETIME2(3) = (SELECT DateGroup FROM dbo.DateGroupingMins(@ToExclusive, 60))

IF @HourFrom < @FromDate SET @HourFrom = DATEADD(HOUR, 1, @HourFrom)
IF @HourFrom >= @HourTo SELECT @HourFrom = @ToExclusive, @HourTo = @ToExclusive

CREATE TABLE #Scope (InstanceID INT NOT NULL PRIMARY KEY);

INSERT INTO #Scope (InstanceID)
SELECT ID
FROM @InstanceIDs
WHERE @InstanceID IS NULL OR ID = @InstanceID;

/* Instance level views pass @InstanceID alone */
IF NOT EXISTS (SELECT 1 FROM #Scope) AND @InstanceID IS NOT NULL
BEGIN
    INSERT INTO #Scope (InstanceID) VALUES (@InstanceID);
END

/*  Scoped to the instances asked for rather than the repository.  One instance collecting must not leave
    another that doesn't showing empty results with nothing to say why. */
IF NOT EXISTS (SELECT 1
               FROM dbo.CollectionDates CD
               JOIN #Scope S ON S.InstanceID = CD.InstanceID
               WHERE CD.Reference = 'QueryStats')
BEGIN
    SELECT 'Set "Query Stats Top N" for the connection in the service config tool to collect statement level query stats.' AS Message,
           'https://dbadash.com/docs/help/query-stats/' AS Url
    RETURN
END

/*
    How much time the collection actually covered, and what it could not account for.  Skipped intervals
    contribute no covered time: the instance was not being watched then, and counting it would understate
    every rate.
*/
CREATE TABLE #Coverage (
    InstanceID INT NOT NULL PRIMARY KEY,
    CoveredMicroseconds BIGINT NOT NULL,
    Collections INT NOT NULL,
    SkippedCollections INT NOT NULL,
    FirstCollections INT NOT NULL,
    UnattributedWorkerTime BIGINT NOT NULL,
    UnattributedExecutions BIGINT NOT NULL,
    BaselineEvictions INT NOT NULL,
    MaxReadDurationMs INT NOT NULL
);

INSERT INTO #Coverage (InstanceID, CoveredMicroseconds, Collections, SkippedCollections, FirstCollections,
                       UnattributedWorkerTime, UnattributedExecutions, BaselineEvictions, MaxReadDurationMs)
SELECT C.InstanceID,
       SUM(C.CoveredTime),
       SUM(C.Collections),
       SUM(C.SkippedCollections),
       SUM(C.FirstCollections),
       SUM(C.UnattributedWorkerTime),
       SUM(C.UnattributedExecutions),
       SUM(C.BaselineEvictions),
       MAX(C.MaxReadDurationMs)
FROM (/* The part hour at the start of the window, from the raw rows */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, CoveredTime = CASE WHEN C.IsSkipped = 1 THEN 0 ELSE C.PeriodTime END,
                          Collections = 1, SkippedCollections = CONVERT(INT, C.IsSkipped),
                          FirstCollections = CONVERT(INT, C.IsFirstCollection), C.UnattributedWorkerTime,
                          C.UnattributedExecutions, C.BaselineEvictions, MaxReadDurationMs = C.ReadDurationMs
                   FROM dbo.QueryStatsCollection C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @FromDate
                   AND C.SnapshotDate < @HourFrom) R
      UNION ALL
      /* The whole hours, from the rollup */
      SELECT H.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, C.CoveredTime, C.Collections, C.SkippedCollections, C.FirstCollections,
                          C.UnattributedWorkerTime, C.UnattributedExecutions, C.BaselineEvictions, C.MaxReadDurationMs
                   FROM dbo.QueryStatsCollection_60MIN C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @HourFrom
                   AND C.SnapshotDate < @HourTo) H
      UNION ALL
      /* The part hour at the end */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, CoveredTime = CASE WHEN C.IsSkipped = 1 THEN 0 ELSE C.PeriodTime END,
                          Collections = 1, SkippedCollections = CONVERT(INT, C.IsSkipped),
                          FirstCollections = CONVERT(INT, C.IsFirstCollection), C.UnattributedWorkerTime,
                          C.UnattributedExecutions, C.BaselineEvictions, MaxReadDurationMs = C.ReadDurationMs
                   FROM dbo.QueryStatsCollection C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @HourTo
                   AND C.SnapshotDate < @ToExclusive) R) C
GROUP BY C.InstanceID
OPTION (RECOMPILE);

/*
    A family drill-down names its statements by their shape.  Resolved to the statements first, by a seek on the
    shape, so the read below can go straight to their rows rather than reading the whole window and discarding
    every other family's.  The parameter is converted rather than every statement's hash, which would make the
    seek a scan.
*/
CREATE TABLE #FamilyStatements (StatementID BIGINT NOT NULL PRIMARY KEY);

IF @QueryHash IS NOT NULL
BEGIN
    INSERT INTO #FamilyStatements (StatementID)
    SELECT S.StatementID
    FROM dbo.QueryStatements S
    WHERE S.query_hash = CONVERT(BINARY(8), @QueryHash, 1)
    AND EXISTS (SELECT 1 FROM #Scope T WHERE T.InstanceID = S.InstanceID);
END

/*
    One pass over the deltas, aggregated at statement and plan grain before anything is joined to them.  The
    statements, their text and the filters on them then apply to a row per statement and plan rather than to
    every interval the window holds - a statement collected every five minutes for a day is 288 raw rows, or 24
    hourly ones.  Whole hours come from the rollup - see @HourFrom.
*/
SELECT F.InstanceID,
       F.StatementID,
       F.query_plan_hash,
       IsCompile = CONVERT(BIT, MAX(CONVERT(TINYINT, F.IsCompile))),
       Executions = SUM(F.execution_count),
       WorkerTime = SUM(F.total_worker_time),
       ElapsedTime = SUM(F.total_elapsed_time),
       LogicalReads = SUM(F.total_logical_reads),
       LogicalWrites = SUM(F.total_logical_writes),
       PhysicalReads = SUM(F.total_physical_reads),
       Spills = SUM(F.total_spills),
       GrantKB = SUM(F.total_grant_kb),
       UsedGrantKB = SUM(F.total_used_grant_kb),
       Rows = SUM(F.total_rows),
       /*  The most cache entries the plan had behind it in one interval.  One for a statement in a procedure;
           for an ad hoc shape, one per literal variant that ran in the interval. */
       MaxPlanCount = MAX(F.MaxPlanCount),
       /*  The window the row's work was done in: from the start of the first interval it ran in to the end of
           the last.  The start rather than the first snapshot, so the running queries link covers all of it. */
       FirstPeriodStart = MIN(F.FirstPeriodStart),
       LastSnapshotDate = MAX(F.LastSnapshotDate)
INTO #Facts
FROM (/* The part hour at the start of the window, from the raw rows */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.StatementID, Q.query_plan_hash, Q.IsCompile, Q.execution_count,
                          Q.total_worker_time, Q.total_elapsed_time, Q.total_logical_reads, Q.total_logical_writes,
                          Q.total_physical_reads, Q.total_spills, Q.total_grant_kb, Q.total_used_grant_kb, Q.total_rows,
                          MaxPlanCount = Q.PlanCount, FirstPeriodStart = Q.PeriodStartTime, LastSnapshotDate = Q.SnapshotDate
                   FROM dbo.QueryStats Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @FromDate
                   AND Q.SnapshotDate < @HourFrom) R
      UNION ALL
      /* The whole hours, from the rollup */
      SELECT H.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.StatementID, Q.query_plan_hash, Q.IsCompile, Q.execution_count,
                          Q.total_worker_time, Q.total_elapsed_time, Q.total_logical_reads, Q.total_logical_writes,
                          Q.total_physical_reads, Q.total_spills, Q.total_grant_kb, Q.total_used_grant_kb, Q.total_rows,
                          Q.MaxPlanCount, Q.FirstPeriodStart, Q.LastSnapshotDate
                   FROM dbo.QueryStats_60MIN Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @HourFrom
                   AND Q.SnapshotDate < @HourTo) H
      UNION ALL
      /* The part hour at the end */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.StatementID, Q.query_plan_hash, Q.IsCompile, Q.execution_count,
                          Q.total_worker_time, Q.total_elapsed_time, Q.total_logical_reads, Q.total_logical_writes,
                          Q.total_physical_reads, Q.total_spills, Q.total_grant_kb, Q.total_used_grant_kb, Q.total_rows,
                          MaxPlanCount = Q.PlanCount, FirstPeriodStart = Q.PeriodStartTime, LastSnapshotDate = Q.SnapshotDate
                   FROM dbo.QueryStats Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @HourTo
                   AND Q.SnapshotDate < @ToExclusive) R) F
WHERE (@StatementID IS NULL OR F.StatementID = @StatementID)
AND (@QueryHash IS NULL OR F.StatementID IN (SELECT S.StatementID FROM #FamilyStatements S))
GROUP BY F.InstanceID, F.StatementID, F.query_plan_hash
OPTION (RECOMPILE);

/*
    Then rolled to whichever grain was asked for.  The extra step pays for two things an aggregate over the
    group cannot give: a distinct count of plan shapes across a family whose variants share plans, and the
    heaviest statement, which is the one whose text a family row has to show.
*/
SELECT F.InstanceID,
       S.DatabaseID,
       GroupKey = CASE @GroupBy
                      /* A statement with no hash is its own family rather than being lumped in with every
                         other hashless statement */
                      WHEN 'Family' THEN ISNULL(CONVERT(VARCHAR(20), S.query_hash, 1), CONCAT('S', S.StatementID))
                      WHEN 'Statement' THEN CONCAT('S', S.StatementID)
                      ELSE CONCAT('S', S.StatementID, '|', CONVERT(VARCHAR(20), F.query_plan_hash, 1))
                  END,
       S.StatementID,
       /*  0x0 is the stored sentinel for a row with no plan of its own - a rollup, or the roll-up of the plans
           past the per statement cap - so it is shown as blank rather than as a hash that does not exist. */
       PlanHash = NULLIF(CONVERT(VARCHAR(20), F.query_plan_hash, 1), '0x0000000000000000'),
       QueryHash = CONVERT(VARCHAR(20), S.query_hash, 1),
       /* A real object name wins; the placeholders only label a group that has nothing else */
       QueryName = ISNULL(S.SchemaName + '.' + S.ObjectName,
                          CASE S.StatementType
                              WHEN 0 THEN '{Ad hoc}'
                              WHEN 2 THEN '{Other queries}'
                              WHEN 3 THEN '{Other variants}'
                              WHEN 4 THEN '{Other databases}'
                              WHEN 5 THEN '{Ad hoc}'
                          END),
       ObjectName = S.SchemaName + '.' + S.ObjectName,
       IncludesRollup = CONVERT(BIT, CASE WHEN S.StatementType IN (2, 3, 4) THEN 1 ELSE 0 END),
       IsShape = CONVERT(BIT, CASE WHEN S.StatementType = 5 THEN 1 ELSE 0 END),
       /*  Not for a rollup, whose PlanCount is the statements rolled into it rather than cache entries */
       CacheEntries = CASE WHEN S.StatementType IN (2, 3, 4) THEN NULL ELSE F.MaxPlanCount END,
       F.IsCompile,
       F.Executions,
       F.WorkerTime,
       F.ElapsedTime,
       F.LogicalReads,
       F.LogicalWrites,
       F.PhysicalReads,
       F.Spills,
       F.GrantKB,
       F.UsedGrantKB,
       F.Rows,
       F.FirstPeriodStart,
       F.LastSnapshotDate
INTO #Stmt
FROM #Facts F
JOIN dbo.QueryStatements S ON S.StatementID = F.StatementID
/*  -1 means the rows whose database could not be resolved on the source, which is a real group a chart slice
    can stand for.  Without it those rows would have to be drilled into with a null, and a null parameter
    means no filter at all - so clicking "{Unknown}" would have opened every database. */
WHERE (@DatabaseID IS NULL
       OR (@DatabaseID = -1 AND S.DatabaseID IS NULL)
       OR S.DatabaseID = @DatabaseID)
AND (@ExcludeRollups = 0 OR S.StatementType NOT IN (2, 3, 4))
/*  Matched against the object and against the statement label, because a chart series is named by whichever
    of the two it has - see the @ObjectName comment above.  The stored short label, which is the column the chart
    names the series from, so the two cannot disagree about where the label was cut. */
AND (@ObjectName IS NULL
     OR S.SchemaName + '.' + S.ObjectName = @ObjectName
     OR S.StatementLabelShort = @ObjectName)
OPTION (RECOMPILE);

SELECT InstanceID,
       DatabaseID,
       GroupKey,
       QueryHash = MAX(QueryHash),
       /*  Shown whenever the group ran under one plan shape, not only at the plan grain.  Most statements have
           a single plan, so this is populated in the ordinary case and blank exactly where it would be a lie -
           and blank then means something, because the Plans column says how many there were. */
       PlanHash = CASE WHEN COUNT(DISTINCT PlanHash) = 1 THEN MAX(PlanHash) END,
       /* The heaviest statement in the group.  DENSE_RANK rather than ROW_NUMBER because a statement can
          appear once per plan shape here, and all of its rows have to carry the same rank. */
       StatementID = MIN(CASE WHEN StatementRank = 1 THEN StatementID END),
       /* The heaviest statement with a plan of its own, whose plan stands for the group's where the group ran under
          one plan shape - see PlanHash.  Not the heaviest statement, which can be a rollup with no plan at all. */
       PlanStatementID = MIN(CASE WHEN PlanStatementRank = 1 AND PlanHash IS NOT NULL THEN StatementID END),
       PlanIsShape = CONVERT(BIT, MAX(CASE WHEN PlanStatementRank = 1 AND PlanHash IS NOT NULL THEN CONVERT(TINYINT, IsShape) ELSE 0 END)),
       /* The group's ad hoc shape, where it has one - at most one, since a shape is a family's ad hoc part */
       ShapeStatementID = MAX(CASE WHEN IsShape = 1 THEN StatementID END),
       /* Its busiest plan's most cache entries in one interval: roughly how many texts the row stands for */
       CacheEntries = MAX(CacheEntries),
       /* Only rollups, with no statement in them to look up anywhere else */
       AllRollup = CONVERT(BIT, MIN(CONVERT(TINYINT, IncludesRollup))),
       StatementCount = COUNT(DISTINCT StatementID),
       /* Distinct shapes, not a sum over statements: the variants of one family usually share a plan */
       /*  Real plan shapes only.  A rollup row has no plan of its own, so it neither inflates the count of a
           family it sits in nor claims a plan of its own - it comes back blank, like its hash. */
       PlanCount = NULLIF(COUNT(DISTINCT PlanHash), 0),
       QueryName = ISNULL(MAX(CASE WHEN QueryName NOT LIKE '{%' THEN QueryName END), MIN(QueryName)),
       /*  The object the object execution stats link opens.  Only where the group belongs to one object: a
           shape can be shared by statements in several procedures, and the name shown for it is then just one
           of them, so linking it would open stats for part of the row and present them as the whole. */
       ObjectName = CASE WHEN MIN(ObjectName) = MAX(ObjectName) THEN MAX(ObjectName) END,
       IncludesRollup = CONVERT(BIT, MAX(CONVERT(TINYINT, IncludesRollup))),
       IsCompile = CONVERT(BIT, MAX(CONVERT(TINYINT, IsCompile))),
       Executions = SUM(Executions),
       WorkerTime = SUM(WorkerTime),
       ElapsedTime = SUM(ElapsedTime),
       LogicalReads = SUM(LogicalReads),
       LogicalWrites = SUM(LogicalWrites),
       PhysicalReads = SUM(PhysicalReads),
       Spills = SUM(Spills),
       GrantKB = SUM(GrantKB),
       UsedGrantKB = SUM(UsedGrantKB),
       Rows = SUM(Rows),
       FirstPeriodStart = MIN(FirstPeriodStart),
       LastSnapshotDate = MAX(LastSnapshotDate)
INTO #Agg
/* Two passes rather than one: a window function cannot be ranked by another window function, so the
   per statement total is materialised first and ranked second. */
FROM (SELECT *,
             StatementRank = DENSE_RANK() OVER (PARTITION BY InstanceID, DatabaseID, GroupKey
                                                ORDER BY StatementWorkerTime DESC, StatementID),
             PlanStatementRank = DENSE_RANK() OVER (PARTITION BY InstanceID, DatabaseID, GroupKey
                                                    ORDER BY CASE WHEN PlanHash IS NULL THEN 1 ELSE 0 END,
                                                             StatementWorkerTime DESC, StatementID)
      FROM (SELECT *,
                   StatementWorkerTime = SUM(WorkerTime) OVER (PARTITION BY InstanceID, DatabaseID, GroupKey, StatementID)
            FROM #Stmt) W) X
GROUP BY InstanceID, DatabaseID, GroupKey
OPTION (RECOMPILE);

SELECT TOP (@Top)
       A.InstanceID,
       I.InstanceDisplayName AS Instance,
       D.name AS [Database],
       A.QueryName,
       A.ObjectName,
       /*
           The statement, carved out of the batch text with the offsets, exactly as the running queries view
           does it - or, where the row includes an ad hoc shape, the shape's template.  That covers a family
           row too: a family is a shape, and its ad hoc part is the part with variants.  A family of statements
           in procedures alone shows its heaviest statement's text.
       */
       ST.StatementText,
       /*  The statement the text is from, for the batch link.  The whole batch is not returned: for a statement
           inside a procedure it is the procedure's definition, and up to a thousand of those is a lot to send
           for the one a reader opens.  The grid fetches it when it is clicked - see
           dbo.QueryStatementBatchText_Get - and says which it is, a batch or a shape's example. */
       TextStatementID = S.StatementID,
       ViewBatch = CASE WHEN S.StatementType IN (2, 3, 4) THEN NULL
                        WHEN S.StatementType = 5 THEN 'Example'
                        ELSE 'View'
                   END,
       /*  The running queries caught with the row's query hash, in the window its work was done in - see the
           Running Queries column.  Not for a row of rollups, which has nothing of its own to match on. */
       RunningQueries = CASE WHEN A.QueryHash IS NOT NULL AND A.AllRollup = 0 THEN 'View' END,
       /*  The plan of the row's plan shape, where it ran under one - see PlanHash.  View where one is stored -
           captured with the statistics, or fetched since - Example where that is an ad hoc shape's, which is one
           variant's, and Find where none is: the grid then asks the monitored instance for it, which finds it for
           as long as it stays cached. */
       ViewPlan = CASE WHEN A.PlanStatementID IS NULL OR A.PlanHash IS NULL THEN NULL
                       WHEN NOT EXISTS (SELECT 1
                                        FROM dbo.QueryStatsPlans P
                                        WHERE P.StatementID = A.PlanStatementID
                                        AND P.query_plan_hash = CONVERT(BINARY(8), A.PlanHash, 1)) THEN 'Find'
                       WHEN A.PlanIsShape = 1 THEN 'Example'
                       ELSE 'View'
                  END,
       A.PlanStatementID,
       A.QueryHash,
       A.PlanHash,
       A.StatementID,
       A.StatementCount,
       A.PlanCount,
       A.CacheEntries,
       A.IncludesRollup,
       A.IsCompile,
       A.Executions,
       CPUms = A.WorkerTime / 1000.0,
       AvgCPUms = A.WorkerTime / 1000.0 / NULLIF(A.Executions, 0),
       DurationMs = A.ElapsedTime / 1000.0,
       AvgDurationMs = A.ElapsedTime / 1000.0 / NULLIF(A.Executions, 0),
       /* Share of the CPU this instance accounted for in the window, rollups included, so the column sums
          to 100 rather than to whatever fraction the top N happens to cover */
       PercentCPU = 100.0 * A.WorkerTime / NULLIF(SUM(A.WorkerTime) OVER (PARTITION BY A.InstanceID), 0),
       /*
           Normalised against the time the collection covered rather than against the window, so the figure
           means the same thing whatever window is asked for and whether or not the service was running for
           all of it.  ms/sec is the house convention (see ObjectExecutionStats_Get): 1000 means one core
           fully busy.  Per core divides by the instance's cpu_count, so 100 means the whole box.
       */
       CPUmsPerSec = A.WorkerTime * 1000.0 / NULLIF(C.CoveredMicroseconds, 0),
       CPUmsPerSecPerCore = A.WorkerTime * 1000.0 / NULLIF(C.CoveredMicroseconds, 0) / NULLIF(I.cpu_count, 0),
       DurationMsPerSec = A.ElapsedTime * 1000.0 / NULLIF(C.CoveredMicroseconds, 0),
       ExecutionsPerMin = A.Executions * 60000000.0 / NULLIF(C.CoveredMicroseconds, 0),
       A.LogicalReads,
       A.LogicalWrites,
       A.PhysicalReads,
       A.Spills,
       /*
           Averages rather than totals.  A summed grant is grant size multiplied by how often the query ran, so
           a small grant executed often reads the same as a large one executed once, and says nothing about
           either.  The running totals are diffed, so dividing one by the executions is exact at every grain.
           A max is not offered because it cannot be diffed: max_grant_kb spans the plan's whole life in cache,
           not the window, and for one plan the grant is sized at compile time so the average is usually the
           same figure anyway.
       */
       AvgGrantKB = A.GrantKB * 1.0 / NULLIF(A.Executions, 0),
       AvgUsedGrantKB = A.UsedGrantKB * 1.0 / NULLIF(A.Executions, 0),
       GrantUsedPercent = 100.0 * A.UsedGrantKB / NULLIF(A.GrantKB, 0),
       A.Rows,
       A.FirstPeriodStart,
       A.LastSnapshotDate,
       A.DatabaseID
FROM #Agg A
JOIN dbo.Instances I ON I.InstanceID = A.InstanceID
LEFT JOIN dbo.Databases D ON D.DatabaseID = A.DatabaseID
LEFT JOIN #Coverage C ON C.InstanceID = A.InstanceID
/*
    Text is collected separately from the statistics and capped per collection, so a statement can be stored
    for an interval or two before its text arrives.  Outer joins throughout: a row with no text yet is still
    a row worth showing.
*/
LEFT JOIN dbo.QueryStatements S ON S.StatementID = ISNULL(A.ShapeStatementID, A.StatementID)
OUTER APPLY dbo.QueryStatementText(S.sql_handle, S.statement_start_offset, S.statement_end_offset,
                                   S.StatementType, S.StatementTemplate) ST
ORDER BY A.WorkerTime DESC
OPTION (RECOMPILE);

/*
    Result 2: whether the window above can be trusted, per instance.  Unattributed work is work the
    collection could not diff - a plan seen for the first time with an old compile time - so it is reported as
    a share of the total rather than hidden.  The collector estimates the window's share of it rather than
    sending the plans' whole life in cache, so the share compares like with like.
*/
SELECT I.InstanceDisplayName AS Instance,
       C.Collections,
       C.SkippedCollections,
       C.FirstCollections,
       CoveredMinutes = C.CoveredMicroseconds / 60000000.0,
       WindowMinutes = DATEDIFF(second, @FromDate, @ToDate) / 60.0,
       UnattributedCPUms = C.UnattributedWorkerTime / 1000.0,
       C.UnattributedExecutions,
       PercentUnattributed = 100.0 * C.UnattributedWorkerTime
                             / NULLIF(C.UnattributedWorkerTime + ISNULL(A.WorkerTime, 0), 0),
       RolledUpCPUms = ISNULL(A.RollupWorkerTime, 0) / 1000.0,
       PercentRolledUp = 100.0 * ISNULL(A.RollupWorkerTime, 0) / NULLIF(A.WorkerTime, 0),
       C.BaselineEvictions,
       C.MaxReadDurationMs
FROM #Coverage C
JOIN dbo.Instances I ON I.InstanceID = C.InstanceID
OUTER APPLY (SELECT SUM(G.WorkerTime) AS WorkerTime,
                    SUM(CASE WHEN G.IncludesRollup = 1 THEN G.WorkerTime ELSE 0 END) AS RollupWorkerTime
             FROM #Agg G
             WHERE G.InstanceID = C.InstanceID) A
ORDER BY I.InstanceDisplayName
OPTION (RECOMPILE);
