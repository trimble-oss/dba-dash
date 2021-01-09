
CREATE PROC [Report].[AlertsConfig_Get](
	@InstanceIDs VARCHAR(MAX) = NULL
)
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

SELECT I.InstanceID,
		I.Instance, 
		A.name,
		CASE WHEN A.message_id<> 0 THEN 'MessageID:' + CAST(A.message_id AS VARCHAR(50)) ELSE 'Severity:' + CAST(A.severity AS VARCHAR(MAX)) END AS Alert,
		A.enabled,
		A.has_notification
FROM dbo.Instances I
LEFT JOIN dbo.Alerts A ON A.InstanceID = I.InstanceID AND NOT (A.message_id=0 AND A.severity=0)
WHERE I.EditionID<> 1674378470 --exclude azure
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1