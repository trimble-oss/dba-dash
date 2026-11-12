using DBADashService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// The collection work queue's state for a service stats sample (CollectionWorkQueue.TakeStats, queue based scheduling).
    /// The queue lengths and busy workers are as of the sample; the rest cover the time since the previous sample.
    /// </summary>
    public readonly record struct WorkQueueStats
    {
        public int QueueLengthHigh { get; init; }
        public int QueueLengthNormal { get; init; }
        public int QueueLengthLow { get; init; }
        public int Workers { get; init; }
        public int BusyWorkers { get; init; }
        /// <summary>The most workers processing an item at once</summary>
        public int MaxBusyWorkers { get; init; }
        /// <summary>Items taken off the queue by a worker</summary>
        public int Started { get; init; }
        /// <summary>Items accepted onto the queue - with <see cref="Skipped"/>, every attempt to queue one</summary>
        public int Queued { get; init; }
        /// <summary>Enqueues skipped because the same collection was still queued or running</summary>
        public int Skipped { get; init; }
        /// <summary>Time the started items waited for a worker, added up</summary>
        public long WaitTotalMs { get; init; }
        /// <summary>The longest any started item waited for a worker</summary>
        public long WaitMaxMs { get; init; }
    }

    /// <summary>The source connections the service is handling, as of a service stats sample</summary>
    public readonly record struct SourceConnectionCounts
    {
        /// <summary>Every source: SQL instances and databases, plus folder and S3 imports</summary>
        public int Sources { get; init; }
        /// <summary>SQL connections - an instance, Azure DB or read replica each</summary>
        public int SQL { get; init; }
        /// <summary>Sources the service added itself - Azure DBs found from a master connection, read replicas</summary>
        public int Discovered { get; init; }

        /// <summary>Counts <paramref name="sources"/>, locking it - the service adds and removes sources as it runs</summary>
        public static SourceConnectionCounts Count(List<DBADashSource> sources)
        {
            lock (sources)
            {
                return new SourceConnectionCounts
                {
                    Sources = sources.Count,
                    SQL = sources.Count(s => s.SourceConnection?.Type == DBADashConnection.ConnectionType.SQL),
                    Discovered = sources.Count(s => s.IsDiscovered)
                };
            }
        }
    }

    /// <summary>
    /// Samples the service's own resource usage - CPU, memory, garbage collection, threads, the collection work queue - from
    /// the process itself, so a sample runs no queries.  A sample's CPU, GC and queue throughput figures cover the time since
    /// the previous sample, or since the process started for the first one, so each sample stands on its own.  See
    /// dbo.ServiceStats for what each column means.
    /// </summary>
    public sealed class ServiceStatsSampler
    {
        public const string DataSetName = "ServiceStats";
        public const string TableName = "ServiceStats";

        private readonly Func<WorkQueueStats?> workQueue;
        private readonly Func<int?> offlineInstances;
        private readonly Func<SourceConnectionCounts?> sourceConnections;

        private DateTime? previousUtc;
        private TimeSpan previousCpu;
        private int previousGen0;
        private int previousGen1;
        private int previousGen2;
        private TimeSpan previousGCPause;

        /// <param name="workQueue">The collection work queue's stats, or null where the service doesn't queue collections</param>
        /// <param name="offlineInstances">Source connections the service is treating as offline</param>
        /// <param name="sourceConnections">The source connections the service is handling</param>
        public ServiceStatsSampler(Func<WorkQueueStats?> workQueue = null, Func<int?> offlineInstances = null, Func<SourceConnectionCounts?> sourceConnections = null)
        {
            this.workQueue = workQueue ?? (() => null);
            this.offlineInstances = offlineInstances ?? (() => null);
            this.sourceConnections = sourceConnections ?? (() => null);
        }

        /// <summary>The table samples are added to - columns in the order of the dbo.ServiceStats table type</summary>
        public static DataTable CreateTable()
        {
            var dt = new DataTable(TableName);
            dt.Columns.Add("SnapshotDate", typeof(DateTime));
            dt.Columns.Add("ProcessStartDate", typeof(DateTime));
            dt.Columns.Add("IntervalMs", typeof(int));
            dt.Columns.Add("CpuMs", typeof(int));
            dt.Columns.Add("ProcessorCount", typeof(short));
            dt.Columns.Add("WorkingSetKB", typeof(int));
            dt.Columns.Add("PeakWorkingSetKB", typeof(int));
            dt.Columns.Add("PrivateBytesKB", typeof(int));
            dt.Columns.Add("ManagedHeapKB", typeof(int));
            dt.Columns.Add("GCCommittedKB", typeof(int));
            dt.Columns.Add("Gen0Collections", typeof(int));
            dt.Columns.Add("Gen1Collections", typeof(int));
            dt.Columns.Add("Gen2Collections", typeof(int));
            dt.Columns.Add("GCPauseMs", typeof(int));
            dt.Columns.Add("ThreadCount", typeof(int));
            dt.Columns.Add("ThreadPoolThreads", typeof(int));
            dt.Columns.Add("ThreadPoolQueueLength", typeof(int));
            dt.Columns.Add("HandleCount", typeof(int));
            dt.Columns.Add("SourceConnections", typeof(int));
            dt.Columns.Add("SQLConnections", typeof(int));
            dt.Columns.Add("DiscoveredConnections", typeof(int));
            dt.Columns.Add("OfflineInstances", typeof(int));
            dt.Columns.Add("QueueLengthHigh", typeof(int));
            dt.Columns.Add("QueueLengthNormal", typeof(int));
            dt.Columns.Add("QueueLengthLow", typeof(int));
            dt.Columns.Add("Workers", typeof(int));
            dt.Columns.Add("BusyWorkers", typeof(int));
            dt.Columns.Add("MaxBusyWorkers", typeof(int));
            dt.Columns.Add("QueueItemsStarted", typeof(int));
            dt.Columns.Add("QueueSkipped", typeof(int));
            dt.Columns.Add("QueueWaitTotalMs", typeof(long));
            dt.Columns.Add("QueueWaitMaxMs", typeof(int));
            dt.Columns.Add("QueueItemsQueued", typeof(int));
            return dt;
        }

        /// <summary>Takes a sample, as a row of <paramref name="dt"/> (from <see cref="CreateTable"/>), and adds it</summary>
        public DataRow Sample(DataTable dt)
        {
            var nowUtc = DateTime.UtcNow;
            using var process = Process.GetCurrentProcess();
            var startUtc = TryGet(() => (DateTime?)process.StartTime.ToUniversalTime());
            var cpu = TryGet(() => (TimeSpan?)process.TotalProcessorTime);
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var gcPause = GC.GetTotalPauseDuration();

            // The first sample covers the time since the process started, against counters that started at zero with it
            var since = previousUtc ?? startUtc ?? nowUtc;

            var row = dt.NewRow();
            row["SnapshotDate"] = nowUtc;
            row["ProcessStartDate"] = startUtc is { } start ? start : DBNull.Value;
            row["IntervalMs"] = ToInt((nowUtc - since).TotalMilliseconds);
            row["CpuMs"] = cpu is { } c ? ToInt((c - previousCpu).TotalMilliseconds) : DBNull.Value;
            row["ProcessorCount"] = (short)Math.Min(Environment.ProcessorCount, short.MaxValue);
            row["WorkingSetKB"] = KB(TryGet(() => (long?)process.WorkingSet64));
            row["PeakWorkingSetKB"] = KB(TryGet(() => (long?)process.PeakWorkingSet64));
            row["PrivateBytesKB"] = KB(TryGet(() => (long?)process.PrivateMemorySize64));
            row["ManagedHeapKB"] = KB(GC.GetTotalMemory(false));
            row["GCCommittedKB"] = KB(GC.GetGCMemoryInfo().TotalCommittedBytes);
            row["Gen0Collections"] = gen0 - previousGen0;
            row["Gen1Collections"] = gen1 - previousGen1;
            row["Gen2Collections"] = gen2 - previousGen2;
            row["GCPauseMs"] = ToInt((gcPause - previousGCPause).TotalMilliseconds);
            row["ThreadCount"] = Value(TryGet(() => (int?)process.Threads.Count));
            row["ThreadPoolThreads"] = ThreadPool.ThreadCount;
            row["ThreadPoolQueueLength"] = ToInt(ThreadPool.PendingWorkItemCount);
            row["HandleCount"] = Value(TryGet(() => (int?)process.HandleCount));
            if (TryGet(sourceConnections) is { } sources)
            {
                row["SourceConnections"] = sources.Sources;
                row["SQLConnections"] = sources.SQL;
                row["DiscoveredConnections"] = sources.Discovered;
            }
            row["OfflineInstances"] = Value(TryGet(offlineInstances));
            if (TryGet(workQueue) is { } queue)
            {
                row["QueueLengthHigh"] = queue.QueueLengthHigh;
                row["QueueLengthNormal"] = queue.QueueLengthNormal;
                row["QueueLengthLow"] = queue.QueueLengthLow;
                row["Workers"] = queue.Workers;
                row["BusyWorkers"] = queue.BusyWorkers;
                row["MaxBusyWorkers"] = queue.MaxBusyWorkers;
                row["QueueItemsStarted"] = queue.Started;
                row["QueueSkipped"] = queue.Skipped;
                row["QueueWaitTotalMs"] = queue.WaitTotalMs;
                row["QueueWaitMaxMs"] = ToInt(queue.WaitMaxMs);
                row["QueueItemsQueued"] = queue.Queued;
            }
            dt.Rows.Add(row);

            previousUtc = nowUtc;
            if (cpu is { } total) previousCpu = total;
            previousGen0 = gen0;
            previousGen1 = gen1;
            previousGen2 = gen2;
            previousGCPause = gcPause;
            return row;
        }

        /// <summary>
        /// The samples as a file for the destinations - the agent's metadata identifies the service they're from, so a file
        /// imported by another service (S3, folder) is still recorded against the service that took them.
        /// </summary>
        public static DataSet BuildDataSet(DataTable samples)
        {
            var ds = new DataSet(DataSetName);
            var dtAgent = new DataTable("DBADash");
            DBCollector.AddDBADashServiceMetadata(ref dtAgent);
            dtAgent.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            // Folder and S3 destinations identify a file by Instance and DBName before importing it (see ScheduleInfoReporter)
            dtAgent.Columns.Add("Instance", typeof(string));
            dtAgent.Columns.Add("DBName", typeof(string));
            dtAgent.Rows[0]["SnapshotDateUTC"] = DateTime.UtcNow;
            dtAgent.Rows[0]["Instance"] = FileNamePart;
            dtAgent.Rows[0]["DBName"] = DataSetName;
            ds.Tables.Add(dtAgent);
            ds.Tables.Add(samples);
            return ds;
        }

        /// <summary>Identifies the service's files - ServiceStats_{service name}, with anything that can't be in a file name replaced</summary>
        internal static string FileNamePart
        {
            get
            {
                var name = DBADashAgent.GetCurrent().AgentServiceName ?? string.Empty;
                var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '_' }).ToHashSet();
                return DataSetName + "_" + new string(name.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
            }
        }

        private static T TryGet<T>(Func<T> get)
        {
            try
            {
                return get();
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
            {
                return default;
            }
        }

        private static object KB(long? bytes) => bytes is { } b ? ToInt(b / 1024) : DBNull.Value;

        private static object Value(int? value) => value is { } v ? v : DBNull.Value;

        private static int ToInt(double value) => (int)Math.Clamp(Math.Round(value), 0, int.MaxValue);

        private static int ToInt(long value) => (int)Math.Clamp(value, 0, int.MaxValue);
    }

    /// <summary>
    /// Samples the service's resource usage (<see cref="ServiceStatsSampler"/>) every
    /// <see cref="CollectionConfig.GetServiceStatsFrequencySeconds"/> - only with LogInternalPerformanceCounters on - and
    /// sends it to each destination.  A SQL destination gets each sample as it's taken.  A folder or S3 destination gets them
    /// batched, every <see cref="CollectionConfig.GetServiceStatsReportInterval"/>, as each send is a file for the importing
    /// service (#2042).
    /// Each destination keeps the samples it hasn't been sent - up to <see cref="MaxPendingSamples"/> - for its next send, so
    /// an outage leaves no gap, and one destination being down doesn't hold up or resend to the others.  A send that's still
    /// in progress (the repository being down holds it until it's back) doesn't stop the sampling.
    /// </summary>
    public static class ServiceStatsReporter
    {
        /// <summary>Samples kept for a destination while they can't be sent.  A day, sampling every minute.</summary>
        public const int MaxPendingSamples = 1440;

        private static readonly TimeSpan ShutdownSendTimeout = TimeSpan.FromSeconds(10);

        /// <summary>A destination's samples waiting to be sent, and its send in progress</summary>
        private sealed class DestinationQueue(DBADashConnection destination, TimeSpan interval)
        {
            public DBADashConnection Destination { get; } = destination;
            /// <summary>Zero to send each sample as it's taken</summary>
            public TimeSpan Interval { get; } = interval;
            public DataTable Pending { get; } = ServiceStatsSampler.CreateTable();
            public Task Sending { get; set; } = Task.CompletedTask;
            /// <summary>The first send is straight away - a restart shows without waiting</summary>
            public DateTime LastSend { get; set; } = DateTime.MinValue;
        }

        /// <summary>How often <paramref name="destination"/> is sent samples - each one for SQL, batched for a file</summary>
        public static TimeSpan SendInterval(DBADashConnection destination, CollectionConfig config) =>
            destination.Type == DBADashConnection.ConnectionType.SQL
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(config.GetServiceStatsReportInterval());

        public static async Task RunAsync(CollectionConfig config, Func<WorkQueueStats?> workQueue, CancellationToken cancellationToken)
        {
            var sampleSeconds = config.GetServiceStatsFrequencySeconds();
            if (sampleSeconds <= 0)
            {
                Log.Information("Service stats are disabled{reason}", config.LogInternalPerformanceCounters ? string.Empty : " - they're collected with LogInternalPerformanceCounters on");
                return;
            }
            var queues = config.AllDestinations.Select(d => new DestinationQueue(d, SendInterval(d, config))).ToList();
            Log.Information("Service stats sampled every {sample} seconds.  Sent to SQL destinations as they're taken, to folder and S3 destinations every {report} seconds",
                sampleSeconds, config.GetServiceStatsReportInterval());

            var sampler = new ServiceStatsSampler(workQueue, () => OfflineInstances.OfflineInstanceCount,
                () => SourceConnectionCounts.Count(config.SourceConnections));
            var sample = ServiceStatsSampler.CreateTable();

            void Sample()
            {
                sample.Clear();
                var row = sampler.Sample(sample);
                foreach (var q in queues)
                {
                    lock (q.Pending)
                    {
                        q.Pending.ImportRow(row);
                        while (q.Pending.Rows.Count > MaxPendingSamples)
                        {
                            q.Pending.Rows.RemoveAt(0);
                        }
                    }
                }
            }

            async Task SendAsync(DestinationQueue q)
            {
                DataTable batch;
                lock (q.Pending)
                {
                    if (q.Pending.Rows.Count == 0) return;
                    batch = q.Pending.Copy();
                }
                var sent = batch.Rows.Cast<DataRow>().Select(r => (DateTime)r["SnapshotDate"]).ToHashSet();
                try
                {
                    var fileName = DBADashSource.GenerateFileName(ServiceStatsSampler.FileNamePart);
                    await DestinationHandling.WriteAsync(ServiceStatsSampler.BuildDataSet(batch), q.Destination, fileName, config);
                }
                catch (Exception ex)
                {
                    // Kept for the next send.  A sample the destination did get is skipped then (ServiceStats_Upd).
                    Log.Warning(ex, "Error writing service stats to {destination}.  {count} samples will be sent again", q.Destination.ConnectionForPrint, sent.Count);
                    return;
                }
                lock (q.Pending)
                {
                    foreach (var row in q.Pending.Rows.Cast<DataRow>().Where(r => sent.Contains((DateTime)r["SnapshotDate"])).ToList())
                    {
                        q.Pending.Rows.Remove(row);
                    }
                }
            }

            void SendIfDue()
            {
                foreach (var q in queues)
                {
                    if (!q.Sending.IsCompleted || DateTime.UtcNow - q.LastSend < q.Interval) continue;
                    q.LastSend = DateTime.UtcNow;
                    q.Sending = Task.Run(() => SendAsync(q), CancellationToken.None);
                }
            }

            try
            {
                Sample();
                SendIfDue();
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(sampleSeconds));
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        Sample();
                        SendIfDue();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error sampling service stats");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Stopping - a last sample, so the usage right up to the stop is kept
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Service stats stopped");
                return;
            }

            try
            {
                Sample();
                // Destinations still waiting on a send are left - the service won't wait for them either
                var sends = queues.Where(q => q.Sending.IsCompleted).Select(SendAsync);
                await Task.WhenAll(sends).WaitAsync(ShutdownSendTimeout, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error sending service stats on stop");
            }
        }
    }
}
