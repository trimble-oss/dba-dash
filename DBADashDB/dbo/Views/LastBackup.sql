CREATE VIEW dbo.LastBackup
AS
/*
	App currently only collects the last backup of each type.  This view is to take availability groups into consideration
	Thresholds are looked up once per database and the latest backup of each type is found per database (correlated) so
	that a filter on DatabaseID/InstanceID only reads the backups for the matching databases.
*/
SELECT D.DatabaseID,
       LB.type,
       LB.backup_type_desc,
       LB.backup_start_date,
       LB.backup_finish_date,
       LB.backup_set_id,
       LB.time_zone,
       LB.backup_size,
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
       LB.database_guid,
       LB.family_guid,
       LB.compressed_backup_size,
       LB.key_algorithm,
       LB.encryptor_type,
       LB.backup_start_date_utc,
       LB.backup_finish_date_utc,
       LB.BackupDurationSec,
       LB.BackupDuration,
       LB.BackupMBsec,
       LB.BackupWriteMBsec,
       LB.IsCompressed,
       LB.BackupSizeGB,
       LB.BackupSizeCompressedGB,
       LB.CompressionSavingPct,
	   LB.IsEncrypted,
	   CASE WHEN LB.PartnerDatabaseID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsPartnerBackup,
	   LB.PartnerDatabaseID,
       LB.compression_algorithm
FROM dbo.Databases D
OUTER APPLY(SELECT TOP(1) T.ConsiderCopyOnlyBackups,
                          T.ConsiderSnapshotBackups
			/* Most specific threshold wins: database, instance, root */
			FROM (VALUES(1,D.InstanceID,D.DatabaseID),(2,D.InstanceID,-1),(3,-1,D.DatabaseID),(4,-1,-1)) K(Precedence,InstanceID,DatabaseID)
			JOIN dbo.BackupThresholds T ON T.InstanceID = K.InstanceID AND T.DatabaseID = K.DatabaseID
			ORDER BY K.Precedence
			) AS TH
CROSS APPLY(SELECT B.*,
				src.PartnerDatabaseID,
				ROW_NUMBER() OVER(PARTITION BY B.type ORDER BY B.backup_start_date_utc DESC) rnum
			FROM (
				/* Backups of the database itself */
				SELECT D.DatabaseID AS BackupDatabaseID,
					NULL AS PartnerDatabaseID
				UNION ALL
				/* Backups taken on availability group partners */
				SELECT partnr.DatabaseID,
					partnr.DatabaseID
				FROM dbo.DatabasesHADR hadr
				JOIN dbo.DatabasesHADR partnr ON hadr.group_database_id = partnr.group_database_id AND hadr.DatabaseID <> partnr.DatabaseID
				WHERE hadr.DatabaseID = D.DatabaseID
				AND hadr.is_local=1
				AND partnr.is_local=1
				) src
			JOIN dbo.BackupStats B ON B.DatabaseID = src.BackupDatabaseID
			WHERE NOT(TH.ConsiderCopyOnlyBackups=0 AND B.is_copy_only=1)
			AND NOT(TH.ConsiderSnapshotBackups=0 AND B.is_snapshot=1)
			) LB
WHERE LB.rnum=1