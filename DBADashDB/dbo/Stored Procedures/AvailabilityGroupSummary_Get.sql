CREATE PROC dbo.AvailabilityGroupSummary_Get(
    @InstanceIDs VARCHAR(MAX),
    @ShowHidden BIT=1
)
AS
SELECT I.InstanceID,
       I.InstanceDisplayName AS Instance,
       SUM(CASE WHEN HADR.is_primary_replica = 1 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Primary Replicas],
	   SUM(CASE WHEN HADR.is_primary_replica = 0 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Secondary Replicas],
	   SUM(CASE WHEN AR.secondary_role_allow_connections>0 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Readable Secondaries],
	   SUM(CASE WHEN AR.availability_mode=0 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Async Commit],
	   SUM(CASE WHEN AR.availability_mode=1 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Sync Commit],
       SUM(CASE WHEN HADR.synchronization_state = 0 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Not Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 1 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 2 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Synchronized],
       SUM(CASE WHEN HADR.synchronization_state = 3 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Reverting],
       SUM(CASE WHEN HADR.synchronization_state = 4 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Initializing],
       SUM(CASE WHEN HADR.synchronization_state = 0 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Remote Not Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 1 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Remote Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 2 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Remote Synchronized],
       SUM(CASE WHEN HADR.synchronization_state = 3 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Remote Reverting],
       SUM(CASE WHEN HADR.synchronization_state = 4 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Remote Initializing],
       CASE MIN(HADR.synchronization_health)WHEN 0 THEN 'NOT_HEALTHY' WHEN 1 THEN 'PARTIALLY_HEALTHY' WHEN 2 THEN 'HEALTHY' ELSE NULL END AS [Sync Health],
       MAX(CD.SnapshotDate) as [Snapshot Date],
       MAX(CD.Status) as [Snapshot Status],
       MAX(CD.SnapshotAge) as [Snapshot Age]
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
LEFT JOIN dbo.AvailabilityReplicas AR ON D.InstanceID = AR.InstanceID
                                         AND AR.replica_id = HADR.replica_id
LEFT JOIN dbo.AvailabilityGroups AG ON HADR.group_id = AG.group_id
                                       AND D.InstanceID = AG.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON D.InstanceID = CD.InstanceID AND CD.Reference='DatabaseHADR'
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID
		UNION ALL
		SELECT 1 WHERE @InstanceIDs IS NULL)
AND D.IsActive=1
AND (I.ShowInSummary = 1 OR  @ShowHidden = 1)
GROUP BY I.Instance,I.InstanceID,I.InstanceDisplayName;