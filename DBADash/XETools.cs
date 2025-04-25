using System;
using System.Data;
using System.Xml.Linq;

namespace DBADash
{
    internal class RingBufferTargetAttributes
    {
        public int Truncated;
        public int ProcessingTime;
        public int TotalEventsProcessed;
        public int EventCount;
        public int DroppedCount;
        public int MemoryUsed;

        public DataTable GetTable()
        {
            DataTable dt = new();
            dt.Columns.Add("Truncated", typeof(int));
            dt.Columns.Add("ProcessingTime", typeof(int));
            dt.Columns.Add("TotalEventsProcessed", typeof(int));
            dt.Columns.Add("EventCount", typeof(int));
            dt.Columns.Add("DroppedCount", typeof(int));
            dt.Columns.Add("MemoryUsed", typeof(int));
            var r = dt.NewRow();
            r["Truncated"] = Truncated;
            r["ProcessingTime"] = ProcessingTime;
            r["TotalEventsProcessed"] = TotalEventsProcessed;
            r["EventCount"] = EventCount;
            r["DroppedCount"] = DroppedCount;
            r["MemoryUsed"] = MemoryUsed;
            dt.Rows.Add(r);
            return dt;
        }
    }

    internal class XETools
    {
        private static DataTable GetXELSchema()
        {
            DataTable dt = new("XEL");
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("object_name", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));
            dt.Columns.Add("duration", typeof(long));
            dt.Columns.Add("cpu_time", typeof(long));
            dt.Columns.Add("logical_reads", typeof(long));
            dt.Columns.Add("physical_reads", typeof(long));
            dt.Columns.Add("writes", typeof(long));
            dt.Columns.Add("username", typeof(string));
            dt.Columns.Add("batch_text", typeof(string));
            dt.Columns.Add("statement", typeof(string));
            dt.Columns.Add("database_id", typeof(int));
            dt.Columns.Add("client_hostname", typeof(string));
            dt.Columns.Add("client_app_name", typeof(string));
            dt.Columns.Add("result", typeof(string));
            dt.Columns.Add("session_id", typeof(int));
            dt.Columns.Add("context_info", typeof(byte[]));
            dt.Columns.Add("row_count", typeof(long));
            return dt;
        }

        public static DataTable XEStrToDT(string xe, out RingBufferTargetAttributes ringBufferAtt)
        {
            var dt = GetXELSchema();
            var el = XElement.Parse(xe);
            ringBufferAtt = new RingBufferTargetAttributes();

            if (int.TryParse(el.Attribute("truncated")?.Value, out var truncatedValue))
            {
                ringBufferAtt.Truncated = truncatedValue;
            }

            ringBufferAtt.DroppedCount = int.TryParse(el.Attribute("droppedCount")?.Value, out var droppedCountValue)
                ? droppedCountValue
                : 0;
            ringBufferAtt.ProcessingTime =
                int.TryParse(el.Attribute("processingTime")?.Value, out var processingTimeValue)
                    ? processingTimeValue
                    : 0;
            ringBufferAtt.EventCount = int.TryParse(el.Attribute("eventCount")?.Value, out var eventCountValue)
                ? eventCountValue
                : 0;
            ringBufferAtt.MemoryUsed = int.TryParse(el.Attribute("memoryUsed")?.Value, out var memoryUsedValue)
                ? memoryUsedValue
                : 0;
            ringBufferAtt.TotalEventsProcessed =
                int.TryParse(el.Attribute("totalEventsProcessed")?.Value, out var totalEventsProcessedValue)
                    ? totalEventsProcessedValue
                    : 0;

            foreach (var evt in el.Elements("event"))
            {
                var r = dt.Rows.Add();
                r["event_type"] = evt.Attribute("name")?.Value!;
                var timestamp = evt.Attribute("timestamp")?.Value;
                if (timestamp == null) continue;
                r["timestamp"] = DateTime.Parse(timestamp).ToUniversalTime();

                ProcessEventElements(evt, r, "data");
                ProcessEventElements(evt, r, "action");
            }

            return dt;
        }

        private static void ProcessEventElements(XContainer evt, DataRow row, string elementType)
        {
            foreach (var data in evt.Elements(elementType))
            {
                var name = data.Attribute("name")?.Value;
                if (name != null && row.Table.Columns.Contains(name))
                {
                    row[name] = elementType switch
                    {
                        "action" when name == "context_info" => Convert.FromHexString(data.Element("value")?.Value ??
                            string.Empty),
                        "data" when name == "result" => data.Element("value")?.Value + " - " + data.Element("text")?.Value,
                        "data" => data.Element("value")?.Value ?? string.Empty,
                        "action" => data.Value,
                        _ => row[name]
                    };
                }
            }
        }
    }
}