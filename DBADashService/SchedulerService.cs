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
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DBADash.Messaging;
using static DBADash.DBADashConnection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace DBADashService
{
    public class ScheduleService : BackgroundService
    {
        private readonly IScheduler scheduler;
        public readonly CollectionConfig config;
        private System.Timers.Timer azureScanForNewDBsTimer;
        private System.Timers.Timer folderCleanupTimer;
        private readonly CollectionSchedules schedules;
        private MessageProcessing messageProcessing;
        public static readonly ConcurrentDictionary<string, SemaphoreSlim> Locker = new();

        // Thread-safe one-time logging using Lazy<T>
        private static readonly Lazy<bool> _logAvailableProcsRemoval = new Lazy<bool>(() =>
        {
            Log.Information("Removing AvailableProcs collection. Not applicable with current messaging configuration");
            return true;
        });

        public ScheduleService()
        {
            config = SchedulerServiceConfig.Config;
            schedules = config.GetSchedules();
            if (config.CollectionSchedules != null)
            {
                Log.Information("Custom schedules set at agent level");
            }

            var threads = config.ServiceThreads;
            if (threads < 1)
            {
                threads = 10;
                Log.Logger.Information("Threads {threadCount} (default)", threads);
            }
            else
            {
                Log.Logger.Information("Threads {threadCount} (user)", threads);
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

        private async Task UpgradeDBAsync()
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

                switch (status.VersionStatus)
                {
                    case DBValidations.DBVersionStatusEnum.AppUpgradeRequired:
                        Log.Warning("This version of the app is older than the repository database and should be upgraded. DB {dbVersion}.  App {appVersion}", status.DACVersion, status.DBVersion);
                        break;

                    case DBValidations.DBVersionStatusEnum.CreateDB when config.AutoUpdateDatabase:
                        Log.Information("Validating destination");
                        CollectionConfig.ValidateDestination(d);
                        Log.Information("Create repository database");
                        await DBValidations.UpgradeDBAsync(d.ConnectionString);
                        Log.Information("Repository database created");
                        break;

                    case DBValidations.DBVersionStatusEnum.CreateDB:
                        throw new Exception("Repository database needs to be created.  Use to service configuration tool to deploy the repository database.");
                    case DBValidations.DBVersionStatusEnum.UpgradeRequired when config.AutoUpdateDatabase:
                        {
                            Log.Information("Validating destination");
                            CollectionConfig.ValidateDestination(d);
                            Log.Information("Upgrade DB from {oldVersion} to {newVersion}", status.DBVersion.ToString(), status.DACVersion.ToString());
                            await DBValidations.UpgradeDBAsync(d.ConnectionString);
                            status = DBValidations.VersionStatus(d.ConnectionString);
                            if (status.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                            {
                                Log.Information("Repository DB upgrade completed");
                            }
                            else
                            {
                                throw new Exception(
                                    $"Database version is {status.DBVersion} is not expected following upgrade to {status.DACVersion}");
                            }

                            break;
                        }
                    case DBValidations.DBVersionStatusEnum.UpgradeRequired:
                        throw new Exception("Database upgrade is required.  Enable auto updates or run the service configuration tool to update.");
                    case DBValidations.DBVersionStatusEnum.OK:
                        Log.Information("Repository database version check OK {version}", status.DBVersion.ToString());
                        break;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(Stop);
            var offlineCheckTask = OfflineInstances.AddIfOffline(config.SourceConnections, stoppingToken);
            await scheduler.Start(stoppingToken);
            try
            {
                await UpgradeDBAsync();
            }
            catch (Exception ex)
            {
                Program.LogFatalError(new Exception("Upgrade DB failed", ex));
                throw;
            }
            try
            {
                Log.Information("Checking connections");
                await offlineCheckTask;
                var offlineCount = OfflineInstances.OfflineInstanceCount;
                if (offlineCount > 0)
                {
                    Log.Warning("{offlineCount} connections are offline", offlineCount);
                }
                else
                {
                    Log.Information("All source connections are online");
                }
                _ = Task.Run(() => OfflineInstances.ManageOfflineInstances(config, stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckConnectionsAsync error");
            }
            try
            {
                await ScheduleJobsAsync().WaitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Program.LogFatalError(new Exception("Error scheduling collections.  Please check configuration.", ex));
                throw;
            }
            try
            {
                await UpdateBuildReferenceFromFile().WaitAsync(stoppingToken);
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

        public async void Stop()
        {
            Log.Information("Pause schedules...");
            await scheduler.Standby();
            Log.Information("Wait for jobs to complete...");
            var waitCount = 0;
            while ((await scheduler.GetCurrentlyExecutingJobs()).Count > 0)
            {
                Thread.Sleep(500);
                waitCount++;
                if (waitCount > 60)
                {
                    Log.Warning("Wait operation timeout");
                    break;
                }
            }
            Log.Information("Remove Event Sessions");
            await RemoveEventSessionsAsync();
            Log.Information("Shutdown Scheduler");
            await scheduler.Shutdown();
            Log.Information("Shutdown complete");
        }

        private async Task ScheduleAndRunMaintenanceJobAsync()
        {
            var i = 0;
            foreach (var d in config.AllDestinations.Where(cn => cn.Type == ConnectionType.SQL))
            {
                i += 1;
                var maintenanceCron = config.GetMaintenanceCron();
                var job = JobBuilder.Create<MaintenanceJob>()
                        .WithIdentity("MaintenanceJob" + i)
                        .UsingJobData("PurgeDataCommandTimeout", config.PurgeDataCommandTimeout ?? 1200)
                        .UsingJobData("AddPartitionsCommandTimeout", config.AddPartitionsCommandTimeout ?? 300)
                        .UsingJobData("ConnectionString", d.ConnectionString)
                        .Build();
                var trigger = TriggerBuilder.Create()
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
            foreach (var d in config.AllDestinations.Where(cn => cn.Type == ConnectionType.SQL))
            {
                i += 1;
                var job = JobBuilder.Create<SummaryRefreshJob>()
                    .WithIdentity("SummaryRefreshJob" + i)
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
                    Log.Information("Schedule Scan for new Azure DBS every {scanInterval} seconds", config.ScanForAzureDBsInterval);
                    azureScanForNewDBsTimer = new System.Timers.Timer
                    {
                        Enabled = true,
                        Interval = config.ScanForAzureDBsInterval * 1000
                    };
                    azureScanForNewDBsTimer.Elapsed += ScanForAzureDBs;
                }
            }
        }

        private async Task ScanForAzureDBsAsync(DBADashSource src)
        {
            if (config.ScanForAzureDBs && src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src))
            {
                var isAzureDBMaster = false;

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

        private async void ScanForAzureDBs(object sender, ElapsedEventArgs e)
        {
            await ScanForAzureDBsAsync();
        }

        private CollectionType[] RemoveNotApplicableCollections(CollectionType[] types)
        {
            if (ShouldKeepAvailableProcs())
            {
                return types;
            }

            if (!types.Contains(CollectionType.AvailableProcs))
            {
                return types;
            }

            LogAvailableProcsRemovalOnce();
            return types.Where(type => type != CollectionType.AvailableProcs).ToArray();
        }

        private static void LogAvailableProcsRemovalOnce()
        {
            _ = _logAvailableProcsRemoval.Value;
        }

        private bool ShouldKeepAvailableProcs()
        {
            return config.EnableMessaging &&
                   (!string.IsNullOrEmpty(config.AllowedCustomProcs) ||
                    !string.IsNullOrEmpty(config.AllowedScripts));
        }

        private async Task ScheduleSourceAsync(DBADashSource src)
        {
            var cfgString = JsonConvert.SerializeObject(src);
            CollectionSchedules srcSchedule;
            if (src.CollectionSchedules is { Count: > 0 })
            {
                srcSchedule = CollectionSchedules.Combine(schedules, src.CollectionSchedules);
                Log.Information("Custom schedule defined for instance: {instance}", src.SourceConnection.ConnectionForPrint);
            }
            else
            {
                srcSchedule = schedules;
            }
            var customCollections = src.CustomCollections.CombineCollections(config.CustomCollections);

            var onStartCollections = RemoveNotApplicableCollections(srcSchedule.OnServiceStartCollection);

            if (srcSchedule.OnServiceStartCollection.Length > 0)
            {
                Log.Information("Trigger on startup collections for {source} to collect {collection}", src.SourceConnection.ConnectionForPrint, onStartCollections);
                var onStartCustom = customCollections
                    .Where(c => c.Value.RunOnServiceStart)
                    .ToDictionary(c => c.Key, c => c.Value);

                var serviceStartJob = GetJob(onStartCollections, src, cfgString, onStartCustom);
                scheduler.AddJob(serviceStartJob, true).ConfigureAwait(false).GetAwaiter().GetResult();
                await scheduler.TriggerJob(serviceStartJob.Key);
            }
            switch (src.SourceConnection.Type)
            {
                case ConnectionType.SQL:
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
                            if (string.IsNullOrEmpty(s.Key)) continue; /* Collection is disabled */
                            var collections = RemoveNotApplicableCollections(s.Value);

                            var custom = customCollections
                                .Where(c => c.Value.Schedule == s.Key)
                                .ToDictionary(c => c.Key, c => c.Value);
                            var job = GetJob(collections, src, cfgString, custom);
                            Log.Information("Add schedule for {source} to collect {collection},{custom} on schedule {schedule}", src.SourceConnection.ConnectionForPrint, collections, custom.Keys, s.Key);
                            ScheduleJob(s.Key, job);
                        }

                        if (src.SchemaSnapshotDBs is { Length: > 0 })
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

                        break;
                    }
                case ConnectionType.Directory or ConnectionType.AWSS3:
                    {
                        var job = GetJob(null, src, cfgString, null);
                        Log.Information("Add schedule for {source} to import on schedule {schedule}", src.SourceConnection.ConnectionForPrint, CollectionSchedule.DefaultImportSchedule);
                        ScheduleJob(CollectionSchedule.DefaultImportSchedule.Schedule, job);
                        break;
                    }
            }
        }

        private SQSMessageProcessing sqsMessageProcessing;

        private async Task ScheduleJobsAsync()
        {
            Log.Information("Agent Version {version}", Assembly.GetEntryAssembly().GetName().Version);
            messageProcessing = new MessageProcessing(config);
            var messageTask = messageProcessing.ScheduleMessaging();
            if (!messageTask.Wait(TimeSpan.FromSeconds(60)))
            {
                Log.Warning("Message processing setup timeout.  Proceeding with collection schedules");
            }

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
            folderCleanupTimer.Elapsed += FolderCleanup;
            await messageTask;
            if (!string.IsNullOrEmpty(config.ServiceSQSQueueUrl))
            {
                sqsMessageProcessing = new SQSMessageProcessing(config);
                _ = sqsMessageProcessing.ProcessSQSQueue(DBADashAgent.GetCurrent().AgentIdentifier);
            }

            Log.Information("Alert processing is {IsEnabled}", config.ProcessAlerts ? "enabled" : "disabled");
            if (config.ProcessAlerts)
            {
                foreach (var alertProcessing in config.SQLDestinations.Select(dest => new AlertProcessing(dest)
                {
                    NotificationProcessingStartupDelaySeconds = config.AlertProcessingStartupDelaySeconds ?? CollectionConfig.DefaultAlertProcessingStartupDelaySeconds,
                    NotificationProcessingFrequencySeconds = config.AlertProcessingFrequencySeconds ?? CollectionConfig.DefaultAlertProcessingFrequencySeconds
                }))
                {
                    _ = Task.Run(() => alertProcessing.ProcessAlerts());
                }
            }
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
                     .StoreDurably()
                    .Build();
        }

        private void ScheduleJob(string schedule, IJobDetail job)
        {
            ITrigger trigger;
            ArgumentNullException.ThrowIfNull(schedule);
            if (int.TryParse(schedule, out var seconds)) // If it's an int, schedule is interval in seconds, otherwise use cron trigger
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
            if (src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src))
            {
                try
                {
                    if (src.SourceConnection.ConnectionInfo.IsXESupported)
                    {
                        var collector = await DBCollector.CreateAsync(src, config.ServiceName);
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
                     && (f.Extension.Equals(".xml", StringComparison.CurrentCultureIgnoreCase) || f.Extension.Equals(".json", StringComparison.CurrentCultureIgnoreCase) || f.Extension.Equals(".bin", StringComparison.CurrentCultureIgnoreCase))
                     && f.Name.StartsWith("dbadash", StringComparison.CurrentCultureIgnoreCase)
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