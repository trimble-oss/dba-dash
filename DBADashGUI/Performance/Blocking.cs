using DBADash;
using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System.Threading.Tasks;
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
using System.Text;
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

        // Store deadlock rows to support click-through to sp_BlitzLock with narrowed date range
        private List<DataRow> _deadlockRows;

        // Date grouping used when fetching deadlock data (minutes); used to compute click date window
        private int _deadlockDateGroupingMin;

        // Track total deadlock count for button display
        private long _totalDeadlockCount = 0;

        // True when the deadlock markers come from the Deadlocks collection rather than the performance counter.
        // See dbo.Deadlocks_Get.
        private bool _deadlocksCollected;

        // True when the Deadlocks collection is enabled for the instance, whichever source the chart uses.  Decides
        // which report the chart drills into.
        private bool _deadlockCollectionEnabled;

        // The counter is engine wide, and on Azure SQL DB the engine is shared - it can report deadlocks that never
        // touched this database (#1939).
        private bool IsAzureDB => CurrentContext?.EngineEdition == Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlDatabase;

        private string DeadlockSeriesName => _deadlocksCollected
            ? "Deadlocks"
            : "Deadlocks" + (DatabaseID > 0 ? " (Instance)" : "") + (IsAzureDB ? " (counter)" : ""); // At database level, the counter is instance-wide so clarify in legend

        private string DeadlockTooltipText
        {
            get
            {
                var sb = new StringBuilder();
                if (_deadlockCollectionEnabled)
                {
                    sb.AppendLine("Show Deadlocks");
                    sb.AppendLine("Opens the Deadlocks report, built from the deadlocks stored by the Deadlocks collection." +
                                  (DatabaseID > 0 ? "  Filtered to the selected database." : ""));
                }
                else
                {
                    sb.AppendLine("Show Deadlocks (sp_BlitzLock)");
                    sb.AppendLine("Runs sp_BlitzLock on the monitored instance via the messaging feature to retrieve deadlocks from the system_health extended event." +
                                  (DatabaseID > 0 ? "  Returns deadlocks for the selected database." : ""));
                }

                if (_deadlocksCollected)
                {
                    sb.Append("Chart values are deadlocks stored by the Deadlocks collection." +
                              (DatabaseID > 0 ? "  Only deadlocks involving the selected database are counted." : ""));
                    return sb.ToString();
                }

                sb.AppendLine("Chart values are derived from the \"Number of Deadlocks/sec\" performance counter, converted to cumulative counts.  Minor rounding differences may occur.");
                if (IsAzureDB)
                {
                    sb.AppendLine("On Azure SQL Database the counter covers the shared database engine and can include deadlocks outside this database.  " +
                                  (_deadlockCollectionEnabled
                                      ? "Switch View > Deadlock Counts to Deadlock Collection for accurate counts."
                                      : "Enable the Deadlocks collection for accurate counts."));
                }
                if (DatabaseID > 0)
                {
                    sb.AppendLine("At database level, the chart still reflects instance-level counts.");
                }
                return sb.ToString().TrimEnd();
            }
        }

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

        private (DataTable dt, int dateGroupingMin, bool isCollected, bool isCollectionEnabled) GetDeadlocksDT()
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
            cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("@PreferCollected", !Metric.DeadlockCountsFromCounter);
            var pIsCollected = cmd.Parameters.Add("@IsCollected", SqlDbType.Bit);
            pIsCollected.Direction = ParameterDirection.Output;
            var pIsCollectionEnabled = cmd.Parameters.Add("@IsCollectionEnabled", SqlDbType.Bit);
            pIsCollectionEnabled.Direction = ParameterDirection.Output;
            cmd.CommandTimeout = Config.DefaultCommandTimeout;
            DataTable dt = new();
            da.Fill(dt);
            return (dt, dateGroupingMin, pIsCollected.Value is true, pIsCollectionEnabled.Value is true);
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
                deadlockCountsCollectionToolStripMenuItem.Checked = !value.DeadlockCountsFromCounter;
                deadlockCountsCounterToolStripMenuItem.Checked = value.DeadlockCountsFromCounter;
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
                DataTable deadlockDt;
                if (Metric.Deadlocks)
                {
                    var result = GetDeadlocksDT();
                    deadlockDt = result.dt;
                    _deadlockDateGroupingMin = result.dateGroupingMin;
                    _deadlocksCollected = result.isCollected;
                    _deadlockCollectionEnabled = result.isCollectionEnabled;
                    // The native report needs no messaging or community tools - only the collection.
                    tsDeadlocks.Visible = tsDeadlocks.Enabled = HasDeadlockReportAccess || _deadlockCollectionEnabled;
                    tsDeadlocks.ToolTipText = DeadlockTooltipText;
                }
                else
                {
                    deadlockDt = new DataTable();
                    _deadlockDateGroupingMin = 0;
                }

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
                    _deadlockRows = deadlockDt.Rows.Cast<DataRow>()
                        .Where(r => Convert.ToDouble(r["DeadlockCount"]) > 0)
                        .ToList();
                    var deadlockPoints = _deadlockRows
                        .Select(r => new ObservablePoint(
                            ((DateTime)r["SnapshotDate"]).ToAppTimeZone().Ticks,
                            Convert.ToDouble(r["DeadlockCount"])))
                        .ToArray();

                    // Calculate total deadlock count for button display
                    _totalDeadlockCount = deadlockDt.Rows.Cast<DataRow>()
                        .Sum(r => Convert.ToInt64(r["DeadlockCount"]));

                    _deadlockSeries = new ScatterSeries<ObservablePoint, RectangleGeometry>
                    {
                        Values = deadlockPoints,
                        Name = DeadlockSeriesName,
                        GeometrySize = 8,
                        Fill = new SolidColorPaint(DashColors.Fail.ToSKColor()),
                        ScalesYAt = SeparateDeadlockAxis ? 1 : 0
                    };
                    seriesList.Add(_deadlockSeries);
                }
                else
                {
                    _deadlockSeries = null;
                    _deadlockRows = null;
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
            if ((_rows == null || _rows.Count == 0) && (_deadlockRows == null || _deadlockRows.Count == 0))
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

            // Show hand cursor for clickable blocking points and for deadlock squares. Deadlock squares
            // are clickable even without report access - clicking then explains the missing prerequisites.
            chartBlocking.Cursor = firstPoint is not null ? Cursors.Hand : Cursors.Default;
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
            if ((_rows == null || _rows.Count == 0) && (_deadlockRows == null || _deadlockRows.Count == 0))
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

            // Handle deadlock square clicks: open sp_BlitzLock with a narrowed date range
            if (firstPoint.Context?.Series == _deadlockSeries)
            {
                if (_deadlockRows == null) return;
                var idx = firstPoint.Index;
                if (idx < 0 || idx >= _deadlockRows.Count) return;
                var deadlockRow = _deadlockRows[idx];
                // Collected deadlocks carry the exact window the point counted.
                if (deadlockRow["FromDate"] != DBNull.Value && deadlockRow["ToDate"] != DBNull.Value)
                {
                    _ = ShowDeadlockReportAsync((DateTime)deadlockRow["FromDate"], (DateTime)deadlockRow["ToDate"]);
                    return;
                }
                var snapshotDateUtc = (DateTime)deadlockRow["SnapshotDate"];
                // Determine the report date range represented by this deadlock snapshot.
                // Start = PreviousSnapshotDate (if available) or snapshot minus 1 minute as a safe default.
                // End = snapshot + _deadlockDateGroupingMin minutes to include the full grouping bucket.
                // Example: a deadlock at 01:59:36 may be included in the 02:00 snapshot; using the previous snapshot
                // as the start and adding the grouping minutes to the end ensures the report covers the entire interval
                // represented by that snapshot bucket.
                var fromUtc = deadlockRow["PreviousSnapshotDate"] == DBNull.Value ? snapshotDateUtc.AddMinutes(-1) : (DateTime)deadlockRow["PreviousSnapshotDate"];
                var toUtc = snapshotDateUtc.AddMinutes(_deadlockDateGroupingMin);
                _ = ShowDeadlockReportAsync(fromUtc, toUtc);
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

        private void DeadlockCountsCollection_Click(object sender, EventArgs e) => SetDeadlockCountsFromCounter(false);

        private void DeadlockCountsCounter_Click(object sender, EventArgs e) => SetDeadlockCountsFromCounter(true);

        private void SetDeadlockCountsFromCounter(bool fromCounter)
        {
            Metric.DeadlockCountsFromCounter = fromCounter;
            deadlockCountsCollectionToolStripMenuItem.Checked = !fromCounter;
            deadlockCountsCounterToolStripMenuItem.Checked = fromCounter;
            RefreshData();
        }

        private async void ShowDeadlocks_Click(object sender, EventArgs e)
        {
            await ShowDeadlockReportAsync(DateRange.FromUTC, DateRange.ToUTC);
        }

        private const string CommunityToolsHelpUrl = "https://dbadash.com/docs/help/community-tools/";
        private const string MessagingHelpUrl = "https://dbadash.com/docs/help/messaging/";

        /// <summary>
        /// Opens the native Deadlocks report for the clicked window, using the graphs the collection stored -
        /// no round trip to the instance and nothing for the user to deploy.  Falls back to sp_BlitzLock where
        /// the collection is not enabled for this instance - nothing is storing the graphs the native report
        /// reads, so it has nothing to show.  An instance that collected in the past and has since been
        /// switched off goes to sp_BlitzLock too: the repository holds only what it collected before that, and
        /// the window the user clicked is more recent than that in every case that matters.
        /// </summary>
        private Task ShowDeadlockReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            if (CommonData.IsCollectionEnabled(InstanceID, CollectionType.Deadlocks))
            {
                var nativeContext = CurrentContext.DeepCopy();
                nativeContext.ObjectID = 0;
                nativeContext.ObjectName = string.Empty;
                var report = DeadlocksReport.Instance;
                nativeContext.Report = report;

                var nativeParams = report.GetCustomSqlParameters();
                // The chart's squares cover the period between two snapshots, so the report is scoped to that
                // window rather than to the global date filter.
                SetParameter(nativeParams, "@FromDate", fromUtc);
                SetParameter(nativeParams, "@ToDate", toUtc);
                // At database level the chart counts only deadlocks involving the database, so the report does too.
                if (DatabaseID > 0 && !string.IsNullOrEmpty(CurrentContext.DatabaseName))
                {
                    nativeParams.RemoveAll(p => p.Param.ParameterName.Equals("@DatabaseName", StringComparison.OrdinalIgnoreCase));
                    nativeParams.Add(new CustomSqlParameter
                    {
                        Param = new SqlParameter("@DatabaseName", SqlDbType.NVarChar, 128) { Value = CurrentContext.DatabaseName }
                    });
                }

                var nativeViewer = new CustomReportViewer { Context = nativeContext, CustomParams = nativeParams };
                return nativeViewer.ShowDialogAsync();
            }

            if (!HasDeadlockReportAccess)
            {
                ShowDeadlockAccessMessage();
                return Task.CompletedTask;
            }
            var fromInstance = fromUtc.AddMinutes(-CurrentContext.UTCOffset);
            var toInstance = toUtc.AddMinutes(-CurrentContext.UTCOffset);
            var reportViewer = new CustomReportViewer() { LoadDirectExecutionReport = true };
            var context = CurrentContext.DeepCopy();
            context.ObjectID = 0;
            context.ObjectName = string.Empty;
            context.Report = DeadlockReport;
            reportViewer.Context = context;
            var customParams = DeadlockReport.GetCustomSqlParameters();
            customParams.RemoveAll(p => p.Param.ParameterName.Equals("@StartDate", StringComparison.OrdinalIgnoreCase) || p.Param.ParameterName.Equals("@EndDate", StringComparison.OrdinalIgnoreCase));
            customParams.Add(new CustomSqlParameter { Param = new SqlParameter("@StartDate", fromInstance) { DbType = DbType.DateTime } });
            customParams.Add(new CustomSqlParameter { Param = new SqlParameter("@EndDate", toInstance) { DbType = DbType.DateTime } });
            reportViewer.CustomParams = customParams;
            return reportViewer.ShowDialogAsync();
        }

        private static void SetParameter(List<CustomSqlParameter> customParams, string name, DateTime value)
        {
            customParams.RemoveAll(p => p.Param.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
            customParams.Add(new CustomSqlParameter
            {
                Param = new SqlParameter(name, value) { DbType = DbType.DateTime2 }
            });
        }

        /// <summary>
        /// Explains why the deadlock (sp_BlitzLock) report can't be opened and offers to open the relevant
        /// setup documentation. Deadlock details are collected on demand by running sp_BlitzLock on the SQL
        /// instance via DBA Dash messaging, which requires both the community tools and messaging to be configured.
        /// </summary>
        private void ShowDeadlockAccessMessage()
        {
            var issues = GetMissingDeadlockRequirements(out var communityToolsIssue, out var messagingIssue);

            var links = new List<string>();
            if (communityToolsIssue) links.Add(CommunityToolsHelpUrl);
            if (messagingIssue) links.Add(MessagingHelpUrl);
            // If we couldn't attribute the problem to a specific area (e.g. report visibility role only),
            // offer both help pages so the user can review the full setup.
            if (links.Count == 0)
            {
                links.Add(CommunityToolsHelpUrl);
                links.Add(MessagingHelpUrl);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Deadlock details are retrieved on demand by running the sp_BlitzLock community tool on the SQL instance via DBA Dash messaging.");
            sb.AppendLine();
            sb.AppendLine("This requires the community tools and messaging to be configured. The following prerequisite(s) are not met:");
            sb.AppendLine();
            foreach (var issue in issues)
            {
                sb.AppendLine("• " + issue);
            }
            sb.AppendLine();
            sb.AppendLine("See the documentation for setup instructions:");
            foreach (var link in links)
            {
                sb.AppendLine(link);
            }
            sb.AppendLine();
            sb.Append("Would you like to open the documentation now?");

            if (MessageBox.Show(sb.ToString(), "Deadlock Report Unavailable", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) != DialogResult.Yes)
            {
                return;
            }

            foreach (var link in links)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Blocking.ShowDeadlockAccessMessage open url error: {ex}");
                }
            }
        }

        /// <summary>
        /// Builds the list of prerequisites that are missing for viewing the deadlock report and flags whether
        /// the community tools and/or messaging help pages are relevant.
        /// </summary>
        private List<string> GetMissingDeadlockRequirements(out bool communityToolsIssue, out bool messagingIssue)
        {
            communityToolsIssue = false;
            messagingIssue = false;
            var issues = new List<string>();

            if (CurrentContext == null || CurrentContext.InstanceID <= 0)
            {
                issues.Add("Select a single instance - deadlock analysis is not available at this level.");
                return issues;
            }

            if (!DeadlockReport.HasAccess())
            {
                issues.Add("Your account does not have permission to view the deadlock report. Contact your DBA Dash administrator.");
            }

            // Messaging: the request to run sp_BlitzLock is sent to the collection service via messaging.
            if (!DBADashUser.AllowMessaging)
            {
                messagingIssue = true;
                issues.Add("Messaging is not enabled for your account.");
            }
            else if (!CurrentContext.CanMessage)
            {
                messagingIssue = true;
                issues.Add("Messaging is not enabled on the DBA Dash service(s). Both the collection and import services must have messaging enabled.");
            }

            // Community tools: sp_BlitzLock must be enabled for your account and allowed on the collection service.
            if (!DBADashUser.CommunityScripts)
            {
                communityToolsIssue = true;
                issues.Add("Community tools are not enabled for your account.");
            }
            else
            {
                try
                {
                    var collectAgent = CurrentContext.CollectAgent;
                    if (collectAgent != null && !collectAgent.IsAllowAllScripts &&
                        !collectAgent.AllowedScripts.Contains("sp_BlitzLock"))
                    {
                        communityToolsIssue = true;
                        issues.Add("sp_BlitzLock is not in the list of community scripts allowed for the collection service.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Blocking.GetMissingDeadlockRequirements error: {ex}");
                }
            }

            if (issues.Count == 0)
            {
                // Fallback in case a condition wasn't attributed above - keep the message useful.
                issues.Add("One or more prerequisites for the deadlock report are not met.");
            }

            return issues;
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