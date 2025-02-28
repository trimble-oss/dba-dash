CREATE PROC dbo.RunningQueries_Upd(
	@RunningQueries dbo.RunningQueries READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='RunningQueries'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=CAST(@SnapshotDate AS DATETIME2(2)) AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
    /* Get exact time snapshot was run from table which might be slightly different from input param */
    SELECT TOP(1) @SnapshotDate = SnapshotDateUTC 
    FROM @RunningQueries

	DECLARE @RunningQueriesDD dbo.RunningQueries;

	/* 
		Sometimes a duplicate row for the same session id will appear in the results - de-duplicate by session_id before we import the data.
	*/
	WITH deDupe AS (SELECT t.SnapshotDateUTC,
						   t.session_id,
						   t.statement_start_offset,
						   t.statement_end_offset,
						   t.command,
						   t.status,
						   t.wait_time,
						   t.wait_type,
						   t.wait_resource,
						   t.blocking_session_id,
						   t.cpu_time,
						   t.logical_reads,
						   t.reads,
						   t.writes,
						   t.granted_query_memory,
						   t.percent_complete,
						   t.open_transaction_count,
						   t.transaction_isolation_level,
						   t.login_name,
						   t.host_name,
						   t.database_id,
						   t.program_name,
						   t.client_interface_name,
						   t.start_time_utc,
						   t.last_request_start_time_utc,
						   t.sql_handle,
						   t.plan_handle,
						   t.query_hash,
						   t.query_plan_hash,
						   t.login_time_utc,
						   t.last_request_end_time_utc,
						   t.context_info,
						   t.transaction_begin_time_utc,
						   ROW_NUMBER() OVER(PARTITION BY t.session_id ORDER BY t.cpu_time DESC) rnum
					FROM  @RunningQueries t
	)
	INSERT INTO @RunningQueriesDD
	(
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
	    query_plan_hash,
		login_time_utc,
		last_request_end_time_utc,
		context_info,
		transaction_begin_time_utc
	)
	SELECT  SnapshotDateUTC,
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
	    query_plan_hash,
		login_time_utc,
		last_request_end_time_utc,
		context_info,
		transaction_begin_time_utc
	FROM deDupe
	WHERE deDupe.rnum=1

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
        CriticalWaitTime,
        TempDBWaitCount,
        TempDBWaitTimeMs,
		SumMemoryGrant,
		SleepingSessionsCount,
		SleepingSessionsMaxIdleTimeMs,
		OldestTransactionMs
    )
    SELECT	@InstanceID as InstanceID,
            R.SnapshotDateUTC,
		    COUNT(*) AS RunningQueries,
		    SUM(CASE WHEN R.blocking_session_id>0 THEN 1 ELSE 0 END) AS BlockedQueries,
		    ISNULL(SUM(CASE WHEN R.blocking_session_id>0 THEN CAST(R.wait_time AS BIGINT) ELSE 0 END),0) AS BlockedWaitTime,
		    ISNULL(MAX(R.granted_query_memory),0) AS MaxMemoryGrant,
			/*	For longest running query, ignore TdService (threat detection services) for AzureDB.  Ignore sleeping sessions as these don't have a running query. 
				Ignore queries with SP_SERVER_DIAGNOSTICS_SLEEP or XE_LIVE_TARGET_TVF wait types - we want to report on real user queries.
			*/
		    ISNULL(MAX(CASE WHEN R.wait_type IN('SP_SERVER_DIAGNOSTICS_SLEEP','XE_LIVE_TARGET_TVF') OR calc.Duration<0 OR R.status='sleeping' OR R.program_name = 'TdService' THEN 0 ELSE calc.Duration END),0) AS LongestRunningQueryMs,
		    SUM(CASE WHEN WT.IsCriticalWait=1 THEN 1 ELSE 0 END) CriticalWaitCount,
		    SUM(CASE WHEN WT.IsCriticalWait=1 THEN CAST(R.wait_time AS BIGINT) ELSE 0 END) CriticalWaitTime,
            SUM(calc.IsTempDB) as TempDBWaitCount,
            SUM(CASE WHEN calc.IsTempDB=1 THEN CAST(R.wait_time AS BIGINT) ELSE 0 END) AS TempDBWaitTimeMs,
			ISNULL(SUM(R.granted_query_memory),0) AS SumMemoryGrant,
			SUM(CASE WHEN R.open_transaction_count>0 AND R.status='sleeping' THEN 1 ELSE 0 END) AS SleepingSessionsCount,
			MAX(CASE WHEN R.open_transaction_count>0 AND R.status='sleeping' THEN DATEDIFF_BIG(ms,R.last_request_end_time_utc,R.SnapshotDateUTC) ELSE NULL END) AS SleepingSessionsMaxIdleTimeMs,
			MAX(CASE WHEN calc.TransactionDurationMs<0 THEN 0 ELSE calc.TransactionDurationMs END) AS OldestTransactionMs
    FROM @RunningQueriesDD R 
    CROSS APPLY(SELECT DATEDIFF_BIG(ms,ISNULL(start_time_utc,last_request_start_time_utc),R.SnapshotDateUTC) AS Duration,
						DATEDIFF_BIG(ms,R.transaction_begin_time_utc,R.SnapshotDateUTC) AS TransactionDurationMs,
                        CASE WHEN wait_resource LIKE '2:%' 
			                    OR wait_resource LIKE 'PAGE 2:%'
			                    OR wait_resource LIKE 'OBJECT: 2:%'
			                    OR wait_resource LIKE 'RID: 2%'
			                    OR wait_resource LIKE 'RID: 2:%'
			                    THEN 1 ELSE 0 END AS IsTempDB
                ) calc
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
			CriticalWaitTime,
            TempDBWaitCount,
            TempDBWaitTimeMs,
			SumMemoryGrant,
			SleepingSessionsCount,
			SleepingSessionsMaxIdleTimeMs,
			OldestTransactionMs
		)
		VALUES(@InstanceID,@SnapshotDate,0,0,0,0,0,0,0,0,0,0,0,NULL,NULL)
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
        query_plan_hash,
		login_time_utc,
		last_request_end_time_utc,
		context_info,
		transaction_begin_time_utc
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
        query_plan_hash,
		login_time_utc,
		last_request_end_time_utc,
		context_info,
		transaction_begin_time_utc
    FROM @RunningQueriesDD;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate;
    COMMIT
END