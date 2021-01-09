CREATE PROC [Report].[DriveExpansion](@Days INT=30,@InstanceIDs VARCHAR(MAX)=NULL)
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
	   DATEDIFF(d,H.SnapshotDate,SSD.SnapshotDate) AS Days,
	   (D.Capacity-H.Capacity)/POWER(1024.0,3) AS ChangeInSize
FROM dbo.Drives D 
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN dbo.CollectionDates SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='Drives'
CROSS APPLY(SELECT TOP(1) DSS.Capacity,DSS.SnapshotDate
			FROM dbo.DriveSnapshot DSS 
			WHERE DSS.DriveID = D.DriveID
			AND DSS.SnapshotDate>=DATEADD(d,-@Days,GETUTCDATE())
			AND D.Capacity>DSS.Capacity
			ORDER BY DSS.SnapshotDate) H
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = D.InstanceID)