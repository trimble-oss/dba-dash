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
       mirroring_end_of_log_lsn,
       mirroring_replication_lsn
FROM sys.database_mirroring
WHERE mirroring_state IS NOT NULL