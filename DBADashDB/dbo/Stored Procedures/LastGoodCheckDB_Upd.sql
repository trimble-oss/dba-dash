CREATE PROC LastGoodCheckDB_Upd(
		@LastGoodCheckDB as dbo.LastGoodCheckDB READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='LastGoodCheckDB'
IF NOT EXISTS(
	SELECT * 
	FROM CollectionDates CD 
	WHERE CD.Reference = @Ref 
	AND InstanceID=@InstanceID
	AND CD.SnapshotDate>=@SnapshotDate
	)
BEGIN
	UPDATE D 
	SET D.LastGoodCheckDbTime = cdb.LastGoodCheckDbTime
	FROM dbo.Databases D 
	JOIN @LastGoodCheckDB cdb ON D.database_id = cdb.database_id
	WHERE D.InstanceID = @InstanceID
	AND D.IsActive=1

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
											@Reference = @Ref,
											@SnapshotDate = @SnapshotDate

END