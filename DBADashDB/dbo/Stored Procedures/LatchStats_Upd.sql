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

BEGIN TRAN

DECLARE @PreviousSnapshotDate DATETIME2(3)
SELECT @PreviousSnapshotDate = MAX(SnapshotDate)
FROM Staging.LatchStats
WHERE InstanceID = @InstanceID

/*	Only latch classes with waits are collected, so a class missing from the previous snapshot had a count of zero
	then and its current values are all new.  This also holds after a restart or a DBCC SQLPERF('sys.dm_os_latch_stats',CLEAR),
	as the reset happened after the previous snapshot.  */
INSERT INTO dbo.LatchStats(InstanceID,SnapshotDate,LatchClassID,waiting_requests_count,wait_time_ms,sample_ms_diff)
SELECT @InstanceID,
       @SnapshotDate,
       LC.LatchClassID,
       L2.waiting_requests_count - ISNULL(L1.waiting_requests_count,0),
       L2.wait_time_ms - ISNULL(L1.wait_time_ms,0),
       DATEDIFF(ms,@PreviousSnapshotDate,@SnapshotDate)
FROM @LatchStats L2
JOIN dbo.LatchClass LC ON LC.LatchClass = L2.latch_class
LEFT JOIN Staging.LatchStats L1 ON L1.InstanceID = @InstanceID
                               AND L1.latch_class = L2.latch_class
/* A counter going backwards means a restart or a DBCC SQLPERF('sys.dm_os_latch_stats',CLEAR) between snapshots */
WHERE L2.waiting_requests_count >= ISNULL(L1.waiting_requests_count,0)
AND L2.wait_time_ms >= ISNULL(L1.wait_time_ms,0)
AND (L2.waiting_requests_count > ISNULL(L1.waiting_requests_count,0) OR L2.wait_time_ms > ISNULL(L1.wait_time_ms,0))
/* NULL where there's no previous snapshot, so nothing is inserted */
AND @PreviousSnapshotDate < @SnapshotDate
AND DATEDIFF(mi,@PreviousSnapshotDate,@SnapshotDate) < 2160
AND NOT EXISTS(SELECT 1 FROM dbo.LatchStats L WHERE L.InstanceID = @InstanceID AND L.SnapshotDate = @SnapshotDate);

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

/* Update 60min aggregation */
BEGIN TRAN

DECLARE @Tomorrow DATETIME2(2) = CAST(DATEADD(d,1,GETUTCDATE()) AS DATE)
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = ISNULL(MAX(SnapshotDate),'19000101')
FROM dbo.LatchStats_60MIN
WHERE InstanceID = @InstanceID

DELETE dbo.LatchStats_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate = @MaxDate

/*	sample_ms_diff is the time the hour's snapshots cover, which is what dbo.LatchStats_Get divides by.  Every row of a
	snapshot has the same sample_ms_diff, so it's summed once per snapshot rather than per latch class - a class
	that wasn't waited on in every snapshot would otherwise understate it.  */
;WITH Coverage AS (
    SELECT DG.DateGroup,
        SUM(S.sample_ms_diff) AS sample_ms_diff
    FROM (  SELECT SnapshotDate,
                MAX(sample_ms_diff) AS sample_ms_diff
            FROM dbo.LatchStats
            WHERE InstanceID = @InstanceID
            AND SnapshotDate >= @MaxDate
            AND SnapshotDate < @Tomorrow
            AND sample_ms_diff < 86400000
            GROUP BY SnapshotDate
        ) S
    CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate,60) DG
    GROUP BY DG.DateGroup
)
INSERT INTO dbo.LatchStats_60MIN
(
    InstanceID,
    SnapshotDate,
    LatchClassID,
    waiting_requests_count,
    wait_time_ms,
    sample_ms_diff
)
SELECT L.InstanceID,
    DG.DateGroup AS SnapshotDate,
    L.LatchClassID,
    SUM(L.waiting_requests_count) AS waiting_requests_count,
    SUM(L.wait_time_ms) AS wait_time_ms,
    C.sample_ms_diff
FROM dbo.LatchStats L
CROSS APPLY dbo.DateGroupingMins(L.SnapshotDate,60) DG
JOIN Coverage C ON C.DateGroup = DG.DateGroup
WHERE L.InstanceID = @InstanceID
AND L.SnapshotDate >= @MaxDate
AND L.SnapshotDate < @Tomorrow
AND L.sample_ms_diff < 86400000
GROUP BY L.InstanceID,
    DG.DateGroup,
    L.LatchClassID,
    C.sample_ms_diff
OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'LatchStats',
                             @SnapshotDate = @SnapshotDate
