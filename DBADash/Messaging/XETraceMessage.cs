using AsyncKeyedLock;
using DBADash.XE;
using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Which target the requester would like.  <see cref="Auto"/> lets the service pick the best available - live
    /// streaming everywhere it's supported (on-prem, Managed Instance and, since Microsoft enabled the live event
    /// stream there, Azure SQL Database), falling back to a durable target otherwise.
    /// <see cref="LiveStream"/> is a target-less, real-time trace.
    /// </summary>
    public enum XETraceTargetPreference
    {
        Auto = 0,
        EventFile = 1,
        RingBuffer = 2,
        LiveStream = 3
    }

    /// <summary>
    /// Runs an ad-hoc extended-events trace on a monitored instance and streams the captured events back to the
    /// GUI in batches (via <see cref="MessageBase.ReportProgressAsync"/>), then stops and cleans up.
    ///
    /// <para><b>Security</b>: the request carries only the typed model (<see cref="Events"/>/<see cref="Filters"/>
    /// etc.), never DDL.  The DDL is generated here, on the service, by <see cref="XETraceDefinition"/> and echoed
    /// back in the summary for the client to log.  Requires <see cref="CollectionConfig.AllowAdhocXE"/>.</para>
    ///
    /// <para><b>One trace per instance</b>: enforced by a per-instance in-memory lock around the check-and-create,
    /// backed by the deterministic session name existing on the source (a repo unique index is the third guard,
    /// added when the schema lands).</para>
    /// </summary>
    public class XETraceMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        /// <summary>
        /// The events to capture, each carrying its data columns (from the catalog).  The client resolves the columns
        /// (including for the built-in RPC/Batch/Error shortcuts) so the service applies each data-column filter only to
        /// events that expose it.  See <see cref="XETraceDefinition.Events"/>.
        /// </summary>
        public List<XETraceEventDef> Events { get; set; } = new();

        public List<XEFilter> Filters { get; set; } = new();

        public XETraceTargetPreference RequestedTarget { get; set; } = XETraceTargetPreference.Auto;

        public int MaxDurationSeconds { get; set; } = 300;

        public int BatchIntervalSeconds { get; set; } = 5;

        /// <summary>Minimum severity for error_reported (default 11 drops informational messages).</summary>
        public int ErrorSeverityFloor { get; set; } = 11;

        /// <summary>
        /// Global actions ("global fields") captured on every event.  Defaults to
        /// <see cref="XETraceDefinition.DefaultGlobalActions"/> so an older GUI that doesn't send them still gets the
        /// standard set; a new GUI sends the user's selection (an empty list captures no actions).
        /// </summary>
        public List<XEActionDef> GlobalActions { get; set; } = new(XETraceDefinition.DefaultGlobalActions);

        /// <summary>Per-event customizable-column settings (the <c>SET</c> toggles), keyed by event name.</summary>
        public Dictionary<string, List<XECustomization>> EventCustomizations { get; set; } = new();

        /// <summary>Reattach to an already-running session (e.g. after a service restart) instead of rejecting.</summary>
        public bool Reclaim { get; set; }

        /// <summary>
        /// Capture the native .xel file (event_file target only) and return its bytes for a "Save as .xel" download.
        /// Uses a unique filename so the captured file contains only this trace, and reads the bytes server-side via
        /// OPENROWSET (needs ADMINISTER BULK OPERATIONS on the monitored instance).  Ignored for the ring buffer.
        /// </summary>
        public bool CaptureXel { get; set; }

        // A trace runs for its whole duration (up to minutes), so it must not hold a shared message thread.
        public override bool RunOutsideConcurrencyLimit => true;

        // Deterministic, service-controlled session name.  One per instance, so a fixed name doubles as the lock.
        internal const string SessionName = "DBADash_AdHoc";
        private const string FileStem = "DBADash_AdHoc";
        private const int MinBatchIntervalSeconds = 1;
        private const int MaxXelBytes = 100 * 1024 * 1024; // don't ship an oversized .xel back through the reply

        private byte[] _capturedXel;

        // Server UTC captured just before START; events older than this are leftover data and are dropped.
        private DateTime _eventCutoffUtc = DateTime.MinValue;

        // Serialises the check-and-create per instance so two requests can't both create the session.
        private static readonly AsyncKeyedLocker<string> InstanceLock = new();

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.AllowAdhocXE)
            {
                throw new Exception(
                    "Ad-hoc XE tracing is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            var maxDuration = cfg.AdhocXEMaxDurationSeconds > 0
                ? cfg.AdhocXEMaxDurationSeconds
                : CollectionConfig.DefaultAdhocXEMaxDurationSeconds;
            var duration = MaxDurationSeconds <= 0 ? maxDuration : Math.Min(MaxDurationSeconds, maxDuration);
            var batchInterval = Math.Max(BatchIntervalSeconds, MinBatchIntervalSeconds);

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;

            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
            if (!info.IsXESupported)
            {
                throw new Exception("Extended events are not supported on this instance/edition.");
            }

            var databaseScoped = info.IsAzureDB;
            var scope = databaseScoped ? XESessionScope.Database : XESessionScope.Server;

            var targetType = ResolveTarget(RequestedTarget, info);
            if (targetType == XETraceTargetType.None)
            {
                // Target-less = live streaming.  When .xel capture is requested the live session ALSO gets an
                // event_file target so we stream for the grid and keep a native .xel to read back on stop.
                return await ProcessLiveAsync(cfg, connectionString, databaseScoped, scope, duration, batchInterval,
                    handle, cancellationToken);
            }

            using var op = Operation.Begin(
                "Ad-hoc XE trace on {instance} ({target}, {duration}s) triggered from message {id} with handle {handle}",
                ConnectionID, targetType, duration, Id, handle);

            var definition = BuildDefinition(targetType, scope);
            string createPath = null, readPath = null;
            if (targetType == XETraceTargetType.EventFile)
            {
                // Normally use a fixed filename (so files don't accumulate - DROP doesn't delete them and there's no
                // reliable server-side delete) and skip any leftover data via the start cursor.  For .xel capture use
                // a UNIQUE filename so the captured file contains only this trace's events (worth the leftover file).
                var fileStem = CaptureXel ? $"{FileStem}_{Id:N}" : FileStem;
                (createPath, readPath) = await ResolveEventFilePathAsync(connectionString, cfg.AdhocXEDirectory,
                    fileStem, cancellationToken);
                definition.FileName = createPath;
            }

            var ddl = definition.BuildCreateSessionSql();
            var own = false;
            var cancelled = false;
            var heartbeatLost = false;
            // Every trace is kept alive by GUI heartbeats; if they stop, the trace stops itself (the client is gone).
            var heartbeatTimeout = TimeSpan.FromSeconds(XETraceHeartbeat.TimeoutSeconds);
            var totalEvents = 0;
            var startUtc = DateTime.UtcNow;
            // For event_file with a fixed filename, seed the read cursor past any pre-existing data so a leftover
            // file from a previous trace isn't re-read.  None => read from the start (no prior file).
            var fileStartCursor = FileTargetCursor.None;
            IXETraceReader reader = null;

            try
            {
                using (await InstanceLock.LockAsync(ConnectionID, cancellationToken))
                {
                    var running = await IsSessionRunningAsync(connectionString, databaseScoped, cancellationToken);
                    if (running && !Reclaim)
                    {
                        throw new Exception(
                            $"A trace is already running on this instance ({ConnectionID}).  Only one ad-hoc trace per instance is allowed.");
                    }

                    if (running)
                    {
                        // Reclaim an orphaned session (e.g. after a service restart).  We now own it and will stop it.
                        own = true;
                        Log.Information("Reclaiming existing ad-hoc XE session on {instance}", ConnectionID);
                    }
                    else
                    {
                        await ExecAsync(connectionString, DropIfExistsSql(scope), cancellationToken); // clear a stopped orphan
                        await ExecAsync(connectionString, ddl, cancellationToken);
                        // Capture the end of any leftover file BEFORE starting (the previous session is dropped, so
                        // the file is static now).  The new session's events land after this point; the cursor spans
                        // forward whether SQL appends to the file or rolls a new one.
                        // Unique-name .xel files have no prior data to skip, so only seed the cursor for the
                        // shared fixed-name file.
                        if (targetType == XETraceTargetType.EventFile && !CaptureXel && !string.IsNullOrEmpty(readPath))
                        {
                            fileStartCursor = await GetFileEndCursorAsync(connectionString, readPath, cancellationToken);
                        }
                        // Cutoff (from the source's clock, so it lines up with event timestamps) for dropping any
                        // leftover pre-start events.  A small back-buffer avoids dropping genuine events at t0.
                        _eventCutoffUtc = (await GetServerUtcAsync(connectionString, cancellationToken)).AddSeconds(-2);
                        await ExecAsync(connectionString, StateSql("START", scope), cancellationToken);
                        own = true;
                    }
                }

                // Start monitoring before the loop so an initial beat is on record; the GUI beats every ~30s.
                HeartbeatManager.Register(Id);

                await ReportProgressAsync(new ResponseMessage
                {
                    Type = ResponseMessage.ResponseTypes.Progress,
                    Message = $"Trace running on {ConnectionID} ({targetType}) for up to {duration}s",
                    // Persist the DDL/target now (row still Running) - the completion summary that also carries them can
                    // be lost to the Status guard when Stop force-cancels the row first, or never arrive if abandoned.
                    XETraceStarted = new XETraceStartedInfo { GeneratedDDL = ddl, TargetType = TargetTypeByte(targetType) }
                });

                reader = targetType == XETraceTargetType.EventFile
                    ? new EventFileTraceReader(connectionString, readPath, fileStartCursor)
                    : new RingBufferTraceReader(connectionString, SessionName, databaseScoped);

                var deadline = startUtc.AddSeconds(duration);
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

                    // Stop promptly if the session was dropped externally (a Stop/Cleanup from another process, or
                    // where the cancellation token didn't reach this process).  For event_file the reader reads the
                    // file, so it wouldn't otherwise notice the session is gone and would run to the deadline.
                    if (!cancellationToken.IsCancellationRequested && !await SessionStillRunningAsync(connectionString,
                            databaseScoped))
                    {
                        cancelled = true;
                        break;
                    }

                    // Stop if the GUI has gone quiet - the client that started the trace is presumed gone, so there's
                    // no one watching and no one to stop it.  This is what lets long traces run safely.
                    if (HeartbeatManager.IsExpired(Id, heartbeatTimeout))
                    {
                        heartbeatLost = true;
                        Log.Warning(
                            "Ad-hoc XE trace on {instance} stopping: no heartbeat from the client for over {timeout}s",
                            ConnectionID, XETraceHeartbeat.TimeoutSeconds);
                        break;
                    }
                }

                cancelled = cancelled || heartbeatLost || cancellationToken.IsCancellationRequested;
            }
            finally
            {
                HeartbeatManager.Remove(Id);
                if (own)
                {
                    // event_file: a final drain after STOP picks up events still buffered at cancel/deadline, using
                    // the SAME reader so it continues from its cursor (not re-reading the file).  Ring buffer isn't
                    // drained here - its reader flushes on every read and would restart the session.  Add the drained
                    // count to the running total so the summary (and TotalEvents) includes these final events.
                    totalEvents += await StopReadDrainAndDropAsync(reader, connectionString, scope, targetType, readPath,
                        totalEvents, cancellationToken);
                }
            }

            op.Complete();
            return BuildSummary(targetType, totalEvents, startUtc, DateTime.UtcNow, duration, ddl, cancelled,
                heartbeatLost);
        }

        /// <summary>
        /// Live-streaming trace: create a session, START it, stream its events live via <see cref="XELiveTraceReader"/>
        /// in batches, and DROP it when done.  Normally target-less (no leftover data, no offset cursor - the stream is
        /// inherently "from now").  When <see cref="CaptureXel"/> is set the session ALSO gets an event_file target (a
        /// session can stream live and write to a target at the same time) so a native .xel is available to read back on
        /// stop; a unique filename keeps that file to just this trace.  Stops on the duration cap, cancellation, the
        /// session being dropped externally, or the client's heartbeat going quiet.
        /// </summary>
        private async Task<DataSet> ProcessLiveAsync(CollectionConfig cfg, string connectionString, bool databaseScoped,
            XESessionScope scope, int duration, int batchInterval, Guid handle, CancellationToken cancellationToken)
        {
            using var op = Operation.Begin(
                "Ad-hoc LIVE XE trace on {instance} ({duration}s) triggered from message {id} with handle {handle}",
                ConnectionID, duration, Id, handle);

            // A .xel capture bolts an event_file target onto the live session, but Azure SQL Database can't write one
            // (no local disk).  Live streaming itself works there; the .xel capture doesn't, so reject that combination.
            if (CaptureXel && databaseScoped)
            {
                throw new Exception(
                    "Capturing a .xel file is not supported on Azure SQL Database (the event_file target needs blob storage).  Watch live without .xel capture, or use the ring buffer target.");
            }

            // Capturing a .xel bolts an event_file target onto the live session; without it the session is target-less.
            var targetType = CaptureXel ? XETraceTargetType.EventFile : XETraceTargetType.None;
            var definition = BuildDefinition(targetType, scope);
            string readPath = null;
            if (CaptureXel)
            {
                // Unique filename => the captured file contains only this trace's events (worth the leftover file).
                var (createPath, resolvedReadPath) = await ResolveEventFilePathAsync(connectionString,
                    cfg.AdhocXEDirectory, $"{FileStem}_{Id:N}", cancellationToken);
                definition.FileName = createPath;
                readPath = resolvedReadPath;
            }

            var ddl = definition.BuildCreateSessionSql();
            var own = false;
            var monitor = new LiveMonitor();
            var totalEvents = 0;
            var startUtc = DateTime.UtcNow;
            var heartbeatTimeout = TimeSpan.FromSeconds(XETraceHeartbeat.TimeoutSeconds);

            try
            {
                using (await InstanceLock.LockAsync(ConnectionID, cancellationToken))
                {
                    var running = await IsSessionRunningAsync(connectionString, databaseScoped, cancellationToken);
                    if (running && !Reclaim)
                    {
                        throw new Exception(
                            $"A trace is already running on this instance ({ConnectionID}).  Only one ad-hoc trace per instance is allowed.");
                    }
                    if (running)
                    {
                        own = true; // reclaim an orphan
                        Log.Information("Reclaiming existing ad-hoc XE session on {instance}", ConnectionID);
                    }
                    else
                    {
                        await ExecAsync(connectionString, DropIfExistsSql(scope), cancellationToken);
                        await ExecAsync(connectionString, ddl, cancellationToken);
                        await ExecAsync(connectionString, StateSql("START", scope), cancellationToken);
                        own = true;
                    }
                }

                HeartbeatManager.Register(Id);
                await ReportProgressAsync(new ResponseMessage
                {
                    Type = ResponseMessage.ResponseTypes.Progress,
                    Message = $"Live trace running on {ConnectionID} for up to {duration}s",
                    // A live trace is reported target-less ("Live") - matching the summary - even when a .xel capture
                    // bolts on an event_file target, so persist a null target alongside the DDL.
                    XETraceStarted = new XETraceStartedInfo
                    {
                        GeneratedDDL = ddl,
                        TargetType = TargetTypeByte(XETraceTargetType.None)
                    }
                });

                using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                streamCts.CancelAfter(TimeSpan.FromSeconds(duration));

                // Watch for the session being dropped externally or the client's heartbeat going quiet; either cancels
                // the stream (the live streamer otherwise runs until its connection is cancelled).
                var monitorTask = MonitorLiveAsync(connectionString, databaseScoped, heartbeatTimeout, streamCts, monitor);

                var reader = new XELiveTraceReader(connectionString, SessionName, 500,
                    TimeSpan.FromSeconds(batchInterval));
                try
                {
                    await reader.StreamAsync(async batch =>
                    {
                        if (batch == null || batch.Rows.Count == 0) return;
                        totalEvents += batch.Rows.Count;
                        var ds = new DataSet();
                        ds.Tables.Add(batch);
                        await ReportProgressAsync(new ResponseMessage
                        {
                            Type = ResponseMessage.ResponseTypes.Progress,
                            Message = $"Captured {totalEvents} events",
                            Data = ds
                        });
                    }, streamCts.Token);
                }
                catch (Exception ex)
                {
                    // Any end of the stream is a stop, not a failure: an early Stop drops the session (via Cleanup) which
                    // aborts the streaming query, and an external drop can surface as a SqlException before the token is
                    // observed cancelled.  Swallow it so we fall through to teardown and still return the summary - which
                    // for a .xel-capture trace carries the captured bytes the caller needs for "Save .xel".
                    Log.Debug(ex, "Live ad-hoc XE stream on {instance} ended via exception (treated as stop)", ConnectionID);
                }
                finally
                {
                    streamCts.Cancel(); // stop the monitor
                    try { await monitorTask; } catch { /* monitor stops via cancellation */ }
                }
            }
            finally
            {
                HeartbeatManager.Remove(Id);
                if (own)
                {
                    // For a .xel capture, STOP and let the file flush, then grab its bytes before the DROP (the file has
                    // a unique name so it holds only this trace).  Cleanup must run even when cancelled - use None token.
                    if (CaptureXel && !string.IsNullOrEmpty(readPath))
                    {
                        try
                        {
                            await ExecAsync(connectionString, StateSql("STOP", scope), CancellationToken.None);
                            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None); // let buffered events flush
                            _capturedXel = await ReadXelBytesAsync(connectionString, readPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Error capturing .xel for live ad-hoc XE session on {instance}", ConnectionID);
                        }
                    }
                    try { await ExecAsync(connectionString, DropIfExistsSql(scope), CancellationToken.None); }
                    catch (Exception ex) { Log.Warning(ex, "Error dropping live ad-hoc XE session on {instance}", ConnectionID); }
                }
            }

            var cancelled = monitor.SessionGone || monitor.HeartbeatLost || cancellationToken.IsCancellationRequested;
            op.Complete();
            return BuildSummary(XETraceTargetType.None, totalEvents, startUtc, DateTime.UtcNow, duration, ddl, cancelled,
                monitor.HeartbeatLost);
        }

        private sealed class LiveMonitor
        {
            public bool SessionGone;
            public bool HeartbeatLost;
        }

        private async Task MonitorLiveAsync(string connectionString, bool databaseScoped, TimeSpan heartbeatTimeout,
            CancellationTokenSource streamCts, LiveMonitor monitor)
        {
            try
            {
                while (!streamCts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), streamCts.Token);
                    if (HeartbeatManager.IsExpired(Id, heartbeatTimeout))
                    {
                        monitor.HeartbeatLost = true;
                        Log.Warning("Live ad-hoc XE trace on {instance} stopping: no heartbeat from the client for over {timeout}s",
                            ConnectionID, XETraceHeartbeat.TimeoutSeconds);
                        streamCts.Cancel();
                        return;
                    }
                    if (!await SessionStillRunningAsync(connectionString, databaseScoped))
                    {
                        monitor.SessionGone = true;
                        streamCts.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal stop (duration cap / cancellation / the stream ending).
            }
        }

        private XETraceDefinition BuildDefinition(XETraceTargetType targetType, XESessionScope scope) => new()
        {
            SessionName = SessionName,
            Events = Events ?? new List<XETraceEventDef>(),
            Filters = Filters ?? new List<XEFilter>(),
            TargetType = targetType,
            Scope = scope,
            ErrorSeverityFloor = ErrorSeverityFloor,
            GlobalActions = GlobalActions ?? new List<XEActionDef>(XETraceDefinition.DefaultGlobalActions),
            EventCustomizations = BuildCustomizationMap()
        };

        private IDictionary<string, IList<XECustomization>> BuildCustomizationMap()
        {
            var map = new Dictionary<string, IList<XECustomization>>(StringComparer.OrdinalIgnoreCase);
            if (EventCustomizations != null)
            {
                foreach (var kv in EventCustomizations)
                {
                    map[kv.Key] = kv.Value ?? new List<XECustomization>();
                }
            }
            return map;
        }

        /// <summary>
        /// Chooses the target.  <see cref="XETraceTargetType.None"/> means a target-less <b>live-streaming</b> trace.
        /// Azure SQL Database can't write a local event_file (that target needs blob storage), so event_file is rejected
        /// there; it can stream live and use the ring buffer, so <see cref="XETraceTargetPreference.Auto"/> and
        /// <see cref="XETraceTargetPreference.LiveStream"/> resolve to live streaming just like on-prem.  Off Azure,
        /// Auto and LiveStream both resolve to live streaming too.
        /// </summary>
        private static XETraceTargetType ResolveTarget(XETraceTargetPreference pref, ConnectionInfo info)
        {
            // The preference comes from the client - reject an undefined enum value rather than letting it fall
            // through the switch below to target-less live streaming (which would bypass a requested durable target).
            if (!Enum.IsDefined(typeof(XETraceTargetPreference), pref))
            {
                throw new Exception($"Unsupported target preference '{pref}'.");
            }
            // Azure SQL Database has no local disk for the event_file target; everything else resolves as it does
            // on-prem (live streaming now works on Azure SQL DB via sys.fn_MSxe_read_event_stream).
            if (info.IsAzureDB && pref == XETraceTargetPreference.EventFile)
            {
                throw new Exception(
                    "The event_file target is not supported on Azure SQL Database.  Use live streaming or the ring buffer target.");
            }
            return pref switch
            {
                XETraceTargetPreference.EventFile => XETraceTargetType.EventFile,
                XETraceTargetPreference.RingBuffer => XETraceTargetType.RingBuffer,
                _ => XETraceTargetType.None // Auto + LiveStream -> live streaming (target-less)
            };
        }

        private async Task<int> ReadAndReportAsync(IXETraceReader reader, int runningTotal, CancellationToken ct)
        {
            DataTable batch;
            try { batch = await reader.ReadNextAsync(ct); }
            catch (OperationCanceledException) { return 0; }

            if (batch == null || batch.Rows.Count == 0) return 0;

            // Belt-and-braces guard against events from before the trace started leaking in (e.g. a leftover
            // event_file the offset cursor didn't fully skip): drop anything older than the trace start.
            DropEventsBeforeStart(batch);
            if (batch.Rows.Count == 0) return 0;

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

        /// <summary>Removes events with a timestamp before the trace's start cutoff (older, leftover data).</summary>
        private void DropEventsBeforeStart(DataTable batch)
        {
            if (_eventCutoffUtc == DateTime.MinValue || !batch.Columns.Contains("timestamp")) return;
            for (var i = batch.Rows.Count - 1; i >= 0; i--)
            {
                var ts = batch.Rows[i]["timestamp"];
                if (ts != DBNull.Value && (DateTime)ts < _eventCutoffUtc)
                {
                    batch.Rows.RemoveAt(i);
                }
            }
        }

        /// <summary>Stops, drains and drops the session.  Returns the number of events picked up by the final drain
        /// (0 for the ring buffer, which isn't drained here) so the caller can add them to the running total.</summary>
        private async Task<int> StopReadDrainAndDropAsync(IXETraceReader reader, string connectionString,
            XESessionScope scope, XETraceTargetType targetType, string readPath, int runningTotal, CancellationToken ct)
        {
            var drained = 0;
            // Cleanup must run even when the trace was cancelled, so don't observe the (already-cancelled) token here.
            try
            {
                await ExecAsync(connectionString, StateSql("STOP", scope), CancellationToken.None);

                if (targetType == XETraceTargetType.EventFile && reader != null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None); // let buffered events flush
                    drained = await ReadAndReportAsync(reader, runningTotal, CancellationToken.None);
                }

                // Grab the native .xel bytes now (after STOP has flushed, file still on disk).  The file has a unique
                // name for capture traces, so it contains only this trace.
                if (CaptureXel && targetType == XETraceTargetType.EventFile && !string.IsNullOrEmpty(readPath))
                {
                    _capturedXel = await ReadXelBytesAsync(connectionString, readPath);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error stopping/draining ad-hoc XE session on {instance}", ConnectionID);
            }
            finally
            {
                try { await ExecAsync(connectionString, DropIfExistsSql(scope), CancellationToken.None); }
                catch (Exception ex) { Log.Warning(ex, "Error dropping ad-hoc XE session on {instance}", ConnectionID); }
            }
            return drained;
        }

        /// <summary>Maps a target to the repo TINYINT: 1 = event_file, 2 = ring_buffer, null for a target-less
        /// (live) session - matching the column's semantics and the GUI's own mapping of the summary row.</summary>
        private static byte? TargetTypeByte(XETraceTargetType targetType) =>
            targetType == XETraceTargetType.None ? (byte?)null : (byte)targetType;

        private DataSet BuildSummary(XETraceTargetType targetType, int totalEvents, DateTime startUtc, DateTime endUtc,
            int duration, string ddl, bool cancelled, bool heartbeatLost)
        {
            var dt = new DataTable("TraceSummary");
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("TargetType", typeof(string));
            dt.Columns.Add("TotalEvents", typeof(int));
            dt.Columns.Add("StartTimeUtc", typeof(DateTime));
            dt.Columns.Add("EndTimeUtc", typeof(DateTime));
            dt.Columns.Add("DurationSeconds", typeof(int));
            dt.Columns.Add("Cancelled", typeof(bool));
            dt.Columns.Add("HeartbeatLost", typeof(bool));
            dt.Columns.Add("GeneratedDDL", typeof(string));
            dt.Columns.Add("XelData", typeof(byte[]));
            var targetLabel = targetType == XETraceTargetType.None ? "Live" : targetType.ToString();
            dt.Rows.Add(ConnectionID, targetLabel, totalEvents, startUtc, endUtc, duration, cancelled,
                heartbeatLost, ddl, (object)_capturedXel ?? DBNull.Value);
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        // ---- SQL helpers ------------------------------------------------------------------------

        private static string StateSql(string state, XESessionScope scope) =>
            $"ALTER EVENT SESSION [{SessionName}] {ScopeKeyword(scope)} STATE = {state};";

        private static string DropIfExistsSql(XESessionScope scope)
        {
            var catalog = scope == XESessionScope.Database ? "sys.database_event_sessions" : "sys.server_event_sessions";
            return $"IF EXISTS(SELECT 1 FROM {catalog} WHERE name = N'{SessionName}') " +
                   $"DROP EVENT SESSION [{SessionName}] {ScopeKeyword(scope)};";
        }

        private static string ScopeKeyword(XESessionScope scope) =>
            scope == XESessionScope.Database ? "ON DATABASE" : "ON SERVER";

        private static async Task<bool> IsSessionRunningAsync(string connectionString, bool databaseScoped,
            CancellationToken ct)
        {
            var view = databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {view} WHERE name = @name", cn);
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = SessionName;
            await cn.OpenAsync(ct);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            return count > 0;
        }

        /// <summary>Session-running check for the read loop - swallows transient errors (assume running, don't stop).</summary>
        private static async Task<bool> SessionStillRunningAsync(string connectionString, bool databaseScoped)
        {
            try { return await IsSessionRunningAsync(connectionString, databaseScoped, CancellationToken.None); }
            catch { return true; }
        }

        /// <summary>
        /// Resolves the event_file paths on the monitored server.  Uses the configured directory or, when empty, the
        /// instance's SQL Server LOG directory (which always exists and is engine-writable).  Runs server-side, so no
        /// filesystem access from the DBA Dash host - works against remote instances.  Returns the create path
        /// (single .xel) and the wildcard read path (SQL appends a suffix / rolls the file over).
        /// </summary>
        private static async Task<(string createPath, string readPath)> ResolveEventFilePathAsync(
            string connectionString, string overrideDir, string fileStem, CancellationToken ct)
        {
            const string sql = @"
DECLARE @dir NVARCHAR(512) = NULLIF(@override, N'');
DECLARE @log NVARCHAR(512) = CAST(SERVERPROPERTY('ErrorLogFileName') AS NVARCHAR(512));
DECLARE @sep NCHAR(1) = CASE WHEN @log LIKE N'%/%' THEN N'/' ELSE N'\' END;
IF @dir IS NULL SET @dir = LEFT(@log, LEN(@log) - CHARINDEX(@sep, REVERSE(@log)));
IF RIGHT(@dir, 1) IN (N'\', N'/') SET @dir = LEFT(@dir, LEN(@dir) - 1);
SELECT @dir + @sep + @stem;";

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add("@override", SqlDbType.NVarChar, 512).Value = (object)overrideDir ?? DBNull.Value;
            cmd.Parameters.Add("@stem", SqlDbType.NVarChar, 128).Value = fileStem;
            await cn.OpenAsync(ct);
            var basePath = Convert.ToString(await cmd.ExecuteScalarAsync(ct));
            if (string.IsNullOrEmpty(basePath))
            {
                throw new Exception("Unable to resolve the event_file directory on the monitored instance.");
            }
            return (basePath + ".xel", basePath + "*.xel");
        }

        /// <summary>
        /// Returns a cursor at the end of the events already in the (fixed-name) event_file, so the trace resumes
        /// reading only <b>new</b> events - skipping any leftover data from a previous trace without re-reading it.
        /// One metadata scan (file_name / file_offset only) of the current file; returns <see cref="FileTargetCursor.None"/>
        /// when there is no readable file yet.  The cursor's ConsumedAtOffset is the number of events sharing the last
        /// offset, so all of them are skipped on the first read.
        /// </summary>
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
    /* No readable file yet (e.g. first trace) - return no rows so we read from the start. */
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
                Log.Warning(ex, "Unable to determine event_file start cursor on {instance}; reading from the start",
                    ConnectionID);
            }
            return FileTargetCursor.None;
        }

        /// <summary>
        /// Reads the native .xel bytes for the trace's (unique-name) file server-side via OPENROWSET BULK - no UNC /
        /// filesystem access from the DBA Dash host.  Returns null on any failure (e.g. the service login lacks
        /// ADMINISTER BULK OPERATIONS) or if the file exceeds <see cref="MaxXelBytes"/>.  Captures the first file
        /// (the norm for a duration-capped trace); a rolled-over multi-file capture keeps only the first.
        /// </summary>
        private async Task<byte[]> ReadXelBytesAsync(string connectionString, string readPath)
        {
            const string sql = @"
DECLARE @file NVARCHAR(260);
SELECT TOP (1) @file = file_name
FROM sys.fn_xe_file_target_read_file(@path, NULL, NULL, NULL)
ORDER BY file_name;
IF @file IS NOT NULL
BEGIN
    DECLARE @s NVARCHAR(MAX) =
        N'SELECT BulkColumn FROM OPENROWSET(BULK N''' + REPLACE(@file, '''', '''''') + N''', SINGLE_BLOB) AS x';
    EXEC sp_executesql @s;
END";
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text, CommandTimeout = 120 };
                cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = readPath;
                await cn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value) return null;
                var bytes = (byte[])result;
                if (bytes.Length > MaxXelBytes)
                {
                    Log.Warning("Captured .xel for {instance} is {size} bytes (> {cap} cap) - not returned",
                        ConnectionID, bytes.Length, MaxXelBytes);
                    return null;
                }
                return bytes;
            }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "Unable to capture .xel bytes for {instance} (OPENROWSET may need ADMINISTER BULK OPERATIONS)",
                    ConnectionID);
                return null;
            }
        }

        private static async Task<DateTime> GetServerUtcAsync(string connectionString, CancellationToken ct)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("SELECT SYSUTCDATETIME()", cn) { CommandType = CommandType.Text };
            await cn.OpenAsync(ct);
            return Convert.ToDateTime(await cmd.ExecuteScalarAsync(ct));
        }

        private static async Task ExecAsync(string connectionString, string sql, CancellationToken ct)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            await cn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
