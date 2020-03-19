CREATE VIEW NonDefaultDBConfig
AS
SELECT i.Instance, d.name,cfgo.name AS configuration_name,value
FROM dbo.DBConfig cfg
JOIN dbo.Databases d ON d.DatabaseID = cfg.DatabaseID
JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
JOIN dbo.DBConfigOptions cfgo ON cfgo.configuration_id = cfg.configuration_id
WHERE NOT (cfg.configuration_id=1 AND value=0) --MAXDOP
AND NOT (cfg.configuration_id=2 AND cfg.value=0) --LEGACY_CARDINALITY_ESTIMATION
AND NOT (cfg.configuration_id=3 AND cfg.value=1) -- PARAMETER_SNIFFING
AND NOT (cfg.configuration_id=4 AND cfg.value=0) -- QUERY_OPTIMIZER_HOTFIXES
AND NOT (cfg.configuration_id=6 AND cfg.value=1) -- IDENTITY_CACHE