using DBADashService;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    /// <summary>
    /// Adds new source connections to a <see cref="CollectionConfig"/> in two steps, so the person adding them can decide
    /// what happens to each one before anything is changed: <see cref="CheckAsync"/> connects to the instance and works out
    /// what adding it means (its ConnectionID, whether it's already monitored...), then <see cref="Apply"/> adds it with
    /// the <see cref="SourceConnectionDecisions"/> made.  Used by the service configuration tool directly, and by the
    /// service for connections added from the GUI - where the check runs on the service, as the service account, because
    /// the GUI might not be able to reach the instance at all.
    /// </summary>
    public static class SourceConnectionAdder
    {
        /// <summary>Checks <paramref name="src"/> before it's added to <paramref name="cfg"/>.  Sets its ConnectionID.</summary>
        /// <param name="validate">Connect to the instance.  False to skip the connection - the ConnectionID is then taken from the connection string.</param>
        public static async Task<SourceConnectionCheck> CheckAsync(CollectionConfig cfg, DBADashSource src, bool validate = true)
        {
            var check = new SourceConnectionCheck
            {
                ConnectionForPrint = src.SourceConnection.ConnectionForPrint,
                ConnectionType = src.SourceConnection.Type
            };
            if (src.SourceConnection.Type == ConnectionType.Invalid)
            {
                check.IsInvalid = true;
                check.ValidationError = "Invalid connection string, directory or S3 path";
                return check;
            }

            if (src.SourceConnection.Type != ConnectionType.SQL && cfg.DestinationConnection.Type != ConnectionType.SQL)
            {
                check.IsInvalid = true;
                check.ValidationError = "Invalid source and destination connection combination.  One of these should be a SQL connection string";
                return check;
            }

            if (validate)
            {
                try
                {
                    await Task.Run(src.SourceConnection.Validate);
                    if (src.SourceConnection.Type == ConnectionType.SQL)
                    {
                        src.ConnectionID = await src.GetGeneratedConnectionIDAsync();
                        // The collector already applies the "|ReadOnly" suffix itself for Azure DB read-only replicas, so
                        // only add it here if it's not already present - otherwise the ConnectionID ends up as
                        // <server>|<db>|ReadOnly|ReadOnly
                        if (src.SourceConnection.ApplicationIntent() == ApplicationIntent.ReadOnly &&
                            !src.ConnectionID.EndsWith("|ReadOnly", StringComparison.InvariantCultureIgnoreCase))
                        {
                            src.ConnectionID += "|ReadOnly";
                        }

                        var info = src.SourceConnection.ConnectionInfo;
                        check.ServerName = info.ServerName;
                        check.ServerNameIsNull = string.IsNullOrEmpty(info.ServerName);
                        check.ProductVersion = info.ProductVersion;
                        check.Edition = info.EngineEdition.ToString();
                        check.IsAzureDB = info.IsAzureDB;
                        check.XESupported = info.IsXESupported;
                        check.LoginName = await GetLoginNameAsync(src.SourceConnection.ConnectionString);
                        check.SlowQueryWillBeDisabled = !info.IsXESupported && src.SlowQueryThresholdMs >= 0;
                    }

                    check.Validated = true;
                }
                catch (Exception ex)
                {
                    check.ValidationError = ex.Message;
                }
            }

            src.SetConnectionIDFromBuilderIfNotSet();
            check.ConnectionID = src.ConnectionID;

            await RefreshExistingAsync(cfg, src, check);
            check.DeadlockScheduleMissing = src.IsDeadlockCollectionEnabled &&
                                            !IsDeadlockCollectionScheduled(cfg, src, check.ExistingSource?.CollectionSchedules ?? src.CollectionSchedules);
            return check;
        }

        /// <summary>
        /// Finds the source <paramref name="src"/> matches in <paramref name="cfg"/> as it is now, updating
        /// <paramref name="check"/>.  Called again before each connection of a batch is applied: two entries for the same
        /// instance (e.g. its name and a connection string) both look new when they're checked, so the second has to see
        /// the first once it's been added.
        /// </summary>
        public static async Task RefreshExistingAsync(CollectionConfig cfg, DBADashSource src, SourceConnectionCheck check)
        {
            var existing = await cfg.FindSourceConnectionAsync(src.SourceConnection.ConnectionString, src.ConnectionID);
            check.ExistingSource = existing;
            check.ExistingConnectionID = existing?.ConnectionID;
            check.DiscoveredVia = null;
            if (existing == null)
            {
                check.Existing = ExistingConnectionStates.None;
            }
            // The match isn't necessarily a configured connection.  With "Scan for Azure DBs" enabled,
            // FindSourceConnectionAsync also resolves a database that is only monitored implicitly through the master
            // connection's scan - that source doesn't exist in the config, so there is nothing to update or replace.
            else if (cfg.SourceConnections.Contains(existing))
            {
                check.Existing = ExistingConnectionStates.Configured;
            }
            else
            {
                check.Existing = ExistingConnectionStates.Discovered;
                check.DiscoveredVia = src.SourceConnection.ApplicationIntent() == ApplicationIntent.ReadOnly
                    ? "Monitor read replicas"
                    : "Scan for AzureDBs on service start";
            }
        }

        /// <summary>
        /// Adds <paramref name="src"/> to <paramref name="cfg"/> as decided, after <see cref="CheckAsync"/> (or
        /// <see cref="RefreshExistingAsync"/>) with the same config.  An existing connection that's updated is replaced by
        /// a copy of it with the new connection string and options (<see cref="SourceConnectionOptions"/>), so its
        /// ConnectionID, schedules and every setting that isn't an option are kept - and its custom collections too, when
        /// <paramref name="src"/> doesn't specify any (null).  The result's Source is the source added.
        /// </summary>
        public static SourceConnectionAddResult Apply(CollectionConfig cfg, DBADashSource src, SourceConnectionCheck check,
            SourceConnectionDecisions decisions)
        {
            if (check.IsInvalid)
            {
                return SourceConnectionAddResult.Skip(check.ValidationError);
            }

            if (!check.Validated && !string.IsNullOrEmpty(check.ValidationError) && !decisions.AddUnvalidated)
            {
                return SourceConnectionAddResult.Skip("Couldn't connect: " + check.ValidationError);
            }

            if (check.SlowQueryWillBeDisabled)
            {
                src.SlowQueryThresholdMs = -1;
                src.PersistXESessions = false;
            }

            var outcome = SourceConnectionAddOutcomes.Added;
            var added = src;
            var existing = check.ExistingSource;
            if (existing != null)
            {
                if (check.Existing == ExistingConnectionStates.Configured)
                {
                    if (!decisions.UpdateExisting)
                    {
                        return SourceConnectionAddResult.Skip("Already configured");
                    }

                    cfg.SourceConnections.Remove(existing);
                    added = existing.DeepCopy();
                    added.ConnectionString = src.ConnectionString;
                    // Saved without one, it's generated at runtime - as the check just did
                    if (string.IsNullOrEmpty(added.ConnectionID)) added.ConnectionID = src.ConnectionID;
                    SourceConnectionOptions.FromSource(src).ApplyTo(added, includeCustomCollections: src.CustomCollections != null);
                    outcome = SourceConnectionAddOutcomes.Updated;
                }
                else if (!decisions.AddDiscovered)
                {
                    return SourceConnectionAddResult.Skip($"Already monitored via \"{check.DiscoveredVia}\"");
                }
                else
                {
                    // Configured explicitly with the schedule it's collected on now
                    src.CollectionSchedules = existing.CollectionSchedules;
                }
            }

            //  Choosing a session says which session to read, not when to read it: the Deadlocks collection is disabled in
            //  the default schedule, so unless a schedule has been set the connection would be configured for deadlocks
            //  and still collect nothing.
            if (decisions.UseDefaultDeadlockSchedule && added.IsDeadlockCollectionEnabled && !IsDeadlockCollectionScheduled(cfg, added, added.CollectionSchedules))
            {
                SetDefaultDeadlockSchedule(added);
            }

            cfg.SourceConnections.Add(added);
            return new SourceConnectionAddResult(outcome, null, added);
        }

        /// <summary>
        /// True if the Deadlocks collection has a schedule that would actually run it, taking the connection's own schedule
        /// overrides into account.  The collection is disabled in the default schedule, so setting a session name alone
        /// leaves it collecting nothing.
        /// </summary>
        public static bool IsDeadlockCollectionScheduled(CollectionConfig cfg, DBADashSource src) =>
            IsDeadlockCollectionScheduled(cfg, src, src.CollectionSchedules);

        private static bool IsDeadlockCollectionScheduled(CollectionConfig cfg, DBADashSource src, CollectionSchedules srcSchedules)
        {
            var schedules = srcSchedules is { Count: > 0 }
                ? CollectionSchedules.Combine(cfg.GetSchedules(), srcSchedules)
                : cfg.GetSchedules();
            return schedules.TryGetValue(CollectionType.Deadlocks, out var schedule) &&
                   !string.IsNullOrWhiteSpace(schedule?.Schedule);
        }

        public static void SetDefaultDeadlockSchedule(DBADashSource src)
        {
            src.CollectionSchedules ??= new CollectionSchedules();
            src.CollectionSchedules[CollectionType.Deadlocks] = new CollectionSchedule
            {
                Schedule = CollectionSchedules.DeadlocksSchedule,
                RunOnServiceStart = false
            };
        }

        /// <summary>True if the instance is marked deleted in the repository - it won't show in the GUI until it's restored.</summary>
        public static async Task<bool> IsInstanceDeletedAsync(string connectionID, string repositoryConnectionString)
        {
            await using var cn = new SqlConnection(repositoryConnectionString);
            await using var cmd = new SqlCommand("dbo.Instances_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("IsActive", 0);
            cmd.Parameters.AddWithValue("ConnectionID", connectionID);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync(); // Instance is soft deleted (IsActive=0) if a row is returned
        }

        private static async Task<string> GetLoginNameAsync(string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("SELECT SUSER_SNAME()", cn);
            await cn.OpenAsync();
            return (await cmd.ExecuteScalarAsync()) as string;
        }
    }

    public enum ExistingConnectionStates
    {
        /// <summary>Not monitored</summary>
        None,

        /// <summary>A source connection in the config</summary>
        Configured,

        /// <summary>Monitored without being in the config - found by the Azure DB or read replica scan</summary>
        Discovered
    }

    /// <summary>What <see cref="SourceConnectionAdder.CheckAsync"/> found out about a connection before it's added.</summary>
    public class SourceConnectionCheck
    {
        public string ConnectionForPrint { get; set; }
        public ConnectionType ConnectionType { get; set; }

        /// <summary>Can't be added at all - see <see cref="ValidationError"/></summary>
        public bool IsInvalid { get; set; }

        /// <summary>Connected successfully</summary>
        public bool Validated { get; set; }

        /// <summary>Why it couldn't connect, or why it's invalid</summary>
        public string ValidationError { get; set; }

        public string ConnectionID { get; set; }
        public string ServerName { get; set; }

        /// <summary>@@SERVERNAME returned NULL - consider fixing it with sp_addserver</summary>
        public bool ServerNameIsNull { get; set; }

        public string ProductVersion { get; set; }
        public string Edition { get; set; }
        public bool IsAzureDB { get; set; }
        public bool XESupported { get; set; }

        /// <summary>The login the connection was made as (SUSER_SNAME()) - with Windows authentication, the account checking.</summary>
        public string LoginName { get; set; }

        /// <summary>Slow query capture needs an extended events session, which the instance doesn't support</summary>
        public bool SlowQueryWillBeDisabled { get; set; }

        public ExistingConnectionStates Existing { get; set; }
        public string ExistingConnectionID { get; set; }

        /// <summary>The option that monitors a <see cref="ExistingConnectionStates.Discovered"/> connection</summary>
        public string DiscoveredVia { get; set; }

        /// <summary>Deadlock collection is configured but the Deadlocks collection has no schedule, so nothing would be collected</summary>
        public bool DeadlockScheduleMissing { get; set; }

        /// <summary>Marked deleted in the repository.  Not set by <see cref="SourceConnectionAdder.CheckAsync"/> - checked by the caller, which knows the repository.</summary>
        public bool IsDeletedInRepository { get; set; }

        /// <summary>
        /// Another service already collects the instance, so adding it would collect it twice.  Like
        /// <see cref="IsDeletedInRepository"/>, set by the caller from the repository.
        /// </summary>
        public string CollectedByOtherService { get; set; }

        /// <summary>The source matched by <see cref="Existing"/> - in the config that was checked</summary>
        [JsonIgnore]
        public DBADashSource ExistingSource { get; set; }
    }

    /// <summary>How each connection is added - the questions <see cref="SourceConnectionCheck"/> raises, answered.</summary>
    public class SourceConnectionDecisions
    {
        /// <summary>Add it even though it couldn't be validated</summary>
        public bool AddUnvalidated { get; set; }

        /// <summary>Replace a connection that's already configured</summary>
        public bool UpdateExisting { get; set; }

        /// <summary>Add a connection that's already monitored via the Azure DB or read replica scan, to configure it explicitly</summary>
        public bool AddDiscovered { get; set; }

        /// <summary>Give the connection the default deadlock schedule if deadlock collection is configured without one</summary>
        public bool UseDefaultDeadlockSchedule { get; set; }
    }

    public enum SourceConnectionAddOutcomes
    {
        Added,
        Updated,
        Skipped
    }

    /// <param name="Source">The source added to the config - for an update, the copy of the existing source that replaced it.  Null when skipped.</param>
    public record SourceConnectionAddResult(SourceConnectionAddOutcomes Outcome, string Reason, DBADashSource Source = null)
    {
        public static SourceConnectionAddResult Skip(string reason) => new(SourceConnectionAddOutcomes.Skipped, reason);
    }
}
