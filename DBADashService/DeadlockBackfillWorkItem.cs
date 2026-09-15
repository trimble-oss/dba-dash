using DBADash;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// The one-off system_health deadlock backfill for an instance, run as a low priority work item.
    ///
    /// <para>A Deadlocks run schedules it by marking it pending - see <see cref="DBCollector.IsDeadlockBackfillPending"/>
    /// - and the <see cref="WorkItem"/> that ran the collection hands it here once its own data is written.  Reading
    /// the whole of system_health can take minutes, so it waits behind the scheduled collections and the low priority
    /// queue's concurrency cap stops a batch of newly enabled instances taking every worker.</para>
    ///
    /// <para>Under traditional scheduling there is no queue, so it runs straight after the collection that scheduled
    /// it, on that collection's thread - the same cost it had when it was part of the collection.</para>
    /// </summary>
    internal sealed class DeadlockBackfillWorkItem : IWorkItem
    {
        private static CollectionWorkQueue _workQueue;

        /// <summary>
        /// Instances with a backfill queued or running.  The queue already refuses a duplicate, but it logs a warning
        /// each time - and every Deadlocks run for the instance would try again while the backfill waits in the low
        /// priority queue, which with a short schedule is a warning every few minutes for nothing.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> Scheduled = new(StringComparer.Ordinal);

        public DBADashSource Source { get; init; }

        public string Schedule { get; set; } = "DeadlockBackfill";

        public string DedupKey => "DeadlockBackfill_" + Source.ConnectionString;

        public WorkItemPriority Priority => WorkItemPriority.Low;

        public string Description => $"Deadlock backfill from system_health for {InstanceName(Source)}";

        /// <summary>The connection ID where the config sets one, otherwise the printable connection.</summary>
        private static string InstanceName(DBADashSource source) =>
            source.ConnectionID ?? source.SourceConnection.ConnectionForPrint;

        /// <summary>Called when queue based scheduling starts.  Left unset under traditional scheduling.</summary>
        public static void Initialize(CollectionWorkQueue workQueue) => _workQueue = workQueue;

        /// <summary>Queues the backfill for <paramref name="source"/>, or runs it now when there is no queue.</summary>
        public static async Task ScheduleAsync(DBADashSource source, CollectionConfig config,
            CancellationToken cancellationToken)
        {
            if (!Scheduled.TryAdd(source.ConnectionString, 0)) return;

            var item = new DeadlockBackfillWorkItem { Source = source };
            if (_workQueue == null)
            {
                await item.ExecuteAsync(config, cancellationToken);
                return;
            }

            try
            {
                if (await _workQueue.EnqueueAsync(item, cancellationToken))
                {
                    Log.Information("Queued deadlock backfill for {instance}", InstanceName(source));
                    return;
                }
            }
            catch
            {
                Scheduled.TryRemove(source.ConnectionString, out _);
                throw;
            }
            // Not queued - the queue has closed.  The backfill is still pending, so a later run schedules it again.
            Scheduled.TryRemove(source.ConnectionString, out _);
        }

        public async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            try
            {
                if (OfflineInstances.IsOffline(Source))
                {
                    // Left pending: the next Deadlocks run once the instance is back schedules it again.
                    Log.Warning("Skipping deadlock backfill for {instance} - offline", InstanceName(Source));
                    return;
                }

                var collector = await DBCollector.CreateAsync(Source, config.ServiceName);
                collector.DeadlockBackfillTimeLimitSeconds = config.GetDeadlockBackfillTimeLimitSeconds();

                // Checked again here: it may have waited in the queue behind a service restart, or been completed by
                // an earlier item for the same instance.
                if (!collector.IsDeadlockBackfillPending) return;

                using var op = SerilogTimings.Operation.Begin("Deadlock backfill for instance {instance}",
                    Source.SourceConnection.ConnectionForPrint);

                if (!await collector.CollectDeadlockBackfillAsync(cancellationToken))
                {
                    op.Complete();
                    return;
                }

                var fileName = DBADashSource.GenerateFileName(Source.SourceConnection.ConnectionForFileName);
                try
                {
                    await DestinationHandling.WriteAllDestinationsAsync(collector.Data, Source, fileName, config);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing {filename} to destination. File will be copied to {folder}",
                        fileName, SchedulerServiceConfig.FailedMessageFolder);
                    try
                    {
                        await DestinationHandling.WriteFolderAsync(collector.Data,
                            SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                    }
                    catch (Exception folderEx)
                    {
                        // Caught so it can't escape to the handler below, which would leave the backfill pending.
                        Log.Error(folderEx, "Error writing {filename} to {folder}", fileName,
                            SchedulerServiceConfig.FailedMessageFolder);
                    }

                    // WriteFolderAsync only logs when the folder is missing - FailedMessageFolder is blank if it
                    // couldn't be created - so check the file is really there.
                    if (string.IsNullOrEmpty(SchedulerServiceConfig.FailedMessageFolder) ||
                        !File.Exists(Path.Combine(SchedulerServiceConfig.FailedMessageFolder, fileName)))
                    {
                        Log.Error("Deadlock backfill for {instance} could not be written to a destination or to the " +
                                  "failed message folder.  Its {count} deadlock(s) are discarded, and the backfill is " +
                                  "not attempted again.", InstanceName(Source),
                            collector.Data.Tables["Deadlocks"]?.Rows.Count ?? 0);
                    }
                }

                // Completed whether or not the write succeeded: a backfill is a full read of system_health, and one
                // that failed to write would be repeated on every scheduled run for as long as the destination is
                // unavailable.  A failed write has gone to the failed message folder, where it can be imported later.
                collector.CompleteDeadlockBackfill();
                op.Complete();
            }
            catch (DatabaseConnectionException ex)
            {
                OfflineInstances.Add(Source, ex.InnerException?.Message);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Left pending, so a later Deadlocks run schedules it again.
                Log.Error(ex, "Error running deadlock backfill for {instance}", Source.SourceConnection.ConnectionForPrint);
            }
            finally
            {
                Scheduled.TryRemove(Source.ConnectionString, out _);
            }
        }
    }
}
