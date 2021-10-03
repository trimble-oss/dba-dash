DECLARE @counters TABLE(
	object_name NCHAR(128) NOT NULL,
	counter_name NVARCHAR(128) NOT NULL,
	instance_name NCHAR(128) NULL
)
INSERT INTO @counters(object_name,counter_name,instance_name)
SELECT ctrs.c.value('@object_name','NCHAR(128)'),ctrs.c.value('@counter_name','NCHAR(128)') ,ctrs.c.value('@instance_name','NCHAR(128)') 
FROM @CountersXML.nodes('Counters/Counter') ctrs(c)

DECLARE @AvailablePhysicalMemory DECIMAL(28,9)
DECLARE @MemoryHighSignalState DECIMAL(28,9)
DECLARE @MemoryLowSignalState DECIMAL(28,9)
DECLARE @CountOfNodesWithThreadResourcesLow DECIMAL(28,9)

IF OBJECT_ID('sys.dm_os_sys_memory') IS NOT NULL AND EXISTS(SELECT * FROM @counters WHERE object_name='sys.dm_os_sys_memory')
BEGIN
	SELECT @AvailablePhysicalMemory= available_physical_memory_kb,
		@MemoryHighSignalState= system_high_memory_signal_state,
		@MemoryLowSignalState  = system_low_memory_signal_state
	FROM    sys.dm_os_sys_memory
END
IF EXISTS(SELECT * FROM @counters WHERE object_name='sys.dm_os_nodes')
BEGIN
	SELECT @CountOfNodesWithThreadResourcesLow=COUNT(*)   
	FROM sys.dm_os_nodes
	WHERE node_state_desc LIKE '%THREAD_RESOURCES_LOW%'
END

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
		STUFF(pc.object_name,1,CHARINDEX(':',pc.object_name),'') AS object_name,
       pc.counter_name,
       pc.instance_name,
       CAST(pc.cntr_value AS DECIMAL(28,9)) cntr_value,
       pc.cntr_type 
FROM sys.dm_os_performance_counters pc
WHERE EXISTS(SELECT 1 
FROM @counters c
WHERE c.object_name = STUFF(pc.object_name,1,CHARINDEX(':',pc.object_name),'')
AND c.counter_name = pc.counter_name
AND (c.instance_name = pc.instance_name OR c.instance_name IS NULL)
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