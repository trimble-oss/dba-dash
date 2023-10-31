using DBADash;
using Newtonsoft.Json;
using Polly;
using Quartz;
using Quartz.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    public class ScheduleService
    {
        private readonly IScheduler scheduler;
        public readonly CollectionConfig config;
        private System.Timers.Timer azureScanForNewDBsTimer;
        private System.Timers.Timer folderCleanupTimer;
        private readonly CollectionSchedules schedules;

        public ScheduleService()
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

            NameValueCollection props = new()
            {
            { "quartz.serializer.type", "binary" },
            { "quartz.scheduler.instanceName", "DBADashScheduler" },
            { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
            { "quartz.threadPool.threadCount", threads.ToString() },
            { "quartz.threadPool.maxConcurrency", threads.ToString() }
            };

            StdSchedulerFactory factory = new(props);
            scheduler = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void UpgradeDB()
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
                      Log.Error(exception, "Version check for repository database failed");
                  }).Execute(() => status = DBValidations.VersionStatus(d.ConnectionString));

                if (status.VersionStatus == DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                {
                    Log.Warning("This version of the app is older than the repository database and should be upgraded. DB {dbversion}.  App {appversion}", status.DACVersion, status.DBVersion);
                }
                else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    if (config.AutoUpdateDatabase)
                    {
                        Log.Information("Validating destination");
                        CollectionConfig.ValidateDestination(d);
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
                        Log.Information("Validating destination");
                        CollectionConfig.ValidateDestination(d);
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
            try
            {
                UpgradeDB();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "upgradeDB failed");
                throw;
            }
            try
            {
                ScheduleJobsAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scheduling collections.  Please check configuration.");
                throw;
            }

            try
            {
                UpdateBuildReferenceFromFile().Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating build reference");
            }
        }

        public async Task UpdateBuildReferenceFromFile()
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(appDirectory, "BuildReference.json");
            if (!File.Exists(filePath)) return;
            foreach (var d in config.AllDestinations.Where(cn => cn.Type == ConnectionType.SQL))
            {
                Log.Information("Updating BuildReference {connection}", d.ConnectionForPrint);
                var jsonBuildReference = await File.ReadAllTextAsync(filePath);
                await BuildReference.UpdateBuildReference(d.ConnectionString, jsonBuildReference);
            }
            Log.Debug("Remove {filePath}", filePath);
            File.Delete(filePath);
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
            RemoveEventSessionsAsync().Wait();
            Log.Information("Shutdown Scheduler");
            scheduler.Shutdown().ConfigureAwait(false).GetAwaiter().GetResult();
            Log.Information("Shutdown complete");
        }

        private async Task ScheduleAndRunMaintenanceJobAsync()
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

        private void ScheduleSummaryRefresh()
        {
            if (string.IsNullOrEmpty(config.SummaryRefreshCron)) return;
            var i = 0;
            foreach (DBADashConnection d in config.AllDestinations.Where(cn => cn.Type == ConnectionType.SQL))
            {
                i += 1;
                IJobDetail job = JobBuilder.Create<SummaryRefreshJob>()
                    .WithIdentity("SummaryRefreshJob" + i.ToString())
                    .UsingJobData("ConnectionString", d.ConnectionString)
                    .Build();
                Log.Information("Schedule summary refresh on schedule {schedule}", config.SummaryRefreshCron);
                ScheduleJob(config.SummaryRefreshCron, job);
            }
        }

        private async Task ScheduleAndRunAzureScanAsync()
        {
            if (config.ScanForAzureDBs)
            {
                Log.Information("Running Scan for Azure DBs.");
                try
                {
                    await ScanForAzureDBsAsync();
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
                    azureScanForNewDBsTimer.Elapsed += new System.Timers.ElapsedEventHandler(ScanForAzureDBs);
                }
            }
        }

        private async Task ScanForAzureDBsAsync(DBADashSource src)
        {
            if (config.ScanForAzureDBs && src.SourceConnection.Type == ConnectionType.SQL)
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
                        await ScheduleCollectionsAsync(config.AddAzureDBs(src));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ConnectionInfo Error {0}", src.SourceConnection.ConnectionForPrint);
                }
            }
        }

        private async Task ScanForAzureDBsAsync()
        {
            Log.Information("Scan for new azure DBs");
            await ScheduleCollectionsAsync(config.AddAzureDBs());
        }

        private void ScanForAzureDBs(object sender, ElapsedEventArgs e)
        {
            ScanForAzureDBsAsync().Wait();
        }

        private async Task ScheduleSourceAsync(DBADashSource src)
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
            var customCollections = src.CustomCollections.CombineCollections(config.CustomCollections);

            if (srcSchedule.OnServiceStartCollection.Length > 0)
            {
                Log.Information("Trigger on startup collections for {source} to collect {collection}", src.SourceConnection.ConnectionForPrint, srcSchedule.OnServiceStartCollection);
                var onStartCustom = customCollections
                    .Where(c => c.Value.RunOnServiceStart)
                    .ToDictionary(c => c.Key, c => c.Value);

                var serviceStartJob = GetJob(srcSchedule.OnServiceStartCollection, src, cfgString, onStartCustom);
                scheduler.AddJob(serviceStartJob, true).ConfigureAwait(false).GetAwaiter().GetResult();
                await scheduler.TriggerJob(serviceStartJob.Key);
            }
            if (src.SourceConnection.Type == ConnectionType.SQL)
            {
                var groupedSchedule = srcSchedule.GroupedBySchedule;
                foreach (var schedule in customCollections
                     .GroupBy(c => c.Value.Schedule)
                     .Where(c => !groupedSchedule.ContainsKey(c.Key))
                     .Select(c => c.Key))
                {
                    groupedSchedule.Add(schedule, Array.Empty<CollectionType>());
                }

                foreach (var s in groupedSchedule)
                {
                    var custom = customCollections
                        .Where(c => c.Value.Schedule == s.Key)
                        .ToDictionary(c => c.Key, c => c.Value);
                    var job = GetJob(s.Value, src, cfgString, custom);
                    Log.Information("Add schedule for {source} to collect {collection},{custom} on schedule {schedule}", src.SourceConnection.ConnectionForPrint, s.Value, custom.Keys, s.Key);
                    ScheduleJob(s.Key, job);
                }

                if (src.SchemaSnapshotDBs != null && src.SchemaSnapshotDBs.Length > 0)
                {
                    var snapshotSchedule = srcSchedule[CollectionType.SchemaSnapshot];
                    if (!string.IsNullOrEmpty(snapshotSchedule.Schedule))
                    {
                        Log.Information("Add schedule for {source} to collect Schema Snapshots on schedule {schedule}", src.SourceConnection.ConnectionForPrint, snapshotSchedule.Schedule);
                        var job = JobBuilder.Create<SchemaSnapshotJob>()
                                .UsingJobData("Source", src.SourceConnection.ConnectionString)
                                .UsingJobData("CFG", cfgString)
                                .UsingJobData("SchemaSnapshotDBs", src.SchemaSnapshotDBs)
                                    .Build();

                        ScheduleJob(snapshotSchedule.Schedule, job);

                        if (snapshotSchedule.RunOnServiceStart)
                        {
                            await scheduler.TriggerJob(job.Key);
                        }
                    }
                }
            }
            else if (src.SourceConnection.Type is ConnectionType.Directory or ConnectionType.AWSS3)
            {
                var job = GetJob(null, src, cfgString, null);
                Log.Information("Add schedule for {source} to import on schedule {schedule}", src.SourceConnection.ConnectionForPrint, CollectionSchedule.DefaultImportSchedule);
                ScheduleJob(CollectionSchedule.DefaultImportSchedule.Schedule, job);
            }
        }

        private async Task ScheduleJobsAsync()
        {
            Log.Information("Agent Version {version}", Assembly.GetEntryAssembly().GetName().Version);

            await ScheduleAndRunMaintenanceJobAsync();
            ScheduleSummaryRefresh();

            await ScheduleCollectionsAsync(config.SourceConnections.ToList());

            _ = ScheduleAndRunAzureScanAsync();

            FolderCleanup();
            folderCleanupTimer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 14400000 // 4hrs
            };
            folderCleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(FolderCleanup);
        }

        private async Task ScheduleCollectionsAsync(List<DBADashSource> connections)
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 30
            };
            await Parallel.ForEachAsync(connections, options, async (src, ct) =>
            {
                await RemoveEventSessionAsync(src);
                await Task.WhenAll(
                    ScheduleSourceAsync(src),
                    ScanForAzureDBsAsync(src)
                    );
            });
        }

        private static IJobDetail GetJob(CollectionType[] types, DBADashSource src, string cfgString, Dictionary<string, CustomCollection> customCollections)
        {
            return JobBuilder.Create<DBADashJob>()
                     .UsingJobData("Type", JsonConvert.SerializeObject(types))
                     .UsingJobData("Source", src.ConnectionString)
                     .UsingJobData("CFG", cfgString)
                     .UsingJobData("Job_instance_id", 0)
                     .UsingJobData("SourceType", JsonConvert.SerializeObject(src.SourceConnection.Type))
                     .UsingJobData("CustomCollections", JsonConvert.SerializeObject(customCollections))
                     .StoreDurably(true)
                    .Build();
        }

        private void ScheduleJob(string schedule, IJobDetail job)
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

        private async Task RemoveEventSessionsAsync()
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 30
            };
            await Parallel.ForEachAsync(config.SourceConnections, options, async (src, ct) =>
            {
                await RemoveEventSessionAsync(src);
            });
        }

        private async Task RemoveEventSessionAsync(DBADashSource src)
        {
            if (src.SourceConnection.Type == ConnectionType.SQL)
            {
                try
                {
                    if (src.SourceConnection.ConnectionInfo.IsXESupported)
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
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error Stop/Remove DBADash event sessions for {connection}", src.SourceConnection.ConnectionForPrint);
                }
            }
        }

        private static void FolderCleanup(object sender, ElapsedEventArgs e)
        {
            FolderCleanup();
        }

        private static void FolderCleanup()
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
                try
                {
                    BasicConfig.ClearOldConfigBackups(SchedulerServiceConfig.Config.ConfigBackupRetentionDays);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error removing old configs");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Maintenance: FailedMessageFolderCleanup");
            }
        }
    }
}