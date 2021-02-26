using DBADash;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashService
{
    class SchedulerServiceConfig
    {
        public static readonly CollectionConfig Config;

        static SchedulerServiceConfig()
        {
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
                Console.WriteLine("Saving ServiceConfig.json with encrypted password");

                string confString = conf.Serialize();
                System.IO.File.WriteAllText(jsonConfigPath, confString);
            }

            return conf;
        }
    }
}
