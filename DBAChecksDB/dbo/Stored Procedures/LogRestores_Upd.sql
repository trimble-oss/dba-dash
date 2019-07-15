CREATE PROC [dbo].[LogRestores_Upd] (
	@LogRestores LogRestores READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME
)
AS
BEGIN TRAN
DELETE L 
FROM dbo.LogRestores  L
WHERE EXISTS(SELECT 1 FROM dbo.Databases D WHERE InstanceID = @InstanceID AND D.DatabaseID = L.DatabaseID)

INSERT INTO dbo.LogRestores(DatabaseID,restore_date,backup_start_date,last_file)
SELECT DatabaseID,restore_date,backup_start_date,last_file
FROM @LogRestores L 
JOIN dbo.Databases D ON d.name = L.database_name AND D.InstanceID= @InstanceID
WHERE D.IsActive=1

UPDATE dbo.SnapshotDates
SET LogRestoresDate = @SnapshotDate
WHERE InstanceID = @InstanceID

COMMIT