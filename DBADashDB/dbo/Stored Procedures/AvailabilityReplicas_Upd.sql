CREATE PROC dbo.AvailabilityReplicas_Upd(
		@AvailabilityReplicas dbo.AvailabilityReplicas READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON;
DECLARE @Ref VARCHAR(30)='AvailabilityReplicas'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	DECLARE @PreviousSnapshotDate DATETIME2(2)
	SELECT @PreviousSnapshotDate = SnapshotDate
	FROM dbo.CollectionDates
	WHERE InstanceID = @InstanceID
	AND Reference = @Ref

BEGIN TRAN;
	/*	Record changes to the role of the local replica.  Each instance reports the role of its own replica so a failover
		is recorded on the old primary (PRIMARY => SECONDARY) and on the new primary (SECONDARY => PRIMARY) if both are monitored.
		The role change happened between the previous snapshot and this one.
	*/
	INSERT INTO dbo.AvailabilityGroupRoleChanges(
		InstanceID,
		group_id,
		replica_id,
		AvailabilityGroupName,
		ReplicaServerName,
		PreviousRole,
		NewRole,
		ChangedAfter,
		ChangedBefore,
		Source
	)
	SELECT	@InstanceID,
			New.group_id,
			New.replica_id,
			AG.name,
			New.replica_server_name,
			Old.role,
			New.role,
			@PreviousSnapshotDate,
			@SnapshotDate,
			'Snapshot'
	FROM dbo.AvailabilityReplicas Old
	JOIN @AvailabilityReplicas New ON Old.replica_id = New.replica_id
	LEFT JOIN dbo.AvailabilityGroups AG ON AG.InstanceID = @InstanceID AND AG.group_id = New.group_id
	WHERE Old.InstanceID = @InstanceID
	AND Old.is_local = 1
	AND New.is_local = 1
	AND Old.role <> New.role

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
           read_write_routing_url,
           is_local,
           role,
           operational_state,
           connected_state,
           recovery_health,
           synchronization_health)
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
           read_write_routing_url,
           is_local,
           role,
           operational_state,
           connected_state,
           recovery_health,
           synchronization_health
    FROM @AvailabilityReplicas

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate;
	COMMIT;
END;
