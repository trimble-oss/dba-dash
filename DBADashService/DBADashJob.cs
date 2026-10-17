using DBADash;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    [DisallowConcurrentExecution, PersistJobDataAfterExecution]
    public class DBADashJob : IJob
    {
        private static readonly CollectionConfig config = SchedulerServiceConfig.Config;

        /// <summary>
        /// Collections in progress, by the key the work queue uses to stop a collection overlapping itself
        /// (<see cref="WorkItem.DedupKey"/>).  DisallowConcurrentExecution only covers a job key, and a schedule change
        /// replaces the jobs with new ones, so a new job could otherwise start while the one it replaced is still running.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> InProgress = new(StringComparer.Ordinal);

        public async ValueTask Execute(IJobExecutionContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            Log.Information("Processing Job : " + context.JobDetail.Key);
            var dataMap = context.JobDetail.JobDataMap;
            var cfg = JsonConvert.DeserializeObject<DBADashSource>(dataMap.GetString("CFG")!);
            var schedule = dataMap.GetString("Schedule");
            try
            {
                if (OfflineInstances.IsOffline(cfg))
                {
                    Log.Warning("Skipping {job} on {instance} as it is offline", context.JobDetail.Key, cfg.ConnectionID ?? cfg.SourceConnection.ConnectionForPrint);
                    return;
                }
                switch (cfg.SourceConnection.Type)
                {
                    case ConnectionType.Directory:
                        {
                            var wi = new DirectoryWorkItem() { Source = cfg, Schedule = schedule };
                            await wi.ExecuteAsync(config, cancellationToken);
                            break;
                        }
                    case ConnectionType.AWSS3:
                        {
                            var wi = new S3WorkItem() { Source = cfg, Schedule = schedule };
                            await wi.ExecuteAsync(config, cancellationToken);
                            break;
                        }
                    case ConnectionType.SQL:
                        var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type")!);
                        var customCollections = JsonConvert.DeserializeObject<Dictionary<string, CustomCollection>>(dataMap.GetString("CustomCollections")!);

                        var workItem = new WorkItem
                        {
                            Source = cfg,
                            Types = types,
                            CustomCollections = customCollections ?? new Dictionary<string, CustomCollection>(),
                            Schedule = schedule,
                            PreviousFireTime = context.PreviousFireTimeUtc?.UtcDateTime
                        };

                        var inProgressKey = workItem.DedupKey;
                        if (!InProgress.TryAdd(inProgressKey, 0))
                        {
                            Log.Warning("Skipping {job} for {instance}.  The previous run of these collections is still in progress.",
                                context.JobDetail.Key, cfg.ConnectionID ?? cfg.SourceConnection.ConnectionForPrint);
                            return;
                        }
                        try
                        {
                            await workItem.ExecuteAsync(config, cancellationToken);
                        }
                        finally
                        {
                            InProgress.TryRemove(inProgressKey, out _);
                        }
                        break;

                    case ConnectionType.Invalid:
                    default:
                        throw new Exception("Invalid Connection Type");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Log.Information("Job {job} cancelled during shutdown", context.JobDetail.Key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "JobExecute");
            }
        }
    }
}