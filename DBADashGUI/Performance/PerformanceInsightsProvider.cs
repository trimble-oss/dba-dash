using DBADash.DailyReport;
using DBADash.PerformanceInsights;
using DBADashGUI.Charts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// The hourly figures for the date range and the same hours in the previous weeks (dbo.DailyReport_Performance_Get), shared by the
    /// Performance tab's insight cards and the charts' "baseline" lines so a refresh reads them once.  Kept for a short time only: long enough
    /// for the cards and every chart on the tab to load from one read, short enough that a refresh a minute later reads new figures.
    /// </summary>
    internal static class PerformanceBaselineCache
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

        private static readonly ConcurrentDictionary<string, (DateTime Created, Lazy<Task<PerformanceData>> Data)> Cache = new();

        /// <summary>The hours read for the global date range - see <see cref="CurrentWindow"/></summary>
        internal sealed record BaselineWindow(InsightWindow Window, HashSet<(int Period, int HourOffset)> IncludedHours, Dictionary<int, int> PeriodShiftHours);

        /// <summary>
        /// The window for the global date range, with the hours its time of day and day of week filters keep (null: all), and each baseline
        /// week's shift for a clock change in the app's time zone, so it covers the same local hours
        /// </summary>
        public static BaselineWindow CurrentWindow()
        {
            var window = InsightWindow.For(DateRange.FromUTC, DateRange.ToUTC);
            var shifts = PerformanceData.PeriodShifts(window.EndUtc, window.Hours, window.BaselineWeeks, DateHelper.AppTimeZone);
            if (!DateRange.HasTimeOfDayFilter && !DateRange.HasDayOfWeekFilter) return new BaselineWindow(window, null, shifts);
            var hours = DateRange.HasTimeOfDayFilter ? DateRange.TimeOfDay.ToHashSet() : null;
            // 1 = Monday, as the day of week filter (SET DATEFIRST 1)
            var days = DateRange.HasDayOfWeekFilter ? DateRange.DayOfWeek.ToHashSet() : null;
            var included = PerformanceData.HoursFor(window.EndUtc, window.Hours, window.BaselineWeeks, DateHelper.AppTimeZone,
                local => (hours == null || hours.Contains(local.Hour)) && (days == null || days.Contains(((int)local.DayOfWeek + 6) % 7 + 1)),
                shifts);
            return new BaselineWindow(window, included, shifts);
        }

        /// <param name="detail">
        /// True for everything the insight cards need.  False for the charts' baseline lines, which need only CPU, IO and total waits - a
        /// detailed read already made (or under way) for the same hours is used for those too, so the Performance tab still reads once
        /// </param>
        public static Task<PerformanceData> GetAsync(int instanceId, BaselineWindow w, bool detail)
        {
            var baseKey = $"{Common.ConnectionString?.GetHashCode()}|{instanceId}|{w.Window.Key}|{PerformanceData.HoursParameter(w.IncludedHours)}|{PerformanceData.PeriodShiftParameter(w.PeriodShiftHours)}";
            var now = DateTime.UtcNow;
            foreach (var expired in Cache.Where(c => now - c.Value.Created > Lifetime).Select(c => c.Key).ToList())
            {
                Cache.TryRemove(expired, out _);
            }
            if (!detail && Cache.TryGetValue(baseKey + "|detail", out var detailed))
            {
                return detailed.Data.Value;
            }
            var key = baseKey + (detail ? "|detail" : "|lines");
            var connectionString = Common.ConnectionString;
            var entry = Cache.GetOrAdd(key, _ => (now, new Lazy<Task<PerformanceData>>(() => Task.Run(() =>
                PerformanceDataRepository.Get(connectionString, instanceId.ToString(), w.Window.EndUtc, w.Window.Hours, w.Window.BaselineWeeks,
                    w.IncludedHours, Config.DefaultCommandTimeout, w.PeriodShiftHours, detail)))));
            var task = entry.Data.Value;
            // A failed read isn't kept - the next refresh tries again
            task.ContinueWith(_ => Cache.TryRemove(key, out var _), TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }

        /// <summary>
        /// A dashed "baseline" line for a chart: for each hour of the date range, the median of <paramref name="value"/> for that hour in the
        /// baseline weeks.  Drawn flat across each hour, as the figure is for the hour.  Null when there's no baseline to draw.
        /// </summary>
        public static ChartOverlayData BaselineOverlay(PerformanceData data, int instanceId, InsightWindow window, Func<PerformanceHour, double?> value)
        {
            var instance = data?.Instances.FirstOrDefault(i => i.InstanceID == instanceId);
            if (instance == null || data.BaselineWeeks == 0) return null;
            var counted = instance.Hours.Where(h => data.IsIncluded(h.Period, h.HourOffset)).ToList();
            // Too little history to call anything a baseline - as the insight cards
            if (counted.Where(h => h.Period > 0).Select(h => h.Period).Distinct().Count() < PerformanceInsightBuilder.MinBaselineWeeks) return null;
            var baseline = PerformanceAnalyzer.BaselineByHour(counted, data.BaselineWeeks, value);
            if (baseline.Count == 0) return null;

            var table = new DataTable();
            table.Columns.Add("Time", typeof(DateTime));
            table.Columns.Add("Baseline", typeof(double));
            foreach (var (hour, v) in baseline)
            {
                if (!data.IsIncluded(0, hour)) continue;
                var start = data.WindowStartUtc.AddHours(hour);
                var end = start.AddHours(1);
                // Only the part of the hour the chart shows
                if (start < window.FromUtc) start = window.FromUtc;
                if (end > window.ToUtc) end = window.ToUtc;
                if (end <= start) continue;
                table.Rows.Add(start.ToAppTimeZone(), v);
                table.Rows.Add(end.ToAppTimeZone(), v);
            }
            if (table.Rows.Count == 0) return null;

            var overlay = new ChartOverlay
            {
                XColumn = "Time",
                MetricColumns = new[] { "Baseline" },
                ChartType = ChartTypes.Line,
                LineSmoothness = 0,
                GeometrySize = 0,
                SeriesNames = new Dictionary<string, string> { { "Baseline", $"Baseline (median of the previous {data.BaselineWeeks} weeks)" } },
                SeriesStyles = new Dictionary<string, ChartSeriesStyle>
                {
                    { "Baseline", ChartSeriesStyle.FromColor(DashColors.Gray4) with { StrokeThickness = 2, Dashed = true } }
                }
            };
            return new ChartOverlayData(overlay, table);
        }

        private static bool showBaseline = Properties.Settings.Default.ShowBaseline;

        /// <summary>
        /// Draw the baseline lines on the CPU, Waits and IO charts.  One setting for every chart, remembered for the user: set from the main
        /// toolbar or any chart's toolbar, and <see cref="ShowBaselineChanged"/> tells the rest
        /// </summary>
        public static bool ShowBaseline
        {
            get => showBaseline;
            set
            {
                if (showBaseline == value) return;
                showBaseline = value;
                try
                {
                    Properties.Settings.Default.ShowBaseline = value;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Baseline setting not saved: {ex.Message}");
                }
                ShowBaselineChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary><see cref="ShowBaseline"/> was changed - by the main toolbar or a chart</summary>
        public static event EventHandler ShowBaselineChanged;

        public const string BaselineToolTip = "Baseline: a dashed line at the median of the same hours (same time and day of the week) in the previous 4 weeks.\nDate ranges of up to a week only.";

        /// <summary>
        /// A chart's toolbar button that turns the baseline lines on and off - for every chart, as the main toolbar's does.  <paramref name="changed"/>
        /// is called on the chart's thread whenever the setting changes, from wherever
        /// </summary>
        public static System.Windows.Forms.ToolStripButton CreateBaselineButton(Action changed)
        {
            var button = new System.Windows.Forms.ToolStripButton("Baseline")
            {
                CheckOnClick = true,
                Checked = ShowBaseline,
                ToolTipText = BaselineToolTip,
                DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image,
                Image = Properties.Resources.Baseline_16x,
                AccessibleName = "Baseline"
            };
            // Clicking it sets the shared setting; the change event then updates this button and every other one
            button.CheckedChanged += (_, _) => ShowBaseline = button.Checked;
            EventHandler onChanged = (_, _) =>
            {
                if (button.IsDisposed) return;
                button.Checked = ShowBaseline;
                changed();
            };
            ShowBaselineChanged += onChanged;
            button.Disposed += (_, _) => ShowBaselineChanged -= onChanged;
            return button;
        }

        /// <summary>The baseline line for an instance and the global date range, or null when it's not compared or can't be read</summary>
        public static async Task<ChartOverlayData> BaselineOverlayAsync(int instanceId, Func<PerformanceHour, double?> value)
        {
            var w = CurrentWindow();
            if (!w.Window.IsCompared || instanceId <= 0) return null;
            try
            {
                var data = await GetAsync(instanceId, w, detail: false);
                return BaselineOverlay(data, instanceId, w.Window, value);
            }
            catch (Exception ex)
            {
                // The chart means something without its baseline line
                System.Diagnostics.Debug.WriteLine($"Baseline line not drawn: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>The Performance tab's insight cards - see <see cref="PerformanceInsightBuilder"/></summary>
    internal static class PerformanceInsightsProvider
    {
        public static async Task<List<Insight>> GetAsync(DBADashContext context, CancellationToken ct)
        {
            var instanceId = context?.InstanceID ?? 0;
            if (instanceId <= 0) return null;
            var w = PerformanceBaselineCache.CurrentWindow();
            var window = w.Window;
            var connectionString = Common.ConnectionString;
            var timeout = Config.DefaultCommandTimeout;
            var errors = new List<string>();

            // Started before the first await, so a caller on the UI thread has the detailed read under way before the charts ask for their
            // lines - they then use it rather than reading the same hours again
            var perfTask = window.IsTooLong ? Task.FromResult<PerformanceData>(null) : PerformanceBaselineCache.GetAsync(instanceId, w, detail: true);
            var contextTask = Task.Run(() => PerformanceInsightsRepository.GetContext(connectionString, instanceId, window.FromUtc, window.ToUtc, timeout), ct);
            // The same time filter as the figures and charts - read here, on the calling thread
            var hours = DateRange.HasTimeOfDayFilter ? DateRange.TimeOfDay.ToList() : null;
            var days = DateRange.HasDayOfWeekFilter ? DateRange.DayOfWeek.ToList() : null;
            var utcOffset = DateHelper.UtcOffset;
            var countersTask = Task.Run(() => PerformanceInsightsRepository.GetCounters(connectionString, instanceId, window.FromUtc, window.ToUtc, timeout, hours, days, utcOffset), ct);

            var perf = await Read(perfTask, "CPU, IO and wait figures", errors);
            var insightsContext = await Read(contextTask, "alerts, restarts and changes", errors);
            var counters = await Read(countersTask, "performance counters", errors);
            ct.ThrowIfCancellationRequested();

            var items = PerformanceInsightBuilder.Build(new PerformanceInsightsInput
            {
                InstanceID = instanceId,
                Window = window,
                Performance = perf,
                Context = insightsContext,
                Counters = counters,
                TimeZone = DateHelper.AppTimeZone,
                HasTimeFilter = w.IncludedHours != null,
                Errors = errors
            });
            return items.Select(i => ToInsight(i, instanceId)).ToList();
        }

        private static async Task<T> Read<T>(Task<T> task, string what, List<string> errors) where T : class
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"Couldn't read {what} for the insights: {ex.Message}");
                return null;
            }
        }

        private static Insight ToInsight(PerformanceInsightItem item, int instanceId)
        {
            var severity = item.Level switch
            {
                InsightLevel.Critical => InsightSeverity.Critical,
                InsightLevel.Warning => InsightSeverity.Warning,
                _ => InsightSeverity.Info
            };
            var actions = new Dictionary<string, Action>();
            foreach (var link in item.Links)
            {
                if (link.ZoomFromUtc is { } from && link.ZoomToUtc is { } to)
                {
                    actions[link.Key] = () => Main.MainFormInstance?.SetDateRangeUtc(from, to);
                }
                else if (Enum.TryParse<Main.Tabs>(link.Tab, out var tab))
                {
                    actions[link.Key] = () => Main.MainFormInstance?.Instance_Selected(Main.MainFormInstance,
                        new Main.InstanceSelectedEventArgs { InstanceID = instanceId, Tab = tab, SearchFromRoot = true });
                }
            }
            return new Insight(severity, item.Text, actions) { IsSummary = item.IsSummary };
        }
    }

    /// <summary>
    /// A chart's dashed baseline lines and the toolbar button that turns them on and off - one line per figure the chart can show (average
    /// and max CPU, say).  Read after the chart is drawn so it never waits for them, and dropped when a newer refresh starts.
    /// </summary>
    internal sealed class BaselineLines
    {
        private readonly System.Windows.Forms.Control owner;
        private readonly Action render;
        private readonly Func<bool> applies;
        private readonly (string Key, Func<PerformanceHour, double?> Value)[] lines;
        private readonly Dictionary<string, ChartOverlayData> overlays = new();
        private int refresh;
        private int instanceId;

        public System.Windows.Forms.ToolStripButton Button { get; }

        /// <param name="owner">The chart control - lines arriving after it's disposed are dropped</param>
        /// <param name="render">Redraws the chart, picking its lines up with <see cref="For"/></param>
        /// <param name="applies">Whether the chart is showing something the lines fit - e.g. not one wait type, or one drive</param>
        public BaselineLines(System.Windows.Forms.Control owner, Action render, Func<bool> applies, params (string Key, Func<PerformanceHour, double?> Value)[] lines)
        {
            this.owner = owner;
            this.render = render;
            this.applies = applies ?? (() => true);
            this.lines = lines;
            Button = PerformanceBaselineCache.CreateBaselineButton(Changed);
        }

        /// <summary>Call when the chart starts loading new data: the lines for the old data are dropped and any still loading ignored</summary>
        public void Reset()
        {
            refresh++;
            overlays.Clear();
        }

        /// <summary>Read the lines for the instance and the global date range, and redraw the chart when they're ready</summary>
        public async void Load(int instance)
        {
            instanceId = instance;
            var current = refresh;
            if (!Button.Checked || !applies()) return;
            var loaded = new Dictionary<string, ChartOverlayData>();
            foreach (var (key, value) in lines)
            {
                var overlay = await PerformanceBaselineCache.BaselineOverlayAsync(instance, value);
                if (current != refresh || owner.IsDisposed) return;
                if (overlay != null) loaded[key] = overlay;
            }
            if (loaded.Count == 0) return;
            foreach (var (key, overlay) in loaded) overlays[key] = overlay;
            render();
        }

        /// <summary>
        /// Read the lines if they apply now but weren't read when the chart loaded - the chart switched to a measure they fit, say.  Does
        /// nothing when they're off, don't fit, or are already read
        /// </summary>
        public void EnsureLoaded()
        {
            if (Button.Checked && applies() && overlays.Count == 0 && instanceId > 0) Load(instanceId);
        }

        /// <summary>The line for <paramref name="key"/> as the overlays ChartHelper takes, or null when it's off, doesn't fit or isn't loaded</summary>
        public IReadOnlyList<ChartOverlayData> For(string key) =>
            Button.Checked && applies() && overlays.TryGetValue(key, out var overlay) ? new[] { overlay } : null;

        private void Changed()
        {
            if (owner.IsDisposed) return;
            if (Button.Checked && overlays.Count == 0) Load(instanceId);
            else render();
        }
    }
}
