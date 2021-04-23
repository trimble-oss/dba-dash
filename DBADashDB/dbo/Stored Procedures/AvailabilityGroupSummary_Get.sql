CREATE PROC dbo.AvailabilityGroupSummary_Get(
    @InstanceIDs VARCHAR(MAX)
)
AS
SELECT I.InstanceID,
		I.Instance,
       SUM(CASE WHEN HADR.is_primary_replica = 1 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Primary Replicas],
	   SUM(CASE WHEN HADR.is_primary_replica = 0 AND HADR.is_local=1 THEN 1 ELSE 0 END) AS [Secondary Replicas],
       SUM(CASE WHEN HADR.synchronization_state = 0 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Not Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 1 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 2 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Synchronized],
       SUM(CASE WHEN HADR.synchronization_state = 3 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Reverting],
       SUM(CASE WHEN HADR.synchronization_state = 4 AND HADR.is_local = 1 THEN 1 ELSE 0 END) AS [Initializing],
       SUM(CASE WHEN HADR.synchronization_state = 0 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Replica Not Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 1 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Replica Synchronizing],
       SUM(CASE WHEN HADR.synchronization_state = 2 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Replica Synchronized],
       SUM(CASE WHEN HADR.synchronization_state = 3 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Replica Reverting],
       SUM(CASE WHEN HADR.synchronization_state = 4 AND HADR.is_local = 0 THEN 1 ELSE 0 END) AS [Replica Initializing],
       CASE MIN(HADR.synchronization_health)WHEN 0 THEN 'NOT_HEALTHY' WHEN 1 THEN 'PARTIALLY_HEALTHY' WHEN 2 THEN 'HEALTHY' ELSE NULL END AS [Sync Health]
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
LEFT JOIN dbo.AvailabilityReplicas AR ON D.InstanceID = AR.InstanceID
                                         AND AR.replica_id = HADR.replica_id
LEFT JOIN dbo.AvailabilityGroups AG ON HADR.group_id = AG.group_id
                                       AND D.InstanceID = AG.InstanceID
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID
		UNION ALL
		SELECT 1 WHERE @InstanceIDs IS NULL)
GROUP BY I.Instance,I.InstanceID;