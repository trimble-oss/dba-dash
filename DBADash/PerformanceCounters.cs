using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Serilog;
namespace DBADash
{
    static class PerformanceCounters
    {

       static string countersXML;
       static readonly string defaultFileName = "PerformanceCounters.xml";
       static readonly string userFileName = "PerformanceCountersCustom.xml";

        public static string PerformanceCountersXML
        {
            get
            {
                if (countersXML == null)
                {
                    try
                    {

                        if (File.Exists(userFileName))
                        {
                            Log.Information("Read performance counters from {filename} (user)", userFileName);
                            countersXML = File.ReadAllText(userFileName);
                        }
                        else if (File.Exists(defaultFileName))
                        {
                            Log.Information("Read performance counters from {filename} (default)", defaultFileName);
                            countersXML = File.ReadAllText(defaultFileName);
                        }
                        else
                        {
                            Log.Warning("Performance counters file '{filename}' not found.  Performance counter collection disabled", defaultFileName);
                            countersXML = "";
                            return countersXML;
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex,"Error reading performance counters file '{filename}'.  Performance counter collection disabled", defaultFileName);
                        countersXML = "";
                    }
                    
                   
                }
         
                return countersXML;
            }
        }

    }
}
