using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DBADash.PerformanceInsights
{
    public sealed class InsightAlert
    {
        public bool IsOpen { get; init; }
        public string AlertType { get; init; }
        public string AlertKey { get; init; }
        /// <summary>0 Critical, 1-10 High, 11-20 Medium, 21+ Low - see DBADashAlerts.Alert.Priorities</summary>
        public int Priority { get; init; }
        public string Message { get; init; }
        public DateTime TriggerDateUtc { get; init; }
        public DateTime? ResolvedDateUtc { get; init; }
        public bool IsAcknowledged { get; init; }
        public bool IsBlackout { get; init; }
    }

    public sealed class InsightOfflinePeriod
    {
        public DateTime FirstFailUtc { get; init; }
        public DateTime LastFailUtc { get; init; }
        public bool IsCurrent { get; init; }
        public int FailCount { get; init; }
    }

    public sealed class InsightRoleChange
    {
        public string AvailabilityGroup { get; init; }
        public string PreviousRole { get; init; }
        public string NewRole { get; init; }
        public DateTime ChangedUtc { get; init; }
    }

    public sealed class InsightConfigChange
    {
        /// <summary>Configuration, Trace flag, Patching or Schema</summary>
        public string ChangeType { get; init; }
        public string Name { get; init; }
        public string OldValue { get; init; }
        public string NewValue { get; init; }
        public DateTime ChangeDateUtc { get; init; }
        /// <summary>Objects created, modified or dropped - Schema only</summary>
        public int? Count { get; init; }
    }

    public sealed class InsightJobRun
    {
        public string JobName { get; init; }
        public DateTime StartUtc { get; init; }
        public int DurationSec { get; init; }
        /// <summary>sysjobhistory run_status: 0 failed, 1 succeeded, 2 retry, 3 cancelled, 4 in progress</summary>
        public int RunStatus { get; init; }
        public DateTime EndUtc => StartUtc.AddSeconds(DurationSec);
        public bool Failed => RunStatus is 0 or 3;
    }

    public sealed class InsightCollectionError
    {
        public string Source { get; init; }
        public int Errors { get; init; }
    }

    /// <summary>
    /// Tempdb page latch contention seen in the running queries snapshots in a range.  Allocation is PFS/GAM/SGAM pages; metadata is any other
    /// tempdb page - usually system tables, from creating and dropping temp tables quickly
    /// </summary>
    public sealed class InsightTempdbContention
    {
        public int Snapshots { get; init; }
        /// <summary>Snapshots from an agent new enough to capture the contention - 0 means it can't be judged</summary>
        public int SnapshotsWithTypes { get; init; }
        public int AllocationSnapshots { get; init; }
        public int MaxAllocationWaiting { get; init; }
        public long MaxAllocationWaitMs { get; init; }
        public DateTime? WorstAllocationSnapshotUtc { get; init; }
        public int MetadataSnapshots { get; init; }
        public int MaxMetadataWaiting { get; init; }
        public long MaxMetadataWaitMs { get; init; }
        public DateTime? WorstMetadataSnapshotUtc { get; init; }
    }

    /// <summary>A performance counter outside its thresholds - from dbo.PerformanceCounterSummary_Get</summary>
    public sealed class InsightCounter
    {
        public string ObjectName { get; init; }
        public string CounterName { get; init; }
        public string InstanceName { get; init; }
        /// <summary>1 Critical, 2 Warning - judged on the minimum, maximum and average over the range (not the current value)</summary>
        public int Status { get; init; }
        /// <summary>Which figure is out: Avg, Max or Min</summary>
        public string Measure { get; init; }
        public double Value { get; init; }

        public string DisplayName => string.IsNullOrEmpty(InstanceName) ? CounterName : $"{CounterName} ({InstanceName})";
    }

    /// <summary>What happened on an instance in a date range - dbo.PerformanceInsights_Get</summary>
    public sealed class PerformanceInsightsContext
    {
        public string InstanceName { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public bool IsAzureDB { get; set; }
        public List<InsightAlert> Alerts { get; } = new();
        public List<InsightOfflinePeriod> Offline { get; } = new();
        public List<InsightRoleChange> RoleChanges { get; } = new();
        public List<InsightConfigChange> ConfigChanges { get; } = new();
        public List<InsightJobRun> Jobs { get; } = new();
        public List<InsightCollectionError> CollectionErrors { get; } = new();
        /// <summary>Null when the range has no running queries snapshots</summary>
        public InsightTempdbContention Tempdb { get; set; }
    }

    public static class PerformanceInsightsRepository
    {
        public static PerformanceInsightsContext GetContext(string connectionString, int instanceId, DateTime fromUtc, DateTime toUtc, int commandTimeout = 60)
        {
            var result = new PerformanceInsightsContext();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.PerformanceInsights_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = fromUtc });
            cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = toUtc });
            cn.Open();
            using var r = cmd.ExecuteReader();

            if (r.Read())
            {
                result.InstanceName = r["InstanceDisplayName"] as string;
                result.StartTimeUtc = Date(r, "StartTimeUtc");
                result.IsAzureDB = r["IsAzureDB"] is true;
            }

            r.NextResult();
            while (r.Read())
            {
                result.Alerts.Add(new InsightAlert
                {
                    IsOpen = (string)r["Source"] == "Active" && r["IsResolved"] is false,
                    AlertType = r["AlertType"] as string,
                    AlertKey = r["AlertKey"] as string,
                    Priority = Convert.ToInt32(r["Priority"]),
                    Message = r["Message"] as string,
                    TriggerDateUtc = Date(r, "TriggerDate") ?? DateTime.MinValue,
                    ResolvedDateUtc = Date(r, "ResolvedDate") ?? Date(r, "ClosedDate"),
                    IsAcknowledged = r["IsAcknowledged"] is true,
                    IsBlackout = r["IsBlackout"] is true
                });
            }

            r.NextResult();
            while (r.Read())
            {
                result.Offline.Add(new InsightOfflinePeriod
                {
                    FirstFailUtc = Date(r, "FirstFail") ?? DateTime.MinValue,
                    LastFailUtc = Date(r, "LastFail") ?? DateTime.MinValue,
                    IsCurrent = r["IsCurrent"] is true,
                    FailCount = Convert.ToInt32(r["FailCount"])
                });
            }

            r.NextResult();
            while (r.Read())
            {
                result.RoleChanges.Add(new InsightRoleChange
                {
                    AvailabilityGroup = r["AvailabilityGroupName"] as string,
                    PreviousRole = r["PreviousRoleDesc"] as string,
                    NewRole = r["NewRoleDesc"] as string,
                    ChangedUtc = Date(r, "ChangedBefore") ?? DateTime.MinValue
                });
            }

            r.NextResult();
            while (r.Read())
            {
                result.ConfigChanges.Add(new InsightConfigChange
                {
                    ChangeType = r["ChangeType"] as string,
                    Name = r["Name"] as string,
                    OldValue = (r["OldValue"] as string)?.Trim(),
                    NewValue = (r["NewValue"] as string)?.Trim(),
                    ChangeDateUtc = Date(r, "ChangeDate") ?? DateTime.MinValue,
                    Count = r["Count"] == DBNull.Value ? null : Convert.ToInt32(r["Count"])
                });
            }

            r.NextResult();
            while (r.Read())
            {
                result.Jobs.Add(new InsightJobRun
                {
                    JobName = r["JobName"] as string ?? "(unknown job)",
                    StartUtc = Date(r, "RunDateTime") ?? DateTime.MinValue,
                    DurationSec = r["RunDurationSec"] == DBNull.Value ? 0 : Convert.ToInt32(r["RunDurationSec"]),
                    RunStatus = r["RunStatus"] == DBNull.Value ? -1 : Convert.ToInt32(r["RunStatus"])
                });
            }

            r.NextResult();
            while (r.Read())
            {
                result.CollectionErrors.Add(new InsightCollectionError
                {
                    Source = r["ErrorSource"] as string,
                    Errors = Convert.ToInt32(r["Errors"])
                });
            }

            if (r.NextResult() && r.Read() && Convert.ToInt32(r["Snapshots"]) > 0)
            {
                int Int(string column) => r[column] == DBNull.Value ? 0 : Convert.ToInt32(r[column]);
                long Long(string column) => r[column] == DBNull.Value ? 0 : Convert.ToInt64(r[column]);
                result.Tempdb = new InsightTempdbContention
                {
                    Snapshots = Int("Snapshots"),
                    SnapshotsWithTypes = Int("SnapshotsWithTypes"),
                    AllocationSnapshots = Int("AllocationSnapshots"),
                    MaxAllocationWaiting = Int("MaxAllocationWaiting"),
                    MaxAllocationWaitMs = Long("MaxAllocationWaitMs"),
                    WorstAllocationSnapshotUtc = Date(r, "WorstAllocationSnapshot"),
                    MetadataSnapshots = Int("MetadataSnapshots"),
                    MaxMetadataWaiting = Int("MaxMetadataWaiting"),
                    MaxMetadataWaitMs = Long("MaxMetadataWaitMs"),
                    WorstMetadataSnapshotUtc = Date(r, "WorstMetadataSnapshot")
                };
            }
            return result;
        }

        /// <summary>
        /// Performance counters outside their warning or critical thresholds over the range, judged on their minimum, maximum and average -
        /// not their current value, which isn't in a past range (so it isn't read)
        /// </summary>
        /// <param name="hours">The hours of the day (0-23, local) the time filter keeps, or null for all - as the charts and the figures count</param>
        /// <param name="daysOfWeek">The days of the week (1 = Monday) the time filter keeps, or null for all</param>
        /// <param name="utcOffset">Minutes from UTC of the local time the filter is in</param>
        public static List<InsightCounter> GetCounters(string connectionString, int instanceId, DateTime fromUtc, DateTime toUtc, int commandTimeout = 60,
            IEnumerable<int> hours = null, IEnumerable<int> daysOfWeek = null, int utcOffset = 0)
        {
            var list = new List<InsightCounter>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = fromUtc });
            cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = toUtc });
            cmd.Parameters.AddWithValue("@ShowCollectionMetrics", false);
            // Not judged here, and a read per counter
            cmd.Parameters.AddWithValue("@IncludeCurrentValue", false);
            if (hours != null) cmd.Parameters.Add(new SqlParameter("@Hours", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = IDs(hours) });
            if (daysOfWeek != null) cmd.Parameters.Add(new SqlParameter("@DaysOfWeek", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = IDs(daysOfWeek) });
            cmd.Parameters.AddWithValue("@UTCOffset", utcOffset);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                // Worst of the range's figures: critical (1) beats warning (2).  3/4 are OK/good
                var candidates = new[] { ("Avg", "AvgValueStatus", "AvgValue"), ("Max", "MaxValueStatus", "MaxValue"), ("Min", "MinValueStatus", "MinValue") };
                (string Measure, int Status, double Value)? worst = null;
                foreach (var (measure, statusCol, valueCol) in candidates)
                {
                    if (r[statusCol] == DBNull.Value || r[valueCol] == DBNull.Value) continue;
                    var status = Convert.ToInt32(r[statusCol]);
                    if (status is not (1 or 2)) continue;
                    if (worst == null || status < worst.Value.Status)
                    {
                        worst = (measure, status, Convert.ToDouble(r[valueCol], CultureInfo.InvariantCulture));
                    }
                }
                if (worst == null) continue;
                list.Add(new InsightCounter
                {
                    ObjectName = r["object_name"] as string,
                    CounterName = r["counter_name"] as string,
                    InstanceName = r["instance_name"] as string,
                    Status = worst.Value.Status,
                    Measure = worst.Value.Measure,
                    Value = worst.Value.Value
                });
            }
            return list;
        }

        /// <summary>A dbo.IDs table-valued parameter</summary>
        private static DataTable IDs(IEnumerable<int> ids)
        {
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            foreach (var id in ids.Distinct()) table.Rows.Add(id);
            return table;
        }

        private static DateTime? Date(SqlDataReader r, string column)
        {
            var v = r[column];
            return v == DBNull.Value ? null : DateTime.SpecifyKind(Convert.ToDateTime(v), DateTimeKind.Utc);
        }
    }
}
