using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Printing.IndexedProperties;

namespace DBADashGUI.Performance
{
    public class RunningQueriesFilters
    {
        [Category("String Filters (LIKE)")]
        [DisplayName("Application Name")]
        public string AppName { get; set; }

        [Category("String Filters (=)")]
        [DisplayName("Database Name")]
        public string DatabaseName { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Login Name")]
        public string LoginName { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Wait Type")]
        [Description("Type of wait. e.g. PAGEIOLATCH%, RESOURCE_SEMAPHORE")]
        public string WaitType { get; set; }

        [Category("Date Range")]
        [DisplayName("Date From")]
        public DateTime From { get; set; }

        [Category("Date Range")]
        [DisplayName("Date To")]
        public DateTime To { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Wait Resource")]
        [Description("Wait Resource.  e.g. 2% to filter for tempdb")]
        public string WaitResource { get; set; }

        [Browsable(false)] public int InstanceID { get; set; }

        [Browsable(false)] public int Skip { get; set; }

        [Browsable(false)]
        public Guid? JobID { get; set; }

        [Category("Integers")]
        [DisplayName("Session ID")]
        public int? SessionID { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Object Name")]
        [Description("Includes schema.  e.g. dbo.MyProc")]
        public string ObjectName { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Batch Text")]
        public string BatchText { get; set; }

        [Category("String Filters (LIKE)")]
        public string Text { get; set; }

        [Category("String Filters (LIKE)")]
        [DisplayName("Host Name")]
        public string HostName { get; set; }

        [Category("String Filters (=)")]
        [Description("e.g. runnable, running, rollback, sleeping, suspended")]
        public string Status { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Duration (ms)")]
        public long? MinDurationMs { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Physical Reads")]
        public long? MinReads { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Writes")]
        public long? MinWrites { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Logical Reads")]
        public long? MinLogicalReads { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum CPU (ms)")]
        public int? MinCPUMs { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Memory Grant KB")]
        public long? MinMemoryGrantKB { get; set; }

        [Category("Boolean")]
        [DisplayName("Blocked Or Blocking Only")]
        public bool BlockedOrBlocking { get; set; }

        [Category("Integers")]
        [Description("Maximum number of rows to return")]
        public int? Top { get; set; }

        [Category("Integers")]
        [DisplayName("Minimum Sleeping Session Idle Time (sec)")]
        public int? MinSleepingSessionIdleTimeSec { get; set; }

        [Category("Boolean")]
        [DisplayName("Has Open Transaction")]
        public bool? HasOpenTransaction { get; set; }

        [Category("String Filters (=)")]
        [DisplayName("Context Info")]
        [Description("Enter context info as hex string.  e.g. 0x4400420041004400610073006800")]
        public string ContextInfoSting
        {
            get => ContextInfo == null || ContextInfo.Length == 0 ? null : Common.ByteArrayToString(ContextInfo);
            set => ContextInfo = Convert.FromHexString(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? value[2..]
                : value);
        }

        [Category("String Filters (=)")]
        [DisplayName("Query Hash")]
        public string QueryHashString
        {
            get => QueryHash == null || QueryHash.Length == 0 ? null : Common.ByteArrayToString(QueryHash);
            set => QueryHash = Convert.FromHexString(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? value[2..]
                : value);
        }

        [Category("String Filters (=)")]
        [DisplayName("Query Plan Hash")]
        public string QueryPlanHashString
        {
            get => QueryPlanHash == null || QueryPlanHash.Length == 0 ? null : Common.ByteArrayToString(QueryPlanHash);
            set => QueryPlanHash = Convert.FromHexString(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? value[2..]
                : value);
        }

        [Category("String Filters (=)")]
        [DisplayName("SQL Handle")]
        public string SQLHandleString
        {
            get => SQLHandle == null || SQLHandle.Length == 0 ? null : Common.ByteArrayToString(SQLHandle);
            set => SQLHandle = Convert.FromHexString(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? value[2..]
                : value);
        }

        [Category("String Filters (=)")]
        [DisplayName("Plan Handle")]
        public string PlanHandleString
        {
            get => PlanHandle == null || PlanHandle.Length == 0 ? null : Common.ByteArrayToString(PlanHandle);
            set => PlanHandle = Convert.FromHexString(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? value[2..]
                : value);
        }

        public byte[] ContextInfo;

        public byte[] QueryHash;

        public byte[] QueryPlanHash;

        public byte[] SQLHandle;

        public byte[] PlanHandle;

        public SqlParameter[] GetParameters()
        {
            return new SqlParameter[]
            {
                new SqlParameter("AppName", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(AppName) ? DBNull.Value : AppName },
                new SqlParameter("DatabaseName", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(DatabaseName) ? DBNull.Value : DatabaseName },
                new SqlParameter("LoginName", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(LoginName) ? DBNull.Value : LoginName },
                new SqlParameter("WaitType", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(WaitType) ? DBNull.Value : WaitType },
                new SqlParameter("SnapshotDateFrom", SqlDbType.DateTime2) { Value = From == DateTime.MaxValue || From == DateTime.MinValue ? DBNull.Value : From, Direction = ParameterDirection.InputOutput },
                new SqlParameter("SnapshotDateTo", SqlDbType.DateTime2) { Value = To == DateTime.MaxValue || To == DateTime.MinValue ? DBNull.Value : To },
                new SqlParameter("WaitResource", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(WaitResource) ? DBNull.Value : WaitResource },
                new SqlParameter("InstanceID", SqlDbType.Int) { Value = InstanceID },
                new SqlParameter("Skip", SqlDbType.Int) { Value = Skip },
                new SqlParameter("JobID", SqlDbType.UniqueIdentifier) { Value = JobID == Guid.Empty ? DBNull.Value : JobID },
                new SqlParameter("SessionID", SqlDbType.Int) { Value = SessionID == 0 ? DBNull.Value : SessionID },
                new SqlParameter("ObjectName", SqlDbType.NVarChar) {Value = string.IsNullOrEmpty(ObjectName) ? DBNull.Value : ObjectName},
                new SqlParameter("BatchText", SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(BatchText)? DBNull.Value : BatchText},
                new SqlParameter("Text", SqlDbType.NVarChar){Value = string.IsNullOrEmpty(Text) ? DBNull.Value : Text},
                new SqlParameter("HostName", SqlDbType.NVarChar) {Value = string.IsNullOrEmpty(HostName) ? DBNull.Value : HostName},
                new SqlParameter("Status", SqlDbType.NVarChar) {Value = string.IsNullOrEmpty(Status) ? DBNull.Value : Status},
                new SqlParameter("MinDurationMs", SqlDbType.BigInt) {Value = MinDurationMs>0 ? MinDurationMs :  DBNull.Value},
                new SqlParameter("MinReads", SqlDbType.BigInt) {Value = MinReads>0 ? MinReads : DBNull.Value},
                new SqlParameter("MinWrites", SqlDbType.BigInt) {Value = MinWrites>0 ? MinWrites : DBNull.Value},
                new SqlParameter("MinLogicalReads", SqlDbType.BigInt) {Value = MinLogicalReads>0 ? MinLogicalReads : DBNull.Value},
                new SqlParameter("MinCPUMs", SqlDbType.BigInt) {Value = MinCPUMs>0 ? MinCPUMs : DBNull.Value},
                new SqlParameter("MinMemoryGrantKB", SqlDbType.BigInt) {Value = MinMemoryGrantKB>0 ? MinMemoryGrantKB : DBNull.Value},
                new SqlParameter("BlockedOrBlocking", SqlDbType.Bit){Value = BlockedOrBlocking},
                new SqlParameter("MinSleepingSessionIdleTimeSec", SqlDbType.Int) {Value = MinSleepingSessionIdleTimeSec==null? DBNull.Value : MinSleepingSessionIdleTimeSec},
                new SqlParameter("HasOpenTransaction",SqlDbType.Bit) {Value = HasOpenTransaction==null ? DBNull.Value : HasOpenTransaction },
                new SqlParameter("ContextInfo", SqlDbType.VarBinary){Value = ContextInfo==null || ContextInfo.Length==0 ? DBNull.Value : ContextInfo},
                new SqlParameter("QueryHash", SqlDbType.VarBinary){Value = QueryHash==null || QueryHash.Length==0 ? DBNull.Value :QueryHash},
                new SqlParameter("QueryPlanHash", SqlDbType.VarBinary){Value = QueryPlanHash==null || QueryPlanHash.Length==0 ? DBNull.Value :QueryPlanHash},
                new SqlParameter("SQLHandle", SqlDbType.VarBinary){Value = SQLHandle==null || SQLHandle.Length==0 ? DBNull.Value :SQLHandle},
                new SqlParameter("PlanHandle", SqlDbType.VarBinary){Value = PlanHandle == null || PlanHandle.Length == 0 ? DBNull.Value :PlanHandle},
                new SqlParameter("Top", SqlDbType.Int){ Value = Top==null ? DBNull.Value : Top},
                new SqlParameter("HasCursors", SqlDbType.Bit) { Value = 0, Direction= ParameterDirection.Output }
            };
        }

        public SqlParameter[] GetNonDefaultParameters() => GetParameters().Where(p => p.Value != DBNull.Value || p.ParameterName == "SnapshotDateFrom").ToArray();
    }
}