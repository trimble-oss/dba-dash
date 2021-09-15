CREATE PROC dbo.LogRestores_Upd (
	@LogRestores LogRestores READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='LogRestores'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
BEGIN TRAN

	DELETE L 
	FROM dbo.LogRestores  L
	WHERE EXISTS(SELECT 1 FROM dbo.Databases D WHERE InstanceID = @InstanceID AND D.DatabaseID = L.DatabaseID)

	INSERT INTO dbo.LogRestores(
			DatabaseID,
			restore_date,
			backup_start_date,
			last_file,
			backup_time_zone
	)
	SELECT D.DatabaseID,
			L.restore_date,
			L.backup_start_date,
			L.last_file,
			L.backup_time_zone
	FROM @LogRestores L 
	JOIN dbo.Databases D ON d.name = L.database_name AND D.InstanceID= @InstanceID
	WHERE D.IsActive=1


	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

	COMMIT
END