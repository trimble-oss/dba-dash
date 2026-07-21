using DBADashGUI.Charts;
using DBADashGUI.Theme;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounters : UserControl, IMetricChart, IThemedControl
    {
        public PerformanceCounters()
        {
            InitializeComponent();
            this.Controls.Add(lblError);
            lblError.BringToFront();
        }

        private void UpdateLegendMenuChecked()
        {
            try
            {
                // Ensure menu reflects the Metric's LegendPosition
                foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    var tag = menuItem.Tag?.ToString();
                    menuItem.Checked = tag != null && Metric != null && tag == Metric.LegendPosition.ToString();
                    if (menuItem.Checked)
                    {
                        tsLegend.Text = menuItem.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible;
            set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime FromDate { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ToDate { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CounterID { get => Metric.CounterID; set => Metric.CounterID = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CounterName { get => Metric.CounterName; set => Metric.CounterName = value; }

        private PerformanceCounterMetric _metric = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PerformanceCounterMetric Metric
        { get => _metric; set { _metric = value; EnsureCountersHaveAggregate(); UpdateLegendMenuChecked(); UpdateYAxisMenuChecked(); UpdateChartTypeMenuChecked(); } }

        IMetric IMetricChart.Metric => Metric;

        private bool smoothLines = false;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;
        private double lineSmoothness = 0;
        private Label lblError = new Label() { Dock = DockStyle.Fill, Visible = false, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                lineSmoothness = value ? ChartConfiguration.DefaultLineSmoothness : 0;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double PointSize
        {
            get => geometrySize;
            set => geometrySize = value;
        }

        private int durationMins;
        private int DateGrouping;
        private DataTable dt;

        public void RefreshData()
        {
            try
            {
                ToggleError(false);
                SetDateGroup(DateRange.DurationMins);
                dt = GetPerformanceCounter();
                RefreshChart();
            }
            catch (Exception ex)
            {
                ToggleError(true, ex.Message);
            }
        }

        private void ToggleError(bool show, string message = "")
        {
            lblError.Text = message;
            lblError.Visible = show;
            chart1.Visible = !show;
        }

        public void RefreshData(int instanceID)
        {
            InstanceID = instanceID;
            RefreshData();
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context == null) return;
            InstanceID = _context.InstanceID;
            FromDate = DateRange.FromUTC;
            ToDate = DateRange.ToUTC;
            RefreshData();
        }

        private void SetDateGroup(int mins)
        {
            if (durationMins == mins) return; // Change date group only if date range has changed.
            DateGrouping = DateHelper.DateGrouping(mins, 200);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            durationMins = mins;
        }

        private void RefreshChart()
        {
            tsTitle.SetTruncatedText(Metric.GetTitle());
            if (dt == null || dt.Rows.Count < 2)
            {
                chart1.Series = Array.Empty<ISeries>();
                chart1.XAxes = Array.Empty<Axis>();
                chart1.YAxes = Array.Empty<Axis>();
                return;
            }

            // Build series definitions using per-counter aggregation preferences
            // Each series is a (CounterID, CounterName, AggName) tuple that maps to a source column like Value_Avg
            var seriesDefs = new System.Collections.Generic.List<(int CounterID, string CounterName, string AggName, string ColumnName)>();
            if (dt.Columns.Contains("CounterID") && Metric.Counters != null && Metric.Counters.Count > 0)
            {
                foreach (var c in Metric.Counters)
                {
                    // A counter with no aggregate selected shows nothing (it stays in the Counters menu so
                    // the aggregate can be re-enabled).
                    foreach (var agg in c.GetAggColumns(includeCurrent: false))
                    {
                        // Map aggregation to column name produced by stored proc
                        string col;
                        if (agg == "SampleCount") col = "Value_SampleCount";
                        else if (agg == "Total") col = "Value_Total";
                        else col = "Value_" + agg;

                        if (dt.Columns.Contains(col))
                        {
                            seriesDefs.Add((c.CounterID, c.FullName, agg, col));
                        }
                    }
                }
            }
            if (seriesDefs.Count == 0)
            {
                ToggleError(true, "No valid counters or aggregates selected. Enable an aggregate from the Counters menu.");
                return;
            }

            // We have something to draw - clear any prior error so the chart is shown again
            // (RefreshChart can be called directly, e.g. from the Counters menu, bypassing RefreshData).
            ToggleError(false);

            // Build a flattened datatable where each counter+agg becomes a series (SeriesName) with SelectedValue
            var dtSeries = new DataTable();
            dtSeries.Columns.Add("SnapshotDate", typeof(DateTime));
            dtSeries.Columns.Add("SeriesName", typeof(string));
            dtSeries.Columns.Add("SelectedValue", typeof(double));
            // Pre-index series definitions by CounterID for O(1) lookup per row
            var seriesByCounter = seriesDefs
                .GroupBy(sd => sd.CounterID)
                .ToDictionary(g => g.Key, g => g.ToList());

            dtSeries.BeginLoadData();
            try
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (r["SnapshotDate"] == DBNull.Value) continue;
                    var snapshot = Convert.ToDateTime(r["SnapshotDate"]);
                    var rowCounterId = dt.Columns.Contains("CounterID") && r["CounterID"] != DBNull.Value ? Convert.ToInt32(r["CounterID"]) : 0;

                    if (!seriesByCounter.TryGetValue(rowCounterId, out var matches)) continue;

                    foreach (var s in matches)
                    {
                        var valObj = r[s.ColumnName];
                        if (valObj == null || valObj == DBNull.Value) continue;
                        var val = Convert.ToDouble(valObj);
                        var seriesName = s.CounterName + " - " + s.AggName;
                        dtSeries.LoadDataRow(new object[] { snapshot, seriesName, val }, false);
                    }
                }
            }
            finally
            {
                dtSeries.EndLoadData();
            }

            // Calculate min/max for Y-axis scaling from dtSeries.
            double maxValue = 0, minValue = 0;
            var isStacked = Metric.ChartType is ChartTypes.StackedArea or ChartTypes.StackedColumn;
            if (isStacked)
            {
                // For stacked charts the visual extent at each timestamp is the sum of the series,
                // not any individual value, so the axis must be scaled to the stacked total.
                // Positive and negative values stack in opposite directions - track them separately.
                var stackByDate = new Dictionary<DateTime, (double Positive, double Negative)>();
                foreach (DataRow r in dtSeries.Rows)
                {
                    var date = (DateTime)r["SnapshotDate"];
                    var value = Convert.ToDouble(r["SelectedValue"]);
                    stackByDate.TryGetValue(date, out var acc);
                    if (value >= 0) acc.Positive += value; else acc.Negative += value;
                    stackByDate[date] = acc;
                }
                foreach (var acc in stackByDate.Values)
                {
                    maxValue = acc.Positive > maxValue ? acc.Positive : maxValue;
                    minValue = acc.Negative < minValue ? acc.Negative : minValue;
                }
            }
            else
            {
                foreach (DataRow r in dtSeries.Rows)
                {
                    var value = Convert.ToDouble(r["SelectedValue"]);
                    maxValue = value > maxValue ? value : maxValue;
                    minValue = value < minValue ? value : minValue;
                }
            }

            // Adjust Y-axis limits
            if (maxValue == 0 && minValue == 0)
            {
                maxValue = 1;
            }
            maxValue *= 1.1;

            // Apply any user-configured fixed axis limits (overrides auto-scaling)
            if (Metric.YAxisMin.HasValue) minValue = Metric.YAxisMin.Value;
            if (Metric.YAxisMax.HasValue) maxValue = Metric.YAxisMax.Value;

            // A one-sided override can leave the bounds crossed (e.g. a user-set min above the auto-scaled
            // max). Keep min < max so the axis still renders instead of collapsing or throwing.
            if (maxValue <= minValue)
            {
                maxValue = minValue + (minValue == 0 ? 1 : Math.Abs(minValue) * 0.1);
            }

            // Auto-adjust point size based on data count (use flattened series table).
            // Scatter charts are nothing but points, so keep them visible (smaller) rather than
            // hiding them entirely on dense datasets the way line/area charts do.
            var isScatter = Metric.ChartType == ChartTypes.Scatter;
            var effectiveGeometrySize = dtSeries.Rows.Count > 500
                ? (isScatter ? 4 : 0)
                : geometrySize;

            var config = new ChartConfiguration
            {
                XColumn = "SnapshotDate",
                MetricColumn = "SelectedValue",
                SeriesColumn = "SeriesName",
                ChartType = Metric.ChartType,
                LineSmoothness = lineSmoothness,
                LineFill = true,
                GeometrySize = effectiveGeometrySize,
                XAxisMin = (FromDate == DateTime.MinValue ? DateRange.FromUTC : FromDate).ToAppTimeZone(),
                XAxisMax = (ToDate == DateTime.MinValue ? DateRange.ToUTC : ToDate).ToAppTimeZone(),
                YAxisLabel = string.Empty,
                YAxisFormat = "#,##0.######",
                YAxisMin = minValue,
                YAxisMax = maxValue,
                LegendPosition = Metric.LegendPosition
            };

            ChartHelper.UpdateChart(chart1, dtSeries, config);
            // Ensure legend menu state is in sync with the Metric setting after chart is updated
            UpdateLegendMenuChecked();
        }

        private DataTable GetPerformanceCounter()
        {
            if (Metric.Counters == null) return new DataTable();
            using var cn = new SqlConnection(Common.ConnectionString);
            cn.Open();
            DataTable dt = new();

            var counterIds = Metric.Counters.Where(c => c.CounterID > 0).Select(c => c.CounterID).Distinct().ToList().AsDataTable();
            if (counterIds.Rows.Count == 0)
            {
                throw new Exception("Unable to resolve Counter IDs for the selected counters.");
            }
            using var cmd = new SqlCommand("dbo.PerformanceCounter_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("FromDate", FromDate == DateTime.MinValue ? DateRange.FromUTC : FromDate);
            cmd.Parameters.AddWithValue("ToDate", ToDate == DateTime.MinValue ? DateRange.ToUTC : ToDate);
            cmd.Parameters.AddWithValue("CounterIDs", counterIds);
            cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
            cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void PerformanceCounters_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
            tsTitle.EnableAutoTruncate(t => string.IsNullOrEmpty(t)
                ? "Click to edit the chart label"
                : t + "\n(Click to edit)");
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            try
            {
                dt = GetPerformanceCounter();
                RefreshChart();
            }
            catch (Exception ex)
            {
                ToggleError(true, ex.Message);
            }
        }

        /// <summary>
        /// Set the aggregation flags on a counter to the single selected aggregate type,
        /// clearing any previously enabled aggregates.
        /// </summary>
        private static void SetCounterAggregate(Counter c, AggregateTypes aggType)
        {
            // Clear existing flags
            c.Avg = false;
            c.Max = false;
            c.Min = false;
            c.Total = false;
            c.SampleCount = false;
            c.Current = false;

            switch (aggType)
            {
                case AggregateTypes.Avg:
                    c.Avg = true;
                    break;

                case AggregateTypes.Max:
                    c.Max = true;
                    break;

                case AggregateTypes.Min:
                    c.Min = true;
                    break;

                case AggregateTypes.Total:
                case AggregateTypes.Sum:
                    c.Total = true;
                    break;

                case AggregateTypes.SampleCount:
                    c.SampleCount = true;
                    break;

                case AggregateTypes.None:
                default:
                    // leave all flags false
                    break;
            }
        }

        /// <summary>
        /// Ensures every counter has at least one aggregate selected, defaulting to the chart aggregate
        /// (Avg) - e.g. for the first counter, which is created from the grid without aggregate flags.
        /// </summary>
        private void EnsureCountersHaveAggregate()
        {
            if (Metric?.Counters == null) return;
            foreach (var c in Metric.Counters.Where(c => c.GetAggColumns(includeCurrent: false).Count == 0))
            {
                SetCounterAggregate(c, Metric.AggregateType);
            }
        }

        /// <summary>
        /// Add a counter to this chart and refresh.  Used by the Metrics tab "Add" link
        /// to combine multiple counters on a single chart.
        /// </summary>
        public void AddCounter(Counter counter)
        {
            if (AddCounterInternal(counter))
            {
                RefreshData();
            }
        }

        /// <summary>
        /// Add multiple counters to this chart, refreshing only once.  Used by the "Add Metric" button.
        /// </summary>
        public void AddCounters(IEnumerable<Counter> counters)
        {
            if (counters == null) return;
            var added = false;
            foreach (var counter in counters)
            {
                added |= AddCounterInternal(counter);
            }
            if (added)
            {
                RefreshData();
            }
        }

        /// <summary>
        /// Add a counter to the metric without refreshing.  Returns true if the counter was added.
        /// </summary>
        private bool AddCounterInternal(Counter counter)
        {
            if (counter == null) return false;
            Metric.Counters ??= new List<Counter>();

            // If the counter is already on the chart, don't add a duplicate.
            if (counter.CounterID > 0 && Metric.Counters.Any(c => c.CounterID == counter.CounterID))
            {
                return false;
            }

            // Default the new counter's aggregate to match the chart's current selection
            if (counter.GetAggColumns(includeCurrent: false).Count == 0)
            {
                SetCounterAggregate(counter, Metric.AggregateType);
            }

            Metric.Counters.Add(counter);
            return true;
        }

        private void TsAddMetric_Click(object sender, EventArgs e)
        {
            // ShowCurrent = false: this is a time-series chart, so only the over-time aggregates
            // (Avg/Max/Min/Total/Sample Count) are offered, selected per counter in the dialog.
            using var picker = new SelectPerformanceCounters() { ShowCurrent = false };
            picker.ApplyTheme();
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedCounters == null || picker.SelectedCounters.Count == 0) return;
            AddCounters(picker.SelectedCounters.Values);
        }

        // Aggregates offered per counter on time-series charts (Current is a point-in-time value, not plotted).
        private static readonly (string Text, AggregateTypes Value)[] ChartAggregates =
        {
            ("Avg", AggregateTypes.Avg),
            ("Max", AggregateTypes.Max),
            ("Min", AggregateTypes.Min),
            ("Total", AggregateTypes.Total),
            ("Sample Count", AggregateTypes.SampleCount),
        };

        private void TsCounters_DropDownOpening(object sender, EventArgs e)
        {
            tsCounters.DropDownItems.Clear();

            if (Metric?.Counters is { Count: > 0 })
            {
                foreach (var c in Metric.Counters)
                {
                    // Each counter has a submenu of aggregate check items. Clicking an aggregate toggles it on
                    // the chart, so several aggregates can be shown for a counter at once (the "All" item and
                    // the Add Counter dialog set them in bulk).
                    var counterItem = new ToolStripMenuItem(CounterMenuText(c)) { Tag = c };

                    var allItem = new ToolStripMenuItem("All")
                    {
                        Checked = ChartAggregates.All(a => IsAggregateSet(c, a.Value)),
                        Tag = c,
                        ToolTipText = "Show every aggregate (click again to clear them all)."
                    };
                    allItem.Click += CounterAllAggregates_Click;
                    counterItem.DropDownItems.Add(allItem);
                    counterItem.DropDownItems.Add(new ToolStripSeparator());

                    foreach (var (text, agg) in ChartAggregates)
                    {
                        var aggItem = new ToolStripMenuItem(text)
                        {
                            Checked = IsAggregateSet(c, agg),
                            Tag = (c, agg),
                            ToolTipText = "Click to toggle this aggregate on the chart."
                        };
                        aggItem.Click += CounterAggregate_Click;
                        counterItem.DropDownItems.Add(aggItem);
                    }
                    counterItem.DropDownItems.Add(new ToolStripSeparator());
                    var removeItem = new ToolStripMenuItem("Remove", Properties.Resources.Close_red_16x) { Tag = c };
                    removeItem.Click += RemoveCounter_Click;
                    counterItem.DropDownItems.Add(removeItem);

                    tsCounters.DropDownItems.Add(counterItem);
                }
                tsCounters.DropDownItems.Add(new ToolStripSeparator());

                if (Metric.Counters.Count > 1)
                {
                    // Quick way to switch every counter to a single aggregate (Max, Avg, ...).
                    var setAll = new ToolStripMenuItem("Set All To") { ToolTipText = "Set every counter to a single aggregate" };
                    foreach (var (text, agg) in ChartAggregates)
                    {
                        var setAllItem = new ToolStripMenuItem(text) { Tag = agg };
                        setAllItem.Click += SetAllAggregate_Click;
                        setAll.DropDownItems.Add(setAllItem);
                    }
                    tsCounters.DropDownItems.Add(setAll);
                    tsCounters.DropDownItems.Add(new ToolStripSeparator());
                }
            }

            var addItem = new ToolStripMenuItem("Add Counter...", Properties.Resources.LineChart_16x);
            addItem.Click += TsAddMetric_Click;
            tsCounters.DropDownItems.Add(addItem);
        }

        private static string CounterMenuText(Counter c)
        {
            var aggs = c.GetAggColumns(includeCurrent: false);
            return aggs.Count > 0 ? $"{c.FullName}  ({string.Join(", ", aggs)})" : c.FullName;
        }

        private void CounterAggregate_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not (Counter c, AggregateTypes agg)) return;
            // Toggle: clicking a checked aggregate removes it from the chart (it stays in the menu so it
            // can be re-enabled); a counter may end up with none selected, showing nothing.
            SetCounterAggregateFlag(c, agg, !IsAggregateSet(c, agg));
            RefreshChart();
        }

        private void CounterAllAggregates_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not Counter c) return;
            // Toggle all aggregates on; if they are already all on, clear them.
            var enable = !ChartAggregates.All(a => IsAggregateSet(c, a.Value));
            foreach (var (_, agg) in ChartAggregates)
            {
                SetCounterAggregateFlag(c, agg, enable);
            }
            RefreshChart();
        }

        private void SetAllAggregate_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not AggregateTypes agg || Metric?.Counters == null) return;
            // Switch every counter to just this aggregate.
            foreach (var c in Metric.Counters)
            {
                SetCounterAggregate(c, agg);
            }
            RefreshChart();
        }

        private void RemoveCounter_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not Counter c || Metric?.Counters == null) return;
            if (Metric.Counters.Count <= 1)
            {
                MessageBox.Show("At least one counter must remain. Use Close to remove the whole chart.", "Remove Counter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Metric.Counters.Remove(c);
            RefreshData();
        }

        private static bool IsAggregateSet(Counter c, AggregateTypes agg) => agg switch
        {
            AggregateTypes.Avg => c.Avg,
            AggregateTypes.Max => c.Max,
            AggregateTypes.Min => c.Min,
            AggregateTypes.Total or AggregateTypes.Sum => c.Total,
            AggregateTypes.SampleCount => c.SampleCount,
            _ => false
        };

        private static void SetCounterAggregateFlag(Counter c, AggregateTypes agg, bool value)
        {
            switch (agg)
            {
                case AggregateTypes.Avg: c.Avg = value; break;
                case AggregateTypes.Max: c.Max = value; break;
                case AggregateTypes.Min: c.Min = value; break;
                case AggregateTypes.Total:
                case AggregateTypes.Sum: c.Total = value; break;
                case AggregateTypes.SampleCount: c.SampleCount = value; break;
            }
        }

        private void TsChartType_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string tag) return;
            if (!Enum.TryParse<ChartTypes>(tag, out var chartType)) return;
            Metric.ChartType = chartType;
            UpdateChartTypeMenuChecked();
            RefreshChart();
        }

        private void UpdateChartTypeMenuChecked()
        {
            try
            {
                foreach (ToolStripMenuItem menuItem in tsChartType.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    var tag = menuItem.Tag?.ToString();
                    menuItem.Checked = tag != null && Metric != null && tag == Metric.ChartType.ToString();
                    if (menuItem.Checked)
                    {
                        tsChartType.Text = "Chart Type: " + menuItem.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void TsYAxisAuto_Click(object sender, EventArgs e)
        {
            Metric.YAxisMin = null;
            Metric.YAxisMax = null;
            UpdateYAxisMenuChecked();
            RefreshChart();
        }

        private void TsYAxisPercent_Click(object sender, EventArgs e)
        {
            Metric.YAxisMin = 0;
            Metric.YAxisMax = 100;
            UpdateYAxisMenuChecked();
            RefreshChart();
        }

        private void TsYAxisCustom_Click(object sender, EventArgs e)
        {
            using var frm = new YAxisRangeDialog(Metric.YAxisMin, Metric.YAxisMax);
            if (frm.ShowDialog(this) != DialogResult.OK) return;
            Metric.YAxisMin = frm.AxisMin;
            Metric.YAxisMax = frm.AxisMax;
            UpdateYAxisMenuChecked();
            RefreshChart();
        }

        private void UpdateYAxisMenuChecked()
        {
            try
            {
                var min = Metric?.YAxisMin;
                var max = Metric?.YAxisMax;
                bool isAuto = !min.HasValue && !max.HasValue;
                bool isPercent = min == 0 && max == 100;

                autoYAxisMenuItem.Checked = isAuto;
                percentYAxisMenuItem.Checked = isPercent;
                customYAxisMenuItem.Checked = !isAuto && !isPercent;

                tsYAxis.Text = isAuto
                    ? "Y-Axis: Auto"
                    : isPercent
                        ? "Y-Axis: 0-100 %"
                        : $"Y-Axis: {(min.HasValue ? min.Value.ToString("#,##0.###") : "Auto")} - {(max.HasValue ? max.Value.ToString("#,##0.###") : "Auto")}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void TsTitle_Click(object sender, EventArgs e)
        {
            var label = Metric.GetTitle() ?? string.Empty;
            if (CommonShared.ShowInputDialog(ref label, "Chart Label",
                    description: "Enter a label for the chart. Leave blank to use the counter name(s).") != DialogResult.OK)
            {
                return;
            }
            Metric.Title = label?.Trim() ?? string.Empty;
            tsTitle.SetTruncatedText(Metric.GetTitle());
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void SetLegendPosition(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            LegendPosition legendPosition;
            if (!Enum.TryParse(item.Tag.ToString(), out legendPosition)) return;
            foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
            {
                menuItem.Checked = menuItem == item;
            }
            chart1.LegendPosition = legendPosition;
            _metric.LegendPosition = legendPosition;
        }

        public void ApplyTheme(BaseTheme theme)
        {
            chart1.ApplyTheme();
            toolStrip1.ApplyTheme();
            lblError.ForeColor = DBADashUser.IsDarkTheme ? DashColors.White : DashColors.Fail;
        }
    }
}