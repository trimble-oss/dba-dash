IF OBJECT_ID('sys.dm_db_resource_stats') IS NOT NULL
BEGIN
	SELECT TOP(@TOP) end_time AS EventTime,
		CAST(avg_cpu_percent AS INT),
		100-CAST(avg_cpu_percent AS INT) AS SystemIdle
	FROM sys.dm_db_resource_stats
END
ELSE
BEGIN
	DECLARE @ts_now bigint 
	SELECT @ts_now= cpu_ticks/(cpu_ticks/ms_ticks) 
	FROM sys.dm_os_sys_info; 
 
	SELECT TOP(@TOP) DATEADD(ms, -1 * (@ts_now - [timestamp]), GETUTCDATE()) AS [EventTime],
					SQLProcessUtilization AS [SQLProcessCPU], 
				   SystemIdle AS [SystemIdleProcess] 
	FROM (SELECT record.value('(./Record/@id)[1]', 'int') AS record_id, 
				record.value('(./Record//SystemHealth/SystemIdle)[1]', 'int') 
				AS [SystemIdle], 
				record.value('(./Record//SystemHealth/ProcessUtilization)[1]', 'int') 
				AS [SQLProcessUtilization], [timestamp] 
		  FROM (SELECT [timestamp], CONVERT(xml, record) AS [record] 
				FROM sys.dm_os_ring_buffers WITH (NOLOCK)
				WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR' 
				AND record LIKE N'%<SystemHealth>%') AS x) AS y 
	ORDER BY record_id DESC OPTION (RECOMPILE);
END