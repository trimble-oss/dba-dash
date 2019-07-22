CREATE PROC [dbo].[Backups_Upd](
	@Backups Backups READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
IF NOT EXISTS(SELECT 1 FROM dbo.SnapshotDates WHERE BackupsDate >=@SnapshotDate AND InstanceID=@InstanceID)
BEGIN
	MERGE dbo.Backups as T
	USING (SELECT B.LastBackup,B.type,d.DatabaseID FROM @Backups B JOIN dbo.Databases d ON d.name = B.database_name AND d.IsActive=1) as S
		ON S.DatabaseID = T.DatabaseID AND S.type=T.type
	WHEN MATCHED THEN
	UPDATE SET T.LastBackup = S.LastBackup
	WHEN NOT MATCHED BY TARGET THEN
	INSERT (DatabaseID,type,LastBackup)
	VALUES(DatabaseID,type,LastBackup);

	UPDATE dbo.SnapshotDates
	SET BackupsDate=@SnapshotDate
	WHERE InstanceID=@InstanceID
END
	