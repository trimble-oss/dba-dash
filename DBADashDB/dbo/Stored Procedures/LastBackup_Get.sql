CREATE PROC dbo.LastBackup_Get(
	@DatabaseID INT
)
AS
SELECT LB.DatabaseID,
	   d.name,
       LB.type,
       LB.backup_type_desc,
       LB.backup_start_date_utc,
       LB.backup_finish_date_utc,
	   LB.BackupSizeGB,
       LB.BackupSizeCompressedGB,
       LB.CompressionSavingPct,
	   LB.BackupDurationSec,
       LB.BackupDuration,
	   LB.BackupMBsec,
       LB.BackupWriteMBsec,
       LB.is_password_protected,
       LB.recovery_model,
       LB.has_bulk_logged_data,
       LB.is_snapshot,
       LB.is_readonly,
       LB.is_single_user,
       LB.has_backup_checksums,
       LB.is_damaged,
       LB.has_incomplete_metadata,
       LB.is_force_offline,
       LB.is_copy_only,
       LB.key_algorithm,
       LB.encryptor_type,
       LB.IsCompressed,
       LB.IsEncrypted,
       LB.IsPartnerBackup,
       PDBI.Instance as Partner,
       LB.compression_algorithm
FROM dbo.LastBackup LB
JOIN dbo.Databases D ON D.DatabaseID = LB.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.Databases PDB ON LB.PartnerDatabaseID = PDB.DatabaseID
LEFT JOIN dbo.Instances PDBI ON PDB.InstanceID = PDBI.InstanceID
WHERE D.DatabaseID=@DatabaseID
ORDER BY LB.backup_start_date DESC