using System;
using System.Data;
using System.Xml.Linq;

namespace DBAChecks
{

    class RingBufferTargetAttributes
    {
        public Int32 Truncated;
        public Int32 ProcessingTime;
        public Int32 TotalEventsProcessed;
        public Int32 EventCount;
        public Int32 DroppedCount;
        public Int32 MemoryUsed;

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Truncated", typeof(Int32));
            dt.Columns.Add("ProcessingTime", typeof(Int32));
            dt.Columns.Add("TotalEventsProcessed", typeof(Int32));
            dt.Columns.Add("EventCount", typeof(Int32));
            dt.Columns.Add("DroppedCount", typeof(Int32));
            dt.Columns.Add("MemoryUsed", typeof(Int32));
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

    class XETools
    {
        public static DataTable XEStrToDT(string xe, out RingBufferTargetAttributes ringBufferAtt)
        {
            var dtm = DateTime.Now;
            DataTable dt = new DataTable("XEL");
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("object_name", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));
            dt.Columns.Add("duration", typeof(Int64));
            dt.Columns.Add("cpu_time", typeof(Int64));
            dt.Columns.Add("logical_reads", typeof(Int64));
            dt.Columns.Add("physical_reads", typeof(Int64));
            dt.Columns.Add("writes", typeof(Int64));
            dt.Columns.Add("username", typeof(string));
            dt.Columns.Add("batch_text", typeof(string));
            dt.Columns.Add("statement", typeof(string));
            dt.Columns.Add("database_id", typeof(Int32));
            dt.Columns.Add("client_hostname", typeof(string));
            dt.Columns.Add("client_app_name", typeof(string));
            dt.Columns.Add("result", typeof(string));
            string name;
            var el = XElement.Parse(xe);
            ringBufferAtt = new RingBufferTargetAttributes();
            ringBufferAtt.Truncated = Int32.Parse(el.Attribute("truncated").Value);
            ringBufferAtt.DroppedCount = Int32.Parse(el.Attribute("droppedCount").Value);
            ringBufferAtt.ProcessingTime = Int32.Parse(el.Attribute("processingTime").Value);
            ringBufferAtt.EventCount = Int32.Parse(el.Attribute("eventCount").Value);
            ringBufferAtt.MemoryUsed = Int32.Parse(el.Attribute("memoryUsed").Value);
            ringBufferAtt.TotalEventsProcessed = Int32.Parse(el.Attribute("totalEventsProcessed").Value);

            foreach (XElement evt in el.Elements("event"))
            {
                var r = dt.Rows.Add();
                r["event_type"] = evt.Attribute("name").Value;
                r["timestamp"] =  evt.Attribute("timestamp").Value;
                foreach (XElement data in evt.Elements("data"))
                {
                    name = data.Attribute("name").Value;
                    if (name == "result")
                    {
                        r[name] = data.Element("value").Value + " - " + data.Element("text").Value;
                    }
                    else if (dt.Columns.Contains(name))
                    {
                        r[name] = data.Element("value").Value;
                    }

                }
                foreach (XElement data in evt.Elements("action"))
                {
                    name = data.Attribute("name").Value;
                    if (dt.Columns.Contains(name))
                    {
                        r[name] = data.Value;
                    }
                }
            }

            return dt;
        }
    }
}
