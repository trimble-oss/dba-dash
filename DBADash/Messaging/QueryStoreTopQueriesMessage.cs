using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using static Microsoft.SqlServer.Management.SqlParser.Metadata.MetadataInfoProvider;
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
            query_plan_hash
        }

        public bool NearestInterval { get; set; } = true;
        
        private async Task<int> GetIdleSchedulerCount(string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(@"SELECT COUNT(*) FROM sys.dm_os_schedulers WHERE status = 'VISIBLE ONLINE' AND is_idle=1", cn);
            await cn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Calculate MAX DOP based on CPU availability
        private async Task<int> GetThreadCountBasedOnSchedulerAvailability(string connectionString)
        {
            var idleSchedulerCount = 1;
            try
            {
                idleSchedulerCount = await GetIdleSchedulerCount(connectionString);
                Log.Debug("Idle scheduler count {idleSchedulerCount}",idleSchedulerCount);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error getting idle scheduler count");
            }

            return idleSchedulerCount switch
            {
                < 8 => 1,
                < 16 => 2,
                < 32 => 4,
                _ => 8
            };
        }

        public static async Task<List<string>> GetDatabasesAsync(string connectionString)
        {
            var databases = new List<string>();
            var query = @"
        SELECT D.name
        FROM sys.databases D
        WHERE D.is_query_store_on=1
        AND D.database_id>4
        AND D.state=0
        AND HAS_DBACCESS(D.name)=1
        AND D.is_in_standby = 0
        AND DATABASEPROPERTYEX(D.name, 'Updateability') = 'READ_WRITE';
    ";
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(query, cn);

            await cn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }

            return databases;
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle)
        {
            if (IsExpired)
            {
                throw new Exception("Message expired");
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
                    databases = await GetDatabasesAsync(src.SourceConnection.ConnectionString);
                }
                else
                {
                    databases = new List<string> { DatabaseName };
                }
                Log.Debug("Collecting Query Store for databases: {databases} for message {handle}", databases,handle);
                var threadCount = 1;
                switch (databases.Count)
                {
                    case 0:
                        throw new Exception("No databases found with Query Store enabled");
                    case 1:
                        break;
                    default:
                        threadCount = await GetThreadCountBasedOnSchedulerAvailability(src.SourceConnection.ConnectionString);
                        Log.Information("Processing message {handle} with thread count {maxDegreeOfParallelism}",handle, threadCount);
                        break;
                }
                var resultTable = new DataTable();
        
           
                var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount};

                // Use a concurrent bag to collect DataTables from parallel tasks.
                var dataTables = new ConcurrentBag<DataTable>();
                var exceptions = new ConcurrentBag<Exception>();

                var semaphore = new SemaphoreSlim(threadCount);

                var tasks = new List<Task>();

                foreach (var database in databases)
                {
                    // Wait to enter the semaphore. If the maximum number of requests are already running,
                    // this will block until one of them completes and releases the semaphore.
                    await semaphore.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var dt = await GetTopQueriesForDatabase(src.SourceConnection.ConnectionString, database);
                            Log.Debug("Query store top queries for {database} returned {rows} rows", database, dt.Rows.Count);
                            dataTables.Add(dt); // Ensure dataTables is thread-safe, e.g., ConcurrentBag.
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error getting query store top queries for {database} from {handle}", database, handle);
                            exceptions.Add(ex); // Ensure exceptions is thread-safe, e.g., ConcurrentBag.
                        }
                        finally
                        {
                            // Release the semaphore so another task can enter.
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                if (!exceptions.IsEmpty)
                {
                    throw new AggregateException(exceptions);
                }

                // Merge all DataTables into a single DataTable.
                foreach (var dt in dataTables)
                {
                    if (resultTable.Columns.Count == 0)
                    {
                        resultTable = dt.Clone(); // Clone the structure of the first DataTable.
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        resultTable.ImportRow(row);
                    }
                }

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
            await using var cmd = new SqlCommand(SqlStrings.QueryStoreTopQueries, cn) {CommandTimeout = Lifetime};
            cmd.Parameters.AddWithValue("@Database", db);
            cmd.Parameters.AddWithValue("@FromDate", From);
            cmd.Parameters.AddWithValue("@ToDate ", To);
            cmd.Parameters.AddWithValue("@Top", Top);
            cmd.Parameters.AddWithValue("@SortCol", SortColumn);
            cmd.Parameters.AddWithValue("@NearestInterval", NearestInterval);
            cmd.Parameters.AddWithValue("@ObjectID", ObjectID is > 0 ? ObjectID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ObjectName", string.IsNullOrEmpty(ObjectName) ? DBNull.Value : ObjectName);
            cmd.Parameters.AddWithValue("@GroupBy", GroupBy.ToString());
            var pQueryHash  = cmd.Parameters.Add("@QueryHash", SqlDbType.Binary, 8);
            pQueryHash.Value = QueryHash is not null ? QueryHash : DBNull.Value;
            var pPlanHash = cmd.Parameters.Add("@QueryPlanHash", SqlDbType.Binary,8);
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