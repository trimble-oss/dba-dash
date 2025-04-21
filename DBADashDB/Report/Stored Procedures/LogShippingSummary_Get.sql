CREATE PROC Report.LogShippingSummary_Get(@InstanceIDs VARCHAR(MAX)=NULL)
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
END

SELECT InstanceID,
       InstanceDisplayName AS Instance,
       Status,
       StatusDescription,
       LogShippedDBCount,
       WarningCount,
       CriticalCount,
       MaxTotalTimeBehind,
       MaxLatencyOfLast,
       MaxTimeSinceLast AS TimeSinceLast,
       SnapshotAge,
       MinDateOfLastBackupRestored,
       MinLastRestoreCompleted
FROM dbo.LogShippingStatusSummary LSS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LSS.InstanceID)