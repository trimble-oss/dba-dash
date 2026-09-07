using System;
using System.Collections.Generic;
using System.Data;
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
            if (targets.TryGetValue("event_file", out var fileTargetData) &&
                XESessionTargetResolver.ResolveEventFileReadPath(fileTargetData) is { Length: > 0 } readPath)
            {
                return await CollectFromEventFileAsync(connectionString, readPath, state, cancellationToken);
            }

            if (targets.TryGetValue("ring_buffer", out var ringTargetData))
            {
                var result = ShredRingBufferTargetData(ringTargetData, state);

                // Only a session DBA Dash created may be emptied - stopping someone else's session would
                // throw away data its owner is relying on - and only when it holds something, so an idle
                // database is never stopped and started for nothing.  SeenHashes is everything the read
                // found, new or not, which is what decides whether the next read would be an expensive one.
                if (flushRingBuffer && manageSession && state.SeenHashes.Count > 0)
                {
                    await FlushRingBufferAsync(connectionString, sessionName, databaseScoped, state,
                        cancellationToken);
                }
                return result;
            }

            throw new Exception(
                $"Extended events session '{sessionName}' has no readable event stream - it has " +
                $"{string.Join(", ", targets.Keys)}, and only event_file and ring_buffer carry one.  Add one " +
                "of those targets, or point the collection at a session that has one.");
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
        private static async Task<Result> CollectFromEventFileAsync(string connectionString, string readPath,
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

            return ShredEvents(eventXml);
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
            return Build(eventXml.SelectMany(ParseEvent).ToList());
        }

        /// <summary>
        /// Shreds a ring_buffer target's <c>target_data</c>, suppressing what the previous read of the same
        /// buffer already returned.  Reads only - the buffer is never flushed.
        /// </summary>
        public static Result ShredRingBufferTargetData(string targetDataXml, DeadlockCollectionState state)
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

            return Build(graphs);
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

            // Stored as DATETIME2(3); truncate here so the value that goes into the key is the value we
            // matched on, rather than something SQL Server rounds afterwards.
            eventTime = new DateTime(parsed.Ticks - parsed.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);
            return true;
        }

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
