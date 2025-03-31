CREATE PROC dbo.AvailabilityGroupSummary_Get(
    @InstanceIDs IDs READONLY
)
AS
WITH T AS (
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
       MAX(HADR.secondary_lag_seconds) AS [Max Secondary Lag (sec)],
       MAX(CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.synchronization_state=2 THEN 0 ELSE ISNULL(DATEDIFF(ss, HADR.last_commit_time, PrimaryHADR.last_commit_time), -2) END) [Max Estimated Data Loss (sec)],
       MAX(CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.redo_queue_size IS NULL THEN -2 WHEN HADR.redo_queue_size = 0 THEN 0 WHEN HADR.redo_rate IS NULL OR HADR.redo_rate = 0 THEN -2 ELSE CAST(HADR.redo_queue_size AS FLOAT) / HADR.redo_rate END) AS [Max Estimated Recovery Time (sec)],
       SUM(HADR.redo_queue_size) AS [Total Redo Queue Size (KB)],
       SUM(HADR.log_send_queue_size) AS [Total Log Send Queue Size (KB)],
       AVG(HADR.redo_queue_size) AS [Avg Redo Queue Size (KB)],
       AVG(HADR.log_send_queue_size) AS [Avg Log Send Queue Size (KB)],
       MIN(CD.SnapshotDate) as [Snapshot Date],
       MIN(CD.Status) as [Snapshot Status],
       MAX(CD.HumanSnapshotAge) as [Snapshot Age],
       MAX(CASE WHEN ISNULL(HADR.synchronization_state,0) <> 2 AND HADR.is_local = 1 THEN 6 ELSE 4 END) AS [Synchronized Status]
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID AND HADR.InstanceID = D.InstanceID
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
LEFT JOIN dbo.AvailabilityReplicas AR ON D.InstanceID = AR.InstanceID
                                         AND AR.replica_id = HADR.replica_id
LEFT JOIN dbo.AvailabilityGroups AG ON HADR.group_id = AG.group_id
                                       AND D.InstanceID = AG.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON D.InstanceID = CD.InstanceID AND CD.Reference='DatabaseHADR'
LEFT JOIN dbo.DatabasesHADR PrimaryHADR ON PrimaryHADR.InstanceID = HADR.InstanceID 
                                         AND PrimaryHADR.group_database_id = HADR.group_database_id 
                                         AND PrimaryHADR.is_primary_replica = 1
                                         AND PrimaryHADR.is_local = 1     
WHERE EXISTS(
        SELECT 1 
        FROM @InstanceIDs T 
        WHERE T.ID= I.InstanceID
        )
AND D.IsActive=1
GROUP BY I.Instance,I.InstanceID,I.InstanceDisplayName
)
SELECT T.InstanceID,
       T.Instance,
       T.[Primary Replicas],
       T.[Secondary Replicas],
       T.[Readable Secondaries],
       T.[Async Commit],
       T.[Sync Commit],
       T.[Not Synchronizing],
       T.Synchronizing,
       T.Synchronized,
       T.Reverting,
       T.Initializing,
       T.[Remote Not Synchronizing],
       T.[Remote Synchronizing],
       T.[Remote Synchronized],
       T.[Remote Reverting],
       T.[Remote Initializing],
       T.[Sync Health],
       T.[Snapshot Date],
       T.[Snapshot Status],
       T.[Snapshot Age],
       T.[Synchronized Status],
       T.[Max Secondary Lag (sec)],
       T.[Max Estimated Data Loss (sec)],
       T.[Max Estimated Recovery Time (sec)],
       HDLag.HumanDuration AS [Max Secondary Lag],
       CASE WHEN T.[Max Estimated Data Loss (sec)] =-1 THEN 'None (Primary)' WHEN T.[Max Estimated Data Loss (sec)]=-2 THEN 'Unknown' ELSE HDDataLoss.HumanDuration END AS [Max Estimated Data Loss],
       CASE WHEN T.[Max Estimated Recovery Time (sec)]=-1 THEN 'None (Primary)' WHEN T.[Max Estimated Recovery Time (sec)]=-2 THEN 'Unknown' ELSE HDRecovery.HumanDuration END AS [Max Estimated Recovery Time],
       T.[Total Redo Queue Size (KB)],
       T.[Avg Redo Queue Size (KB)],
       T.[Total Log Send Queue Size (KB)],
       T.[Avg Log Send Queue Size (KB)]
FROM T
OUTER APPLY dbo.SecondsToHumanDuration(T.[Max Secondary Lag (sec)]) AS HDLag
OUTER APPLY dbo.SecondsToHumanDuration(T.[Max Estimated Data Loss (sec)]) AS HDDataLoss
OUTER APPLY dbo.SecondsToHumanDuration(T.[Max Estimated Recovery Time (sec)]) AS HDRecovery