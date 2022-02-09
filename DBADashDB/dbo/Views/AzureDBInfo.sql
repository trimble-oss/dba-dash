CREATE VIEW dbo.AzureDBInfo
AS
SELECT	I.InstanceID,
		D.DatabaseID,
		I.ConnectionID,
		I.Instance,
		D.name AS DB,
		O.edition,
		O.service_objective,
		O.elastic_pool_name,
		Size.AllocatedSpaceMB,
		Size.UsedSpaceMB,
		Size.MaxStorageSizeMB,
		Size.AllocatedPctOfMaxSize,
		Size.UsedPctOfMaxSize,
		cfg.ConfiguredLevel,
		cfg.PctMaxSizeWarningThreshold,
		cfg.PctMaxSizeCriticalThreshold,
		CASE WHEN Size.AllocatedPctOfMaxSize > cfg.PctMaxSizeCriticalThreshold THEN 1 WHEN Size.AllocatedPctOfMaxSize > cfg.PctMaxSizeWarningThreshold THEN 2 ELSE 4 END PctMaxSizeStatus,
		CD.SnapshotDate AS FileSnapshotDate,
		CD.HumanSnapshotAge AS FileSnapshotAge,
		ISNULL(CD.Status,3) AS FileSnapshotStatus
FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.AzureDBServiceObjectives O ON O.InstanceID = I.InstanceID
OUTER APPLY(SELECT	 SUM(F.size)/128.0 AS AllocatedSpaceMB,
					 SUM(F.space_used)/128.0 AS UsedSpaceMB,
					 CASE WHEN MIN(F.max_size)<0 THEN NULL ELSE SUM(F.max_size)/128.0 END AS MaxStorageSizeMB,
					 CASE WHEN MIN(F.max_size)<0 THEN NULL ELSE SUM(F.size)*1.0/SUM(F.max_size) END AllocatedPctOfMaxSize,
					 CASE WHEN MIN(F.max_size)<0 THEN NULL ELSE SUM(F.space_used)*1.0/SUM(F.max_size) END UsedPctOfMaxSize
			FROM dbo.DBFiles F
			WHERE F.DatabaseID = D.DatabaseID 
			AND F.type=0
			AND F.IsActive=1
			AND F.data_space_id=1
			) Size
OUTER APPLY(SELECT TOP(1)	CASE WHEN T.data_space_id NOT IN(0,-1) THEN 'FG'
									WHEN T.DatabaseID <>-1 THEN 'DB'
									WHEN T.InstanceID<>-1 THEN 'Instance'
									WHEN T.InstanceID=-1 THEN 'Root'
									ELSE 'N/A' END AS ConfiguredLevel,
							T.PctMaxSizeWarningThreshold,
							T.PctMaxSizeCriticalThreshold
			FROM dbo.DBFileThresholds T 
			WHERE (T.InstanceID = I.InstanceID OR T.InstanceID=-1)
			AND (T.DatabaseID = D.DatabaseID OR T.DatabaseID=-1)
			AND (T.data_space_id = 1 OR T.data_space_id=-1)
			ORDER BY T.InstanceID DESC,T.DatabaseID DESC,T.data_space_id DESC
			) cfg
LEFT JOIN dbo.CollectionDatesStatus CD ON CD.InstanceID = D.InstanceID AND CD.Reference='DBFiles'
WHERE I.EditionID=1674378470
AND D.IsActive=1
AND I.IsActive=1
