DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT servicename,
	startup_type,
	startup_type_desc,
	status,
	status_desc,
	process_id,
	last_startup_time,
	service_account,
	filename,
	is_clustered,
	cluster_nodename,
	' + CASE WHEN COL_LENGTH('sys.dm_server_services','instant_file_initialization_enabled') IS NULL THEN 'CAST(NULL AS NVARCHAR(1)) AS instant_file_initialization_enabled' ELSE 'instant_file_initialization_enabled' END + '
FROM sys.dm_server_services'

EXEC sp_executesql @SQL
