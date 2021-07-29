CREATE PROC dbo.RunningQueries_Upd(
	@RunningQueries dbo.RunningQueries READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='RunningQueries'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
    /* Get exact time snapshot was run from table which might be slightly different from input param */
    SELECT TOP(1) @SnapshotDate = SnapshotDateUTC 
    FROM @RunningQueries

    BEGIN TRAN
    INSERT INTO dbo.RunningQueriesSummary
    (
        InstanceID,
        SnapshotDateUTC,
        RunningQueries,
        BlockedQueries,
        BlockedQueriesWaitMs,
        MaxMemoryGrant,
        LongestRunningQueryMs,
        CriticalWaitCount,
        CriticalWaitTime
    )
    SELECT @InstanceID as InstanceID,
            R.SnapshotDateUTC,
		    COUNT(*) AS RunningQueries,
		    SUM(CASE WHEN R.blocking_session_id>0 THEN 1 ELSE 0 END) AS BlockedQueries,
		    ISNULL(SUM(CASE WHEN R.blocking_session_id>0 THEN CAST(R.wait_time AS BIGINT) ELSE 0 END),0) AS BlockedWaitTime,
		    ISNULL(MAX(R.granted_query_memory),0) AS MaxMemoryGrant,
		    ISNULL(MAX(calc.Duration),0) AS LongestRunningQueryMs,
		    SUM(CASE WHEN WT.IsCriticalWait=1 THEN 1 ELSE 0 END) CriticalWaitCount,
		    SUM(CASE WHEN WT.IsCriticalWait=1 THEN CAST(ISNULL(R.wait_time,0) AS BIGINT) ELSE 0 END) CriticalWaitTime
    FROM @RunningQueries R 
    CROSS APPLY(SELECT DATEDIFF_BIG(ms,ISNULL(start_time_utc,last_request_start_time_utc),R.SnapshotDateUTC) AS Duration) calc
    LEFT JOIN dbo.WaitType WT ON R.wait_type  = WT.WaitType
    GROUP BY R.SnapshotDateUTC

    IF @@ROWCOUNT=0
	BEGIN
	    INSERT INTO dbo.RunningQueriesSummary
		(
			InstanceID,
			SnapshotDateUTC,
			RunningQueries,
			BlockedQueries,
			BlockedQueriesWaitMs,
			MaxMemoryGrant,
			LongestRunningQueryMs,
			CriticalWaitCount,
			CriticalWaitTime
		)
		VALUES(@InstanceID,@SnapshotDate,0,0,0,0,0,0,0)
	END
    /* Running Queries replaces legacy blocking snapshot collection */
    INSERT INTO dbo.BlockingSnapshotSummary(
        InstanceID,
        SnapshotDateUTC,
        BlockedSessionCount,
        BlockedWaitTime,
        UTCOffset
    )
    SELECT RQS.InstanceID,
	    RQS.SnapshotDateUTC,
	    RQS.BlockedQueries,
	    RQS.BlockedQueriesWaitMs,
	    I.UTCOffset
    FROM dbo.RunningQueriesSummary RQS 
    JOIN dbo.Instances I ON I.InstanceID = RQS.InstanceID
    WHERE RQS.InstanceID = @InstanceID
    AND RQS.SnapshotDateUTC = @SnapshotDate
    AND RQS.BlockedQueries > 0

	INSERT INTO dbo.RunningQueries
    (
        InstanceID,
        SnapshotDateUTC,
        session_id,
        statement_start_offset,
        statement_end_offset,
        command,
        status,
        wait_time,
        wait_type,
        wait_resource,
        blocking_session_id,
        cpu_time,
        logical_reads,
        reads,
        writes,
        granted_query_memory,
        percent_complete,
        open_transaction_count,
        transaction_isolation_level,
        login_name,
        host_name,
        database_id,
        program_name,
        client_interface_name,
        start_time_utc,
        last_request_start_time_utc,
        sql_handle,
        plan_handle,
        query_hash,
        query_plan_hash
    )
    SELECT @InstanceID as InstanceID,
        SnapshotDateUTC,
        session_id,
        statement_start_offset,
        statement_end_offset,
        command,
        status,
        wait_time,
        wait_type,
        wait_resource,
        blocking_session_id,
        cpu_time,
        logical_reads,
        reads,
        writes,
        granted_query_memory,
        percent_complete,
        open_transaction_count,
        transaction_isolation_level,
        login_name,
        host_name,
        database_id,
        program_name,
        client_interface_name,
        start_time_utc,
        last_request_start_time_utc,
        sql_handle,
        plan_handle,
        query_hash,
        query_plan_hash
    FROM @RunningQueries;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate;
    COMMIT
END