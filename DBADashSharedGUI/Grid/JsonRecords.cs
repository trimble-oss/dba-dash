using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Reads JSON of any shape into a <see cref="DataSet"/> for the grid viewer.  What the file holds is worked out by
    /// parsing it, not from its layout - whitespace and line breaks play no part:
    /// <list type="bullet">
    ///   <item>One array - a grid DBA Dash exported (<see cref="GridSerializer"/>), read as Newtonsoft's DataTable.  An
    ///   array it can't read that way - nested values, or a column whose type changes - is read as records instead.</item>
    ///   <item>One object whose every property is an array of objects - a DataSet DBA Dash exported, a table per
    ///   property.  Any other object is a single record.</item>
    ///   <item>More than one value - a sequence of records: JSON lines (.jsonl / .ndjson), a structured log, or JSON
    ///   values simply written one after another.</item>
    /// </list>
    /// Records become rows with a column per property, in the order each is first seen.  A property a record leaves
    /// out is NULL in it, a nested object or array is shown as its JSON, and each column's type is the narrowest every
    /// value in it fits - whole numbers, numbers, true/false or dates - else text.
    ///
    /// Records in Serilog's compact format (CLEF: every record has "@t" and "@m" or "@mt") are recognised and laid out
    /// as a log - see <see cref="IsCompactLog"/>.
    /// </summary>
    public static class JsonRecords
    {
        public const string RecordsTableName = "Records";
        public const string LogTableName = "Log";

        /// <summary>The column a record that isn't an object - a bare number, string or array - is shown in.</summary>
        public const string ValueColumnName = "Value";

        /// <summary>The column the text of a record that couldn't be read is shown in, with the error on the row.</summary>
        public const string UnreadableColumnName = "(Unreadable)";

        /// <summary>
        /// A table extended property: a one-line warning about how the table was loaded - here, that some records
        /// couldn't be read - for the viewer to show above the grid.
        /// </summary>
        public const string LoadWarningProperty = "LoadWarning";

        /// <summary>
        /// Reads the JSON <paramref name="open"/> returns.  It may be called a second time: when a sequence of records
        /// has one that isn't valid JSON - often the last of a log that's still being written - the records before it
        /// are kept, and the rest of the file is read again a line at a time, so what can be read still opens and what
        /// can't is shown with its error.
        /// </summary>
        public static DataSet Load(Func<TextReader> open)
        {
            var records = new RecordsBuilder();
            TextPosition resumeAt;
            using (var text = open())
            {
                var reader = CreateReader(text);
                if (!MoveToContent(reader)) return AsDataSet(new DataTable(RecordsTableName));

                // An error in the first value means the file isn't JSON - reported as it stands, not as a table of lines.
                var first = JToken.ReadFrom(reader);
                resumeAt = TextPosition.Of(reader);

                try
                {
                    if (!MoveToContent(reader)) return LoadSingle(first);

                    records.Add(first);
                    do
                    {
                        records.Add(JToken.ReadFrom(reader));
                        resumeAt = TextPosition.Of(reader);
                    } while (MoveToContent(reader));

                    return AsDataSet(records.ToTable());
                }
                catch (JsonReaderException)
                {
                    // The first value, followed by something that isn't JSON, is a record like any other.
                    if (records.Count == 0) records.Add(first);
                }
            }

            RecoverRecords(open, resumeAt, records);
            return AsDataSet(records.ToTable());
        }

        /// <summary>
        /// Strings are read as they're written: whether one is a date is decided per column (see InferType), so a
        /// message or property that happens to look like a date is still shown, and rendered, as written.
        /// </summary>
        private static JsonTextReader CreateReader(TextReader text) => new(text)
        {
            SupportMultipleContent = true,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            CloseInput = false
        };

        /// <summary>Moves to the start of the next value, past any comments.  False at the end.</summary>
        private static bool MoveToContent(JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.Comment) return true;
            }

            return false;
        }

        private static DataSet AsDataSet(DataTable table)
        {
            var ds = new DataSet();
            ds.Tables.Add(table);
            return ds;
        }

        /// <summary>A file holding a single JSON value: a DBA Dash grid or DataSet export, or one record.</summary>
        private static DataSet LoadSingle(JToken value)
        {
            switch (value)
            {
                case JArray array:
                    try
                    {
                        return AsDataSet(Deserialize<DataTable>(array) ?? new DataTable());
                    }
                    catch (Exception ex) when (IsConversionError(ex))
                    {
                        return AsDataSet(FromRecords(array));
                    }

                case JObject obj when IsDataSetShaped(obj):
                    try
                    {
                        return Deserialize<DataSet>(obj) ?? new DataSet();
                    }
                    catch (Exception ex) when (IsConversionError(ex))
                    {
                        return AsDataSet(FromRecords([obj]));
                    }

                default:
                    return AsDataSet(FromRecords([value]));
            }
        }

        /// <summary>
        /// An export read as Newtonsoft reads one from its text - with date strings as dates, which is how its
        /// DataTable reading gives a column the DateTime type.
        /// </summary>
        private static T Deserialize<T>(JToken token)
        {
            using var reader = new DateReadingTokenReader(token);
            return JsonSerializer.CreateDefault().Deserialize<T>(reader);
        }

        /// <summary>Reads a token as a JsonTextReader with its default date handling would have read its text.</summary>
        private sealed class DateReadingTokenReader(JToken token) : JTokenReader(token)
        {
            public override bool Read()
            {
                if (!base.Read()) return false;
                if (TokenType == JsonToken.String && TryParseDate((string)Value, out var date)) SetToken(JsonToken.Date, date, false);
                return true;
            }
        }

        /// <summary>
        /// A string Newtonsoft would read as a date by default - ISO 8601, or Microsoft's /Date(...)/ - as it would read
        /// it.  The cheap check first, as almost no strings are dates.
        /// </summary>
        private static bool TryParseDate(string value, out DateTime date)
        {
            date = default;
            if (value == null ||
                !((value.Length >= 10 && char.IsDigit(value[0]) && value[4] == '-') ||
                  value.StartsWith("/Date(", StringComparison.Ordinal)))
            {
                return false;
            }

            using var reader = new JsonTextReader(new StringReader(JsonConvert.ToString(value)))
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };
            if (!reader.Read() || reader.TokenType != JsonToken.Date || reader.Value is not DateTime parsed) return false;
            date = parsed;
            return true;
        }

        /// <summary>Newtonsoft's DataTable reading fails on nested values, and a type that changes down a column.</summary>
        private static bool IsConversionError(Exception ex) =>
            ex is JsonException or ArgumentException or InvalidCastException or FormatException or OverflowException;

        /// <summary>How Newtonsoft writes a DataSet: an object with an array of row objects per table.</summary>
        private static bool IsDataSetShaped(JObject obj) =>
            obj.Properties().All(p => p.Value is JArray rows && rows.All(r => r is JObject));

        private static DataTable FromRecords(IEnumerable<JToken> values)
        {
            var records = new RecordsBuilder();
            foreach (var value in values) records.Add(value);
            return records.ToTable();
        }

        /// <summary>Where the reader is in the text: its 1-based line, and how many characters of it it has read.</summary>
        private readonly record struct TextPosition(int Line, int Column)
        {
            public static TextPosition Of(JsonTextReader reader) => new(reader.LineNumber, reader.LinePosition);
        }

        /// <summary>
        /// Reads the rest of a sequence of records after one that isn't valid JSON - which the reader can't step past, as
        /// it can't tell where that one ends.  From the end of the last record read, values are read on as before.  At an
        /// error, the values that ended on an earlier line are kept, the text from there to the end of the error's line
        /// is kept as unreadable, and reading starts again at the next line - so the records after a bad one are found
        /// however they're laid out, a pretty-printed one over several lines too.  A value that ends on the error's own
        /// line goes with it: it's likely a piece of the bad record - "Route": catchall reads as the string "Route" first.
        /// Unreadable text with no record between is one row, with the first error on it.
        /// </summary>
        private static void RecoverRecords(Func<TextReader> open, TextPosition from, RecordsBuilder records)
        {
            // The rest of the file, from the line the last record ended on.  Only on this path, for a file with a fault.
            var lines = new List<string>();
            using (var text = open())
            {
                for (var n = 1; text.ReadLine() is { } line; n++)
                {
                    if (n >= from.Line) lines.Add(line);
                }
            }

            var unreadable = new StringBuilder();
            int firstUnreadable = 0, lastUnreadable = 0;
            string error = null;

            void Flush()
            {
                if (unreadable.Length == 0) return;
                var span = firstUnreadable == lastUnreadable ? $"Line {firstUnreadable}" : $"Lines {firstUnreadable}-{lastUnreadable}";
                records.AddUnreadable(unreadable.ToString(), $"{span}: {error}");
                unreadable.Clear();
            }

            // From (line, column) to the end of lastLine - indexes into lines.
            void AddUnreadable(int line, int column, int lastLine, string message)
            {
                for (var i = line; i <= lastLine; i++)
                {
                    var part = i == line ? lines[i][Math.Min(column, lines[i].Length)..] : lines[i];
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    if (unreadable.Length == 0)
                    {
                        firstUnreadable = from.Line + i;
                        error = message;
                    }
                    else
                    {
                        unreadable.AppendLine();
                    }

                    unreadable.Append(part);
                    lastUnreadable = from.Line + i;
                }
            }

            var (startLine, startColumn) = (0, from.Column);
            while (startLine < lines.Count)
            {
                var reader = CreateReader(new LinesReader(lines, startLine, startColumn));
                // Where each value ended: an index into lines, and the column after it.
                var read = new List<(JToken Value, int Line, int Column)>();
                try
                {
                    while (MoveToContent(reader))
                    {
                        var value = JToken.ReadFrom(reader);
                        var line = startLine + reader.LineNumber - 1;
                        read.Add((value, line, reader.LineNumber == 1 ? startColumn + reader.LinePosition : reader.LinePosition));
                    }

                    if (read.Count > 0) Flush();
                    foreach (var value in read) records.Add(value.Value);
                    break;
                }
                catch (JsonReaderException ex)
                {
                    var errorLine = Math.Min(lines.Count - 1, startLine + Math.Max(ex.LineNumber > 0 ? ex.LineNumber : reader.LineNumber, 1) - 1);
                    var kept = read.Where(r => r.Line < errorLine).ToList();
                    if (kept.Count > 0) Flush();
                    foreach (var value in kept) records.Add(value.Value);

                    var (line, column) = kept.Count > 0 ? (kept[^1].Line, kept[^1].Column) : (startLine, startColumn);
                    AddUnreadable(line, column, errorLine, ErrorMessage(ex));
                    (startLine, startColumn) = (errorLine + 1, 0);
                }
            }

            Flush();
        }

        /// <summary>The error, without its line and position - which are of the text read from, not the file.</summary>
        private static string ErrorMessage(JsonReaderException ex)
        {
            var at = ex.Message.LastIndexOf(", line ", StringComparison.Ordinal);
            return at > 0 ? ex.Message[..at] + "." : ex.Message;
        }

        /// <summary>Lines, from a position in one of them on, as text - read as it's needed rather than joined up.</summary>
        private sealed class LinesReader(List<string> lines, int line, int column) : TextReader
        {
            private int _line = line;
            private int _column = column;

            public override int Peek()
            {
                if (_line >= lines.Count) return -1;
                return _column < lines[_line].Length ? lines[_line][_column] : '\n';
            }

            public override int Read()
            {
                var c = Peek();
                if (c < 0) return -1;
                Advance(1);
                return c;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var n = 0;
                while (n < count && _line < lines.Count)
                {
                    var current = lines[_line];
                    if (_column < current.Length)
                    {
                        var take = Math.Min(count - n, current.Length - _column);
                        current.CopyTo(_column, buffer, index + n, take);
                        n += take;
                        Advance(take);
                    }
                    else
                    {
                        buffer[index + n++] = '\n';
                        Advance(1);
                    }
                }

                return n;
            }

            private void Advance(int count)
            {
                if (_column < lines[_line].Length)
                {
                    _column += count;
                }
                else
                {
                    _line++;
                    _column = 0;
                }
            }
        }

        /// <summary>A nested object or array, kept as its JSON - text in the grid, but not quoted in a log message.</summary>
        private sealed record JsonText(string Json)
        {
            public override string ToString() => Json;
        }

        /// <summary>
        /// A property that's in the record with a null value - shown as NULL like one that isn't there, but told apart
        /// from it when a message is rendered: {Name} becomes null for this, and stays {Name} for a missing one.
        /// </summary>
        private sealed class JsonNull
        {
            public static readonly JsonNull Instance = new();
        }

        private sealed class Row
        {
            public object[] Values = [];
            public string Unreadable;
            public string Error;
        }

        /// <summary>One output column: its name, and how its value is taken from a row.</summary>
        private sealed record ColumnPlan(string Name, Func<Row, object> Value);

        /// <summary>Collects records as rows, a column per property name in the order first seen.</summary>
        private sealed class RecordsBuilder
        {
            private readonly Dictionary<string, int> _bySource = new(StringComparer.Ordinal);
            private readonly List<string> _sources = new();
            private readonly List<Row> _rows = new();
            private int _valueColumn = -1;

            public int Count => _rows.Count;

            private int Column(string source)
            {
                if (_bySource.TryGetValue(source, out var index)) return index;
                index = _sources.Count;
                _sources.Add(source);
                _bySource.Add(source, index);
                return index;
            }

            public void Add(JToken record)
            {
                var row = new Row { Values = new object[_sources.Count] };
                if (record is JObject obj)
                {
                    foreach (var property in obj.Properties())
                    {
                        Set(row, Column(property.Name), Scalar(property.Value));
                    }
                }
                else
                {
                    if (_valueColumn < 0)
                    {
                        // Not through Column(): a property can have any name, including this one.
                        _valueColumn = _sources.Count;
                        _sources.Add(null);
                    }

                    Set(row, _valueColumn, Scalar(record));
                }

                _rows.Add(row);
            }

            public void AddUnreadable(string text, string error) =>
                _rows.Add(new Row { Unreadable = text, Error = error });

            private static void Set(Row row, int index, object value)
            {
                if (index >= row.Values.Length) Array.Resize(ref row.Values, index + 1);
                row.Values[index] = value;
            }

            private Func<Row, object> Getter(int index) =>
                row => index < row.Values.Length && row.Values[index] is not JsonNull ? row.Values[index] : null;

            private Func<Row, object> Getter(string source) =>
                _bySource.TryGetValue(source, out var index) ? Getter(index) : _ => null;

            public DataTable ToTable()
            {
                var unreadable = _rows.Any(r => r.Unreadable != null)
                    ? new ColumnPlan(UnreadableColumnName, r => r.Unreadable)
                    : null;

                return IsCompactLog() ? Build(LogTableName, LogColumns(), unreadable) : Build(RecordsTableName, RecordColumns(), unreadable);
            }

            private IEnumerable<ColumnPlan> RecordColumns() =>
                _sources.Select((source, index) => new ColumnPlan(source ?? ValueColumnName, Getter(index)));

            /// <summary>
            /// Serilog's compact JSON format (CLEF) - every record that could be read has a timestamp "@t" and a message,
            /// "@m" as written or "@mt", the template it's rendered from.  Both are required of the format, and "@t" has
            /// to be a date, so a file that only happens to use one of the names isn't taken for a log.
            /// </summary>
            private bool IsCompactLog()
            {
                var readable = _rows.Where(r => r.Unreadable == null).ToList();
                if (readable.Count == 0) return false;

                var t = Getter("@t");
                var m = Getter("@m");
                var mt = Getter("@mt");
                return readable.All(r => t(r) is string time && TryParseDate(time, out _) && (m(r) is string || mt(r) is string));
            }

            /// <summary>
            /// The log's own fields first - when, how severe and what - then the event's properties.  "@l" is left out of
            /// a record at the default level, Information.  A property whose name starts with @ is written with it
            /// doubled, so as not to be taken for one of the format's own; that's undone here.
            /// </summary>
            private IEnumerable<ColumnPlan> LogColumns()
            {
                var level = Getter("@l");
                var message = Getter("@m");
                var template = Getter("@mt");

                yield return new ColumnPlan("Timestamp", Getter("@t"));
                yield return new ColumnPlan("Level", r => level(r) ?? "Information");
                yield return new ColumnPlan("Message", r => message(r) ?? (template(r) is string mt ? RenderTemplate(mt, r) : null));
                if (_bySource.ContainsKey("@x")) yield return new ColumnPlan("Exception", Getter("@x"));

                var formatFields = new HashSet<string>(StringComparer.Ordinal) { "@t", "@l", "@m", "@mt", "@x", "@r" };
                var trailing = new List<ColumnPlan>();
                for (var i = 0; i < _sources.Count; i++)
                {
                    var source = _sources[i];
                    if (source != null && formatFields.Contains(source)) continue;

                    var name = source switch
                    {
                        null => ValueColumnName,
                        "@i" => "EventType",
                        "@tr" => "TraceId",
                        "@sp" => "SpanId",
                        _ when source.StartsWith("@@", StringComparison.Ordinal) => source[1..],
                        _ => source
                    };

                    var column = new ColumnPlan(name, Getter(i));
                    // The format's other fields - the event type, trace and span ids - after the event's own properties.
                    if (source != null && source.StartsWith('@') && !source.StartsWith("@@", StringComparison.Ordinal))
                        trailing.Add(column);
                    else
                        yield return column;
                }

                foreach (var column in trailing) yield return column;
            }

            /// <summary>
            /// Renders the event's template from its own properties - one another event has doesn't count, so a hole
            /// this event has no property for is left as it is.  Holes with a format use the event's "@r" renderings,
            /// as the program that wrote the log formatted them, in its culture and with its types.
            /// </summary>
            private string RenderTemplate(string template, Row row)
            {
                var renderings = Renderings(row);
                return MessageTemplate.Render(template, name =>
                {
                    if (!_bySource.TryGetValue(name, out var index) || index >= row.Values.Length) return (false, null);
                    var value = row.Values[index];
                    return value == null ? (false, null) : (true, value is JsonNull ? null : value);
                }, renderings);
            }

            /// <summary>The event's "@r" - a string per formatted hole, in the template's order - or null.</summary>
            private IReadOnlyList<string> Renderings(Row row)
            {
                if (!_bySource.TryGetValue("@r", out var index) || index >= row.Values.Length ||
                    row.Values[index] is not JsonText { Json: var json })
                {
                    return null;
                }

                try
                {
                    return JArray.Parse(json).Select(r => r.Type == JTokenType.String ? (string)r : null).ToList();
                }
                catch (JsonReaderException)
                {
                    return null;
                }
            }

            private DataTable Build(string tableName, IEnumerable<ColumnPlan> plan, ColumnPlan unreadable)
            {
                var columns = plan.ToList();
                if (unreadable != null) columns.Add(unreadable);

                var values = _rows.Select(row => columns.Select(c => c.Value(row)).ToArray()).ToList();

                var table = new DataTable(tableName);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                {
                    var index = i;
                    table.Columns.Add(UniqueName(columns[i].Name, names), InferType(values.Select(v => v[index])));
                }

                table.BeginLoadData();
                for (var r = 0; r < values.Count; r++)
                {
                    var cells = values[r];
                    for (var i = 0; i < cells.Length; i++) cells[i] = ToCell(cells[i], table.Columns[i].DataType);
                    var dataRow = table.Rows.Add(cells);
                    if (_rows[r].Error != null) dataRow.RowError = _rows[r].Error;
                }

                table.EndLoadData();

                var unreadableCount = _rows.Count(r => r.Unreadable != null);
                if (unreadableCount > 0)
                {
                    table.ExtendedProperties[LoadWarningProperty] = unreadableCount == 1
                        ? $"1 part of the file couldn't be read as JSON - see the {UnreadableColumnName} column."
                        : $"{unreadableCount:N0} parts of the file couldn't be read as JSON - see the {UnreadableColumnName} column.";
                }

                return table;
            }
        }

        /// <summary>The grid, like DataTable lookups, doesn't tell column names apart by case - a, A becomes a, A (2).</summary>
        private static string UniqueName(string name, HashSet<string> taken)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "(No column name)";
            var unique = name;
            for (var n = 2; !taken.Add(unique); n++) unique = $"{name} ({n})";
            return unique;
        }

        /// <summary>A JSON value as a cell value: long, double, bool, DateTime or string, or the JSON of a nested one.</summary>
        private static object Scalar(JToken token) => token switch
        {
            JValue { Type: JTokenType.Null or JTokenType.Undefined } => JsonNull.Instance,
            JValue { Value: long or double or bool or DateTime or string } v => v.Value,
            JValue v => System.Convert.ToString(v.Value, CultureInfo.InvariantCulture), // BigInteger, DateTimeOffset...
            _ => new JsonText(token.ToString(Formatting.None))
        };

        /// <summary>The narrowest of long, double, bool and DateTime every non-NULL value fits, else string.</summary>
        private static Type InferType(IEnumerable<object> values)
        {
            bool isLong = true, isDouble = true, isBool = true, isDate = true, any = false;
            foreach (var value in values)
            {
                if (value == null) continue;
                any = true;
                isLong &= value is long;
                isDouble &= value is long or double;
                isBool &= value is bool;
                isDate &= value is DateTime || (value is string s && TryParseDate(s, out _));
                if (!isLong && !isDouble && !isBool && !isDate) return typeof(string);
            }

            if (!any) return typeof(string);
            if (isLong) return typeof(long);
            if (isDouble) return typeof(double);
            if (isBool) return typeof(bool);
            return isDate ? typeof(DateTime) : typeof(string);
        }

        private static object ToCell(object value, Type type)
        {
            if (value == null) return DBNull.Value;
            if (type == typeof(string)) return ToText(value);
            if (type == typeof(double) && value is long l) return (double)l;
            if (type == typeof(DateTime) && value is string s && TryParseDate(s, out var date)) return date;
            return value;
        }

        /// <summary>A value in a column of mixed types, written as it was in the JSON.</summary>
        private static string ToText(object value) => value switch
        {
            string s => s,
            bool b => b ? "true" : "false",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        /// <summary>
        /// Renders a message template - "Delivered {FileName} to {Folder}" - with an event's properties, close to the
        /// way Serilog does: text quoted, {{ and }} for a literal brace, an optional ,alignment and :format, and the
        /// @ and $ operators accepted.  A hole whose property isn't there, or whose format doesn't suit its value, is
        /// left as it is.
        ///
        /// <paramref name="renderings"/> - CLEF's "@r" - are the writer's own text for each hole with a format, in the
        /// template's order: used in place of formatting the value here, which can't match its culture or custom types.
        /// </summary>
        internal static class MessageTemplate
        {
            public static string Render(string template, Func<string, (bool Found, object Value)> property,
                IReadOnlyList<string> renderings = null)
            {
                var formattedHoles = 0;
                var sb = new StringBuilder(template.Length + 32);
                for (var i = 0; i < template.Length; i++)
                {
                    var c = template[i];
                    if (c == '{')
                    {
                        if (i + 1 < template.Length && template[i + 1] == '{')
                        {
                            sb.Append('{');
                            i++;
                            continue;
                        }

                        var end = template.IndexOf('}', i + 1);
                        if (end < 0)
                        {
                            sb.Append(template, i, template.Length - i);
                            break;
                        }

                        var hole = template.Substring(i + 1, end - i - 1);
                        sb.Append(RenderHole(hole, property, renderings, ref formattedHoles) ?? template.Substring(i, end - i + 1));
                        i = end;
                        continue;
                    }

                    if (c == '}' && i + 1 < template.Length && template[i + 1] == '}') i++;
                    sb.Append(c);
                }

                return sb.ToString();
            }

            private static string RenderHole(string hole, Func<string, (bool Found, object Value)> property,
                IReadOnlyList<string> renderings, ref int formattedHoles)
            {
                string format = null;
                var colon = hole.IndexOf(':');
                if (colon >= 0)
                {
                    format = hole[(colon + 1)..];
                    hole = hole[..colon];
                }

                int? alignment = null;
                var comma = hole.IndexOf(',');
                if (comma >= 0)
                {
                    if (!int.TryParse(hole[(comma + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var a)) return null;
                    alignment = a;
                    hole = hole[..comma];
                }

                if (hole.StartsWith('@') || hole.StartsWith('$')) hole = hole[1..];
                if (hole.Length == 0 || !hole.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) return null;

                // "@r" has an entry for every hole with a format, whether or not the event has its property.
                var rendering = format != null && renderings != null && formattedHoles < renderings.Count
                    ? renderings[formattedHoles]
                    : null;
                if (format != null) formattedHoles++;

                var (found, value) = property(hole);
                if (!found) return null;

                // Already aligned by the writer.
                if (rendering != null) return rendering;

                string text;
                try
                {
                    text = value switch
                    {
                        null => "null",
                        string s when format == "l" => s,
                        string s => "\"" + s.Replace("\"", "\\\"") + "\"",
                        JsonText json => json.Json,
                        bool b => b ? "True" : "False",
                        IFormattable f => f.ToString(format, CultureInfo.InvariantCulture),
                        _ => value.ToString()
                    };
                }
                catch (FormatException)
                {
                    // A format the value's type doesn't know - left as written rather than failing the whole log.
                    return null;
                }

                if (alignment is { } width) text = width < 0 ? text.PadRight(-width) : text.PadLeft(width);
                return text;
            }
        }
    }
}
