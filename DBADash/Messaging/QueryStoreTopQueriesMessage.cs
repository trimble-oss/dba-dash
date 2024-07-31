using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System.Collections.Concurrent;
using System.Threading;

namespace DBADash.Messaging
{
    public class QueryStoreTopQueriesMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public int Top { get; set; } = 25;

        public int? ObjectID { get; set; }

        public string ObjectName { get; set; }

        public string SortColumn { get; set; } = "total_cpu_time_ms";

        public QueryStoreGroupByEnum GroupBy { get; set; } = QueryStoreGroupByEnum.query_id;

        public byte[] QueryHash { get; set; }

        public byte[] QueryPlanHash { get; set; }

        public long? QueryID { get; set; }

        public long? PlanID { get; set; }

        public bool ParallelPlans { get; set; }

        public bool IncludeWaits { get; set; }

        public int MinimumPlanCount { get; set; } = 1;

        public enum QueryStoreGroupByEnum
        {
            query_id,
            plan_id,
            query_hash,
            query_plan_hash,
            object_id,
            date_bucket
        }

        public bool NearestInterval { get; set; } = true;


        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle)
        {
            ThrowIfExpired();
            if (GroupBy == QueryStoreGroupByEnum.date_bucket && (string.IsNullOrEmpty(DatabaseName) || QueryID == null))
            {
                throw new Exception("Database and QueryID must be specified when grouping by date bucket");
            }
            using var op = Operation.Begin("Query store top queries for {database} on {instance} triggered from message {handle}",
                DatabaseName,
                ConnectionID,
                handle);
            try
            {
                var src = cfg.GetSourceConnection(ConnectionID);
                List<string> databases;
                if (string.IsNullOrEmpty(DatabaseName))
                {
                    databases = await PerDatabaseCollectionHelper.GetDatabasesWithQueryStoreAsync(src.SourceConnection.ConnectionString);
                }
                else
                {
                    databases = new List<string> { DatabaseName };
                }
                Log.Debug("Collecting Query Store for databases: {databases} for message {handle}", databases, handle);
                
                if (databases.Count == 0)
                {
                    throw new Exception("No databases found with Query Store enabled");
                }
                PerDatabaseCollectionHelper.DatabaseOperationDelegate operation = GetTopQueriesForDatabase;
                var resultTable = await PerDatabaseCollectionHelper.RunPerDatabaseCollectionWithUnionResults(operation,src.SourceConnection.ConnectionString, databases);

                if (resultTable.Rows.Count > Top)
                {
                    var topXRows = resultTable.AsEnumerable()
                        .OrderByDescending(row => row[SortColumn]) // Or OrderByDescending for descending order.
                        .Take(Top);

                    resultTable = topXRows.CopyToDataTable();
                }

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

        private async Task<DataTable> GetTopQueriesForDatabase(string connectionString, string db)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(SqlStrings.QueryStoreTopQueries, cn) { CommandTimeout = Lifetime };
            cmd.Parameters.AddWithValue("@Database", db);
            cmd.Parameters.AddWithValue("@FromDate", From);
            cmd.Parameters.AddWithValue("@ToDate ", To);
            cmd.Parameters.AddWithValue("@Top", Top);
            cmd.Parameters.AddWithValue("@SortCol", SortColumn);
            cmd.Parameters.AddWithValue("@NearestInterval", NearestInterval);
            cmd.Parameters.AddWithValue("@ObjectID", ObjectID is > 0 ? ObjectID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ObjectName", string.IsNullOrEmpty(ObjectName) ? DBNull.Value : ObjectName);
            cmd.Parameters.AddWithValue("@GroupBy", GroupBy.ToString());
            var pQueryHash = cmd.Parameters.Add("@QueryHash", SqlDbType.Binary, 8);
            pQueryHash.Value = QueryHash is not null ? QueryHash : DBNull.Value;
            var pPlanHash = cmd.Parameters.Add("@QueryPlanHash", SqlDbType.Binary, 8);
            pPlanHash.Value = QueryPlanHash is not null ? QueryPlanHash : DBNull.Value;
            cmd.Parameters.AddWithValue("@QueryID", QueryID is > 0 ? QueryID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PlanID", PlanID is > 0 ? PlanID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("ParallelPlans", ParallelPlans);
            cmd.Parameters.AddWithValue("IncludeWaits", IncludeWaits);
            cmd.Parameters.AddWithValue("MinimumPlanCount", MinimumPlanCount);
            var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }
}