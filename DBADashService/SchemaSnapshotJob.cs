using DBADash;
using Newtonsoft.Json;
using Quartz;
using System.Threading.Tasks;
using Serilog;

namespace DBADashService
{
    public class SchemaSnapshotJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var sourceString = context.JobDetail.JobDataMap.GetString("CFG");
            if (sourceString == null) return;

            var src = JsonConvert.DeserializeObject<DBADashSource>(sourceString);
            if(OfflineInstances.IsOffline(src))
            {
                Log.Warning("Connection to {Connection} is offline.  Skipping schema snapshot", src.ConnectionID ?? src.SourceConnection.ConnectionForPrint);
                return;
            }
            await SchemaSnapshotDB.GenerateSchemaSnapshots(SchedulerServiceConfig.Config, src);
        }
    }
}