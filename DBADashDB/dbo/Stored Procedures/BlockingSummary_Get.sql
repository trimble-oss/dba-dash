CREATE PROC BlockingSummary_Get(@BlockingSnapshotID INT)
AS
SELECT SS.BlockingSnapshotID,
       SS.InstanceID,
       SS.SnapshotDateUTC,
       SS.BlockedSessionCount,
       SS.BlockedWaitTime,
       I.ConnectionID
FROM dbo.BlockingSnapshotSummary SS
JOIN dbo.Instances I ON I.InstanceID = SS.InstanceID
WHERE SS.BlockingSnapshotID = @BlockingSnapshotID