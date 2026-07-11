CREATE PROC [dbo].[CollectionDatesHeartbeat_Upd](
	@ConnectionID NVARCHAR(128),
	@SnapshotDate DATETIME2(2),
	@Heartbeat dbo.CollectionDatesHeartbeat READONLY
)
AS
/*
	Records that a change-detection collection (e.g. Jobs) *ran* even though no data was written because
	nothing changed.  Advances only HeartbeatDate - SnapshotDate keeps tracking the last time data was
	actually collected.  The CollectionDatesStatus view bases its status on the more recent of the two, so
	the collection is no longer reported as stale/overdue just because its data is unchanged, while a
	genuinely stopped collection (no heartbeat) is still flagged within its normal threshold.

	A heartbeat is only ever emitted after a successful collection has already created the CollectionDates
	row, so there's deliberately no INSERT here - a heartbeat with no prior collection would be meaningless
	(and we don't want to fabricate a SnapshotDate for a collection that never ran).
*/
SET XACT_ABORT ON

DECLARE @InstanceID INT = (SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = @ConnectionID)
IF @InstanceID IS NULL RETURN

UPDATE CD
	SET HeartbeatDate = @SnapshotDate
FROM dbo.CollectionDates CD
JOIN @Heartbeat H ON H.Reference = CD.Reference
WHERE CD.InstanceID = @InstanceID
AND (CD.HeartbeatDate IS NULL OR CD.HeartbeatDate < @SnapshotDate)
