using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DBADash.Deadlocks;
using DBADash.XE;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DBADash.SlowQueries
{
    /// <summary>
    /// Captures slow rpc_completed and sql_batch_completed events for the SlowQueries collection, in one of three
    /// modes - see <see cref="DBADashSource.SlowQueryCaptureModes"/>.  Whatever the mode, the result is the same
    /// two tables, SlowQueries and SlowQueriesStats, so the repository can't tell which mode captured them.
    ///
    /// <list type="bullet">
    /// <item><see cref="CollectRingBufferAsync"/> - the default and the original behaviour, unchanged: the
    /// SlowQueries script creates DBADash_1 (and DBADash_2 in dual session mode), returns the ring buffer and empties
    /// it with a stop/start.</item>
    /// <item><see cref="CollectEventFileAsync"/> - a session DBA Dash creates with an event_file target, read
    /// from a resume cursor the way the deadlock collection reads its session.  Never stopped to be emptied, so
    /// nothing is lost in a flush window, and a burst spills to disk instead of overflowing a buffer.</item>
    /// <item><see cref="CollectExistingSessionAsync"/> - a session the DBA runs, read and never altered.</item>
    /// </list>
    /// </summary>
    internal static class SlowQueryCollector
    {
        /// <summary>The application name every slow query read connects with.  Every session's predicate excludes
        /// it, so the reads - which can easily exceed the threshold themselves - are never captured.</summary>
        public const string XEApplicationName = "DBADashXE";

        /// <summary>Rows one event file read asks for.  A run pages through the file a batch at a time.</summary>
        public const int MaxEventsPerBatch = 5000;

        /// <summary>
        /// Batches one run reads before leaving the rest for the next run.  Bounds the first read of a session that
        /// already holds a lot - an existing session, or the managed one after the service has been down - so a
        /// single collection can't build an unbounded DataSet.  The cursor means the next run carries on from here.
        /// </summary>
        public const int MaxBatchesPerRun = 10;

        /// <summary>The events read from a session DBA Dash doesn't own.  The managed sessions only capture the
        /// first two; the statement level events are accepted so a session the DBA built for statements works too -
        /// the import takes the statement text where there is no batch text.</summary>
        public static readonly IReadOnlyList<string> ExistingSessionEventNames = new[]
        {
            "rpc_completed", "sql_batch_completed", "sql_statement_completed", "sp_statement_completed"
        };

        public sealed class Result
        {
            public DataTable SlowQueries { get; init; }

            public DataTable Stats { get; init; }
        }

        private static int CommandTimeout => CollectionType.SlowQueries.GetCommandTimeout();

        /// <summary>The connection string every slow query read uses - see <see cref="XEApplicationName"/>.</summary>
        public static string GetXEConnectionString(string connectionString) =>
            new SqlConnectionStringBuilder(connectionString) { ApplicationName = XEApplicationName }.ConnectionString;

        #region Ring buffer (default)

        /// <summary>
        /// The original slow query capture, moved here from DBCollector without changing what it sends: the same
        /// script, the same parameters, and the target_data streamed straight into an XElement.
        /// </summary>
        public static async Task<Result> CollectRingBufferAsync(string connectionString, bool isAzureDB,
            int thresholdMs, int sessionMaxMemoryKB, bool useDualSession, int targetMaxMemoryKB,
            bool collectGroupIDAndPoolID)
        {
            var slowQueriesSQL = isAzureDB ? SqlStrings.SlowQueriesAzure : SqlStrings.SlowQueries;
            await using var cn = new SqlConnection(GetXEConnectionString(connectionString));
            await using var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = CommandTimeout };
            await cn.OpenAsync();
            cmd.Parameters.AddWithValue("SlowQueryThreshold", thresholdMs * 1000);
            cmd.Parameters.AddWithValue("MaxMemory", sessionMaxMemoryKB);
            cmd.Parameters.AddWithValue("UseDualSession", useDualSession);
            cmd.Parameters.AddWithValue("MaxTargetMemory", targetMaxMemoryKB);
            cmd.Parameters.AddWithValue("CollectGroupIDAndPoolID", collectGroupIDAndPoolID);

            XElement targetData;
            await using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess |
                                                                   CommandBehavior.SingleResult |
                                                                   CommandBehavior.SingleRow))
            {
                if (!await reader.ReadAsync() || reader.IsDBNull(0))
                {
                    throw new Exception("Result is NULL");
                }

                using var textReader = reader.GetTextReader(0);
                var settings = new XmlReaderSettings
                {
                    Async = true,
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                };
                using var xmlReader = XmlReader.Create(textReader, settings);
                targetData = await XElement.LoadAsync(xmlReader, LoadOptions.None, CancellationToken.None);
            }

            var dt = XETools.XEStrToDT(targetData, out var ringBufferAtt);
            return new Result { SlowQueries = dt, Stats = ringBufferAtt.GetTable() };
        }

        #endregion

        #region Event file (managed session)

        /// <summary>
        /// Ensures the managed session exists, is running and matches the configuration, then reads what it has
        /// captured since <paramref name="state"/>'s cursor.
        /// </summary>
        /// <param name="startupState">Start the session with the instance - see
        /// <see cref="DBADashSource.KeepSlowQueryXESessionRunning"/>.</param>
        public static async Task<Result> CollectEventFileAsync(string connectionString, int thresholdMs,
            int sessionMaxMemoryKB, int maxFileSizeMB, int maxRolloverFiles, bool startupState,
            bool collectGroupIDAndPoolID, SlowQueryCollectionState state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(state);
            var xeConnectionString = GetXEConnectionString(connectionString);
            const string sessionName = DBADashSource.ManagedSlowQueryXESessionName;

            var targets = await EnsureManagedSessionAsync(xeConnectionString, thresholdMs, sessionMaxMemoryKB,
                maxFileSizeMB, maxRolloverFiles, startupState, collectGroupIDAndPoolID, cancellationToken);
            var readPath = targets.TryGetValue("event_file", out var fileTargetData)
                ? XESessionTargetResolver.ResolveEventFileReadPath(fileTargetData)
                : null;
            if (string.IsNullOrEmpty(readPath))
            {
                throw new Exception($"Extended events session '{sessionName}' is not running or has no event file to read.");
            }

            // Everything the managed session captures is wanted - its predicates are the threshold - so no filter.
            var events = await ReadEventFileAsync(xeConnectionString, readPath, null, state, cancellationToken);
            var dropped = await GetDroppedSinceLastReadAsync(xeConnectionString, sessionName, false, state,
                cancellationToken);
            return Build(events, dropped, truncated: false);
        }

        /// <summary>
        /// The configuration each instance's session was last checked against by the full session script.  While it
        /// still matches and the session is running, a run only confirms the session is running - see
        /// <see cref="EnsureManagedSessionAsync"/>.  In memory only, so a service restart checks every session again.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> VerifiedDefinition =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// "The target ... encountered a configuration error during initialization" - raised when the session is
        /// started and SQL Server can't create its event file.  The message ends "(null)" rather than saying why, and
        /// the usual reasons are no free space or no write access in the instance's log directory.
        /// </summary>
        private const int ErrorTargetInitialization = 25602;

        /// <summary>
        /// A message for <see cref="ErrorTargetInitialization"/> that says where the file was being written and what
        /// to check, since the server's own message doesn't.  The directory is looked up best effort - the original
        /// error is what matters, so a failure here only leaves the directory out.
        /// </summary>
        private static async Task<string> DescribeEventFileFailureAsync(string connectionString, SqlException ex,
            CancellationToken cancellationToken)
        {
            string directory = null;
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('ErrorLogFileName') AS NVARCHAR(260))", cn)
                { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
                await cn.OpenAsync(cancellationToken);
                var errorLog = await cmd.ExecuteScalarAsync(cancellationToken) as string;
                directory = string.IsNullOrEmpty(errorLog) ? null : System.IO.Path.GetDirectoryName(errorLog);
            }
            catch (Exception lookup) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Debug(lookup, "Could not look up the log directory after an event file failure");
            }

            return $"Extended events session '{DBADashSource.ManagedSlowQueryXESessionName}' could not start because " +
                   "SQL Server could not create its event file" +
                   (directory == null ? " in the instance's log directory" : $" in {directory}") +
                   ".  The usual causes are no free space on that drive or the SQL Server service account not being " +
                   "able to write there.  Free up space or fix the permissions, or set this connection's slow query " +
                   $"capture mode to RingBuffer, which needs nothing on disk.  SQL Server's message: {ex.Message}";
        }

        /// <summary>
        /// Makes sure the managed session exists, matches the configuration and is running.
        ///
        /// <para>The full session script - around eight catalog queries comparing the session with the configuration -
        /// only runs on an instance's first collection after the service starts, when the configuration has changed
        /// since it last ran, or when the session isn't running.  Every other run only looks up the session's targets,
        /// which the read needs anyway and which come back empty when the session isn't running - so in steady state
        /// checking the session costs nothing extra.  Measured in a lab, the full script was about half the cost of an
        /// event file collection.
        /// What that gives up is noticing a session altered by hand while it keeps running: that is picked up at the
        /// next service restart or configuration change rather than within a minute, which is acceptable since the
        /// check is there to apply DBA Dash's own configuration.  A session that is stopped or dropped - by hand, or by
        /// another DBA Dash service in ring buffer mode removing it as a leftover - isn't running, so the next run puts
        /// it back.</para>
        ///
        /// <para>Needs ALTER ANY EVENT SESSION, which the ring buffer mode needs too.</para>
        /// </summary>
        /// <returns>The running session's targets, keyed by target name.</returns>
        private static async Task<Dictionary<string, string>> EnsureManagedSessionAsync(string connectionString,
            int thresholdMs, int sessionMaxMemoryKB, int maxFileSizeMB, int maxRolloverFiles, bool startupState,
            bool collectGroupIDAndPoolID, CancellationToken cancellationToken)
        {
            var instance = new SqlConnectionStringBuilder(connectionString).DataSource;
            var definition = string.Join("|", thresholdMs, sessionMaxMemoryKB, maxFileSizeMB, maxRolloverFiles,
                startupState, collectGroupIDAndPoolID);

            if (VerifiedDefinition.TryGetValue(instance, out var verified) && verified == definition)
            {
                var targets = await GetTargetsAsync(connectionString, cancellationToken);
                // Running with its event file, and the configuration hasn't changed since it was checked.  Anything
                // else - stopped, dropped, or its target changed by hand - gets the full script.
                if (targets.ContainsKey("event_file")) return targets;
            }

            await RunSessionScriptAsync(connectionString, instance, thresholdMs, sessionMaxMemoryKB, maxFileSizeMB,
                maxRolloverFiles, startupState, collectGroupIDAndPoolID, cancellationToken);
            // Recorded only once the script has succeeded, so a failed start is checked in full again next run.
            VerifiedDefinition[instance] = definition;
            return await GetTargetsAsync(connectionString, cancellationToken);
        }

        private static Task<Dictionary<string, string>> GetTargetsAsync(string connectionString,
            CancellationToken cancellationToken) =>
            XESessionTargetResolver.GetSessionTargetsAsync(connectionString, false,
                DBADashSource.ManagedSlowQueryXESessionName, cancellationToken, CommandTimeout);

        /// <summary>Runs SQLSlowQueriesEventFileSession.sql, which creates, rebuilds and starts the session as needed.</summary>
        private static async Task RunSessionScriptAsync(string connectionString, string instance, int thresholdMs,
            int sessionMaxMemoryKB, int maxFileSizeMB, int maxRolloverFiles, bool startupState,
            bool collectGroupIDAndPoolID, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(SqlStrings.GetSqlString("SlowQueriesEventFileSession"), cn)
            { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = DBADashSource.ManagedSlowQueryXESessionName;
            // Microseconds, as the predicate compares - a BIGINT so a large threshold in ms can't overflow.
            cmd.Parameters.Add("@SlowQueryThreshold", SqlDbType.BigInt).Value = thresholdMs * 1000L;
            cmd.Parameters.Add("@MaxMemory", SqlDbType.Int).Value = sessionMaxMemoryKB;
            cmd.Parameters.Add("@MaxFileSizeMB", SqlDbType.BigInt).Value = maxFileSizeMB;
            cmd.Parameters.Add("@MaxRolloverFiles", SqlDbType.BigInt).Value = maxRolloverFiles;
            cmd.Parameters.Add("@StartupState", SqlDbType.Bit).Value = startupState;
            cmd.Parameters.Add("@CollectGroupIDAndPoolID", SqlDbType.Bit).Value = collectGroupIDAndPoolID;
            await cn.OpenAsync(cancellationToken);
            SqlDataReader rdr;
            try
            {
                rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.Number == ErrorTargetInitialization)
            {
                throw new Exception(await DescribeEventFileFailureAsync(connectionString, ex, cancellationToken), ex);
            }
            await using var _ = rdr;
            if (!await rdr.ReadAsync(cancellationToken)) return;

            var created = rdr.GetBoolean(0);
            var rebuilt = rdr.GetBoolean(1);
            if (rebuilt)
            {
                Log.Information("Rebuilt extended events session {session} on {instance} to match the slow query configuration",
                    DBADashSource.ManagedSlowQueryXESessionName, instance);
            }
            else if (created)
            {
                Log.Information("Created extended events session {session} on {instance} for slow query capture",
                    DBADashSource.ManagedSlowQueryXESessionName, instance);
            }
        }

        /// <summary>Connections (and the mode they were in) already checked for leftover sessions - see
        /// <see cref="RemoveLeftoverSessionsOnceAsync"/>.</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> LeftoverChecked =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Drops the sessions that belong to the other mode, once per connection for the life of the service: in event
        /// file mode the ring buffer sessions (DBADash_1 and DBADash_2), in any other mode the event file session.
        ///
        /// <para>Needed because a session can outlive its mode: a switch of mode leaves the old mode's sessions on the
        /// instance, and with <see cref="DBADashSource.KeepSlowQueryXESessionRunning"/> set even a service stop doesn't
        /// remove the event file session.  Once is enough - nothing in this service creates them again while the
        /// other mode is configured.  Deliberately not on every run, so that two DBA Dash services can capture the same
        /// instance in different modes: each removes the other's sessions once, at its first collection, and the other
        /// service recreates them on its next run.  Server scoped only: neither mode drops sessions on Azure SQL
        /// Database this way.  A failure is logged rather than failing the collection, and isn't retried.</para>
        /// </summary>
        public static async Task RemoveLeftoverSessionsOnceAsync(string connectionString, string connectionID,
            bool eventFileMode)
        {
            if (!LeftoverChecked.TryAdd((connectionID ?? string.Empty) + "|" + eventFileMode, 0)) return;
            var sessionNames = eventFileMode
                ? new[] { "DBADash_1", "DBADash_2" }
                : new[] { DBADashSource.ManagedSlowQueryXESessionName };
            try
            {
                // Fixed, known names, so inlining them is safe.
                var sql = string.Concat(sessionNames.Select(name =>
                    $"IF EXISTS(SELECT 1 FROM sys.server_event_sessions WHERE name = N'{name}') DROP EVENT SESSION {name} ON SERVER;"));
                await using var cn = new SqlConnection(GetXEConnectionString(connectionString));
                await using var cmd = new SqlCommand(sql, cn)
                { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not remove leftover extended events sessions {sessions} for {connection}",
                    string.Join(", ", sessionNames), connectionID);
            }
        }

        #endregion

        #region Existing session

        /// <summary>
        /// Reads a session DBA Dash doesn't own.  Read only throughout: never created, started, stopped or emptied.
        /// An event_file target is read from the cursor; a ring buffer, which can't be emptied without stopping the
        /// session, is read whole and diffed against the previous read.  The configured threshold and the
        /// exclusions the managed sessions have in their predicates are applied as the events are read.
        /// </summary>
        public static async Task<Result> CollectExistingSessionAsync(string connectionString, string sessionName,
            bool databaseScoped, bool isManagedInstance, int thresholdMs, SlowQueryCollectionState state,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                throw new Exception("Slow query capture is set to read an existing session, but no session name is configured.");
            }
            if (DBADashSource.IsReservedSlowQueryXESessionName(sessionName))
            {
                // Checked here as well as in the config tools, for a config edited by hand: reading it would only work
                // until DBA Dash next dropped it as its own.
                throw new Exception($"Slow query capture can't read '{sessionName}' as an existing session - it is " +
                                    "one of the sessions DBA Dash creates and removes itself.  Use the EventFile or " +
                                    "RingBuffer capture mode, or a session of your own with a different name.");
            }
            ArgumentNullException.ThrowIfNull(state);
            var xeConnectionString = GetXEConnectionString(connectionString);

            var targets = await XESessionTargetResolver.GetSessionTargetsAsync(xeConnectionString, databaseScoped,
                sessionName, cancellationToken, CommandTimeout);
            if (targets.Count == 0)
            {
                throw new Exception($"Extended events session '{sessionName}' is not running.  Slow query capture " +
                                    "reads an existing session but never starts it.");
            }

            var thresholdMicroseconds = thresholdMs * 1000L;
            IEnumerable<XElement> events;
            var truncated = false;
            if (targets.TryGetValue("event_file", out var fileTargetData) &&
                DeadlockCollector.ResolveEventFileReadPath(fileTargetData, isManagedInstance) is { Length: > 0 } readPath)
            {
                events = await ReadEventFileAsync(xeConnectionString, readPath, ExistingSessionEventNames, state,
                    cancellationToken);
            }
            else if (targets.TryGetValue("ring_buffer", out var ringTargetData))
            {
                var (fresh, seen) = RingBufferWatchDiff.Apply(ringTargetData, state.SeenEvents);
                // Kept only when the buffer was read, so an empty read doesn't make the next one resend everything.
                if (!string.IsNullOrEmpty(ringTargetData)) state.SeenEvents = seen;
                events = fresh;
                truncated = IsRingBufferTruncated(ringTargetData);
            }
            else
            {
                throw new Exception($"Extended events session '{sessionName}' has no event_file or ring_buffer " +
                                    $"target to read - it has {string.Join(", ", targets.Keys)}.");
            }

            var matching = events.Where(e => IsSlowQueryEvent(e, thresholdMicroseconds)).ToList();
            var dropped = await GetDroppedSinceLastReadAsync(xeConnectionString, sessionName, databaseScoped, state,
                cancellationToken);
            return Build(matching, dropped, truncated);
        }

        /// <summary>
        /// The filter applied to a session DBA Dash doesn't own: one of <see cref="ExistingSessionEventNames"/>, over
        /// the threshold, and not something the managed sessions' predicates would have excluded.
        /// </summary>
        internal static bool IsSlowQueryEvent(XElement evt, long thresholdMicroseconds)
        {
            var name = evt.Attribute("name")?.Value;
            if (name == null || !ExistingSessionEventNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var duration = GetValue(evt, "data", "duration");
            if (!long.TryParse(duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationValue) ||
                durationValue <= thresholdMicroseconds)
            {
                return false;
            }

            if (string.Equals(GetValue(evt, "action", "client_app_name"), XEApplicationName, StringComparison.Ordinal))
            {
                return false;
            }

            return !(string.Equals(name, "rpc_completed", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(GetValue(evt, "data", "object_name"), "sp_readrequest", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetValue(XElement evt, string elementType, string name)
        {
            var field = evt.Elements(elementType).FirstOrDefault(e => e.Attribute("name")?.Value == name);
            return field == null ? null : field.Element("value")?.Value ?? field.Value;
        }

        private static bool IsRingBufferTruncated(string targetDataXml)
        {
            if (string.IsNullOrEmpty(targetDataXml)) return false;
            try
            {
                return XElement.Parse(targetDataXml).Attribute("truncated")?.Value is "1";
            }
            catch (XmlException)
            {
                return false;
            }
        }

        #endregion

        #region Shared

        /// <summary>
        /// Reads forward from the cursor until the file set runs out or <see cref="MaxBatchesPerRun"/> is reached,
        /// moving <paramref name="state"/>'s cursor as it goes - the same paging the deadlock collection does, for the
        /// same reason: a read without paging would crawl one batch per run towards the present.
        /// </summary>
        /// <param name="eventNameFilter">Only these events' payloads are returned - see
        /// <see cref="EventFileTraceReader"/>.  Null for all of them.</param>
        private static async Task<List<XElement>> ReadEventFileAsync(string connectionString, string readPath,
            IReadOnlyList<string> eventNameFilter, SlowQueryCollectionState state, CancellationToken cancellationToken)
        {
            var reader = new EventFileTraceReader(connectionString, readPath, state.Cursor, MaxEventsPerBatch,
                newestFirst: false, eventNameFilter: eventNameFilter, commandTimeout: CommandTimeout);

            var events = new List<XElement>();
            for (var batch = 0; batch < MaxBatchesPerRun; batch++)
            {
                var before = reader.Cursor;
                var rows = await reader.ReadRawNextAsync(cancellationToken);
                state.Cursor = reader.Cursor;

                foreach (var row in rows)
                {
                    if (string.IsNullOrEmpty(row.EventData)) continue; // filtered out - payload suppressed
                    try
                    {
                        events.Add(XElement.Parse(row.EventData));
                    }
                    catch (XmlException ex)
                    {
                        Log.Warning(ex, "Slow query collection could not parse an event from {path}", readPath);
                    }
                }

                if (reader.LastRawRowCount < MaxEventsPerBatch) break;
                if (SamePosition(before, reader.Cursor)) break;
                if (batch == MaxBatchesPerRun - 1)
                {
                    // Expected once on a first read of a session that already holds a lot.  On every run it means the
                    // session captures more than a run reads, and the collection will fall further behind.
                    Log.Warning("Slow query collection read the maximum of {count} events from {path}; the rest is left for the next run.  If this repeats, raise the threshold or collect SlowQueries more often.",
                        MaxBatchesPerRun * MaxEventsPerBatch, readPath);
                }
            }

            return events;
        }

        private static bool SamePosition(FileTargetCursor a, FileTargetCursor b) =>
            a.HasValue == b.HasValue
            && a.FileName == b.FileName
            && a.Offset == b.Offset
            && a.ConsumedAtOffset == b.ConsumedAtOffset;

        /// <summary>
        /// Events the session has dropped since the previous read.  The ring buffer mode reports what its buffer
        /// dropped since it was last emptied; the nearest equivalent for a session that is never emptied is the
        /// change in its dropped_event_count.  Zero on the first read, and the whole count when it went down - the
        /// session was restarted, so the counter started again.  Best effort: a failure here isn't worth failing the
        /// collection over.
        /// </summary>
        private static async Task<int> GetDroppedSinceLastReadAsync(string connectionString, string sessionName,
            bool databaseScoped, SlowQueryCollectionState state, CancellationToken cancellationToken)
        {
            long current;
            try
            {
                var sessions = databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand($"SELECT dropped_event_count FROM {sessions} WHERE name = @name", cn)
                { CommandType = CommandType.Text, CommandTimeout = CommandTimeout };
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = sessionName;
                await cn.OpenAsync(cancellationToken);
                var value = await cmd.ExecuteScalarAsync(cancellationToken);
                if (value == null || value == DBNull.Value) return 0;
                current = Convert.ToInt64(value);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Debug(ex, "Could not read dropped_event_count for session {session}", sessionName);
                return 0;
            }

            var prior = state.DroppedEventCount;
            state.DroppedEventCount = current;
            if (prior == null) return 0;
            var delta = current >= prior.Value ? current - prior.Value : current;
            return (int)Math.Min(delta, int.MaxValue);
        }

        private static Result Build(IReadOnlyCollection<XElement> events, int dropped, bool truncated)
        {
            var dt = XETools.XEEventsToDT(events);
            var stats = new RingBufferTargetAttributes
            {
                Truncated = truncated ? 1 : 0,
                EventCount = events.Count,
                TotalEventsProcessed = events.Count,
                DroppedCount = dropped
            };
            return new Result { SlowQueries = dt, Stats = stats.GetTable() };
        }

        #endregion
    }
}
