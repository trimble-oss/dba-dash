CREATE PROC DBADash.AI_DBConfigCurrent_Get(
    @MaxRows INT = 500,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = NULL
)
AS
/* Result set 1: Database-scoped configuration values that differ from defaults */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    cfgo.name AS ConfigName,
    cfg.value AS CurrentValue,
    cfgo.default_value AS DefaultValue
FROM dbo.DBConfig cfg
INNER JOIN dbo.Databases d ON d.DatabaseID = cfg.DatabaseID
INNER JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
INNER JOIN dbo.DBConfigOptions cfgo ON cfgo.configuration_id = cfg.configuration_id
WHERE i.IsActive = 1
  AND d.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND NOT (cfg.configuration_id = 1 AND cfg.value = '0')   -- MAXDOP default
  AND NOT (cfg.configuration_id = 2 AND cfg.value = '0')   -- LEGACY_CARDINALITY_ESTIMATION default
  AND NOT (cfg.configuration_id = 3 AND cfg.value = '1')   -- PARAMETER_SNIFFING default
  AND NOT (cfg.configuration_id = 4 AND cfg.value = '0')   -- QUERY_OPTIMIZER_HOTFIXES default
  AND NOT (cfg.configuration_id = 6 AND cfg.value = '1')   -- IDENTITY_CACHE default
ORDER BY i.InstanceDisplayName, d.name, cfgo.name

/* Result set 2: Recent database-scoped config changes */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    cfgo.name AS ConfigName,
    h.value AS PreviousValue,
    h.new_value AS NewValue,
    h.ValidTo AS ChangedUtc
FROM dbo.DBConfigHistory h
INNER JOIN dbo.Databases d ON d.DatabaseID = h.DatabaseID
INNER JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
INNER JOIN dbo.DBConfigOptions cfgo ON cfgo.configuration_id = h.configuration_id
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND h.ValidTo >= DATEADD(HOUR, -ISNULL(@HoursBack, 168), SYSUTCDATETIME())
ORDER BY h.ValidTo DESC

/* Result set 3: Recent database option changes (recovery model, compat level, auto_shrink, etc.) */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    h.Setting,
    CAST(h.OldValue AS NVARCHAR(256)) AS OldValue,
    CAST(h.NewValue AS NVARCHAR(256)) AS NewValue,
    h.ChangeDate AS ChangedUtc
FROM dbo.DBOptionsHistory h
INNER JOIN dbo.Databases d ON d.DatabaseID = h.DatabaseID
INNER JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
WHERE i.IsActive = 1
  AND d.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND h.Setting NOT IN ('state', 'is_read_only', 'is_in_standby')
  AND h.ChangeDate >= DATEADD(HOUR, -ISNULL(@HoursBack, 168), SYSUTCDATETIME())
ORDER BY h.ChangeDate DESC
