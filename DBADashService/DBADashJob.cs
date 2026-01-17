using DBADash;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System;
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

        public async Task Execute(IJobExecutionContext context)
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
                            await wi.ExecuteAsync(config, CancellationToken.None);
                            break;
                        }
                    case ConnectionType.AWSS3:
                        {
                            var wi = new S3WorkItem() { Source = cfg, Schedule = schedule };
                            await wi.ExecuteAsync(config, CancellationToken.None);
                            break;
                        }
                    case ConnectionType.SQL:
                        var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type")!);
                        var customCollections = JsonConvert.DeserializeObject<Dictionary<string, CustomCollection>>(dataMap.GetString("CustomCollections")!);

                        var workItem = new WorkItem
                        {
                            Source = cfg,
                            Types = types,
                            CustomCollections = customCollections,
                            Schedule = schedule,
                            PreviousFireTime = context.PreviousFireTimeUtc?.UtcDateTime
                        };

                        await workItem.ExecuteAsync(config, CancellationToken.None);
                        break;

                    case ConnectionType.Invalid:
                    default:
                        throw new Exception("Invalid Connection Type");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "JobExecute");
            }
        }
    }
}