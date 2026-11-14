using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.DailyReport
{
    /// <summary>
    /// A scheduled daily health report, emailed to the recipients of its notification channels.
    /// Stored in the repository (dbo.DailyReport) so the service can send it without the GUI running.
    /// </summary>
    public class DailyReportConfig
    {
        public int DailyReportID { get; set; }
        public string ReportName { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Time of day the report is sent, in TimeZone - or the first time, when it repeats</summary>
        public TimeSpan ScheduleTime { get; set; } = new TimeSpan(8, 0, 0);
        public string TimeZone { get; set; } = "UTC";

        /// <summary>Days the report is sent: a bit per day, Monday 1 ... Sunday 64 (see <see cref="DayBit"/>)</summary>
        public int DaysOfWeek { get; set; } = AllDays;
        /// <summary>Null: once a day at ScheduleTime.  Otherwise every IntervalMinutes from ScheduleTime until WindowEndTime</summary>
        public int? IntervalMinutes { get; set; }
        /// <summary>The last time a repeating report can be sent.  Null: the end of the day</summary>
        public TimeSpan? WindowEndTime { get; set; }
        public DailyReportType ReportType { get; set; } = DailyReportType.Full;

        public const int AllDays = 127;
        public const int Weekdays = 31;
        public const int Weekends = 96;

        /// <summary>The report's bit for a day: Monday 1, Tuesday 2 ... Sunday 64</summary>
        public static int DayBit(DayOfWeek day) => 1 << (((int)day + 6) % 7);

        /// <summary>"08:00, every day", "Every hour 08:00-18:00, weekdays", "Every 30 minutes from 07:00, Mon, Wed, Fri"</summary>
        public string ScheduleDescription
        {
            get
            {
                var days = DaysText(DaysOfWeek);
                if (IntervalMinutes is not > 0) return $"{ScheduleTime:hh\\:mm}, {days}";
                var range = WindowEndTime.HasValue ? $"{ScheduleTime:hh\\:mm}-{WindowEndTime:hh\\:mm}" : $"from {ScheduleTime:hh\\:mm}";
                return $"{IntervalText(IntervalMinutes.Value)} {range}, {days}";
            }
        }

        public static string IntervalText(int minutes) => minutes switch
        {
            60 => "Every hour",
            _ when minutes % 60 == 0 => $"Every {minutes / 60} hours",
            _ => $"Every {minutes} minutes"
        };

        public static string DaysText(int daysOfWeek)
        {
            switch (daysOfWeek & AllDays)
            {
                case AllDays: return "every day";
                case Weekdays: return "weekdays";
                case Weekends: return "weekends";
                case 0: return "no days";
            }
            var names = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return string.Join(", ", names.Where((_, i) => (daysOfWeek & (1 << i)) != 0));
        }

        /// <summary>-1: all instances, &gt;0: single tag, -2: multiple tags in ApplyToTagIDs (same convention as alert rules)</summary>
        public int ApplyToTagID { get; set; } = -1;
        public string ApplyToTagIDs { get; set; }
        public string ScopeDescription { get; set; }

        public bool IncludePerformance { get; set; }
        public bool IncludeWeekOverWeek { get; set; }
        /// <summary>Relative change against the same time last week that is reported</summary>
        public int ChangeThresholdPct { get; set; } = 50;

        /// <summary>Everything else - charts, top queries, AI summary, baseline weeks, per-measure settings</summary>
        public DailyReportSettings Settings { get; set; } = new();

        public DateTime? LastRunUTC { get; set; }
        public string LastRunError { get; set; }
        /// <summary>The last send (scheduled or Send now) that reached at least one channel</summary>
        public DateTime? LastSentUTC { get; set; }
        public DateTime? LastSendAttemptUTC { get; set; }
        /// <summary>Each channel's outcome on the last send: the mail server's reply, or why it wasn't sent</summary>
        public string LastSendResult { get; set; }

        public List<DailyReportChannelRef> Channels { get; set; } = new();
        public List<int> ChannelIDs => Channels.Select(c => c.NotificationChannelID).ToList();
    }

    public enum DailyReportType
    {
        /// <summary>Everything in scope: checks, performance, resolved issues</summary>
        Full = 0,
        /// <summary>Only what changed since the last send - new, worse, better and resolved issues.  Nothing is sent when nothing changed</summary>
        Changes = 1
    }

    public class DailyReportChannelRef
    {
        public int NotificationChannelID { get; set; }
        public string ChannelName { get; set; }
        public string ChannelType { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Reads and writes daily reports in the repository database.</summary>
    public static class DailyReportRepository
    {
        public static List<DailyReportConfig> GetReports(string connectionString, int? dailyReportID = null)
        {
            var reports = new List<DailyReportConfig>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", (object)dailyReportID ?? DBNull.Value);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                reports.Add(new DailyReportConfig
                {
                    DailyReportID = r.GetInt32(r.GetOrdinal("DailyReportID")),
                    ReportName = r.GetString(r.GetOrdinal("ReportName")),
                    IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
                    ScheduleTime = (TimeSpan)r.GetValue(r.GetOrdinal("ScheduleTime")),
                    TimeZone = r.GetString(r.GetOrdinal("TimeZone")),
                    DaysOfWeek = Convert.ToInt32(r["DaysOfWeek"]),
                    IntervalMinutes = r["IntervalMinutes"] == DBNull.Value ? null : Convert.ToInt32(r["IntervalMinutes"]),
                    WindowEndTime = r["WindowEndTime"] == DBNull.Value ? null : (TimeSpan)r["WindowEndTime"],
                    ReportType = (DailyReportType)Convert.ToInt32(r["ReportType"]),
                    ApplyToTagID = r.GetInt32(r.GetOrdinal("ApplyToTagID")),
                    ApplyToTagIDs = r.IsDBNull(r.GetOrdinal("ApplyToTagIDs")) ? null : r.GetString(r.GetOrdinal("ApplyToTagIDs")),
                    ScopeDescription = r.GetString(r.GetOrdinal("ScopeDescription")),
                    IncludePerformance = r.GetBoolean(r.GetOrdinal("IncludePerformance")),
                    IncludeWeekOverWeek = r.GetBoolean(r.GetOrdinal("IncludeWeekOverWeek")),
                    ChangeThresholdPct = r.GetInt32(r.GetOrdinal("ChangeThresholdPct")),
                    LastRunUTC = r.IsDBNull(r.GetOrdinal("LastRunUTC")) ? null : r.GetDateTime(r.GetOrdinal("LastRunUTC")),
                    LastRunError = r.IsDBNull(r.GetOrdinal("LastRunError")) ? null : r.GetString(r.GetOrdinal("LastRunError")),
                    LastSentUTC = r.IsDBNull(r.GetOrdinal("LastSentUTC")) ? null : r.GetDateTime(r.GetOrdinal("LastSentUTC")),
                    LastSendAttemptUTC = r.IsDBNull(r.GetOrdinal("LastSendAttemptUTC")) ? null : r.GetDateTime(r.GetOrdinal("LastSendAttemptUTC")),
                    LastSendResult = r.IsDBNull(r.GetOrdinal("LastSendResult")) ? null : r.GetString(r.GetOrdinal("LastSendResult")),
                    Settings = DailyReportSettings.FromJson(r.IsDBNull(r.GetOrdinal("Settings")) ? null : r.GetString(r.GetOrdinal("Settings")))
                });
            }

            r.NextResult();
            while (r.Read())
            {
                var reportID = r.GetInt32(r.GetOrdinal("DailyReportID"));
                var report = reports.FirstOrDefault(x => x.DailyReportID == reportID);
                report?.Channels.Add(new DailyReportChannelRef
                {
                    NotificationChannelID = r.GetInt32(r.GetOrdinal("NotificationChannelID")),
                    ChannelName = r.GetString(r.GetOrdinal("ChannelName")),
                    ChannelType = r.GetString(r.GetOrdinal("NotificationChannelType")),
                    IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
                });
            }
            return reports;
        }

        /// <summary>Adds (DailyReportID = 0) or updates a report.  Returns the DailyReportID.</summary>
        public static int Save(string connectionString, DailyReportConfig config)
        {
            var channelIDs = new DataTable();
            channelIDs.Columns.Add("ID", typeof(int));
            foreach (var id in config.ChannelIDs.Distinct())
            {
                channelIDs.Rows.Add(id);
            }

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Upd", cn) { CommandType = CommandType.StoredProcedure };
            var pID = new SqlParameter("@DailyReportID", SqlDbType.Int)
            { Direction = ParameterDirection.InputOutput, Value = config.DailyReportID > 0 ? config.DailyReportID : DBNull.Value };
            cmd.Parameters.Add(pID);
            cmd.Parameters.AddWithValue("@ReportName", config.ReportName ?? string.Empty);
            cmd.Parameters.AddWithValue("@IsActive", config.IsActive);
            cmd.Parameters.AddWithValue("@ScheduleTime", config.ScheduleTime);
            cmd.Parameters.AddWithValue("@TimeZone", config.TimeZone);
            cmd.Parameters.AddWithValue("@DaysOfWeek", config.DaysOfWeek);
            cmd.Parameters.AddWithValue("@IntervalMinutes", (object)config.IntervalMinutes ?? DBNull.Value);
            cmd.Parameters.Add(new SqlParameter("@WindowEndTime", SqlDbType.Time) { Value = (object)config.WindowEndTime ?? DBNull.Value });
            cmd.Parameters.AddWithValue("@ReportType", (int)config.ReportType);
            cmd.Parameters.AddWithValue("@ApplyToTagID", config.ApplyToTagID);
            cmd.Parameters.AddWithValue("@ApplyToTagIDs", (object)config.ApplyToTagIDs ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IncludePerformance", config.IncludePerformance);
            cmd.Parameters.AddWithValue("@IncludeWeekOverWeek", config.IncludeWeekOverWeek);
            cmd.Parameters.AddWithValue("@ChangeThresholdPct", config.ChangeThresholdPct);
            cmd.Parameters.AddWithValue("@Settings", (config.Settings ?? new DailyReportSettings()).ToJson());
            cmd.Parameters.Add(new SqlParameter("@NotificationChannelIDs", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = channelIDs });
            cn.Open();
            cmd.ExecuteNonQuery();
            return (int)pID.Value;
        }

        public static void Delete(string connectionString, int dailyReportID)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>Returns the IDs of reports that are due and marks them as run.  Each report is returned once per local day.</summary>
        public static List<int> ClaimDueReports(string connectionString)
        {
            var ids = new List<int>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Claim", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                ids.Add(r.GetInt32(0));
            }
            return ids;
        }

        /// <param name="retry">Nothing was sent, so try again later the same day (see dbo.DailyReport_Claim)</param>
        public static void SetRunResult(string connectionString, int dailyReportID, string errorMessage, bool retry = false)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_RunResult_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Retry", retry);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>Takes the report's run lock for a Send now (dbo.DailyReport_RunLock_Upd).  Returns the lock to release, or null if a run holds it</summary>
        public static DateTime? AcquireRunLock(string connectionString, int dailyReportID)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_RunLock_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cmd.Parameters.AddWithValue("@Acquire", true);
            var lockParam = cmd.Parameters.Add(new SqlParameter("@LockUTC", SqlDbType.DateTime2) { Scale = 3, Direction = ParameterDirection.Output });
            cn.Open();
            cmd.ExecuteNonQuery();
            return lockParam.Value == DBNull.Value ? null : (DateTime)lockParam.Value;
        }

        /// <summary>Releases a lock taken by AcquireRunLock - only if it's still that lock</summary>
        public static void ReleaseRunLock(string connectionString, int dailyReportID, DateTime lockUtc)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_RunLock_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cmd.Parameters.AddWithValue("@Acquire", false);
            cmd.Parameters.Add(new SqlParameter("@LockUTC", SqlDbType.DateTime2) { Scale = 3, Value = lockUtc });
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>Records a send (scheduled or Send now): sent is true when it reached at least one channel</summary>
        public static void SetSendResult(string connectionString, int dailyReportID, bool sent, string result)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_SendResult_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cmd.Parameters.AddWithValue("@Sent", sent);
            cmd.Parameters.AddWithValue("@Result", (object)result ?? DBNull.Value);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>Critical and warning problems on individual databases, drives and jobs for the instances in scope</summary>
        public static List<DailyReportIssue> GetObjectIssues(string connectionString, string instanceList)
        {
            var issues = new List<DailyReportIssue>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Issues_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceList);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                issues.Add(new DailyReportIssue
                {
                    CheckGroup = r.GetString(r.GetOrdinal("CheckGroup")),
                    Status = r.GetInt32(r.GetOrdinal("Status")),
                    Instance = r.GetString(r.GetOrdinal("Instance")),
                    ObjectName = r.IsDBNull(r.GetOrdinal("ObjectName")) ? string.Empty : r.GetString(r.GetOrdinal("ObjectName")),
                    Detail = r.IsDBNull(r.GetOrdinal("Detail")) ? string.Empty : r.GetString(r.GetOrdinal("Detail"))
                });
            }
            return issues;
        }

        /// <summary>InstanceIDs in the report's scope (active, shown in the summary, matching its tags)</summary>
        public static List<int> GetInstanceIDs(string connectionString, DailyReportConfig config) =>
            GetInstanceIDs(connectionString, config.ApplyToTagID, config.ApplyToTagIDs);

        /// <summary>
        /// The instances the performance section covers: the report's instances, narrowed to the performance tag scope when it has one
        /// (DailyReportSettings.PerformanceApplyToTagID)
        /// </summary>
        public static List<int> GetPerformanceInstanceIDs(string connectionString, DailyReportSettings settings, List<int> reportInstanceIDs)
        {
            if (settings == null || settings.PerformanceApplyToTagID == -1) return reportInstanceIDs;
            var inScope = GetInstanceIDs(connectionString, settings.PerformanceApplyToTagID, settings.PerformanceApplyToTagIDs).ToHashSet();
            return reportInstanceIDs.Where(inScope.Contains).ToList();
        }

        public static List<int> GetInstanceIDs(string connectionString, int applyToTagID, string applyToTagIDs)
        {
            var ids = new List<int>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_InstanceIDs_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ApplyToTagID", applyToTagID);
            cmd.Parameters.AddWithValue("@ApplyToTagIDs", (object)applyToTagIDs ?? DBNull.Value);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                ids.Add(r.GetInt32(0));
            }
            return ids;
        }
    }
}

namespace DBADash.DailyReport
{
    public static class DailyReportLookups
    {
        /// <summary>Notification channels that can be chosen for a daily report</summary>
        public static List<DailyReportChannelRef> GetChannels(string connectionString)
        {
            var channels = new List<DailyReportChannelRef>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannel_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                channels.Add(new DailyReportChannelRef
                {
                    NotificationChannelID = r.GetInt32(r.GetOrdinal("NotificationChannelID")),
                    ChannelName = r.GetString(r.GetOrdinal("ChannelName")),
                    ChannelType = r.GetString(r.GetOrdinal("NotificationChannelType")),
                    IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
                });
            }
            return channels.OrderBy(c => c.ChannelName).ToList();
        }

        /// <summary>Time zone names accepted by the repository (sys.time_zone_info)</summary>
        public static List<string> GetTimeZones(string connectionString)
        {
            var zones = new List<string>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("SELECT name FROM sys.time_zone_info ORDER BY name", cn);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                zones.Add(r.GetString(0));
            }
            return zones;
        }
    }
}
