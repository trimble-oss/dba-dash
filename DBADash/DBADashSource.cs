using DBADashService;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [DefaultValue(false)]
        public bool PersistXESessions
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && persistXESessions; set => persistXESessions = value;
        }

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
        /// ring buffer - so in practice Azure SQL Database.  A session the DBA runs is never stopped
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