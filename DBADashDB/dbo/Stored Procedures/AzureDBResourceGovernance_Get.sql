CREATE PROC dbo.AzureDBResourceGovernance_Get(
		@InstanceIDs VARCHAR(MAX)=NULL
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
	AND EngineEdition = 5 -- AzureDB 
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

SELECT InstanceID,
       ValidFrom,
       database_id,
       logical_database_guid,
       physical_database_guid,
       server_name,
       database_name,
       slo_name,
       dtu_limit,
       cpu_limit,
       min_cpu,
       max_cpu,
       cap_cpu,
       min_cores,
       max_dop,
       min_memory,
       max_memory,
       max_sessions,
       max_memory_grant,
       max_db_memory,
       govern_background_io,
       min_db_max_size_in_mb,
       max_db_max_size_in_mb,
       default_db_max_size_in_mb,
       db_file_growth_in_mb,
       initial_db_file_size_in_mb,
       log_size_in_mb,
       instance_cap_cpu,
       instance_max_log_rate,
       instance_max_worker_threads,
       replica_type,
       max_transaction_size,
       checkpoint_rate_mbps,
       checkpoint_rate_io,
       last_updated_date_utc,
       primary_group_id,
       primary_group_max_workers,
       primary_min_log_rate,
       primary_max_log_rate,
       primary_group_min_io,
       primary_group_max_io,
       primary_group_min_cpu,
       primary_group_max_cpu,
       primary_log_commit_fee,
       primary_pool_max_workers,
       pool_max_io,
       govern_db_memory_in_resource_pool,
       volume_local_iops,
       volume_managed_xstore_iops,
       volume_external_xstore_iops,
       volume_type_local_iops,
       volume_type_managed_xstore_iops,
       volume_type_external_xstore_iops,
       volume_pfs_iops,
       volume_type_pfs_iops,
       user_data_directory_space_quota_mb,
       user_data_directory_space_usage_mb		 
FROM dbo.AzureDBResourceGovernance RG
WHERE EXISTS(SELECT 1 
				FROM @Instances I 
				WHERE I.InstanceID = RG.InstanceID
				)