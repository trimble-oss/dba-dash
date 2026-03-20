using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.Performance.IMetric;
using System.Diagnostics;
using System.CodeDom;

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
        { get => _metric; set { _metric = value; SelectAggregate(); UpdateLegendMenuChecked(); } }

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

        private void SelectAggregate()
        {
            // Determine visible aggregate based on per-counter aggregation flags.
            // If counters have differing enabled aggregates (or a counter has multiple), hide the aggregate selector.
            if (Metric?.Counters == null || Metric.Counters.Count == 0)
            {
                // Legacy single-metric behavior
                tsAgg.Visible = true;
                tsAgg.Text = Enum.GetName(Metric.AggregateType);
                foreach (ToolStripMenuItem mnu in tsAgg.DropDownItems)
                {
                    mnu.Checked = (string)mnu.Tag == Enum.GetName(Metric.AggregateType);
                }
                return;
            }

            var distinctAggs = new System.Collections.Generic.HashSet<string>();
            foreach (var c in Metric.Counters)
            {
                var aggs = c.GetAggColumns(includeCurrent: false);
                if (aggs == null || aggs.Count == 0)
                {
                    // Default to Avg when no per-counter flags set
                    distinctAggs.Add("Avg");
                }
                else
                {
                    foreach (var a in aggs)
                    {
                        distinctAggs.Add(a);
                    }
                }
            }

            if (distinctAggs.Count != 1)
            {
                // Multiple different aggregates in use across counters - hide selector
                tsAgg.Visible = false;
                return;
            }

            // Single aggregate in use - show selector and select the matching menu item
            var aggName = distinctAggs.First();
            tsAgg.Visible = true;
            foreach (ToolStripMenuItem mnu in tsAgg.DropDownItems)
            {
                var tag = (string)mnu.Tag;
                var isMatch = tag == aggName;
                mnu.Checked = isMatch;
                if (isMatch)
                {
                    // Use the menu item's display text (handles "Sample Count" spacing)
                    tsAgg.Text = mnu.Text;
                }
            }
        }

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

        private void SetDateGroup(int mins)
        {
            if (durationMins == mins) return; // Change date group only if date range has changed.
            DateGrouping = DateHelper.DateGrouping(mins, 200);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            durationMins = mins;
        }

        private void RefreshChart()
        {
            tsTitle.Text = Metric.GetTitle();
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
                    var aggCols = c.GetAggColumns(includeCurrent: false);
                    if (aggCols.Count == 0)
                    {
                        seriesDefs.Add((c.CounterID, c.FullName, "Avg", "Value_Avg"));
                    }
                    else
                    {
                        foreach (var agg in aggCols)
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
            }
            if (seriesDefs.Count == 0)
            {
                ToggleError(true, "No valid counters or aggregates selected.");
                return;
            }

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

            // Calculate min/max for Y-axis scaling from dtSeries
            double maxValue = 0, minValue = 0;
            foreach (DataRow r in dtSeries.Rows)
            {
                var value = Convert.ToDouble(r["SelectedValue"]);
                maxValue = value > maxValue ? value : maxValue;
                minValue = value < minValue ? value : minValue;
            }

            // Adjust Y-axis limits
            if (maxValue == 0 && minValue == 0)
            {
                maxValue = 1;
            }
            maxValue *= 1.1;
            // Auto-adjust point size based on data count (use flattened series table)
            var effectiveGeometrySize = dtSeries.Rows.Count > 500 ? 0 : geometrySize;

            var config = new ChartConfiguration
            {
                XColumn = "SnapshotDate",
                MetricColumn = "SelectedValue",
                SeriesColumn = "SeriesName",
                ChartType = ChartTypes.Line,
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

        private void TsAgg_Click(object sender, EventArgs e)
        {
            var selected = sender as ToolStripMenuItem;
            if (selected == null) return;

            foreach (ToolStripMenuItem itm in tsAgg.DropDownItems)
            {
                itm.Checked = itm == selected;
            }

            var aggStr = (string)selected.Tag!;
            var aggType = Enum.Parse<AggregateTypes>(aggStr);

            // Update Metric.AggregateType for backward compatibility
            Metric.AggregateType = aggType;

            // If there are multiple counters, apply the selected aggregate to all counters
            if (Metric?.Counters != null && Metric.Counters.Count > 0)
            {
                foreach (var c in Metric.Counters)
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
            }

            tsAgg.Text = selected.Text;
            RefreshChart();
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