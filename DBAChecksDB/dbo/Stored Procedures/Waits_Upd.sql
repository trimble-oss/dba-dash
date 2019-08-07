CREATE PROC Waits_Upd(@Waits dbo.Waits READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
INSERT INTO dbo.WaitType(WaitType)
SELECT wait_type 
FROM @Waits
EXCEPT 
SELECT WaitType 
FROM dbo.WaitType

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
WHERE W2.waiting_tasks_count>W1.waiting_tasks_count
AND W2.wait_time_ms>=w1.wait_time_ms
AND W2.signal_wait_time_ms>=W2.signal_wait_time_ms
AND W1.SnapshotDate<@SnapshotDate
AND DATEDIFF(mi,W1.SnapshotDate,@SnapshotDate)<2160;

DELETE Staging.Waits WHERE InstanceID=@InstanceID;

INSERT INTO staging.Waits
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

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'Waits', 
                             @SnapshotDate = @SnapshotDate