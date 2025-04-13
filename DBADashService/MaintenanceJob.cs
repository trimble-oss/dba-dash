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
        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var connectionString = dataMap.GetString("ConnectionString");
            var addPartitionsCommandTimeout = dataMap.GetInt("AddPartitionsCommandTimeout");
            var purgeDataCommandTimeout = dataMap.GetInt("PurgeDataCommandTimeout");
            try
            {
                await AddPartitionsAsync(connectionString, addPartitionsCommandTimeout);
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, connectionString, "AddPartitions", ex.Message);
            }
            try
            {
                await PurgeDataAsync(connectionString, purgeDataCommandTimeout);
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, connectionString, "PurgeData", ex.Message);
            }
        }

        public static async Task AddPartitionsAsync(string connectionString, int commandTimeout)
        {
            using var op = Operation.Begin("AddPartitions");
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.Partitions_Add", cn)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            await cn.OpenAsync();
            Log.Information("Maintenance: Creating partitions");
            await cmd.ExecuteNonQueryAsync();
            op.Complete();
        }

        public static async Task PurgeDataAsync(string connectionString, int commandTimeout)
        {
            using var op = Operation.Begin("PurgeData");
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.PurgeData", cn)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            await cn.OpenAsync();
            Log.Information("Maintenance : PurgeData");
            await cmd.ExecuteNonQueryAsync();
            op.Complete();
        }

        private static async Task LogErrorAsync(Exception ex, string connectionString, string errorSource, string errorMessage, string errorContext = "Maintenance")
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
                await DBImporter.InsertErrorsAsync(connectionString, null, DateTime.UtcNow, ds, 60);
            }
            catch (Exception ex2)
            {
                Log.Error(ex2, "Write errors to database from {errorContext} | {errorSource}", errorContext, errorSource);
            }
        }
    }
}