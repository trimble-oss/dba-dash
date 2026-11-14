using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADash.DailyReport
{
    public enum PerformanceSeverity
    {
        Critical = 1,
        Warning = 2,
        /// <summary>Within thresholds, but significantly worse than the usual for this day of the week</summary>
        WorseThanUsual = 3
    }

    /// <summary>The last 24 hours (or the same 24 hours of an earlier week) for one instance, worked out from its hourly rows</summary>
    public class DayStats
    {
        public bool HasCPU { get; init; }
        public bool HasIO { get; init; }
        public bool HasWaits { get; init; }
        public bool HasAny { get; init; }

        public double? AvgCPU { get; init; }
        public double? MaxCPU { get; init; }
        /// <summary>Average CPU of the busiest hour - a day's average is diluted by quiet hours, so this is what's compared with the CPU thresholds</summary>
        public double? PeakHourCPU { get; init; }
        public int? PeakHourCPUOffset { get; init; }
        /// <summary>Minutes at 80% CPU or more</summary>
        public double? HighCPUMinutes { get; init; }

        public double? ReadIOPs { get; init; }
        public double? WriteIOPs { get; init; }
        /// <summary>IO weighted, so busy hours count for more than quiet ones</summary>
        public double? ReadLatency { get; init; }
        public double? WriteLatency { get; init; }

        public double? WaitMsPerSec { get; init; }
        public double? CriticalWaitMsPerSec { get; init; }
        public double? LockWaitMsPerSec { get; init; }
        public long WaitSampleMs { get; init; }

        public int BlockingSnapshots { get; init; }
        public int MaxBlockedSessions { get; init; }
        public long MaxBlockedWaitMs { get; init; }
        public int Deadlocks { get; init; }
        public int SlowQueries { get; init; }
        public int FailedSlowQueries { get; init; }

        /// <summary>Hours with fewer CPU samples than this are left out of the busiest hour - a part hour is too noisy</summary>
        public const int MinSamplesForPeakHour = 10;

        /// <param name="fullHourSamples">CPU samples in a fully collected hour for this instance - see FullHourCPUSamples</param>
        public static DayStats From(IEnumerable<PerformanceHour> hours, long fullHourSamples = 0)
        {
            var list = hours.ToList();
            var cpuHours = list.Where(h => h.CPUSamples > 0 && h.AvgCPU.HasValue).ToList();
            var ioHours = list.Where(h => h.IOSampleMs > 0).ToList();
            var waitHours = list.Where(h => h.WaitSampleMs > 0).ToList();

            var cpuSamples = cpuHours.Sum(h => h.CPUSamples!.Value);
            var peak = cpuHours.Where(h => h.CPUSamples >= MinSamplesForPeakHour)
                .OrderByDescending(h => h.AvgCPU)
                .ThenBy(h => h.HourOffset)
                .FirstOrDefault();

            var ioSec = ioHours.Sum(h => h.IOSampleMs!.Value) / 1000.0;
            var reads = ioHours.Sum(h => h.Reads ?? 0);
            var writes = ioHours.Sum(h => h.Writes ?? 0);
            var waitSampleMs = waitHours.Sum(h => h.WaitSampleMs!.Value);

            return new DayStats
            {
                HasCPU = cpuHours.Count > 0,
                HasIO = ioHours.Count > 0,
                HasWaits = waitHours.Count > 0,
                HasAny = cpuHours.Count > 0 || ioHours.Count > 0 || waitHours.Count > 0,

                AvgCPU = cpuSamples > 0 ? cpuHours.Sum(h => h.AvgCPU!.Value * h.CPUSamples!.Value) / cpuSamples : null,
                MaxCPU = cpuHours.Count > 0 ? cpuHours.Max(h => h.MaxCPU) : null,
                PeakHourCPU = peak?.AvgCPU,
                PeakHourCPUOffset = peak?.HourOffset,
                HighCPUMinutes = cpuHours.Count > 0 ? cpuHours.Sum(h => PerformanceAnalyzer.HighCPUMinutes(h, fullHourSamples) ?? 0) : null,

                ReadIOPs = ioSec > 0 ? reads / ioSec : null,
                WriteIOPs = ioSec > 0 ? writes / ioSec : null,
                ReadLatency = reads > 0 ? ioHours.Sum(h => h.ReadStallMs ?? 0) / (double)reads : null,
                WriteLatency = writes > 0 ? ioHours.Sum(h => h.WriteStallMs ?? 0) / (double)writes : null,

                WaitMsPerSec = waitSampleMs > 0 ? waitHours.Sum(h => h.WaitMs ?? 0) * 1000.0 / waitSampleMs : null,
                CriticalWaitMsPerSec = waitSampleMs > 0 ? waitHours.Sum(h => h.CriticalWaitMs ?? 0) * 1000.0 / waitSampleMs : null,
                LockWaitMsPerSec = waitSampleMs > 0 ? waitHours.Sum(h => h.LockWaitMs ?? 0) * 1000.0 / waitSampleMs : null,
                WaitSampleMs = waitSampleMs,

                BlockingSnapshots = list.Sum(h => h.BlockingSnapshots ?? 0),
                MaxBlockedSessions = list.Count == 0 ? 0 : list.Max(h => h.MaxBlockedSessions ?? 0),
                MaxBlockedWaitMs = list.Count == 0 ? 0 : list.Max(h => h.MaxBlockedWaitMs ?? 0),
                Deadlocks = list.Sum(h => h.Deadlocks ?? 0),
                SlowQueries = list.Sum(h => h.SlowQueries ?? 0),
                FailedSlowQueries = list.Sum(h => h.FailedSlowQueries ?? 0)
            };
        }
    }

    /// <summary>One thing worth reporting about one instance: over a threshold, worse than usual, or both</summary>
    public class PerformanceFinding
    {
        public string Metric { get; init; }
        public string Label { get; init; }
        public PerformanceSeverity Severity { get; init; }
        public double Current { get; init; }
        /// <summary>Median of the same 24 hours in the baseline weeks.  Null when there's no baseline</summary>
        public double? Baseline { get; init; }
        /// <summary>Null when the baseline is 0 (a change from nothing) or there's no baseline</summary>
        public double? ChangePct { get; init; }
        public bool IsWorseThanUsual { get; init; }
        /// <summary>The figure, e.g. "94% in the busiest hour (14:00)"</summary>
        public string ValueText { get; init; }
        /// <summary>The baseline in the same form as the figure, e.g. "60%"</summary>
        public string BaselineText { get; init; }
        /// <summary>What explains it, e.g. the wait types behind a waits increase, or the IOPs behind a latency figure</summary>
        public string Context { get; init; }

        public string ChangeText => !IsWorseThanUsual ? null
            : ChangePct.HasValue ? $"+{ChangePct.Value.ToString("0", CultureInfo.InvariantCulture)}%"
            : "new";
    }

    public class InstancePerformanceResult
    {
        public int InstanceID { get; init; }
        public string Instance { get; init; }
        public List<PerformanceFinding> Findings { get; } = new();
        public PerformanceSeverity Severity => Findings.Count == 0 ? PerformanceSeverity.WorseThanUsual : Findings.Min(f => f.Severity);
        /// <summary>Baseline weeks with data for this instance</summary>
        public int BaselineWeeks { get; init; }
        /// <summary>The instance's hourly rows, for charts and for the queries behind a regression</summary>
        public InstancePerformanceData Data { get; init; }
        /// <summary>The procedures and queries behind it, when they were looked up</summary>
        public List<TopQuery> TopQueries { get; } = new();
        /// <summary>What TopQueries are ranked by, e.g. "CPU" - see TopQueryAnalyzer</summary>
        public string TopQueriesMeasure { get; set; }
        /// <summary>Whether TopQueries are ranked by their increase on the usual (true) or by their total (false)</summary>
        public bool TopQueriesByIncrease { get; set; }
        /// <summary>Long running requests that fit the findings (DBCC, backups, index maintenance...), from the running query snapshots</summary>
        public List<DailyReportActivity> Activity { get; } = new();
        /// <summary>What Activity is ranked by: writes, reads, CPU or duration - see DailyReportActivityAnalyzer</summary>
        public string ActivityMeasure { get; set; }
    }

    public class PerformanceAnalysis
    {
        public DateTime WindowStartUtc { get; init; }
        public int BaselineWeeks { get; init; }
        public bool ComparedWithBaseline { get; init; }
        public int ChangeThresholdPct { get; init; }
        public DailyReportSettings Settings { get; init; }
        public PerformanceThresholdValues Thresholds { get; init; }
        /// <summary>The hours counted, when the report counts only certain hours of the day - see PerformanceData.IncludedHours.  Null: all 24</summary>
        public HashSet<(int Period, int HourOffset)> IncludedHours { get; init; }

        /// <summary>Instances with something to report, worst first</summary>
        public List<InstancePerformanceResult> Flagged { get; } = new();
        /// <summary>Instances with data and nothing to report</summary>
        public int OkCount { get; set; }
        /// <summary>Instances significantly better than usual on some measure (not listed - just counted)</summary>
        public int BetterThanUsualCount { get; set; }
        /// <summary>No CPU, IO or wait data in the last 24 hours</summary>
        public List<string> NoData { get; } = new();
        /// <summary>Data now, but none in any baseline week, so not compared</summary>
        public List<string> NoBaseline { get; } = new();
        /// <summary>Instances with a measure over a threshold but within the usual range, left out (ThresholdsOnlyWhenWorseThanUsual).  Named in the overview</summary>
        public List<string> OverThresholdAsUsual { get; } = new();

        public int Deadlocks { get; set; }
        public int DeadlockInstances { get; set; }
        public int BlockingInstances { get; set; }
        public int SlowQueries { get; set; }
        public int FailedSlowQueries { get; set; }
        public int SlowQueryInstances { get; set; }

        public int CriticalInstances => Flagged.Count(f => f.Severity == PerformanceSeverity.Critical);
        public int WarningInstances => Flagged.Count(f => f.Severity == PerformanceSeverity.Warning);
        public int WorseThanUsualInstances => Flagged.Count(f => f.Severity == PerformanceSeverity.WorseThanUsual);
    }

    /// <summary>
    /// Works out what in the last 24 hours is worth a line in the daily report: an instance over the Performance Summary thresholds, or
    /// significantly worse than the median of the same 24 hours in the previous weeks (same weekday, so a quiet Sunday is compared with
    /// quiet Sundays).  A median over several weeks is used rather than one week so a single unusual day doesn't set the baseline.
    /// Everything that's normal is summed up in a count rather than listed.  Pure logic, so it can be unit tested.
    /// </summary>
    public static class PerformanceAnalyzer
    {
        public const int MaxWaitTypesShown = 3;

        private enum Status { None, Critical, Warning }

        /// <param name="includeThresholds">Report instances over the thresholds</param>
        /// <param name="includeChanges">Report instances significantly worse than usual, and show the usual figures</param>
        /// <param name="formatHour">Formats an hour offset from the start of the window, for "busiest hour" text</param>
        public static PerformanceAnalysis Analyze(PerformanceData data, bool includeThresholds, bool includeChanges, int thresholdPct, DailyReportSettings settings = null, Func<int, string> formatHour = null)
        {
            settings ??= new DailyReportSettings();
            formatHour ??= h => data.WindowStartUtc.AddHours(h).ToString("HH:mm", CultureInfo.InvariantCulture);
            var t = data.Thresholds;
            var analysis = new PerformanceAnalysis
            {
                WindowStartUtc = data.WindowStartUtc,
                BaselineWeeks = data.BaselineWeeks,
                ComparedWithBaseline = includeChanges,
                ChangeThresholdPct = thresholdPct,
                Settings = settings,
                Thresholds = data.Thresholds,
                IncludedHours = data.IncludedHours
            };

            foreach (var instance in data.Instances)
            {
                // Only the hours the report counts (all of them, unless it's set to certain hours of the day)
                var counted = instance.Hours.Where(h => data.IsIncluded(h.Period, h.HourOffset)).ToList();
                var fullHour = FullHourCPUSamples(instance.Hours);
                var current = DayStats.From(counted.Where(h => h.Period == 0), fullHour);
                var baselineDays = Enumerable.Range(1, data.BaselineWeeks)
                    .Select(p => (Period: p, Stats: DayStats.From(counted.Where(h => h.Period == p), fullHour)))
                    .Where(d => d.Stats.HasAny)
                    .ToList();

                // Fleet totals, for the measures the report includes - a measure switched off is left out of the overview too
                if (settings.IsEnabled("Deadlocks"))
                {
                    analysis.Deadlocks += current.Deadlocks;
                    if (current.Deadlocks > 0) analysis.DeadlockInstances++;
                }
                if (settings.IsEnabled("Blocking") && current.BlockingSnapshots > 0) analysis.BlockingInstances++;
                if (settings.IsEnabled("SlowQueries"))
                {
                    analysis.SlowQueries += current.SlowQueries;
                    if (current.SlowQueries > 0) analysis.SlowQueryInstances++;
                }
                if (settings.IsEnabled("FailedSlowQueries")) analysis.FailedSlowQueries += current.FailedSlowQueries;

                if (!current.HasAny)
                {
                    analysis.NoData.Add(instance.Instance);
                    continue;
                }
                if (includeChanges && baselineDays.Count == 0)
                {
                    analysis.NoBaseline.Add(instance.Instance);
                }

                var compare = includeChanges && baselineDays.Count > 0;
                var baseline = baselineDays.Select(d => d.Stats).ToList();
                var result = new InstancePerformanceResult { InstanceID = instance.InstanceID, Instance = instance.Instance, BaselineWeeks = baselineDays.Count, Data = instance };
                var better = false;

                var overThresholdAsUsual = false;
                void Consider(string metric, string label, double? cur, Func<DayStats, double?> selector, Status status,
                    Func<double, string> fmt, string valueText = null, string context = null)
                {
                    if (cur == null || !settings.IsEnabled(metric)) return;
                    var minChange = settings.MinChange(metric);
                    double? usual = compare ? Median(baseline.Select(selector)) : null;
                    var worse = usual.HasValue && IsSignificant(cur.Value - usual.Value, usual.Value, minChange, thresholdPct);
                    if (usual.HasValue && IsSignificant(usual.Value - cur.Value, usual.Value, minChange, thresholdPct)) better = true;

                    var overThreshold = includeThresholds && status != Status.None;
                    if (!overThreshold && !worse) return;
                    // Over a threshold, but within the usual range hour by hour: normal for this instance.  Judged per hour rather than on the
                    // day's figure, so a backup spike the instance has every week at about the same time isn't reported as new because it was a
                    // bit heavier or fell in a busier hour.  Only judged where there's a usual to judge by
                    if (overThreshold && settings.ThresholdsOnlyWhenWorseThanUsual && usual.HasValue
                        && ExcessOverUsualRange(metric, counted, data.BaselineWeeks) is { } excess
                        && excess < Math.Max(minChange, double.Epsilon))
                    {
                        overThresholdAsUsual = true;
                        return;
                    }

                    result.Findings.Add(new PerformanceFinding
                    {
                        Metric = metric,
                        Label = label,
                        Severity = status == Status.Critical ? PerformanceSeverity.Critical : status == Status.Warning ? PerformanceSeverity.Warning : PerformanceSeverity.WorseThanUsual,
                        Current = cur.Value,
                        Baseline = usual,
                        ChangePct = usual is > 0 ? (cur.Value - usual.Value) / usual.Value * 100 : null,
                        IsWorseThanUsual = worse,
                        ValueText = valueText ?? fmt(cur.Value),
                        BaselineText = usual.HasValue ? fmt(usual.Value) : null,
                        Context = context
                    });
                }

                // CPU: the busiest hour against the thresholds - a day's average hides a busy afternoon behind a quiet night
                if (current.PeakHourCPU.HasValue)
                {
                    var cpuContext = new List<string>();
                    if (current.HighCPUMinutes > 0) cpuContext.Add($"{Duration(current.HighCPUMinutes.Value)} at 80%+");
                    if (current.AvgCPU.HasValue) cpuContext.Add($"day average {Pct(current.AvgCPU.Value)}");
                    Consider("CPU", "CPU", current.PeakHourCPU, d => d.PeakHourCPU,
                        Over(current.PeakHourCPU, t.CPUCritical, t.CPUWarning), Pct,
                        valueText: $"{Pct(current.PeakHourCPU.Value)} in the busiest hour ({formatHour(current.PeakHourCPUOffset!.Value)})",
                        context: string.Join(", ", cpuContext));
                }
                // Time at high CPU is only reported as a change - the busiest hour already covers the thresholds
                if (!result.Findings.Any(f => f.Metric == "CPU"))
                {
                    Consider("HighCPU", "Time at 80%+ CPU", current.HighCPUMinutes, d => d.HighCPUMinutes, Status.None, Duration);
                }

                // Latency is ignored below the minimum IOPs, as on the Performance Summary - a few slow IOs on an idle instance aren't a problem
                if (current.ReadIOPs >= t.MinIOPs)
                {
                    Consider("ReadLatency", "Read latency", current.ReadLatency, d => d.ReadIOPs >= t.MinIOPs ? d.ReadLatency : null,
                        Over(current.ReadLatency, t.ReadLatencyCritical, t.ReadLatencyWarning), Ms,
                        context: IOPsContext(current.ReadIOPs, compare ? Median(baseline.Select(d => d.ReadIOPs)) : null));
                }
                if (current.WriteIOPs >= t.MinIOPs)
                {
                    Consider("WriteLatency", "Write latency", current.WriteLatency, d => d.WriteIOPs >= t.MinIOPs ? d.WriteLatency : null,
                        Over(current.WriteLatency, t.WriteLatencyCritical, t.WriteLatencyWarning), Ms,
                        context: IOPsContext(current.WriteIOPs, compare ? Median(baseline.Select(d => d.WriteIOPs)) : null));
                }

                // Waits, with the wait types behind them
                var waitTypes = WaitTypeRates(instance, current, baselineDays, compare);
                Consider("CriticalWaits", "Critical waits", current.CriticalWaitMsPerSec, d => d.CriticalWaitMsPerSec,
                    Over(current.CriticalWaitMsPerSec, t.CriticalWaitCritical, t.CriticalWaitWarning), MsPerSec,
                    context: TopWaitTypes(waitTypes.Where(w => w.IsCritical), compare));
                Consider("LockWaits", "Lock waits", current.LockWaitMsPerSec, d => d.LockWaitMsPerSec, Status.None, MsPerSec,
                    context: TopWaitTypes(waitTypes.Where(w => w.WaitType.StartsWith("LCK", StringComparison.OrdinalIgnoreCase)), compare));
                Consider("Waits", "Total waits", current.WaitMsPerSec, d => d.WaitMsPerSec, Status.None, MsPerSec,
                    context: TopWaitTypes(waitTypes, compare));

                // Blocking, deadlocks and slow queries only as changes: every busy system has some, so the count alone isn't news
                Consider("Blocking", "Snapshots with blocking", current.BlockingSnapshots, d => d.BlockingSnapshots, Status.None, Count,
                    context: current.BlockingSnapshots > 0 ? $"up to {Plural(current.MaxBlockedSessions, "session")} blocked, {DurationMs(current.MaxBlockedWaitMs)} blocked wait in one snapshot" : null);
                Consider("Deadlocks", "Deadlocks", current.Deadlocks, d => d.Deadlocks, Status.None, Count);
                Consider("SlowQueries", "Slow queries", current.SlowQueries, d => d.SlowQueries, Status.None, Count);
                Consider("FailedSlowQueries", "Failed or aborted queries", current.FailedSlowQueries, d => d.FailedSlowQueries, Status.None, Count);

                if (result.Findings.Count > 0)
                {
                    analysis.Flagged.Add(result);
                }
                else
                {
                    analysis.OkCount++;
                }
                if (better) analysis.BetterThanUsualCount++;
                if (overThresholdAsUsual) analysis.OverThresholdAsUsual.Add(instance.Instance);
            }

            var ordered = analysis.Flagged
                .OrderBy(r => r.Severity)
                .ThenByDescending(r => r.Findings.Count)
                .ThenBy(r => r.Instance, StringComparer.OrdinalIgnoreCase)
                .ToList();
            analysis.Flagged.Clear();
            analysis.Flagged.AddRange(ordered);
            foreach (var r in analysis.Flagged)
            {
                var findings = r.Findings.OrderBy(f => f.Severity).ToList();
                r.Findings.Clear();
                r.Findings.AddRange(findings);
            }
            analysis.NoData.Sort(StringComparer.OrdinalIgnoreCase);
            analysis.NoBaseline.Sort(StringComparer.OrdinalIgnoreCase);
            analysis.OverThresholdAsUsual.Sort(StringComparer.OrdinalIgnoreCase);
            return analysis;
        }

        /// <summary>
        /// One hour's figure for a measure, for the report's charts.  Null when the hour has no data for it, or - for latency - when the
        /// hour's IO is under the minimum IOPs, as latency is ignored there.
        /// </summary>
        /// <param name="fullHourSamples">CPU samples in a fully collected hour for the instance (FullHourCPUSamples), for time at high CPU</param>
        public static double? HourlyValue(string metric, PerformanceHour h, double minIOPs, long fullHourSamples = 0)
        {
            double? Rate(long? ms) => h.WaitSampleMs > 0 ? (ms ?? 0) * 1000.0 / h.WaitSampleMs.Value : null;
            double? IOPs(long? count) => h.IOSampleMs > 0 ? (count ?? 0) * 1000.0 / h.IOSampleMs.Value : null;
            return metric switch
            {
                "CPU" => h.CPUSamples > 0 ? h.AvgCPU : null,
                "HighCPU" => HighCPUMinutes(h, fullHourSamples),
                "ReadLatency" => h.Reads > 0 && IOPs(h.Reads) >= minIOPs ? (h.ReadStallMs ?? 0) / (double)h.Reads.Value : null,
                "WriteLatency" => h.Writes > 0 && IOPs(h.Writes) >= minIOPs ? (h.WriteStallMs ?? 0) / (double)h.Writes.Value : null,
                "CriticalWaits" => Rate(h.CriticalWaitMs),
                "LockWaits" => Rate(h.LockWaitMs),
                "Waits" => Rate(h.WaitMs),
                "Blocking" => h.BlockingSnapshots ?? 0,
                "Deadlocks" => h.Deadlocks ?? 0,
                "SlowQueries" => h.SlowQueries ?? 0,
                "FailedSlowQueries" => h.FailedSlowQueries ?? 0,
                _ => null
            };
        }

        /// <summary>Hours either side of an hour that its usual range also takes in - a job that starts a little earlier or later, or a clock change</summary>
        public const int UsualRangeHourTolerance = 1;

        /// <summary>
        /// How far the last 24 hours went above the usual range, in the measure's own units, so it can be compared with the minimum change.
        /// Each hour is compared with the highest the baseline weeks had in that hour or the hours either side (UsualRangeHourTolerance) and
        /// only the part above that counts.  The parts are weighted as the day's figure is: latency by the hour's reads or writes, critical
        /// waits by the hour's sample time, and CPU - whose figure is the busiest hour - by taking the largest.  An hour with no baseline at
        /// all counts in full.  Null for a measure without a threshold, or with no hourly figures today.
        /// </summary>
        /// <param name="hours">The instance's counted hours, all periods</param>
        public static double? ExcessOverUsualRange(string metric, IReadOnlyCollection<PerformanceHour> hours, int baselineWeeks)
        {
            Func<PerformanceHour, double> weight = metric switch
            {
                "ReadLatency" => h => h.Reads ?? 0,
                "WriteLatency" => h => h.Writes ?? 0,
                "CriticalWaits" => h => h.WaitSampleMs ?? 0,
                "CPU" => h => 1,
                _ => null
            };
            if (weight == null) return null;
            // Raw hourly figures (no minimum IOPs): a quiet hour's latency counts for little through its weight, and leaving it out of the
            // baseline would make an hour that's usually quiet look like it has no usual at all
            double? Value(PerformanceHour h) => HourlyValue(metric, h, 0);

            var baseline = hours.Where(h => h.Period > 0 && h.Period <= baselineWeeks).ToList();
            double total = 0, weighted = 0, largest = 0;
            var any = false;
            foreach (var h in hours.Where(h => h.Period == 0))
            {
                var v = Value(h);
                var w = weight(h);
                if (v == null || w <= 0) continue;
                any = true;
                var limit = baseline.Where(b => Math.Abs(b.HourOffset - h.HourOffset) <= UsualRangeHourTolerance)
                    .Select(Value).Where(b => b.HasValue).Select(b => b!.Value)
                    .DefaultIfEmpty(0).Max();
                var over = Math.Max(0, v.Value - limit);
                total += w;
                weighted += over * w;
                largest = Math.Max(largest, over);
            }
            if (!any) return null;
            return metric == "CPU" ? largest : total > 0 ? weighted / total : 0;
        }

        /// <summary>
        /// CPU samples in a fully collected hour for an instance: the most any of its hours has.  The CPU sources sample at different rates
        /// (once a minute for SQL Server, every 15 seconds for Azure DB), so it's learned from the data rather than assumed
        /// </summary>
        public static long FullHourCPUSamples(IEnumerable<PerformanceHour> hours) =>
            hours.Select(h => h.CPUSamples ?? 0).DefaultIfEmpty(0).Max();

        /// <summary>
        /// Minutes at 80% CPU or more in an hour.  Each sample stands for 60 / fullHourSamples minutes, so an hour with only part of its samples
        /// (a restart or a collection gap) counts only the time it saw rather than being scaled up to the whole hour.  Without a full hour to go
        /// by, the hour's own samples are taken as the whole hour.  Null when the hour has no CPU samples
        /// </summary>
        public static double? HighCPUMinutes(PerformanceHour h, long fullHourSamples)
        {
            if (!(h.CPUSamples > 0)) return null;
            var perHour = Math.Max(h.CPUSamples.Value, fullHourSamples);
            return Math.Min(60, (h.HighCPUSamples ?? 0) * 60.0 / perHour);
        }

        /// <summary>The warning threshold a chart draws for a measure, or null when it has none</summary>
        public static double? WarningThreshold(string metric, PerformanceThresholdValues t) => metric switch
        {
            "CPU" => t.CPUWarning,
            "ReadLatency" => t.ReadLatencyWarning,
            "WriteLatency" => t.WriteLatencyWarning,
            "CriticalWaits" => t.CriticalWaitWarning,
            _ => null
        };

        /// <summary>A change worth reporting: at least the minimum absolute change, and at least thresholdPct of the baseline (any rise from 0 counts)</summary>
        public static bool IsSignificant(double change, double baseline, double minChange, int thresholdPct)
        {
            // No change is never significant, even with a minimum change of 0
            if (change <= 0 || change < minChange) return false;
            return baseline <= 0 || change / baseline * 100 >= thresholdPct;
        }

        /// <summary>Median of the values that aren't null.  Null when there are none</summary>
        public static double? Median(IEnumerable<double?> values)
        {
            var list = values.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
            if (list.Count == 0) return null;
            var mid = list.Count / 2;
            return list.Count % 2 == 1 ? list[mid] : (list[mid - 1] + list[mid]) / 2;
        }

        private static Status Over(double? value, double critical, double warning) =>
            value > critical ? Status.Critical : value > warning ? Status.Warning : Status.None;

        private record WaitTypeRate(string WaitType, bool IsCritical, double Current, double? Usual);

        /// <summary>ms/sec of each wait type now and its median over the baseline weeks (0 in a week with waits data but not that type)</summary>
        private static List<WaitTypeRate> WaitTypeRates(InstancePerformanceData instance, DayStats current, List<(int Period, DayStats Stats)> baselineDays, bool compare)
        {
            if (current.WaitSampleMs <= 0) return new List<WaitTypeRate>();
            var baselineWithWaits = baselineDays.Where(d => d.Stats.WaitSampleMs > 0).ToList();
            return instance.WaitTypes
                .GroupBy(w => (w.WaitType, w.IsCriticalWait))
                .Select(g =>
                {
                    var cur = g.Where(w => w.Period == 0).Sum(w => w.WaitMs) * 1000.0 / current.WaitSampleMs;
                    double? usual = compare && baselineWithWaits.Count > 0
                        ? Median(baselineWithWaits.Select(d => (double?)(g.Where(w => w.Period == d.Period).Sum(w => w.WaitMs) * 1000.0 / d.Stats.WaitSampleMs)))
                        : null;
                    return new WaitTypeRate(g.Key.WaitType, g.Key.IsCriticalWait, cur, usual);
                })
                .ToList();
        }

        /// <summary>
        /// The wait types behind a waits figure.  Compared with a baseline: the biggest increases ("RESOURCE_SEMAPHORE +280 ms/s").
        /// Without one: the biggest now.
        /// </summary>
        private static string TopWaitTypes(IEnumerable<WaitTypeRate> rates, bool compare)
        {
            var list = rates.ToList();
            IEnumerable<string> parts;
            if (compare && list.Any(r => r.Usual.HasValue))
            {
                parts = list.Where(r => r.Current - (r.Usual ?? 0) >= 1)
                    .OrderByDescending(r => r.Current - (r.Usual ?? 0))
                    .Take(MaxWaitTypesShown)
                    .Select(r => $"{r.WaitType} +{MsPerSec(r.Current - (r.Usual ?? 0))}");
            }
            else
            {
                parts = list.Where(r => r.Current >= 1)
                    .OrderByDescending(r => r.Current)
                    .Take(MaxWaitTypesShown)
                    .Select(r => $"{r.WaitType} {MsPerSec(r.Current)}");
            }
            var text = string.Join(", ", parts);
            return text.Length == 0 ? null : text;
        }

        private static string IOPsContext(double? iops, double? usual) =>
            iops == null ? null
            : usual.HasValue ? $"at {Num(iops.Value)} IOPs (usual {Num(usual.Value)})"
            : $"at {Num(iops.Value)} IOPs";

        public static string Pct(double v) => v.ToString("0", CultureInfo.InvariantCulture) + "%";
        public static string Ms(double v) => v.ToString(v < 10 ? "0.#" : "0", CultureInfo.InvariantCulture) + " ms";
        public static string MsPerSec(double v) => v.ToString(v < 10 ? "0.#" : "#,0", CultureInfo.InvariantCulture) + " ms/s";
        public static string Num(double v) => v.ToString("#,0", CultureInfo.InvariantCulture);
        public static string Count(double v) => Num(Math.Round(v));
        private static string Plural(double v, string word) => Math.Round(v) == 1 ? $"1 {word}" : $"{Num(v)} {word}s";

        /// <summary>Milliseconds as "40s", "45m" or "3h 10m"</summary>
        public static string DurationMs(double ms) => ms < 60000 ? $"{Math.Round(ms / 1000)}s" : Duration(ms / 60000);

        /// <summary>Minutes as "45m" or "3h 10m"</summary>
        public static string Duration(double minutes)
        {
            var m = (int)Math.Round(minutes);
            if (m < 60) return $"{m}m";
            return m % 60 == 0 ? $"{m / 60}h" : $"{m / 60}h {m % 60}m";
        }
    }
}
