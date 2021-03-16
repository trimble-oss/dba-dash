CREATE PROC dbo.DatabaseMirroring_Upd(
	@DatabaseMirroring DatabaseMirroring READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='DatabaseMirroring'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN;
	
	DELETE dbo.DatabaseMirroring 
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.DatabaseMirroring(DatabaseID,
									   InstanceID,
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
									   )
	SELECT D.DatabaseID,
		D.InstanceID,
		DM.mirroring_guid,
		DM.mirroring_state,
		DM.mirroring_role,
		DM.mirroring_role_sequence,
		DM.mirroring_safety_level,
		DM.mirroring_safety_sequence,
		DM.mirroring_partner_name,
		DM.mirroring_partner_instance,
		DM.mirroring_witness_name,
		DM.mirroring_witness_state,
		DM.mirroring_failover_lsn,
		DM.mirroring_connection_timeout,
		DM.mirroring_redo_queue,
		DM.mirroring_redo_queue_type,
		DM.mirroring_end_of_log_lsn,
		DM.mirroring_replication_lsn
	FROM @DatabaseMirroring DM 
	JOIN dbo.Databases D ON DM.database_id = D.database_id AND D.InstanceID = @InstanceID

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate
END

