using DBADash;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    public class ScheduleService
    {
        private readonly IScheduler scheduler;
        public ScheduleService()
        {
            var conf = getConfig();
            Int32 threads = conf.ServiceThreads;
            if (threads < 1)
            {
                threads = 10;
                Console.WriteLine("Threads:" + threads + " (default)");
            }
            else
            {
                Console.WriteLine("Threads:" + threads + "(user)");
            }
            
            NameValueCollection props = new NameValueCollection
        {
            { "quartz.serializer.type", "binary" },
            { "quartz.scheduler.instanceName", "DBADashScheduler" },
            { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
            { "quartz.threadPool.threadCount", threads.ToString() }
        };
            
            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            scheduler = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static void ErrorLogger(Exception ex, string context)
        {
            Console.WriteLine(context + ": " + ex.Message);
            EventLog.WriteEntry("DBADashService", context + ": " + ex.Message, EventLogEntryType.Error);
        }


        private void removeEventSessions(CollectionConfig config)
        {
            try
            {
                Parallel.ForEach(config.SourceConnections, cfg => {
                    string cfgString = JsonConvert.SerializeObject(cfg);
                    if (cfg.SourceConnection.Type == ConnectionType.SQL)
                    {
                        var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI);
                        if (cfg.PersistXESessions)
                        {
                            try
                            {
                                Console.WriteLine("Stop DBADash event sessions: " + cfg.SourceConnection.DataSource());
                                collector.StopEventSessions();
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger(ex, "Stop Event Sessions:");
                            }
                        }
                        else
                        {
                            try
                            {
                                Console.WriteLine("Remove DBADash event sessions: " + cfg.SourceConnection.DataSource());
                                collector.RemoveEventSessions();
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger(ex, "Remove DBADash Event Sessions:");
                            }
                        }

                    }
                });
            }
            catch (Exception ex)
            {
                ErrorLogger(ex, "Remove Event Sessions:");
            }
        }


        public void Start()
        {
            scheduler.Start().ConfigureAwait(false).GetAwaiter().GetResult();

            ScheduleJobs();
        }

        private CollectionConfig getConfig()
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


        public void ScheduleJobs()
        {
            Console.WriteLine("Agent Version:" + Assembly.GetEntryAssembly().GetName().Version);

            var conf = getConfig();
            removeEventSessions(conf);

            
            if (conf.DestinationConnection.Type == ConnectionType.SQL)
            {
                string maintenanceCron = conf.GetMaintenanceCron();

                IJobDetail job = JobBuilder.Create<MaintenanceJob>()
                        .WithIdentity("MaintenanceJob")
                        .UsingJobData("ConnectionString", conf.DestinationConnection.ConnectionString)
                        .Build();
                ITrigger trigger = TriggerBuilder.Create()
                .StartNow()
                .WithCronSchedule(maintenanceCron)
                .Build();
             
                scheduler.ScheduleJob(job, trigger).ConfigureAwait(false).GetAwaiter().GetResult();
                scheduler.TriggerJob(job.Key);

            }
            foreach (DBADashSource cfg in conf.SourceConnections)
            {
                string cfgString = JsonConvert.SerializeObject(cfg);

                foreach (var s in cfg.GetSchedule())
                {
                    IJobDetail job = JobBuilder.Create<DBADashJob>()
                           .UsingJobData("Type", JsonConvert.SerializeObject(s.CollectionTypes))
                           .UsingJobData("CFG", cfgString)
                           .UsingJobData("BinarySerialization", conf.BinarySerialization)
                           .UsingJobData("AccessKey", conf.AccessKey)
                           .UsingJobData("SecretKey", conf.GetSecretKey())
                           .UsingJobData("AWSProfile", conf.AWSProfile)
                           .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                           .UsingJobData("Destination", conf.DestinationConnection.ConnectionString)
                           .UsingJobData("SourceType", JsonConvert.SerializeObject(cfg.SourceConnection.Type))
                           .UsingJobData("DestinationType", JsonConvert.SerializeObject(conf.DestinationConnection.Type))
                          .Build();
                    ITrigger trigger = TriggerBuilder.Create()
                    .StartNow()
                    .WithCronSchedule(s.CronSchedule)
                    .Build();
             
                    scheduler.ScheduleJob(job, trigger).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (s.RunOnServiceStart)
                    {
                        scheduler.TriggerJob(job.Key);
                    }

                }
                if (cfg.SchemaSnapshotDBs != null && cfg.SchemaSnapshotDBs.Length > 0)
                {
                    IJobDetail job = JobBuilder.Create<SchemaSnapshotJob>()
                           .UsingJobData("CFG", cfgString)
                          .UsingJobData("AccessKey", conf.AccessKey)
                          .UsingJobData("SecretKey", conf.GetSecretKey())
                          .UsingJobData("AWSProfile", conf.AWSProfile)
                          .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                          .UsingJobData("Destination", conf.DestinationConnection.ConnectionString)
                          .UsingJobData("DestinationType", JsonConvert.SerializeObject(conf.DestinationConnection.Type))
                          .UsingJobData("Options", JsonConvert.SerializeObject(conf.SchemaSnapshotOptions))
                          .UsingJobData("SchemaSnapshotDBs", cfg.SchemaSnapshotDBs)
                             .Build();
                    ITrigger trigger = TriggerBuilder.Create()
                      .StartNow()
                      .WithCronSchedule(cfg.SchemaSnapshotCron)
                      .Build();

                    
                    scheduler.ScheduleJob(job, trigger).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (cfg.SchemaSnapshotOnServiceStart)
                    {
                        scheduler.TriggerJob(job.Key);
                    }


                }
            }
        }
        public void Stop()
        {
            var conf = getConfig();
            removeEventSessions(conf);
            scheduler.Shutdown().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
