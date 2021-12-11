using DBADash;
using Newtonsoft.Json;
using Polly;
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
using System.Timers;
using static DBADash.DBADashConnection;
using Serilog;
using System.IO;

namespace DBADashService
{

    public class ScheduleService
    {
        private readonly IScheduler scheduler;
        public readonly CollectionConfig config;
        System.Timers.Timer azureScanForNewDBsTimer;
        System.Timers.Timer folderCleanupTimer;
        CollectionSchedules schedules;

        public  ScheduleService()
        {
            config = SchedulerServiceConfig.Config;
            schedules = config.GetSchedules();
            if (config.CollectionSchedules != null)
            {
                Log.Information("Custom schedules set at agent level");
            }

            Int32 threads = config.ServiceThreads;
            if (threads < 1)
            {
                threads = 10;
                Log.Logger.Information("Threads {threadcount} (default)", threads);
            }
            else
            {
                Log.Logger.Information("Threads {threadcount} (user)", threads);
            }
            
            NameValueCollection props = new NameValueCollection
        {
            { "quartz.serializer.type", "binary" },
            { "quartz.scheduler.instanceName", "DBADashScheduler" },
            { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
            { "quartz.threadPool.threadCount", threads.ToString() },
            { "quartz.threadPool.maxConcurrency", threads.ToString() }
            };
            
            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            scheduler = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void upgradeDB()
        {
            foreach (var d in config.AllDestinations.Where(dest => dest.Type == ConnectionType.SQL))
            {
                Log.Logger.Information("Version check for repository database {connection}", d.ConnectionForPrint);
                DBValidations.DBVersionStatus status = null;
                Policy.Handle<Exception>()
                  .WaitAndRetry(new[]
                  {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(20),
                    TimeSpan.FromSeconds(60)
                  }, (exception, timeSpan, context) =>
                  {
                      Log.Error(exception,"Version check for repository database failed");
                  }).Execute(() => status = DBValidations.VersionStatus(d.ConnectionString));

                if (status.VersionStatus == DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                {
                    Log.Warning("This version of the app is older than the repository database and should be upgraded. DB {dbversion}.  App {appversion}", status.DACVersion, status.DBVersion);
                }
                else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    if (config.AutoUpdateDatabase)
                    {
                        Log.Information("Create repository database");
                        DBValidations.UpgradeDBAsync(d.ConnectionString).Wait();
                        Log.Information("Repository database created");
                    }
                    else
                    {
                        throw new Exception("Repository database needs to be created.  Use to service configuration tool to deploy the repository database.");
                    }
                }
                else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.UpgradeRequired)
                {
                    if (config.AutoUpdateDatabase)
                    {
                        Log.Information("Upgrade DB from {oldversion} to {newversion}", status.DBVersion.ToString(), status.DACVersion.ToString());
                        DBValidations.UpgradeDBAsync(d.ConnectionString).Wait();
                        status = DBValidations.VersionStatus(d.ConnectionString);
                        if (status.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                        {
                            Log.Information("Repository DB upgrade completed");
                        }
                        else
                        {
                            throw new Exception(string.Format("Database version is {0} is not expected following upgrade to {1}", status.DBVersion.ToString(), status.DACVersion.ToString()));
                        }
                    }
                    else
                    {
                        throw new Exception("Database upgrade is required.  Enable auto updates or run the service configuration tool to update.");
                    }
                }
                else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                {
                    Log.Information("Repository database version check OK {version}", status.DBVersion.ToString());
                }

            }
        }

        public void Start()
        {
            scheduler.Start();
            upgradeDB();
            try
            {
                scheduleJobsAsync().Wait();
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error scheduling collections.  Please check configuration.");
                throw;
            }
        }

        public void Stop()
        {
            Log.Information("Pause schedules...");
            scheduler.Standby().ConfigureAwait(false).GetAwaiter().GetResult();
            Log.Information("Wait for jobs to complete...");
            int waitCount = 0;
            while (scheduler.GetCurrentlyExecutingJobs().ConfigureAwait(false).GetAwaiter().GetResult().Count > 0)
            {
                System.Threading.Thread.Sleep(500);
                waitCount++;
                if (waitCount > 60)
                {
                    Log.Warning("Wait operation timeout");
                    break;
                }
            }
            Log.Information("Remove Event Sessions");
            removeEventSessionsAsync().Wait();
            Log.Information("Shutdown Scheduler");
            scheduler.Shutdown().ConfigureAwait(false).GetAwaiter().GetResult();
            Log.Information("Shutdown complete");

        }

        private async Task scheduleAndRunMaintenanceJobAsync()
        {
            Int32 i = 0;
            foreach (DBADashConnection d in config.AllDestinations.Where(cn => cn.Type == ConnectionType.SQL))
            {
                i += 1;
                string maintenanceCron = config.GetMaintenanceCron();

                IJobDetail job = JobBuilder.Create<MaintenanceJob>()
                        .WithIdentity("MaintenanceJob" + i.ToString())
                        .UsingJobData("ConnectionString", d.ConnectionString)
                        .Build();
                ITrigger trigger = TriggerBuilder.Create()
                .StartNow()
                .WithCronSchedule(maintenanceCron)
                .Build();

                await scheduler.ScheduleJob(job, trigger);
                await scheduler.TriggerJob(job.Key);
            }
        }

        private async Task scheduleAndRunAzureScanAsync()
        {
            if (config.ScanForAzureDBs)
            {
                Log.Information("Running Scan for Azure DBs.");
                try
                {
                    await scanForAzureDBsAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error scanning for Azure DBs");
                }
                if (config.ScanForAzureDBsInterval > 0)
                {
                    Log.Information("Schedule Scan for new Azure DBS every {scaninterval} seconds", config.ScanForAzureDBsInterval);
                    azureScanForNewDBsTimer = new System.Timers.Timer
                    {
                        Enabled = true,
                        Interval = config.ScanForAzureDBsInterval * 1000
                    };
                    azureScanForNewDBsTimer.Elapsed += new System.Timers.ElapsedEventHandler(scanForAzureDBs);
                }
            }

        }

        private async Task scanForAzureDBsAsync(DBADashSource src)
        {
            if (config.ScanForAzureDBs)
            {
                bool isAzureDBMaster = false;

                try
                {
                    isAzureDBMaster = src.SourceConnection.ConnectionInfo.IsAzureMasterDB;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ConnectionInfo Error {0}", src.SourceConnection.ConnectionForPrint);
                }
                try
                {
                    if (isAzureDBMaster)
                    {
                        await scheduleCollectionsAsync(config.AddAzureDBs(src));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ConnectionInfo Error {0}", src.SourceConnection.ConnectionForPrint);
                }
            }
        }

        private async Task scanForAzureDBsAsync()
        {
            Log.Information("Scan for new azure DBs");
            await scheduleCollectionsAsync(config.AddAzureDBs());
        }

        private void scanForAzureDBs(object sender, ElapsedEventArgs e)
        {
            scanForAzureDBsAsync().Wait();
        }

        private async Task scheduleSourceAsync(DBADashSource src)
        {              
            string cfgString = JsonConvert.SerializeObject(src);
            CollectionSchedules srcSchedule;
            if (src.CollectionSchedules != null && src.CollectionSchedules.Count > 0)
            {
                srcSchedule = CollectionSchedules.Combine(schedules, src.CollectionSchedules);
                Log.Information("Custom schedule defined for instance: {instance}", src.SourceConnection.ConnectionForPrint);
            }
            else
            {
                srcSchedule = schedules;
            }

            IJobDetail serviceStartJob = GetJob(CollectionSchedules.DefaultSchedules.OnServiceStartCollection, src, cfgString);
            scheduler.AddJob(serviceStartJob, true).ConfigureAwait(false).GetAwaiter().GetResult();
            await scheduler.TriggerJob(serviceStartJob.Key);
            if (src.SourceConnection.Type == ConnectionType.SQL)
            {
                foreach (var s in srcSchedule.GroupedBySchedule)
                {
                    IJobDetail job = GetJob(s.Value, src, cfgString);
                    Log.Information("Add schedule for {source} to collect {collection} on schedule {schedule}", src.SourceConnection.ConnectionForPrint, s.Value, s.Key);
                    scheduleJob(s.Key, job);
                }
                if (src.SchemaSnapshotDBs != null && src.SchemaSnapshotDBs.Length > 0)
                {
                    var snapshotSchedule = srcSchedule[CollectionType.SchemaSnapshot];
                    if (!string.IsNullOrEmpty(snapshotSchedule.Schedule))
                    {
                        Log.Information("Add schedule for {source} to collect Schema Snapshots on schedule {schedule}", src.SourceConnection.ConnectionForPrint, snapshotSchedule.Schedule);
                        IJobDetail job = JobBuilder.Create<SchemaSnapshotJob>()
                                .UsingJobData("Source", src.SourceConnection.ConnectionString)
                                .UsingJobData("CFG", cfgString)
                                .UsingJobData("SchemaSnapshotDBs", src.SchemaSnapshotDBs)
                                    .Build();

                        scheduleJob(snapshotSchedule.Schedule, job);

                        if (snapshotSchedule.RunOnServiceStart)
                        {
                            await scheduler.TriggerJob(job.Key);
                        }
                    }
                }
            }
            else if (src.SourceConnection.Type == ConnectionType.Directory || src.SourceConnection.Type == ConnectionType.AWSS3)
            {
                IJobDetail job = GetJob(null, src, cfgString);
                Log.Information("Add schedule for {source} to import on schedule {schedule}", src.SourceConnection.ConnectionForPrint, CollectionSchedule.DefaultImportSchedule);
                scheduleJob(CollectionSchedule.DefaultImportSchedule.Schedule, job);
            }
        }

        private async Task scheduleJobsAsync()
        {
            Log.Information("Agent Version {version}", Assembly.GetEntryAssembly().GetName().Version);

            await scheduleAndRunMaintenanceJobAsync();

            await scheduleCollectionsAsync(config.SourceConnections.ToList());

            _ = scheduleAndRunAzureScanAsync();
       
            folderCleanup();
            folderCleanupTimer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 14400000 // 4hrs
            };
            folderCleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(folderCleanup);
        }

        private async Task scheduleCollectionsAsync(List<DBADashSource> connections)
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 30
            };
            await Parallel.ForEachAsync(connections, options, async (src, ct) =>
            {
                await removeEventSessionAsync(src);
                await scheduleSourceAsync(src);
                await scanForAzureDBsAsync(src);
            });

        }

        private IJobDetail GetJob(CollectionType[]types,DBADashSource src,string cfgString)
        {
            return JobBuilder.Create<DBADashJob>()
                     .UsingJobData("Type", JsonConvert.SerializeObject(types))
                     .UsingJobData("Source", src.ConnectionString)
                     .UsingJobData("CFG", cfgString)
                     .UsingJobData("Job_instance_id", 0)
                     .UsingJobData("SourceType", JsonConvert.SerializeObject(src.SourceConnection.Type))
                     .StoreDurably(true)
                    .Build();
        }

        private void scheduleJob(string schedule, IJobDetail job)
        {
            ITrigger trigger;
            if (int.TryParse(schedule, out int seconds)) // If it's an int, schedule is interval in seconds, otherwise use cron trigger
            {
                trigger = TriggerBuilder.Create()
                 .StartNow()
                 .WithSimpleSchedule(x =>
                     x.WithIntervalInSeconds(seconds)
                    .RepeatForever()
                    )
                 .Build();
            }
            else
            {
                trigger = TriggerBuilder.Create()
                 .StartNow()
                 .WithCronSchedule(schedule)
                 .Build();
            }
            scheduler.ScheduleJob(job, trigger).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task removeEventSessionsAsync()
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 30
            };
            await Parallel.ForEachAsync(config.SourceConnections, options, async (src, ct) =>
            {
                await removeEventSessionAsync(src);
            });
        }

        private async Task removeEventSessionAsync(DBADashSource src)
        {
            if (src.SourceConnection.Type == ConnectionType.SQL)
            {
                try
                {
                    var collector = new DBCollector(src, config.ServiceName);
                    if (src.PersistXESessions)
                    {
                        Log.Logger.Information("Stop DBADash event sessions for {connection}", src.SourceConnection.ConnectionForPrint);
                        await collector.StopEventSessionsAsync();
                    }
                    else
                    {
                        Log.Logger.Information("Remove DBADash event sessions for {connection}", src.SourceConnection.ConnectionForPrint);
                        await collector.RemoveEventSessionsAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error Stop/Remove DBADash event sessions for {connection}", src.SourceConnection.ConnectionForPrint);
                }
            }
        }

        private static void folderCleanup(object sender, ElapsedEventArgs e)
        {
            folderCleanup();
        }

        private static void folderCleanup()
        {
            try
            {
                if (Directory.Exists(SchedulerServiceConfig.FailedMessageFolder))
                {
                    Log.Information("Maintenance: Failed Message Folder cleanup");
                    (from f in new DirectoryInfo(SchedulerServiceConfig.FailedMessageFolder).GetFiles()
                     where f.LastWriteTime < DateTime.Now.Subtract(TimeSpan.FromDays(7))
                     && (f.Extension.ToLower() == ".xml" || f.Extension.ToLower() == ".json" || f.Extension.ToLower() == ".bin")
                     && f.Name.ToLower().StartsWith("dbadash")
                     select f
                    ).ToList()
                        .ForEach(f => f.Delete());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Maintenance: FailedMessageFolderCleanup");
            }
        }
    }
}
