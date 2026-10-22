using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Gets the live query plan (actual row counts etc. so far) for a request that is still running, from
    /// sys.dm_exec_query_statistics_xml.  The plan is only returned if the same request is still running on the session -
    /// the request start time must match the snapshot the user is looking at, so a reused SPID doesn't return a plan for a
    /// different query.<br/>
    /// Needs lightweight query profiling: on by default from SQL 2019, trace flag 7412 on SQL 2016 SP1 and 2017.  Without it
    /// a plan is only available if the query was started with SET STATISTICS XML/PROFILE ON or an XE session capturing
    /// query_post_execution_showplan was running.<br/>
    /// The plan isn't stored in the repository - it's a point in time view of a request in progress.
    /// </summary>
    public class LivePlanMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public int SessionID { get; set; }

        /// <summary>The request start time (UTC) from the snapshot.  Compared with the live value to confirm it's the same request.</summary>
        public DateTime StartTimeUtc { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            using var op = Operation.Begin(
                "Get live plan for session {session} on {instance} triggered from message {id} with handle {handle}",
                SessionID,
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(LivePlanSql, cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = SessionID;
                cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value = StartTimeUtc;

                await cn.OpenAsync(cancellationToken);
                var ds = new DataSet();
                var dt = new DataTable("LivePlan");
                ds.Tables.Add(dt);
                await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    dt.Load(reader);
                }

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting live plan for session {session} on {instance} from message {id} with handle {handle}",
                    SessionID, ConnectionID, Id, handle);
                throw;
            }
        }

        /// <summary>
        /// Checks the same request is still running then returns its live plan, and which statement it is for.
        ///
        /// The DMVs can't be read together atomically: the request could finish (and another start on the session)
        /// between checking it and taking the plan, or move on to its next statement between taking the plan and reading
        /// which statement is running.  A plan put down to the wrong statement would have the next statement's counters
        /// written into it on refresh.  So the request - start time, statement offsets and plan hash - is read before and
        /// after the plan is taken.  If the statement changed in between, the plan can't be put down to either, and the
        /// user is asked to try again.  One that has finished by the second read keeps the plan: it was taken from the
        /// request checked, and there are no more statements to confuse it with.
        ///
        /// The DMV doesn't exist before SQL 2016 SP1 so it's queried with sp_executesql to allow the batch to compile on
        /// older versions.  The live start time is converted to UTC using the same whole-minute offset the collector uses.
        /// </summary>
        private const string LivePlanSql = @"
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
DECLARE @msg NVARCHAR(400), @QueryPlan NVARCHAR(MAX);
DECLARE @startUtc DATETIME, @StartOffset INT, @EndOffset INT, @PlanHash BINARY(8), @CpuTime INT, @ElapsedTime INT;
DECLARE @startUtc2 DATETIME, @StartOffset2 INT, @EndOffset2 INT, @PlanHash2 BINARY(8);

IF OBJECT_ID('sys.dm_exec_query_statistics_xml') IS NULL
BEGIN
    THROW 50000, N'Live query plans require SQL Server 2016 SP1 or later.', 1;
END

SELECT TOP(1) @startUtc = DATEADD(mi,@UTCOffset,r.start_time),
        @StartOffset = r.statement_start_offset,
        @EndOffset = r.statement_end_offset,
        @PlanHash = r.query_plan_hash,
        @CpuTime = r.cpu_time,
        @ElapsedTime = r.total_elapsed_time
FROM sys.dm_exec_requests r
WHERE r.session_id = @SessionID
ORDER BY r.request_id;

IF @startUtc IS NULL
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running a request.  The query may have completed.');
    THROW 50000, @msg, 1;
END
/* Exact, apart from a clock change: the snapshot's start time and this one are both made UTC with the whole-minute
   offset at the time, so a daylight saving change since the snapshot moves this one by exactly that many minutes */
IF DATEDIFF(ms, @ExpectedStartUtc, @startUtc) % 60000 <> 0 OR ABS(DATEDIFF(ms, @ExpectedStartUtc, @startUtc)) > 3600000
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running the same request (the request start time has changed).  The query may have completed.');
    THROW 50000, @msg, 1;
END

EXEC sp_executesql N'SELECT TOP(1) @QueryPlan = CAST(query_plan AS NVARCHAR(MAX)) FROM sys.dm_exec_query_statistics_xml(@SessionID)',
    N'@SessionID INT, @QueryPlan NVARCHAR(MAX) OUTPUT',
    @SessionID = @SessionID, @QueryPlan = @QueryPlan OUTPUT;

SELECT TOP(1) @startUtc2 = DATEADD(mi,@UTCOffset,r.start_time),
        @StartOffset2 = r.statement_start_offset,
        @EndOffset2 = r.statement_end_offset,
        @PlanHash2 = r.query_plan_hash
FROM sys.dm_exec_requests r
WHERE r.session_id = @SessionID
ORDER BY r.request_id;

/* Still running and on a different statement: the plan could be for either */
IF @startUtc2 IS NOT NULL
    AND NOT (@startUtc2 = @startUtc
        AND @StartOffset2 = @StartOffset
        AND @EndOffset2 = @EndOffset
        AND (@PlanHash2 = @PlanHash OR (@PlanHash2 IS NULL AND @PlanHash IS NULL)))
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' moved on to another statement while its live plan was being captured.  Try again.');
    THROW 50000, @msg, 1;
END

IF @QueryPlan IS NULL
BEGIN
    SET @msg = CONCAT(N'A live plan is not available for session ', @SessionID, N'.  Some statements have no plan to show - WAITFOR, BACKUP, DBCC and the like.  Otherwise lightweight query profiling is required - it''s on by default from SQL 2019 (LIGHTWEIGHT_QUERY_PROFILING database scoped configuration) and needs trace flag 7412 on SQL 2016 SP1 and 2017.  The query might also have completed.');
    THROW 50000, @msg, 1;
END

/* The statement the plan is for, as read either side of taking it, so a live refresh can tell when the request has moved on to another one */
SELECT @SessionID AS session_id,
    @startUtc AS start_time_utc,
    SYSUTCDATETIME() AS captured_utc,
    @StartOffset AS statement_start_offset,
    @EndOffset AS statement_end_offset,
    @PlanHash AS query_plan_hash,
    @CpuTime AS cpu_time,
    @ElapsedTime AS total_elapsed_time,
    @QueryPlan AS query_plan;";
    }
}
