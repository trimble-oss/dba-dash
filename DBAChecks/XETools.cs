using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBAChecks
{
    class XETools
    {
       public static DataTable XEStrToDT(string xe)
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
            foreach (XElement evt in XElement.Parse(xe).Elements("event"))
            {
                var r = dt.Rows.Add();
                r["event_type"] = evt.Attribute("name").Value;
                r["timestamp"] = evt.Attribute("timestamp").Value;
                foreach (XElement data in evt.Elements("data"))
                {
                    name = data.Attribute("name").Value;
                    if (dt.Columns.Contains(name))
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
