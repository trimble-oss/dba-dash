CREATE PROC [Report].[Drives_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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

SELECT DS.InstanceID,
       DS.DriveID,
       DS.Instance,
	   DS.ConnectionID,
       DS.Name,
       DS.Label,
       DS.TotalGB,
       DS.UsedGB,
       DS.FreeGB,
       DS.PctFreeSpace,
       DS.SnapshotAgeMins,
       DS.DriveWarningThreshold,
       DS.DriveCriticalThreshold,
       DS.DriveCheckType,
       DS.Status,
       DS.StatusDescription,
       DS.DriveCheckConfiguredLevel 
FROM dbo.DriveStatus DS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = DS.InstanceID)
AND (Status<=@FilterLevel)
ORDER BY Status,PctFreeSpace
OPTION(RECOMPILE)