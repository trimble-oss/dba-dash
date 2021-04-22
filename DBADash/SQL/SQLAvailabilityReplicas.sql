DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT replica_id,
       group_id,
       replica_metadata_id,
       replica_server_name,
       endpoint_url,
       availability_mode,
       failover_mode,
       session_timeout,
       primary_role_allow_connections,
       secondary_role_allow_connections,
       create_date,
       modify_date,
       backup_priority,
       read_only_routing_url,
       seeding_mode,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_replicas'),'read_write_routing_url','ColumnID') IS NULL THEN ' CAST(NULL as NVARCHAR(256)) AS ' ELSE '' END + 'read_write_routing_url
FROM sys.availability_replicas;'

EXEC sp_executesql @SQL
