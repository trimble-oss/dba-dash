using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Serilog;

namespace DBADash.Messaging
{
    public class QueryStorePlanForcingMessage:MessageBase
    {
        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }

        public long QueryID { get; set; }

        public long PlanID { get; set; }

        public enum PlanForcingOperations
        {
            Force,
            Unforce
        }

        public PlanForcingOperations PlanForcingOperation { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle)
        {
            ThrowIfExpired();
            if (!cfg.AllowPlanForcing)
            {
                throw new Exception("Plan forcing is not enabled on the DBA Dash service");
            }
            using var op = Operation.Begin(
                "{Operation} plan {PlanID} for query {QueryID} in database {database} on {instance} triggered from message {handle}",
                PlanForcingOperation,
                PlanID,
                QueryID,
                DatabaseName,
                ConnectionID,
                handle);
            try
            {
                var src = cfg.GetSourceConnection(ConnectionID);
                var builder = new SqlConnectionStringBuilder(src.SourceConnection.ConnectionString)
                {
                    InitialCatalog = DatabaseName
                };
                switch (PlanForcingOperation)
                {
                    case PlanForcingOperations.Force:
                        await ForcePlan(builder.ConnectionString, QueryID, PlanID);
                        break;
                    case PlanForcingOperations.Unforce:
                        await UnforcePlan(builder.ConnectionString, QueryID, PlanID);
                        break;
                    default:
                        throw new Exception($"Plan forcing operation {PlanForcingOperation} is not supported");
                }
                op.Complete();
                return new DataSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex,"Error with {type} plan operation",PlanForcingOperation.ToString());
                throw;
            }
        }

        public static async Task ForcePlan(string connectionString, long queryID, long planID)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(@"sys.sp_query_store_force_plan", cn) {CommandType = CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@query_id", queryID);
            cmd.Parameters.AddWithValue("@plan_id", planID);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task UnforcePlan(string connectionString, long queryID, long planID)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(@"sys.sp_query_store_unforce_plan", cn) {CommandType = CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@query_id", queryID);
            cmd.Parameters.AddWithValue("@plan_id", planID);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
