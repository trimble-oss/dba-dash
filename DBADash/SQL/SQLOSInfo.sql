DECLARE @SQL NVARCHAR(MAX);

WITH cols AS (
	SELECT 'softnuma_configuration' AS col,'INT' AS typ
	UNION ALL
	SELECT 'sql_memory_model' AS col,'INT' AS typ
	UNION ALL
	SELECT 'socket_count' AS col,'INT' AS typ
	UNION ALL
	SELECT 'cores_per_socket' AS col,'INT' AS typ
	UNION ALL
	SELECT 'numa_node_count' AS col,'INT' AS typ
	UNION ALL
	SELECT 'affinity_type' AS col,'INT' AS typ
	UNION ALL
	SELECT 'sqlserver_start_time' AS col,'DATETIME' AS typ
	UNION ALL
	SELECT 'os_priority_class' AS col,'INT' AS typ
	UNION ALL
	SELECT 'physical_memory_kb' AS col,'BIGINT' AS typ
	UNION ALL
	SELECT 'cpu_count' AS col,'INT' AS typ
	UNION ALL
	SELECT 'hyperthread_ratio' AS col,'INT' AS typ
	UNION ALL
	SELECT 'ms_ticks' AS col,'BIGINT' AS typ
	UNION ALL
	SELECT 'scheduler_count','INT'
	UNION ALL
	SELECT 'max_workers_count','INT'
)
SELECT @SQL ='SELECT ' + STUFF((SELECT ',' + CASE WHEN col='physical_memory_kb' 
												AND COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'physical_memory_kb','ColumnID') IS NULL 
												AND COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),'physical_memory_in_bytes','ColumnID') IS NOT NULL 
												THEN 'physical_memory_in_bytes/1024 as physical_memory_kb'
												WHEN col='sqlserver_start_time' AND COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),col,'ColumnID') IS NULL
												THEN '(SELECT create_date FROM sys.databases WHERE name=''tempdb'') as sqlserver_start_time'
												WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_sys_info'),col,'ColumnID') IS NULL THEN 'CAST(NULL AS ' + typ + ') as ' + QUOTENAME(col) ELSE col END
FROM cols
ORDER BY col
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'') + ',DATEDIFF(mi,GETDATE(),GETUTCDATE()) AS UTCOffset
FROM sys.dm_os_sys_info'
PRINT @SQL

EXEC sp_executesql @SQL