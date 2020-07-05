/*
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
EXEC dbo.Partitions_Add
GO

INSERT INTO dbo.DataRetention
(
    TableName,
    RetentionDays
)
SELECT t.TableName,t.RetentionDays
FROM (VALUES('ProcStats',180),
				('FunctionStats',180),
				('Waits',180),
				('IOStats',365),
				('CPU',365),
				('BlockingSnapshot',120)
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
		TimeSinceLastCriticalThreshold
	)
	VALUES
	( -1, -1, 1440, 2880, 1440, 2880 )
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