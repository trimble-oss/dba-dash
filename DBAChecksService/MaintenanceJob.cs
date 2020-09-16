using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecksService
{
    public class MaintenanceJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string connectionString = dataMap.GetString("ConnectionString");
            AddPartitions(connectionString);
            PurgeData(connectionString);
            return Task.CompletedTask;
        }

        public static void AddPartitions(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Partitions_Add", cn);
                Console.WriteLine("Maintenance: Creating partitions");
                cmd.ExecuteNonQuery();
            }
        }
        public static void PurgeData(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                Console.WriteLine("Maintenance: PurgeData");
                SqlCommand cmd = new SqlCommand("PurgeData", cn);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
