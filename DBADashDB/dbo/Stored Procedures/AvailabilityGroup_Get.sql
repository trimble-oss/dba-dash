CREATE PROC dbo.AvailabilityGroup_Get(
    @InstanceID INT
)
AS
SELECT D.name AS [Database],
       AG.name AS [Availability Group],
       AR.replica_server_name AS [Replica Server],
       HADR.synchronization_state_desc AS [Sync State],
       HADR.synchronization_health_desc AS [Sync Health],
       HADR.suspend_reason_desc AS [Suspend Reason],
       HADR.database_state_desc AS [Database State],
       HADR.is_local AS [Is Local],
       AR.availability_mode_desc AS [Availability Mode],
       AR.failover_mode_desc AS [Failover Mode],
	   HADR.is_primary_replica [Is Primary],
	   AR.primary_role_allow_connections_desc [Primary Connections],
	   AR.secondary_role_allow_connections_desc [Secondary Connections],
       CD.SnapshotDate as [Snapshot Date],
       CD.Status as [Snapshot Status],
       CD.HumanSnapshotAge as [Snapshot Age],
       calc.[Estimated Data Loss (sec)],
       calc.[Estimated Recovery Time (sec)],
       HADR.secondary_lag_seconds AS [Secondary Lag (sec)],
       CASE WHEN HADR.is_primary_replica =1 THEN 'None (Primary)' WHEN HADR.is_local=1 THEN 'Reported on Primary' WHEN PrimaryHADR.is_suspended=1 THEN PrimaryHADR.suspend_reason_desc ELSE HDLag.HumanDuration END AS [Secondary Lag],
       CASE WHEN Calc.[Estimated Data Loss (sec)] =-1 THEN 'None (Primary)' WHEN Calc.[Estimated Data Loss (sec)]=-2 THEN 'Unknown' ELSE HDDataLoss.HumanDuration END AS [Estimated Data Loss],
       CASE WHEN calc.[Estimated Recovery Time (sec)]=-1 THEN 'None (Primary)' WHEN calc.[Estimated Recovery Time (sec)]=-2 THEN 'Unknown' ELSE HDRecovery.HumanDuration END AS [Estimated Recovery Time],
       HADR.redo_queue_size AS [Log Redo Queue Size (KB)],        
       HADR.redo_rate AS [Log Redo Rate (KB/s)],
       HADR.log_send_queue_size AS [Log Send Queue Size (KB)],
       HADR.log_send_rate AS [Log Send Rate (KB/s)],
       HADR.last_sent_time AS [Last Sent Time],
       HADR.last_received_time AS [Last Received Time],
       HADR.last_hardened_time AS [Last Hardened Time],
       HADR.last_redone_time AS [Last Redone Time],
       HADR.last_commit_time AS [Last Commit Time]     
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID
LEFT JOIN dbo.AvailabilityReplicas AR ON D.InstanceID = AR.InstanceID
                                         AND AR.replica_id = HADR.replica_id
LEFT JOIN dbo.AvailabilityGroups AG ON HADR.group_id = AG.group_id
                                       AND D.InstanceID = AG.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON D.InstanceID = CD.InstanceID AND CD.Reference='DatabaseHADR'
LEFT JOIN dbo.DatabasesHADR PrimaryHADR ON PrimaryHADR.InstanceID = HADR.InstanceID 
                                         AND PrimaryHADR.group_database_id = HADR.group_database_id 
                                         AND PrimaryHADR.is_primary_replica = 1
                                         AND PrimaryHADR.is_local = 1     
OUTER APPLY(SELECT  CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.synchronization_state=2 THEN 0 ELSE DATEDIFF(ss, HADR.last_commit_time, PrimaryHADR.last_commit_time) END [Estimated Data Loss (sec)],
                    CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.redo_queue_size IS NULL THEN -2 WHEN HADR.redo_queue_size = 0 THEN 0 WHEN HADR.redo_rate IS NULL OR HADR.redo_rate = 0 THEN -2 ELSE CAST(HADR.redo_queue_size AS FLOAT) / HADR.redo_rate END AS [Estimated Recovery Time (sec)]
            ) AS Calc
OUTER APPLY dbo.SecondsToHumanDuration(calc.[Estimated Data Loss (sec)]) AS HDDataLoss
OUTER APPLY dbo.SecondsToHumanDuration(calc.[Estimated Recovery Time (sec)]) AS HDRecovery
OUTER APPLY dbo.SecondsToHumanDuration(HADR.secondary_lag_seconds) AS HDLag
WHERE D.InstanceID = @InstanceID
AND HADR.InstanceID = @InstanceID
AND D.IsActive=1
ORDER BY HADR.is_local DESC,
         AG.name,
         D.name;