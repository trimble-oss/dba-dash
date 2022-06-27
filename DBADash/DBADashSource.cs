using DBADashService;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class DBADashSource
    {
        private Int32 slowQueryThresholdMs = -1;

        private CollectionSchedules collectionSchedules;
        private PlanCollectionThreshold runningQueryPlanThreshold;
        private string schemaSnapshotDBs;
        private bool noWMI;
        private Int32 slowQuerySessionMaxMemoryKB = 4096;
        private Int32 slowQueryTargetMaxMemoryKB = -1;
        private bool persistXESessions = false;
        private bool useDualXESesion = true;

        public string ConnectionID { get; set; }

        public  CollectionSchedules CollectionSchedules {
            get
            {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return collectionSchedules;
                }
                else
                {
                    return null;
                }
            }
            set {
                 collectionSchedules = value;
            }
        }

        public PlanCollectionThreshold RunningQueryPlanThreshold {
            get {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return runningQueryPlanThreshold;
                }
                else
                {
                    return null;
                }
            }
            set {
              
                    runningQueryPlanThreshold = value;
            }
        }

        public string SchemaSnapshotDBs { 
            get {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return schemaSnapshotDBs;
                }
                else
                {
                    return string.Empty;
                }
            } 
            set {
                    schemaSnapshotDBs = value;
            } 
        }

        [JsonIgnore]
        public bool HasCustomSchedule
        {
            get
            {
                return !(CollectionSchedules == null || CollectionSchedules.Count == 0);
            }
        }
        #region "Plan Collection Threshold Properties"
        // Added plan collection properties to allow them to be visible/editable in the grid

        [JsonIgnore]
        public bool PlanCollectionEnabled
        {
            get
            {
                if (RunningQueryPlanThreshold == null)
                {
                    return false;
                }
                else
                {
                    return RunningQueryPlanThreshold.PlanCollectionEnabled;
                }
            }
            set
            {
                if (value)
                {
                    RunningQueryPlanThreshold = PlanCollectionThreshold.DefaultThreshold;
                }
                else
                {
                    RunningQueryPlanThreshold = null;
                }
            }
        }

        [JsonIgnore]
        public int PlanCollectionCPUThreshold
        {
            get
            {
                if (RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    return RunningQueryPlanThreshold.CPUThreshold;
                }
            }
            set
            {
                if (RunningQueryPlanThreshold == null)
                {
                    RunningQueryPlanThreshold = PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                }
                RunningQueryPlanThreshold.CPUThreshold = value;
            }
        }
        [JsonIgnore]
        public int PlanCollectionMemoryGrantThreshold
        {
            get
            {
                if (RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    return RunningQueryPlanThreshold.MemoryGrantThreshold;
                }
            }
            set
            {
                if (RunningQueryPlanThreshold == null)
                {
                    RunningQueryPlanThreshold = PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                }
                RunningQueryPlanThreshold.MemoryGrantThreshold = value;
                
            }
        }
        [JsonIgnore]
        public int PlanCollectionDurationThreshold
        {
            get
            {
                if (RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    return RunningQueryPlanThreshold.DurationThreshold;
                }
            }
            set
            {
                if (RunningQueryPlanThreshold == null)
                {
                    RunningQueryPlanThreshold = PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                }
                RunningQueryPlanThreshold.DurationThreshold = value;                
            }
        }

        [JsonIgnore]
        public int PlanCollectionCountThreshold
        {
            get
            {
                if (RunningQueryPlanThreshold == null || SourceConnection.Type != ConnectionType.SQL)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    return RunningQueryPlanThreshold.CountThreshold;
                }
            }
            set
            {
                if (RunningQueryPlanThreshold == null)
                {
                    RunningQueryPlanThreshold = PlanCollectionThreshold.PlanCollectionDisabledThreshold;
                }
                RunningQueryPlanThreshold.CountThreshold = value;             
            }
        }

        #endregion

        [JsonIgnore]
        public DBADashConnection SourceConnection { get; set; }


        public string GetSource()
        {
            return SourceConnection.ConnectionString;
        }

        // Note if source is SQL connection string, password is encrypted.  Use GetSource() to return with real password
        public string ConnectionString
        {
            get
            {
                if (SourceConnection == null)
                {
                    return "";
                }
                else
                {
                    return SourceConnection.EncryptedConnectionString;
                }
            }
            set
            {
                SourceConnection = new DBADashConnection(value);
            }
        }

        public string GenerateFileName(string connection)
        {
            return "DBADash_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmm_ss") + "_" + connection + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("/","-") + ".xml";
        }

        [DefaultValue(false)]
        public bool NoWMI { 
            get {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return noWMI;
                }
                else
                {
                    return false;
                }
            } 
            set {
             
                    noWMI = value;
            } 
        }


        public Int32 SlowQuerySessionMaxMemoryKB { 
            get {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return slowQuerySessionMaxMemoryKB;
                }
                else
                {
                    return 0;
                }
            } 
            set {            
                    slowQuerySessionMaxMemoryKB = value;
            } 
        }

        public Int32 SlowQueryTargetMaxMemoryKB
        {
            get
            {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return slowQueryTargetMaxMemoryKB;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                slowQueryTargetMaxMemoryKB = value;
            }
        }

        [DefaultValue(true)]
        public bool UseDualEventSession {
            get {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return useDualXESesion;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                    useDualXESesion = value;
            }
        }
       

        [DefaultValue(-1)]
        public Int32 SlowQueryThresholdMs
        {
            get
            {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return slowQueryThresholdMs;
                }
                else
                {
                    return -1;
                }
                
             }
            set {           
                    slowQueryThresholdMs = value;             
            }
        }
        [DefaultValue(false)]
        public bool PersistXESessions { 
            get
            {
                if (SourceConnection != null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return persistXESessions;
                }
                else
                {
                    return false;
                }
                
            }
            set {
              
                    persistXESessions = value;
            }
        }

        private bool _collectSessionWaits = true;

        public bool CollectSessionWaits
        {
            get
            {
                if(SourceConnection!=null && SourceConnection.Type == ConnectionType.SQL)
                {
                    return _collectSessionWaits;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                _collectSessionWaits = value;
            }
        }

        public DBADashSource(string source)
        {
            this.ConnectionString = source;
        }
        public DBADashSource()
        {

        }


    }

}
