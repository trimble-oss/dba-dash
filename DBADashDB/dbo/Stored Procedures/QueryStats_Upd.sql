CREATE PROC dbo.QueryStats_Upd (
    @QueryStats dbo.QueryStats READONLY,
    @QueryStatsCollection dbo.QueryStatsCollection READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(3)
)
AS
/*
    Store statement level deltas, and the collection row that describes them.

    What arrives here has already been diffed, ranked and rolled up by the collector, so this proc does no
    arithmetic on counters: it resolves identity and inserts.  The diff cannot be done here because the raw
    grain - one row per cached plan per statement - would mean sending thousands of rows per instance per
    interval just to compute a few hundred, and holding a baseline for every one of them in the repository.

    Identity resolution is the whole job.  Statements are keyed per instance on a computed StatementKey,
    because the statement types are identified by different columns and the alternative is a join with an
    OR branch per type.  CONCAT is used to build it deliberately: it renders NULL as an empty string, so a row
    whose database could not be resolved on the source gets a stable key of its own rather than failing to
    match anything.

    The other things stored here are an ad hoc shape's template, with the batch of the example variant it was
    made from, and the plans of the plan shapes.  The collector sends a template once per shape and a plan once
    per plan shape, each on one of its rows, and again only when it has lost track of having sent it - so the
    first to arrive is kept and the rest are ignored.

    The collection row is written in the same transaction as the deltas.  Every rate the reports show divides
    one by the other's covered time, so a window holding one without the other is wrong rather than partial.
    It is written even when there are no deltas: an interval with nothing to report and an interval that was
    skipped are different things, and only dbo.QueryStatsCollection tells them apart.

    The deltas and the collection row are each added to their hour, in dbo.QueryStats_60MIN and
    dbo.QueryStatsCollection_60MIN, in the same transaction again, so the hourly rollups always agree with the
    raw rows.
*/
DECLARE @Ref VARCHAR(30) = 'QueryStats'
SET XACT_ABORT ON
SET NOCOUNT ON

/*
    Refuse a collection that overlaps one already stored for this instance.  The collector moves its baseline
    on before it writes, so each collection's period starts exactly where the previous one ended, and a
    re-import of a stored collection is the same collection rather than an overlap.  An overlap means the same
    work is being reported twice - a baseline file older than what the service has already sent, after a save
    failed and the service restarted, or two services collecting the same connection - and storing it would
    count that work twice with nothing downstream able to tell.  A gap is the better failure, because coverage
    shows it.

    Logged as a warning rather than raised.  An error would be retried, and then the whole file treated as a
    failed import, when no retry will ever import it.

    Skipped collections carry no deltas.  A first collection is not checked either: with no baseline it can only
    report plans compiled inside its own short window, and that window can legitimately start before the
    previous collection ended when a baseline was lost in between.
*/
DECLARE @OverlapSnapshotDate DATETIME2(3)
DECLARE @OverlapPeriodStart DATETIME2(3)

SELECT TOP (1) @OverlapSnapshotDate = N.SnapshotDate,
               @OverlapPeriodStart = NP.PeriodStartTime
FROM @QueryStatsCollection N
CROSS APPLY (SELECT PeriodStartTime = DATEADD(day, -(N.PeriodTime / (86400000000.)), DATEADD(millisecond, -((N.PeriodTime % (86400000000.)) / (1000)), N.SnapshotDate))) NP
WHERE N.IsSkipped = 0
AND N.IsFirstCollection = 0
AND EXISTS (SELECT 1
            FROM dbo.QueryStatsCollection C
            WHERE C.InstanceID = @InstanceID
            AND C.SnapshotDate > NP.PeriodStartTime
            AND C.SnapshotDate <> N.SnapshotDate
            AND C.PeriodStartTime < N.SnapshotDate
            AND C.IsSkipped = 0);

IF @OverlapSnapshotDate IS NOT NULL
BEGIN
    /*  Dated now rather than with @SnapshotDate.  The collection's own errors are logged with the snapshot date
        after this, and CollectionErrorLog_Add skips a batch whose date is already logged for the instance. */
    INSERT INTO dbo.CollectionErrorLog (ErrorDate, InstanceID, ErrorSource, ErrorMessage, ErrorContext)
    VALUES (SYSUTCDATETIME(), @InstanceID, @Ref,
            CONCAT('Warning: the query stats collection at ', CONVERT(VARCHAR(23), @OverlapSnapshotDate, 121),
                   ' (from ', CONVERT(VARCHAR(23), @OverlapPeriodStart, 121), ') overlaps a collection already stored, so it was not imported.  ',
                   'Storing it would count the work in the overlap twice.  This happens when the service''s query stats baseline file is older than what it has already sent - ',
                   'a save that failed before a restart, or a file restored from a backup - or when two services collect the same connection.'),
            'Import');
    RETURN;
END

CREATE TABLE #QueryStats (
    StatementKey BINARY(20) NOT NULL,
    StatementID BIGINT NULL,
    StatementType TINYINT NOT NULL,
    DatabaseID INT NULL,
    SchemaName NVARCHAR(128) NULL,
    ObjectName NVARCHAR(128) NULL,
    object_id INT NULL,
    sql_handle VARBINARY(64) NULL,
    statement_start_offset INT NOT NULL,
    statement_end_offset INT NOT NULL,
    query_hash BINARY(8) NULL,
    query_plan_hash BINARY(8) NOT NULL,
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    execution_count BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    total_clr_time BIGINT NOT NULL,
    total_rows BIGINT NULL,
    total_dop BIGINT NULL,
    total_grant_kb BIGINT NULL,
    total_used_grant_kb BIGINT NULL,
    total_spills BIGINT NULL,
    PlanCount INT NOT NULL,
    IsCompile BIT NOT NULL,
    IsOtherPlans BIT NOT NULL,
    StatementTemplate NVARCHAR(MAX) NULL,
    ExampleBatchText NVARCHAR(MAX) NULL,
    query_plan_compressed VARBINARY(MAX) NULL
);

/*
    Resolve the database, then build the key.  A database that could not be resolved leaves DatabaseID NULL
    rather than dropping the row: the work still belongs to the interval, and losing it would make the
    period totals wrong, which is the one thing this collection is built not to do.
*/
INSERT INTO #QueryStats (
    StatementKey, StatementType, DatabaseID, SchemaName, ObjectName, object_id, sql_handle,
    statement_start_offset, statement_end_offset, query_hash, query_plan_hash, SnapshotDate, PeriodTime,
    execution_count, total_worker_time, total_elapsed_time, total_logical_reads, total_logical_writes,
    total_physical_reads, total_clr_time, total_rows, total_dop, total_grant_kb, total_used_grant_kb,
    total_spills, PlanCount, IsCompile, IsOtherPlans, StatementTemplate, ExampleBatchText, query_plan_compressed
)
SELECT CONVERT(BINARY(20), HASHBYTES('SHA2_256',
            CASE t.StatementType
                WHEN 0 THEN CONCAT('0|', d.DatabaseID, '|', CONVERT(VARCHAR(130), t.sql_handle, 1), '|', t.statement_start_offset, '|', t.statement_end_offset)
                WHEN 1 THEN CONCAT('1|', d.DatabaseID, '|', t.schema_name, '|', t.object_name, '|', t.statement_start_offset, '|', t.statement_end_offset)
                WHEN 2 THEN CONCAT('2|', d.DatabaseID)
                WHEN 3 THEN CONCAT('3|', d.DatabaseID, '|', CONVERT(VARCHAR(20), t.query_hash, 1))
                WHEN 4 THEN '4|'
                WHEN 5 THEN CONCAT('5|', d.DatabaseID, '|', CONVERT(VARCHAR(20), t.query_hash, 1))
            END)),
       t.StatementType,
       d.DatabaseID,
       t.schema_name,
       t.object_name,
       t.object_id,
       t.sql_handle,
       t.statement_start_offset,
       t.statement_end_offset,
       t.query_hash,
       ISNULL(t.query_plan_hash, 0x0000000000000000),
       t.SnapshotDate,
       t.PeriodTime,
       t.execution_count,
       t.total_worker_time,
       t.total_elapsed_time,
       t.total_logical_reads,
       t.total_logical_writes,
       t.total_physical_reads,
       t.total_clr_time,
       t.total_rows,
       t.total_dop,
       t.total_grant_kb,
       t.total_used_grant_kb,
       t.total_spills,
       t.PlanCount,
       t.IsCompile,
       t.IsOtherPlans,
       t.StatementTemplate,
       t.ExampleBatchText,
       t.query_plan_compressed
FROM @QueryStats t
LEFT JOIN dbo.Databases d
    ON d.InstanceID = @InstanceID
    AND d.name = t.database_name
    AND d.IsActive = 1;

/*
    What the inserts below actually store, which is what the hourly rollups are added to.  Captured rather than
    taken from the parameters, because a re-import of a collection stores nothing, and must add nothing.
*/
CREATE TABLE #Stored (
    SnapshotDate DATETIME2(3) NOT NULL,
    StatementID BIGINT NOT NULL,
    query_plan_hash BINARY(8) NOT NULL,
    PeriodStartTime DATETIME2(3) NOT NULL,
    execution_count BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    total_clr_time BIGINT NOT NULL,
    total_rows BIGINT NULL,
    total_dop BIGINT NULL,
    total_grant_kb BIGINT NULL,
    total_used_grant_kb BIGINT NULL,
    total_spills BIGINT NULL,
    PlanCount INT NOT NULL,
    IsCompile BIT NOT NULL,
    IsOtherPlans BIT NOT NULL
);

CREATE TABLE #StoredCollection (
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    UnattributedWorkerTime BIGINT NOT NULL,
    UnattributedElapsedTime BIGINT NOT NULL,
    UnattributedExecutions BIGINT NOT NULL,
    BaselineEvictions INT NOT NULL,
    ReadDurationMs INT NOT NULL,
    IsFirstCollection BIT NOT NULL,
    IsSkipped BIT NOT NULL
);

BEGIN TRAN;

/*
    Every read of QueryStatements below is forced to seek.  Imports for different instances run in parallel
    and each holds X locks on the rows it has inserted or updated until commit.  A scan reads every
    instance's rows, so it waits on another import's locks while holding its own, and parallel imports
    deadlock.  The UPDATE is the worst case because it takes a U lock on every row it reads.  A small or
    newly created table makes a scan the cheapest plan, so the cost model can't be trusted to avoid it.
*/

/*  Statements seen for the first time on this instance.  A shape's rows all carry the same example, so its
    handle and offsets here are the ones its template, where one came with it, was made from. */
INSERT INTO dbo.QueryStatements (
    InstanceID, StatementKey, StatementType, DatabaseID, SchemaName, ObjectName, object_id, sql_handle,
    statement_start_offset, statement_end_offset, query_hash, FirstSeen, LastSeen, StatementTemplate
)
SELECT @InstanceID,
       t.StatementKey,
       MIN(t.StatementType),
       MIN(t.DatabaseID),
       MIN(t.SchemaName),
       MIN(t.ObjectName),
       MAX(t.object_id),
       MAX(t.sql_handle),
       MIN(t.statement_start_offset),
       MIN(t.statement_end_offset),
       MAX(t.query_hash),
       @SnapshotDate,
       @SnapshotDate,
       MAX(t.StatementTemplate)
FROM #QueryStats t
WHERE NOT EXISTS (SELECT 1
                  FROM dbo.QueryStatements s WITH (FORCESEEK)
                  WHERE s.InstanceID = @InstanceID
                  AND s.StatementKey = t.StatementKey)
GROUP BY t.StatementKey;

UPDATE t
    SET t.StatementID = s.StatementID
FROM #QueryStats t
JOIN dbo.QueryStatements s WITH (FORCESEEK)
    ON s.InstanceID = @InstanceID
    AND s.StatementKey = t.StatementKey;

/*
    Refresh the attributes that move without the statement changing.  The handle is the one that matters:
    a module handle contains the object_id, so it changes at every redeploy, and text collection needs the
    current one.  Written only when it actually changed, so a statement seen every interval is not an
    update every interval.

    Not a shape's handle, which is a different variant almost every interval.  It is its example's, and only
    changes with its template - below.
*/
UPDATE s
    SET s.LastSeen = @SnapshotDate,
        s.sql_handle = ISNULL(t.sql_handle, s.sql_handle),
        s.object_id = ISNULL(t.object_id, s.object_id)
FROM dbo.QueryStatements s WITH (FORCESEEK (PK_QueryStatements (StatementID)))
JOIN (SELECT StatementID,
             MAX(CASE WHEN StatementType <> 5 THEN sql_handle END) AS sql_handle,
             MAX(object_id) AS object_id
      FROM #QueryStats
      GROUP BY StatementID) t
    ON t.StatementID = s.StatementID
WHERE s.LastSeen < DATEADD(hh, -1, @SnapshotDate)
OR ISNULL(t.sql_handle, s.sql_handle) <> ISNULL(s.sql_handle, 0x)
OR ISNULL(t.object_id, s.object_id) <> ISNULL(s.object_id, -1);

/*
    A shape stored before its template arrived takes it now - the text is fetched separately from the statistics
    and capped per collection - along with the example it was made from, which is usually a different variant
    from the one the shape was stored with.  Only where it has none: a template sent again by a collector that
    lost track of sending it is the same shape, made from another variant, and changes nothing worth a write.
*/
UPDATE s
    SET s.StatementTemplate = t.StatementTemplate,
        s.sql_handle = t.sql_handle,
        s.statement_start_offset = t.statement_start_offset,
        s.statement_end_offset = t.statement_end_offset
FROM dbo.QueryStatements s WITH (FORCESEEK (PK_QueryStatements (StatementID)))
JOIN (SELECT StatementID,
             MAX(StatementTemplate) AS StatementTemplate,
             MAX(sql_handle) AS sql_handle,
             MIN(statement_start_offset) AS statement_start_offset,
             MIN(statement_end_offset) AS statement_end_offset
      FROM #QueryStats
      WHERE StatementTemplate IS NOT NULL
      GROUP BY StatementID) t
    ON t.StatementID = s.StatementID
WHERE s.StatementTemplate IS NULL;

/*
    The example's batch, for the shapes whose template was taken above or with the statement - which is what
    their handle now matching the row's says.  Stored here rather than collected as text so a template that was
    ignored leaves no batch behind.  Into dbo.QueryText, where every other batch is, so the batch link and the
    purge treat it like any other: kept while the statement points at it.
*/
INSERT INTO dbo.QueryText (sql_handle, dbid, object_id, encrypted, text, SnapshotDate, CollectInstanceID)
SELECT t.sql_handle,
       NULL,
       NULL,
       NULL,
       MAX(t.ExampleBatchText),
       @SnapshotDate,
       @InstanceID
FROM #QueryStats t
JOIN dbo.QueryStatements s WITH (FORCESEEK (PK_QueryStatements (StatementID)))
    ON s.StatementID = t.StatementID
    AND s.sql_handle = t.sql_handle
WHERE t.ExampleBatchText IS NOT NULL
AND NOT EXISTS (SELECT 1
                FROM dbo.QueryText QT
                WHERE QT.sql_handle = t.sql_handle)
GROUP BY t.sql_handle;

/*
    The plans the collector fetched, each for the statement and plan shape of the row it came on.  Only where there
    is none yet: the collector sends a plan shape's plan again when it has lost track of sending it - after a
    restart, or a day on - and the plan of a shape already stored is the same operators.  The lookup is forced to
    seek, for the reason given for dbo.QueryStatements above.
*/
INSERT INTO dbo.QueryStatsPlans (StatementID, query_plan_hash, query_plan_compressed, CaptureDate)
SELECT t.StatementID,
       t.query_plan_hash,
       MAX(t.query_plan_compressed),
       @SnapshotDate
FROM #QueryStats t
WHERE t.query_plan_compressed IS NOT NULL
AND t.StatementID IS NOT NULL
AND t.query_plan_hash <> 0x0000000000000000
AND NOT EXISTS (SELECT 1
                FROM dbo.QueryStatsPlans P WITH (FORCESEEK)
                WHERE P.StatementID = t.StatementID
                AND P.query_plan_hash = t.query_plan_hash)
GROUP BY t.StatementID, t.query_plan_hash;

INSERT INTO dbo.QueryStats (
    InstanceID, SnapshotDate, StatementID, query_plan_hash, PeriodTime, execution_count, total_worker_time,
    total_elapsed_time, total_logical_reads, total_logical_writes, total_physical_reads, total_clr_time,
    total_rows, total_dop, total_grant_kb, total_used_grant_kb, total_spills, PlanCount, IsCompile,
    IsOtherPlans
)
OUTPUT inserted.SnapshotDate, inserted.StatementID, inserted.query_plan_hash, inserted.PeriodStartTime,
       inserted.execution_count, inserted.total_worker_time, inserted.total_elapsed_time,
       inserted.total_logical_reads, inserted.total_logical_writes, inserted.total_physical_reads,
       inserted.total_clr_time, inserted.total_rows, inserted.total_dop, inserted.total_grant_kb,
       inserted.total_used_grant_kb, inserted.total_spills, inserted.PlanCount, inserted.IsCompile,
       inserted.IsOtherPlans
INTO #Stored (
    SnapshotDate, StatementID, query_plan_hash, PeriodStartTime, execution_count, total_worker_time,
    total_elapsed_time, total_logical_reads, total_logical_writes, total_physical_reads, total_clr_time,
    total_rows, total_dop, total_grant_kb, total_used_grant_kb, total_spills, PlanCount, IsCompile,
    IsOtherPlans
)
SELECT @InstanceID,
       t.SnapshotDate,
       t.StatementID,
       t.query_plan_hash,
       MAX(t.PeriodTime),
       SUM(t.execution_count),
       SUM(t.total_worker_time),
       SUM(t.total_elapsed_time),
       SUM(t.total_logical_reads),
       SUM(t.total_logical_writes),
       SUM(t.total_physical_reads),
       SUM(t.total_clr_time),
       SUM(t.total_rows),
       SUM(t.total_dop),
       SUM(t.total_grant_kb),
       SUM(t.total_used_grant_kb),
       SUM(t.total_spills),
       SUM(t.PlanCount),
       CONVERT(BIT, MAX(CONVERT(TINYINT, t.IsCompile))),
       CONVERT(BIT, MAX(CONVERT(TINYINT, t.IsOtherPlans)))
FROM #QueryStats t
WHERE t.StatementID IS NOT NULL
AND NOT EXISTS (SELECT 1
                FROM dbo.QueryStats q
                WHERE q.InstanceID = @InstanceID
                AND q.SnapshotDate = t.SnapshotDate
                AND q.StatementID = t.StatementID
                AND q.query_plan_hash = t.query_plan_hash)
/*
    Grouped because two rows can collapse to one identity here even though they were distinct on the
    source: a statement whose database could not be resolved, or two objects that differ only by a case
    insensitive name comparison.  Summing is right for a re-import of the same interval only because the
    NOT EXISTS above makes this proc idempotent per snapshot.
*/
GROUP BY t.SnapshotDate, t.StatementID, t.query_plan_hash;

/*
    The hourly rollup: what was just stored, added to its hour.  dbo.QueryStats_60MIN then holds exactly what
    dbo.QueryStats does, summed by the hour, which the reports rely on to read whole hours from one and part hours
    from the other.  An hour is the collections whose snapshot falls in it.  Measures add, the cache entry count
    and the flags keep their largest, and the window the row's work was done in widens.

    Every read of the rollup is forced to seek, for the reason given for dbo.QueryStatements above.
*/
SELECT S.StatementID,
       S.query_plan_hash,
       Hour = CONVERT(DATETIME2(3), DG.DateGroup),
       execution_count = SUM(S.execution_count),
       total_worker_time = SUM(S.total_worker_time),
       total_elapsed_time = SUM(S.total_elapsed_time),
       total_logical_reads = SUM(S.total_logical_reads),
       total_logical_writes = SUM(S.total_logical_writes),
       total_physical_reads = SUM(S.total_physical_reads),
       total_clr_time = SUM(S.total_clr_time),
       total_rows = SUM(S.total_rows),
       total_dop = SUM(S.total_dop),
       total_grant_kb = SUM(S.total_grant_kb),
       total_used_grant_kb = SUM(S.total_used_grant_kb),
       total_spills = SUM(S.total_spills),
       MaxPlanCount = MAX(S.PlanCount),
       IsCompile = CONVERT(BIT, MAX(CONVERT(TINYINT, S.IsCompile))),
       IsOtherPlans = CONVERT(BIT, MAX(CONVERT(TINYINT, S.IsOtherPlans))),
       FirstPeriodStart = MIN(S.PeriodStartTime),
       LastSnapshotDate = MAX(S.SnapshotDate)
INTO #Hourly
FROM #Stored S
CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate, 60) DG
GROUP BY DG.DateGroup, S.StatementID, S.query_plan_hash;

UPDATE H
    SET H.execution_count += T.execution_count,
        H.total_worker_time += T.total_worker_time,
        H.total_elapsed_time += T.total_elapsed_time,
        H.total_logical_reads += T.total_logical_reads,
        H.total_logical_writes += T.total_logical_writes,
        H.total_physical_reads += T.total_physical_reads,
        H.total_clr_time += T.total_clr_time,
        /*  Null only while neither side has the counter, as a SUM over the raw rows would be: an instance
            upgraded within the hour starts counting rather than wiping out what the hour had */
        H.total_rows = CASE WHEN H.total_rows IS NULL AND T.total_rows IS NULL THEN NULL
                            ELSE ISNULL(H.total_rows, 0) + ISNULL(T.total_rows, 0) END,
        H.total_dop = CASE WHEN H.total_dop IS NULL AND T.total_dop IS NULL THEN NULL
                           ELSE ISNULL(H.total_dop, 0) + ISNULL(T.total_dop, 0) END,
        H.total_grant_kb = CASE WHEN H.total_grant_kb IS NULL AND T.total_grant_kb IS NULL THEN NULL
                                ELSE ISNULL(H.total_grant_kb, 0) + ISNULL(T.total_grant_kb, 0) END,
        H.total_used_grant_kb = CASE WHEN H.total_used_grant_kb IS NULL AND T.total_used_grant_kb IS NULL THEN NULL
                                     ELSE ISNULL(H.total_used_grant_kb, 0) + ISNULL(T.total_used_grant_kb, 0) END,
        H.total_spills = CASE WHEN H.total_spills IS NULL AND T.total_spills IS NULL THEN NULL
                              ELSE ISNULL(H.total_spills, 0) + ISNULL(T.total_spills, 0) END,
        H.MaxPlanCount = CASE WHEN T.MaxPlanCount > H.MaxPlanCount THEN T.MaxPlanCount ELSE H.MaxPlanCount END,
        H.IsCompile = H.IsCompile | T.IsCompile,
        H.IsOtherPlans = H.IsOtherPlans | T.IsOtherPlans,
        H.FirstPeriodStart = CASE WHEN T.FirstPeriodStart < H.FirstPeriodStart THEN T.FirstPeriodStart ELSE H.FirstPeriodStart END,
        H.LastSnapshotDate = CASE WHEN T.LastSnapshotDate > H.LastSnapshotDate THEN T.LastSnapshotDate ELSE H.LastSnapshotDate END
FROM dbo.QueryStats_60MIN H WITH (FORCESEEK)
JOIN #Hourly T
    ON H.InstanceID = @InstanceID
    AND H.SnapshotDate = T.Hour
    AND H.StatementID = T.StatementID
    AND H.query_plan_hash = T.query_plan_hash;

INSERT INTO dbo.QueryStats_60MIN (
    InstanceID, SnapshotDate, StatementID, query_plan_hash, execution_count, total_worker_time,
    total_elapsed_time, total_logical_reads, total_logical_writes, total_physical_reads, total_clr_time,
    total_rows, total_dop, total_grant_kb, total_used_grant_kb, total_spills, MaxPlanCount, IsCompile,
    IsOtherPlans, FirstPeriodStart, LastSnapshotDate
)
SELECT @InstanceID,
       T.Hour,
       T.StatementID,
       T.query_plan_hash,
       T.execution_count,
       T.total_worker_time,
       T.total_elapsed_time,
       T.total_logical_reads,
       T.total_logical_writes,
       T.total_physical_reads,
       T.total_clr_time,
       T.total_rows,
       T.total_dop,
       T.total_grant_kb,
       T.total_used_grant_kb,
       T.total_spills,
       T.MaxPlanCount,
       T.IsCompile,
       T.IsOtherPlans,
       T.FirstPeriodStart,
       T.LastSnapshotDate
FROM #Hourly T
WHERE NOT EXISTS (SELECT 1
                  FROM dbo.QueryStats_60MIN H WITH (FORCESEEK)
                  WHERE H.InstanceID = @InstanceID
                  AND H.SnapshotDate = T.Hour
                  AND H.StatementID = T.StatementID
                  AND H.query_plan_hash = T.query_plan_hash);

INSERT INTO dbo.QueryStatsCollection (
    InstanceID, SnapshotDate, PeriodTime, DMVRowCount, RowsWithDelta, RowsUnattributed,
    UnattributedWorkerTime, UnattributedElapsedTime, UnattributedExecutions, FamilyCount, FamiliesKept,
    StatementsKept, RowsPersisted, BaselineCount, BaselineEvictions, ReadDurationMs, ProcessingDurationMs,
    IsFirstCollection, IsSkipped
)
OUTPUT inserted.SnapshotDate, inserted.PeriodTime, inserted.UnattributedWorkerTime, inserted.UnattributedElapsedTime,
       inserted.UnattributedExecutions, inserted.BaselineEvictions, inserted.ReadDurationMs,
       inserted.IsFirstCollection, inserted.IsSkipped
INTO #StoredCollection (
    SnapshotDate, PeriodTime, UnattributedWorkerTime, UnattributedElapsedTime, UnattributedExecutions,
    BaselineEvictions, ReadDurationMs, IsFirstCollection, IsSkipped
)
SELECT @InstanceID,
       t.SnapshotDate,
       t.PeriodTime,
       t.DMVRowCount,
       t.RowsWithDelta,
       t.RowsUnattributed,
       t.UnattributedWorkerTime,
       t.UnattributedElapsedTime,
       t.UnattributedExecutions,
       t.FamilyCount,
       t.FamiliesKept,
       t.StatementsKept,
       t.RowsPersisted,
       t.BaselineCount,
       t.BaselineEvictions,
       t.ReadDurationMs,
       t.ProcessingDurationMs,
       t.IsFirstCollection,
       t.IsSkipped
FROM @QueryStatsCollection t
WHERE NOT EXISTS (SELECT 1
                  FROM dbo.QueryStatsCollection c
                  WHERE c.InstanceID = @InstanceID
                  AND c.SnapshotDate = t.SnapshotDate);

/*
    The hourly rollup of the collection rows, for the hours dbo.QueryStats_60MIN holds: the time each hour's
    collections covered, which every rate over those hours divides by, and what they could not account for.  A
    skipped collection counts as a collection that covered nothing, as it does in the raw rows.
*/
SELECT Hour = CONVERT(DATETIME2(3), DG.DateGroup),
       CoveredTime = SUM(CASE WHEN C.IsSkipped = 1 THEN 0 ELSE C.PeriodTime END),
       Collections = COUNT(*),
       SkippedCollections = SUM(CONVERT(INT, C.IsSkipped)),
       FirstCollections = SUM(CONVERT(INT, C.IsFirstCollection)),
       UnattributedWorkerTime = SUM(C.UnattributedWorkerTime),
       UnattributedElapsedTime = SUM(C.UnattributedElapsedTime),
       UnattributedExecutions = SUM(C.UnattributedExecutions),
       BaselineEvictions = SUM(C.BaselineEvictions),
       MaxReadDurationMs = MAX(C.ReadDurationMs)
INTO #HourlyCollection
FROM #StoredCollection C
CROSS APPLY dbo.DateGroupingMins(C.SnapshotDate, 60) DG
GROUP BY DG.DateGroup;

UPDATE H
    SET H.CoveredTime += T.CoveredTime,
        H.Collections += T.Collections,
        H.SkippedCollections += T.SkippedCollections,
        H.FirstCollections += T.FirstCollections,
        H.UnattributedWorkerTime += T.UnattributedWorkerTime,
        H.UnattributedElapsedTime += T.UnattributedElapsedTime,
        H.UnattributedExecutions += T.UnattributedExecutions,
        H.BaselineEvictions += T.BaselineEvictions,
        H.MaxReadDurationMs = CASE WHEN T.MaxReadDurationMs > H.MaxReadDurationMs THEN T.MaxReadDurationMs ELSE H.MaxReadDurationMs END
FROM dbo.QueryStatsCollection_60MIN H WITH (FORCESEEK)
JOIN #HourlyCollection T
    ON H.InstanceID = @InstanceID
    AND H.SnapshotDate = T.Hour;

INSERT INTO dbo.QueryStatsCollection_60MIN (
    InstanceID, SnapshotDate, CoveredTime, Collections, SkippedCollections, FirstCollections,
    UnattributedWorkerTime, UnattributedElapsedTime, UnattributedExecutions, BaselineEvictions, MaxReadDurationMs
)
SELECT @InstanceID,
       T.Hour,
       T.CoveredTime,
       T.Collections,
       T.SkippedCollections,
       T.FirstCollections,
       T.UnattributedWorkerTime,
       T.UnattributedElapsedTime,
       T.UnattributedExecutions,
       T.BaselineEvictions,
       T.MaxReadDurationMs
FROM #HourlyCollection T
WHERE NOT EXISTS (SELECT 1
                  FROM dbo.QueryStatsCollection_60MIN H WITH (FORCESEEK)
                  WHERE H.InstanceID = @InstanceID
                  AND H.SnapshotDate = T.Hour);

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = @Ref,
                             @SnapshotDate = @SnapshotDate;

COMMIT;
