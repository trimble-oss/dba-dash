CREATE PROC [dbo].[LogRestores_Upd] (
	@LogRestores LogRestores READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME
)
AS
BEGIN TRAN
DELETE L 
FROM dbo.LogRestores  L
WHERE EXISTS(SELECT 1 FROM Databases D WHERE InstanceID = @InstanceID AND D.DatabaseID = L.DatabaseID)

INSERT INTO LogRestores(DatabaseID,restore_date,backup_start_date,last_file)
SELECT DatabaseID,restore_date,backup_start_date,last_file
FROM @LogRestores L 
JOIN dbo.Databases D ON d.name = L.database_name AND D.InstanceID= @InstanceID

UPDATE dbo.Instances
SET LogRestoreSnapshotDate = @SnapshotDate
WHERE InstanceID = @InstanceID

COMMIT