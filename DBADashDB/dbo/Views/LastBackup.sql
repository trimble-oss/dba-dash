CREATE VIEW dbo.LastBackup
AS
/*
	App currently only collects the last backup of each type.  This view is to take availability groups into consideration
	Thresholds are looked up once per database and the latest backup of each type is found per database (correlated) so
	that a filter on DatabaseID/InstanceID only reads the backups for the matching databases.
	IsPreviousDatabaseBackup: The backup was taken of a previous database with the same name (dropped and recreated, or another
	database's backup restored over it) - its family_guid doesn't match the database it was taken from.  Valid backups are
	ranked first, so a flagged row is only returned when there is no valid backup of that type.  A NULL GUID is treated as valid.
	(family_guid is used as database_guid is different on each availability group replica)
	The database's GUID is captured by the Databases collection, which runs separately from the Backups collection.  A backup that
	started after the GUID was last collected can't be judged (the database might have been recreated since) so it's treated as valid.
	Partner backups are also checked against this database's GUID (replicas share family_guid), so a stale DatabasesHADR link (e.g. the
	database was recreated and HADR is no longer collected) can't credit it with another database's backups.
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
       LB.compression_algorithm,
       LB.IsPreviousDatabaseBackup
FROM dbo.Databases D
OUTER APPLY(SELECT TOP(1) T.ConsiderCopyOnlyBackups,
                          T.ConsiderSnapshotBackups
			/* Most specific threshold wins: database, instance, root */
			FROM (VALUES(1,D.InstanceID,D.DatabaseID),(2,D.InstanceID,-1),(3,-1,D.DatabaseID),(4,-1,-1)) K(Precedence,InstanceID,DatabaseID)
			JOIN dbo.BackupThresholds T ON T.InstanceID = K.InstanceID AND T.DatabaseID = K.DatabaseID
			ORDER BY K.Precedence
			) AS TH
OUTER APPLY(SELECT CD.SnapshotDate AS GuidCollectedDate
			FROM dbo.CollectionDates CD
			WHERE CD.InstanceID = D.InstanceID
			AND CD.Reference = 'Databases'
			) AS DG
CROSS APPLY(SELECT B.*,
				src.PartnerDatabaseID,
				PDB.IsPreviousDatabaseBackup,
				ROW_NUMBER() OVER(PARTITION BY B.type ORDER BY PDB.IsPreviousDatabaseBackup ASC, B.backup_start_date_utc DESC) rnum
			FROM (
				/* Backups of the database itself */
				SELECT D.DatabaseID AS BackupDatabaseID,
					NULL AS PartnerDatabaseID,
					D.family_guid AS BackupDatabaseFamilyGuid,
					DG.GuidCollectedDate
				UNION ALL
				/* Backups taken on availability group partners */
				SELECT partnr.DatabaseID,
					partnr.DatabaseID,
					PD.family_guid,
					(SELECT CD.SnapshotDate FROM dbo.CollectionDates CD WHERE CD.InstanceID = PD.InstanceID AND CD.Reference = 'Databases')
				FROM dbo.DatabasesHADR hadr
				JOIN dbo.DatabasesHADR partnr ON hadr.group_database_id = partnr.group_database_id AND hadr.DatabaseID <> partnr.DatabaseID
				JOIN dbo.Databases PD ON PD.DatabaseID = partnr.DatabaseID
				WHERE hadr.DatabaseID = D.DatabaseID
				AND hadr.is_local=1
				AND partnr.is_local=1
				) src
			JOIN dbo.BackupStats B ON B.DatabaseID = src.BackupDatabaseID
			CROSS APPLY(SELECT CASE WHEN src.BackupDatabaseFamilyGuid <> B.family_guid
										AND B.backup_start_date_utc < src.GuidCollectedDate
										THEN CAST(1 AS BIT)
									/* Partner backup that doesn't belong to this database */
									WHEN src.PartnerDatabaseID IS NOT NULL
										AND D.family_guid <> B.family_guid
										AND B.backup_start_date_utc < DG.GuidCollectedDate
										THEN CAST(1 AS BIT)
									ELSE CAST(0 AS BIT) END AS IsPreviousDatabaseBackup) PDB
			WHERE NOT(TH.ConsiderCopyOnlyBackups=0 AND B.is_copy_only=1)
			AND NOT(TH.ConsiderSnapshotBackups=0 AND B.is_snapshot=1)
			) LB
WHERE LB.rnum=1