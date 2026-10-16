CREATE PROC dbo.Waits_Upd(
    @Waits dbo.Waits READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON
IF EXISTS(SELECT wait_type 
        FROM @Waits
        EXCEPT 
        SELECT WaitType 
        FROM dbo.WaitType
        )
BEGIN
    INSERT INTO dbo.WaitType(WaitType)
    SELECT wait_type 
    FROM @Waits
    EXCEPT 
    SELECT WaitType 
    FROM dbo.WaitType WITH(UPDLOCK,HOLDLOCK)
END

DECLARE @PreviousSnapshotDate DATETIME2(3)
SELECT @PreviousSnapshotDate = MAX(SnapshotDate)
FROM Staging.Waits
WHERE InstanceID = @InstanceID

/*  A snapshot older than the baseline (a file imported out of order) has nothing to add: its interval was covered
    by the snapshots since, and replacing the baseline with it would have the next snapshot diffed against older
    values - counting the activity in between twice.  The same snapshot imported again carries on, so a retry still
    rebuilds the hourly rollup, and the W1.SnapshotDate < @SnapshotDate test below stops it being diffed twice.  */
IF @SnapshotDate < @PreviousSnapshotDate
BEGIN
    RETURN
END

BEGIN TRAN

INSERT INTO dbo.Waits(InstanceID,SnapshotDate,WaitTypeID,waiting_tasks_count,wait_time_ms,signal_wait_time_ms,sample_ms_diff)
SELECT @InstanceID,
       @SnapshotDate,
       WT.WaitTypeID,
       W2.waiting_tasks_count - W1.waiting_tasks_count,
       W2.wait_time_ms - W1.wait_time_ms,
	   W2.signal_wait_time_ms-W1.signal_wait_time_ms,
	   DATEDIFF(ms,W1.SnapshotDate,@SnapshotDate)
FROM @Waits W2
    JOIN dbo.WaitType WT ON WT.WaitType = W2.wait_type
    JOIN Staging.Waits W1 ON  W1.InstanceID = @InstanceID
                          AND W1.wait_type = W2.wait_type
WHERE W2.waiting_tasks_count>=W1.waiting_tasks_count
AND W2.wait_time_ms>=w1.wait_time_ms
AND W2.signal_wait_time_ms>=W1.signal_wait_time_ms
AND (W2.waiting_tasks_count>W1.waiting_tasks_count OR W2.wait_time_ms>w1.wait_time_ms OR W2.signal_wait_time_ms>W1.signal_wait_time_ms)
AND W1.SnapshotDate<@SnapshotDate
AND DATEDIFF(mi,W1.SnapshotDate,@SnapshotDate)<2160
AND NOT EXISTS(SELECT 1 FROM dbo.Waits w WHERE w.InstanceID = @InstanceID AND w.SnapshotDate = @SnapshotDate);

DELETE Staging.Waits WHERE InstanceID=@InstanceID;

INSERT INTO Staging.Waits
(
    InstanceID,
	SnapshotDate,
    wait_type,
    waiting_tasks_count,
    wait_time_ms,
    signal_wait_time_ms
)
SELECT @InstanceID,
	   @SnapshotDate,
       wait_type,
       waiting_tasks_count,
       wait_time_ms,
       signal_wait_time_ms
FROM @Waits;

COMMIT

/* Update 60min aggregation */
BEGIN TRAN

DECLARE @Tomorrow DATETIME2(2) = CAST(DATEADD(d,1,GETUTCDATE()) AS DATE)
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = ISNULL(MAX(SnapshotDate),'19000101') 
FROM dbo.Waits_60MIN 
WHERE InstanceID = @InstanceID

/*  Rebuilt from the first hour that still has raw rows, which is the latest hour in the rollup unless the raw rows
    for it have been purged - collection stopped for longer than the raw retention, then started again.  That hour
    can't be rebuilt, so it's kept as it is rather than deleted.  NULL where there are no raw rows since, which
    leaves the rollup alone (nothing compares >= NULL).  */
DECLARE @RebuildFrom DATETIME2(2)
SELECT @RebuildFrom = DG.DateGroup
FROM (  SELECT MIN(SnapshotDate) AS SnapshotDate
        FROM dbo.Waits
        WHERE InstanceID = @InstanceID
        AND SnapshotDate >= @MaxDate
    ) W
CROSS APPLY dbo.DateGroupingMins(W.SnapshotDate,60) DG

DELETE dbo.Waits_60MIN
WHERE InstanceID=@InstanceID
AND SnapshotDate>=@RebuildFrom

INSERT INTO dbo.Waits_60MIN
(
    InstanceID,
    SnapshotDate,
    WaitTypeID,
    waiting_tasks_count,
    wait_time_ms,
    signal_wait_time_ms,
    sample_ms_diff
)
SELECT w.InstanceID,
	DG.DateGroup AS SnapshotDate,
	w.WaitTypeID,
	SUM(w.waiting_tasks_count) waiting_tasks_count,
	SUM(w.wait_time_ms) wait_time_ms,
	SUM(w.signal_wait_time_ms) signal_wait_ms,
	MAX(SUM(w.sample_ms_diff)) OVER(PARTITION BY w.InstanceID,DG.DateGroup) sample_ms_diff
FROM dbo.Waits w
CROSS APPLY dbo.DateGroupingMins(SnapshotDate,60) DG
WHERE w.InstanceID=@InstanceID
AND w.SnapshotDate >= @RebuildFrom
AND w.SnapshotDate < @Tomorrow
AND w.sample_ms_diff < 86400000
GROUP BY w.InstanceID,
	DG.DateGroup,
	w.WaitTypeID
 OPTION(OPTIMIZE FOR(@RebuildFrom='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'Waits', 
                             @SnapshotDate = @SnapshotDate