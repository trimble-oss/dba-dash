using DBAChecks;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace DBAChecksService
{
    class Program
    {
        static void Main(string[] args)
        {
            string jsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
            if (!(System.IO.File.Exists(jsonConfigPath)))
            {
                EventLog.WriteEntry("DBAChecksService", "ServiceConfig.json file is missing.  Please create.", EventLogEntryType.Error);
                throw new Exception("ServiceConfig.json file is missing.Please create.");
            }            
            string jsonConfig = System.IO.File.ReadAllText(jsonConfigPath);
            var conf = CollectionConfig.Deserialize(jsonConfig);
            bool wasEncrypted = false;

            if (conf.DestinationConnection.WasEncrypted)
            {
                wasEncrypted = true;
            }
            else
            {
                foreach (var c in conf.SourceConnections)
                {
                    if (c.SourceConnection.WasEncrypted)
                    {
                        wasEncrypted = true;
                    }
                }
            }
            if (wasEncrypted)
            {
                Console.WriteLine("Saving ServiceConfig.json with encrypted password");

                string confString = conf.Serialize();
                System.IO.File.WriteAllText(jsonConfigPath, confString);
            }


            ConfigureService.Configure(conf);
        }

        private static void CreateEventLogSource()
        {
            if (!EventLog.SourceExists("DBAChecksService"))
            {
                EventLog.CreateEventSource("DBAChecksService", "Application");
            }
        }
    }



}
