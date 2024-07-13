using Microsoft.Data.SqlClient;
using Quartz;
using Serilog;
using System;
using System.Data;
using System.Threading.Tasks;
using DBADash;
using SerilogTimings;

namespace DBADashService
{
    public class MaintenanceJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var connectionString = dataMap.GetString("ConnectionString");
            var addPartitionsCommandTimeout = dataMap.GetInt("AddPartitionsCommandTimeout");
            var purgeDataCommandTimeout = dataMap.GetInt("PurgeDataCommandTimeout");
            try
            {
                AddPartitions(connectionString, addPartitionsCommandTimeout);
            }
            catch (Exception ex)
            {
                LogError(ex, connectionString, "AddPartitions", ex.Message);
            }
            try
            {
                PurgeData(connectionString, purgeDataCommandTimeout);
            }
            catch (Exception ex)
            {
                LogError(ex, connectionString, "PurgeData", ex.Message);
            }

            return Task.CompletedTask;
        }

        public static void AddPartitions(string connectionString, int commandTimeout)
        {
            using (var op = Operation.Begin("AddPartitions"))
            {
                using var cn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("dbo.Partitions_Add", cn)
                { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
                cn.Open();
                Log.Information("Maintenance: Creating partitions");
                cmd.ExecuteNonQuery();
                op.Complete();
            }
        }

        public static void PurgeData(string connectionString, int commandTimeout)
        {
            using (var op = Operation.Begin("PurgeData"))
            {
                using var cn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("dbo.PurgeData", cn)
                { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
                cn.Open();
                Log.Information("Maintenance : PurgeData");
                cmd.ExecuteNonQuery();
                op.Complete();
            }
        }

        private static void LogError(Exception ex, string connectionString, string errorSource, string errorMessage, string errorContext = "Maintenance")
        {
            Log.Error(ex, "{errorContext} | {errorSource}", errorContext, errorSource);
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
                DBImporter.InsertErrors(connectionString, null, DateTime.UtcNow, ds, 60);
            }
            catch (Exception ex2)
            {
                Log.Error(ex2, "Write errors to database from {errorContext} | {errorSource}", errorContext, errorSource);
            }
        }
    }
}