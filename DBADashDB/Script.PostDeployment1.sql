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
/* Security */
GRANT SELECT ON SCHEMA::dbo TO App
GRANT EXECUTE ON SCHEMA::dbo TO App;
GRANT SELECT ON SCHEMA::dbo TO Reports
GRANT EXECUTE ON SCHEMA::Report TO Reports;
GRANT SELECT ON SCHEMA::DBADash TO App;
GRANT EXECUTE ON SCHEMA::DBADash TO App;
/************/
MERGE INTO [dbo].[SysConfigOptions] AS [Target]
USING (VALUES
  (101,N'recovery interval (min)',N'Maximum recovery interval in minutes',1,1,0,0,32767)
 ,(102,N'allow updates',N'Allow updates to system tables',1,0,0,0,1)
 ,(103,N'user connections',N'Number of user connections allowed',0,1,0,0,32767)
 ,(106,N'locks',N'Number of locks for all users',0,1,0,5000,2147483647)
 ,(107,N'open objects',N'Number of open database objects',0,1,0,0,2147483647)
 ,(109,N'fill factor (%)',N'Default fill factor percentage',0,1,0,0,100)
 ,(114,N'disallow results from triggers',N'Disallow returning results from triggers',1,1,0,0,1)
 ,(115,N'nested triggers',N'Allow triggers to be invoked within triggers',1,0,1,0,1)
 ,(116,N'server trigger recursion',N'Allow recursion for server level triggers',1,0,1,0,1)
 ,(117,N'remote access',N'Allow remote access',0,0,1,0,1)
 ,(124,N'default language',N'default language',1,0,0,0,9999)
 ,(400,N'cross db ownership chaining',N'Allow cross db ownership chaining',1,0,0,0,1)
 ,(503,N'max worker threads',N'Maximum worker threads',1,1,0,128,65535)
 ,(505,N'network packet size (B)',N'Network packet size',1,1,4096,512,32767)
 ,(518,N'show advanced options',N'show advanced options',1,0,0,0,1)
 ,(542,N'remote proc trans',N'Create DTC transaction for remote procedures',1,0,0,0,1)
 ,(544,N'c2 audit mode',N'c2 audit mode',0,1,0,0,1)
 ,(1126,N'default full-text language',N'default full-text language',1,1,1033,0,2147483647)
 ,(1127,N'two digit year cutoff',N'two digit year cutoff',1,1,2049,1753,9999)
 ,(1505,N'index create memory (KB)',N'Memory for index create sorts (kBytes)',1,1,0,704,2147483647)
 ,(1517,N'priority boost',N'Priority boost',0,1,0,0,1)
 ,(1519,N'remote login timeout (s)',N'remote login timeout',1,0,10,0,2147483647)
 ,(1520,N'remote query timeout (s)',N'remote query timeout',1,0,600,0,2147483647)
 ,(1531,N'cursor threshold',N'cursor threshold',1,1,-1,-1,2147483647)
 ,(1532,N'set working set size',N'set working set size',0,1,0,0,1)
 ,(1534,N'user options',N'user options',1,0,0,0,32767)
 ,(1535,N'affinity mask',N'affinity mask',1,1,0,-2147483648,2147483647)
 ,(1536,N'max text repl size (B)',N'Maximum size of a text field in replication.',1,0,65536,-1,2147483647)
 ,(1537,N'media retention',N'Tape retention period in days',1,1,0,0,365)
 ,(1538,N'cost threshold for parallelism',N'cost threshold for parallelism',1,1,5,0,32767)
 ,(1539,N'max degree of parallelism',N'maximum degree of parallelism',1,1,0,0,32767)
 ,(1540,N'min memory per query (KB)',N'minimum memory per query (kBytes)',1,1,1024,512,2147483647)
 ,(1541,N'query wait (s)',N'maximum time to wait for query memory (s)',1,1,-1,-1,2147483647)
 ,(1543,N'min server memory (MB)',N'Minimum size of server memory (MB)',1,1,0,0,2147483647)
 ,(1544,N'max server memory (MB)',N'Maximum size of server memory (MB)',1,1,2147483647,128,2147483647)
 ,(1545,N'query governor cost limit',N'Maximum estimated cost allowed by query governor',1,1,0,0,2147483647)
 ,(1546,N'lightweight pooling',N'User mode scheduler uses lightweight pooling',0,1,0,0,1)
 ,(1547,N'scan for startup procs',N'scan for startup stored procedures',0,1,0,0,1)
 ,(1549,N'affinity64 mask',N'affinity64 mask',1,1,0,-2147483648,2147483647)
 ,(1550,N'affinity I/O mask',N'affinity I/O mask',0,1,0,-2147483648,2147483647)
 ,(1551,N'affinity64 I/O mask',N'affinity64 I/O mask',0,1,0,-2147483648,2147483647)
 ,(1555,N'transform noise words',N'Transform noise words for full-text query',1,1,0,0,1)
 ,(1556,N'precompute rank',N'Use precomputed rank for full-text query',1,1,0,0,1)
 ,(1557,N'PH timeout (s)',N'DB connection timeout for full-text protocol handler (s)',1,1,60,1,3600)
 ,(1562,N'clr enabled',N'CLR user code execution enabled in the server',1,0,0,0,1)
 ,(1563,N'max full-text crawl range',N'Maximum  crawl ranges allowed in full-text indexing',1,1,4,0,256)
 ,(1564,N'ft notify bandwidth (min)',N'Number of reserved full-text notifications buffers',1,1,0,0,32767)
 ,(1565,N'ft notify bandwidth (max)',N'Max number of full-text notifications buffers',1,1,100,0,32767)
 ,(1566,N'ft crawl bandwidth (min)',N'Number of reserved full-text crawl buffers',1,1,0,0,32767)
 ,(1567,N'ft crawl bandwidth (max)',N'Max number of full-text crawl buffers',1,1,100,0,32767)
 ,(1568,N'default trace enabled',N'Enable or disable the default trace',1,1,1,0,1)
 ,(1569,N'blocked process threshold (s)',N'Blocked process reporting threshold',1,1,0,0,86400)
 ,(1570,N'in-doubt xact resolution',N'Recovery policy for DTC transactions with unknown outcome',1,1,0,0,2)
 ,(1576,N'remote admin connections',N'Dedicated Admin Connections are allowed from remote clients',1,0,0,0,1)
 ,(1577,N'common criteria compliance enabled',N'Common Criteria compliance mode enabled',0,1,0,0,1)
 ,(1578,N'EKM provider enabled',N'Enable or disable EKM provider',1,1,0,0,1)
 ,(1579,N'backup compression default',N'Enable compression of backups by default',1,0,0,0,1)
 ,(1580,N'filestream access level',N'Sets the FILESTREAM access level',1,0,0,0,2)
 ,(1581,N'optimize for ad hoc workloads',N'When this option is set, plan cache size is further reduced for single-use adhoc OLTP workload.',1,1,0,0,1)
 ,(1582,N'access check cache bucket count',N'Default hash bucket count for the access check result security cache',1,1,0,0,65536)
 ,(1583,N'access check cache quota',N'Default quota for the access check result security cache',1,1,0,0,2147483647)
 ,(1584,N'backup checksum default',N'Enable checksum of backups by default',1,0,0,0,1)
 ,(1585,N'automatic soft-NUMA disabled',N'Automatic soft-NUMA is enabled by default',0,1,0,0,1)
 ,(1586,N'external scripts enabled',N'Allows execution of external scripts',1,0,0,0,1)
 ,(1587,N'clr strict security',N'CLR strict security enabled in the server',1,1,NULL,0,1)
 ,(1588,N'column encryption enclave type',N'Type of enclave used for computations on encrypted columns',0,0,0,0,2)
 ,(1589,N'tempdb metadata memory-optimized',N'Tempdb metadata memory-optimized is disabled by default.',0,1,0,0,1)
 ,(1591,N'ADR cleaner retry timeout (min)',N'ADR cleaner retry timeout.',1,1,15,0,32767)
 ,(1592,N'ADR Preallocation Factor',N'ADR Preallocation Factor.',1,1,4,0,32767)
 ,(1593,N'version high part of SQL Server',N'version high part of SQL Server that model database copied for',1,1,1048576,-2147483648,2147483647)
 ,(1594,N'version low part of SQL Server',N'version low part of SQL Server that model database copied for',1,1,39321609,-2147483648,2147483647)
 ,(1595,N'Data processed daily limit in TB',N'SQL On-demand data processed daily limit in TB',1,0,2147483647,0,2147483647)
 ,(1596,N'Data processed weekly limit in TB',N'SQL On-demand data processed weekly limit in TB',1,0,2147483647,0,2147483647)
 ,(1597,N'Data processed monthly limit in TB',N'SQL On-demand data processed monthly limit in TB',1,0,2147483647,0,2147483647)
 ,(1598,N'ADR Cleaner Thread Count',N'Max number of threads ADR cleaner can assign.',1,1,1,1,32767)
 ,(1599,N'hardware offload enabled',N'Enable hardware offloading on the server',0,1,0,0,1)
 ,(1600,N'hardware offload config',N'Configure hardware offload accelerator',0,1,0,0,255)
 ,(1601,N'hardware offload mode',N'Configure hardware offload accelerator mode',0,1,0,0,255)
 ,(1602,N'backup compression algorithm',N'Configure default backup compression algorithm',1,0,0,0,2)
 ,(16384,N'Agent XPs',N'Enable or disable Agent XPs',1,1,1,0,1)
 ,(16386,N'Database Mail XPs',N'Enable or disable Database Mail XPs',1,1,0,0,1)
 ,(16387,N'SMO and DMO XPs',N'Enable or disable SMO and DMO XPs',1,1,1,0,1)
 ,(16388,N'Ole Automation Procedures',N'Enable or disable Ole Automation Procedures',1,1,0,0,1)
 ,(16390,N'xp_cmdshell',N'Enable or disable command shell',1,1,0,0,1)
 ,(16391,N'Ad Hoc Distributed Queries',N'Enable or disable Ad Hoc Distributed Queries',1,1,0,0,1)
 ,(16392,N'Replication XPs',N'Enable or disable Replication XPs',1,1,0,0,1)
 ,(16393,N'contained database authentication',N'Enables contained databases and contained authentication',1,0,0,0,1)
 ,(16394,N'hadoop connectivity',N'Configure SQL Server to connect to external Hadoop or Microsoft Azure storage blob data sources through PolyBase',1,0,0,0,8)
 ,(16395,N'polybase network encryption',N'Configure SQL Server to encrypt control and data channels when using PolyBase',1,0,1,0,1)
 ,(16396,N'remote data archive',N'Allow the use of the REMOTE_DATA_ARCHIVE data access for databases',1,0,0,0,1)
 ,(16397,N'allow polybase export',N'Allows writing into an external table using PolyBase',1,0,0,0,1)
 ,(16398,N'allow filesystem enumeration',N'Allow enumeration of filesystem',1,1,1,0,1)
 ,(16399,N'polybase enabled',N'Configure SQL Server to connect to external data sources through PolyBase',1,0,0,0,1)
 ,(16400,N'suppress recovery model errors',N'Return warning instead of error for unsupported ALTER DATABASE SET RECOVERY command',1,1,0,0,1)
 ,(16401,N'openrowset auto_create_statistics',N'Enable or disable auto create statistics for openrowset sources.',1,1,1,0,1)
) AS [Source] ([configuration_id],[name],[description],[is_dynamic],[is_advanced],[default_value],[minimum],[maximum])
ON ([Target].[configuration_id] = [Source].[configuration_id])
WHEN MATCHED AND (
	NULLIF([Source].[name], [Target].[name]) IS NOT NULL OR NULLIF([Target].[name], [Source].[name]) IS NOT NULL OR 
	NULLIF([Source].[description], [Target].[description]) IS NOT NULL OR NULLIF([Target].[description], [Source].[description]) IS NOT NULL OR 
	NULLIF([Source].[is_dynamic], [Target].[is_dynamic]) IS NOT NULL OR NULLIF([Target].[is_dynamic], [Source].[is_dynamic]) IS NOT NULL OR 
	NULLIF([Source].[is_advanced], [Target].[is_advanced]) IS NOT NULL OR NULLIF([Target].[is_advanced], [Source].[is_advanced]) IS NOT NULL OR 
	NULLIF([Source].[default_value], [Target].[default_value]) IS NOT NULL OR NULLIF([Target].[default_value], [Source].[default_value]) IS NOT NULL OR 
	NULLIF([Source].[minimum], [Target].[minimum]) IS NOT NULL OR NULLIF([Target].[minimum], [Source].[minimum]) IS NOT NULL OR 
	NULLIF([Source].[maximum], [Target].[maximum]) IS NOT NULL OR NULLIF([Target].[maximum], [Source].[maximum]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[name] = [Source].[name], 
  [Target].[description] = [Source].[description], 
  [Target].[is_dynamic] = [Source].[is_dynamic], 
  [Target].[is_advanced] = [Source].[is_advanced], 
  [Target].[default_value] = [Source].[default_value], 
  [Target].[minimum] = [Source].[minimum], 
  [Target].[maximum] = [Source].[maximum]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([configuration_id],[name],[description],[is_dynamic],[is_advanced],[default_value],[minimum],[maximum])
 VALUES([Source].[configuration_id],[Source].[name],[Source].[description],[Source].[is_dynamic],[Source].[is_advanced],[Source].[default_value],[Source].[minimum],[Source].[maximum]);

-- Wait descriptions from https://docs.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-os-wait-stats-transact-sql?view=sql-server-ver15
MERGE INTO dbo.WaitType AS [Target]
USING (VALUES
  (N'ABR',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'AM_INDBUILD_ALLOCATION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'AM_SCHEMAMGR_UNSHARED_CACHE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'ASSEMBLY_FILTER_HASHTABLE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'ASSEMBLY_LOAD',0,N'Occurs during exclusive access to assembly loading.')
 ,(N'ASYNC_DISKPOOL_LOCK',0,N'Occurs when there is an attempt to synchronize parallel threads that are performing tasks such as creating or initializing a file.')
 ,(N'ASYNC_IO_COMPLETION',0,N'Occurs when a task is waiting for asynchronous non-data I/Os to finish. Examples include I/O involved in warm standby log shipping, database mirroring, some bulk import related operations.')
 ,(N'ASYNC_NETWORK_IO',0,N'Occurs on network writes when the task is blocked waiting for the client application to acknowledge it has processed all the data sent to it. Verify that the client application is processing data from the server or. Reasons the client application cannot consume data fast enough include: application design issues like writing results to a file while the results arrive, waiting for user input, client-side filtering on a large dataset instead of server-side filtering, or an intentional wait introduced. Also the client computer may be experiencing slow response due to issues like low virtual/physical memory, 100% CPU consumption, etc. Network delays can also lead to this wait - typically caused by network drivers, firewalls or misconfigured routers.')
 ,(N'ASYNC_OP_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'ASYNC_OP_CONTEXT_READ',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'ASYNC_OP_CONTEXT_WRITE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'ASYNC_SOCKETDUP_IO',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'AUDIT_GROUPCACHE_LOCK',0,N'Occurs when there is a wait on a lock that controls access to a special cache. The cache contains information about which audits are being used to audit each audit action group.')
 ,(N'AUDIT_LOGINCACHE_LOCK',0,N'Occurs when there is a wait on a lock that controls access to a special cache. The cache contains information about which audits are being used to audit login audit action groups.')
 ,(N'AUDIT_ON_DEMAND_TARGET_LOCK',0,N'Occurs when there is a wait on a lock that is used to ensure single initialization of audit related Extended Event targets.')
 ,(N'AUDIT_XE_SESSION_MGR',0,N'Occurs when there is a wait on a lock that is used to synchronize the starting and stopping of audit related Extended Events sessions.')
 ,(N'BACKUP',0,N'Occurs when a task is blocked as part of backup processing.')
 ,(N'BACKUP_OPERATOR',0,N'Occurs when a task is waiting for a tape mount. To view the tape status, query sys.dm_io_backup_tapes. If a mount operation is not pending, this wait type may indicate a hardware problem with the tape drive.')
 ,(N'BACKUPBUFFER',0,N'Occurs when a backup task is waiting for data, or is waiting for a buffer in which to store data. This type is not typical, except when a task is waiting for a tape mount.')
 ,(N'BACKUPIO',0,N'Occurs when a backup task is waiting for data, or is waiting for a buffer in which to store data. This type is not typical, except when a task is waiting for a tape mount.')
 ,(N'BACKUPTHREAD',0,N'Occurs when a task is waiting for a backup task to finish. Wait times may be long, from several minutes to several hours. If the task that is being waited on is in an I/O process, this type does not indicate a problem.')
 ,(N'BAD_PAGE_PROCESS',0,N'Occurs when the background suspect page logger is trying to avoid running more than every five seconds. Excessive suspect pages cause the logger to run frequently.')
 ,(N'BLOB_METADATA',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'BMPALLOCATION',0,N'Occurs with parallel batch-mode plans when synchronizing the allocation of a large bitmap filter. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.    Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BMPBUILD',0,N'Occurs with parallel batch-mode plans when synchronizing the building of a large bitmap filter. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BMPREPARTITION',0,N'Occurs with parallel batch-mode plans when synchronizing the repartitioning of a large bitmap filter. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BMPREPLICATION',0,N'Occurs with parallel batch-mode plans when synchronizing the replication of a large bitmap filter across worker threads. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BPSORT',0,N'Occurs with parallel batch-mode plans when synchronizing the sorting of a dataset across multiple threads. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'BROKER_CONNECTION_RECEIVE_TASK',0,N'Occurs when waiting for access to receive a message on a connection endpoint. Receive access to the endpoint is serialized.')
 ,(N'BROKER_DISPATCHER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'BROKER_ENDPOINT_STATE_MUTEX',0,N'Occurs when there is contention to access the state of a Service Broker connection endpoint. Access to the state for changes is serialized.')
 ,(N'BROKER_EVENTHANDLER',0,N'Occurs when a task is waiting in the primary event handler of the Service Broker. This should occur very briefly.')
 ,(N'BROKER_FORWARDER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'BROKER_INIT',0,N'Occurs when initializing Service Broker in each active database. This should occur infrequently.')
 ,(N'BROKER_MASTERSTART',0,N'Occurs when a task is waiting for the primary event handler of the Service Broker to start. This should occur very briefly.')
 ,(N'BROKER_RECEIVE_WAITFOR',0,N'Occurs when the RECEIVE WAITFOR is waiting. This may mean that either no messages are ready to be received in the queue or a lock contention is preventing it from receiving messages from the queue.')
 ,(N'BROKER_REGISTERALLENDPOINTS',0,N'Occurs during the initialization of a Service Broker connection endpoint. This should occur very briefly.')
 ,(N'BROKER_SERVICE',0,N'Occurs when the Service Broker destination list that is associated with a target service is updated or re-prioritized.')
 ,(N'BROKER_SHUTDOWN',0,N'Occurs when there is a planned shutdown of Service Broker. This should occur very briefly, if at all.')
 ,(N'BROKER_START',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'BROKER_TASK_SHUTDOWN',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BROKER_TASK_STOP',0,N'Occurs when the Service Broker queue task handler tries to shut down the task. The state check is serialized and must be in a running state beforehand.')
 ,(N'BROKER_TASK_SUBMIT',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'BROKER_TO_FLUSH',0,N'Occurs when the Service Broker lazy flusher flushes the in-memory transmission objects to a work table.')
 ,(N'BROKER_TRANSMISSION_OBJECT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'BROKER_TRANSMISSION_TABLE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'BROKER_TRANSMISSION_WORK',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'BROKER_TRANSMITTER',0,N'Occurs when the Service Broker transmitter is waiting for work. Service Broker has a component known as the Transmitter which schedules messages from multiple dialogs to be sent across the wire over one or more connection endpoints. The transmitter has 2 dedicated threads for this purpose. This wait type is charged when these transmitter threads are waiting for dialog messages to be sent using the transport connections. High values of waiting_tasks_count for this wait type point to intermittent work for these transmitter threads and are not indications of any performance problem. If service broker is not used at all, waiting_tasks_count should be 2 (for the 2 transmitter threads) and wait_time_ms should be twice the duration since instance startup. See Service broker wait stats.')
 ,(N'BUILTIN_HASHKEY_MUTEX',0,N'May occur after startup of instance, while internal data structures are initializing. Will not recur once data structures have initialized.')
 ,(N'CHANGE_TRACKING_WAITFORCHANGES',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'CHECK_PRINT_RECORD',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'CHECK_SCANNER_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'CHECK_TABLES_INITIALIZATION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'CHECK_TABLES_SINGLE_SCAN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'CHECK_TABLES_THREAD_BARRIER',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'CHECKPOINT_QUEUE',0,N'Occurs while the checkpoint task is waiting for the next checkpoint request.')
 ,(N'CHKPT',0,N'Occurs at server startup to tell the checkpoint thread that it can start.')
 ,(N'CLEAR_DB',0,N'Occurs during operations that change the state of a database, such as opening or closing a database.')
 ,(N'CLR_AUTO_EVENT',0,N'Occurs when a task is currently performing common language runtime (CLR) execution and is waiting for a particular autoevent to be initiated. Long waits are typical, and do not indicate a problem.')
 ,(N'CLR_CRST',0,N'Occurs when a task is currently performing CLR execution and is waiting to enter a critical section of the task that is currently being used by another task.')
 ,(N'CLR_JOIN',0,N'Occurs when a task is currently performing CLR execution and waiting for another task to end. This wait state occurs when there is a join between tasks.')
 ,(N'CLR_MANUAL_EVENT',0,N'Occurs when a task is currently performing CLR execution and is waiting for a specific manual event to be initiated.')
 ,(N'CLR_MEMORY_SPY',0,N'Occurs during a wait on lock acquisition for a data structure that is used to record all virtual memory allocations that come from CLR. The data structure is locked to maintain its integrity if there is parallel access.')
 ,(N'CLR_MONITOR',0,N'Occurs when a task is currently performing CLR execution and is waiting to obtain a lock on the monitor.')
 ,(N'CLR_RWLOCK_READER',0,N'Occurs when a task is currently performing CLR execution and is waiting for a reader lock.')
 ,(N'CLR_RWLOCK_WRITER',0,N'Occurs when a task is currently performing CLR execution and is waiting for a writer lock.')
 ,(N'CLR_SEMAPHORE',0,N'Occurs when a task is currently performing CLR execution and is waiting for a semaphore.')
 ,(N'CLR_TASK_START',0,N'Occurs while waiting for a CLR task to complete startup.')
 ,(N'CLRHOST_STATE_ACCESS',0,N'Occurs where there is a wait to acquire exclusive access to the CLR-hosting data structures. This wait type occurs while setting up or tearing down the CLR runtime.')
 ,(N'CMEMPARTITIONED',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'CMEMTHREAD',0,N'Occurs when a task is waiting on a thread-safe memory object. The wait time might increase when there is contention caused by multiple tasks trying to allocate memory from the same memory object.')
 ,(N'COLUMNSTORE_BUILD_THROTTLE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'COLUMNSTORE_COLUMNDATASET_SESSION_LIST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'COMMIT_TABLE',0,N'Internal use only.')
 ,(N'CONNECTION_ENDPOINT_LOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'COUNTRECOVERYMGR',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'CREATE_DATINISERVICE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'CXCONSUMER',0,N'Occurs with parallel query plans when a consumer thread (parent) waits for a producer thread to send rows. CXCONSUMER waits are caused by an Exchange Iterator that runs out of rows from its producer thread. This is a normal part of parallel query execution.     Applies to : SQL Server (Starting with SQL Server 2016 (13.x) SP2, SQL Server 2017 (14.x) CU3), Azure SQL Database, Azure SQL Managed Instance')
 ,(N'CXPACKET',0,N'Occurs with parallel query plans when waiting to synchronize the Query Processor Exchange Iterator, and when producing and consuming rows. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the Cost Threshold for Parallelism or lowering the Max Degree of Parallelism (MaxDOP).     Note:  Starting with SQL Server 2016 (13.x) SP2 and SQL Server 2017 (14.x) CU3, CXPACKET only refers to waiting to synchronize the Exchange Iterator and producing rows. Threads consuming rows are tracked separately in the CXCONSUMER wait type. If the consumer threads are too slow, the Exchange Iterator buffer may become full and cause CXPACKET waits.     Note:  In Azure SQL Database and Azure SQL Managed Instance, CXPACKET only refers to waiting on threads producing rows. Exchange Iterator synchronization is tracked separately in the CXSYNC_PORT and CXSYNC_CONSUMER wait types. Threads consuming rows are tracked separately in the CXCONSUMER wait type.')
 ,(N'CXROWSET_SYNC',0,N'Occurs during a parallel range scan.')
 ,(N'CXSYNC_CONSUMER',0,N'Occurs with parallel query plans when waiting to reach an Exchange Iterator synchronization point among all consumer threads.     Applies to : Azure SQL Database, Azure SQL Managed Instance')
 ,(N'CXSYNC_PORT',0,N'Occurs with parallel query plans when waiting to open, close, and synchronize Exchange Iterator ports between producer and consumer threads. For example, if a query plan has a long sort operation, CXSYNC_PORT waits may be higher because the sort must complete before the Exchange Iterator port can be synchronized.     Applies to : Azure SQL Database, Azure SQL Managed Instance')
 ,(N'DAC_INIT',0,N'Occurs while the dedicated administrator connection is initializing.')
 ,(N'DBCC_SCALE_OUT_EXPR_CACHE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'DBMIRROR_DBM_EVENT',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'DBMIRROR_DBM_MUTEX',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'DBMIRROR_EVENTS_QUEUE',0,N'Occurs when database mirroring waits for events to process.')
 ,(N'DBMIRROR_SEND',0,N'Occurs when a task is waiting for a communications backlog at the network layer to clear to be able to send messages. Indicates that the communications layer is starting to become overloaded and affect the database mirroring data throughput.')
 ,(N'DBMIRROR_WORKER_QUEUE',0,N'Indicates that the database mirroring worker task is waiting for more work.')
 ,(N'DBMIRRORING_CMD',0,N'Occurs when a task is waiting for log records to be flushed to disk. This wait state is expected to be held for long periods of time.')
 ,(N'DBSEEDING_FLOWCONTROL',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'DBSEEDING_OPERATION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'DEADLOCK_ENUM_MUTEX',0,N'Occurs when the deadlock monitor and sys.dm_os_waiting_tasks try to make sure that SQL Server is not running multiple deadlock searches at the same time.')
 ,(N'DEADLOCK_TASK_SEARCH',0,N'Large waiting time on this resource indicates that the server is executing queries on top of sys.dm_os_waiting_tasks, and these queries are blocking deadlock monitor from running deadlock search. This wait type is used by deadlock monitor only. Queries on top of sys.dm_os_waiting_tasks use DEADLOCK_ENUM_MUTEX.')
 ,(N'DEBUG',0,N'Occurs during Transact-SQL and CLR debugging for internal synchronization.')
 ,(N'DIRECTLOGCONSUMER_LIST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DIRTY_PAGE_POLL',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'DIRTY_PAGE_SYNC',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'DIRTY_PAGE_TABLE_LOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DISABLE_VERSIONING',0,N'Occurs when SQL Server polls the version transaction manager to see whether the timestamp of the earliest active transaction is later than the timestamp of when the state started changing. If this is this case, all the snapshot transactions that were started before the ALTER DATABASE statement was run have finished. This wait state is used when SQL Server disables versioning by using the ALTER DATABASE statement.')
 ,(N'DISKIO_SUSPEND',0,N'Occurs when a task is waiting to access a file when an external backup is active. This is reported for each waiting user process. A count larger than five per user process may indicate that the external backup is taking too much time to finish.')
 ,(N'DISPATCHER_PRIORITY_QUEUE_SEMAPHORE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'DISPATCHER_QUEUE_SEMAPHORE',0,N'Occurs when a thread from the dispatcher pool is waiting for more work to process. The wait time for this wait type is expected to increase when the dispatcher is idle.')
 ,(N'DLL_LOADING_MUTEX',0,N'Occurs once while waiting for the XML parser DLL to load.')
 ,(N'DPT_ENTRY_LOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DROP_DATABASE_TIMER_TASK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'DROPTEMP',0,N'Occurs between attempts to drop a temporary object if the previous attempt failed. The wait duration grows exponentially with each failed drop attempt.')
 ,(N'DTC',0,N'Occurs when a task is waiting on an event that is used to manage state transition. This state controls when the recovery of Microsoft Distributed Transaction Coordinator (MS DTC) transactions occurs after SQL Server receives notification that the MS DTC service has become unavailable.')
 ,(N'DTC_ABORT_REQUEST',0,N'Occurs in a MS DTC worker session when the session is waiting to take ownership of a MS DTC transaction. After MS DTC owns the transaction, the session can roll back the transaction. Generally, the session will wait for another session that is using the transaction.')
 ,(N'DTC_RESOLVE',0,N'Occurs when a recovery task is waiting for the master database in a cross-database transaction so that the task can query the outcome of the transaction.')
 ,(N'DTC_STATE',0,N'Occurs when a task is waiting on an event that protects changes to the internal MS DTC global state object. This state should be held for very short periods of time.')
 ,(N'DTC_TMDOWN_REQUEST',0,N'Occurs in a MS DTC worker session when SQL Server receives notification that the MS DTC service is not available. First, the worker will wait for the MS DTC recovery process to start. Then, the worker waits to obtain the outcome of the distributed transaction that the worker is working on. This may continue until the connection with the MS DTC service has been reestablished.')
 ,(N'DTC_WAITFOR_OUTCOME',0,N'Occurs when recovery tasks wait for MS DTC to become active to enable the resolution of prepared transactions.')
 ,(N'DTCNEW_ENLIST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DTCNEW_PREPARE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DTCNEW_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DTCNEW_TM',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DTCNEW_TRANSACTION_ENLISTMENT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'DTCPNTSYNC',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'DUMP_LOG_COORDINATOR',0,N'Occurs when a main task is waiting for a subtask to generate data. Ordinarily, this state does not occur. A long wait indicates an unexpected blockage. The subtask should be investigated.')
 ,(N'DUMP_LOG_COORDINATOR_QUEUE',0,N'Internal use only.')
 ,(N'DUMPTRIGGER',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'EC',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'EE_PMOLOCK',0,N'Occurs during synchronization of certain types of memory allocations during statement execution.')
 ,(N'EE_SPECPROC_MAP_INIT',0,N'Occurs during synchronization of internal procedure hash table creation. This wait can only occur during the initial accessing of the hash table after the SQL Server instance starts.')
 ,(N'ENABLE_EMPTY_VERSIONING',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'ENABLE_VERSIONING',0,N'Occurs when SQL Server waits for all update transactions in this database to finish before declaring the database ready to transition to snapshot isolation allowed state. This state is used when SQL Server enables snapshot isolation by using the ALTER DATABASE statement.')
 ,(N'ERROR_REPORTING_MANAGER',0,N'Occurs during synchronization of multiple concurrent error log initializations.')
 ,(N'EXCHANGE',0,N'Occurs during synchronization in the query processor exchange iterator during parallel queries.')
 ,(N'EXECSYNC',0,N'Occurs during parallel queries while synchronizing in query processor in areas not related to the exchange iterator. Examples of such areas are bitmaps, large binary objects (LOBs), and the spool iterator. LOBs may frequently use this wait state.')
 ,(N'EXECUTION_PIPE_EVENT_INTERNAL',0,N'Occurs during synchronization between producer and consumer parts of batch execution that are submitted through the connection context.')
 ,(N'EXTERNAL_RG_UPDATE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'EXTERNAL_SCRIPT_NETWORK_IO',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) through current.')
 ,(N'EXTERNAL_SCRIPT_PREPARE_SERVICE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'EXTERNAL_SCRIPT_SHUTDOWN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'EXTERNAL_WAIT_ON_LAUNCHER,',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'FABRIC_HADR_TRANSPORT_CONNECTION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FABRIC_REPLICA_CONTROLLER_LIST',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FABRIC_REPLICA_CONTROLLER_STATE_AND_CONFIG',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FABRIC_REPLICA_PUBLISHER_EVENT_PUBLISH',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FABRIC_REPLICA_PUBLISHER_SUBSCRIBER_LIST',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FABRIC_WAIT_FOR_BUILD_REPLICA_EVENT_PROCESSING',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FAILPOINT',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FCB_REPLICA_READ',0,N'Occurs when the reads of a snapshot (or a temporary snapshot created by DBCC) sparse file are synchronized.')
 ,(N'FCB_REPLICA_WRITE',0,N'Occurs when the pushing or pulling of a page to a snapshot (or a temporary snapshot created by DBCC) sparse file is synchronized.')
 ,(N'FEATURE_SWITCHES_UPDATE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FFT_NSO_DB_KILL_FLAG',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_DB_LIST',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_FCB',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_FCB_FIND',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_FCB_PARENT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_FCB_RELEASE_CACHED_ENTRIES',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_FCB_STATE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'FFT_NSO_FILEOBJECT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NSO_TABLE_LIST',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_NTFS_STORE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_RSFX_COMM',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_RSFX_WAIT_FOR_MEMORY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_STARTUP_SHUTDOWN',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_STORE_DB',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_STORE_ROWSET_LIST',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FFT_STORE_TABLE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILE_VALIDATION_THREADS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'FILESTREAM_CACHE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILESTREAM_CHUNKER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILESTREAM_CHUNKER_INIT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILESTREAM_FCB',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILESTREAM_FILE_OBJECT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILESTREAM_WORKITEM_QUEUE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FILETABLE_SHUTDOWN',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FOREIGN_REDO',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) through current.')
 ,(N'FORWARDER_TRANSITION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'FS_FC_RWLOCK',0,N'Occurs when there is a wait by the FILESTREAM garbage collector to do either of the following:')
 ,(N'FS_GARBAGE_COLLECTOR_SHUTDOWN',0,N'Occurs when the FILESTREAM garbage collector is waiting for cleanup tasks to be completed.')
 ,(N'FS_HEADER_RWLOCK',0,N'Occurs when there is a wait to acquire access to the FILESTREAM header of a FILESTREAM data container to either read or update contents in the FILESTREAM header file (Filestream.hdr).')
 ,(N'FS_LOGTRUNC_RWLOCK',0,N'Occurs when there is a wait to acquire access to FILESTREAM log truncation to do either of the following:')
 ,(N'FSA_FORCE_OWN_XACT',0,N'Occurs when a FILESTREAM file I/O operation needs to bind to the associated transaction, but the transaction is currently owned by another session.')
 ,(N'FSAGENT',0,N'Occurs when a FILESTREAM file I/O operation is waiting for a FILESTREAM agent resource that is being used by another file I/O operation.')
 ,(N'FSTR_CONFIG_MUTEX',0,N'Occurs when there is a wait for another FILESTREAM feature reconfiguration to be completed.')
 ,(N'FSTR_CONFIG_RWLOCK',0,N'Occurs when there is a wait to serialize access to the FILESTREAM configuration parameters.')
 ,(N'FT_COMPROWSET_RWLOCK',0,N'Full-text is waiting on fragment metadata operation. Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_IFTS_RWLOCK',0,N'Full-text is waiting on internal synchronization. Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_IFTS_SCHEDULER_IDLE_WAIT',0,N'Full-text scheduler sleep wait type. The scheduler is idle.')
 ,(N'FT_IFTSHC_MUTEX',0,N'Full-text is waiting on an fdhost control operation. Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_IFTSISM_MUTEX',0,N'Full-text is waiting on communication operation. Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_MASTER_MERGE',0,N'Full-text is waiting on master merge operation. Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_MASTER_MERGE_COORDINATOR',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FT_METADATA_MUTEX',0,N'Documented for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'FT_PROPERTYLIST_CACHE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'FT_RESTART_CRAWL',0,N'Occurs when a full-text crawl needs to restart from a last known good point to recover from a transient failure. The wait lets the worker tasks currently working on that population to complete or exit the current step.')
 ,(N'FULLTEXT GATHERER',0,N'Occurs during synchronization of full-text operations.')
 ,(N'GDMA_GET_RESOURCE_OWNER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'GHOSTCLEANUP_UPDATE_STATS',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'GHOSTCLEANUPSYNCMGR',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'GLOBAL_QUERY_CANCEL',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'GLOBAL_QUERY_CLOSE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'GLOBAL_QUERY_CONSUMER',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'GLOBAL_QUERY_PRODUCER',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'GLOBAL_TRAN_CREATE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'GLOBAL_TRAN_UCS_SESSION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'GUARDIAN',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'HADR_AG_MUTEX',0,N'Occurs when an Always On DDL statement or Windows Server Failover Clustering command is waiting for exclusive read/write access to the configuration of an availability group.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_AR_CRITICAL_SECTION_ENTRY',0,N'Occurs when an Always On DDL statement or Windows Server Failover Clustering command is waiting for exclusive read/write access to the runtime state of the local replica of the associated availability group.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_AR_MANAGER_MUTEX',0,N'Occurs when an availability replica shutdown is waiting for startup to complete or an availability replica startup is waiting for shutdown to complete. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_AR_UNLOAD_COMPLETED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_ARCONTROLLER_NOTIFICATIONS_SUBSCRIBER_LIST',0,N'The publisher for an availability replica event (such as a state change or configuration change) is waiting for exclusive read/write access to the list of event subscribers. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_BACKUP_BULK_LOCK',0,N'The Always On primary database received a backup request from a secondary database and is waiting for the background thread to finish processing the request on acquiring or releasing the BulkOp lock.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_BACKUP_QUEUE',0,N'The backup background thread of the Always On primary database is waiting for a new work request from the secondary database. (Typically, this occurs when the primary database is holding the BulkOp log and is waiting for the secondary database to indicate that the primary database can release the lock).     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_CLUSAPI_CALL',0,N'A SQL Server thread is waiting to switch from non-preemptive mode (scheduled by SQL Server) to preemptive mode (scheduled by the operating system) in order to invoke Windows Server Failover Clustering APIs.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_COMPRESSED_CACHE_SYNC',0,N'Waiting for access to the cache of compressed log blocks that is used to avoid redundant compression of the log blocks sent to multiple secondary databases.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_CONNECTIVITY_INFO',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DATABASE_FLOW_CONTROL',0,N'Waiting for messages to be sent to the partner when the maximum number of queued messages has been reached. Indicates that the log scans are running faster than the network sends. This is an issue only if network sends are slower than expected.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DATABASE_VERSIONING_STATE',0,N'Occurs on the versioning state change of an Always On secondary database. This wait is for internal data structures and usually is very short with no direct effect on data access.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DATABASE_WAIT_FOR_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_DATABASE_WAIT_FOR_RESTART',0,N'Waiting for the database to restart under Always On Availability Groups control. Under normal conditions, this is not a customer issue because waits are expected here.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING',0,N'A query on object(s) in a readable secondary database of an Always On availability group is blocked on row versioning while waiting for commit or rollback of all transactions that were in-flight when the secondary replica was enabled for read workloads. This wait type guarantees that row versions are available before execution of a query under snapshot isolation.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DB_COMMAND',0,N'Waiting for responses to conversational messages (which require an explicit response from the other side, using the Always On conversational message infrastructure). A number of different message types use this wait type.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DB_OP_COMPLETION_SYNC',0,N'Waiting for responses to conversational messages (which require an explicit response from the other side, using the Always On conversational message infrastructure). A number of different message types use this wait type.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DB_OP_START_SYNC',0,N'An Always On DDL statement or a Windows Server Failover Clustering command is waiting for serialized access to an availability database and its runtime state.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DBR_SUBSCRIBER',0,N'The publisher for an availability replica event (such as a state change or configuration change) is waiting for exclusive read/write access to the runtime state of an event subscriber that corresponds to an availability database. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DBR_SUBSCRIBER_FILTER_LIST',0,N'The publisher for an availability replica event (such as a state change or configuration change) is waiting for exclusive read/write access to the list of event subscribers that correspond to availability databases. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_DBSEEDING',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HADR_DBSEEDING_LIST',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HADR_DBSTATECHANGE_SYNC',0,N'Concurrency control wait for updating the internal state of the database replica.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FABRIC_CALLBACK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HADR_FILESTREAM_BLOCK_FLUSH',0,N'The FILESTREAM Always On transport manager is waiting until processing of a log block is finished.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_FILE_CLOSE',0,N'The FILESTREAM Always On transport manager is waiting until the next FILESTREAM file gets processed and its handle gets closed.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_FILE_REQUEST',0,N'An Always On secondary replica is waiting for the primary replica to send all requested FILESTREAM files during UNDO.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_IOMGR',0,N'The FILESTREAM Always On transport manager is waiting for R/W lock that protects the FILESTREAM Always On I/O manager during startup or shutdown.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_IOMGR_IOCOMPLETION',0,N'The FILESTREAM Always On I/O manager is waiting for I/O completion.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_MANAGER',0,N'The FILESTREAM Always On transport manager is waiting for the R/W lock that protects the FILESTREAM Always On transport manager during startup or shutdown.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_FILESTREAM_PREPROC',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_GROUP_COMMIT',0,N'Transaction commit processing is waiting to allow a group commit so that multiple commit log records can be put into a single log block. This wait is an expected condition that optimizes the log I/O, capture, and send operations.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_LOGCAPTURE_SYNC',0,N'Concurrency control around the log capture or apply object when creating or destroying scans. This is an expected wait when partners change state or connection status.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_LOGCAPTURE_WAIT',0,N'Waiting for log records to become available. Can occur either when waiting for new log records to be generated by connections or for I/O completion when reading log not in the cache. This is an expected wait if the log scan is caught up to the end of log or is reading from disk.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_LOGPROGRESS_SYNC',0,N'Concurrency control wait when updating the log progress status of database replicas.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_NOTIFICATION_DEQUEUE',0,N'A background task that processes Windows Server Failover Clustering notifications is waiting for the next notification. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_NOTIFICATION_WORKER_EXCLUSIVE_ACCESS',0,N'The Always On availability replica manager is waiting for serialized access to the runtime state of a background task that processes Windows Server Failover Clustering notifications. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_NOTIFICATION_WORKER_STARTUP_SYNC',0,N'A background task is waiting for the completion of the startup of a background task that processes Windows Server Failover Clustering notifications. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_NOTIFICATION_WORKER_TERMINATION_SYNC',0,N'A background task is waiting for the termination of a background task that processes Windows Server Failover Clustering notifications. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_PARTNER_SYNC',0,N'Concurrency control wait on the partner list.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_READ_ALL_NETWORKS',0,N'Waiting to get read or write access to the list of WSFC networks. Internal use only. Note: The engine keeps a list of WSFC networks that is used in dynamic management views (such as sys.dm_hadr_cluster_networks) or to validate Always On Transact-SQL statements that reference WSFC network information. This list is updated upon engine startup, WSFC related notifications, and internal Always On restart (for example, losing and regaining of WSFC quorum). Tasks will usually be blocked when an update in that list is in progress.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_RECOVERY_WAIT_FOR_CONNECTION',0,N'Waiting for the secondary database to connect to the primary database before running recovery. This is an expected wait, which can lengthen if the connection to the primary is slow to establish.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_RECOVERY_WAIT_FOR_UNDO',0,N'Database recovery is waiting for the secondary database to finish the reverting and initializing phase to bring it back to the common log point with the primary database. This is an expected wait after failovers. Undo progress can be tracked through the Windows System Monitor (perfmon.exe) and dynamic management views.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_REPLICAINFO_SYNC',0,N'Waiting for concurrency control to update the current replica state.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_SEEDING_CANCELLATION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SEEDING_FILE_LIST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SEEDING_LIMIT_BACKUPS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SEEDING_SYNC_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SEEDING_TIMEOUT_TASK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SEEDING_WAIT_FOR_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_SYNC_COMMIT',0,N'Waiting for a transaction commit processing on the synchronized secondary databases to harden the log. This wait is also reflected by the Transaction Delay performance counter. This wait type is expected for synchronous-commit Availability Groups and indicates the time to send, write, and acknowledge log commit to the secondary databases.  For detailed information and troubleshooting HADR_SYNC_COMMIT, refer to this blog post    Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_SYNCHRONIZING_THROTTLE',0,N'Waiting for transaction commit processing to allow a synchronizing secondary database to catch up to the primary end of log in order to transition to the synchronized state. This is an expected wait when a secondary database is catching up.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_TDS_LISTENER_SYNC',0,N'Either the internal Always On system or the WSFC cluster will request that listeners are started or stopped. The processing of this request is always asynchronous, and there is a mechanism to remove redundant requests. There are also moments that this process is suspended because of configuration changes. All waits related with this listener synchronization mechanism use this wait type. Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_TDS_LISTENER_SYNC_PROCESSING',0,N'Used at the end of an Always On Transact-SQL statement that requires starting and/or stopping an availability group listener. Since the start/stop operation is done asynchronously, the user thread will block using this wait type until the situation of the listener is known.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_THROTTLE_LOG_RATE_GOVERNOR',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HADR_THROTTLE_LOG_RATE_LOG_SIZE',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'HADR_THROTTLE_LOG_RATE_MISMATCHED_SLO',0,N'Occurs when a geo-replication secondary is configured with lower compute size (lower SLO) than the primary. A primary database is throttled due to delayed log consumption by the secondary. This is caused  by the secondary database having insufficient compute capacity to keep up with the primary database''s rate of change.     Applies to : Azure SQL Database')
 ,(N'HADR_THROTTLE_LOG_RATE_SEEDING',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'HADR_THROTTLE_LOG_RATE_SEND_RECV_QUEUE_SIZE',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'HADR_TIMER_TASK',0,N'Waiting to get the lock on the timer task object and is also used for the actual waits between times that work is being performed. For example, for a task that runs every 10 seconds, after one execution, Always On Availability Groups waits about 10 seconds to reschedule the task, and the wait is included here.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_TRANSPORT_DBRLIST',0,N'Waiting for access to the transport layer''s database replica list. Used for the spinlock that grants access to it.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_TRANSPORT_FLOW_CONTROL',0,N'Waiting when the number of outstanding unacknowledged Always On messages is over the out flow control threshold. This is on an availability replica-to-replica basis (not on a database-to-database basis).     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_TRANSPORT_SESSION',0,N'Always On Availability Groups is waiting while changing or accessing the underlying transport state.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_WORK_POOL',0,N'Concurrency control wait on the Always On Availability Groups background work task object.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_WORK_QUEUE',0,N'Always On Availability Groups background worker thread waiting for new work to be assigned. This is an expected wait when there are ready workers waiting for new work, which is the normal state.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HADR_XRF_STACK_ACCESS',0,N'Accessing (look up, add, and delete) the extended recovery fork stack for an Always On availability database.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HCCO_CACHE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HK_RESTORE_FILEMAP',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HKCS_PARALLEL_MIGRATION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HKCS_PARALLEL_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'HTBUILD',0,N'Occurs with parallel batch-mode plans when synchronizing the building of the hash table on the input side of a hash join/aggregation. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HTDELETE',0,N'Occurs with parallel batch-mode plans when synchronizing at the end of a hash join/aggregation. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HTMEMO',0,N'Occurs with parallel batch-mode plans when synchronizing before scanning hash table to output matches / non-matches in hash join/aggregation. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HTREINIT',0,N'Occurs with parallel batch-mode plans when synchronizing before resetting a hash join/aggregation for the next partial join. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'HTREPARTITION',0,N'Occurs with parallel batch-mode plans when synchronizing the repartitioning of the hash table on the input side of a hash join/aggregation. If waiting is excessive and cannot be reduced by tuning the query (such as adding indexes), consider adjusting the cost threshold for parallelism or lowering the degree of parallelism.    Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'HTTP_ENUMERATION',0,N'Occurs at startup to enumerate the HTTP endpoints to start HTTP.')
 ,(N'HTTP_START',0,N'Occurs when a connection is waiting for HTTP to complete initialization.')
 ,(N'HTTP_STORAGE_CONNECTION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'IMPPROV_IOWAIT',0,N'Occurs when SQL Server waits for a bulkload I/O to finish.')
 ,(N'INSTANCE_LOG_RATE_GOVERNOR',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'INTERNAL_TESTING',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'IO_AUDIT_MUTEX',0,N'Occurs during synchronization of trace event buffers.')
 ,(N'IO_COMPLETION',0,N'Occurs while waiting for I/O operations to complete. This wait type generally represents non-data page I/Os. Data page I/O completion waits appear as PAGEIOLATCH_* waits.')
 ,(N'IO_QUEUE_LIMIT',1,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'IO_RETRY',1,N'Occurs when an I/O operation such as a read or a write to disk fails because of insufficient resources, and is then retried.')
 ,(N'IOAFF_RANGE_QUEUE',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'KSOURCE_WAKEUP',0,N'Used by the service control task while waiting for requests from the Service Control Manager. Long waits are expected and do not indicate a problem.')
 ,(N'KTM_ENLISTMENT',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'KTM_RECOVERY_MANAGER',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'KTM_RECOVERY_RESOLUTION',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'LATCH_DT',0,N'Occurs when waiting for a DT (destroy) latch. This does not include buffer latches or transaction mark latches. A listing of LATCH_* waits is available in sys.dm_os_latch_stats. Note that sys.dm_os_latch_stats groups LATCH_NL, LATCH_SH, LATCH_UP, LATCH_EX, and LATCH_DT waits together.')
 ,(N'LATCH_EX',0,N'Occurs when waiting for an EX (exclusive) latch. This does not include buffer latches or transaction mark latches. A listing of LATCH_* waits is available in sys.dm_os_latch_stats. Note that sys.dm_os_latch_stats groups LATCH_NL, LATCH_SH, LATCH_UP, LATCH_EX, and LATCH_DT waits together.')
 ,(N'LATCH_KP',0,N'Occurs when waiting for a KP (keep) latch. This does not include buffer latches or transaction mark latches. A listing of LATCH_* waits is available in sys.dm_os_latch_stats. Note that sys.dm_os_latch_stats groups LATCH_NL, LATCH_SH, LATCH_UP, LATCH_EX, and LATCH_DT waits together.')
 ,(N'LATCH_NL',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'LATCH_SH',0,N'Occurs when waiting for an SH (share) latch. This does not include buffer latches or transaction mark latches. A listing of LATCH_* waits is available in sys.dm_os_latch_stats. Note that sys.dm_os_latch_stats groups LATCH_NL, LATCH_SH, LATCH_UP, LATCH_EX, and LATCH_DT waits together.')
 ,(N'LATCH_UP',0,N'Occurs when waiting for an UP (update) latch. This does not include buffer latches or transaction mark latches. A listing of LATCH_* waits is available in sys.dm_os_latch_stats. Note that sys.dm_os_latch_stats groups LATCH_NL, LATCH_SH, LATCH_UP, LATCH_EX, and LATCH_DT waits together.')
 ,(N'LAZYWRITER_SLEEP',0,N'Occurs when lazy writer tasks are suspended. This is a measure of the time spent by background tasks that are waiting. Do not consider this state when you are looking for user stalls.')
 ,(N'LCK_M_BU',0,N'Occurs when a task is waiting to acquire a Bulk Update (BU) lock. See Bulk Update Locks for more information')
 ,(N'LCK_M_BU_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Bulk Update (BU) lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Bulk Update Locks for more information     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_BU_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Bulk Update (BU) lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Bulk Update Locks for more information     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IS',0,N'Occurs when a task is waiting to acquire an Intent Shared (IS) lock. See Intent Locks for more information')
 ,(N'LCK_M_IS_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Intent Shared (IS) lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.) See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IS_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Intent Shared (IS) lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IU',0,N'Occurs when a task is waiting to acquire an Intent Update (IU) lock. See Intent Locks for more information')
 ,(N'LCK_M_IU_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Intent Update (IU) lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.) See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IU_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Intent Update (IU) lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IX',0,N'Occurs when a task is waiting to acquire an Intent Exclusive (IX) lock. See Intent Locks for more information')
 ,(N'LCK_M_IX_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Intent Exclusive (IX) lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_IX_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Intent Exclusive (IX) lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_NL',0,N'Occurs when a task is waiting to acquire a NULL lock on the current key value, and an Insert Range lock between the current and previous key. A NULL lock on the key is an instant release lock.')
 ,(N'LCK_M_RIN_NL_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a NULL lock with Abort Blockers on the current key value, and an Insert Range lock with Abort Blockers between the current and previous key. A NULL lock on the key is an instant release lock. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_NL_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a NULL lock with Low Priority on the current key value, and an Insert Range lock with Low Priority between the current and previous key. A NULL lock on the key is an instant release lock. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_S',0,N'Occurs when a task is waiting to acquire a shared lock on the current key value, and an Insert Range lock between the current and previous key.')
 ,(N'LCK_M_RIN_S_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a shared lock with Abort Blockers on the current key value, and an Insert Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_S_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a shared lock with Low Priority on the current key value, and an Insert Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_U',0,N'Task is waiting to acquire an Update lock on the current key value, and an Insert Range lock between the current and previous key.')
 ,(N'LCK_M_RIN_U_ABORT_BLOCKERS',0,N'Task is waiting to acquire an Update lock with Abort Blockers on the current key value, and an Insert Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_U_LOW_PRIORITY',0,N'Task is waiting to acquire an Update lock with Low Priority on the current key value, and an Insert Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_X',0,N'Occurs when a task is waiting to acquire an Exclusive lock on the current key value, and an Insert Range lock between the current and previous key.')
 ,(N'LCK_M_RIN_X_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Abort Blockers on the current key value, and an Insert Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RIN_X_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Low Priority on the current key value, and an Insert Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RS_S',0,N'Occurs when a task is waiting to acquire a Shared lock on the current key value, and a Shared Range lock between the current and previous key.')
 ,(N'LCK_M_RS_S_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Shared lock with Abort Blockers on the current key value, and a Shared Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RS_S_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Shared lock with Low Priority on the current key value, and a Shared Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RS_U',0,N'Occurs when a task is waiting to acquire an Update lock on the current key value, and an Update Range lock between the current and previous key.')
 ,(N'LCK_M_RS_U_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Update lock with Abort Blockers on the current key value, and an Update Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RS_U_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Update lock with Low Priority on the current key value, and an Update Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_S',0,N'Occurs when a task is waiting to acquire a Shared lock on the current key value, and an Exclusive Range lock between the current and previous key.')
 ,(N'LCK_M_RX_S_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Shared lock with Abort Blockers on the current key value, and an Exclusive Range with Abort Blockers lock between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_S_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Shared lock with Low Priority on the current key value, and an Exclusive Range with Low Priority lock between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_U',0,N'Occurs when a task is waiting to acquire an Update lock on the current key value, and an Exclusive range lock between the current and previous key.')
 ,(N'LCK_M_RX_U_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Update lock with Abort Blockers on the current key value, and an Exclusive range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_U_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Update lock with Low Priority on the current key value, and an Exclusive range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_X',0,N'Occurs when a task is waiting to acquire an Exclusive lock on the current key value, and an Exclusive Range lock between the current and previous key.')
 ,(N'LCK_M_RX_X_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Abort Blockers on the current key value, and an Exclusive Range lock with Abort Blockers between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_RX_X_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Low Priority on the current key value, and an Exclusive Range lock with Low Priority between the current and previous key. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX.),     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_S',0,N'Occurs when a task is waiting to acquire a Shared lock. See Shared Locks for more information.')
 ,(N'LCK_M_S_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Shared lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Shared Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_S_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Shared lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Shared Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SCH_M',0,N'Occurs when a task is waiting to acquire a Schema Modify lock. See Schema Locks for more information.')
 ,(N'LCK_M_SCH_M_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Schema Modify lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Schema Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SCH_M_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Schema Modify lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Schema Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SCH_S',0,N'Occurs when a task is waiting to acquire a Schema Share lock. See Schema Locks for more information.')
 ,(N'LCK_M_SCH_S_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Schema Share lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Schema Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SCH_S_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Schema Share lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX) See Schema Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SIU',0,N'Occurs when a task is waiting to acquire a Shared With Intent Update lock. See Intent Locks for more information.')
 ,(N'LCK_M_SIU_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Shared With Intent Update lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SIU_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Shared With Intent Update lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SIX',0,N'Occurs when a task is waiting to acquire a Shared With Intent Exclusive lock. See Intent Locks for more information.')
 ,(N'LCK_M_SIX_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire a Shared With Intent Exclusive lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_SIX_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire a Shared With Intent Exclusive lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_U',0,N'Occurs when a task is waiting to acquire an Update lock. See Update Locks for more information.')
 ,(N'LCK_M_U_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Update lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Update Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_U_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Update lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Update Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_UIX',0,N'Occurs when a task is waiting to acquire an Update With Intent Exclusive lock. See Intent Locks for more information.')
 ,(N'LCK_M_UIX_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Update With Intent Exclusive lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_UIX_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Update With Intent Exclusive lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Intent Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_X',0,N'Occurs when a task is waiting to acquire an Exclusive lock. See Exclusive Locks for more information.')
 ,(N'LCK_M_X_ABORT_BLOCKERS',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Abort Blockers. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Exclusive Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LCK_M_X_LOW_PRIORITY',0,N'Occurs when a task is waiting to acquire an Exclusive lock with Low Priority. (Related to the low priority wait option of ALTER TABLE and ALTER INDEX). See Exclusive Locks for more information.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'LOG_POOL_SCAN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'LOG_RATE_GOVERNOR',1,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'LOGBUFFER',0,N'Occurs when a task is waiting for space in the log buffer to store a log record. Consistently high values may indicate that the log devices cannot keep up with the amount of log being generated by the server.')
 ,(N'LOGCAPTURE_LOGPOOLTRUNCPOINT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGGENERATION',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'LOGMGR',0,N'Occurs when a task is waiting for any outstanding log I/Os to finish before shutting down the log while closing the database.')
 ,(N'LOGMGR_FLUSH',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'LOGMGR_PMM_LOG',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'LOGMGR_QUEUE',0,N'Occurs while the log writer task waits for work requests.')
 ,(N'LOGMGR_RESERVE_APPEND',0,N'Occurs when a task is waiting to see whether log truncation frees up log space to enable the task to write a new log record. Consider increasing the size of the log file(s) for the affected database to reduce this wait.')
 ,(N'LOGPOOL_CACHESIZE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOL_CONSUMER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOL_CONSUMERSET',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOL_FREEPOOLS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOL_MGRSET',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOL_REPLACEMENTSET',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOGPOOLREFCOUNTEDOBJECT_REFDONE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'LOWFAIL_MEMMGR_QUEUE',0,N'Occurs while waiting for memory to be available for use.')
 ,(N'MD_AGENT_YIELD',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'MD_LAZYCACHE_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'MEMORY_ALLOCATION_EXT',0,N'Occurs while allocating memory from either the internal SQL Server memory pool or the operation system.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'MEMORY_GRANT_UPDATE',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'METADATA_LAZYCACHE_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2008 R2 only.')
 ,(N'MIGRATIONBUFFER',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'MISCELLANEOUS',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'MSQL_DQ',0,N'Occurs when a task is waiting for a distributed query operation to finish. This is used to detect potential Multiple Active Result Set (MARS) application deadlocks. The wait ends when the distributed query call finishes.')
 ,(N'MSQL_XACT_MGR_MUTEX',0,N'Occurs when a task is waiting to obtain ownership of the session transaction manager to perform a session level transaction operation.')
 ,(N'MSQL_XACT_MUTEX',0,N'Occurs during synchronization of transaction usage. A request must acquire the mutex before it can use the transaction.')
 ,(N'MSQL_XP',0,N'Occurs when a task is waiting for an extended stored procedure to end. SQL Server uses this wait state to detect potential MARS application deadlocks. The wait stops when the extended stored procedure call ends.')
 ,(N'MSSEARCH',0,N'Occurs during Full-Text Search calls. This wait ends when the full-text operation completes. It does not indicate contention, but rather the duration of full-text operations.')
 ,(N'NET_WAITFOR_PACKET',0,N'Occurs when a connection is waiting for a network packet during a network read.')
 ,(N'NETWORKSXMLMGRLOAD',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'NODE_CACHE_MUTEX',0,N'Internal use only.')
 ,(N'OLEDB',0,N'Occurs when SQL Server calls the SNAC OLE DB Provider (SQLNCLI) or the Microsoft OLE DB Driver for SQL Server (MSOLEDBSQL). This wait type is not used for synchronization. Instead, it indicates the duration of calls to the OLE DB provider.')
 ,(N'ONDEMAND_TASK_QUEUE',0,N'Occurs while a background task waits for high priority system task requests. Long wait times indicate that there have been no high priority requests to process, and should not cause concern.')
 ,(N'PAGEIOLATCH_DT',0,N'Occurs when a task is waiting on a latch for a buffer that is in an I/O request. The latch request is in Destroy mode. Long waits may indicate problems with the disk subsystem.')
 ,(N'PAGEIOLATCH_EX',0,N'Occurs when a task is waiting on a latch for a buffer that is in an I/O request. The latch request is in Exclusive mode - a mode used when the buffer is being written to disk. Long waits may indicate problems with the disk subsystem.    See this SQL Server Slow I/O troubleshooting blog for more information.')
 ,(N'PAGEIOLATCH_KP',0,N'Occurs when a task is waiting on a latch for a buffer that is in an I/O request. The latch request is in Keep mode. Long waits may indicate problems with the disk subsystem.')
 ,(N'PAGEIOLATCH_NL',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PAGEIOLATCH_SH',0,N'Occurs when a task is waiting on a latch for a buffer that is in an I/O request. The latch request is in Shared mode - a mode used when the buffer is being read from disk. Long waits may indicate problems with the disk subsystem.   See this SQL Server Slow I/O troubleshooting blog for more information.')
 ,(N'PAGEIOLATCH_UP',0,N'Occurs when a task is waiting on a latch for a buffer that is in an I/O request. The latch request is in Update mode. Long waits may indicate problems with the disk subsystem.   See this SQL Server Slow I/O troubleshooting blog for more information.')
 ,(N'PAGELATCH_DT',0,N'Occurs when a task is waiting on a latch for a buffer that is not in an I/O request. The latch request is in Destroy mode. Destroy mode must be acquired before deleting contents of a page. See Latch Modes for more information.')
 ,(N'PAGELATCH_EX',0,N'Occurs when a task is waiting on a latch for a buffer that is not in an I/O request. The latch request is in Exclusive mode - it blocks other threads from writing to or reading from the page (buffer).   A common scenario that leads to this latch is the "last-page insert" buffer latch contention. To understand and resolve this, use Resolve last-page insert PAGELATCH_EX contention and Diagnose and resolve last-page-insert latch contention on SQL Server. Another scenario is Latch contention on small tables with a non-clustered index and random inserts (queue table).')
 ,(N'PAGELATCH_KP',0,N'Occurs when a task is waiting on a latch for a buffer that is not in an I/O request. The latch request is in Keep mode which prevents the page from being destroyed by another thread. See Latch Modes for more information.')
 ,(N'PAGELATCH_NL',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PAGELATCH_SH',0,N'Occurs when a task is waiting on a latch for a buffer that is not in an I/O request. The latch request is in Shared mode which allows multiple threads to read, but not modify, a buffer (page). See Latch Modes for more information.')
 ,(N'PAGELATCH_UP',0,N'Occurs when a task is waiting on a latch for a buffer that is not in an I/O request. The latch request is in Update mode. Commonly this wait type may be observed when a system page (buffer) like PFS, GAM, SGAM is latched. See Latch Modes for more information.   For troubleshooting a common scenario with this latch, refer to Reduce Allocation Contention in SQL Server tempdb database.')
 ,(N'PARALLEL_BACKUP_QUEUE',0,N'Occurs when serializing output produced by RESTORE HEADERONLY, RESTORE FILELISTONLY, or RESTORE LABELONLY.')
 ,(N'PARALLEL_REDO_DRAIN_WORKER',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_FLOW_CONTROL',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_LOG_CACHE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_TRAN_LIST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_TRAN_TURN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_WORKER_SYNC',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PARALLEL_REDO_WORKER_WAIT_WORK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PERFORMANCE_COUNTERS_RWLOCK',0,N'Internal use only.')
 ,(N'PHYSICAL_SEEDING_DMV',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'POOL_LOG_RATE_GOVERNOR',1,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PREEMPTIVE_ABR',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PREEMPTIVE_AUDIT_ACCESS_EVENTLOG',0,N'Occurs when the SQL Server Operating System (SQLOS) scheduler switches to preemptive mode to write an audit event to the Windows event log.     Applies to : SQL Server 2008 R2 only.')
 ,(N'PREEMPTIVE_AUDIT_ACCESS_SECLOG',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to write an audit event to the Windows Security log.     Applies to : SQL Server 2008 R2 only.')
 ,(N'PREEMPTIVE_CLOSEBACKUPMEDIA',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to close backup media.')
 ,(N'PREEMPTIVE_CLOSEBACKUPTAPE',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to close a tape backup device.')
 ,(N'PREEMPTIVE_CLOSEBACKUPVDIDEVICE',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to close a virtual backup device.')
 ,(N'PREEMPTIVE_CLUSAPI_CLUSTERRESOURCECONTROL',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to perform Windows failover cluster operations.')
 ,(N'PREEMPTIVE_COM_COCREATEINSTANCE',0,N'Occurs when the SQLOS scheduler switches to preemptive mode to create a COM object.')
 ,(N'PREEMPTIVE_COM_COGETCLASSOBJECT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_CREATEACCESSOR',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_DELETEROWS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_GETCOMMANDTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_GETDATA',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_GETNEXTROWS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_GETRESULT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_GETROWSBYBOOKMARK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBFLUSH',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBLOCKREGION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBREADAT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBSETSIZE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBSTAT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBUNLOCKREGION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_LBWRITEAT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_QUERYINTERFACE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_RELEASE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_RELEASEACCESSOR',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_RELEASEROWS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_RELEASESESSION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_RESTARTPOSITION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_SEQSTRMREAD',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_SEQSTRMREADANDWRITE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_SETDATAFAILURE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_SETPARAMETERINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_SETPARAMETERPROPERTIES',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMLOCKREGION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMSEEKANDREAD',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMSEEKANDWRITE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMSETSIZE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMSTAT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_COM_STRMUNLOCKREGION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_CONSOLEWRITE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_CREATEPARAM',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DEBUG',1,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSADDLINK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSLINKEXISTCHECK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSLINKHEALTHCHECK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSREMOVELINK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSREMOVEROOT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSROOTFOLDERCHECK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSROOTINIT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DFSROOTSHARECHECK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_ABORT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_ABORTREQUESTDONE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_BEGINTRANSACTION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_COMMITREQUESTDONE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_ENLIST',0,N'Internal use only.')
 ,(N'PREEMPTIVE_DTC_PREPAREREQUESTDONE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_FILESIZEGET',0,N'Internal use only.')
 ,(N'PREEMPTIVE_FSAOLEDB_ABORTTRANSACTION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_FSAOLEDB_COMMITTRANSACTION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_FSAOLEDB_STARTTRANSACTION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_FSRECOVER_UNCONDITIONALUNDO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_GETRMINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_HADR_LEASE_MECHANISM',0,N'Always On Availability Groups lease manager scheduling for Microsoft Support diagnostics.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PREEMPTIVE_HTTP_EVENT_WAIT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PREEMPTIVE_HTTP_REQUEST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PREEMPTIVE_LOCKMONITOR',0,N'Internal use only.')
 ,(N'PREEMPTIVE_MSS_RELEASE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_ODBCOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLE_UNINIT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_ABORTORCOMMITTRAN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_ABORTTRAN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_GETDATASOURCE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_GETLITERALINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_GETPROPERTIES',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_GETPROPERTYINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_GETSCHEMALOCK',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_JOINTRANSACTION',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_RELEASE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDB_SETPROPERTIES',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OLEDBOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_ACCEPTSECURITYCONTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_ACQUIRECREDENTIALSHANDLE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_AUTHENTICATIONOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_AUTHORIZATIONOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_AUTHZGETINFORMATIONFROMCONTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_AUTHZINITIALIZECONTEXTFROMSID',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_AUTHZINITIALIZERESOURCEMANAGER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_BACKUPREAD',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CLOSEHANDLE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CLUSTEROPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_COMOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_COMPLETEAUTHTOKEN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_COPYFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CREATEDIRECTORY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CREATEFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CRYPTACQUIRECONTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CRYPTIMPORTKEY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_CRYPTOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DECRYPTMESSAGE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DELETEFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DELETESECURITYCONTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DEVICEIOCONTROL',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DEVICEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DIRSVC_NETWORKOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DISCONNECTNAMEDPIPE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DOMAINSERVICESOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DSGETDCNAME',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_DTCOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_ENCRYPTMESSAGE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FILEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FINDFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FLUSHFILEBUFFERS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FORMATMESSAGE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FREECREDENTIALSHANDLE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_FREELIBRARY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GENERICOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETADDRINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETCOMPRESSEDFILESIZE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETDISKFREESPACE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETFILEATTRIBUTES',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETFILESIZE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETFINALFILEPATHBYHANDLE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PREEMPTIVE_OS_GETLONGPATHNAME',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETPROCADDRESS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETVOLUMENAMEFORVOLUMEMOUNTPOINT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_GETVOLUMEPATHNAME',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_INITIALIZESECURITYCONTEXT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_LIBRARYOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_LOADLIBRARY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_LOGONUSER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_LOOKUPACCOUNTSID',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_MESSAGEQUEUEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_MOVEFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETGROUPGETUSERS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETLOCALGROUPGETMEMBERS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETUSERGETGROUPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETUSERGETLOCALGROUPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETUSERMODALSGET',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETVALIDATEPASSWORDPOLICY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_NETVALIDATEPASSWORDPOLICYFREE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_OPENDIRECTORY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_PDH_WMI_INIT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PREEMPTIVE_OS_PIPEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_PROCESSOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_QUERYCONTEXTATTRIBUTES',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PREEMPTIVE_OS_QUERYREGISTRY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_QUERYSECURITYCONTEXTTOKEN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_REMOVEDIRECTORY',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_REPORTEVENT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_REVERTTOSELF',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_RSFXDEVICEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SECURITYOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SERVICEOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SETENDOFFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SETFILEPOINTER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SETFILEVALIDDATA',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SETNAMEDSECURITYINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SQLCLROPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_SQMLAUNCH',0,N'Internal use only.     Applies to : SQL Server 2008 R2 through SQL Server 2016 (13.x).')
 ,(N'PREEMPTIVE_OS_VERIFYSIGNATURE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_VERIFYTRUST',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PREEMPTIVE_OS_VSSOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_WAITFORSINGLEOBJECT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_WINSOCKOPS',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_WRITEFILE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_WRITEFILEGATHER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_OS_WSASETLASTERROR',0,N'Internal use only.')
 ,(N'PREEMPTIVE_REENLIST',0,N'Internal use only.')
 ,(N'PREEMPTIVE_RESIZELOG',0,N'Internal use only.')
 ,(N'PREEMPTIVE_ROLLFORWARDREDO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_ROLLFORWARDUNDO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SB_STOPENDPOINT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SERVER_STARTUP',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SETRMINFO',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SHAREDMEM_GETDATA',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SNIOPEN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SOSHOST',0,N'Internal use only.')
 ,(N'PREEMPTIVE_SOSTESTING',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PREEMPTIVE_SP_SERVER_DIAGNOSTICS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PREEMPTIVE_STARTRM',0,N'Internal use only.')
 ,(N'PREEMPTIVE_STREAMFCB_CHECKPOINT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_STREAMFCB_RECOVER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_STRESSDRIVER',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PREEMPTIVE_TESTING',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PREEMPTIVE_TRANSIMPORT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_UNMARSHALPROPAGATIONTOKEN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_VSS_CREATESNAPSHOT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_VSS_CREATEVOLUMESNAPSHOT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_CALLBACKEXECUTE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_CX_FILE_OPEN',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PREEMPTIVE_XE_CX_HTTP_CALL',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PREEMPTIVE_XE_DISPATCHER',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_ENGINEINIT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_GETTARGETSTATE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_SESSIONCOMMIT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_TARGETFINALIZE',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_TARGETINIT',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XE_TIMERRUN',0,N'Internal use only.')
 ,(N'PREEMPTIVE_XETESTING',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'PRINT_ROLLBACK_PROGRESS',0,N'Used to wait while user processes are ended in a database that has been transitioned by using the ALTER DATABASE termination clause. For more information, see ALTER DATABASE (Transact-SQL).')
 ,(N'PRU_ROLLBACK_DEFERRED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_ALL_COMPONENTS_INITIALIZED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PVS_PREALLOCATE',0,'Wiat for background task that is sleeping between prealocating space in the persistent version store (for databases with ADR enabled).  Applies to : SQL Server 2019 (15.x) and later.')
 ,(N'PWAIT_COOP_SCAN',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_DIRECTLOGCONSUMER_GETNEXT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PWAIT_EVENT_SESSION_INIT_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_FABRIC_REPLICA_CONTROLLER_DATA_LOSS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PWAIT_HADR_ACTION_COMPLETED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_CHANGE_NOTIFIER_TERMINATION_SYNC',0,N'Occurs when a background task is waiting for the termination of the background task that receives (via polling) Windows Server Failover Clustering notifications.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_CLUSTER_INTEGRATION',0,N'An append, replace, and/or remove operation is waiting to grab a write lock on an Always On internal list (such as a list of networks, network addresses, or availability group listeners). Internal use only,     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_FAILOVER_COMPLETED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_JOIN',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PWAIT_HADR_OFFLINE_COMPLETED',0,N'An Always On drop availability group operation is waiting for the target availability group to go offline before destroying Windows Server Failover Clustering objects.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_ONLINE_COMPLETED',0,N'An Always On create or failover availability group operation is waiting for the target availability group to come online.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_POST_ONLINE_COMPLETED',0,N'An Always On drop availability group operation is waiting for the termination of any background task that was scheduled as part of a previous command. For example, there may be a background task that is transitioning availability databases to the primary role. The DROP AVAILABILITY GROUP DDL must wait for this background task to terminate in order to avoid race conditions.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_SERVER_READY_CONNECTIONS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADR_WORKITEM_COMPLETED',0,N'Internal wait by a thread waiting for an async work task to complete. This is an expected wait and is for CSS use.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_HADRSIM',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'PWAIT_LOG_CONSOLIDATION_IO',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PWAIT_LOG_CONSOLIDATION_POLL',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PWAIT_MD_LOGIN_STATS',0,N'Occurs during internal synchronization in metadata on login stats.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_MD_RELATION_CACHE',0,N'Occurs during internal synchronization in metadata on table or index.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_MD_SERVER_CACHE',0,N'Occurs during internal synchronization in metadata on linked servers.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_MD_UPGRADE_CONFIG',0,N'Occurs during internal synchronization in upgrading server wide configurations.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_PREEMPTIVE_APP_USAGE_TIMER',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'PWAIT_PREEMPTIVE_AUDIT_ACCESS_WINDOWSLOG',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_QRY_BPMEMORY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_REPLICA_ONLINE_INIT_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_RESOURCE_SEMAPHORE_FT_PARALLEL_QUERY_SYNC',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'PWAIT_SBS_FILE_OPERATION',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'PWAIT_XTP_FSSTORAGE_MAINTENANCE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'PWAIT_XTP_HOST_STORAGE_WAIT',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_ASYNC_CHECK_CONSISTENCY_TASK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_ASYNC_PERSIST_TASK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_ASYNC_PERSIST_TASK_START',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_ASYNC_QUEUE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'QDS_BCKG_TASK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_BLOOM_FILTER',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_CTXS',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_DB_DISK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_DYN_VECTOR',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_EXCLUSIVE_ACCESS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'QDS_HOST_INIT',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'QDS_LOADDB',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_PERSIST_TASK_MAIN_LOOP_SLEEP',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_QDS_CAPTURE_INIT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'QDS_SHUTDOWN_QUEUE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_STMT',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_STMT_DISK',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_TASK_SHUTDOWN',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QDS_TASK_START',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QE_WARN_LIST_SYNC',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'QPJOB_KILL',0,N'Indicates that an asynchronous automatic statistics update was canceled by a call to KILL as the update was starting to run. The terminating thread is suspended, waiting for it to start listening for KILL commands. A good value is less than one second.')
 ,(N'QPJOB_WAITFOR_ABORT',0,N'Indicates that an asynchronous automatic statistics update was canceled by a call to KILL when it was running. The update has now completed but is suspended until the terminating thread message coordination is complete. This is an ordinary but rare state, and should be very short. A good value is less than one second.')
 ,(N'QRY_MEM_GRANT_INFO_MUTEX',0,N'Occurs when Query Execution memory management tries to control access to static grant information list. This state lists information about the current granted and waiting memory requests. This state is a simple access control state. There should never be a long wait on this state. If this mutex is not released, all new memory-using queries will stop responding.')
 ,(N'QRY_PARALLEL_THREAD_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'QRY_PROFILE_LIST_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'QUERY_ERRHDL_SERVICE_DONE',0,N'Identified for informational purposes only. Not supported.     Applies to : SQL Server 2008 R2 only.')
 ,(N'QUERY_EXECUTION_INDEX_SORT_EVENT_OPEN',0,N'Occurs in certain cases when offline create index build is run in parallel, and the different worker threads that are sorting synchronize access to the sort files.')
 ,(N'QUERY_NOTIFICATION_MGR_MUTEX',0,N'Occurs during synchronization of the garbage collection queue in the Query Notification Manager.')
 ,(N'QUERY_NOTIFICATION_SUBSCRIPTION_MUTEX',0,N'Occurs during state synchronization for transactions in Query Notifications.')
 ,(N'QUERY_NOTIFICATION_TABLE_MGR_MUTEX',0,N'Occurs during internal synchronization within the Query Notification Manager.')
 ,(N'QUERY_NOTIFICATION_UNITTEST_MUTEX',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'QUERY_OPTIMIZER_PRINT_MUTEX',0,N'Occurs during synchronization of query optimizer diagnostic output production. This wait type only occurs if diagnostic settings have been enabled under direction of Microsoft Product Support.')
 ,(N'QUERY_TASK_ENQUEUE_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'QUERY_TRACEOUT',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'QUERY_WAIT_ERRHDL_SERVICE',0,N'Identified for informational purposes only.  Not supported.     Applies to : SQL Server 2008 R2 only.')
 ,(N'RBIO_RG_DESTAGE',0,N'Occurs when a Hyperscale database compute node is being throttled due to delayed log consumption by the long term log storage.     Applies to : Azure SQL Database Hyperscale.')
 ,(N'RBIO_RG_LOCALDESTAGE',0,N'Occurs when a Hyperscale database compute node is being throttled due to delayed log consumption by the log service.     Applies to : Azure SQL Database Hyperscale.')
 ,(N'RBIO_RG_REPLICA',0,N'Occurs when a Hyperscale database compute node is being throttled due to delayed log consumption by the readable secondary replica node(s).     Applies to : Azure SQL Database Hyperscale.')
 ,(N'RBIO_RG_STORAGE',0,N'Occurs when a Hyperscale database compute node is being throttled due to delayed log consumption at the page server(s).     Applies to : Azure SQL Database Hyperscale.')
 ,(N'RBIO_WAIT_VLF',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'RECOVER_CHANGEDB',0,N'Occurs during synchronization of database status in warm standby database.')
 ,(N'RECOVERY_MGR_LOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'REDO_THREAD_PENDING_WORK',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'REDO_THREAD_SYNC',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'REMOTE_BLOCK_IO',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'REMOTE_DATA_ARCHIVE_MIGRATION_DMV',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'REMOTE_DATA_ARCHIVE_SCHEMA_DMV',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'REMOTE_DATA_ARCHIVE_SCHEMA_TASK_QUEUE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'REPL_CACHE_ACCESS',0,N'Occurs during synchronization on a replication article cache. During these waits, the replication log reader stalls, and data definition language (DDL) statements on a published table are blocked.')
 ,(N'REPL_HISTORYCACHE_ACCESS',0,N'Internal use only.')
 ,(N'REPL_SCHEMA_ACCESS',0,N'Occurs during synchronization of replication schema version information. This state exists when DDL statements are executed on the replicated object, and when the log reader builds or consumes versioned schema based on DDL occurrence. Contention can be seen on this wait type if you have many published databases on a single publisher with transactional replication and the published databases are very active.')
 ,(N'REPL_TRANFSINFO_ACCESS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'REPL_TRANHASHTABLE_ACCESS',0,N'Internal use only.')
 ,(N'REPL_TRANTEXTINFO_ACCESS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'REPLICA_WRITES',0,N'Occurs while a task waits for completion of page writes to database snapshots or DBCC replicas.')
 ,(N'REQUEST_DISPENSER_PAUSE',0,N'Occurs when a task is waiting for all outstanding I/O to complete, so that I/O to a file can be frozen for snapshot backup.')
 ,(N'REQUEST_FOR_DEADLOCK_SEARCH',0,N'Occurs while the deadlock monitor waits to start the next deadlock search. This wait is expected between deadlock detections, and lengthy total waiting time on this resource does not indicate a problem.')
 ,(N'RESERVED_MEMORY_ALLOCATION_EXT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'RESMGR_THROTTLED',1,N'Occurs when a new request comes in and is throttled based on the GROUP_MAX_REQUESTS setting.')
 ,(N'RESOURCE_GOVERNOR_IDLE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'RESOURCE_QUEUE',0,N'Occurs during synchronization of various internal resource queues.')
 ,(N'RESOURCE_SEMAPHORE',1,N'Occurs when a query memory request during query execution cannot be granted immediately due to other concurrent queries. High waits and wait times may indicate excessive number of concurrent queries, or excessive memory request amounts. Excessive waits of this type may raise SQL error 8645, "A time out occurred while waiting for memory resources to execute the query. Rerun the query."   For detailed information and troubleshooting ideas on memory grant waits, refer to this blog post')
 ,(N'RESOURCE_SEMAPHORE_MUTEX',0,N'Occurs while a query waits for its request for a thread reservation to be fulfilled. It also occurs when synchronizing query compile and memory grant requests.')
 ,(N'RESOURCE_SEMAPHORE_QUERY_COMPILE',1,N'Occurs when the number of concurrent query compilations reaches a throttling limit. High waits and wait times may indicate excessive compilations, recompiles, or uncacheable plans.')
 ,(N'RESOURCE_SEMAPHORE_SMALL_QUERY',0,N'Occurs when memory request by a small query cannot be granted immediately due to other concurrent queries. Wait time should not exceed more than a few seconds, because the server transfers the request to the main query memory pool if it fails to grant the requested memory within a few seconds. High waits may indicate an excessive number of concurrent small queries while the main memory pool is blocked by waiting queries.     Applies to : SQL Server 2008 R2 only.')
 ,(N'RESTORE_FILEHANDLECACHE_ENTRYLOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'RESTORE_FILEHANDLECACHE_LOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'RG_RECONFIG',0,N'Internal use only.')
 ,(N'ROWGROUP_OP_STATS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'ROWGROUP_VERSION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'RTDATA_LIST',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'SATELLITE_CARGO',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SATELLITE_SERVICE_SETUP',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SATELLITE_TASK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SBS_DISPATCH',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'SBS_RECEIVE_TRANSPORT',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'SBS_TRANSPORT',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'SCAN_CHAR_HASH_ARRAY_INITIALIZATION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SE_REPL_CATCHUP_THROTTLE',1,NULL)
 ,(N'SE_REPL_COMMIT_ACK',1,NULL)
 ,(N'SE_REPL_COMMIT_TURN',1,NULL)
 ,(N'SE_REPL_ROLLBACK_ACK',1,NULL)
 ,(N'SE_REPL_SLOW_SECONDARY_THROTTLE',1,NULL)
 ,(N'SEC_DROP_TEMP_KEY',0,N'Occurs after a failed attempt to drop a temporary security key before a retry attempt.')
 ,(N'SECURITY_CNG_PROVIDER_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'SECURITY_CRYPTO_CONTEXT_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SECURITY_DBE_STATE_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SECURITY_KEYRING_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SECURITY_MUTEX',0,N'Occurs when there is a wait for mutexes that control access to the global list of Extensible Key Management (EKM) cryptographic providers and the session-scoped list of EKM sessions.')
 ,(N'SECURITY_RULETABLE_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SEMPLAT_DSI_BUILD',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SEQUENCE_GENERATION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SEQUENTIAL_GUID',0,N'Occurs while a new sequential GUID is being obtained.')
 ,(N'SERVER_IDLE_CHECK',0,N'Occurs during synchronization of SQL Server instance idle status when a resource monitor is attempting to declare a SQL Server instance as idle or trying to wake up.')
 ,(N'SERVER_RECONFIGURE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SESSION_WAIT_STATS_CHILDREN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SHARED_DELTASTORE_CREATION',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SHUTDOWN',0,N'Occurs while a shutdown statement waits for active connections to exit.')
 ,(N'SLEEP_BPOOL_FLUSH',0,N'Occurs when a checkpoint is throttling the issuance of new I/Os in order to avoid flooding the disk subsystem.')
 ,(N'SLEEP_BUFFERPOOL_HELPLW',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SLEEP_DBSTARTUP',0,N'Occurs during database startup while waiting for all databases to recover.')
 ,(N'SLEEP_DCOMSTARTUP',0,N'Occurs once at most during SQL Server instance startup while waiting for DCOM initialization to complete.')
 ,(N'SLEEP_MASTERDBREADY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SLEEP_MASTERMDREADY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SLEEP_MASTERUPGRADED',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SLEEP_MEMORYPOOL_ALLOCATEPAGES',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SLEEP_MSDBSTARTUP',0,N'Occurs when SQL Trace waits for the msdb database to complete startup.')
 ,(N'SLEEP_RETRY_VIRTUALALLOC',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SLEEP_SYSTEMTASK',0,N'Occurs during the start of a background task while waiting for tempdb to complete startup.')
 ,(N'SLEEP_TASK',0,N'Occurs when a task sleeps while waiting for a generic event to occur.')
 ,(N'SLEEP_TEMPDBSTARTUP',0,N'Occurs while a task waits for tempdb to complete startup.')
 ,(N'SLEEP_WORKSPACE_ALLOCATEPAGE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SLO_UPDATE',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'SMSYNC',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SNI_CONN_DUP',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'SNI_CRITICAL_SECTION',0,N'Occurs during internal synchronization within SQL Server networking components.')
 ,(N'SNI_HTTP_WAITFOR_0_DISCON',0,N'Occurs during SQL Server shutdown, while waiting for outstanding HTTP connections to exit.')
 ,(N'SNI_LISTENER_ACCESS',0,N'Occurs while waiting for non-uniform memory access (NUMA) nodes to update state change. Access to state change is serialized.')
 ,(N'SNI_TASK_COMPLETION',0,N'Occurs when there is a wait for all tasks to finish during a NUMA node state change.')
 ,(N'SNI_WRITE_ASYNC',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'SOAP_READ',0,N'Occurs while waiting for an HTTP network read to complete.')
 ,(N'SOAP_WRITE',0,N'Occurs while waiting for an HTTP network write to complete.')
 ,(N'SOCKETDUPLICATEQUEUE_CLEANUP',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'SOS_CALLBACK_REMOVAL',0,N'Occurs while performing synchronization on a callback list in order to remove a callback. It is not expected for this counter to change after server initialization is completed.')
 ,(N'SOS_DISPATCHER_MUTEX',0,N'Occurs during internal synchronization of the dispatcher pool. This includes when the pool is being adjusted.')
 ,(N'SOS_LOCALALLOCATORLIST',0,N'Occurs during internal synchronization in the SQL Server memory manager.     Applies to : SQL Server 2008 R2 only.')
 ,(N'SOS_MEMORY_TOPLEVELBLOCKALLOCATOR',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SOS_MEMORY_USAGE_ADJUSTMENT',0,N'Occurs when memory usage is being adjusted among pools.')
 ,(N'SOS_OBJECT_STORE_DESTROY_MUTEX',0,N'Occurs during internal synchronization in memory pools when destroying objects from the pool.')
 ,(N'SOS_PHYS_PAGE_CACHE',0,N'Accounts for the time a thread waits to acquire the mutex it must acquire before it allocates physical pages or before it returns those pages to the operating system. Waits on this type only appear if the instance of SQL Server uses AWE memory.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SOS_PROCESS_AFFINITY_MUTEX',0,N'Occurs during synchronizing of access to process affinity settings.')
 ,(N'SOS_RESERVEDMEMBLOCKLIST',0,N'Occurs during internal synchronization in the SQL Server Memory Manager.     Applies to : SQL Server 2008 R2 only.')
 ,(N'SOS_SCHEDULER_YIELD',0,N'Occurs when a task voluntarily yields the scheduler for other tasks to execute. During this wait, the task is waiting in a runnable queue for its quantum to be renewed, i.e. waiting to be scheduled to run on the CPU again. Prolonged waits on this wait type most frequently indicate opportunities to optimize queries that perform index or table scans. Focus on plan regression, missing index, stats updates, query re-writes. Optimizing runtimes reduces the need for tasks to be yielding multiple times. If query times for such CPU-consuming tasks are acceptable, then this wait type is expected and can be ignored.')
 ,(N'SOS_SMALL_PAGE_ALLOC',0,N'Occurs during the allocation and freeing of memory that is managed by some memory objects.')
 ,(N'SOS_STACKSTORE_INIT_MUTEX',0,N'Occurs during synchronization of internal store initialization.')
 ,(N'SOS_SYNC_TASK_ENQUEUE_EVENT',0,N'Occurs when a task is started in a synchronous manner. Most tasks in SQL Server are started in an asynchronous manner, in which control returns to the starter immediately after the task request has been placed on the work queue.')
 ,(N'SOS_VIRTUALMEMORY_LOW',0,N'Occurs when a memory allocation waits for a Resource Manager to free up virtual memory.')
 ,(N'SOSHOST_EVENT',0,N'Occurs when a hosted component, such as CLR, waits on a SQL Server event synchronization object.')
 ,(N'SOSHOST_INTERNAL',0,N'Occurs during synchronization of memory manager callbacks used by hosted components, such as CLR.')
 ,(N'SOSHOST_MUTEX',0,N'Occurs when a hosted component, such as CLR, waits on a SQL Server mutex synchronization object.')
 ,(N'SOSHOST_RWLOCK',0,N'Occurs when a hosted component, such as CLR, waits on a SQL Server reader-writer synchronization object.')
 ,(N'SOSHOST_SEMAPHORE',0,N'Occurs when a hosted component, such as CLR, waits on a SQL Server semaphore synchronization object.')
 ,(N'SOSHOST_SLEEP',0,N'Occurs when a hosted task sleeps while waiting for a generic event to occur. Hosted tasks are used by hosted components such as CLR.')
 ,(N'SOSHOST_TRACELOCK',0,N'Occurs during synchronization of access to trace streams.')
 ,(N'SOSHOST_WAITFORDONE',0,N'Occurs when a hosted component, such as CLR, waits for a task to complete.')
 ,(N'SP_PREEMPTIVE_SERVER_DIAGNOSTICS_SLEEP',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SP_SERVER_DIAGNOSTICS_BUFFER_ACCESS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SP_SERVER_DIAGNOSTICS_INIT_MUTEX',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SP_SERVER_DIAGNOSTICS_SLEEP',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLCLR_APPDOMAIN',0,N'Occurs while CLR waits for an application domain to complete startup.')
 ,(N'SQLCLR_ASSEMBLY',0,N'Occurs while waiting for access to the loaded assembly list in the appdomain.')
 ,(N'SQLCLR_DEADLOCK_DETECTION',0,N'Occurs while CLR waits for deadlock detection to complete.')
 ,(N'SQLCLR_QUANTUM_PUNISHMENT',0,N'Occurs when a CLR task is throttled because it has exceeded its execution quantum. This throttling is done in order to reduce the effect of this resource-intensive task on other tasks.')
 ,(N'SQLSORT_NORMMUTEX',0,N'Occurs during internal synchronization, while initializing internal sorting structures.')
 ,(N'SQLSORT_SORTMUTEX',0,N'Occurs during internal synchronization, while initializing internal sorting structures.')
 ,(N'SQLTRACE_BUFFER_FLUSH',0,N'Occurs when a task is waiting for a background task to flush trace buffers to disk every four seconds.     Applies to : SQL Server 2008 R2 only.')
 ,(N'SQLTRACE_FILE_BUFFER',0,N'Occurs during synchronization on trace buffers during a file trace.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLTRACE_FILE_READ_IO_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLTRACE_FILE_WRITE_IO_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLTRACE_INCREMENTAL_FLUSH_SLEEP',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLTRACE_LOCK',0,N'Internal use only.     Applies to : SQL Server 2008 R2 only.')
 ,(N'SQLTRACE_PENDING_BUFFER_WRITERS',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'SQLTRACE_SHUTDOWN',0,N'Occurs while trace shutdown waits for outstanding trace events to complete.')
 ,(N'SQLTRACE_WAIT_ENTRIES',0,N'Occurs while a SQL Trace event queue waits for packets to arrive on the queue.')
 ,(N'SRVPROC_SHUTDOWN',0,N'Occurs while the shutdown process waits for internal resources to be released to shutdown cleanly.')
 ,(N'STARTUP_DEPENDENCY_MANAGER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'TDS_BANDWIDTH_STATE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'TDS_INIT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'TDS_PROXY_CONTAINER',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'TEMPOBJ',0,N'Occurs when temporary object drops are synchronized. This wait is rare, and only occurs if a task has requested exclusive access for temp table drops.')
 ,(N'TEMPORAL_BACKGROUND_PROCEED_CLEANUP',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'TERMINATE_LISTENER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'THREADPOOL',1,N'Occurs when a task (query or login/logout) is waiting for a worker thread to execute it. This can indicate that the maximum worker thread setting is misconfigured, or that, most commonly, batch executions are taking unusually long, thus reducing the number of worker threads available to satisfy other batches. Examine the performance of batches (queries) and reduce query duration by either reducing bottlenecks (blocking, parallelism, I/O, latch waits), or providing proper indexing or query design.')
 ,(N'TIMEPRIV_TIMEPERIOD',0,N'Occurs during internal synchronization of the Extended Events timer.')
 ,(N'TRACE_EVTNOTIF',0,N'Internal use only.')
 ,(N'TRACEWRITE',0,N'Occurs when the SQL Trace rowset trace provider waits for either a free buffer or a buffer with events to process.')
 ,(N'TRAN_MARKLATCH_DT',0,N'Occurs when waiting for a destroy mode latch on a transaction mark latch. Transaction mark latches are used for synchronization of commits with marked transactions.')
 ,(N'TRAN_MARKLATCH_EX',0,N'Occurs when waiting for an exclusive mode latch on a marked transaction. Transaction mark latches are used for synchronization of commits with marked transactions.')
 ,(N'TRAN_MARKLATCH_KP',0,N'Occurs when waiting for a keep mode latch on a marked transaction. Transaction mark latches are used for synchronization of commits with marked transactions.')
 ,(N'TRAN_MARKLATCH_NL',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'TRAN_MARKLATCH_SH',0,N'Occurs when waiting for a shared mode latch on a marked transaction. Transaction mark latches are used for synchronization of commits with marked transactions.')
 ,(N'TRAN_MARKLATCH_UP',0,N'Occurs when waiting for an update mode latch on a marked transaction. Transaction mark latches are used for synchronization of commits with marked transactions.')
 ,(N'TRANSACTION_MUTEX',0,N'Occurs during synchronization of access to a transaction by multiple batches.')
 ,(N'UCS_ENDPOINT_CHANGE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UCS_MANAGER',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UCS_MEMORY_NOTIFICATION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UCS_SESSION_REGISTRATION',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UCS_TRANSPORT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UCS_TRANSPORT_STREAM_CHANGE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'UTIL_PAGE_ALLOC',0,N'Occurs when transaction log scans wait for memory to be available during memory pressure.')
 ,(N'VDI_CLIENT_COMPLETECOMMAND',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'VDI_CLIENT_GETCOMMAND',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'VDI_CLIENT_OPERATION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'VDI_CLIENT_OTHER',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'VERSIONING_COMMITTING',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'VIA_ACCEPT',0,N'Occurs when a Virtual Interface Adapter (VIA) provider connection is completed during startup.')
 ,(N'VIEW_DEFINITION_MUTEX',0,N'Occurs during synchronization on access to cached view definitions.')
 ,(N'WAIT_FOR_RESULTS',0,N'Occurs when waiting for a query notification to be triggered.')
 ,(N'WAIT_ON_SYNC_STATISTICS_REFRESH',0,N'Occurs when waiting for synchronous statistics update to complete before query compilation and execution can resume.    Applies to : Starting with SQL Server 2019 (15.x)')
 ,(N'WAIT_SCRIPTDEPLOYMENT_REQUEST',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_SCRIPTDEPLOYMENT_WORKER',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XLOGREAD_SIGNAL',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'WAIT_XTP_ASYNC_TX_COMPLETION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_CKPT_AGENT_WAKEUP',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_CKPT_CLOSE',0,N'Occurs when waiting for a checkpoint to complete.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_CKPT_ENABLED',0,N'Occurs when checkpointing is disabled, and waiting for checkpointing to be enabled.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_CKPT_STATE_LOCK',0,N'Occurs when synchronizing checking of checkpoint state.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_COMPILE_WAIT',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'WAIT_XTP_GUEST',0,N'Occurs when the database memory allocator needs to stop receiving low-memory notifications.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'WAIT_XTP_HOST_WAIT',0,N'Occurs when waits are triggered by the database engine and implemented by the host.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_OFFLINE_CKPT_BEFORE_REDO',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_OFFLINE_CKPT_LOG_IO',0,N'Occurs when offline checkpoint is waiting for a log read IO to complete.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_OFFLINE_CKPT_NEW_LOG',0,N'Occurs when offline checkpoint is waiting for new log records to scan.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_PROCEDURE_ENTRY',0,N'Occurs when a drop procedure is waiting for all current executions of that procedure to complete.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_RECOVERY',0,N'Occurs when database recovery is waiting for recovery of memory-optimized objects to finish.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAIT_XTP_SERIAL_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'WAIT_XTP_SWITCH_TO_INACTIVE',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'WAIT_XTP_TASK_SHUTDOWN',0,N'Occurs when waiting for an In-Memory OLTP thread to complete.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'WAIT_XTP_TRAN_DEPENDENCY',0,N'Occurs when waiting for transaction dependencies.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WAITFOR',0,N'Occurs as a result of a WAITFOR Transact-SQL statement. The duration of the wait is determined by the parameters to the statement. This is a user-initiated wait.')
 ,(N'WAITFOR_PER_QUEUE',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'WAITFOR_TASKSHUTDOWN',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'WAITSTAT_MUTEX',0,N'Occurs during synchronization of access to the collection of statistics used to populate sys.dm_os_wait_stats.')
 ,(N'WCC',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'WINDOW_AGGREGATES_MULTIPASS',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'WINFAB_API_CALL',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WINFAB_REPLICA_BUILD_OPERATION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'WINFAB_REPORT_FAULT',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'WORKTBL_DROP',0,N'Occurs while pausing before retrying, after a failed worktable drop.')
 ,(N'WRITE_COMPLETION',0,N'Occurs when a write operation is in progress.')
 ,(N'WRITELOG',0,N'Occurs while waiting for a log flush to complete. Common operations that cause log flushes are transaction commits and checkpoints. Common reasons for long waits on WRITELOG are: disk latency (where transaction log files reside), the inability for I/O to keep up with transactions, or, a large number of transaction log operations and flushes (commits, rollback)')
 ,(N'XACT_OWN_TRANSACTION',0,N'Occurs while waiting to acquire ownership of a transaction.')
 ,(N'XACT_RECLAIM_SESSION',0,N'Occurs while waiting for the current owner of a session to release ownership of the session.')
 ,(N'XACTLOCKINFO',0,N'Occurs during synchronization of access to the list of locks for a transaction. In addition to the transaction itself, the list of locks is accessed by operations such as deadlock detection and lock migration during page splits.')
 ,(N'XACTWORKSPACE_MUTEX',0,N'Occurs during synchronization of defections from a transaction, as well as the number of database locks between enlist members of a transaction.')
 ,(N'XDB_CONN_DUP_HASH',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XDES_HISTORY',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XDES_OUT_OF_ORDER_LIST',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XDES_SNAPSHOT',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XDESTSVERMGR',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XE_BUFFERMGR_ALLPROCESSED_EVENT',0,N'Occurs when Extended Events session buffers are flushed to targets. This wait occurs on a background thread.')
 ,(N'XE_BUFFERMGR_FREEBUF_EVENT',0,N'Occurs when either of the following conditions is true:')
 ,(N'XE_CALLBACK_LIST',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XE_CX_FILE_READ',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'XE_DISPATCHER_CONFIG_SESSION_LIST',0,N'Occurs when an Extended Events session that is using asynchronous targets is started or stopped. This wait indicates either of the following:')
 ,(N'XE_DISPATCHER_JOIN',0,N'Occurs when a background thread that is used for Extended Events sessions is terminating.')
 ,(N'XE_DISPATCHER_WAIT',0,N'Occurs when a background thread that is used for Extended Events sessions is waiting for event buffers to process.')
 ,(N'XE_FILE_TARGET_TVF',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XE_LIVE_TARGET_TVF',0,N'Internal use only.     Applies to : SQL Server 2012 (11.x) and later.')
 ,(N'XE_MODULEMGR_SYNC',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'XE_OLS_LOCK',0,N'Identified for informational purposes only. Not supported. Future compatibility is not guaranteed.')
 ,(N'XE_PACKAGE_LOCK_BACKOFF',0,N'Identified for informational purposes only. Not supported.     Applies to : SQL Server 2008 R2 only.')
 ,(N'XE_SERVICES_EVENTMANUAL',0,N'Internal use only.')
 ,(N'XE_SERVICES_MUTEX',0,N'Internal use only.')
 ,(N'XE_SERVICES_RWLOCK',0,N'Internal use only.')
 ,(N'XE_SESSION_CREATE_SYNC',0,N'Internal use only.')
 ,(N'XE_SESSION_FLUSH',0,N'Internal use only.')
 ,(N'XE_SESSION_SYNC',0,N'Internal use only.')
 ,(N'XE_STM_CREATE',0,N'Internal use only.')
 ,(N'XE_TIMER_EVENT',0,N'Internal use only.')
 ,(N'XE_TIMER_MUTEX',0,N'Internal use only.')
 ,(N'XE_TIMER_TASK_DONE',0,N'Internal use only.')
 ,(N'XIO_CREDENTIAL_MGR_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XIO_CREDENTIAL_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XIO_EDS_MGR_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'XIO_EDS_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'XIO_IOSTATS_BLOBLIST_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'XIO_IOSTATS_FCBLIST_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2017 (14.x) and later.')
 ,(N'XIO_LEASE_RENEW_MGR_RWLOCK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XTP_HOST_DB_COLLECTION',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'XTP_HOST_LOG_ACTIVITY',0,N'Internal use only.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'XTP_HOST_PARALLEL_RECOVERY',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XTP_PREEMPTIVE_TASK',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XTP_TRUNCATION_LSN',0,N'Internal use only.     Applies to : SQL Server 2016 (13.x) and later.')
 ,(N'XTPPROC_CACHE_ACCESS',0,N'Occurs when for accessing all natively compiled stored procedure cache objects.     Applies to : SQL Server 2014 (12.x) and later.')
 ,(N'XTPPROC_PARTITIONED_STACK_CREATE',0,N'Occurs when allocating per-NUMA node natively compiled stored procedure cache structures (must be done single threaded) for a given procedure.     Applies to : SQL Server 2012 (11.x) and later.')
) AS [Source] ([WaitType],[IsCriticalWait],[Description])
ON ([Target].[WaitType] = [Source].[WaitType])
WHEN MATCHED AND (
	NULLIF([Source].[IsCriticalWait], [Target].[IsCriticalWait]) IS NOT NULL OR NULLIF([Target].[IsCriticalWait], [Source].[IsCriticalWait]) IS NOT NULL OR 
	NULLIF([Source].[Description], [Target].[Description]) IS NOT NULL OR NULLIF([Target].[Description], [Source].[Description]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[IsCriticalWait] = [Source].[IsCriticalWait], 
  [Target].[Description] = [Source].[Description]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([WaitType],[IsCriticalWait],[Description])
 VALUES([Source].[WaitType],[Source].[IsCriticalWait],[Source].[Description]);

 -- These wait types are excluded from collection
 UPDATE dbo.WaitType
 SET IsExcluded=1
 WHERE WaitType IN ( N'BROKER_EVENTHANDLER', N'BROKER_RECEIVE_WAITFOR', N'BROKER_TASK_STOP', N'BROKER_TO_FLUSH',
                           N'BROKER_TRANSMITTER', N'CHECKPOINT_QUEUE', N'CHKPT', N'CLR_AUTO_EVENT',
                           N'CLR_MANUAL_EVENT', N'CLR_SEMAPHORE', N'CXCONSUMER', N'DBMIRROR_DBM_EVENT',
                           N'DBMIRROR_EVENTS_QUEUE', N'DBMIRROR_WORKER_QUEUE', N'DBMIRRORING_CMD', N'DIRTY_PAGE_POLL',
                           N'DISPATCHER_QUEUE_SEMAPHORE', N'EXECSYNC', N'FSAGENT', N'FT_IFTS_SCHEDULER_IDLE_WAIT',
                           N'FT_IFTSHC_MUTEX', N'HADR_CLUSAPI_CALL', N'HADR_FABRIC_CALLBACK',
                           N'HADR_FILESTREAM_IOMGR_IOCOMPLETION', N'HADR_LOGCAPTURE_WAIT',
                           N'HADR_NOTIFICATION_DEQUEUE', N'HADR_TIMER_TASK', N'HADR_WORK_QUEUE', N'KSOURCE_WAKEUP',
                           N'LAZYWRITER_SLEEP', N'LOGMGR_QUEUE', N'MEMORY_ALLOCATION_EXT', N'ONDEMAND_TASK_QUEUE',
                           N'PARALLEL_REDO_DRAIN_WORKER', N'PARALLEL_REDO_LOG_CACHE', N'PARALLEL_REDO_TRAN_LIST',
                           N'PARALLEL_REDO_WORKER_SYNC', N'PARALLEL_REDO_WORKER_WAIT_WORK',
                           N'PREEMPTIVE_HADR_LEASE_MECHANISM', N'PREEMPTIVE_SP_SERVER_DIAGNOSTICS',
                           N'PREEMPTIVE_OS_LIBRARYOPS', N'PREEMPTIVE_OS_COMOPS', N'PREEMPTIVE_OS_CRYPTOPS',
                           N'PREEMPTIVE_OS_PIPEOPS', N'PREEMPTIVE_OS_AUTHENTICATIONOPS', N'PREEMPTIVE_OS_GENERICOPS',
                           N'PREEMPTIVE_OS_VERIFYTRUST', N'PREEMPTIVE_OS_FILEOPS', N'PREEMPTIVE_OS_DEVICEOPS',
                           N'PREEMPTIVE_OS_QUERYREGISTRY', N'PREEMPTIVE_OS_WRITEFILE',
                           N'PREEMPTIVE_XE_CALLBACKEXECUTE', N'PREEMPTIVE_XE_DISPATCHER',
                           N'PREEMPTIVE_XE_GETTARGETSTATE', N'PREEMPTIVE_XE_SESSIONCOMMIT',
                           N'PREEMPTIVE_XE_TARGETINIT', N'PREEMPTIVE_XE_TARGETFINALIZE',
                           N'PWAIT_ALL_COMPONENTS_INITIALIZED', N'PWAIT_DIRECTLOGCONSUMER_GETNEXT',
                           N'PWAIT_EXTENSIBILITY_CLEANUP_TASK', N'QDS_PERSIST_TASK_MAIN_LOOP_SLEEP',
                           N'QDS_ASYNC_QUEUE', N'QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP',
                           N'REQUEST_FOR_DEADLOCK_SEARCH', N'RESOURCE_QUEUE', N'SERVER_IDLE_CHECK',
                           N'SLEEP_BPOOL_FLUSH', N'SLEEP_DBSTARTUP', N'SLEEP_DCOMSTARTUP', N'SLEEP_MASTERDBREADY',
                           N'SLEEP_MASTERMDREADY', N'SLEEP_MASTERUPGRADED', N'SLEEP_MSDBSTARTUP', N'SLEEP_SYSTEMTASK',
                           N'SLEEP_TASK', N'SLEEP_TEMPDBSTARTUP', N'SNI_HTTP_ACCEPT', N'SOS_WORK_DISPATCHER',
                           N'SP_SERVER_DIAGNOSTICS_SLEEP', N'SQLTRACE_BUFFER_FLUSH',
                           N'SQLTRACE_INCREMENTAL_FLUSH_SLEEP', N'SQLTRACE_WAIT_ENTRIES',
                           N'STARTUP_DEPENDENCY_MANAGER', N'WAIT_FOR_RESULTS', N'WAITFOR', N'WAITFOR_TASKSHUTDOWN',
                           N'WAIT_XTP_HOST_WAIT', N'WAIT_XTP_OFFLINE_CKPT_NEW_LOG', N'WAIT_XTP_CKPT_CLOSE',
                           N'WAIT_XTP_RECOVERY', N'XE_BUFFERMGR_ALLPROCESSED_EVENT', N'XE_DISPATCHER_JOIN',
                           N'XE_DISPATCHER_WAIT', N'XE_LIVE_TARGET_TVF', N'XE_TIMER_EVENT',N'VDI_CLIENT_OTHER',
						   N'PVS_PREALLOCATE',N'REDO_THREAD_PENDING_WORK'
                         )
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
				('SessionWaits',30),
				('IdentityColumnsHistory',730)
				) AS t(TableName,RetentionDays)
WHERE NOT EXISTS(SELECT 1 FROM dbo.DataRetention DR WHERE DR.TableName = T.TableName)

DELETE dbo.OSLoadedModulesStatus
WHERE IsSystem=1

/*	MS docs list of drivers and modules known to cause issues:
	https://learn.microsoft.com/en-us/troubleshoot/sql/performance/performance-consistency-issues-filter-drivers-modules
*/
MERGE INTO dbo.OSLoadedModulesStatus AS [Target]
USING (
	VALUES
	( N'%', N'%', N'XTP Native DLL', 4,NULL,1 ), 
	( N'%', N'Microsoft Corporation', N'%', 4,NULL,1 ), 
	( N'%', N'Корпорация Майкрософт', N'%', 4, NULL,1 ), 
	( N'%\ENTAPI.DLL', N'%', N'%', 1, N'McAfee VirusScan Enterprise',1 ), 
	( N'%\HcApi.dll', N'%', N'%', 1, N'McAfee Host Intrusion',1  ), 
	( N'%\HcSQL.dll', N'%', N'%', 1, N'McAfee Host Intrusion',1  ), 
	( N'%\HcThe.dll', N'%', N'%', 1, N'McAfee Host Intrusion',1  ), 
	( N'%\HIPI.DLL', N'%', N'%', 1, N'McAfee Host Intrusion',1  ), 
	( N'%\PIOLEDB.DLL', N'%', N'%', 1, N'OSISoft PI data access',1  ), 
	( N'%\PISDK.DLL', N'%', N'%', 1, N'OSISoft PI data access',1   ), 
	( N'%\SOPHOS_DETOURED.DLL', N'%', N'%', 1,N'Sophos AV',1  ), 
	( N'%\SOPHOS_DETOURED_x64.DLL', N'%', N'%', 1,N'Sophos AV',1  ), 
	( N'%\SOPHOS~%.dll', N'%', N'%', 1,N'Sophos AV',1  ), 
	( N'%\SWI_IFSLSP_64.dll', N'%', N'%', 1, N'Sophos AV',1  ), 
	( N'%IisRTL.DLL', N'%', N'%', 4, NULL,1 ), 
	( N'%iisutil.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%instapi.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%MSDART.DLL', N'%', N'%', 4, NULL,1 ), 
	( N'%msxml3.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%msxmlsql.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%ODBC32.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%oledb32.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%OLEDB32R.DLL', N'%', N'%', 4, NULL,1 ), 
	( N'%ScriptControl64%.dll', N'%', N'%', 1, N'CrowdStrike',1 ), 
	( N'%UMPDC.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%umppc%.dll', N'%', N'%', 1, N'CrowdStrike',1 ), 
	( N'%w3ctrs.dll', N'%', N'%', 4 , NULL,1 ), 
	( N'%XmlLite.dll', N'%', N'%', 4, NULL,1 ), 
	( N'%xpsqlbot.dll', N'%', N'%', 4, NULL,1 ),
	( N'%perfiCrcPerfMonMgr.DLL',N'%',N'%',1,N'Trend Micro',1),
	( N'%MFEBOPK.SYS',N'%',N'%',1,N'McAfee VirusScan Enterprise',1),
	( N'%NLEMSQL%.SYS',N'%',N'%',1,N'NetLib Encryptionizer-Software',1),
	( N'%MFETDIK.SYS',N'%',N'%',1,N'McAfee Anti-Virus Mini-Firewall',1),
	( N'%sqlmaggieAntiVirus%.dll',N'%',N'%',1,N'Malware',1),
	( N'%AntiVirus%',N'%',N'%',1,N'AntiVirus??',1),
	( N'%odbccp32.dll',N'%',N'%',4,NULL,1),
	( N'%isa-l.dll',N'Intel Corporation',N'%',4,NULL,1),
	( N'%qatzip.dll',N'Intel Corporation',N'%',4,NULL,1),
	( N'%\msadce.dll',N'%',N'%',4,NULL,1),
	( N'%\msdatl3.dll',N'%',N'%',4,NULL,1)
	)  [Source](Name,Company,Description,Status,Notes,IsSystem)
ON ([Target].[Name] = [Source].[Name] AND [Target].[Company] = [Source].[Company] AND [Target].[Description] = [Source].[Description])
WHEN MATCHED AND (
	NULLIF([Source].[Status], [Target].[Status]) IS NOT NULL OR NULLIF([Target].[Status], [Source].[Status]) IS NOT NULL OR 
	NULLIF([Source].[Notes], [Target].[Notes]) IS NOT NULL OR NULLIF([Target].[Notes], [Source].[Notes]) IS NOT NULL OR 
	NULLIF([Source].[IsSystem], [Target].[IsSystem]) IS NOT NULL OR NULLIF([Target].[IsSystem], [Source].[IsSystem]) IS NOT NULL) THEN
UPDATE SET
  [Target].[Status] = [Source].[Status], 
  [Target].[Notes] = [Source].[Notes], 
  [Target].[IsSystem] = [Source].[IsSystem]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([Name],[Company],[Description],[Status],[Notes],[IsSystem])
 VALUES([Source].[Name],[Source].[Company],[Source].[Description],[Source].[Status],[Source].[Notes],[Source].[IsSystem])
WHEN NOT MATCHED BY SOURCE AND [Target].IsSystem=1 THEN 
 DELETE;

 IF @@ROWCOUNT>0
 BEGIN
	EXEC dbo.OSLoadedModules_RefreshStatus
 END


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
(-1,'MemoryUsage',5,10),
(-1,'IdentityColumns',10080,20160)
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
 ,(28,N'PARAMETER_SENSITIVE_PLAN_OPTIMIZATION',N'1')
 ,(29,N'ASYNC_STATS_UPDATE_WAIT_AT_LOW_PRIORITY',N'0')
 ,(31,N'CE_FEEDBACK', N'1' )
 ,(33,N'MEMORY_GRANT_FEEDBACK_PERSISTENCE', N'1' )
 ,(34,N'MEMORY_GRANT_FEEDBACK_PERCENTILE_GRANT', N'1' )
 ,(35,N'OPTIMIZED_PLAN_FORCING', N'0' )
 ,(37,N'DOP_FEEDBACK', N'0' )
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
	  (N'Latches',N'Average Latch Wait Time (ms)',N'Average Latch Wait Time Base')
	 ,(N'Locks',N'Average Wait Time (ms)',N'Average Wait Time Base')
	 ,(N'Resource Pool Stats',N'Avg Disk Read IO (ms)',N'Avg Disk Read IO (ms) Base')
	 ,(N'Resource Pool Stats',N'Avg Disk Write IO (ms)',N'Avg Disk Write IO (ms) Base')
	 ,(N'HTTP Storage',N'Avg. Bytes/Read',N'Avg. Bytes/Read BASE')
	 ,(N'HTTP Storage',N'Avg. Bytes/Transfer',N'Avg. Bytes/Transfer BASE')
	 ,(N'HTTP Storage',N'Avg. Bytes/Write',N'Avg. Bytes/Write BASE')
	 ,(N'Broker TO Statistics',N'Avg. Length of Batched Writes',N'Avg. Length of Batched Writes BS')
	 ,(N'HTTP Storage',N'Avg. microsec/Read',N'Avg. microsec/Read BASE')
	 ,(N'HTTP Storage',N'Avg. microsec/Read Comp',N'Avg. microsec/Read Comp BASE')
	 ,(N'HTTP Storage',N'Avg. microsec/Transfer',N'Avg. microsec/Transfer BASE')
	 ,(N'HTTP Storage',N'Avg. microsec/Write',N'Avg. microsec/Write BASE')
	 ,(N'HTTP Storage',N'Avg. microsec/Write Comp',N'Avg. microsec/Write Comp BASE')
	 ,(N'Broker TO Statistics',N'Avg. Time Between Batches (ms)',N'Avg. Time Between Batches Base')
	 ,(N'Broker TO Statistics',N'Avg. Time to Write Batch (ms)',N'Avg. Time to Write Batch Base')
	 ,(N'Buffer Manager',N'Buffer cache hit ratio',N'Buffer cache hit ratio base')
	 ,(N'Catalog Metadata',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
	 ,(N'Cursor Manager by Type',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
	 ,(N'Plan Cache',N'Cache Hit Ratio',N'Cache Hit Ratio Base')
	 ,(N'Resource Pool Stats',N'CPU delayed %',N'CPU delayed % base')
	 ,(N'Workload Group Stats',N'CPU delayed %',N'CPU delayed % base')
	 ,(N'Resource Pool Stats',N'CPU effective %',N'CPU effective % base')
	 ,(N'Workload Group Stats',N'CPU effective %',N'CPU effective % base')
	 ,(N'Resource Pool Stats',N'CPU usage %',N'CPU usage % base')
	 ,(N'Workload Group Stats',N'CPU usage %',N'CPU usage % base')
	 ,(N'Databases',N'Log Cache Hit Ratio',N'Log Cache Hit Ratio Base')
	 ,(N'Broker/DBM Transport',N'Msg Fragment Recv Size Avg',N'Msg Fragment Recv Size Avg Base')
	 ,(N'Broker/DBM Transport',N'Msg Fragment Send Size Avg',N'Msg Fragment Send Size Avg Base')
	 ,(N'Broker/DBM Transport',N'Receive I/O Len Avg',N'Receive I/O Len Avg Base')
	 ,(N'Columnstore',N'Segment Cache Hit Ratio',N'Segment Cache Hit Ratio Base')
	 ,(N'Broker/DBM Transport',N'Send I/O Len Avg',N'Send I/O Len Avg Base')
	 ,(N'Transactions',N'Update conflict ratio',N'Update conflict ratio base')
	 ,(N'Access Methods',N'Worktables From Cache Ratio',N'Worktables From Cache Base')
) AS [Source] ([object_name],[counter_name],[base_counter_name])
ON ([Target].[counter_name] = [Source].[counter_name] AND [Target].[object_name] = [Source].[object_name])
WHEN MATCHED AND (
	NULLIF([Source].[base_counter_name], [Target].[base_counter_name]) IS NOT NULL OR NULLIF([Target].[base_counter_name], [Source].[base_counter_name]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[base_counter_name] = [Source].[base_counter_name]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([object_name],[counter_name],[base_counter_name])
 VALUES([Source].[object_name],[Source].[counter_name],[Source].[base_counter_name])
WHEN NOT MATCHED BY SOURCE THEN 
	DELETE;

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
			('MemoryDumpAckDate'),
			('PurgeBlockingSnapshotSummary_CompletedDate'),
			('PurgeBlockingSnapshotSummary_StartDate')
	  ) T(SettingName)
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.Settings S
				WHERE S.SettingName=T.SettingName);

INSERT INTO dbo.Settings(SettingName,SettingValue)
SELECT SettingName,SettingValue 
FROM (VALUES('MemoryDumpCriticalThresholdHrs',48),
		('MemoryDumpWarningThresholdHrs',168),
		('CPUCriticalThreshold',90),
		('CPUWarningThreshold',75),
		('CPULowThreshold',50),
		('ReadLatencyCriticalThreshold',50),
		('ReadLatencyWarningThreshold',10),
		('ReadLatencyGoodThreshold',10),
		('WriteLatencyCriticalThreshold',50),
		('WriteLatencyWarningThreshold',10),
		('WriteLatencyGoodThreshold',10),
		('MinIOPsThreshold',100),
		('CriticalWaitCriticalThreshold',1000),
		('CriticalWaitWarningThreshold',10)
		) T(SettingName,SettingValue)
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


IF NOT EXISTS(SELECT 1 
			FROM dbo.Settings
			WHERE SettingName = 'InstanceTagIDsMigratedDate'
			)
BEGIN
	PRINT 'Migrating Tags'
	/* 
		Migrate tags from InstanceTags to InstanceIDsTags table
		Excluding user tags on AzureDB
	*/
	INSERT INTO dbo.InstanceIDsTags(
			InstanceID,
			TagID
	)
	SELECT I.InstanceID,
			IT.TagID
	FROM dbo.InstanceTags IT 
	JOIN dbo.Instances I ON IT.Instance = I.Instance 
	JOIN dbo.Tags T ON IT.TagID = T.TagID
	WHERE NOT (I.EngineEdition=5 AND T.TagName NOT LIKE '{%')
	AND NOT EXISTS(SELECT 1 
					FROM dbo.InstanceIDsTags IDT 
					WHERE IT.TagID = IDT.TagID 
					AND I.InstanceID = IDT.InstanceID
					)

	DELETE IT 
	FROM dbo.InstanceTags IT 
	JOIN dbo.Instances I ON IT.Instance = I.Instance 
	JOIN dbo.Tags T ON IT.TagID = T.TagID
	WHERE NOT (I.EngineEdition=5 AND T.TagName NOT LIKE '{%')

	INSERT INTO dbo.Settings(SettingName,SettingValue)
	VALUES('InstanceTagIDsMigratedDate',GETUTCDATE())
END;

MERGE INTO dbo.Counters AS T
USING (VALUES
	('sys.dm_os_nodes','Count of Nodes Reporting thread resources Low','',1.0,9999999999999999999.999999999,0.000000001,1.0,0,0),
	('sys.dm_os_sys_memory','System Low Memory Signal State','',1.0,9999999999999999999.999999999,0.000000001,1.0,0,0),
	('sys.dm_os_sys_memory','Available Physical Memory (KB)','',0,262144,262144,524288,NULL,NULL),
	('Memory Manager','Memory Grants Pending','',1.0,9999999999999999999.999999999,0.000000001,1.0,0,0),
	('Locks','Number of Deadlocks/sec','_Total',1,9999999999999999999.999999999,0.000000001,1,0,0),
	('Plan Cache','Cache Object Counts','_Total',0,200,200,1000,NULL,NULL),
	('General Statistics','Processes blocked','',50,9999999999999999999.999999999,1,50,0,0)
) AS S (object_name,counter_name,instance_name,SystemCriticalFrom,SystemCriticalTo,SystemWarningFrom,SystemWarningTo,SystemGoodFrom,SystemGoodTo)
ON T.object_name = S.object_name AND T.counter_name = S.counter_name AND T.instance_name = S.instance_name 
WHEN MATCHED THEN 
UPDATE SET T.SystemCriticalFrom = S.SystemCriticalFrom,
			T.SystemCriticalTo = S.SystemCriticalTo,
			T.SystemWarningFrom = S.SystemWarningfrom,
			T.SystemWarningTo = S.SystemWarningTo,
			T.SystemGoodFrom = S.SystemGoodFrom,
			T.SystemGoodTo = S.SystemGoodTo,
			/* 
				Set user values to NULL if they match system value and system value isn't set yet.  Handling upgrades before System columns were added 
				System thresholds can always be updated now without impacting user preferences
			*/
			T.CriticalFrom = CASE WHEN T.CriticalFrom = S.SystemCriticalFrom AND T.SystemCriticalFrom IS NULL THEN NULL  ELSE T.CriticalFrom END,
			T.CriticalTo = CASE WHEN T.CriticalTo = S.SystemCriticalTo AND T.SystemCriticalTo IS NULL THEN NULL  ELSE T.CriticalTo END,
			T.WarningFrom = CASE WHEN T.WarningFrom = S.SystemWarningFrom AND T.SystemWarningFrom IS NULL THEN NULL  ELSE T.WarningFrom END,
			T.WarningTo = CASE WHEN T.WarningTo = S.SystemWarningTo AND T.SystemWarningTo IS NULL THEN NULL  ELSE T.WarningTo END,
			T.GoodFrom = CASE WHEN T.GoodFrom = S.SystemGoodFrom AND T.SystemGoodFrom IS NULL THEN NULL  ELSE T.GoodFrom END,
			T.GoodTo = CASE WHEN T.GoodTo = S.SystemGoodTo AND T.SystemGoodTo IS NULL THEN NULL  ELSE T.GoodTo END
WHEN NOT MATCHED BY TARGET THEN
INSERT(object_name,counter_name,instance_name,SystemCriticalFrom,SystemCriticalTo,SystemWarningFrom,SystemWarningTo,SystemGoodFrom,SystemGoodTo)
VALUES(object_name,counter_name,instance_name,SystemCriticalFrom,SystemCriticalTo,SystemWarningFrom,SystemWarningTo,SystemGoodFrom,SystemGoodTo);

MERGE INTO [DBADash].[ViewType] AS [Target]
USING (VALUES
	(0,N'Metric'),
	(1,N'PerformanceSummary'),
	(2,N'Tree')
) AS [Source] ([ViewTypeID],[ViewType])
ON ([Target].[ViewTypeID] = [Source].[ViewTypeID])
WHEN MATCHED AND (
	NULLIF([Source].[ViewType], [Target].[ViewType]) IS NOT NULL OR NULLIF([Target].[ViewType], [Source].[ViewType]) IS NOT NULL) THEN
 UPDATE SET
  [Target].[ViewType] = [Source].[ViewType]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([ViewTypeID],[ViewType])
 VALUES([Source].[ViewTypeID],[Source].[ViewType])
WHEN NOT MATCHED BY SOURCE THEN 
 DELETE;

IF NOT EXISTS(
		SELECT 1 
		FROM DBADash.Users 
		WHERE UserID=-1
		)
BEGIN
	SET IDENTITY_INSERT DBADash.Users ON
	INSERT INTO DBADash.Users
	(
		UserID,
		UserName
	)
	VALUES(-1, N'{SYSTEM}')
	SET IDENTITY_INSERT DBADash.Users OFF
END

IF NOT EXISTS(SELECT 1 
			FROM dbo.IdentityColumnThresholds
			WHERE InstanceID=-1
			AND DatabaseID=-1
			AND object_name=''
			)
BEGIN
	INSERT INTO dbo.IdentityColumnThresholds
	(
		InstanceID,
		DatabaseID,
		object_name,
		PctUsedWarningThreshold,
		PctUsedCriticalThreshold
	)
	VALUES (-1, -1, N'', 0.5, 0.8);
END

ALTER DATABASE [$(DatabaseName)] SET AUTO_UPDATE_STATISTICS_ASYNC ON WITH NO_WAIT
