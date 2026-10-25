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
    /// Removes source connections from the GUI.  The connection is removed from ServiceConfig.json (with a backup) and the
    /// running service stops collecting it, along with any databases and read replicas it discovered from it.  Optionally
    /// drops the extended events sessions DBA Dash created on the instance.  The GUI marks the instance deleted in the
    /// repository - the service might not write to it directly.  Must be explicitly enabled on the service
    /// (<see cref="CollectionConfig.AllowConnectionChanges"/>).
    /// </summary>
    public class ConnectionRemoveMessage : MessageBase
    {
        public List<string> ConnectionIDs { get; set; } = new();

        /// <summary>Drop the extended events sessions DBA Dash created on each instance (<see cref="DBCollector.DropDBADashEventSessionsAsync"/>).</summary>
        public bool DropEventSessions { get; set; } = true;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            ConnectionChanges.ThrowIfNotAllowed(cfg);
            ConnectionChanges.ValidateConnectionIDs(ConnectionIDs);

            var results = new List<ConnectionChangeResult>();
            var removedSources = new List<DBADashSource>();
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                var fileConfig = ConnectionChanges.LoadFileConfig();
                var removedFromFile = new List<(string ConnectionID, DBADashSource FileSource)>();
                foreach (var connectionID in ConnectionIDs.Distinct(StringComparer.InvariantCultureIgnoreCase))
                {
                    var fileSource = FindInFileConfig(fileConfig, cfg, connectionID);
                    if (fileSource == null)
                    {
                        results.Add(new ConnectionChangeResult
                        {
                            ConnectionID = connectionID,
                            Outcome = ConnectionChangeResult.Failed,
                            Detail = IsCollected(cfg, connectionID)
                                ? "Not in the service config - it's monitored automatically, via the \"Scan for AzureDBs\" or \"Monitor read replicas\" option on the connection it was found from.  Remove that connection, or turn the option off, to stop collecting it."
                                : "Not collected by this service."
                        });
                        continue;
                    }
                    fileConfig.SourceConnections.Remove(fileSource);
                    removedFromFile.Add((connectionID, fileSource));
                }

                if (removedFromFile.Count == 0)
                {
                    return ConnectionChanges.ToDataSet(results);
                }

                fileConfig.Save();

                lock (cfg.SourceConnections)
                {
                    var configured = ConnectionChanges.GetConfiguredConnectionIDs(fileConfig, cfg.SourceConnections);
                    foreach (var (connectionID, fileSource) in removedFromFile)
                    {
                        var running = cfg.SourceConnections
                            .Where(s => string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase)
                                        || s.ConnectionString == fileSource.ConnectionString)
                            .ToList();
                        // Databases and read replicas discovered from it go with it - they'd be found again from it on restart
                        var derived = CollectionScheduleUpdateMessage.GetDerivedSources(cfg.SourceConnections, connectionID, configured);
                        var toRemove = running.Concat(derived).Distinct().ToList();
                        cfg.SourceConnections.RemoveAll(toRemove.Contains);
                        removedSources.AddRange(toRemove);
                        results.Add(new ConnectionChangeResult
                        {
                            ConnectionID = connectionID,
                            ConnectionForPrint = fileSource.SourceConnection.ConnectionForPrint,
                            Outcome = ConnectionChangeResult.Removed,
                            Detail = derived.Count > 0 ? $"Also stopped collecting {string.Join(", ", derived.Select(d => d.ConnectionID))}" : null
                        });
                    }
                }
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            Log.Information("Source connections removed via message {id}: {connections}", Id, removedSources.Select(s => s.ConnectionID));

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = "Connections removed from ServiceConfig.json.  Rescheduling collections..."
            });

            var warnings = new List<string>();
            try
            {
                if (cfg.RescheduleCollectionsAsync != null)
                {
                    await cfg.RescheduleCollectionsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error rescheduling collections after removing connections via message {id}", Id);
                warnings.Add($"The connections were removed from the config, but rescheduling failed - restart the service to stop collecting them.  {ex.Message}");
            }

            ClearRuntimeState(removedSources.Select(s => s.ConnectionID).Where(id => !string.IsNullOrEmpty(id)).ToList(), warnings);

            if (DropEventSessions)
            {
                await ReportProgressAsync(new ResponseMessage
                {
                    Type = ResponseMessage.ResponseTypes.Progress,
                    Message = "Removing DBA Dash extended events sessions..."
                });
                await DropEventSessionsAsync(cfg, removedSources, results, warnings, cancellationToken);
            }

            return ConnectionChanges.ToDataSet(results, warnings);
        }

        /// <summary>
        /// The source in the saved config for <paramref name="connectionID"/>.  Matched on ConnectionID, or for a source
        /// saved without one (it's generated at runtime), on the connection string of the running source.
        /// </summary>
        internal static DBADashSource FindInFileConfig(CollectionConfig fileConfig, CollectionConfig cfg, string connectionID)
        {
            // Would match a source saved without a ConnectionID
            if (string.IsNullOrWhiteSpace(connectionID)) return null;
            var match = fileConfig.SourceConnections.FirstOrDefault(s =>
                string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
            if (match != null) return match;

            DBADashSource running;
            lock (cfg.SourceConnections)
            {
                running = cfg.SourceConnections.FirstOrDefault(s =>
                    string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
            }
            return running == null
                ? null
                : fileConfig.SourceConnections.FirstOrDefault(s => string.IsNullOrEmpty(s.ConnectionID) && s.ConnectionString == running.ConnectionString);
        }

        private static bool IsCollected(CollectionConfig cfg, string connectionID)
        {
            lock (cfg.SourceConnections)
            {
                return cfg.SourceConnections.Exists(s => string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        /// <summary>Pauses and temporary schedules for removed instances - so they don't apply if the instance is added again.</summary>
        private static void ClearRuntimeState(IReadOnlyCollection<string> connectionIDs, List<string> warnings)
        {
            try
            {
                CollectionPauseStore.Current.Update(pauses =>
                {
                    foreach (var id in connectionIDs) pauses.Remove(id);
                    return pauses;
                });
                TemporaryScheduleStore.Current.Remove(connectionIDs);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error clearing pauses and temporary schedules for removed connections");
                warnings.Add($"Pauses and temporary schedules for the removed connections couldn't be cleared.  {ex.Message}");
            }
        }

        private async Task DropEventSessionsAsync(CollectionConfig cfg, IReadOnlyList<DBADashSource> sources, List<ConnectionChangeResult> results,
            List<string> warnings, CancellationToken cancellationToken)
        {
            var sqlSources = sources.Where(s => s.SourceConnection.Type == ConnectionType.SQL).ToList();
            // A collection that started before the removal would recreate a session dropped under it - and with the
            // source gone, nothing would remove it again
            if (!await CollectionActivity.WaitForIdleAsync(sqlSources, ConnectionChanges.RunningCollectionWait, cancellationToken))
            {
                warnings.Add("A collection that started before the removal was still running, so an extended events session it uses might be recreated after it was dropped.  Check for DBADash sessions on the instance.");
            }
            var outcomes = await ConnectionChanges.ForEachThrottledAsync(sqlSources, async src =>
            {
                try
                {
                    var collector = await DBCollector.CreateAsync(src, cfg.ServiceName);
                    return (src, Dropped: await collector.DropDBADashEventSessionsAsync(cancellationToken), Error: (string)null);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error dropping DBA Dash event sessions for removed connection {connection}", src.SourceConnection.ConnectionForPrint);
                    return (src, Dropped: new List<string>(), Error: ex.Message);
                }
            }, cancellationToken);

            foreach (var (src, dropped, error) in outcomes)
            {
                if (error != null)
                {
                    warnings.Add($"Extended events sessions on {src.ConnectionID} couldn't be removed: {error}");
                }
                else if (dropped.Count > 0)
                {
                    var result = results.FirstOrDefault(r => string.Equals(r.ConnectionID, src.ConnectionID, StringComparison.InvariantCultureIgnoreCase));
                    if (result != null)
                    {
                        var detail = $"Dropped extended events sessions: {string.Join(", ", dropped)}";
                        result.Detail = string.IsNullOrEmpty(result.Detail) ? detail : result.Detail + ".  " + detail;
                    }
                }
            }
        }
    }
}
