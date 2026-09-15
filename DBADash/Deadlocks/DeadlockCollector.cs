using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DBADash.Deadlock;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADash.XE;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.XEvent.XELite;
using Serilog;

namespace DBADash.Deadlocks
{
    /// <summary>
    /// Reads <c>xml_deadlock_report</c> events from an extended events session and shreds them into the three
    /// tables the repository takes.
    ///
    /// <para>The session is named by configuration (see <see cref="DBADashSource.DeadlockXESessionName"/>).
    /// The default is a session DBA Dash creates and starts itself; any other name is read and never
    /// altered, which covers <c>system_health</c> and whatever session the DBA already runs.</para>
    ///
    /// <para>Azure SQL Database is read the same way, one scope down: the session is database scoped, the
    /// event carrying the graph is <c>database_xml_deadlock_report</c> rather than
    /// <c>xml_deadlock_report</c>, and the target is a ring buffer because an event_file there writes to
    /// blob storage.  Both event names are accepted on both paths, so a session holding either is read.</para>
    ///
    /// <para>Read-only throughout.  Neither target is flushed and the session is never stopped or started:
    /// this reads sessions it does not own, including the health session the instance's own owner relies on.
    /// That rules out <see cref="RingBufferTraceReader"/>, whose stop/start flush would discard their data.</para>
    /// </summary>
    public static class DeadlockCollector
    {
        /// <summary>The event carrying a deadlock graph.  Fixed by SQL Server, whatever the session is called.</summary>
        public const string DeadlockEventName = "xml_deadlock_report";

        /// <summary>
        /// The database scoped equivalent, and the only one Azure SQL Database offers: a server scoped
        /// session is not available there, and <see cref="DeadlockEventName"/> is not available to a
        /// database scoped session.  The payload is the same graph.
        /// </summary>
        public const string DatabaseDeadlockEventName = "database_xml_deadlock_report";

        /// <summary>
        /// Both names, which is what every read matches on.  A session only ever raises one of them - the
        /// scope decides which - so matching both costs nothing and means neither path has to know which
        /// scope it is reading.
        /// </summary>
        public static readonly IReadOnlyList<string> DeadlockEventNames =
            new[] { DeadlockEventName, DatabaseDeadlockEventName };

        /// <summary>Rows one read asks for.  The run pages through the file in batches of this size.</summary>
        public const int MaxEventsPerRun = 20000;

        /// <summary>
        /// Seconds every statement this collection sends is given, from the timeout registered for
        /// <see cref="CollectionType.Deadlocks"/> - which the user can raise in commandTimeouts.json like any
        /// other collection's.
        ///
        /// <para>Taken here rather than left to SqlClient's 30 second default because the cost of an
        /// event_file read is opening and seeking the file set rather than the number of new events in it: a
        /// first run against a session with a large file set - system_health especially - can exceed the
        /// default on a single batch, and a read that times out never advances the cursor, so the same batch
        /// times out on every run after it.</para>
        /// </summary>
        private static int CommandTimeout => CollectionType.Deadlocks.GetCommandTimeout();

        /// <summary>
        /// Batches one run will page through before giving up and leaving the rest for the next run.  A
        /// backstop rather than a tuning knob: it bounds a first run against a session with a very large file
        /// set, and the cursor means the next run resumes where this one stopped rather than starting over.
        /// </summary>
        public const int MaxBatchesPerRun = 100;

        /// <summary>The three DataTables of a collection run, in the shape <c>dbo.Deadlocks_Upd</c> takes.</summary>
        public sealed class Result
        {
            public DataTable Deadlocks { get; init; }

            public DataTable Processes { get; init; }

            public DataTable Resources { get; init; }

            public int GraphCount => Deadlocks?.Rows.Count ?? 0;
        }

        /// <param name="manageSession">
        /// The session is one DBA Dash owns, so it may be created, started and emptied.  True only for the
        /// reserved name - see <see cref="DBADashSource.IsDeadlockXESessionManaged"/>.
        /// </param>
        /// <param name="flushRingBuffer">
        /// Empty the ring buffer once its contents have been read.  Ignored unless the session is ours, and
        /// unless the target actually is a ring buffer.  See <see cref="FlushRingBufferAsync"/>.
        /// </param>
        /// <param name="ringBufferKB">
        /// Size of the ring buffer on the session DBA Dash creates.  Only reaches the database scoped
        /// session, which is the only one with a ring buffer - see
        /// <see cref="CollectionConfig.DeadlockXERingBufferKB"/>.
        /// </param>
        public static async Task<Result> CollectAsync(string connectionString, string sessionName,
            bool databaseScoped, DeadlockCollectionState state, CancellationToken cancellationToken,
            bool manageSession = false, bool flushRingBuffer = false,
            int ringBufferKB = CollectionConfig.DefaultDeadlockXERingBufferKB)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                throw new ArgumentException("No extended events session name configured for deadlock collection.",
                    nameof(sessionName));
            }
            ArgumentNullException.ThrowIfNull(state);

            if (manageSession)
            {
                await EnsureManagedSessionAsync(connectionString, sessionName, databaseScoped, ringBufferKB,
                    cancellationToken);
            }

            var targets = await XESessionTargetResolver.GetSessionTargetsAsync(connectionString, databaseScoped,
                sessionName, cancellationToken, CommandTimeout);
            if (targets.Count == 0)
            {
                throw new Exception(manageSession
                    ? $"Extended events session '{sessionName}' could not be started on this instance."
                    : $"Extended events session '{sessionName}' is not running.  Deadlock collection reads the " +
                      "session but never starts it, so start it " +
                      (databaseScoped ? "on the database" : "on the instance") +
                      " or point the collection at a session that is running.");
            }

            // event_file first: it survives a restart, and the cursor makes each run read only what is new.
            var readPath = targets.TryGetValue("event_file", out var fileTargetData)
                ? XESessionTargetResolver.ResolveEventFileReadPath(fileTargetData)
                : null;
            var hasRingBuffer = targets.TryGetValue("ring_buffer", out var ringTargetData);

            if (string.IsNullOrEmpty(readPath) && !hasRingBuffer)
            {
                throw new Exception(
                    $"Extended events session '{sessionName}' has no readable event stream - it has " +
                    $"{string.Join(", ", targets.Keys)}, and only event_file and ring_buffer carry one.  Add one " +
                    "of those targets, or point the collection at a session that has one.");
            }

            if (!string.IsNullOrEmpty(readPath))
            {
                return ShredEvents(await ReadEventFileAsync(connectionString, readPath, state, cancellationToken));
            }

            var captured = SelectUnseenRingBufferDeadlocks(ringTargetData, state);

            // Only a session DBA Dash created may be emptied - stopping someone else's session would throw away
            // data its owner is relying on - and only when it holds something, so an idle database is never
            // stopped and started for nothing.  SeenHashes is everything the read found, new or not, which is
            // what decides whether the next read would be an expensive one.
            if (flushRingBuffer && manageSession && state.SeenHashes.Count > 0)
            {
                await FlushRingBufferAsync(connectionString, sessionName, databaseScoped, state,
                    cancellationToken);
            }
            return Build(captured);
        }

        /// <summary>
        /// How far past the configured session's start the backfill keeps deadlocks.  The two clocks compared - the
        /// session's start and an event's timestamp - are read separately and converted separately, so an exact
        /// cutoff could drop a deadlock that fired as the session was starting.  A minute of overlap costs at most a
        /// deadlock or two sent twice, which the repository discards.
        /// </summary>
        internal static readonly TimeSpan BackfillCutoffMargin = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The deadlocks <paramref name="backfillSessionName"/> holds from before
        /// <paramref name="configuredSessionName"/> started - system_health's history for a dedicated session that
        /// started empty.  Null, having read nothing, when the configured session isn't running: the cutoff can't
        /// be known, and a backfill taken without one could leave a gap the configured session doesn't cover.
        ///
        /// <para>Runs on its own, after the run that first read the configured session, rather than inside that run
        /// - so a read of the whole of system_health waits in a low priority queue instead of holding up the
        /// collection.  The cutoff divides the work between the two: anything from before the configured session
        /// started can only be here, and anything after is the configured session's to collect, so what they both
        /// send is limited to <see cref="BackfillCutoffMargin"/>.  Their imports can run at the same time, and a
        /// deadlock in that overlap arriving in both is discarded by dbo.Deadlocks_Upd, whose dedup check locks the
        /// key so concurrent imports of the same deadlock don't both insert it.</para>
        ///
        /// <para>A session started more than once - an instance restart, say - is cut off at its latest start.
        /// Older deadlocks it holds from before the restart come back from system_health too, and the repository
        /// discards them as already stored.</para>
        /// </summary>
        public static async Task<Result> BackfillAsync(string connectionString, string backfillSessionName,
            string configuredSessionName, bool databaseScoped, int timeLimitSeconds,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backfillSessionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(configuredSessionName);

            // The time limit covers the whole backfill, from the first lookup - not only the read.
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeLimitSeconds > 0) budget.CancelAfter(TimeSpan.FromSeconds(timeLimitSeconds));
            var elapsed = Stopwatch.StartNew();

            DateTime? configuredStart;
            try
            {
                configuredStart = await GetSessionStartUtcAsync(connectionString, configuredSessionName,
                    databaseScoped, timeLimitSeconds, budget.Token);
            }
            catch (Exception) when (budget.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Treated like a read that ran out of time - finished, with nothing to keep - rather than as a
                // failure that leaves the backfill to be tried again.
                Log.Warning("Deadlock backfill from {backfill} stopped at its {limit}s time limit before finding when " +
                            "{session} started; the backfill is not attempted again.", backfillSessionName,
                    timeLimitSeconds, configuredSessionName);
                return Build(Array.Empty<CapturedDeadlock>());
            }

            if (configuredStart == null)
            {
                Log.Information("Deadlock backfill from {backfill} deferred: session {session} is not running, so " +
                                "there is no start time to backfill up to.", backfillSessionName, configuredSessionName);
                return null;
            }

            var cutoff = configuredStart.Value + BackfillCutoffMargin;
            var captured = await ReadBackfillAsync(connectionString, backfillSessionName, databaseScoped,
                timeLimitSeconds, budget, elapsed, cancellationToken);
            var before = captured.Where(c => c.EventTime < cutoff).ToList();

            Log.Information("Deadlock backfill keeping {kept} of {read} deadlock(s) from {backfill}: those before " +
                            "{session} started at {start:u}", before.Count, captured.Count, backfillSessionName,
                configuredSessionName, configuredStart.Value);
            return Build(before);
        }

        /// <summary>
        /// The batch <see cref="GetSessionStartUtcAsync"/> sends.  <c>create_time</c> is the server's local time, so
        /// it is moved to UTC on the server by the server's own offset - the event timestamps it is compared with are
        /// UTC.  Second precision on the offset is enough given <see cref="BackfillCutoffMargin"/>, and keeps to
        /// functions every supported version has.
        /// </summary>
        internal static string BuildSessionStartSql(bool databaseScoped) =>
            "SELECT DATEADD(SECOND, DATEDIFF(SECOND, GETDATE(), GETUTCDATE()), create_time) " +
            $"FROM {(databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions")} " +
            "WHERE name = @name;";

        /// <summary>When the running session last started, in UTC, or null when it isn't running.</summary>
        private static async Task<DateTime?> GetSessionStartUtcAsync(string connectionString, string sessionName,
            bool databaseScoped, int timeLimitSeconds, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(BuildSessionStartSql(databaseScoped), cn)
            { CommandType = CommandType.Text, CommandTimeout = timeLimitSeconds };
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = sessionName;
            await cn.OpenAsync(cancellationToken);
            var value = await cmd.ExecuteScalarAsync(cancellationToken);
            return value is DateTime start ? DateTime.SpecifyKind(start, DateTimeKind.Unspecified) : null;
        }

        /// <summary>
        /// Reads the whole of another session, once, for the deadlocks it already holds - the read behind
        /// <see cref="BackfillAsync"/>.
        ///
        /// <para>Read from the start of its file set, with no cursor at all: it is read once, so there is no
        /// position worth keeping, and the configured session's cursor must never be moved by a read of a
        /// different file set.  Read only, like any session DBA Dash does not own.</para>
        ///
        /// <para>Best effort.  A backfill that can't be read - system_health stopped, or a file set too large to
        /// read in time - is a warning rather than a failure, and what was read before it stopped is kept.  It
        /// still counts as the backfill having run: a read that timed out would most likely time out again, and
        /// repeating a full read of system_health is the cost the dedicated session exists to avoid.  Shutdown is
        /// the exception, and propagates, so a backfill the service stopped is attempted again.</para>
        ///
        /// <para>An event file set is read through <see cref="TryReadDeadlockEventStreamAsync"/> where the instance
        /// allows it, and through <see cref="ReadDeadlockEventsAsync"/> where it doesn't.  Neither can put the newest
        /// first without holding every row back until the end, so a partial read keeps whatever the scan reached -
        /// see <see cref="ReadDeadlockEventsAsync"/>.</para>
        ///
        /// <para>Bounded by <paramref name="budget"/>, the backfill's own time limit rather than the Deadlocks command
        /// timeout: it reads the whole file set once, where the command timeout is sized for the routine read every
        /// run makes.  Every command it sends - finding the target as well as reading it - takes the limit as its
        /// command timeout too, so 0 is no limit throughout.  <paramref name="cancellationToken"/> is the caller's,
        /// and is what tells shutdown, which propagates, apart from the limit.</para>
        /// </summary>
        private static async Task<List<CapturedDeadlock>> ReadBackfillAsync(string connectionString,
            string sessionName, bool databaseScoped, int timeLimitSeconds, CancellationTokenSource budget,
            Stopwatch elapsed, CancellationToken cancellationToken)
        {

            // Both filled as they are read, so whatever was read before a failure is still here to keep.  A deadlock
            // in both - a stream read that failed part way, then the fallback - is the same graph with the same
            // timestamp, which Build de-duplicates.
            var captured = new List<CapturedDeadlock>();
            var eventXml = new List<string>();
            // Named for the read in progress, so a warning says which read the limit or the failure stopped.
            var readMethod = "session target lookup";
            try
            {
                var targets = await XESessionTargetResolver.GetSessionTargetsAsync(connectionString, databaseScoped,
                    sessionName, budget.Token, timeLimitSeconds);

                if (targets.TryGetValue("event_file", out var fileTargetData) &&
                    XESessionTargetResolver.ResolveEventFileReadPath(fileTargetData) is { Length: > 0 } readPath)
                {
                    readMethod = "fn_MSxe_read_event_stream";
                    if (!await TryReadDeadlockEventStreamAsync(connectionString,
                            XESessionTargetResolver.ResolveEventFileCurrentFile(fileTargetData), captured,
                            timeLimitSeconds, budget.Token))
                    {
                        readMethod = "fn_xe_file_target_read_file";
                        await ReadDeadlockEventsAsync(connectionString, readPath, eventXml, timeLimitSeconds,
                            budget.Token);
                    }
                }
                else if (targets.TryGetValue("ring_buffer", out var ringTargetData))
                {
                    var fromRingBuffer = ParseRingBuffer(ringTargetData).ToList();
                    Log.Information("Deadlock backfill read {count} deadlock(s) from session {session}",
                        fromRingBuffer.Count, sessionName);
                    return fromRingBuffer;
                }
                else
                {
                    Log.Warning("Deadlock backfill skipped: extended events session {session} is not running or " +
                                "has no event_file or ring_buffer target to read.", sessionName);
                    return new List<CapturedDeadlock>();
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The time limit lands here too - it is the linked token, not the caller's - so running out of time
                // keeps what was read, where a shutdown still propagates.
                if (budget.IsCancellationRequested)
                {
                    Log.Warning("Deadlock backfill from session {session} stopped at its {limit}s time limit, " +
                                "reading with {method}.  Keeping the {count} deadlock event(s) read before it " +
                                "stopped; the backfill is not attempted again.  Raise DeadlockBackfillTimeLimitSeconds " +
                                "in the service configuration (0 for no limit) if older deadlocks are needed.",
                        sessionName, timeLimitSeconds, readMethod, captured.Count + eventXml.Count);
                }
                else
                {
                    Log.Warning(ex, "Deadlock backfill from session {session} failed after {elapsed:N0}s, reading " +
                                    "with {method}.  Keeping the {count} deadlock event(s) read before it stopped; the " +
                                    "backfill is not attempted again.", sessionName, elapsed.Elapsed.TotalSeconds,
                        readMethod, captured.Count + eventXml.Count);
                }
            }

            captured.AddRange(ParseEvents(eventXml));
            Log.Information("Deadlock backfill read {count} deadlock(s) from session {session} in {elapsed:N0}s using {method}",
                captured.Count, sessionName, elapsed.Elapsed.TotalSeconds, readMethod);
            return captured;
        }

        /// <summary>
        /// Reads every deadlock in an event file set through <c>sys.fn_MSxe_read_event_stream</c> - the call SSMS
        /// uses - adding each to <paramref name="captured"/> as it is parsed.  Returns false, having read what it
        /// could, when this path isn't available, so the caller falls back to <see cref="ReadDeadlockEventsAsync"/>.
        ///
        /// <para>Worth the extra path because it moves the cost of the read.  <c>fn_xe_file_target_read_file</c>
        /// converts every event in the set to XML on the server before any filter applies, and a system_health file
        /// set is hundreds of thousands of events with a handful of deadlocks among them.  The stream returns the
        /// files' raw buffers instead and XELite decodes them here: measured against a 142MB system_health set, 3s
        /// against 8s, and the gap is widest on a busy server, where that conversion competes with the workload.
        /// What it costs is the file set crossing the network, so on a slow link to the instance the gap narrows.
        /// </para>
        ///
        /// <para>The graph and timestamp it produces match the TVF's exactly - the timestamp once truncated to the
        /// millisecond, as <see cref="TryGetEventTime"/> does - so a deadlock read both ways, or read here and by the
        /// configured session, is still recognised as one.</para>
        ///
        /// <para>Each buffer is parsed as it arrives and only the deadlocks are kept, so memory is bounded by a
        /// buffer rather than the file set.  Relies on XELite internals and an undocumented function, which is why
        /// any failure other than the caller's cancellation falls back rather than fails.</para>
        /// </summary>
        private static async Task<bool> TryReadDeadlockEventStreamAsync(string connectionString, string currentFile,
            List<CapturedDeadlock> captured, int timeLimitSeconds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(currentFile)) return false;

            var events = 0;
            try
            {
                var parser = new XELiveStreamShredder.RowParser();

                await using var cn = new SqlConnection(connectionString);
                // The caller's time limit cancels the read; the command timeout matches it as a backstop - see
                // ReadDeadlockEventsAsync.
                await using var cmd = new SqlCommand("SELECT type, data FROM sys.fn_MSxe_read_event_stream(@source, 1)", cn)
                { CommandType = CommandType.Text, CommandTimeout = timeLimitSeconds };
                // 1 reads the session's files rather than its live buffers.  The source is the file name pattern, the
                // way SSMS passes it - see XELiteEventFileReader.BuildEventStreamSource.
                cmd.Parameters.Add("@source", SqlDbType.NVarChar, 256).Value =
                    XELiteEventFileReader.BuildEventStreamSource(currentFile);

                await cn.OpenAsync(cancellationToken);
                await using var registration = cancellationToken.Register(() => cmd.Cancel());
                await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    var type = rdr.IsDBNull(0) ? -1 : Convert.ToInt32(rdr.GetValue(0));
                    if (rdr.IsDBNull(1)) continue;
                    var data = (byte[])rdr.GetValue(1);

                    await parser.ParseRowAsync(type, data, ev =>
                    {
                        events++;
                        if (IsDeadlockEvent(ev.Name)) captured.AddRange(ParseStreamEvent(ev));
                    }, cancellationToken);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Information(ex, "Deadlock backfill could not read {file} with fn_MSxe_read_event_stream after " +
                                    "{events} event(s); falling back to fn_xe_file_target_read_file", currentFile, events);
                return false;
            }

            // A system_health file set is never empty, so nothing decoded means a stream this parser doesn't
            // understand rather than a session with nothing in it.  The TVF is the one to say which.
            if (events > 0) return true;
            Log.Information("Deadlock backfill decoded no events from {file} with fn_MSxe_read_event_stream; falling " +
                            "back to fn_xe_file_target_read_file", currentFile);
            return false;
        }

        /// <summary>A deadlock event decoded by XELite, as <see cref="ParseEvent(XElement)"/> takes one from XML.</summary>
        private static IEnumerable<CapturedDeadlock> ParseStreamEvent(IXEvent ev)
        {
            if (ev.Fields == null || !ev.Fields.TryGetValue("xml_report", out var report) ||
                report is not string { Length: > 0 } graphXml)
            {
                Log.Warning("Skipping a deadlock event at {eventTime} with no xml_report.", ev.Timestamp);
                return Array.Empty<CapturedDeadlock>();
            }

            var eventTime = TruncateToMillisecond(ev.Timestamp.UtcDateTime);
            if (!DeadlockParser.TryParse(graphXml, out var graphs))
            {
                Log.Warning("Skipping a deadlock event at {eventTime} whose graph could not be parsed.", eventTime);
                return Array.Empty<CapturedDeadlock>();
            }

            return graphs.Select(g => new CapturedDeadlock(eventTime, g)).ToList();
        }


        /// <summary>
        /// Creates and starts the session DBA Dash owns, if it isn't already there.  Only ever called for the
        /// reserved name - a session DBA Dash did not create is read and never altered.
        ///
        /// <para>Needs ALTER ANY EVENT SESSION on the instance, which is the same permission the slow query
        /// collection already takes; the error says so when it is missing, since that is the one thing likely
        /// to stop this working.  On Azure SQL Database the session is database scoped instead, and the
        /// permission is ALTER ANY DATABASE EVENT SESSION on the database being monitored.</para>
        /// </summary>
        private static async Task EnsureManagedSessionAsync(string connectionString, string sessionName,
            bool databaseScoped, int ringBufferKB, CancellationToken cancellationToken)
        {
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(
                    SqlStrings.GetSqlString(databaseScoped ? "DeadlockSessionAzure" : "DeadlockSession"), cn)
                { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = sessionName;
                // Only the database scoped script has a ring buffer to size.  The server scoped session
                // writes to an event file, where the resume cursor is what bounds a read.
                if (databaseScoped)
                {
                    cmd.Parameters.Add("@RingBufferKB", SqlDbType.Int).Value = ringBufferKB;
                }
                await cn.OpenAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Could not create or start the extended events session '{sessionName}' used for deadlock " +
                    "collection.  The DBA Dash service account needs " +
                    (databaseScoped
                        ? "ALTER ANY DATABASE EVENT SESSION on this database, or the collection can be pointed " +
                          "at a database scoped session it only reads."
                        : "ALTER ANY EVENT SESSION on this instance, or the collection can be pointed at " +
                          "system_health or another session it only reads."), ex);
            }
        }

        /// <summary>
        /// Empties the ring buffer by stopping and starting the session, so the next read has little to read.
        ///
        /// <para>Worth doing because the cost of a ring buffer read is the size of the buffer rather than the
        /// number of new events in it: a full buffer takes around half a second to return and shred where an
        /// empty one takes around thirty milliseconds, on every collection, for every database.  Nothing
        /// else brings that down - the hash suppression stops a deadlock being <em>stored</em> twice, but the
        /// whole buffer still crosses the wire first.</para>
        ///
        /// <para>What it costs is a window: a deadlock that has fired but is still sitting in the session's
        /// memory buffer, undispatched, is lost when the session stops.  The window is the session's
        /// MAX_DISPATCH_LATENCY, which the managed session sets low for exactly this reason.  That is why
        /// this is off unless asked for, and why it is confined to a session DBA Dash created: a session the
        /// DBA runs is never stopped, whatever this is set to.</para>
        ///
        /// <para>A failed flush is not a failed collection.  The events have already been read and are on
        /// their way to the repository; all that is lost is the saving on the next read, and the seen-hash
        /// set is kept so that read still suppresses what this one returned.</para>
        /// </summary>
        /// <summary>
        /// The batch <see cref="FlushRingBufferAsync"/> sends, taking the session name as <c>@name</c>.
        ///
        /// <para>QUOTENAME escapes the identifier, so the name is safe to inline into a statement that cannot
        /// take it as a variable.  The scope and the states are controlled constants.</para>
        ///
        /// <para>Separated from the execution so its syntax can be checked without an instance to send it
        /// to - see <c>SqlScriptSyntaxTests</c>.</para>
        /// </summary>
        internal static string BuildRingBufferFlushSql(bool databaseScoped)
        {
            var scope = databaseScoped ? "ON DATABASE" : "ON SERVER";
            return
                $"DECLARE @sql NVARCHAR(MAX) = N'ALTER EVENT SESSION ' + QUOTENAME(@name) + N' {scope} STATE = STOP;'" +
                $" + N'ALTER EVENT SESSION ' + QUOTENAME(@name) + N' {scope} STATE = START;';" +
                "EXEC sp_executesql @sql;";
        }

        private static async Task FlushRingBufferAsync(string connectionString, string sessionName,
            bool databaseScoped, DeadlockCollectionState state, CancellationToken cancellationToken)
        {
            var sql = BuildRingBufferFlushSql(databaseScoped);

            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(sql, cn)
                { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = sessionName;
                await cn.OpenAsync(cancellationToken);
                await using var registration = cancellationToken.Register(() => cmd.Cancel());
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // The buffer is empty, so there is nothing left for the next read to repeat.  Cleared only on
                // success: a set kept after a failed flush is what stops the next read resending everything.
                state.SeenHashes = new HashSet<string>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "Could not empty the ring buffer of extended events session {session} after reading it.  " +
                    "The deadlocks it returned are unaffected; the next read will be slower for having the " +
                    "same buffer to read again.", sessionName);
            }
        }

        /// <summary>
        /// Reads forward through the event file until it runs out, a batch at a time.
        ///
        /// <para>The read takes TOP(n) in file order, which is oldest first, so a single batch starting from
        /// no cursor returns the <em>beginning</em> of the file rather than anything recent.  Stopping there
        /// would advance the cursor by one batch per run: a scheduled collection would crawl towards the
        /// present over many runs, and a collection triggered by hand - where the whole point is to see what
        /// happened recently - would return events from days ago and appear to have done nothing.</para>
        ///
        /// <para>So it pages: a batch that comes back short of the cap is the end of the file.  In steady
        /// state that is one read returning a handful of rows; the paging only does work on the first run
        /// after the cursor is lost, which is also what makes the collection pick up the deadlocks that
        /// happened before it was switched on.</para>
        /// </summary>
        private static async Task<List<string>> ReadEventFileAsync(string connectionString, string readPath,
            DeadlockCollectionState state, CancellationToken cancellationToken)
        {
            var reader = new EventFileTraceReader(connectionString, readPath, state.Cursor, MaxEventsPerRun,
                newestFirst: false, eventNameFilter: DeadlockEventNames, commandTimeout: CommandTimeout);

            var eventXml = new List<string>();
            for (var batch = 0; batch < MaxBatchesPerRun; batch++)
            {
                var before = reader.Cursor;
                var rows = await reader.ReadRawNextAsync(cancellationToken);

                // Advance even when nothing matched: the rows we skipped were still read, and re-reading them
                // every run is exactly what the cursor exists to prevent.
                state.Cursor = reader.Cursor;

                // Non-deadlock events come back with a NULL payload - see EventFileTraceReader's filter.
                eventXml.AddRange(rows.Where(r => !string.IsNullOrEmpty(r.EventData)).Select(r => r.EventData));

                // Short of the cap means the file had nothing more to give.  Compared against the raw count,
                // not the de-duplicated one: a full batch whose boundary rows were skipped would otherwise
                // look like the end and leave the rest of the file unread.
                if (reader.LastRawRowCount < MaxEventsPerRun) break;

                // A cursor that hasn't moved would page forever over the same rows.  Cannot happen while the
                // read returns a full batch, but the loop should not depend on that to terminate.
                if (SamePosition(before, reader.Cursor)) break;
            }

            return eventXml;
        }

        /// <summary>
        /// The query <see cref="ReadDeadlockEventsAsync"/> sends, taking the file set as <c>@path</c> and each of
        /// <see cref="DeadlockEventNames"/> as <c>@eventName0</c>, <c>@eventName1</c>...  Separated from the
        /// execution so its syntax can be checked without an instance - see <c>SqlScriptSyntaxTests</c>.
        /// </summary>
        internal static string BuildDeadlockEventsSql() =>
            "SELECT event_data FROM sys.fn_xe_file_target_read_file(@path, NULL, NULL, NULL) " +
            "WHERE object_name IN (" +
            string.Join(", ", Enumerable.Range(0, DeadlockEventNames.Count).Select(i => "@eventName" + i)) + ");";

        /// <summary>
        /// Reads every deadlock event in an event file set in a single filtered query - the backfill's read.
        ///
        /// <para>Not <see cref="ReadEventFileAsync"/>, because that read has a cursor to keep and this one
        /// doesn't.  The cursor comes from the last row read, so that read has to return every row - deadlock or
        /// not - and pages to keep the result bounded, stopping at <see cref="MaxBatchesPerRun"/>.  With no cursor
        /// the filter can go in the WHERE clause instead: a system_health file set can hold millions of events
        /// and only a handful of deadlocks, and this returns just the handful.  One query, so no paging and no
        /// batch cap to stop short of the newest; SQL Server still scans the whole set, but without shipping every
        /// row across or restarting the scan from an offset every batch.</para>
        ///
        /// <para>No ORDER BY, so rows come back in file order as the scan reaches them, and each is added to
        /// <paramref name="eventXml"/> as it is read: a read cut off by the caller's time limit keeps what arrived
        /// before it, the older end of the file set.  Newest first would be the better end to keep, but ordering
        /// holds every row back until the whole set has been scanned, so a read cut off would keep nothing.</para>
        /// </summary>
        private static async Task ReadDeadlockEventsAsync(string connectionString, string readPath,
            List<string> eventXml, int timeLimitSeconds, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            // The backfill's time limit rather than the Deadlocks command timeout, which would cut the read off at 90
            // seconds whatever the limit said.  The caller's cancellation is what enforces the limit - a command
            // timeout doesn't reliably bound the total time of a result read row by row - so this is the backstop
            // for a read that cancellation fails to interrupt.  0 on both is no limit.
            await using var cmd = new SqlCommand(BuildDeadlockEventsSql(), cn)
            { CommandType = CommandType.Text, CommandTimeout = timeLimitSeconds };
            cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = readPath;
            // object_name is NVARCHAR(60) on the function - see EventFileTraceReader.
            for (var i = 0; i < DeadlockEventNames.Count; i++)
            {
                cmd.Parameters.Add("@eventName" + i, SqlDbType.NVarChar, 60).Value = DeadlockEventNames[i];
            }

            await cn.OpenAsync(cancellationToken);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                if (!rdr.IsDBNull(0)) eventXml.Add(rdr.GetString(0));
            }
        }

        private static bool SamePosition(FileTargetCursor a, FileTargetCursor b) =>
            a.HasValue == b.HasValue
            && a.FileName == b.FileName
            && a.Offset == b.Offset
            && a.ConsumedAtOffset == b.ConsumedAtOffset;

        /// <summary>
        /// Shreds <c>xml_deadlock_report</c> event XML into the three tables.  The seam the event_file path
        /// uses, and the one to reuse for an on-demand read that doesn't go through a collection.
        /// </summary>
        public static Result ShredEvents(IEnumerable<string> eventXml)
        {
            ArgumentNullException.ThrowIfNull(eventXml);
            return Build(ParseEvents(eventXml));
        }

        private static List<CapturedDeadlock> ParseEvents(IEnumerable<string> eventXml) =>
            eventXml.SelectMany(ParseEvent).ToList();

        /// <summary>
        /// Shreds a ring_buffer target's <c>target_data</c>, suppressing what the previous read of the same
        /// buffer already returned.  Reads only - the buffer is never flushed.
        /// </summary>
        public static Result ShredRingBufferTargetData(string targetDataXml, DeadlockCollectionState state) =>
            Build(SelectUnseenRingBufferDeadlocks(targetDataXml, state));

        private static List<CapturedDeadlock> SelectUnseenRingBufferDeadlocks(string targetDataXml,
            DeadlockCollectionState state)
        {
            var graphs = new List<CapturedDeadlock>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var captured in ParseRingBuffer(targetDataXml))
            {
                var key = captured.HashHex;
                seen.Add(key);
                // The buffer returns everything it holds on every read, so suppress what the last read saw.
                if (!state.SeenHashes.Contains(key)) graphs.Add(captured);
            }

            // Replace rather than union: a graph that has aged out of the buffer leaves the set with it, which
            // is what keeps this bounded by the buffer rather than growing for the life of the service.
            if (seen.Count > 0 || !string.IsNullOrEmpty(targetDataXml)) state.SeenHashes = seen;

            return graphs;
        }

        private static IEnumerable<CapturedDeadlock> ParseRingBuffer(string targetDataXml)
        {
            if (string.IsNullOrEmpty(targetDataXml)) yield break;

            XElement root;
            try
            {
                root = XElement.Parse(targetDataXml);
            }
            catch (Exception ex)
            {
                // Nearly always the buffer being too big: SQL Server truncates a large ring buffer's
                // target_data mid-XML, so what comes back will not parse and the whole read is lost rather
                // than part of it.  Named here because nothing else about the failure points at the cause.
                Log.Warning(ex,
                    "Deadlock collection could not parse the ring buffer target data.  A ring buffer over " +
                    "1MB can have its target_data truncated, which leaves it malformed - reduce " +
                    "DeadlockXERingBufferKB in the service configuration if it has been raised above that.");
                yield break;
            }

            // The buffer says so itself when it has dropped events to stay within its size.  Worth saying
            // out loud: it is the one sign that collections are not keeping up with the deadlock rate, and
            // the answer is a bigger buffer or a shorter schedule rather than anything in the data.
            if (root.Attribute("truncated")?.Value is "1")
            {
                Log.Warning("The deadlock ring buffer reported dropping events to stay within its size.  " +
                            "Deadlocks are being produced faster than they are collected: raise " +
                            "DeadlockXERingBufferKB or collect Deadlocks more often.");
            }

            foreach (var ev in root.Elements("event"))
            {
                if (!IsDeadlockEvent(ev.Attribute("name")?.Value)) continue;
                foreach (var captured in ParseEvent(ev)) yield return captured;
            }
        }

        /// <summary>True for either of the names a deadlock graph arrives under - see
        /// <see cref="DeadlockEventNames"/>.</summary>
        private static bool IsDeadlockEvent(string eventName) =>
            eventName != null
            && DeadlockEventNames.Any(n => string.Equals(eventName, n, StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<CapturedDeadlock> ParseEvent(string eventXml)
        {
            XElement ev;
            try
            {
                ev = XElement.Parse(eventXml);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Deadlock collection could not parse an event.");
                return Array.Empty<CapturedDeadlock>();
            }
            return ParseEvent(ev);
        }

        private static IEnumerable<CapturedDeadlock> ParseEvent(XElement ev)
        {
            // The graph carries no time of its own (see DeadlockGraph.OccurredAt), so the event envelope's
            // timestamp is the only real answer to when this happened.
            if (!TryGetEventTime(ev, out var eventTime))
            {
                Log.Warning("Skipping a deadlock event with no usable timestamp attribute.");
                return Array.Empty<CapturedDeadlock>();
            }

            // The parser handles the XE envelope itself, so it gets the event rather than the extracted graph.
            if (!DeadlockParser.TryParse(ev.ToString(SaveOptions.DisableFormatting), out var graphs))
            {
                Log.Warning("Skipping a deadlock event at {eventTime} whose graph could not be parsed.", eventTime);
                return Array.Empty<CapturedDeadlock>();
            }

            return graphs.Select(g => new CapturedDeadlock(eventTime, g)).ToList();
        }

        private static bool TryGetEventTime(XElement ev, out DateTime eventTime)
        {
            eventTime = default;
            var raw = ev.Attribute("timestamp")?.Value;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return false;
            }

            eventTime = TruncateToMillisecond(parsed);
            return true;
        }

        /// <summary>
        /// Stored as DATETIME2(3); truncate here so the value that goes into the key is the value we matched on,
        /// rather than something SQL Server rounds afterwards.  Truncated rather than rounded because that is what the
        /// XML timestamp attribute already is, so a deadlock decoded from the binary stream - which carries the full
        /// precision - keys the same as one read as XML.
        /// </summary>
        private static DateTime TruncateToMillisecond(DateTime value) =>
            new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);

        /// <summary>A parsed graph with the time its event carried, and its identity.</summary>
        private sealed class CapturedDeadlock
        {
            public CapturedDeadlock(DateTime eventTime, DeadlockGraph graph)
            {
                EventTime = eventTime;
                Graph = graph;
                Hash = ComputeHash(graph.Xml);
                HashHex = Convert.ToHexString(Hash);
            }

            public DateTime EventTime { get; }

            public DeadlockGraph Graph { get; }

            public byte[] Hash { get; }

            public string HashHex { get; }
        }

        /// <summary>
        /// The occurrence identity: a SHA-256 of the graph, truncated to
        /// <see cref="DeadlockTables.DeadlockHashBytes"/>.
        ///
        /// <para>Hashes <see cref="DeadlockGraph.Xml"/>, which the parser produces by re-serialising the
        /// element it parsed.  That round trip is what makes this stable: whitespace between elements is
        /// dropped on the way in, so the same deadlock read through the event file and through the ring
        /// buffer produces the same bytes.  Every attribute value survives it, which is what keeps two
        /// distinct deadlocks apart.</para>
        ///
        /// <para>The normalisation reaches formatting only.  Text inside <c>inputbuf</c> and <c>frame</c> is
        /// the statement itself and is preserved verbatim, trailing spaces included - so if a future read path
        /// were to return that text padded differently, the same deadlock would hash differently and be stored
        /// twice.  Nothing does today, and the alternative - trimming statement text before hashing - would
        /// throw away part of what distinguishes one occurrence from another.</para>
        /// </summary>
        private static byte[] ComputeHash(string graphXml)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(graphXml ?? string.Empty));
            return digest[..DeadlockTables.DeadlockHashBytes];
        }

        private static Result Build(IReadOnlyCollection<CapturedDeadlock> captured)
        {
            var dtDeadlocks = DeadlockTables.CreateDeadlocksTable();
            var dtProcesses = DeadlockTables.CreateProcessesTable();
            var dtResources = DeadlockTables.CreateResourcesTable();

            // A single read can legitimately return the same graph twice (a graph present in two rollover
            // files, say).  The table types are keyed on (EventTime, DeadlockHash), so a duplicate here would
            // fail the whole import rather than be skipped on the server.
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in captured)
            {
                if (!seen.Add($"{item.EventTime:O}|{item.HashHex}")) continue;

                AddDeadlock(dtDeadlocks, item);
                AddProcesses(dtProcesses, item);
                AddResources(dtResources, item);
            }

            return new Result { Deadlocks = dtDeadlocks, Processes = dtProcesses, Resources = dtResources };
        }

        private static void AddDeadlock(DataTable dt, CapturedDeadlock item)
        {
            var graph = item.Graph;
            var row = dt.NewRow();
            row["EventTime"] = item.EventTime;
            row["DeadlockHash"] = item.Hash;
            row["Signature"] = Value(DeadlockSignature.Compute(graph).Value);
            row["SignatureVersion"] = (byte)DeadlockSignature.VersionNumber;
            row["ProcessCount"] = (short)graph.Processes.Count;
            row["VictimCount"] = (short)graph.Victims.Count;
            row["ResourceCount"] = (short)graph.Resources.Count;
            row["IsParallel"] = graph.IsParallel;
            row["DeadlockXmlCompressed"] = Value(DeadlockTables.CompressGraph(graph.Xml));
            dt.Rows.Add(row);
        }

        private static void AddProcesses(DataTable dt, CapturedDeadlock item)
        {
            var index = 0;
            foreach (var process in item.Graph.Processes)
            {
                var frame = process.PrimaryFrame;
                var row = dt.NewRow();
                row["EventTime"] = item.EventTime;
                row["DeadlockHash"] = item.Hash;
                row["ProcessIndex"] = (short)index++;
                row["IsVictim"] = process.IsVictim;
                row["database_id"] = Value(process.CurrentDatabaseId);
                row["SPID"] = Value(process.Spid);
                row["Ecid"] = Value(process.Ecid);
                row["LoginName"] = Value(Trim(process.LoginName, 128));
                row["HostName"] = Value(Trim(process.HostName, 128));
                row["ClientApp"] = Value(Trim(process.ClientApp, 128));
                // Only a real module is worth storing - "adhoc" and "unknown" are placeholders, not names.
                row["ProcedureName"] = Value(frame is { IsModule: true } ? Trim(frame.ProcedureName, 776) : null);
                row["StatementText"] = Value(process.PrimaryStatement);
                row["IsolationLevel"] = Value(Trim(process.IsolationLevel, 50));
                row["LockMode"] = Value(Trim(process.LockMode, 20));
                row["WaitResource"] = Value(Trim(process.WaitResource, 512));
                row["WaitTimeMs"] = Value(process.WaitTime.HasValue ? (long)process.WaitTime.Value.TotalMilliseconds : null);
                row["LogUsed"] = Value(process.LogUsed);
                row["TransactionName"] = Value(Trim(process.TransactionName, 128));
                row["Priority"] = Value(Clamp(process.Priority));
                row["LastBatchStarted"] = Value(process.LastBatchStarted);
                row["LastBatchCompleted"] = Value(process.LastBatchCompleted);
                row["LastTransactionStarted"] = Value(process.LastTransactionStarted);
                row["Status"] = Value(Trim(process.Status, 30));
                row["TransactionCount"] = Value(process.TransactionCount);
                row["HostPid"] = Value(process.HostPid);
                row["InputBuffer"] = Value(process.InputBuffer);
                row["ClientOption1"] = Value(process.ClientOption1);
                row["ClientOption2"] = Value(process.ClientOption2);
                dt.Rows.Add(row);
            }
        }

        private static void AddResources(DataTable dt, CapturedDeadlock item)
        {
            var index = 0;
            foreach (var resource in item.Graph.Resources)
            {
                var row = dt.NewRow();
                row["EventTime"] = item.EventTime;
                row["DeadlockHash"] = item.Hash;
                row["ResourceIndex"] = (short)index++;
                row["ResourceType"] = Trim(resource.TypeName, 50) ?? string.Empty;
                row["database_id"] = Value(resource.DatabaseId);
                row["ObjectName"] = Value(Trim(resource.ObjectName, 776));
                row["IndexName"] = Value(Trim(resource.IndexName, 128));
                row["LockMode"] = Value(Trim(resource.Mode, 20));
                row["OwnerModes"] = Value(Modes(resource.Owners));
                row["WaiterModes"] = Value(Modes(resource.Waiters));
                row["OwnerCount"] = (short)resource.Owners.Count;
                row["WaiterCount"] = (short)resource.Waiters.Count;
                row["IsParallelismResource"] = resource.IsParallelismResource;
                dt.Rows.Add(row);
            }
        }

        /// <summary>
        /// The distinct lock modes across a resource's owners or waiters, sorted so the value is stable.  The
        /// participants themselves are already rows in dbo.DeadlockProcesses; what a report wants here is the
        /// set of modes involved.
        /// </summary>
        private static string Modes(IEnumerable<DeadlockResourceParticipant> participants)
        {
            var modes = participants
                .Select(p => p.Mode)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m, StringComparer.Ordinal);

            return Trim(string.Join(",", modes), 200);
        }

        /// <summary>
        /// Truncates to the storage width.  A graph is not a trusted source of short strings - a wait resource
        /// or an object name can run long - and an over-length value fails the whole batch on insert, which
        /// would lose every deadlock in the run rather than one column of one row.
        /// </summary>
        private static string Trim(string value, int maxLength) =>
            value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;

        /// <summary>DEADLOCK_PRIORITY is documented as -10..10, but the column is narrower than the graph's int.</summary>
        private static short? Clamp(int? value) =>
            value is null ? null : (short)Math.Clamp(value.Value, short.MinValue, short.MaxValue);

        private static object Value(object value) => value ?? DBNull.Value;
    }
}
