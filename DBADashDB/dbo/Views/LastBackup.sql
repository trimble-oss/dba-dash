CREATE VIEW dbo.LastBackup
AS
/*
	App currently only collects the last backup of each type.  This view is to take availability groups into consideration
*/
WITH hadr AS (
	SELECT D.DatabaseID,
		partnr.DatabaseID BackupDatabaseID,
		partnr.DatabaseID PartnerDatabaseID,
        D.InstanceID
	FROM dbo.Databases D
	JOIN dbo.DatabasesHADR hadr ON D.DatabaseID = hadr.DatabaseID
	JOIN dbo.DatabasesHADR partnr ON hadr.group_database_id = partnr.group_database_id AND D.DatabaseID <> partnr.DatabaseID 
	WHERE hadr.is_local=1
	AND partnr.is_local=1
	UNION ALL
	SELECT D.DatabaseID,
		D.DatabaseID,
		NULL as PartnerDatabaseID,
        D.InstanceID
	FROM dbo.Databases D
),
LastBackup AS (
	SELECT  hadr.InstanceID,
            hadr.DatabaseID,
            B.type,
            B.backup_type_desc,
            B.backup_start_date,
            B.backup_finish_date,
            B.backup_set_id,
            B.time_zone,
            B.backup_size,
            B.is_password_protected,
            B.recovery_model,
            B.has_bulk_logged_data,
            B.is_snapshot,
            B.is_readonly,
            B.is_single_user,
            B.has_backup_checksums,
            B.is_damaged,
            B.has_incomplete_metadata,
            B.is_force_offline,
            B.is_copy_only,
            B.database_guid,
            B.family_guid,
            B.compressed_backup_size,
            B.key_algorithm,
            B.encryptor_type,
            B.backup_start_date_utc,
            B.backup_finish_date_utc,
            B.BackupDurationSec,
            B.BackupDuration,
            B.BackupMBsec,
            B.BackupWriteMBsec,
            B.IsCompressed,
            B.BackupSizeGB,
            B.BackupSizeCompressedGB,
            B.CompressionSavingPct,
			B.IsEncrypted,
			CASE WHEN hadr.PartnerDatabaseID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsPartnerBackup,
			hadr.PartnerDatabaseID,
            B.compression_algorithm,
			ROW_NUMBER() OVER(PARTITION BY hadr.DatabaseID,B.type ORDER BY B.backup_start_date_utc DESC) rnum
	FROM dbo.BackupStats B
	JOIN hadr ON hadr.BackupDatabaseID = B.DatabaseID
    OUTER APPLY(SELECT TOP(1) T.ConsiderCopyOnlyBackups,
                              T.ConsiderSnapshotBackups
			FROM dbo.BackupThresholds T 
			WHERE (hadr.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (hadr.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY T.InstanceID DESC,T.DatabaseID DESC
			) AS TH
    WHERE NOT(TH.ConsiderCopyOnlyBackups=0 AND B.is_copy_only=1)
    AND NOT(TH.ConsiderSnapshotBackups=0 AND B.is_snapshot=1)
)
SELECT B.DatabaseID,
       B.type,
       B.backup_type_desc,
       B.backup_start_date,
       B.backup_finish_date,
       B.backup_set_id,
       B.time_zone,
       B.backup_size,
       B.is_password_protected,
       B.recovery_model,
       B.has_bulk_logged_data,
       B.is_snapshot,
       B.is_readonly,
       B.is_single_user,
       B.has_backup_checksums,
       B.is_damaged,
       B.has_incomplete_metadata,
       B.is_force_offline,
       B.is_copy_only,
       B.database_guid,
       B.family_guid,
       B.compressed_backup_size,
       B.key_algorithm,
       B.encryptor_type,
       B.backup_start_date_utc,
       B.backup_finish_date_utc,
       B.BackupDurationSec,
       B.BackupDuration,
       B.BackupMBsec,
       B.BackupWriteMBsec,
       B.IsCompressed,
       B.BackupSizeGB,
       B.BackupSizeCompressedGB,
       B.CompressionSavingPct,
	   B.IsEncrypted,
	   B.IsPartnerBackup,
	   B.PartnerDatabaseID,
       B.compression_algorithm
FROM LastBackup B
WHERE rnum=1