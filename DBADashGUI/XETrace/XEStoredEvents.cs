using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Rebuilds a display table from stored ad-hoc XE trace events - the (event_type, timestamp, InstanceID, Fields
    /// JSON) rows returned by <c>XETraceEvents_Get</c> / <c>XETraceEvents_GetByRunGroup</c>.  Shared by the live trace
    /// UI (<see cref="QuickXETrace"/> history) and the Trace History report's "View Data" viewer so both expand stored
    /// events identically.  The union of JSON keys becomes the columns, and the column type is inferred from the JSON
    /// value kinds (integer/float -> numeric) so numeric fields like duration/cpu_time/reads come back typed -
    /// otherwise the grid's Group By disables Sum/Sum %/Avg because a string column isn't numeric.
    ///
    /// Built straight from a forward-only <see cref="DbDataReader"/> rather than a materialized DataTable: for a large
    /// (~85K-row) trace this skips building a throwaway table of raw JSON strings, overlaps JSON shredding with the
    /// network read, and folds the UTC-&gt;app-time-zone timestamp conversion into the single build pass instead of a
    /// separate mutation pass over every row afterwards.
    /// </summary>
    internal static class XEStoredEvents
    {
        /// <summary>Per-row values buffered during the read pass (types aren't known until every row is seen).</summary>
        private struct BufferedRow
        {
            public string EventType;
            public DateTime? Timestamp;
            public int InstanceId;
            public bool HasInstance;
            public List<KeyValuePair<string, object>> Fields; // boxed long/double/string; null when no Fields JSON
        }

        /// <summary>
        /// Reads every event row from <paramref name="reader"/> and builds the typed display table.
        /// <paramref name="convertTimestampToLocal"/> converts the UTC <c>timestamp</c> to the app time zone inline
        /// (stored events are always UTC), so the caller must NOT convert again.
        ///
        /// Deliberately synchronous (<c>reader.Read()</c>, not <c>ReadAsync</c>): the callers run on the UI thread, so
        /// an async per-row read would marshal all ~85K continuations back onto the UI message pump.  The repo runs
        /// this whole method on a background thread instead (see <c>XETraceRepo.ReadExpandedAsync</c>).
        /// </summary>
        public static DataTable BuildFromReader(DbDataReader reader, bool convertTimestampToLocal)
        {
            var ordEventType = reader.GetOrdinal("event_type");
            var ordTimestamp = reader.GetOrdinal("timestamp");
            var ordFields = reader.GetOrdinal("Fields");
            var ordInstance = HasColumn(reader, "InstanceID");

            // Pass 1: read + parse.  Buffer each row's values and infer a column type per field across all rows.  A
            // legacy "Instance" key (stamped into the JSON by an older build) is ignored - the InstanceID-derived
            // column wins.
            var buffered = new List<BufferedRow>();
            var fieldTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            var order = new List<string>();
            var instanceIds = new HashSet<int>();

            while (reader.Read())
            {
                var b = new BufferedRow
                {
                    EventType = reader.IsDBNull(ordEventType) ? null : reader.GetString(ordEventType),
                    Timestamp = reader.IsDBNull(ordTimestamp) ? (DateTime?)null : reader.GetDateTime(ordTimestamp)
                };
                if (ordInstance >= 0 && !reader.IsDBNull(ordInstance))
                {
                    b.InstanceId = reader.GetInt32(ordInstance);
                    b.HasInstance = true;
                    instanceIds.Add(b.InstanceId);
                }
                if (!reader.IsDBNull(ordFields))
                {
                    var json = reader.GetString(ordFields);
                    if (json.Length > 0)
                    {
                        using var doc = JsonDocument.Parse(json);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, XETraceController.InstanceColumn, StringComparison.Ordinal)) continue;
                            var (value, type) = ReadJsonValue(prop.Value);
                            if (!fieldTypes.ContainsKey(prop.Name)) { fieldTypes[prop.Name] = null; order.Add(prop.Name); }
                            fieldTypes[prop.Name] = MergeType(fieldTypes[prop.Name], type);
                            (b.Fields ??= new List<KeyValuePair<string, object>>()).Add(new(prop.Name, value));
                        }
                    }
                }
                buffered.Add(b);
            }

            var multiInstance = instanceIds.Count > 1;

            // Pass 2: build the typed table and fill it.
            var dt = new DataTable();
            var colEventType = dt.Columns.Add("event_type", typeof(string));
            var colTimestamp = dt.Columns.Add("timestamp", typeof(DateTime));
            var colInstance = multiInstance ? dt.Columns.Add(XETraceController.InstanceColumn, typeof(string)) : null;
            // Cache the DataColumn per field name so the fill loop indexes rows by column reference rather than doing
            // an 85K-rows x fields-per-row string lookup through dt.Columns[name].
            var fieldColumns = new Dictionary<string, DataColumn>(StringComparer.Ordinal);
            foreach (var name in order)
            {
                fieldColumns[name] = dt.Columns.Add(name, fieldTypes[name] ?? typeof(string));
            }

            var instanceLabels = new Dictionary<int, string>();

            // BeginLoadData turns off constraint checking, index maintenance and change notifications for the bulk
            // fill - a large saving when adding ~85K rows.
            dt.BeginLoadData();
            try
            {
                foreach (var b in buffered)
                {
                    var row = dt.NewRow();
                    row[colEventType] = (object)b.EventType ?? DBNull.Value;
                    if (b.Timestamp.HasValue)
                    {
                        row[colTimestamp] = convertTimestampToLocal ? b.Timestamp.Value.ToAppTimeZone() : b.Timestamp.Value;
                    }
                    if (colInstance != null && b.HasInstance)
                    {
                        if (!instanceLabels.TryGetValue(b.InstanceId, out var label))
                        {
                            label = XEInstanceLabels.Resolve(b.InstanceId, b.InstanceId.ToString());
                            instanceLabels[b.InstanceId] = label;
                        }
                        row[colInstance] = label;
                    }
                    if (b.Fields != null)
                    {
                        foreach (var kv in b.Fields)
                        {
                            if (!fieldColumns.TryGetValue(kv.Key, out var col)) continue;
                            row[col] = Coerce(kv.Value, col.DataType);
                        }
                    }
                    dt.Rows.Add(row);
                }
            }
            finally
            {
                dt.EndLoadData();
            }
            return dt;
        }

        /// <summary>Ordinal of <paramref name="name"/>, or -1 when the reader has no such column.</summary>
        private static int HasColumn(DbDataReader reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        /// <summary>Reads a JSON scalar as a boxed value plus the .NET type it implies (null value = no type constraint).</summary>
        private static (object Value, Type Type) ReadJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null or JsonValueKind.Undefined:
                    return (null, null);
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var l)) return (l, typeof(long));
                    if (element.TryGetDouble(out var d)) return (d, typeof(double));
                    return (element.GetRawText(), typeof(string));
                case JsonValueKind.True:
                    return ("True", typeof(string));
                case JsonValueKind.False:
                    return ("False", typeof(string));
                case JsonValueKind.String:
                    return (element.GetString(), typeof(string));
                default: // Object / Array - keep the raw JSON text
                    return (element.GetRawText(), typeof(string));
            }
        }

        /// <summary>Widens the running inferred type for a field as more values are seen (null = not yet known).</summary>
        private static Type MergeType(Type existing, Type candidate)
        {
            if (candidate == null) return existing; // null values don't constrain the type
            if (existing == null || existing == candidate) return candidate;
            // long + double both seen -> use double; anything else mixed -> fall back to string
            if ((existing == typeof(long) || existing == typeof(double)) &&
                (candidate == typeof(long) || candidate == typeof(double)))
            {
                return typeof(double);
            }
            return typeof(string);
        }

        /// <summary>Coerces a buffered value to the column's final inferred type (e.g. a long into a double column,
        /// or any value into a string column when the field turned out to be mixed).</summary>
        private static object Coerce(object value, Type type)
        {
            if (value == null) return DBNull.Value;
            try
            {
                if (type == typeof(long)) return value is long l ? l : Convert.ToInt64(value);
                if (type == typeof(double)) return value is double d ? d : Convert.ToDouble(value);
                return value is string s ? s : value.ToString();
            }
            catch
            {
                return DBNull.Value; // shouldn't happen (type was inferred to fit) - be defensive
            }
        }
    }
}
