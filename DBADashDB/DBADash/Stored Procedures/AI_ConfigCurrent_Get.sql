CREATE PROC DBADash.AI_ConfigCurrent_Get(
    @MaxRows INT = 500,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = NULL
)
AS
/* Current configuration values for key settings across all active instances */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    o.name AS ConfigName,
    CAST(sc.value_in_use AS NVARCHAR(256)) AS CurrentValue,
    o.description,
    i.cpu_count,
    CAST(i.physical_memory_kb / 1024.0 / 1024.0 AS DECIMAL(18,2)) AS PhysicalMemoryGB
FROM dbo.SysConfig sc
INNER JOIN dbo.SysConfigOptions o ON o.configuration_id = sc.configuration_id
INNER JOIN dbo.Instances i ON i.InstanceID = sc.InstanceID
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND o.name IN (
    'max degree of parallelism',
    'cost threshold for parallelism',
    'max server memory (MB)',
    'min server memory (MB)',
    'optimize for ad hoc workloads',
    'backup compression default',
    'remote admin connections',
    'fill factor (%)',
    'max worker threads',
    'recovery interval (min)',
    'cross db ownership chaining',
    'clr enabled',
    'xp_cmdshell',
    'Database Mail XPs',
    'Ole Automation Procedures',
    'Ad Hoc Distributed Queries',
    'blocked process threshold (s)',
    'default trace enabled',
    'priority boost',
    'lightweight pooling'
  )
ORDER BY i.InstanceDisplayName, o.name
