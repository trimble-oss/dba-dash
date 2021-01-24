CREATE PROC [Report].[OSLoadedModules_Get](@InstanceIDs VARCHAR(MAX)=NULL,@Status INT=3)
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

SELECT I.Instance,M.name,M.description,M.company ,CD.SnapshotDate,M.Status
FROM dbo.Instances I 
JOIN dbo.OSLoadedModules M ON M.InstanceID = I.InstanceID
LEFT JOIN dbo.CollectionDates CD ON CD.InstanceID = I.InstanceID AND CD.Reference = 'OSLoadedModules'
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
AND M.Status <= @Status
ORDER BY M.Status