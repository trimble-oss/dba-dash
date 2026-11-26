SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
DECLARE @SQL NVARCHAR(MAX)

/*	Backup history is attributed to a current database by name, with family_guid used to confirm the match:
	- DB_ID resolves the name in the session's context, so in a contained AG the node names of its system databases (<AG>_master, <AG>_msdb)
	  resolve to master/msdb.
	- The history is dropped if both GUIDs are known and differ (history of a previous database with the same name - dropped and recreated,
	  or another database's backup restored over it).  A NULL GUID (offline database or old history) can't disprove the name match.
	- History is never given to a database with a different name by GUID alone.  Restored copies share family_guid, so the backups of a
	  dropped copy could otherwise be attributed to the database it was restored from.
	family_guid is used rather than database_guid: database_guid is different on each availability group replica, so it changes when a
	contained AG fails over.  family_guid is shared by the replicas, and changes when a database is recreated or another database's backup
	is restored over it.
	The name is returned as it appears in sys.databases, matching the Databases collection.

	Contained AG: msdb belongs to the AG and fails over with it, so it holds history for backups taken on any replica.  History from all the
	AG's replicas is included, not just the current server.
*/
DECLARE @ContainedAGID UNIQUEIDENTIFIER
IF COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_sessions'),'contained_availability_group_id','ColumnID') IS NOT NULL
	AND OBJECT_ID('sys.availability_replicas') IS NOT NULL /* AzureDB has contained_availability_group_id column but not the AG DMVs */
BEGIN
	EXEC sp_executesql N'SELECT @ContainedAGID = contained_availability_group_id FROM sys.dm_exec_sessions WHERE session_id = @@SPID',
		N'@ContainedAGID UNIQUEIDENTIFIER OUT',
		@ContainedAGID OUT
END

CREATE TABLE #BackupDBs(
	database_id INT NOT NULL PRIMARY KEY,
	name SYSNAME NOT NULL,
	family_guid UNIQUEIDENTIFIER NULL
)

INSERT INTO #BackupDBs(database_id, name, family_guid)
SELECT	d.database_id,
		d.name,
		rs.family_guid
FROM sys.databases d
LEFT JOIN sys.database_recovery_status rs ON rs.database_id = d.database_id

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
	/*	Backups are ranked by UTC time.  backup_finish_date is the local time of the server that took the backup, and a contained AG''s history
		can come from replicas in different time zones.  time_zone is the local time''s offset from UTC in 15 minute intervals (127 = unknown),
		so UTC = local time - time_zone * 15 minutes.  e.g. UTC+2: time_zone = 8, 12:00 local = 10:00 UTC
		backup_finish_date is to the second, so backups that finish in the same second are ranked by backup_set_id (the order msdb recorded them). */
	DATEADD(mi,-15*ISNULL(NULLIF(time_zone,127),0),backup_finish_date) AS backup_finish_date_utc,
	ROW_NUMBER() OVER(PARTITION BY family_guid,database_name,type,is_snapshot,is_copy_only ORDER BY DATEADD(mi,-15*ISNULL(NULLIF(time_zone,127),0),backup_finish_date) DESC,backup_set_id DESC) rnum
	FROM msdb.dbo.backupset bs
	WHERE (server_name=CAST(SERVERPROPERTY(''SERVERNAME'') AS NVARCHAR(128)) COLLATE SQL_Latin1_General_CP1_CI_AS
		' + CASE WHEN @ContainedAGID IS NOT NULL THEN 'OR server_name COLLATE SQL_Latin1_General_CP1_CI_AS IN(SELECT AR.replica_server_name COLLATE SQL_Latin1_General_CP1_CI_AS FROM sys.availability_replicas AR WHERE AR.group_id = @ContainedAGID)' ELSE '' END + '
		)
	AND backup_finish_date>=DATEADD(d,-10,GETUTCDATE())
),
/* Map the latest backup of each database name/GUID to a current database. DB_ID resolves the name using the server rules for database names. */
M AS (
	SELECT	n.database_id AS mapped_database_id,
			T.*
	FROM T
	JOIN #BackupDBs n ON n.database_id = DB_ID(T.database_name)
	WHERE T.rnum = 1
	AND (n.family_guid IS NULL OR T.family_guid IS NULL OR n.family_guid = T.family_guid)
),
/* More than one history name can map to the same database (e.g. <AG>_master and master).  Keep the latest. */
L AS (
	SELECT	M.*,
			ROW_NUMBER() OVER(PARTITION BY M.mapped_database_id,M.type,M.is_snapshot,M.is_copy_only ORDER BY M.backup_finish_date_utc DESC,M.backup_set_id DESC) AS mapped_rnum
	FROM M
)
SELECT	d.name AS database_name,
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
		L.family_guid,
		compressed_backup_size,
		key_algorithm,
		encryptor_type,
		compression_algorithm
FROM L
JOIN #BackupDBs d ON d.database_id = L.mapped_database_id
WHERE L.mapped_rnum = 1'

EXEC sp_executesql @SQL, N'@ContainedAGID UNIQUEIDENTIFIER', @ContainedAGID

DROP TABLE #BackupDBs