CREATE VIEW DBFileStatus
AS
WITH agg AS (SELECT D.InstanceID,
       F.DatabaseID,
       F.data_space_id,
       I.Instance,
       D.name,
       F.filegroup_name,
	   SUM(F.size)/128.0 AS SizeMB,
       SUM(F.space_used)/128.0 AS UsedMB,
	   SUM(F.size-F.space_used)/ 128.0 AS FreeMB,
	   1.0-(SUM(f.space_used)/SUM(f.size*1.0)) AS PctFree,
       COUNT(*) AS NumberOfFiles,
	   f.is_read_only,
	   D.is_read_only AS is_db_read_only,
	   D.is_in_standby,
	   D.state
FROM dbo.DBFiles F
    JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
    JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
GROUP BY D.InstanceID,
         I.Instance,
         F.DatabaseID,
         F.data_space_id,
         D.name,
         F.filegroup_name,
		 F.is_read_only,
		 D.is_in_standby,
		 D.is_read_only,
		 D.state
)
SELECT agg.InstanceID,
       agg.DatabaseID,
       agg.data_space_id,
       agg.Instance,
       agg.name,
       agg.filegroup_name,
       agg.SizeMB,
       agg.UsedMB,
       agg.FreeMB,
       agg.PctFree,
       agg.NumberOfFiles,
       agg.is_read_only,
	   agg.is_db_read_only,
       agg.is_in_standby,
	   agg.state,
	   CASE WHEN agg.is_in_standby=1 OR agg.is_read_only=1 OR agg.is_db_read_only=1 OR agg.state<>0 THEN 3 WHEN cfg.FreeSpaceCheckType='%' AND agg.PctFree<= cfg.FreeSpaceCriticalThreshold THEN 1 
			WHEN cfg.FreeSpaceCheckType='M' AND agg.FreeMB<cfg.FreeSpaceCriticalThreshold THEN 1
			WHEN cfg.FreeSpaceCheckType='%' AND agg.PctFree<=cfg.FreeSpaceWarningThreshold THEN 2
			WHEN cfg.FreeSpaceCheckType='M' AND agg.FreeMB <=cfg.FreeSpaceWarningThreshold THEN 2
			WHEN cfg.FreeSpaceWarningThreshold IS NULL AND cfg.FreeSpaceCriticalThreshold IS NULL THEN 3
			ELSE 4 END AS FreeSpaceStatus,
	   cfg.FreeSpaceWarningThreshold,
       cfg.FreeSpaceCriticalThreshold,
       cfg.FreeSpaceCheckType,
	   cfg.ConfiguredLevel
FROM agg
	OUTER APPLY(SELECT TOP(1) T.FreeSpaceWarningThreshold,
                    T.FreeSpaceCriticalThreshold,
                    T.FreeSpaceCheckType,
					CASE WHEN T.data_space_id <> -1 THEN 'FG'
					WHEN T.DatabaseID <>-1 THEN 'DB'
					WHEN T.InstanceID<>-1 THEN 'Instance'
					WHEN T.InstanceID=-1 THEN 'Root'
					ELSE 'N/A' END AS ConfiguredLevel
			FROM dbo.DBFileThresholds T 
			WHERE (T.InstanceID = agg.InstanceID OR T.InstanceID=-1)
			AND (T.DatabaseID = agg.DatabaseID OR T.DatabaseID=-1)
			AND (T.data_space_id = agg.data_space_id OR T.data_space_id=-1)
			ORDER BY T.InstanceID DESC,T.DatabaseID DESC,T.data_space_id DESC
			) cfg