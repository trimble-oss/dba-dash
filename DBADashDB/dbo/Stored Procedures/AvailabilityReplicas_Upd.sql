CREATE PROC dbo.AvailabilityReplicas_Upd(
		@AvailabilityReplicas dbo.AvailabilityReplicas READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='AvailabilityReplicas'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
BEGIN TRAN;
	DELETE dbo.AvailabilityReplicas WHERE InstanceID = @InstanceID 

	INSERT INTO dbo.AvailabilityReplicas(
		   InstanceID,
		   replica_id,
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
           read_write_routing_url)
    SELECT @InstanceID,
		   replica_id,
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
           read_write_routing_url
    FROM @AvailabilityReplicas

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate;
	COMMIT;
END;