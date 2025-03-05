using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBADash;
using Humanizer;
using Serilog;

namespace DBADashService
{
    public static class OfflineInstances
    {

        private static CancellationTokenSource newInstanceAddedSignal = new CancellationTokenSource();

        private static ConcurrentDictionary<string, (DBADashSource Source,DateTime FirstFail,DateTime LastFail,int FailCount,string FirstMessage, string LastMessage)> Instances { get; } = new();

        public static int OfflineInstanceCount => Instances.Count;

        public static void Add(DBADashSource src, string message)
        {
            AddInternal(src, message);
            newInstanceAddedSignal.Cancel();
        }

        private static void AddInternal(DBADashSource src, string message)
        {
            Log.Warning("Connection to {Connection} failed.  Adding to offline instances", src.ConnectionID ?? src.SourceConnection.ConnectionForPrint);
            Instances.TryAdd(src.SourceConnection.ConnectionString, (src, DateTime.UtcNow, DateTime.UtcNow, 1, message, message));
        }

        public static async Task AddIfOffline(List<DBADashSource> connections, CancellationToken stoppingToken)
        {
            var tasks = connections.Where(src => src.SourceConnection.Type == DBADashConnection.ConnectionType.SQL)
                .Select(instance => CheckConnectionAsync(instance, stoppingToken));
            
            
            // Await all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Remove instances where the connection was successful
            foreach (var result in results)
            {
                if (!result.IsConnected)
                {
                    AddInternal(result.Source,result.message);
                }
            }
            await newInstanceAddedSignal.CancelAsync();
        }

        public static bool IsOffline(DBADashSource src)
        {
            return Instances.ContainsKey(src.SourceConnection.ConnectionString);
        }

        private static async Task DelayBetweenIterations(DateTime waitUntil,CancellationToken stoppingToken)
        {
            while (DateTime.Now < waitUntil)
            {
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, newInstanceAddedSignal.Token);
                try
                {
                    await Task.Delay(500, linkedTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    Log.Debug("Delay between iterations was canceled");
                    // Reset the signal if it was the cause of the cancellation
                    if (newInstanceAddedSignal.IsCancellationRequested)
                    {
                        newInstanceAddedSignal = new CancellationTokenSource();
                    }
                    break; // Exit the delay loop early if the delay was canceled
                }
                await Task.Delay(500, stoppingToken);
            }
        }

        private const int DelayBetweenChecks = 10;

        public static async Task ManageOfflineInstances(CollectionConfig config, CancellationToken stoppingToken)
        {
            var lastCheck = DateTime.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                await DelayBetweenIterations(lastCheck.AddSeconds(DelayBetweenChecks), stoppingToken);
                lastCheck = DateTime.Now;
                try
                {
                    if (Instances.Count > 0)
                    {
                        await CheckConnectionsAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error checking offline connections");
                }
                try
                {
                    await LogOfflineInstances(config,lastCheck);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error logging offline instances");
                }
            }
        }

        private static DataSet GetOfflineDataSet(DateTime snapshotDate)
        {
            var ds = new DataSet();
            var dt = new DataTable();
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("FirstFail", typeof(DateTime));
            dt.Columns.Add("LastFail", typeof(DateTime));
            dt.Columns.Add("FirstMessage", typeof(string));
            dt.Columns.Add("LastMessage", typeof(string));
            dt.Columns.Add("FailCount", typeof(int));
            var errorDT = DBCollector.GetErrorDataTableSchema();
            foreach (var instance in Instances)
            {
                var connectionId = instance.Value.Source.ConnectionID;
                if (string.IsNullOrEmpty(connectionId))
                {
                    Log.Warning("{instance} is offline & doesn't have a ConnectionID set for tracking", instance.Value.Source.SourceConnection.ConnectionForPrint);
                    continue;
                }
                dt.Rows.Add(connectionId, instance.Value.FirstFail, instance.Value.LastFail, instance.Value.FirstMessage,instance.Value.LastMessage, instance.Value.FailCount);
            }
            
            dt.TableName = "OfflineInstances";
            ds.Tables.Add(dt);
            var dtAgent = GetAgentDataTable();
            dtAgent.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            dtAgent.Columns.Add("Instance", typeof(string));
            dtAgent.Columns.Add("DBName", typeof(string));
            dtAgent.Rows[0]["SnapshotDateUTC"] = snapshotDate;
            dtAgent.Rows[0]["Instance"] = "{OfflineInstances}";
            dtAgent.Rows[0]["DBName"] = "{OfflineInstances}";

            ds.Tables.Add(dtAgent);
            ds.DataSetName = "OfflineInstances";

            return ds;
        }

        private static DataTable AgentDataTable;

        private static DataTable GetAgentDataTable()
        {
            if (AgentDataTable != null) return AgentDataTable.Copy();
            AgentDataTable = new DataTable("DBADash");
            DBCollector.AddDBADashServiceMetaData(ref AgentDataTable);
            return AgentDataTable.Copy();
        }

        public static async Task LogOfflineInstances(CollectionConfig config,DateTime snapshotDate)
        {
            var offlineInstances = GetOfflineDataSet(snapshotDate);
            var fileName = DBADashSource.GenerateFileName("OfflineInstances");
            await DestinationHandling.WriteAllDestinations(offlineInstances,fileName, config);
        }

        private static async Task CheckConnectionsAsync(CancellationToken stoppingToken)
        {
            var tasks = Instances.Select(instance => CheckConnectionAsync(instance.Value.Source, stoppingToken)).ToList();
            
            // Await all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Remove instances where the connection was successful
            foreach (var result in results)
            {
                if (result.IsConnected)
                {
                    Instances.TryRemove(result.Source.SourceConnection.ConnectionString, out var instance);
                    Log.Information("Connection to {Connection} was successful.  Instance was offline for {mins}. Removing from offline instances", instance.Source.ConnectionID, instance.LastFail.Subtract(instance.FirstFail).Humanize());
                }
                else
                {
                    var (Source, FirstFail, LastFail, FailCount,FirstMessage,LastMessage) = Instances[result.Source.SourceConnection.ConnectionString];
                    Instances[result.Source.SourceConnection.ConnectionString] = (Source, FirstFail, result.FailTime,FailCount+1,FirstMessage,result.message);
                }
            }
            if(Instances.Count > 0)
            {
                Log.Warning("{InstanceCount} instances are offline", Instances.Count);
            }
        }

        private static async Task<(DBADashSource Source, bool IsConnected,string message,DateTime FailTime)> CheckConnectionAsync(DBADashSource source,CancellationToken stoppingToken)
        {
            try
            {
                await using var cn = new SqlConnection(source.SourceConnection.ConnectionString); 
                
                await cn.OpenAsync(stoppingToken); // Attempt to open the connection asynchronously
                return(source, true, string.Empty,DateTime.UtcNow); // Connection successful
            }
            catch(Exception ex)
            {
               return (source, false,ex.Message, DateTime.UtcNow); // Connection failed
            }

        }

    }
}
