
CREATE VIEW [dbo].[FileStatus]
AS
WITH F AS (SELECT F.FileID,
	   D.InstanceID,
       F.DatabaseID,
       F.data_space_id,
       I.Instance,
	   I.ConnectionID,
       D.name,
	   F.name AS file_name,
       F.Filegroup_name,
	   F.physical_name,
	   f.size/128.0 AS FileSizeMB,
	   f.space_used /128.0 AS FileUsedMB,
	   (f.size-f.space_used) / 128.0 AS FileFreeMB,
	   1.0 - (f.space_used/(f.size*1.0)) AS FilePctFree,
	   SUM(F.size) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /128.0 AS FilegroupSizeMB,
       SUM(F.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /128.0 AS FilegroupUsedMB,
	   SUM(F.size-F.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) / 128.0 AS FilegroupFreeMB,
	   1.0-(SUM(f.space_used) OVER(PARTITION BY F.data_space_id, F.DatabaseID) /SUM(f.size*1.0) OVER(PARTITION BY f.data_space_id, F.DatabaseID)) AS FilegroupPctFree,
       COUNT(*) OVER(PARTITION BY F.data_space_id,F.DatabaseID) AS FilegroupNumberOfFiles,
	   f.is_read_only,
	   D.is_read_only AS is_db_read_only,
	   D.is_in_standby,
	   D.state,
	   D.state_desc,
	   F.type,
	   SSD.SnapshotDate AS FileSnapshotDate,
	   DATEDIFF(mi,SSD.SnapshotDate,GETUTCDATE()) AS FileSnapshotAge
FROM dbo.DBFiles F
    JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
    JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
	LEFT JOIN dbo.CollectionDates SSD ON I.InstanceID = SSD.InstanceID AND SSD.Reference='DBFiles'
	WHERE F.IsActive=1
	AND I.IsActive=1
)
SELECT F.FileID,
	   F.InstanceID,
       F.DatabaseID,
       F.data_space_id,
       F.Instance,
	   F.ConnectionID,
       F.name,
	   F.file_name,
       F.Filegroup_name,
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
			ELSE NULL END AS ExcludedReason, 
	   CASE WHEN F.is_in_standby=1 OR F.is_read_only=1 OR F.is_db_read_only=1 OR F.state<>0 OR F.type=2 THEN 3 
			WHEN F.FilegroupUsedMB IS NULL THEN 3
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
	   CASE WHEN cdt.WarningThreshold IS NULL AND cdt.CriticalThreshold IS NULL THEN 3 WHEN F.FileSnapshotAge > cdt.CriticalThreshold THEN 1 WHEN F.FileSnapshotAge>cdt.WarningThreshold THEN 2 ELSE 4 END AS FileSnapshotAgeStatus
FROM F

	OUTER APPLY(SELECT TOP(1) T.FreeSpaceWarningThreshold,
                    T.FreeSpaceCriticalThreshold,
                    T.FreeSpaceCheckType,
					CASE WHEN T.data_space_id <> -1 THEN 'FG'
					WHEN T.DatabaseID <>-1 THEN 'DB'
					WHEN T.InstanceID<>-1 THEN 'Instance'
					WHEN T.InstanceID=-1 THEN 'Root'
					ELSE 'N/A' END AS ConfiguredLevel
			FROM dbo.DBFileThresholds T 
			WHERE (T.InstanceID = F.InstanceID OR T.InstanceID=-1)
			AND (T.DatabaseID = F.DatabaseID OR T.DatabaseID=-1)
			AND (T.data_space_id = F.data_space_id OR T.data_space_id=-1)
			ORDER BY T.InstanceID DESC,T.DatabaseID DESC,T.data_space_id DESC
			) cfg
OUTER APPLY(SELECT TOP(1) t.WarningThreshold,
						t.CriticalThreshold
			FROM [dbo].[CollectionDatesThresholds] t
			WHERE (t.InstanceID = F.InstanceID OR t.InstanceID=-1)
			AND t.Reference='DBFiles'
			ORDER BY t.InstanceID DESC) cdt