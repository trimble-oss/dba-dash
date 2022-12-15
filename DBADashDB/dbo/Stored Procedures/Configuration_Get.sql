CREATE PROC dbo.Configuration_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ConfiguredOnly BIT=0,
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
	SELECT I.Instance, 
			I.ConnectionID,
			I.InstanceDisplayName,
			SCO.configuration_id,
			SCO.name,
			SCO.default_value,
			SC.value,
			CASE WHEN SC.value=SCO.default_value THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsDefault,
			MAX(CASE WHEN SC.value=SCO.default_value THEN 0 ELSE 1 END) OVER(PARTITION BY SCO.configuration_id) AS configured 
	FROM dbo.SysConfig SC
	JOIN dbo.Instances I ON SC.InstanceID=I.InstanceID
	JOIN dbo.SysConfigOptions SCO ON SC.configuration_id = SCO.configuration_id
	WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	AND I.IsActive=1
	AND (I.ShowInSummary=1 OR @ShowHidden=1)
)
SELECT T.Instance,
       T.ConnectionID,
	   T.InstanceDisplayName,
       T.configuration_id,
       T.name,
       T.default_value,
       T.value,
       T.IsDefault,
       T.configured
FROM T
WHERE (T.configured=1 OR @ConfiguredOnly=0)
ORDER BY T.InstanceDisplayName,T.name