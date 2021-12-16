﻿/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
/* Security */
GRANT SELECT ON SCHEMA::dbo TO [App]
GRANT EXECUTE ON SCHEMA::dbo TO [App];
GRANT SELECT ON SCHEMA::dbo TO [Reports]
GRANT EXECUTE ON SCHEMA::[Report] TO [Reports];
/************/
IF NOT EXISTS(SELECT 1 FROM dbo.SysConfigOptions)
BEGIN
INSERT INTO dbo.SysConfigOptions
(
    configuration_id,
    name,
    description,
    is_dynamic,
    is_advanced,
    default_value,
    minimum,
    maximum
)
VALUES
( 101, N'recovery interval (min)', N'Maximum recovery interval in minutes', 1, 1, 0, 0, 32767 ), 
( 102, N'allow updates', N'Allow updates to system tables', 1, 0, 0, 0, 1 ), 
( 103, N'user connections', N'Number of user connections allowed', 0, 1, 0, 0, 32767 ), 
( 106, N'locks', N'Number of locks for all users', 0, 1, 0, 5000, 2147483647 ), 
( 107, N'open objects', N'Number of open database objects', 0, 1, 0, 0, 2147483647 ), 
( 109, N'fill factor (%)', N'Default fill factor percentage', 0, 1, 0, 0, 100 ), 
( 114, N'disallow results from triggers', N'Disallow returning results from triggers', 1, 1, 0, 0, 1 ), 
( 115, N'nested triggers', N'Allow triggers to be invoked within triggers', 1, 0, 1, 0, 1 ), 
( 116, N'server trigger recursion', N'Allow recursion for server level triggers', 1, 0, 1, 0, 1 ), 
( 117, N'remote access', N'Allow remote access', 0, 0, 1, 0, 1 ), 
( 124, N'default language', N'default language', 1, 0, 0, 0, 9999 ), 
( 400, N'cross db ownership chaining', N'Allow cross db ownership chaining', 1, 0, 0, 0, 1 ), 
( 503, N'max worker threads', N'Maximum worker threads', 1, 1, 0, 128, 65535 ), 
( 505, N'network packet size (B)', N'Network packet size', 1, 1, 4096, 512, 32767 ), 
( 518, N'show advanced options', N'show advanced options', 1, 0, N'0', 0, 1 ), 
( 542, N'remote proc trans', N'Create DTC transaction for remote procedures', 1, 0, 0, 0, 1 ), 
( 544, N'c2 audit mode', N'c2 audit mode', 0, 1, 0, 0, 1 ), 
( 1126, N'default full-text language', N'default full-text language', 1, 1, 1033, 0, 2147483647 ), 
( 1127, N'two digit year cutoff', N'two digit year cutoff', 1, 1, 2049, 1753, 9999 ), 
( 1505, N'index create memory (KB)', N'Memory for index create sorts (kBytes)', 1, 1, 0, 704, 2147483647 ), 
( 1517, N'priority boost', N'Priority boost', 0, 1, 0, 0, 1 ), 
( 1519, N'remote login timeout (s)', N'remote login timeout', 1, 0, 10, 0, 2147483647 ), 
( 1520, N'remote query timeout (s)', N'remote query timeout', 1, 0, 600, 0, 2147483647 ), 
( 1531, N'cursor threshold', N'cursor threshold', 1, 1, -1, -1, 2147483647 ), 
( 1532, N'set working set size', N'set working set size', 0, 1, 0, 0, 1 ), 
( 1534, N'user options', N'user options', 1, 0, 0, 0, 32767 ), 
( 1535, N'affinity mask', N'affinity mask', 1, 1, 0, -2147483648, 2147483647 ), 
( 1536, N'max text repl size (B)', N'Maximum size of a text field in replication.', 1, 0, 65536, -1, 2147483647 ), 
( 1537, N'media retention', N'Tape retention period in days', 1, 1, 0, 0, 365 ), 
( 1538, N'cost threshold for parallelism', N'cost threshold for parallelism', 1, 1, 5, 0, 32767 ), 
( 1539, N'max degree of parallelism', N'maximum degree of parallelism', 1, 1, 0, 0, 32767 ), 
( 1540, N'min memory per query (KB)', N'minimum memory per query (kBytes)', 1, 1, 1024, 512, 2147483647 ), 
( 1541, N'query wait (s)', N'maximum time to wait for query memory (s)', 1, 1, -1, -1, 2147483647 ), 
( 1543, N'min server memory (MB)', N'Minimum size of server memory (MB)', 1, 1, 0, 0, 2147483647 ), 
( 1544, N'max server memory (MB)', N'Maximum size of server memory (MB)', 1, 1, N'2147483647', 128, 2147483647 ), 
( 1545, N'query governor cost limit', N'Maximum estimated cost allowed by query governor', 1, 1, 0, 0, 2147483647 ), 
( 1546, N'lightweight pooling', N'User mode scheduler uses lightweight pooling', 0, 1, 0, 0, 1 ), 
( 1547, N'scan for startup procs', N'scan for startup stored procedures', 0, 1, 0, 0, 1 ), 
( 1549, N'affinity64 mask', N'affinity64 mask', 1, 1, 0, -2147483648, 2147483647 ), 
( 1550, N'affinity I/O mask', N'affinity I/O mask', 0, 1, 0, -2147483648, 2147483647 ), 
( 1551, N'affinity64 I/O mask', N'affinity64 I/O mask', 0, 1, 0, -2147483648, 2147483647 ), 
( 1555, N'transform noise words', N'Transform noise words for full-text query', 1, 1, 0, 0, 1 ), 
( 1556, N'precompute rank', N'Use precomputed rank for full-text query', 1, 1, 0, 0, 1 ), 
( 1557, N'PH timeout (s)', N'DB connection timeout for full-text protocol handler (s)', 1, 1, 60, 1, 3600 ), 
( 1562, N'clr enabled', N'CLR user code execution enabled in the server', 1, 0, N'0', 0, 1 ), 
( 1563, N'max full-text crawl range', N'Maximum  crawl ranges allowed in full-text indexing', 1, 1, 4, 0, 256 ), 
( 1564, N'ft notify bandwidth (min)', N'Number of reserved full-text notifications buffers', 1, 1, 0, 0, 32767 ), 
( 1565, N'ft notify bandwidth (max)', N'Max number of full-text notifications buffers', 1, 1, 100, 0, 32767 ), 
( 1566, N'ft crawl bandwidth (min)', N'Number of reserved full-text crawl buffers', 1, 1, 0, 0, 32767 ), 
( 1567, N'ft crawl bandwidth (max)', N'Max number of full-text crawl buffers', 1, 1, 100, 0, 32767 ), 
( 1568, N'default trace enabled', N'Enable or disable the default trace', 1, 1, 1, 0, 1 ), 
( 1569, N'blocked process threshold (s)', N'Blocked process reporting threshold', 1, 1, 0, 0, 86400 ), 
( 1570, N'in-doubt xact resolution', N'Recovery policy for DTC transactions with unknown outcome', 1, 1, 0, 0, 2 ), 
( 1576, N'remote admin connections', N'Dedicated Admin Connections are allowed from remote clients', 1, 0, 0, 0, 1 ), 
( 1577, N'common criteria compliance enabled', N'Common Criteria compliance mode enabled', 0, 1, 0, 0, 1 ), 
( 1578, N'EKM provider enabled', N'Enable or disable EKM provider', 1, 1, 0, 0, 1 ), 
( 1579, N'backup compression default', N'Enable compression of backups by default', 1, 0, 0, 0, 1 ), 
( 1580, N'filestream access level', N'Sets the FILESTREAM access level', 1, 0, N'0', 0, 2 ), 
( 1581, N'optimize for ad hoc workloads', N'When this option is set, plan cache size is further reduced for single-use adhoc OLTP workload.', 1, 1, 0, 0, 1 ), 
( 1582, N'access check cache bucket count', N'Default hash bucket count for the access check result security cache', 1, 1, 0, 0, 65536 ), 
( 1583, N'access check cache quota', N'Default quota for the access check result security cache', 1, 1, 0, 0, 2147483647 ), 
( 1584, N'backup checksum default', N'Enable checksum of backups by default', 1, 0, 0, 0, 1 ), 
( 1585, N'automatic soft-NUMA disabled', N'Automatic soft-NUMA is enabled by default', 0, 1, 0, 0, 1 ), 
( 1586, N'external scripts enabled', N'Allows execution of external scripts', 0, 0, 0, 0, 1 ), 
( 1587, N'clr strict security', N'CLR strict security enabled in the server', 1, 1, null, 0, 1 ),
( 16384, N'Agent XPs', N'Enable or disable Agent XPs', 1, 1, 1, 0, 1 ), 
( 16386, N'Database Mail XPs', N'Enable or disable Database Mail XPs', 1, 1, N'0', 0, 1 ), 
( 16387, N'SMO and DMO XPs', N'Enable or disable SMO and DMO XPs', 1, 1, 1, 0, 1 ), 
( 16388, N'Ole Automation Procedures', N'Enable or disable Ole Automation Procedures', 1, 1, 0, 0, 1 ), 
( 16390, N'xp_cmdshell', N'Enable or disable command shell', 1, 1, 0, 0, N'0' ), 
( 16391, N'Ad Hoc Distributed Queries', N'Enable or disable Ad Hoc Distributed Queries', 1, 1, 0, 0, 1 ), 
( 16392, N'Replication XPs', N'Enable or disable Replication XPs', 1, 1, 0, 0, 1 ), 
( 16393, N'contained database authentication', N'Enables contained databases and contained authentication', 1, 0, 0, 0, 1 ), 
( 16394, N'hadoop connectivity', N'Configure SQL Server to connect to external Hadoop or Microsoft Azure storage blob data sources through PolyBase', 0, 0, 0, 0, 7 ), 
( 16395, N'polybase network encryption', N'Configure SQL Server to encrypt control and data channels when using PolyBase', 0, 0, 1, 0, 1 ), 
( 16396, N'remote data archive', N'Allow the use of the REMOTE_DATA_ARCHIVE data access for databases', 1, 0, 0, 0, 1 ), 
( 16397, N'allow polybase export', N'Allow INSERT into a Hadoop external table', 1, 0, 0, 0, 1 )

END

DECLARE @waits TABLE ( [WaitType] nvarchar(60) NOT NULL PRIMARY KEY, [IsCriticalWait] BIT NOT NULL )

INSERT INTO @waits
(
    WaitType,
    IsCriticalWait
)
VALUES
( N'RESOURCE_SEMAPHORE', 1 ), 
( N'THREADPOOL', 1 ), 
( N'RESOURCE_SEMAPHORE_QUERY_COMPILE', 1 ), 
( N'PREEMPTIVE_DEBUG', 1 ), 
( N'IO_QUEUE_LIMIT', 1 ), 
( N'IO_RETRY', 1 ), 
( N'LOG_RATE_GOVERNOR', 1 ), 
( N'POOL_LOG_RATE_GOVERNOR', 1 ), 
( N'RESMGR_THROTTLED', 1 ), 
( N'SE_REPL_CATCHUP_THROTTLE', 1 ), 
( N'SE_REPL_COMMIT_ACK', 1 ), 
( N'SE_REPL_COMMIT_TURN', 1 ), 
( N'SE_REPL_ROLLBACK_ACK', 1 ), 
( N'SE_REPL_SLOW_SECONDARY_THROTTLE', 1 )

INSERT INTO dbo.WaitType
(
    WaitType,
    IsCriticalWait
)
SELECT t.WaitType,
       t.IsCriticalWait
FROM @waits t
WHERE NOT EXISTS(SELECT 1 FROM dbo.WaitType wt WHERE wt.WaitType = t.WaitType)

UPDATE WT 
	SET WT.IsCriticalWait = t.IsCriticalWait
FROM dbo.WaitType wt 
JOIN @waits t ON  t.WaitType = wt.WaitType
WHERE WT.IsCriticalWait<>t.IsCriticalWait
GO
INSERT INTO dbo.DataRetention
(
    TableName,
    RetentionDays
)
SELECT t.TableName,t.RetentionDays
FROM (VALUES('ObjectExecutionStats',120),
				('Waits',120),
				('DBIOStats',120),
				('CPU',365),
				('BlockingSnapshot',120),
				('SlowQueries',120),
				('AzureDBElasticPoolResourceStats',120),
				('AzureDBResourceStats',120),
				('CustomChecksHistory',120),
				('DBIOStats_60MIN',730),
				('CPU_60MIN',730),
				('ObjectExecutionStats_60MIN',730),
				('AzureDBElasticPoolResourceStats_60MIN',730),
				('AzureDBResourceStats_60MIN',730),
				('Waits_60MIN',730),
				('PerformanceCounters', 180),
				('PerformanceCounters_60MIN',730),
				('JobStats_60MIN',730),
				('JobHistory',8),
				('RunningQueries',30),
				('CollectionErrorLog',14),
				('MemoryUsage',30),
				('SessionWaits',30)
				) AS t(TableName,RetentionDays)
WHERE NOT EXISTS(SELECT 1 FROM dbo.DataRetention DR WHERE DR.TableName = T.TableName)

INSERT INTO dbo.OSLoadedModulesStatus
(
    NAME,
    Company,
    Description,
    STATUS
)
SELECT * FROM  (
VALUES
( N'%', N'%', N'XTP Native DLL', 4 ), 
( N'%', N'Microsoft Corporation', N'%', 4 ), 
( N'%', N'Корпорация Майкрософт', N'%', 4 ), 
( N'%\ENTAPI.DLL', N'%', N'%', 1 ), 
( N'%\HcApi.dll', N'%', N'%', 1 ), 
( N'%\HcSQL.dll', N'%', N'%', 1 ), 
( N'%\HcThe.dll', N'%', N'%', 1 ), 
( N'%\HIPI.DLL', N'%', N'%', 1 ), 
( N'%\PIOLEDB.DLL', N'%', N'%', 1 ), 
( N'%\PISDK.DLL', N'%', N'%', 1 ), 
( N'%\SOPHOS_DETOURED.DLL', N'%', N'%', 1 ), 
( N'%\SOPHOS_DETOURED_x64.DLL', N'%', N'%', 1 ), 
( N'%\SOPHOS~%.dll', N'%', N'%', 1 ), 
( N'%\SWI_IFSLSP_64.dll', N'%', N'%', 1 ), 
( N'%IisRTL.DLL', N'%', N'%', 4 ), 
( N'%iisutil.dll', N'%', N'%', 4 ), 
( N'%instapi.dll', N'%', N'%', 4 ), 
( N'%MSDART.DLL', N'%', N'%', 4 ), 
( N'%msxml3.dll', N'%', N'%', 4 ), 
( N'%msxmlsql.dll', N'%', N'%', 4 ), 
( N'%ODBC32.dll', N'%', N'%', 4 ), 
( N'%oledb32.dll', N'%', N'%', 4 ), 
( N'%OLEDB32R.DLL', N'%', N'%', 4 ), 
( N'%ScriptControl64%.dll', N'%', N'%', 4 ), 
( N'%UMPDC.dll', N'%', N'%', 4 ), 
( N'%umppc%.dll', N'%', N'%', 4 ), 
( N'%w3ctrs.dll', N'%', N'%', 4 ), 
( N'%XmlLite.dll', N'%', N'%', 4 ), 
( N'%xpsqlbot.dll', N'%', N'%', 4 )
) t(name,company,description,status)
WHERE NOT EXISTS(SELECT 1 FROM dbo.OSLoadedModulesStatus s
			WHERE s.NAME = t.name 
			AND s.Company = t.company
			AND s.Description = t.description)

IF NOT EXISTS(SELECT 1 FROM dbo.DriveThresholds)
BEGIN 
	INSERT INTO dbo.DriveThresholds
	(
		InstanceID,
		DriveID,
		DriveWarningThreshold,
		DriveCriticalThreshold,
		DriveCheckType
	)
	VALUES
	( -1, -1, 0.200, 0.100, '%' )
END

IF NOT EXISTS(SELECT 1 FROM dbo.LogRestoreThresholds)
BEGIN
	INSERT INTO dbo.LogRestoreThresholds
	(
		InstanceID,
		DatabaseID,
		LatencyWarningThreshold,
		LatencyCriticalThreshold,
		TimeSinceLastWarningThreshold,
		TimeSinceLastCriticalThreshold,
		NewDatabaseExcludePeriodMin
	)
	VALUES
	( -1, -1, 1440, 2880, 1440, 2880, 1440 )
END
IF NOT EXISTS(SELECT 1 FROM dbo.BackupThresholds)
BEGIN
	INSERT INTO dbo.BackupThresholds
	(
		InstanceID,
		DatabaseID,
		LogBackupWarningThreshold,
		LogBackupCriticalThreshold,
		FullBackupWarningThreshold,
		FullBackupCriticalThreshold,
		DiffBackupWarningThreshold,
		DiffBackupCriticalThreshold,
		ConsiderPartialBackups,
		ConsiderFGBackups
	)
	VALUES
	( -1, -1, 720, 1440, 10080, 14400, NULL, NULL, 0, 0 )
END
IF NOT EXISTS(SELECT 1 FROM dbo.DDL WHERE DDLID=-1)
BEGIN
	SET IDENTITY_INSERT dbo.DDL ON
	INSERT INTO dbo.DDL(DDLID,DDLHash,DDL)
	VALUES
	( -1, 0xe3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855, 0x )
	SET IDENTITY_INSERT dbo.DDL OFF
END
IF NOT EXISTS(SELECT 1 FROM dbo.ObjectType)
BEGIN
	INSERT INTO dbo.ObjectType
	(
		ObjectType,
		TypeDescription
	)
	VALUES( 'P','Stored Procedure'),
	('V','View'),
	('IF','Inline Function'),
	('U','Table'),
	('TF','Table Function'),
	('FN','Scalar Function'),
	('AF','Aggregate Function'),
	('DTR','Database Trigger'),
	('CLR','CLR Assembly'),
	('FT','CLR Table Function'),
	('FS','CLR Scalar Function'),
	('TYP','User Defined Data Type'),
	('TT','User Defined Table Type'),
	('UTY','User Defined Type'),
	('XSC','XML Schema Collection'),
	('SO','Sequence Object'),
	('SCH','Schema'),
	('SN','Synonym'),
	('DB','Database'),
	('PC','CLR Procedure'),
	('ROL','Role'),
	('SBM','Service Broker Message Type'),
	('SBS','Service Broker Service'),
	('SBC','Service Broker Contract'),
	('SBB','Service Broker Binding'),
	('SBP','Service Broker Priorities'),
	('SQ','Service Broker Queue'),
	('SBR','Service Broker Route')
END

INSERT INTO dbo.CollectionDatesThresholds
(
    InstanceID,
    Reference,
    WarningThreshold,
    CriticalThreshold
)

SELECT T.InstanceID,
       T.Reference,
       T.WarningThreshold,
       T.CriticalThreshold
FROM 
(VALUES
(-1,'DBFiles',125,180),
(-1,'ServerExtraProperties',125,180),
(-1,'OSLoadedModules',1445,2880),
(-1,'TraceFlags',125,180),
(-1,'ServerProperties',125,180),
(-1,'LogRestores',125,180),
(-1,'DatabaseHADR',5,10),
(-1,'LastGoodCheckDB',125,180),
(-1,'Alerts',125,180),
(-1,'DBTuningOptions',125,180),
(-1,'DBConfig',125,180),
(-1,'OSInfo',125,180),
(-1,'Drives',125,180),
(-1,'Databases',125,180),
(-1,'Instance',125,180),
(-1,'Drivers',1445,2880),
(-1,'SysConfig',125,180),
(-1,'Backups',125,180),
(-1,'AzureDBServiceObjectives',125,180),
(-1,'Corruption',125,180),
(-1,'RunningQueries',5,10),
(-1,'CPU',5,10),
(-1,'IOStats',5,10),
(-1,'ObjectExecutionStats',5,10),
(-1,'SlowQueries',5,10),
(-1,'SlowQueriesStats',5,10),
(-1,'AzureDBElasticPoolResourceStat',5,10),
(-1,'Waits',5,10),
(-1,'AzureDBResourceStats',5,10),
(-1,'DatabasePrincipals',1445,2880),
(-1,'ServerPermissions',1445,2880),
(-1,'ServerPrincipals',1445,2880),
(-1,'DatabasePermissions',1445,2880),
(-1,'ServerRoleMembers',1445,2880),
(-1,'DatabaseRoleMembers',1445,2880),
(-1,'DatabaseMirroring',125,180),
(-1,'Jobs',1445,2880),
(-1,'JobHistory',5,10),
(-1,'VLF',1445,2880),
(-1,'CustomChecks',125,180),
(-1,'PerformanceCounters',5,10),
(-1,'DatabaseQueryStoreOptions',1445,2880),
(-1,'AzureDBResourceGovernance',125,180),
(-1,'ResourceGovernorConfiguration',1445,2880),
(-1,'MemoryUsage',5,10)
) T(InstanceID,Reference,WarningThreshold,CriticalThreshold)
WHERE NOT EXISTS(SELECT 1 FROM dbo.CollectionDatesThresholds CDT WHERE CDT.InstanceID = T.InstanceID AND CDT.Reference = T.Reference)

-- Delete thresholds for legacy collections
DELETE dbo.CollectionDatesThresholds WHERE Reference IN('AgentJobs','BlockingSnapshot','Database')

-- Delete collection history for legacy collections
DELETE dbo.CollectionDates
WHERE Reference IN('AgentJobs','BlockingSnapshot','Database')

--replace old defaults
UPDATE CollectionDatesThresholds
SET WarningThreshold=1445,
	CriticalThreshold=2880
WHERE Reference IN('Drivers','OSLoadedModules')
AND WarningThreshold=125
AND CriticalThreshold=180

DELETE CD 
FROM dbo.CollectionDates CD
WHERE Reference='DatabaseHADR'
AND NOT EXISTS(SELECT 1 
		FROM dbo.DatabasesHADR HA
		JOIN dbo.Databases D ON D.DatabaseID = HA.DatabaseID
		WHERE D.InstanceID = CD.InstanceID)

INSERT INTO dbo.InstanceUptimeThresholds
(
    InstanceID,
    WarningThreshold,
    CriticalThreshold
)
SELECT -1 AS InstanceID,2880,720
WHERE NOT EXISTS(SELECT 1 FROM dbo.InstanceUptimeThresholds T WHERE T.InstanceID = -1)

INSERT INTO dbo.DBVersionHistory(DeployDate,Version)
VALUES(GETUTCDATE(),'$(VersionNumber)')

MERGE INTO [DBConfigOptions] AS [Target]
USING (VALUES
  (1,N'MAXDOP',N'0')
 ,(2,N'LEGACY_CARDINALITY_ESTIMATION',N'0')
 ,(3,N'PARAMETER_SNIFFING',N'1')
 ,(4,N'QUERY_OPTIMIZER_HOTFIXES',N'0')
 ,(6,N'IDENTITY_CACHE',N'1')
 ,(7,N'INTERLEAVED_EXECUTION_TVF',N'1')
 ,(8,N'BATCH_MODE_MEMORY_GRANT_FEEDBACK',N'1')
 ,(9,N'BATCH_MODE_ADAPTIVE_JOINS',N'1')
 ,(10,N'TSQL_SCALAR_UDF_INLINING',N'1')
 ,(11,N'ELEVATE_ONLINE',N'OFF')
 ,(12,N'ELEVATE_RESUMABLE',N'OFF')
 ,(13,N'OPTIMIZE_FOR_AD_HOC_WORKLOADS',N'0')
 ,(14,N'XTP_PROCEDURE_EXECUTION_STATISTICS',N'0')
 ,(15,N'XTP_QUERY_EXECUTION_STATISTICS',N'0')
 ,(16,N'ROW_MODE_MEMORY_GRANT_FEEDBACK',N'1')
 ,(17,N'ISOLATE_SECURITY_POLICY_CARDINALITY',N'0')
 ,(18,N'BATCH_MODE_ON_ROWSTORE',N'1')
 ,(19,N'DEFERRED_COMPILATION_TV',N'1')
 ,(20,N'ACCELERATED_PLAN_FORCING',N'1')
 ,(21,N'GLOBAL_TEMPORARY_TABLE_AUTO_DROP',N'1')
 ,(22,N'LIGHTWEIGHT_QUERY_PROFILING',N'1')
 ,(23,N'VERBOSE_TRUNCATION_WARNINGS',N'1')
 ,(24,N'LAST_QUERY_PLAN_STATS',N'0')
 ,(25,N'PAUSED_RESUMABLE_INDEX_ABORT_DURATION_MINUTES',N'1440')
 ,(26,N'DW_COMPATIBILITY_LEVEL',N'0')
 ,(27,N'EXEC_QUERY_STATS_FOR_SCALAR_FUNCTIONS',N'1')
 ,(29,N'ASYNC_STATS_UPDATE_WAIT_AT_LOW_PRIORITY',N'0')
) AS [Source] ([configuration_id],[name],[default_value])
ON ([Target].[configuration_id] = [Source].[configuration_id])
WHEN MATCHED AND (
	NULLIF([Source].[name], [Target].[name]) IS NOT NULL OR NULLIF([Target].[name], [Source].[name]) IS NOT NULL OR 
	NULLIF([Source].[default_value], [Target].[default_value]) IS NOT NULL OR NULLIF([Target].[default_value], [Source].[default_value]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[name] = [Source].[name], 
  [Target].[default_value] = [Source].[default_value]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([configuration_id],[name],[default_value])
 VALUES([Source].[configuration_id],[Source].[name],[Source].[default_value]);

IF NOT EXISTS(SELECT 1 FROM dbo.LastGoodCheckDBThresholds WHERE InstanceID=-1 AND DatabaseID=-1)
BEGIN
	INSERT INTO dbo.LastGoodCheckDBThresholds(InstanceID,DatabaseID,WarningThresholdHrs,CriticalThresholdHrs)
	VALUES(-1,-1,192,360)
END

MERGE INTO [CounterMapping] AS [Target]
USING (VALUES
  (N'SQLServer:Latches',N'Average Latch Wait Time (ms)',N'Average Latch Wait Time Base')
 ,(N'SQLServer:Locks',N'Average Wait Time (ms)',N'Average Wait Time Base')
 ,(N'SQLServer:Resource Pool Stats',N'Avg Disk Read IO (ms)',N'Avg Disk Read IO (ms) Base')
 ,(N'SQLServer:Resource Pool Stats',N'Avg Disk Write IO (ms)',N'Avg Disk Write IO (ms) Base')
 ,(N'SQLServer:HTTP Storage',N'Avg. Bytes/Read',N'Avg. Bytes/Read BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. Bytes/Transfer',N'Avg. Bytes/Transfer BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. Bytes/Write',N'Avg. Bytes/Write BASE')
 ,(N'SQLServer:Broker TO Statistics',N'Avg. Length of Batched Writes',N'Avg. Length of Batched Writes BS')
 ,(N'SQLServer:HTTP Storage',N'Avg. microsec/Read',N'Avg. microsec/Read BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. microsec/Read Comp',N'Avg. microsec/Read Comp BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. microsec/Transfer',N'Avg. microsec/Transfer BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. microsec/Write',N'Avg. microsec/Write BASE')
 ,(N'SQLServer:HTTP Storage',N'Avg. microsec/Write Comp',N'Avg. microsec/Write Comp BASE')
 ,(N'SQLServer:Broker TO Statistics',N'Avg. Time Between Batches (ms)',N'Avg. Time Between Batches Base')
 ,(N'SQLServer:Broker TO Statistics',N'Avg. Time to Write Batch (ms)',N'Avg. Time to Write Batch Base')
 ,(N'SQLServer:Buffer Manager',N'Buffer cache hit ratio',N'Buffer cache hit ratio base')
 ,(N'SQLServer:Catalog Metadata',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
 ,(N'SQLServer:Cursor Manager by Type',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
 ,(N'SQLServer:Plan Cache',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
 ,(N'SQLServer:Resource Pool Stats',N'CPU delayed %',N'CPU delayed % base')
 ,(N'SQLServer:Workload Group Stats',N'CPU delayed %',N'CPU delayed % base')
 ,(N'SQLServer:Resource Pool Stats',N'CPU effective %',N'CPU effective % base')
 ,(N'SQLServer:Workload Group Stats',N'CPU effective %',N'CPU effective % base')
 ,(N'SQLServer:Resource Pool Stats',N'CPU usage %',N'CPU usage % base')
 ,(N'SQLServer:Workload Group Stats',N'CPU usage %',N'CPU usage % base')
 ,(N'SQLServer:Databases',N'Log Cache Hit Ratio',N'Log Cache Hit Ratio Base')
 ,(N'SQLServer:Broker/DBM Transport',N'Msg Fragment Recv Size Avg',N'Msg Fragment Recv Size Avg Base')
 ,(N'SQLServer:Broker/DBM Transport',N'Msg Fragment Send Size Avg',N'Msg Fragment Send Size Avg Base')
 ,(N'SQLServer:Broker/DBM Transport',N'Receive I/O Len Avg',N'Receive I/O Len Avg Base')
 ,(N'SQLServer:Columnstore',N'Segment Cache Hit Ratio',N'Segment Cache Hit Ratio Base')
 ,(N'SQLServer:Broker/DBM Transport',N'Send I/O Len Avg',N'Send I/O Len Avg Base')
 ,(N'SQLServer:Transactions',N'Update conflict ratio',N'Update conflict ratio base')
 ,(N'SQLServer:Access Methods',N'Worktables From Cache Ratio',N'Worktables From Cache Base')
) AS [Source] ([object_name],[counter_name],[base_counter_name])
ON ([Target].[counter_name] = [Source].[counter_name] AND [Target].[object_name] = [Source].[object_name])
WHEN MATCHED AND (
	NULLIF([Source].[base_counter_name], [Target].[base_counter_name]) IS NOT NULL OR NULLIF([Target].[base_counter_name], [Source].[base_counter_name]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[base_counter_name] = [Source].[base_counter_name]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([object_name],[counter_name],[base_counter_name])
 VALUES([Source].[object_name],[Source].[counter_name],[Source].[base_counter_name]);

 IF NOT EXISTS(SELECT 1 FROM dbo.AzureDBElasticPoolStorageThresholds)
BEGIN
	INSERT INTO dbo.AzureDBElasticPoolStorageThresholds
	(
		PoolID,
		WarningThreshold,
		CriticalThreshold
	)
	VALUES(-1,0.8,0.9)
END
IF NOT EXISTS(SELECT 1 FROM dbo.DBFileThresholds WHERE InstanceID =-1 AND DatabaseID = -1 AND data_space_id=-1)
BEGIN
	-- Data file threshold root level default
	INSERT INTO dbo.DBFileThresholds
	(
		InstanceID,
		DatabaseID,
		data_space_id,
		FreeSpaceWarningThreshold,
		FreeSpaceCriticalThreshold,
		FreeSpaceCheckType,
		PctMaxSizeWarningThreshold,
		PctMaxSizeCriticalThreshold,
		FreeSpaceCheckZeroAutogrowthOnly
	)
	VALUES
	(   -1,
		-1,
		-1,
		0.2,
		0.1,
		'%',  
		0.8, 
		0.9, 
		1 
		)
END
IF NOT EXISTS(SELECT 1 FROM dbo.DBFileThresholds WHERE InstanceID =-1 AND DatabaseID = -1 AND data_space_id=0)
BEGIN
	-- Log file threshold root level default
	INSERT INTO dbo.DBFileThresholds
	(
		InstanceID,
		DatabaseID,
		data_space_id,
		FreeSpaceWarningThreshold,
		FreeSpaceCriticalThreshold,
		FreeSpaceCheckType,
		PctMaxSizeWarningThreshold,
		PctMaxSizeCriticalThreshold,
		FreeSpaceCheckZeroAutogrowthOnly
	)
	VALUES
	(   -1,
		-1,
		0,
		0.2,
		0.1,
		'%',  
		0.8, 
		0.9, 
		0
		)
END

EXEC dbo.Partitions_Add

-- transition to 2.0.3.6
IF NOT EXISTS(SELECT * FROM dbo.PerformanceCounters_60MIN)
BEGIN
	INSERT INTO dbo.PerformanceCounters_60MIN
	(
		InstanceID,
		CounterID,
		SnapshotDate,
		Value_Total,
		Value_Min,
		Value_Max,
		SampleCount
	)
	SELECT PC.InstanceID,
			PC.CounterID,
			DG.DateGroup,
			SUM(PC.Value),
			MIN(PC.Value),
			MAX(PC.Value),
			COUNT(*)  
	FROM dbo.PerformanceCounters PC
	CROSS APPLY [dbo].[DateGroupingMins](PC.SnapshotDate,60) DG
	GROUP BY  PC.InstanceID,
			PC.CounterID,
			DG.DateGroup
END

IF NOT EXISTS(SELECT 1 FROM dbo.AgentJobThresholds WHERE InstanceId=-1 AND job_id = '00000000-0000-0000-0000-000000000000')
BEGIN
	INSERT INTO dbo.AgentJobThresholds(InstanceId,job_id,TimeSinceLastFailureCritical,TimeSinceLastFailureWarning,LastFailIsCritical,LastFailIsWarning)
	SELECT -1,'00000000-0000-0000-0000-000000000000',60,10080,1,0
END

INSERT INTO dbo.Settings(SettingName,SettingValue)
SELECT SettingName,CAST('19000101' AS DATETIME) 
FROM (VALUES('PurgeCollectionErrorLog_StartDate'),
			('PurgeCollectionErrorLog_CompletedDate'),
			('PurgeQueryText_StartDate'),
			('PurgeQueryText_CompletedDate'),
			('PurgeQueryPlans_StartDate'),
			('PurgeQueryPlans_CompletedDate'),
			('PurgePartitions_StartDate'),
			('PurgePartitions_CompletedDate'),
			('MemoryDumpAckDate')
	  ) T(SettingName)
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.Settings S
				WHERE S.SettingName=T.SettingName);

INSERT INTO dbo.Settings(SettingName,SettingValue)
SELECT SettingName,SettingValue 
FROM (VALUES('MemoryDumpCriticalThresholdHrs',48),
		('MemoryDumpWarningThresholdHrs',168)) T(SettingName,SettingValue)
WHERE NOT EXISTS(
	SELECT 1 
	FROM dbo.Settings S 
	WHERE S.SettingName = T.SettingName
	)

/* From: https://docs.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-os-memory-clerks-transact-sql?view=sql-server-ver15 */
MERGE INTO dbo.MemoryClerkType AS T
USING (VALUES
('CACHESTORE_BROKERDSH','This cache store is used to store allocations by Service Broker Dialog Security Header Cache'),
('CACHESTORE_BROKERKEK','This cache store is used to store allocations by Service Broker Key Exchange Key Cache'),
('CACHESTORE_BROKERREADONLY','This cache store is used to store allocations by Service Broker Read Only Cache'),
('CACHESTORE_BROKERRSB','This cache store is used to store allocations by Service Broker Remote Service Binding Cache.'),
('CACHESTORE_BROKERTBLACS','This cache store is used to store allocations by Service Broker for security access structures.'),
('CACHESTORE_BROKERTO','This cache store is used to store allocations by Service Broker Transmission Object Cache'),
('CACHESTORE_BROKERUSERCERTLOOKUP','This cache store is used to store allocations by Service Broker user certificates lookup cache'),
('CACHESTORE_COLUMNSTOREOBJECTPOOL','This cache store is used for allocations by Columnstore Indexes for segments and dictionaries'),
('CACHESTORE_CONVPRI','This cache store is used to store allocations by Service Broker to keep track of Conversations priorities'),
('CACHESTORE_EVENTS','This cache store is used to store allocations by Service Broker Event Notifications'),
('CACHESTORE_FULLTEXTSTOPLIST','This memory clerk is used for allocations by Full-Text engine for stoplist functionality.'),
('CACHESTORE_NOTIF','This cache store is used for allocations by Query Notification functionality'),
('CACHESTORE_OBJCP','This cache store is used for caching objects with compiled plans (CP): stored procedures, functions, triggers. To illustrate, after a query plan for a stored procedure is created, its plan is stored in this cache.'),
('CACHESTORE_PHDR','This cache store is used for temporary memory caching during parsing for views, constraints, and defaults algebrizer trees during compilation of a query. Once query is parsed, the memory should be released. Some examples include: many statements in one batch - thousands of inserts or updates into one batch, a T-SQL batch that contains a large dynamically generated query, a large number of values in an IN clause.'),
('CACHESTORE_QDSRUNTIMESTATS','This cache store is used to cache Query Store runtime statistics'),
('CACHESTORE_SEARCHPROPERTYLIST','This cache store is used for allocations by Full-Text engine for Property List Cache'),
('CACHESTORE_SEHOBTCOLUMNATTRIBUTE','This cache store is used by storage engine for caching Heap or B-Tree (HoBT) column metadata structures.'),
('CACHESTORE_SQLCP','This cache store is used for caching ad hoc queries, prepared statements, and server-side cursors in plan cache. Ad hoc queries are commonly language-event T-SQL statements submitted to the server without explicit parameterization. Prepared statements also use this cache store - they are submitted by the application using API calls like SQLPrepare()/ SQLExecute (ODBC) or SqlCommand.Prepare/SqlCommand.ExecuteNonQuery (ADO.NET) and will appear on the server as sp_prepare/sp_execute or sp_prepexec system procedure executions. Also, server-side cursors would consume from this cache store (sp_cursoropen, sp_cursorfetch, sp_cursorclose).'),
('CACHESTORE_STACKFRAMES','This cache store is used for allocations of internal SQL OS structures related to stack frames.'),
('CACHESTORE_SYSTEMROWSET','This cache store is used for allocations of internal structures related to transaction logging and recovery.'),
('CACHESTORE_TEMPTABLES','This cache store is used for allocations related to temporary tables and table variables caching - part of plan cache.'),
('CACHESTORE_VIEWDEFINITIONS','This cache store is used for caching view definitions as part of query optimization.'),
('CACHESTORE_XML_SELECTIVE_DG','This cache store is used to cache XML structures for XML processing.'),
('CACHESTORE_XMLDBATTRIBUTE','This cache store is used to cache XML attribute structures for XML activity like XQuery.'),
('CACHESTORE_XMLDBELEMENT','This cache store is used to cache XML element structures for XML activity like XQuery.'),
('CACHESTORE_XMLDBTYPE','This cache store is used to cache XML structures for XML activity like XQuery.'),
('CACHESTORE_XPROC','This cache store is used for caching structures for Extended Stored procedures (Xprocs) in plan cache.'),
('MEMORYCLERK_BACKUP','This memory clerk is used for various allocations by Backup functionality'),
('MEMORYCLERK_BHF','This memory clerk is used for allocations for binary large objects (BLOB) management during query execution (Blob Handle support)'),
('MEMORYCLERK_BITMAP','This memory clerk is used for allocations by SQL OS functionality for bitmap filtering'),
('MEMORYCLERK_CSILOBCOMPRESSION','This memory clerk is used for allocations by Columnstore Index binary large objects (BLOB) Compression'),
('MEMORYCLERK_DRTLHEAP','This memory clerk is used for allocations by SQL OS functionality'),
('MEMORYCLERK_EXPOOL','This memory clerk is used for allocations by SQL OS functionality'),
('MEMORYCLERK_EXTERNAL_EXTRACTORS','This memory clerk is used for allocations by query execution engine for batch mode operations'),
('MEMORYCLERK_FILETABLE','This memory clerk is used for various allocations by FileTables functionality.'),
('MEMORYCLERK_FSAGENT','This memory clerk is used for various allocations by FILESTREAM functionality.'),
('MEMORYCLERK_FSCHUNKER','This memory clerk is used for various allocations by FILESTREAM functionality for creating filestream chunks.'),
('MEMORYCLERK_FULLTEXT','This memory clerk is used for allocations by Full-Text engine structures.'),
('MEMORYCLERK_FULLTEXT_SHMEM','This memory clerk is used for allocations by Full-Text engine structures related to Shared memory connectivity with the Full Text Daemon process.'),
('MEMORYCLERK_HADR','This memory clerk is used for memory allocations by AlwaysOn functionality'),
('MEMORYCLERK_HOST','This memory clerk is used for allocations by SQL OS functionality.'),
('MEMORYCLERK_LANGSVC','This memory clerk is used for allocations by SQL T-SQL statements and commands (parser, algebrizer, etc.)'),
('MEMORYCLERK_LWC','This memory clerk is used for allocations by Full-Text Semantic Search engine'),
('MEMORYCLERK_POLYBASE','This memory clerk keeps track of memory allocations for Polybase functionality inside SQL Server.'),
('MEMORYCLERK_QSRANGEPREFETCH','This memory clerk is used for allocations during query execution for query scan range prefetch.'),
('MEMORYCLERK_QUERYDISKSTORE','This memory clerk is used by Query Store memory allocations inside SQL Server.'),
('MEMORYCLERK_QUERYDISKSTORE_HASHMAP','This memory clerk is used by Query Store memory allocations inside SQL Server.'),
('MEMORYCLERK_QUERYDISKSTORE_STATS','This memory clerk is used by Query Store memory allocations inside SQL Server.'),
('MEMORYCLERK_QUERYPROFILE','This memory clerk is used for during server startup to enable query profiling'),
('MEMORYCLERK_RTLHEAP','This memory clerk is used for allocations by SQL OS functionality.'),
('MEMORYCLERK_SECURITYAPI','This memory clerk is used for allocations by SQL OS functionality.'),
('MEMORYCLERK_SERIALIZATION','Internal use only'),
('MEMORYCLERK_SLOG','This memory clerk is used for allocations by sLog (secondary in-memory log stream) in Accelerated Database Recovery'),
('MEMORYCLERK_SNI','This memory clerk allocates memory for the Server Network Interface (SNI) components. SNI manages connectivity and TDS packets for SQL Server'),
('MEMORYCLERK_SOSMEMMANAGER','This memory clerk allocates structures for SQLOS (SOS) thread scheduling and memory and I/O management..'),
('MEMORYCLERK_SOSNODE','This memory clerk allocates structures for SQLOS (SOS) thread scheduling and memory and I/O management.'),
('MEMORYCLERK_SOSOS','This memory clerk allocates structures for SQLOS (SOS) thread scheduling and memory and I/O management..'),
('MEMORYCLERK_SPATIAL','This memory clerk is used by Spatial Data components for memory allocations.'),
('MEMORYCLERK_SQLBUFFERPOOL','This memory clerk keeps track of commonly the largest memory consumer inside SQL Server - data and index pages. Buffer Pool or data cache keeps data and index pages loaded in memory to provide fast access to data.'),
('MEMORYCLERK_SQLCLR','This memory clerk is used for allocations by SQLCLR .'),
('MEMORYCLERK_SQLCLRASSEMBLY','This memory clerk is used for allocations for SQLCLR assemblies.'),
('MEMORYCLERK_SQLCONNECTIONPOOL','This memory clerk caches information on the server that the client application may need the server to keep track of. One example is an application that creates prepare handles via sp_prepexecrpc. The application should properly unprepare (close) those handles after execution.'),
('MEMORYCLERK_SQLEXTENSIBILITY','This memory clerk is used for allocations by the Extensibility Framework for running external Python or R scripts on SQL Server.'),
('MEMORYCLERK_SQLGENERAL','This memory clerk could be used by multiple consumers inside SQL engine. Examples include replication memory, internal debugging/diagnostics, some SQL Server startup functionality, some SQL parser functionality, building system indexes, initialize global memory objects, Create OLEDB connection inside the server and Linked Server queries, Server-side Profiler tracing, creating showplan data, some security functionality, compilation of computed columns, memory for Parallelism structures, memory for some XML functionality'),
('MEMORYCLERK_SQLHTTP','Deprecated'),
('MEMORYCLERK_SQLLOGPOOL','This memory clerk is used by SQL Server Log Pool. Log Pool is a cache used to improve performance when reading the transaction log. Specifically it improves log cache utilization during multiple log reads, reduces disk I/O log reads and allows sharing of log scans. Primary consumers of log pool are AlwaysOn (Change Capture and Send), Redo Manager, Database Recovery - Analysis/Redo/Undo, Transaction Runtime Rollback, Replication/CDC, Backup/Restore.'),
('MEMORYCLERK_SQLOPTIMIZER','This memory clerk is used for memory allocations during different phases of compiling a query. Some uses include query optimization, index statistics manager, view definitions compilation, histogram generation.'),
('MEMORYCLERK_SQLQERESERVATIONS','This memory clerk is used for Memory Grant allocations, that is memory allocated to queries to perform sort and hash operations during query execution.'),
('MEMORYCLERK_SQLQUERYCOMPILE','This memory clerk is used by Query optimizer for allocating memory during query compiling.'),
('MEMORYCLERK_SQLQUERYEXEC','This memory clerk is used for allocations in the following areas: Batch mode processing, Parallel query execution, query execution context, spatial index tessellation, sort and hash operations (sort tables, hash tables), some DVM processing, update statistics execution'),
('MEMORYCLERK_SQLQUERYPLAN','This memory clerk is used for allocations by Heap page management, DBCC CHECKTABLE allocations, and sp_cursor* stored procedure allocations'),
('MEMORYCLERK_SQLSERVICEBROKER','This memory clerk is used by SQL Server Service Broker memory allocations.'),
('MEMORYCLERK_SQLSERVICEBROKERTRANSPORT','This memory clerk is used by SQL Server Service Broker transport memory allocations.'),
('MEMORYCLERK_SQLSLO_OPERATIONS','This memory clerk is used to gather performance statistics'),
('MEMORYCLERK_SQLSOAP','Deprecated'),
('MEMORYCLERK_SQLSOAPSESSIONSTORE','Deprecated'),
('MEMORYCLERK_SQLSTORENG','This memory clerk is used for allocations by multiple storage engine components. Examples of components include structures for database files, database snapshot replica file manager, deadlock monitor, DBTABLE structures, Log manager structures, some tempdb versioning structures, some server startup functionality, execution context for child threads in parallel queries.'),
('MEMORYCLERK_SQLTRACE','This memory clerk is used for server-side SQL Trace memory allocations.'),
('MEMORYCLERK_SQLUTILITIES','This memory clerk can be used by multiple allocators inside SQL Server. Examples include Backup and Restore, Log Shipping, Database Mirroring, DBCC commands, BCP code on the server side, some query parallelism work, Log Scan buffers.'),
('MEMORYCLERK_SQLXML','This memory clerk is used for memory allocations when performing XML operations.'),
('MEMORYCLERK_SQLXP','This memory clerk is used for memory allocations when calling SQL Server Extended Stored procedures.'),
('MEMORYCLERK_SVL','This memory clerk is used used for allocations of internal SQL OS structures'),
('MEMORYCLERK_TEST','Internal use only'),
('MEMORYCLERK_UNITTEST','Internal use only'),
('MEMORYCLERK_WRITEPAGERECORDER','This memory clerk is used for allocations by Write Page Recorder.'),
('MEMORYCLERK_XE','This memory clerk is used for Extended Events memory allocations'),
('MEMORYCLERK_XE_BUFFER','This memory clerk is used for Extended Events memory allocations'),
('MEMORYCLERK_XLOG_SERVER','This memory clerk is used for allocations by Xlog used for log file management in SQL Azure Database'),
('MEMORYCLERK_XTP','This memory clerk is used for In-Memory OLTP memory allocations.'),
('OBJECTSTORE_LBSS','This object store is used to allocate temporary LOBs - variables, parameters, and intermediate results for expressions. An example that uses this store is table-valued parameters (TVP) . See the KB article 4468102 and KB article 4051359 for more information on fixes in this space.'),
('OBJECTSTORE_LOCK_MANAGER','This memory clerk keeps track of allocations made by the Lock Manager in SQL Server.'),
('OBJECTSTORE_SECAUDIT_EVENT_BUFFER','This object store is used for SQL Server Audit memory allocations.'),
('OBJECTSTORE_SERVICE_BROKER','This object store is used by Service Broker'),
('OBJECTSTORE_SNI_PACKET','This object store is used by Server Network Interface (SNI) components which manage connectivity'),
('OBJECTSTORE_XACT_CACHE','This object store is used to cache transactions information'),
('USERSTORE_DBMETADATA','This object store is used for metadata structures'),
('USERSTORE_OBJPERM','This store is used for structures keeping track of object security/permission'),
('USERSTORE_QDSSTMT','This cache store is used to cache Query Store statements'),
('USERSTORE_SCHEMAMGR','Schema manager cache stores different types of metadata information about the database objects in memory (e.g tables). A common user of this store could be the tempdb database with objects like tables, temp procedures, table variables, table-valued parameters, worktables, workfiles, version store.'),
('USERSTORE_SXC','This user store is used for allocations to store all RPC parameters.'),
('USERSTORE_TOKENPERM','TokenAndPermUserStore is a single SOS user store that keeps track of security entries for security context, login, user, permission, and audit. Multiple hash tables are allocated to store these objects.')
) AS S(MemoryClerkType,MemoryClerkDescription)
ON S.MemoryClerkType = T.MemoryClerkType
WHEN MATCHED AND (NULLIF(S.MemoryClerkDescription,T.MemoryClerkDescription) IS NOT NULL OR NULLIF(T.MemoryClerkDescription,S.MemoryClerkDescription) IS NOT NULL) THEN 
UPDATE SET 
T.MemoryClerkDescription = S.MemoryClerkDescription
WHEN NOT MATCHED BY TARGET THEN
INSERT(MemoryClerkType,MemoryClerkDescription)
VALUES(S.MemoryClerkType,S.MemoryClerkDescription);
