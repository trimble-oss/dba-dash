using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// The operator counters of a request that is still running, from sys.dm_exec_query_profiles - what the plan viewer
    /// needs to refresh a live plan it already has (see <see cref="LivePlanMessage"/>) without fetching the whole plan
    /// again.  A few small rows per operator rather than the showplan XML, so it is cheap enough to send every few
    /// seconds.<br/>
    /// Returns a Request table: Status is Running, Completed when the session is no longer running the request with the
    /// expected start time - an expected outcome when following a query, so not an error - or Moving when the request
    /// changed statement while the counters were read, so they couldn't be put down to one statement.  While it is
    /// running the table also identifies the statement, so the GUI can tell the request has moved on to another one and
    /// fetch its plan, and a Profiles table has the counters for each operator and thread.
    /// </summary>
    public class LiveQueryProfileMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public int SessionID { get; set; }

        /// <summary>The request start time (UTC) from the snapshot.  Compared with the live value to confirm it's the same request.</summary>
        public DateTime StartTimeUtc { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            // Not logged as a timed operation: it runs every few seconds while a live plan is open.
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(ProfileSql, cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = SessionID;
                cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value = StartTimeUtc;

                await cn.OpenAsync(cancellationToken);
                var ds = new DataSet();
                await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    var request = new DataTable("Request");
                    request.Load(reader);
                    ds.Tables.Add(request);

                    if (!reader.IsClosed)
                    {
                        var profiles = new DataTable("Profiles");
                        profiles.Load(reader);
                        ds.Tables.Add(profiles);
                    }
                }

                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting live query profile for session {session} on {instance} from message {id} with handle {handle}",
                    SessionID, ConnectionID, Id, handle);
                throw;
            }
        }

        /// <summary>
        /// The counters are read between two reads of the request - start time, statement offsets and plan hash - and only
        /// returned when both agree.  The DMVs can't be read together atomically, and counters taken as the request moved
        /// on to its next statement would be put down to the statement read before them: written into the operators of
        /// the wrong plan.  A request that moved is reported as Moving with no counters, so the GUI skips that refresh
        /// rather than failing - the next one sees the new statement and fetches its plan.  One that has finished by the
        /// second read is Completed.
        ///
        /// actual_read_row_count isn't in every version that has sys.dm_exec_query_profiles, so the counters are selected
        /// with dynamic SQL that includes it where the column exists.  The live start time is converted to UTC using the
        /// same whole-minute offset the collector uses.
        /// </summary>
        private const string ProfileSql = @"
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
DECLARE @Status VARCHAR(20);
DECLARE @startUtc DATETIME, @RequestID INT, @StartOffset INT, @EndOffset INT, @PlanHash BINARY(8), @CpuTime INT, @ElapsedTime INT;
DECLARE @startUtc2 DATETIME, @StartOffset2 INT, @EndOffset2 INT, @PlanHash2 BINARY(8);

CREATE TABLE #Profiles(
    node_id INT,
    thread_id INT,
    row_count BIGINT,
    rewind_count BIGINT,
    rebind_count BIGINT,
    end_of_scan_count BIGINT,
    elapsed_time_ms BIGINT,
    cpu_time_ms BIGINT,
    scan_count BIGINT,
    logical_read_count BIGINT,
    physical_read_count BIGINT,
    read_ahead_count BIGINT,
    lob_logical_read_count BIGINT,
    lob_physical_read_count BIGINT,
    actual_read_row_count BIGINT,
    open_time BIGINT,
    close_time BIGINT
);

DECLARE @SQL NVARCHAR(MAX) = N'
INSERT INTO #Profiles
SELECT  node_id,
        thread_id,
        row_count,
        rewind_count,
        rebind_count,
        end_of_scan_count,
        elapsed_time_ms,
        cpu_time_ms,
        scan_count,
        logical_read_count,
        physical_read_count,
        read_ahead_count,
        lob_logical_read_count,
        lob_physical_read_count,
        ' + CASE WHEN COL_LENGTH('sys.dm_exec_query_profiles','actual_read_row_count') IS NULL THEN N'CAST(NULL AS BIGINT)' ELSE N'actual_read_row_count' END + N' AS actual_read_row_count,
        open_time,
        close_time
FROM sys.dm_exec_query_profiles
WHERE session_id = @SessionID
AND request_id = @RequestID
OPTION(RECOMPILE)';

SELECT  TOP(1) @startUtc = DATEADD(mi,@UTCOffset,r.start_time),
        @RequestID = r.request_id,
        @StartOffset = r.statement_start_offset,
        @EndOffset = r.statement_end_offset,
        @PlanHash = r.query_plan_hash,
        @CpuTime = r.cpu_time,
        @ElapsedTime = r.total_elapsed_time
FROM sys.dm_exec_requests r
WHERE r.session_id = @SessionID
ORDER BY r.request_id;

/* Exact, apart from a clock change - see LivePlanMessage */
IF @startUtc IS NULL
    OR DATEDIFF(ms, @ExpectedStartUtc, @startUtc) % 60000 <> 0
    OR ABS(DATEDIFF(ms, @ExpectedStartUtc, @startUtc)) > 3600000
BEGIN
    SET @Status = 'Completed';
END
ELSE
BEGIN
    EXEC sp_executesql @SQL, N'@SessionID INT, @RequestID INT', @SessionID = @SessionID, @RequestID = @RequestID;

    SELECT  @startUtc2 = DATEADD(mi,@UTCOffset,r.start_time),
            @StartOffset2 = r.statement_start_offset,
            @EndOffset2 = r.statement_end_offset,
            @PlanHash2 = r.query_plan_hash
    FROM sys.dm_exec_requests r
    WHERE r.session_id = @SessionID
    AND r.request_id = @RequestID;

    /* On a different statement by the second read, the counters could be for either */
    SET @Status = CASE WHEN @startUtc2 IS NULL THEN 'Completed'
                    WHEN @startUtc2 = @startUtc
                        AND @StartOffset2 = @StartOffset
                        AND @EndOffset2 = @EndOffset
                        AND (@PlanHash2 = @PlanHash OR (@PlanHash2 IS NULL AND @PlanHash IS NULL)) THEN 'Running'
                    ELSE 'Moving' END;
END

IF @Status <> 'Running'
BEGIN
    SELECT @Status AS Status, SYSUTCDATETIME() AS captured_utc;
    RETURN;
END

SELECT  @Status AS Status,
        SYSUTCDATETIME() AS captured_utc,
        @StartOffset AS statement_start_offset,
        @EndOffset AS statement_end_offset,
        @PlanHash AS query_plan_hash,
        @CpuTime AS cpu_time,
        @ElapsedTime AS total_elapsed_time;

SELECT * FROM #Profiles;";
    }
}
