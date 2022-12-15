CREATE PROC dbo.DBConfiguration_Get(
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

WITH T AS (
	SELECT I.InstanceGroupName,
		D.DatabaseID,
		D.name AS DB,
		C.configuration_id,
		CO.name, 
		C.value,
		C.value_for_secondary,
		CO.default_value,
		CASE WHEN c.value=CO.default_value THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsDefault,
		C.ValidFrom,
		MAX(CASE WHEN C.value<> CO.default_value THEN 1 ELSE 0 END) OVER(PARTITION BY D.DatabaseID) AS HasDBScopedConfiguration,
		MAX(CASE WHEN C.value<> CO.default_value THEN 1 ELSE 0 END) OVER(PARTITION BY C.configuration_id) AS IsConfiguredForAnyDB
	FROM dbo.DBConfig C 
	JOIN dbo.DBConfigOptions CO ON C.configuration_id = CO.configuration_id
	JOIN dbo.Databases D ON C.DatabaseID = D.DatabaseID
	JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
	WHERE EXISTS(SELECT 1 
				FROM @Instances t 
				WHERE t.InstanceID = I.InstanceID)
	AND (D.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
	AND (I.ShowInSummary=1 OR @ShowHidden=1)
)
SELECT T.InstanceGroupName,
       T.DB,
	   T.DatabaseID,
       T.configuration_id,
       T.name,
       T.value,
       T.value_for_secondary,
	   T.IsDefault,
       T.ValidFrom
FROM T 
WHERE (
		(T.HasDBScopedConfiguration=1 AND T.IsConfiguredForAnyDB=1) 
		OR @ConfiguredOnly=0
	  )
ORDER BY T.InstanceGroupName,
		T.DB,
		T.DatabaseID,
		T.name