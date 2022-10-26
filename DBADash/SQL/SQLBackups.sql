DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WITH T AS (
	SELECT database_name,
	type,
	backup_start_date,
	backup_finish_date,
	backup_set_id,
	time_zone,
	backup_size,
	is_password_protected,
	recovery_model,
	has_bulk_logged_data,
	is_snapshot,
	is_readonly,
	is_single_user,
	has_backup_checksums,
	is_damaged,
	has_incomplete_metadata,
	is_force_offline,
	is_copy_only,
	database_guid,
	family_guid,
	' + CASE WHEN NOT EXISTS(SELECT 1 FROM msdb.sys.columns WHERE object_id = OBJECT_ID('msdb.dbo.backupset') AND name = 'compressed_backup_size') THEN 'CAST(NULL AS NUMERIC(20,0)) AS compressed_backup_size,' ELSE 'compressed_backup_size,' END + '
	' + CASE WHEN NOT EXISTS(SELECT 1 FROM msdb.sys.columns WHERE object_id = OBJECT_ID('msdb.dbo.backupset') AND name = 'key_algorithm') THEN 'CAST(NULL AS NVARCHAR(32)) AS key_algorithm,' ELSE 'key_algorithm,' END + '
	' + CASE WHEN NOT EXISTS(SELECT 1 FROM msdb.sys.columns WHERE object_id = OBJECT_ID('msdb.dbo.backupset') AND name = 'encryptor_type') THEN 'CAST(NULL AS NVARCHAR(32)) AS encryptor_type,' ELSE 'encryptor_type,' END + '
	' + CASE WHEN NOT EXISTS(SELECT 1 FROM msdb.sys.columns WHERE object_id = OBJECT_ID('msdb.dbo.backupset') AND name = 'compression_algorithm') THEN 'CAST(NULL AS NVARCHAR(32)) AS compression_algorithm,' ELSE 'compression_algorithm,' END + '
	ROW_NUMBER() OVER(PARTITION BY database_name,type ORDER BY backup_finish_date DESC) rnum
	FROM msdb.dbo.backupset bs
	WHERE server_name=CAST(SERVERPROPERTY(''SERVERNAME'') AS NVARCHAR(128)) COLLATE SQL_Latin1_General_CP1_CI_AS
	AND backup_finish_date>=DATEADD(d,-10,GETUTCDATE())
)
SELECT	database_name,
		type,
		backup_start_date,
		backup_finish_date,
		backup_set_id,
		time_zone,
		backup_size,
		is_password_protected,
		recovery_model,
		has_bulk_logged_data,
		is_snapshot,
		is_readonly,
		is_single_user,
		has_backup_checksums,
		is_damaged,
		has_incomplete_metadata,
		is_force_offline,
		is_copy_only,
		database_guid,
		family_guid,
		compressed_backup_size,
		key_algorithm,
		encryptor_type,
		compression_algorithm
FROM T
WHERE rnum=1'

EXEC sp_executesql @SQL