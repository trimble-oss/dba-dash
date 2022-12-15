CREATE PROC dbo.TempDBConfig_Get(
	@InstanceIDs VARCHAR(MAX),
    @ShowHidden BIT=1
)
AS
SELECT InstanceID,
       InstanceDisplayName,
       Instance,
       NumberOfDataFiles,
       MinimumRecommendedFiles,
       InsufficientFiles,
       NumberOfLogFiles,
       IsEvenlySized,
       IsEvenGrowth,
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
       IsTempDBMetadataMemoryOptimized
FROM dbo.TempDBConfiguration T
WHERE EXISTS(SELECT 1 
            FROM STRING_SPLIT(@InstanceIDs,',') ss 
            WHERE ss.value = T.InstanceID)
AND (T.ShowInSummary=1 OR @ShowHidden=1)