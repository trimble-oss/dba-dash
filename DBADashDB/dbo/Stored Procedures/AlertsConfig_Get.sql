CREATE PROC dbo.AlertsConfig_Get(
	@InstanceIDs VARCHAR(MAX) = NULL,
	@ShowHidden BIT=1
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
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END

SELECT I.InstanceID,
		I.Instance, 
		I.InstanceDisplayName,
		A.name,
		CASE WHEN A.message_id<> 0 THEN 'MessageID:' + CAST(A.message_id AS VARCHAR(50)) WHEN A.severity<>0 THEN 'Severity:' + CAST(A.severity AS VARCHAR(MAX)) ELSE A.name END AS Alert,
		A.enabled,
		A.has_notification
FROM dbo.Instances I
LEFT JOIN dbo.Alerts A ON A.InstanceID = I.InstanceID
WHERE I.EditionID<> 1674378470 --exclude azure DB & Azure managed instance
AND EXISTS	(	
			SELECT 1 
			FROM @Instances t 
			WHERE I.InstanceID = t.InstanceID
			)
AND I.IsActive=1
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY I.Instance,Alert