using AsyncKeyedLock;
using DBADash;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    public enum WorkItemPriority
    {
        High = 0,
        Normal = 1,
        Low = 2
    }

    public class WorkItem : IWorkItem
    {
        private static ConcurrentDictionary<string, WorkItemState> WorkItemStates = new();
        public DBADashSource Source { get; set; }
        public virtual CollectionType[] Types { get; set; }
        public Dictionary<string, CustomCollection> CustomCollections { get; set; } = new();

        private WorkItemState _state;

        private WorkItemState State { get => _state ?? GetState(Source.ConnectionString); }
        public DateTime? PreviousFireTime { get; set; }
        public string Schedule { get; set; } // Track which schedule this work item belongs to

        private Stopwatch EnqueueSW = Stopwatch.StartNew();

        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

        // Stable unique identifier for deduplication across queue runs
        public string DedupKey => $"{Source.ConnectionID ?? Source.ConnectionString}::{string.Join("|", Types)}{string.Join("|", CustomCollections.Keys.OrderBy(k => k))}";

        public IEnumerable<string> AllTypes => Types.Select(c => c.ToString()).Union(CustomCollections?.Keys.ToArray<string>() ?? []);

        public string Description => $"Collect {string.Join(',', AllTypes)} on schedule {Schedule} from {Source.ConnectionID}";

        private WorkItemState GetState(string connectionString)
        {
            return WorkItemStates.GetOrAdd(connectionString, _ => new WorkItemState());
        }

        public virtual async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            if (Types == null || Types.Length == 0)
                return;

            var dequeueLatencyMs = EnqueueSW.ElapsedMilliseconds;
            var collectJobs = Types.Contains(CollectionType.Jobs);
            var types = collectJobs ? Types.Where(t => t != CollectionType.Jobs).ToArray() : Types;

            try
            {
                if (types.Length > 0 || CustomCollections?.Count > 0)
                {
                    var collector = await DBCollector.CreateAsync(Source, config.ServiceName);
                    collector.Job_instance_id = State.JobInstanceId;
                    collector.IsExtendedEventsNotSupportedException = State.IsExtendedEventsNotSupportedException;
                    collector.FailedLoginsBackfillMinutes = config.FailedLoginsBackfillMinutes ??
                        CollectionConfig.DefaultFailedLoginsBackfillMinutes;

                    if (SchedulerServiceConfig.Config.IdentityCollectionThreshold.HasValue)
                    {
                        collector.IdentityCollectionThreshold = (int)SchedulerServiceConfig.Config.IdentityCollectionThreshold;
                    }

                    if (PreviousFireTime.HasValue)
                    {
                        collector.PerformanceCollectionPeriodMins =
                            (int)DateTime.UtcNow.Subtract(PreviousFireTime.Value).TotalMinutes + 5;
                    }
                    else
                    {
                        collector.PerformanceCollectionPeriodMins = 30;
                    }

                    collector.LogInternalPerformanceCounters = config.LogInternalPerformanceCounters;

                    using (var op = SerilogTimings.Operation.Begin("Collect {types} from instance {instance} on schedule {schedule} with priority {priority}. Dequeue latency {latency}ms.",
                               string.Join(", ", types.Select(s => s.ToString())),
                               Source.SourceConnection.ConnectionForPrint,
                               Schedule,
                               Priority,
                               dequeueLatencyMs))
                    {
                        await collector.CollectAsync(types);

                        if (!State.IsExtendedEventsNotSupportedException && collector.IsExtendedEventsNotSupportedException)
                        {
                            Log.Information(
                                "Disabling Extended events collection for {0}. Instance type doesn't support extended events",
                                Source.SourceConnection.ConnectionForPrint);
                            State.IsExtendedEventsNotSupportedException = true;
                        }

                        State.JobInstanceId = collector.Job_instance_id;
                        op.Complete();
                    }

                    if (CustomCollections?.Count > 0)
                    {
                        using var op = SerilogTimings.Operation.Begin("Collect Custom Collections {types} from instance {instance}",
                            string.Join(", ", CustomCollections.Select(s => s.Key)),
                            Source.SourceConnection.ConnectionForPrint);
                        await collector.CollectAsync(CustomCollections);
                        op.Complete();
                    }

                    var fileName = DBADashSource.GenerateFileName(Source.SourceConnection.ConnectionForFileName);
                    try
                    {
                        await DestinationHandling.WriteAllDestinationsAsync(collector.Data, Source, fileName, config);
                        collector.CacheCollectedText();
                        collector.CacheCollectedPlans();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error writing {filename} to destination. File will be copied to {folder}",
                            fileName, SchedulerServiceConfig.FailedMessageFolder);
                        await DestinationHandling.WriteFolderAsync(collector.Data,
                            SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                    }
                }

                if (collectJobs)
                {
                    await CollectJobsAsync(Source, State, config, cancellationToken);
                }
            }
            catch (DatabaseConnectionException ex)
            {
                OfflineInstances.Add(Source, ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error collecting types {types} from instance {instance}",
                    string.Join(", ", Types.Select(s => s.ToString())),
                    Source.SourceConnection.ConnectionForPrint);
            }
        }

        private static async Task CollectJobsAsync(DBADashSource cfg, WorkItemState state,
            CollectionConfig config, CancellationToken cancellationToken)
        {
            const int MAX_TIME_SINCE_LAST_JOB_COLLECTION = 1430;

            var minsSinceLastCollection = DateTime.Now.Subtract(state.JobCollectDate).TotalMinutes;
            var forcedCollectionDate = state.JobCollectDate.AddMinutes(MAX_TIME_SINCE_LAST_JOB_COLLECTION);

            var collector = await DBCollector.CreateAsync(cfg, config.ServiceName, config.LogInternalPerformanceCounters);

            if (state.JobCollectDate == DateTime.MinValue)
            {
                Log.Debug("Skipping setting JobLastModified (First collection on startup) on {Connection}",
                    cfg.SourceConnection.ConnectionForPrint);
            }
            else if (DateTime.Now < forcedCollectionDate)
            {
                collector.JobLastModified = state.JobLastModified;
                Log.Debug("Setting JobLastModified to {JobLastModified}. Forced collection will run after {ForcedCollectionDate}. {MinsSinceLastCollection}mins since last collection ({LastCollected}) on {Connection}",
                    state.JobLastModified, forcedCollectionDate, minsSinceLastCollection.ToString("N0"),
                    state.JobCollectDate, cfg.SourceConnection.ConnectionForPrint);
            }
            else
            {
                Log.Debug("Skipping setting JobLastModified to {JobLastModified} - forcing job collection to run. {MinsSinceLastCollection}mins since last collection ({LastCollected}) on {Connection}.",
                    state.JobLastModified, minsSinceLastCollection.ToString("N0"),
                    state.JobCollectDate, cfg.SourceConnection.ConnectionForPrint);
            }

            await collector.CollectAsync(CollectionType.Jobs);
            bool containsJobs = collector.Data.Tables.Contains("Jobs");

            if (containsJobs)
            {
                state.JobLastModified = collector.JobLastModified;
                state.JobCollectDate = DateTime.Now;

                var fileName = DBADashSource.GenerateFileName(cfg.SourceConnection.ConnectionForFileName);
                try
                {
                    await DestinationHandling.WriteAllDestinationsAsync(collector.Data, cfg, fileName, config);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing {filename} to destination. File will be copied to {folder}",
                        fileName, SchedulerServiceConfig.FailedMessageFolder);
                    await DestinationHandling.WriteFolderAsync(collector.Data,
                        SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                }
            }
        }
    }
}