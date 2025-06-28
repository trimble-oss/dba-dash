DECLARE @Counters TABLE(
	object_name NVARCHAR(128),
	counter_name NVARCHAR(128),
	instance_name NVARCHAR(128)
)
IF OBJECT_ID('sys.dm_tran_persistent_version_store_stats') IS NOT NULL
BEGIN
	INSERT INTO @Counters(
		object_name,
		counter_name,
		instance_name
	)
	SELECT	N'sys.dm_tran_persistent_version_store_stats' as object_name,
			'Persistent Version Store Size (KB)',
			d.name AS instance_name
	FROM sys.databases d
	JOIN sys.dm_tran_persistent_version_store_stats pvs on d.database_id = pvs.database_id
	WHERE d.is_accelerated_database_recovery_on=1
END
SELECT 	RTRIM(STUFF(object_name,1,CHARINDEX(':',object_name),'')) AS object_name, /* Remove text to left of semi-colon e.g. Buffer Manager instead of SQLServer:Buffer Manager */
		RTRIM(counter_name) AS counter_name,
		RTRIM(instance_name) AS instance_name
FROM sys.dm_os_performance_counters
UNION ALL
SELECT 	'sys.dm_os_schedulers' AS object_name,
		'Worker threads used %' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT  'sys.dm_os_schedulers' AS object_name,
		'Avg runnable tasks' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT  'sys.dm_os_schedulers' AS object_name,
		'Avg current tasks' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_schedulers' AS object_name,
		'Avg pending disk IO' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_schedulers' AS object_name,
		'Avg work queue count' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_nodes' AS object_name,
		'Count of Nodes reporting thread resources low' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_sys_memory' AS object_name,
		'Available Physical Memory (KB)' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_sys_memory' AS object_name,
		'System Low Memory Signal State' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_os_sys_memory' AS object_name,
		'System High Memory Signal State' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_tran_persistent_version_store_stats' AS object_name,
		'Max Persistent Version Store Size (KB)' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	'sys.dm_tran_persistent_version_store_stats' AS object_name,
		'Sum Persistent Version Store Size (KB)' AS counter_name,
		'' AS instance_name
UNION ALL
SELECT 	object_name,
		counter_name,
		instance_name
FROM @Counters