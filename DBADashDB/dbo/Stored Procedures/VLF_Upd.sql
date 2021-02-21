CREATE PROC VLF_Upd(@VLF VLF READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='VLF'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	UPDATE d 
	SET d.VLFCount = vlf.VLFCount
	FROM dbo.Databases d
	JOIN @VLF vlf ON d.database_id = vlf.database_id
	WHERE d.InstanceID = @InstanceID

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
END