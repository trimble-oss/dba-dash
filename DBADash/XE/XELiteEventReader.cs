using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.XEvent.XELite;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.XE
{
    /// <summary>
    /// Builds a dynamic-schema <see cref="DataTable"/> from XELite <see cref="IXEvent"/> objects, matching the column
    /// conventions of <see cref="XETraceShredder"/> (event_type + timestamp always, a column per data/action field,
    /// numeric fields typed <c>long</c> so the grid sorts them).  XELite has already decoded the binary into typed
    /// values, so unlike the XML shredder there is no per-event <c>XElement.Parse</c> - the expensive step the
    /// <c>fn_xe_file_target_read_file</c> path pays.
    /// </summary>
    public static class XELiteShredder
    {
        public static DataTable Build(IEnumerable<IXEvent> events)
        {
            var dt = new DataTable("XE");
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));

            if (events == null) return dt;

            foreach (var ev in events)
            {
                if (ev == null) continue;
                var row = dt.NewRow();
                row["event_type"] = ev.Name ?? string.Empty;
                row["timestamp"] = ev.Timestamp.UtcDateTime;

                if (ev.Fields != null)
                {
                    foreach (var kv in ev.Fields) SetField(dt, row, kv.Key, kv.Value);
                }
                if (ev.Actions != null)
                {
                    foreach (var kv in ev.Actions) SetField(dt, row, kv.Key, kv.Value);
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static void SetField(DataTable dt, DataRow row, string name, object value)
        {
            if (string.IsNullOrEmpty(name) || name is "event_type" or "timestamp") return;

            var numeric = XETraceShredder.NumericFields.Contains(name);
            if (!dt.Columns.Contains(name))
            {
                dt.Columns.Add(name, numeric ? typeof(long) : typeof(string));
            }

            if (numeric)
            {
                try { if (value != null) row[name] = Convert.ToInt64(value, CultureInfo.InvariantCulture); }
                catch { /* non-integer value for a nominally numeric field - leave null */ }
                return;
            }

            row[name] = value switch
            {
                null => string.Empty,
                byte[] bytes => "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty),
                string s => s,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }

    /// <summary>
    /// Fast event_file reader that bypasses <c>fn_xe_file_target_read_file</c> (whose per-event binary→XML conversion
    /// dominates the load time).  Instead it enumerates the session's rollover files server-side
    /// (<c>sys.dm_os_enumerate_filesystem</c>), pulls the raw <c>.xel</c> bytes of the newest file(s) via
    /// <c>OPENROWSET(BULK …, SINGLE_BLOB)</c>, and parses them with XELite straight from a <see cref="MemoryStream"/> -
    /// the same native binary path SSMS uses.  Reads newest files first and stops once it has the cap's worth, so
    /// older files are skipped.
    ///
    /// <para>Needs <c>ADMINISTER BULK OPERATIONS</c> (for OPENROWSET) and works only against files reachable by the
    /// engine (not Azure SQL DB).  The caller is expected to fall back to the <c>fn_xe_file_target_read_file</c> path
    /// on any failure (missing permission, older SQL without the DMF, a locked file, …).</para>
    /// </summary>
    public sealed class XELiteEventFileReader
    {
        private readonly string _connectionString;
        private readonly string _currentFilePath;
        private readonly int _maxEvents;
        private readonly DateTime? _startUtc;

        public long EnumerateMilliseconds { get; private set; }
        public long BytesReadMilliseconds { get; private set; }
        public long ParseMilliseconds { get; private set; }
        public long TvfMilliseconds { get; private set; }
        public int XELiteFilesRead { get; private set; }
        public int TvfFilesRead { get; private set; }
        public long BytesRead { get; private set; }

        /// <summary>True when the fast <c>sys.fn_MSxe_read_event_stream</c> path served the read (the SSMS approach).</summary>
        public bool UsedEventStream { get; private set; }

        /// <param name="maxEvents">Upper bound on events returned (0 = uncapped).</param>
        /// <param name="startUtc">Optional inclusive lower bound on event time (UTC).  When set, the fn_MSxe path reads
        /// the current file first and only widens to the whole file set if that file doesn't reach back this far -
        /// so a recent range (e.g. last day) reads far less than the entire target.  The read is always newest-anchored
        /// (there is no upper bound); it returns the newest events at or after this bound.</param>
        public XELiteEventFileReader(string connectionString, string currentFilePath, int maxEvents,
            DateTime? startUtc = null)
        {
            _connectionString = connectionString;
            _currentFilePath = currentFilePath;
            _maxEvents = maxEvents > 0 ? maxEvents : 0;
            _startUtc = startUtc;
        }

        /// <summary>
        /// Reads the newest events across the session's rollover files and returns them shredded into a
        /// <see cref="DataTable"/> in chronological (oldest -&gt; newest) order, capped at the constructor's maxEvents.
        /// <para>The session's <b>active</b> file is locked against <c>OPENROWSET BULK</c> (the engine holds it open),
        /// so it's read via the TVF (<see cref="EventFileTraceReader"/>, which opens it shared) while the closed
        /// rollover files are read as raw binary through XELite.  Newest files are read first and older files skipped
        /// once the cap is met.</para>
        /// Throws only when the rollover files can't be enumerated (older SQL without the DMF), so the caller falls
        /// back to the full TVF path; a per-file OPENROWSET failure just reads that one file via the TVF instead.
        /// </summary>
        public async Task<DataTable> ReadNewestAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                throw new InvalidOperationException("No current event_file path was supplied for the XELite read.");
            }

            // Fast path - what SSMS does: sys.fn_MSxe_read_event_stream streams the raw binary of the session's files
            // (INCLUDING the active one) in one call.  No directory enumeration, no OPENROWSET (so no bulk permission
            // and no active-file lock), no per-event XML conversion.  It returns the LIVE buffer format, which we parse
            // via XELite's internal live-buffer reader (see XELiveStreamShredder).  On any failure we fall through to
            // the OPENROWSET/TVF hybrid.
            try
            {
                var streamed = await ReadEventStreamScopedAsync(ct);
                UsedEventStream = true;
                return streamed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Debug(ex, "fn_MSxe_read_event_stream path failed for {file}; using the OPENROWSET/TVF hybrid",
                    _currentFilePath);
            }

            var readCap = _maxEvents > 0 ? _maxEvents + 1 : 0;

            var merged = new DataTable("XE");

            // 1) The active file (known name) holds the newest events but is locked to OPENROWSET, so read it via the
            //    TVF (which opens it shared).  Reading it FIRST means we can skip the directory enumeration entirely
            //    when it already holds the cap's worth - enumeration turned out to be expensive (statting the LOG dir).
            merged.Merge(await ReadFileViaTvfAsync(_currentFilePath, readCap, ct), false, MissingSchemaAction.Add);

            // 2) Need more than the active file has (or uncapped): enumerate the closed rollover files, newest-first,
            //    and read them as fast binary via XELite until we reach the cap.
            if (readCap == 0 || merged.Rows.Count < readCap)
            {
                var (directory, pattern) = SplitPath(_currentFilePath);
                var sw = Stopwatch.StartNew();
                var files = await EnumerateRolloverFilesAsync(directory, pattern, ct);
                EnumerateMilliseconds = sw.ElapsedMilliseconds;

                foreach (var file in files)
                {
                    // Already read via the TVF above.  Match on the full path OR just the filename: a session's
                    // rollover files all live in one directory with distinct names, so the filename alone uniquely
                    // identifies the active file even when the two path representations differ (8.3 short path, UNC vs
                    // local, slash direction) - which would otherwise read the active file twice and duplicate events.
                    if (SamePath(file, _currentFilePath) || SameFileName(file, _currentFilePath)) continue;

                    DataTable table;
                    try
                    {
                        table = await ReadClosedFileViaXELiteAsync(file, ct);
                    }
                    catch (SqlException ex)
                    {
                        // e.g. a rollover made this the active (locked) file, OPENROWSET isn't permitted, or a transient
                        // FS error - the TVF can still read it.  (Enumeration errors bubble up so the caller uses the
                        // full TVF fallback.)
                        Log.Debug(ex, "OPENROWSET failed for {file}; reading it via fn_xe_file_target_read_file", file);
                        table = await ReadFileViaTvfAsync(file, readCap, ct);
                    }

                    merged.Merge(table, false, MissingSchemaAction.Add);
                    if (readCap > 0 && merged.Rows.Count >= readCap) break;
                }
            }

            return TrimNewest(merged, readCap);
        }

        /// <summary>
        /// SSMS's read path via <c>sys.fn_MSxe_read_event_stream</c>, with optional date-range file scoping.  With no
        /// range it reads the whole logical file set (SSMS's filename-only pattern).  With a range it reads the
        /// <b>current</b> (newest) file first and only widens to the whole set when that file doesn't reach back to the
        /// range start - so a recent range (e.g. last day, usually within the current file) reads far less than the
        /// entire target.  The events are then filtered to the range and capped to the newest <c>maxEvents</c>.
        /// </summary>
        private async Task<DataTable> ReadEventStreamScopedAsync(CancellationToken ct)
        {
            List<IXEvent> events;
            if (_startUtc == null)
            {
                events = await ReadEventStreamRawAsync(BuildEventStreamSource(_currentFilePath), ct);
            }
            else
            {
                // Read only the current file first; widen to the full set if it doesn't reach the range start.
                events = await ReadEventStreamRawAsync(FileNameOnly(_currentFilePath), ct);
                if (MinUtc(events) > _startUtc.Value)
                {
                    events = await ReadEventStreamRawAsync(BuildEventStreamSource(_currentFilePath), ct);
                }

                var start = _startUtc.Value;
                events = events.Where(e => e.Timestamp.UtcDateTime >= start).ToList();
            }

            events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            var readCap = _maxEvents > 0 ? _maxEvents + 1 : 0;
            var take = readCap > 0 ? Math.Min(readCap, events.Count) : events.Count;
            var newest = take == events.Count ? events : events.GetRange(events.Count - take, take);
            return XELiteShredder.Build(newest);
        }

        /// <summary>
        /// Reads and parses one <c>fn_MSxe_read_event_stream</c> source (a filename pattern or exact filename) into
        /// events (unsorted, unfiltered).  Throws if the function is unavailable/blocked, the XELite internals can't be
        /// reached, or nothing parses - so the caller uses the hybrid fallback.
        /// </summary>
        private async Task<List<IXEvent>> ReadEventStreamRawAsync(string source, CancellationToken ct)
        {
            const string sql = "SELECT type, data FROM sys.fn_MSxe_read_event_stream(@source, @opt)";

            var sw = Stopwatch.StartNew();
            var rows = new List<(int Type, byte[] Data)>();
            await using (var cn = new SqlConnection(_connectionString))
            await using (var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text })
            {
                cmd.Parameters.Add("@source", SqlDbType.NVarChar, 256).Value = source;
                cmd.Parameters.Add("@opt", SqlDbType.Int).Value = 1; // 1 = read from files (0 = live session)
                await cn.OpenAsync(ct);
                await using var rdr = await cmd.ExecuteReaderAsync(ct);
                while (await rdr.ReadAsync(ct))
                {
                    if (rdr.IsDBNull(1)) continue;
                    var type = rdr.IsDBNull(0) ? -1 : Convert.ToInt32(rdr.GetValue(0));
                    var data = (byte[])rdr.GetValue(1);
                    rows.Add((type, data));
                    BytesRead += data.Length;
                }
            }
            BytesReadMilliseconds += sw.ElapsedMilliseconds;
            if (rows.Count == 0)
            {
                throw new InvalidOperationException("sys.fn_MSxe_read_event_stream returned no data for " + source);
            }

            sw.Restart();
            var events = new List<IXEvent>();
            await XELiveStreamShredder.ParseRowsAsync(rows, ev => events.Add(ev), ct);
            ParseMilliseconds += sw.ElapsedMilliseconds;
            if (events.Count == 0)
            {
                throw new InvalidOperationException("sys.fn_MSxe_read_event_stream produced no parseable events.");
            }

            DateTime min = DateTime.MaxValue, max = DateTime.MinValue;
            foreach (var e in events)
            {
                var t = e.Timestamp.UtcDateTime;
                if (t < min) min = t;
                if (t > max) max = t;
            }
            Log.Information(
                "fn_MSxe {source}: {rows} buffer rows, {events} events, ts {minTs:yyyy-MM-dd HH:mm:ss}..{maxTs:yyyy-MM-dd HH:mm:ss} (UTC)",
                source, rows.Count, events.Count, min, max);
            return events;
        }

        private static DateTime MinUtc(List<IXEvent> events)
        {
            var min = DateTime.MaxValue;
            foreach (var e in events)
            {
                var t = e.Timestamp.UtcDateTime;
                if (t < min) min = t;
            }
            return min;
        }

        private static string FileNameOnly(string path)
        {
            var sep = path.LastIndexOfAny(new[] { '\\', '/' });
            return sep >= 0 ? path.Substring(sep + 1) : path;
        }

        /// <summary>Reads one closed rollover file's raw bytes (OPENROWSET) and shreds them with XELite.</summary>
        private async Task<DataTable> ReadClosedFileViaXELiteAsync(string file, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var bytes = await ReadFileBytesAsync(file, ct);
            BytesReadMilliseconds += sw.ElapsedMilliseconds;
            if (bytes == null || bytes.Length == 0) return XELiteShredder.Build(null);
            BytesRead += bytes.Length;
            XELiteFilesRead++;

            sw.Restart();
            var events = new List<IXEvent>();
            await using var ms = new MemoryStream(bytes, writable: false);
            var streamer = new XEFileEventStreamer(ms, false);
            await streamer.ReadEventStream(ev =>
            {
                events.Add(ev);
                return Task.CompletedTask;
            }, ct);
            var table = XELiteShredder.Build(events);
            ParseMilliseconds += sw.ElapsedMilliseconds;
            return table;
        }

        /// <summary>Reads one file via <c>fn_xe_file_target_read_file</c> (the only reader that opens the active file).</summary>
        private async Task<DataTable> ReadFileViaTvfAsync(string file, int readCap, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var reader = new EventFileTraceReader(_connectionString, file, FileTargetCursor.None, readCap, newestFirst: true);
            var table = await reader.ReadNextAsync(ct) ?? XELiteShredder.Build(null);
            TvfMilliseconds += sw.ElapsedMilliseconds;
            TvfFilesRead++;
            return table;
        }

        /// <summary>Sorts the merged events chronologically and keeps the newest <paramref name="readCap"/>.</summary>
        private static DataTable TrimNewest(DataTable merged, int readCap)
        {
            if (merged.Rows.Count == 0 || !merged.Columns.Contains("timestamp")) return merged;
            merged.DefaultView.Sort = "timestamp ASC";
            var sorted = merged.DefaultView.ToTable();
            sorted.TableName = "XE";
            if (readCap > 0 && sorted.Rows.Count > readCap)
            {
                var remove = sorted.Rows.Count - readCap;
                for (var i = 0; i < remove; i++) sorted.Rows.RemoveAt(0);
            }
            return sorted;
        }

        /// <summary>Case-insensitive path comparison (normalising slashes) to spot the known active file.</summary>
        private static bool SamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
            static string Normalize(string p) => p.Replace('/', '\\').TrimEnd('\\');
        }

        /// <summary>
        /// Case-insensitive filename comparison, ignoring the directory.  A backstop for <see cref="SamePath"/> when
        /// the two representations of the active file differ only in the directory portion.
        /// </summary>
        private static bool SameFileName(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(FileNameOnly(a), FileNameOnly(b), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lists the rollover files matching the session's stem, newest first (by last-write time), via the filesystem
        /// DMF.  Note: the DMF stats every file it returns, so it can be slow on a busy LOG directory.
        /// </summary>
        private async Task<List<string>> EnumerateRolloverFilesAsync(string directory, string pattern, CancellationToken ct)
        {
            const string sql =
                "SELECT full_filesystem_path " +
                "FROM sys.dm_os_enumerate_filesystem(@dir, @pattern) " +
                "WHERE is_directory = 0 " +
                "ORDER BY last_write_time DESC, full_filesystem_path DESC";

            var result = new List<string>();
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            cmd.Parameters.Add("@dir", SqlDbType.NVarChar, 260).Value = directory;
            cmd.Parameters.Add("@pattern", SqlDbType.NVarChar, 260).Value = pattern;
            await cn.OpenAsync(ct);
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                if (!rdr.IsDBNull(0)) result.Add(rdr.GetString(0));
            }
            return result;
        }

        /// <summary>Pulls a single .xel file's raw bytes over the connection via OPENROWSET BULK (SINGLE_BLOB).</summary>
        private async Task<byte[]> ReadFileBytesAsync(string filePath, CancellationToken ct)
        {
            // OPENROWSET BULK requires a literal path, so it cannot be parameterised.  The path comes from SQL Server's
            // own filesystem enumeration (not user input); still, double any single quotes defensively.
            var literal = filePath.Replace("'", "''");
            var sql = $"SELECT BulkColumn FROM OPENROWSET(BULK N'{literal}', SINGLE_BLOB) AS x";

            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            await cn.OpenAsync(ct);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result == null || result == DBNull.Value ? null : (byte[])result;
        }

        /// <summary>
        /// Builds the <c>fn_MSxe_read_event_stream</c> <c>@source</c> the way SSMS does: the FILENAME only (no
        /// directory), with the <c>_&lt;targetId&gt;_&lt;timestamp&gt;.xel</c> rollover suffix replaced by <c>_*.xel</c>.
        /// The engine resolves this against the session's file location; a full directory path made it return only an
        /// older subset.
        /// </summary>
        internal static string BuildEventStreamSource(string currentFilePath)
        {
            var sep = currentFilePath.LastIndexOfAny(new[] { '\\', '/' });
            var fileName = sep >= 0 ? currentFilePath.Substring(sep + 1) : currentFilePath;
            var pattern = Regex.Replace(fileName, @"_\d+_\d+\.xel$", "_*.xel");
            if (pattern == fileName && fileName.EndsWith(".xel", StringComparison.OrdinalIgnoreCase))
            {
                // No rollover suffix recognised - widen the stem so sibling rollovers still match.
                pattern = Regex.Replace(fileName, @"\.xel$", "*.xel");
            }
            return pattern;
        }

        /// <summary>Splits a full file path into (directory, filename-wildcard) for the filesystem DMF.</summary>
        internal static (string Directory, string Pattern) SplitPath(string fullPath)
        {
            var sep = fullPath.LastIndexOfAny(new[] { '\\', '/' });
            var directory = sep >= 0 ? fullPath.Substring(0, sep) : string.Empty;
            var fileName = sep >= 0 ? fullPath.Substring(sep + 1) : fullPath;
            // Strip the _<targetId>_<timestamp> rollover suffix to a wildcard so every rollover file matches.
            var pattern = Regex.Replace(fileName, @"_\d+_\d+\.xel$", "*.xel");
            if (pattern == fileName && fileName.EndsWith(".xel", StringComparison.OrdinalIgnoreCase))
            {
                // No rollover suffix recognised - widen to the stem so sibling rollovers still match.
                pattern = Regex.Replace(fileName, @"\.xel$", "*.xel");
            }
            return (directory, pattern);
        }
    }
}
