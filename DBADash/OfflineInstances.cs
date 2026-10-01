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

        private static ConcurrentDictionary<string, (DBADashSource Source, DateTime FirstFail, DateTime LastFail, int FailCount, string FirstMessage, string LastMessage)> Instances { get; } = new();

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
                    AddInternal(result.Source, result.message);
                }
            }
            await newInstanceAddedSignal.CancelAsync();
        }

        public static bool IsOffline(DBADashSource src)
        {
            return Instances.ContainsKey(src.SourceConnection.ConnectionString);
        }

        private static async Task DelayBetweenIterations(DateTime waitUntil, CancellationToken stoppingToken)
        {
            while (DateTime.Now < waitUntil)
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, newInstanceAddedSignal.Token);
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

        /// <summary>
        /// How long before a report that didn't reach every destination is repeated, in seconds.  Used in
        /// place of the configured interval, which can be 0 for no repeat at all.
        /// </summary>
        private const int FailedReportRetryInterval = 60;

        private static string lastReportedState;
        private static DateTime lastReportDate = DateTime.MinValue;
        private static bool lastReportFailed;

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
                    await LogOfflineInstancesIfRequired(config);
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
                dt.Rows.Add(connectionId, instance.Value.FirstFail, instance.Value.LastFail, instance.Value.FirstMessage, instance.Value.LastMessage, instance.Value.FailCount);
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
            if (AgentDataTable is { Rows.Count: 1 }) return AgentDataTable.Copy();
            AgentDataTable = GenerateDBADashDataTable();
            return AgentDataTable.Copy();
        }

        private static DataTable GenerateDBADashDataTable()
        {
            var dt = new DataTable("DBADash");
            DBCollector.AddDBADashServiceMetadata(ref dt);
            return dt;
        }

        public static async Task LogOfflineInstances(CollectionConfig config, DateTime snapshotDate)
        {
            var offlineInstances = GetOfflineDataSet(snapshotDate);
            var fileName = DBADashSource.GenerateFileName("OfflineInstances");
            await DestinationHandling.WriteAllDestinationsAsync(offlineInstances, fileName, config);
        }

        /// <summary>
        /// Identifies the current set of offline instances by the ConnectionID and FirstFail of each open
        /// incident.  LastFail and FailCount are deliberately excluded as they change on every check, which
        /// would make every state look like a new one.  Instances without a ConnectionID are excluded to
        /// match what's actually reported - see <see cref="GetOfflineDataSet"/>.
        /// </summary>
        private static string GetCurrentState() =>
            string.Join('|', Instances.Values
                .Where(instance => !string.IsNullOrEmpty(instance.Source.ConnectionID))
                .Select(instance => instance.Source.ConnectionID + "@" + instance.FirstFail.Ticks)
                .OrderBy(key => key, StringComparer.Ordinal));

        /// <summary>
        /// Report the offline instances if the set of them has changed since the last report, or if the
        /// report interval has elapsed.  The repository opens and closes offline incidents based on these
        /// reports, so a change is always reported immediately - the repeat exists to recover the state if a
        /// report is lost and to keep LastFail/FailCount current.  Repeating an unchanged state on a short
        /// interval gains nothing at any destination, and on a folder/S3 destination it leaves a file behind
        /// each time, which piles up if the service importing them falls behind (#2042).
        /// </summary>
        private static async Task LogOfflineInstancesIfRequired(CollectionConfig config)
        {
            var state = GetCurrentState();
            // Note: SnapshotDateUTC, so UtcNow rather than the local time used to schedule the checks.
            var now = DateTime.UtcNow;
            // A report that threw is repeated on its own interval rather than on the next check.  A write to
            // several destinations can succeed on some and fail on one, so repeating it every 10 seconds would
            // leave a file behind at every healthy folder/S3 destination each time, which is the backlog this
            // is meant to avoid (#2042).
            var interval = lastReportFailed ? FailedReportRetryInterval : config.OfflineInstancesReportInterval;
            if (!IsReportRequired(state, lastReportedState, lastReportDate, now, interval)) return;

            var reported = false;
            try
            {
                await LogOfflineInstances(config, now);
                reported = true;
            }
            finally
            {
                // Recorded even when the report threw, so that the retry is driven by the interval above
                // rather than by the state looking new on every check.  A further change to the set of
                // offline instances is still reported straight away.
                lastReportedState = state;
                lastReportDate = now;
                lastReportFailed = !reported;
            }
        }

        /// <summary>
        /// A change to the set of offline instances is always reported.  An unchanged state is reported again
        /// once <paramref name="reportInterval"/> seconds have elapsed, or not at all if it's 0.
        /// </summary>
        internal static bool IsReportRequired(string currentState, string previousState, DateTime previousReportDate, DateTime now, int reportInterval) =>
            currentState != previousState ||
            (reportInterval > 0 && now >= previousReportDate.AddSeconds(reportInterval));

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
                    var (Source, FirstFail, LastFail, FailCount, FirstMessage, LastMessage) = Instances[result.Source.SourceConnection.ConnectionString];
                    Instances[result.Source.SourceConnection.ConnectionString] = (Source, FirstFail, result.FailTime, FailCount + 1, FirstMessage, result.message);
                }
            }
            if (Instances.Count > 0)
            {
                Log.Warning("{InstanceCount} instances are offline", Instances.Count);
            }
        }

        private static async Task<(DBADashSource Source, bool IsConnected, string message, DateTime FailTime)> CheckConnectionAsync(DBADashSource source, CancellationToken stoppingToken)
        {
            try
            {
                await using var cn = new SqlConnection(source.SourceConnection.ConnectionString);

                await cn.OpenAsync(stoppingToken); // Attempt to open the connection asynchronously
                return (source, true, string.Empty, DateTime.UtcNow); // Connection successful
            }
            catch (Exception ex)
            {
                return (source, false, ex.Message, DateTime.UtcNow); // Connection failed
            }
        }
    }
}