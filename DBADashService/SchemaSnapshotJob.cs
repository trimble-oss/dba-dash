using DBADash;
using Newtonsoft.Json;
using Quartz;
using System.Threading.Tasks;
using Serilog;

namespace DBADashService
{
    public class SchemaSnapshotJob : IJob
    {
        public async ValueTask Execute(IJobExecutionContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            var sourceString = context.JobDetail.JobDataMap.GetString("CFG");
            if (sourceString == null) return;

            var src = JsonConvert.DeserializeObject<DBADashSource>(sourceString);
            if(OfflineInstances.IsOffline(src))
            {
                Log.Warning("Connection to {Connection} is offline.  Skipping schema snapshot", src.ConnectionID ?? src.SourceConnection.ConnectionForPrint);
                return;
            }
            if (CollectionPauseStore.Current.IsPaused(src.ConnectionID, out var pausedUntil))
            {
                Log.Information("Collections for {Connection} are paused until {pausedUntil:u}.  Skipping schema snapshot", src.ConnectionID, pausedUntil);
                return;
            }
            await SchemaSnapshotDB.GenerateSchemaSnapshots(SchedulerServiceConfig.Config, src);
        }
    }
}