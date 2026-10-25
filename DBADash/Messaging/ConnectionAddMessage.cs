using DBADashService;
using Microsoft.Data.SqlClient;
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
            var applied = new List<DBADashSource>();
            // Running sources replaced, with their options before the change - for the sessions it leaves unused
            var replaced = new List<(DBADashSource Source, SourceConnectionOptions Before)>();
            // New to the running service - their collections are scheduled and run straight away
            var newSources = new List<DBADashSource>();
            // Sources that replaced others in the running service, whose jobs are rebuilt
            var replacing = new List<DBADashSource>();
            // The running sources they replaced, whose collections may still be running
            var replacedRunning = new List<DBADashSource>();
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

                    await SourceConnectionAdder.RefreshExistingAsync(fileConfig, src, checks[i]);
                    var result = SourceConnectionAdder.Apply(fileConfig, src, checks[i], request.Decisions ?? new SourceConnectionDecisions());
                    results.Add(new ConnectionChangeResult
                    {
                        ConnectionID = result.Source?.ConnectionID ?? src.ConnectionID,
                        ConnectionForPrint = src.SourceConnection.ConnectionForPrint,
                        Outcome = result.Outcome switch
                        {
                            SourceConnectionAddOutcomes.Added => ConnectionChangeResult.Added,
                            SourceConnectionAddOutcomes.Updated => ConnectionChangeResult.Updated,
                            _ => ConnectionChangeResult.Skipped
                        },
                        Detail = result.Reason
                    });
                    if (result.Source != null) applied.Add(result.Source);
                }

                // A source can be replaced by a later request for the same instance - only those still in the config count
                applied = applied.Where(fileConfig.SourceConnections.Contains).ToList();
                if (applied.Count == 0)
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
                    var configured = ConnectionChanges.GetConfiguredConnectionIDs(fileConfig, cfg.SourceConnections);
                    foreach (var src in applied)
                    {
                        // The connection it updates, or a database or replica the service discovered and monitors already
                        var running = cfg.SourceConnections
                            .Where(s => string.Equals(s.ConnectionID, src.ConnectionID, StringComparison.InvariantCultureIgnoreCase)
                                        || s.ConnectionString == src.ConnectionString)
                            .ToList();
                        // Databases and read replicas discovered from an updated connection are copies of it - they'd get
                        // its new options and schedules on restart
                        var derived = running.Count == 0
                            ? new List<DBADashSource>()
                            : CollectionScheduleUpdateMessage.GetDerivedSources(cfg.SourceConnections, src.ConnectionID, configured);

                        replaced.AddRange(running.Select(s => (Source: src, Before: SourceConnectionOptions.FromSource(s))));
                        replacedRunning.AddRange(running);
                        cfg.SourceConnections.RemoveAll(running.Contains);
                        cfg.SourceConnections.Add(src);
                        (running.Count == 0 ? newSources : replacing).Add(src);

                        var options = SourceConnectionOptions.FromSource(src);
                        foreach (var child in derived)
                        {
                            replaced.Add((child, SourceConnectionOptions.FromSource(child)));
                            // The parent's connection string may have changed (e.g. a new password) - the child's is the
                            // parent's with its own database and application intent
                            child.ConnectionString = GetDerivedConnectionString(src, child);
                            options.ApplyTo(child, includeCustomCollections: false);
                            child.CollectionSchedules = src.CollectionSchedules == null ? null : CollectionSchedules.Combine(new CollectionSchedules(), src.CollectionSchedules);
                            replacing.Add(child);
                        }
                    }
                }
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            Log.Information("Source connections changed via message {id}: added {added}, replaced {replaced}", Id,
                newSources.Select(s => s.ConnectionID), replacing.Select(s => s.ConnectionID));

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
                if (newSources.Count > 0 && cfg.ScheduleNewSourcesAsync != null)
                {
                    await cfg.ScheduleNewSourcesAsync(newSources);
                }

                // The jobs of a replaced source carry its old settings
                if (replacing.Count > 0 && cfg.RescheduleCollectionsAsync != null)
                {
                    await cfg.RescheduleCollectionsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scheduling collections for connections added via message {id}", Id);
                warnings.Add($"The connections were saved, but scheduling their collections failed - restart the service to collect them.  {ex.Message}");
            }

            await ConnectionChanges.DropRedundantSessionsAsync(cfg, replaced, results, warnings, cancellationToken, replacedRunning);

            var failures = await ScheduleInfoReporter.ReportAsync(newSources.Concat(replacing).Distinct().ToList(), cfg, cancellationToken);
            var warning = MessageWarnings.ForScheduleInfoFailures(failures, "Collection Dates won't show their schedules");
            if (warning != null) warnings.Add(warning);

            return ConnectionChanges.ToDataSet(results, warnings);
        }

        /// <summary>
        /// The connection string for a database or read replica discovered from <paramref name="parent"/>: the parent's,
        /// with the child's database and application intent - as the Azure DB and read replica scans build it.
        /// </summary>
        internal static string GetDerivedConnectionString(DBADashSource parent, DBADashSource child)
        {
            var childBuilder = new SqlConnectionStringBuilder(child.SourceConnection.ConnectionString);
            return new SqlConnectionStringBuilder(parent.SourceConnection.ConnectionString)
            {
                InitialCatalog = childBuilder.InitialCatalog,
                ApplicationIntent = childBuilder.ApplicationIntent
            }.ConnectionString;
        }
    }
}
