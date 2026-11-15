using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.DailyReport
{
    /// <summary>One hour of one period for one instance, from dbo.DailyReport_Performance_Get.  Null when the source has no rows for the hour.</summary>
    public class PerformanceHour
    {
        /// <summary>0: the last 24 hours.  n: the same 24 hours n weeks earlier</summary>
        public int Period { get; init; }
        /// <summary>Hours from the start of the period's window (0-23)</summary>
        public int HourOffset { get; init; }

        public double? AvgCPU { get; init; }
        public double? MaxCPU { get; init; }
        /// <summary>CPU samples at 80% or more</summary>
        public long? HighCPUSamples { get; init; }
        public long? CPUSamples { get; init; }

        public long? Reads { get; init; }
        public long? Writes { get; init; }
        public long? ReadStallMs { get; init; }
        public long? WriteStallMs { get; init; }
        public long? ReadBytes { get; init; }
        public long? WriteBytes { get; init; }
        public long? IOSampleMs { get; init; }

        public long? WaitMs { get; init; }
        public long? CriticalWaitMs { get; init; }
        public long? LockWaitMs { get; init; }
        public long? WaitSampleMs { get; init; }

        public int? BlockingSnapshots { get; init; }
        public int? MaxBlockedSessions { get; init; }
        public long? MaxBlockedWaitMs { get; init; }
        public int? Deadlocks { get; init; }
        public int? SlowQueries { get; init; }
        public int? FailedSlowQueries { get; init; }
    }

    /// <summary>Wait time of one wait type in one period (the 24 hours)</summary>
    public class PerformanceWaitType
    {
        public int Period { get; init; }
        public string WaitType { get; init; }
        public bool IsCriticalWait { get; init; }
        public long WaitMs { get; init; }
    }

    public class InstancePerformanceData
    {
        public int InstanceID { get; init; }
        public string Instance { get; init; }
        public bool IsAzureDB { get; init; }
        public List<PerformanceHour> Hours { get; } = new();
        public List<PerformanceWaitType> WaitTypes { get; } = new();
    }

    /// <summary>dbo.PerformanceThresholds - the thresholds the Performance Summary colours by</summary>
    public class PerformanceThresholdValues
    {
        public double CPUCritical { get; init; } = 90;
        public double CPUWarning { get; init; } = 75;
        public double ReadLatencyCritical { get; init; } = 50;
        public double ReadLatencyWarning { get; init; } = 10;
        public double WriteLatencyCritical { get; init; } = 50;
        public double WriteLatencyWarning { get; init; } = 10;
        /// <summary>Latency is ignored below this many IOPs - a few slow IOs on an idle instance aren't a problem</summary>
        public double MinIOPs { get; init; } = 100;
        public double CriticalWaitCritical { get; init; } = 1000;
        public double CriticalWaitWarning { get; init; } = 10;
    }

    public class PerformanceData
    {
        /// <summary>End of the window (UTC, on the hour).  Period n covers [WindowEndUtc - WindowHours - 7n days, WindowEndUtc - 7n days)</summary>
        public DateTime WindowEndUtc { get; init; }
        /// <summary>Whole hours in the window: 24 for the daily report, the hours of the date range for the Performance tab's insights</summary>
        public int WindowHours { get; init; } = 24;
        public DateTime WindowStartUtc => WindowEndUtc.AddHours(-WindowHours);
        public int BaselineWeeks { get; init; }
        public List<InstancePerformanceData> Instances { get; } = new();
        public PerformanceThresholdValues Thresholds { get; set; } = new();

        /// <summary>
        /// The (Period, HourOffset) hours performance is counted in, when the report counts only certain hours of the day
        /// (DailyReportSettings.PerformanceHoursFrom/To).  Null: every hour.  Hours keep their offsets, so charts still draw all 24
        /// </summary>
        public HashSet<(int Period, int HourOffset)> IncludedHours { get; set; }

        public bool IsIncluded(int period, int hourOffset) => IncludedHours == null || IncludedHours.Contains((period, hourOffset));

        /// <summary>
        /// How much each (Period, HourOffset) hour counts in the analysis, when a range covers only part of some hours - see RangeWeights.  Null,
        /// or an hour not in it: all of it (1).  The daily report counts whole days, so it leaves this null
        /// </summary>
        public IReadOnlyDictionary<(int Period, int HourOffset), double> HourWeights { get; init; }

        public double Weight(int period, int hourOffset) =>
            HourWeights != null && HourWeights.TryGetValue((period, hourOffset), out var w) ? w : 1;

        /// <summary>
        /// Weights that make the hours add up to a date range that isn't whole hours, so each part of it is compared with the baseline for its
        /// own hour.  For each hour, the minutes of the range in it: the window's hour counts that share of the data it has so far (all of an
        /// hour in progress, as its data is only that far), and each baseline week's hour that share of a whole hour.  At 15:50 with a range
        /// of 14:50-15:50, the 14:00 hour counts 10/60 against 10/60 of its baseline, and the 15:00 hour all of its 50 minutes against 50/60
        /// of a whole hour.  The hourly rollups can't be split, so a part of an hour stands in for its share of the hour's figures
        /// </summary>
        public static Dictionary<(int Period, int HourOffset), double> RangeWeights(DateTime windowStartUtc, int windowHours, int baselineWeeks,
            DateTime fromUtc, DateTime toUtc, DateTime nowUtc)
        {
            var weights = new Dictionary<(int, int), double>();
            for (var hour = 0; hour < windowHours; hour++)
            {
                var start = windowStartUtc.AddHours(hour);
                var end = start.AddHours(1);
                var inRange = Minutes(Max(start, fromUtc), Min(end, toUtc));
                var collected = Minutes(start, Min(end, nowUtc));
                var current = inRange <= 0 || collected <= 0 ? 0 : Math.Min(1, inRange / collected);
                var baseline = Math.Max(0, inRange) / 60.0;
                weights[(0, hour)] = current;
                for (var period = 1; period <= baselineWeeks; period++) weights[(period, hour)] = baseline;
            }
            return weights;

            static double Minutes(DateTime from, DateTime to) => Math.Max(0, (to - from).TotalMinutes);
            static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
            static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
        }

        /// <summary>A copy with <paramref name="weights"/> - the figures are shared, as they may be cached for other readers</summary>
        public PerformanceData WithHourWeights(IReadOnlyDictionary<(int Period, int HourOffset), double> weights)
        {
            var copy = new PerformanceData
            {
                WindowEndUtc = WindowEndUtc,
                WindowHours = WindowHours,
                BaselineWeeks = BaselineWeeks,
                Thresholds = Thresholds,
                IncludedHours = IncludedHours,
                PeriodShiftHours = PeriodShiftHours,
                HourWeights = weights
            };
            copy.Instances.AddRange(Instances);
            return copy;
        }

        /// <summary>The end of the 24 hours a report run at nowUtc covers: the hour, rounded down</summary>
        public static DateTime WindowEndFor(DateTime nowUtc) => new(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The hours of each period that fall in the settings' hours of the day, in the report's time zone.  Worked out per period, so a
        /// baseline week either side of a clock change still covers 09:00-17:00 local time.  Null when the report counts the whole day
        /// </summary>
        public static HashSet<(int Period, int HourOffset)> HoursFor(DateTime windowEndUtc, int baselineWeeks, DailyReportSettings settings, string timeZone)
        {
            if (settings == null || !settings.HasPerformanceHours) return null;
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone ?? "UTC");
            }
            catch (Exception)
            {
                tz = TimeZoneInfo.Utc;
            }
            return HoursFor(windowEndUtc, 24, baselineWeeks, tz, local => settings.IsPerformanceHour(local.Hour));
        }

        /// <summary>
        /// The hours of each period whose start, in <paramref name="tz"/>, <paramref name="include"/> keeps - e.g. the hours of the day and days
        /// of the week a filter allows.  Worked out per period, so a baseline week either side of a clock change keeps the same local hours
        /// </summary>
        /// <param name="periodShiftHours">Each baseline period's shift from exactly 7 days a week (PeriodShifts) - the hours are counted from the shifted start</param>
        public static HashSet<(int Period, int HourOffset)> HoursFor(DateTime windowEndUtc, int windowHours, int baselineWeeks, TimeZoneInfo tz, Func<DateTime, bool> include,
            IReadOnlyDictionary<int, int> periodShiftHours = null)
        {
            var set = new HashSet<(int, int)>();
            for (var period = 0; period <= baselineWeeks; period++)
            {
                var shift = periodShiftHours != null && periodShiftHours.TryGetValue(period, out var s) ? s : 0;
                var start = windowEndUtc.AddDays(-7 * period).AddHours(-windowHours + shift);
                for (var hour = 0; hour < windowHours; hour++)
                {
                    var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(start.AddHours(hour), DateTimeKind.Utc), tz ?? TimeZoneInfo.Utc);
                    if (include(local)) set.Add((period, hour));
                }
            }
            return set;
        }

        /// <summary>
        /// How many hours each baseline period has to move from exactly 7 days a week back to start at the same local time as the window, in
        /// <paramref name="tz"/> - 0, or an hour either way when a clock change falls between the window and that week.  Without it, the week
        /// before the clocks go forward compares 09:00 now with 08:00 local then.  Only the periods that move are returned
        /// </summary>
        public static Dictionary<int, int> PeriodShifts(DateTime windowEndUtc, int windowHours, int baselineWeeks, TimeZoneInfo tz)
        {
            var shifts = new Dictionary<int, int>();
            if (tz == null || tz == TimeZoneInfo.Utc) return shifts;
            var startUtc = DateTime.SpecifyKind(windowEndUtc.AddHours(-windowHours), DateTimeKind.Utc);
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
            for (var period = 1; period <= baselineWeeks; period++)
            {
                var local = DateTime.SpecifyKind(startLocal.AddDays(-7 * period), DateTimeKind.Unspecified);
                // A local time the clocks skipped doesn't exist - the hour after it does
                if (tz.IsInvalidTime(local)) local = local.AddHours(1);
                var utc = TimeZoneInfo.ConvertTimeToUtc(local, tz);
                var shift = (int)Math.Round((utc - startUtc.AddDays(-7 * period)).TotalHours);
                if (shift != 0) shifts[period] = shift;
            }
            return shifts;
        }

        /// <summary>The shifts as the proc takes them: "period:minutes,..." - null for none</summary>
        public static string PeriodShiftParameter(IReadOnlyDictionary<int, int> shifts) =>
            shifts == null || shifts.Count == 0 ? null : string.Join(",", shifts.OrderBy(s => s.Key).Select(s => $"{s.Key}:{s.Value * 60}"));

        /// <summary>Each baseline period's shift in hours from exactly 7 days a week - see PeriodShifts.  Empty: none</summary>
        public IReadOnlyDictionary<int, int> PeriodShiftHours { get; init; } = new Dictionary<int, int>();

        /// <summary>The included hours as the procs take them: "period:hour,..." - null for every hour</summary>
        public static string HoursParameter(HashSet<(int Period, int HourOffset)> hours) =>
            hours == null ? null : string.Join(",", hours.OrderBy(h => h.Period).ThenBy(h => h.HourOffset).Select(h => $"{h.Period}:{h.HourOffset}"));
    }

    public static class PerformanceDataRepository
    {
        public const int DefaultBaselineWeeks = 4;

        /// <param name="includedHours">Hours performance is counted in (PerformanceData.HoursFor) - the wait types are only summed over these.  Null: every hour</param>
        public static PerformanceData Get(string connectionString, string instanceList, DateTime nowUtc, int baselineWeeks = DefaultBaselineWeeks,
            HashSet<(int Period, int HourOffset)> includedHours = null) =>
            Get(connectionString, instanceList, PerformanceData.WindowEndFor(nowUtc), 24, baselineWeeks, includedHours);

        /// <param name="windowEndUtc">End of the window, on the hour</param>
        /// <param name="windowHours">Whole hours in the window - at most 168 with baseline weeks</param>
        /// <param name="periodShiftHours">Baseline period shifts for a clock change (PerformanceData.PeriodShifts).  Null: exactly 7 days a week</param>
        /// <param name="detail">False reads only CPU, IO and total waits by hour - enough for the charts' baseline lines - and no wait types,
        /// blocking, deadlocks or slow queries</param>
        public static PerformanceData Get(string connectionString, string instanceList, DateTime windowEndUtc, int windowHours, int baselineWeeks,
            HashSet<(int Period, int HourOffset)> includedHours, int commandTimeout = 300, IReadOnlyDictionary<int, int> periodShiftHours = null, bool detail = true)
        {
            var data = new PerformanceData
            {
                WindowEndUtc = windowEndUtc,
                WindowHours = windowHours,
                BaselineWeeks = baselineWeeks,
                IncludedHours = includedHours,
                PeriodShiftHours = periodShiftHours ?? new Dictionary<int, int>()
            };

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Performance_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceList);
            cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = windowEndUtc });
            cmd.Parameters.AddWithValue("@BaselineWeeks", (byte)baselineWeeks);
            cmd.Parameters.Add(new SqlParameter("@Hours", SqlDbType.VarChar, -1) { Value = (object)PerformanceData.HoursParameter(includedHours) ?? DBNull.Value });
            cmd.Parameters.AddWithValue("@WindowHours", (short)windowHours);
            cmd.Parameters.Add(new SqlParameter("@PeriodShiftMins", SqlDbType.VarChar, -1) { Value = (object)PerformanceData.PeriodShiftParameter(periodShiftHours) ?? DBNull.Value });
            cmd.Parameters.AddWithValue("@Detail", detail);
            cn.Open();
            using var r = cmd.ExecuteReader();

            var byID = new Dictionary<int, InstancePerformanceData>();
            while (r.Read())
            {
                var instance = new InstancePerformanceData
                {
                    InstanceID = r.GetInt32(0),
                    Instance = r.IsDBNull(1) ? $"InstanceID {r.GetInt32(0)}" : r.GetString(1),
                    IsAzureDB = r.GetBoolean(2)
                };
                byID[instance.InstanceID] = instance;
                data.Instances.Add(instance);
            }

            r.NextResult();
            while (r.Read())
            {
                if (!byID.TryGetValue(r.GetInt32(r.GetOrdinal("InstanceID")), out var instance)) continue;
                instance.Hours.Add(new PerformanceHour
                {
                    Period = Convert.ToInt32(r["Period"]),
                    HourOffset = Convert.ToInt32(r["HourOffset"]),
                    AvgCPU = D(r, "AvgCPU"),
                    MaxCPU = D(r, "MaxCPU"),
                    HighCPUSamples = L(r, "HighCPUSamples"),
                    CPUSamples = L(r, "CPUSamples"),
                    Reads = L(r, "Reads"),
                    Writes = L(r, "Writes"),
                    ReadStallMs = L(r, "ReadStallMs"),
                    WriteStallMs = L(r, "WriteStallMs"),
                    ReadBytes = L(r, "ReadBytes"),
                    WriteBytes = L(r, "WriteBytes"),
                    IOSampleMs = L(r, "IOSampleMs"),
                    WaitMs = L(r, "WaitMs"),
                    CriticalWaitMs = L(r, "CriticalWaitMs"),
                    LockWaitMs = L(r, "LockWaitMs"),
                    WaitSampleMs = L(r, "WaitSampleMs"),
                    BlockingSnapshots = I(r, "BlockingSnapshots"),
                    MaxBlockedSessions = I(r, "MaxBlockedSessions"),
                    MaxBlockedWaitMs = L(r, "MaxBlockedWaitMs"),
                    Deadlocks = I(r, "Deadlocks"),
                    SlowQueries = I(r, "SlowQueries"),
                    FailedSlowQueries = I(r, "FailedSlowQueries")
                });
            }

            r.NextResult();
            while (r.Read())
            {
                if (!byID.TryGetValue(r.GetInt32(r.GetOrdinal("InstanceID")), out var instance)) continue;
                instance.WaitTypes.Add(new PerformanceWaitType
                {
                    Period = Convert.ToInt32(r["Period"]),
                    WaitType = r.GetString(r.GetOrdinal("WaitType")),
                    IsCriticalWait = r.GetBoolean(r.GetOrdinal("IsCriticalWait")),
                    WaitMs = Convert.ToInt64(r["WaitMs"])
                });
            }

            r.NextResult();
            if (r.Read())
            {
                var defaults = new PerformanceThresholdValues();
                data.Thresholds = new PerformanceThresholdValues
                {
                    CPUCritical = D(r, "CPUCriticalThreshold") ?? defaults.CPUCritical,
                    CPUWarning = D(r, "CPUWarningThreshold") ?? defaults.CPUWarning,
                    ReadLatencyCritical = D(r, "ReadLatencyCriticalThreshold") ?? defaults.ReadLatencyCritical,
                    ReadLatencyWarning = D(r, "ReadLatencyWarningThreshold") ?? defaults.ReadLatencyWarning,
                    WriteLatencyCritical = D(r, "WriteLatencyCriticalThreshold") ?? defaults.WriteLatencyCritical,
                    WriteLatencyWarning = D(r, "WriteLatencyWarningThreshold") ?? defaults.WriteLatencyWarning,
                    MinIOPs = D(r, "MinIOPsThreshold") ?? defaults.MinIOPs,
                    CriticalWaitCritical = D(r, "CriticalWaitCriticalThreshold") ?? defaults.CriticalWaitCritical,
                    CriticalWaitWarning = D(r, "CriticalWaitWarningThreshold") ?? defaults.CriticalWaitWarning
                };
            }
            return data;
        }

        private static double? D(SqlDataReader r, string column)
        {
            var v = r[column];
            return v == DBNull.Value ? null : Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static long? L(SqlDataReader r, string column)
        {
            var v = r[column];
            return v == DBNull.Value ? null : Convert.ToInt64(v);
        }

        private static int? I(SqlDataReader r, string column)
        {
            var v = r[column];
            return v == DBNull.Value ? null : Convert.ToInt32(v);
        }
    }
}
