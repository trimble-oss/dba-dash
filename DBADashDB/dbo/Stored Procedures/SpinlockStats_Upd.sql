CREATE PROC dbo.SpinlockStats_Upd(
    @SpinlockStats dbo.SpinlockStats READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(2)
)
AS
/*
    sys.dm_os_spinlock_stats is cumulative since the instance started (or the stats were cleared).
    The previous snapshot is held in Staging.SpinlockStats and the difference between it and this one is what gets stored.
*/
SET XACT_ABORT ON
SET NOCOUNT ON
IF EXISTS(SELECT spinlock_name
        FROM @SpinlockStats
        EXCEPT
        SELECT SpinlockName
        FROM dbo.Spinlock
        )
BEGIN
    INSERT INTO dbo.Spinlock(SpinlockName)
    SELECT spinlock_name
    FROM @SpinlockStats
    EXCEPT
    SELECT SpinlockName
    FROM dbo.Spinlock WITH(UPDLOCK,HOLDLOCK)
END

BEGIN TRAN

DECLARE @PreviousSnapshotDate DATETIME2(3)
SELECT @PreviousSnapshotDate = MAX(SnapshotDate)
FROM Staging.SpinlockStats
WHERE InstanceID = @InstanceID

/*  The interval is diffed where there's a previous snapshot (NULL fails the test), it's no more than 36 hours old,
    and this snapshot hasn't already been imported.  */
IF @PreviousSnapshotDate < @SnapshotDate
    AND DATEDIFF(mi,@PreviousSnapshotDate,@SnapshotDate) < 2160
    AND NOT EXISTS(SELECT 1 FROM dbo.SpinlockStatsCollection C WHERE C.InstanceID = @InstanceID AND C.SnapshotDate = @SnapshotDate)
BEGIN
    /*  The time covered is recorded whether or not any spinlock had activity in the interval.  An idle interval adds
        no rows to dbo.SpinlockStats, and dbo.SpinlockStats_Get divides by the time recorded here, so leaving it out
        would overstate the rates.  */
    INSERT INTO dbo.SpinlockStatsCollection(InstanceID,SnapshotDate,sample_ms_diff)
    VALUES(@InstanceID,@SnapshotDate,DATEDIFF(ms,@PreviousSnapshotDate,@SnapshotDate));

    /*	Only spinlocks with collisions or backoffs are collected, so a spinlock missing from the previous snapshot had
	    zero counts then and its current values are all new.  This also holds after a restart, as the reset happened
	    after the previous snapshot.  */
    INSERT INTO dbo.SpinlockStats(InstanceID,SnapshotDate,SpinlockID,collisions,spins,sleep_time,backoffs)
    SELECT @InstanceID,
           @SnapshotDate,
           SL.SpinlockID,
           S2.collisions - ISNULL(S1.collisions,0),
           S2.spins - ISNULL(S1.spins,0),
           S2.sleep_time - ISNULL(S1.sleep_time,0),
           S2.backoffs - ISNULL(S1.backoffs,0)
    FROM @SpinlockStats S2
    JOIN dbo.Spinlock SL ON SL.SpinlockName = S2.spinlock_name
    LEFT JOIN Staging.SpinlockStats S1 ON S1.InstanceID = @InstanceID
                                      AND S1.spinlock_name = S2.spinlock_name
    /* A counter going backwards means a restart or a DBCC SQLPERF('sys.dm_os_spinlock_stats',CLEAR) between snapshots */
    WHERE S2.collisions >= ISNULL(S1.collisions,0)
    AND S2.spins >= ISNULL(S1.spins,0)
    AND S2.sleep_time >= ISNULL(S1.sleep_time,0)
    AND S2.backoffs >= ISNULL(S1.backoffs,0)
    AND (S2.collisions > ISNULL(S1.collisions,0) OR S2.spins > ISNULL(S1.spins,0) OR S2.backoffs > ISNULL(S1.backoffs,0));
END

DELETE Staging.SpinlockStats WHERE InstanceID = @InstanceID;

INSERT INTO Staging.SpinlockStats
(
    InstanceID,
    SnapshotDate,
    spinlock_name,
    collisions,
    spins,
    sleep_time,
    backoffs
)
SELECT @InstanceID,
       @SnapshotDate,
       spinlock_name,
       collisions,
       spins,
       sleep_time,
       backoffs
FROM @SpinlockStats;

COMMIT

/*  Update 60min aggregation.  An hour in the rollup is exactly the raw rows whose snapshot falls in it, so
    dbo.SpinlockStats_Get can read whole hours from here and the part hours at either end of a range from the raw rows.  */
BEGIN TRAN

DECLARE @Tomorrow DATETIME2(2) = CAST(DATEADD(d,1,GETUTCDATE()) AS DATE)
DECLARE @MaxDate DATETIME2(2)
/* From the coverage rollup - the latest hour may have no spinlock rows if there was no activity */
SELECT @MaxDate = ISNULL(MAX(SnapshotDate),'19000101')
FROM dbo.SpinlockStatsCollection_60MIN
WHERE InstanceID = @InstanceID

DELETE dbo.SpinlockStatsCollection_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate >= @MaxDate

DELETE dbo.SpinlockStats_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate >= @MaxDate

INSERT INTO dbo.SpinlockStatsCollection_60MIN
(
    InstanceID,
    SnapshotDate,
    sample_ms_diff
)
SELECT C.InstanceID,
    DG.DateGroup AS SnapshotDate,
    SUM(C.sample_ms_diff) AS sample_ms_diff
FROM dbo.SpinlockStatsCollection C
CROSS APPLY dbo.DateGroupingMins(C.SnapshotDate,60) DG
WHERE C.InstanceID = @InstanceID
AND C.SnapshotDate >= @MaxDate
AND C.SnapshotDate < @Tomorrow
GROUP BY C.InstanceID,
    DG.DateGroup
OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

INSERT INTO dbo.SpinlockStats_60MIN
(
    InstanceID,
    SnapshotDate,
    SpinlockID,
    collisions,
    spins,
    sleep_time,
    backoffs
)
SELECT S.InstanceID,
    DG.DateGroup AS SnapshotDate,
    S.SpinlockID,
    SUM(S.collisions) AS collisions,
    SUM(S.spins) AS spins,
    SUM(S.sleep_time) AS sleep_time,
    SUM(S.backoffs) AS backoffs
FROM dbo.SpinlockStats S
CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate,60) DG
WHERE S.InstanceID = @InstanceID
AND S.SnapshotDate >= @MaxDate
AND S.SnapshotDate < @Tomorrow
GROUP BY S.InstanceID,
    DG.DateGroup,
    S.SpinlockID
OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'SpinlockStats',
                             @SnapshotDate = @SnapshotDate
