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
    /// Kills a session (SPID) on the source instance.  Guarded on the service side: killing sessions must be
    /// explicitly enabled (<see cref="CollectionConfig.AllowKillSession"/>) and the validation + KILL run as a
    /// single atomic batch on the source instance - the batch only issues KILL if the <b>same</b> SPID is still
    /// running with the same login and request start time (guarding against SPID reuse, i.e. a different query now
    /// running on that SPID), so nothing can change between the check and the KILL.  System sessions
    /// (session_id &lt;= 50 / background tasks) are never killed.
    /// </summary>
    public class KillSessionMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public int SessionID { get; set; }

        /// <summary>Login expected to own the session (from the snapshot the user is acting on).</summary>
        public string ExpectedLoginName { get; set; }

        /// <summary>
        /// The request start time (UTC) from the snapshot - falls back to last_request_start_time for sleeping
        /// sessions.  Compared against the live value on the source instance to confirm it's the same request.
        /// </summary>
        public DateTime? ExpectedStartTimeUtc { get; set; }

        // Sessions at or below this id are system sessions and must never be killed.
        private const int MaxSystemSessionId = 50;

        // Allowed difference (ms) when matching the request start time.  The values should be identical - this only
        // absorbs the datetime rounding (~3.34ms) and timezone round-trip between the stored snapshot value and the
        // live value.  It is not a security knob: the atomic validate+kill batch closes any check-then-act gap.
        private const int StartTimeToleranceMs = 100;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.AllowKillSession)
            {
                throw new Exception("Killing sessions is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            if (SessionID <= MaxSystemSessionId)
            {
                throw new Exception($"Session {SessionID} is a system session and cannot be killed.");
            }

            using var op = Operation.Begin(
                "Kill session {session} on {instance} triggered from message {id} with handle {handle}",
                SessionID,
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(ValidateAndKillSql, cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = SessionID;
                cmd.Parameters.Add("@ExpectedLogin", SqlDbType.NVarChar, 128).Value = (object)ExpectedLoginName ?? DBNull.Value;
                cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value =
                    ExpectedStartTimeUtc.HasValue ? ExpectedStartTimeUtc.Value : (object)DBNull.Value;
                cmd.Parameters.Add("@ToleranceMs", SqlDbType.Int).Value = StartTimeToleranceMs;

                await cn.OpenAsync(cancellationToken);
                var ds = new DataSet();
                var da = new SqlDataAdapter(cmd);
                await using var registration = cancellationToken.Register(() => cmd.Cancel());
                try
                {
                    da.Fill(ds);
                }
                finally
                {
                    registration.Unregister();
                }

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error killing session {session} on {instance} from message {id} with handle {handle}",
                    SessionID, ConnectionID, Id, handle);
                throw;
            }
        }

        /// <summary>
        /// Atomic validate-then-kill batch.  Re-reads the live session, aborts (THROW) if it isn't the same
        /// session/request the user was looking at, and only then issues KILL.  The live request start time is
        /// converted to UTC using the same whole-minute offset the collector uses so it can be compared with the
        /// snapshot value.  KILL can't take a variable, so the (already validated, &gt; system range) SPID is
        /// issued via EXEC.
        /// </summary>
        private const string ValidateAndKillSql = @"
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
DECLARE @login SYSNAME, @isUser BIT, @startUtc DATETIME, @msg NVARCHAR(400);

SELECT  @login = s.login_name,
        @isUser = s.is_user_process,
        @startUtc = DATEADD(mi,@UTCOffset, ISNULL(r.start_time, s.last_request_start_time))
FROM    sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
WHERE   s.session_id = @SessionID;

IF @@ROWCOUNT = 0 OR @login IS NULL
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' no longer exists on the source instance. It may have already completed or been killed.');
    THROW 50000, @msg, 1;
END
IF @isUser = 0
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is a system session and cannot be killed.');
    THROW 50000, @msg, 1;
END
IF @ExpectedLogin IS NULL OR @login <> @ExpectedLogin
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running the same request (login is now ''', @login, N'''). The SPID may have been reused. Kill aborted.');
    THROW 50000, @msg, 1;
END
IF @startUtc IS NULL OR @ExpectedStartUtc IS NULL OR ABS(DATEDIFF(ms, @startUtc, @ExpectedStartUtc)) > @ToleranceMs
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running the same request (the request start time has changed). The SPID may have been reused. Kill aborted.');
    THROW 50000, @msg, 1;
END

DECLARE @kill NVARCHAR(20) = N'KILL ' + CAST(@SessionID AS NVARCHAR(10));
EXEC(@kill);

SELECT @SessionID AS SessionID, CAST(1 AS BIT) AS Killed, CONCAT(N'Session ', @SessionID, N' killed.') AS Message;";
    }
}
