using DBADash.XE;
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
    /// Watches an <b>existing</b> extended-events session on a monitored instance and streams its events back to the
    /// GUI in batches (via <see cref="MessageBase.ReportProgressAsync"/>) for up to a capped duration.
    ///
    /// <para><b>Non-destructive</b>: it only READS the session's target (event_file via
    /// <see cref="EventFileTraceReader"/> or ring_buffer via <see cref="WatchRingBufferReader"/>) - it never creates,
    /// drops, starts or stops the session.  Only sessions with an event_file or ring_buffer target can be watched;
    /// other target types have no readable event stream.  Requires <see cref="CollectionConfig.AllowWatchXE"/> and the
    /// per-session watch list.</para>
    /// </summary>
    public class XEWatchSessionMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string SessionName { get; set; }

        public int MaxDurationSeconds { get; set; } = 300;

        public int BatchIntervalSeconds { get; set; } = 5;

        // A watch runs for its whole duration, so it must not hold a shared message thread (like XETraceMessage).
        public override bool RunOutsideConcurrencyLimit => true;

        private const int MinBatchIntervalSeconds = 1;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!cfg.AllowWatchXE)
            {
                throw new Exception(
                    "Watching extended events is not enabled on the DBA Dash service.  Enable Watch XE in the service configuration tool.");
            }
            if (string.IsNullOrWhiteSpace(SessionName))
            {
                throw new Exception("No session name was supplied.");
            }
            if (!cfg.CanWatchXESession(SessionName))
            {
                throw new Exception(
                    $"Watching the extended-events session '{SessionName}' is not permitted by the DBA Dash service's " +
                    "watchable-sessions list.");
            }

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;
            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
            var databaseScoped = info.IsAzureDB;

            // Clamp the watch duration to the ad-hoc cap - a sensible upper bound for a live watch too.
            var cap = cfg.AdhocXEMaxDurationSeconds > 0
                ? cfg.AdhocXEMaxDurationSeconds
                : CollectionConfig.DefaultAdhocXEMaxDurationSeconds;
            var duration = MaxDurationSeconds <= 0 ? cap : Math.Min(MaxDurationSeconds, cap);
            var batchInterval = Math.Max(BatchIntervalSeconds, MinBatchIntervalSeconds);

            var targets = await XESessionTargetResolver.GetSessionTargetsAsync(connectionString, databaseScoped,
                SessionName, cancellationToken);
            if (targets.Count == 0)
            {
                throw new Exception(
                    $"Session '{SessionName}' is not running on {ConnectionID}.  Start it before watching.");
            }

            using var op = Operation.Begin(
                "Watch XE session {session} on {instance} ({duration}s) from message {id} with handle {handle}",
                SessionName, ConnectionID, duration, Id, handle);

            IXETraceReader reader;
            string chosenTarget;
            // Prefer event_file (lossless, offset cursor) over ring_buffer.
            if (targets.TryGetValue("event_file", out var eventFileData))
            {
                var readPath = XESessionTargetResolver.ResolveEventFileReadPath(eventFileData);
                if (string.IsNullOrEmpty(readPath))
                {
                    throw new Exception($"Unable to determine the event_file path for session '{SessionName}'.");
                }
                // Tail from now: seed the cursor past events already in the file so the watch shows only new events.
                var startCursor = await GetFileEndCursorAsync(connectionString, readPath, cancellationToken);
                reader = new EventFileTraceReader(connectionString, readPath, startCursor);
                chosenTarget = "event_file";
            }
            else if (targets.ContainsKey("ring_buffer"))
            {
                reader = new WatchRingBufferReader(connectionString, SessionName, databaseScoped);
                chosenTarget = "ring_buffer";
                // Prime the diff with the current buffer so the watch shows only events added after it starts.
                try { await reader.ReadNextAsync(cancellationToken); }
                catch (OperationCanceledException) { }
            }
            else
            {
                throw new Exception(
                    $"Session '{SessionName}' has no watchable target.  Watching needs an event_file or ring_buffer " +
                    $"target (found: {string.Join(", ", targets.Keys)}).");
            }

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = $"Watching {SessionName} on {ConnectionID} ({chosenTarget}) for up to {duration}s"
            });

            var startUtc = DateTime.UtcNow;
            var deadline = startUtc.AddSeconds(duration);
            var totalEvents = 0;
            var cancelled = false;
            var heartbeatLost = false;
            // Kept alive by GUI heartbeats: if they stop, the watch stops itself rather than polling to the deadline
            // after the client that started it has gone away (a watch is non-destructive, so this only frees the
            // service-side read loop - it never touched the watched session).
            var heartbeatTimeout = TimeSpan.FromSeconds(XETraceHeartbeat.TimeoutSeconds);
            HeartbeatManager.Register(Id);

            try
            {
                while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    var delay = TimeSpan.FromSeconds(batchInterval);
                    if (delay > remaining) delay = remaining;
                    if (delay > TimeSpan.Zero)
                    {
                        try { await Task.Delay(delay, cancellationToken); }
                        catch (OperationCanceledException) { break; }
                    }

                    totalEvents += await ReadAndReportAsync(reader, totalEvents, cancellationToken);

                    // Stop promptly if the session was stopped externally (nothing left to read).
                    if (!cancellationToken.IsCancellationRequested &&
                        !await SessionRunningAsync(connectionString, databaseScoped))
                    {
                        cancelled = true;
                        break;
                    }

                    // Stop if the GUI has gone quiet - the client watching is presumed gone, so keep polling no longer.
                    if (HeartbeatManager.IsExpired(Id, heartbeatTimeout))
                    {
                        heartbeatLost = true;
                        Log.Warning(
                            "Watch of {session} on {instance} stopping: no heartbeat from the client for over {timeout}s",
                            SessionName, ConnectionID, XETraceHeartbeat.TimeoutSeconds);
                        break;
                    }
                }
                cancelled = cancelled || heartbeatLost || cancellationToken.IsCancellationRequested;

                // event_file: a final drain picks up events still buffered at cancel/deadline (uses the same reader/cursor).
                if (chosenTarget == "event_file")
                {
                    try { totalEvents += await ReadAndReportAsync(reader, totalEvents, CancellationToken.None); }
                    catch (Exception ex) { Log.Warning(ex, "Error draining watched event_file on {instance}", ConnectionID); }
                }
            }
            finally
            {
                HeartbeatManager.Remove(Id);
            }

            op.Complete();
            return BuildSummary(chosenTarget, totalEvents, startUtc, DateTime.UtcNow, duration, cancelled, heartbeatLost);
        }

        private async Task<int> ReadAndReportAsync(IXETraceReader reader, int runningTotal, CancellationToken ct)
        {
            DataTable batch;
            try { batch = await reader.ReadNextAsync(ct); }
            catch (OperationCanceledException) { return 0; }

            if (batch == null || batch.Rows.Count == 0) return 0;

            var ds = new DataSet();
            ds.Tables.Add(batch);
            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = $"Captured {runningTotal + batch.Rows.Count} events",
                Data = ds
            });
            return batch.Rows.Count;
        }

        /// <summary>End-of-file cursor so the watch resumes reading only new events (tails from now).</summary>
        private async Task<FileTargetCursor> GetFileEndCursorAsync(string connectionString, string readPath,
            CancellationToken ct)
        {
            const string sql = @"
BEGIN TRY
    SELECT TOP (1) file_name, file_offset, COUNT(*) OVER (PARTITION BY file_name, file_offset) AS cnt
    FROM sys.fn_xe_file_target_read_file(@path, NULL, NULL, NULL)
    ORDER BY file_name DESC, file_offset DESC;
END TRY
BEGIN CATCH
    /* No readable file yet - return no rows so we read from the start. */
END CATCH";
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
                cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = readPath;
                await cn.OpenAsync(ct);
                await using var rdr = await cmd.ExecuteReaderAsync(ct);
                if (await rdr.ReadAsync(ct))
                {
                    return new FileTargetCursor(rdr.GetString(0), rdr.GetInt64(1), rdr.GetInt32(2));
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to determine event_file start cursor for watch on {instance}", ConnectionID);
            }
            return FileTargetCursor.None;
        }

        private async Task<bool> SessionRunningAsync(string connectionString, bool databaseScoped)
        {
            try
            {
                var view = databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {view} WHERE name = @name", cn);
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = SessionName;
                await cn.OpenAsync();
                return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
            }
            catch { return true; } // transient error - assume still running, don't stop the watch
        }

        private DataSet BuildSummary(string targetType, int totalEvents, DateTime startUtc, DateTime endUtc,
            int duration, bool cancelled, bool heartbeatLost)
        {
            var dt = new DataTable("WatchSummary");
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("SessionName", typeof(string));
            dt.Columns.Add("TargetType", typeof(string));
            dt.Columns.Add("TotalEvents", typeof(int));
            dt.Columns.Add("StartTimeUtc", typeof(DateTime));
            dt.Columns.Add("EndTimeUtc", typeof(DateTime));
            dt.Columns.Add("DurationSeconds", typeof(int));
            dt.Columns.Add("Cancelled", typeof(bool));
            dt.Columns.Add("HeartbeatLost", typeof(bool));
            dt.Rows.Add(ConnectionID, SessionName, targetType, totalEvents, startUtc, endUtc, duration, cancelled,
                heartbeatLost);
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
