using DBADash.XE;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Reads the <b>current contents</b> of an existing XE session's target and returns it as a single batch - a
    /// one-shot "view what's already been captured", as opposed to <see cref="XEWatchSessionMessage"/> which tails
    /// only new events from now.
    ///
    /// <para><b>Non-destructive</b> and read-only.  When more than <see cref="MaxEvents"/> were captured the
    /// <b>newest</b> are returned (a <c>TOP</c> guard so a large file can't return an unbounded resultset): event_file
    /// (preferred) via <see cref="EventFileTraceReader"/> ordered newest-first; ring_buffer once via
    /// <see cref="WatchRingBufferReader"/> with no flush, keeping the newest slice.
    /// Only these two targets carry a readable event stream - histogram / pair_matching / etc. are rejected with a
    /// clear message.  Gated on <see cref="CollectionConfig.AllowWatchXE"/> and the per-session watch list (viewing
    /// captured data is the same read sensitivity as watching).</para>
    /// </summary>
    public class XEViewTargetDataMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string SessionName { get; set; }

        /// <summary>Caps the events returned to the newest N within the range (0 = uncapped).</summary>
        public int MaxEvents { get; set; } = DefaultMaxEvents;

        /// <summary>
        /// Optional inclusive lower bound on event time (UTC).  Null = no lower bound (read the whole target).  The
        /// read is always newest-anchored - it returns the newest events at or after this bound (there is no upper
        /// bound), which is what lets every read path serve the range consistently.
        /// </summary>
        public DateTime? StartUtc { get; set; }

        // A high backstop, not a value users are expected to tune: the default 1-day range keeps the row count small,
        // so the cap only bites on "All" / wide ranges on busy sessions, where it bounds the reply payload.
        public const int DefaultMaxEvents = 50000;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!cfg.AllowWatchXE)
            {
                throw new Exception(
                    "Viewing extended events is not enabled on the DBA Dash service.  Enable Watch XE in the service configuration tool.");
            }
            if (string.IsNullOrWhiteSpace(SessionName))
            {
                throw new Exception("No session name was supplied.");
            }
            if (!cfg.CanWatchXESession(SessionName))
            {
                throw new Exception(
                    $"Viewing the extended-events session '{SessionName}' is not permitted by the DBA Dash service's " +
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

            var targets = await XESessionTargetResolver.GetSessionTargetsAsync(connectionString, databaseScoped,
                SessionName, cancellationToken);
            if (targets.Count == 0)
            {
                throw new Exception(
                    $"Session '{SessionName}' is not running on {ConnectionID}.  Start it to view its captured data.");
            }

            using var op = Operation.Begin(
                "View XE target data for {session} on {instance} from message {id} with handle {handle}",
                SessionName, ConnectionID, Id, handle);

            var maxEvents = MaxEvents > 0 ? MaxEvents : 0;
            // Read one extra so we can tell "there are more than the cap" from "exactly the cap" and report it.
            var readCap = maxEvents > 0 ? maxEvents + 1 : 0;

            DataTable events;
            string chosenTarget;
            string readDiag;
            var readSw = Stopwatch.StartNew();
            // Prefer event_file (ordered, lossless) over ring_buffer, matching the watch.
            if (targets.TryGetValue("event_file", out var eventFileData))
            {
                chosenTarget = "event_file";
                (events, readDiag) = await ReadEventFileAsync(connectionString, eventFileData, readCap, cancellationToken);
            }
            else if (targets.ContainsKey("ring_buffer"))
            {
                chosenTarget = "ring_buffer";
                // Whole current buffer, once, without flushing (non-destructive).  An empty diff => every event present.
                var rb = new WatchRingBufferReader(connectionString, SessionName, databaseScoped);
                events = await rb.ReadNextAsync(cancellationToken) ?? new DataTable("XE");
                readDiag = $"db read {rb.LastReadMilliseconds}ms, shred {rb.LastShredMilliseconds}ms";
            }
            else
            {
                throw new Exception(
                    $"Session '{SessionName}' has no readable target data.  Viewing needs an event_file or ring_buffer " +
                    $"target (found: {string.Join(", ", targets.Keys)}).");
            }
            readSw.Stop();

            // Restrict to the requested time window (UTC timestamps).  The read is newest-anchored, so the window is a
            // lower bound only: drop anything older than StartUtc.  Because the readers return the NEWEST events, and
            // the window ends at "now", the newest slice always contains the whole [StartUtc, now] range - so this
            // lower-bound filter is correct for every path (the ring_buffer / TVF-fallback paths don't pre-filter,
            // while the event_file/XELite fast path already applied it, making this a no-op there).
            if (StartUtc.HasValue && events.Columns.Contains("timestamp") &&
                events.Columns["timestamp"].DataType == typeof(DateTime))
            {
                for (var i = events.Rows.Count - 1; i >= 0; i--)
                {
                    var v = events.Rows[i]["timestamp"];
                    if (v == DBNull.Value) continue;
                    if ((DateTime)v < StartUtc.Value) events.Rows.RemoveAt(i); // UTC
                }
            }

            // Both paths return events chronologically (oldest -> newest).  When more than the cap were captured,
            // keep the NEWEST maxEvents by dropping the oldest (front) rows - more useful than the oldest slice.
            var capped = false;
            if (maxEvents > 0 && events.Rows.Count > maxEvents)
            {
                var remove = events.Rows.Count - maxEvents;
                for (var i = 0; i < remove; i++) events.Rows.RemoveAt(0);
                capped = true;
            }

            // Diagnostics: the read stage timing (per-path) so we can see where the time goes vs the transport/bind
            // (timed on the client).  Serialize cost is logged in ResponseMessage.Serialize.
            Log.Information(
                "View XE data {session} on {instance}: {rows} rows, {cols} cols, target {target} - {readDiag} " +
                "(total read {totalMs}ms)",
                SessionName, ConnectionID, events.Rows.Count, events.Columns.Count, chosenTarget, readDiag,
                readSw.ElapsedMilliseconds);

            op.Complete();
            return BuildResult(events, chosenTarget, capped, maxEvents);
        }

        /// <summary>
        /// Reads the newest events from the event_file target.  Fast path: native binary read via
        /// <see cref="XELiteEventFileReader"/> (skips <c>fn_xe_file_target_read_file</c>'s per-event XML conversion).
        /// Falls back to <see cref="EventFileTraceReader"/> (the TVF, newest-first) on any failure - missing
        /// <c>ADMINISTER BULK OPERATIONS</c>, older SQL without <c>sys.dm_os_enumerate_filesystem</c>, a locked file, etc.
        /// Returns the events plus a short diagnostics string naming which path ran and its stage timings.
        /// </summary>
        private async Task<(DataTable events, string diag)> ReadEventFileAsync(string connectionString,
            string eventFileData, int readCap, CancellationToken ct)
        {
            var currentFile = XESessionTargetResolver.ResolveEventFileCurrentFile(eventFileData);
            var wildcard = XESessionTargetResolver.ResolveEventFileReadPath(eventFileData);
            if (string.IsNullOrEmpty(currentFile) && string.IsNullOrEmpty(wildcard))
            {
                throw new Exception($"Unable to determine the event_file path for session '{SessionName}'.");
            }

            if (!string.IsNullOrEmpty(currentFile))
            {
                try
                {
                    var xr = new XELiteEventFileReader(connectionString, currentFile, MaxEvents, StartUtc);
                    var events = await xr.ReadNewestAsync(ct);
                    return (events, xr.UsedEventStream
                        ? $"MSxe stream: {xr.BytesRead / 1024}KB read {xr.BytesReadMilliseconds}ms, parse {xr.ParseMilliseconds}ms"
                        : $"hybrid: {xr.XELiteFilesRead} binary file(s) ({xr.BytesRead / 1024}KB, read {xr.BytesReadMilliseconds}ms, " +
                          $"parse {xr.ParseMilliseconds}ms) + {xr.TvfFilesRead} tvf file(s) ({xr.TvfMilliseconds}ms), " +
                          $"enumerate {xr.EnumerateMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex,
                        "XELite fast path failed for {session} on {instance}; falling back to fn_xe_file_target_read_file",
                        SessionName, ConnectionID);
                }
            }

            if (string.IsNullOrEmpty(wildcard))
            {
                throw new Exception($"Unable to determine the event_file path for session '{SessionName}'.");
            }
            var reader = new EventFileTraceReader(connectionString, wildcard, FileTargetCursor.None, readCap,
                newestFirst: true);
            var tvfEvents = await reader.ReadNextAsync(ct) ?? new DataTable("XE");
            return (tvfEvents, $"tvf: db read {reader.LastReadMilliseconds}ms, shred {reader.LastShredMilliseconds}ms");
        }

        private DataSet BuildResult(DataTable events, string targetType, bool capped, int maxEvents)
        {
            events.TableName = "XE";

            var summary = new DataTable("ViewSummary");
            summary.Columns.Add("ConnectionID", typeof(string));
            summary.Columns.Add("SessionName", typeof(string));
            summary.Columns.Add("TargetType", typeof(string));
            summary.Columns.Add("TotalEvents", typeof(int));
            summary.Columns.Add("Capped", typeof(bool));
            summary.Columns.Add("MaxEvents", typeof(int));
            summary.Rows.Add(ConnectionID, SessionName, targetType, events.Rows.Count, capped, maxEvents);

            var ds = new DataSet();
            ds.Tables.Add(events);
            ds.Tables.Add(summary);
            return ds;
        }
    }
}
