using DBADashGUI.Charts;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using Humanizer;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Bar chart of running queries over time for the Running Queries summary view.
    ///
    /// <para>Each bar is a snapshot where the range is short enough, otherwise a time bucket.  A bucket's bar is the average over its snapshots,
    /// stacked as not blocked / blocked / sleeping with an open transaction, with a marker for the busiest snapshot in it.  Averages keep the same
    /// meaning at every bucket size and aren't shrunk by missing snapshots - the marker is there so a short spike isn't averaged out of sight.</para>
    ///
    /// <para>Clicking a bucket zooms into it (the chart and the snapshot list follow), so repeated clicks get down to one bar per snapshot.
    /// Clicking a snapshot's bar, or a bucket's peak marker, loads that snapshot.</para>
    /// </summary>
    public sealed class RunningQueriesSummaryChart : UserControl
    {
        private const string PeakSeriesName = "Peak";
        private const string NoLine = "None";

        /// <summary>Metrics that can be drawn as a line over the bars, against an axis on the right.  Key = column returned by the proc.</summary>
        private static readonly (string Column, string Name, string Format)[] LineMetrics =
        {
            ("SumMemoryGrantMB", "Sum Memory Grant (MB)", "N0"),
            ("MaxMemoryGrantMB", "Max Memory Grant (MB)", "N0"),
            ("TempDBWaitTimeMs", "TempDB Wait Time (ms)", "N0"),
            ("BlockedQueriesWaitMs", "Blocked Wait Time (ms)", "N0"),
            ("CriticalWaitTimeMs", "Critical Wait Time (ms)", "N0"),
            ("LongestRunningQuerySec", "Longest Running Query (sec)", "N0"),
            ("OldestTransactionSec", "Oldest Transaction (sec)", "N0"),
            ("TempDBCurrentMB", "TempDB Current (MB)", "N0")
        };

        private readonly CartesianChart chart;
        private readonly ToolStrip toolStrip;
        private readonly ToolStripLabel lblBucket;
        private readonly ToolStripLabel lblHint;
        private readonly ToolStripDropDownButton tsLine;
        private readonly ToolStripButton tsZoomOut;

        private readonly Stack<(DateTime From, DateTime To)> zoomStack = new();
        private DateTime baseFromUTC;
        private DateTime baseToUTC;
        private int instanceId;
        private DataTable data;
        private int bucketSeconds;

        /// <summary>A snapshot was clicked - a single snapshot's bar or a bucket's peak marker, or any bar with Ctrl held (its peak snapshot).</summary>
        public event EventHandler<SnapshotSelectedEventArgs> SnapshotSelected;

        /// <param name="SnapshotDateUTC">Snapshot to load</param>
        /// <param name="NewWindow">Ctrl was held - open the snapshot in a new window rather than in place</param>
        public sealed record SnapshotSelectedEventArgs(DateTime SnapshotDateUTC, bool NewWindow);

        /// <summary>The range shown changed by zooming in or out.  The snapshot list should follow <see cref="ViewFromUTC"/> / <see cref="ViewToUTC"/>.</summary>
        public event EventHandler ViewRangeChanged;

        public DateTime ViewFromUTC { get; private set; }
        public DateTime ViewToUTC { get; private set; }
        public bool IsZoomed => zoomStack.Count > 0;

        public RunningQueriesSummaryChart()
        {
            chart = new CartesianChart { Dock = DockStyle.Fill };
            lblBucket = new ToolStripLabel();
            tsLine = new ToolStripDropDownButton
            {
                Image = Properties.Resources.LineChart_16x,
                ToolTipText = "Line drawn over the bars against an axis on the right.  Maximum per bar."
            };
            tsZoomOut = new ToolStripButton("Zoom Out", Properties.Resources.ZoomOut_16x)
            {
                Enabled = false,
                ToolTipText = "Return to the range shown before the last zoom"
            };
            tsZoomOut.Click += (_, _) => ZoomOut();
            lblHint = new ToolStripLabel("Click a bar to zoom in, or a peak marker to open that snapshot.  Ctrl+click for a new window")
            {
                Alignment = ToolStripItemAlignment.Right,
                ForeColor = Color.Gray
            };
            toolStrip = new ToolStrip(lblBucket, new ToolStripSeparator(), tsLine, tsZoomOut, lblHint)
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top
            };

            tsLine.DropDownItems.Add(CreateLineItem(NoLine, NoLine));
            foreach (var metric in LineMetrics)
            {
                tsLine.DropDownItems.Add(CreateLineItem(metric.Column, metric.Name));
            }
            UpdateLineMenu();

            Controls.Add(chart);
            Controls.Add(toolStrip);
            AttachClickHandling();
        }

        private ToolStripMenuItem CreateLineItem(string column, string name)
        {
            var item = new ToolStripMenuItem(name) { Tag = column };
            item.Click += (_, _) =>
            {
                Properties.Settings.Default.RunningQueriesSummaryChartLine = column;
                Properties.Settings.Default.Save();
                UpdateLineMenu();
                Render();
            };
            return item;
        }

        private static string SelectedLine
        {
            get
            {
                var line = Properties.Settings.Default.RunningQueriesSummaryChartLine;
                return LineMetrics.Any(m => m.Column == line) ? line : NoLine;
            }
        }

        private void UpdateLineMenu()
        {
            var line = SelectedLine;
            foreach (ToolStripMenuItem item in tsLine.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (string)item.Tag == line;
            }
            tsLine.Text = "Line: " + (line == NoLine ? NoLine : LineMetrics.First(m => m.Column == line).Name);
        }

        /// <summary>
        /// Set the instance and the range selected for the tab.  The zoom is kept while these stay the same (e.g. returning to the summary from a snapshot)
        /// and cleared when either changes.
        /// </summary>
        /// <param name="instance">InstanceID</param>
        /// <param name="fromUTC">Start of the selected range</param>
        /// <param name="toUTC">End of the selected range</param>
        /// <param name="relativeSpan">The time span for a range relative to now (e.g. last 24hrs), null for a custom range.  A relative range moves on with the clock,
        /// so it's the selection that's compared to decide if the zoom still applies rather than the dates.</param>
        public void SetRange(int instance, DateTime fromUTC, DateTime toUTC, TimeSpan? relativeSpan)
        {
            var key = relativeSpan.HasValue ? "R" + relativeSpan.Value.Ticks : $"C{fromUTC.Ticks}-{toUTC.Ticks}";
            var sameSelection = instance == instanceId && key == rangeKey;
            instanceId = instance;
            rangeKey = key;
            baseFromUTC = fromUTC;
            baseToUTC = toUTC;
            if (!sameSelection || !IsZoomed)
            {
                ResetZoom();
            }
        }

        private string rangeKey;

        public void ResetZoom()
        {
            zoomStack.Clear();
            ViewFromUTC = baseFromUTC;
            ViewToUTC = baseToUTC;
            tsZoomOut.Enabled = false;
        }

        public void ZoomOut()
        {
            if (zoomStack.Count == 0) return;
            (ViewFromUTC, ViewToUTC) = zoomStack.Pop();
            tsZoomOut.Enabled = IsZoomed;
            RefreshData();
            ViewRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ZoomIn(DateTime fromUTC, DateTime toUTC)
        {
            zoomStack.Push((ViewFromUTC, ViewToUTC));
            ViewFromUTC = fromUTC < ViewFromUTC ? ViewFromUTC : fromUTC;
            ViewToUTC = toUTC > ViewToUTC ? ViewToUTC : toUTC;
            tsZoomOut.Enabled = true;
            RefreshData();
            ViewRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Load the chart for the range currently in view</summary>
        public void RefreshData()
        {
            try
            {
                data = GetChartData(out bucketSeconds);
            }
            catch (Exception ex)
            {
                data = null;
                lblBucket.Text = "Error loading chart: " + ex.Message;
                Serilog.Log.Warning(ex, "Error loading running queries summary chart");
            }
            Render();
        }

        /// <summary>
        /// Bars to aim for - enough to use the width without them becoming too thin to click.  From the width of the panel the chart is docked in:
        /// the chart is loaded straight after it's shown, which can be before its own width has been laid out.
        /// </summary>
        private int MaxBars => Math.Clamp(Math.Max(chart.Width, Parent?.ClientSize.Width ?? 0) / 6, 50, 400);

        private DataTable GetChartData(out int bucket)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningQueriesSummaryChart_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.AddWithValue("FromDate", ViewFromUTC);
            cmd.Parameters.AddWithValue("ToDate", ViewToUTC);
            cmd.Parameters.AddWithValue("MaxBars", MaxBars);
            var pBucket = cmd.Parameters.Add("BucketSeconds", SqlDbType.Int);
            pBucket.Direction = ParameterDirection.Output;
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            bucket = pBucket.Value is int b ? b : 0;

            // Drawn in the app's time zone.  The UTC columns are kept for drill down.
            dt.Columns.Add("BucketTime", typeof(DateTime));
            foreach (DataRow row in dt.Rows)
            {
                row["BucketTime"] = ((DateTime)row["BucketMidUTC"]).ToAppTimeZone();
            }
            return dt;
        }

        /// <summary>
        /// Bar width with a bar per snapshot - the median gap between snapshots, so bars fit the collection frequency (e.g. 30 seconds) without
        /// overlapping.  The median rather than the smallest gap, as one snapshot triggered from the GUI just after a scheduled one would otherwise
        /// make every bar a sliver.  Null (let the chart decide) where there's only one snapshot.
        /// </summary>
        private TimeSpan? TypicalSnapshotInterval()
        {
            var times = data.Rows.Cast<DataRow>().Select(r => (DateTime)r["BucketMidUTC"]).OrderBy(d => d).ToList();
            var gaps = times.Zip(times.Skip(1), (a, b) => b - a).Where(g => g > TimeSpan.Zero).OrderBy(g => g).ToList();
            if (gaps.Count == 0) return null;
            var median = gaps[gaps.Count / 2];
            return median < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : median;
        }

        private void Render()
        {
            if (data == null || data.Rows.Count == 0)
            {
                chart.Series = Array.Empty<ISeries>();
                chart.XAxes = Array.Empty<Axis>();
                chart.YAxes = Array.Empty<Axis>();
                if (data != null) lblBucket.Text = "No snapshots in this range";
                return;
            }

            var isBucketed = bucketSeconds > 0;
            lblBucket.Text = isBucketed
                ? $"Average running queries per {TimeSpan.FromSeconds(bucketSeconds).Humanize()} (◆ = peak snapshot)"
                : "Running queries per snapshot";

            var line = SelectedLine;
            var lineMetric = LineMetrics.FirstOrDefault(m => m.Column == line);

            // Buckets are aligned to fixed times, so the first and last can start before / end after the range in view (always the case for the last
            // bucket of a range up to now).  The axis is widened to take in the whole of those buckets - otherwise a bar and its peak marker,
            // drawn at the middle of the bucket, can fall outside the axis and the most recent spike isn't shown.
            var axisFromUTC = ViewFromUTC;
            var axisToUTC = ViewToUTC;
            if (isBucketed)
            {
                var firstBucketStart = (DateTime)data.Rows[0]["BucketStartUTC"];
                var lastBucketEnd = (DateTime)data.Rows[^1]["BucketEndUTC"];
                if (firstBucketStart < axisFromUTC) axisFromUTC = firstBucketStart;
                if (lastBucketEnd > axisToUTC) axisToUTC = lastBucketEnd;
            }

            var config = new ChartConfiguration
            {
                XColumn = "BucketTime",
                MetricColumns = new[] { "AvgNotBlocked", "AvgBlocked", "AvgSleeping" },
                SeriesNames = new Dictionary<string, string>
                {
                    { "AvgNotBlocked", isBucketed ? "Not Blocked (avg)" : "Not Blocked" },
                    { "AvgBlocked", isBucketed ? "Blocked (avg)" : "Blocked" },
                    { "AvgSleeping", isBucketed ? "Sleeping, Open Tran (avg)" : "Sleeping, Open Tran" }
                },
                SeriesStyles = new Dictionary<string, ChartSeriesStyle>
                {
                    { "AvgNotBlocked", new ChartSeriesStyle { Color = "#4A90D9" } },
                    { "AvgBlocked", new ChartSeriesStyle { Color = ColorTranslator.ToHtml(DashColors.Fail) } },
                    { "AvgSleeping", new ChartSeriesStyle { Color = "#9E9E9E" } }
                },
                ChartType = ChartTypes.StackedColumn,
                LegendPosition = LegendPosition.Right,
                XAxisMin = axisFromUTC.ToAppTimeZone(),
                XAxisMax = axisToUTC.ToAppTimeZone(),
                FixXAxisToRange = true,
                YAxisLabel = "Queries",
                YAxisFormat = "0.#",
                YAxisMin = 0,
                DateUnit = isBucketed ? TimeSpan.FromSeconds(bucketSeconds) : TypicalSnapshotInterval()
            };

            var overlays = new List<ChartOverlayData>();
            if (isBucketed)
            {
                overlays.Add(new ChartOverlayData(new ChartOverlay
                {
                    MetricColumns = new[] { "PeakRunning" },
                    ChartType = ChartTypes.Scatter,
                    SeriesNames = new Dictionary<string, string> { { "PeakRunning", PeakSeriesName } },
                    SeriesStyles = new Dictionary<string, ChartSeriesStyle> { { "PeakRunning", new ChartSeriesStyle { Color = "#FFB300", Marker = ChartMarkers.Diamond } } },
                    GeometrySize = 10
                }, data));
            }
            if (lineMetric.Column != null)
            {
                overlays.Add(new ChartOverlayData(new ChartOverlay
                {
                    MetricColumns = new[] { lineMetric.Column },
                    ChartType = ChartTypes.Line,
                    SeriesNames = new Dictionary<string, string> { { lineMetric.Column, lineMetric.Name } },
                    SeriesStyles = new Dictionary<string, ChartSeriesStyle> { { lineMetric.Column, new ChartSeriesStyle { Color = "#9C27B0", StrokeThickness = 2 } } },
                    YAxis = new YAxisConfiguration { Name = "Line", Label = lineMetric.Name, Format = lineMetric.Format, MinLimit = 0, Position = AxisPosition.End },
                    LineSmoothness = 0,
                    GeometrySize = 0
                }, data));
            }

            ChartHelper.UpdateChart(chart, data, config, overlays);
        }

        #region Click handling

        private void AttachClickHandling() =>
            ChartHelper.AttachPointClick(chart,
                location => FindClickTarget(location).Row != null,
                location =>
                {
                    try
                    {
                        OnChartClick(location);
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, "Error loading running queries");
                    }
                });

        /// <summary>The row of the bar or peak marker under the pointer, and whether it was the peak marker</summary>
        private (DataRow Row, bool IsPeak) FindClickTarget(Point location)
        {
            if (data == null) return (null, false);
            var point = ChartHelper.GetPointAt(chart, location);
            if (point == null) return (null, false);
            var seriesName = point.Context?.Series?.Name;
            // Only the bars and peak marker can be clicked - not the line
            var isPeak = seriesName == PeakSeriesName;
            if (!isPeak && ChartHelper.IsOverlaySeries(point.Context?.Series)) return (null, false);

            var x = point.Coordinate.SecondaryValue;
            if (double.IsNaN(x)) return (null, false);
            // The bars and the peak marker are both drawn from this table at BucketTime.  ChartHelper.FindSourceRow would do for a bar but
            // won't trace an overlay point (the peak marker), so the nearest row is looked up directly.  Further than a second from any bar
            // means the point didn't come from this table.
            var row = ChartHelper.FindNearestRow(data, r => ((DateTime)r["BucketTime"]).Ticks, x, TimeSpan.TicksPerSecond);
            return row == null ? (null, false) : (row, isPeak);
        }

        private void OnChartClick(Point location)
        {
            var (row, isPeak) = FindClickTarget(location);
            if (row == null) return;
            // Ctrl+click opens the snapshot in a new window, as on the grid.  For a bucket that's its peak snapshot - a new window is for a snapshot, not a zoom.
            var newWindow = (ModifierKeys & Keys.Control) == Keys.Control;
            if (newWindow || isPeak || bucketSeconds == 0 || Convert.ToInt32(row["SnapshotCount"]) == 1)
            {
                SnapshotSelected?.Invoke(this, new SnapshotSelectedEventArgs((DateTime)row["PeakSnapshotDateUTC"], newWindow));
            }
            else
            {
                ZoomIn((DateTime)row["BucketStartUTC"], (DateTime)row["BucketEndUTC"]);
            }
        }

        #endregion

        private BaseTheme appliedTheme;

        /// <summary>Theme the chart and its toolbar.  Only does the work where the theme has changed, as it's called each time the summary is loaded.</summary>
        public void ApplyTheme(BaseTheme theme)
        {
            if (ReferenceEquals(theme, appliedTheme)) return;
            appliedTheme = theme;
            toolStrip.ApplyTheme(theme);
            // After the toolbar's theme, which sets every item to the theme's text colour
            lblHint.ForeColor = Color.Gray;
            chart.ApplyTheme();
        }
    }
}
