CREATE PROC dbo.LatchStats_Upd(
    @LatchStats dbo.LatchStats READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(2)
)
AS
/*
    sys.dm_os_latch_stats is cumulative since the instance started (or the stats were cleared).
    The previous snapshot is held in Staging.LatchStats and the difference between it and this one is what gets stored.
*/
SET XACT_ABORT ON
SET NOCOUNT ON
IF EXISTS(SELECT latch_class
        FROM @LatchStats
        EXCEPT
        SELECT LatchClass
        FROM dbo.LatchClass
        )
BEGIN
    INSERT INTO dbo.LatchClass(LatchClass)
    SELECT latch_class
    FROM @LatchStats
    EXCEPT
    SELECT LatchClass
    FROM dbo.LatchClass WITH(UPDLOCK,HOLDLOCK)
END

DECLARE @PreviousSnapshotDate DATETIME2(3)
SELECT @PreviousSnapshotDate = MAX(SnapshotDate)
FROM Staging.LatchStats
WHERE InstanceID = @InstanceID

/*  A snapshot older than the baseline (a file imported out of order) has nothing to add: its interval was covered
    by the snapshots since, and replacing the baseline with it would have the next snapshot diffed against older
    values - counting the activity in between twice.  The same snapshot imported again carries on, so a retry still
    rebuilds the hourly rollup, and the check below stops it being diffed twice.  */
IF @SnapshotDate < @PreviousSnapshotDate
BEGIN
    RETURN
END

BEGIN TRAN

/*  The interval is diffed where there's a previous snapshot (NULL fails the test), it's no more than 36 hours old,
    and this snapshot hasn't already been imported.  */
IF @PreviousSnapshotDate < @SnapshotDate
    AND DATEDIFF(mi,@PreviousSnapshotDate,@SnapshotDate) < 2160
    AND NOT EXISTS(SELECT 1 FROM dbo.LatchStatsCollection C WHERE C.InstanceID = @InstanceID AND C.SnapshotDate = @SnapshotDate)
BEGIN
    /*  The time covered is recorded whether or not any latch class was waited on in the interval.  An idle interval
        adds no rows to dbo.LatchStats, and dbo.LatchStats_Get divides by the time recorded here, so leaving it out
        would overstate the rates.  */
    INSERT INTO dbo.LatchStatsCollection(InstanceID,SnapshotDate,sample_ms_diff)
    VALUES(@InstanceID,@SnapshotDate,DATEDIFF(ms,@PreviousSnapshotDate,@SnapshotDate));

    /*	Only latch classes with waits are collected, so a class missing from the previous snapshot had a count of zero
	    then and its current values are all new.  This also holds after a restart or a DBCC SQLPERF('sys.dm_os_latch_stats',CLEAR),
	    as the reset happened after the previous snapshot.  */
    INSERT INTO dbo.LatchStats(InstanceID,SnapshotDate,LatchClassID,waiting_requests_count,wait_time_ms)
    SELECT @InstanceID,
           @SnapshotDate,
           LC.LatchClassID,
           L2.waiting_requests_count - ISNULL(L1.waiting_requests_count,0),
           L2.wait_time_ms - ISNULL(L1.wait_time_ms,0)
    FROM @LatchStats L2
    JOIN dbo.LatchClass LC ON LC.LatchClass = L2.latch_class
    LEFT JOIN Staging.LatchStats L1 ON L1.InstanceID = @InstanceID
                                   AND L1.latch_class = L2.latch_class
    /* A counter going backwards means a restart or a DBCC SQLPERF('sys.dm_os_latch_stats',CLEAR) between snapshots */
    WHERE L2.waiting_requests_count >= ISNULL(L1.waiting_requests_count,0)
    AND L2.wait_time_ms >= ISNULL(L1.wait_time_ms,0)
    AND (L2.waiting_requests_count > ISNULL(L1.waiting_requests_count,0) OR L2.wait_time_ms > ISNULL(L1.wait_time_ms,0));
END

DELETE Staging.LatchStats WHERE InstanceID = @InstanceID;

INSERT INTO Staging.LatchStats
(
    InstanceID,
    SnapshotDate,
    latch_class,
    waiting_requests_count,
    wait_time_ms,
    max_wait_time_ms
)
SELECT @InstanceID,
       @SnapshotDate,
       latch_class,
       waiting_requests_count,
       wait_time_ms,
       max_wait_time_ms
FROM @LatchStats;

COMMIT

/*  Update 60min aggregation.  An hour in the rollup is exactly the raw rows whose snapshot falls in it, so
    dbo.LatchStats_Get can read whole hours from here and the part hours at either end of a range from the raw rows.  */
BEGIN TRAN

DECLARE @Tomorrow DATETIME2(2) = CAST(DATEADD(d,1,GETUTCDATE()) AS DATE)
DECLARE @MaxDate DATETIME2(2)
/* From the coverage rollup - the latest hour may have no latch rows if nothing was waited on */
SELECT @MaxDate = ISNULL(MAX(SnapshotDate),'19000101')
FROM dbo.LatchStatsCollection_60MIN
WHERE InstanceID = @InstanceID

/*  Rebuilt from the first hour that still has raw rows, which is the latest hour in the rollup unless the raw rows
    for it have been purged - collection switched off for longer than the raw retention, then on again.  That hour
    can't be rebuilt, so it's kept as it is rather than deleted.  NULL where there are no raw rows since, which
    leaves the rollup alone (nothing compares >= NULL).  */
DECLARE @RebuildFrom DATETIME2(2)
SELECT @RebuildFrom = DG.DateGroup
FROM (  SELECT MIN(SnapshotDate) AS SnapshotDate
        FROM dbo.LatchStatsCollection
        WHERE InstanceID = @InstanceID
        AND SnapshotDate >= @MaxDate
    ) C
CROSS APPLY dbo.DateGroupingMins(C.SnapshotDate,60) DG

DELETE dbo.LatchStatsCollection_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate >= @RebuildFrom

DELETE dbo.LatchStats_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate >= @RebuildFrom

INSERT INTO dbo.LatchStatsCollection_60MIN
(
    InstanceID,
    SnapshotDate,
    sample_ms_diff
)
SELECT C.InstanceID,
    DG.DateGroup AS SnapshotDate,
    SUM(C.sample_ms_diff) AS sample_ms_diff
FROM dbo.LatchStatsCollection C
CROSS APPLY dbo.DateGroupingMins(C.SnapshotDate,60) DG
WHERE C.InstanceID = @InstanceID
AND C.SnapshotDate >= @RebuildFrom
AND C.SnapshotDate < @Tomorrow
GROUP BY C.InstanceID,
    DG.DateGroup
OPTION(OPTIMIZE FOR(@RebuildFrom='99991231'))

INSERT INTO dbo.LatchStats_60MIN
(
    InstanceID,
    SnapshotDate,
    LatchClassID,
    waiting_requests_count,
    wait_time_ms
)
SELECT L.InstanceID,
    DG.DateGroup AS SnapshotDate,
    L.LatchClassID,
    SUM(L.waiting_requests_count) AS waiting_requests_count,
    SUM(L.wait_time_ms) AS wait_time_ms
FROM dbo.LatchStats L
CROSS APPLY dbo.DateGroupingMins(L.SnapshotDate,60) DG
WHERE L.InstanceID = @InstanceID
AND L.SnapshotDate >= @RebuildFrom
AND L.SnapshotDate < @Tomorrow
GROUP BY L.InstanceID,
    DG.DateGroup,
    L.LatchClassID
OPTION(OPTIMIZE FOR(@RebuildFrom='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'LatchStats',
                             @SnapshotDate = @SnapshotDate
