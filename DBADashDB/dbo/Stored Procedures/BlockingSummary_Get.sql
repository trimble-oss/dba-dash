CREATE PROC dbo.BlockingSummary_Get(
    /* Provide @BlockingSnapshotID or @InstanceID + @SnapshotDate */
    @BlockingSnapshotID INT=NULL,
    @InstanceID INT=NULL,
    @SnapshotDate DATETIME2(7)=NULL
 )
AS
IF @BlockingSnapshotID IS NULL
BEGIN
    SELECT @BlockingSnapshotID = BlockingSnapshotID 
    FROM dbo.BlockingSnapshotSummary
    WHERE InstanceID = @InstanceID 
    AND SnapshotDateUTC = @SnapshotDate;
END

SELECT SS.BlockingSnapshotID,
       SS.InstanceID,
       SS.SnapshotDateUTC,
       SS.BlockedSessionCount,
       SS.BlockedWaitTime,
       I.ConnectionID
FROM dbo.BlockingSnapshotSummary SS
JOIN dbo.Instances I ON I.InstanceID = SS.InstanceID
WHERE SS.BlockingSnapshotID = @BlockingSnapshotID