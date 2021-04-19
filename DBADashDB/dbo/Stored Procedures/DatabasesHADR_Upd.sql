CREATE PROC [dbo].[DatabasesHADR_Upd](@DatabasesHADR DatabasesHADR READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
SET XACT_ABORT ON;
DECLARE @Ref VARCHAR(30)='DatabaseHADR'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
	BEGIN
	BEGIN TRAN;
	DELETE hadr
	FROM dbo.DatabasesHADR hadr
	WHERE EXISTS
	(
		SELECT 1
		FROM dbo.Databases D
		WHERE D.DatabaseID = hadr.DatabaseID
		AND   D.InstanceID = @InstanceID
	);
	INSERT INTO dbo.DatabasesHADR
	(
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
		secondary_lag_seconds
	)
	SELECT d.DatabaseID,
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
		   hadr.secondary_lag_seconds
	FROM @DatabasesHADR hadr
		JOIN dbo.Databases d ON hadr.database_id = d.database_id
	WHERE d.InstanceID = @InstanceID;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT;
END