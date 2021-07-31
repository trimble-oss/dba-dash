 
# Data Collection
## Notes
Data collection runs on a schedule by the agent which is listed below. Collections will also run on service start.  If you need to refresh data prior to the scheduled collection, the only only way to do this is to restart the DBA Dash service. 

*The refresh button in the GUI will refresh the data from the repository that has already been collected from the agent.  The agent collects data from your SQL Server instances.  The GUI tool only is just used to report on data from the repository database - it doesn't connect to your SQL instances directly.  The GUI tool is packaged with the agent but can also be deployed separately.*

## Schedule
### Every 1min
- [ObjectExecutionStats](../DBADash/SQL/SQLObjectExecutionStats.sql)
*Captures object execution stats from sys.dm_exec_procedure_stats, sys.dm_exec_function_stats & sys.dm_exec_trigger_stats*
- [CPU](../DBADash/SQL/SQLCPU.sql)
*Capture CPU utilization from sys.dm_os_ring_buffers or sys.dm_db_resource_stats (Azure).*
- [RunningQueries](RunningQueries.md)
*Captures a snapshot of queries currently executing. Captures blocking chains so replaces blocking snapshot. Also captures query text and optionally captures query plans*
- [IOStats](../DBADash/SQL/SQLIOStats.sql)
*Collects data from sys.dm_io_virtual_file_stats*
- [Waits](../DBADash/SQL/SQLWaits.sql)
*Collects data from sys.dm_os_wait_stats*
- [PerformanceCounters](../DBADash/SQL/SQLPerformanceCounters.sql)
*Collects data from sys.dm_os_performance_counters.  Collection can be [customized](OSPerformanceCounters.md), adding additional performance counters or collecting your own metrics with custom SQL.*
- [SlowQueries](../DBADash/SQL/SQLSlowQueries.sql) (Not enabled by default)
*Captures queries that take longer than 1second (or custom) to run using extended events*
- [JobHistory](../DBADash/SQL/SQLJobHistory.sql)
*Collects job execution data from msdb.dbo.sysjobhistory (just what's new since the last collection)*
- [DatabasesHADR](../DBADash/SQL/SQLJobHistory.sql)
*Collects data from dm_hadr_database_replica_states if your SQL instance is using Always On Availability Groups.*
- ~~-[BlockingSnapshot](../DBADash/SQL/SQLBlockingSnapshot.sql)
*Captures a snapshot of any blocking/blocked queries currently running if the total wait time is more than 1second.
Replaced with RunningQueries*~~
#### Azure DB Only:
- [AzureDBElasticPoolResourceStats](../DBADash/SQL/SQLAzureDBElasticPoolResourceStats.sql)
*Collects data from sys.elastic_pool_resource_stats*
- [AzureDBResourceStats](../DBADash/SQL/SQLAzureDBResourceStats.sql)
*Collects data from sys.dm_db_resource_stats*
### Every Hour
- [ServerProperties](../DBADash/SQL/SQLServerProperties.sql)
*Various SERVERPROPERTY() function calls to get server property information.*
- [Databases](../DBADash/SQL/SQLDatabases.sql)
*Collect data from sys.databases*
- [SysConfig](../DBADash/SQL/SQLSysConfig.sql)
*Collect data from sys.configurations*
- [Drives](../DBADash/SQL/SQLDrives.sql) *(When not collected via WMI)*
*Drive collection is done via WMI if possible as this method can collect data from all volumes. The SQL collection method only collects drive capacity and free space for volumes that contain database files.*
- [DBFiles](../DBADash/SQL/SQLDBFiles.sql)
*Collects data from sys.database_files for every database. Uses sys.master_files to collect data for databases that are not accessible.*  
- [Backups](../DBADash/SQL/SQLBackups.sql)
*Get's the last backup of each type for every database from msdb.dbo.backupset*
- [LogRestores](../DBADash/SQL/SQLLogRestores.sql)
*Collects the last log file restored for each database*
- [ServerExtraProperties](../DBADash/SQL/SQLServerExtraProperties.sql)
*Collects server level data from various sources. Some data collections require SysAdmin permissions and xp_cmdshell - these will be skipped if not available.   e.g. Processor name, power plans & more*
- [DBConfig](../DBADash/SQL/SQLDBConfig.sql)
*Collect data from sys.database_scoped_configurations*
- [Corruption](../DBADash/SQL/SQLCorruption.sql)
*Collect data from msdb.dbo.suspect_pages, msdb.sys.dm_db_mirroring_auto_page_repair & msdb.sys.dm_hadr_auto_page_repair*
- [OSInfo](../DBADash/SQL/SQLOSInfo.sql)
*Collect data from sys.dm_os_sys_info*
- [TraceFlags](../DBADash/SQL/SQLTraceFlags.sql)
*Gets trace flags that are enabled globally with DBCC TRACESTATUS(-1)*
- [DBTuningOptions](../DBADash/SQL/SQLDBTuningOptions.sql)
*Returns data from sys.database_automatic_tuning_options for each database*
- [LastGoodCheckDB](../DBADash/SQL/SQLLastGoodCheckDB.sql)
*Note: This collection requires SysAdmin permissions*
- [Alerts](../DBADash/SQL/SQLAlerts.sql)
*Collect data from msdb..sysalerts*
- [CustomChecks](../DBADash/SQL/SQLCustomChecks.sql)
Add [your own](CustomChecks.md) checks to DBA Dash.
- [DatabaseMirroring](../DBADash/SQL/SQLDatabaseMirroring.sql)
*Collect data from sys.database_mirroring*
- [Jobs](../DBADash/SchemaSnapshotDB.cs)
*Collects metadata for SQL Agent jobs including a DDL snapshot using SMO. A lightweight check is run every hour to see if any jobs have been modified since the last collection. If any jobs have been modified, the collection will run.  The lightweight check won't detect some changes like changes to job schedules.  After 24hrs, the collection is run even if no modification to jobs is detected.*  
- [AvailabilityReplicas](../DBADash/SQL/SQLAvailabilityReplicas.sql)
*Collects data from sys.availability_replicas*
- [AvailabilityGroups](../DBADash/SQL/SQLAvailabilityGroups.sql)
*Collects data from sys.availability_groups*
- ~~- [AgentJobs](../DBADash/SQL/AgentJobs.sql)~~
 *Replaced with Jobs/JobHistory*
 
#### Azure DB Only:
- [AzureDBServiceObjectives](../DBADash/SQL/SQLAzureDBServiceObjectives.sql)
*Collects data from sys.database_service_objectives*
- [AzureDBResourceGovernance](../DBADash/SQL/SQLAzureDBResourceGovernance.sql)
*Collects data from sys.dm_user_db_resource_governance*

### Daily @ Midnight
- [ServerPrincipals](../DBADash/SQL/SQLServerPrincipals.sql)
*Collects data from sys.server_principals*
- [ServerRoleMembers](../DBADash/SQL/SQLServerRoleMembers.sql)
*Collects data from sys.server_role_members*
- [ServerPermissions](../DBADash/SQL/SQLServerPermissions.sql)
*Collects data from sys.server_permissions.*
- [DatabasePrincipals](../DBADash/SQL/SQLDatabasePrincipals.sql)
*Collects data from sys.database_principals for each database*
- [DatabaseRoleMembers](../DBADash/SQL/SQLDatabaseRoleMembers.sql)
*Collects data from sys.database_role_members for each database*
- [DatabasePermissions](../DBADash/SQL/SQLDatabasePermissions.sql)
*Collects data from sys.database_permissions for each database*
- [VLF](../DBADash/SQL/SQLVLF.sql)
*Gets the Virtual Log File Count for each database.* 
- [DriversWMI](../DBADash/DBCollector.cs)
*Collects driver information from Win32_PnPSignedDriver via WMI.*  
- [OSLoadedModules](../DBADash/SQL/SQLOSLoadedModules.sql)
*Collects data from sys.dm_os_loaded_modules - can be used to check if antivirus has loaded into SQL Server address space*
- [ResourceGovernorConfiguration](../DBADash/SchemaSnapshotDB.cs)
*Scripts resource governor configuration using SMO*
- [DatabaseQueryStoreOptions](../DBADash/SQL/SQLDatabaseQueryStoreOptions.sql)
*Collects data from sys.database_query_store_options for each database*

### Other
- [Database Schema Snapshots](../DBADash/SchemaSnapshotDB.cs) (Not enabled by default)
*Creates a schema snapshot of databases using SMO.  This isn't enabled by default and you can choose what databases to snapshot and when.  Schema snapshots can be very useful for automatically keeping track of schema changes. It can also be a slow process depending how many objects you have in your database and how many databases you are capturing schema snapshots for.  I would recommend creating daily snapshots and set the schedule to run outside of peak instance usage.*

## Schedule Customization
It's recommended to leave the default schedule but support was added to allow you to customize what data is collected and at what frequency. If you click "Customize Schedule" when adding a connection using the DBA Dash Service Config tool, it will add some extra json to the config that you can customize.  It will look like this:

      "Schedules": [
        {
          "CronSchedule": "0 0 * ? * *",
          "RunOnServiceStart": true,
          "CollectionTypes": [
            "General"
          ]
        },
        {
          "CronSchedule": "0 * * ? * *",
          "RunOnServiceStart": true,
          "CollectionTypes": [
            "Performance"
          ]
        },
        {
          "CronSchedule": "0 0 0 1/1 * ? *",
          "RunOnServiceStart": true,
          "CollectionTypes": [
            "Infrequent"
          ]
        }
      ],
You can generate a custom cron schedule using [cronmaker.com](http://www.cronmaker.com). 

The collection type "General" refers to the hourly collections, "Performance" to the 1min collections and "Infrequent" to the daily collections.  
You can specify the individual collections like this:

      "CollectionTypes": [
        "CPU", "Waits", "IOStats"
      ]
Note:
 - This feature might change in future 
 - You might not automatically get new collection types as you upgrade the application in future.
