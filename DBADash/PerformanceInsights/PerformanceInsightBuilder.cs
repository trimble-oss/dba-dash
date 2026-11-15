using DBADash.DailyReport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADash.PerformanceInsights
{
    public enum InsightLevel
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    /// <summary>What a link in a card does: open a tab for the instance, or zoom the date range</summary>
    public sealed class InsightLink
    {
        /// <summary>The link's key - the card's text has it as [text](action:key)</summary>
        public string Key { get; init; }
        /// <summary>Main.Tabs name of the tab to open, or null</summary>
        public string Tab { get; init; }
        public DateTime? ZoomFromUtc { get; init; }
        public DateTime? ZoomToUtc { get; init; }
    }

    public sealed class PerformanceInsightItem
    {
        public InsightLevel Level { get; init; }
        /// <summary>Orders cards of the same level, and lets tests find a card</summary>
        public InsightCategory Category { get; init; }
        /// <summary>Plain text with [text](action:key) links - see Links</summary>
        public string Text { get; set; }
        public List<InsightLink> Links { get; } = new();
        /// <summary>A note about the figures (what they're compared with), not an issue</summary>
        public bool IsSummary { get; init; }
    }

    /// <summary>Card order within a level</summary>
    public enum InsightCategory
    {
        Performance,
        Tempdb,
        DataCollection,
        Offline,
        Restart,
        Failover,
        Alerts,
        Counters,
        Jobs,
        Changes,
        Summary
    }

    public sealed class PerformanceInsightsInput
    {
        public int InstanceID { get; init; }
        public InsightWindow Window { get; init; }
        /// <summary>Hourly figures for the window and its baseline weeks.  Null when they weren't read (too long a window, or an error)</summary>
        public PerformanceData Performance { get; init; }
        public PerformanceInsightsContext Context { get; init; }
        public List<InsightCounter> Counters { get; init; }
        public DateTime NowUtc { get; init; } = DateTime.UtcNow;
        /// <summary>The time zone times are shown in</summary>
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;
        /// <summary>Only certain hours of the day or days of the week are counted (PerformanceData.IncludedHours) - said in the summary</summary>
        public bool HasTimeFilter { get; init; }
        /// <summary>How much worse than the baseline (%) is worth a card - as the daily report's ChangeThresholdPct</summary>
        public int ChangeThresholdPct { get; init; } = 50;
        public DailyReportSettings Settings { get; init; } = new();
        /// <summary>Errors reading part of the data - each gets a card so the user knows the cards are incomplete</summary>
        public List<string> Errors { get; init; } = new();
    }

    /// <summary>
    /// The Performance tab's insight cards: what's over a threshold or not normal for the time of day and day of week in the tab's date range,
    /// and what else happened that the charts don't show (alerts, restarts, failovers, config changes, jobs, gaps in collection).  The same
    /// comparison as the daily report (PerformanceAnalyzer) over the tab's hours rather than the last 24.  Pure logic, so it can be unit tested.
    /// </summary>
    public static class PerformanceInsightBuilder
    {
        /// <summary>Most cards shown - the rest are counted in the last one</summary>
        public const int MaxCards = 10;

        /// <summary>Items named in a card before "and N more"</summary>
        public const int MaxListed = 3;

        /// <summary>Fewest baseline weeks with data before "above baseline" is said - one week alone is no baseline</summary>
        public const int MinBaselineWeeks = 2;

        /// <summary>The hour that started less than this long ago isn't expected to have data yet</summary>
        public static readonly TimeSpan CollectionAllowance = TimeSpan.FromMinutes(15);

        public static List<PerformanceInsightItem> Build(PerformanceInsightsInput input)
        {
            var cards = new List<PerformanceInsightItem>();
            var fmt = new TimeFormatter(input.Window, input.TimeZone);
            var linkId = 0;
            string Link(PerformanceInsightItem card, string text, string tab = null, DateTime? from = null, DateTime? to = null)
            {
                var key = $"l{++linkId}";
                card.Links.Add(new InsightLink { Key = key, Tab = tab, ZoomFromUtc = from, ZoomToUtc = to });
                return $"[{text}](action:{key})";
            }

            foreach (var error in input.Errors ?? new List<string>())
            {
                cards.Add(new PerformanceInsightItem { Level = InsightLevel.Warning, Category = InsightCategory.Summary, Text = error });
            }

            var summary = AddPerformance(input, cards, fmt, Link, out var tempdbExplained);
            if (!tempdbExplained) AddTempdbContention(input, cards, fmt, Link);
            AddCollectionGaps(input, cards, fmt, Link);
            AddOffline(input, cards, fmt, Link);
            AddRestart(input, cards, fmt, Link);
            AddFailovers(input, cards, fmt, Link);
            AddAlerts(input, cards, fmt, Link);
            AddCounters(input, cards, Link);
            AddJobs(input, cards, fmt, Link);
            AddChanges(input, cards, fmt, Link);

            var ordered = cards.Where(c => !c.IsSummary)
                .OrderByDescending(c => c.Level)
                .ThenBy(c => c.Category)
                .ToList();
            if (ordered.Count > MaxCards)
            {
                var more = ordered.Count - (MaxCards - 1);
                ordered = ordered.Take(MaxCards - 1).ToList();
                ordered.Add(new PerformanceInsightItem { Level = InsightLevel.Info, Category = InsightCategory.Summary, Text = $"And {Plural(more, "more insight")} - shorten the date range to see them." });
            }
            if (summary != null) ordered.Add(summary);
            return ordered;
        }

        private delegate string LinkFactory(PerformanceInsightItem card, string text, string tab = null, DateTime? from = null, DateTime? to = null);

        #region Performance

        /// <summary>A card per finding.  Returns the note on what the figures were compared with, shown last</summary>
        /// <param name="tempdbExplained">
        /// A waits card with page latch waits among its wait types said what the running queries snapshots show of tempdb contention, so there's
        /// no separate tempdb card
        /// </param>
        private static PerformanceInsightItem AddPerformance(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link,
            out bool tempdbExplained)
        {
            tempdbExplained = false;
            var window = input.Window;
            var data = input.Performance;
            if (window.IsTooLong)
            {
                return new PerformanceInsightItem
                {
                    Level = InsightLevel.Info,
                    Category = InsightCategory.Summary,
                    IsSummary = true,
                    Text = $"CPU, IO and waits aren't checked for a date range of more than {InsightWindow.MaxHours / 24} days - shorten it to see whether anything was over a threshold or not normal for the time."
                };
            }
            var instance = data?.Instances.FirstOrDefault(i => i.InstanceID == input.InstanceID);
            if (instance == null) return null;

            // Each hour counts for its share of the range - against its own hour's baseline - so a range that starts or ends part way through an
            // hour, or ends in the hour in progress, is compared like for like.  See PerformanceData.RangeWeights
            data = data.WithHourWeights(PerformanceData.RangeWeights(data.WindowStartUtc, data.WindowHours, data.BaselineWeeks, window.FromUtc, window.ToUtc, input.NowUtc));

            var analysis = PerformanceAnalyzer.Analyze(data, includeThresholds: true, includeChanges: window.IsCompared, input.ChangeThresholdPct, input.Settings,
                h => fmt.Time(data.WindowStartUtc.AddHours(h)));
            var counted = instance.Hours.Where(h => data.IsIncluded(h.Period, h.HourOffset) && data.Weight(h.Period, h.HourOffset) > 0).ToList();
            var baselineWeeks = counted.Where(h => h.Period > 0).Select(h => h.Period).Distinct().Count();
            var enoughBaseline = baselineWeeks >= MinBaselineWeeks;

            if (analysis.NoData.Count > 0)
            {
                // No figures at all - a gap card would say the same at more length
                var card = new PerformanceInsightItem { Level = InsightLevel.Warning, Category = InsightCategory.DataCollection };
                card.Text = $"No CPU, IO or wait figures were collected for {fmt.Range(window.StartUtc, window.EndUtc)}. {link(card, "Collection Health", "CollectionHealth")}";
                cards.Add(card);
                return null;
            }

            var result = analysis.Flagged.FirstOrDefault(f => f.InstanceID == input.InstanceID);
            var shown = 0;
            foreach (var f in result?.Findings ?? new List<PerformanceFinding>())
            {
                // With too little history, "above baseline" would be one week's figure - only thresholds are reported
                var worse = f.IsWorseThanUsual && enoughBaseline;
                var overThreshold = f.Severity is PerformanceSeverity.Critical or PerformanceSeverity.Warning;
                if (!worse && !overThreshold) continue;

                var card = new PerformanceInsightItem
                {
                    Level = f.Severity == PerformanceSeverity.Critical ? InsightLevel.Critical : InsightLevel.Warning,
                    Category = InsightCategory.Performance
                };
                var what = overThreshold && worse ? $"{f.Label} over the {Threshold(f)} threshold and above baseline for the time"
                    : overThreshold ? $"{f.Label} over the {Threshold(f)} threshold"
                    : $"{f.Label} above baseline for the time";
                var text = $"{what}: {f.ValueText}";
                if (worse)
                {
                    text += $" - baseline {f.BaselineText} ({f.ChangeText})";
                }
                else if (f.BaselineText != null && enoughBaseline)
                {
                    text += $" - in line with baseline ({f.BaselineText})";
                }
                text += ".";
                if (!string.IsNullOrEmpty(f.Context))
                {
                    text += f.Metric is "Waits" or "CriticalWaits" or "LockWaits"
                        ? (window.IsCompared && enoughBaseline ? " Up most: " : " Top: ") + f.Context + "."
                        : " " + Capitalise(f.Context) + ".";
                }

                var links = new List<string>();
                // Page latch waits: whether the running queries snapshots show them on tempdb - and which kind - or not, which points elsewhere
                var explainsTempdb = !tempdbExplained && f.Metric is "Waits" or "CriticalWaits"
                    && f.Context?.Contains("PAGELATCH_", StringComparison.OrdinalIgnoreCase) == true
                    && input.Context?.Tempdb is { SnapshotsWithTypes: > 0 };
                if (explainsTempdb)
                {
                    text += " " + TempdbInWaits(input.Context.Tempdb, fmt);
                    tempdbExplained = true;
                }
                if (window.Hours > 2 && WorstHour(f.Metric, counted, data, worse) is { } hour)
                {
                    var hourStart = data.WindowStartUtc.AddHours(hour);
                    links.Add(link(card, $"Zoom to {fmt.Time(hourStart)}", from: hourStart, to: hourStart.AddHours(1)));
                }
                if (TabFor(f.Metric) is { } tab)
                {
                    links.Add(link(card, tab.Text, tab.Tab));
                }
                if (explainsTempdb) links.Add(link(card, "Running Queries", "RunningQueries"));
                if (links.Count > 0) text += " " + string.Join(" · ", links);
                card.Text = text;
                cards.Add(card);
                shown++;
            }

            // What the figures were compared with - last, as a note
            string note;
            // About these insights - the charts' dashed lines are the same median, drawn hour by hour
            if (!window.IsCompared)
            {
                note = "These insights are checked against the Performance Summary thresholds only - a range of more than a week isn't compared with a baseline.";
            }
            else if (!enoughBaseline)
            {
                note = baselineWeeks == 0
                    ? $"These insights are checked against the Performance Summary thresholds only - there's no data for the same hours in the previous {Plural(window.BaselineWeeks, "week")} to compare with."
                    : $"These insights are checked against the Performance Summary thresholds only - only {Plural(baselineWeeks, "week")} of data for the same hours in the previous {Plural(window.BaselineWeeks, "week")}, so there's no baseline to compare with yet.";
            }
            else
            {
                note = $"These insights compare the date range with its baseline: the median of the same time and day of the week in the previous {Plural(window.BaselineWeeks, "week")}"
                    + (baselineWeeks < window.BaselineWeeks ? $" ({baselineWeeks} with data)" : string.Empty)
                    + ", each hour weighted by how much of the range it covers. The dashed Baseline lines on the charts are the same median, hour by hour.";
            }
            if (shown == 0)
            {
                note = (window.IsCompared && enoughBaseline
                    ? "CPU, IO latency and waits were within thresholds and in line with baseline for the time. "
                    : "CPU, IO latency and waits were within thresholds. ") + note;
            }
            if (window.IsRounded) note += " The figures are hourly, so an hour the range only partly covers counts for its share of that hour's figures.";
            if (input.HasTimeFilter) note += " Only the hours and days in the time filter are counted.";
            return new PerformanceInsightItem { Level = InsightLevel.Info, Category = InsightCategory.Summary, IsSummary = true, Text = note };
        }

        private static string Threshold(PerformanceFinding f) => f.Severity == PerformanceSeverity.Critical ? "critical" : "warning";

        private static (string Text, string Tab)? TabFor(string metric) => metric switch
        {
            "Waits" or "CriticalWaits" or "LockWaits" => ("Waits", "Waits"),
            "Blocking" => ("Running Queries", "RunningQueries"),
            "SlowQueries" or "FailedSlowQueries" => ("Slow Queries", "SlowQueries"),
            _ => null
        };

        /// <summary>
        /// The hour to zoom to for a finding: the one furthest above its baseline (worse than baseline), or with the highest figure (over a threshold).
        /// Null for a measure without hourly figures
        /// </summary>
        internal static int? WorstHour(string metric, List<PerformanceHour> counted, PerformanceData data, bool byExcess)
        {
            var full = PerformanceAnalyzer.FullHourCPUSamples(counted);
            double? Value(PerformanceHour h) => metric == "CPU" && h.CPUSamples < DayStats.MinSamplesForPeakHour
                ? null
                : PerformanceAnalyzer.HourlyValue(metric, h, data.Thresholds.MinIOPs, full);
            var baseline = byExcess ? PerformanceAnalyzer.BaselineByHour(counted, data.BaselineWeeks, Value) : null;
            var best = counted.Where(h => h.Period == 0)
                .Select(h => (h.HourOffset, Value: Value(h)))
                .Where(h => h.Value.HasValue)
                .Select(h => (h.HourOffset, Score: h.Value!.Value - (baseline != null && baseline.TryGetValue(h.HourOffset, out var u) ? u : 0)))
                .OrderByDescending(h => h.Score)
                .ThenBy(h => h.HourOffset)
                .FirstOrDefault();
            return best == default || best.Score <= 0 ? null : best.HourOffset;
        }

        #endregion

        #region Tempdb contention

        /// <summary>
        /// Sessions waiting at once in a snapshot that make tempdb contention worth a card of its own - the same as the running queries
        /// snapshot insights' allocation contention threshold
        /// </summary>
        public const int TempdbContentionMinWaiting = 3;

        /// <summary>Share of snapshots (%) with any tempdb contention that makes it worth a card of its own, however few sessions</summary>
        public const int TempdbContentionMinPct = 5;

        /// <summary>Sessions waiting at once, or share of snapshots (%), that make it critical</summary>
        public const int TempdbContentionCriticalWaiting = 10;
        public const int TempdbContentionCriticalPct = 25;

        private static string AllocationAdvice => "more tempdb data files of equal size usually helps";
        private static string MetadataAdvice => "usually from creating and dropping temp tables at a high rate - memory-optimized tempdb metadata (SQL Server 2019+) can help";

        /// <summary>What the running queries snapshots show behind page latch waits - added to the waits card</summary>
        private static string TempdbInWaits(InsightTempdbContention t, TimeFormatter fmt)
        {
            if (t.AllocationSnapshots == 0 && t.MetadataSnapshots == 0)
            {
                return $"The running queries snapshots ({t.SnapshotsWithTypes:N0} in the range) show no sessions waiting on tempdb pages, so the page latch waits are more likely on a user database page - " +
                       "e.g. inserts all going to the last page of an index.";
            }
            var parts = new List<string>();
            if (t.AllocationSnapshots > 0) parts.Add($"allocation (PFS/GAM/SGAM pages) {ContentionFigures(t.AllocationSnapshots, t.SnapshotsWithTypes, t.MaxAllocationWaiting, t.WorstAllocationSnapshotUtc, fmt)} - {AllocationAdvice}");
            if (t.MetadataSnapshots > 0) parts.Add($"metadata (other tempdb pages) {ContentionFigures(t.MetadataSnapshots, t.SnapshotsWithTypes, t.MaxMetadataWaiting, t.WorstMetadataSnapshotUtc, fmt)} - {MetadataAdvice}");
            return $"Tempdb contention in the running queries snapshots: {string.Join("; ", parts)}.";
        }

        /// <summary>A card for tempdb contention no waits card has explained, when it's enough to matter</summary>
        private static void AddTempdbContention(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            if (input.Context?.Tempdb is not { SnapshotsWithTypes: > 0 } t) return;
            Add("Tempdb allocation contention", "sessions waiting on allocation pages (PFS/GAM/SGAM)", AllocationAdvice,
                t.AllocationSnapshots, t.MaxAllocationWaiting, t.MaxAllocationWaitMs, t.WorstAllocationSnapshotUtc);
            Add("Tempdb metadata contention", "sessions waiting on other tempdb pages", MetadataAdvice,
                t.MetadataSnapshots, t.MaxMetadataWaiting, t.MaxMetadataWaitMs, t.WorstMetadataSnapshotUtc);

            void Add(string title, string what, string advice, int snapshots, int maxWaiting, long maxWaitMs, DateTime? worst)
            {
                if (snapshots == 0) return;
                var pct = snapshots * 100.0 / t.SnapshotsWithTypes;
                var significant = maxWaiting >= TempdbContentionMinWaiting || (pct >= TempdbContentionMinPct && snapshots >= 2);
                if (!significant) return;
                var card = new PerformanceInsightItem
                {
                    Level = maxWaiting >= TempdbContentionCriticalWaiting || pct >= TempdbContentionCriticalPct ? InsightLevel.Critical : InsightLevel.Warning,
                    Category = InsightCategory.Tempdb
                };
                var waited = maxWaitMs <= 0 ? string.Empty : $", {WaitText(maxWaitMs)} waiting in total";
                var links = new List<string>();
                if (worst is { } at) links.Add(link(card, $"Zoom to {fmt.Time(at)}", from: at.AddMinutes(-10), to: at.AddMinutes(10)));
                links.Add(link(card, "Running Queries", "RunningQueries"));
                card.Text = $"{title}: {what} {ContentionFigures(snapshots, t.SnapshotsWithTypes, maxWaiting, worst, fmt)}{waited}. " +
                            $"{Capitalise(advice)}. {string.Join(" · ", links)}";
                cards.Add(card);
            }
        }

        /// <summary>"in 12 of 240 snapshots (5%), up to 8 sessions at once (14:05)"</summary>
        private static string ContentionFigures(int snapshots, int total, int maxWaiting, DateTime? worst, TimeFormatter fmt)
        {
            var pct = snapshots * 100.0 / Math.Max(1, total);
            var pctText = pct < 1 ? "<1" : pct.ToString("0", CultureInfo.InvariantCulture);
            return $"in {snapshots:N0} of {total:N0} snapshots ({pctText}%), up to {Plural(maxWaiting, "session")} at once" +
                   (worst is { } at ? $" ({fmt.Time(at)})" : string.Empty);
        }

        private static string WaitText(long ms) => ms < 1000 ? $"{ms:N0} ms" : PerformanceAnalyzer.DurationMs(ms);

        #endregion

        #region Data collection

        /// <summary>Hours of the window with no CPU or wait figures, and collection errors - either means the charts and cards are incomplete</summary>
        private static void AddCollectionGaps(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var errors = input.Context?.CollectionErrors ?? new List<InsightCollectionError>();
            var gaps = new List<int>();
            var instance = input.Performance?.Instances.FirstOrDefault(i => i.InstanceID == input.InstanceID);
            if (instance != null && !input.Window.IsTooLong)
            {
                var data = input.Performance;
                var current = instance.Hours.Where(h => h.Period == 0 && (h.CPUSamples > 0 || h.WaitSampleMs > 0)).Select(h => h.HourOffset).ToHashSet();
                // Nothing at all is said by the performance card.  Hours before the instance's first figures aren't gaps when it's new
                if (current.Count > 0)
                {
                    var hasHistory = instance.Hours.Any(h => h.Period > 0);
                    var first = hasHistory ? 0 : current.Min();
                    for (var hour = first; hour < data.WindowHours; hour++)
                    {
                        var start = data.WindowStartUtc.AddHours(hour);
                        if (start + CollectionAllowance > input.NowUtc) break;
                        if (!data.IsIncluded(0, hour)) continue;
                        if (!current.Contains(hour)) gaps.Add(hour);
                    }
                }
            }
            var errorCount = errors.Sum(e => e.Errors);
            if (gaps.Count == 0 && errorCount == 0) return;

            var card = new PerformanceInsightItem
            {
                Level = gaps.Count > 0 ? InsightLevel.Warning : InsightLevel.Info,
                Category = InsightCategory.DataCollection
            };
            var parts = new List<string>();
            if (gaps.Count > 0)
            {
                var start = input.Performance.WindowStartUtc;
                var ranges = Ranges(gaps).Select(r => fmt.Range(start.AddHours(r.First), start.AddHours(r.Last + 1))).ToList();
                parts.Add($"No CPU or wait figures for {Plural(gaps.Count, "hour")} ({List(ranges)}) - the charts and figures here are incomplete.");
            }
            if (errorCount > 0)
            {
                var bySource = errors.OrderByDescending(e => e.Errors).Select(e => $"{e.Source} {e.Errors:N0}").ToList();
                parts.Add($"{Plural(errorCount, "collection error")} ({List(bySource)}). {link(card, "DBA Dash Error Log", "DBADashErrorLog")}");
            }
            parts.Add(link(card, "Collection Health", "CollectionHealth"));
            card.Text = string.Join(" ", parts);
            cards.Add(card);
        }

        #endregion

        #region What happened

        private static void AddOffline(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var offline = input.Context?.Offline;
            if (offline is not { Count: > 0 }) return;
            var card = new PerformanceInsightItem
            {
                Level = offline.Any(o => o.IsCurrent) ? InsightLevel.Critical : InsightLevel.Warning,
                Category = InsightCategory.Offline
            };
            var periods = offline.OrderBy(o => o.FirstFailUtc).Select(o => o.IsCurrent
                ? $"since {fmt.Time(o.FirstFailUtc)} (still offline)"
                : fmt.Range(o.FirstFailUtc, o.LastFailUtc)).ToList();
            card.Text = $"DBA Dash couldn't connect to the instance {List(periods)} - nothing was collected while it couldn't. {link(card, "Offline Instances", "OfflineInstances")}";
            cards.Add(card);
        }

        private static void AddRestart(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            if (input.Context?.StartTimeUtc is not { } started) return;
            if (started < input.Window.FromUtc || started >= input.Window.ToUtc) return;
            var card = new PerformanceInsightItem { Level = InsightLevel.Warning, Category = InsightCategory.Restart };
            card.Text = $"SQL Server restarted at {fmt.Time(started)}. Waits, IO and query stats start again from zero, and the plan cache and buffer pool are empty for a while after, so figures either side aren't comparable. "
                + link(card, $"Zoom to {fmt.Time(started)}", from: started.AddMinutes(-30), to: started.AddMinutes(30));
            cards.Add(card);
        }

        private static void AddFailovers(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var changes = input.Context?.RoleChanges;
            if (changes is not { Count: > 0 }) return;
            var card = new PerformanceInsightItem { Level = InsightLevel.Warning, Category = InsightCategory.Failover };
            var listed = changes.OrderBy(c => c.ChangedUtc)
                .Select(c => $"{c.AvailabilityGroup} {c.PreviousRole?.ToLowerInvariant()} to {c.NewRole?.ToLowerInvariant()} at {fmt.Time(c.ChangedUtc)}")
                .ToList();
            var last = changes.Max(c => c.ChangedUtc);
            card.Text = $"Availability group role {(changes.Count == 1 ? "change" : "changes")}: {List(listed)}. "
                + link(card, $"Zoom to {fmt.Time(last)}", from: last.AddMinutes(-30), to: last.AddMinutes(30)) + " · "
                + link(card, "Availability Groups", "AG");
            cards.Add(card);
        }

        private static void AddAlerts(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var alerts = input.Context?.Alerts?.Where(a => !a.IsBlackout).ToList();
            if (alerts is not { Count: > 0 }) return;
            // Open at the end of the range: still open, or resolved since - looking back at Monday, an alert resolved on Wednesday was open
            // then.  Only the rest were resolved within the range
            bool OpenAtEnd(InsightAlert a) => a.IsOpen || a.ResolvedDateUtc is not { } resolvedAt || resolvedAt >= input.Window.ToUtc;
            var open = alerts.Where(OpenAtEnd).OrderBy(a => a.Priority).ThenByDescending(a => a.TriggerDateUtc).ToList();
            var resolved = alerts.Where(a => !OpenAtEnd(a)).OrderBy(a => a.Priority).ThenByDescending(a => a.TriggerDateUtc).ToList();
            var stillOpen = open.Where(a => a.IsOpen).ToList();
            var card = new PerformanceInsightItem
            {
                // By the alerts still open: resolved ones are history - worth knowing, not an issue now
                Level = stillOpen.Count == 0 ? InsightLevel.Info
                    : stillOpen.Min(a => a.Priority) <= 10 ? InsightLevel.Critical
                    : stillOpen.Min(a => a.Priority) <= 20 ? InsightLevel.Warning
                    : InsightLevel.Info,
                Category = InsightCategory.Alerts
            };
            var parts = new List<string>();
            if (open.Count > 0)
            {
                var when = stillOpen.Count == open.Count ? "open" : "open at the end of this period";
                parts.Add($"{Plural(open.Count, "alert")} {when}: {List(open.Select(a => AlertText(a, fmt, sinceResolved: !a.IsOpen)).ToList(), open.Count)}.");
            }
            if (resolved.Count > 0)
            {
                parts.Add(open.Count == 0
                    ? $"{Plural(resolved.Count, "alert")} resolved in this period: {List(resolved.Select(a => AlertText(a, fmt)).ToList(), resolved.Count)}."
                    : $"{resolved.Count:N0} more resolved in this period.");
            }
            parts.Add(link(card, "Alerts", "Alerts"));
            card.Text = string.Join(" ", parts);
            cards.Add(card);
        }

        /// <param name="sinceResolved">Open at the end of the range but resolved after it</param>
        private static string AlertText(InsightAlert a, TimeFormatter fmt, bool sinceResolved = false)
        {
            var message = Shorten(a.Message ?? a.AlertKey, 120);
            var what = string.IsNullOrEmpty(a.AlertType) || (message?.StartsWith(a.AlertType, StringComparison.OrdinalIgnoreCase) ?? false)
                ? message
                : $"{a.AlertType} - {message}";
            return $"{what} ({PriorityName(a.Priority)}, raised {fmt.Time(a.TriggerDateUtc)}{(sinceResolved ? ", since resolved" : string.Empty)})";
        }

        private static string PriorityName(int priority) => priority switch
        {
            0 => "critical",
            <= 10 => "high",
            <= 20 => "medium",
            <= 30 => "low",
            _ => "information"
        };

        private static void AddCounters(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, LinkFactory link)
        {
            var counters = input.Counters;
            if (counters is not { Count: > 0 }) return;
            var card = new PerformanceInsightItem
            {
                Level = counters.Any(c => c.Status == 1) ? InsightLevel.Critical : InsightLevel.Warning,
                Category = InsightCategory.Counters
            };
            var listed = counters.OrderBy(c => c.Status).ThenBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(c => $"{c.DisplayName} {c.Measure.ToLowerInvariant()} {Number(c.Value)} ({(c.Status == 1 ? "critical" : "warning")})")
                .ToList();
            card.Text = $"{Plural(counters.Count, "performance counter")} outside {(counters.Count == 1 ? "its" : "their")} thresholds: {List(listed, counters.Count)}. {link(card, "Metrics", "Metrics")}";
            cards.Add(card);
        }

        private static void AddJobs(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var jobs = input.Context?.Jobs;
            if (jobs is not { Count: > 0 }) return;
            var failed = jobs.Where(j => j.Failed).OrderByDescending(j => j.StartUtc).ToList();
            if (failed.Count > 0)
            {
                var card = new PerformanceInsightItem { Level = InsightLevel.Warning, Category = InsightCategory.Jobs };
                var listed = failed.Select(j => $"{j.JobName} at {fmt.Time(j.StartUtc)} ({(j.RunStatus == 3 ? "cancelled" : "failed")})").ToList();
                card.Text = $"{Plural(failed.Count, "job run")} failed or {(failed.Count == 1 ? "was" : "were")} cancelled: {List(listed, failed.Count)}. {link(card, "Jobs", "Jobs")}";
                cards.Add(card);
            }

            var longRuns = jobs.Where(j => !j.Failed).OrderByDescending(j => j.DurationSec).ToList();
            if (longRuns.Count > 0)
            {
                var card = new PerformanceInsightItem { Level = InsightLevel.Info, Category = InsightCategory.Jobs };
                var listed = longRuns.Select(j => $"{j.JobName} {fmt.Time(j.StartUtc)} for {PerformanceAnalyzer.Duration(j.DurationSec / 60.0)}").ToList();
                card.Text = $"Long running {(longRuns.Count == 1 ? "job" : "jobs")} (5 minutes or more): {List(listed, longRuns.Count)}. {link(card, "Job Timeline", "JobTimeline")}";
                cards.Add(card);
            }
        }

        private static void AddChanges(PerformanceInsightsInput input, List<PerformanceInsightItem> cards, TimeFormatter fmt, LinkFactory link)
        {
            var changes = input.Context?.ConfigChanges;
            if (changes is not { Count: > 0 }) return;
            var card = new PerformanceInsightItem { Level = InsightLevel.Info, Category = InsightCategory.Changes };
            var listed = changes.OrderBy(c => c.ChangeDateUtc).Select(c => c.ChangeType switch
            {
                "Schema" => $"{Plural(c.Count ?? 0, "object")} created, changed or dropped in {c.Name}",
                "Trace flag" => $"{c.Name} turned {c.NewValue?.ToLowerInvariant()} at {fmt.Time(c.ChangeDateUtc)}",
                _ => $"{c.Name} {c.OldValue} to {c.NewValue} at {fmt.Time(c.ChangeDateUtc)}"
            }).ToList();
            var links = new List<string>();
            if (changes.Any(c => c.ChangeType == "Configuration")) links.Add(link(card, "Configuration", "Configuration"));
            if (changes.Any(c => c.ChangeType == "Trace flag")) links.Add(link(card, "Trace Flags", "TraceFlags"));
            if (changes.Any(c => c.ChangeType == "Patching")) links.Add(link(card, "SQL Patching", "SQLPatching"));
            card.Text = $"{(changes.Count == 1 ? "Change" : "Changes")} in this period: {List(listed, changes.Count)}." + (links.Count > 0 ? " " + string.Join(" · ", links) : string.Empty);
            cards.Add(card);
        }

        #endregion

        #region Text

        /// <summary>Times in the user's time zone, with as much of the date as the window needs</summary>
        internal sealed class TimeFormatter
        {
            private readonly TimeZoneInfo tz;
            private readonly string format;

            public TimeFormatter(InsightWindow window, TimeZoneInfo tz)
            {
                this.tz = tz ?? TimeZoneInfo.Utc;
                var start = Local(window.StartUtc);
                var end = Local(window.EndUtc.AddSeconds(-1));
                format = start.Date == end.Date ? "HH:mm"
                    : window.Hours <= InsightWindow.MaxBaselineHours ? "ddd HH:mm"
                    : "d MMM HH:mm";
            }

            private DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);

            public string Time(DateTime utc) => Local(utc).ToString(format, CultureInfo.InvariantCulture);

            /// <summary>"02:00-05:00", or "Mon 22:00-Tue 02:00" - the end without the day when it's the same day</summary>
            public string Range(DateTime fromUtc, DateTime toUtc)
            {
                var from = Local(fromUtc);
                var to = Local(toUtc);
                var toText = from.Date == to.Date || (to.TimeOfDay == TimeSpan.Zero && to.Date == from.Date.AddDays(1))
                    ? to.ToString("HH:mm", CultureInfo.InvariantCulture)
                    : to.ToString(format, CultureInfo.InvariantCulture);
                return $"{from.ToString(format, CultureInfo.InvariantCulture)}-{toText}";
            }
        }

        /// <summary>Consecutive hours as ranges: 1,2,3,7 => (1,3),(7,7)</summary>
        internal static List<(int First, int Last)> Ranges(IEnumerable<int> hours)
        {
            var result = new List<(int, int)>();
            foreach (var h in hours.Distinct().OrderBy(h => h))
            {
                if (result.Count > 0 && result[^1].Item2 == h - 1) result[^1] = (result[^1].Item1, h);
                else result.Add((h, h));
            }
            return result;
        }

        /// <summary>The first MaxListed items, then "and N more" - <paramref name="total"/> counts items not passed in</summary>
        internal static string List(IReadOnlyList<string> items, int? total = null)
        {
            var count = total ?? items.Count;
            var shown = items.Take(MaxListed).ToList();
            var more = count - shown.Count;
            return string.Join("; ", shown) + (more > 0 ? $" and {more:N0} more" : string.Empty);
        }

        private static string Plural(int count, string word) => count == 1 ? $"1 {word}" : $"{count:N0} {word}s";

        private static string Capitalise(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

        private static string Shorten(string s, int length)
        {
            if (s == null) return null;
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length <= length ? s : s[..(length - 3)].TrimEnd() + "...";
        }

        private static string Number(double v) => v.ToString(Math.Abs(v) >= 100 ? "#,0" : "#,0.##", CultureInfo.InvariantCulture);

        #endregion
    }
}
