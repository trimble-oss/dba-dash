CREATE PROC Report.BlockingSummary(@FromDate DATETIME=NULL,@ToDate DATETIME=NULL,@InstanceIDs VARCHAR(MAX)=NULL)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;

SELECT BSS.BlockingSnapshotID,BSS.InstanceID, I.Instance,BSS.SnapshotDateUTC,BSS.BlockedSessionCount,BSS.BlockedWaitTime
FROM dbo.BlockingSnapshotSummary BSS
JOIN dbo.Instances I ON I.InstanceID = BSS.InstanceID
WHERE I.IsActive=1
AND BSS.SnapshotDateUTC>=ISNULL(@FromDate,DATEADD(mi,-60,GETUTCDATE()))
AND BSS.SnapshotDateUTC < ISNULL(@ToDate,GETUTCDATE())
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)