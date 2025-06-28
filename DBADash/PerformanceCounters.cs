using Serilog;
using System;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DBADash
{
    public static class PerformanceCounters
    {
        private static string countersXML;

        private const string defaultFileNameResourceName = "DBADash.PerformanceCounters.xml";

        private static readonly string userFileName =
            Path.Combine(AppContext.BaseDirectory, "PerformanceCountersCustom.xml");

        private static readonly string legacyFileName = Path.Combine(AppContext.BaseDirectory, "PerformanceCounters.xml");

        private static string defaultCountersXML;

        public static bool HasCustomPerformanceCounters() => File.Exists(userFileName);

        /// <summary>
        /// Get the performance counters XML - either from the users custom file or from the default embedded resource. Remove the legacy file if it exists to avoid confusion.
        /// </summary>
        public static string PerformanceCountersXML
        {
            get
            {
                if (countersXML != null) return countersXML;
                RemoveLegacyFile();
                try
                {
                    if (HasCustomPerformanceCounters())
                    {
                        Log.Information("Read performance counters from {filename} (user)", userFileName);
                        countersXML = File.ReadAllText(userFileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex,
                        "Error reading performance counters file '{filename}'.  Performance counter collection disabled",
                        userFileName);
                    countersXML = "";
                }
                if (countersXML != null) return countersXML;
                Log.Information("Using default performance counters");
                countersXML = DefaultPerformanceCountersXML;

                return countersXML;
            }
        }

        /// <summary>
        /// Get default XML performance counters definition from embedded resource
        /// </summary>
        public static string DefaultPerformanceCountersXML
        {
            get
            {
                if (defaultCountersXML != null) return defaultCountersXML;
                try
                {
                    defaultCountersXML = Utility.GetResourceString(defaultFileNameResourceName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex,
                        "Error reading performance counters from embedded resource '{filename}'.  Performance counter collection disabled",
                        defaultFileNameResourceName);
                    countersXML = "";
                }
                return defaultCountersXML;
            }
        }

        /// <summary>
        /// Get performance counters as a DataTable for service config tool
        /// </summary>
        public static DataTable PerformanceCountersDataTable() => XmlToDataTable(PerformanceCountersXML);

        /// <summary>
        /// Get default performance counters as DataTable for service config tool
        /// </summary>
        public static DataTable GetDefaultCountersDataTable() => XmlToDataTable(DefaultPerformanceCountersXML);

        private static DataTable GetEmptyDataTable()
        {
            var dt = new DataTable("Counters");
            dt.Columns.Add("ObjectName", typeof(string));
            dt.Columns.Add("CounterName", typeof(string));
            dt.Columns.Add("InstanceName", typeof(string));
            return dt;
        }

        public static void Save(DataTable dt)
        {
            var xml = DataTableToXML(dt);
            Save(xml);
        }

        /// <summary>
        /// Saves performance counters.  If counters matches default, remove the user file (embedded resource will be used).
        /// </summary>
        /// <param name="xml">Performance Counters XML</param>
        public static void Save(string xml)
        {
            countersXML = null; // force reload
            if (IsDefault(xml))
            {
                if (File.Exists(userFileName))
                {
                    File.Delete(userFileName);
                }
            }
            else
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, userFileName);
                File.WriteAllText(path, xml);
            }
        }

        /// <summary>
        /// Remove the old file that used to contain the performance counter defaults. This is now an embedded resource.
        /// </summary>
        public static void RemoveLegacyFile()
        {
            try
            {
                if (File.Exists(legacyFileName))
                {
                    File.Delete(legacyFileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing legacy file {file}", legacyFileName);
            }
        }

        // Check if XML mat
        /// <summary>
        /// Check if XML matches the default performance counters.  Converts default counters to DataTable then back to XML to ensure it's in the same format
        /// </summary>
        /// <param name="xml">Performance counters XML</param>
        /// <returns>True/False if matches default counters</returns>
        private static bool IsDefault(string xml)
        {
            var defaultXml = DataTableToXML(GetDefaultCountersDataTable());
            return xml == defaultXml;
        }

        /// <summary>
        /// Convert DataTable to performance counters XML
        /// </summary>
        /// <param name="dt">DataTable to convert.  ObjectName,CounterName & InstanceName columns required</param>
        /// <returns>XML string</returns>
        public static string DataTableToXML(DataTable dt)
        {
            ArgumentNullException.ThrowIfNull(dt);
            try
            {
                var xmlDoc = new XmlDocument();

                var rootElement = xmlDoc.CreateElement("Counters");
                xmlDoc.AppendChild(rootElement);

                var sortedRows = dt.AsEnumerable().OrderBy(row => row.Field<string>("ObjectName"))
                    .ThenBy(row => row.Field<string>("CounterName"))
                    .ThenBy(row => row.Field<string>("InstanceName"));

                foreach (var row in sortedRows)
                {
                    var objectName = row.Field<string>("ObjectName");
                    var counterName = row.Field<string>("CounterName");
                    var instanceName = row.Field<string>("InstanceName");
                    if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(counterName))
                    {
                        throw new Exception("Object/Counter name are required");
                    }

                    var counterElement = xmlDoc.CreateElement("Counter");
                    counterElement.SetAttribute("object_name", objectName);
                    counterElement.SetAttribute("counter_name", counterName);

                    if (instanceName != "*")
                    {
                        counterElement.SetAttribute("instance_name", instanceName ?? string.Empty);
                    }

                    rootElement.AppendChild(counterElement);
                }

                // Format the XML with indentation
                return FormatXml(xmlDoc.OuterXml);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting DataTable to XML: {ex.Message}", ex);
            }
        }

        private static string FormatXml(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                // Handle and throw if fatal exception here; don't just ignore them
                return xml;
            }
        }

        /// <summary>
        /// Convert Performance Counters XML to DataTable.
        /// </summary>
        /// <param name="xml">Performance Counters XML</param>
        /// <returns>DataTable with ObjectName,CounterName & InstanceName columns</returns>
        public static DataTable XmlToDataTable(string xml)
        {
            var dt = GetEmptyDataTable();
            try
            {
                var xDoc = XDocument.Parse(xml);

                var counters = xDoc.Descendants("Counter");

                foreach (var counter in counters)
                {
                    var row = dt.NewRow();

                    row["ObjectName"] = counter.Attribute("object_name")?.Value ?? string.Empty;
                    row["CounterName"] = counter.Attribute("counter_name")?.Value ?? string.Empty;
                    row["InstanceName"] = counter.Attribute("instance_name")?.Value ?? "*";

                    dt.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing XML: {ex.Message}", ex);
            }

            return dt;
        }
    }
}