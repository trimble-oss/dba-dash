using Microsoft.Data.SqlClient;
using Quartz;
using Serilog;
using System;
using System.Data;
using System.Threading.Tasks;
using SerilogTimings;

namespace DBADashService
{
    internal class SummaryRefreshJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var connectionString = dataMap.GetString("ConnectionString");
            try
            {
                using Operation op = Operation.Begin("SummaryRefresh");
                RefreshSummary(connectionString);
                op.Complete();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SummaryRefresh error");
            }

            return Task.CompletedTask;
        }

        public static void RefreshSummary(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Summary_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}