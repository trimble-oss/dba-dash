CREATE VIEW dbo.FileStatus
AS
WITH F AS (
	SELECT F.FileID,
				   D.InstanceID,
				   F.DatabaseID,
				   F.data_space_id,
				   I.Instance,
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
				   CASE WHEN I.EngineEdition=5 THEN CAST(1 as BIT) ELSE CAST(0 as BIT) END as IsAzureDB
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
	   CASE WHEN cdt.WarningThreshold IS NULL AND cdt.CriticalThreshold IS NULL THEN 3 WHEN F.FileSnapshotAge > cdt.CriticalThreshold THEN 1 WHEN F.FileSnapshotAge>cdt.WarningThreshold THEN 2 ELSE 4 END AS FileSnapshotAgeStatus,
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
	  CASE WHEN F.type =0 THEN N'ROWS' WHEN F.type = 1 THEN N'LOG' WHEN F.type=2 THEN N'FILESTREAM' WHEN F.type = 4 THEN N'FULLTEXT' ELSE CAST(f.type as NVARCHAR(60)) END as type_desc
FROM F
OUTER APPLY(SELECT TOP(1) T.FreeSpaceWarningThreshold,
                    T.FreeSpaceCriticalThreshold,
                    T.FreeSpaceCheckType,
					CASE WHEN T.data_space_id NOT IN(0,-1) THEN 'FG'
					WHEN T.DatabaseID <>-1 THEN 'DB'
					WHEN T.InstanceID<>-1 THEN 'Instance'
					WHEN T.InstanceID=-1 THEN 'Root'
					ELSE 'N/A' END AS ConfiguredLevel,
					T.PctMaxSizeWarningThreshold,
					T.PctMaxSizeCriticalThreshold,
					T.FreeSpaceCheckZeroAutogrowthOnly
			FROM dbo.DBFileThresholds T 
			WHERE (T.InstanceID = F.InstanceID OR T.InstanceID=-1)
			AND (T.DatabaseID = F.DatabaseID OR T.DatabaseID=-1)
			AND (T.data_space_id = F.data_space_id OR (T.data_space_id=-1 AND F.type=0))
			ORDER BY T.InstanceID DESC,T.DatabaseID DESC,T.data_space_id DESC
			) cfg
OUTER APPLY(SELECT TOP(1) t.WarningThreshold,
						t.CriticalThreshold
			FROM [dbo].[CollectionDatesThresholds] t
			WHERE (t.InstanceID = F.InstanceID OR t.InstanceID=-1)
			AND t.Reference='DBFiles'
			ORDER BY t.InstanceID DESC) cdt