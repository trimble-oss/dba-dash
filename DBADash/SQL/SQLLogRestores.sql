WITH t
AS (SELECT rsh.destination_database_name AS database_name,
           rsh.restore_date,
           bs.backup_start_date,
           bmf.physical_device_name AS last_file,
           ROW_NUMBER() OVER (PARTITION BY rsh.destination_database_name
                              ORDER BY rsh.restore_date DESC
                             ) rnum
    FROM msdb.dbo.restorehistory rsh
        INNER JOIN msdb.dbo.backupset bs ON rsh.backup_set_id = bs.backup_set_id
        INNER JOIN msdb.dbo.restorefile rf ON rsh.restore_history_id = rf.restore_history_id
        INNER JOIN msdb.dbo.backupmediafamily bmf ON bmf.media_set_id = bs.media_set_id
        INNER JOIN sys.databases d ON rsh.destination_database_name = d.name
    WHERE rsh.restore_type = 'L'
	AND restore_date>=DATEADD(d,-14,GETUTCDATE())
)
SELECT t.database_name,
       t.restore_date,
       t.backup_start_date,
       t.last_file
FROM t
WHERE rnum = 1;