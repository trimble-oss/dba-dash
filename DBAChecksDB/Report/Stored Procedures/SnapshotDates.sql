CREATE PROC [Report].[SnapshotDates](@InstanceIDs VARCHAR(MAX)=NULL)
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
	i.ConnectionID,
	SSD.Reference,
	I.AgentHostName,
	SSD.SnapshotDate,
	DATEDIFF(mi,SSD.SnapshotDate,GETUTCDATE()) AS SnapshotAge 
FROM dbo.Instances I 
JOIN dbo.CollectionDates SSD ON SSD.InstanceID = I.InstanceID
WHERE I.IsActive=1
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)