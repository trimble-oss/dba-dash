CREATE PROC [dbo].[Backups_Upd](
	@Backups Backups READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='Backups'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	MERGE dbo.Backups as T
	USING (SELECT B.LastBackup,B.type,d.DatabaseID 
		FROM @Backups B 
		JOIN dbo.Databases d ON d.name = B.database_name
		AND d.IsActive=1
		AND D.InstanceID=@InstanceID) as S
		ON S.DatabaseID = T.DatabaseID AND S.type=T.type
	WHEN MATCHED THEN
	UPDATE SET T.LastBackup = S.LastBackup
	WHEN NOT MATCHED BY TARGET THEN
	INSERT (DatabaseID,type,LastBackup)
	VALUES(DatabaseID,type,LastBackup);

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
END
	