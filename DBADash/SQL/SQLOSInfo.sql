DECLARE @SQL NVARCHAR(MAX)

SET @SQL = N'
SELECT  ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'affinity_type','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS affinity_type,' ELSE 'affinity_type,' END + '
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'cores_per_socket','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS cores_per_socket,' ELSE 'cores_per_socket,' END + '
		cpu_count,
		hyperthread_ratio,
		max_workers_count,
		ms_ticks,
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'numa_node_count','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS numa_node_count,' ELSE 'numa_node_count,' END + '
		os_priority_class,
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'physical_memory_kb','ColumnID') IS NOT NULL THEN 'physical_memory_kb,'
		    WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'physical_memory_in_bytes','ColumnID') IS NOT NULL THEN 'physical_memory_in_bytes/1024 AS physical_memory_kb,'
			ELSE 'CAST(NULL AS BIGINT) AS AS physical_memory_kb,' END + '
		scheduler_count,
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'socket_count','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS socket_count,' ELSE 'socket_count,' END + '
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'softnuma_configuration','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS softnuma_configuration,' ELSE 'softnuma_configuration,' END + '
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'sql_memory_model','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS sql_memory_model,' ELSE 'sql_memory_model,' END + '
		' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'sqlserver_start_time','ColumnID') IS NULL THEN '(SELECT create_date FROM sys.databases WHERE name=''tempdb'') as sqlserver_start_time,' ELSE 'sqlserver_start_time,' END + '
		DATEDIFF(mi,GETDATE(),GETUTCDATE()) AS UTCOffset
FROM sys.dm_os_sys_info'

EXEC sp_executesql @SQL
