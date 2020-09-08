using DBAChecks;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data.SqlClient;
using Topshelf;
using Topshelf.Quartz;
using static DBAChecks.DBAChecksConnection;
using System.Diagnostics;

namespace DBAChecksService
{
    class DBAChecksService
    {

        public static void ErrorLogger(Exception ex, string context)
        {
            Console.WriteLine(context + ": " + ex.Message);
            EventLog.WriteEntry("DBAChecksService", context+ ": " +  ex.Message, EventLogEntryType.Error);
        }

        public void Start(CollectionConfig config)
        {
              removeEventSessions(config);

        }

        private void removeEventSessions(CollectionConfig config)
        {
            try
            {
                foreach (DBAChecksSource cfg in config.SourceConnections)
                {

                    string cfgString = JsonConvert.SerializeObject(cfg);
                    if (cfg.SourceConnection.Type == ConnectionType.SQL)
                    {
                        var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI);
                        if (cfg.PersistXESessions)
                        {
                            try
                            {
                                Console.WriteLine("Stop DBAChecks event sessions: " + cfg.SourceConnection.DataSource());
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
                                Console.WriteLine("Remove DBAChecks event sessions: " + cfg.SourceConnection.DataSource());
                                collector.RemoveEventSessions();
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger(ex, "Remove DBAChecks Event Sessions:");
                            }
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                ErrorLogger(ex, "Remove Event Sessions:");
            }
        }


        public void Stop(CollectionConfig config)
        {
            removeEventSessions(config);
        }

        public void Shutdown(CollectionConfig config)
        {
            removeEventSessions(config);
        }

    }

    public class MaintenanceJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string connectionString = dataMap.GetString("ConnectionString");
            AddPartitions(connectionString);
            PurgeData(connectionString);

        }

        public static void AddPartitions(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Partitions_Add", cn);
                Console.WriteLine("Maintenance: Creating partitions");
                cmd.ExecuteNonQuery();
            }
        }
        public static void PurgeData(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                Console.WriteLine("Maintenance: PurgeData");
                SqlCommand cmd = new SqlCommand("PurgeData", cn);
                cmd.ExecuteNonQuery();
            }
        }
    }






    internal static class ConfigureService
    {
        internal static void Configure(CollectionConfig config)
        {
            HostFactory.Run(configure =>
            {

                configure.Service<DBAChecksService>(service =>
                {
                    service.ConstructUsing(s => new DBAChecksService());
                    ServiceConfiguratorExtensions.WhenStarted<DBAChecksService>(service, s => s.Start(config));
                    ServiceConfiguratorExtensions.WhenStopped<DBAChecksService>(service, s => s.Stop(config));
                    ServiceConfiguratorExtensions.WhenShutdown<DBAChecksService>(service, s => s.Shutdown(config));
                    if (config.DestinationConnection.Type == ConnectionType.SQL)
                    {
                        string maintenanceCron = config.GetMaintenanceCron();
                        var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                     q.WithJob(() =>
                     JobBuilder.Create<MaintenanceJob>()
                          .UsingJobData("ConnectionString", config.DestinationConnection.ConnectionString)
                          .Build())
                          .AddTrigger(() => TriggerBuilder.Create()
                              .WithCronSchedule(maintenanceCron)
                              .Build()
                              )); ; ;
                        MaintenanceJob.AddPartitions(config.DestinationConnection.ConnectionString);

                    }
                    foreach (DBAChecksSource cfg in config.SourceConnections)
                    {

                        string cfgString = JsonConvert.SerializeObject(cfg);

                        foreach (var s in cfg.GetSchedule())
                        {

                            var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                              q.WithJob(() =>
                              JobBuilder.Create<DBAChecksJob>()
                                   .UsingJobData("Type", JsonConvert.SerializeObject(s.CollectionTypes))
                                   .UsingJobData("CFG", cfgString)
                                   .UsingJobData("BinarySerialization", config.BinarySerialization)
                                   .UsingJobData("AccessKey", config.AccessKey)
                                   .UsingJobData("SecretKey", config.GetSecretKey())
                                   .UsingJobData("AWSProfile", config.AWSProfile)
                                   .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                                   .UsingJobData("Destination", config.DestinationConnection.ConnectionString)
                                   .UsingJobData("SourceType", JsonConvert.SerializeObject(cfg.SourceConnection.Type))
                                   .UsingJobData("DestinationType", JsonConvert.SerializeObject(config.DestinationConnection.Type))
                                  .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                      .WithSimpleSchedule(b => b
                                          .WithIntervalInSeconds(1)
                                          .WithRepeatCount(0)).StartAt(DateTime.Now.AddSeconds(5))
                                      .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                  .WithCronSchedule(s.CronSchedule)
                                  .Build()
                                  )); ; ;
                        }
                        if (cfg.SchemaSnapshotDBs !=null && cfg.SchemaSnapshotDBs.Length > 0)
                        {
                            var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                 q.WithJob(() =>
                 JobBuilder.Create<SchemaSnapshotJob>()
                      .UsingJobData("CFG", cfgString)
                      .UsingJobData("AccessKey", config.AccessKey)
                      .UsingJobData("SecretKey", config.GetSecretKey())
                      .UsingJobData("AWSProfile", config.AWSProfile)
                      .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                      .UsingJobData("Destination", config.DestinationConnection.ConnectionString)
                      .UsingJobData("DestinationType", JsonConvert.SerializeObject(config.DestinationConnection.Type))
                      .UsingJobData("Options", JsonConvert.SerializeObject(config.SchemaSnapshotOptions))
                      .UsingJobData("SchemaSnapshotDBs", cfg.SchemaSnapshotDBs)
                     .Build())
                 .AddTrigger(() => TriggerBuilder.Create()
                         .WithSimpleSchedule(b => b
                             .WithIntervalInSeconds(1)
                             .WithRepeatCount(0)).StartAt(DateTime.Now.AddSeconds(5))
                         .Build())
                 .AddTrigger(() => TriggerBuilder.Create()
                     .WithCronSchedule(cfg.SchemaSnapshotCron)
                     .Build()
                     )); ; ;
                        }
                    }
                });

                //Setup Account that window service use to run.  
                // configure.RunAsPrompt();
                //configure.RunAsLocalSystem();
                configure.SetServiceName("DBAChecksService");
                configure.SetDisplayName("DBAChecksService");
                configure.SetDescription("Collect data from SQL Instances");
            });
        }
    }
}
