using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    internal class PerDatabaseCollectionHelper
    {

        public delegate Task<DataTable> DatabaseOperationDelegate(string connectionString, string db);


        private static async Task<int> GetIdleSchedulerCount(string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(@"SELECT COUNT(*) FROM sys.dm_os_schedulers WHERE status = 'VISIBLE ONLINE' AND is_idle=1", cn);
            await cn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Calculate MAX DOP based on CPU availability
        private static async Task<int> GetThreadCountBasedOnSchedulerAvailability(string connectionString)
        {
            var idleSchedulerCount = 1;
            try
            {
                idleSchedulerCount = await GetIdleSchedulerCount(connectionString);
                Log.Debug("Idle scheduler count {idleSchedulerCount}", idleSchedulerCount);
            }
            catch (Exception ex)
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

        public static async Task<ConcurrentBag<DataTable>> RunPerDatabaseCollection(
            DatabaseOperationDelegate databaseCollection, string connectionString, List<string> databases,
            int? threadCount = null)
        {
            threadCount ??= databases.Count==1 ? 1 : await GetThreadCountBasedOnSchedulerAvailability(connectionString);


            // Use a concurrent bag to collect DataTables from parallel tasks.
            var dataTables = new ConcurrentBag<DataTable>();
            var exceptions = new ConcurrentBag<Exception>();

            var semaphore = new SemaphoreSlim(threadCount.Value);

            var tasks = new List<Task>();
            Log.Debug("Running {Method} with thread count {maxDegreeOfParallelism}", databaseCollection.Method.Name, threadCount);
            foreach (var database in databases)
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = database
                };
                // Wait to enter the semaphore. If the maximum number of requests are already running,
                // this will block until one of them completes and releases the semaphore.
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var dt = await databaseCollection(builder.ConnectionString, database);
                        dt.TableName = database;
                        Log.Debug("{Method} for {database} returned {rows} rows", databaseCollection.Method.Name, database, dt.Rows.Count);
                        dataTables.Add(dt); // Ensure dataTables is thread-safe, e.g., ConcurrentBag.
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Running {Method} for {database} failed", databaseCollection.Method.Name, database);
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
            return dataTables;
        }   

        public static async Task<DataTable>RunPerDatabaseCollectionWithUnionResults(DatabaseOperationDelegate databaseCollection,string connectionString, List<string>databases,int? threadCount=null)
        {
            var dataTables = await RunPerDatabaseCollection(databaseCollection, connectionString, databases, threadCount);

            return MergeDataTables(dataTables);
        }

        public static DataTable MergeDataTables(ConcurrentBag<DataTable> dataTables)
        {
            var resultTable = new DataTable();
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

            return resultTable;
        }

        public static async Task<List<string>> GetDatabasesWithQueryStoreAsync(string connectionString)
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

    }
}
