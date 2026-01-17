using DBADash;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Scheduled job that enqueues collection work for all instances with a specific schedule.
    /// One job per cron schedule instead of one job per instance per schedule.
    /// </summary>
    [DisallowConcurrentExecution]
    public class ScheduledCollectionJob : IJob
    {
        private static CollectionWorkQueue _workQueue;

        private const int HIGH_THRESHOLD_SECONDS = 300;    // Up to 5 minutes.  If the schedule is this frequent, treat as high priority. Less frequent is normal or low priority
        private const int NORMAL_THRESHOLD_SECONDS = 7200; // Up to 2 hours. If the schedule is this frequent, treat as normal priority. Less frequent is low priority

        private static readonly ConcurrentDictionary<string, WorkItemPriority> _priorityCache = new(StringComparer.Ordinal);

        // jobKey -> instanceKey -> source
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DBADashSource>> _sourcesByJob
            = new(StringComparer.Ordinal);

        public static void Initialize(CollectionWorkQueue workQueue)
        {
            _workQueue = workQueue;
        }

        // Upsert sources for a job identity
        public static void UpsertSources(string jobKey, IEnumerable<DBADashSource> sources)
        {
            if (string.IsNullOrEmpty(jobKey) || sources == null) return;
            var bucket = _sourcesByJob.GetOrAdd(jobKey, _ => new ConcurrentDictionary<string, DBADashSource>(StringComparer.Ordinal));
            foreach (var s in sources)
            {
                if (s == null) continue;
                var key = s.ConnectionString;
                if (!string.IsNullOrEmpty(key))
                {
                    bucket[key] = s;
                }
            }
        }

        private static IReadOnlyCollection<DBADashSource> GetSourcesForJob(string jobKey)
        {
            if (!string.IsNullOrEmpty(jobKey) && _sourcesByJob.TryGetValue(jobKey, out var bucket) && bucket.Count > 0)
            {
                return bucket.Values.ToArray();
            }
            return Array.Empty<DBADashSource>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var swTotal = Stopwatch.StartNew();
            var dataMap = context.JobDetail.JobDataMap;
            var schedule = dataMap.GetString("Schedule");

            var jobKey = context.JobDetail.Key.Name;
            var sources = GetSourcesForJob(jobKey);

            var priority = GetOrComputePriority(schedule, dataMap);

            var collectionTypes = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Types"));
            var customCollections = JsonConvert.DeserializeObject<Dictionary<string, CustomCollection>>(dataMap.GetString("CustomCollections"));

            var queueDepth = _workQueue.QueueDepth;
            Log.Information("Enqueuing collection for {instanceCount} instances on schedule {schedule}. Queue Total:{total} (H:{high}, N:{normal} L:{low}). Priority: {priority}",
                sources.Count, schedule, _workQueue.QueueDepth, _workQueue.HighQueueDepth, _workQueue.NormalQueueDepth, _workQueue.LowQueueDepth, priority);

            var enqueueTasks = new List<Task<bool>>(sources.Count * 2);
            int enqueuedWork = 0;

            foreach (var source in sources)
            {
                switch (source.SourceConnection.Type)
                {
                    case DBADashConnection.ConnectionType.AWSS3:
                        EnqueueS3(enqueueTasks, source, schedule, context, priority);
                        enqueuedWork++;
                        break;

                    case DBADashConnection.ConnectionType.Directory:
                        EnqueueDirectory(enqueueTasks, source, schedule, context, priority);
                        enqueuedWork++;
                        break;

                    case DBADashConnection.ConnectionType.SQL:
                        enqueuedWork += EnqueueSQL(enqueueTasks, source, schedule, context, priority, collectionTypes, customCollections);
                        break;
                }
            }

            var results = await Task.WhenAll(enqueueTasks);
            var failedCount = results.Count(r => !r);
            var totalMs = swTotal.Elapsed.TotalMilliseconds;

            if (failedCount > 0)
            {
                Log.Warning("Failed to enqueue {failedCount} work items for schedule {schedule} in {duration}ms", failedCount, schedule, totalMs);
            }
            else
            {
                Log.Information("Successfully enqueued {count} work items for schedule {schedule} in {duration}ms", enqueueTasks.Count, schedule, totalMs);
            }
        }

        private static int EnqueueSQL(
            List<Task<bool>> enqueueTasks,
            DBADashSource source,
            string schedule,
            IJobExecutionContext context,
            WorkItemPriority priority,
            CollectionType[] collectionTypes,
            Dictionary<string, CustomCollection> customCollections)
        {
            var enqueueCount = 0;

            var isOffline = OfflineInstances.IsOffline(source);
            if (isOffline)
            {
                Log.Debug("Skipping {instance} - offline",
                    source.ConnectionID ?? source.SourceConnection.ConnectionForPrint);
                return 0;
            }

            var typesSet = new HashSet<CollectionType>(collectionTypes);
            var hasSnapshot = typesSet.Contains(CollectionType.SchemaSnapshot);
            var typesWithoutSnapshot = hasSnapshot
                ? collectionTypes.Where(c => c != CollectionType.SchemaSnapshot).ToArray()
                : collectionTypes;

            if (hasSnapshot)
            {
                var snapshotWorkItem = new SchemaSnapshotWorkItem
                {
                    Source = source,
                    Schedule = schedule,
                };

                enqueueTasks.Add(_workQueue.EnqueueAsync(snapshotWorkItem, context.CancellationToken));
                enqueueCount++;
                if (typesWithoutSnapshot.Length == 0 && customCollections.Count == 0)
                {
                    return enqueueCount;
                }
            }

            var workItem = new WorkItem
            {
                Source = source,
                Types = typesWithoutSnapshot,
                CustomCollections = customCollections,
                PreviousFireTime = context.PreviousFireTimeUtc?.UtcDateTime,
                Schedule = schedule,
                Priority = priority
            };
            enqueueCount++;
            enqueueTasks.Add(_workQueue.EnqueueAsync(workItem, context.CancellationToken));
            return enqueueCount;
        }

        private static void EnqueueS3(List<Task<bool>> enqueueTasks, DBADashSource source, string schedule, IJobExecutionContext context, WorkItemPriority priority)
        {
            var s3WorkItem = new S3WorkItem
            {
                Source = source,
                Schedule = schedule,
                Priority = priority
            };
            enqueueTasks.Add(_workQueue.EnqueueAsync(s3WorkItem, context.CancellationToken));
        }

        private static void EnqueueDirectory(List<Task<bool>> enqueueTasks, DBADashSource source, string schedule, IJobExecutionContext context, WorkItemPriority priority)
        {
            var dirWorkItem = new DirectoryWorkItem
            {
                Source = source,
                Schedule = schedule,
                Priority = priority
            };
            enqueueTasks.Add(_workQueue.EnqueueAsync(dirWorkItem, context.CancellationToken));
        }

        private static WorkItemPriority GetOrComputePriority(string schedule, JobDataMap map)
        {
            // Prefer stored value in JobDataMap
            if (map.ContainsKey("Priority"))
            {
                return (WorkItemPriority)map.GetInt("Priority");
            }

            // Try cache
            if (_priorityCache.TryGetValue(schedule, out var cached))
            {
                map.Put("Priority", (int)cached);
                return cached;
            }

            // Compute once
            var computed = ComputePriorityFromSchedule(schedule);

            // Persist for subsequent runs
            _priorityCache[schedule] = computed;
            map.Put("Priority", (int)computed);

            return computed;
        }

        internal static WorkItemPriority ComputePriorityFromSchedule(string schedule)
        {
            if (string.IsNullOrEmpty(schedule)) return WorkItemPriority.Normal;
            // Numeric seconds schedule
            if (int.TryParse(schedule, out var seconds))
            {
                if (seconds <= HIGH_THRESHOLD_SECONDS) return WorkItemPriority.High;
                if (seconds <= NORMAL_THRESHOLD_SECONDS) return WorkItemPriority.Normal;
                return WorkItemPriority.Low;
            }

            // Cron expression: compute interval between next two occurrences
            try
            {
                var cron = new CronExpression(schedule);
                var now = DateTimeOffset.UtcNow;

                var next1 = cron.GetNextValidTimeAfter(now);
                var next2 = next1.HasValue ? cron.GetNextValidTimeAfter(next1.Value) : null;

                if (next1.HasValue && next2.HasValue)
                {
                    var intervalSeconds = (int)Math.Round((next2.Value - next1.Value).TotalSeconds);

                    if (intervalSeconds <= HIGH_THRESHOLD_SECONDS) return WorkItemPriority.High;
                    if (intervalSeconds <= NORMAL_THRESHOLD_SECONDS) return WorkItemPriority.Normal;
                    return WorkItemPriority.Low;
                }

                Log.Warning("Unable to determine interval from cron schedule {schedule}. Defaulting priority to Normal.", schedule);
                return WorkItemPriority.Normal;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid cron schedule {schedule}. Defaulting priority to Normal.", schedule);
                return WorkItemPriority.Normal;
            }
        }
    }
}