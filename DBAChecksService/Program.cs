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
            var conf = JsonConvert.DeserializeObject<CollectionConfig[]>(jsonConfig);
            bool wasEncrypted = false;
            foreach (var c in conf)
            {
                if (c.WasEncrypted())
                {
                    wasEncrypted = true;
                }
            }
            if (wasEncrypted)
            {
                Console.WriteLine("Saving ServiceConfig.json with encrypted password");

                string confString = JsonConvert.SerializeObject(conf, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
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
