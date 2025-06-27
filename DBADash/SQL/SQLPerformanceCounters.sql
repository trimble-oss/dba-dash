SET TRAN ISOLATION LEVEL READ UNCOMMITTED
DECLARE @counters TABLE(
	object_name NCHAR(128) NOT NULL,
	counter_name NVARCHAR(128) NOT NULL,
	instance_name NCHAR(128) NULL,
	UNIQUE(object_name,counter_name,instance_name)
)
DECLARE @Metrics AS TABLE(
	SnapshotDate DATETIME2(7) NOT NULL DEFAULT(SYSUTCDATETIME()),
	object_name NVARCHAR(128) NOT NULL,
	counter_name NVARCHAR(128) NOT NULL,
	instance_name NVARCHAR(128) NOT NULL,
	cntr_value DECIMAL(28,9) NOT NULL,
	cntr_type INT NOT NULL DEFAULT(65792)
)
INSERT INTO @counters(object_name,counter_name,instance_name)
SELECT	ctrs.c.value('@object_name','NCHAR(128)'),
		ctrs.c.value('@counter_name','NCHAR(128)'),
		ctrs.c.value('@instance_name','NCHAR(128)') 
FROM @CountersXML.nodes('Counters/Counter') ctrs(c)

DECLARE @AvailablePhysicalMemory DECIMAL(28,9)
DECLARE @MemoryHighSignalState DECIMAL(28,9)
DECLARE @MemoryLowSignalState DECIMAL(28,9)
DECLARE @CountOfNodesWithThreadResourcesLow DECIMAL(28,9)
DECLARE @PctWorkerThreadsUsed DECIMAL(28,9)
DECLARE @AvgRunnableTasks DECIMAL(28,9)
DECLARE @AvgCurrentTasks DECIMAL(28,9)
DECLARE @AvgPendingDiskIO DECIMAL(28,9)
DECLARE @AvgWorkQueueCount DECIMAL(28,9)
DECLARE @MaxPVSSizeKB DECIMAL(28,9)
DECLARE @SumPVSSizeKB DECIMAL(28,9)

IF OBJECT_ID('sys.dm_os_sys_memory') IS NOT NULL AND EXISTS(SELECT * FROM @counters WHERE object_name='sys.dm_os_sys_memory')
BEGIN
	SELECT @AvailablePhysicalMemory= available_physical_memory_kb,
		@MemoryHighSignalState= system_high_memory_signal_state,
		@MemoryLowSignalState  = system_low_memory_signal_state
	FROM sys.dm_os_sys_memory
END
IF OBJECT_ID('sys.dm_os_nodes') IS NOT NULL AND EXISTS(SELECT * FROM @counters WHERE object_name='sys.dm_os_nodes')
BEGIN
	SELECT @CountOfNodesWithThreadResourcesLow=COUNT(*)   
	FROM sys.dm_os_nodes
	WHERE node_state_desc LIKE '%THREAD_RESOURCES_LOW%'
END
IF EXISTS(SELECT * FROM @counters WHERE object_name='sys.dm_os_schedulers')
BEGIN
	SELECT @PctWorkerThreadsUsed = SUM(active_workers_count)*100.0/(SELECT max_workers_count FROM sys.dm_os_sys_info),
		@AvgRunnableTasks = AVG(runnable_tasks_count*1.0),
		@AvgCurrentTasks = AVG(current_tasks_count*1.0),
		@AvgPendingDiskIO = AVG(pending_disk_io_count*1.0),
		@AvgWorkQueueCount = AVG(work_queue_count*1.0)
	FROM sys.dm_os_schedulers 
	WHERE status='VISIBLE ONLINE'
END;
IF OBJECT_ID('sys.dm_tran_persistent_version_store_stats') IS NOT NULL 
	AND EXISTS(SELECT * 
				FROM @counters 
				WHERE object_name='sys.dm_tran_persistent_version_store_stats'
				AND (counter_name = 'Max Persistent Version Store Size (KB)' 
					OR counter_name = 'Sum Persistent Version Store Size (KB)'
					OR counter_name= '*')
				AND (instance_name IS NULL OR instance_name='*' OR instance_name='')
	)
BEGIN
	SELECT	@MaxPVSSizeKB = MAX(pvs.persistent_version_store_size_kb),
			@SumPVSSizeKB = SUM(pvs.persistent_version_store_size_kb)
	FROM sys.databases d
	JOIN sys.dm_tran_persistent_version_store_stats pvs on d.database_id = pvs.database_id
	WHERE d.is_accelerated_database_recovery_on=1 /* Filter for where ADR is enabled.  If no DBs are enabled, values will be NULL and instance level metrics won't be tracked */
END 

IF OBJECT_ID('sys.dm_tran_persistent_version_store_stats') IS NOT NULL 
	AND EXISTS(SELECT * 
				FROM @counters 
				WHERE object_name='sys.dm_tran_persistent_version_store_stats'
				AND (counter_name = 'Persistent Version Store Size (KB)' OR counter_name = '*')
	)
BEGIN
	/* Captures PVS size for databases with ADR enabled (avoiding capturing metrics for DBs where it's not applicable) */
	INSERT @Metrics	(
	    SnapshotDate,
	    object_name,
	    counter_name,
	    instance_name,
	    cntr_value,
	    cntr_type
	)
	SELECT SYSUTCDATETIME() AS SnapshotDate,
		N'sys.dm_tran_persistent_version_store_stats' as object_name,
		'Persistent Version Store Size (KB)',
		d.name AS instance_name,
		CAST(pvs.persistent_version_store_size_kb AS DECIMAL(28,9)),
		65792 AS cntr_type
	FROM sys.databases d
	JOIN sys.dm_tran_persistent_version_store_stats pvs on d.database_id = pvs.database_id
	WHERE d.is_accelerated_database_recovery_on=1
	AND EXISTS(	SELECT 1 
				FROM @counters c 
				WHERE c.object_name = N'sys.dm_tran_persistent_version_store_stats' 
				AND (c.counter_name = 'Persistent Version Store Size (KB)' OR c.counter_name = '*')
				AND (d.name = c.instance_name OR c.instance_name IS NULL OR c.instance_name='*')
			   )
END;

WITH Metrics AS (
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_schedulers' AS object_name,
			'Worker threads used %' AS counter_name,
			'' AS instance_name,
			@PctWorkerThreadsUsed AS  cntr_value,
			65792 AS cntr_type
	WHERE @PctWorkerThreadsUsed IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_schedulers' AS object_name,
			'Avg runnable tasks' AS counter_name,
			'' AS instance_name,
			@AvgRunnableTasks AS  cntr_value,
			65792 AS cntr_type
	WHERE @AvgRunnableTasks IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_schedulers' AS object_name,
			'Avg current tasks' AS counter_name,
			'' AS instance_name,
			@AvgCurrentTasks AS  cntr_value,
			65792 AS cntr_type
	WHERE @AvgCurrentTasks IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_schedulers' AS object_name,
			'Avg pending disk IO' AS counter_name,
			'' AS instance_name,
			@AvgPendingDiskIO AS  cntr_value,
			65792 AS cntr_type
	WHERE @AvgPendingDiskIO IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_schedulers' AS object_name,
			'Avg work queue count' AS counter_name,
			'' AS instance_name,
			@AvgWorkQueueCount AS  cntr_value,
			65792 AS cntr_type
	WHERE @AvgWorkQueueCount IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_nodes' AS object_name,
			'Count of Nodes reporting thread resources low' AS counter_name,
			'' AS instance_name,
			@CountOfNodesWithThreadResourcesLow AS  cntr_value,
			65792 AS cntr_type
	WHERE @CountOfNodesWithThreadResourcesLow IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_sys_memory' AS object_name,
			'Available Physical Memory (KB)' AS counter_name,
			'' AS instance_name,
			@AvailablePhysicalMemory AS  cntr_value,
			65792 AS cntr_type
	WHERE @AvailablePhysicalMemory IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME()AS SnapshotDate,
			'sys.dm_os_sys_memory' AS object_name,
			'System Low Memory Signal State' AS counter_name,
			'' AS instance_name,
			@MemoryLowSignalState AS  cntr_value,
			65792 AS cntr_type
	WHERE @MemoryLowSignalState IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_os_sys_memory' AS object_name,
			'System High Memory Signal State' AS counter_name,
			'' AS instance_name,
			@MemoryHighSignalState AS  cntr_value,
			65792 AS cntr_type
	WHERE @MemoryHighSignalState IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_tran_persistent_version_store_stats' AS object_name,
			'Max Persistent Version Store Size (KB)' AS counter_name,
			'' AS instance_name,
			@MaxPVSSizeKB AS  cntr_value,
			65792 AS cntr_type
	WHERE @MaxPVSSizeKB IS NOT NULL
	UNION ALL
	SELECT SYSUTCDATETIME() AS SnapshotDate,
			'sys.dm_tran_persistent_version_store_stats' AS object_name,
			'Sum Persistent Version Store Size (KB)' AS counter_name,
			'' AS instance_name,
			@SumPVSSizeKB AS  cntr_value,
			65792 AS cntr_type
	WHERE @SumPVSSizeKB IS NOT NULL
)
INSERT INTO @Metrics(
    SnapshotDate,
    object_name,
    counter_name,
    instance_name,
    cntr_value,
    cntr_type
)
SELECT SnapshotDate,
    object_name,
    counter_name,
    instance_name,
    cntr_value,
    cntr_type
FROM Metrics M
WHERE EXISTS(SELECT 1 
			FROM @counters c 
			WHERE c.object_name = M.object_name
			AND (c.counter_name = M.counter_name OR c.counter_name = '*')
			AND (c.instance_name = M.instance_name OR c.instance_name IS NULL OR c.instance_name='*')
		   )

SELECT SnapshotDate,
    object_name,
    counter_name,
    instance_name,
    cntr_value,
    cntr_type
FROM @Metrics
UNION ALL
SELECT SYSUTCDATETIME() AS SnapshotDate,
		STUFF(pc.object_name,1,CHARINDEX(':',pc.object_name),'') AS object_name,
       pc.counter_name,
       pc.instance_name,
       CAST(pc.cntr_value AS DECIMAL(28,9)) cntr_value,
       pc.cntr_type 
FROM sys.dm_os_performance_counters pc
WHERE EXISTS(	SELECT 1 
				FROM @counters c
				WHERE c.object_name COLLATE SQL_Latin1_General_CP1_CI_AS = STUFF(pc.object_name,1,CHARINDEX(':',pc.object_name),'') COLLATE SQL_Latin1_General_CP1_CI_AS
				AND c.counter_name COLLATE SQL_Latin1_General_CP1_CI_AS = pc.counter_name COLLATE SQL_Latin1_General_CP1_CI_AS
				AND (c.instance_name COLLATE SQL_Latin1_General_CP1_CI_AS = pc.instance_name COLLATE SQL_Latin1_General_CP1_CI_AS OR c.instance_name IS NULL)
			)
AND pc.instance_name NOT IN('AuditingGroup',
							'AutoShrinkLogGroup',
							'CheckpointGroup',
							'DACGroup',
							'GhostCleanupGroup',
							'InMemBackupGroup',
							'InMemBackupRestorePool',
							'InMemDmvCollectorGroup',
							'InMemDmvCollectorPool',
							'InMemDTAPool',
							'InMemFullBackupGroup',
							'InMemMetricsDownloaderGroup',
							'InMemMetricsDownloaderPool',
							'InMemQueryStoreGroup',
							'InMemQueryStoreGroupFeedback',
							'InMemQueryStoreGroupLowPri',
							'InMemQueryStorePool',
							'InMemRestoreGroup',
							'InMemTdeScanGroup',
							'InMemTdeScanPool',
							'InMemWIAutoTuningPool',
							'InMemWIIndexValidationGroup',
							'InMemXdbLoginPool',
							'InMemXdbSeedingPool',
							'LazywriterGroup',
							'PVSCleanerGroup',
							'PVSCleanerPool',
							'RecoveryWriterGroup',
							'RedoGroup',
							'ResourceMonitorGroup',
							'SeedingGroup',
							'SloDTAGroup',
							'SloHkPool',
							'SloSecSharedInitGroup',
							'SloSecSharedPool',
							'UcsGroup',
							'UpdateAutoStatsAsyncGroup',
							'XdbLoginGroup',
							'XOdbcGroup',
							'mssqlsystemresource'
							)
AND NOT (pc.counter_name= 'Percent Log Used' AND pc.instance_name='_Total')
AND (SERVERPROPERTY('EngineEdition')<> 5
	OR (pc.instance_name NOT IN('msdb','master','model','model_masterdb','model_userdb','mssqlsystemresource','tempdb')		
		AND NOT (pc.counter_name= 'Log Growths' AND pc.instance_name='_Total')
		)
	)
IF (OBJECT_ID('dbo.DBADash_CustomPerformanceCounters')) IS NOT NULL
BEGIN
	EXEC dbo.DBADash_CustomPerformanceCounters
END