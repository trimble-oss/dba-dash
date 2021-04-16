using DBADash;
using System;
using System.Diagnostics;
using System.IO;
namespace DBADashService
{
    class SchedulerServiceConfig
    {
        public static readonly CollectionConfig Config;
        public static readonly string FailedMessageFolder;

        static SchedulerServiceConfig()
        {
            FailedMessageFolder = Path.Combine(AppContext.BaseDirectory, "Failed");
            try
            {
                if (!Directory.Exists(FailedMessageFolder)){
                    Directory.CreateDirectory(FailedMessageFolder);
                }
            }
            catch
            {
                Console.WriteLine($"Error creating failed message folder: { FailedMessageFolder }");
                FailedMessageFolder = String.Empty;
            }
            
            Config = GetConfig();
        }

        public static CollectionConfig GetConfig()
        {
            string jsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
            if (!(System.IO.File.Exists(jsonConfigPath)))
            {
                EventLog.WriteEntry("DBADashService", "ServiceConfig.json file is missing.  Please create.", EventLogEntryType.Error);
                throw new Exception("ServiceConfig.json file is missing.Please create.");
            }
            string jsonConfig = System.IO.File.ReadAllText(jsonConfigPath);
            var conf = CollectionConfig.Deserialize(jsonConfig);
            if (conf.WasEncrypted())
            {
                ScheduleService.InfoLogger("Saving ServiceConfig.json with encrypted password");

                string confString = conf.Serialize();
                System.IO.File.WriteAllText(jsonConfigPath, confString);
            }

            return conf;
        }
    }
}
