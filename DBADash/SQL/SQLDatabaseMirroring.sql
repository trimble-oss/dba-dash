DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT database_id,
		mirroring_guid,
		mirroring_state,
		mirroring_role,
		mirroring_role_sequence,
		mirroring_safety_level,
		mirroring_safety_sequence,
		mirroring_partner_name,
		mirroring_partner_instance,
		mirroring_witness_name,
		mirroring_witness_state,
		mirroring_failover_lsn,
		mirroring_connection_timeout,
		mirroring_redo_queue,
		mirroring_redo_queue_type,
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_mirroring'),'mirroring_end_of_log_lsn','ColumnId') IS NULL THEN 'CAST(NULL AS DECIMAL(25,0)) AS mirroring_end_of_log_lsn,' ELSE 'mirroring_end_of_log_lsn,' END + '
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_mirroring'),'mirroring_replication_lsn','ColumnId') IS NULL THEN 'CAST(NULL AS DECIMAL(25,0)) AS mirroring_replication_lsn' ELSE 'mirroring_replication_lsn' END + '		
FROM sys.database_mirroring
WHERE mirroring_state IS NOT NULL' 

EXEC sp_executesql @SQL
