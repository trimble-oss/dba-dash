using DBADashService;
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
    /// Adds source connections from the GUI, after they've been checked with a <see cref="ConnectionTestMessage"/> and the
    /// user has decided what to do with each one.  Each connection is checked again here - the GUI's decisions are only
    /// applied to what the service finds now.  Written to ServiceConfig.json (with a backup), so they show in the config
    /// tool and survive a restart, and collected straight away.  Must be explicitly enabled on the service
    /// (<see cref="CollectionConfig.AllowConnectionChanges"/>).
    /// </summary>
    public class ConnectionAddMessage : MessageBase
    {
        public class ConnectionRequest
        {
            /// <summary>Connection string (or instance name), encrypted with the service's key (<see cref="MessageEncryption"/>).</summary>
            public string ConnectionString { get; set; }

            public SourceConnectionDecisions Decisions { get; set; } = new();
        }

        public List<ConnectionRequest> Connections { get; set; } = new();

        /// <summary>The options for every connection added.  Custom collections aren't allowed.</summary>
        public SourceConnectionOptions Options { get; set; } = new();

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            ConnectionChanges.ThrowIfNotAllowed(cfg);
            ConnectionChanges.ValidateCount(Connections?.Count ?? 0);
            ConnectionChanges.ValidateOptions(Options);

            var sources = new List<(ConnectionRequest Request, DBADashSource Source, string Error)>();
            foreach (var request in Connections)
            {
                try
                {
                    sources.Add((request, ConnectionChanges.CreateSource(request.ConnectionString, Options), null));
                }
                catch (Exception ex)
                {
                    sources.Add((request, null, ex.Message));
                }
            }

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = $"Checking {sources.Count} connection(s)..."
            });

            var results = new List<ConnectionChangeResult>();
            var added = new List<DBADashSource>();
            var updated = new List<DBADashSource>();
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                var fileConfig = ConnectionChanges.LoadFileConfig();
                var checks = await ConnectionChanges.ForEachThrottledAsync(sources,
                    async s => s.Source == null ? null : await SourceConnectionAdder.CheckAsync(fileConfig, s.Source), cancellationToken);

                // Applied one at a time - two requests for the same instance must see each other
                for (var i = 0; i < sources.Count; i++)
                {
                    var (request, src, error) = sources[i];
                    if (src == null)
                    {
                        results.Add(new ConnectionChangeResult { Outcome = ConnectionChangeResult.Failed, Detail = error });
                        continue;
                    }

                    var result = SourceConnectionAdder.Apply(fileConfig, src, checks[i], request.Decisions ?? new SourceConnectionDecisions());
                    results.Add(new ConnectionChangeResult
                    {
                        ConnectionID = src.ConnectionID,
                        ConnectionForPrint = src.SourceConnection.ConnectionForPrint,
                        Outcome = result.Outcome switch
                        {
                            SourceConnectionAddOutcomes.Added => ConnectionChangeResult.Added,
                            SourceConnectionAddOutcomes.Updated => ConnectionChangeResult.Updated,
                            _ => ConnectionChangeResult.Skipped
                        },
                        Detail = result.Reason
                    });
                    if (result.Outcome == SourceConnectionAddOutcomes.Added) added.Add(src);
                    if (result.Outcome == SourceConnectionAddOutcomes.Updated) updated.Add(src);
                }

                if (added.Count + updated.Count == 0)
                {
                    return ConnectionChanges.ToDataSet(results);
                }

                if (!string.IsNullOrEmpty(Options.SchemaSnapshotDBs) && fileConfig.SchemaSnapshotOptions == null)
                {
                    fileConfig.SchemaSnapshotOptions = new SchemaSnapshotDBOptions();
                    cfg.SchemaSnapshotOptions ??= fileConfig.SchemaSnapshotOptions;
                }
                fileConfig.Save();

                // The running service's sources are added to by the Azure DB and read replica scans, so they're only
                // changed under the lock those take
                lock (cfg.SourceConnections)
                {
                    foreach (var src in updated)
                    {
                        cfg.SourceConnections.RemoveAll(s => string.Equals(s.ConnectionID, src.ConnectionID, StringComparison.InvariantCultureIgnoreCase));
                    }
                    cfg.SourceConnections.AddRange(updated);
                    cfg.SourceConnections.AddRange(added);
                }
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            Log.Information("Source connections changed via message {id}: added {added}, updated {updated}", Id,
                added.Select(s => s.ConnectionID), updated.Select(s => s.ConnectionID));

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = "Connections saved to ServiceConfig.json.  Scheduling collections..."
            });

            var warnings = new List<string>();
            try
            {
                // New sources first: scheduling them runs their service start collections, so they show in the GUI.  A
                // reschedule would schedule them without those.
                if (added.Count > 0 && cfg.ScheduleNewSourcesAsync != null)
                {
                    await cfg.ScheduleNewSourcesAsync(added);
                }

                if (updated.Count > 0 && cfg.RescheduleCollectionsAsync != null)
                {
                    await cfg.RescheduleCollectionsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scheduling collections for connections added via message {id}", Id);
                warnings.Add($"The connections were saved, but scheduling their collections failed - restart the service to collect them.  {ex.Message}");
            }

            var failures = await ScheduleInfoReporter.ReportAsync(added.Concat(updated).ToList(), cfg, cancellationToken);
            var warning = MessageWarnings.ForScheduleInfoFailures(failures, "Collection Dates won't show their schedules");
            if (warning != null) warnings.Add(warning);

            return ConnectionChanges.ToDataSet(results, warnings);
        }
    }
}
