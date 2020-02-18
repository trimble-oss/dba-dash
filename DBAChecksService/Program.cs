using DBAChecks;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Quartz;
using System.Text.Json;
using System.Diagnostics;

namespace DBAChecksService
{
    class Program
    {
        static void Main(string[] args)
        {
            string jsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
            if (!(System.IO.File.Exists(jsonConfigPath))){
                EventLog.WriteEntry("DBAChecksService","ServiceConfig.json file is missing.  Please create." , EventLogEntryType.Error);
            }
            string jsonConfig = System.IO.File.ReadAllText(jsonConfigPath);
            var conf =  JsonSerializer.Deserialize<CollectionConfig[]>(jsonConfig);
            bool wasEncrypted = false;
            foreach(var c in conf)
            {
                if (c.WasEncrypted())
                {
                    wasEncrypted = true;
                }
            }
            if (wasEncrypted)
            {
                Console.WriteLine("Saving ServiceConfig.json with encrypted password");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IgnoreNullValues=true
                };
                string confString = JsonSerializer.Serialize(conf, options);
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
