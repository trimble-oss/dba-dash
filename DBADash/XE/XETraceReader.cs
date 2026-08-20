using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace DBADash.XE
{
    /// <summary>One raw event row as returned by <c>sys.fn_xe_file_target_read_file</c>.</summary>
    public readonly struct RawXEvent
    {
        public RawXEvent(string fileName, long offset, string eventData)
        {
            FileName = fileName;
            Offset = offset;
            EventData = eventData;
        }

        public string FileName { get; }
        public long Offset { get; }
        public string EventData { get; }
    }

    /// <summary>
    /// Resume position for the event_file target.  <c>sys.fn_xe_file_target_read_file</c> returns events at buffer
    /// (offset) granularity, so a single offset can hold several events.  We remember the last file/offset and how
    /// many events at that offset we already emitted, so on the next read we can skip exactly those and avoid
    /// duplicating the boundary buffer.
    /// </summary>
    public readonly struct FileTargetCursor
    {
        public static readonly FileTargetCursor None = default;

        public FileTargetCursor(string fileName, long offset, int consumedAtOffset)
        {
            FileName = fileName;
            Offset = offset;
            ConsumedAtOffset = consumedAtOffset;
        }

        public string FileName { get; }
        public long Offset { get; }
        public int ConsumedAtOffset { get; }

        public bool HasValue => FileName != null;
    }

    /// <summary>
    /// Pure cursor logic for the event_file target - no DB, fully unit-testable.  Given the rows returned by a
    /// read and the cursor from the previous read, it returns the genuinely new events and the next cursor.
    /// </summary>
    public static class FileTargetCursorReader
    {
        public static (List<RawXEvent> NewEvents, FileTargetCursor Cursor) Apply(
            IReadOnlyList<RawXEvent> rows, FileTargetCursor prior)
        {
            if (rows == null || rows.Count == 0)
            {
                return (new List<RawXEvent>(), prior);
            }

            // Skip the events at the prior boundary (file+offset) that we already emitted last time.  The read is
            // requested from that exact offset, so the boundary events come back again first.
            var skipRemaining = prior.HasValue ? prior.ConsumedAtOffset : 0;
            var newEvents = new List<RawXEvent>(rows.Count);
            foreach (var row in rows)
            {
                if (skipRemaining > 0 && prior.HasValue &&
                    row.FileName == prior.FileName && row.Offset == prior.Offset)
                {
                    skipRemaining--;
                    continue;
                }
                newEvents.Add(row);
            }

            // Next cursor points at the final offset of this read, counting every row that shares it - so next
            // time we skip precisely the events we've now consumed there.
            var last = rows[rows.Count - 1];
            var consumedAtLast = 0;
            foreach (var row in rows)
            {
                if (row.FileName == last.FileName && row.Offset == last.Offset)
                {
                    consumedAtLast++;
                }
            }

            return (newEvents, new FileTargetCursor(last.FileName, last.Offset, consumedAtLast));
        }
    }

    /// <summary>
    /// Shreds XE event XML into a <see cref="DataTable"/> whose columns are built <b>dynamically</b> from whatever
    /// data/action fields each event carries.  The ad-hoc trace schema is not fixed - different events
    /// (rpc_completed, error_reported, *_statement_completed, compilation, showplan, ...) expose different fields,
    /// and all of them are captured as columns without a predefined schema.  Fields in <see cref="NumericFields"/>
    /// are typed numeric (so the grid sorts them as numbers); everything else is a string, including context_info
    /// (kept as its hex text rather than binary, so it renders normally).
    /// </summary>
    public static class XETraceShredder
    {
        internal static readonly HashSet<string> NumericFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "duration", "cpu_time", "logical_reads", "physical_reads", "writes", "row_count", "error_number",
            "severity", "state", "session_id", "database_id", "source_database_id", "object_id", "line_number",
            "offset", "offset_end", "nest_level", "attention_count", "spills", "granted_memory_kb",
            "used_memory_kb", "estimated_rows", "actual_rows"
        };

        /// <summary>Shreds event_file rows (each <see cref="RawXEvent.EventData"/> is a single &lt;event&gt;).</summary>
        public static DataTable Shred(IEnumerable<RawXEvent> events)
        {
            var elements = new List<XElement>();
            if (events != null)
            {
                foreach (var e in events)
                {
                    if (string.IsNullOrEmpty(e.EventData)) continue;
                    try { elements.Add(XElement.Parse(e.EventData)); }
                    catch { /* skip malformed */ }
                }
            }
            return ShredEvents(elements);
        }

        /// <summary>Shreds a ring_buffer target_data blob (&lt;RingBufferTarget&gt; with &lt;event&gt; children).</summary>
        public static DataTable ShredRingBuffer(string targetDataXml)
        {
            if (string.IsNullOrEmpty(targetDataXml)) return ShredEvents(Enumerable.Empty<XElement>());
            XElement root;
            try { root = XElement.Parse(targetDataXml); }
            catch { return ShredEvents(Enumerable.Empty<XElement>()); }
            return ShredEvents(root.Elements("event"));
        }

        /// <summary>Shreds a pre-parsed collection of &lt;event&gt; elements (used by the non-destructive watch reader).</summary>
        public static DataTable ShredElements(IEnumerable<XElement> eventElements) =>
            ShredEvents(eventElements ?? Enumerable.Empty<XElement>());

        private static DataTable ShredEvents(IEnumerable<XElement> eventElements)
        {
            var dt = new DataTable("XE");
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));

            foreach (var evt in eventElements)
            {
                var row = dt.NewRow();
                row["event_type"] = evt.Attribute("name")?.Value ?? string.Empty;
                var ts = evt.Attribute("timestamp")?.Value;
                // Every XE event carries a non-nullable timestamp_utc; only malformed/unparseable XML lands here.
                // Fall back to now (UTC) so the promoted timestamp - used as the DB partition key - is never null.
                row["timestamp"] = ts != null && DateTime.TryParse(ts, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var tsUtc)
                    ? tsUtc
                    : DateTime.UtcNow;

                foreach (var field in evt.Elements("data").Concat(evt.Elements("action")))
                {
                    var name = field.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(name) || name is "event_type" or "timestamp") continue;
                    SetField(dt, row, name, field);
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static void SetField(DataTable dt, DataRow row, string name, XElement field)
        {
            var numeric = NumericFields.Contains(name);
            if (!dt.Columns.Contains(name))
            {
                dt.Columns.Add(name, numeric ? typeof(long) : typeof(string));
            }

            var valueEl = field.Element("value");
            // An xml-typed field (showplan_xml, query_plan, tsql_frame/stack, ...) embeds nested XML elements inside
            // <value> rather than text.  XElement.Value concatenates only descendant text nodes, so for a plan it
            // returns (near) empty and the column shows blank.  When the value holds child elements, serialize the
            // inner XML instead so the full plan/markup is preserved.
            var raw = valueEl == null
                ? field.Value
                : valueEl.HasElements
                    ? string.Concat(valueEl.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)))
                    : valueEl.Value;

            if (numeric)
            {
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    row[name] = n;
                }
                return;
            }

            // Some fields carry a friendly <text> alongside the raw <value> (e.g. result, category) - keep both.
            var textEl = field.Element("text");
            row[name] = textEl != null && !string.IsNullOrEmpty(textEl.Value) && textEl.Value != raw
                ? (string.IsNullOrEmpty(raw) ? textEl.Value : $"{raw} - {textEl.Value}")
                : raw ?? string.Empty;
        }
    }

    /// <summary>Reads batches of events from a running ad-hoc trace.  One implementation per target type.</summary>
    public interface IXETraceReader
    {
        /// <summary>Reads the events captured since the previous call.  Returns an empty (schema-only) table when none.</summary>
        Task<DataTable> ReadNextAsync(CancellationToken cancellationToken);

        /// <summary>Diagnostics: wall-clock of the DB read in the most recent <see cref="ReadNextAsync"/> call.</summary>
        long LastReadMilliseconds { get; }

        /// <summary>Diagnostics: wall-clock of the XML shred in the most recent <see cref="ReadNextAsync"/> call.</summary>
        long LastShredMilliseconds { get; }
    }

    /// <summary>
    /// event_file reader.  Holds the offset cursor between calls so the running session is never stopped - no
    /// flush, no event loss.  <paramref name="readPath"/> is the wildcard path (e.g. <c>...\DBADash_AdHoc*.xel</c>)
    /// because SQL Server appends a partition/timestamp suffix to the configured filename and rolls files over.
    /// </summary>
    public sealed class EventFileTraceReader : IXETraceReader
    {
        private readonly string _connectionString;
        private readonly string _readPath;
        private readonly int _maxEvents;
        private readonly bool _newestFirst;
        private FileTargetCursor _cursor = FileTargetCursor.None;

        public EventFileTraceReader(string connectionString, string readPath)
            : this(connectionString, readPath, FileTargetCursor.None, 0)
        {
        }

        /// <summary>
        /// <paramref name="initialCursor"/> seeds the read position - pass the end of any pre-existing data so a
        /// leftover file from a previous trace is skipped rather than re-read.  <see cref="FileTargetCursor.None"/>
        /// reads from the start.
        /// </summary>
        public EventFileTraceReader(string connectionString, string readPath, FileTargetCursor initialCursor)
            : this(connectionString, readPath, initialCursor, 0)
        {
        }

        /// <summary>
        /// <paramref name="maxEvents"/> caps the number of rows returned per read via <c>TOP</c> (0 = uncapped) - used
        /// by the one-shot "view existing data" read so a large event_file can't return an unbounded resultset.  When
        /// <paramref name="newestFirst"/> is set the cap yields the <b>newest</b> events (the SQL orders by file/offset
        /// descending, so the whole file is scanned but only <paramref name="maxEvents"/> rows come back) and
        /// <see cref="ReadNextAsync"/> reverses them so the returned batch is still in chronological order; otherwise
        /// (the incremental trace/watch readers) the cap has no ORDER BY and short-circuits on the oldest rows.
        /// </summary>
        public EventFileTraceReader(string connectionString, string readPath, FileTargetCursor initialCursor,
            int maxEvents, bool newestFirst = false)
        {
            _connectionString = connectionString;
            _readPath = readPath;
            _cursor = initialCursor;
            _maxEvents = maxEvents > 0 ? maxEvents : 0;
            _newestFirst = newestFirst;
        }

        public long LastReadMilliseconds { get; private set; }
        public long LastShredMilliseconds { get; private set; }

        public async Task<DataTable> ReadNextAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var rows = await ReadRawAsync(cancellationToken);
            LastReadMilliseconds = sw.ElapsedMilliseconds;

            sw.Restart();
            var (newEvents, cursor) = FileTargetCursorReader.Apply(rows, _cursor);
            _cursor = cursor;
            // A newest-first read fetches rows in descending file order; flip them back to chronological so the batch
            // reads oldest -> newest like every other read (the caller ignores the cursor for this one-shot mode).
            if (_newestFirst) newEvents.Reverse();
            var dt = XETraceShredder.Shred(newEvents);
            LastShredMilliseconds = sw.ElapsedMilliseconds;
            return dt;
        }

        private async Task<List<RawXEvent>> ReadRawAsync(CancellationToken cancellationToken)
        {
            var rows = new List<RawXEvent>();
            var top = _maxEvents > 0 ? "TOP (@max) " : string.Empty;
            // Newest-first needs an explicit order (file_name then file_offset are monotonic with time, matching the
            // watch's end-cursor scan); the incremental readers stay unordered so their TOP can short-circuit.
            var orderBy = _newestFirst ? " ORDER BY file_name DESC, file_offset DESC" : string.Empty;
            var sql =
                $"SELECT {top}file_name, file_offset, CAST(event_data AS NVARCHAR(MAX)) AS event_data " +
                "FROM sys.fn_xe_file_target_read_file(@path, NULL, @initFile, @initOffset)" + orderBy;
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            if (_maxEvents > 0) cmd.Parameters.Add("@max", SqlDbType.Int).Value = _maxEvents;
            cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = _readPath;
            cmd.Parameters.Add("@initFile", SqlDbType.NVarChar, 260).Value =
                _cursor.HasValue ? _cursor.FileName : (object)DBNull.Value;
            cmd.Parameters.Add("@initOffset", SqlDbType.BigInt).Value =
                _cursor.HasValue ? _cursor.Offset : (object)DBNull.Value;

            await cn.OpenAsync(cancellationToken);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                rows.Add(new RawXEvent(
                    rdr.GetString(0),
                    rdr.GetInt64(1),
                    rdr.IsDBNull(2) ? null : rdr.GetString(2)));
            }
            return rows;
        }
    }

    /// <summary>
    /// ring_buffer reader (the Azure SQL DB / no-writable-disk fallback).  There is no efficient way to skip data
    /// already captured, so each read returns the whole ring buffer and then flushes it with a stop/start.  The
    /// flush drops any events dispatched during the brief stop window - the accepted trade-off for this target.
    /// </summary>
    public sealed class RingBufferTraceReader : IXETraceReader
    {
        private readonly string _connectionString;
        private readonly string _sessionName;
        private readonly bool _databaseScoped;

        public RingBufferTraceReader(string connectionString, string sessionName, bool databaseScoped)
        {
            if (!IsSafeSessionName(sessionName))
            {
                throw new ArgumentException($"Invalid session name: '{sessionName}'.", nameof(sessionName));
            }
            _connectionString = connectionString;
            _sessionName = sessionName;
            _databaseScoped = databaseScoped;
        }

        public long LastReadMilliseconds { get; private set; }
        public long LastShredMilliseconds { get; private set; }

        public async Task<DataTable> ReadNextAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var targetData = await ReadTargetDataAsync(cancellationToken);
            LastReadMilliseconds = sw.ElapsedMilliseconds;

            sw.Restart();
            var dt = XETraceShredder.ShredRingBuffer(targetData);
            LastShredMilliseconds = sw.ElapsedMilliseconds;

            await FlushAsync(cancellationToken);
            return dt;
        }

        private async Task<string> ReadTargetDataAsync(CancellationToken cancellationToken)
        {
            var sessions = _databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
            var targets = _databaseScoped ? "sys.dm_xe_database_session_targets" : "sys.dm_xe_session_targets";
            var sql =
                $"SELECT CAST(t.target_data AS NVARCHAR(MAX)) FROM {sessions} s " +
                $"JOIN {targets} t ON t.event_session_address = s.address " +
                "WHERE s.name = @name AND t.target_name = 'ring_buffer'";

            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = _sessionName;
            await cn.OpenAsync(cancellationToken);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result == null || result == DBNull.Value ? null : (string)result;
        }

        private async Task FlushAsync(CancellationToken cancellationToken)
        {
            var scope = _databaseScoped ? "ON DATABASE" : "ON SERVER";
            var name = _sessionName.Replace("]", "]]");
            var sql =
                $"ALTER EVENT SESSION [{name}] {scope} STATE = STOP;" +
                $"ALTER EVENT SESSION [{name}] {scope} STATE = START;";
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            await cn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // The name is inlined into ALTER EVENT SESSION (which can't take a variable), so it is validated the same
        // way XETraceDefinition validates it before ever being executed.
        private static bool IsSafeSessionName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 100)
            {
                return false;
            }
            foreach (var c in name)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Pure diff logic for the non-destructive ring_buffer <b>watch</b>: given a target_data blob and the set of
    /// event hashes seen on the previous read, returns the genuinely new events and the new "seen" set.  No DB, fully
    /// unit-testable.  The ring buffer has no per-event id, so events are identified by the (disable-formatting) XML
    /// of each &lt;event&gt; element; two byte-identical events across reads collide (rare) - the accepted trade-off
    /// for a tail that never flushes the user's session.
    /// </summary>
    public static class RingBufferWatchDiff
    {
        public static (List<XElement> NewEvents, HashSet<string> Seen) Apply(string targetDataXml, ISet<string> priorSeen)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var fresh = new List<XElement>();
            if (string.IsNullOrEmpty(targetDataXml)) return (fresh, seen);

            XElement root;
            try { root = XElement.Parse(targetDataXml); }
            catch { return (fresh, seen); }

            foreach (var e in root.Elements("event"))
            {
                var hash = e.ToString(SaveOptions.DisableFormatting);
                seen.Add(hash);
                if (priorSeen == null || !priorSeen.Contains(hash)) fresh.Add(e);
            }
            return (fresh, seen);
        }
    }

    /// <summary>
    /// ring_buffer reader for <b>watching an existing</b> session.  Reads target_data on each call like
    /// <see cref="RingBufferTraceReader"/> but NEVER flushes (no stop/start) - the user's session is left running
    /// untouched.  New events are detected by diffing against the previous read (see <see cref="RingBufferWatchDiff"/>).
    /// The read is fully parameterised (the session name is a @param, never inlined into DDL), so no name validation
    /// is required.
    /// </summary>
    public sealed class WatchRingBufferReader : IXETraceReader
    {
        private readonly string _connectionString;
        private readonly string _sessionName;
        private readonly bool _databaseScoped;
        private HashSet<string> _seen = new(StringComparer.Ordinal);

        public WatchRingBufferReader(string connectionString, string sessionName, bool databaseScoped)
        {
            _connectionString = connectionString;
            _sessionName = sessionName;
            _databaseScoped = databaseScoped;
        }

        public long LastReadMilliseconds { get; private set; }
        public long LastShredMilliseconds { get; private set; }

        public async Task<DataTable> ReadNextAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var targetData = await ReadTargetDataAsync(cancellationToken);
            LastReadMilliseconds = sw.ElapsedMilliseconds;

            sw.Restart();
            var (fresh, seen) = RingBufferWatchDiff.Apply(targetData, _seen);
            // Only advance the seen-set when we actually read target data.  A null/empty poll (e.g. the session
            // briefly failing to resolve) must not clear it, or the next non-empty read would re-emit the whole
            // buffer as "new".  Current callers break out on a session-gone check so this is belt-and-braces.
            if (!string.IsNullOrEmpty(targetData)) _seen = seen;
            var dt = XETraceShredder.ShredElements(fresh);
            LastShredMilliseconds = sw.ElapsedMilliseconds;
            return dt;
        }

        private async Task<string> ReadTargetDataAsync(CancellationToken cancellationToken)
        {
            var sessions = _databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
            var targets = _databaseScoped ? "sys.dm_xe_database_session_targets" : "sys.dm_xe_session_targets";
            var sql =
                $"SELECT CAST(t.target_data AS NVARCHAR(MAX)) FROM {sessions} s " +
                $"JOIN {targets} t ON t.event_session_address = s.address " +
                "WHERE s.name = @name AND t.target_name = 'ring_buffer'";

            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = _sessionName;
            await cn.OpenAsync(cancellationToken);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result == null || result == DBNull.Value ? null : (string)result;
        }
    }
}
