select database_name,type, MAX(backup_start_date) LastBackup
from msdb.dbo.backupset
where server_name=@@SERVERNAME
AND backup_finish_date>=DATEADD(d,-10,GETUTCDATE())
group by database_name, type