CREATE PROC [dbo].[Corruption_Upd](@Corruption dbo.Corruption READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='Corruption'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	DELETE C 
	FROM dbo.Corruption C
	WHERE EXISTS(SELECT 1 FROM dbo.Databases D WHERE D.InstanceID = @InstanceID AND D.DatabaseID = C.DatabaseID)

	INSERT INTO dbo.Corruption
	(
		DatabaseID,
		SourceTable,
		UpdateDate
	)
	SELECT D.DatabaseID, C.SourceTable,C.last_update_date 
	FROM @Corruption C 
	JOIN dbo.Databases D ON C.database_id = D.database_id AND D.InstanceID  = @InstanceID
	WHERE D.IsActive=1

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

END