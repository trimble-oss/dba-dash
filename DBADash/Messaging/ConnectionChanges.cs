using Newtonsoft.Json;
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
    /// Shared by the messages that add and remove source connections from the GUI (<see cref="ConnectionTestMessage"/>,
    /// <see cref="ConnectionAddMessage"/>, <see cref="ConnectionRemoveMessage"/>).
    /// </summary>
    public static class ConnectionChanges
    {
        /// <summary>The most connections one message can add, check or remove</summary>
        public const int MaxConnectionsPerMessage = 100;

        /// <summary>Connections checked at once - an unreachable instance takes the connect timeout to fail</summary>
        internal const int MaxConcurrentChecks = 8;

        internal const string PayloadTableName = "Payload";

        /// <summary>Throws unless the service accepts connection changes and has finished starting up.</summary>
        internal static void ThrowIfNotAllowed(CollectionConfig cfg)
        {
            if (!cfg.AllowConnectionChanges)
            {
                throw new Exception("Adding and removing connections is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            if (!cfg.AcceptsSourceChanges)
            {
                throw new Exception("The DBA Dash service is still starting.  Try again in a moment.");
            }
        }

        /// <summary>
        /// A source for an encrypted connection string sent from the GUI, with <paramref name="options"/>.  Only SQL
        /// connections - a folder or S3 path would have the service import from wherever it's pointed.
        /// </summary>
        internal static DBADashSource CreateSource(string encryptedConnectionString, SourceConnectionOptions options)
        {
            var connectionString = MessageEncryption.Decrypt(encryptedConnectionString);
            var src = options.CreateSource(connectionString);
            if (src.SourceConnection.Type != ConnectionType.SQL)
            {
                throw new ArgumentException("Only SQL Server connections can be added from the GUI.");
            }
            // A protected value (e.g. copied from a config file) the service can't decrypt would leave the connection with a
            // blank password - testing or collecting it would then be failed logins, which can lock out the login
            if (src.SourceConnection.SecretError != null)
            {
                throw new ArgumentException("The password in the connection string can't be decrypted by the service.  Enter the password itself rather than an encrypted value from a config file.");
            }
            return src;
        }

        /// <summary>Throws if <paramref name="options"/> can't be used for a connection added from the GUI.</summary>
        internal static void ValidateOptions(SourceConnectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            if (options.CustomCollections is { Count: > 0 })
            {
                throw new ArgumentException("Custom collections can't be added from the GUI.  Use the service configuration tool.");
            }

            var error = options.Validate();
            if (error != null)
            {
                throw new ArgumentException(error);
            }
        }

        /// <summary>
        /// Throws unless <paramref name="connectionIDs"/> are all given.  A blank ConnectionID would match a source saved
        /// without one (they're generated at runtime in older configs), changing an unrelated connection.
        /// </summary>
        internal static void ValidateConnectionIDs(IReadOnlyCollection<string> connectionIDs)
        {
            ValidateCount(connectionIDs?.Count ?? 0);
            if (connectionIDs!.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Every connection must have a ConnectionID.");
            }
        }

        internal static void ValidateCount(int count)
        {
            if (count == 0)
            {
                throw new ArgumentException("No connections were requested.");
            }

            if (count > MaxConnectionsPerMessage)
            {
                throw new ArgumentException($"At most {MaxConnectionsPerMessage} connections can be changed at once.");
            }
        }

        /// <summary>
        /// The service's config as saved in ServiceConfig.json.  Not the running config, which includes sources the
        /// service discovered at runtime (Azure DBs, read replicas) that mustn't be saved.  Caller holds
        /// <see cref="CollectionScheduleUpdateMessage.ConfigFileLock"/>.  Throws if a SQL source is saved without a
        /// ConnectionID (<see cref="ThrowIfMissingConnectionIDs"/>).
        /// </summary>
        internal static CollectionConfig LoadFileConfig()
        {
            var fileConfig = BasicConfig.Load<CollectionConfig>();
            ThrowIfMissingConnectionIDs(fileConfig);
            return fileConfig;
        }

        /// <summary>
        /// Throws if a SQL source in the saved config has no ConnectionID.  Saved and running sources are matched on
        /// ConnectionID only - older configs without one have it generated at runtime, so they can't be matched reliably.
        /// </summary>
        internal static void ThrowIfMissingConnectionIDs(CollectionConfig fileConfig)
        {
            var missing = fileConfig.SourceConnections
                .Where(s => s.SourceConnection?.Type == ConnectionType.SQL && string.IsNullOrEmpty(s.ConnectionID))
                .Select(s => s.SourceConnection.ConnectionForPrint)
                .ToList();
            if (missing.Count == 0) return;
            const int maxListed = 5;
            var list = string.Join(", ", missing.Take(maxListed)) + (missing.Count > maxListed ? $" and {missing.Count - maxListed} more" : "");
            throw new InvalidOperationException(
                $"{missing.Count} source connection(s) in the service config don't have a ConnectionID: {list}.  Changing connections from the GUI requires every connection to have a ConnectionID.  " +
                "Run \"DBADashConfig -a PopulateConnectionID\" on the service host, then restart the service.");
        }

        /// <summary>
        /// ConnectionIDs of the sources in the saved config, for telling them apart from the sources the service discovered
        /// from them (<see cref="CollectionScheduleUpdateMessage.GetDerivedSources"/>).
        /// </summary>
        internal static HashSet<string> GetConfiguredConnectionIDs(CollectionConfig fileConfig) =>
            fileConfig.SourceConnections
                .Where(s => !string.IsNullOrEmpty(s.ConnectionID))
                .Select(s => s.ConnectionID)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Drops the DBA Dash extended events sessions each source no longer uses after its options changed from Before
        /// to its current options (<see cref="ConnectionOptionsUpdateMessage.GetSessionsToDrop"/>), adding what was
        /// dropped to its result and anything that failed to <paramref name="warnings"/>.
        /// </summary>
        /// <param name="replacedSources">Sources replaced by the changed ones (e.g. with a different connection string), whose collections may still be running.</param>
        internal static async Task DropRedundantSessionsAsync(CollectionConfig cfg, IReadOnlyList<(DBADashSource Source, SourceConnectionOptions Before)> changed,
            List<ConnectionChangeResult> results, List<string> warnings, CancellationToken cancellationToken,
            IReadOnlyCollection<DBADashSource> replacedSources = null)
        {
            // Only those where a session might need dropping, whichever platform the instance turns out to be
            var sqlSources = changed.Where(c => c.Source.SourceConnection.Type == ConnectionType.SQL)
                .Where(c =>
                {
                    var after = SourceConnectionOptions.FromSource(c.Source);
                    return ConnectionOptionsUpdateMessage.GetSessionsToDrop(c.Before, after, false, false).Count > 0
                           || ConnectionOptionsUpdateMessage.GetSessionsToDrop(c.Before, after, true, false).Count > 0;
                })
                .ToList();
            if (sqlSources.Count == 0) return;

            // A collection that started with the old configuration would recreate a session dropped under it
            if (!await CollectionActivity.WaitForIdleAsync(sqlSources.Select(c => c.Source).Concat(replacedSources ?? Array.Empty<DBADashSource>()).ToList(),
                    RunningCollectionWait, cancellationToken))
            {
                warnings.Add("A collection that started before the change was still running, so an extended events session it uses might be recreated after it was dropped.  It will be removed when the service stops.");
            }

            var outcomes = await ForEachThrottledAsync(sqlSources, async c =>
            {
                try
                {
                    // Only connects when a session might need dropping, whichever platform the instance turns out to be
                    var after = SourceConnectionOptions.FromSource(c.Source);
                    if (ConnectionOptionsUpdateMessage.GetSessionsToDrop(c.Before, after, false, false).Count == 0
                        && ConnectionOptionsUpdateMessage.GetSessionsToDrop(c.Before, after, true, false).Count == 0)
                    {
                        return (c.Source, Dropped: new List<string>(), Error: (string)null);
                    }
                    var info = c.Source.SourceConnection.ConnectionInfo;
                    var sessions = ConnectionOptionsUpdateMessage.GetSessionsToDrop(c.Before, SourceConnectionOptions.FromSource(c.Source),
                        info.IsAzureDB, info.EngineEdition == Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlManagedInstance);
                    if (sessions.Count == 0)
                    {
                        return (c.Source, Dropped: new List<string>(), Error: (string)null);
                    }
                    var collector = await DBCollector.CreateAsync(c.Source, cfg.ServiceName);
                    return (c.Source, Dropped: await collector.DropEventSessionsAsync(sessions, cancellationToken), Error: (string)null);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "Error dropping extended events sessions no longer used by {connection}", c.Source.SourceConnection.ConnectionForPrint);
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
                var result = results.FirstOrDefault(r => string.Equals(r.ConnectionID, src.ConnectionID, StringComparison.InvariantCultureIgnoreCase))
                             // A discovered database or replica that followed the connection it was found from
                             ?? results.FirstOrDefault(r => r.ConnectionID != null && src.ConnectionID?.StartsWith(r.ConnectionID.Replace("|master", "|"), StringComparison.InvariantCultureIgnoreCase) == true);
                if (result == null) continue;
                var detail = $"Dropped extended events sessions on {src.ConnectionID}: {string.Join(", ", dropped)}";
                result.Detail = string.IsNullOrEmpty(result.Detail) ? detail : result.Detail + ".  " + detail;
            }
        }

        /// <summary>How long to wait for collections that started before a change before dropping the sessions they use</summary>
        internal static readonly TimeSpan RunningCollectionWait = TimeSpan.FromMinutes(2);

        /// <summary>Runs <paramref name="action"/> for each item, a few at a time, keeping the order of the results.</summary>
        internal static async Task<List<TResult>> ForEachThrottledAsync<T, TResult>(IReadOnlyList<T> items, Func<T, Task<TResult>> action, CancellationToken cancellationToken)
        {
            using var throttle = new SemaphoreSlim(MaxConcurrentChecks);
            var tasks = items.Select(async item =>
            {
                await throttle.WaitAsync(cancellationToken);
                try
                {
                    return await action(item);
                }
                finally
                {
                    throttle.Release();
                }
            });
            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>A reply DataSet carrying <paramref name="payload"/> as JSON, plus any <paramref name="warnings"/>.</summary>
        public static DataSet ToDataSet(object payload, IReadOnlyCollection<string> warnings = null)
        {
            var ds = MessageWarnings.ToDataSet(warnings) ?? new DataSet();
            var dt = new DataTable(PayloadTableName);
            dt.Columns.Add("Json", typeof(string));
            dt.Rows.Add(JsonConvert.SerializeObject(payload));
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>The payload of a reply from <see cref="ToDataSet"/> - default if there isn't one.</summary>
        public static T FromDataSet<T>(DataSet ds) =>
            ds?.Tables[PayloadTableName] is { Rows.Count: > 0 } dt
                ? JsonConvert.DeserializeObject<T>((string)dt.Rows[0]["Json"])
                : default;
    }

    /// <summary>What happened to a connection a <see cref="ConnectionAddMessage"/> or <see cref="ConnectionRemoveMessage"/> asked for.</summary>
    public class ConnectionChangeResult
    {
        public string ConnectionID { get; set; }
        public string ConnectionForPrint { get; set; }

        /// <summary>Added, Updated, Removed, Skipped or Failed</summary>
        public string Outcome { get; set; }

        /// <summary>Why it was skipped or failed, or anything else worth knowing</summary>
        public string Detail { get; set; }

        public const string Added = "Added";
        public const string Updated = "Updated";
        public const string Removed = "Removed";
        public const string Skipped = "Skipped";
        public const string Failed = "Failed";

        public bool IsChange => Outcome is Added or Updated or Removed;
    }
}
