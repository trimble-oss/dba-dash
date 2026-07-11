CREATE PROC CollectionDates_Upd(@InstanceID INT,@Reference VARCHAR(100),@SnapshotDate DATETIME2(2))
AS
-- A real collection is also a heartbeat, so advance HeartbeatDate alongside SnapshotDate (never move it
-- backwards).  This keeps HeartbeatDate >= SnapshotDate for every collection - only change-detection
-- collections (see CollectionDatesHeartbeat_Upd) ever advance the heartbeat without the snapshot.
UPDATE dbo.CollectionDates
SET SnapshotDate=@SnapshotDate,
	HeartbeatDate = CASE WHEN HeartbeatDate IS NULL OR HeartbeatDate < @SnapshotDate THEN @SnapshotDate ELSE HeartbeatDate END
WHERE InstanceID=@InstanceID
AND Reference = @Reference
IF @@ROWCOUNT=0
BEGIN
	INSERT INTO dbo.CollectionDates
	(
		InstanceID,
		Reference,
		SnapshotDate,
		HeartbeatDate
	)
	VALUES
	(   @InstanceID,
		@Reference,
		@SnapshotDate,
		@SnapshotDate)
END