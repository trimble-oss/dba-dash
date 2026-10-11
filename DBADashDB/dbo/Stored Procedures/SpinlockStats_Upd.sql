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

/*	Only spinlocks with collisions or backoffs are collected, so a spinlock missing from the previous snapshot had
	zero counts then and its current values are all new.  This also holds after a restart, as the reset happened
	after the previous snapshot.  */
INSERT INTO dbo.SpinlockStats(InstanceID,SnapshotDate,SpinlockID,collisions,spins,sleep_time,backoffs,sample_ms_diff)
SELECT @InstanceID,
       @SnapshotDate,
       SL.SpinlockID,
       S2.collisions - ISNULL(S1.collisions,0),
       S2.spins - ISNULL(S1.spins,0),
       S2.sleep_time - ISNULL(S1.sleep_time,0),
       S2.backoffs - ISNULL(S1.backoffs,0),
       DATEDIFF(ms,@PreviousSnapshotDate,@SnapshotDate)
FROM @SpinlockStats S2
JOIN dbo.Spinlock SL ON SL.SpinlockName = S2.spinlock_name
LEFT JOIN Staging.SpinlockStats S1 ON S1.InstanceID = @InstanceID
                                  AND S1.spinlock_name = S2.spinlock_name
/* A counter going backwards means a restart or a DBCC SQLPERF('sys.dm_os_spinlock_stats',CLEAR) between snapshots */
WHERE S2.collisions >= ISNULL(S1.collisions,0)
AND S2.spins >= ISNULL(S1.spins,0)
AND S2.sleep_time >= ISNULL(S1.sleep_time,0)
AND S2.backoffs >= ISNULL(S1.backoffs,0)
AND (S2.collisions > ISNULL(S1.collisions,0) OR S2.spins > ISNULL(S1.spins,0) OR S2.backoffs > ISNULL(S1.backoffs,0))
/* NULL where there's no previous snapshot, so nothing is inserted */
AND @PreviousSnapshotDate < @SnapshotDate
AND DATEDIFF(mi,@PreviousSnapshotDate,@SnapshotDate) < 2160
AND NOT EXISTS(SELECT 1 FROM dbo.SpinlockStats S WHERE S.InstanceID = @InstanceID AND S.SnapshotDate = @SnapshotDate);

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

/* Update 60min aggregation */
BEGIN TRAN

DECLARE @Tomorrow DATETIME2(2) = CAST(DATEADD(d,1,GETUTCDATE()) AS DATE)
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = ISNULL(MAX(SnapshotDate),'19000101')
FROM dbo.SpinlockStats_60MIN
WHERE InstanceID = @InstanceID

DELETE dbo.SpinlockStats_60MIN
WHERE InstanceID = @InstanceID
AND SnapshotDate = @MaxDate

/*	sample_ms_diff is the time the hour's snapshots cover, which is what dbo.SpinlockStats_Get divides by.  Every row of a
	snapshot has the same sample_ms_diff, so it's summed once per snapshot rather than per spinlock - a spinlock
	without activity in every snapshot would otherwise understate it.  */
;WITH Coverage AS (
    SELECT DG.DateGroup,
        SUM(SN.sample_ms_diff) AS sample_ms_diff
    FROM (  SELECT SnapshotDate,
                MAX(sample_ms_diff) AS sample_ms_diff
            FROM dbo.SpinlockStats
            WHERE InstanceID = @InstanceID
            AND SnapshotDate >= @MaxDate
            AND SnapshotDate < @Tomorrow
            AND sample_ms_diff < 86400000
            GROUP BY SnapshotDate
        ) SN
    CROSS APPLY dbo.DateGroupingMins(SN.SnapshotDate,60) DG
    GROUP BY DG.DateGroup
)
INSERT INTO dbo.SpinlockStats_60MIN
(
    InstanceID,
    SnapshotDate,
    SpinlockID,
    collisions,
    spins,
    sleep_time,
    backoffs,
    sample_ms_diff
)
SELECT S.InstanceID,
    DG.DateGroup AS SnapshotDate,
    S.SpinlockID,
    SUM(S.collisions) AS collisions,
    SUM(S.spins) AS spins,
    SUM(S.sleep_time) AS sleep_time,
    SUM(S.backoffs) AS backoffs,
    C.sample_ms_diff
FROM dbo.SpinlockStats S
CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate,60) DG
JOIN Coverage C ON C.DateGroup = DG.DateGroup
WHERE S.InstanceID = @InstanceID
AND S.SnapshotDate >= @MaxDate
AND S.SnapshotDate < @Tomorrow
AND S.sample_ms_diff < 86400000
GROUP BY S.InstanceID,
    DG.DateGroup,
    S.SpinlockID,
    C.sample_ms_diff
OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'SpinlockStats',
                             @SnapshotDate = @SnapshotDate
