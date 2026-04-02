using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl, IMetricChart, IThemedControl
    {
        private Label lblError = new Label() { Dock = DockStyle.Fill, Visible = false, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        private void ToggleError(bool show, string message = "")
        {
            lblError.Text = message;
            lblError.Visible = show;
            chartBlocking.Visible = !show;
        }

        public Blocking()
        {
            InitializeComponent();
            Controls.Add(lblError);
            lblError.BringToFront();
        }

        private int InstanceID;
        private long maxBlockedTime;
        private int databaseID;

        private List<DataRow> _rows;
        private Dictionary<long, int> _tickIndexMap;

        // Sorted arrays to support fast nearest-tick lookup when exact match isn't found
        private long[] _sortedTicks;

        private int[] _sortedIndices;

        // Strongly-typed reference to the scatter series to avoid reflection in tooltip rendering
        private ScatterSeries<WeightedPoint, CircleGeometry> _scatterSeries;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public void SetContext(DBADashContext _context)
        {
            if (_context == null) return;
            this.InstanceID = _context.InstanceID;
            this.databaseID = _context.DatabaseID;
            RefreshData();
        }

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
            try
            {
                if (InstanceID == 0)
                {
                    ToggleError(true, "No instance selected");
                    return;
                }
                ToggleError(false);

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
                    // Clear series and any retained state from previous refreshes to avoid
                    // holding onto DataRow references via tooltip closures and to keep
                    // click/tooltip mapping consistent with the empty chart.
                    chartBlocking.Series = Array.Empty<ISeries>();
                    _scatterSeries = null;
                    _rows = null;
                    _tickIndexMap = null;
                    _sortedTicks = null;
                    _sortedIndices = null;

                    // Reset custom tooltip state to remove any closure over previous rows
                    // and restore default tooltip behavior.
                    try
                    {
                        chartBlocking.DisableCustomTooltips();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Blocking.RefreshData.DisableCustomTooltips error: {ex}");
                    }
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

                // Use ChartHelper to create the scatter series and keep a strongly-typed reference to avoid reflection
                _scatterSeries = ChartHelper.CreateWeightedScatterSeries(weightedPoints, "Blocked Sessions", minDiameter, MaxPointShapeDiameter);
                chartBlocking.Series = new ISeries[] { _scatterSeries };

                // Build a fast lookup from X ticks -> row index for tooltip and click mapping using ChartHelper
                try
                {
                    var ticks = weightedPoints.Select(wp => (long)Math.Round(wp.X ?? 0.0)).ToArray();
                    var mapResult = ChartHelper.BuildTickIndexMap(ticks);
                    _tickIndexMap = mapResult.tickIndexMap;
                    _sortedTicks = mapResult.sortedTicks;
                    _sortedIndices = mapResult.sortedIndices;
                }
                catch (Exception ex)
                {
                    // Fall back to null state but log for diagnostics
                    Debug.WriteLine($"Blocking.RefreshData.BuildTickIndexMap error: {ex}");
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
                        // If the tooltip is for the scatter series we created, use point.Index directly (avoids reflection)
                        if (point.Context?.Series == _scatterSeries && point.Index >= 0 && point.Index < rows.Count)
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

                        // Fallback: try to match by X ticks value using precomputed lookup for performance
                        var coord = point.Coordinate; // Coordinate is a struct (not nullable)
                                                      // Prefer SecondaryValue for the X coordinate on cartesian charts; fall back to PrimaryValue when rotated
                        double coordValue = double.NaN;
                        if (!double.IsNaN(coord.SecondaryValue)) coordValue = coord.SecondaryValue;
                        else if (!double.IsNaN(coord.PrimaryValue)) coordValue = coord.PrimaryValue;
                        if (!double.IsNaN(coordValue))
                        {
                            var x = (long)Math.Round(coordValue);
                            if (ChartHelper.TryGetIndexFromTicks(_tickIndexMap, _sortedTicks, _sortedIndices, x, out var idx) && idx >= 0 && idx < rows.Count)
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Blocking.TooltipFormatter error: {ex}");
                    }

                    // Last fallback: show the primary Y value formatted
                    var fallbackY = double.NaN;
                    try
                    {
                        fallbackY = point.Coordinate.PrimaryValue;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Blocking.TooltipFormatter fallback retrieval error: {ex}");
                    }
                    return !double.IsNaN(fallbackY) ? fallbackY.ToString("N0") : string.Empty;
                });
            }
            catch (Exception ex)
            {
                ToggleError(true, $"Error loading data: {ex.Message}");
            }
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
                // Prefer SecondaryValue for X (primary/secondary depend on chart orientation)
                double x = double.NaN;
                if (!double.IsNaN(coord.SecondaryValue)) x = coord.SecondaryValue;
                else if (!double.IsNaN(coord.PrimaryValue)) x = coord.PrimaryValue;

                if (!double.IsNaN(x))
                {
                    var xTicks = (long)Math.Round(x);

                    // Try to resolve the clicked X ticks to an original row index using
                    // shared helper to keep behavior consistent with tooltip lookup.
                    try
                    {
                        if (ChartHelper.TryGetIndexFromTicks(_tickIndexMap, _sortedTicks, _sortedIndices, xTicks, out var mappedIdx))
                        {
                            index = mappedIdx;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Blocking.ChartMouseDown tick lookup error: {ex}");
                        index = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Blocking.ChartMouseDown lookup error: {ex}");
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

        public void ApplyTheme(BaseTheme theme)
        {
            chartBlocking.ApplyTheme();
            toolStrip1.ApplyTheme();
            lblError.ForeColor = DBADashUser.IsDarkTheme ? DashColors.White : DashColors.Fail;
        }
    }
}