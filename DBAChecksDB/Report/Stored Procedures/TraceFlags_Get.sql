CREATE PROC [Report].[TraceFlags_Get](@InstanceIDs VARCHAR(MAX)=NULL)
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
		tf.TraceFlag,
		tf.ValidFrom
FROM dbo.Instances I 
LEFT JOIN dbo.TraceFlags tf ON tf.InstanceID=I.InstanceID
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.EditionID<> 1674378470 --exclude azure
AND I.IsActive=1