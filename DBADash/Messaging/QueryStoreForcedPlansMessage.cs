using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using SerilogTimings;
using static DBADash.Messaging.PerDatabaseCollectionHelper;
using Microsoft.Data.SqlClient;
using System.Threading;

namespace DBADash.Messaging
{
    public class QueryStoreForcedPlansMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            using var op = Operation.Begin("Query store forced plans {database} on {instance} triggered from message {handle}",
    DatabaseName,
    ConnectionID,
    handle);
            try
            {
                var src = cfg.GetSourceConnection(ConnectionID);
                List<string> databases;
                if (string.IsNullOrEmpty(DatabaseName))
                {
                    databases = await GetDatabasesWithQueryStoreAsync(src.SourceConnection.ConnectionString);
                }
                else
                {
                    databases = new List<string> { DatabaseName };
                }

                if (databases.Count == 0)
                {
                    throw new Exception("No databases found with Query Store enabled");
                }
                Log.Debug("Collecting Query Store Forced Plans for databases: {databases} for message {handle}", databases, handle);

                DatabaseOperationDelegate operation = GetForcedPlans;
                var resultTable = await RunPerDatabaseCollectionWithUnionResults(operation, src.SourceConnection.ConnectionString, databases);

                var ds = new DataSet();
                ds.Tables.Add(resultTable);
                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing query store message");
                throw;
            }
        }

        private async Task<DataTable> GetForcedPlans(string connectionString, string db)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(SqlStrings.QueryStoreForcedPlans, cn) { CommandTimeout = Lifetime };
            cmd.Parameters.AddWithValue("@Database", db);
            var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }
}