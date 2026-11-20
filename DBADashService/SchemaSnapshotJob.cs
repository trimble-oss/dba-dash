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
            using var logConnection = LogScope.Connection(src.ConnectionID);
            if(OfflineInstances.IsOffline(src))
            {
                Log.Warning("Connection to {ConnectionID} is offline.  Skipping schema snapshot", src.ConnectionID ?? src.SourceConnection.ConnectionForPrint);
                return;
            }
            if (CollectionPauseStore.Current.IsPaused(src.ConnectionID, out var pausedUntil))
            {
                Log.Information("Collections for {ConnectionID} are paused until {PausedUntil:u}.  Skipping schema snapshot", src.ConnectionID, pausedUntil);
                return;
            }
            await SchemaSnapshotDB.GenerateSchemaSnapshots(SchedulerServiceConfig.Config, src);
        }
    }
}