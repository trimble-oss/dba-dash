using Microsoft.Data.SqlClient;
using Quartz;
using Serilog;
using System;
using System.Data;
using System.Threading.Tasks;
using DBADash;

namespace DBADashService
{
    public class MaintenanceJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string connectionString = dataMap.GetString("ConnectionString");
            try
            {
                AddPartitions(connectionString);
            }
            catch (Exception ex)
            {
                LogError(ex, connectionString, "AddPartitions", ex.Message);
            }
            try
            {
                PurgeData(connectionString);
            }
            catch (Exception ex)
            {
                LogError(ex, connectionString, "PurgeData", ex.Message);
            }

            return Task.CompletedTask;
        }

        public static void AddPartitions(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                using (var cmd = new SqlCommand("dbo.Partitions_Add", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 })
                {
                    cn.Open();
                    Log.Information("Maintenance: Creating partitions");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void PurgeData(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                using (var cmd = new SqlCommand("dbo.PurgeData", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 600 })
                {
                    cn.Open();
                    Log.Information("Maintenance : PurgeData");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void LogError(Exception ex, string connectionString, string errorSource, string errorMessage, string errorContext = "Maintenance")
        {
            Log.Error(ex, "{errorcontext} | {errorsource}", errorContext, errorSource);
            try
            {
                var dtErrors = new DataTable("Errors");
                dtErrors.Columns.Add("ErrorSource");
                dtErrors.Columns.Add("ErrorMessage");
                dtErrors.Columns.Add("ErrorContext");
                var rError = dtErrors.NewRow();
                rError["ErrorSource"] = errorSource;
                rError["ErrorMessage"] = errorMessage;
                rError["ErrorContext"] = errorContext;
                dtErrors.Rows.Add(rError);
                DataSet ds = new();
                ds.Tables.Add(dtErrors);
                DBADash.DBImporter.InsertErrors(connectionString, null, DateTime.UtcNow, ds);
            }
            catch (Exception ex2)
            {
                Log.Error(ex2, "Write errors to database from {errorcontext} | {errorsource}", errorContext, errorSource);
            }
        }
    }
}