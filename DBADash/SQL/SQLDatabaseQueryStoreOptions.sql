SET NOCOUNT ON
DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #querystore (
	database_id INT NOT NULL PRIMARY KEY,
    [desired_state] SMALLINT NOT NULL,
    [actual_state] SMALLINT NOT NULL,
    [readonly_reason] INT  NOT NULL,
    [current_storage_size_mb] BIGINT NOT NULL,
    [flush_interval_seconds] BIGINT NOT NULL,
    [interval_length_minutes] BIGINT NOT NULL,
    [max_storage_size_mb] BIGINT NOT NULL,
    [stale_query_threshold_days] BIGINT NOT NULL,
    [max_plans_per_query] BIGINT NOT NULL,
    [query_capture_mode] SMALLINT NOT NULL,
    [size_based_cleanup_mode] SMALLINT NOT NULL,
    [actual_state_additional_info] NVARCHAR(4000) NOT NULL,
    [wait_stats_capture_mode] SMALLINT NULL,
    [capture_policy_execution_count] INT NULL,
    [capture_policy_total_compile_cpu_time_ms] BIGINT NULL,
    [capture_policy_total_execution_cpu_time_ms] BIGINT NULL,
    [capture_policy_stale_threshold_hours] INT NULL
);

DECLARE @QueryStoreDMV NVARCHAR(MAX)
SET @QueryStoreDMV = 'SELECT DB_ID() AS database_id,
	   desired_state,
       actual_state,
       readonly_reason,
       current_storage_size_mb,
       flush_interval_seconds,
       interval_length_minutes,
       max_storage_size_mb,
       stale_query_threshold_days,
       max_plans_per_query,
       query_capture_mode,
       size_based_cleanup_mode,
       actual_state_additional_info,
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_query_store_options'),'wait_stats_capture_mode','ColumnID') IS NULL THEN 'NULL,' ELSE 'wait_stats_capture_mode,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_query_store_options'),'capture_policy_execution_count','ColumnID') IS NULL THEN 'NULL,' ELSE 'capture_policy_execution_count,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_query_store_options'),'capture_policy_total_compile_cpu_time_ms','ColumnID') IS NULL THEN 'NULL,' ELSE 'capture_policy_total_compile_cpu_time_ms,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_query_store_options'),'capture_policy_total_execution_cpu_time_ms','ColumnID') IS NULL THEN 'NULL,' ELSE 'capture_policy_total_execution_cpu_time_ms,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_query_store_options'),'capture_policy_stale_threshold_hours','ColumnID') IS NULL THEN 'NULL' ELSE 'capture_policy_stale_threshold_hours' END + '
FROM sys.database_query_store_options'

DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
SELECT name
FROM sys.databases
WHERE state  = 0
AND HAS_DBACCESS(name)=1

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN

	SET @SQL =  N'USE ' + QUOTENAME(@DBName)  + '
	' + @QueryStoreDMV


	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		INSERT INTO #querystore
		(
		    database_id,
		    desired_state,
		    actual_state,
		    readonly_reason,
		    current_storage_size_mb,
		    flush_interval_seconds,
		    interval_length_minutes,
		    max_storage_size_mb,
		    stale_query_threshold_days,
		    max_plans_per_query,
		    query_capture_mode,
		    size_based_cleanup_mode,
		    actual_state_additional_info,
		    wait_stats_capture_mode,
		    capture_policy_execution_count,
		    capture_policy_total_compile_cpu_time_ms,
		    capture_policy_total_execution_cpu_time_ms,
		    capture_policy_stale_threshold_hours
		)
		EXEC sp_executesql  @SQL
	END

	FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

SELECT database_id,
       desired_state,
       actual_state,
       readonly_reason,
       current_storage_size_mb,
       flush_interval_seconds,
       interval_length_minutes,
       max_storage_size_mb,
       stale_query_threshold_days,
       max_plans_per_query,
       query_capture_mode,
       size_based_cleanup_mode,
       actual_state_additional_info,
       wait_stats_capture_mode,
       capture_policy_execution_count,
       capture_policy_total_compile_cpu_time_ms,
       capture_policy_total_execution_cpu_time_ms,
       capture_policy_stale_threshold_hours 
FROM #querystore

DROP TABLE #querystore




