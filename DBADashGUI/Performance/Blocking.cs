using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl, IMetricChart
    {
        public Blocking()
        {
            InitializeComponent();
        }

        private int InstanceID;
        private long maxBlockedTime;
        private int databaseID;

        private List<DataRow> _rows;
        private Dictionary<long, int> _tickIndexMap;
        // Sorted arrays to support fast nearest-tick lookup when exact match isn't found
        private long[] _sortedTicks;
        private int[] _sortedIndices;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public void RefreshData(int InstanceID, int databaseID)
        {
            this.InstanceID = InstanceID;
            this.databaseID = databaseID;
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1);
        }

        private DataTable GetDT()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BlockingSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", databaseID);
            cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            cmd.CommandTimeout = Config.DefaultCommandTimeout;

            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private double MaxPointShapeDiameter => maxBlockedTime switch
        {
            > 3600000 => 60,
            > 600000 => 30,
            > 60000 => 10,
            _ => 8  // Increased from 5 to 8 for better tooltip hit detection
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BlockingMetric Metric { get; set; } = new();

        IMetric IMetricChart.Metric => Metric;

        public void RefreshData()
        {
            var dt = GetDT();

            var fromTicks = DateRange.FromUTC.ToAppTimeZone().Ticks;
            var toTicks = Math.Min(DateHelper.AppNow.Ticks, DateRange.ToUTC.ToAppTimeZone().Ticks);

            // Create theme-aware paint for labels
            var labelPaint = CreateLabelPaint();
            var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
            var nameFontSize = DBADashUser.ChartAxisNameFontSize;

            // Configure axes first (even if no data) to show proper date labels
            chartBlocking.XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString(DateRange.DateFormatString),
                    MinLimit = fromTicks,
                    MaxLimit = toTicks,
                    LabelsPaint = labelPaint,
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize
                }
            };

            chartBlocking.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("0"),
                    MinLimit = 0,
                    LabelsPaint = labelPaint,
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize,
                    Name = "Blocked Sessions"
                }
            };

            if (dt.Rows.Count == 0)
            {
                chartBlocking.Series = Array.Empty<ISeries>();
                lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
                toolStrip1.Tag = databaseID > 0 ? "ALT" : null;
                toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
                return;
            }

            var rows = dt.Rows.Cast<DataRow>().ToList();
            _rows = rows;

            // compute maximum blocked time up-front so sizing is consistent
            maxBlockedTime = rows.Count == 0 ? 0 : rows.Max(r => (long)r["BlockedWaitTime"]);

            // Use a single scatter series with mapping to provide a weight (blocked wait time)
            // Mapping's third value is used as the weight to scale geometry between MinGeometrySize and GeometrySize
            const double minDiameter = 6;

            // Build weighted points (X=ticks, Y=blocked count, Weight=blocked wait ms)
            var weightedPoints = rows.Select(r =>
                new WeightedPoint(((DateTime)r["SnapshotDateUTC"]).ToAppTimeZone().Ticks,
                                  (double)(int)r["BlockedSessionCount"],
                                  (double)(long)r["BlockedWaitTime"]))
                .ToArray();

            // Use ChartHelper to create the scatter series
            var scatterSeries = ChartHelper.CreateWeightedScatterSeries(weightedPoints, "Blocked Sessions", minDiameter, MaxPointShapeDiameter);
            chartBlocking.Series = new ISeries[] { scatterSeries };

            // Build a fast lookup from X ticks -> row index for tooltip and click mapping using ChartHelper
            try
            {
                var ticks = weightedPoints.Select(wp => (long)Math.Round(wp.X ?? 0.0)).ToArray();
                var mapResult = ChartHelper.BuildTickIndexMap(ticks);
                _tickIndexMap = mapResult.tickIndexMap;
                _sortedTicks = mapResult.sortedTicks;
                _sortedIndices = mapResult.sortedIndices;
            }
            catch
            {
                _tickIndexMap = null;
                _sortedTicks = null;
                _sortedIndices = null;
            }

            // Update Y axis max based on actual data
            var yMax = Math.Max(100, rows.Max(r => (int)r["BlockedSessionCount"]));
            var yAxes = chartBlocking.YAxes.ToArray();
            yAxes[0].MaxLimit = yMax;
            chartBlocking.YAxes = yAxes;

            lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
            toolStrip1.Tag = databaseID > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
            toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);

            // Enable custom tooltips with custom formatter to show blocked time
            chartBlocking.EnableCustomTooltips(point =>
            {
                try
                {
                    // If series Values is an IList and matches the rows ordering, use point.Index to map back
                    var valuesObj = point.Context?.Series?.GetType().GetProperty("Values")?.GetValue(point.Context.Series);
                    if (valuesObj is System.Collections.IList list && list.Count == rows.Count && point.Index >= 0 && point.Index < list.Count)
                    {
                        var row = rows[point.Index];
                        var blockedCnt = (int)row["BlockedSessionCount"];
                        var blockedTime = (long)row["BlockedWaitTime"];
                        var timeSpan = TimeSpan.FromMilliseconds(blockedTime);
                        var timeFormat = timeSpan.TotalDays >= 1
                            ? $"{(int)timeSpan.TotalDays}d {timeSpan:hh\\:mm\\:ss}"
                            : $"{timeSpan:hh\\:mm\\:ss}";
                        return $"{blockedCnt:N0} ({timeFormat})";
                    }

                    // If the ChartPoint.DataSource contains our adapter, use it
                    if (point.Context?.DataSource is BlockingPointAdapter bp)
                    {
                        var blockedCnt = (int)bp.BlockedSessions;
                        var blockedTime = (long)bp.BlockedWaitTime;
                        var timeSpan = TimeSpan.FromMilliseconds(blockedTime);
                        var timeFormat = timeSpan.TotalDays >= 1
                            ? $"{(int)timeSpan.TotalDays}d {timeSpan:hh\\:mm\\:ss}"
                            : $"{timeSpan:hh\\:mm\\:ss}";
                        return $"{blockedCnt:N0} ({timeFormat})";
                    }

                    // Fallback: try to match by X ticks value using precomputed lookup for performance
                    var coord = point.Coordinate; // Coordinate is a struct (not nullable)
                    if (!double.IsNaN(coord.PrimaryValue))
                    {
                        var x = (long)Math.Round(coord.PrimaryValue);
                        int idx = -1;
                        // Exact map lookup first
                        if (_tickIndexMap != null && _tickIndexMap.TryGetValue(x, out var mapped))
                        {
                            idx = mapped;
                        }
                        else if (_sortedTicks != null && _sortedTicks.Length > 0)
                        {
                            // Binary search for nearest
                            var pos = Array.BinarySearch(_sortedTicks, x);
                            if (pos >= 0)
                            {
                                idx = _sortedIndices[pos];
                            }
                            else
                            {
                                var insert = ~pos;
                                long bestDiff = long.MaxValue;
                                int bestIdx = -1;
                                if (insert < _sortedTicks.Length)
                                {
                                    var diff = Math.Abs(_sortedTicks[insert] - x);
                                    if (diff < bestDiff) { bestDiff = diff; bestIdx = _sortedIndices[insert]; }
                                }
                                if (insert - 1 >= 0)
                                {
                                    var diff = Math.Abs(_sortedTicks[insert - 1] - x);
                                    if (diff < bestDiff) { bestDiff = diff; bestIdx = _sortedIndices[insert - 1]; }
                                }
                                // Accept only if within 1 second
                                if (bestIdx >= 0 && bestDiff <= TimeSpan.TicksPerSecond)
                                {
                                    idx = bestIdx;
                                }
                            }
                        }

                        if (idx >= 0 && idx < rows.Count)
                        {
                            var row = rows[idx];
                            var blockedCnt = (int)row["BlockedSessionCount"];
                            var blockedTime = (long)row["BlockedWaitTime"];
                            var timeSpan = TimeSpan.FromMilliseconds(blockedTime);
                            var timeFormat = timeSpan.TotalDays >= 1
                                ? $"{(int)timeSpan.TotalDays}d {timeSpan:hh\\:mm\\:ss}"
                                : $"{timeSpan:hh\\:mm\\:ss}";
                            return $"{blockedCnt:N0} ({timeFormat})";
                        }
                    }
                }
                catch { }

                // Last fallback: show the primary Y value formatted
                var fallbackY = double.NaN;
                try
                {
                    fallbackY = point.Coordinate.PrimaryValue;
                }
                catch { }
                return !double.IsNaN(fallbackY) ? fallbackY.ToString("N0") : string.Empty;
            });
        }

        private SolidColorPaint CreateLabelPaint()
        {
            return DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
        }

        private void Blocking_Load(object sender, EventArgs e)
        {
            chartBlocking.MouseDown += ChartBlocking_MouseDown;
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void ChartBlocking_MouseDown(object sender, MouseEventArgs e)
        {
            if (_rows == null || _rows.Count == 0)
            {
                return;
            }

            // Ask LiveCharts which points are under the mouse
            var foundPoints = chartBlocking.GetPointsAt(
                new LiveChartsCore.Drawing.LvcPointD(e.Location.X, e.Location.Y));

            var firstPoint = foundPoints.FirstOrDefault();
            if (firstPoint is null)
            {
                return;
            }
            // Try to map the clicked ChartPoint back to the original row.
            // Preferred: match by X coordinate (ticks) to avoid relying on series index ordering.
            int index = -1;
            try
            {
                var coord = firstPoint.Coordinate;
                double x = double.NaN;
                if (!double.IsNaN(coord.PrimaryValue)) x = coord.PrimaryValue;
                else if (!double.IsNaN(coord.SecondaryValue)) x = coord.SecondaryValue;

                if (!double.IsNaN(x))
                {
                    var xTicks = (long)Math.Round(x);

                    // Try exact dictionary lookup first
                    if (_tickIndexMap != null && _tickIndexMap.TryGetValue(xTicks, out var mappedIdx))
                    {
                        index = mappedIdx;
                    }
                    else if (_sortedTicks != null && _sortedTicks.Length > 0)
                    {
                        // Binary search nearest
                        var pos = Array.BinarySearch(_sortedTicks, xTicks);
                        if (pos >= 0)
                        {
                            index = _sortedIndices[pos];
                        }
                        else
                        {
                            var insert = ~pos;
                            long bestDiff = long.MaxValue;
                            int bestIdx = -1;
                            if (insert < _sortedTicks.Length)
                            {
                                var diff = Math.Abs(_sortedTicks[insert] - xTicks);
                                if (diff < bestDiff) { bestDiff = diff; bestIdx = _sortedIndices[insert]; }
                            }
                            if (insert - 1 >= 0)
                            {
                                var diff = Math.Abs(_sortedTicks[insert - 1] - xTicks);
                                if (diff < bestDiff) { bestDiff = diff; bestIdx = _sortedIndices[insert - 1]; }
                            }
                            if (bestIdx >= 0 && bestDiff <= TimeSpan.TicksPerSecond)
                            {
                                index = bestIdx;
                            }
                        }
                    }
                }
            }
            catch
            {
                index = -1;
            }

            // Fallback to using the point.Index when X matching failed (single-series mode)
            if (index < 0)
            {
                index = firstPoint.Index;
            }

            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            var row = _rows[index];
            var snapshotDateLocal = ((DateTime)row["SnapshotDateUTC"]).ToAppTimeZone();

            var frm = new RunningQueriesViewer
            {
                SnapshotDateFrom = snapshotDateLocal.AppTimeZoneToUtc(),
                SnapshotDateTo = snapshotDateLocal.AppTimeZoneToUtc(),
                InstanceID = InstanceID,
                ShowRootBlockers = true
            };
            frm.Show(this);
        }
    }
}

internal class BlockingPointAdapter
{
    public DateTime SnapshotDate { get; set; }
    public double BlockedSessions { get; set; }
    public double BlockedWaitTime { get; set; }
    public int Index { get; set; }

    public BlockingPointAdapter(DateTime snapshotDate, double blockedSessions, double blockedWaitTime)
    {
        SnapshotDate = snapshotDate;
        BlockedSessions = blockedSessions;
        BlockedWaitTime = blockedWaitTime;
    }
}

internal class BlockingPoint
{
    public int SnapshotID { get; set; }

    public DateTime SnapshotDate { get; set; }

    public int BlockedSessions { get; set; }

    public long BlockedWaitTime { get; set; }

    public BlockingPoint(int snapshotID, DateTime snapshotDate, int blockedSessions, long blockedWaitTime)
    {
        SnapshotID = snapshotID;
        BlockedSessions = blockedSessions;
        BlockedWaitTime = blockedWaitTime;
        SnapshotDate = snapshotDate;
    }
}