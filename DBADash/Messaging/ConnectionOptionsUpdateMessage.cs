using DBADashService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash.Messaging
{
    /// <summary>
    /// Changes the options of existing connections from the GUI - slow query capture, running query plans, deadlocks,
    /// query stats and so on (<see cref="SourceConnectionOptions"/>).  Everything else about the connection is kept,
    /// including settings that aren't among the options and its custom collections, which can only be changed in the
    /// config tool.  Written to ServiceConfig.json (with a backup) and applied to the running service without a restart,
    /// including the databases and read replicas the service discovered from the connection.  Must be explicitly enabled
    /// on the service (<see cref="CollectionConfig.AllowConnectionChanges"/>).
    ///
    /// <para>The collections rebuild the slow query sessions when their configuration changes, but nothing removes a
    /// session that's no longer used until the service stops - so a session made redundant by the change is dropped here
    /// (<see cref="GetSessionsToDrop"/>).</para>
    /// </summary>
    public class ConnectionOptionsUpdateMessage : MessageBase
    {
        public List<string> ConnectionIDs { get; set; } = new();

        public SourceConnectionOptions Options { get; set; } = new();

        /// <summary>Give a connection the default deadlock schedule if deadlock collection is enabled without one</summary>
        public bool UseDefaultDeadlockSchedule { get; set; } = true;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            ConnectionChanges.ThrowIfNotAllowed(cfg);
            ConnectionChanges.ValidateCount(ConnectionIDs?.Count ?? 0);
            ConnectionChanges.ValidateOptions(Options);

            var results = new List<ConnectionChangeResult>();
            // Running sources changed, with their options before the change
            var changed = new List<(DBADashSource Source, SourceConnectionOptions Before)>();
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                var fileConfig = ConnectionChanges.LoadFileConfig();
                var updated = new List<(string ConnectionID, DBADashSource FileSource)>();
                foreach (var connectionID in ConnectionIDs.Distinct(StringComparer.InvariantCultureIgnoreCase))
                {
                    var fileSource = ConnectionRemoveMessage.FindInFileConfig(fileConfig, cfg, connectionID);
                    if (fileSource == null)
                    {
                        results.Add(new ConnectionChangeResult
                        {
                            ConnectionID = connectionID,
                            Outcome = ConnectionChangeResult.Failed,
                            Detail = "Not in the service config - it's monitored automatically, via the \"Scan for AzureDBs\" or \"Monitor read replicas\" option.  Change the connection it was found from."
                        });
                        continue;
                    }

                    if (fileSource.SourceConnection.Type != ConnectionType.SQL)
                    {
                        results.Add(new ConnectionChangeResult { ConnectionID = connectionID, Outcome = ConnectionChangeResult.Failed, Detail = "Only SQL Server connections can be changed from the GUI." });
                        continue;
                    }

                    Options.ApplyTo(fileSource, includeCustomCollections: false);
                    if (UseDefaultDeadlockSchedule && fileSource.IsDeadlockCollectionEnabled && !SourceConnectionAdder.IsDeadlockCollectionScheduled(fileConfig, fileSource))
                    {
                        SourceConnectionAdder.SetDefaultDeadlockSchedule(fileSource);
                    }
                    updated.Add((connectionID, fileSource));
                }

                if (updated.Count == 0)
                {
                    return ConnectionChanges.ToDataSet(results);
                }

                if (!string.IsNullOrEmpty(Options.SchemaSnapshotDBs) && fileConfig.SchemaSnapshotOptions == null)
                {
                    fileConfig.SchemaSnapshotOptions = new SchemaSnapshotDBOptions();
                    cfg.SchemaSnapshotOptions ??= fileConfig.SchemaSnapshotOptions;
                }
                fileConfig.Save();

                var configured = fileConfig.SourceConnections
                    .Where(s => !string.IsNullOrEmpty(s.ConnectionID))
                    .Select(s => s.ConnectionID)
                    .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
                // The running service's sources are added to by the Azure DB and read replica scans, so they're only
                // changed under the lock those take
                lock (cfg.SourceConnections)
                {
                    foreach (var (connectionID, fileSource) in updated)
                    {
                        var running = cfg.SourceConnections
                            .Where(s => string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase)
                                        || s.ConnectionString == fileSource.ConnectionString)
                            .ToList();
                        // Databases and read replicas discovered from it are copies of it - they'd get the new options on restart
                        var derived = CollectionScheduleUpdateMessage.GetDerivedSources(cfg.SourceConnections, connectionID, configured);
                        foreach (var src in running.Concat(derived).Distinct())
                        {
                            changed.Add((src, SourceConnectionOptions.FromSource(src)));
                            Options.ApplyTo(src, includeCustomCollections: false);
                            src.CollectionSchedules = fileSource.CollectionSchedules == null ? null : CollectionSchedules.Combine(new CollectionSchedules(), fileSource.CollectionSchedules);
                        }
                        results.Add(new ConnectionChangeResult
                        {
                            ConnectionID = connectionID,
                            ConnectionForPrint = fileSource.SourceConnection.ConnectionForPrint,
                            Outcome = ConnectionChangeResult.Updated,
                            Detail = derived.Count > 0 ? $"Also applied to {string.Join(", ", derived.Select(d => d.ConnectionID))}" : null
                        });
                    }
                }
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            Log.Information("Connection options changed via message {id} for {connections}: {options}", Id,
                changed.Select(c => c.Source.ConnectionID), Options.Serialize());

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = "Options saved to ServiceConfig.json.  Rescheduling collections..."
            });

            var warnings = new List<string>();
            try
            {
                // The collection jobs carry a copy of the source's settings, so they're rebuilt
                if (cfg.RescheduleCollectionsAsync != null)
                {
                    await cfg.RescheduleCollectionsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error rescheduling collections after changing connection options via message {id}", Id);
                warnings.Add($"The options were saved, but rescheduling failed - restart the service to apply them.  {ex.Message}");
            }

            await DropRedundantSessionsAsync(cfg, changed, results, warnings, cancellationToken);

            var failures = await ScheduleInfoReporter.ReportAsync(changed.Select(c => c.Source).ToList(), cfg, cancellationToken);
            var warning = MessageWarnings.ForScheduleInfoFailures(failures, "Collection Dates won't reflect the change");
            if (warning != null) warnings.Add(warning);

            return ConnectionChanges.ToDataSet(results, warnings);
        }

        /// <summary>
        /// The DBA Dash sessions that changing from <paramref name="before"/> to <paramref name="after"/> leaves unused or
        /// out of date, which nothing else removes until the service stops:
        /// <list type="bullet">
        /// <item>the ring buffer sessions (DBADash_1, DBADash_2) after any change to slow query capture - they're emptied
        /// on every read, so recreating them loses next to nothing;</item>
        /// <item>the event file session when capture is switched off or to another mode - a changed definition is rebuilt
        /// by the collection itself, which keeps its unread events;</item>
        /// <item>the managed deadlock session when deadlocks are read from another session or not at all.</item>
        /// </list>
        /// A session the user created (existing session modes) is never dropped.
        /// </summary>
        public static List<string> GetSessionsToDrop(SourceConnectionOptions before, SourceConnectionOptions after)
        {
            var sessions = new List<string>();
            var wasCapturing = before.SlowQueryThresholdMs >= 0;
            var isCapturing = after.SlowQueryThresholdMs >= 0;
            if (wasCapturing && before.SlowQueryCaptureMode == DBADashSource.SlowQueryCaptureModes.RingBuffer)
            {
                var unchanged = isCapturing
                                && after.SlowQueryCaptureMode == DBADashSource.SlowQueryCaptureModes.RingBuffer
                                && after.SlowQueryThresholdMs == before.SlowQueryThresholdMs
                                && after.UseDualEventSession == before.UseDualEventSession
                                && after.PersistXESessions == before.PersistXESessions;
                if (!unchanged)
                {
                    sessions.AddRange(new[] { "DBADash_1", "DBADash_2" });
                }
            }

            if (wasCapturing && before.SlowQueryCaptureMode == DBADashSource.SlowQueryCaptureModes.EventFile
                             && !(isCapturing && after.SlowQueryCaptureMode == DBADashSource.SlowQueryCaptureModes.EventFile))
            {
                sessions.Add(DBADashSource.ManagedSlowQueryXESessionName);
            }

            if (string.Equals(before.DeadlockXESessionName, DBADashSource.ManagedDeadlockXESessionName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(after.DeadlockXESessionName, DBADashSource.ManagedDeadlockXESessionName, StringComparison.OrdinalIgnoreCase))
            {
                sessions.Add(DBADashSource.ManagedDeadlockXESessionName);
            }

            return sessions;
        }

        private async Task DropRedundantSessionsAsync(CollectionConfig cfg, IReadOnlyList<(DBADashSource Source, SourceConnectionOptions Before)> changed,
            List<ConnectionChangeResult> results, List<string> warnings, CancellationToken cancellationToken)
        {
            var toDrop = changed
                .Select(c => (c.Source, Sessions: GetSessionsToDrop(c.Before, Options)))
                .Where(c => c.Sessions.Count > 0)
                .ToList();
            if (toDrop.Count == 0) return;

            var outcomes = await ConnectionChanges.ForEachThrottledAsync(toDrop, async c =>
            {
                try
                {
                    var collector = await DBCollector.CreateAsync(c.Source, cfg.ServiceName);
                    return (c.Source, Dropped: await collector.DropEventSessionsAsync(c.Sessions, cancellationToken), Error: (string)null);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error dropping extended events sessions no longer used by {connection}", c.Source.SourceConnection.ConnectionForPrint);
                    return (c.Source, Dropped: new List<string>(), Error: ex.Message);
                }
            }, cancellationToken);

            foreach (var (src, dropped, error) in outcomes)
            {
                if (error != null)
                {
                    warnings.Add($"Extended events sessions no longer used by {src.ConnectionID} couldn't be removed - they'll be removed when the service stops.  {error}");
                    continue;
                }
                if (dropped.Count == 0) continue;
                var result = results.FirstOrDefault(r => string.Equals(r.ConnectionID, src.ConnectionID, StringComparison.InvariantCultureIgnoreCase));
                if (result == null) continue;
                var detail = $"Dropped extended events sessions: {string.Join(", ", dropped)}";
                result.Detail = string.IsNullOrEmpty(result.Detail) ? detail : result.Detail + ".  " + detail;
            }
        }
    }
}
