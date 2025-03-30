CREATE PROC dbo.DatabasesHADR_Upd(
		@DatabasesHADR DatabasesHADR READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON;
DECLARE @Ref VARCHAR(30)='DatabaseHADR'
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
			 )
BEGIN
	BEGIN TRAN;

	DELETE hadr
	FROM dbo.DatabasesHADR hadr
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.DatabasesHADR
	(
		InstanceID,
		DatabaseID,
		group_database_id,
		is_primary_replica,
		synchronization_state,
		synchronization_health,
		is_suspended,
		suspend_reason,
		replica_id,
		group_id,
		is_commit_participant,
		database_state,
		is_local,
		secondary_lag_seconds,
		last_sent_time,
		last_received_time,
		last_hardened_time,
		last_redone_time,
		log_send_queue_size,
		log_send_rate,
		redo_queue_size,
		redo_rate,	
		filestream_send_rate,
		last_commit_time
	)
	SELECT @InstanceID,
		   d.DatabaseID,
		   hadr.group_database_id,
		   hadr.is_primary_replica,
		   hadr.synchronization_state,
		   hadr.synchronization_health,
		   hadr.is_suspended,
		   hadr.suspend_reason,
		   ISNULL(hadr.replica_id,'00000000-0000-0000-0000-000000000000'),
		   hadr.group_id,
		   hadr.is_commit_participant,
		   hadr.database_state,
		   hadr.is_local,
		   hadr.secondary_lag_seconds,
		   hadr.last_sent_time,
		   hadr.last_received_time,
		   hadr.last_hardened_time,
		   hadr.last_redone_time,
		   hadr.log_send_queue_size,
		   hadr.log_send_rate,
		   hadr.redo_queue_size,
		   hadr.redo_rate,	
		   hadr.filestream_send_rate,
		   hadr.last_commit_time
	FROM @DatabasesHADR hadr
		JOIN dbo.Databases d ON hadr.database_id = d.database_id
	WHERE d.InstanceID = @InstanceID
	AND d.IsActive=1;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT;
END