CREATE PROC dbo.DatabaseQueryStoreOptionsSummary_Get(
	@InstanceIDs VARCHAR(MAX)
)
AS
SELECT	I.InstanceGroupName,
		SUM(CASE WHEN QS.actual_state = 0 THEN 1 ELSE 0 END) as QS_OFF,
		SUM(CASE WHEN QS.actual_state = 1 THEN 1 ELSE 0 END) as QS_READ_ONLY,
		SUM(CASE WHEN QS.actual_state = 1 AND QS.readonly_reason NOT IN(0,1,8) THEN 1 ELSE 0 END) as QS_READ_ONLY_ATT,
		SUM(CASE WHEN QS.actual_state = 2 THEN 1 ELSE 0 END) as QS_READ_WRITE,
		SUM(CASE WHEN QS.actual_state = 3 THEN 1 ELSE 0 END) as QS_ERROR,
		SUM(QS.current_storage_size_mb) TotalCurrentStorageSizeMB,
		AVG(QS.current_storage_size_mb) AvgCurrentStorageSizeMB,
		MAX(QS.current_storage_size_mb) MaxCurrentStorageSizeMB,
		MAX(QS.max_storage_size_mb) MaxSizeLimitMB,
		MIN(QS.max_storage_size_mb) MinSizeLimitMB,
		MAX(CDS.SnapshotAge) AS SnapshotAge,
		MAX(CDS.SnapshotDate) AS SnapshotDate,
		ISNULL(MIN(CDS.Status),3) AS CollectionDateStatus
FROM dbo.DatabaseQueryStoreOptions QS
JOIN dbo.Databases D ON QS.DatabaseID = D.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CDS ON CDS.InstanceID = I.InstanceID AND CDS.Reference = 'DatabaseQueryStoreOptions'
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID)
GROUP BY I.InstanceGroupName
