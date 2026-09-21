using DBADash;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlocks;
using Quartz;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Brings the deadlock signatures stored in a repository - on AI analyses first, then on the deadlocks - up to the
    /// current signature version, as a low priority work item - see <see cref="DeadlockSignatureRecompute"/>.
    ///
    /// <para>Scheduled per SQL destination by <see cref="DeadlockSignatureRecomputeJob"/>.  Each run works through
    /// batches for up to <see cref="MaxRunTime"/>, then yields its worker until the next run.  Nothing needs remembering
    /// between runs: a recomputed row no longer matches, so the next run picks up whatever is left.  Once a run reaches
    /// the end the job is deleted: there is nothing left to do until a service with a newer signature version starts.</para>
    ///
    /// <para>Under traditional scheduling there is no queue, so it runs on the job's own thread.</para>
    /// </summary>
    internal sealed class DeadlockSignatureRecomputeWorkItem : IWorkItem
    {
        /// <summary>Seconds between runs.</summary>
        public const int IntervalSeconds = 600;

        /// <summary>
        /// Rows recomputed per batch.  Each batch is a connection, a query and an update, so a larger batch spreads that
        /// fixed cost further.  The update is one short statement by primary key, so even if its locks escalate,
        /// collection imports only wait for that statement.
        /// </summary>
        public const int BatchSize = 1000;

        /// <summary>How long one run keeps its worker before leaving the rest to the next run.</summary>
        public static readonly TimeSpan MaxRunTime = TimeSpan.FromMinutes(1);

        private static CollectionWorkQueue _workQueue;

        /// <summary>Destinations with a run queued or in progress - so a run delayed in the low priority queue doesn't
        /// cause a duplicate enqueue warning every interval.</summary>
        private static readonly ConcurrentDictionary<string, byte> Scheduled = new(StringComparer.Ordinal);

        public DBADashSource Source => null;

        public string Schedule { get; set; } = "DeadlockSignatureRecompute";

        public string DedupKey => "DeadlockSignatureRecompute_" + ConnectionString;

        public WorkItemPriority Priority => WorkItemPriority.Low;

        public string Description => $"Deadlock signature recompute for {ConnectionForPrint}";

        public string ConnectionString { get; init; }

        public string ConnectionForPrint { get; init; }

        /// <summary>Called once there is nothing left to recompute.</summary>
        public Func<Task> OnComplete { get; init; }

        /// <summary>Called when queue based scheduling starts.  Left unset under traditional scheduling.</summary>
        public static void Initialize(CollectionWorkQueue workQueue) => _workQueue = workQueue;

        /// <summary>Queues a run, or runs it now when there is no queue.</summary>
        public static async Task ScheduleAsync(DeadlockSignatureRecomputeWorkItem item, CancellationToken cancellationToken)
        {
            if (!Scheduled.TryAdd(item.ConnectionString, 0)) return;

            if (_workQueue == null)
            {
                await item.ExecuteAsync(SchedulerServiceConfig.Config, cancellationToken);
                return;
            }

            try
            {
                if (await _workQueue.EnqueueAsync(item, cancellationToken)) return;
            }
            catch
            {
                Scheduled.TryRemove(item.ConnectionString, out _);
                throw;
            }
            // Not queued - the queue has closed.
            Scheduled.TryRemove(item.ConnectionString, out _);
        }

        public async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                // AI analyses first: there are few of them, and a pattern's answer is what the viewer looks up by the
                // signature it computes now.
                var analysesDone = false;
                var analysesRead = 0;
                var read = 0;
                var failed = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!analysesDone)
                    {
                        var batch = await DeadlockSignatureRecompute.RecomputeAnalysisBatchAsync(ConnectionString, BatchSize,
                            cancellationToken);
                        analysesRead += batch.Read;
                        failed += batch.Failed;
                        analysesDone = batch.IsEnd;
                    }
                    else
                    {
                        var batch = await DeadlockSignatureRecompute.RecomputeBatchAsync(ConnectionString, BatchSize,
                            cancellationToken);
                        read += batch.Read;
                        failed += batch.Failed;

                        if (batch.IsEnd)
                        {
                            if (read > 0 || analysesRead > 0)
                            {
                                Log.Information("Deadlock signature recompute to version {version} complete for {connection}.  " +
                                                "{read} deadlock(s) and {analysesRead} AI analyses read this run, {failed} could not be parsed.",
                                    DeadlockSignature.VersionNumber, ConnectionForPrint, read, analysesRead, failed);
                            }

                            if (OnComplete != null) await OnComplete();
                            return;
                        }
                    }

                    if (sw.Elapsed >= MaxRunTime) break;
                }

                Log.Information("Deadlock signature recompute to version {version} for {connection}: {read} deadlock(s) " +
                                "and {analysesRead} AI analyses read in {elapsed}, {failed} could not be parsed.  Continuing next run.",
                    DeadlockSignature.VersionNumber, ConnectionForPrint, read, analysesRead, sw.Elapsed, failed);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The job stays scheduled, so the next run tries again.
                Log.Error(ex, "Error recomputing deadlock signatures for {connection}", ConnectionForPrint);
            }
            finally
            {
                Scheduled.TryRemove(ConnectionString, out _);
            }
        }
    }

    /// <summary>
    /// Runs <see cref="DeadlockSignatureRecomputeWorkItem"/> for one SQL destination every
    /// <see cref="DeadlockSignatureRecomputeWorkItem.IntervalSeconds"/>, and deletes itself once there is nothing left to do.
    /// </summary>
    [DisallowConcurrentExecution]
    internal sealed class DeadlockSignatureRecomputeJob : IJob
    {
        public async ValueTask Execute(IJobExecutionContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var scheduler = context.Scheduler;
            var jobKey = context.JobDetail.Key;

            try
            {
                await DeadlockSignatureRecomputeWorkItem.ScheduleAsync(new DeadlockSignatureRecomputeWorkItem
                {
                    ConnectionString = dataMap.GetString("ConnectionString"),
                    ConnectionForPrint = dataMap.GetString("ConnectionForPrint"),
                    OnComplete = async () =>
                    {
                        Log.Debug("No deadlock signatures left to recompute.  Removing job {job}", jobKey);
                        await scheduler.DeleteJob(jobKey);
                    }
                }, context.CancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scheduling deadlock signature recompute");
            }
        }
    }
}
