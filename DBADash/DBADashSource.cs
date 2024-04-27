using DBADashService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SqlServer.Dac.Model;
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
        private bool persistXESessions = false;
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
            return "DBADash_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmm_ss") + "_" + connection + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("/", "-") + ".xml";
        }

        [DefaultValue(false)]
        public bool NoWMI
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && noWMI;
            set => noWMI = value;
        }

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

        [DefaultValue(false)]
        public bool PersistXESessions
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && persistXESessions; set => persistXESessions = value;
        }

        private bool _collectSessionWaits = true;

        public bool CollectSessionWaits
        {
            get => SourceConnection is { Type: ConnectionType.SQL } && _collectSessionWaits;
            set => _collectSessionWaits = value;
        }

        public int? TableSizeCollectionThresholdMB { get; set; }

        public int? TableSizeMaxDatabaseThreshold { get; set; }

        public string TableSizeDatabases { get; set; }

        public int? TableSizeMaxTableThreshold { get; set; }

        public DBADashSource(string source)
        {
            this.ConnectionString = source;
        }

        public DBADashSource()
        {
        }
    }
}