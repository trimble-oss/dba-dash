using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Rebuilds a display table from stored ad-hoc XE trace events - the (event_type, timestamp, Fields JSON) rows
    /// returned by <c>XETraceEvents_Get</c> / <c>XETraceEvents_GetByRunGroup</c>.  Shared by the live trace UI
    /// (<see cref="QuickXETrace"/> history) and the Trace History report's "View Data" viewer so both expand stored
    /// events identically.  The union of JSON keys becomes the columns, and the column type is inferred from the JSON
    /// token types (integer/float -> numeric) so numeric fields like duration/cpu_time/reads come back typed -
    /// otherwise the grid's Group By disables Sum/Sum %/Avg because a string column isn't numeric.
    /// </summary>
    internal static class XEStoredEvents
    {
        public static DataTable Expand(DataTable stored)
        {
            // Pass 1: parse each row's Fields JSON once and infer a column type per field across all rows.
            var parsed = new List<(DataRow Source, JObject Fields)>();
            var fieldTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            var order = new List<string>();

            foreach (DataRow r in stored.Rows)
            {
                JObject fields = null;
                if (r["Fields"] != DBNull.Value && r["Fields"] is string json && json.Length > 0)
                {
                    fields = JObject.Parse(json);
                    foreach (var p in fields.Properties())
                    {
                        if (!fieldTypes.ContainsKey(p.Name)) { fieldTypes[p.Name] = null; order.Add(p.Name); }
                        fieldTypes[p.Name] = MergeJsonType(fieldTypes[p.Name], p.Value);
                    }
                }
                parsed.Add((r, fields));
            }

            // Pass 2: build the typed table and fill it.
            var dt = new DataTable();
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));
            foreach (var name in order)
            {
                dt.Columns.Add(name, fieldTypes[name] ?? typeof(string));
            }

            foreach (var (source, fields) in parsed)
            {
                var row = dt.NewRow();
                row["event_type"] = source["event_type"];
                if (source["timestamp"] != DBNull.Value) row["timestamp"] = source["timestamp"];
                if (fields != null)
                {
                    foreach (var p in fields.Properties())
                    {
                        row[p.Name] = ConvertJsonValue(p.Value, dt.Columns[p.Name].DataType);
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>Widens the running inferred type for a field as more JSON values are seen (null = not yet known).</summary>
        private static Type MergeJsonType(Type existing, JToken token)
        {
            var candidate = token?.Type switch
            {
                JTokenType.Integer => typeof(long),
                JTokenType.Float => typeof(double),
                JTokenType.Null or JTokenType.None => null, // null values don't constrain the type
                _ => typeof(string)
            };
            if (candidate == null) return existing;
            if (existing == null || existing == candidate) return candidate;
            // long + double both seen -> use double; anything else mixed -> fall back to string
            if ((existing == typeof(long) || existing == typeof(double)) &&
                (candidate == typeof(long) || candidate == typeof(double)))
            {
                return typeof(double);
            }
            return typeof(string);
        }

        private static object ConvertJsonValue(JToken token, Type type)
        {
            if (token == null || token.Type == JTokenType.Null) return DBNull.Value;
            try
            {
                if (type == typeof(long)) return token.Value<long>();
                if (type == typeof(double)) return token.Value<double>();
                return token.ToString();
            }
            catch
            {
                return DBNull.Value; // shouldn't happen (type was inferred to fit) - be defensive
            }
        }
    }
}
