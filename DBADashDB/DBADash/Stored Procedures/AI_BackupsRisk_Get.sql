CREATE PROC DBADash.AI_BackupsRisk_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
SELECT TOP (@MaxRows)
	bs.InstanceDisplayName,
	bs.name AS DatabaseName,
	bs.recovery_model_desc AS RecoveryModel,
	bs.BackupStatus,
	bs.FullBackupStatus,
	bs.DiffBackupStatus,
	bs.LogBackupStatus,
	bs.LastFull,
	bs.LastDiff,
	bs.LastLog,
	bs.SnapshotAge,
	bs.FullBackupSizeGB,
	bs.FullBackupSizeCompressedGB,
	bs.LastFullDurationSec,
	bs.FullBackupMBsec,
	bs.IsFullChecksum,
	bs.IsDiffChecksum,
	bs.IsLogChecksum,
	bs.IsFullEncrypted,
	bs.IsFullCompressed,
	bs.IsFullDamaged,
	bs.IsDiffDamaged,
	bs.IsLogDamaged
FROM dbo.BackupStatus bs
WHERE bs.BackupStatus IN (1,2)
  AND (@InstanceFilter IS NULL OR bs.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY bs.BackupStatus ASC, bs.SnapshotAge DESC
