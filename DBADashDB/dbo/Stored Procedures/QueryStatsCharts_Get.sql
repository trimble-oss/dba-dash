CREATE PROC dbo.QueryStatsCharts_Get (
    @InstanceIDs dbo.IDs READONLY,
    @InstanceID INT = NULL,
    @DatabaseID INT = NULL,
    @FromDate DATETIME2(3) = NULL,
    @ToDate DATETIME2(3) = NULL,
    @DateGroupingMin INT = NULL,
    @TopN INT = 10,
    /* CPU | Duration | Executions | Reads | Writes */
    @Measure VARCHAR(20) = 'CPU',
    /* Total (the absolute figure) | PerSecond (normalised against the time the collection covered) */
    @Units VARCHAR(20) = 'PerSecond',
    /*
        Leaves out the rollup bands - {Other queries}, {Other variants}, {Other databases} and the {Other} tail this proc rolls
        up itself.  Off by default, because with them the chart's height is the work the instance did and
        without them it is a top N drawn as though it were a total.  Worth switching on when the rollups
        dominate and the question is what the named queries are doing.
    */
    @ExcludeRollups BIT = 0,
    /*
        The smallest share of the window a series or a database may have and still be drawn in its own right.
        Anything below it joins the {Other} slice.

        This is not only tidiness.  A slice worth a thousandth of a percent has no width to hover over, and a
        pie series with no width answers the chart's hit test everywhere, so one invisible slice ends up named
        in the tooltip of every other slice.  The floor is applied here rather than by the chart's own
        MinSlicePercent so that there is exactly one {Other} slice rather than one from each of us.
    */
    @MinSlicePercent DECIMAL(9, 4) = 0.5
)
AS
/*
    The shape of the workload rather than its rows: what the instance spent its time on, over time and in
    proportion.

    Normalised by default.  A bucket at the edge of a range is usually partial, and a range where the
    service was not collecting for part of it has buckets covering less time than their width, so absolute
    totals per bucket are not comparable with each other - the first and last columns of a stacked chart
    would always look short.  Dividing by the time each bucket actually covered fixes both.  The absolute
    figures are a picker away for anyone who wants them.

    Bands are statements, qualified by the object they belong to.  Object level is what Object Execution Stats
    already does, and does better - it reads from a collection built for it rather than from whatever the plan
    cache happened to be holding - so the value of this report is the level below that: which statement inside
    the procedure, which is the thing you can actually go and fix.  The object pie is still here for the cases
    where the cost is spread across statements rather than concentrated in one.  The per plan breakdown stays
    in the grid report.

    Everything not charted individually - the tail past the Series picker and the collection's own rollups -
    becomes a single {Other} band.  A thick {Other} means the picture is not describing this workload, and the
    Coverage chart is what says whether the missing detail exists to be found.
*/
SET NOCOUNT ON

IF @Measure NOT IN ('CPU', 'Duration', 'Executions', 'Reads', 'Writes')
BEGIN
    RAISERROR('Invalid @Measure value.  Valid options: CPU, Duration, Executions, Reads, Writes', 11, 1);
    RETURN;
END

IF @Units NOT IN ('Total', 'PerSecond')
BEGIN
    RAISERROR('Invalid @Units value.  Valid options: Total, PerSecond', 11, 1);
    RETURN;
END

SELECT @FromDate = ISNULL(@FromDate, DATEADD(hh, -1, SYSUTCDATETIME())),
       @ToDate = ISNULL(@ToDate, SYSUTCDATETIME())

CREATE TABLE #Scope (InstanceID INT NOT NULL PRIMARY KEY);

INSERT INTO #Scope (InstanceID)
SELECT ID
FROM @InstanceIDs
WHERE @InstanceID IS NULL OR ID = @InstanceID;

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

/*  Bucket the time axis to roughly a hundred points across whatever range is in context, so the chart stays
    readable at an hour and at a year without the caller having to say which.  Same ladder as
    dbo.DeadlockCharts_Get, so the two reports bucket a given range the same way. */
IF @DateGroupingMin IS NULL
BEGIN
    DECLARE @RangeMins INT = DATEDIFF(MINUTE, @FromDate, @ToDate)

    SET @DateGroupingMin = CASE
        WHEN @RangeMins IS NULL OR @RangeMins <= 0 THEN 1
        WHEN @RangeMins <= 100 THEN 1
        WHEN @RangeMins <= 500 THEN 5
        WHEN @RangeMins <= 1500 THEN 15
        WHEN @RangeMins <= 6000 THEN 60
        WHEN @RangeMins <= 36000 THEN 360
        ELSE 1440 END
END

/* A bucket of zero has no width to normalise against, and the smallest useful bucket here is a minute */
SET @DateGroupingMin = CASE WHEN @DateGroupingMin < 1 THEN 1 ELSE @DateGroupingMin END

/*
    Whole hours come from the hourly rollups and only the part hours at either end of the window from the raw
    rows, as in dbo.QueryStats_Get - see the notes there on the three ranges and on why each is read as it is.
    Only where buckets are whole hours: an hour's rows can be added into a bucket of an hour, six or a day, which
    the ladder above picks for any range over 25 hours, but not split into smaller ones, so a bucket that is not a
    whole number of hours reads the raw rows alone.
*/
DECLARE @ToExclusive DATETIME2(3) = DATEADD(MILLISECOND, 1, @ToDate)
DECLARE @HourFrom DATETIME2(3) = (SELECT DateGroup FROM dbo.DateGroupingMins(@FromDate, 60))
DECLARE @HourTo DATETIME2(3) = (SELECT DateGroup FROM dbo.DateGroupingMins(@ToExclusive, 60))

IF @HourFrom < @FromDate SET @HourFrom = DATEADD(HOUR, 1, @HourFrom)
IF @HourFrom >= @HourTo OR @DateGroupingMin % 60 <> 0 SELECT @HourFrom = @ToExclusive, @HourTo = @ToExclusive

/*
    What each bucket actually covered, per instance.  Per instance and not summed across them, because a
    series belongs to one instance: dividing one instance's work by the covered time of every instance in
    context would deflate every rate by the number of instances on screen.  Skipped intervals contribute
    nothing - the instance was not being watched then.
*/
SELECT C.InstanceID,
       DG.DateGroup AS TimeBucket,
       CoveredMicroseconds = SUM(C.CoveredTime),
       UnattributedWorkerTime = SUM(C.UnattributedWorkerTime)
INTO #Cov
FROM (/* The part hour at the start of the window, from the raw rows */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, C.SnapshotDate, CoveredTime = CASE WHEN C.IsSkipped = 1 THEN 0 ELSE C.PeriodTime END,
                          C.UnattributedWorkerTime
                   FROM dbo.QueryStatsCollection C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @FromDate
                   AND C.SnapshotDate < @HourFrom) R
      UNION ALL
      /* The whole hours, from the rollup */
      SELECT H.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, C.SnapshotDate, C.CoveredTime, C.UnattributedWorkerTime
                   FROM dbo.QueryStatsCollection_60MIN C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @HourFrom
                   AND C.SnapshotDate < @HourTo) H
      UNION ALL
      /* The part hour at the end */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT C.InstanceID, C.SnapshotDate, CoveredTime = CASE WHEN C.IsSkipped = 1 THEN 0 ELSE C.PeriodTime END,
                          C.UnattributedWorkerTime
                   FROM dbo.QueryStatsCollection C WITH (FORCESEEK)
                   WHERE C.InstanceID = T.InstanceID
                   AND C.SnapshotDate >= @HourTo
                   AND C.SnapshotDate < @ToExclusive) R) C
CROSS APPLY dbo.DateGroupingMins(C.SnapshotDate, @DateGroupingMin) DG
GROUP BY C.InstanceID, DG.DateGroup
OPTION (RECOMPILE);

/*
    Whether more than one instance is on screen.  It decides how a series is named: an object or a database
    name identifies one thing within an instance and nothing at all across an estate - DB1 on three nodes is
    three databases, and merging them into one slice is the kind of picture that quietly answers the wrong
    question.
*/
DECLARE @MultiInstance BIT = CASE WHEN (SELECT COUNT(DISTINCT InstanceID) FROM #Cov) > 1 THEN 1 ELSE 0 END

/*
    One row per bucket per statement, built in steps rather than one: the numbers, then what each statement is,
    then the labels, then the numbers and labels joined.

    The deltas are read once, here - whole hours from the rollup, see @HourFrom - and everything else in this
    proc, the coverage chart included, works from what this keeps.  Nothing is joined to them while they are read:
    the filters on statements are applied to what this aggregates, a row per statement per bucket, rather than to
    every stored interval.

    The labels are the expensive part.  Cutting a statement out of its batch and flattening it runs on
    NVARCHAR(MAX), and done inline it ran once per stored row - every plan of every statement in every
    interval - when the answer only depends on the statement.  Worse, the labels came out as NVARCHAR(MAX), so
    the GROUP BY sorted and repartitioned LOBs, and every later pass over #Base read rows a few to a page.  So
    the numbers are aggregated on integer keys first, the labels are computed once per statement - and only for
    the statements the filters keep - and cast to widths that hold their longest value, and only then are the
    two put together.
*/
SELECT F.InstanceID,
       DG.DateGroup AS TimeBucket,
       F.StatementID,
       WorkerTime = SUM(F.total_worker_time),
       Value = SUM(CASE @Measure
                       WHEN 'CPU' THEN F.total_worker_time / 1000.0
                       WHEN 'Duration' THEN F.total_elapsed_time / 1000.0
                       WHEN 'Executions' THEN F.execution_count
                       WHEN 'Reads' THEN F.total_logical_reads
                       WHEN 'Writes' THEN F.total_logical_writes
                   END)
INTO #Agg
FROM (/* The part hour at the start of the window, from the raw rows */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.SnapshotDate, Q.StatementID, Q.total_worker_time, Q.total_elapsed_time,
                          Q.execution_count, Q.total_logical_reads, Q.total_logical_writes
                   FROM dbo.QueryStats Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @FromDate
                   AND Q.SnapshotDate < @HourFrom) R
      UNION ALL
      /* The whole hours, from the rollup */
      SELECT H.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.SnapshotDate, Q.StatementID, Q.total_worker_time, Q.total_elapsed_time,
                          Q.execution_count, Q.total_logical_reads, Q.total_logical_writes
                   FROM dbo.QueryStats_60MIN Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @HourFrom
                   AND Q.SnapshotDate < @HourTo) H
      UNION ALL
      /* The part hour at the end */
      SELECT R.*
      FROM #Scope T
      CROSS APPLY (SELECT Q.InstanceID, Q.SnapshotDate, Q.StatementID, Q.total_worker_time, Q.total_elapsed_time,
                          Q.execution_count, Q.total_logical_reads, Q.total_logical_writes
                   FROM dbo.QueryStats Q WITH (FORCESEEK)
                   WHERE Q.InstanceID = T.InstanceID
                   AND Q.SnapshotDate >= @HourTo
                   AND Q.SnapshotDate < @ToExclusive) R) F
CROSS APPLY dbo.DateGroupingMins(F.SnapshotDate, @DateGroupingMin) DG
GROUP BY F.InstanceID, DG.DateGroup, F.StatementID
OPTION (RECOMPILE);

/*  What each statement in the window is, looked up once per statement, and whether the database filter keeps it.
    The rollup filter is applied later than this, because the coverage chart reports the rollups whatever it
    is set to. */
SELECT S.StatementID,
       S.DatabaseID,
       IsRollup = CONVERT(BIT, CASE WHEN S.StatementType IN (2, 3, 4) THEN 1 ELSE 0 END),
       InDatabase = CONVERT(BIT, CASE WHEN @DatabaseID IS NULL OR S.DatabaseID = @DatabaseID THEN 1 ELSE 0 END)
INTO #Kinds
FROM dbo.QueryStatements S
WHERE S.StatementID IN (SELECT A.StatementID FROM #Agg A)
OPTION (RECOMPILE);

/*
    Two labels are carried, because the two things a reader asks of this data are named differently: the
    statement, which is what the time series and its pie are built from, and the object it belongs to, which
    is what the object pie groups by.  Per statement is enough for both, including the instance prefix: a
    statement belongs to exactly one instance.
*/
SELECT S.StatementID,
       /*  The statement as a band: its own text, which is the thing being charted and what the reader is here
           for.  The object it belongs to is the object pie's business and is left out of the label - it is
           the same prefix on every statement of a procedure, and it crowds out the text in the space a legend
           or a tooltip has.  Null for a rollup, which is not a statement anyone can look at.  The object name
           does stand in where the text has not been collected yet, so the band is identifiable rather than
           being one of several called {No text}.  The instance prefix and the disambiguating id are added
           below, once the duplicates are known.  Wide enough for a qualified object name and its suffix. */
       StatementLabel = CONVERT(NVARCHAR(300),
                        CASE WHEN S.StatementType IN (2, 3, 4) THEN NULL
                             WHEN ST.LabelLong IS NOT NULL THEN ST.LabelLong
                             WHEN S.ObjectName IS NOT NULL THEN S.SchemaName + '.' + S.ObjectName + ': {No text}'
                             ELSE '{No text}'
                        END),
       RollupName = CASE S.StatementType
                        WHEN 2 THEN '{Other queries}'
                        WHEN 3 THEN '{Other variants}'
                        WHEN 4 THEN '{Other databases}'
                    END,
       /*  The object pie's label: the object where there is one, the statement text where there is not.
           Qualified with the instance only where there is more than one on screen, so the common case of a
           single instance keeps a legend of bare object names.  Wide enough for an instance name, the
           separator and a qualified object name. */
       ObjectSeries = CONVERT(NVARCHAR(400),
                      CASE WHEN @MultiInstance = 1 THEN I.InstanceDisplayName + ' / ' ELSE '' END
                      + CASE S.StatementType
                          WHEN 2 THEN '{Other queries}'
                          WHEN 3 THEN '{Other variants}'
                          WHEN 4 THEN '{Other databases}'
                          ELSE ISNULL(S.SchemaName + '.' + S.ObjectName, ISNULL(ST.LabelLong, '{Ad hoc}'))
                        END),
       /*  What an object slice can be drilled into.  An object or a statement label is something the grid
           report can filter on; a rollup band is not, so it is left null and the chart excludes it from
           drill-down.  Unqualified, because it is matched against the statement rather than displayed - the
           instance travels with it as its own parameter. */
       ObjectDrillValue = CASE WHEN S.StatementType IN (2, 3, 4) THEN NULL
                               ELSE ISNULL(S.SchemaName + '.' + S.ObjectName, ST.Label)
                          END,
       S.DatabaseID,
       IsRollup = CONVERT(BIT, CASE WHEN S.StatementType IN (2, 3, 4) THEN 1 ELSE 0 END),
       /*  The databases past the cap on their own rollups, combined: no one database, so the database pie puts
           it in {Other} rather than in the slice for rows whose database could not be resolved. */
       IsOtherDatabases = CONVERT(BIT, CASE WHEN S.StatementType = 4 THEN 1 ELSE 0 END)
INTO #Stmt
FROM dbo.QueryStatements S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
/*  An ad hoc shape is labelled from its template, not from any one variant's text - see the function */
OUTER APPLY dbo.QueryStatementText(S.sql_handle, S.statement_start_offset, S.statement_end_offset,
                                   S.StatementType, S.StatementTemplate) ST
WHERE S.StatementID IN (SELECT K.StatementID
                        FROM #Kinds K
                        WHERE K.InDatabase = 1
                        AND (@ExcludeRollups = 0 OR K.IsRollup = 0))
OPTION (RECOMPILE);

SELECT A.InstanceID,
       A.TimeBucket,
       A.StatementID,
       S.StatementLabel,
       S.RollupName,
       S.ObjectSeries,
       S.ObjectDrillValue,
       S.DatabaseID,
       A.WorkerTime,
       A.Value,
       S.IsRollup,
       S.IsOtherDatabases
INTO #Base
FROM #Agg A
JOIN #Stmt S ON S.StatementID = A.StatementID;

/*
    The band a statement gets in the time series and in its pie.  Built here rather than above because it has
    to be unique per statement: statement text repeats far more readily than an object name does - the same
    line in two procedures, or two statements whose text has not been collected yet and are both {No text} -
    and two statements sharing a band would be charted as one query and drill into whichever sorted first.
    So where a label covers more than one statement in an instance, the id is appended.
*/
ALTER TABLE #Base ADD Series NVARCHAR(600) NULL;

/*
    CASE rather than ISNULL for the rollup name, deliberately.  ISNULL returns the type of its FIRST argument,
    so ISNULL(RollupName, <the statement label>) silently cuts every label to the width of '{Other variants}' -
    sixteen characters, which reads as the statement text having been collected truncated rather than as a
    typing rule.  A CASE takes the widest branch, which is what is wanted here.
*/
UPDATE B
    SET Series = CASE WHEN @MultiInstance = 1 THEN I.InstanceDisplayName + ' / ' ELSE '' END
                 + CASE WHEN B.RollupName IS NOT NULL THEN B.RollupName
                        ELSE B.StatementLabel
                             + CASE WHEN X.Statements > 1 THEN ' #' + CONVERT(VARCHAR(12), B.StatementID) ELSE '' END
                   END
FROM #Base B
JOIN dbo.Instances I ON I.InstanceID = B.InstanceID
LEFT JOIN (SELECT InstanceID, StatementLabel, Statements = COUNT(DISTINCT StatementID)
           FROM #Base
           WHERE StatementLabel IS NOT NULL
           GROUP BY InstanceID, StatementLabel) X
    ON X.InstanceID = B.InstanceID AND X.StatementLabel = B.StatementLabel;

/*
    The statements worth naming, in measure order.  Everything else becomes one band rather than a legend
    nobody can read, and the rank is carried so the chart's bands and legend come out in the order a reader
    expects rather than in whatever order the rows happened to arrive.
*/
DECLARE @SeriesTotal DECIMAL(38, 6) = (SELECT SUM(Value) FROM #Base WHERE IsRollup = 0)

SELECT TOP (@TopN) Series, SeriesRank = ROW_NUMBER() OVER (ORDER BY SUM(Value) DESC, Series)
INTO #Top
FROM #Base
WHERE IsRollup = 0
GROUP BY Series
/*  Two ways to miss the cut: not being in the top N, or being too small to draw.  See @MinSlicePercent. */
HAVING SUM(Value) >= ISNULL(@SeriesTotal, 0) * @MinSlicePercent / 100.0
ORDER BY SUM(Value) DESC;

/*
    Resolved into columns rather than repeated as expressions: a GROUP BY cannot contain a subquery, and the
    result sets below all have to group by the same label.
*/
/*  Wide enough for the longest band Series can hold - an instance name, a qualified object and a statement
    label - so a long label errors nowhere and is cut nowhere. */
ALTER TABLE #Base ADD SeriesLabel NVARCHAR(600) NULL, SeriesRank INT NULL, IsResidual BIT NULL;

/*
    One residual band, not three.  The collection's own rollups and this proc's tail are different things -
    the first is detail that was never stored, the second is detail that is stored but not charted - and on a
    chart that difference reads as two slices with almost the same name.  The distinction is not lost by
    merging them: the Coverage chart splits the window into named, rolled up and unattributed, which is where
    the question "can I get this detail at all" belongs.  There the answer decides which lever to pull -
    raising the chart's Series picker, or raising Query Stats Top N on the connection.
*/
UPDATE B
    SET SeriesLabel = CASE WHEN B.IsRollup = 1 THEN '{Other}'
                           WHEN T.Series IS NOT NULL THEN B.Series
                           ELSE '{Other}' END,
        IsResidual = CASE WHEN B.IsRollup = 1 OR T.Series IS NULL THEN 1 ELSE 0 END
FROM #Base B
LEFT JOIN #Top T ON T.Series = B.Series;

/*
    Band order: the named queries by measure, then the residual bands by measure.  Residuals last whatever
    their size, because a band that stands for "everything else" belongs at the end of a stack rather than
    in the middle of the queries it is hiding - and {Other queries} is often the largest thing in the chart.
*/
SELECT SeriesLabel,
       SeriesRank = ROW_NUMBER() OVER (ORDER BY MAX(CONVERT(INT, IsResidual)), SUM(Value) DESC, SeriesLabel),
       WindowValue = SUM(Value)
INTO #Rank
FROM #Base
GROUP BY SeriesLabel;

/*
    The floor again, this time on the bands themselves.  Rolling the tail into {Other} is not enough on its
    own: where the tail is tiny, {Other} is a slice with no width too, and one of those names itself in every
    other slice's tooltip - which is the whole reason the floor exists.  So a band whose share of the window
    is below the floor is dropped rather than merged any further, because there is nothing left to merge it
    into.

    Judged on the window total rather than per bucket, so a band is either in the chart or not: a series that
    appeared in some buckets and vanished from others would shuffle the stack and the legend between columns.

    What this gives up is at most the floor's worth of the picture, and only from bands nobody could see or
    hover.  The Coverage chart still reads from the stored rows, so it remains the honest account of the
    window whatever the other three drop.
*/
DECLARE @ChartTotal DECIMAL(38, 6) = (SELECT SUM(Value) FROM #Base)

UPDATE B
    SET SeriesRank = R.SeriesRank
FROM #Base B
JOIN #Rank R ON R.SeriesLabel = B.SeriesLabel;

/*
    0 - Over time.  Stacked, so the height is the total and each band is a series.  TimeBucketEnd is the
    exclusive end of the bucket, carried so that clicking a column can drill into the grid report for
    exactly the window the column covered.

    Ordered by the measure first and the bucket second: the chart takes its series order from the order the
    rows arrive, so this is what puts the biggest band at the bottom of the stack and at the top of the
    legend.  Points within a series carry their own timestamps, so a series missing from a bucket leaves a
    gap rather than shifting the stack.

    A bucket whose every collection was skipped has no covered time to divide by, so its rate is unknown
    rather than zero.  Those rows are dropped rather than plotted as null: a gap in the chart is the honest
    picture of an interval nobody was watching.
*/
SELECT TimeBucket, TimeBucketEnd, Series, Value, StatementID, InstanceID
FROM (SELECT B.TimeBucket,
             TimeBucketEnd = DATEADD(MINUTE, @DateGroupingMin, B.TimeBucket),
             Series = B.SeriesLabel,
             SeriesRank = MIN(B.SeriesRank),
             /*  Each instance's work over its own covered time, then added up.  A stacked total is therefore
                 cores busy across everything on screen rather than an average of averages. */
             Value = CASE WHEN @Units = 'Total' THEN SUM(B.Value)
                          ELSE SUM(B.Value / NULLIF(C.CoveredMicroseconds / 1000000.0, 0)) END,
             /*  Only where the band is exactly one statement.  A residual band has nothing a filter could be
                 set to even when only one named query happens to be inside it today, so it is always null
                 rather than pointing at whichever query that was - and a click on it drills into the whole
                 bucket, which is what the band stands for. */
             StatementID = CASE WHEN MAX(CONVERT(INT, B.IsResidual)) = 1 THEN NULL
                                WHEN COUNT(DISTINCT B.StatementID) = 1 THEN MIN(B.StatementID) END,
             InstanceID = CASE WHEN COUNT(DISTINCT B.InstanceID) = 1 THEN MIN(B.InstanceID) END
      FROM #Base B
      LEFT JOIN #Cov C ON C.TimeBucket = B.TimeBucket AND C.InstanceID = B.InstanceID
      JOIN #Rank R ON R.SeriesLabel = B.SeriesLabel
      WHERE R.WindowValue >= ISNULL(@ChartTotal, 0) * @MinSlicePercent / 100.0
      GROUP BY B.TimeBucket, B.SeriesLabel) X
WHERE Value IS NOT NULL
ORDER BY SeriesRank, TimeBucket
OPTION (RECOMPILE);

/*
    1 - By statement over the whole window: the same bands as the time series, without the time.  InstanceID
    travels with the slice so a drill-down lands on the instance the slice belongs to rather than on every
    instance that happens to run a statement whose text reads the same.

    Slices drill into the statement's plans, since a statement filter is what the grid report reads as the plan
    grain: a statement expensive enough to be a slice here is one worth knowing ran under one plan or two.

    The statement's whole text comes back alongside the label.  The label is cut to a length a chart can draw,
    which is the right thing for a slice and the wrong thing for the grid behind it - a reader who has turned
    the results on wants the statement, not the first line of it.  Fetched by joining back on the id of the
    one statement a slice stands for rather than carried through the aggregate, so it costs one lookup per
    slice instead of a copy of every statement's text per time bucket.
*/
SELECT X.Series,
       X.Value,
       X.StatementID,
       X.InstanceID,
       ST.StatementText
FROM (SELECT Series = B.SeriesLabel,
             Value = SUM(B.Value),
             StatementID = CASE WHEN MAX(CONVERT(INT, B.IsResidual)) = 1 THEN NULL
                                WHEN COUNT(DISTINCT B.StatementID) = 1 THEN MIN(B.StatementID) END,
             InstanceID = CASE WHEN COUNT(DISTINCT B.InstanceID) = 1 THEN MIN(B.InstanceID) END
      FROM #Base B
      GROUP BY B.SeriesLabel
      HAVING SUM(B.Value) >= ISNULL(@ChartTotal, 0) * @MinSlicePercent / 100.0) X
/*  Outer joins throughout: {Other} has no statement, and a statement whose text has not been collected yet is
    still a slice worth showing. */
LEFT JOIN dbo.QueryStatements S ON S.StatementID = X.StatementID
OUTER APPLY dbo.QueryStatementText(S.sql_handle, S.statement_start_offset, S.statement_end_offset,
                                   S.StatementType, S.StatementTemplate) ST
ORDER BY X.Value DESC
OPTION (RECOMPILE);

/*
    2 - By object: a procedure's statements as one slice, which is the level the pie above deliberately does
    not work at.  Ranked and capped on its own rather than inherited from the statement bands, because the top
    ten statements and the top ten objects are not the same ten things - an object whose cost is spread thinly
    across forty statements has no band in the chart above and can still be one of the largest slices here.

    Statements with no object keep their own text as a label, as they do everywhere else here, so ad hoc work
    is not collapsed into a single unnamed slice.
*/
SELECT TOP (@TopN) ObjectSeries
INTO #TopObj
FROM #Base
WHERE IsRollup = 0
GROUP BY ObjectSeries
/*  The floor, for the same reason as everywhere else here: a slice too thin to hover over answers the chart's
    hit test everywhere and names itself in every other slice's tooltip. */
HAVING SUM(Value) >= ISNULL(@ChartTotal, 0) * @MinSlicePercent / 100.0
ORDER BY SUM(Value) DESC;

SELECT Series = ISNULL(T.ObjectSeries, '{Other}'),
       Value = SUM(B.Value),
       /*  Null for the {Other} slice, which is the tail and the collection's rollups together, and which the
           chart therefore excludes from drill-down. */
       DrillValue = CASE WHEN T.ObjectSeries IS NULL THEN NULL
                         WHEN COUNT(DISTINCT B.ObjectDrillValue) = 1 THEN MIN(B.ObjectDrillValue) END,
       InstanceID = CASE WHEN T.ObjectSeries IS NULL THEN NULL
                         WHEN COUNT(DISTINCT B.InstanceID) = 1 THEN MIN(B.InstanceID) END
FROM #Base B
/*  Rollups are matched to nothing on purpose, so they land in {Other} with the tail rather than as slices of
    their own - see the note on the residual bands above. */
LEFT JOIN #TopObj T ON T.ObjectSeries = B.ObjectSeries AND B.IsRollup = 0
GROUP BY ISNULL(T.ObjectSeries, '{Other}'), T.ObjectSeries
/*  Drops {Other} when the tail it collected is itself too thin to draw.  The named objects are above the floor
    by construction, so this only ever removes {Other}. */
HAVING SUM(B.Value) >= ISNULL(@ChartTotal, 0) * @MinSlicePercent / 100.0
ORDER BY Value DESC
OPTION (RECOMPILE);

/*
    3 - By database.  Capped the same way as the series, because an estate with fifty databases in context
    otherwise draws fifty slices, and a legend and tooltip nobody can read.
*/
/*  A database is identified by its id, not its name: the same name on two instances is two databases, and a
    slice that merges them is a slice of nothing in particular.  The name alone is shown where only one
    instance is on screen, since there is nothing to disambiguate it from. */
/*  {Other databases} is several databases, so it has no name of its own here: a null name joins no top database
    below and lands in {Other} with the tail, which is what it is. */
SELECT B.DatabaseID,
       B.InstanceID,
       BaseName = CASE WHEN B.IsOtherDatabases = 1 THEN NULL
                       ELSE CASE WHEN @MultiInstance = 1 THEN I.InstanceDisplayName + ' / ' ELSE '' END
                            + ISNULL(D.name, '{Unknown}')
                  END,
       Value = SUM(B.Value)
INTO #DB
FROM #Base B
LEFT JOIN dbo.Databases D ON D.DatabaseID = B.DatabaseID
LEFT JOIN dbo.Instances I ON I.InstanceID = B.InstanceID
GROUP BY B.DatabaseID,
         B.InstanceID,
         CASE WHEN B.IsOtherDatabases = 1 THEN NULL
              ELSE CASE WHEN @MultiInstance = 1 THEN I.InstanceDisplayName + ' / ' ELSE '' END
                   + ISNULL(D.name, '{Unknown}')
         END;

/*
    A slice's label has to name exactly one database, because clicking it sends that database's id to the grid
    report.  Two databases can still share a name within one instance once a database has been dropped and
    created again - dbo.Databases keeps the old row - so where that happens the id is appended.  An ugly label
    on a rare slice is better than a slice that drills into whichever of the two sorted first.
*/
ALTER TABLE #DB ADD DatabaseName NVARCHAR(300) NULL;

UPDATE D
    SET DatabaseName = CASE WHEN X.Databases > 1 THEN D.BaseName + ' #' + CONVERT(VARCHAR(12), D.DatabaseID)
                            ELSE D.BaseName END
FROM #DB D
JOIN (SELECT BaseName, Databases = COUNT(DISTINCT DatabaseID) FROM #DB GROUP BY BaseName) X
    ON X.BaseName = D.BaseName;

DECLARE @DBTotal DECIMAL(38, 6) = (SELECT SUM(Value) FROM #DB)

SELECT TOP (@TopN) DatabaseName
INTO #TopDB
FROM #DB
WHERE DatabaseName IS NOT NULL
GROUP BY DatabaseName
/*  A database worth a thousandth of a percent of the window - Deadlock at 1.5ms against five minutes of CPU -
    is a slice with no width, and one of those names itself in every other slice's tooltip. */
HAVING SUM(Value) >= ISNULL(@DBTotal, 0) * @MinSlicePercent / 100.0
ORDER BY SUM(Value) DESC;

/* Joined rather than tested with EXISTS, because a GROUP BY cannot contain a subquery */
SELECT Series = ISNULL(T.DatabaseName, '{Other}'),
       Value = SUM(D.Value),
       /*  What the slice drills into.  -1 stands for the rows whose database could not be resolved, and null
           for the {Other} tail, which the chart excludes from drill-down because it is several databases. */
       DatabaseID = CASE WHEN T.DatabaseName IS NULL THEN NULL
                         ELSE ISNULL(MIN(D.DatabaseID), -1) END,
       InstanceID = CASE WHEN T.DatabaseName IS NULL THEN NULL
                         WHEN COUNT(DISTINCT D.InstanceID) = 1 THEN MIN(D.InstanceID) END
FROM #DB D
LEFT JOIN #TopDB T ON T.DatabaseName = D.DatabaseName
GROUP BY ISNULL(T.DatabaseName, '{Other}'), T.DatabaseName
/*  Drops the {Other} slice when the tail it collected is itself too small to draw.  The named databases are
    all above the floor by construction, so this only ever removes {Other}. */
HAVING SUM(D.Value) >= ISNULL(@DBTotal, 0) * @MinSlicePercent / 100.0
ORDER BY Value DESC
OPTION (RECOMPILE);

/*
    4 - How much of the window's CPU is actually named.  Always worker time, whatever measure the caller
    picked, because that is the only figure all three parts of it exist for.  A large Unattributed slice
    means the collection could not place that work in time - a first collection, or an instance whose plan
    cache churns faster than the schedule.  It is the collector's estimate of the window's share of that work,
    so it sits on the same scale as the other two.  A large Rolled up slice means the top N is too small.

    Read from #Agg, which holds every statement in the window, rather than from #Base, so switching the rollups
    off in the chart above does not change what this one says about the window.  Not from the stored rows again:
    #Agg already has their worker time, and reading the window twice more for it doubled the cost of the proc.
*/
SELECT Series,
       Value
FROM (
    SELECT Series = CASE WHEN K.IsRollup = 1 THEN 'Rolled up' ELSE 'Named' END,
           Value = SUM(A.WorkerTime) / 1000.0
    FROM #Agg A
    JOIN #Kinds K ON K.StatementID = A.StatementID
    WHERE K.InDatabase = 1
    GROUP BY K.IsRollup
    UNION ALL
    SELECT 'Unattributed', (SELECT ISNULL(SUM(UnattributedWorkerTime), 0) / 1000.0 FROM #Cov)
) X
WHERE Value > 0
ORDER BY Value DESC
OPTION (RECOMPILE);
