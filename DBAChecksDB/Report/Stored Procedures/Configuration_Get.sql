CREATE PROC [Report].[Configuration_Get](@InstanceIDs VARCHAR(MAX)=NULL)
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
		I.ConnectionID,
		SCO.configuration_id,
		SCO.name,
		SCO.default_value,
		SC.value,
		CASE WHEN SC.value=SCO.default_value THEN 1 ELSE 0 END AS IsDefault
FROM dbo.SysConfig SC
JOIN dbo.Instances I ON SC.InstanceID=I.InstanceID
JOIN dbo.SysConfigOptions SCO ON SC.configuration_id = SCO.configuration_id
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.EditionID<> 1674378470 --exclude azure
AND I.IsActive=1