CREATE VIEW dbo.BackupStats
AS
/*
	Add some calculated data to Backups table
*/
SELECT 		B.DatabaseID,
            B.type,
			CASE B.type WHEN 'D' THEN 'FULL' WHEN 'I' THEN 'DIFF' WHEN 'L' THEN 'LOG' WHEN 'F' THEN 'FILE/FILEGROUP' WHEN 'G' THEN 'FILE/FILEGROUP DIFF' WHEN 'P' THEN 'PARTIAL' WHEN 'Q' THEN 'PARTIAL DIFF' ELSE NULL END AS backup_type_desc,
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
			calc.backup_start_date_utc,
			calc.backup_finish_date_utc,
			calc.BackupDurationSec,
			HD.HumanDuration AS BackupDuration,
			B.backup_size/POWER(1024.0,2)/ISNULL(NULLIF(DATEDIFF(s,B.backup_start_date,B.backup_finish_date),0),1) BackupMBsec,
			B.compressed_backup_size/POWER(1024.0,2)/ISNULL(NULLIF(DATEDIFF(s,B.backup_start_date,B.backup_finish_date),0),1) BackupWriteMBsec,
			CASE WHEN B.backup_size = B.compressed_backup_size OR B.compressed_backup_size IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsCompressed,
			B.backup_size/POWER(1024.0,3) AS BackupSizeGB,
			B.compressed_backup_size/POWER(1024.0,3) AS BackupSizeCompressedGB,
			1.0-((B.compressed_backup_size*1.0)/NULLIF(B.backup_size,0)) AS CompressionSavingPct,
			CASE WHEN B.encryptor_type IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsEncrypted,
            B.compression_algorithm
FROM dbo.Backups B
OUTER APPLY (SELECT DATEADD(mi,ISNULL(NULLIF(-B.time_zone,127)*15,0),B.backup_start_date) AS backup_start_date_utc,
				DATEADD(mi,ISNULL(NULLIF(-B.time_zone,127)*15,0),B.backup_finish_date) AS backup_finish_date_utc,
					DATEDIFF(s,B.backup_start_date,B.backup_finish_date) AS BackupDurationSec) calc
OUTER APPLY dbo.SecondsToHumanDuration(calc.BackupDurationSec) HD