CREATE PROC dbo.AvailabilityGroup_Get(@InstanceID INT)
AS
SELECT D.name AS [Database],
       AG.name AS [Availability Group],
       AR.replica_server_name AS [Replica Server],
       HADR.synchronization_state_desc AS [Sync State],
       HADR.synchronization_health_desc AS [Sync Health,
       HADR.suspend_reason_desc AS [Suspend Reason],
       database_state_desc AS [Database State],
       HADR.is_local AS [Is Local],
       AR.availability_mode_desc AS [Availability Mode],
       AR.failover_mode_desc AS [Failover Mode],
	   HADR.is_primary_replica [Is Primary]
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID
LEFT JOIN dbo.AvailabilityReplicas AR ON D.InstanceID = AR.InstanceID
                                         AND AR.replica_id = HADR.replica_id
LEFT JOIN dbo.AvailabilityGroups AG ON HADR.group_id = AG.group_id
                                       AND D.InstanceID = AG.InstanceID
WHERE D.InstanceID = 1
ORDER BY HADR.is_local DESC,
         AG.name,
         D.name;