using DBADash;
using DBADash.InstanceMetadata;
using DBADash.Messaging;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Quartz;
using Quartz.Impl;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static DBADash.DBADashConnection;

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
        private CollectionWorkQueue workQueue;
        private readonly ConcurrentBag<Task> backgroundTasks = new();
        private CancellationTokenSource backgroundTasksCts;

        private static readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 4, // Will try 5 times total (initial + 4 retries)
                DelayGenerator = args =>
                {
                    var delays = new[] { 1, 5, 20, 60 };
                    var index = args.AttemptNumber;
                    var seconds = index < delays.Length ? delays[index] : 60;
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(seconds));
                }
            })
            .Build();

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
            // Initialize work queue if queue-based scheduling is enabled
            if (config.IsUseQueueBasedScheduling())
            {
                workQueue = new CollectionWorkQueue(config);
                ScheduledCollectionJob.Initialize(workQueue);
                Log.Information("Queue-based scheduling enabled");
            }
            else
            {
                Log.Information("Using traditional per-instance scheduling");
            }

            var schedulerThreads = config.GetSchedulerThreadCount();

            NameValueCollection props = new()
            {
            { "quartz.serializer.type", "binary" },
            { "quartz.scheduler.instanceName", "DBADashScheduler" },
            { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
            { "quartz.threadPool.threadCount", schedulerThreads.ToString() },
            { "quartz.threadPool.maxConcurrency", schedulerThreads.ToString() }
            };

            StdSchedulerFactory factory = new(props);
            scheduler = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the version status of the repository database. (Requires upgrade etc.).  If a deployment is in progress, it will wait for a period of time until the deployment is complete before returning the status.
        /// </summary>
        /// <param name="d">Destination connection</param>
        /// <returns></returns>
        private static async Task<DBValidations.DBVersionStatus> GetDBVersionStatus(DBADashConnection d)
        {
            DBValidations.DBVersionStatus status = null;

            await pipeline.ExecuteAsync(async _ =>
            {
                try
                {
                    var timeout = TimeSpan.FromSeconds(60);
                    var endTime = DateTime.UtcNow.Add(timeout);
                    var count = 0;
                    do
                    {
                        status = await DBValidations.VersionStatusAsync(d.ConnectionString);
                        if (!status.DeployInProgress) break; // Exit if deployment is not in progress
                        if (DateTime.UtcNow >=
                            endTime) // We've waited long enough.  It's possible that a previous upgrade was interrupted.
                        {
                            Log.Warning(
                                "Timeout waiting for DB deployment to complete.  It's possible that a previous DB deployment was interrupted or is still running but taking longer than expected on another DBA Dash service instance.");
                            status.VersionStatus =
                                DBValidations.DBVersionStatusEnum
                                    .UpgradeRequired; // Force the upgrade to re-run.  It's possible that a previous upgrade was interrupted.
                            break;
                        }

                        if (count == 0)
                        {
                            Log.Warning("Repository database deployment already appears to be in progress.");
                        }

                        Log.Information("Waiting {waitTime} seconds for completion before continuing",
                            Convert.ToInt32(endTime.Subtract(DateTime.UtcNow).TotalSeconds));
                        count++;
                        await Task.Delay(5000, _); // Wait for 5 seconds before checking again
                    } while (status.DeployInProgress);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Version check for repository database failed");
                    throw;
                }
                ;
            }, CancellationToken.None);

            return status;
        }

        private async Task UpgradeDBAsync()
        {
            foreach (var d in config.AllDestinations.Where(dest => dest.Type == ConnectionType.SQL))
            {
                Log.Logger.Information("Version check for repository database {connection}", d.ConnectionForPrint);
                DBValidations.DBVersionStatus status = null;
                status = await GetDBVersionStatus(d);

                switch (status.VersionStatus)
                {
                    case DBValidations.DBVersionStatusEnum.AppUpgradeRequired:
                        Log.Warning("This version of the app is older than the repository database and should be upgraded. DB {dbVersion}.  App {appVersion}", status.DACVersion, status.DBVersion);
                        break;

                    case DBValidations.DBVersionStatusEnum.CreateDB when config.AutoUpdateDatabase:
                        Log.Information("Validating destination");
                        await CollectionConfig.ValidateDestinationAsync(d);
                        Log.Information("Create repository database");
                        await DBValidations.UpgradeDBAsync(d.ConnectionString);
                        Log.Information("Repository database created");
                        break;

                    case DBValidations.DBVersionStatusEnum.CreateDB:
                        throw new Exception("Repository database needs to be created.  Use to service configuration tool to deploy the repository database.");
                    case DBValidations.DBVersionStatusEnum.UpgradeRequired when config.AutoUpdateDatabase:
                        {
                            Log.Information("Validating destination");
                            await CollectionConfig.ValidateDestinationAsync(d);
                            Log.Information("Upgrade DB from {oldVersion} to {newVersion}", status.DBVersion.ToString(), status.DACVersion.ToString());
                            await DBValidations.UpgradeDBAsync(d.ConnectionString);
                            status = await DBValidations.VersionStatusAsync(d.ConnectionString);
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
            // Create linked cancellation token for all background tasks (can be cancelled early during shutdown)
            backgroundTasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var offlineCheckTask = OfflineInstances.AddIfOffline(config.SourceConnections, stoppingToken);
            await scheduler.Start(stoppingToken);

            // Start work queue if using queue based scheduling
            if (config.IsUseQueueBasedScheduling())
            {
                workQueue.Start(stoppingToken);
            }
            try
            {
                await UpgradeDBAsync();
            }
            catch (Exception ex)
            {
                Program.LogFatalError(new Exception("Upgrade DB failed", ex));
                throw;
            }

            InstanceMetadataProviders.EnabledProviders = config.EnabledMetadataProviders;
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
                backgroundTasks.Add(Task.Run(() => OfflineInstances.ManageOfflineInstances(config, backgroundTasksCts.Token), backgroundTasksCts.Token));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckConnectionsAsync error");
            }
            try
            {
                await ScheduleJobsAsync(stoppingToken).WaitAsync(stoppingToken);
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
                await BuildReference.UpdateBuildReferenceAsync(d.ConnectionString, jsonBuildReference);
            }
            Log.Debug("Remove {filePath}", filePath);
            File.Delete(filePath);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            const int workQueueTimeout = 20;
            const int backgroundTaskTimeout = 20;

            // Cancel all background tasks immediately so they can start shutting down
            if (backgroundTasksCts != null)
            {
                Log.Information("Cancelling background tasks...");
                backgroundTasksCts.Cancel();
            }

            Log.Information("Pause schedules...");
            await scheduler.Standby();
            Log.Information("Wait for jobs to complete...");
            var waitCount = 0;
            while ((await scheduler.GetCurrentlyExecutingJobs()).Count > 0)
            {
                await Task.Delay(500, cancellationToken);
                waitCount++;
                if (waitCount > 60)
                {
                    Log.Warning("Wait operation timeout");
                    break;
                }
            }
            // Stop work queue if using queue based scheduling
            if (config.IsUseQueueBasedScheduling())
            {
                Log.Information("Stopping collection work queue...");
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(workQueueTimeout));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    await workQueue.StopAsync().WaitAsync(linkedCts.Token);
                    Log.Information("Collection work queue stopped");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.Warning("Collection work queue stop was cancelled by shutdown token");
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Collection work queue stop timed out after {timeout} seconds", (workQueueTimeout));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error stopping collection work queue");
                }
            }

            await RemoveEventSessionsAsync();

            // Wait for background tasks to complete (alert processing, offline instances, Azure scan, SQS)
            var tasksSnapshot = backgroundTasks.ToArray();

            try
            {
                if (tasksSnapshot.Length > 0)
                {
                    Log.Information("Waiting for {count} background task(s) to complete...", tasksSnapshot.Length);
                    try
                    {
                        var timeout = TimeSpan.FromSeconds(backgroundTaskTimeout);
                        await Task.WhenAll(tasksSnapshot).WaitAsync(timeout);
                        Log.Information("Background tasks completed");
                    }
                    catch (TimeoutException)
                    {
                        Log.Warning("Background tasks did not complete within {timeout} seconds", backgroundTaskTimeout);
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Information("Background tasks cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Background tasks completed with exceptions (expected during shutdown)");
                    }
                }
            }
            finally
            {
                backgroundTasksCts?.Dispose();
            }

            Log.Information("Shutdown Scheduler");
            await scheduler.Shutdown();
            Log.Information("Shutdown complete");

            await base.StopAsync(cancellationToken);
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
                        .UsingJobData("PurgeDataCommandTimeout", config.PurgeDataCommandTimeout ?? CollectionConfig.DefaultPurgeDataCommandTimeout)
                        .UsingJobData("AddPartitionsCommandTimeout", config.AddPartitionsCommandTimeout ?? CollectionConfig.DefaultAddPartitionsCommandTimeout)
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

        private async void ScanForAzureDBs(object sender, ElapsedEventArgs e)
        {
            await ScanForAzureDBsAsync();
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
                        var newSources = config.AddAzureDBs(src);
                        if (config.IsUseQueueBasedScheduling())
                        {
                            await ScheduleCollectionsWithQueueAsync(newSources.ToList());
                        }
                        else
                        {
                            await ScheduleCollectionsAsync(newSources.ToList());
                        }
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
            var newSources = config.AddAzureDBs();
            if (config.IsUseQueueBasedScheduling())
            {
                await ScheduleCollectionsWithQueueAsync(newSources.ToList());
            }
            else
            {
                await ScheduleCollectionsAsync(newSources.ToList());
            }
        }

        private IEnumerable<CollectionType> RemoveNotApplicableCollections(IEnumerable<CollectionType> types)
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
            return types.Where(type => type != CollectionType.AvailableProcs);
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

            if (config.EnabledMetadataProviders.Count == 0)
            {
                srcSchedule.Remove(CollectionType.InstanceMetadata);
            }
            var customCollections = src.CustomCollections.CombineCollections(config.CustomCollections);

            var onStartCollections = RemoveNotApplicableCollections(srcSchedule.OnServiceStartCollection).Where(typ => typ != CollectionType.SchemaSnapshot);

            if (srcSchedule.OnServiceStartCollection.Any())
            {
                Log.Information("Trigger on startup collections for {source} to collect {collection}", src.SourceConnection.ConnectionForPrint, onStartCollections);
                var onStartCustom = customCollections
                    .Where(c => c.Value.RunOnServiceStart)
                    .ToDictionary(c => c.Key, c => c.Value);

                var serviceStartJob = GetJob(onStartCollections.ToArray(), src, cfgString, onStartCustom, "OnStart");
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
                            var job = GetJob(collections.ToArray(), src, cfgString, custom, s.Key);
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
                                    .UsingJobData("Schedule", snapshotSchedule.Schedule)
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
                        var job = GetJob(null, src, cfgString, null, CollectionSchedule.DefaultImportSchedule.Schedule);
                        Log.Information("Add schedule for {source} to import on schedule {schedule}", src.SourceConnection.ConnectionForPrint, CollectionSchedule.DefaultImportSchedule);
                        ScheduleJob(CollectionSchedule.DefaultImportSchedule.Schedule, job);
                        break;
                    }
            }
        }

        private SQSMessageProcessing sqsMessageProcessing;

        private async Task ScheduleJobsAsync(CancellationToken stoppingToken)
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

            if (config.IsUseQueueBasedScheduling())
            {
                await ScheduleCollectionsWithQueueAsync(config.SourceConnections.ToList());
            }
            else
            {
                await ScheduleCollectionsAsync(config.SourceConnections.ToList());
            }

            backgroundTasks.Add(ScheduleAndRunAzureScanAsync());

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
                backgroundTasks.Add(sqsMessageProcessing.ProcessSQSQueue(DBADashAgent.GetCurrent().AgentIdentifier, backgroundTasksCts.Token));
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
                    var task = Task.Run(() => alertProcessing.ProcessAlerts(backgroundTasksCts.Token), backgroundTasksCts.Token);
                    backgroundTasks.Add(task);
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

        private CollectionSchedules GetCombinedSchedule(DBADashSource src)
        {
            var s = (src.CollectionSchedules is { Count: > 0 })
                ? CollectionSchedules.Combine(schedules, src.CollectionSchedules)
                : schedules;

            if (config.EnabledMetadataProviders.Count == 0)
            {
                s.Remove(CollectionType.InstanceMetadata);
            }

            return s;
        }

        private Dictionary<string, CollectionType[]> BuildGroupedSchedules(CollectionSchedules srcSchedule, Dictionary<string, CustomCollection> customCollections)
        {
            var grouped = srcSchedule.GroupedBySchedule;

            foreach (var schedule in customCollections
                .GroupBy(c => c.Value.Schedule)
                .Where(g => !grouped.ContainsKey(g.Key))
                .Select(g => g.Key))
            {
                grouped.Add(schedule, Array.Empty<CollectionType>());
            }

            return grouped;
        }

        private async Task ScheduleCollectionsWithQueueAsync(List<DBADashSource> connections)
        {
            // Schedule -> (Types, Sources, CustomCollectionsForSchedule)
            var scheduleGroups = new Dictionary<string, (CollectionType[] Types, List<DBADashSource> Sources, Dictionary<string, CustomCollection> Custom)>();

            foreach (var src in connections.Where(src => src.SourceConnection.Type == ConnectionType.SQL))
            {
                var srcSchedule = GetCombinedSchedule(src);
                // Compute once for this source and reuse
                var customCollections = src.CustomCollections.CombineCollections(config.CustomCollections);

                // Build combined grouped schedules (core + custom)
                var groupedSchedule = BuildGroupedSchedules(srcSchedule, customCollections);

                // Aggregate sources per schedule with consistent type sets
                foreach (var group in groupedSchedule)
                {
                    var scheduleKey = group.Key;
                    if (string.IsNullOrEmpty(scheduleKey))
                    {
                        continue;
                    }

                    var collections = RemoveNotApplicableCollections(group.Value).ToArray();

                    // Pre-filter custom collections for this schedule, compute once
                    var customForSchedule = customCollections
                        .Where(c => c.Value.Schedule == scheduleKey)
                        .ToDictionary(c => c.Key, c => c.Value);

                    var finalKey = BuildScheduleGroupKey(scheduleKey, collections, customForSchedule.Keys);

                    if (!scheduleGroups.TryGetValue(finalKey, out var bucket))
                    {
                        bucket = (collections, new List<DBADashSource>(), customForSchedule);
                        scheduleGroups[finalKey] = bucket;
                    }

                    bucket.Sources.Add(src);
                }

                // Enqueue on-start grouped by priority (reuse the precomputed customCollections)
                await EnqueueOnStartByPriorityAsync(src, srcSchedule, customCollections);
            }
            foreach (var src in connections.Where(src => src.SourceConnection.Type != ConnectionType.SQL))
            {
                // Non-SQL sources (Directory, AWSS3) - single import schedule
                var scheduleKey = CollectionSchedule.DefaultImportSchedule.Schedule;
                var finalKey = BuildScheduleGroupKey(scheduleKey, Array.Empty<CollectionType>(), null);
                if (!scheduleGroups.TryGetValue(finalKey, out var bucket))
                {
                    bucket = (Array.Empty<CollectionType>(), new List<DBADashSource>(), new Dictionary<string, CustomCollection>());
                    scheduleGroups[finalKey] = bucket;
                }
                bucket.Sources.Add(src);
            }

            // Create one scheduled job per unique schedule+types combination
            foreach (var kvp in scheduleGroups)
            {
                var schedule = ExtractScheduleFromGroupKey(kvp.Key);
                var (types, sources, customForSchedule) = kvp.Value;

                Log.Information("Creating scheduled job for {instanceCount} instances on schedule {schedule} with types {types}",
                    sources.Count, schedule, string.Join(", ", types.Select(t => t.ToString())));

                var job = JobBuilder.Create<ScheduledCollectionJob>()
                    .WithIdentity($"ScheduledCollection_{kvp.Key}")
                    .UsingJobData("Schedule", schedule)
                    .UsingJobData("Types", JsonConvert.SerializeObject(types))
                    .UsingJobData("CustomCollections", JsonConvert.SerializeObject(customForSchedule))
                    // no Sources in JobDataMap
                    .Build();

                // Upsert instances into the in-memory dictionary
                ScheduledCollectionJob.UpsertSources(job.Key.Name, sources);

                // Ensure job exists (no duplicate scheduling)
                EnsureScheduledJob(schedule, job);
            }
        }

        private async Task EnqueueOnStartByPriorityAsync(
    DBADashSource src,
    CollectionSchedules srcSchedule,
    Dictionary<string, CustomCollection> customCollections)
        {
            var onStartTypes = RemoveNotApplicableCollections(srcSchedule.OnServiceStartCollection).ToArray();
            if (onStartTypes.Length == 0)
            {
                return;
            }

            // SchemaSnapshot as a separate low-priority work item
            if (onStartTypes.Contains(CollectionType.SchemaSnapshot) && src.SchemaSnapshotDBs is { Length: > 0 })
            {
                var snapshotWorkItem = new SchemaSnapshotWorkItem
                {
                    Source = src,
                    Schedule = "OnStartup"
                };

                await workQueue.EnqueueAsync(snapshotWorkItem);
                onStartTypes = onStartTypes.Where(t => t != CollectionType.SchemaSnapshot).ToArray();

                var anyCustomOnStart = customCollections.Any(c => c.Value.RunOnServiceStart);
                if (onStartTypes.Length == 0 && !anyCustomOnStart)
                {
                    return; // only schema snapshot on start
                }
            }

            var typeGroups = onStartTypes
                .GroupBy(t =>
                {
                    var sched = srcSchedule.ContainsKey(t) ? srcSchedule[t].Schedule : null;
                    return ScheduledCollectionJob.ComputePriorityFromSchedule(sched);
                })
                .ToDictionary(g => g.Key, g => g.ToArray());

            var customOnStartGroups = customCollections
                .Where(c => c.Value.RunOnServiceStart)
                .GroupBy(kvp => ScheduledCollectionJob.ComputePriorityFromSchedule(kvp.Value?.Schedule))
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Key, x => x.Value));

            // Enqueue one work item per priority group (merge custom with same priority)
            foreach (var kvp in typeGroups)
            {
                customOnStartGroups.TryGetValue(kvp.Key, out var customForPriority);

                var workItem = new WorkItem
                {
                    Source = src,
                    Types = kvp.Value,
                    CustomCollections = customForPriority ?? new Dictionary<string, CustomCollection>(),
                    Schedule = "OnStartup",
                    Priority = kvp.Key
                };
                Log.Information("Enqueue on-start work item for {source} with types {types} at priority {priority}", src.SourceConnection.ConnectionForPrint, workItem.AllTypes, kvp.Key);

                await workQueue.EnqueueAsync(workItem);
            }

            // Custom-only groups (no core types in the same priority)
            foreach (var kvp in customOnStartGroups)
            {
                if (typeGroups.ContainsKey(kvp.Key)) continue;

                var workItem = new WorkItem
                {
                    Source = src,
                    Types = Array.Empty<CollectionType>(),
                    CustomCollections = kvp.Value,
                    Schedule = "OnStartup",
                    Priority = kvp.Key
                };

                await workQueue.EnqueueAsync(workItem);
            }
        }

        private static string BuildScheduleGroupKey(string scheduleKey, CollectionType[] types, IEnumerable<string> customNames)
        {
            var typeParts = (types ?? Array.Empty<CollectionType>())
                .OrderBy(t => (int)t)
                .Select(t => t.ToString());

            var customParts = (customNames ?? Enumerable.Empty<string>())
                .OrderBy(n => n, StringComparer.Ordinal);

            var signature = string.Join("|", typeParts.Concat(customParts)); // stable, readable

            return $"{scheduleKey}|{signature}";
        }

        private static string ExtractScheduleFromGroupKey(string groupKey)
        {
            if (string.IsNullOrEmpty(groupKey)) return groupKey;
            var idx = groupKey.IndexOf('|');
            return idx <= 0 ? groupKey : groupKey.Substring(0, idx);
        }

        private static IJobDetail GetJob(CollectionType[] types, DBADashSource src, string cfgString, Dictionary<string, CustomCollection> customCollections, string schedule)
        {
            return JobBuilder.Create<DBADashJob>()
                     .UsingJobData("Type", JsonConvert.SerializeObject(types))
                     .UsingJobData("Source", src.ConnectionString)
                     .UsingJobData("CFG", cfgString)
                     .UsingJobData("Job_instance_id", 0)
                     .UsingJobData("SourceType", JsonConvert.SerializeObject(src.SourceConnection.Type))
                     .UsingJobData("CustomCollections", JsonConvert.SerializeObject(customCollections))
                     .UsingJobData("Schedule", schedule)
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

        private void EnsureScheduledJob(string schedule, IJobDetail job)
        {
            ArgumentNullException.ThrowIfNull(schedule);

            if (scheduler.CheckExists(job.Key).ConfigureAwait(false).GetAwaiter().GetResult())
            {
                return;
            }

            ITrigger trigger = int.TryParse(schedule, out var seconds)
                ? TriggerBuilder.Create().StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever()).Build()
                : TriggerBuilder.Create().StartNow().WithCronSchedule(schedule).Build();

            scheduler.ScheduleJob(job, trigger).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task RemoveEventSessionsAsync()
        {
            Log.Information("Remove event sessions started");
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 30
            };
            await Parallel.ForEachAsync(config.SourceConnections, options, async (src, ct) =>
            {
                await RemoveEventSessionAsync(src);
            });
            Log.Information("Remove event sessions completed");
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