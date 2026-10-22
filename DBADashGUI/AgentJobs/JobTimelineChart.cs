using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Agent job timeline drawn with SkiaSharp.
    ///
    /// Rows scroll vertically under a fixed time axis and a fixed column of job names.  Time zooms
    /// around the pointer (Ctrl+wheel) and pans by dragging; dragging across the time axis zooms to
    /// the range dragged.  An overview strip along the bottom shows the whole period with the part
    /// in view, and can be dragged.  Hovering a bar shows its details, clicking selects it (and
    /// highlights the rest of that job), double clicking opens the job.  With steps included each
    /// job is a group that can be collapsed.  The legend counts executions by status, and clicking a
    /// status hides or shows it.
    ///
    /// Drawing is in <c>JobTimelineChart.Render.cs</c>; this file holds the view state and input.
    /// </summary>
    [DesignerCategory("")]
    internal sealed partial class JobTimelineChart : UserControl
    {
        private readonly SKControl _canvas;

        private JobTimelineModel _model = JobTimelineModel.Empty;
        private List<TimelineRowModel> _rows = new();
        private float _contentHeight;

        private DateTime _periodFrom = DateTime.Now.AddHours(-1);
        private DateTime _periodTo = DateTime.Now;
        private DateTime _viewFrom;
        private DateTime _viewTo;
        private float _scrollY;

        private string _filter = string.Empty;
        private TimelineSort _sort = TimelineSort.Name;
        private readonly HashSet<RunStatus> _hiddenStatuses = new();

        private TimelineBar _hoverBar;
        private TimelineRowModel _hoverRow;
        private TimelineBar _selectedBar;
        private Point _mouse = new(-1, -1);
        private bool _mouseInside;

        private enum DragMode
        {
            None,
            Pan,
            ZoomRange,
            Overview,
            ScrollThumb,
            LabelSplitter
        }

        private DragMode _drag = DragMode.None;
        private Point _dragStart;
        private DateTime _dragViewFrom;
        private DateTime _dragViewTo;
        private float _dragScrollY;
        private float _dragLabelWidth;
        private bool _dragMoved;

        private float _labelWidthUnscaled = 260;

        private static readonly TimeSpan MinimumSpan = TimeSpan.FromSeconds(10);

        public JobTimelineChart()
        {
            _canvas = new SKControl { Dock = DockStyle.Fill, TabStop = true };
            _canvas.PaintSurface += Canvas_PaintSurface;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseLeave += Canvas_MouseLeave;
            _canvas.MouseWheel += Canvas_MouseWheel;
            _canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            _canvas.KeyDown += Canvas_KeyDown;
            _canvas.PreviewKeyDown += (_, e) => e.IsInputKey = e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down;
            _canvas.Resize += (_, _) =>
            {
                ClampScroll();
                _canvas.Invalidate();
            };
            // Focus on enter so the wheel and keyboard work without clicking first - but only while this window is the
            // active one, so a window opened from here isn't pulled behind it when the pointer crosses the chart.
            _canvas.MouseEnter += (_, _) =>
            {
                _mouseInside = true;
                if (ReferenceEquals(Form.ActiveForm, FindForm())) _canvas.Focus();
            };
            Controls.Add(_canvas);
            InitFonts();
        }

        /// <summary>Raised when a bar is double clicked or "Open job" is picked from its menu.</summary>
        public event EventHandler<TimelineBar> BarActivated;

        /// <summary>Shown in the top left corner, above the job names.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Text drawn in place of the rows when there is nothing to show.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EmptyText { get; set; } = "No job executions in the selected period";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value ?? string.Empty;
                Relayout();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimelineSort Sort
        {
            get => _sort;
            set
            {
                _sort = value;
                Relayout();
            }
        }

        private float Dpi => DeviceDpi / 96f;

        /// <summary>
        /// Show a new set of data.  The view keeps its zoom when the period is unchanged (a refresh), and is reset
        /// to the whole period otherwise.  Collapsed groups stay collapsed.
        /// </summary>
        public void SetData(JobTimelineModel model, DateTime periodFrom, DateTime periodTo)
        {
            var collapsed = _model.Roots.Where(r => r.Collapsed).Select(r => r.JobName).ToHashSet();
            var selected = _selectedBar;
            var samePeriod = periodFrom == _periodFrom && periodTo == _periodTo && _viewTo > _viewFrom;

            _model = model ?? JobTimelineModel.Empty;
            foreach (var root in _model.Roots.Where(r => r.IsGroup && collapsed.Contains(r.JobName)))
            {
                root.Collapsed = true;
            }
            _periodFrom = periodFrom;
            _periodTo = periodTo > periodFrom ? periodTo : periodFrom.Add(MinimumSpan);
            if (!samePeriod)
            {
                _viewFrom = _periodFrom;
                _viewTo = _periodTo;
                _scrollY = 0;
            }
            _selectedBar = selected == null ? null : _model.Bars.FirstOrDefault(b => b.JobName == selected.JobName && b.StepId == selected.StepId && b.Start == selected.Start);
            _hoverBar = null;
            _hoverRow = null;
            Relayout();
        }

        /// <summary>Rebuild the visible row list and their positions after anything that changes which rows show.</summary>
        private void Relayout()
        {
            _rows = _model.VisibleRows(_filter, IsVisible, _sort, _hiddenStatuses.Count > 0);
            float y = 0;
            foreach (var row in _rows)
            {
                row.Top = y;
                row.Height = row.LaneCount * LaneHeight + RowPadding * 2;
                y += row.Height;
            }
            _contentHeight = y;
            if (_hoverBar != null && !IsVisible(_hoverBar)) _hoverBar = null;
            ClampScroll();
            _canvas.Invalidate();
        }

        private bool IsVisible(TimelineBar bar) => !_hiddenStatuses.Contains(bar.Status);

        #region Geometry

        private float LaneHeight => 20 * Dpi;
        private float RowPadding => 4 * Dpi;
        private float HeaderHeight => 46 * Dpi;
        private float OverviewHeight => 28 * Dpi;
        private float ScrollBarWidth => 10 * Dpi;
        private float LabelWidth => Math.Clamp(_labelWidthUnscaled * Dpi, 80 * Dpi, Math.Max(80 * Dpi, _canvas.Width * 0.6f));

        private float ChartLeft => LabelWidth;
        private float ChartRight => _canvas.Width - ScrollBarWidth;
        private float ChartWidth => Math.Max(1, ChartRight - ChartLeft);
        private float RowsTop => HeaderHeight;
        private float RowsBottom => CanvasHeight - OverviewHeight;
        private float RowsHeight => Math.Max(0, RowsBottom - RowsTop);

        private float MaxScroll => Math.Max(0, _contentHeight - RowsHeight);

        private void ClampScroll()
        {
            _scrollY = Math.Clamp(_scrollY, 0, MaxScroll);
            RefreshHover();
        }

        /// <summary>The view moved under a still pointer (keys, overview, zoom) - pick up whatever is under it now.</summary>
        private void RefreshHover()
        {
            if (!_mouseInside || _drag != DragMode.None) return;
            _hoverBar = BarAt(_mouse);
            _hoverRow = RowAt(_mouse.Y);
        }

        private double ViewTicks => Math.Max(1, (_viewTo - _viewFrom).Ticks);

        private float TimeToX(DateTime t) => ChartLeft + (float)((t - _viewFrom).Ticks / ViewTicks * ChartWidth);

        private DateTime XToTime(float x)
        {
            var ticks = (long)((x - ChartLeft) / ChartWidth * ViewTicks);
            return ClampTime(_viewFrom.AddTicks(ticks));
        }

        private DateTime ClampTime(DateTime t) => t < _periodFrom ? _periodFrom : t > _periodTo ? _periodTo : t;

        private float RowScreenTop(TimelineRowModel row) => RowsTop + row.Top - _scrollY;

        private SKRect BarRect(TimelineBar bar)
        {
            var row = bar.Row;
            var top = RowScreenTop(row) + RowPadding + bar.Lane * LaneHeight + 2 * Dpi;
            var left = TimeToX(bar.Start < _periodFrom ? _periodFrom : bar.Start);
            var right = TimeToX(bar.End > _periodTo ? _periodTo : bar.End);
            if (right - left < 3 * Dpi) right = left + 3 * Dpi;
            return new SKRect(left, top, right, top + LaneHeight - 4 * Dpi);
        }

        private SKRect OverviewRect => new(ChartLeft, RowsBottom + 4 * Dpi, ChartRight, CanvasHeight - 4 * Dpi);

        private double PeriodTicks => Math.Max(1, (_periodTo - _periodFrom).Ticks);

        private float PeriodTimeToX(DateTime t, SKRect strip) => strip.Left + (float)((t - _periodFrom).Ticks / PeriodTicks * strip.Width);

        private SKRect ScrollThumbRect
        {
            get
            {
                if (_contentHeight <= RowsHeight || RowsHeight <= 0) return SKRect.Empty;
                var thumbHeight = Math.Max(24 * Dpi, RowsHeight * RowsHeight / _contentHeight);
                var top = RowsTop + (RowsHeight - thumbHeight) * (_scrollY / MaxScroll);
                return new SKRect(ChartRight + 2 * Dpi, top, _canvas.Width - 2 * Dpi, top + thumbHeight);
            }
        }

        private TimelineRowModel RowAt(float y)
        {
            if (y < RowsTop || y >= RowsBottom) return null;
            var contentY = y - RowsTop + _scrollY;
            foreach (var row in _rows)
            {
                if (contentY >= row.Top && contentY < row.Top + row.Height) return row;
            }
            return null;
        }

        private TimelineBar BarAt(Point p)
        {
            if (p.X < ChartLeft || p.X > ChartRight) return null;
            var row = RowAt(p.Y);
            if (row == null) return null;
            // Topmost first, and allow a little slack either side so very short runs can still be picked.
            var slack = 2 * Dpi;
            return row.Bars.Where(IsVisible).LastOrDefault(b =>
            {
                var r = BarRect(b);
                return p.Y >= r.Top && p.Y <= r.Bottom && p.X >= r.Left - slack && p.X <= r.Right + slack;
            });
        }

        private bool IsOnSplitter(Point p) => p.Y >= RowsTop && Math.Abs(p.X - LabelWidth) <= 4 * Dpi;

        private bool IsOnChevron(TimelineRowModel row, Point p) =>
            row is { IsGroup: true } && p.X < LabelWidth && p.X <= (8 + 18) * Dpi;

        #endregion Geometry

        #region View changes

        private void SetView(DateTime from, DateTime to)
        {
            var span = to - from;
            var periodSpan = _periodTo - _periodFrom;
            if (span < MinimumSpan) span = MinimumSpan < periodSpan ? MinimumSpan : periodSpan;
            if (span > periodSpan) span = periodSpan;
            if (from < _periodFrom) from = _periodFrom;
            if (from + span > _periodTo) from = _periodTo - span;
            _viewFrom = from;
            _viewTo = from + span;
            RefreshHover();
            _canvas.Invalidate();
        }

        /// <summary>Zoom by <paramref name="factor"/> (below 1 zooms in) keeping the time under <paramref name="x"/> still.</summary>
        private void ZoomAt(float x, double factor)
        {
            var anchor = XToTime(x);
            var fraction = (x - ChartLeft) / ChartWidth;
            var span = TimeSpan.FromTicks((long)(ViewTicks * factor));
            var from = anchor - TimeSpan.FromTicks((long)(span.Ticks * fraction));
            SetView(from, from + span);
        }

        private void PanBy(TimeSpan delta) => SetView(_viewFrom + delta, _viewTo + delta);

        public void ResetZoom() => SetView(_periodFrom, _periodTo);

        private void ZoomToBar(TimelineBar bar)
        {
            var span = bar.End - bar.Start;
            var margin = TimeSpan.FromTicks(Math.Max(span.Ticks / 4, MinimumSpan.Ticks / 2));
            SetView(bar.Start - margin, bar.End + margin);
            ScrollIntoView(bar.Row);
        }

        private void ScrollIntoView(TimelineRowModel row)
        {
            if (row.Top < _scrollY) _scrollY = row.Top;
            else if (row.Top + row.Height > _scrollY + RowsHeight) _scrollY = row.Top + row.Height - RowsHeight;
            ClampScroll();
            _canvas.Invalidate();
        }

        private void ToggleCollapsed(TimelineRowModel row)
        {
            if (row is not { IsGroup: true }) return;
            row.Collapsed = !row.Collapsed;
            if (_selectedBar != null && _selectedBar.Row.Parent == row && row.Collapsed) _selectedBar = null;
            Relayout();
        }

        private void SetAllCollapsed(bool collapsed)
        {
            foreach (var root in _model.Roots.Where(r => r.IsGroup)) root.Collapsed = collapsed;
            if (collapsed && _selectedBar?.Row.Parent != null) _selectedBar = null;
            Relayout();
        }

        private void ToggleStatus(RunStatus status)
        {
            if (!_hiddenStatuses.Remove(status)) _hiddenStatuses.Add(status);
            if (_selectedBar != null && !IsVisible(_selectedBar)) _selectedBar = null;
            Relayout();
        }

        #endregion View changes

        #region Input

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            _canvas.Focus();
            _dragStart = e.Location;
            _dragMoved = false;
            _dragViewFrom = _viewFrom;
            _dragViewTo = _viewTo;
            _dragScrollY = _scrollY;
            if (e.Button != MouseButtons.Left) return;

            if (IsOnSplitter(e.Location))
            {
                _drag = DragMode.LabelSplitter;
                _dragLabelWidth = _labelWidthUnscaled;
            }
            else if (ScrollThumbRect.Contains(e.X, e.Y))
            {
                _drag = DragMode.ScrollThumb;
            }
            else if (e.X >= ChartRight && e.Y >= RowsTop && e.Y < RowsBottom)
            {
                // Click in the scroll track: page towards the click
                _scrollY += (e.Y < ScrollThumbRect.Top ? -1 : 1) * RowsHeight * 0.9f;
                ClampScroll();
                _canvas.Invalidate();
            }
            else if (OverviewRect.Contains(e.X, e.Y))
            {
                _drag = DragMode.Overview;
                CenterViewOnOverview(e.X);
            }
            else if (e.Y < RowsTop && e.X >= ChartLeft && e.X <= ChartRight)
            {
                _drag = DragMode.ZoomRange;
            }
            else if (e.X >= ChartLeft && e.X <= ChartRight && (ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                _drag = DragMode.ZoomRange;
            }
            else if (e.Y >= RowsTop && e.Y < RowsBottom)
            {
                _drag = DragMode.Pan;
            }
        }

        private void CenterViewOnOverview(float x)
        {
            var strip = OverviewRect;
            var t = _periodFrom.AddTicks((long)((x - strip.Left) / strip.Width * PeriodTicks));
            var half = TimeSpan.FromTicks((long)(ViewTicks / 2));
            SetView(t - half, t + half);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            _mouse = e.Location;
            _mouseInside = true;
            if (_drag != DragMode.None && (Math.Abs(e.X - _dragStart.X) > 3 || Math.Abs(e.Y - _dragStart.Y) > 3)) _dragMoved = true;

            switch (_drag)
            {
                case DragMode.Pan:
                    {
                        var dx = e.X - _dragStart.X;
                        var delta = TimeSpan.FromTicks((long)(-dx / ChartWidth * (_dragViewTo - _dragViewFrom).Ticks));
                        SetView(_dragViewFrom + delta, _dragViewTo + delta);
                        _scrollY = _dragScrollY - (e.Y - _dragStart.Y);
                        ClampScroll();
                        _canvas.Cursor = Cursors.SizeAll;
                        break;
                    }
                case DragMode.Overview:
                    CenterViewOnOverview(e.X);
                    break;

                case DragMode.ScrollThumb:
                    {
                        var thumb = ScrollThumbRect;
                        var track = RowsHeight - thumb.Height;
                        if (track > 0) _scrollY = _dragScrollY + (e.Y - _dragStart.Y) / track * MaxScroll;
                        ClampScroll();
                        break;
                    }
                case DragMode.LabelSplitter:
                    _labelWidthUnscaled = Math.Max(80, _dragLabelWidth + (e.X - _dragStart.X) / Dpi);
                    break;

                case DragMode.ZoomRange:
                    break;

                default:
                    UpdateHover(e.Location);
                    break;
            }
            _canvas.Invalidate();
        }

        private void UpdateHover(Point p)
        {
            _hoverBar = BarAt(p);
            _hoverRow = RowAt(p.Y);
            if (IsOnSplitter(p)) _canvas.Cursor = Cursors.VSplit;
            else if (_hoverBar != null || IsOnChevron(_hoverRow, p) || LegendHit(p) != null) _canvas.Cursor = Cursors.Hand;
            else if (p.Y < RowsTop && p.X >= ChartLeft && p.X <= ChartRight) _canvas.Cursor = Cursors.IBeam;
            else _canvas.Cursor = Cursors.Default;
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            var drag = _drag;
            _drag = DragMode.None;

            if (drag == DragMode.ZoomRange && _dragMoved)
            {
                var a = XToTime(Math.Min(_dragStart.X, e.X));
                var b = XToTime(Math.Max(_dragStart.X, e.X));
                if (b > a) SetView(a, b);
            }
            else if (!_dragMoved && e.Button == MouseButtons.Left && drag is DragMode.Pan or DragMode.None or DragMode.ZoomRange)
            {
                HandleClick(e.Location);
            }
            else if (!_dragMoved && e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
            UpdateHover(e.Location);
            _canvas.Invalidate();
        }

        private void HandleClick(Point p)
        {
            var status = LegendHit(p);
            if (status.HasValue)
            {
                ToggleStatus(status.Value);
                return;
            }
            var row = RowAt(p.Y);
            if (IsOnChevron(row, p))
            {
                ToggleCollapsed(row);
                return;
            }
            _selectedBar = BarAt(p);
        }

        private void Canvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var bar = BarAt(e.Location);
            if (bar != null)
            {
                BarActivated?.Invoke(this, bar);
                return;
            }
            var row = RowAt(e.Y);
            if (row is { IsGroup: true } && e.X < LabelWidth)
            {
                // The first click of the double click already toggled it via the chevron - only toggle for the rest of the label.
                if (!IsOnChevron(row, e.Location)) ToggleCollapsed(row);
                return;
            }
            if (e.Y < RowsTop || (e.X >= ChartLeft && e.Y < RowsBottom)) ResetZoom();
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            _mouseInside = false;
            _hoverBar = null;
            _hoverRow = null;
            _canvas.Invalidate();
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            var notches = e.Delta / 120.0;
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                ZoomAt(Math.Clamp(e.X, ChartLeft, ChartRight), Math.Pow(0.8, notches));
            }
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                PanBy(TimeSpan.FromTicks((long)(-notches * ViewTicks / 10)));
            }
            else
            {
                _scrollY -= (float)(notches * LaneHeight * 3);
                ClampScroll();
            }
            UpdateHover(e.Location);
            _canvas.Invalidate();
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            var handled = true;
            switch (e.KeyCode)
            {
                case Keys.Oemplus or Keys.Add:
                    ZoomAt(ChartLeft + ChartWidth / 2, 0.8);
                    break;

                case Keys.OemMinus or Keys.Subtract:
                    ZoomAt(ChartLeft + ChartWidth / 2, 1.25);
                    break;

                case Keys.Home or Keys.D0 or Keys.NumPad0:
                    ResetZoom();
                    break;

                case Keys.Left:
                    PanBy(TimeSpan.FromTicks((long)(-ViewTicks / 10)));
                    break;

                case Keys.Right:
                    PanBy(TimeSpan.FromTicks((long)(ViewTicks / 10)));
                    break;

                case Keys.Up:
                    _scrollY -= LaneHeight * 2;
                    break;

                case Keys.Down:
                    _scrollY += LaneHeight * 2;
                    break;

                case Keys.PageUp:
                    _scrollY -= RowsHeight * 0.9f;
                    break;

                case Keys.PageDown:
                    _scrollY += RowsHeight * 0.9f;
                    break;

                case Keys.Escape:
                    _selectedBar = null;
                    break;

                case Keys.Enter when _selectedBar != null:
                    BarActivated?.Invoke(this, _selectedBar);
                    break;

                case Keys.C when e.Control && _selectedBar != null:
                    Clipboard.SetText(BarDetailsText(_selectedBar));
                    break;

                default:
                    handled = false;
                    break;
            }
            if (!handled) return;
            e.Handled = true;
            ClampScroll();
            _canvas.Invalidate();
        }

        private ContextMenuStrip _contextMenu;

        private void ShowContextMenu(Point p)
        {
            var bar = BarAt(p);
            if (bar != null) _selectedBar = bar;
            var row = RowAt(p.Y);

            _contextMenu?.Dispose();
            _contextMenu = new ContextMenuStrip();
            if (bar != null)
            {
                var open = new ToolStripMenuItem("Open job", null, (_, _) => BarActivated?.Invoke(this, bar)) { Enabled = bar.JobId != Guid.Empty, Font = new Font(_contextMenu.Font, FontStyle.Bold) };
                _contextMenu.Items.Add(open);
                _contextMenu.Items.Add("Zoom to execution", null, (_, _) => ZoomToBar(bar));
                _contextMenu.Items.Add("Copy details", null, (_, _) => Clipboard.SetText(BarDetailsText(bar)));
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            else if (row != null && row.JobId != Guid.Empty)
            {
                var first = row.AllBars.FirstOrDefault();
                if (first != null) _contextMenu.Items.Add(new ToolStripMenuItem("Open job", null, (_, _) => BarActivated?.Invoke(this, first)) { Font = new Font(_contextMenu.Font, FontStyle.Bold) });
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            if (row is { IsGroup: true }) _contextMenu.Items.Add(row.Collapsed ? "Expand" : "Collapse", null, (_, _) => ToggleCollapsed(row));
            if (_model.Roots.Any(r => r.IsGroup))
            {
                _contextMenu.Items.Add("Expand all", null, (_, _) => SetAllCollapsed(false));
                _contextMenu.Items.Add("Collapse all", null, (_, _) => SetAllCollapsed(true));
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            _contextMenu.Items.Add("Reset zoom", null, (_, _) => ResetZoom()).Enabled = _viewFrom != _periodFrom || _viewTo != _periodTo;
            if (_hiddenStatuses.Count > 0)
            {
                _contextMenu.Items.Add("Show all statuses", null, (_, _) =>
                {
                    _hiddenStatuses.Clear();
                    Relayout();
                });
            }
            _contextMenu.Items.Add("Copy image", null, (_, _) => CopyImage());
            _canvas.Invalidate();
            _contextMenu.Show(_canvas, p);
        }

        #endregion Input

        private static string BarDetailsText(TimelineBar bar) =>
            string.Join(Environment.NewLine, BarDetails(bar).Prepend((bar.JobName, null)).Select(l => l.Item2 == null ? l.Item1 : $"{l.Item1}: {l.Item2}"));

        /// <summary>Label/value lines describing an execution, for the tooltip and for copying.</summary>
        private static IEnumerable<(string, string)> BarDetails(TimelineBar bar)
        {
            yield return ("Step", bar.StepLabel);
            yield return ("Status", TimelineBar.StatusText(bar.Status));
            yield return ("Start", bar.Start.ToString("G"));
            yield return ("End", bar.IsRunning ? "(still running)" : bar.End.ToString("G"));
            yield return (bar.IsRunning ? "Elapsed" : bar.Executions > 1 ? "Avg duration" : "Duration", FormatDuration(TimeSpan.FromSeconds(bar.DurationSec), true));
            if (bar.Executions > 1) yield return ("Executions", bar.Executions.ToString("N0"));
        }

        /// <summary>A compact duration: 45s, 3m 12s, 1h 05m, 2d 3h.  <paramref name="precise"/> keeps seconds under an hour.</summary>
        internal static string FormatDuration(TimeSpan ts, bool precise = false)
        {
            if (ts.TotalSeconds < 1) return "<1s";
            if (ts.TotalMinutes < 1) return $"{(int)ts.TotalSeconds}s";
            if (ts.TotalHours < 1) return precise || ts.Seconds != 0 ? $"{(int)ts.TotalMinutes}m {ts.Seconds:00}s" : $"{(int)ts.TotalMinutes}m";
            if (ts.TotalDays < 1) return $"{(int)ts.TotalHours}h {ts.Minutes:00}m";
            return $"{(int)ts.TotalDays}d {ts.Hours}h";
        }

        /// <summary>Render every row (not just those scrolled into view) at the current zoom and copy it as an image.</summary>
        public void CopyImage()
        {
            var width = Math.Max(1, _canvas.Width);
            var height = (int)Math.Ceiling(HeaderHeight + _contentHeight + OverviewHeight);
            height = Math.Clamp(height, 1, 30000);
            var scroll = _scrollY;
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                _scrollY = 0;
                _exportHeight = height;
                try
                {
                    Render(surface.Canvas, width, height, false);
                }
                finally
                {
                    _exportHeight = null;
                    _scrollY = scroll;
                }
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var ms = new MemoryStream(data.ToArray());
                using var bmp = new Bitmap(ms);
                Clipboard.SetImage(bmp);
            }
            _canvas.Invalidate();
        }

        private void Canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            Render(e.Surface.Canvas, e.Info.Width, e.Info.Height, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contextMenu?.Dispose();
                DisposeFonts();
            }
            base.Dispose(disposing);
        }
    }
}
