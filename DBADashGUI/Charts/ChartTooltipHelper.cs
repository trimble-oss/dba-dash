using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Helper class to add custom tooltips to CartesianChart and PieChart controls that can overflow chart boundaries
    /// </summary>
    internal static class ChartTooltipHelper
    {
        private static readonly ConditionalWeakTable<Control, TooltipForm> _tooltipForms = new();
        private static readonly ConditionalWeakTable<Control, TooltipState> _tooltipStates = new();

        private const int TooltipShowDelayMs = 100; // Delay before showing tooltip when mouse stops
        private const int SlowMovementThresholdMs = 50; // Time between point changes to be considered "slow movement"
        private const int TimerIntervalMs = 100; // Timer check interval
        private const int TooltipOffsetX = 15; // Horizontal offset from cursor
        private const int TooltipOffsetY = 15; // Vertical offset from cursor
        private const int TooltipMargin = 5; // Margin when adjusting to stay on screen
        private const double PointDetectionTolerance = 20.0; // Pixel tolerance for point detection
        private const int MouseJitterThreshold = 2; // Pixel threshold for mouse movement to be considered "moving"

        /// <summary>
        /// How wide a series name may be drawn before it wraps onto another line.  A statement is a legitimate
        /// series name and runs to a couple of hundred characters, which on one line is a tooltip wider than the
        /// screen; wrapping keeps it readable and keeps the value column where the eye expects it.
        /// </summary>
        private const int MaxNameWidth = 520;

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
            public int LastHighlightIndex { get; set; } = -1;
            public ISeries LastSeries { get; set; } // Pie charts: the slice under the mouse (each slice is a series)
            public Dictionary<ISeries, int> PieSliceRows { get; set; } // Pie charts: tooltip row index of each slice
            public Timer ShowTimer { get; set; }
            public bool IsTooltipVisible { get; set; } = false;
            public DateTime? PendingDate { get; set; }
            public string PendingXLabel { get; set; }
            public List<(string name, string value, Color color)> PendingSeriesData { get; set; }
            public int PendingHighlightIndex { get; set; } = -1;
            public Point PendingMouseLocation { get; set; }
            public DateTime LastMouseMoveTime { get; set; } = DateTime.MinValue;
            public DateTime LastPointChangeTime { get; set; } = DateTime.MinValue;
            public Point LastMousePosition { get; set; } = Point.Empty;
            public TooltipValueFormatter ValueFormatter { get; set; }
            public Point LastTooltipPosition { get; set; } = Point.Empty; // Track last tooltip screen position
            // When true, the tooltip logic is paused and Chart_MouseMove should ignore mouse events
            public bool IsPaused { get; set; } = false;
        }

        private class TooltipForm : Form
        {
            private readonly Panel _borderPanel;
            private readonly Font _headerFont;
            private readonly Font _normalFont;
            private readonly Font _highlightFont;
            private readonly SolidBrush _highlightBrush;
            private readonly Pen _highlightColorBoxPen;
            private static readonly Color TextColor = DashColors.TrimbleBlue;
            private static readonly Color HighlightTextColor = DashColors.White;
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_TRANSPARENT = 0x00000020; // Click-through

            private Label _dateLabel;
            private TableLayoutPanel _layout;
            private List<(Panel namePanel, Label valueLabel)> _seriesControls = new();
            private int _highlightedIndex = -1; // Index into series of the item under the mouse, -1 for none

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
                _highlightFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                _highlightBrush = new SolidBrush(DashColors.TrimbleBlue);
                _highlightColorBoxPen = new Pen(HighlightTextColor); // Keeps the series color visible against the highlight

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
                    _highlightFont?.Dispose();
                    _highlightBrush?.Dispose();
                    _highlightColorBoxPen?.Dispose();
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

            /// <summary>
            /// Highlight the series row under the mouse (e.g. the segment of a stacked column). Pass -1 for no highlight.
            /// Only touches the previous and new rows when the highlight changes - the row background is painted in Layout_CellPaint.
            /// </summary>
            public void SetHighlight(int index)
            {
                if (index == _highlightedIndex) return;
                ApplyRowStyle(_highlightedIndex, false);
                _highlightedIndex = index;
                ApplyRowStyle(_highlightedIndex, true);
                _layout?.Invalidate(true); // Row controls are transparent so they need to repaint over the cell background
            }

            private void ApplyRowStyle(int index, bool highlighted)
            {
                if (index < 0 || index >= _seriesControls.Count) return;
                var (namePanel, valueLabel) = _seriesControls[index];
                var nameLabel = (Label)namePanel.Controls[1];
                var font = highlighted ? _highlightFont : _normalFont;
                var foreColor = highlighted ? HighlightTextColor : TextColor;
                nameLabel.Font = font;
                nameLabel.ForeColor = foreColor;
                valueLabel.Font = font;
                valueLabel.ForeColor = foreColor;
            }

            /// <summary>
            /// Sets label text, reserving the width needed for the highlight font so highlighting a row doesn't resize the tooltip.
            /// Measurement is skipped when the text is unchanged.
            /// </summary>
            private void SetLabelText(Label label, string text)
            {
                if (label.Text == text) return;
                // A label with a maximum width wraps at it, so it is measured wrapped - see MaxNameWidth
                var maxWidth = label.MaximumSize.Width;
                if (maxWidth > 0)
                {
                    var size = TextRenderer.MeasureText(text, _highlightFont, new Size(maxWidth, int.MaxValue), TextFormatFlags.WordBreak);
                    label.MinimumSize = new Size(Math.Min(size.Width, maxWidth), size.Height);
                }
                else
                {
                    label.MinimumSize = TextRenderer.MeasureText(text, _highlightFont);
                }
                label.Text = text;
            }

            private void Layout_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
            {
                if (_highlightedIndex < 0) return;
                var headerRows = _dateLabel != null ? 1 : 0;
                if (e.Row != _highlightedIndex + headerRows) return;

                e.Graphics.FillRectangle(_highlightBrush, e.CellBounds);
                if (e.Column == 0 && _highlightedIndex < _seriesControls.Count)
                {
                    var namePanel = _seriesControls[_highlightedIndex].namePanel;
                    var colorBox = namePanel.Controls[0];
                    e.Graphics.DrawRectangle(_highlightColorBoxPen, namePanel.Left + colorBox.Left - 1, namePanel.Top + colorBox.Top - 1, colorBox.Width + 1, colorBox.Height + 1);
                }
            }

            public void SetContent(DateTime? date, string xLabel, List<(string name, string value, Color color)> series, int highlightIndex)
            {
                // Try to reuse existing controls if structure is the same (same series count, same header visibility)
                var hasHeader = date.HasValue || !string.IsNullOrEmpty(xLabel);
                bool canReuse = _layout != null &&
                               series.Count == _seriesControls.Count &&
                               (hasHeader == (_dateLabel != null));

                if (canReuse)
                {
                    // Suspend auto-sizing during update to prevent flicker from size changes
                    this.SuspendLayout();

                    // Update existing controls - much faster, no flicker
                    if (_dateLabel != null)
                    {
                        if (date.HasValue)
                        {
                            _dateLabel.Text = date.Value.ToString("G");
                        }
                        else
                        {
                            _dateLabel.Text = xLabel;
                        }
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
                        SetLabelText(nameLabel, name);

                        // Update value label
                        SetLabelText(valueLabel, value);
                    }

                    // Force layout but maintain current size to prevent flicker
                    this.ResumeLayout(false);
                    this.PerformLayout();
                    SetHighlight(highlightIndex);
                }
                else
                {
                    // Structure changed, rebuild from scratch
                    _borderPanel.SuspendLayout();
                    _borderPanel.Controls.Clear();
                    _seriesControls.Clear();
                    _highlightedIndex = -1;

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
                    _layout.CellPaint += Layout_CellPaint;

                    // Header (spans both columns): show date if available, otherwise show xLabel if provided
                    if (date.HasValue || !string.IsNullOrEmpty(xLabel))
                    {
                        _dateLabel = new Label
                        {
                            Text = date.HasValue ? date.Value.ToString("G") : xLabel,
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
                            Padding = new Padding(0),
                            BackColor = Color.Transparent
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
                            AutoSize = true,
                            // Wraps rather than running off the screen - see MaxNameWidth
                            MaximumSize = new Size(MaxNameWidth, 0),
                            Font = _normalFont,
                            ForeColor = TextColor,
                            Left = 16,
                            Top = 0,
                            Padding = new Padding(0),
                            Margin = new Padding(0),
                            BackColor = Color.Transparent
                        };
                        SetLabelText(nameLabel, name);

                        namePanel.Controls.Add(colorBox);
                        namePanel.Controls.Add(nameLabel);
                        namePanel.Width = 16 + nameLabel.Width;

                        // Right column: value (right-aligned)
                        var valueLabel = new Label
                        {
                            AutoSize = true,
                            Font = _normalFont,
                            ForeColor = TextColor,
                            TextAlign = ContentAlignment.MiddleRight,
                            Margin = new Padding(0, 2, 0, 2),
                            Padding = new Padding(0),
                            Anchor = AnchorStyles.Right,
                            BackColor = Color.Transparent
                        };
                        SetLabelText(valueLabel, value);

                        _layout.Controls.Add(namePanel, 0, _layout.RowCount);
                        _layout.Controls.Add(valueLabel, 1, _layout.RowCount);
                        _layout.RowCount++;

                        _seriesControls.Add((namePanel, valueLabel));
                    }

                    _highlightedIndex = highlightIndex;
                    ApplyRowStyle(_highlightedIndex, true);

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
            AttachTooltip(chart, valueFormatter);

            // Wire up mouse events
            chart.MouseMove -= Chart_MouseMove;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseLeave -= Chart_MouseLeave;
            chart.MouseLeave += Chart_MouseLeave;
        }

        /// <summary>
        /// Enable custom tooltips for a PieChart. This hides the built-in tooltips and shows the slice under the mouse
        /// in the same floating tooltip used by cartesian charts.
        /// </summary>
        /// <param name="chart">The chart to add custom tooltips to</param>
        public static void EnableCustomTooltips(this PieChart chart)
        {
            EnableCustomTooltips(chart, null);
        }

        /// <summary>
        /// Enable custom tooltips for a PieChart with a custom value formatter.
        /// </summary>
        /// <param name="chart">The chart to add custom tooltips to</param>
        /// <param name="valueFormatter">Optional custom formatter for tooltip values. If null, uses the series ToolTipLabelFormatter.</param>
        public static void EnableCustomTooltips(this PieChart chart, TooltipValueFormatter valueFormatter)
        {
            chart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden;
            AttachTooltip(chart, valueFormatter);

            chart.MouseMove -= PieChart_MouseMove;
            chart.MouseMove += PieChart_MouseMove;
            chart.MouseLeave -= Chart_MouseLeave;
            chart.MouseLeave += Chart_MouseLeave;
        }

        /// <summary>
        /// Creates the tooltip form and state for a chart, or updates the formatter if they already exist
        /// </summary>
        private static void AttachTooltip(Control chart, TooltipValueFormatter valueFormatter)
        {
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
                    try
                    {
                        // If the chart has been disposed while the timer was running, clean up and return
                        if (chart == null || chart.IsDisposed)
                        {
                            try { state.ShowTimer.Stop(); } catch { }
                            try { state.ShowTimer.Dispose(); } catch { }
                            _tooltipStates.Remove(chart);
                            if (_tooltipForms.TryGetValue(chart, out var tf))
                            {
                                try { tf.Hide(); tf.Dispose(); } catch { }
                                _tooltipForms.Remove(chart);
                            }
                            return;
                        }

                        // Check if enough time has passed since last mouse movement
                        var timeSinceLastMove = DateTime.Now - state.LastMouseMoveTime;

                        if (timeSinceLastMove.TotalMilliseconds >= TooltipShowDelayMs)
                        {
                            // Mouse has been still for the required delay, show tooltip
                            state.ShowTimer.Stop();

                            if (state.PendingSeriesData != null && state.PendingSeriesData.Count > 0)
                            {
                                tooltipForm.SetContent(state.PendingDate, state.PendingXLabel, state.PendingSeriesData, state.PendingHighlightIndex);
                                try
                                {
                                    PositionTooltip(chart, tooltipForm, state.PendingMouseLocation);
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Chart was disposed during positioning; fully disable custom tooltips for this chart
                                    try { DisableCustomTooltipsCore(chart); } catch { }
                                    return;
                                }
                                catch (InvalidOperationException)
                                {
                                    // In case the control handle/state is invalid, disable tooltips
                                    try { DisableCustomTooltipsCore(chart); } catch { }
                                    return;
                                }

                                state.LastTooltipPosition = tooltipForm.Location; // Track initial position
                                tooltipForm.Show();
                                state.IsTooltipVisible = true;
                            }
                        }
                        // If not enough time has passed, timer will continue and check again
                    }
                    catch
                    {
                        // Any unexpected error: ensure tooltip state is reset
                        try { ResetTooltipState(state, tooltipForm); } catch { }
                    }
                };

                _tooltipStates.Add(chart, state);

                // The tooltip is a window of its own rather than a child of the chart, so release it with the chart
                chart.Disposed -= Chart_Disposed;
                chart.Disposed += Chart_Disposed;
            }
            else
            {
                // Tooltip already exists, just update the formatter
                if (_tooltipStates.TryGetValue(chart, out var state))
                {
                    state.ValueFormatter = valueFormatter;
                }
            }
        }

        /// <summary>
        /// Disable custom tooltips for a CartesianChart and restore default behavior
        /// </summary>
        /// <param name="chart">The chart to disable custom tooltips for</param>
        public static void DisableCustomTooltips(this CartesianChart chart) => DisableCustomTooltipsCore(chart);

        /// <summary>
        /// Disable custom tooltips for a PieChart and restore default behavior
        /// </summary>
        /// <param name="chart">The chart to disable custom tooltips for</param>
        public static void DisableCustomTooltips(this PieChart chart) => DisableCustomTooltipsCore(chart);

        private static void Chart_Disposed(object sender, EventArgs e)
        {
            if (sender is not Control chart) return;
            try { ReleaseTooltip(chart); } catch (Exception ex) { Debug.WriteLine($"ChartTooltipHelper.Chart_Disposed error: {ex}"); }
        }

        private static void DisableCustomTooltipsCore(Control chart)
        {
            if (chart == null) return;

            ReleaseTooltip(chart);

            // Re-enable built-in tooltips
            if (chart is IChartView view)
            {
                view.TooltipPosition = chart is PieChart ? LiveChartsCore.Measure.TooltipPosition.Auto : LiveChartsCore.Measure.TooltipPosition.Top;
            }
        }

        /// <summary>
        /// Unwires the chart and disposes its tooltip form and timer
        /// </summary>
        private static void ReleaseTooltip(Control chart)
        {
            chart.MouseMove -= Chart_MouseMove;
            chart.MouseMove -= PieChart_MouseMove;
            chart.MouseLeave -= Chart_MouseLeave;
            chart.Disposed -= Chart_Disposed;

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
        }

        /// <summary>
        /// Pause custom tooltips for a CartesianChart without tearing down internal state.
        /// Hides any visible tooltip and stops the show-timer. Use ResumeCustomTooltips to resume.
        /// </summary>
        /// <param name="chart">The chart to pause custom tooltips for</param>
        public static void PauseCustomTooltips(this CartesianChart chart) => PauseCustomTooltipsCore(chart);

        /// <summary>
        /// Pause custom tooltips for a PieChart without tearing down internal state. Use ResumeCustomTooltips to resume.
        /// </summary>
        /// <param name="chart">The chart to pause custom tooltips for</param>
        public static void PauseCustomTooltips(this PieChart chart) => PauseCustomTooltipsCore(chart);

        private static void PauseCustomTooltipsCore(Control chart)
        {
            if (chart == null) return;

            try
            {
                _tooltipForms.TryGetValue(chart, out var tooltipForm);
                if (_tooltipStates.TryGetValue(chart, out var state))
                {
                    // Forget the point/slice under the mouse too, otherwise after resuming, moves over the same point
                    // look unchanged and the tooltip stays hidden until the mouse reaches a different one
                    try { ResetTooltipState(state, tooltipForm); } catch { }
                    state.IsPaused = true;
                }
                else
                {
                    try { tooltipForm?.Hide(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PauseCustomTooltips error: {ex}");
            }
        }

        /// <summary>
        /// Resume custom tooltips previously paused via PauseCustomTooltips. Does not force-show a tooltip;
        /// normal mouse movement will restart tooltip behavior.
        /// </summary>
        /// <param name="chart">The chart to resume custom tooltips for</param>
        public static void ResumeCustomTooltips(this CartesianChart chart) => ResumeCustomTooltipsCore(chart);

        /// <summary>
        /// Resume custom tooltips for a PieChart previously paused via PauseCustomTooltips.
        /// </summary>
        /// <param name="chart">The chart to resume custom tooltips for</param>
        public static void ResumeCustomTooltips(this PieChart chart) => ResumeCustomTooltipsCore(chart);

        private static void ResumeCustomTooltipsCore(Control chart)
        {
            if (chart == null) return;

            try
            {
                if (_tooltipStates.TryGetValue(chart, out var state))
                {
                    // Reset timers so future mouse movements behave normally
                    try { state.LastMouseMoveTime = DateTime.Now; } catch { }
                    try { state.ShowTimer?.Stop(); } catch { }
                    // Resume handling of mouse events
                    state.IsPaused = false;
                    // Do not auto-start the timer here; the mouse move handler will start it when appropriate
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResumeCustomTooltips error: {ex}");
            }
        }

        private static void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not CartesianChart chart) return;
            if (!_tooltipForms.TryGetValue(chart, out var tooltipForm)) return;
            if (!_tooltipStates.TryGetValue(chart, out var state)) return;

            // If tooltip handling is paused, ignore mouse moves entirely
            if (state.IsPaused)
            {
                return;
            }

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
                    var highlightIndex = GetHighlightIndex(chart, points, e.Location);

                    // Check if we moved to a different point
                    bool pointChanged = currentIndex != state.LastPointIndex || currentCount != state.LastPointCount;

                    if (pointChanged)
                    {
                        // Calculate time since last point change to detect slow vs fast movement
                        var timeSinceLastPointChange = now - state.LastPointChangeTime;
                        state.LastPointChangeTime = now;
                        state.LastPointIndex = currentIndex;
                        state.LastPointCount = currentCount;
                        state.LastHighlightIndex = highlightIndex;
                        state.PendingHighlightIndex = highlightIndex;

                        // Get date from first point (nullable)
                        DateTime? dateTime = null;
                        if (TryGetDateFromPoint(firstPoint, out var dt))
                        {
                            dateTime = dt;
                        }

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

                        // Compute an X label to show in header: prefer DateTime if available, otherwise use point.Coordinate.SecondaryValue or Primary as fallback
                        string xLabel = null;
                        if (dateTime.HasValue)
                        {
                            xLabel = dateTime.Value.ToString("G");
                        }
                        else
                        {
                            try
                            {
                                // Prefer using axis Labels (category labels) if available on the chart X axis
                                var coord = firstPoint.Coordinate;
                                var secondary = coord.SecondaryValue;

                                if (chart != null && chart.XAxes != null && chart.XAxes.Any())
                                {
                                    var xAxis = chart.XAxes.First();
                                    // If axis has string labels, map the numeric index to the label
                                    if (xAxis.Labels != null && xAxis.Labels.Count > 0 && !double.IsNaN(secondary))
                                    {
                                        var idx = (int)Math.Round(secondary);
                                        if (idx >= 0 && idx < xAxis.Labels.Count)
                                        {
                                            xLabel = xAxis.Labels[idx];
                                        }
                                    }
                                }

                                // Fallback to coordinate values if no axis labels mapped
                                if (xLabel == null)
                                {
                                    var primary = coord.PrimaryValue;
                                    // Prefer secondary (X) value when available
                                    if (!double.IsNaN(secondary))
                                    {
                                        // Try to interpret numeric secondary value as ticks-based DateTime
                                        if (TryFormatNumericXAsDate(secondary, out var secDateLabel))
                                        {
                                            xLabel = secDateLabel;
                                        }
                                        else
                                        {
                                            xLabel = secondary.ToString();
                                        }
                                    }
                                    else
                                    {
                                        // If primary looks like a DateTime ticks value, convert to readable date
                                        try
                                        {
                                            if (!double.IsNaN(primary) && TryFormatNumericXAsDate(primary, out var primDateLabel))
                                            {
                                                xLabel = primDateLabel;
                                            }
                                            else
                                            {
                                                xLabel = primary.ToString();
                                            }
                                        }
                                        catch
                                        {
                                            xLabel = primary.ToString();
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        // If tooltip is already visible
                        if (state.IsTooltipVisible)
                        {
                            // Tooltip already showing: keep it visible and update immediately
                            // User has already indicated they want to see tooltips
                            tooltipForm.SetContent(dateTime, xLabel, seriesData, highlightIndex);
                            state.PendingDate = dateTime;
                            state.PendingXLabel = xLabel;
                            state.PendingSeriesData = seriesData;
                            state.PendingMouseLocation = e.Location;
                        }
                        else
                        {
                            // Tooltip not visible: store pending data and start/continue timer
                            state.PendingDate = dateTime;
                            state.PendingXLabel = xLabel;
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

                        // Moving within the same stack (e.g. up/down a stacked column) only changes the highlighted row
                        if (highlightIndex != state.LastHighlightIndex)
                        {
                            state.LastHighlightIndex = highlightIndex;
                            state.PendingHighlightIndex = highlightIndex;
                            if (state.IsTooltipVisible)
                            {
                                tooltipForm.SetHighlight(highlightIndex);
                            }
                        }
                    }

                    FollowMouse(chart, tooltipForm, state, e.Location);
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
                            state.LastHighlightIndex = -1;
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
            state.LastHighlightIndex = -1;
            state.LastSeries = null;
            tooltipForm?.Hide();
        }

        /// <summary>
        /// Always update position if tooltip is visible, but only if position actually changed
        /// </summary>
        private static void FollowMouse(Control chart, TooltipForm tooltipForm, TooltipState state, Point mouseLocation)
        {
            if (!state.IsTooltipVisible || !tooltipForm.Visible) return;

            var screenPt = chart.PointToScreen(mouseLocation);
            var newTooltipPos = new Point(screenPt.X + TooltipOffsetX, screenPt.Y + TooltipOffsetY);

            // Only update if moved by more than a few pixels to reduce flicker
            if (Math.Abs(newTooltipPos.X - state.LastTooltipPosition.X) > 3 ||
                Math.Abs(newTooltipPos.Y - state.LastTooltipPosition.Y) > 3)
            {
                PositionTooltip(chart, tooltipForm, mouseLocation);
                state.LastTooltipPosition = tooltipForm.Location;
            }
        }

        /// <summary>
        /// Lists every slice of the pie with the one under the mouse highlighted.
        /// The rows are built when the mouse enters the pie so they reflect the current data - moving between slices only
        /// moves the highlight and other moves just reposition the tooltip.
        /// LiveCharts hit tests slices against their drawn shape, so the tolerance search used for cartesian points isn't needed.
        /// </summary>
        private static void PieChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not PieChart chart) return;
            if (!_tooltipForms.TryGetValue(chart, out var tooltipForm)) return;
            if (!_tooltipStates.TryGetValue(chart, out var state)) return;
            if (state.IsPaused) return;

            try
            {
                // Ignore tiny jitters so they don't reset the tooltip delay timer
                if (state.LastMousePosition == Point.Empty ||
                    Math.Abs(e.X - state.LastMousePosition.X) > MouseJitterThreshold ||
                    Math.Abs(e.Y - state.LastMousePosition.Y) > MouseJitterThreshold)
                {
                    state.LastMouseMoveTime = DateTime.Now;
                    state.LastMousePosition = e.Location;
                }

                LiveChartsCore.Kernel.ChartPoint point = null;
                foreach (var p in chart.GetPointsAt(new LiveChartsCore.Drawing.LvcPointD(e.X, e.Y)))
                {
                    point = p;
                    break;
                }

                if (point == null)
                {
                    // Off the pie (legend, donut hole, margins)
                    if (state.LastSeries != null || state.ShowTimer.Enabled || tooltipForm.Visible)
                    {
                        ResetTooltipState(state, tooltipForm);
                    }
                    return;
                }

                state.PendingMouseLocation = e.Location;

                var series = point.Context.Series;
                if (!ReferenceEquals(series, state.LastSeries))
                {
                    var entering = state.LastSeries == null;
                    state.LastSeries = series;

                    if (entering || state.PieSliceRows == null || !state.PieSliceRows.ContainsKey(series))
                    {
                        var seriesData = GetPieSliceRows(chart, state);
                        var highlightIndex = GetPieHighlightIndex(state, series);
                        state.PendingDate = null;
                        state.PendingXLabel = null;
                        state.PendingSeriesData = seriesData;
                        state.PendingHighlightIndex = highlightIndex;

                        if (state.IsTooltipVisible)
                        {
                            tooltipForm.SetContent(null, null, seriesData, highlightIndex);
                        }
                        else if (!state.ShowTimer.Enabled)
                        {
                            state.ShowTimer.Start();
                        }
                    }
                    else
                    {
                        // Same slices - just move the highlight
                        state.PendingHighlightIndex = GetPieHighlightIndex(state, series);
                        if (state.IsTooltipVisible)
                        {
                            tooltipForm.SetHighlight(state.PendingHighlightIndex);
                        }
                    }
                }

                FollowMouse(chart, tooltipForm, state, e.Location);
            }
            catch
            {
                // Suppress any errors during tooltip display
                ResetTooltipState(state, tooltipForm);
            }
        }

        /// <summary>
        /// Builds a tooltip row for each visible slice in series order (the legend order) and records each slice's row
        /// </summary>
        private static List<(string name, string value, Color color)> GetPieSliceRows(PieChart chart, TooltipState state)
        {
            var rows = new List<(string name, string value, Color color)>();
            var rowIndexes = new Dictionary<ISeries, int>(ReferenceEqualityComparer.Instance);
            if (chart.Series != null)
            {
                foreach (var series in chart.Series)
                {
                    if (!series.IsVisible) continue;

                    LiveChartsCore.Kernel.ChartPoint point = null;
                    foreach (var p in series.Fetch(chart.CoreChart))
                    {
                        point = p;
                        break;
                    }
                    if (point == null) continue;

                    var value = state.ValueFormatter != null ? state.ValueFormatter(point) : series.GetPrimaryToolTipText(point);
                    if (string.IsNullOrEmpty(value))
                    {
                        value = point.Coordinate.PrimaryValue.ToString("N2");
                    }

                    rowIndexes[series] = rows.Count;
                    rows.Add((series.Name ?? "Value", value, GetSeriesColor(series)));
                }
            }
            state.PieSliceRows = rowIndexes;
            return rows;
        }

        /// <summary>
        /// Row to highlight for the slice under the mouse. Like cartesian charts, a single row isn't highlighted.
        /// </summary>
        private static int GetPieHighlightIndex(TooltipState state, ISeries series)
        {
            if (state.PieSliceRows == null || state.PieSliceRows.Count < 2) return -1;
            return state.PieSliceRows.TryGetValue(series, out var index) ? index : -1;
        }

        /// <summary>
        /// When the tooltip lists multiple points (e.g. all series at the same X), returns the index of the point that
        /// the mouse is visually over so it can be highlighted. Returns -1 if there is nothing to highlight.
        /// - Bars (e.g. stacked column segments): the drawn rectangle contains the mouse.
        /// - Stacked areas: the mouse is inside the series' fill. Fills extend down to the pivot and lower layers are drawn
        ///   on top, so this is the first series in stack order whose curve is above the mouse.
        /// - Lines/scatter: the nearest line or point within PointDetectionTolerance. Where series overlap, the one drawn on top wins.
        /// Lines and areas are evaluated on the same smoothed curve LiveCharts draws (see EvaluateSpline).
        /// Work is a few arithmetic operations per series - no allocations and no iteration over the series data.
        /// </summary>
        private static int GetHighlightIndex(CartesianChart chart, List<LiveChartsCore.Kernel.ChartPoint> points, Point mouseLocation)
        {
            if (points.Count < 2) return -1;
            try
            {
                var pixel = new LiveChartsCore.Drawing.LvcPoint(mouseLocation.X, mouseLocation.Y);
                var mousePixel = new LiveChartsCore.Drawing.LvcPointD(mouseLocation.X, mouseLocation.Y);

                var bestIndex = -1;
                var bestDistance = PointDetectionTolerance;
                var bestZIndex = int.MinValue;

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    var series = point.Context.Series;
                    var properties = series.SeriesProperties;

                    if (properties.HasFlag(SeriesProperties.Bar))
                    {
                        if (point.Context.HoverArea?.IsPointerOver(pixel, FindingStrategy.CompareAll) == true) return i;
                        continue;
                    }

                    if (series is not ICartesianSeries cartesian) continue;
                    var coordinate = point.Coordinate;
                    if (coordinate.IsEmpty) continue;

                    var mouseData = chart.ScalePixelsToData(mousePixel, cartesian.ScalesXAt, cartesian.ScalesYAt);

                    double distance;
                    if (properties.HasFlag(SeriesProperties.Scatter))
                    {
                        var pointPixel = chart.ScaleDataToPixels(new LiveChartsCore.Drawing.LvcPointD(coordinate.SecondaryValue, coordinate.PrimaryValue), cartesian.ScalesXAt, cartesian.ScalesYAt);
                        var px = pointPixel.X - mouseLocation.X;
                        var py = pointPixel.Y - mouseLocation.Y;
                        distance = Math.Sqrt(px * px + py * py);
                    }
                    else
                    {
                        var isStacked = properties.HasFlag(SeriesProperties.Stacked);
                        double curveY;
                        var stack = default(SplineStack);
                        if (TryGetSplineSegment(series, point.Index, coordinate.SecondaryValue, mouseData.X, out var segment) &&
                            (!isStacked || TryGetStackStarts(chart, series, segment, out stack)))
                        {
                            var smoothness = series is ILineSeries lineSeries ? lineSeries.LineSmoothness : 0; // 0 = straight line
                            curveY = series is IStepLineSeries
                                ? segment.V1 + stack.S1 // Step lines hold the value until the next point
                                : EvaluateSpline(segment, stack, smoothness, mouseData.X);
                        }
                        else
                        {
                            // Outside the data range, a gap or values we can't read (e.g. a Mapping): use the point itself
                            curveY = isStacked ? point.StackedValue?.CumulativeEnd ?? coordinate.PrimaryValue : coordinate.PrimaryValue;
                        }

                        if (isStacked)
                        {
                            var pivot = series.Pivot;
                            if (mouseData.Y >= Math.Min(pivot, curveY) && mouseData.Y <= Math.Max(pivot, curveY)) return i;
                            continue;
                        }

                        var curvePixel = chart.ScaleDataToPixels(new LiveChartsCore.Drawing.LvcPointD(mouseData.X, curveY), cartesian.ScalesXAt, cartesian.ScalesYAt);
                        distance = Math.Abs(curvePixel.Y - mouseLocation.Y);
                    }

                    if (distance > PointDetectionTolerance) continue;

                    // Overlapping series (within a pixel): prefer the one drawn on top - higher ZIndex, then later in the series collection
                    if (bestIndex < 0 || distance < bestDistance - 1 || (distance <= bestDistance + 1 && series.ZIndex >= bestZIndex))
                    {
                        bestIndex = i;
                        bestDistance = distance;
                        bestZIndex = series.ZIndex;
                    }
                }

                return bestIndex;
            }
            catch
            {
                // Axes may not be measured yet - just don't highlight
                return -1;
            }
        }

        /// <summary>
        /// X and raw Y values of the 4 points LiveCharts uses to draw the curve between point 1 and point 2:
        /// the previous point (0), the segment start (1), the segment end (2) and the point after (3). Clamped at the ends of the series.
        /// </summary>
        private readonly struct SplineSegment(int i0, int i1, int i2, int i3, double x0, double x1, double x2, double x3, double v0, double v1, double v2, double v3)
        {
            public readonly int I0 = i0, I1 = i1, I2 = i2, I3 = i3;
            public readonly double X0 = x0, X1 = x1, X2 = x2, X3 = x3;
            public readonly double V0 = v0, V1 = v1, V2 = v2, V3 = v3;
        }

        /// <summary>
        /// Cumulative stack start (sum of the series below) at each of the 4 spline points. Default is an unstacked series.
        /// </summary>
        private struct SplineStack
        {
            public double S0, S1, S2, S3;
        }

        /// <summary>
        /// Gets the stack start at each of the segment's X values from the series below this one in its stack.
        /// Matches LiveCharts' Stacker: a stack is the visible series with the same SeriesProperties and stack group, in series
        /// order, and values are stacked by X value rather than by position - a lower series with no value at an X adds nothing there.
        /// Series built from grouped data can have different X values, so each X is looked up in each lower series.
        /// The lower series come from chart.Series rather than the tooltip points, which can omit a sparse series with no point near the mouse.
        /// </summary>
        private static bool TryGetStackStarts(CartesianChart chart, ISeries series, in SplineSegment segment, out SplineStack stack)
        {
            stack = default;
            var properties = series.SeriesProperties;
            var stackGroup = series.GetStackGroup();
            foreach (var lower in chart.Series)
            {
                if (ReferenceEquals(lower, series)) return true;
                if (!lower.IsVisible || lower.SeriesProperties != properties || lower.GetStackGroup() != stackGroup) continue;
                if (lower.Values is not System.Collections.IList values) return false;

                if (!TryGetValueAtX(values, segment.X0, segment.I0, out var v0) ||
                    !TryGetValueAtX(values, segment.X1, segment.I1, out var v1) ||
                    !TryGetValueAtX(values, segment.X2, segment.I2, out var v2) ||
                    !TryGetValueAtX(values, segment.X3, segment.I3, out var v3))
                {
                    return false;
                }
                stack.S0 += v0;
                stack.S1 += v1;
                stack.S2 += v2;
                stack.S3 += v3;
            }
            return false; // Series not found in the chart
        }

        /// <summary>
        /// Gets the value at exactly x (0 if the series has no value there, as LiveCharts' stacker does).
        /// Checks the hint index first - series usually share X values so this is the normal case - then binary searches,
        /// relying on values being sorted by X (as ChartHelper builds them). Returns false if the values can't be read.
        /// </summary>
        private static bool TryGetValueAtX(System.Collections.IList values, double x, int hint, out double value)
        {
            value = 0;
            if (hint >= 0 && hint < values.Count && values[hint] is IChartEntity hintEntity && hintEntity.Coordinate.SecondaryValue == x)
            {
                value = hintEntity.Coordinate.IsEmpty ? 0 : hintEntity.Coordinate.PrimaryValue;
                return true;
            }

            int lo = 0, hi = values.Count - 1;
            while (lo <= hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (values[mid] is not IChartEntity entity || entity.Coordinate.IsEmpty) return false; // Can't compare - give up
                var midX = entity.Coordinate.SecondaryValue;
                if (midX == x)
                {
                    value = entity.Coordinate.PrimaryValue;
                    return true;
                }
                if (midX < x) lo = mid + 1; else hi = mid - 1;
            }
            return true; // No value at this X
        }

        /// <summary>
        /// Gets the 4 spline points for the segment of the series containing mouseX.
        /// Only works for values that are chart entities (DateTimePoint, ObservablePoint etc.) - series using a Mapping return false.
        /// Returns false outside the data range or next to a gap (null value), where LiveCharts splits the line.
        /// </summary>
        private static bool TryGetSplineSegment(ISeries series, int pointIndex, double pointX, double mouseX, out SplineSegment segment)
        {
            segment = default;
            if (series.Values is not System.Collections.IList values) return false;

            var start = mouseX >= pointX ? pointIndex : pointIndex - 1;
            if (start < 0 || start + 1 >= values.Count) return false;

            var i0 = Math.Max(start - 1, 0);
            var i3 = Math.Min(start + 2, values.Count - 1);
            if (!TryGetCoordinate(values, i0, out var c0) ||
                !TryGetCoordinate(values, start, out var c1) ||
                !TryGetCoordinate(values, start + 1, out var c2) ||
                !TryGetCoordinate(values, i3, out var c3))
            {
                return false;
            }

            segment = new SplineSegment(i0, start, start + 1, i3,
                c0.SecondaryValue, c1.SecondaryValue, c2.SecondaryValue, c3.SecondaryValue,
                c0.PrimaryValue, c1.PrimaryValue, c2.PrimaryValue, c3.PrimaryValue);
            return true;
        }

        private static bool TryGetCoordinate(System.Collections.IList values, int index, out Coordinate coordinate)
        {
            if (values[index] is IChartEntity entity && !entity.Coordinate.IsEmpty)
            {
                coordinate = entity.Coordinate;
                return true;
            }
            coordinate = Coordinate.Empty;
            return false;
        }

        /// <summary>
        /// Returns the Y value (in data units, including the stack offset) of the drawn curve at x.
        /// Replicates CoreLineSeries.GetSpline from LiveCharts2 2.0.5 so the hit test matches what is drawn - including the
        /// "- previous + stack" sign in its length terms. Revisit if LiveCharts changes how splines are built.
        /// The curve is a cubic Bezier from point 1 to point 2. X is monotonic along it, so t is found by bisection.
        /// </summary>
        private static double EvaluateSpline(in SplineSegment g, in SplineStack s, double smoothness, double x)
        {
            var y1 = g.V1 + s.S1;
            var y2 = g.V2 + s.S2;

            var xc1 = (g.X0 + g.X1) / 2.0;
            var yc1 = (g.V0 + s.S0 + g.V1 + s.S1) / 2.0;
            var xc2 = (g.X1 + g.X2) / 2.0;
            var yc2 = (g.V1 + s.S1 + g.V2 + s.S2) / 2.0;
            var xc3 = (g.X2 + g.X3) / 2.0;
            var yc3 = (g.V2 + s.S2 + g.V3 + s.S3) / 2.0;

            var d1 = g.V1 + s.S1 - g.V0 + s.S0;
            var d2 = g.V2 + s.S2 - g.V1 + s.S1;
            var d3 = g.V3 + s.S3 - g.V2 + s.S2;
            var len1 = (float)Math.Sqrt((g.X1 - g.X0) * (g.X1 - g.X0) + d1 * d1);
            var len2 = (float)Math.Sqrt((g.X2 - g.X1) * (g.X2 - g.X1) + d2 * d2);
            var len3 = (float)Math.Sqrt((g.X3 - g.X2) * (g.X3 - g.X2) + d3 * d3);

            var k1 = len1 / (len1 + len2);
            var k2 = len2 / (len2 + len3);
            if (float.IsNaN(k1)) k1 = 0f;
            if (float.IsNaN(k2)) k2 = 0f;

            var xm1 = xc1 + (xc2 - xc1) * k1;
            var ym1 = yc1 + (yc2 - yc1) * k1;
            var xm2 = xc2 + (xc3 - xc2) * k2;
            var ym2 = yc2 + (yc3 - yc2) * k2;

            var c1X = xm1 + (xc2 - xm1) * smoothness + g.X1 - xm1;
            var c1Y = ym1 + (yc2 - ym1) * smoothness + y1 - ym1;
            var c2X = xm2 + (xc2 - xm2) * smoothness + g.X2 - xm2;
            var c2Y = ym2 + (yc2 - ym2) * smoothness + y2 - ym2;

            // Find t where the curve's X equals x (16 iterations = 1/65536 of the segment width)
            double lo = 0, hi = 1, t = 0.5;
            for (int i = 0; i < 16; i++)
            {
                t = (lo + hi) / 2;
                if (CubicBezier(g.X1, c1X, c2X, g.X2, t) < x) lo = t; else hi = t;
            }

            return CubicBezier(y1, c1Y, c2Y, y2, t);
        }

        private static double CubicBezier(double p0, double p1, double p2, double p3, double t)
        {
            var u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        private static void PositionTooltip(Control chart,TooltipForm tooltipForm, Point mouseLocation)
        {
            try
            {
                if (chart == null || chart.IsDisposed || chart.Disposing || !chart.IsHandleCreated)
                {
                    try { tooltipForm.Hide(); } catch { }
                    return;
                }

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
            catch (ObjectDisposedException)
            {
                try { tooltipForm.Hide(); } catch { }
            }
            catch (InvalidOperationException)
            {
                try { tooltipForm.Hide(); } catch { }
            }
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
                    ColumnSeries<DateTimePoint> columnSeries => columnSeries.Values as IList<DateTimePoint>,
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

        /// <summary>
        /// Try to format a numeric coordinate value as a DateTime string when it plausibly represents ticks.
        /// Uses a conservative lower bound (1970-01-01) to avoid interpreting small integers (0..N) as year 0001.
        /// </summary>
        private static bool TryFormatNumericXAsDate(double numericValue, out string formatted)
        {
            formatted = null;
            try
            {
                if (double.IsNaN(numericValue)) return false;
                var longTicks = (long)Math.Round(numericValue);
                var minPlausibleDateTicks = new DateTime(1970, 1, 1).Ticks;
                if (longTicks >= minPlausibleDateTicks && longTicks < DateTime.MaxValue.Ticks)
                {
                    formatted = new DateTime(longTicks).ToString("G");
                    return true;
                }
            }
            catch
            {
                // ignore
            }
            return false;
        }

        private static Color GetSeriesColor(ISeries series)
        {
            // Try pattern matching first for common series types
            try
            {
                SolidColorPaint paint = series switch
                {
                    LineSeries<DateTimePoint> ls => ls.Stroke as SolidColorPaint ?? ls.Fill as SolidColorPaint,
                    StackedAreaSeries<DateTimePoint> sas => sas.Stroke as SolidColorPaint ?? sas.Fill as SolidColorPaint,
                    StackedColumnSeries<DateTimePoint> scs => scs.Stroke as SolidColorPaint ?? scs.Fill as SolidColorPaint,
                    ColumnSeries<DateTimePoint> csd => csd.Fill as SolidColorPaint ?? csd.Stroke as SolidColorPaint,
                    ScatterSeries<DateTimePoint> ss => ss.Stroke as SolidColorPaint ?? ss.Fill as SolidColorPaint,
                    LineSeries<ObservablePoint> lso => lso.Stroke as SolidColorPaint ?? lso.Fill as SolidColorPaint,
                    StackedAreaSeries<ObservablePoint> saso => saso.Stroke as SolidColorPaint ?? saso.Fill as SolidColorPaint,
                    StackedColumnSeries<ObservablePoint> scso => scso.Stroke as SolidColorPaint ?? scso.Fill as SolidColorPaint,
                    ColumnSeries<ObservablePoint> cso => cso.Fill as SolidColorPaint ?? cso.Stroke as SolidColorPaint,
                    ScatterSeries<ObservablePoint> sso => sso.Stroke as SolidColorPaint ?? sso.Fill as SolidColorPaint,
                    // Pie slices are drawn with Fill - Stroke is the border between slices
                    PieSeries<ObservableValue> pso => pso.Fill as SolidColorPaint ?? pso.Stroke as SolidColorPaint,
                    PieSeries<double> psd => psd.Fill as SolidColorPaint ?? psd.Stroke as SolidColorPaint,
                    _ => null
                };

                if (paint?.Color is SKColor skColor)
                {
                    return Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);
                }
            }
            catch
            {
                // ignore and try reflection fallback
            }

            // Reflection fallback: try to read Stroke or Fill properties for other series types
            try
            {
                var seriesType = series.GetType();
                var strokeProp = seriesType.GetProperty("Stroke");
                var fillProp = seriesType.GetProperty("Fill");

                var strokeVal = strokeProp?.GetValue(series) as SolidColorPaint;
                var fillVal = fillProp?.GetValue(series) as SolidColorPaint;

                var paintRef = series is IPieSeries ? fillVal ?? strokeVal : strokeVal ?? fillVal;
                if (paintRef?.Color is SKColor skColor2)
                {
                    return Color.FromArgb(skColor2.Alpha, skColor2.Red, skColor2.Green, skColor2.Blue);
                }
            }
            catch
            {
                // ignore and fall through to default
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
                    ColumnSeries<DateTimePoint> csd => csd.ScalesYAt,
                    ScatterSeries<DateTimePoint> ss => ss.ScalesYAt,
                    LineSeries<ObservablePoint> lso => lso.ScalesYAt,
                    StackedAreaSeries<ObservablePoint> saso => saso.ScalesYAt,
                    StackedColumnSeries<ObservablePoint> scso => scso.ScalesYAt,
                    ColumnSeries<ObservablePoint> csf => csf.ScalesYAt,
                    ScatterSeries<ObservablePoint> sso => sso.ScalesYAt,
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
            if (sender is not Control chart) return;

            if (_tooltipStates.TryGetValue(chart, out var state))
            {
                state.ShowTimer.Stop();
                state.PendingSeriesData = null;
                state.IsTooltipVisible = false;
                state.LastPointIndex = -1;
                state.LastPointCount = 0;
                state.LastHighlightIndex = -1;
                state.LastSeries = null;
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
