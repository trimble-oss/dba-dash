using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace DBADash
{
    /// <summary>
    /// Packs the sys.dm_exec_query_profiles rows of one request into a single compressed value for the repository, and
    /// unpacks them again - see <see cref="LiveProfileCollector"/>.<br/>
    /// A row per operator per thread is a lot of rows to keep for one query that is overwritten every collection, and
    /// nothing queries them individually.  JSON compressed the same way as plans (<see cref="SMOBaseClass.Zip"/>), so
    /// <c>CAST(DECOMPRESS(Counters) AS NVARCHAR(MAX))</c> reads it in the repository when it needs looking at.  The
    /// column names are those of the DMV, the same as <see cref="Messaging.LiveQueryProfileMessage"/> returns, so the GUI
    /// reads both the same way.
    /// </summary>
    public static class LiveProfileCounters
    {
        public static readonly string[] Columns =
        [
            "node_id", "thread_id", "row_count", "rewind_count", "rebind_count", "end_of_scan_count", "elapsed_time_ms",
            "cpu_time_ms", "scan_count", "logical_read_count", "physical_read_count", "read_ahead_count",
            "lob_logical_read_count", "lob_physical_read_count", "actual_read_row_count", "open_time", "close_time"
        ];

        /// <summary>The counters in <paramref name="rows"/>, compressed.  Columns missing from the rows are written as null.</summary>
        public static byte[] Encode(DataTable table, Func<DataRow, bool> rows)
        {
            using var sw = new StringWriter();
            using (var writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("columns");
                writer.WriteStartArray();
                foreach (var column in Columns) writer.WriteValue(column);
                writer.WriteEndArray();
                writer.WritePropertyName("rows");
                writer.WriteStartArray();
                foreach (var row in table.Rows.Cast<DataRow>().Where(rows))
                {
                    writer.WriteStartArray();
                    foreach (var column in Columns)
                    {
                        if (!table.Columns.Contains(column) || row[column] == DBNull.Value) writer.WriteNull();
                        else writer.WriteValue(Convert.ToInt64(row[column]));
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            return SMOBaseClass.Zip(sw.ToString());
        }

        /// <summary>The counters <see cref="Encode"/> packed, as a Profiles table.</summary>
        public static DataTable Decode(byte[] counters)
        {
            var json = JObject.Parse(SMOBaseClass.Unzip(counters));
            var columns = json["columns"]!.Values<string>().ToList();
            var table = new DataTable("Profiles");
            foreach (var column in columns) table.Columns.Add(column, typeof(long));

            foreach (var row in json["rows"]!.Children<JArray>())
            {
                table.Rows.Add(row.Select(v => v.Type == JTokenType.Null ? (object)DBNull.Value : v.Value<long>()).ToArray());
            }
            return table;
        }
    }
}
