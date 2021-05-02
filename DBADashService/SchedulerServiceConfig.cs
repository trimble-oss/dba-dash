using DBADash;
using System;
using System.Diagnostics;
using System.IO;
using Serilog;
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
                Log.Error("Error creating failed message folder {FailedMessageFolder}", FailedMessageFolder);
                FailedMessageFolder = String.Empty;
            }
            
            Config = GetConfig();
        }

        public static CollectionConfig GetConfig()
        {
            string jsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
            if (!(System.IO.File.Exists(jsonConfigPath)))
            {
                Log.Fatal("ServiceConfig.json file is missing.  Use service config tool to create.");
                throw new Exception("ServiceConfig.json file is missing.  Use service config tool to create.");
            }
            string jsonConfig = System.IO.File.ReadAllText(jsonConfigPath);
            var conf = CollectionConfig.Deserialize(jsonConfig);
            if (conf.WasEncrypted())
            {
                Log.Information("Saving ServiceConfig.json with encrypted password");

                string confString = conf.Serialize();
                System.IO.File.WriteAllText(jsonConfigPath, confString);
            }

            return conf;
        }
    }
}
