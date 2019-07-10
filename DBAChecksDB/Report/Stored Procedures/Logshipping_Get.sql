CREATE PROC [Report].[Logshipping_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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
       Instance,
       name,
       restore_date,
       backup_start_date,
       TimeSinceLast,
       LatencyOfLast,
       TotalTimeBehind,
       SnapshotAge,
       LogRestoreSnapshotDate,
       Status,
       StatusDescription 
FROM dbo.LogShippingStatus LSS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LSS.InstanceID)
AND (Status<=@FilterLevel)
ORDER BY Status,TotalTimeBehind DESC
OPTION(RECOMPILE)