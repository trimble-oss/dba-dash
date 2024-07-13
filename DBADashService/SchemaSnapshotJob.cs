using DBADash;
using Newtonsoft.Json;
using Quartz;
using System.Threading.Tasks;

namespace DBADashService
{
    public class SchemaSnapshotJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var sourceString = context.JobDetail.JobDataMap.GetString("CFG");
            if (sourceString == null) return;

            var src = JsonConvert.DeserializeObject<DBADashSource>(sourceString);

            await SchemaSnapshotDB.GenerateSchemaSnapshots(SchedulerServiceConfig.Config, src);
        }
    }
}