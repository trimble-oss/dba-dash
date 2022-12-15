CREATE PROC dbo.DBConfigurationHistory_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ConfiguredOnly BIT=0,
	@DatabaseID INT=NULL,
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
END;
SELECT	I.InstanceGroupName,
		D.name AS DB, 
		H.configuration_id, 
		CO.name,H.value,
		H.new_value,
		H.value_for_secondary,
		H.new_value_for_secondary,
		H.ValidFrom,
		H.ValidTo
FROM dbo.DBConfigHistory H 
JOIN dbo.DBConfigOptions CO ON H.configuration_id = CO.configuration_id
JOIN dbo.Databases D ON D.DatabaseID = H.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE EXISTS(
			SELECT 1 
			FROM @Instances t 
			WHERE I.InstanceID = t.InstanceID
			)
AND I.IsActive=1
AND D.IsActive=1
AND (D.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY H.ValidTo DESC