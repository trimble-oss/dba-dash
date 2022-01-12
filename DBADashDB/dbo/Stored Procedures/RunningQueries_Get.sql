CREATE PROC dbo.RunningQueries_Get(
	@InstanceID INT,
	@SnapshotDate DATETIME2=NULL OUT, /* If NULL the latest snapshot will be returned */
	@Skip INT=0 /* Option to return the next or previous snapshot. 1= next, -1 = previous, 0 = Snapshot at @SnapshotDate (default) */
)
AS
IF @SnapshotDate IS NULL
BEGIN
    SELECT TOP(1) @SnapshotDate=SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
    ORDER BY SnapshotDateUTC DESC
END
IF @Skip NOT IN(0,1,-1)
BEGIN;
	THROW 50000,'Invalid value for parameter @Skip.  Valid options: 0,1,-1',1;
END
IF @Skip=1
BEGIN
	/* Get the next snapshot */
	SELECT TOP(1) @SnapshotDate=SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
	AND SnapshotDateUTC > @SnapshotDate
    ORDER BY SnapshotDateUTC
END
ELSE IF @Skip=-1
BEGIN
	/* Get the previous snapshot */
	SELECT TOP(1) @SnapshotDate=SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
	AND SnapshotDateUTC < @SnapshotDate
    ORDER BY SnapshotDateUTC DESC
END 

SELECT InstanceID,
       Duration,
       batch_text,
       text,
       query_plan,
       object_id,
       object_name,
       SnapshotDateUTC,
       session_id,
       command,
       status,
       wait_time,
       wait_type,
       TopSessionWaits,
       blocking_session_id,
       BlockingHierarchy,
       BlockCountRecursive, 
	   BlockWaitTimeRecursiveMs,
       BlockWaitTimeRecursive,
	   BlockCount,
       IsRootBlocker,
	   BlockWaitTimeMs,
       BlockWaitTime,
       cpu_time,
       logical_reads,
       reads,
       writes,
       granted_query_memory_kb,
       percent_complete,
       open_transaction_count,
       transaction_isolation_level,
       login_name,
       host_name,
       database_id,
       database_name,
       program_name,
       job_id,
       job_name,
       client_interface_name,
       start_time_utc,
       last_request_start_time_utc,
       sql_handle,
       plan_handle,
       query_hash,
       query_plan_hash,
       [Duration (ms)],
       wait_resource,
       wait_resource_type,
       wait_database_id,
       wait_file_id,
       wait_page_id,
       wait_object_id,
       wait_index_id,
       wait_hobt,
       wait_hash,
       wait_slot,
       wait_is_compile,
       page_type,
       wait_db,
       wait_object,
       wait_file,
       login_time_utc     
FROM dbo.RunningQueriesInfo Q
WHERE Q.SnapshotDateUTC = @SnapshotDate
AND Q.InstanceID = @InstanceID
ORDER BY Q.Duration DESC