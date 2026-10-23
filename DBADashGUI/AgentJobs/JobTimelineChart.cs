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
    /// highlights the rest of that job), double clicking raises <see cref="BarActivated"/> - the
    /// execution details on the job timeline.  With steps included each
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
        private Dictionary<RunStatus, int> _legendCounts = new();

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

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
            // Not while the user is typing somewhere else in the window, such as the job filter.
            _canvas.MouseEnter += (_, _) =>
            {
                _mouseInside = true;
                if (ReferenceEquals(Form.ActiveForm, FindForm()) && FromHandle(GetFocus()) is not (TextBoxBase or ComboBox)) _canvas.Focus();
            };
            Controls.Add(_canvas);
            InitFonts();
        }

        /// <summary>Raised when a bar of a single execution is double clicked, Enter is pressed on it, or its menu item is picked.</summary>
        public event EventHandler<TimelineBar> BarActivated;

        /// <summary>Raised when "Open job" is picked from the menu.  The item is only offered when something handles this.</summary>
        public event EventHandler<TimelineBar> JobActivated;

        /// <summary>Raised when the user selects a bar, or clears the selection (null).</summary>
        public event EventHandler<TimelineBar> SelectedBarChanged;

        /// <summary>The menu text for <see cref="BarActivated"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BarActivatedText { get; set; } = "Execution details";

        /// <summary>Tell the user in the tooltip that a double click does <see cref="BarActivatedText"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ActivateHint { get; set; } = true;

        public TimelineBar SelectedBar => _selectedBar;

        /// <summary>Select the first bar that matches, scrolling it into view.  Doesn't raise <see cref="SelectedBarChanged"/>.</summary>
        public void SelectBar(Func<TimelineBar, bool> match)
        {
            _selectedBar = _rows.SelectMany(r => r.Bars).Where(IsVisible).FirstOrDefault(match);
            if (_selectedBar != null) ScrollIntoView(_selectedBar.Row);
            _canvas.Invalidate();
        }

        private void SetSelectedBar(TimelineBar bar)
        {
            if (bar == _selectedBar) return;
            _selectedBar = bar;
            SelectedBarChanged?.Invoke(this, bar);
        }

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
        /// Show a new set of data.  A refresh keeps the user's place: the scroll position, collapsed groups, the
        /// selection, and a zoomed in window (the same times, so it holds still while a rolling period moves on).
        /// Not zoomed in, the view follows the new period.  The view is reset when <paramref name="resetView"/> is set
        /// (different instance or job), the length of the period changes, or the zoomed window falls outside it.
        /// </summary>
        public void SetData(JobTimelineModel model, DateTime periodFrom, DateTime periodTo, bool resetView = false)
        {
            var collapsed = _model.Roots.Where(r => r.IsGroup).ToDictionary(RowKey, r => r.Collapsed);
            var selected = _selectedBar;
            if (periodTo <= periodFrom) periodTo = periodFrom.Add(MinimumSpan);
            var hadView = _viewTo > _viewFrom;
            var zoomed = hadView && (_viewFrom != _periodFrom || _viewTo != _periodTo);
            // A rolling period's ends are worked out moments apart, so its length can differ by a few milliseconds from one
            // refresh to the next - that's the same period, not a different one
            var spanChanged = Math.Abs((periodTo - periodFrom - (_periodTo - _periodFrom)).Ticks) > Math.Max((_periodTo - _periodFrom).Ticks / 100, TimeSpan.TicksPerSecond);
            var overlaps = _viewFrom < periodTo && _viewTo > periodFrom;
            var reset = resetView || !hadView || spanChanged || (zoomed && !overlaps);
            var (viewFrom, viewTo) = (_viewFrom, _viewTo);

            _model = model ?? JobTimelineModel.Empty;
            if (!resetView)
            {
                foreach (var root in _model.Roots.Where(r => r.IsGroup))
                {
                    if (collapsed.TryGetValue(RowKey(root), out var wasCollapsed)) root.Collapsed = wasCollapsed;
                }
            }
            _periodFrom = periodFrom;
            _periodTo = periodTo;
            if (reset || !zoomed)
            {
                _viewFrom = _periodFrom;
                _viewTo = _periodTo;
                if (reset) _scrollY = 0;
            }
            else
            {
                SetView(viewFrom, viewTo);
            }
            if (resetView) selected = null;
            _selectedBar = selected == null ? null : _model.Bars.FirstOrDefault(b => b.JobName == selected.JobName && b.StepId == selected.StepId && b.Start == selected.Start);
            _hoverBar = null;
            _hoverRow = null;
            Relayout();
            // Instances that were expanded before the refresh need their jobs again
            foreach (var root in _model.Roots.Where(r => !r.Collapsed && r.CanLoadChildren)) RequestChildren(root);
            // The view may not have moved, but the detail the host fetched for it might not suit the new data
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Identifies a group row across refreshes, to keep it expanded or collapsed.</summary>
        private static string RowKey(TimelineRowModel row) => row.Kind == TimelineRowKind.Instance ? "instance:" + row.InstanceId : "job:" + row.JobName;

        /// <summary>
        /// Raised when a row whose children haven't been loaded is expanded - an instance on the multi-instance
        /// timeline.  The host fetches them and passes them to <see cref="SetChildren"/>, or <see cref="SetChildrenError"/>.
        /// </summary>
        public event EventHandler<TimelineRowModel> ChildrenRequested;

        private void RequestChildren(TimelineRowModel row)
        {
            if (!row.CanLoadChildren || row.Loading) return;
            row.Loading = true;
            row.LoadError = null;
            ChildrenRequested?.Invoke(this, row);
            _canvas.Invalidate();
        }

        /// <summary>Show the children fetched for <paramref name="row"/>.  Ignored if the data has been replaced since.</summary>
        public void SetChildren(TimelineRowModel row, JobTimelineModel children)
        {
            if (!_model.Roots.Contains(row)) return;
            _model.SetChildren(row, children);
            Relayout();
        }

        public void SetChildrenError(TimelineRowModel row, string message)
        {
            if (!_model.Roots.Contains(row)) return;
            row.Loading = false;
            row.LoadError = message;
            _canvas.Invalidate();
        }

        /// <summary>
        /// Failed runs per row (own, and with the rows beneath it) from the full period load, when what's drawn was put
        /// together from more than one level of detail - see <see cref="JobTimelineModel.CountsFrom"/>.  Counting the
        /// drawn bars would count a failure twice: once in the zoomed in detail, and again in the share of it given to a
        /// merged bar trimmed back to the edges of the detail.
        /// </summary>
        private Dictionary<string, (int Own, int All)> _exactFailures;

        private static string FailureKey(TimelineRowModel row) => $"{row.InstanceId}|{row.Kind}|{row.JobName}|{row.Label}";

        private static Dictionary<string, (int Own, int All)> FailuresByRow(IEnumerable<TimelineRowModel> rows)
        {
            var failures = new Dictionary<string, (int Own, int All)>();
            void Add(TimelineRowModel row)
            {
                failures[FailureKey(row)] = (row.Bars.Sum(b => b.RunsWith(RunStatus.Failed)), row.AllBars.Sum(b => b.RunsWith(RunStatus.Failed)));
                foreach (var child in row.Children) Add(child);
            }
            foreach (var row in rows) Add(row);
            return failures;
        }

        /// <summary>The failed runs to show on a row's badge: with the rows beneath it when it's collapsed.</summary>
        private int RowFailures(TimelineRowModel row)
        {
            if (_exactFailures != null && _exactFailures.TryGetValue(FailureKey(row), out var exact)) return row.Collapsed ? exact.All : exact.Own;
            return (row.Collapsed ? row.AllBars : row.Bars).Where(IsVisible).Sum(b => b.RunsWith(RunStatus.Failed));
        }

        /// <summary>Changes whenever the rows are laid out again, so caches drawn from them know to redraw.</summary>
        private int _layoutVersion;

        private int HiddenStatusKey => _hiddenStatuses.Aggregate(0, (key, status) => key | 1 << (int)status);

        /// <summary>Rebuild the visible row list and their positions after anything that changes which rows show.</summary>
        private void Relayout()
        {
            _rows = _model.VisibleRows(_filter, IsVisible, HiddenStatusKey, _sort, _hiddenStatuses.Count > 0 ? s => !_hiddenStatuses.Contains(s) : null);
            _legendCounts = _model.LegendCounts(_filter);
            _exactFailures = _model.CountsFrom == null ? null : FailuresByRow(_model.CountsFrom.Roots);
            _layoutVersion++;
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

        /// <summary>
        /// Whether a bar shows with the statuses hidden in the legend.  A bar merging several runs shows if any of them has
        /// a status that isn't hidden - judging it by its worst status alone hid an instance's span holding a running job
        /// when only running was shown, because a failure overlapped it.
        /// </summary>
        private bool IsVisible(TimelineBar bar) =>
            bar.StatusRuns == null ? !_hiddenStatuses.Contains(bar.Status) : StatusSeverity.Any(s => bar.RunsWith(s) > 0 && !_hiddenStatuses.Contains(s));

        /// <summary>Statuses from worst to best, as a merged bar takes the worst of its runs.</summary>
        private static readonly RunStatus[] StatusSeverity = { RunStatus.Failed, RunStatus.Retry, RunStatus.Cancelled, RunStatus.InProgress, RunStatus.Succeeded };

        /// <summary>The status to colour a bar by: the worst of its runs that isn't hidden in the legend.</summary>
        private RunStatus ShownStatus(TimelineBar bar)
        {
            if (bar.StatusRuns == null || _hiddenStatuses.Count == 0) return bar.Status;
            foreach (var status in StatusSeverity)
            {
                if (bar.RunsWith(status) > 0 && !_hiddenStatuses.Contains(status)) return status;
            }
            return bar.Status;
        }

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
        private float RowsTop => HeaderHeight + BandHeight;

        /// <summary>The concurrency band, between the axis and the rows, when shown.</summary>
        private float BandHeight => ShowConcurrency ? 36 * Dpi : 0;
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
            var index = RowIndexAt(y - RowsTop + _scrollY);
            return index < 0 ? null : _rows[index];
        }

        /// <summary>The index of the row at a content position, by binary search on the rows' tops, or -1.</summary>
        private int RowIndexAt(float contentY)
        {
            int lo = 0, hi = _rows.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                var row = _rows[mid];
                if (contentY < row.Top) hi = mid - 1;
                else if (contentY >= row.Top + row.Height) lo = mid + 1;
                else return mid;
            }
            return -1;
        }

        /// <summary>The first row that is at least partly below <paramref name="contentY"/>.</summary>
        private int FirstRowEndingAfter(float contentY)
        {
            int lo = 0, hi = _rows.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (_rows[mid].Top + _rows[mid].Height < contentY) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private TimelineBar BarAt(Point p)
        {
            if (p.X < ChartLeft || p.X > ChartRight) return null;
            var row = RowAt(p.Y);
            if (row == null) return null;
            // Topmost first, and allow a little slack either side so very short runs can still be picked.  A short bar
            // is drawn wider than its time, so look a few pixels either side of the pointer's time.
            var slack = 2 * Dpi;
            var reach = TimeSpan.FromTicks((long)((slack + 3 * Dpi) / ChartWidth * ViewTicks));
            var t = _viewFrom.AddTicks((long)((p.X - ChartLeft) / ChartWidth * ViewTicks));
            return row.BarsBetween(t - reach, t + reach).Where(IsVisible).LastOrDefault(b =>
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
            var changed = from != _viewFrom || from + span != _viewTo;
            _viewFrom = from;
            _viewTo = from + span;
            RefreshHover();
            _canvas.Invalidate();
            if (changed) ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raised when the time in view changes - zoom, pan, reset, or new data.  Raised often while dragging, so a host
        /// that fetches more detail for the view should wait for it to settle.
        /// </summary>
        public event EventHandler ViewChanged;

        /// <summary>The time in view, in the app time zone.</summary>
        public DateTime ViewFrom => _viewFrom;

        public DateTime ViewTo => _viewTo;

        /// <summary>The whole period the data is for, in the app time zone.</summary>
        public DateTime PeriodFrom => _periodFrom;

        public DateTime PeriodTo => _periodTo;

        /// <summary>The width of the time axis in pixels, for working out how much time a pixel covers.</summary>
        public float TimeAxisWidth => ChartWidth;

        private bool _showConcurrency;

        /// <summary>Show the band of how many jobs were running at once, between the axis and the rows.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowConcurrency
        {
            get => _showConcurrency;
            set
            {
                if (_showConcurrency == value) return;
                _showConcurrency = value;
                ClampScroll();
                _layoutVersion++;
                _canvas.Invalidate();
            }
        }

        // The band's data: the most jobs running at once in each slice, from _bandFrom
        private DateTime _bandFrom;
        private TimeSpan _bandSlice;
        private int[] _bandPeaks = Array.Empty<int>();

        /// <summary>
        /// The concurrency band's data: the most jobs running at once in each <paramref name="slice"/> from
        /// <paramref name="from"/> (app time zone).  It needn't cover the whole period - the host fetches it for the view.
        /// </summary>
        public void SetConcurrency(DateTime from, TimeSpan slice, int[] peaks)
        {
            _bandFrom = from;
            _bandSlice = slice > TimeSpan.Zero ? slice : TimeSpan.FromSeconds(1);
            _bandPeaks = peaks ?? Array.Empty<int>();
            _canvas.Invalidate();
        }

        /// <summary>The most jobs running at once at <paramref name="t"/>, or null where the band has no data.</summary>
        private int? ConcurrencyAt(DateTime t)
        {
            if (_bandPeaks.Length == 0 || t < _bandFrom) return null;
            var index = (long)((t - _bandFrom).Ticks / _bandSlice.Ticks);
            return index < _bandPeaks.Length ? _bandPeaks[index] : null;
        }

        /// <summary>A note shown across the top of the rows, e.g. that runs are merged at this zoom.  Null for none.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Banner
        {
            get => _banner;
            set
            {
                if (_banner == value) return;
                _banner = value;
                _canvas.Invalidate();
            }
        }

        private string _banner;

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
            if (!row.Collapsed) RequestChildren(row);
        }

        /// <summary>
        /// Expand or collapse every group shown.  Only the rows left by the filter are expanded, so on the multi-instance
        /// timeline a filter limits how many instances' jobs are fetched.
        /// </summary>
        private void SetAllCollapsed(bool collapsed)
        {
            var groups = collapsed ? _model.Roots.Where(r => r.IsGroup).ToList() : _rows.Where(r => r.Depth == 0 && r.IsGroup).ToList();
            foreach (var root in groups) root.Collapsed = collapsed;
            if (collapsed && _selectedBar?.Row.Parent != null) _selectedBar = null;
            Relayout();
            if (!collapsed) groups.ForEach(RequestChildren);
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
            SetSelectedBar(BarAt(p));
        }

        private void Canvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var bar = BarAt(e.Location);
            if (bar != null)
            {
                if (bar.IsSummary)
                {
                    if (bar.Row.Collapsed) ToggleCollapsed(bar.Row);
                }
                else if (bar.IsSingleExecution)
                {
                    BarActivated?.Invoke(this, bar);
                }
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
                    SetSelectedBar(null);
                    break;

                case Keys.Enter when _selectedBar is { IsSingleExecution: true }:
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
            if (bar != null) SetSelectedBar(bar);
            var row = RowAt(p.Y);

            _contextMenu?.Dispose();
            _contextMenu = new ContextMenuStrip();
            if (bar is { IsSummary: true })
            {
                if (bar.Row.Collapsed)
                {
                    _contextMenu.Items.Add(new ToolStripMenuItem("Show jobs", null, (_, _) => ToggleCollapsed(bar.Row)) { Font = new Font(_contextMenu.Font, FontStyle.Bold) });
                }
                _contextMenu.Items.Add("Zoom to span", null, (_, _) => ZoomToBar(bar));
                _contextMenu.Items.Add("Copy details", null, (_, _) => Clipboard.SetText(BarDetailsText(bar)));
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            else if (bar != null)
            {
                if (BarActivated != null)
                {
                    _contextMenu.Items.Add(new ToolStripMenuItem(BarActivatedText, null, (_, _) => BarActivated?.Invoke(this, bar))
                    {
                        Enabled = bar.IsSingleExecution,
                        Font = new Font(_contextMenu.Font, FontStyle.Bold),
                        ToolTipText = bar.IsSingleExecution ? null : "This bar combines several executions.  Zoom in to see each one (or set the date grouping to None)."
                    });
                }
                if (JobActivated != null)
                {
                    _contextMenu.Items.Add(new ToolStripMenuItem("Open job", null, (_, _) => JobActivated?.Invoke(this, bar)) { Enabled = bar.JobId != Guid.Empty });
                }
                _contextMenu.Items.Add("Zoom to execution", null, (_, _) => ZoomToBar(bar));
                _contextMenu.Items.Add("Copy details", null, (_, _) => Clipboard.SetText(BarDetailsText(bar)));
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            else if (row != null && row.JobId != Guid.Empty && JobActivated != null)
            {
                var first = row.AllBars.FirstOrDefault();
                if (first != null) _contextMenu.Items.Add(new ToolStripMenuItem("Open job", null, (_, _) => JobActivated?.Invoke(this, first)) { Font = new Font(_contextMenu.Font, FontStyle.Bold) });
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
            if (bar.IsSummary)
            {
                yield return ("Executions", bar.Executions.ToString("N0") + (bar.Executions == 1 ? " job run" : " job runs"));
                yield return ("Worst status", TimelineBar.StatusText(bar.Status));
                yield return ("Start", bar.Start.ToString("G"));
                yield return ("End", bar.End.ToString("G"));
                yield return ("Busy for", FormatDuration(TimeSpan.FromSeconds(bar.DurationSec), true));
                yield break;
            }
            yield return ("Step", bar.StepLabel);
            yield return ("Status", TimelineBar.StatusText(bar.Status));
            yield return ("Start", bar.Start.ToString("G"));
            yield return ("End", bar.IsRunning ? "(still running)" : bar.End.ToString("G"));
            yield return (bar.IsRunning ? "Elapsed" : bar.Executions > 1 ? "Avg duration" : "Duration", FormatDuration(TimeSpan.FromSeconds(bar.DurationSec), true));
            if (bar.Executions > 1) yield return ("Executions", bar.Executions.ToString("N0"));
            // A merged bar is coloured by its worst run - say what the rest were
            if (bar.StatusRuns != null && bar.StatusRuns.Count(n => n > 0) > 1)
            {
                yield return ("By status", string.Join(", ", JobTimelineModel.StatusRunColumns
                    .Where(c => bar.RunsWith(c.Status) > 0)
                    .Select(c => $"{bar.RunsWith(c.Status):N0} {TimelineBar.StatusText(c.Status).ToLowerInvariant()}")));
            }
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
