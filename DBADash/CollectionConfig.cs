using DBADash.InstanceMetadata;
using DBADashService;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class CollectionConfig : BasicConfig
    {
        public int ServiceThreads
        {
            get;
            set { field = value; _cachedThreadCount = null; }
        } = -1;

        public int SchedulerThreads { get; set; } = -1;
        public int? ChannelCapacity { get; set; }

        private int? _cachedThreadCount;
        private string _secretKey;
        private bool wasEncryptionPerformed;
        public SchemaSnapshotDBOptions SchemaSnapshotOptions = null;
        public bool ScanForAzureDBs { get; set; } = true;
        public int ScanForAzureDBsInterval { get; set; } = 3600;

        /// <summary>
        /// When enabled, DBA Dash probes each Azure SQL Database source for a readable secondary (read replica) by
        /// connecting with ApplicationIntent=ReadOnly and checking DATABASEPROPERTYEX(DB_NAME(),'Updateability').
        /// Only databases that route to a READ_ONLY replica (Business Critical / Premium / Hyperscale tiers) are
        /// added; those that route back to the primary (READ_WRITE) are skipped.  Discovered replicas are added as
        /// distinct sources with a "|ReadOnly" ConnectionID suffix.  Re-scanned on the Azure DB scan interval.
        /// </summary>
        public bool MonitorReadReplicas { get; set; }

        /// <summary>
        /// How many read replica probes run concurrently.  Each probe is a connection that spends almost all its time
        /// waiting, so this is about capping load on the Azure gateway and the thread pool, not about CPU.
        /// </summary>
        private const int ReadReplicaScanParallelism = 8;

        /// <summary>
        /// How often (seconds) the set of offline instances is reported when nothing has changed.  A change -
        /// an instance going offline, coming back, or failing again after it recovered - is always reported
        /// immediately, so this only controls the repeat.  The repeat is what recovers the state if a report
        /// is lost, and what keeps LastFail/FailCount current for an instance that's still offline, so those
        /// values lag by up to this interval.  Every destination is reported to: a SQL destination runs
        /// OfflineInstances_Add, a folder or S3 destination gets a file for the importing service to process.
        /// 0 = report changes only (not recommended - a lost report would leave an instance marked offline
        /// until it's reported again).
        /// </summary>
        public int OfflineInstancesReportInterval { get; set; } = 60;

        public string ServiceName { get; set; } = "DBADashService";

        public bool AutoUpdateDatabase { get; set; } = true;
        public bool LogInternalPerformanceCounters = false;
        public int? IdentityCollectionThreshold = null;
        public CollectionSchedules CollectionSchedules;
        public Dictionary<string, CustomCollection> CustomCollections { get; set; } = new();

        /// <summary>
        /// OS-level (perfmon) performance counters collected from the host via WMI.  Separate from
        /// the DMV performance counters (sys.dm_os_performance_counters).  Empty = collect nothing
        /// (no auto-defaults at runtime; the curated <see cref="PerfmonCounter.DefaultCounters"/> are
        /// only offered as a starting point via the "Defaults" button in the config UI).
        /// </summary>
        public List<PerfmonCounter> PerfmonCounters { get; set; } = new();

        public string SummaryRefreshCron { get; set; }

        public string UpgradeCheckCron { get; set; }

        public bool UpgradeAllowPreRelease { get; set; }

        public int? ImportCommandTimeout { get; set; }
        public int? PurgeDataCommandTimeout { get; set; }
        public int? AddPartitionsCommandTimeout { get; set; }

        public bool? UseQueueBasedScheduling { get; set; }

        public const int DefaultImportCommandTimeout = 60;
        public const int DefaultPurgeDataCommandTimeout = 1200;
        public const int DefaultAddPartitionsCommandTimeout = 300;
        public const int DefaultFailedLoginsBackfillMinutes = 1440;
        private const bool DefaultUseQueueBasedScheduling = true;
        private const double DefaultLowPriorityQueueMaxThreadPercentage = 0.25;

        public double? LowPriorityQueueMaxThreadPercentage
        {
            get => field;
            set
            {
                if (value == null || (value > 0 && value <= 1))
                {
                    field = value;
                }
                else
                {
                    throw new ArgumentException("LowPriorityQueueMaxThreadPercentage must be null (to use the default) or greater than 0 and less than or equal to 1.");
                }
            }
        }

        public double GetLowPriorityQueueMaxThreadPercentage() => LowPriorityQueueMaxThreadPercentage ?? DefaultLowPriorityQueueMaxThreadPercentage;

        public bool EnableMessaging { get; set; } = true;

        public bool AllowPlanForcing { get; set; }

        public bool AllowKillSession { get; set; }

        /// <summary>
        /// Collection schedules can be changed from the GUI when enabled.  The service applies the change to
        /// ServiceConfig.json (taking a backup first) and reschedules its collections without a restart.  Also allows
        /// collections to be paused for a period.  When false, <see cref="Messaging.CollectionScheduleUpdateMessage"/> and
        /// <see cref="Messaging.CollectionPauseMessage"/> are rejected.
        /// </summary>
        public bool AllowScheduleChanges { get; set; }

        /// <summary>
        /// Source connections can be added and removed from the GUI when enabled
        /// (<see cref="Messaging.ConnectionTestMessage"/>, <see cref="Messaging.ConnectionAddMessage"/> and
        /// <see cref="Messaging.ConnectionRemoveMessage"/>).  Connections are checked by the service, as the service
        /// account, and written to ServiceConfig.json.  Also allows schedule changes - see <see cref="IsScheduleChangesAllowed"/>.
        /// </summary>
        public bool AllowConnectionChanges { get; set; }

        /// <summary>
        /// Schedule changes and pauses are accepted - <see cref="AllowScheduleChanges"/>, or <see cref="AllowConnectionChanges"/>
        /// which goes further: someone who can add a connection can set its schedule.
        /// </summary>
        [JsonIgnore]
        public bool IsScheduleChangesAllowed => AllowScheduleChanges || AllowConnectionChanges;

        /// <summary>
        /// Set by the service so that a schedule change made via messaging can take effect without a restart.  Rebuilds
        /// the collection jobs from the current config.  Null outside the service (e.g. the config tools).
        /// </summary>
        [JsonIgnore]
        public Func<Task> RescheduleCollectionsAsync { get; set; }

        /// <summary>
        /// Set by the service so that sources added via messaging start collecting without a restart - schedules their
        /// collections and runs the service start collections.  The sources have already been added to
        /// <see cref="SourceConnections"/>.  Null outside the service.
        /// </summary>
        [JsonIgnore]
        public Func<IReadOnlyList<DBADashSource>, Task> ScheduleNewSourcesAsync { get; set; }

        /// <summary>
        /// Set by the service once its collections are scheduled at startup - sources added or removed before then could be
        /// missed by, or scheduled twice with, the startup scheduling.  False outside the service.
        /// </summary>
        [JsonIgnore]
        public bool AcceptsSourceChanges { get; set; }

        /// <summary>
        /// Ad-hoc extended-events tracing must be explicitly enabled on the service (mirrors
        /// <see cref="AllowKillSession"/>).  When false, <see cref="Messaging.XETraceMessage"/> is rejected.
        /// </summary>
        public bool AllowAdhocXE { get; set; }

        /// <summary>
        /// Optional directory <b>on the monitored SQL Server</b> for ad-hoc XE event_file captures.  When empty the
        /// instance's SQL Server LOG directory is resolved at trace start.  Not used for the ring_buffer target.
        /// </summary>
        public string AdhocXEDirectory { get; set; }

        /// <summary>
        /// Hard cap (seconds) on ad-hoc XE trace duration; a request longer than this is clamped down.  Traces longer
        /// than <see cref="Messaging.XETraceHeartbeat.TimeoutSeconds"/> are kept alive by GUI heartbeats and
        /// stop themselves if the client goes away, so a large cap is safe (an abandoned trace won't run to the cap).
        /// </summary>
        public int AdhocXEMaxDurationSeconds { get; set; } = DefaultAdhocXEMaxDurationSeconds;

        public const int DefaultAdhocXEMaxDurationSeconds = 3600;

        /// <summary>
        /// Comma-separated allow/deny list of session names the Manage-XE feature may <b>start/stop</b> on a monitored
        /// instance (see <see cref="XE.XESessionFilter"/> for the syntax; <c>*</c> = all, <c>-name</c> = deny, deny
        /// always wins).  Blank disables start/stop entirely.  Separate from <see cref="WatchXESessions"/> so a service
        /// can permit watching a session (read-only) without permitting it to be stopped - e.g. protect
        /// <c>system_health</c> from being disabled while still allowing it to be viewed.
        /// </summary>
        public string ManageXESessions { get; set; }

        /// <summary>
        /// Comma-separated allow/deny list of session names the Manage-XE feature may <b>watch</b> (stream events from,
        /// read-only) on a monitored instance.  Same syntax as <see cref="ManageXESessions"/>.  Blank disables watching.
        /// Kept separate because watching can expose captured event data (query text, parameters) even though it never
        /// changes the session.
        /// </summary>
        public string WatchXESessions { get; set; }

        /// <summary>Default for <see cref="ManageXESessions"/>: everything except the built-in health/telemetry sessions.</summary>
        public const string DefaultManageXESessions = "*,-system_health,-AlwaysOn_health,-telemetry_xevents";

        /// <summary>Default for <see cref="WatchXESessions"/>: any session may be watched.</summary>
        public const string DefaultWatchXESessions = "*";

        /// <summary>
        /// True when start/stop of existing XE sessions is enabled (<see cref="ManageXESessions"/> permits at least one
        /// session).  Gates <see cref="Messaging.XESessionControlMessage"/>; the per-session check is
        /// <see cref="CanManageXESession"/>.  Independent of watching.
        /// </summary>
        [JsonIgnore]
        public bool AllowManageXE => !string.IsNullOrWhiteSpace(ManageXESessions);

        /// <summary>
        /// True when watching/viewing existing XE sessions is enabled (<see cref="WatchXESessions"/> permits at least one
        /// session).  Gates the read-only operations (list / script / watch / view captured data) - viewing is treated
        /// as the same capability as watching.  The per-session check is <see cref="CanWatchXESession"/>.  Independent
        /// of start/stop and of ad-hoc tracing.
        /// </summary>
        [JsonIgnore]
        public bool AllowWatchXE => !string.IsNullOrWhiteSpace(WatchXESessions);

        /// <summary>True if <paramref name="sessionName"/> may be started/stopped per <see cref="ManageXESessions"/>.</summary>
        public bool CanManageXESession(string sessionName) =>
            XE.XESessionFilter.Parse(ManageXESessions).IsAllowed(sessionName);

        /// <summary>
        /// True if <paramref name="sessionName"/> may be watched/viewed per <see cref="WatchXESessions"/>.  Governed
        /// solely by the watch list (deny always wins) - independent of Manage-XE and ad-hoc tracing.
        /// </summary>
        public bool CanWatchXESession(string sessionName) =>
            XE.XESessionFilter.Parse(WatchXESessions).IsAllowed(sessionName);

        public string AllowedScripts { get; set; }

        public string AllowedCustomProcs { get; set; }

        public string AllowedJobs { get; set; }

        public string ServiceSQSQueueUrl { get; set; }

        public bool ProcessAlerts { get; set; } = true;

        public int? AlertProcessingFrequencySeconds { get; set; }

        public int? AlertProcessingStartupDelaySeconds { get; set; }

        public int? FailedLoginsBackfillMinutes { get; set; }

        /// <summary>
        /// Size in KB of the ring buffer on the deadlock extended events session DBA Dash creates.  Null uses
        /// <see cref="DefaultDeadlockXERingBufferKB"/>.
        ///
        /// <para>Only Azure SQL Database and Azure SQL Managed Instance are affected.  They are the platforms
        /// where the deadlock session reads a ring buffer, because an event_file target there writes to blob
        /// storage; everywhere else the session writes to an event file and the resume cursor bounds the read
        /// instead.</para>
        ///
        /// <para>Bigger buys a longer outage to bridge - a deadlock stays in the buffer until a collection
        /// reads it or it is pushed out - and costs read time, because a ring buffer read costs what the
        /// buffer holds rather than what is new in it.  Service level rather than per connection: it is a
        /// tuning decision about how the service reads, not a fact about one database.</para>
        ///
        /// <para>Applied to a session DBA Dash already created as well as to a new one - see the deadlock
        /// session script - so changing it takes effect on the next collection rather than only on a fresh
        /// deployment.  Resizing discards what the buffer holds, since a target's options cannot be altered
        /// in place, so it happens once when the value changes and not on every run.</para>
        /// </summary>
        public int? DeadlockXERingBufferKB { get; set; }

        /// <summary>
        /// 1MB, which is the largest ring buffer SQL Server recommends: above it a target's target_data can
        /// be returned truncated, and truncated XML does not parse, so the whole read is lost rather than
        /// part of it.  A deadlock graph is a few KB, so this still holds hundreds of them.
        /// </summary>
        public const int DefaultDeadlockXERingBufferKB = 1024;

        /// <summary>Below this the buffer cannot hold a single large deadlock graph.</summary>
        public const int MinDeadlockXERingBufferKB = 64;

        /// <summary>
        /// Above <see cref="DefaultDeadlockXERingBufferKB"/> is allowed but is a deliberate choice against
        /// SQL Server's own advice, so the ceiling is the point at which truncation is close to certain
        /// rather than merely possible.  A truncated buffer is reported when it is read.
        /// </summary>
        public const int MaxDeadlockXERingBufferKB = 4096;

        /// <summary>The configured buffer size, defaulted and clamped to what the target can actually take.</summary>
        public int GetDeadlockXERingBufferKB() =>
            Math.Clamp(DeadlockXERingBufferKB ?? DefaultDeadlockXERingBufferKB,
                MinDeadlockXERingBufferKB, MaxDeadlockXERingBufferKB);

        /// <summary>
        /// Seconds the one-off system_health deadlock backfill may take before it stops and keeps what it has read.
        /// Null uses <see cref="DefaultDeadlockBackfillTimeLimitSeconds"/>; 0 means no limit.
        ///
        /// <para>Separate from the Deadlocks command timeout, which bounds every run: the backfill reads the whole
        /// system_health file set once per instance, so it can reasonably need far longer than a routine read.  What
        /// it costs is a collection worker - and one of that instance's concurrent collection slots - for that long,
        /// once per instance.  It runs in the low priority queue, whose concurrency cap stops a batch of newly enabled
        /// instances taking every worker; the limit bounds how long each one holds its worker and slot.</para>
        ///
        /// <para>Service level rather than per connection, like <see cref="DeadlockXERingBufferKB"/>: a tuning
        /// decision about how the service reads rather than a fact about one instance.</para>
        /// </summary>
        public int? DeadlockBackfillTimeLimitSeconds { get; set; }

        public const int DefaultDeadlockBackfillTimeLimitSeconds = 300;

        /// <summary>The configured backfill time limit, defaulted.  A negative value is treated as no limit, as 0 is.</summary>
        public int GetDeadlockBackfillTimeLimitSeconds() =>
            Math.Max(0, DeadlockBackfillTimeLimitSeconds ?? DefaultDeadlockBackfillTimeLimitSeconds);

        /// <summary>
        /// The Service Broker collection fails (with an error saying so) on an instance with more databases than this for it to visit -
        /// online, accessible, Service Broker enabled and not an availability group secondary.  Null uses
        /// <see cref="DefaultServiceBrokerMaxDatabases"/>; 0 means no limit.
        ///
        /// <para>Each database visited costs some catalog reads every collection.  The limit stops an instance with a very large number
        /// of broker enabled databases timing out on every run instead.  Service level rather than per connection: it only affects
        /// instances over it, and the collection can be disabled for one instance with a per connection schedule.</para>
        /// </summary>
        public int? ServiceBrokerMaxDatabases { get; set; }

        public const int DefaultServiceBrokerMaxDatabases = 500;

        /// <summary>The configured database limit, defaulted.  A negative value is treated as no limit, as 0 is.</summary>
        public int GetServiceBrokerMaxDatabases() =>
            Math.Max(0, ServiceBrokerMaxDatabases ?? DefaultServiceBrokerMaxDatabases);

        public const int DefaultAlertProcessingFrequencySeconds = 60;

        public const int DefaultAlertProcessingStartupDelaySeconds = 60;

        public HashSet<InstanceMetadataProviders.Providers> EnabledMetadataProviders { get; set; } = new();

        public bool IsUseQueueBasedScheduling() => UseQueueBasedScheduling ?? DefaultUseQueueBasedScheduling;

        public int GetThreadCount()
        {
            // Return cached value if already calculated
            if (_cachedThreadCount.HasValue)
            {
                return _cachedThreadCount.Value;
            }

            if (ServiceThreads > 0)
            {
                Log.Information("Threads {threadCount} (user)", ServiceThreads);
                _cachedThreadCount = ServiceThreads;
                return ServiceThreads;
            }

            const int minimumCalcThreads = 10; // Minimum threads we should calculate
            const int maximumCalcThreads = 100; // Maximum threads we should calculate
            const int sourceConnectionsAdditionalThreads = 5; // Additional threads to add on top of multiplier
            const double sourceConnectionsMultiplier = 0.5; // 1 thread for every 2 source connections
            const int maxThreadsPerProcessor = 4; // Max threads per processor

            var connectionCount = SourceConnections.Count;
            var processorCount = Environment.ProcessorCount;
            var connectionThreadCalc = Convert.ToInt32((connectionCount * sourceConnectionsMultiplier)) + sourceConnectionsAdditionalThreads; // Calculate thead count based on number of source connections
            var processorLimit = processorCount * maxThreadsPerProcessor; // Max of 4 threads per processor.  Threads will often be idle waiting for queries to complete
            var threads = Convert.ToInt32(Math.Min(processorLimit, connectionThreadCalc)); // Use the lower of the two calculations
            threads = int.Clamp(threads, minimumCalcThreads, maximumCalcThreads); // Clamp to minimum and maximum

            Log.Information(
                "Calculated thread count: {ThreadCount}. " +
                "Based on {ConnectionCount} connections ({ConnectionThreads} threads) " +
                "and {ProcessorCount} processors ({ProcessorLimit} max threads). " +
                "Clamped between {MinThreads}-{MaxThreads}",
                threads, connectionCount, connectionThreadCalc, processorCount, processorLimit, minimumCalcThreads, maximumCalcThreads);

            // Cache the calculated value
            _cachedThreadCount = threads;
            return threads;
        }

        /// <summary>
        /// Get the number of threads for Quartz scheduler.
        /// In traditional mode: these threads do all the work (scheduling + collection execution)
        /// In queue-based mode: these threads only do lightweight scheduling/enqueue operations
        /// </summary>
        public int GetSchedulerThreadCount()
        {
            // Traditional scheduling: scheduler threads do all the work, use ServiceThreads/calculated
            if (!IsUseQueueBasedScheduling())
            {
                var threads = ServiceThreads > 0 ? ServiceThreads : GetThreadCount();
                Log.Information("Scheduler thread count: {threads} (traditional scheduling - these threads do all work)", threads);
                return threads;
            }

            // Queue-based scheduling: use explicit SchedulerThreads if set
            if (SchedulerThreads > 0)
            {
                Log.Information("Scheduler thread count: {threads} (user-configured, queue-based scheduling)", SchedulerThreads);
                return SchedulerThreads;
            }

            // Queue-based scheduling default: 10 threads for lightweight scheduling/enqueue operations
            const int defaultSchedulerThreads = 10;
            Log.Information("Scheduler thread count: {schedulerThreads} (default for queue-based scheduling - lightweight scheduling/enqueue)",
                defaultSchedulerThreads);
            return defaultSchedulerThreads;
        }

        /// <summary>
        /// Get the number of worker threads for CollectionWorkQueue (only used in queue-based scheduling).
        /// These are the threads that actually execute collection work.
        /// </summary>
        public int GetWorkerThreadCount()
        {
            var threads = ServiceThreads > 0 ? ServiceThreads : GetThreadCount();
            Log.Information("Worker thread count: {threads} (queue-based scheduling - collection processing threads)", threads);
            return threads;
        }

        public CollectionSchedules GetSchedules()
        {
            if (CollectionSchedules == null)
            {
                return CollectionSchedules.DefaultSchedules;
            }
            else
            {
                return CollectionSchedules.CombineWithDefault();
            }
        }

        /// <summary>
        /// The schedule <paramref name="source"/> actually runs on: the service schedule (<paramref name="serviceSchedules"/>,
        /// defaulting to <see cref="GetSchedules"/>), then the source's own overrides, then any temporary schedules in
        /// <see cref="TemporaryScheduleStore"/>.  Always a new collection, so the caller can modify it.
        /// </summary>
        public CollectionSchedules GetEffectiveSchedules(DBADashSource source, CollectionSchedules serviceSchedules = null)
        {
            var schedules = CollectionSchedules.Combine(serviceSchedules ?? GetSchedules(), source.CollectionSchedules);
            var temporary = TemporaryScheduleStore.Current.GetOverrides(source.ConnectionID);
            return temporary == null ? schedules : CollectionSchedules.Combine(schedules, temporary);
        }

        /// <summary>
        /// The longest normal gap between runs of <paramref name="type"/> for <paramref name="source"/>, from the
        /// schedule it actually runs on - see <see cref="GetEffectiveSchedules"/>.  Null when the
        /// collection has no schedule, or one <see cref="CronParser"/> can't size.
        /// </summary>
        public TimeSpan? GetMaxScheduleInterval(DBADashSource source, CollectionType type)
        {
            var schedules = GetEffectiveSchedules(source);
            return schedules.TryGetValue(type, out var schedule)
                   && CronParser.TryGetMaxIntervalMinutes(schedule.Schedule, out var minutes)
                ? TimeSpan.FromMinutes(minutes)
                : null;
        }

        public List<DBADashSource> SourceConnections = new();

        public static CollectionConfig Deserialize(string json, string password = null)
        {
            return BasicConfig.Deserialize<CollectionConfig>(json, password);
        }

        public string AWSProfile { get; set; }

        public string AccessKey { get; set; }

        public string SecretKey
        {
            get => _secretKey;
            set
            {
                if (value == "")
                {
                    _secretKey = null;
                }
                else if (value == null || SecretProtection.IsProtected(value))
                {
                    _secretKey = value;
                }
                else
                {
                    _secretKey = SecretProtection.Protect(value);
                    wasEncryptionPerformed = true;
                }
            }
        }

        /// <exception cref="SecretUnprotectException">The secret key can't be decrypted on this machine</exception>
        public string GetSecretKey() => SecretProtection.Unprotect(_secretKey);

        /// <summary>The decrypted secret key, or false with the reason if it can't be decrypted on this machine</summary>
        private bool TryGetSecretKey(out string secretKey, out string error)
        {
            try
            {
                secretKey = GetSecretKey();
                error = null;
                return true;
            }
            catch (SecretUnprotectException ex)
            {
                secretKey = null;
                error = ex.Message;
                return false;
            }
        }

        protected override void ProtectSecrets(ConfigKey key, HashSet<string> seen)
        {
            base.ProtectSecrets(key, seen);
            SecondaryDestinationConnections.ForEach(d => d.ProtectSecret(key, seen));
            lock (SourceConnections)
            {
                SourceConnections.ForEach(s => s.SourceConnection?.ProtectSecret(key, seen));
            }
            if (!SecretProtection.IsProtectedWith(_secretKey, key) && TryGetSecretKey(out var secretKey, out _))
            {
                _secretKey = SecretProtection.Protect(secretKey, key);
            }
        }

        protected override bool SecretsNeedProtection(ConfigKey key)
        {
            lock (SourceConnections)
            {
                return base.SecretsNeedProtection(key)
                       || SecondaryDestinationConnections.Exists(d => d.SecretNeedsProtection(key))
                       || SourceConnections.Exists(s => s.SourceConnection?.SecretNeedsProtection(key) == true)
                       || (!SecretProtection.IsProtectedWith(_secretKey, key) && TryGetSecretKey(out _, out _));
            }
        }

        private static string SourceName(DBADashSource s) =>
            string.IsNullOrEmpty(s.ConnectionID) ? s.SourceConnection.ConnectionForPrint : s.ConnectionID;

        private static string Describe(DBADashSource s) => "Source " + SourceName(s);

        /// <summary>
        /// Remove sources and secondary destinations whose password can't be decrypted, so the running service doesn't
        /// keep trying to log in with a blank password (which can lock out the login).  For the running config only - the
        /// saved config keeps them so they can be re-entered.  Returns the connections removed.
        /// </summary>
        public List<string> RemoveConnectionsWithUnreadableSecrets()
        {
            var removed = SecondaryDestinationConnections.Where(d => d.SecretError != null)
                .Select(d => $"Destination {d.ConnectionForPrint}").ToList();
            SecondaryDestinationConnections.RemoveAll(d => d.SecretError != null);
            lock (SourceConnections)
            {
                removed.AddRange(SourceConnections.Where(s => s.SourceConnection?.SecretError != null).Select(Describe));
                SourceConnections.RemoveAll(s => s.SourceConnection?.SecretError != null);
            }
            return removed;
        }

        public override List<ConfigSecret> GetSecrets()
        {
            var list = base.GetSecrets();
            list.AddRange(SecondaryDestinationConnections.Where(d => d.HasPassword)
                .Select(d => ConfigSecret.ForConnection(ConfigSecret.SecretTypes.Destination, d.ConnectionForPrint, d)));
            lock (SourceConnections)
            {
                list.AddRange(SourceConnections.Where(s => s.SourceConnection?.HasPassword == true)
                    .Select(s => ConfigSecret.ForConnection(ConfigSecret.SecretTypes.Source, SourceName(s), s.SourceConnection)));
            }
            if (!string.IsNullOrEmpty(_secretKey))
            {
                TryGetSecretKey(out _, out var error);
                list.Add(new ConfigSecret(ConfigSecret.SecretTypes.AWSSecretKey, AccessKey, null, error, value => SecretKey = value));
            }
            return list;
        }

        /// <summary>
        /// A copy of the config encrypted with <paramref name="password"/>, to move DBA Dash to another machine (or keep as a
        /// backup) without re-entering passwords.  The config itself isn't changed.  Secrets that can't be decrypted here
        /// (<see cref="BasicConfig.GetUnreadableSecrets"/>) are copied as they are and still can't be decrypted.
        /// </summary>
        public string ExportEncrypted(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            // Serializing with a password changes the keys the config uses, so work on a copy - and leave new secrets
            // entered in this process protected the way they were
            var defaultKey = SecretProtection.DefaultKey;
            try
            {
                var copy = Deserialize(Serialize());
                copy.EncryptionOption = EncryptionOptions.Encrypt;
                return copy.Serialize(EncryptionOptions.Encrypt, password);
            }
            finally
            {
                SecretProtection.SetDefaultKey(defaultKey);
            }
        }

        /// <summary>
        /// The SQL connection passwords matching <paramref name="connectionID"/> (a source's ConnectionID or a destination's
        /// server|database) and/or <paramref name="userName"/>.  A user name can match many connections, but a ConnectionID
        /// must match one - a source's ConnectionID can be the same as a destination's name.
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="connectionID"/> matches more than one connection</exception>
        public List<ConfigSecret> FindConnectionSecrets(string connectionID, string userName)
        {
            var matches = GetSecrets()
                .Where(s => s.Connection != null
                            && (string.IsNullOrEmpty(connectionID) || string.Equals(s.Name, connectionID, StringComparison.OrdinalIgnoreCase))
                            && (string.IsNullOrEmpty(userName) || string.Equals(s.UserName, userName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (!string.IsNullOrEmpty(connectionID) && matches.Count > 1)
            {
                throw new InvalidOperationException($"{connectionID} matches more than one connection: {string.Join(", ", matches.Select(m => $"{m.TypeDescription} {m.Name} (user {m.UserName})"))}.  " +
                                                    "Specify the user name as well to say which, or change the password in the service config tool.  No passwords were changed.");
            }
            return matches;
        }

        /// <summary>
        /// A connection string that can't be parsed is kept as it is, so any password in it can't be found and removed.
        /// The message doesn't include the connection strings, as they might have a password in them.
        /// </summary>
        private void ThrowIfInvalidConnections()
        {
            static bool IsInvalid(DBADashConnection cn) => cn != null && !string.IsNullOrEmpty(cn.EncryptedConnectionString) && cn.Type == ConnectionType.Invalid;

            var invalid = new List<string>();
            if (IsInvalid(DestinationConnection)) invalid.Add("Destination");
            invalid.AddRange(SecondaryDestinationConnections.Select((d, i) => (d, i)).Where(x => IsInvalid(x.d)).Select(x => $"Secondary destination {x.i + 1}"));
            lock (SourceConnections)
            {
                invalid.AddRange(SourceConnections.Select((s, i) => (s, i)).Where(x => IsInvalid(x.s.SourceConnection))
                    .Select(x => string.IsNullOrEmpty(x.s.ConnectionID) ? $"Source connection {x.i + 1}" : $"Source {x.s.ConnectionID}"));
            }
            if (invalid.Count > 0)
            {
                throw new InvalidOperationException("The config can't be exported without secrets as these connection strings can't be parsed, so a password in them can't be removed.  Fix or remove them first: " + string.Join(", ", invalid));
            }
        }

        /// <summary>
        /// A copy of the config without its secrets, e.g. a backup that doesn't need protecting.  Each secret is replaced
        /// with <see cref="SecretProtection.RemovedSecret"/>, so when the config is used the secrets are listed to be
        /// re-entered and the connections aren't used until they are.  The config itself isn't changed.
        /// </summary>
        /// <exception cref="InvalidOperationException">The config has a connection string that can't be parsed.  Its password can't be found to remove it.</exception>
        public string ExportWithoutSecrets()
        {
            ThrowIfInvalidConnections();
            var defaultKey = SecretProtection.DefaultKey;
            try
            {
                var copy = Deserialize(Serialize());
                // Blank passwords too - leaving them would show which connections have a blank password.  The config tool tries
                // a blank password for a connection that's left empty when they're re-entered.
                foreach (var secret in copy.GetSecrets())
                {
                    secret.Remove();
                }
                copy.EncryptionOption = EncryptionOptions.Basic;
                // No secrets, so no machine key - it would only be one that can't be decrypted on another machine
                // Dates left as strings so they are written back unchanged
                var json = JObject.Load(new JsonTextReader(new System.IO.StringReader(copy.Serialize())) { DateParseHandling = DateParseHandling.None });
                json.Remove(nameof(MachineKey));
                return json.ToString(Formatting.Indented);
            }
            finally
            {
                SecretProtection.SetDefaultKey(defaultKey);
            }
        }

        private const string defaultMaintenanceCron = "0 0 0/12 1/1 * ? *";

        public string MaintenanceScheduleCron { get; set; }

        public string GetMaintenanceCron()
        {
            return MaintenanceScheduleCron is null or "" ? defaultMaintenanceCron : MaintenanceScheduleCron;
        }

        [JsonIgnore]
        public List<DBADashConnection> SecondaryDestinationConnections { get; set; } = new();

        // Required for serialization
        public string[] SecondaryDestinations
        {
            get
            {
                var encryptedStrings = new List<string>();
                foreach (var d in SecondaryDestinationConnections)
                {
                    encryptedStrings.Add(d.EncryptedConnectionString);
                }
                return encryptedStrings.ToArray();
            }
            set
            {
                SecondaryDestinationConnections = new List<DBADashConnection>();
                foreach (var s in value.Distinct().ToArray())
                {
                    SecondaryDestinationConnections.Add(new DBADashConnection(s));
                }
            }
        }

        [JsonIgnore]
        public List<DBADashConnection> AllDestinations
        {
            get
            {
                var all = new List<DBADashConnection>();
                if (!string.IsNullOrEmpty(Destination))
                {
                    all.Add(DestinationConnection);
                }
                all.AddRange(SecondaryDestinationConnections);
                return all;
            }
        }

        [JsonIgnore]
        public List<DBADashConnection> SQLDestinations => AllDestinations.Where(d => d.Type == ConnectionType.SQL).ToList();

        public bool WasEncrypted()
        {
            if (wasEncryptionPerformed)
            {
                return wasEncryptionPerformed;
            }
            if (DestinationConnection.WasEncrypted)
            {
                wasEncryptionPerformed = true;
            }
            else
            {
                foreach (var c in SourceConnections)
                {
                    if (c.SourceConnection.WasEncrypted)
                    {
                        wasEncryptionPerformed = true;
                    }
                }
            }
            return wasEncryptionPerformed;
        }

        public int? MessageThreads { get; set; }

        public async Task ValidateDestinationAsync()
        {
            await ValidateDestinationAsync(DestinationConnection);
        }

        public static async Task ValidateDestinationAsync(DBADashConnection destination)
        {
            if (destination.Type == ConnectionType.Invalid)
            {
                throw new Exception("Invalid connection string");
            }
            else if (destination.Type == ConnectionType.SQL)
            {
                if (string.IsNullOrEmpty(destination.InitialCatalog()))
                {
                    throw new Exception("Provide a name for the DBADash repository database through the Initial Catalog property of the connection string");
                }
                // VersionStatus check will throw an error if there is an issue connecting to the DB or the DB isn't valid.
                var status = await DBValidations.VersionStatusAsync(destination.ConnectionString);
                if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    ValidateDestinationVersion(destination);
                }
                Log.Information("DB Version Status: {status}", status.VersionStatus);
            }
            else
            {
                destination.Validate();
            }
        }

        private static void ValidateDestinationVersion(DBADashConnection destination)
        {
            var master = destination.MasterConnection();
            var cInfo = master.ConnectionInfo;
            if (string.IsNullOrEmpty(cInfo.ServerName))
            {
                Log.Warning("@@SERVERNAME returned NULL for {connection}.  Consider fixing with sp_addserver", destination.ConnectionForPrint);
            }
            if (cInfo.MajorVersion < 13 && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlDatabase && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlManagedInstance) // 13=2016, 12 might be Azure DB which is OK
            {
                throw new Exception("DBA Dash repository database requires SQL 2016 SP1 or later");
            }
            else if (cInfo.MajorVersion == 13 && (new Version(cInfo.ProductVersion)).CompareTo(new Version("13.0.4001.0")) < 0 && cInfo.EngineEdition != Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.Enterprise)
            { // Check if we are running SP1 for SQL 2016
                throw new Exception("DBA Dash repository database requires SQL 2016 SP1 or later.  Please upgrade to SP1 or later.");
            }
        }

        public DBADashSource GetSourceFromConnectionString(string connectionString, bool? isAzure = null)
        {
            return GetSourceFromConnectionString(connectionString, SourceConnections, isAzure);
        }

        public DBADashSource GetSourceFromConnectionString(string connectionString, IEnumerable<DBADashSource> sources, bool? isAzure = null)
        {
            var findConnection = new DBADashConnection(connectionString);
            foreach (var s in sources.Where(s => s.SourceConnection.Type == findConnection.Type))
            {
                if (s.SourceConnection.Type == ConnectionType.SQL && string.Equals(s.SourceConnection.DataSource(), findConnection.DataSource(), StringComparison.CurrentCultureIgnoreCase) && s.SourceConnection.ApplicationIntent() == findConnection.ApplicationIntent())
                {
                    // normally we can treat as same connection if we just vary by initial catalog.  For AzureDB, a different DB is a different instance
                    if (string.Equals(s.SourceConnection.InitialCatalog(), findConnection.InitialCatalog(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        return s;
                    }
                    else if ((new[] { "master", string.Empty }).Contains(s.SourceConnection.InitialCatalog().ToLower())
                             && (new[] { "master", string.Empty }).Contains(findConnection.InitialCatalog().ToLower()))
                    {
                        return s;
                    }
                    else
                    {
                        isAzure ??= findConnection.IsAzureDB();
                        if (isAzure == false)
                        {
                            return s;
                        }
                    }
                }
                else if (s.SourceConnection.ConnectionString == findConnection.ConnectionString)
                {
                    return s;
                }
            }
            return null;
        }

        public bool SourceExists(string connectionString, bool? isAzure = null)
        {
            return GetSourceFromConnectionString(connectionString, isAzure) != null;
        }

        public bool SourceExists(string connectionString, IEnumerable<DBADashSource> sources, bool? isAzure = null)
        {
            return GetSourceFromConnectionString(connectionString, sources, isAzure) != null;
        }

        /// <summary>
        /// True if <paramref name="source"/> - or a copy of it, as a collection job carries - is one of the sources, with
        /// the same connection string.
        /// </summary>
        public bool IsCollected(DBADashSource source)
        {
            lock (SourceConnections)
            {
                return SourceConnections.Exists(s => ReferenceEquals(s, source) || s.ConnectionString == source.ConnectionString);
            }
        }

        public void AddConnections(List<DBADashSource> connections)
        {
            lock (SourceConnections)
            {
                SourceConnections.AddRange(connections);
            }
        }

        public List<DBADashSource> AddAzureDBs()
        {
            // Snapshot - probing is slow and the list can change while it runs
            List<DBADashSource> sources;
            lock (SourceConnections)
            {
                sources = SourceConnections.ToList();
            }

            var added = new List<DBADashSource>();
            foreach (var master in sources.Where(src => src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src)))
            {
                try
                {
                    if ((new[] { "master", String.Empty }).Contains(master.SourceConnection.InitialCatalog()) && master.SourceConnection.ConnectionInfo.IsAzureMasterDB)
                    {
                        added.AddRange(AddAzureDBs(master));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Azure DB connections from master {connection}", master.SourceConnection.ConnectionForPrint);
                }
            }
            return added;
        }

        /// <summary>
        /// Adds the databases of an Azure master connection that aren't already monitored, returning them.  The databases
        /// are found outside the lock, so they're only added if the master is still a source then - it may have been
        /// removed (<see cref="Messaging.ConnectionRemoveMessage"/>) - and each is checked again against the live list.
        /// </summary>
        public List<DBADashSource> AddAzureDBs(DBADashSource masterConnection)
        {
            var candidates = GetNewAzureDBConnections(masterConnection);
            var added = new List<DBADashSource>();
            lock (SourceConnections)
            {
                if (!SourceConnections.Contains(masterConnection)) return added;
                foreach (var db in candidates)
                {
                    if (SourceExists(db.SourceConnection.ConnectionString, SourceConnections, true)) continue;
                    SourceConnections.Add(db);
                    added.Add(db);
                }
            }
            return added;
        }

        public List<DBADashSource> GetNewAzureDBConnections()
        {
            var newConnections = new List<DBADashSource>();
            foreach (var cfg in SourceConnections.Where(src => src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src)))
            {
                try
                {
                    if ((new[] { "master", String.Empty }).Contains(cfg.SourceConnection.InitialCatalog()) && cfg.SourceConnection.ConnectionInfo.IsAzureMasterDB)
                    {
                        newConnections.AddRange(GetNewAzureDBConnections(cfg));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Azure DB connections from master {connection}", cfg.SourceConnection.ConnectionForPrint);
                }
            }
            return newConnections;
        }

        public List<DBADashSource> GetNewAzureDBConnections(DBADashSource masterConnection)
        {
            var newConnections = new List<DBADashSource>();
            using (var cn = new SqlConnection(masterConnection.SourceConnection.ConnectionString))
            using (var cmd = new SqlCommand("SELECT name AS DB, @@SERVERNAME + '|' + name AS ConnectionID from sys.databases WHERE name <> 'master'", cn))
            {
                cn.Open();
                using var rdr = cmd.ExecuteReader();
                var builder = new SqlConnectionStringBuilder(masterConnection.SourceConnection.ConnectionString);

                while (rdr.Read())
                {
                    builder.InitialCatalog = rdr.GetString(0);
                    var dbCn = masterConnection.DeepCopy();
                    dbCn.SourceConnection.ConnectionString = builder.ConnectionString;
                    dbCn.ConnectionID = rdr.GetString(1);

                    if (!SourceExists(dbCn.SourceConnection.ConnectionString, true))
                    {
                        newConnections.Add(dbCn);
                        Log.Information("Add Azure DB connection {DataSource}|{DB}", builder.DataSource, builder.InitialCatalog);
                    }
                }
            }
            return newConnections;
        }

        public List<DBADashSource> AddReadReplicas() => AddReadReplicaCandidates(GetNewReadReplicaConnections());

        /// <summary>Adds the read replicas of <paramref name="primaries"/> only - e.g. sources just added - returning them.</summary>
        public List<DBADashSource> AddReadReplicas(List<DBADashSource> primaries) => AddReadReplicaCandidates(GetNewReadReplicaConnections(primaries));

        private List<DBADashSource> AddReadReplicaCandidates(List<DBADashSource> candidates)
        {
            if (candidates.Count == 0) return candidates;
            var added = new List<DBADashSource>();
            lock (SourceConnections)
            {
                foreach (var replica in candidates)
                {
                    // Authoritative dedup under the lock: another thread (or an earlier candidate) may have added an
                    // equivalent source since we snapshotted, so re-check against the live list before adding.
                    if (SourceExists(replica.SourceConnection.ConnectionString, SourceConnections, true)) continue;
                    // The primary was probed outside the lock and may have been removed since
                    var primary = new SqlConnectionStringBuilder(replica.SourceConnection.ConnectionString) { ApplicationIntent = ApplicationIntent.ReadWrite }.ConnectionString;
                    if (!SourceExists(primary, SourceConnections, true)) continue;
                    SourceConnections.Add(replica);
                    added.Add(replica);
                }
            }
            return added;
        }

        public List<DBADashSource> GetNewReadReplicaConnections()
        {
            if (!MonitorReadReplicas) return new List<DBADashSource>();

            // Snapshot the source list - probing can be slow and we must not enumerate while AddReadReplicas mutates it.
            List<DBADashSource> sources;
            lock (SourceConnections)
            {
                sources = SourceConnections.ToList();
            }

            return GetNewReadReplicaConnections(sources);
        }

        /// <summary>
        /// Get the read replicas of the supplied sources that aren't already covered by them.  Takes the sources to
        /// probe so the config tool's scan can include replicas of Azure DBs found in the same scan but not yet added
        /// to the config.
        /// </summary>
        public List<DBADashSource> GetNewReadReplicaConnections(List<DBADashSource> sources)
        {
            if (!MonitorReadReplicas) return new List<DBADashSource>();

            var candidates = sources
                .Where(src => src.SourceConnection.Type == ConnectionType.SQL && !OfflineInstances.IsOffline(src))
                .ToList();
            var newConnections = new ConcurrentBag<DBADashSource>();

            // Every candidate that isn't rejected on its connection string alone costs a connection (and the read-only
            // probe costs another), so probe in parallel - a scan of a large estate is otherwise as slow as the sum of
            // its connection times.  Bounded so a big config doesn't flood the gateway or the thread pool.  Each
            // candidate is probed by a single task and "sources" is only read, so no locking is needed here.
            Parallel.ForEach(candidates,
                new ParallelOptions { MaxDegreeOfParallelism = ReadReplicaScanParallelism },
                cfg =>
                {
                    try
                    {
                        var replica = GetReadReplicaConnection(cfg, sources);
                        if (replica != null)
                        {
                            newConnections.Add(replica);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error checking for read replica for {connection}", cfg.SourceConnection.ConnectionForPrint);
                    }
                });

            // Ordered so the replicas are added (and written to the config) in a stable order rather than in whichever
            // order the probes happened to complete.
            return newConnections.OrderBy(src => src.ConnectionID, StringComparer.InvariantCultureIgnoreCase).ToList();
        }

        /// <summary>
        /// Returns a read-only (read replica) source for the supplied primary source, or null if the primary is not an
        /// eligible Azure SQL Database, a replica source already exists, or the read-only connection routes back to the
        /// primary (i.e. there is no readable secondary).  The returned source has ApplicationIntent=ReadOnly and a
        /// distinct "|ReadOnly" suffixed ConnectionID.
        /// </summary>
        private DBADashSource GetReadReplicaConnection(DBADashSource primary, IEnumerable<DBADashSource> existingSources)
        {
            // Skip sources that are already read-only (an existing replica source).
            if (primary.SourceConnection.ApplicationIntent() == ApplicationIntent.ReadOnly) return null;

            // Only Azure SQL Database user databases have readable secondaries reached via ApplicationIntent=ReadOnly.
            // The connection string tests come first because they're free: reading ConnectionInfo opens a connection
            // for any source we haven't inspected yet, so an instance monitored without an initial catalog (the normal
            // case for anything that isn't an Azure DB) is skipped without connecting to it.
            var initialCatalog = primary.SourceConnection.InitialCatalog();
            if (string.IsNullOrEmpty(initialCatalog) || string.Equals(initialCatalog, "master", StringComparison.OrdinalIgnoreCase)) return null;

            var builder = new SqlConnectionStringBuilder(primary.SourceConnection.ConnectionString)
            {
                ApplicationIntent = ApplicationIntent.ReadOnly
            };
            var readOnlyConnectionString = builder.ConnectionString;

            // Skip if a source for this replica already exists (added manually or on a previous scan).  Checked against
            // the caller's snapshot to avoid enumerating the live SourceConnections list without a lock; AddReadReplicas
            // re-checks authoritatively under the lock before adding.
            if (SourceExists(readOnlyConnectionString, existingSources, true)) return null;

            if (!primary.SourceConnection.ConnectionInfo.IsAzureDB) return null;

            // Probe: connect read-only and confirm the gateway routed us to a read-only secondary.  If it routed back
            // to the primary (READ_WRITE) there is no replica to monitor, so skip.
            using (var cn = new SqlConnection(readOnlyConnectionString))
            using (var cmd = new SqlCommand("SELECT CAST(CASE WHEN DATABASEPROPERTYEX(DB_NAME(),'Updateability') = 'READ_ONLY' THEN 1 ELSE 0 END AS BIT)", cn))
            {
                cn.Open();
                if (!(bool)cmd.ExecuteScalar()) return null;
            }

            var replica = primary.DeepCopy();
            replica.SourceConnection.ConnectionString = readOnlyConnectionString;
            // Set the ConnectionID to match what the collector would generate (<@@SERVERNAME>|<db>|ReadOnly) so the
            // replica resolves correctly for on-demand collection and stays distinct from the primary instance.
            replica.ConnectionID = $"{primary.SourceConnection.ConnectionInfo.ServerName}|{initialCatalog}|ReadOnly";
            Log.Information("Add read replica connection for {ConnectionID}", replica.ConnectionID);
            return replica;
        }

        public DBADashConnection GetDestination(string hash)
        {
            return DestinationConnection.Hash == hash ? DestinationConnection : SecondaryDestinationConnections.FirstOrDefault(c => c.Hash == hash);
        }

        public async Task<DBADashSource> GetSourceConnectionAsync(string connectionID)
        {
            if (!string.IsNullOrEmpty(connectionID))
            {
                var src = SourceConnections.FirstOrDefault(s =>
                    string.Equals(s.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase));
                if (src != null) // We have a match on ConnectionID
                {
                    return src;
                }
                src = await GetImplicitAzureDBSourceAsync(connectionID);
                if (src != null)
                {
                    return src;
                }
            }

            throw new ArgumentException($"Unable to find instance with ConnectionID {connectionID}");
        }

        /// <summary>
        /// Resolve an Azure SQL Database source that isn't in the config, but is monitored implicitly: databases added
        /// by the Azure DB scan (from the server's master connection) and read replicas added by replica monitoring
        /// (from the primary).  Returns null if the ConnectionID can't be resolved this way.
        /// </summary>
        private async Task<DBADashSource> GetImplicitAzureDBSourceAsync(string connectionID)
        {
            if (!connectionID.Contains('|')) return null; // Doesn't look like an Azure DB ConnectionID

            // <@@SERVERNAME>|<db>, or <@@SERVERNAME>|<db>|ReadOnly for a read replica.  Split from each end rather than
            // taking element [1] so a database name containing a pipe doesn't get truncated.
            var parts = connectionID.Split('|');
            var isReadOnlyReplica = parts.Length > 2 &&
                                    string.Equals(parts[^1], "ReadOnly", StringComparison.InvariantCultureIgnoreCase);
            var serverName = parts[0];
            var dbName = string.Join("|", parts[1..(isReadOnlyReplica ? ^1 : ^0)]);
            if (dbName.Length == 0) return null;

            // A read replica source only exists while replica monitoring is enabled, so there is nothing to resolve
            // otherwise - short circuit instead of paying for a connection attempt that can't produce a match.
            if (isReadOnlyReplica && !MonitorReadReplicas) return null;

            // The replica of an explicitly configured Azure DB is created from that source, so use the primary as the
            // template where we have it.  Otherwise fall back to the master connection, which is what the Azure DB scan
            // adds databases from.
            var template = isReadOnlyReplica
                ? SourceConnections.FirstOrDefault(s => string.Equals(s.ConnectionID, serverName + "|" + dbName,
                    StringComparison.InvariantCultureIgnoreCase))
                : null;
            if (template == null && ScanForAzureDBs)
            {
                template = SourceConnections.FirstOrDefault(s => string.Equals(s.ConnectionID, serverName + "|master",
                    StringComparison.InvariantCultureIgnoreCase));
            }
            if (template == null) return null;

            var src = template.DeepCopy();
            src.ConnectionID = null;
            var builder = new SqlConnectionStringBuilder(template.SourceConnection.ConnectionString)
            {
                InitialCatalog = dbName
            };
            if (isReadOnlyReplica)
            {
                builder.ApplicationIntent = ApplicationIntent.ReadOnly;
            }
            src.SourceConnection.ConnectionString = builder.ToString();
            var collector = await DBCollector.CreateAsync(src, ServiceName);
            src.ConnectionID = collector.ConnectionID;
            // Double check that the generated ConnectionID matches the one we're looking for & return the connection.
            // For a replica this also confirms the read-only connection actually routed to a readable secondary - if it
            // routed back to the primary the collector generates the primary's ConnectionID and we don't match.
            return string.Equals(src.ConnectionID, connectionID, StringComparison.InvariantCultureIgnoreCase) ? src : null;
        }

        /// <summary>
        /// Get source connection from connection string or ConnectionID.  Returns null if not found
        /// </summary>
        public async Task<DBADashSource> FindSourceConnectionAsync(string connectionString, string connectionId)
        {
            var source = GetSourceFromConnectionString(connectionString);
            try
            {
                if (!string.IsNullOrEmpty(connectionId))
                {
                    source = await GetSourceConnectionAsync(connectionId);
                }
            }
            catch (Exception ex)
            {
                // Not found, or the probe connection for an implicitly monitored Azure DB failed.  Fall back to the
                // connection string match - logged because it's otherwise invisible why a known instance didn't match.
                Log.Debug(ex, "Unable to resolve source connection for ConnectionID {ConnectionID}", connectionId);
            }

            return source;
        }
    }
}