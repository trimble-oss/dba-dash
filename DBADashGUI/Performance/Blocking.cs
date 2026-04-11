using DBADash;
using DBADashGUI.Charts;
using DBADashGUI.CommunityTools;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl, IMetricChart, IThemedControl
    {
        private Label lblError = new Label() { Dock = DockStyle.Fill, Visible = false, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
        private DBADashContext CurrentContext;
        private CustomReport DeadlockReport = CommunityTools.sp_BlitzLock.Instance;

        private bool SeparateDeadlockAxis = true;

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

        private int InstanceID => CurrentContext?.InstanceID ?? 0;
        private long maxBlockedTime;
        private int DatabaseID => CurrentContext?.DatabaseID ?? 0;

        private List<DataRow> _rows;
        private Dictionary<long, int> _tickIndexMap;

        // Mouse move throttling to avoid expensive GetPointsAt() calls on every mouse movement
        private Point _lastMousePosition = Point.Empty;

        private const int MouseMoveThresholdPixels = 2; // Only recompute when mouse moves beyond this threshold

        // Sorted arrays to support fast nearest-tick lookup when exact match isn't found
        private long[] _sortedTicks;

        private int[] _sortedIndices;

        // Strongly-typed reference to the scatter series to avoid reflection in tooltip rendering
        private ScatterSeries<WeightedPoint, CircleGeometry> _scatterSeries;

        // Strongly-typed reference to the deadlock scatter series for tooltip rendering
        private ScatterSeries<ObservablePoint, RectangleGeometry> _deadlockSeries;

        // Track total deadlock count for button display
        private long _totalDeadlockCount = 0;

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
            this.CurrentContext = _context;
            tsDeadlocks.Visible = HasDeadlockReportAccess;
            tsDeadlocks.Enabled = HasDeadlockReportAccess;
            RefreshData();
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
            cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", DatabaseID);
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

        private DataTable GetDeadlocksDT()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Deadlocks_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            var dateGroupingMin = DateHelper.DateGrouping(DateRange.DurationMins, 200);
            cmd.Parameters.AddWithValue("@DateGroupingMin", dateGroupingMin);
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
        public BlockingMetric Metric
        {
            get => field;
            set
            {
                field = value;
                blockingSnapshotsToolStripMenuItem.Checked = value.BlockingSnapshots;
                deadlocksToolStripMenuItem.Checked = value.Deadlocks;
            }
        } = new();

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
                if (!Metric.Deadlocks && !Metric.BlockingSnapshots)
                {
                    ToggleError(true, "No metrics selected");
                    return;
                }
                ToggleError(false);

                var dt = Metric.BlockingSnapshots ? GetDT() : new DataTable();
                var deadlockDt = Metric.Deadlocks ? GetDeadlocksDT() : new DataTable();

                // Create theme-aware paint for labels
                var labelPaint = CreateLabelPaint();
                var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
                var nameFontSize = DBADashUser.ChartAxisNameFontSize;

                // Configure axes first (even if no data) to show proper date labels
                var fromDate = DateRange.FromUTC.ToAppTimeZone();
                var toDate = DateHelper.AppNow < DateRange.ToUTC.ToAppTimeZone() ? DateHelper.AppNow : DateRange.ToUTC.ToAppTimeZone();
                var duration = toDate - fromDate;
                var xStepMinutes = Math.Max(1, DateHelper.DateGrouping((int)duration.TotalMinutes, 200, 1));
                var xUnit = TimeSpan.FromMinutes(xStepMinutes);
                chartBlocking.XAxes = new Axis[]
                {
                    new DateTimeAxis(xUnit, date => ChartHelper.FormatDateForChartLabel(date, duration))
                    {
                        MinLimit = fromDate.Ticks,
                        MaxLimit = toDate.Ticks,
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
                        Name = "Blocked Sessions",
                        IsVisible = Metric.BlockingSnapshots
                    }
                };

                if (dt.Rows.Count == 0 && deadlockDt.Rows.Count == 0)
                {
                    // Clear series and any retained state from previous refreshes to avoid
                    // holding onto DataRow references via tooltip closures and to keep
                    // click/tooltip mapping consistent with the empty chart.
                    chartBlocking.Series = Array.Empty<ISeries>();
                    _scatterSeries = null;
                    _deadlockSeries = null;
                    _rows = null;
                    _tickIndexMap = null;
                    _sortedTicks = null;
                    _sortedIndices = null;
                    _totalDeadlockCount = 0;
                    _lastMousePosition = Point.Empty; // Reset mouse throttling state

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

                    // Reset button text to default
                    tsDeadlocks.Text = "Show Deadlocks";

                    lblBlocking.Text = DatabaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
                    toolStrip1.Tag = DatabaseID > 0 ? "ALT" : null;
                    toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
                    return;
                }

                var rows = dt.Rows.Cast<DataRow>().ToList();
                _rows = rows;
                _lastMousePosition = Point.Empty; // Reset mouse throttling state on data refresh

                var seriesList = new List<ISeries>();

                if (rows.Count > 0)
                {
                    // compute maximum blocked time up-front so sizing is consistent
                    maxBlockedTime = rows.Max(r => (long)r["BlockedWaitTime"]);

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
                    seriesList.Add(_scatterSeries);

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
                }
                else
                {
                    _scatterSeries = null;
                    _tickIndexMap = null;
                    _sortedTicks = null;
                    _sortedIndices = null;
                    maxBlockedTime = 0;
                }

                if (deadlockDt.Rows.Count > 0)
                {
                    var deadlockPoints = deadlockDt.Rows.Cast<DataRow>()
                        .Select(r => new ObservablePoint(
                            ((DateTime)r["SnapshotDate"]).ToAppTimeZone().Ticks,
                            Convert.ToDouble(r["DeadlockCount"])))
                        .Where(p => p.Y > 0)
                        .ToArray();

                    // Calculate total deadlock count for button display
                    _totalDeadlockCount = deadlockDt.Rows.Cast<DataRow>()
                        .Sum(r => Convert.ToInt64(r["DeadlockCount"]));

                    _deadlockSeries = new ScatterSeries<ObservablePoint, RectangleGeometry>
                    {
                        Values = deadlockPoints,
                        Name = "Deadlocks" + (DatabaseID > 0 ? " (Instance)" : ""), // At database level, deadlocks are instance-wide so clarify in legend
                        GeometrySize = 8,
                        Fill = new SolidColorPaint(DashColors.Fail.ToSKColor()),
                        ScalesYAt = SeparateDeadlockAxis ? 1 : 0
                    };
                    seriesList.Add(_deadlockSeries);
                }
                else
                {
                    _deadlockSeries = null;
                    _totalDeadlockCount = 0;
                }

                // Update Y axes before setting series so the secondary axis (index 1)
                // exists when the deadlock series references ScalesYAt = 1.
                var yMax = rows.Count > 0 ? Math.Max(100, rows.Max(r => (int)r["BlockedSessionCount"])) : 100;
                var yAxesList = chartBlocking.YAxes.ToList();
                yAxesList[0].MaxLimit = yMax;

                if (_deadlockSeries != null && SeparateDeadlockAxis)
                {
                    var deadlockMax = deadlockDt.Rows.Cast<DataRow>().Max(r => Convert.ToDouble(r["DeadlockCount"]));
                    yAxesList.Add(new Axis
                    {
                        Labeler = value => value.ToString("0"),
                        MinLimit = 0,
                        MaxLimit = Math.Max(10, deadlockMax * 1.1),
                        LabelsPaint = labelPaint,
                        TextSize = labelFontSize,
                        NamePaint = labelPaint,
                        NameTextSize = nameFontSize,
                        Name = "Deadlocks",
                        Position = AxisPosition.End,
                        SeparatorsPaint = null
                    });
                }

                chartBlocking.YAxes = yAxesList.ToArray();

                // Set series after Y axes are configured so ScalesYAt = 1 resolves correctly.
                chartBlocking.Series = seriesList;

                // Update the Show Deadlocks button text with the total deadlock count
                if (_totalDeadlockCount > 0)
                {
                    tsDeadlocks.Text = $"Show Deadlocks ({_totalDeadlockCount:N0})";
                }
                else
                {
                    tsDeadlocks.Text = "Show Deadlocks";
                }
                var metricLabel = Metric.Deadlocks && Metric.BlockingSnapshots
                   ? "Blocking && Deadlocks"
                   : Metric.Deadlocks
                       ? "Deadlocks"
                       : "Blocking";
                lblBlocking.Text = DatabaseID > 0 ? $"{metricLabel}: Database" : $"{metricLabel}: Instance";
                toolStrip1.Tag = DatabaseID > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
                toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);

                // Enable custom tooltips with custom formatter to show blocked time
                chartBlocking.EnableCustomTooltips(point =>
                {
                    try
                    {
                        // If the tooltip is for the deadlock scatter series
                        if (point.Context?.Series == _deadlockSeries)
                        {
                            var y = point.Coordinate.PrimaryValue;
                            return !double.IsNaN(y) ? $"Deadlocks: {y:N0}" : string.Empty;
                        }

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
            chartBlocking.MouseMove += ChartBlocking_MouseMove;
        }

        private void ChartBlocking_MouseMove(object sender, MouseEventArgs e)
        {
            if (_rows == null || _rows.Count == 0)
            {
                chartBlocking.Cursor = Cursors.Default;
                return;
            }

            // Throttle GetPointsAt() call: only recompute when mouse moves beyond a small pixel threshold
            // This avoids expensive hit-testing on every single mouse move event
            var mouseMoved = _lastMousePosition == Point.Empty ||
                             Math.Abs(e.X - _lastMousePosition.X) > MouseMoveThresholdPixels ||
                             Math.Abs(e.Y - _lastMousePosition.Y) > MouseMoveThresholdPixels;

            if (!mouseMoved)
            {
                return; // Cursor state hasn't changed, skip expensive GetPointsAt() call
            }

            _lastMousePosition = e.Location;

            var foundPoints = chartBlocking.GetPointsAt(
                new LiveChartsCore.Drawing.LvcPointD(e.Location.X, e.Location.Y));

            var firstPoint = foundPoints.FirstOrDefault();

            // Show hand cursor only for clickable blocking points (not deadlock series)
            chartBlocking.Cursor = firstPoint is not null && firstPoint.Context?.Series != _deadlockSeries
                ? Cursors.Hand
                : Cursors.Default;
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
            if (firstPoint is null || firstPoint.Context?.Series == _deadlockSeries)
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

        private void BlockingSnapshots_Click(object sender, EventArgs e)
        {
            Metric.BlockingSnapshots = blockingSnapshotsToolStripMenuItem.Checked;
            RefreshData();
        }

        private void DeadlocksSelection_Click(object sender, EventArgs e)
        {
            Metric.Deadlocks = deadlocksToolStripMenuItem.Checked;
            RefreshData();
        }

        private async void ShowDeadlocks_Click(object sender, EventArgs e)
        {
            if (!HasDeadlockReportAccess)
            {
                MessageBox.Show("You do not have access to this report.");
                return;
            }
            var reportViewer = new CustomReportViewer() { LoadDirectExecutionReport = true };
            var context = CurrentContext.DeepCopy();
            context.ObjectID = 0;
            context.ObjectName = string.Empty;
            context.Report = DeadlockReport;
            reportViewer.Context = context;

            // Convert date range to instance local time
            var fromDateUtc = DateRange.FromUTC;
            var toDateUtc = DateRange.ToUTC;
            var fromDateInstance = fromDateUtc.AddMinutes(-CurrentContext.UTCOffset);
            var toDateInstance = toDateUtc.AddMinutes(-CurrentContext.UTCOffset);
            var customParams = DeadlockReport.GetCustomSqlParameters();
            customParams.RemoveAll(p => p.Param.ParameterName.Equals("@StartDate", StringComparison.OrdinalIgnoreCase) || p.Param.ParameterName.Equals("@EndDate", StringComparison.OrdinalIgnoreCase));
            customParams.Add(new CustomSqlParameter { Param = new SqlParameter("@StartDate", fromDateInstance) { DbType = DbType.DateTime } });
            customParams.Add(new CustomSqlParameter { Param = new SqlParameter("@EndDate", toDateInstance) { DbType = DbType.DateTime } });
            reportViewer.CustomParams = customParams;
            await reportViewer.ShowDialogAsync();
        }

        private bool HasDeadlockReportAccess => CurrentContext != null &&
            DeadlockReport.HasAccess() &&
            DBADashUser.AllowMessaging &&
            CurrentContext.CanMessage &&
            DBADashUser.CommunityScripts
            && CurrentContext.InstanceID > 0
            && (CurrentContext.CollectAgent.IsAllowAllScripts || CurrentContext.IsScriptAllowed("dbo", "sp_BlitzLock"));
    }
}