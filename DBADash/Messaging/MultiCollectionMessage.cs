using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Triggers one or more collection types across multiple instances that share the same collect &amp;
    /// import agent.  Instances are processed concurrently (up to <see cref="MaxParallelInstances"/>), each
    /// instance still collected serially, and a per-instance reply is sent back to the GUI as each completes
    /// (via <see cref="MessageBase.ProgressReporter"/>) so they can be ticked off as they finish rather than
    /// waiting for the whole batch.  Grouping by service means a single message fans a collection out across
    /// many instances - far fewer service broker conversations than one message per instance.
    /// </summary>
    public class MultiCollectionMessage : MessageBase
    {
        // Default number of instances to collect concurrently.  Kept low so the repository / network isn't
        // hammered - each individual instance is still collected serially to limit its monitoring impact.
        public const int DefaultMaxParallelInstances = 4;

        public List<string> CollectionTypes { get; set; }

        public List<CollectionInstance> Instances { get; set; } = new();

        /// <summary>
        /// Maximum number of instances collected at the same time.  Collection of a single instance always
        /// runs serially regardless of this value.
        /// </summary>
        public int MaxParallelInstances { get; set; } = DefaultMaxParallelInstances;

        /// <summary>
        /// When true, collection types whose schedule is disabled for an instance are run anyway rather than
        /// skipped.  Propagated to each per-instance <see cref="CollectionMessage"/>.  Set for the forced
        /// re-run after the user confirms they want to run unscheduled collection(s).
        /// </summary>
        public bool IgnoreDisabledSchedule { get; set; }

        public MultiCollectionMessage()
        {
            // Batched collections can run for a while and hold a single service slot for the whole
            // batch.  Allow longer to acquire the processing semaphore than a single message.
            SemaphoreTimeout = 30000;
        }

        public MultiCollectionMessage(List<string> collectionTypes, List<CollectionInstance> instances) : this()
        {
            CollectionTypes = collectionTypes;
            Instances = instances;
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            var parallelism = Math.Max(1, MaxParallelInstances);
            Log.Information("Processing batched collection {Id} for {types} across {count} instances ({parallelism} at a time)",
                Id, CollectionTypes, Instances.Count, parallelism);

            // Throttle limits how many instances collect at once; replyLock serialises the per-instance
            // replies so concurrent collection doesn't interleave sends on the same conversation.
            using var throttle = new SemaphoreSlim(parallelism);
            using var replyLock = new SemaphoreSlim(1, 1);

            var tasks = Instances.Select(instance =>
                ProcessInstanceAsync(cfg, instance, throttle, replyLock, cancellationToken)).ToList();
            await Task.WhenAll(tasks);

            // Per-instance data (if any) has already been relayed via the progress replies above, so the
            // final result is empty - the wrapper sends a Success "Completed" to close the conversation.
            return null;
        }

        private async Task ProcessInstanceAsync(CollectionConfig cfg, CollectionInstance instance,
            SemaphoreSlim throttle, SemaphoreSlim replyLock, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Per-instance collection types take precedence over the message-level list, allowing a single
                // batch to trigger different collections on each instance (e.g. the Collection Dates view).
                var types = instance.CollectionTypes ?? CollectionTypes;

                // A fresh CollectionMessage per instance keeps state isolated across the parallel collections.
                var collector = new CollectionMessage(types, null)
                {
                    CollectAgent = CollectAgent,
                    ImportAgent = ImportAgent,
                    Id = Id,
                    Lifetime = Lifetime,
                    IgnoreDisabledSchedule = IgnoreDisabledSchedule
                };

                DataSet data = null;
                string status;
                string detail = null;
                List<string> disabledCollections = null;
                try
                {
                    data = await collector.CollectAsync(cfg, instance.ConnectionID, instance.DatabaseName, cancellationToken);

                    // In a relay scenario the collected data is returned here rather than written to
                    // destinations, so collection errors are carried in an "Errors" table instead of being
                    // thrown.  Surface them as a failure for this instance, mirroring the direct path which
                    // throws an AggregateException when the collector records errors.
                    if (TryGetCollectionErrors(data, out var errorDetail))
                    {
                        status = "Failed";
                        detail = errorDetail;
                    }
                    else if (collector.UnknownCollectionTypes is { Count: > 0 })
                    {
                        // Some requested collection types were unknown for this instance (e.g. a custom
                        // collection that's since been removed); the valid ones still ran.  Report a warning so
                        // the skipped types are visible rather than silently dropped.
                        status = "Warning";
                        detail = UnknownCollectionTypeException.DescribeSkipped(collector.UnknownCollectionTypes);
                        Log.Warning("Batched collection {Id} skipped unknown collection type(s) for instance {InstanceID} ({ConnectionID}): {detail}",
                            Id, instance.InstanceID, instance.ConnectionID, detail);
                    }
                    else
                    {
                        status = "Success";
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Abort the batch - remaining instances are reported as cancelled by the GUI.
                }
                catch (UnknownCollectionTypeException ex)
                {
                    // None of the requested collections are valid for this instance - report a warning, not a
                    // failure.  Unlike disabled collections there's nothing to re-run, so no disabled list.
                    status = "Warning";
                    detail = ex.Message;
                    Log.Warning("Batched collection {Id} skipped all-unknown collection type(s) for instance {InstanceID} ({ConnectionID}): {detail}",
                        Id, instance.InstanceID, instance.ConnectionID, ex.Message);
                }
                catch (CollectionScheduleDisabledException ex)
                {
                    // The collection(s) are disabled for this instance - report a warning, not a failure.  The
                    // disabled list is carried back so the GUI can offer to re-run just these forced.
                    status = "Warning";
                    detail = ex.Message;
                    disabledCollections = ex.DisabledCollections;
                    Log.Warning("Batched collection {Id} skipped disabled collection(s) for instance {InstanceID} ({ConnectionID}): {detail}",
                        Id, instance.InstanceID, instance.ConnectionID, ex.Message);
                }
                catch (Exception ex)
                {
                    status = "Failed";
                    detail = ex.Message;
                    Log.Error(ex, "Batched collection {Id} failed for instance {InstanceID} ({ConnectionID})",
                        Id, instance.InstanceID, instance.ConnectionID);
                }

                // Send a per-instance reply.  In a relay scenario `data` carries the collected data to be
                // ferried back and written by the origin; otherwise it was already written to destinations
                // and `data` is null.  Type is Progress so the batch keeps running - per-instance failures
                // are reported via CollectionProgress, not by failing the whole conversation.
                var reply = new ResponseMessage
                {
                    Type = ResponseMessage.ResponseTypes.Progress,
                    Message = $"{instance.ConnectionID}: {status}" + (detail != null ? $" - {detail}" : string.Empty),
                    Data = data,
                    CollectionProgress = new CollectionProgress
                    {
                        InstanceID = instance.InstanceID,
                        Status = status,
                        Detail = detail,
                        DisabledCollections = disabledCollections
                    }
                };

                await replyLock.WaitAsync(cancellationToken);
                try
                {
                    await ReportProgressAsync(reply);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A failed progress reply (e.g. a transient send error) must not sink the whole batch -
                    // this instance's collection has already run.  Log it and let the other instances finish.
                    Log.Error(ex, "Batched collection {Id} failed to send progress reply for instance {InstanceID} ({ConnectionID})",
                        Id, instance.InstanceID, instance.ConnectionID);
                }
                finally
                {
                    replyLock.Release();
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        /// <summary>
        /// Reads any collection errors recorded in the data's "Errors" table (populated in the relay scenario
        /// where the data is ferried back rather than written here).  Returns true with the concatenated error
        /// messages when present so the instance can be reported as failed.
        /// </summary>
        private static bool TryGetCollectionErrors(DataSet data, out string detail)
        {
            detail = null;
            if (data == null || !data.Tables.Contains("Errors")) return false;

            var errors = data.Tables["Errors"];
            if (errors.Rows.Count == 0) return false;

            detail = errors.Columns.Contains("ErrorMessage")
                ? string.Join(Environment.NewLine, errors.Rows.Cast<DataRow>()
                    .Select(r => Convert.ToString(r["ErrorMessage"]))
                    .Where(m => !string.IsNullOrWhiteSpace(m)))
                : null;
            if (string.IsNullOrWhiteSpace(detail)) detail = $"{errors.Rows.Count} collection error(s).";
            return true;
        }
    }

    /// <summary>
    /// Identifies a single instance within a <see cref="MultiCollectionMessage"/>.
    /// </summary>
    public class CollectionInstance
    {
        public int InstanceID { get; set; }

        public string ConnectionID { get; set; }

        public string DatabaseName { get; set; }

        /// <summary>
        /// Collection types to run for this specific instance.  When null the message-level
        /// <see cref="MultiCollectionMessage.CollectionTypes"/> is used instead.  Allows a single batch to
        /// trigger a different set of collections per instance (e.g. the Collection Dates view).
        /// </summary>
        public List<string> CollectionTypes { get; set; }
    }
}