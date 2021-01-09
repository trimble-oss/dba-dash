CREATE PROC [Report].[DriveUsedGrowth](@Days INT=30,@InstanceIDs VARCHAR(MAX) = NULL)
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
SELECT I.Instance,
		D.DriveID,
       D.InstanceID,
       D.Name,
       D.Capacity/POWER(1024.0,3) AS TotalGB,
       D.FreeSpace/POWER(1024.0,3) AS FreeGB,
       D.UsedSpace/POWER(1024.0,3) AS UsedGB,
       D.Label,
       D.IsActive,
	   DATEDIFF(d,H.SnapshotDate,SSD.SnapshotDate) Days,
	   (D.UsedSpace-H.UsedSpace)/POWER(1024.0,3) AS GrowthGB,
	   ((D.UsedSpace-H.UsedSpace*1.0)/H.UsedSpace) PctGrowth
FROM dbo.Drives D 
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN dbo.CollectionDates SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='Drives'
CROSS APPLY(SELECT TOP(1) DSS.Capacity ,DSS.UsedSpace,DSS.SnapshotDate
			FROM dbo.DriveSnapshot DSS 
			WHERE DSS.DriveID = D.DriveID
			AND DSS.SnapshotDate>=DATEADD(d,-@Days,GETUTCDATE())
			ORDER BY DSS.SnapshotDate) H
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = D.InstanceID)
ORDER BY PctGrowth DESC