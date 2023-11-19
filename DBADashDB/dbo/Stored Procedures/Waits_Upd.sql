﻿CREATE PROC dbo.Waits_Upd(
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
AND W2.signal_wait_time_ms>=W2.signal_wait_time_ms
AND (W2.waiting_tasks_count>W1.waiting_tasks_count OR W2.wait_time_ms>w1.wait_time_ms OR W2.signal_wait_time_ms>W2.signal_wait_time_ms)
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

DELETE dbo.Waits_60MIN
WHERE InstanceID=@InstanceID
AND SnapshotDate=@MaxDate

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
AND w.SnapshotDate >= @MaxDate
AND w.SnapshotDate < @Tomorrow
AND w.sample_ms_diff < 86400000
GROUP BY w.InstanceID,
	DG.DateGroup,
	w.WaitTypeID
 OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

COMMIT
/* **************** */

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'Waits', 
                             @SnapshotDate = @SnapshotDate