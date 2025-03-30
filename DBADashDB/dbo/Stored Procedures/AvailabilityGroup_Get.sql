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
	   AR.secondary_role_allow_connections_desc [Seconadary Connections],	  
       CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.synchronization_state=2 THEN 0 ELSE ISNULL(DATEDIFF(ss, HADR.last_commit_time, PrimaryHADR.last_commit_time), -2) END [Max Estimated Data Loss (sec)],
       CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.redo_queue_size is null THEN -2 WHEN HADR.redo_queue_size = 0 THEN 0 WHEN HADR.redo_rate is null or HADR.redo_rate = 0 THEN -2 ELSE CAST(HADR.redo_queue_size AS float) / HADR.redo_rate END AS [Max Estimated Recovery Time],
       HADR.log_send_queue_size AS [Log Send Queue Size (KB)],
       HADR.log_send_rate AS [Log Send Rate (KB/s)],
       HADR.redo_queue_size AS [Log Redo Queue Size (KB)],        
       HADR.redo_rate AS [Log Redo Rate (KB/s)],
       HADR.last_sent_time AS [Last Sent Time],
       HADR.last_received_time AS [Last Received Time],
       HADR.last_hardened_time AS [Last Hardened Time],
       HADR.last_redone_time AS [Last Redone Time],
       HADR.last_commit_time AS [Last Commit Time],      
       CD.SnapshotDate as [Snapshot Date],
       CD.Status as [Snapshot Status],
       CD.SnapshotAge as [Snapshot Age]
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
WHERE D.InstanceID = @InstanceID
AND HADR.InstanceID = @InstanceID
AND D.IsActive=1
ORDER BY HADR.is_local DESC,
         AG.name,
         D.name;