DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT AR.replica_id,
       AR.group_id,
       AR.replica_metadata_id,
       AR.replica_server_name,
       AR.endpoint_url,
       AR.availability_mode,
       AR.failover_mode,
       AR.session_timeout,
       AR.primary_role_allow_connections,
       AR.secondary_role_allow_connections,
       AR.create_date,
       AR.modify_date,
       AR.backup_priority,
       AR.read_only_routing_url,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_replicas'),'seeding_mode','ColumnID') IS NULL THEN ' CAST(NULL as TINYINT) AS ' ELSE 'AR.' END + 'seeding_mode,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_replicas'),'read_write_routing_url','ColumnID') IS NULL THEN ' CAST(NULL as NVARCHAR(256)) AS ' ELSE 'AR.' END + 'read_write_routing_url,
       RS.is_local,
       RS.role,
       RS.operational_state,
       RS.connected_state,
       RS.recovery_health,
       RS.synchronization_health
FROM sys.availability_replicas AR
LEFT JOIN sys.dm_hadr_availability_replica_states RS ON RS.replica_id = AR.replica_id;'

EXEC sp_executesql @SQL
