CREATE PROC Report.Configuration
AS
SELECT I.Instance, SCO.configuration_id,
                  SCO.name,
                  SCO.default_value,
				  SC.value,
				  CASE WHEN SC.value=SCO.default_value THEN 1 ELSE 0 END AS IsDefault
FROM dbo.SysConfig SC
JOIN dbo.Instances I ON SC.InstanceID=I.InstanceID
JOIN dbo.SysConfigOptions SCO ON SC.configuration_id = SCO.configuration_id
WHERE I.IsActive=1