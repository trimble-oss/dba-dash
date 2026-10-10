using DBADashService;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class DBADashSource
    {
        public enum IOCollectionLevels
        {
            Full = 1,
            InstanceOnly = 2,
            Drive = 3,
            Database = 4,
            DriveAndDatabase = 5
        }

        private int slowQueryThresholdMs = -1;
        private CollectionSchedules collectionSchedules;
        private PlanCollectionThreshold runningQueryPlanThreshold;
        private string schemaSnapshotDBs;
        private bool noWMI;
        private int slowQuerySessionMaxMemoryKB = 4096;
        private int slowQueryTargetMaxMemoryKB = -1;
        private bool persistXESessions;
        private string deadlockXESessionName;
        private bool useDualXESession = true;
        public bool WriteToSecondaryDestinations { get; set; } = true;
        public string ConnectionID { get; set; }
        public bool ScriptAgentJobs { get; set; } = true;
        private Dictionary<string, CustomCollection> customCollections = new();

        public IOCollectionLevels IOCollectionLevel { get; set; } = IOCollectionLevels.Full;

        public CollectionSchedules CollectionSchedules
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? collectionSchedules : null;
            set => collectionSchedules = value;
        }

        public Dictionary<string, CustomCollection> CustomCollections
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? customCollections : null;
            set => customCollections = value;
        }

        public PlanCollectionThreshold RunningQueryPlanThreshold
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? runningQueryPlanThreshold : null;
            set => runningQueryPlanThreshold = value;
        }

        public string SchemaSnapshotDBs
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? schemaSnapshotDBs : string.Empty;
            set => schemaSnapshotDBs = value;
        }

        [JsonIgnore]
        public bool HasCustomSchedule => !(CollectionSchedules == null || CollectionSchedules.Count == 0);

        #region "Plan Collection Threshold Properties"

        // Added plan collection properties to allow them to be visible/editable in the grid

        [JsonIgnore]
        public bool PlanCollectionEnabled
        {
            get => RunningQueryPlanThreshold is { PlanCollectionEnabled: true };
            set => RunningQueryPlanThreshold = value ? PlanCollectionThreshold.DefaultThreshold : null;
        }

        [JsonIgnore]
        public int PlanCollectionCPUThreshold
        {
            get => RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL
                    ? int.MaxValue
                    : RunningQueryPlanThreshold.CPUThreshold;
            set
            {
                RunningQueryPlanThreshold ??= PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                RunningQueryPlanThreshold.CPUThreshold = value;
            }
        }

        [JsonIgnore]
        public int PlanCollectionMemoryGrantThreshold
        {
            get => RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL ? int.MaxValue : RunningQueryPlanThreshold.MemoryGrantThreshold;
            set
            {
                RunningQueryPlanThreshold ??= PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                RunningQueryPlanThreshold.MemoryGrantThreshold = value;
            }
        }

        [JsonIgnore]
        public int PlanCollectionDurationThreshold
        {
            get => RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL ? int.MaxValue : RunningQueryPlanThreshold.DurationThreshold;
            set
            {
                RunningQueryPlanThreshold ??= PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                RunningQueryPlanThreshold.DurationThreshold = value;
            }
        }

        [JsonIgnore]
        public int PlanCollectionCountThreshold
        {
            get => RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL ? int.MaxValue : RunningQueryPlanThreshold.CountThreshold;
            set
            {
                RunningQueryPlanThreshold ??= PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                RunningQueryPlanThreshold.CountThreshold = value;
            }
        }

        #endregion "Plan Collection Threshold Properties"

        [JsonIgnore]
        public DBADashConnection SourceConnection { get; set; }

        public string GetSource()
        {
            return SourceConnection.ConnectionString;
        }

        // Note if source is SQL connection string, password is encrypted.  Use GetSource() to return with real password
        public string ConnectionString
        {
            get => SourceConnection == null ? "" : SourceConnection.EncryptedConnectionString;
            set => SourceConnection = new DBADashConnection(value);
        }

        public static string GenerateFileName(string connection)
        {
            return DestinationHandling.FileNamePrefix + DateTime.UtcNow.ToString("yyyyMMdd_HHmm_ss") + "_" + connection + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("/", "-") + DestinationHandling.FileExtension;
        }

        [DefaultValue(false)]
        public bool NoWMI
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && noWMI;
            set => noWMI = value;
        }

        /// <summary>
        /// Per-instance perfmon counters, a tri-state override of the global list
        /// (<see cref="CollectionConfig.PerfmonCounters"/>):
        /// <list type="bullet">
        /// <item><c>null</c> - inherit the global list.</item>
        /// <item>empty list - disabled (collect no perfmon counters for this instance).</item>
        /// <item>populated - collect exactly these counters.</item>
        /// </list>
        /// Only applies to SQL sources and requires WMI (no effect when <see cref="NoWMI"/> is set).
        /// </summary>
        public List<PerfmonCounter> PerfmonCounters
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? perfmonCounters : null;
            set => perfmonCounters = value;
        }

        private List<PerfmonCounter> perfmonCounters;

        public int SlowQuerySessionMaxMemoryKB
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? slowQuerySessionMaxMemoryKB : 0;
            set => slowQuerySessionMaxMemoryKB = value;
        }

        public int SlowQueryTargetMaxMemoryKB
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? slowQueryTargetMaxMemoryKB : 0;
            set => slowQueryTargetMaxMemoryKB = value;
        }

        [DefaultValue(true)]
        public bool UseDualEventSession
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && useDualXESession;
            set => useDualXESession = value;
        }

        [DefaultValue(-1)]
        public int SlowQueryThresholdMs
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? slowQueryThresholdMs : -1;
            set => slowQueryThresholdMs = value;
        }

        /// <summary>True when a threshold is set for the SlowQueries collection to capture against.  A
        /// negative threshold is the off switch - see <see cref="SlowQueryThresholdMs"/>.</summary>
        [JsonIgnore]
        public bool IsSlowQueryCollectionEnabled => SlowQueryThresholdMs >= 0;

        /// <summary>
        /// How many query families to keep per ranking measure each interval, and the off switch for the
        /// whole collection at zero - which is the default.  Reading sys.dm_exec_query_stats walks the plan
        /// cache, so the cost is a property of the instance rather than something the query can filter down,
        /// and that is a decision to make per connection rather than to impose on every upgrade.
        ///
        /// <para>Seven measures are ranked and the results unioned, so the number of families kept is at
        /// least this and usually near it, because heavy queries top several measures at once.  Up to seven
        /// times it, plus any family kept for one slow execution.  Everything not kept is rolled up rather than dropped, so raising it buys
        /// detail rather than accuracy.</para>
        /// </summary>
        [DefaultValue(0)]
        public int QueryStatsTopN
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? queryStatsTopN : 0;
            set => queryStatsTopN = value;
        }

        /// <summary>Statements kept within a family before the remainder becomes a single rolled up row.</summary>
        [DefaultValue(3)]
        public int QueryStatsMaxStatementsPerFamily { get; set; } = 3;

        /// <summary>Plan shapes kept within a statement before the remainder becomes a single rolled up row.</summary>
        [DefaultValue(5)]
        public int QueryStatsMaxPlansPerStatement { get; set; } = 5;

        /// <summary>
        /// An execution known to have taken at least this elapsed time in milliseconds keeps its family whatever
        /// the ranking says.  One expensive run is exactly what a top N by total would hide.  The plan cache only
        /// shows a run like that where it was the last execution, set a new maximum for its plan, or dominates
        /// the interval's average - the Slow Queries collection is the one that records every slow execution.
        /// </summary>
        [DefaultValue(1000)]
        public int QueryStatsSingleExecutionThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Cap on the per-instance baseline, which is what bounds the memory the collection costs the
        /// service.  An entry takes about 160 bytes, so twenty thousand is a little over 3MB per instance;
        /// the least recently seen are evicted first and the eviction count travels with the collection, so
        /// a cap set too low is visible rather than silently lossy.
        /// </summary>
        [DefaultValue(20000)]
        public int QueryStatsBaselineMaxEntries { get; set; } = 20000;

        /// <summary>
        /// Skip the next collection when the previous read of the DMV took longer than this, in
        /// milliseconds.  Zero disables the check.  Monitoring should not become the performance problem it
        /// is reporting on, and the failure mode here is a very large plan cache rather than a bug.
        /// </summary>
        [DefaultValue(5000)]
        public int QueryStatsMaxReadDurationMs { get; set; } = 5000;

        /// <summary>
        /// How far back a collection will diff against.  When the previous collection is older than this -
        /// a service stopped for hours, or a monitored instance unreachable for a while - the baseline is
        /// discarded and the run behaves as a first collection rather than reporting a multi-day delta as
        /// though it happened in one interval.  See <c>Baseline.DiscardIfStale</c> for why that is the
        /// safer answer.
        ///
        /// <para>An hour by default, so a restart or a handful of missed intervals still produce a real
        /// delta: the point is to bound the damage, not to throw away every gap.  Never less than three
        /// intervals of the collection's schedule, so a slower schedule isn't taken for an outage - see
        /// <c>Baseline.GetMaxLookback</c>.</para>
        /// </summary>
        [DefaultValue(60)]
        public int QueryStatsMaxLookbackMinutes { get; set; } = 60;

        /// <summary>
        /// Cap on how many new batch texts one collection will fetch for the statements it stored, and
        /// separately on how many ad hoc query shapes it will fetch an example for to make their templates.
        /// Both are fetched once and cached, so the cap only bites while a workload is new to the
        /// collection: what it leaves behind is picked up over the following intervals, and the statements
        /// are stored either way.
        /// </summary>
        [DefaultValue(100)]
        public int QueryStatsTextHandlesPerCollection { get; set; } = 100;

        /// <summary>
        /// Cap on how many plans one collection will fetch, for the plan shapes of the statements it stored that it
        /// has not sent a plan for in the last day.  Zero switches plan capture off.  Heaviest first, so a cap that
        /// bites - after a restart, when nothing has been sent yet - leaves the cheapest for the following intervals.
        /// Once the plans in use have been sent, a collection only fetches the plan shapes that are new.
        /// </summary>
        [DefaultValue(50)]
        public int QueryStatsPlansPerCollection { get; set; } = 50;

        /// <summary>
        /// A plan is only fetched for a row whose CPU in the interval reached this many milliseconds.  Zero, the
        /// default, fetches one for every row the ranking kept, which is already the top of the workload - and a
        /// threshold misses the plan most worth having, the one a statement ran under before a regression made it
        /// expensive.
        /// </summary>
        [DefaultValue(0)]
        public int QueryStatsPlanCPUThresholdMs { get; set; }

        /// <summary>
        /// Take the query stats settings that are only set in the config file from <paramref name="other"/>.  The service
        /// config tool replaces a connection it updates with one built from its form, which has only the top N and the
        /// plan cap, so without this an update would put the rest back to their defaults.
        /// </summary>
        public void CopyQueryStatsTuningFrom(DBADashSource other)
        {
            QueryStatsMaxStatementsPerFamily = other.QueryStatsMaxStatementsPerFamily;
            QueryStatsMaxPlansPerStatement = other.QueryStatsMaxPlansPerStatement;
            QueryStatsSingleExecutionThresholdMs = other.QueryStatsSingleExecutionThresholdMs;
            QueryStatsBaselineMaxEntries = other.QueryStatsBaselineMaxEntries;
            QueryStatsMaxReadDurationMs = other.QueryStatsMaxReadDurationMs;
            QueryStatsMaxLookbackMinutes = other.QueryStatsMaxLookbackMinutes;
            QueryStatsTextHandlesPerCollection = other.QueryStatsTextHandlesPerCollection;
            QueryStatsPlanCPUThresholdMs = other.QueryStatsPlanCPUThresholdMs;
        }

        /// <summary>True when a top N is configured for the QueryStats collection.  Zero is the off switch,
        /// and the default - see <see cref="QueryStatsTopN"/>.</summary>
        [JsonIgnore]
        public bool IsQueryStatsCollectionEnabled => QueryStatsTopN > 0;

        public QueryStats.QueryStatsLimits GetQueryStatsLimits() => new()
        {
            TopN = QueryStatsTopN,
            MaxStatementsPerFamily = QueryStatsMaxStatementsPerFamily,
            MaxPlansPerStatement = QueryStatsMaxPlansPerStatement,
            SingleExecutionElapsedThreshold = (long)QueryStatsSingleExecutionThresholdMs * 1000
        };

        private int queryStatsTopN;

        [DefaultValue(false)]
        public bool PersistXESessions
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && persistXESessions; set => persistXESessions = value;
        }

        /// <summary>How the SlowQueries collection captures events - see <see cref="SlowQueryCaptureModes"/>.</summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum SlowQueryCaptureModes
        {
            /// <summary>
            /// The DBADash_1 session (and DBADash_2 in dual session mode) with a ring buffer target that is read in
            /// full and then emptied by stopping and starting the session.  The default, and the only mode that
            /// needs nothing on disk - so the one Azure SQL Database and Managed Instance always use.
            /// </summary>
            RingBuffer,

            /// <summary>
            /// The <see cref="ManagedSlowQueryXESessionName"/> session with an event_file target, read from a resume
            /// cursor so each run reads only what is new and the session is never stopped to empty it.  On-premises
            /// only: Azure SQL Database and Managed Instance fall back to <see cref="RingBuffer"/>.
            /// </summary>
            EventFile,

            /// <summary>
            /// A session the DBA already runs, named by <see cref="SlowQueryXESessionName"/>.  Read only - never
            /// created, altered, stopped or emptied.  Its own filters decide what it captures; the configured
            /// threshold is applied as it is read.
            /// </summary>
            ExistingSession
        }

        /// <summary>The session DBA Dash creates and reads in <see cref="SlowQueryCaptureModes.EventFile"/> mode.</summary>
        public const string ManagedSlowQueryXESessionName = "DBADash_SlowQueries";

        /// <summary>
        /// The sessions DBA Dash creates for slow query capture - the ring buffer mode's two and the event file mode's
        /// one.  None of them can be read in <see cref="SlowQueryCaptureModes.ExistingSession"/> mode: DBA Dash drops
        /// them when it cleans up after a mode switch and on service stop, which would remove the session the existing
        /// session mode is meant to read and never alter.
        /// </summary>
        public static readonly IReadOnlyList<string> ReservedSlowQueryXESessionNames =
            new[] { "DBADash_1", "DBADash_2", ManagedSlowQueryXESessionName };

        /// <summary>True when <paramref name="sessionName"/> is one of <see cref="ReservedSlowQueryXESessionNames"/>.
        /// Session names aren't case sensitive on the instance, so neither is this.</summary>
        public static bool IsReservedSlowQueryXESessionName(string sessionName) =>
            !string.IsNullOrWhiteSpace(sessionName) &&
            ReservedSlowQueryXESessionNames.Contains(sessionName.Trim(), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// How slow queries are captured.  <see cref="SlowQueryCaptureModes.RingBuffer"/> is the default and is the
        /// behaviour from before the other modes existed.
        /// </summary>
        [DefaultValue(SlowQueryCaptureModes.RingBuffer)]
        public SlowQueryCaptureModes SlowQueryCaptureMode
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? slowQueryCaptureMode : SlowQueryCaptureModes.RingBuffer;
            set => slowQueryCaptureMode = value;
        }

        private SlowQueryCaptureModes slowQueryCaptureMode = SlowQueryCaptureModes.RingBuffer;

        /// <summary>The session read in <see cref="SlowQueryCaptureModes.ExistingSession"/> mode.  Ignored otherwise.</summary>
        [DefaultValue("")]
        public string SlowQueryXESessionName
        {
            get => SourceConnection is { Type: ConnectionType.SQL } ? slowQueryXESessionName?.Trim() ?? string.Empty : string.Empty;
            set => slowQueryXESessionName = value;
        }

        private string slowQueryXESessionName;

        /// <summary>max_file_size, in MB, of the event file in <see cref="SlowQueryCaptureModes.EventFile"/> mode.</summary>
        [DefaultValue(DefaultSlowQueryEventFileMaxSizeMB)]
        public int SlowQueryEventFileMaxSizeMB { get; set; } = DefaultSlowQueryEventFileMaxSizeMB;

        public const int DefaultSlowQueryEventFileMaxSizeMB = 20;

        /// <summary>max_rollover_files of the event file in <see cref="SlowQueryCaptureModes.EventFile"/> mode.  Together
        /// with <see cref="SlowQueryEventFileMaxSizeMB"/> this is how much the session can hold before the oldest
        /// events are lost - which only matters when the service isn't collecting.</summary>
        [DefaultValue(DefaultSlowQueryEventFileMaxRolloverFiles)]
        public int SlowQueryEventFileMaxRolloverFiles { get; set; } = DefaultSlowQueryEventFileMaxRolloverFiles;

        public const int DefaultSlowQueryEventFileMaxRolloverFiles = 5;

        /// <summary>
        /// Leave the <see cref="ManagedSlowQueryXESessionName"/> session running when the service stops, and start it
        /// with the instance (STARTUP_STATE=ON), so slow queries that run while the service is down are still captured
        /// and collected when it comes back.  Off by default: some would rather DBA Dash left nothing running on the
        /// instance while the service is stopped.  When off, the session is stopped or dropped on service stop like
        /// the ring buffer sessions, as <see cref="PersistXESessions"/> says.  Only applies in
        /// <see cref="SlowQueryCaptureModes.EventFile"/> mode.
        /// </summary>
        [DefaultValue(false)]
        public bool KeepSlowQueryXESessionRunning { get; set; }

        /// <summary>
        /// The capture mode that actually applies to an instance.  Event file mode needs a local event file, which
        /// Azure SQL Database and Managed Instance don't allow without blob storage, so those use the ring buffer.
        /// </summary>
        public SlowQueryCaptureModes GetEffectiveSlowQueryCaptureMode(bool isAzureDB, bool isManagedInstance) =>
            SlowQueryCaptureMode == SlowQueryCaptureModes.EventFile && (isAzureDB || isManagedInstance)
                ? SlowQueryCaptureModes.RingBuffer
                : SlowQueryCaptureMode;

        /// <summary>
        /// True when the service should leave the managed event file session alone on shutdown.  Only in event file
        /// mode with slow query capture switched on: otherwise the session is a leftover - from a mode switch, or from
        /// capture being switched off - and is cleaned up like the others.
        /// </summary>
        [JsonIgnore]
        public bool IsSlowQueryXESessionKeptRunning =>
            KeepSlowQueryXESessionRunning && IsSlowQueryCollectionEnabled &&
            SlowQueryCaptureMode == SlowQueryCaptureModes.EventFile;

        /// <summary>
        /// The extended events session the Deadlocks collection reads <c>xml_deadlock_report</c> from.  Four
        /// options, distinguished by the name alone so there is no separate mode switch to get out of step
        /// with it:
        ///
        /// <list type="bullet">
        /// <item>Blank - deadlock collection is switched off for this connection, and the default.  There is
        /// no session to read, so <c>CollectionTypeIsApplicable</c> excludes the collection whatever its
        /// schedule says.</item>
        /// <item><see cref="ManagedDeadlockXESessionName"/> - DBA Dash creates, starts and reads its own
        /// session.  The one to choose when switching the collection on, because reading an event file costs
        /// roughly what it costs to open and seek the file set rather than what it costs to read the new
        /// events: a session holding nothing but deadlock reports reads in milliseconds where system_health
        /// takes seconds, on every collection, getting worse as its files grow.</item>
        /// <item><c>system_health</c> - read only, nothing to deploy, and it already holds whatever the
        /// instance has done recently, so it is the option that shows deadlocks from before the collection
        /// was switched on.  Needs only VIEW SERVER STATE.</item>
        /// <item>Any other name - a session the DBA already runs.  Read only; if it isn't running that is
        /// reported rather than started.</item>
        /// </list>
        ///
        /// <para>Only the reserved name is ever created or altered.  A session DBA Dash did not make -
        /// system_health included - is read and left alone.</para>
        ///
        /// <para>On Azure SQL Database the same name means a <em>database</em> scoped session, capturing
        /// <c>database_xml_deadlock_report</c> into a ring buffer.  There is no system_health there and no
        /// server scoped session to point at, so the managed session is the only option that needs nothing
        /// set up by hand.</para>
        ///
        /// <para>Null, empty and whitespace are the same thing - off.  Nothing distinguishes a name that was
        /// cleared from one that was never set: a config written before the collection existed has no value
        /// here and gets the collection switched off, which is what its schedule already said.</para>
        /// </summary>
        [DefaultValue("")]
        public string DeadlockXESessionName
        {
            get => SourceConnection is { Type: ConnectionType.SQL }
                ? deadlockXESessionName?.Trim() ?? string.Empty
                : string.Empty; // Only a SQL connection has an XE session to read
            set => deadlockXESessionName = value;
        }

        /// <summary>True when a session is configured for the Deadlocks collection to read.  Blank is the off
        /// switch - see <see cref="DeadlockXESessionName"/>.</summary>
        [JsonIgnore]
        public bool IsDeadlockCollectionEnabled => !string.IsNullOrWhiteSpace(DeadlockXESessionName);

        /// <summary>
        /// True when configuration alone switches this collection off for the connection, whatever its
        /// schedule says: deadlocks have no session to read when the session name is blank, slow queries no
        /// threshold to capture against when it is negative.  <c>DBCollector.CollectionTypeIsApplicable</c>
        /// excludes both from collection, so the schedule reported to the repository and an on-demand
        /// request have to treat them as disabled rather than as merely overdue.
        /// </summary>
        public bool IsCollectionDisabledByConfiguration(CollectionType type) => type switch
        {
            CollectionType.Deadlocks => !IsDeadlockCollectionEnabled,
            CollectionType.SlowQueries => !IsSlowQueryCollectionEnabled,
            CollectionType.QueryStats => !IsQueryStatsCollectionEnabled,
            _ => false
        };

        /// <summary>
        /// The one session name DBA Dash will create and start itself, and the one to pick when switching the
        /// collection on: it is the option that keeps the per-collection cost to milliseconds.  Costs ALTER
        /// ANY EVENT SESSION - the permission the slow query collection already takes - and starts empty, so
        /// it shows deadlocks from when it was switched on rather than before.
        /// </summary>
        public const string ManagedDeadlockXESessionName = "DBADash_Deadlocks";

        /// <summary>The session every supported on-premises instance runs already.  Read only.  Not available
        /// on Azure SQL Database, which has no server scoped sessions.</summary>
        public const string SystemHealthXESessionName = "system_health";


        /// <summary>
        /// Empty the deadlock session's ring buffer after each collection, by stopping and starting it.
        ///
        /// <para>A ring buffer read costs what the buffer holds rather than what is new in it - around half
        /// a second for a full one against around thirty milliseconds for an empty one, on every collection.
        /// On a database that deadlocks steadily the buffer stays full, so every run pays the full cost to
        /// return data almost all of which has already been stored.  Emptying it after reading keeps the
        /// reads short.</para>
        ///
        /// <para>Off by default because it can lose a deadlock: one that has fired but is still in the
        /// session's memory buffer when the session stops is gone.  The window is the session's dispatch
        /// latency, which the managed session keeps short for this reason, and a flush only happens when the
        /// buffer had something in it - so an idle database is never stopped and started at all.</para>
        ///
        /// <para>Only ever applies to the session DBA Dash owns, and only when that session's target is a
        /// ring buffer - so in practice Azure SQL Database and Managed Instance.  A session the DBA runs is never stopped
        /// whatever this is set to, and the on-premises managed session reads an event file, where the
        /// resume cursor already makes each read proportional to what is new.</para>
        /// </summary>
        [DefaultValue(false)]
        public bool FlushDeadlockXERingBuffer
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && flushDeadlockXERingBuffer;
            set => flushDeadlockXERingBuffer = value;
        }

        private bool flushDeadlockXERingBuffer;

        /// <summary>
        /// After the first run of the Deadlocks collection, read system_health once for the deadlocks from before the
        /// configured session started.
        ///
        /// <para>What a dedicated session gives up is history: it starts empty, where system_health already
        /// holds whatever the instance deadlocked on recently.  Reading system_health once, when the collection
        /// is first switched on, gets that history without paying system_health's read cost on every collection
        /// afterwards.  It runs as a low priority work item after the first run has read the configured session,
        /// and keeps only what is older than that session's start, so the two cover the time between them without
        /// a gap and without sending the same deadlock twice.</para>
        ///
        /// <para>On by default because it costs a single read.  Applies only where there is something to
        /// backfill from - see <see cref="GetDeadlockBackfillSessionName"/>.  "First run" means no entry for the
        /// connection in the service's DeadlockCursors.json, where the backfill is recorded as pending until it has
        /// run, so a restart before it runs doesn't lose it.  Losing that file repeats the backfill once; repository
        /// dedup absorbs what comes back.  The read is limited by
        /// <see cref="CollectionConfig.DeadlockBackfillTimeLimitSeconds"/>, and keeps what it read if it runs out of
        /// time.</para>
        /// </summary>
        [DefaultValue(true)]
        public bool BackfillDeadlocksFromSystemHealth
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && backfillDeadlocksFromSystemHealth;
            set => backfillDeadlocksFromSystemHealth = value;
        }

        private bool backfillDeadlocksFromSystemHealth = true;

        /// <summary>
        /// The session a first run should backfill from, or null when there is nothing to backfill: the option is
        /// off, the collection is off, the configured session is system_health already, or the instance is Azure
        /// SQL Database - which has no system_health.
        /// </summary>
        public string GetDeadlockBackfillSessionName(bool isAzureDB) =>
            BackfillDeadlocksFromSystemHealth
            && IsDeadlockCollectionEnabled
            && !isAzureDB
            && !string.Equals(DeadlockXESessionName, SystemHealthXESessionName, StringComparison.OrdinalIgnoreCase)
                ? SystemHealthXESessionName
                : null;

        /// <summary>True when the configured session is one DBA Dash owns, and may therefore create or start.</summary>
        [JsonIgnore]
        public bool IsDeadlockXESessionManaged =>
            string.Equals(DeadlockXESessionName, ManagedDeadlockXESessionName, StringComparison.OrdinalIgnoreCase);

        private bool _collectSessionWaits = true;

        public bool CollectSessionWaits
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectSessionWaits;
            set => _collectSessionWaits = value;
        }

        private bool _collectTaskWaits;

        public bool CollectTaskWaits
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectTaskWaits;
            set => _collectTaskWaits = value;
        }

        private bool _collectCursors;

        public bool CollectCursors
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectCursors;
            set => _collectCursors = value;
        }

        private bool _collectTranBeginTime = true;

        public bool CollectTranBeginTime
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectTranBeginTime;
            set => _collectTranBeginTime = value;
        }

        private bool _collectTempDb;

        public bool CollectTempDB
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectTempDb;
            set => _collectTempDb = value;
        }

        public int? TableSizeCollectionThresholdMB { get; set; }

        public int? TableSizeMaxDatabaseThreshold { get; set; }

        public string TableSizeDatabases { get; set; }

        public int? TableSizeMaxTableThreshold { get; set; }

        public DBADashSource(string source)
        {
            ConnectionString = source;
        }

        public DBADashSource()
        {
        }

        public void SetConnectionIDFromBuilderIfNotSet()
        {
            if (SourceConnection.Type != ConnectionType.SQL || !string.IsNullOrEmpty(ConnectionID)) return;
            var builder = new SqlConnectionStringBuilder(SourceConnection.ConnectionString);
            ConnectionID = builder.DataSource is "." or "LOCALHOST" ? Environment.MachineName : builder.DataSource;
        }

        public async Task<string> GetGeneratedConnectionIDAsync()
        {
            if (SourceConnection.Type != ConnectionType.SQL) return string.Empty;
            if (!string.IsNullOrEmpty(generatedConnectionID)) return generatedConnectionID;
            var collector = await DBCollector.CreateAsync(this, "DBADashService");
            generatedConnectionID = collector.ConnectionID;
            return generatedConnectionID;
        }

        private string generatedConnectionID;
    }
}