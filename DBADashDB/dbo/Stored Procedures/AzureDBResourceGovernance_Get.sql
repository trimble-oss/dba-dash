CREATE PROC dbo.AzureDBResourceGovernance_Get(
		@InstanceIDs VARCHAR(MAX)=NULL,
        @ShowHidden BIT=1
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

SELECT RG.InstanceID,
       RG.ValidFrom,
       RG.database_id,
       RG.logical_database_guid,
       RG.physical_database_guid,
       RG.server_name,
       RG.database_name,
       RG.slo_name,
       RG.dtu_limit,
       RG.cpu_limit,
       RG.min_cpu,
       RG.max_cpu,
       RG.cap_cpu,
       RG.min_cores,
       RG.max_dop,
       RG.min_memory,
       RG.max_memory,
       RG.max_sessions,
       RG.max_memory_grant,
       RG.max_db_memory,
       RG.govern_background_io,
       RG.min_db_max_size_in_mb,
       RG.max_db_max_size_in_mb,
       RG.default_db_max_size_in_mb,
       RG.db_file_growth_in_mb,
       RG.initial_db_file_size_in_mb,
       RG.log_size_in_mb,
       RG.instance_cap_cpu,
       RG.instance_max_log_rate,
       RG.instance_max_worker_threads,
       RG.replica_type,
       RG.max_transaction_size,
       RG.checkpoint_rate_mbps,
       RG.checkpoint_rate_io,
       RG.last_updated_date_utc,
       RG.primary_group_id,
       RG.primary_group_max_workers,
       RG.primary_min_log_rate,
       RG.primary_max_log_rate,
       RG.primary_group_min_io,
       RG.primary_group_max_io,
       RG.primary_group_min_cpu,
       RG.primary_group_max_cpu,
       RG.primary_log_commit_fee,
       RG.primary_pool_max_workers,
       RG.pool_max_io,
       RG.govern_db_memory_in_resource_pool,
       RG.volume_local_iops,
       RG.volume_managed_xstore_iops,
       RG.volume_external_xstore_iops,
       RG.volume_type_local_iops,
       RG.volume_type_managed_xstore_iops,
       RG.volume_type_external_xstore_iops,
       RG.volume_pfs_iops,
       RG.volume_type_pfs_iops,
       RG.user_data_directory_space_quota_mb,
       RG.user_data_directory_space_usage_mb		 
FROM dbo.AzureDBResourceGovernance RG
JOIN dbo.Instances I ON RG.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 
				FROM @Instances I 
				WHERE I.InstanceID = RG.InstanceID
				)
AND (I.ShowInSummary=1 OR @ShowHidden=1)