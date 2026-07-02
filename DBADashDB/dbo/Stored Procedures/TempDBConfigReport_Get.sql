CREATE PROC dbo.TempDBConfigReport_Get(
	@InstanceIDs IDs READONLY,
    @ShowHidden BIT=1
)
AS
SELECT InstanceID,
       InstanceDisplayName,
       Instance,
       NumberOfDataFiles,
       MinimumRecommendedFiles,
       InsufficientFiles,
       InsufficientFilesStatus,
       NumberOfLogFiles,
       NumberOfLogFilesStatus,
       IsEvenlySized,
       IsEvenlySizedStatus,
       IsEvenGrowth,
       IsEvenGrowthStatus,
       TotalSizeMB,
       LogMB,
       FileSizeMB,
       MaxGrowthMB,
       MaxLogGrowthMB,
       MaxGrowthPct,
       MaxLogGrowthPct,
       TempDBVolumes,
       cpu_core_count,
       T1117,
       T1118,
       IsTraceFlagRequired,
       T1117Status,
       T1118Status,
       IsTempDBMetadataMemoryOptimized,
       TempDBMemoryOptStatus
FROM dbo.TempDBConfiguration T
WHERE EXISTS(SELECT 1
            FROM @InstanceIDs ids
            WHERE ids.ID = T.InstanceID)
AND (T.ShowInSummary=1 OR @ShowHidden=1)
