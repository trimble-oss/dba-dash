IF OBJECT_ID('sys.dm_db_resource_stats') IS NOT NULL
BEGIN
	SELECT TOP(@TOP) end_time AS EventTime,
		CAST(avg_cpu_percent AS INT),
		100-CAST(avg_cpu_percent AS INT) AS SystemIdle
	FROM sys.dm_db_resource_stats
END
ELSE
BEGIN
	DECLARE @ts_now BIGINT 
	DECLARE @numa_nodes INT

	SELECT	@ts_now= cpu_ticks/(cpu_ticks/ms_ticks)
	FROM sys.dm_os_sys_info; 

	IF OBJECT_ID('sys.dm_os_memory_nodes') IS NOT NULL
	BEGIN
		/* Get number of physical numa nodes for the SQL instance */
		SELECT @numa_nodes  = COUNT(*) 
		FROM sys.dm_os_memory_nodes 
		WHERE memory_node_id <> 64 /* exclude the internal node for the DAC */
	END
	ELSE
	BEGIN
		/* For SQL 2005 */
		SET @numa_nodes=1
	END
	/* 
		SQL Process utlization might need to be divided by NUMA node count.  
		If we still end up with more than 100% CPU, calculate SQL process as 100-system idle.
		Issue #1149 	
	*/
	SELECT TOP(@TOP) DATEADD(ms, -1 * (@ts_now - RB.timestamp), GETUTCDATE()) AS EventTime,
					CASE	WHEN RB.SQLProcessUtilization+RB.SystemIdle <= 100 THEN RB.SQLProcessUtilization 					
							WHEN (RB.SQLProcessUtilization / @numa_nodes) + RB.SystemIdle <= 100 THEN RB.SQLProcessUtilization / @numa_nodes
							ELSE 100-RB.SystemIdle END AS SQLProcessCPU, 
					RB.SystemIdle AS [SystemIdleProcess] 
	FROM (SELECT	record.value('(./Record/@id)[1]', 'int') AS record_id, 
					record.value('(./Record//SystemHealth/SystemIdle)[1]', 'int') AS SystemIdle, 
					record.value('(./Record//SystemHealth/ProcessUtilization)[1]', 'int') AS SQLProcessUtilization, 
					timestamp 
			FROM (SELECT [timestamp], CONVERT(xml, record) AS [record] 
			FROM sys.dm_os_ring_buffers WITH (NOLOCK)
			WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR' 
			AND record LIKE N'%<SystemHealth>%') AS x) AS RB 
	ORDER BY RB.record_id DESC 
	OPTION (RECOMPILE);
END
