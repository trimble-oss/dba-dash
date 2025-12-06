CREATE VIEW dbo.FileStatus
AS
WITH F AS (
	SELECT F.FileID,
				   D.InstanceID,
				   F.DatabaseID,
				   F.data_space_id,
				   I.Instance,
				   I.InstanceDisplayName,
				   I.InstanceGroupName,
				   I.ConnectionID,
				   D.name,
				   F.name AS file_name,
				   F.filegroup_name,
				   F.physical_name,
				   f.size/128.0 AS FileSizeMB,
				   f.space_used /128.0 AS FileUsedMB,
				   (f.size-f.space_used) / 128.0 AS FileFreeMB,
				   1.0 - (f.space_used/(NULLIF(f.size,0)*1.0)) AS FilePctFree,
				   SUM(F.size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /128.0 AS FilegroupSizeMB,
				   SUM(F.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /128.0 AS FilegroupUsedMB,
				   SUM(F.size-F.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) / 128.0 AS FilegroupFreeMB,
				   1.0-(SUM(f.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /SUM(NULLIF(f.size,0)*1.0) OVER(PARTITION BY f.data_space_id, F.DatabaseID)) AS FilegroupPctFree,
				   COUNT(*) OVER(PARTITION BY F.data_space_id,F.DatabaseID) AS FilegroupNumberOfFiles,
				   CASE WHEN MIN(f.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) <0 THEN NULL ELSE SUM(F.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID)  /128.0 END AS FilegroupMaxSizeMB,
				   CASE WHEN MIN(f.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) <0 THEN NULL ELSE SUM(F.size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /NULLIF(SUM(F.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) *1.0,0) END AS FilegroupPctOfMaxSize,
				   CASE WHEN MIN(f.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) <0 THEN NULL ELSE SUM(F.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /NULLIF(SUM(F.max_size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) *1.0,0) END AS FilegroupUsedPctOfMaxSize,
				   SUM(CASE WHEN f.growth>0 THEN 1 WHEN f.type=2 THEN NULL ELSE 0 END) OVER(PARTITION BY F.data_space_id,F.DatabaseID) AS FilegroupAutogrowFileCount,
				   f.is_read_only,
				   D.is_read_only AS is_db_read_only,
				   D.is_in_standby,
				   D.state,
				   D.state_desc,
				   F.type,
				   SSD.SnapshotDate AS FileSnapshotDate,
				   DATEDIFF(mi,SSD.SnapshotDate,GETUTCDATE()) AS FileSnapshotAge,
				   F.max_size,
				   NULLIF(f.max_size,-1)/128.0 AS MaxSizeMB,
				   CASE WHEN F.is_percent_growth=1 THEN (F.growth*0.01*F.size)/128.0 ELSE F.growth/128.0 END AS GrowthMB,
				   F.growth,
				   CASE WHEN F.is_percent_growth=1 THEN F.growth ELSE NULL END AS GrowthPct,
				   F.is_percent_growth,
				   CASE WHEN I.EngineEdition=5 THEN CAST(1 as BIT) ELSE CAST(0 as BIT) END as IsAzureDB,
				   I.ShowInSummary
		FROM dbo.DBFiles F
			JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
			JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
			LEFT JOIN dbo.CollectionDates SSD ON I.InstanceID = SSD.InstanceID AND SSD.Reference='DBFiles'
			WHERE F.IsActive=1
			AND I.IsActive=1
			AND D.IsActive=1
)
SELECT F.FileID,
	   F.InstanceID,
       F.DatabaseID,
       F.data_space_id,
       F.Instance,
	   F.InstanceDisplayName,
	   F.InstanceGroupName,
	   F.ConnectionID,
       F.name,
	   F.file_name,
       F.filegroup_name,
	   F.physical_name,
	   F.FileSizeMB,
	   F.FileUsedMB,
	   F.FileFreeMB,
	   F.FilePctFree,
	   F.FilegroupSizeMB,
	   F.FilegroupUsedMB,
	   F.FilegroupFreeMB,
	   F.FilegroupPctFree,
	   F.FilegroupNumberOfFiles,
	   F.FilegroupAutogrowFileCount,
       F.is_read_only,
	   F.is_db_read_only,
       F.is_in_standby,
	   F.state,
	   F.state_desc,
	   CASE WHEN F.is_in_standby=1 THEN 'Standby' 
			WHEN F.is_read_only=1 THEN 'Filegroup Readonly' 
			WHEN F.is_db_read_only=1 THEN 'Database Readonly' 
			WHEN F.state<>0 THEN 'Database State:' + F.state_desc 
			WHEN F.type=2 THEN 'Filestream' 
			WHEN cfg.FreeSpaceWarningThreshold IS NULL AND cfg.FreeSpaceCriticalThreshold IS NULL THEN 'No Threshold'
			WHEN F.FilegroupUsedMB IS NULL THEN 'UsedMB is NULL'
			WHEN cfg.FreeSpaceCheckZeroAutogrowthOnly=1 AND F.FilegroupAutogrowFileCount>0 THEN 'Autogrow enabled'
			WHEN F.IsAzureDB = 1 THEN 'AzureDB'
			ELSE NULL END AS ExcludedReason, 
	   CASE WHEN F.is_in_standby=1 OR F.is_read_only=1 OR F.is_db_read_only=1 OR F.state<>0 OR F.type=2 OR F.IsAzureDB=1 THEN 3 
			WHEN F.FilegroupUsedMB IS NULL THEN 3
			WHEN cfg.FreeSpaceCheckZeroAutogrowthOnly=1 AND F.FilegroupAutogrowFileCount>0 THEN 3
			WHEN cfg.FreeSpaceCheckType='%' AND F.FilegroupPctFree<= cfg.FreeSpaceCriticalThreshold THEN 1 
			WHEN cfg.FreeSpaceCheckType='M' AND F.FilegroupFreeMB<cfg.FreeSpaceCriticalThreshold THEN 1
			WHEN cfg.FreeSpaceCheckType='%' AND F.FilegroupPctFree<=cfg.FreeSpaceWarningThreshold THEN 2
			WHEN cfg.FreeSpaceCheckType='M' AND F.FilegroupFreeMB <=cfg.FreeSpaceWarningThreshold THEN 2
			WHEN cfg.FreeSpaceWarningThreshold IS NULL AND cfg.FreeSpaceCriticalThreshold IS NULL THEN 3
			ELSE 4 END AS FreeSpaceStatus,
	   cfg.FreeSpaceWarningThreshold,
       cfg.FreeSpaceCriticalThreshold,
       cfg.FreeSpaceCheckType,
	   cfg.ConfiguredLevel,
	   F.FileSnapshotDate,
	   F.FileSnapshotAge,
	   CASE WHEN CDT.WarningThreshold IS NULL AND CDT.CriticalThreshold IS NULL THEN 3 WHEN F.FileSnapshotAge > CDT.CriticalThreshold THEN 1 WHEN F.FileSnapshotAge>CDT.WarningThreshold THEN 2 ELSE 4 END AS FileSnapshotAgeStatus,
	   F.max_size,
	   F.MaxSizeMB,
	   F.GrowthMB,
	   F.growth,
	   F.GrowthPct,
	   F.is_percent_growth,
	   F.FilegroupMaxSizeMB,
	   F.FilegroupPctOfMaxSize,
	   F.FilegroupUsedPctOfMaxSize,
	   CASE WHEN F.is_in_standby=1 OR F.is_read_only=1 OR F.is_db_read_only=1 OR F.state<>0 OR F.type=2 THEN 3 
			WHEN F.FilegroupPctOfMaxSize > cfg.PctMaxSizeCriticalThreshold THEN 1 
			WHEN F.FilegroupPctOfMaxSize > cfg.PctMaxSizeWarningThreshold THEN 2 
			WHEN F.max_size >= 268435456 AND F.data_space_id=0 THEN 3 
			WHEN cfg.PctMaxSizeWarningThreshold IS NOT NULL AND cfg.PctMaxSizeCriticalThreshold IS NOT NULL AND F.FilegroupMaxSizeMB IS NOT NULL THEN 4 ELSE 3 END AS PctMaxSizeStatus,
	  CASE WHEN F.FilegroupMaxSizeMB IS NULL THEN 'Max size not capped'
			WHEN F.max_size >= 268435456 AND F.data_space_id=0 THEN 'Log file max size'
			WHEN F.is_in_standby=1 THEN 'Standby' 
			WHEN F.is_read_only=1 THEN 'Filegroup Readonly' 
			WHEN F.is_db_read_only=1 THEN 'Database Readonly' 
			WHEN F.state<>0 THEN 'Database State:' + F.state_desc 
			WHEN F.type=2 THEN 'Filestream' 
			WHEN cfg.PctMaxSizeWarningThreshold IS NULL AND cfg.PctMaxSizeCriticalThreshold IS NULL THEN 'No Threshold'
			ELSE NULL END AS MaxSizeExcludedReason,
	  CASE WHEN F.is_db_read_only=0 AND F.state=0 AND F.is_read_only=0 AND F.is_in_standby=0 AND F.FilegroupAutogrowFileCount=0 THEN 2 WHEN F.FilegroupAutogrowFileCount=0 THEN 3 ELSE 4 END AS FilegroupAutogrowStatus,
	  F.type,
	  CASE WHEN F.type =0 THEN N'ROWS' WHEN F.type = 1 THEN N'LOG' WHEN F.type=2 THEN N'FILESTREAM' WHEN F.type = 4 THEN N'FULLTEXT' ELSE CAST(f.type as NVARCHAR(60)) END as type_desc,
	  F.ShowInSummary
FROM F
/* Filegroup level threshold */
LEFT OUTER JOIN dbo.DBFileThresholds T_FG ON T_FG.InstanceID = F.InstanceID 
									AND T_FG.DatabaseID = F.DatabaseID 
									AND T_FG.data_space_id = F.data_space_id 
									AND T_FG.data_space_id <> 0 /* Exclude log as this is considered a DB threshold */
/* DB level threshold */
LEFT OUTER JOIN dbo.DBFileThresholds T_DB ON T_DB.InstanceID = F.InstanceID 
									AND T_DB.DatabaseID = F.DatabaseID 
									AND (
											(T_DB.data_space_id = -1 AND F.type=0) /* Row threshold */
											OR 
											(T_DB.data_space_id=0 AND F.type=1) /* Log Threshold */
										)
									AND T_FG.InstanceID IS NULL /* Only JOIN if we don't have a Filegroup level threshold (so COALESCE works with NULL thresholds) */
/* Instance level threshold */
LEFT OUTER JOIN dbo.DBFileThresholds T_Inst ON T_Inst.InstanceID = F.InstanceID 
									AND T_Inst.DatabaseID = -1 
									AND (
											(T_Inst.data_space_id = -1 AND F.type=0) /* Row threshold */
											OR 
											(T_Inst.data_space_id=0 AND F.type=1) /* Log Threshold */
										) 
									AND T_DB.InstanceID IS NULL /* Only JOIN if we don't have a DB level threshold (so COALESCE works with NULL thresholds) */
/* Root level threshold */
LEFT OUTER JOIN dbo.DBFileThresholds T_Root ON T_Root.InstanceID = -1 
									AND T_Root.DatabaseID = -1 
									AND (
											(T_Root.data_space_id = -1 AND F.type=0) /* Row threshold */
											OR 
											(T_Root.data_space_id=0 AND F.type=1) /* Log Threshold */
										)
									AND T_Inst.InstanceID IS NULL /* Only JOIN if we don't have an instance threshold (so COALESCE works with NULL thresholds) */
/* Calculate the threshold we need to apply. */
OUTER APPLY(
			SELECT	COALESCE(T_FG.FreeSpaceWarningThreshold, T_DB.FreeSpaceWarningThreshold, T_Inst.FreeSpaceWarningThreshold, T_Root.FreeSpaceWarningThreshold) AS FreeSpaceWarningThreshold,
					COALESCE(T_FG.FreeSpaceCriticalThreshold, T_DB.FreeSpaceCriticalThreshold, T_Inst.FreeSpaceCriticalThreshold, T_Root.FreeSpaceCriticalThreshold) AS FreeSpaceCriticalThreshold,
					COALESCE(T_FG.FreeSpaceCheckType, T_DB.FreeSpaceCheckType, T_Inst.FreeSpaceCheckType, T_Root.FreeSpaceCheckType) AS FreeSpaceCheckType,
					COALESCE(T_FG.PctMaxSizeWarningThreshold, T_DB.PctMaxSizeWarningThreshold, T_Inst.PctMaxSizeWarningThreshold, T_Root.PctMaxSizeWarningThreshold) AS PctMaxSizeWarningThreshold,
					COALESCE(T_FG.PctMaxSizeCriticalThreshold, T_DB.PctMaxSizeCriticalThreshold, T_Inst.PctMaxSizeCriticalThreshold, T_Root.PctMaxSizeCriticalThreshold) AS PctMaxSizeCriticalThreshold,
					COALESCE(T_FG.FreeSpaceCheckZeroAutogrowthOnly, T_DB.FreeSpaceCheckZeroAutogrowthOnly, T_Inst.FreeSpaceCheckZeroAutogrowthOnly, T_Root.FreeSpaceCheckZeroAutogrowthOnly) AS FreeSpaceCheckZeroAutogrowthOnly,
					CASE 
						WHEN T_FG.InstanceID IS NOT NULL THEN 'FG'
						WHEN T_DB.InstanceID IS NOT NULL THEN 'DB'
						WHEN T_Inst.InstanceID IS NOT NULL THEN 'Instance'
						WHEN T_Root.InstanceID IS NOT NULL THEN 'Root'
						ELSE 'N/A' 
					END AS ConfiguredLevel
			) cfg
-- Join for Collection Date Thresholds (Instance Specific)
LEFT JOIN dbo.CollectionDatesThresholds CDT_Inst ON CDT_Inst.InstanceID = F.InstanceID AND CDT_Inst.Reference = 'DBFiles'
-- Join for Collection Date Thresholds (Global)
LEFT JOIN dbo.CollectionDatesThresholds CDT_Root ON CDT_Root.InstanceID = -1 AND CDT_Root.Reference = 'DBFiles'
											AND CDT_Inst.InstanceID IS NULL /* Only JOIN if we don't have an instance level threshold. (so COALESCE works with NULL thresholds) */ 
-- Calculate Effective Date Thresholds
CROSS APPLY (
    SELECT 	COALESCE(CDT_Inst.WarningThreshold, CDT_Root.WarningThreshold) AS WarningThreshold,
			COALESCE(CDT_Inst.CriticalThreshold, CDT_Root.CriticalThreshold) AS CriticalThreshold
	) AS CDT;