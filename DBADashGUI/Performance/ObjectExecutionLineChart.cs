using DBADashGUI.Charts;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecutionLineChart : UserControl
    {
        private readonly LoadingOverlay loading;

        public ObjectExecutionLineChart()
        {
            InitializeComponent();
            // No animation: the animated redraw makes the dashed comparison lines appear to move
            chart1.AnimationsSpeed = TimeSpan.Zero;
            loading = new LoadingOverlay(this);
            // Stop a load still running on the server when the control goes away
            Disposed += (_, _) => refreshCts?.Cancel();
        }

        public int InstanceID;
        public string Instance;
        public long ObjectID;
        private int dateGrouping;
        private int durationMins;
        public DateTime FromDate;
        public DateTime ToDate;
        private double lineSmoothness = ChartConfiguration.DefaultLineSmoothness;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => tsTitle.Text; set => tsTitle.Text = value;
        }

        private readonly Dictionary<string, ColumnMetaData> columns = new()
        {
                {"AvgCPU", new ColumnMetaData{Name="Avg CPU (sec)",IsVisible=true, AxisName="Primary" } },
                {"TotalCPU", new ColumnMetaData{Name="Total CPU (sec)",IsVisible=false, AxisName="Primary" } },
                {"cpu_ms_per_sec", new ColumnMetaData{Name="CPU (ms/sec)",IsVisible=false, AxisName="Primary" } },
                {"ExecutionsPerMin", new ColumnMetaData{Name="Executions/min",IsVisible=true, AxisName="Executions" } },
                {"MaxExecutionsPerMin", new ColumnMetaData{Name="Max Executions/min",IsVisible=false, AxisName="Executions" } },
                {"ExecutionCount", new ColumnMetaData{Name="Execution Count",IsVisible=false, AxisName="Executions" } },
                {"AvgDuration", new ColumnMetaData{Name="Avg Duration (sec)",IsVisible=true, AxisName="Primary" } },
                {"TotalDuration", new ColumnMetaData{Name="Total Duration (sec)",IsVisible=false, AxisName="Primary" } },
                {"duration_ms_per_sec", new ColumnMetaData{Name="Duration (ms/sec)",IsVisible=false, AxisName="Primary" } },
                {"AvgLogicalReads", new ColumnMetaData{Name="Avg Logical Reads",IsVisible=false, AxisName="Pages" } },
                {"TotalLogicalReads", new ColumnMetaData{Name="Total Logical Reads",IsVisible=false, AxisName="Pages" } },
                {"AvgPhysicalReads", new ColumnMetaData{Name="Avg Physical Reads",IsVisible=false, AxisName="Pages" } },
                {"TotalPhysicalReads", new ColumnMetaData{Name="Total Physical Reads",IsVisible=false, AxisName="Pages" } },
                {"AvgWrites", new ColumnMetaData{Name="Avg Writes",IsVisible=false, AxisName="Pages" } },
                {"TotalWrites", new ColumnMetaData{Name="Total Writes",IsVisible=false, AxisName="Pages" } },
            };

        /// <summary>The comparison period, drawn over the chart as dotted lines in each metric's colour.  Not set (MinValue) for no comparison.</summary>
        public DateTime CompareFrom = DateTime.MinValue;
        public DateTime CompareTo = DateTime.MinValue;

        private bool HasCompare => CompareFrom > DateTime.MinValue && CompareTo > CompareFrom;

        /// <summary>
        /// Compare to the median of the same time of day in each of the previous this many weeks, drawn as the baseline.  0 for none.
        /// Used instead of <see cref="CompareFrom"/> and <see cref="CompareTo"/> when set.
        /// </summary>
        public int BaselineWeeks;

        private bool HasBaseline => BaselineWeeks > 0;

        /// <summary>The name of a comparison series is the metric's name with this on the end</summary>
        private const string CompareSuffix = " (compare)";
        private const string BaselineSuffix = " (baseline)";


        /// <summary>Overlay series name -> the metric's series on the chart, and the Y axis it's drawn on</summary>
        private readonly Dictionary<string, (int Axis, Color Colour)> compareSeries = new();

        /// <summary>The colours of the metrics, in the order they're shown.  Fixed, so a comparison line is drawn in its metric's colour straight away.</summary>
        private static readonly Color[] SeriesPalette =
        {
            DashColors.TrimbleBlue, DashColors.Red, DashColors.Green, DashColors.YellowDark,
            DashColors.BlueLight, DashColors.RedLight, DashColors.GreenLight, DashColors.Gray7
        };

        private CancellationTokenSource refreshCts;

        public async void RefreshData()
        {
            var newDurationMins = Convert.ToInt32(ToDate.Subtract(FromDate).TotalMinutes);
            if (newDurationMins != durationMins) // Update date grouping only if duration has changed
            {
                dateGrouping = DateHelper.DateGrouping(newDurationMins, 200);
                tsGroup.Text = DateHelper.DateGroupString(dateGrouping);
                durationMins = newDurationMins;
            }

            // A newer refresh supersedes one still loading
            refreshCts?.Cancel();
            var cts = new CancellationTokenSource();
            refreshCts = cts;
            DataTable dt;
            List<(DataTable Table, TimeSpan Shift)> comparePeriods = new();
            try
            {
                loading.Show();
                // The comparison periods: the baseline weeks, or the one period compared to.  Each is shifted to line up with this period
                var windows = new List<(DateTime From, DateTime To)>();
                if (HasBaseline)
                {
                    for (var week = 1; week <= BaselineWeeks; week++)
                    {
                        windows.Add((FromDate.AddDays(-7 * week), ToDate.AddDays(-7 * week)));
                    }
                }
                else if (HasCompare)
                {
                    windows.Add((CompareFrom, CompareTo));
                }
                var fetches = windows.Select(w => CommonData.ObjectExecutionStatsAsync(InstanceID, -1, ObjectID, dateGrouping, "AvgDuration", w.From, w.To, Instance, token: cts.Token)).ToList();
                var fetchTask = Task.WhenAll(fetches);

                dt = await CommonData.ObjectExecutionStatsAsync(InstanceID, -1, ObjectID, dateGrouping, "AvgDuration", FromDate, ToDate, Instance, token: cts.Token);
                var windowTables = await fetchTask;
                // The shift is the elapsed time between the periods, so every period's buckets fall on the same grid as this period's
                comparePeriods = windows.Zip(windowTables, (w, table) => (table, FromDate - w.From)).ToList();
                if (cts.IsCancellationRequested || IsDisposed) return;
            }
            catch (Exception) when (cts.IsCancellationRequested)
            {
                return; // Superseded by a newer refresh
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading object execution stats");
                return;
            }
            finally
            {
                if (refreshCts == cts)
                {
                    refreshCts = null;
                    loading.Hide();
                }
                cts.Dispose();
            }

            compareSeries.Clear();
            if (dt == null || dt.Rows.Count == 0)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            // Get visible columns
            var visibleColumns = columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

            if (visibleColumns.Length == 0)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            // Create series names dictionary
            var seriesNames = columns.ToDictionary(c => c.Key, c => c.Value.Name);

            // Create column-to-axis-name mapping
            var columnAxisNames = columns
                .Where(c => c.Value.IsVisible)
                .ToDictionary(c => c.Key, c => c.Value.AxisName);

            // Setup Y-axes configurations
            var yAxes = new List<YAxisConfiguration>();
            var axisNameToIndexMap = new Dictionary<string, int>();

            // Determine which axes to include based on visible columns
            var hasPrimaryMetrics = columnAxisNames.Values.Any(a => a == "Primary");
            var hasExecutionMetrics = columnAxisNames.Values.Any(a => a == "Executions");
            var hasPagesMetrics = columnAxisNames.Values.Any(a => a == "Pages");

            // Axis 0: Primary (CPU/Duration) - always include if has primary metrics
            if (hasPrimaryMetrics)
            {
                axisNameToIndexMap["Primary"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Primary",
                    Label = string.Empty,
                    Format = "0.000",
                    MinLimit = 0,
                    Position = AxisPosition.Start
                });
            }

            // Axis 1: Executions - include if has execution metrics
            if (hasExecutionMetrics)
            {
                axisNameToIndexMap["Executions"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Executions",
                    Label = "Executions",
                    Format = "0.0",
                    MinLimit = 0,
                    Position = AxisPosition.End
                });
            }

            // Axis 2: Pages - only include if has reads/writes metrics
            if (hasPagesMetrics)
            {
                axisNameToIndexMap["Pages"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Pages",
                    Label = "Pages",
                    Format = "0.0",
                    MinLimit = 0,
                    Position = AxisPosition.End
                });
            }

            // Each metric has its colour from the palette, so a comparison line can be drawn in its metric's colour as it's drawn - not after the theme has painted the chart
            var colours = visibleColumns.Select((key, i) => (key, colour: SeriesPalette[i % SeriesPalette.Length])).ToDictionary(c => c.key, c => c.colour);

            // Update chart using ChartHelper with name-based axis mapping
            var config = new ChartConfiguration
            {
                XColumn = "SnapshotDate",
                MetricColumns = visibleColumns,
                ChartType = ChartTypes.Line,
                LegendPosition = LegendPosition.Bottom,
                LineSmoothness = lineSmoothness,
                GeometrySize = geometrySize,
                XAxisMin = FromDate.ToAppTimeZone(),
                XAxisMax = ToDate.ToAppTimeZone(),
                SeriesNames = seriesNames,
                SeriesStyles = colours.ToDictionary(c => c.Key, c => ChartSeriesStyle.FromColor(c.Value)),
                YAxes = yAxes.ToArray(),
                ColumnAxisNames = columnAxisNames,
                DateUnit = TimeSpan.FromMinutes(dateGrouping)
            };

            var compareAsAverage = tsAverageCompare.Checked;
            var overlays = comparePeriods.Count > 0
                ? BuildCompareOverlays(comparePeriods, HasBaseline ? BaselineSuffix : CompareSuffix, visibleColumns, seriesNames, columnAxisNames, axisNameToIndexMap, compareAsAverage, colours)
                : null;

            ChartHelper.UpdateChart(chart1, dt, config, overlays);
            if (overlays != null)
            {
                StyleCompareSeries(chart1);
            }
        }

        /// <summary>
        /// The comparison periods as overlays on this chart's own axes.  Each row is moved by the difference between the
        /// period's start and this period's start, so it lines up with the period being viewed - the same time of day, four weeks
        /// ago, say - rather than being drawn at its own dates.  Where there's more than one period (the baseline weeks), each
        /// point is the median of the periods' values at that time.
        /// </summary>
        private List<ChartOverlayData> BuildCompareOverlays(List<(DataTable Table, TimeSpan Shift)> periods, string suffix, string[] visibleColumns,
            Dictionary<string, string> seriesNames, Dictionary<string, string> columnAxisNames, Dictionary<string, int> axisNameToIndexMap, bool average,
            Dictionary<string, Color> colours)
        {
            var overlayNames = new Dictionary<string, string>();
            var metricColumns = new List<string>();
            foreach (var key in visibleColumns)
            {
                if (!periods.All(p => p.Table.Columns.Contains(key))) continue;
                var overlayName = seriesNames[key] + suffix;
                overlayNames[key] = overlayName;
                metricColumns.Add(key);
                compareSeries[overlayName] = (axisNameToIndexMap[columnAxisNames[key]], colours[key]);
            }
            if (metricColumns.Count == 0) return null;

            // The values at each shifted time, for each metric
            var valuesByTime = new SortedDictionary<DateTime, Dictionary<string, List<double>>>();
            foreach (var (periodTable, shift) in periods)
            {
                foreach (DataRow row in periodTable.Rows)
                {
                    var time = ((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc().Add(shift).ToAppTimeZone();
                    if (!valuesByTime.TryGetValue(time, out var byMetric))
                    {
                        byMetric = metricColumns.ToDictionary(k => k, _ => new List<double>());
                        valuesByTime[time] = byMetric;
                    }
                    foreach (var key in metricColumns)
                    {
                        if (row[key] != DBNull.Value) byMetric[key].Add(Convert.ToDouble(row[key]));
                    }
                }
            }

            var table = new DataTable();
            table.Columns.Add("SnapshotDate", typeof(DateTime));
            foreach (var key in metricColumns)
            {
                table.Columns.Add(key, typeof(double));
            }
            if (average)
            {
                // The average of the points compared to, as a straight line across the whole period
                var averages = metricColumns.ToDictionary(k => k, k =>
                {
                    var points = valuesByTime.Values.Where(m => m[k].Count > 0).Select(m => Median(m[k])).ToList();
                    return points.Count == 0 ? (double?)null : points.Average();
                });
                // A point at each bucket, not just the ends, so the line has a point under the mouse to show its tooltip
                var start = FromDate.ToAppTimeZone();
                var end = ToDate.ToAppTimeZone();
                var step = TimeSpan.FromMinutes(Math.Max(1, dateGrouping));
                var times = new List<DateTime>();
                for (var time = start; time < end; time += step) times.Add(time);
                times.Add(end);
                foreach (var time in times)
                {
                    var avgRow = table.NewRow();
                    avgRow["SnapshotDate"] = time;
                    foreach (var key in metricColumns)
                    {
                        if (averages[key].HasValue) avgRow[key] = averages[key].Value;
                    }
                    table.Rows.Add(avgRow);
                }
            }
            else
            {
                foreach (var (time, byMetric) in valuesByTime)
                {
                    var newRow = table.NewRow();
                    newRow["SnapshotDate"] = time;
                    foreach (var key in metricColumns)
                    {
                        newRow[key] = byMetric[key].Count == 0 ? DBNull.Value : Median(byMetric[key]);
                    }
                    table.Rows.Add(newRow);
                }
            }

            var overlay = new ChartOverlay
            {
                XColumn = "SnapshotDate",
                MetricColumns = metricColumns.ToArray(),
                ChartType = ChartTypes.Line,
                LineSmoothness = lineSmoothness,
                GeometrySize = 0,
                SeriesNames = overlayNames,
            };
            return new List<ChartOverlayData> { new(overlay, table) };
        }

        private static double Median(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        /// <summary>
        /// Each comparison line goes on the same Y axis as the metric it compares to, and is drawn dotted in that metric's colour.
        /// Set straight after the chart is drawn, as the colours are known up front.
        /// </summary>
        private void StyleCompareSeries(CartesianChartWithDataTable chart)
        {
            foreach (var series in chart.Series)
            {
                if (series?.Name == null || !compareSeries.TryGetValue(series.Name, out var match)) continue;
                ChartHelper.SetScalesYAt(series, match.Axis);
                if (series is LiveChartsCore.Kernel.Sketches.ILineSeries line)
                {
                    var colour = new SKColor(match.Colour.R, match.Colour.G, match.Colour.B, match.Colour.A);
                    line.Stroke = new SolidColorPaint(colour)
                    {
                        StrokeThickness = 2,
                        PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect(new float[] { 6, 4 })
                    };
                }
            }
        }

        /// <summary>The Options menu's average line changes how the comparison is drawn, so the chart is drawn again</summary>
        private void TsAverageCompare_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsLineSmoothness_Click(object sender, EventArgs e)
        {
            lineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
            RefreshData();
        }

        private void ObjectExecutionLineChart_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsGroup, TsGroup_Click);
            LoadMeasures();
        }

        private void TsGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = (int)ts.Tag;
            tsGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void LoadMeasures()
        {
            foreach (var k in columns.Keys)
            {
                var m = columns[k];
                tsMeasures.DropDownItems.Add(new ToolStripMenuItem(m.Name, null, TsMeasure_Click) { Checked = m.IsVisible, Tag = k, CheckOnClick = true });
            }
        }

        private void TsMeasure_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            columns[((string)ts.Tag)!].IsVisible = ts.Checked;
            RefreshData();  // Refresh chart to apply visibility changes
        }

        private void TsPointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            geometrySize = Convert.ToInt32(ts.Tag);
            RefreshData();
        }
    }
}