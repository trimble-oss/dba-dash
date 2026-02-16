using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Helper class to add custom tooltips to CartesianChart controls that can overflow chart boundaries
    /// </summary>
    internal static class ChartTooltipHelper
    {
        private static readonly ConditionalWeakTable<CartesianChart, TooltipForm> _tooltipForms = new();
        private static readonly ConditionalWeakTable<CartesianChart, TooltipState> _tooltipStates = new();

        private const int TooltipShowDelayMs = 100; // Delay before showing tooltip when mouse stops
        private const int SlowMovementThresholdMs = 50; // Time between point changes to be considered "slow movement"
        private const int TimerIntervalMs = 100; // Timer check interval
        private const int TooltipOffsetX = 15; // Horizontal offset from cursor
        private const int TooltipOffsetY = 15; // Vertical offset from cursor
        private const int TooltipMargin = 5; // Margin when adjusting to stay on screen
        private const double PointDetectionTolerance = 20.0; // Pixel tolerance for point detection
        private const int MouseJitterThreshold = 2; // Pixel threshold for mouse movement to be considered "moving"

        // Pre-calculated search offsets for circular pattern (12 points at 30° intervals)
        private static readonly (int dx, int dy)[] _searchOffsets = CalculateSearchOffsets();

        private static (int dx, int dy)[] CalculateSearchOffsets()
        {
            var angles = new[] { 0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330 };
            var offsets = new (int dx, int dy)[angles.Length];

            for (int i = 0; i < angles.Length; i++)
            {
                var radians = angles[i] * Math.PI / 180.0;
                offsets[i] = (
                    (int)(PointDetectionTolerance * Math.Cos(radians)),
                    (int)(PointDetectionTolerance * Math.Sin(radians))
                );
            }

            return offsets;
        }

        /// <summary>
        /// Delegate for custom value formatting in tooltips
        /// </summary>
        /// <param name="point">The chart point being formatted</param>
        /// <returns>Formatted string to display in tooltip</returns>
        public delegate string TooltipValueFormatter(LiveChartsCore.Kernel.ChartPoint point);

        private class TooltipState
        {
            public int LastPointIndex { get; set; } = -1;
            public int LastPointCount { get; set; } = 0;
            public Timer ShowTimer { get; set; }
            public bool IsTooltipVisible { get; set; } = false;
            public DateTime? PendingDate { get; set; }
            public List<(string name, string value, Color color)> PendingSeriesData { get; set; }
            public Point PendingMouseLocation { get; set; }
            public DateTime LastMouseMoveTime { get; set; } = DateTime.MinValue;
            public DateTime LastPointChangeTime { get; set; } = DateTime.MinValue;
            public Point LastMousePosition { get; set; } = Point.Empty;
            public TooltipValueFormatter ValueFormatter { get; set; }
            public Point LastTooltipPosition { get; set; } = Point.Empty; // Track last tooltip screen position
        }

        private class TooltipForm : Form
        {
            private readonly Panel _borderPanel;
            private readonly Font _headerFont;
            private readonly Font _normalFont;
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_TRANSPARENT = 0x00000020; // Click-through

            private Label _dateLabel;
            private TableLayoutPanel _layout;
            private List<(Panel namePanel, Label valueLabel)> _seriesControls = new();

            public TooltipForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                TopMost = true;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                BackColor = DashColors.TrimbleBlue;
                Padding = new Padding(2);
                DoubleBuffered = true;

                // Cache fonts to avoid memory leaks
                _headerFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                _normalFont = new Font("Segoe UI", 9F);

                // Enhanced double buffering to reduce flicker
                SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);

                _borderPanel = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = DashColors.GrayLight,
                    Padding = new Padding(10),
                    Dock = DockStyle.Fill
                };
                Controls.Add(_borderPanel);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _headerFont?.Dispose();
                    _normalFont?.Dispose();
                }
                base.Dispose(disposing);
            }

            // Prevent the tooltip from stealing focus or capturing mouse
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT;
                    return cp;
                }
            }

            protected override bool ShowWithoutActivation => true;

            public void SetContent(DateTime? date, List<(string name, string value, Color color)> series)
            {
                // Try to reuse existing controls if structure is the same (same series count, same date visibility)
                bool canReuse = _layout != null && 
                               series.Count == _seriesControls.Count &&
                               (date.HasValue == (_dateLabel != null));

                if (canReuse)
                {
                    // Suspend auto-sizing during update to prevent flicker from size changes
                    this.SuspendLayout();

                    // Update existing controls - much faster, no flicker
                    if (_dateLabel != null && date.HasValue)
                    {
                        _dateLabel.Text = date.Value.ToString("G");
                    }

                    for (int i = 0; i < series.Count; i++)
                    {
                        var (name, value, color) = series[i];
                        var (namePanel, valueLabel) = _seriesControls[i];

                        // Update color box
                        var colorBox = namePanel.Controls[0];
                        colorBox.BackColor = color;

                        // Update name label
                        var nameLabel = (Label)namePanel.Controls[1];
                        nameLabel.Text = name;

                        // Update value label
                        valueLabel.Text = value;
                    }

                    // Force layout but maintain current size to prevent flicker
                    this.ResumeLayout(false);
                    this.PerformLayout();
                }
                else
                {
                    // Structure changed, rebuild from scratch
                    _borderPanel.SuspendLayout();
                    _borderPanel.Controls.Clear();
                    _seriesControls.Clear();

                    _layout = new TableLayoutPanel
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 2,
                        RowCount = 0,
                        Padding = new Padding(10),
                        Margin = new Padding(0),
                        CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                    };
                    _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                    // Date header (spans both columns)
                    if (date.HasValue)
                    {
                        _dateLabel = new Label
                        {
                            Text = date.Value.ToString("G"),
                            AutoSize = true,
                            Font = _headerFont,
                            ForeColor = DashColors.TrimbleBlue,
                            Margin = new Padding(0, 0, 0, 8),
                            Padding = new Padding(0)
                        };
                        _layout.Controls.Add(_dateLabel, 0, _layout.RowCount);
                        _layout.SetColumnSpan(_dateLabel, 2);
                        _layout.RowCount++;
                    }
                    else
                    {
                        _dateLabel = null;
                    }

                    // Series lines with two columns: name (with color) and value
                    foreach (var (name, value, color) in series)
                    {
                        // Left column: color box + series name
                        var namePanel = new Panel
                        {
                            AutoSize = true,
                            Height = 16,
                            Margin = new Padding(0, 2, 10, 2),
                            Padding = new Padding(0)
                        };

                        var colorBox = new Panel
                        {
                            Width = 12,
                            Height = 12,
                            BackColor = color,
                            Left = 0,
                            Top = 2
                        };

                        var nameLabel = new Label
                        {
                            Text = name,
                            AutoSize = true,
                            Font = _normalFont,
                            ForeColor = DashColors.TrimbleBlue,
                            Left = 16,
                            Top = 0,
                            Padding = new Padding(0),
                            Margin = new Padding(0)
                        };

                        namePanel.Controls.Add(colorBox);
                        namePanel.Controls.Add(nameLabel);
                        namePanel.Width = 16 + nameLabel.Width;

                        // Right column: value (right-aligned)
                        var valueLabel = new Label
                        {
                            Text = value,
                            AutoSize = true,
                            Font = _normalFont,
                            ForeColor = DashColors.TrimbleBlue,
                            TextAlign = ContentAlignment.MiddleRight,
                            Margin = new Padding(0, 2, 0, 2),
                            Padding = new Padding(0),
                            Anchor = AnchorStyles.Right
                        };

                        _layout.Controls.Add(namePanel, 0, _layout.RowCount);
                        _layout.Controls.Add(valueLabel, 1, _layout.RowCount);
                        _layout.RowCount++;

                        _seriesControls.Add((namePanel, valueLabel));
                    }

                    _borderPanel.Controls.Add(_layout);
                    _borderPanel.ResumeLayout(true);
                }
            }
        }

        /// <summary>
        /// Enable custom tooltips for a CartesianChart. This hides the built-in tooltips and creates a floating tooltip.
        /// </summary>
        /// <param name="chart">The chart to add custom tooltips to</param>
        public static void EnableCustomTooltips(this CartesianChart chart)
        {
            EnableCustomTooltips(chart, null);
        }

        /// <summary>
        /// Enable custom tooltips for a CartesianChart with a custom value formatter.
        /// The formatter allows custom display of point values (e.g., including additional data from the data source).
        /// </summary>
        /// <param name="chart">The chart to add custom tooltips to</param>
        /// <param name="valueFormatter">Optional custom formatter for tooltip values. If null, uses default formatting.</param>
        public static void EnableCustomTooltips(this CartesianChart chart, TooltipValueFormatter valueFormatter)
        {
            // Hide built-in LiveCharts tooltips
            chart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden;

            // Create tooltip form if it doesn't exist
            if (!_tooltipForms.TryGetValue(chart, out var tooltipForm))
            {
                tooltipForm = new TooltipForm();
                _tooltipForms.Add(chart, tooltipForm);

                // Initialize state tracking with timer
                var state = new TooltipState
                {
                    ShowTimer = new System.Windows.Forms.Timer { Interval = TimerIntervalMs },
                    ValueFormatter = valueFormatter
                };

                // Set up the single timer tick handler
                state.ShowTimer.Tick += (s, e) =>
                {
                    // Check if enough time has passed since last mouse movement
                    var timeSinceLastMove = DateTime.Now - state.LastMouseMoveTime;

                    if (timeSinceLastMove.TotalMilliseconds >= TooltipShowDelayMs)
                    {
                        // Mouse has been still for the required delay, show tooltip
                        state.ShowTimer.Stop();

                        if (state.PendingSeriesData != null && state.PendingSeriesData.Count > 0)
                        {
                            tooltipForm.SetContent(state.PendingDate, state.PendingSeriesData);
                            PositionTooltip(chart, tooltipForm, state.PendingMouseLocation);
                            state.LastTooltipPosition = tooltipForm.Location; // Track initial position
                            tooltipForm.Show();
                            state.IsTooltipVisible = true;
                        }
                    }
                    // If not enough time has passed, timer will continue and check again
                };

                _tooltipStates.Add(chart, state);
            }
            else
            {
                // Tooltip already exists, just update the formatter
                if (_tooltipStates.TryGetValue(chart, out var state))
                {
                    state.ValueFormatter = valueFormatter;
                }
            }

            // Wire up mouse events
            chart.MouseMove -= Chart_MouseMove;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseLeave -= Chart_MouseLeave;
            chart.MouseLeave += Chart_MouseLeave;
        }

        /// <summary>
        /// Disable custom tooltips for a CartesianChart and restore default behavior
        /// </summary>
        /// <param name="chart">The chart to disable custom tooltips for</param>
        public static void DisableCustomTooltips(this CartesianChart chart)
        {
            if (chart == null) return;

            chart.MouseMove -= Chart_MouseMove;
            chart.MouseLeave -= Chart_MouseLeave;

            if (_tooltipStates.TryGetValue(chart, out var state))
            {
                state.ShowTimer?.Stop();
                state.ShowTimer?.Dispose();
                _tooltipStates.Remove(chart);
            }

            if (_tooltipForms.TryGetValue(chart, out var tooltipForm))
            {
                tooltipForm.Hide();
                tooltipForm.Dispose();
                _tooltipForms.Remove(chart);
            }

            // Re-enable built-in tooltips
            chart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top;
        }

        private static void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not CartesianChart chart) return;
            if (!_tooltipForms.TryGetValue(chart, out var tooltipForm)) return;
            if (!_tooltipStates.TryGetValue(chart, out var state)) return;

            try
            {
                // Only update LastMouseMoveTime if mouse actually moved a meaningful distance
                // This prevents tiny jitters from resetting the tooltip delay timer
                var mouseMoved = state.LastMousePosition == Point.Empty || 
                                 Math.Abs(e.X - state.LastMousePosition.X) > MouseJitterThreshold || 
                                 Math.Abs(e.Y - state.LastMousePosition.Y) > MouseJitterThreshold;

                if (mouseMoved)
                {
                    state.LastMouseMoveTime = DateTime.Now;
                    state.LastMousePosition = e.Location;
                }

                var now = DateTime.Now;

                // Get all points under mouse cursor (important for stacked charts)
                // Try exact position first, then check nearby positions for better tolerance with small points
                var points = chart.GetPointsAt(new LiveChartsCore.Drawing.LvcPointD(e.X, e.Y)).ToList();

                // If no points found at exact location, search in a radius to find the closest point
                if (points.Count == 0)
                {
                    var allFoundPoints = new List<(LiveChartsCore.Kernel.ChartPoint point, double distance)>();
                    var seenPoints = new HashSet<(object series, int index)>(); // For deduplication

                    // Use pre-calculated circular search offsets for better performance
                    foreach (var (dx, dy) in _searchOffsets)
                    {
                        var offsetPoints = chart.GetPointsAt(new LiveChartsCore.Drawing.LvcPointD(e.X + dx, e.Y + dy)).ToList();
                        foreach (var pt in offsetPoints)
                        {
                            var pointKey = (pt.Context.Series, pt.Index);
                            if (seenPoints.Add(pointKey)) // Only process if not already seen
                            {
                                var distance = Math.Sqrt(dx * dx + dy * dy);
                                allFoundPoints.Add((pt, distance));
                            }
                        }
                    }

                    // Select the closest point if any were found - use linear search to avoid LINQ overhead
                    if (allFoundPoints.Count > 0)
                    {
                        var closest = allFoundPoints[0];
                        for (int i = 1; i < allFoundPoints.Count; i++)
                        {
                            if (allFoundPoints[i].distance < closest.distance)
                            {
                                closest = allFoundPoints[i];
                            }
                        }
                        points = new List<LiveChartsCore.Kernel.ChartPoint> { closest.point };
                    }
                }

                if (points.Count > 0)
                {
                    var firstPoint = points[0];
                    var currentIndex = firstPoint.Index;
                    var currentCount = points.Count;

                    // Check if we moved to a different point
                    bool pointChanged = currentIndex != state.LastPointIndex || currentCount != state.LastPointCount;

                    if (pointChanged)
                    {
                        // Calculate time since last point change to detect slow vs fast movement
                        var timeSinceLastPointChange = now - state.LastPointChangeTime;
                        state.LastPointChangeTime = now;
                        state.LastPointIndex = currentIndex;
                        state.LastPointCount = currentCount;

                        // Get date from first point
                        TryGetDateFromPoint(firstPoint, out var dateTime);

                        // Collect series info
                        var seriesData = new List<(string name, string value, Color color)>(points.Count);
                        foreach (var point in points)
                        {
                            var seriesName = point.Context.Series.Name ?? "Value";

                            // Use custom formatter if provided, otherwise try to use Y-axis formatter, then default to N2
                            string formattedValue;
                            if (state.ValueFormatter != null)
                            {
                                formattedValue = state.ValueFormatter(point);
                            }
                            else
                            {
                                formattedValue = GetFormattedValue(chart, point);
                            }

                            var color = GetSeriesColor(point.Context.Series);
                            seriesData.Add((seriesName, formattedValue, color));
                        }

                        // If tooltip is already visible
                        if (state.IsTooltipVisible)
                        {
                            // Tooltip already showing: keep it visible and update immediately
                            // User has already indicated they want to see tooltips
                            tooltipForm.SetContent(dateTime, seriesData);
                            state.PendingDate = dateTime;
                            state.PendingSeriesData = seriesData;
                            state.PendingMouseLocation = e.Location;
                        }
                        else
                        {
                            // Tooltip not visible: store pending data and start/continue timer
                            state.PendingDate = dateTime;
                            state.PendingSeriesData = seriesData;
                            state.PendingMouseLocation = e.Location;

                            // Start timer if not already running
                            if (!state.ShowTimer.Enabled)
                            {
                                state.ShowTimer.Start();
                            }
                        }
                    }
                    else
                    {
                        // Same point, just update pending location for smooth positioning
                        state.PendingMouseLocation = e.Location;
                    }

                    // Always update position if tooltip is visible, but only if position actually changed
                    if (state.IsTooltipVisible && tooltipForm.Visible)
                    {
                        var screenPt = chart.PointToScreen(e.Location);
                        var newTooltipPos = new Point(screenPt.X + TooltipOffsetX, screenPt.Y + TooltipOffsetY);

                        // Only update if moved by more than a few pixels to reduce flicker
                        if (Math.Abs(newTooltipPos.X - state.LastTooltipPosition.X) > 3 || 
                            Math.Abs(newTooltipPos.Y - state.LastTooltipPosition.Y) > 3)
                        {
                            PositionTooltip(chart, tooltipForm, e.Location);
                            state.LastTooltipPosition = tooltipForm.Location;
                        }
                    }
                }
                else
                {
                    // No points found - but don't immediately hide if we have pending data
                    // This handles cases where tolerance search temporarily fails
                    if (state.PendingSeriesData == null || !state.ShowTimer.Enabled)
                    {
                        // No pending data or timer not running - safe to hide
                        if (tooltipForm.Visible)
                        {
                            tooltipForm.Hide();
                            state.IsTooltipVisible = false;
                            state.LastPointIndex = -1;
                            state.LastPointCount = 0;
                        }
                    }
                    // else: keep pending data and let timer continue
                }
            }
            catch
            {
                // Suppress any errors during tooltip display
                ResetTooltipState(state, tooltipForm);
            }
        }

        private static void ResetTooltipState(TooltipState state, TooltipForm tooltipForm)
        {
            state.ShowTimer?.Stop();
            state.PendingSeriesData = null;
            state.IsTooltipVisible = false;
            state.LastPointIndex = -1;
            state.LastPointCount = 0;
            tooltipForm?.Hide();
        }

        private static void PositionTooltip(CartesianChart chart, TooltipForm tooltipForm, Point mouseLocation)
        {
            var screenPt = chart.PointToScreen(mouseLocation);
            var tooltipX = screenPt.X + TooltipOffsetX;
            var tooltipY = screenPt.Y + TooltipOffsetY;

            // Get screen bounds
            var screen = Screen.FromPoint(screenPt);
            var screenBounds = screen.WorkingArea;

            // Adjust if would go off screen
            if (tooltipX + tooltipForm.Width > screenBounds.Right)
            {
                tooltipX = screenPt.X - tooltipForm.Width - TooltipMargin;
            }
            if (tooltipY + tooltipForm.Height > screenBounds.Bottom)
            {
                tooltipY = screenPt.Y - tooltipForm.Height - TooltipMargin;
            }

            tooltipForm.Location = new Point(tooltipX, tooltipY);
        }

        private static bool TryGetDateFromPoint(LiveChartsCore.Kernel.ChartPoint point, out DateTime dateTime)
        {
            dateTime = default;

            try
            {
                // Try to extract DateTimePoint values from various series types
                IList<DateTimePoint> values = point.Context.Series switch
                {
                    LineSeries<DateTimePoint> lineSeries => lineSeries.Values as IList<DateTimePoint>,
                    StackedAreaSeries<DateTimePoint> stackedAreaSeries => stackedAreaSeries.Values as IList<DateTimePoint>,
                    StackedColumnSeries<DateTimePoint> stackedColumnSeries => stackedColumnSeries.Values as IList<DateTimePoint>,
                    ScatterSeries<DateTimePoint> scatterSeries => scatterSeries.Values as IList<DateTimePoint>,
                    _ => null
                };

                if (values != null && point.Index >= 0 && point.Index < values.Count)
                {
                    dateTime = values[point.Index].DateTime;
                    return true;
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        private static Color GetSeriesColor(ISeries series)
        {
            try
            {
                // Try to extract paint color from various series types
                SolidColorPaint paint = series switch
                {
                    LineSeries<DateTimePoint> ls => ls.Fill as SolidColorPaint ?? ls.Stroke as SolidColorPaint,
                    StackedAreaSeries<DateTimePoint> sas => sas.Fill as SolidColorPaint ?? sas.Stroke as SolidColorPaint,
                    StackedColumnSeries<DateTimePoint> scs => scs.Fill as SolidColorPaint ?? scs.Stroke as SolidColorPaint,
                    ScatterSeries<DateTimePoint> ss => ss.Fill as SolidColorPaint ?? ss.Stroke as SolidColorPaint,
                    _ => null
                };

                if (paint?.Color is SKColor skColor)
                {
                    return Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);
                }
            }
            catch
            {
                // Ignore errors
            }

            // Default to TrimbleBlue if color can't be determined
            return DashColors.TrimbleBlue;
        }

        /// <summary>
        /// Gets the formatted value for a chart point using the Y-axis labeler if available
        /// </summary>
        private static string GetFormattedValue(CartesianChart chart, LiveChartsCore.Kernel.ChartPoint point)
        {
            try
            {
                // Try to get the Y-axis index using pattern matching (fast path for common types)
                int? yAxisIndex = point.Context.Series switch
                {
                    LineSeries<DateTimePoint> ls => ls.ScalesYAt,
                    StackedAreaSeries<DateTimePoint> sas => sas.ScalesYAt,
                    StackedColumnSeries<DateTimePoint> scs => scs.ScalesYAt,
                    ScatterSeries<DateTimePoint> ss => ss.ScalesYAt,
                    _ => null
                };

                // If pattern matching didn't match, try reflection as fallback (slow path for other types)
                if (!yAxisIndex.HasValue)
                {
                    var seriesType = point.Context.Series.GetType();
                    var property = seriesType.GetProperty("ScalesYAt");
                    if (property != null && property.CanRead)
                    {
                        yAxisIndex = (int)property.GetValue(point.Context.Series);
                    }
                }

                // Get the Y-axes from the chart
                if (yAxisIndex.HasValue && chart.YAxes != null && yAxisIndex.Value < chart.YAxes.Count())
                {
                    var yAxis = chart.YAxes.ElementAt(yAxisIndex.Value);

                    // Use the axis labeler if available
                    if (yAxis?.Labeler != null)
                    {
                        return yAxis.Labeler(point.Coordinate.PrimaryValue);
                    }
                }
            }
            catch
            {
                // Fall through to default formatting on any error
            }

            // Default to N2 format if no axis labeler found
            return point.Coordinate.PrimaryValue.ToString("N2");
        }

        private static void Chart_MouseLeave(object sender, EventArgs e)
        {
            if (sender is not CartesianChart chart) return;

            if (_tooltipStates.TryGetValue(chart, out var state))
            {
                state.ShowTimer.Stop();
                state.PendingSeriesData = null;
                state.IsTooltipVisible = false;
                state.LastPointIndex = -1;
                state.LastPointCount = 0;
                state.LastMousePosition = Point.Empty;
                state.LastTooltipPosition = Point.Empty;
            }

            if (_tooltipForms.TryGetValue(chart, out var tooltipForm))
            {
                tooltipForm.Hide();
            }
        }
    }
}