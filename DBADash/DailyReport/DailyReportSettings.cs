using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBADash.DailyReport
{
    /// <summary>A measure the performance section can report, and the default for its per-report settings</summary>
    public class PerformanceMetricInfo
    {
        public string Key { get; init; }
        public string Label { get; init; }
        public string Unit { get; init; }
        /// <summary>A change must be at least this big in absolute terms, so tiny figures doubling (1% to 2% CPU) aren't reported</summary>
        public double DefaultMinChange { get; init; }
        /// <summary>Compared with the Performance Summary thresholds as well as with the usual</summary>
        public bool HasThreshold { get; init; }
    }

    /// <summary>Per-report settings for one measure</summary>
    public class PerformanceMetricSetting
    {
        public bool Enabled { get; set; } = true;
        public double MinChange { get; set; }
    }

    /// <summary>
    /// Settings of a daily report beyond its schedule, scope and recipients.  Stored as JSON in dbo.DailyReport.Settings so a new
    /// option doesn't need a schema change.  Anything missing from the stored JSON takes its default, so older reports keep working.
    /// </summary>
    public class DailyReportSettings
    {
        public const int MinBaselineWeeks = 1;
        public const int MaxBaselineWeeks = 8;

        public static readonly PerformanceMetricInfo[] Metrics =
        {
            new() { Key = "CPU", Label = "CPU (busiest hour)", Unit = "%", DefaultMinChange = 10, HasThreshold = true },
            new() { Key = "HighCPU", Label = "Time at 80%+ CPU", Unit = "minutes", DefaultMinChange = 30 },
            new() { Key = "ReadLatency", Label = "Read latency", Unit = "ms", DefaultMinChange = 5, HasThreshold = true },
            new() { Key = "WriteLatency", Label = "Write latency", Unit = "ms", DefaultMinChange = 5, HasThreshold = true },
            new() { Key = "CriticalWaits", Label = "Critical waits", Unit = "ms/s", DefaultMinChange = 20, HasThreshold = true },
            new() { Key = "LockWaits", Label = "Lock waits", Unit = "ms/s", DefaultMinChange = 50 },
            new() { Key = "Waits", Label = "Total waits", Unit = "ms/s", DefaultMinChange = 200 },
            new() { Key = "Blocking", Label = "Snapshots with blocking", Unit = "snapshots", DefaultMinChange = 10 },
            new() { Key = "Deadlocks", Label = "Deadlocks", Unit = "deadlocks", DefaultMinChange = 3 },
            new() { Key = "SlowQueries", Label = "Slow queries", Unit = "queries", DefaultMinChange = 50 },
            new() { Key = "FailedSlowQueries", Label = "Failed or aborted queries", Unit = "queries", DefaultMinChange = 10 }
        };

        /// <summary>Weeks of the same weekday the last 24 hours are compared with (their median)</summary>
        public int BaselineWeeks { get; set; } = 4;

        /// <summary>Instances listed in the performance section before "And N other instances"</summary>
        public int MaxPerformanceInstances { get; set; } = 10;

        /// <summary>A chart of the last 24 hours against the usual range for each instance listed in the performance section</summary>
        public bool IncludeCharts { get; set; } = true;

        /// <summary>The procedures and queries that used the most more CPU or IO than usual on an instance that's listed</summary>
        public bool IncludeTopQueries { get; set; } = true;
        public int TopQueriesCount { get; set; } = 3;

        /// <summary>A short summary written by the DBA Dash AI service at the top of the report.  Only names and figures are sent, never query text</summary>
        public bool IncludeAISummary { get; set; }

        /// <summary>Send the plain text version only - no HTML, no charts.  For mail systems or people that prefer text</summary>
        public bool PlainTextOnly { get; set; }

        /// <summary>Issues that were in the last report and aren't now</summary>
        public bool IncludeResolved { get; set; } = true;

        /// <summary>Open alerts (Alert.ActiveAlerts, Critical to Low) - and in a changes report, alerts raised, escalated or cleared since the last send</summary>
        public bool IncludeAlerts { get; set; } = true;

        /// <summary>
        /// Instances the performance section covers, as a tag scope (same convention as the report's: -1 all, &gt;0 one tag, -2 several in
        /// PerformanceApplyToTagIDs).  It narrows the report's own scope - checks for production and DR, performance for production only - and
        /// never adds instances the report doesn't cover.  -1: the same instances as the report.
        /// </summary>
        public int PerformanceApplyToTagID { get; set; } = -1;
        public string PerformanceApplyToTagIDs { get; set; }
        /// <summary>How the performance scope reads ("Environment:Production"), for the report and the GUI.  Null when it's the report's</summary>
        public string PerformanceScopeDescription { get; set; }

        /// <summary>
        /// Leave out measures that are over a Performance Summary threshold but within the usual range - normal for that instance.  Judged hour by
        /// hour: each hour against the highest the baseline weeks had in that hour or the hour either side (so a backup that starts a little
        /// earlier or later, or a clock change, still matches).  Only the part above that is counted, weighted as the day's figure is, and if
        /// it adds up to less than the measure's minimum change the breach is usual.  They're counted in the overview instead.  An instance
        /// without a baseline yet still reports its threshold breaches.
        /// </summary>
        public bool ThresholdsOnlyWhenWorseThanUsual { get; set; }

        /// <summary>
        /// Count performance only between these hours of the day, in the report's time zone - e.g. 9 to 17 for 09:00-17:00 - so overnight
        /// maintenance isn't reported.  From after To wraps past midnight (22 to 6).  Null (both): the whole 24 hours.  Applies to the measures,
        /// the baseline weeks, the queries and long running requests behind them, and the fleet totals; the charts still show all 24 hours with
        /// the hours left out shaded.  The report covers the 24 hours before it runs, so it's best sent after the end of the hours.
        /// </summary>
        public int? PerformanceHoursFrom { get; set; }
        public int? PerformanceHoursTo { get; set; }
        public bool HasPerformanceHours => PerformanceHoursFrom.HasValue && PerformanceHoursTo.HasValue;

        /// <summary>Whether a local hour of the day (0-23) is one performance is counted in</summary>
        public bool IsPerformanceHour(int localHour)
        {
            if (!HasPerformanceHours) return true;
            int from = PerformanceHoursFrom!.Value, to = PerformanceHoursTo!.Value;
            return from < to ? localHour >= from && localHour < to : localHour >= from || localHour < to;
        }

        /// <summary>"09:00-17:00", or null for the whole day</summary>
        public string PerformanceHoursText => HasPerformanceHours ? $"{PerformanceHoursFrom:00}:00-{PerformanceHoursTo:00}:00" : null;

        /// <summary>Long running requests (DBCC, backups, index maintenance...) from the running query snapshots, for each listed instance</summary>
        public bool IncludeActivity { get; set; } = true;

        /// <summary>Changes report: a short summary of everything still open - one line per check, with counts and how many servers</summary>
        public bool IncludeOpenSummary { get; set; } = true;

        /// <summary>Per measure: whether it's reported, and the minimum absolute change.  A measure that isn't here takes its defaults</summary>
        public Dictionary<string, PerformanceMetricSetting> MetricSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public PerformanceMetricSetting For(string key)
        {
            if (MetricSettings != null && MetricSettings.TryGetValue(key, out var s) && s != null) return s;
            var info = Metrics.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
            return new PerformanceMetricSetting { Enabled = true, MinChange = info?.DefaultMinChange ?? 0 };
        }

        public bool IsEnabled(string key) => For(key).Enabled;

        public double MinChange(string key) => For(key).MinChange;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>The settings as stored.  Doesn't change this instance - FromJson clamps what it reads</summary>
        /// <summary>A normalised copy as JSON - this instance isn't changed.  Normalising first means a value JSON can't hold (NaN) never throws here</summary>
        public string ToJson() => JsonSerializer.Serialize(((DailyReportSettings)MemberwiseClone()).Normalized(), JsonOptions);

        /// <summary>The settings in the JSON, with defaults for anything missing.  Defaults when the JSON is empty or can't be read</summary>
        public static DailyReportSettings FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new DailyReportSettings();
            try
            {
                return (JsonSerializer.Deserialize<DailyReportSettings>(json, JsonOptions) ?? new DailyReportSettings()).Normalized();
            }
            catch (JsonException)
            {
                return new DailyReportSettings();
            }
        }

        /// <summary>Clamped to sensible ranges, and with a comparer that ignores case on the metric keys</summary>
        public DailyReportSettings Normalized()
        {
            BaselineWeeks = Math.Clamp(BaselineWeeks, MinBaselineWeeks, MaxBaselineWeeks);
            MaxPerformanceInstances = Math.Clamp(MaxPerformanceInstances, 1, 500);
            TopQueriesCount = Math.Clamp(TopQueriesCount, 1, 10);
            // Both or neither, and not the same hour (which would be no hours at all)
            if (PerformanceHoursFrom is not (>= 0 and <= 23) || PerformanceHoursTo is not (>= 0 and <= 23) || PerformanceHoursFrom == PerformanceHoursTo)
            {
                PerformanceHoursFrom = null;
                PerformanceHoursTo = null;
            }
            var metrics = new Dictionary<string, PerformanceMetricSetting>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in MetricSettings ?? new Dictionary<string, PerformanceMetricSetting>())
            {
                if (kv.Value == null || Metrics.All(m => !string.Equals(m.Key, kv.Key, StringComparison.OrdinalIgnoreCase))) continue;
                // NaN and Infinity can't be serialized, and aren't a minimum change anyway - they take the measure's default
                var minChange = double.IsFinite(kv.Value.MinChange)
                    ? Math.Max(0, kv.Value.MinChange)
                    : Metrics.First(m => string.Equals(m.Key, kv.Key, StringComparison.OrdinalIgnoreCase)).DefaultMinChange;
                metrics[kv.Key] = new PerformanceMetricSetting { Enabled = kv.Value.Enabled, MinChange = minChange };
            }
            MetricSettings = metrics;
            return this;
        }

        public DailyReportSettings Clone() => FromJson(ToJson());
    }
}
