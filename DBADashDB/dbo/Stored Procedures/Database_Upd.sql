CREATE PROC dbo.Database_Upd(
	@DB SQLDB READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
/* Legacy SP. New agents call Databases_Upd directly */
DECLARE @Ref VARCHAR(30)='Databases'
EXEC dbo.Databases_Upd @Databases=@DB,@InstanceID = @InstanceID,@SnapshotDate=@SnapshotDate

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									@Reference = @Ref,
									@SnapshotDate = @SnapshotDate