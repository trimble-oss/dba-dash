CREATE PROC DBADash.AI_StorageSpace_Get(
    @MaxRows INT = 300,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 168
)
AS
/* Result set 1: Drive growth trends (current vs oldest snapshot in window) */
;WITH CurrentDrive AS (
    SELECT d.DriveID, d.InstanceID, d.Name, d.Label,
        d.Capacity, d.FreeSpace,
        d.Capacity / POWER(1024.0,3) AS TotalGB,
        d.FreeSpace / POWER(1024.0,3) AS FreeGB,
        d.FreeSpace / CAST(NULLIF(d.Capacity,0) AS DECIMAL) AS PctFree
    FROM dbo.Drives d
),
OldestSnapshot AS (
    SELECT ds.DriveID,
        MIN(ds.SnapshotDate) AS OldSnapshotDate,
        MIN(ds.FreeSpace) AS OldFreeSpace_IgnoreThisUseSubquery
    FROM dbo.DriveSnapshot ds
    WHERE ds.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
    GROUP BY ds.DriveID
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    cd.Name AS DriveName,
    cd.Label AS DriveLabel,
    CAST(cd.TotalGB AS DECIMAL(18,2)) AS TotalGB,
    CAST(cd.FreeGB AS DECIMAL(18,2)) AS FreeGB,
    CAST(cd.PctFree * 100 AS DECIMAL(8,2)) AS PctFree,
    os.OldSnapshotDate,
    CAST(dsOld.FreeSpace / POWER(1024.0,3) AS DECIMAL(18,2)) AS OldFreeGB,
    CAST((dsOld.FreeSpace - cd.FreeSpace) / POWER(1024.0,3) AS DECIMAL(18,2)) AS ConsumedGB,
    CASE WHEN dsOld.FreeSpace > cd.FreeSpace AND DATEDIFF(HOUR, os.OldSnapshotDate, SYSUTCDATETIME()) > 0
        THEN CAST(cd.FreeSpace / ((dsOld.FreeSpace - cd.FreeSpace) / (DATEDIFF(HOUR, os.OldSnapshotDate, SYSUTCDATETIME()) / 24.0)) AS DECIMAL(18,1))
        ELSE NULL END AS EstimatedDaysUntilFull
FROM CurrentDrive cd
INNER JOIN dbo.Instances i ON i.InstanceID = cd.InstanceID
LEFT JOIN OldestSnapshot os ON os.DriveID = cd.DriveID
LEFT JOIN dbo.DriveSnapshot dsOld ON dsOld.DriveID = os.DriveID AND dsOld.SnapshotDate = os.OldSnapshotDate
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY cd.PctFree ASC

/* Result set 2: Database file space summary (aggregated per database) */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    SUM(CASE WHEN f.type = 0 THEN f.size * 8 / 1024.0 ELSE 0 END) AS DataAllocatedMB,
    SUM(CASE WHEN f.type = 0 THEN f.space_used * 8 / 1024.0 ELSE 0 END) AS DataUsedMB,
    SUM(CASE WHEN f.type = 1 THEN f.size * 8 / 1024.0 ELSE 0 END) AS LogAllocatedMB,
    SUM(CASE WHEN f.type = 1 THEN f.space_used * 8 / 1024.0 ELSE 0 END) AS LogUsedMB,
    CASE WHEN SUM(CASE WHEN f.type = 0 THEN f.size ELSE 0 END) > 0
        THEN CAST(100.0 * SUM(CASE WHEN f.type = 0 THEN f.space_used ELSE 0 END)
            / SUM(CASE WHEN f.type = 0 THEN f.size ELSE 0 END) AS DECIMAL(5,1))
        ELSE NULL END AS DataPctUsed,
    CASE WHEN SUM(CASE WHEN f.type = 1 THEN f.size ELSE 0 END) > 0
        THEN CAST(100.0 * SUM(CASE WHEN f.type = 1 THEN f.space_used ELSE 0 END)
            / SUM(CASE WHEN f.type = 1 THEN f.size ELSE 0 END) AS DECIMAL(5,1))
        ELSE NULL END AS LogPctUsed,
    COUNT(CASE WHEN f.type = 0 THEN 1 END) AS DataFileCount,
    COUNT(CASE WHEN f.type = 1 THEN 1 END) AS LogFileCount,
    MAX(CASE WHEN f.type = 0 AND f.max_size = -1 THEN 'UNLIMITED'
        WHEN f.type = 0 AND f.max_size > 0 THEN CAST(f.max_size * 8 / 1024 AS NVARCHAR(20)) + ' MB'
        ELSE NULL END) AS DataMaxSize,
    MAX(CASE WHEN f.type = 0 AND f.is_percent_growth = 1 THEN CAST(f.growth AS NVARCHAR(10)) + '%'
        WHEN f.type = 0 AND f.is_percent_growth = 0 THEN CAST(f.growth * 8 / 1024 AS NVARCHAR(20)) + ' MB'
        ELSE NULL END) AS DataAutoGrowth
FROM dbo.DBFiles f
INNER JOIN dbo.Databases d ON d.DatabaseID = f.DatabaseID
INNER JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
WHERE i.IsActive = 1 AND d.IsActive = 1 AND f.IsActive = 1
  AND d.source_database_id IS NULL
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName, d.name
ORDER BY DataAllocatedMB DESC

/* Result set 3: TempDB configuration audit */
SELECT TOP (@MaxRows)
    t.InstanceDisplayName,
    t.NumberOfDataFiles,
    t.MinimumRecommendedFiles,
    t.InsufficientFiles,
    t.IsEvenlySized,
    t.IsEvenGrowth,
    t.TotalSizeMB,
    t.FileSizeMB,
    t.LogMB,
    t.MaxGrowthMB,
    t.MaxGrowthPct,
    t.TempDBVolumes,
    t.cpu_core_count,
    t.T1117,
    t.T1118,
    t.IsTraceFlagRequired,
    t.IsTempDBMetadataMemoryOptimized
FROM dbo.TempDBConfiguration t
INNER JOIN dbo.Instances i ON i.InstanceID = t.InstanceID
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY t.InsufficientFiles DESC, t.IsEvenlySized ASC
