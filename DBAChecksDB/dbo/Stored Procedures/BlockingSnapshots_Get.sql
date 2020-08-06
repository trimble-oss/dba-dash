CREATE PROC dbo.BlockingSnapshots_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@Top INT=500
)
AS
SELECT TOP(@Top) BlockingSnapshotID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime 
FROM dbo.BlockingSnapshotSummary
WHERE InstanceID = @InstanceID
AND SnapshotDateUTC>=@FromDate
AND SnapshotDateUTC<@ToDate
ORDER BY BlockedWaitTime DESC