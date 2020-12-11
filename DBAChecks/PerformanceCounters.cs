using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DBAChecks
{
    static class PerformanceCounters
    {

       static string countersXML;
       static string defaultFileName = "PerformanceCounters.xml";
       static string userFileName = "PerformanceCountersCustom.xml";

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
                            Console.WriteLine("Read Performance Counters XML:" + userFileName);

                            countersXML = File.ReadAllText(userFileName);
                        }
                        else if (File.Exists(defaultFileName))
                        {
                            Console.WriteLine("Read Performance Counters XML:" + defaultFileName);
                            countersXML = File.ReadAllText(defaultFileName);
                        }
                        else
                        {
                            Console.WriteLine("Warning: File not found:" + defaultFileName + Environment.NewLine + "Performance counter collection disabled");
                            countersXML = "";
                            return countersXML;
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error reading performance counters XML file:" + ex.Message);
                        countersXML = "";
                    }
                    
                   
                }
         
                return countersXML;
            }
        }

    }
}
