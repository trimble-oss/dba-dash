using DBADash.JobSchedules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Plans moves to agent job schedules on a job timeline chart: the jobs that ran in the period, on the chart, with the
    /// parts of runs that overlap another previewed job in purple - red stays for failures.  A job's start time can be moved by dragging its bars or
    /// typing a move, start times can be suggested, and the moves scripted.  Nothing is changed on the server.
    ///
    /// <para>The chart belongs to the host - the job timeline in plan mode -
    /// and is drawn on while <see cref="Attach"/>ed.  The editor is the toolbar, summary and the grid of jobs.</para>
    /// <para>Overlap is measured as the schedules predict it over the selected range - what they'll keep doing - or, for a
    /// pair with a job that has no successful runs to time, in the runs that happened.  The other measure is in the tooltips.</para>
    /// </summary>
    [DesignerCategory("")]
    internal sealed partial class SchedulePlanEditor : UserControl
    {
        /// <summary>Jobs ticked to start with: those most involved in overlaps</summary>
        private const int MaxDefaultIncluded = 12;

        /// <summary>
        /// The shortest expected duration of a job listed and previewed - null for Auto, which goes by the preview's range
        /// (<see cref="EffectiveMinDurationSec"/>).  Over a week, jobs of a few seconds are a band at most and drown out the
        /// jobs a move would help; they're for a closer look at a shorter range.  Set by the host, from its own filter.
        /// </summary>
        private TimeSpan? _minDuration;

        /// <summary>The shortest expected duration, in seconds, of a job listed and previewed.  Null for Auto, which goes by the range previewed.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? MinDurationSec
        {
            get => _minDuration is { } d ? (int)d.TotalSeconds : null;
            set
            {
                _minDuration = value is { } seconds ? TimeSpan.FromSeconds(seconds) : null;
                ApplyDurationFilter();
            }
        }

        /// <summary>What the duration filter does now, e.g. "Jobs 5m or longer (auto)", for the host's button</summary>
        public string DurationFilterText { get; private set; } = "";

        /// <summary>How many jobs the duration filter leaves off, and why, for the host's tooltip</summary>
        public string DurationFilterDetail { get; private set; } = "";

        /// <summary>Raised when what the duration filter does changes - <see cref="DurationFilterText"/></summary>
        public event EventHandler DurationFilterChanged;

        /// <summary>
        /// The user has ticked or cleared jobs to preview.  Until then every job the duration filter lists is previewed, and
        /// changing the filter previews the jobs it lists then.
        /// </summary>
        private bool _includedByUser;

        /// <summary>The lengths Auto picks from, the longest no more than a 2000th of the range</summary>
        private static readonly TimeSpan[] DurationSteps = { TimeSpan.Zero, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1) };

        /// <summary>The chart drawn on, while attached</summary>
        private JobTimelineChart _preview;

        /// <summary>The chart's settings before it was attached, put back on detach</summary>
        private (string Title, bool ShowConcurrency, bool ShowNowLine, bool ActivateHint, string EmptyText) _chartWas;

        private readonly DBADashDataGridView _whatIfGrid = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            ShowCellToolTips = true,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        };

        private readonly Label _whatIfSummary = new() { Dock = DockStyle.Top, AutoSize = false, Padding = new Padding(8, 6, 8, 4), Font = new Font("Segoe UI", 9.75f) };

        /// <summary>What the moves do overall - overlap before and after, the pairs better and worse, and the jobs worse off.  Shown when there are moves.</summary>
        private readonly Label _netEffect = new() { Dock = DockStyle.Top, AutoSize = false, Padding = new Padding(8, 6, 8, 0), Font = new Font("Segoe UI", 9.75f, FontStyle.Bold), Visible = false };

        /// <summary>One line on what needs a second look - the jobs it's about have ⚠ in their Note.  Shown only then.</summary>
        private readonly Label _whatIfCaution = new() { Dock = DockStyle.Top, AutoSize = false, Padding = new Padding(8, 0, 8, 4), Font = new Font("Segoe UI", 9.75f), ForeColor = DashColors.Warning, Visible = false };

        /// <summary>The other overlap measure, on the net effect line</summary>
        private readonly ToolTip _tips = new();

        private readonly ToolStrip _whatIfToolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripDropDownButton _basisButton;
        private readonly ToolStripButton _undoButton;

        /// <summary>How much predicted time to preview from midnight today - or zero for the selected range's actual runs</summary>
        private TimeSpan _previewLength = TimeSpan.Zero;

        private DateTime _rangeFrom;
        private DateTime _rangeTo;

        /// <summary>The overlap report the data came with, for picking the most overlapping jobs again</summary>
        private JobOverlapReport _report;

        /// <summary>A clock change the runs were corrected for, from the data</summary>
        private ClockChange? _clockChange;

        private bool PreviewActual => _previewLength == TimeSpan.Zero;

        /// <summary>The jobs, the moves and the jobs getting their own schedule</summary>
        private readonly SchedulePlan _plan = new();

        /// <summary>The plan before each change, to go back to - and after each undo, to go forward to again</summary>
        private readonly Stack<PlanState> _undo = new();

        private readonly Stack<PlanState> _redo = new();

        private int _instanceId;
        private int _whatIfUtcOffset;
        private DataTable _whatIfTable;
        private bool _resetPreviewView = true;
        private bool _updatingWhatIf;
        private string _whatIfWarning;

        /// <summary>The previewed runs, by the source index of their bar</summary>
        private List<PreviewRun> _previewRuns = new();

        /// <param name="JobIndex">The job's place in the jobs previewed, for <see cref="PlanOverlaps.Detail"/></param>
        /// <param name="RunIndex">The run's place in the job's runs</param>
        private sealed record PreviewRun(JobScheduleInput Job, JobRun Run, int JobIndex, int RunIndex);

        private static readonly HashSet<string> DurationColumns = new() { "ExpectedSec", "CycleSec" };

        /// <summary>
        /// Overlap time, shown with how the moves change it ("4m 10s (-2m)") from the column named here - more in red, less
        /// in green.  Hidden columns hold the change and the detail for the tooltip.
        /// </summary>
        private static readonly Dictionary<string, string> OverlapColumns = new() { ["OverlapSec"] = "OverlapChangeSec" };

        /// <summary>Columns whose tooltip is the text in a hidden column: both overlap measures, a job's schedules, its durations</summary>
        private static readonly Dictionary<string, string> DetailColumns = new() { ["OverlapSec"] = "OverlapDetail", ["Job"] = "JobDetail", ["ExpectedSec"] = "DurationDetail", ["BusyRuns"] = "BusyDetail" };

        /// <summary>+4m 10s, -1h 2m, or blank for no change</summary>
        private static string FormatChange(double seconds) =>
            Math.Abs(seconds) < 0.5 ? "" : (seconds > 0 ? "+" : "-") + JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(Math.Abs(seconds)));

        /// <summary>Which pairs of previewed jobs overlap, with the moves, and how that's changed - beside the jobs</summary>
        private readonly DBADashDataGridView _pairsGrid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            ShowCellToolTips = true,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };

        private readonly SplitContainer _gridSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
        private DataTable _pairsTable;

        /// <summary>
        /// Show the chart in the app's time zone, like the job timeline, rather than the server's local time the schedules
        /// use (and the grid shows).
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowAppTimeZone { get; set; }

        /// <summary>A line about the period, ahead of the summary - e.g. that it's the last days of a longer one</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PeriodNote { get; set; }

        /// <summary>The instance the plan is for</summary>
        public string InstanceName { get; private set; }

        /// <summary>Schedules the plan would change, and jobs it would give their own copy of one - what Script Changes would script</summary>
        public int PendingChanges
        {
            get
            {
                var (changes, splits) = _plan.Changes();
                return changes.Count + splits.Count;
            }
        }

        private string DisplayTimeName => ShowAppTimeZone ? "(your time zone)" : "server time";

        /// <summary>A time in the preview as shown: in the app time zone, or server time as it is</summary>
        private DateTime Display(DateTime serverTime) => ShowAppTimeZone ? ToAppZone(ToUtc(serverTime, PreviewActual)) : serverTime;

        /// <summary>A time among the runs that happened as shown, whichever preview is on</summary>
        private DateTime DisplayRan(DateTime serverTime) => ShowAppTimeZone ? ToAppZone(ToUtc(serverTime, true)) : serverTime;

        // The app time zone's offset by quarter hour of UTC - clocks only change on a quarter hour.  Converting each time, a
        // busy week's runs and their overlaps were a million conversions every time the preview was drawn again.
        private readonly Dictionary<long, (TimeSpan Offset, DateTimeKind Kind)> _zoneOffsets = new();
        private TimeZoneInfo _offsetsZone;

        private DateTime ToAppZone(DateTime utc)
        {
            if (!ReferenceEquals(_offsetsZone, DateHelper.AppTimeZone))
            {
                _zoneOffsets.Clear();
                _offsetsZone = DateHelper.AppTimeZone;
            }
            var quarter = utc.Ticks / (TimeSpan.TicksPerMinute * 15);
            if (!_zoneOffsets.TryGetValue(quarter, out var zone))
            {
                var start = new DateTime(quarter * TimeSpan.TicksPerMinute * 15, DateTimeKind.Utc);
                var converted = start.ToAppTimeZone();
                _zoneOffsets[quarter] = zone = (converted - start, converted.Kind);
            }
            return DateTime.SpecifyKind(utc + zone.Offset, zone.Kind);
        }

        /// <summary>
        /// A server time in UTC.  The runs that happened (<paramref name="ran"/>) are in the server's time as its clock showed
        /// it, so one from before a clock change is taken back by it first - shown when it ran, in line with the CPU.  In the
        /// hour the clocks went back, a time is taken as after the change.  The predicted preview is in the server's time now.
        /// </summary>
        private DateTime ToUtc(DateTime serverTime, bool ran) =>
            (ran && _clockChange is { } c && serverTime < c.Until ? serverTime.AddMinutes(-c.Minutes) : serverTime).AddMinutes(_whatIfUtcOffset);

        /// <summary>The expected duration basis picked on the editor's own toolbar - when <see cref="ShowBasisChoice"/></summary>
        public DurationBasis Basis { get; private set; } = DurationBasis.P95;

        /// <summary>Show a choice of duration basis on the toolbar.  The host analyses the data again when it changes.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowBasisChoice
        {
            get => _basisButton.Visible;
            set => _basisButton.Visible = value;
        }

        public event EventHandler BasisChanged;

        public SchedulePlanEditor()
        {
            _viewTimer.Tick += ViewTimer_Tick;
            var periodButton = new ToolStripDropDownButton
            {
                ToolTipText = "What to preview: the runs in the selected range, moved as set here - or the runs the schedules predict, from midnight today (server time)"
            };
            FillChoices(periodButton, "Preview", _previewLength, length =>
                {
                    _previewLength = length;
                    _resetPreviewView = true;
                    if (_minDuration == null && _whatIfTable != null) ApplyDurationFilter();
                    else RefreshWhatIf();
                },
                (TimeSpan.Zero, "Selected range (actual runs)"),
                (TimeSpan.FromHours(2), "2 hours predicted"), (TimeSpan.FromHours(6), "6 hours predicted"), (TimeSpan.FromDays(1), "1 day predicted"), (TimeSpan.FromDays(7), "7 days predicted"));

            _basisButton = new ToolStripDropDownButton { ToolTipText = "How long each job is expected to take, from its successful runs over the last 14 days", Visible = false };
            FillChoices(_basisButton, "Duration", Basis, basis =>
                {
                    Basis = basis;
                    BasisChanged?.Invoke(this, EventArgs.Empty);
                },
                Enum.GetValues<DurationBasis>().Select(b => (b, ScheduleOverlapData.BasisText(b))).ToArray());

            _undoButton = new ToolStripButton("Undo", Properties.Resources.Undo_grey_16x, (_, _) => Undo())
            {
                ToolTipText = "Undo the last change to the plan (Ctrl+Z).  Ctrl+Y redoes it.",
                Enabled = false
            };
            var reset = new ToolStripButton("Reset Moves", Properties.Resources._112_RefreshArrow_Green_16x16_72, (_, _) => Change(_plan.Reset))
            { ToolTipText = "Put every job back to its current start time and schedule.  Undo brings the moves back." };
            var previewButton = new ToolStripDropDownButton("Preview", Properties.Resources.SelectRows)
            {
                ToolTipText = "Choose which jobs to preview: all those listed, none, or the ones that overlap most in ways a move could help.  Clicking the Preview column's header ticks or clears them all too.  " +
                              "Which jobs are listed is up to the duration filter on the toolbar above."
            };
            previewButton.DropDownItems.Add("All Listed Jobs", null, (_, _) =>
            {
                // Back to previewing whatever the duration filter lists
                _includedByUser = false;
                SetIncluded(_plan.Jobs.Where(LongEnough).Select(j => j.JobId));
            });
            previewButton.DropDownItems.Add("None", null, (_, _) =>
            {
                _includedByUser = true;
                SetIncluded(Array.Empty<Guid>());
            });
            previewButton.DropDownItems.Add($"Most Overlapping ({MaxDefaultIncluded})", null, (_, _) =>
            {
                if (_report == null) return;
                _includedByUser = true;
                _plan.Included.Clear();
                _plan.IncludeMostOverlapping(_report, MaxDefaultIncluded, LongEnough);
                SetIncluded(_plan.Included.ToList());
            });
            _whatIfToolStrip.Items.AddRange(new ToolStripItem[] { periodButton, _basisButton, new ToolStripSeparator(), previewButton, _undoButton, reset, CreateBusyItem() });
            _whatIfToolStrip.Items.AddRange(CreateSuggestItems());
            _whatIfToolStrip.Items.AddRange(CreateScriptItems());

            _whatIfGrid.CellFormatting += Grid_CellFormatting;
            _whatIfGrid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
            _whatIfGrid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_whatIfGrid.CurrentCell is DataGridViewCheckBoxCell) _whatIfGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _whatIfGrid.CellValueChanged += WhatIfGrid_CellValueChanged;
            // The Preview header ticks every job, or clears them all when they're all ticked already
            _whatIfGrid.ColumnHeaderMouseClick += (_, e) =>
            {
                if (_whatIfGrid.Columns[e.ColumnIndex].Name != "Include" || e.Button != MouseButtons.Left) return;
                var listed = _plan.Jobs.Where(LongEnough).ToList();
                var all = listed.Count > 0 && listed.All(j => _plan.Included.Contains(j.JobId));
                _includedByUser = all;
                SetIncluded(all ? Array.Empty<Guid>() : listed.Select(j => j.JobId));
            };
            // Own Schedule only applies to jobs that share one
            _whatIfGrid.CellBeginEdit += (_, e) =>
            {
                if (_whatIfGrid.Columns[e.ColumnIndex].Name == "OwnSchedule" && _whatIfGrid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var row } && row["OwnSchedule"] == DBNull.Value) e.Cancel = true;
            };
            _whatIfGrid.DataError += (_, e) => e.ThrowException = false;
            // Hidden here rather than straight after binding: bound while it hasn't been shown, the grid makes the
            // columns visible again when it's first displayed
            _whatIfGrid.DataBindingComplete += (_, _) => HideColumns(_whatIfGrid, "job_id", "OverlapChangeSec", "OverlapDetail", "JobDetail", "DurationDetail", "Warn", "BusyRunsChange", "BusyDetail");

            _pairsGrid.CellFormatting += Grid_CellFormatting;
            _pairsGrid.CellFormatting += PairsGrid_CellFormatting;
            _pairsGrid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
            _pairsGrid.DataBindingComplete += (_, _) => HideColumns(_pairsGrid, "ChangeOrder", "OverlapChangeSec", "OverlapDetail");
            // Split once there's a real width to split - at handle creation the editor isn't sized yet
            var gridSplitSet = false;
            _gridSplit.SizeChanged += (_, _) =>
            {
                if (gridSplitSet || _gridSplit.Width < LogicalToDeviceUnits(400)) return;
                _gridSplit.SplitterDistance = _gridSplit.Width * 55 / 100;
                gridSplitSet = true;
            };
            _gridSplit.Panel1.Controls.Add(_whatIfGrid);
            _gridSplit.Panel2.Controls.Add(_pairsGrid);
            Controls.Add(_gridSplit);
            Controls.Add(_whatIfCaution);
            // What the moves do overall, then the details - together, so the overall line stays under the toolbar
            var summaryPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            summaryPanel.Controls.Add(_whatIfSummary);
            summaryPanel.Controls.Add(_netEffect);
            Controls.Add(summaryPanel);
            Controls.Add(_whatIfToolStrip);
            // Wraps to the width: room for every line
            _netEffect.SizeChanged += (_, _) => SizeNetEffect();
            _whatIfCaution.SizeChanged += (_, _) => SizeToText(_whatIfCaution);
        }

        /// <summary>
        /// A drop-down of choices that shows the one picked as "<paramref name="label"/>: choice" - on a toolbar button or a
        /// menu item
        /// </summary>
        internal static void FillChoices<T>(ToolStripDropDownItem owner, string label, T selected, Action<T> set, params (T Value, string Text)[] choices)
        {
            foreach (var (value, text) in choices)
            {
                var item = new ToolStripMenuItem(text) { Checked = EqualityComparer<T>.Default.Equals(value, selected) };
                item.Click += (_, _) =>
                {
                    foreach (var other in owner.DropDownItems.OfType<ToolStripMenuItem>()) other.Checked = other == item;
                    owner.Text = $"{label}: {text}";
                    set(value);
                };
                owner.DropDownItems.Add(item);
            }
            owner.Text = $"{label}: {choices.First(c => EqualityComparer<T>.Default.Equals(c.Value, selected)).Text}";
        }

        private static void HideColumns(DataGridView grid, params string[] names)
        {
            foreach (var name in names.Where(grid.Columns.Contains)) grid.Columns[name].Visible = false;
        }

        private void SizeNetEffect()
        {
            if (_netEffect.Visible) SizeToText(_netEffect);
        }

        /// <summary>A docked label as tall as its text needs, wrapped to its width</summary>
        private static void SizeToText(Label label)
        {
            if (string.IsNullOrEmpty(label.Text)) return;
            var size = TextRenderer.MeasureText(label.Text, label.Font, new Size(Math.Max(100, label.Width - label.Padding.Horizontal), int.MaxValue), TextFormatFlags.WordBreak);
            var height = size.Height + label.Padding.Vertical;
            if (label.Height != height) label.Height = height;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _whatIfSummary.Height = _whatIfSummary.Font.Height * 3 + _whatIfSummary.Padding.Vertical + LogicalToDeviceUnits(4);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tips.Dispose();
                _viewTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Draw on <paramref name="chart"/>, and move jobs when their bars are dragged</summary>
        public void Attach(JobTimelineChart chart)
        {
            if (_preview == chart) return;
            Detach();
            _preview = chart;
            _chartWas = (chart.Title, chart.ShowConcurrency, chart.ShowNowLine, chart.ActivateHint, chart.EmptyText);
            chart.ShowConcurrency = true;
            chart.ShowNowLine = false;
            chart.ActivateHint = false;
            chart.EmptyText = "Tick jobs below to preview their runs";
            // Snaps to suit the zoom; Shift moves by the second
            chart.RowShiftSnap = TimeSpan.Zero;
            chart.RowShifted += Preview_RowShifted;
            chart.ViewChanged += Preview_ViewChanged;
            chart.BarDetailsProvider = PreviewDetails;
            // A drag is the whole move from where the run grabbed was, and moves the runs that move with it
            chart.RowShiftOffset = bar => TimeSpan.FromSeconds(RunOf(bar) is { } p ? DragSchedule(p) is { } s ? _plan.ScheduleShift(p.Job, s) : _plan.JobShift(p.Job) : 0);
            chart.ShiftsWith = (grabbed, bar) => RunOf(grabbed) is { } g && RunOf(bar) is { } other && MovesWith(g, other);
            _resetPreviewView = true;
            RefreshWhatIf();
        }

        /// <summary>Stop drawing on the chart and put its settings back.  The host draws its own data on it again.</summary>
        public void Detach()
        {
            if (_preview == null) return;
            var chart = _preview;
            _preview = null;
            chart.RowShifted -= Preview_RowShifted;
            chart.ViewChanged -= Preview_ViewChanged;
            _viewTimer.Stop();
            chart.BarDetailsProvider = null;
            chart.RowShiftOffset = null;
            chart.ShiftsWith = null;
            chart.RowShiftSnap = null;
            chart.LegendEntries = null;
            chart.BusyPeriods = null;
            (chart.Title, chart.ShowConcurrency, chart.ShowNowLine, chart.ActivateHint, chart.EmptyText) = _chartWas;
        }

        /// <summary>A message in place of the summary, e.g. while loading or when loading failed</summary>
        public void ShowMessage(string message, bool error = false)
        {
            _whatIfSummary.Text = message;
            _whatIfSummary.ForeColor = error ? DashColors.Fail : ForeColor;
        }

        /// <summary>New data or a different duration basis: keep the ticked jobs and moves, rebuild the rest</summary>
        public void SetData(ScheduleAnalysis analysis, int instanceId, string instanceName)
        {
            _instanceId = instanceId;
            InstanceName = instanceName;
            // Only the jobs that ran in the selected range (and category) are previewed and moved; every job on the instance counts for shared schedules
            _plan.SetJobs(analysis.AllJobs, analysis.ActualRuns, analysis.History);
            _overlapsBefore = null;
            _whatIfUtcOffset = analysis.UtcOffset;
            // The busy hours are in server time by the offset and any clock change
            ResetBusy();
            _rangeFrom = analysis.Report.From;
            _rangeTo = analysis.Report.To;
            _report = analysis.Report;
            _clockChange = analysis.ClockChange;
            BuildWhatIfTable(analysis);
            ApplyDurationFilter();
        }

        /// <summary>Make a change to the plan as one step to undo - when it changes anything</summary>
        private void Change(Action change)
        {
            var before = _plan.Snapshot();
            change();
            RecordUndo(before);
            _whatIfWarning = null;
            ClearSuggestion();
            RefreshWhatIf();
        }

        /// <summary>Keep <paramref name="before"/> to undo to, when the plan has changed since it</summary>
        private void RecordUndo(PlanState before)
        {
            if (!before.SameAs(_plan.Snapshot()))
            {
                _undo.Push(before);
                _redo.Clear();
            }
            UpdateUndo();
        }

        private void Undo() => Step(_undo, _redo);

        private void Redo() => Step(_redo, _undo);

        /// <summary>Back to the plan on top of <paramref name="from"/>, keeping where it is on <paramref name="to"/></summary>
        private void Step(Stack<PlanState> from, Stack<PlanState> to)
        {
            if (from.Count == 0) return;
            to.Push(_plan.Snapshot());
            _plan.Restore(from.Pop());
            _whatIfWarning = null;
            ClearSuggestion();
            UpdateUndo();
            RefreshWhatIf();
        }

        private void UpdateUndo()
        {
            _undoButton.Enabled = _undo.Count > 0;
        }

        /// <summary>Ctrl+Z and Ctrl+Y (or Ctrl+Shift+Z), when the host sees them.  False for other keys, and while a cell is being typed in.</summary>
        public bool ProcessUndoKey(Keys keyData)
        {
            if (_whatIfGrid.IsCurrentCellInEditMode) return false;
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    Undo();
                    return true;

                case Keys.Control | Keys.Y or Keys.Control | Keys.Shift | Keys.Z:
                    Redo();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Preview just these jobs, ticking the grid to match</summary>
        /// <summary>The shortest expected duration of a job listed and previewed, in seconds - for Auto, a 2000th of the preview's range, rounded down to a step: 5 minutes for a week, 30 seconds for a day</summary>
        private double EffectiveMinDurationSec()
        {
            if (_minDuration is { } chosen) return chosen.TotalSeconds;
            var (from, to) = PreviewRange();
            var limit = (to - from).TotalSeconds / 2000;
            return DurationSteps.Last(d => d.TotalSeconds <= limit).TotalSeconds;
        }

        /// <summary>The job is long enough to list and preview (<see cref="EffectiveMinDurationSec"/>) - or how long it takes isn't known</summary>
        private bool LongEnough(JobScheduleInput job) => job.ExpectedDurationSec is not { } seconds || seconds >= EffectiveMinDurationSec();

        /// <summary>
        /// Lists only the jobs long enough, and previews only those: one that's left off is unticked.  Until the user picks
        /// jobs to preview, every job listed is previewed.
        /// </summary>
        private void ApplyDurationFilter()
        {
            if (_whatIfTable == null) return;
            var min = EffectiveMinDurationSec();
            _whatIfTable.DefaultView.RowFilter = min <= 0 ? "" : $"ExpectedSec IS NULL OR ExpectedSec >= {min.ToString(CultureInfo.InvariantCulture)}";
            var hidden = _plan.Jobs.Count(j => !LongEnough(j));
            var shown = min <= 0 ? "all durations" : JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(min)) + " or longer";
            DurationFilterText = min <= 0 ? "All jobs" : $"Jobs {shown}" + (_minDuration == null ? " (auto)" : "");
            DurationFilterDetail = hidden == 0 ? "Every job is listed." : $"{hidden} shorter {(hidden == 1 ? "job is" : "jobs are")} left off the plan.";
            DurationFilterChanged?.Invoke(this, EventArgs.Empty);
            var included = _includedByUser
                ? _plan.Jobs.Where(j => _plan.Included.Contains(j.JobId) && LongEnough(j)).Select(j => j.JobId).ToList()
                : _plan.Jobs.Where(LongEnough).Select(j => j.JobId).ToList();
            // The same jobs ticked keep the last suggestion
            if (_plan.Included.SetEquals(included)) RefreshWhatIf();
            else SetIncluded(included);
        }

        private void SetIncluded(IEnumerable<Guid> jobIds)
        {
            if (_whatIfTable == null) return;
            var ids = jobIds.ToHashSet();
            _plan.Included.Clear();
            _plan.Included.UnionWith(ids);
            _whatIfGrid.EndEdit();
            _updatingWhatIf = true;
            try
            {
                foreach (DataRow row in _whatIfTable.Rows) row["Include"] = ids.Contains((Guid)row["job_id"]);
            }
            finally
            {
                _updatingWhatIf = false;
            }
            ClearSuggestion();
            RefreshWhatIf();
        }

        private static void SetHeader(DataGridView grid, string column, string header, string tooltip)
        {
            grid.Columns[column].HeaderText = header;
            grid.Columns[column].ToolTipText = tooltip;
        }

        private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value is null or DBNull) return;
            var name = grid.Columns[e.ColumnIndex].Name;
            if (OverlapColumns.TryGetValue(name, out var changeColumn))
            {
                // The time, and how the moves change it: more in red, less in green
                var seconds = Convert.ToDouble(e.Value);
                var change = grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var row } && row[changeColumn] is double c ? c : 0;
                var changeText = FormatChange(change);
                e.Value = (seconds < 0.5 ? (Math.Abs(change) < 0.5 ? "" : "none") : JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(seconds))) +
                          (changeText.Length == 0 ? "" : $" ({changeText})");
                if (Math.Abs(change) >= 0.5) e.CellStyle.ForeColor = change > 0 ? DashColors.Fail : DashColors.Success;
                e.FormattingApplied = true;
                return;
            }
            if (name == "BusyRuns")
            {
                // Runs in busy hours, and how the moves change it: more in red, fewer in green
                var runs = Convert.ToInt32(e.Value);
                var change = grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var busyRow } && busyRow["BusyRunsChange"] is int c ? c : 0;
                e.Value = (runs == 0 ? (change == 0 ? "" : "none") : runs.ToString("N0")) + (change == 0 ? "" : $" ({(change > 0 ? "+" : "")}{change:N0})");
                if (change != 0) e.CellStyle.ForeColor = change > 0 ? DashColors.Fail : DashColors.Success;
                e.FormattingApplied = true;
                return;
            }
            // Jobs worth a second look - the pairs' notes have no such flag
            if (name == "Note" && grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var noteRow } && noteRow.Table.Columns.Contains("Warn") && noteRow["Warn"] is true)
            {
                e.CellStyle.ForeColor = DashColors.Warning;
                return;
            }
            if (!DurationColumns.Contains(name)) return;
            var duration = Convert.ToDouble(e.Value);
            e.Value = duration == 0 ? "" : JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(duration));
            e.FormattingApplied = true;
        }

        /// <summary>The detail behind a cell (<see cref="DetailColumns"/>): both overlap measures before and after the moves, a job's schedules, its durations</summary>
        private static void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || !DetailColumns.TryGetValue(grid.Columns[e.ColumnIndex].Name, out var detailColumn)) return;
            if (grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var row } && row.Table.Columns.Contains(detailColumn) && row[detailColumn] is string detail) e.ToolTipText = detail;
        }

        private void ApplyShift(JobScheduleInput job, int seconds)
        {
            string warning = null;
            // A job sharing a schedule moves on its own, with a copy of the schedule, unless the shared schedules are to move together
            Change(() => warning = _plan.TryMoveJob(job, seconds, _splitShared, out var reason) ? null : $"{job.Name} can't be moved by {ScheduleMath.Shift(seconds)}.  {reason}.");
            if (warning == null) return;
            _whatIfWarning = warning;
            RefreshWhatIf();
        }

        /// <summary>
        /// A run dragged: its own schedule moves, when the job has several that move and Moving a Run is Its Schedule -
        /// otherwise the whole job, its schedules keeping any distance they've been moved apart
        /// </summary>
        private void Preview_RowShifted(object sender, RowShift e)
        {
            if (RunOf(e.Bar) is not { } p) return;
            var job = p.Job;
            var delta = (int)Math.Round(e.Delta.TotalSeconds);
            string warning = null;
            if (!MovesWholeJob(job))
            {
                if (DragSchedule(p) is not { } schedule)
                {
                    _whatIfWarning = $"That run of {job.Name} doesn't move: {SchedulePlan.NotMovedReason(job, p.Run)?.ToLowerInvariant() ?? "not started by a schedule that moves"}.  Drag a run one of its schedules started, or set Moving a Run to Whole Job.";
                    RefreshWhatIf();
                    return;
                }
                var seconds = _plan.ScheduleShift(job, schedule) + delta;
                Change(() => warning = _plan.TryMoveSchedule(job, schedule, seconds, _splitShared, out var reason) ? null : $"Schedule {schedule.Name} of {job.Name} can't be moved by {ScheduleMath.Shift(seconds)}.  {reason}.");
            }
            else
            {
                Change(() => warning = _plan.TryMoveJobBy(job, delta, _splitShared, out var reason) ? null : $"{job.Name} can't be moved by {ScheduleMath.Shift(delta)} more.  {reason}.");
            }
            if (warning == null) return;
            _whatIfWarning = warning;
            RefreshWhatIf();
        }

        /// <summary>A drag of one of the job's runs moves all its schedules: it has only one that moves, or Moving a Run is Whole Job</summary>
        private bool MovesWholeJob(JobScheduleInput job) => _dragWholeJob || SchedulePlan.MovableSchedules(job).Count < 2;

        /// <summary>The schedule a drag of the run moves on its own - null when the drag moves the whole job, or the run's schedule doesn't move</summary>
        private AgentSchedule DragSchedule(PreviewRun p) =>
            !MovesWholeJob(p.Job) && p.Run.Schedule is { } s && AgentScheduleShifter.IsMovable(s) ? s : null;

        /// <summary>Dragging <paramref name="grabbed"/> moves <paramref name="other"/>, a run of the same job: one of its schedules that moves, the same one as the grabbed run's unless the drag moves the whole job</summary>
        private bool MovesWith(PreviewRun grabbed, PreviewRun other)
        {
            if (other.Run.Schedule is not { } schedule || !AgentScheduleShifter.IsMovable(schedule)) return false;
            if (MovesWholeJob(grabbed.Job)) return true;
            return DragSchedule(grabbed) is { } s && ReferenceEquals(s, schedule);
        }

        /// <summary>The previewed run a bar of the plan's draws - null for another bar, e.g. the timeline's while a plan loads</summary>
        private PreviewRun RunOf(TimelineBar bar)
        {
            if (bar == null || bar.SourceIndex < 0 || bar.SourceIndex >= _previewRuns.Count) return null;
            var p = _previewRuns[bar.SourceIndex];
            return p.Job.JobId == bar.JobId && Display(p.Run.Start) == bar.Start ? p : null;
        }

        private void BuildWhatIfTable(ScheduleAnalysis analysis)
        {
            var summaries = analysis.Report.Jobs.ToDictionary(j => j.Job.JobId);
            var categories = analysis.JobRows.ToDictionary(r => (Guid)r["job_id"], r => r["category"] as string);

            var dt = new DataTable();
            dt.Columns.Add("job_id", typeof(Guid));
            dt.Columns.Add("Include", typeof(bool));
            dt.Columns.Add("Keep", typeof(bool));
            dt.Columns.Add("OwnSchedule", typeof(bool));
            // The move and what it does first, beside the job; the details after
            dt.Columns.Add("Job", typeof(string));
            dt.Columns.Add("Shift", typeof(string));
            dt.Columns.Add("OverlapSec", typeof(double));
            dt.Columns.Add("StartTime", typeof(string));
            dt.Columns.Add("EndTime", typeof(string));
            dt.Columns.Add("CycleSec", typeof(long));
            dt.Columns.Add("ExpectedSec", typeof(double));
            dt.Columns.Add("OverlapChangeSec", typeof(double));
            dt.Columns.Add("OverlapDetail", typeof(string));
            dt.Columns.Add("JobDetail", typeof(string));
            dt.Columns.Add("DurationDetail", typeof(string));
            dt.Columns.Add("Warn", typeof(bool));
            dt.Columns.Add("Note", typeof(string));
            // After the others, so the rows below fill the columns before them in order; shown beside Overlap
            dt.Columns.Add("BusyRuns", typeof(int));
            dt.Columns.Add("BusyRunsChange", typeof(int));
            dt.Columns.Add("BusyDetail", typeof(string));
            foreach (var job in _plan.Jobs.OrderBy(j => _plan.Included.Contains(j.JobId) ? 0 : 1).ThenBy(j => j.Name))
            {
                var notes = new List<string>();
                var summary = summaries.GetValueOrDefault(job.JobId);
                var movable = SchedulePlan.MovableSchedules(job);
                if (analysis.ParseErrors.TryGetValue(job.JobId, out var parseError)) notes.Add(parseError);
                else if (movable.Count == 0) notes.Add("Can't be moved: " + (summary?.NotProjectableReason ?? "none of its schedules run at a time of day"));
                else if (job.ExpectedDurationSec == null) notes.Add("No successful runs in the last 14 days - shown as 1 second, and its overlap is judged by the runs that happened");
                if (summary is { ProjectionTruncated: true }) notes.Add($"Runs so often that only the first {JobOverlapAnalyzer.MaxRunsPerJob:N0} are predicted");
                if (movable.Count > 1) notes.Add($"{movable.Count} schedules ({string.Join(", ", movable.Select(s => s.Name))}): dragging a run moves the schedule that started it (Moving a Run: Whole Job to move them all), and Move By moves them all");
                foreach (var schedule in job.Schedules.Where(s => s.Enabled && !AgentScheduleShifter.IsMovable(s)))
                {
                    notes.Add($"Schedule {schedule.Name} {SchedulePlan.FixedReason(schedule)}: it isn't moved, nor are the runs it starts");
                }
                // Before its schedules changed, the runs that happened aren't what they'd do now
                if (ScheduleChanged(job)) notes.Add(DescribeChange(_plan.ScheduleChangeOf(job)));
                // Runs that stay where they ran when the job moves, and why
                var staying = _plan.ActualRuns(job, moved: false, _rangeFrom, _rangeTo)
                    .Where(r => !r.Predicted && (r.Schedule == null || !AgentScheduleShifter.IsMovable(r.Schedule)))
                    .GroupBy(r => SchedulePlan.DescribeStart(r) ?? "not matched to a schedule")
                    .Select(g => $"{g.Key} {g.Count():N0}")
                    .ToList();
                if (staying.Count > 0 && movable.Count > 0) notes.Add($"Runs that stay where they ran when it moves: {string.Join(", ", staying)}");
                foreach (var schedule in job.Schedules.Where(_plan.IsShared))
                {
                    var others = _plan.SharedWith(job, schedule);
                    notes.Add($"Shares schedule {schedule.Name} with {string.Join(", ", others)}, so they start together");
                }
                _baseNotes[job.JobId] = notes.Count == 0 ? null : string.Join(". ", notes);
                dt.Rows.Add(job.JobId, _plan.Included.Contains(job.JobId), _plan.Pinned.Contains(job.JobId), _plan.SharesSchedule(job) ? _plan.OwnSchedule.Contains(job.JobId) : DBNull.Value,
                    job.Name, null, DBNull.Value, null, null, summary?.Cycle?.PeriodSec, job.ExpectedDurationSec, DBNull.Value, DBNull.Value,
                    JobDetail(job, categories.GetValueOrDefault(job.JobId)), DurationDetail(analysis.Stats.GetValueOrDefault(job.JobId), summary?.BusyFraction),
                    false, _baseNotes[job.JobId]);
            }

            _updatingWhatIf = true;
            try
            {
                _whatIfTable = dt;
                _whatIfGrid.DataSource = null;
                _whatIfGrid.Columns.Clear();
                _whatIfGrid.DataSource = dt;
                foreach (DataGridViewColumn c in _whatIfGrid.Columns) c.ReadOnly = c.Name is not ("Include" or "Keep" or "OwnSchedule" or "Shift");
                SetHeader(_whatIfGrid, "Include", "Preview", "Show the job's runs on the timeline and count its overlaps.  Click the header to tick or clear every job.");
                SetHeader(_whatIfGrid, "Keep", "Keep", "Leave the job where it is when suggesting start times");
                _whatIfGrid.Columns["Keep"].Width = LogicalToDeviceUnits(50);
                SetHeader(_whatIfGrid, "OwnSchedule", "Own Schedule",
                    "For a job that shares a schedule with other jobs: give it a copy of its own, so it can start at a different time.  The script adds the copy and the rollback removes it.  " +
                    "Ticked for a job when it's moved on its own (Moving Shared Jobs: Give Own Copy); untick it to put the job back on the shared schedule.");
                _whatIfGrid.Columns["OwnSchedule"].Width = LogicalToDeviceUnits(70);
                SetHeader(_whatIfGrid, "CycleSec", "Repeats Every", "Interval the job repeats at, when it has a single schedule that repeats at a fixed interval");
                SetHeader(_whatIfGrid, "ExpectedSec", "Duration", "Expected duration (see the Duration button)");
                SetHeader(_whatIfGrid, "Shift", "Move By", "How far to move the job's runs: 2m, -30s, 1h 15m or 00:02:30.  A number on its own is minutes.  Moves all the job's schedules - or drag one of its runs on the timeline to move the schedule that started it.  A schedule that repeats all day runs at the same times moved by a whole interval, so a move is shown as the smallest that does the same: every 15 minutes, +20m is +5m.");
                SetHeader(_whatIfGrid, "StartTime", "Starting At (server)", "When each schedule starts after the move, in server time - the ones that run at a time of day, which all move together.  For one that runs once a day, the time it runs.");
                SetHeader(_whatIfGrid, "EndTime", "Ending At (server)", "When each schedule that repeats through the day stops after the move, in server time.  A move takes the end with it, unless the schedule runs all day.  Blank (or -) for one that runs once a day.");
                SetHeader(_whatIfGrid, "OverlapSec", "Overlap", "Time the job's schedules predict it would overlap other previewed jobs over the selected range, moved as planned, and how the moves change it: more in red, less in green.  " +
                                                               "Judged by the runs that happened for a job with no expected duration.  Hover for both measures.");
                SetHeader(_whatIfGrid, "Note", "Note", "What to know about the job.  ⚠ in amber: worth a second look before scripting - its own copy of a shared schedule, several schedules moving together, or schedules that changed during the range.");
                _whatIfGrid.Columns["Include"].Width = LogicalToDeviceUnits(60);
                _whatIfGrid.Columns["Job"].Width = LogicalToDeviceUnits(220);
                _whatIfGrid.Columns["OverlapSec"].Width = LogicalToDeviceUnits(110);
                SetHeader(_whatIfGrid, "BusyRuns", "Busy Hours", "Runs in busy hours over the selected range, moved as planned, and how the moves change it: more in red, fewer in green.  " +
                                                                 "Only for jobs that could run outside them - daily, weekly, or repeating less often than the longest busy stretch.  Busy Hours... on the toolbar sets them.");
                _whatIfGrid.Columns["BusyRuns"].DisplayIndex = _whatIfGrid.Columns["OverlapSec"].DisplayIndex + 1;
                _whatIfGrid.Columns["BusyRuns"].Width = LogicalToDeviceUnits(80);
                _whatIfGrid.Columns["Shift"].DefaultCellStyle.Font = new Font(_whatIfGrid.Font, FontStyle.Bold);
                // No MinimumWidth: setting it before the tab has been shown throws inside DataGridViewBand
                _whatIfGrid.Columns["Note"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            finally
            {
                _updatingWhatIf = false;
            }
        }

        /// <summary>The Job cell's tooltip: its category and its schedules, as SQL Server Agent describes them</summary>
        private static string JobDetail(JobScheduleInput job, string category)
        {
            var text = new StringBuilder(job.Name);
            if (!string.IsNullOrEmpty(category)) text.Append($" ({category})");
            if (!job.Enabled) text.Append(" - disabled");
            foreach (var s in job.Schedules) text.AppendLine().Append($"{s.Name}: {(s.Enabled ? "" : "(disabled) ")}{s.Describe()}");
            return text.ToString();
        }

        /// <summary>
        /// The Duration cell's tooltip: average, 95th percentile and longest of the job's successful runs - those a schedule
        /// started, or every run for a job no schedule started - and how busy that keeps it
        /// </summary>
        private static string DurationDetail(DataRow stats, double? busy)
        {
            if (stats == null) return "No successful runs in the last 14 days";
            string Time(object seconds) => seconds is DBNull or null ? "-" : JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(Convert.ToDouble(seconds)));
            var runs = Convert.ToInt32(stats["Runs"]);
            var scheduled = stats["FromScheduled"] is true;
            var other = stats["OtherRuns"] is int o ? o : 0;
            var text = $"{runs:N0} successful {(scheduled ? "scheduled " : "")}{(runs == 1 ? "run" : "runs")} over the last 14 days: average {Time(stats["AvgSec"])}, 95th percentile {Time(stats["P95Sec"])}, longest {Time(stats["MaxSec"])}";
            if (scheduled && other > 0) text += Environment.NewLine + $"Left out: {other:N0} {(other == 1 ? "run" : "runs")} not started by a schedule - by hand, an alert or agent start";
            if (!scheduled) text += Environment.NewLine + "No runs started by a schedule, so these are the runs started otherwise - by hand, an alert or agent start";
            return busy is { } b ? text + Environment.NewLine + $"Expected to be running {b:P1} of the time" : text;
        }

        private void WhatIfGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_updatingWhatIf || e.RowIndex < 0 || _whatIfGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView view) return;
            var column = _whatIfGrid.Columns[e.ColumnIndex].Name;
            var job = _plan.Jobs.FirstOrDefault(j => j.JobId == (Guid)view["job_id"]);
            if (job == null) return;
            if (column == "Keep")
            {
                if (view["Keep"] is true) _plan.Pinned.Add(job.JobId);
                else _plan.Pinned.Remove(job.JobId);
            }
            else if (column == "OwnSchedule")
            {
                Change(() => _plan.SetOwnSchedule(job, view["OwnSchedule"] is true));
            }
            else if (column == "Include")
            {
                _includedByUser = true;
                if (view["Include"] is true) _plan.Included.Add(job.JobId);
                else _plan.Included.Remove(job.JobId);
                ClearSuggestion();
                RefreshWhatIf();
            }
            else if (column == "Shift")
            {
                if (TryParseShift(view["Shift"] as string, out var seconds))
                {
                    ApplyShift(job, seconds);
                }
                else
                {
                    _whatIfWarning = $"Couldn't read \"{view["Shift"]}\" as a move.  Try 2m, -30s, 1h 15m or 00:02:30.";
                    RefreshWhatIf();
                }
            }
        }

        /// <summary>The job's schedules changed during the selected range, so its runs before then are predicted</summary>
        private bool ScheduleChanged(JobScheduleInput job) => _plan.ScheduleChangeOf(job) is { } change && change.Until > _rangeFrom;

        /// <summary>
        /// A pair's overlap as the plan measures it: as the schedules predict it, or in the runs that happened when either job
        /// has no expected duration to predict with
        /// </summary>
        private static (double After, double Before, int Count, bool Predicted) Lead(PlanPairOverlap p) =>
            p.Predicted is { } after ? (after.Seconds, p.PredictedBefore?.Seconds ?? 0, after.Count, true) : (p.Actual.Seconds, p.ActualBefore.Seconds, p.Actual.Count, false);

        /// <summary>Both measures of an overlap, before and after the moves, for a tooltip</summary>
        private static string OverlapDetail(double predicted, double predictedBefore, bool predictable, double ran, double ranBefore, bool anyMoves)
        {
            string Amount(double after, double before) => Format(after) + (anyMoves && Math.Abs(after - before) >= 0.5 ? $", was {Format(before)}" : "");
            var text = new StringBuilder();
            text.AppendLine(predictable ? $"As the schedules predict over the range: {Amount(predicted, predictedBefore)}" : "Not predicted: no expected duration");
            text.Append($"In the runs that happened: {Amount(ran, ranBefore)}");
            return text.ToString();
        }

        /// <summary>The range the chart previews: the selected range's runs, or the schedules' predictions from midnight today</summary>
        private (DateTime From, DateTime To) PreviewRange()
        {
            if (PreviewActual) return (_rangeFrom, _rangeTo);
            var today = DateTime.UtcNow.AddMinutes(-_whatIfUtcOffset).Date;
            return (today, today + _previewLength);
        }

        /// <summary>Work the preview out again with the ticked jobs and their moves</summary>
        private void RefreshWhatIf()
        {
            if (_whatIfTable == null) return;
            var (from, to) = PreviewRange();
            var included = _plan.IncludedJobs;
            var names = included.Select(j => j.Name).ToList();
            var runs = included.Select(j => PreviewRunsOf(j, moved: true, from, to)).ToList();
            // The jobs as moved, and their cycles - for whether start times keep each pair apart
            var shifted = included.Select(_plan.ShiftedJob).ToList();
            var cycles = shifted.Select(JobOverlapAnalyzer.GetCycle).ToList();
            // Overlaps between jobs no start times can keep apart are drawn faintly: what's picked out is what a move could change
            // Only for jobs that run all day: two hourly jobs running 08:00-10:00 could be kept apart by moving one's window
            bool Unavoidable(int a, int b) =>
                cycles[a] is { AllDay: true } ca && cycles[b] is { AllDay: true } cb && shifted[a].ExpectedDurationSec is { } da && shifted[b].ExpectedDurationSec is { } db
                && JobOverlapAnalyzer.CompareCycles(ca, da, cb, db, out _) is CycleVerdict.CannotSeparate or CycleVerdict.AlwaysRunning;
            var overlaps = SchedulePlan.FindOverlaps(runs, names, Unavoidable);
            var anyMoves = included.Any(_plan.IsMoved);
            var before = anyMoves ? OverlapsBeforeMoves(included, names, from, to) : overlaps.Count;
            // Which pairs overlap over the selected range - in what ran and what the schedules predict - with the moves and before
            var pairs = _plan.PairOverlaps(included, _rangeFrom, _rangeTo);
            // Jobs whose own schedules the moves put in each other's way, for their notes and the cautions
            _clashes = _plan.Jobs.Where(_plan.IsMoved)
                .Select(j => (j.JobId, Count: _plan.ClashesAdded(j, _rangeFrom, _rangeTo)))
                .Where(x => x.Count > 0)
                .ToDictionary(x => x.JobId, x => x.Count);
            FindMovedIntoBusy();

            UpdateJobRows(included, pairs, anyMoves);
            FillPairs(pairs, anyMoves, included, shifted, cycles);
            ShowNetEffect(included, pairs, anyMoves);
            ShowRuns(included, runs, overlaps, from, to, anyMoves);
            _whatIfSummary.Text = SummaryText(included, shifted, cycles, from, to, overlaps.Count, before, anyMoves, overlaps.Count - overlaps.AvoidableCount);
            _whatIfSummary.ForeColor = _whatIfWarning == null ? ForeColor : DashColors.Fail;
            ShowCautions();
        }

        /// <summary>
        /// The days a weekly schedule runs on, before its start time - a move past midnight changes them (Sun 00:00 three
        /// hours earlier is Sat 21:00).  Empty for one that runs every day, or isn't weekly.
        /// </summary>
        private static string WeekDays(AgentSchedule s) =>
            s.FrequencyType != AgentSchedule.Weekly || AgentScheduleShifter.IsEveryDay(s) ? string.Empty
                : string.Join("/", Enumerable.Range(0, 7).Where(d => (s.FrequencyInterval & (1 << d)) != 0)
                    .Select(d => CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[d])) + " ";

        /// <summary>Each job's row: its move, its schedules' times after it, its overlap and its note</summary>
        private void UpdateJobRows(List<JobScheduleInput> included, List<PlanPairOverlap> pairs, bool anyMoves)
        {
            _updatingWhatIf = true;
            try
            {
                foreach (DataRow r in _whatIfTable.Rows)
                {
                    var job = _plan.Jobs.First(j => j.JobId == (Guid)r["job_id"]);
                    r["Shift"] = _plan.DescribeShift(job);
                    // Each schedule the move changes, as they all move together: starting at, and ending at for one that
                    // repeats through the day (one that runs once a day has no end - a dash keeps the schedules in step)
                    var moving = SchedulePlan.MovableSchedules(_plan.ShiftedJob(job));
                    r["StartTime"] = moving.Count == 0 ? DBNull.Value : string.Join(", ", moving.Select(s => WeekDays(s) + s.StartTime.ToString(@"hh\:mm\:ss")));
                    r["EndTime"] = moving.All(s => s.SubdayStepSeconds == 0) ? DBNull.Value
                        : string.Join(", ", moving.Select(s => s.SubdayStepSeconds > 0 ? s.EndTime.ToString(@"hh\:mm\:ss") : "-"));
                    if (included.Contains(job))
                    {
                        var mine = pairs.Where(p => p.A == job || p.B == job).ToList();
                        r["OverlapSec"] = mine.Sum(p => Lead(p).After);
                        r["OverlapChangeSec"] = anyMoves ? mine.Sum(p => Lead(p).After - Lead(p).Before) : 0;
                        r["OverlapDetail"] = OverlapDetail(mine.Sum(p => p.Predicted?.Seconds ?? 0), mine.Sum(p => p.PredictedBefore?.Seconds ?? 0), job.ExpectedDurationSec.HasValue,
                            mine.Sum(p => p.Actual.Seconds), mine.Sum(p => p.ActualBefore.Seconds), anyMoves);
                    }
                    else
                    {
                        r["OverlapSec"] = DBNull.Value;
                        r["OverlapChangeSec"] = DBNull.Value;
                        r["OverlapDetail"] = DBNull.Value;
                    }
                    SetBusyCells(r, job, included.Contains(job), anyMoves);
                    if (r["OwnSchedule"] != DBNull.Value) r["OwnSchedule"] = _plan.OwnSchedule.Contains(job.JobId);
                    var warn = Warns(job);
                    r["Warn"] = warn;
                    r["Note"] = JobNote(job.JobId) is { } note ? (warn ? "⚠ " : "") + note : DBNull.Value;
                }
            }
            finally
            {
                _updatingWhatIf = false;
            }
        }

        /// <summary>Worth a second look before scripting: an own copy of a shared schedule, its own schedules moved so they clash, or schedules changed during the range</summary>
        private bool Warns(JobScheduleInput job) =>
            _plan.OwnSchedule.Contains(job.JobId)
            || _clashes.ContainsKey(job.JobId)
            || _movedIntoBusy.Contains(job.JobId)
            || PreviewActual && _plan.Included.Contains(job.JobId) && ScheduleChanged(job);

        /// <summary>The runs on the chart: coloured by outcome, overlaps picked out, outlines where moved runs were, and the legend</summary>
        /// <summary>What <see cref="ShowRuns"/> was last given, to make the bars again for another part of the range as the view moves</summary>
        private (List<JobScheduleInput> Included, List<IReadOnlyList<JobRun>> Runs, PlanOverlaps Overlaps, DateTime From, DateTime To, bool AnyMoves) _shown;

        /// <summary>The part of the range, in server time, the bars were made for, and the seconds a pixel stood for then</summary>
        private (DateTime From, DateTime To, double SecondsPerPixel) _barsFor;

        /// <summary>A band standing for many runs too close together to draw apart, by the <see cref="TimelineBar.SourceIndex"/> of its bar</summary>
        /// <param name="JobIndex">The job's place in the jobs previewed</param>
        /// <param name="First">The band's first run's place in the job's runs</param>
        /// <param name="Last">The place after its last</param>
        private sealed record DenseSpan(int Runs, DateTime Start, DateTime End, double BusySec, double EverySec, int JobIndex, int First, int Last)
        {
            /// <summary>The jobs its runs overlap, and for how long - a move could change, or not - worked out when first asked for</summary>
            public (IReadOnlyCollection<string> With, double Seconds, double UnavoidableSec) Overlaps(PlanOverlaps overlaps)
            {
                if (_overlaps is { } known) return known;
                var (with, segments, unavoidable) = overlaps.Detail(JobIndex, First, Last);
                var names = new HashSet<string>();
                foreach (var w in with) if (w != null) names.UnionWith(w);
                static double Seconds(List<(DateTime Start, DateTime End)>[] runs) => runs.Where(r => r != null).SelectMany(r => r).Sum(o => (o.End - o.Start).TotalSeconds);
                return (_overlaps = (names, Seconds(segments), Seconds(unavoidable))).Value;
            }

            private (IReadOnlyCollection<string>, double, double)? _overlaps;
        }

        private readonly Dictionary<int, DenseSpan> _denseSpans = new();

        /// <summary>Runs closer together than this many pixels are drawn as a band (<see cref="TimelineBar.IsDense"/>)</summary>
        private const float DenseGapPixels = 6;

        /// <summary>Waits for the view to settle before making bars for it</summary>
        private readonly Timer _viewTimer = new() { Interval = 150 };

        private void ShowRuns(List<JobScheduleInput> included, List<IReadOnlyList<JobRun>> runs, PlanOverlaps overlaps, DateTime from, DateTime to, bool anyMoves)
        {
            _shown = (included, runs, overlaps, from, to, anyMoves);
            if (_preview == null) return;
            // The outcomes there are, counted as on the timeline, then the overlaps - over the whole range, not just what's in view
            var statusCounts = new Dictionary<TimelineRow.RunStatus, int>();
            var (overlapping, unavoidableOnly) = (0, 0);
            for (var i = 0; i < runs.Count; i++)
            {
                for (var k = 0; k < runs[i].Count; k++)
                {
                    var status = ShownStatus(runs[i][k]);
                    statusCounts[status] = statusCounts.GetValueOrDefault(status) + 1;
                    var flags = overlaps.Flags[i][k];
                    if ((flags & PlanOverlaps.Avoidable) != 0) overlapping++;
                    else if (flags != 0) unavoidableOnly++;
                }
            }
            var legend = statusCounts
                .OrderBy(c => c.Key switch { TimelineRow.RunStatus.Succeeded => 0, TimelineRow.RunStatus.Failed => 1, _ => 2 })
                .Select(c => new LegendEntry(TimelineBar.StatusColor(c.Key), c.Key == TimelineRow.RunStatus.Predicted ? $"{c.Value:N0} predicted" : c.Value.ToString("N0")))
                .ToList();
            legend.Add(new LegendEntry(TimelineBar.HighlightColor, $"{overlapping:N0} overlap"));
            if (unavoidableOnly > 0) legend.Add(new LegendEntry(TimelineBar.FaintHighlightColor, $"{unavoidableOnly:N0} unavoidable"));
            // The dotted outline where a moved run was is explained in the summary: there isn't room for it here too
            if (anyMoves) legend.Add(new LegendEntry(TimelineBar.StatusColor(PreviewActual ? TimelineRow.RunStatus.Succeeded : TimelineRow.RunStatus.Predicted), "moved", LegendSwatch.Striped));
            // Runs of jobs that could keep clear of busy hours that are in them: outlined in amber
            var inBusy = FlagBusyRuns(included, runs);
            if (inBusy > 0) legend.Add(new LegendEntry(TimelineBar.BusyOutlineColor, $"{inBusy:N0} in busy hours", LegendSwatch.Dotted));
            ShowBusyOnChart();
            _preview.LegendEntries = legend;
            _preview.Title = (PreviewActual ? "Plan: actual runs" : "Plan: predicted runs") + (ShowAppTimeZone ? "" : " (server time)");
            _preview.SetData(BuildPreviewModel(), Display(from), Display(to), _resetPreviewView);
            var (edges, peaks) = SchedulePlan.Concurrency(runs.SelectMany(r => r), from, to);
            _preview.SetConcurrency(edges.Select(Display).ToArray(), peaks);
            _resetPreviewView = false;
        }

        /// <summary>
        /// The part of the range to make bars for, in server time - the time in view and as much again either side, so a pan
        /// shows bars while the view settles - and the seconds a pixel stands for at the zoom
        /// </summary>
        private (DateTime From, DateTime To, double SecondsPerPixel) BarsWindow()
        {
            var (from, to) = (_shown.From, _shown.To);
            var shownFrom = Display(from);
            var viewFrom = _preview.ViewFrom;
            var viewTo = _preview.ViewTo;
            if (_resetPreviewView || viewTo <= viewFrom || viewTo <= shownFrom || viewFrom >= Display(to))
            {
                viewFrom = shownFrom;
                viewTo = Display(to);
            }
            var span = viewTo - viewFrom;
            // The view is in the time shown; the runs in server time - each end mapped with its own offset, which a clock
            // change in the range makes differ by an hour, more than the margin at a close zoom
            var windowFrom = ServerTime(viewFrom) - span;
            var windowTo = ServerTime(viewTo) + span;
            var secondsPerPixel = span.TotalSeconds / Math.Max(200, _preview.ChartPixelWidth);
            return (windowFrom < from ? from : windowFrom, windowTo > to ? to : windowTo, secondsPerPixel);
        }

        /// <summary>
        /// The server time shown at <paramref name="shown"/> - the reverse of <see cref="Display"/>, found by applying the
        /// offset at the time it gives until it settles: the offset at the range's start, then at the time that gives, which
        /// is past any clock change between the two.  In the hour a clock change repeats or skips, a time either side.
        /// </summary>
        private DateTime ServerTime(DateTime shown)
        {
            var server = shown - (Display(_shown.From) - _shown.From);
            for (var i = 0; i < 2; i++) server = shown - (Display(server) - server);
            return server;
        }

        /// <summary>Makes the bars for the part of the range in view again, when the view has moved past them or the zoom has changed much</summary>
        private void ViewTimer_Tick(object sender, EventArgs e)
        {
            _viewTimer.Stop();
            if (_preview == null || _shown.Included == null) return;
            var window = BarsWindow();
            var view = (From: _preview.ViewFrom, To: _preview.ViewTo);
            var shownFrom = Display(_barsFor.From);
            var shownTo = Display(_barsFor.To);
            var zoomChanged = window.SecondsPerPixel > _barsFor.SecondsPerPixel * 1.5 || window.SecondsPerPixel < _barsFor.SecondsPerPixel / 1.5;
            // The bars made cover the view: the part they were made for (the view then, and its width either side) holds it
            var covered = view.From >= shownFrom && view.To <= shownTo
                          || (_barsFor.From <= _shown.From && _barsFor.To >= _shown.To);
            if (covered && !zoomChanged) return;
            _preview.SetData(BuildPreviewModel(), Display(_shown.From), Display(_shown.To));
        }

        private void Preview_ViewChanged(object sender, EventArgs e)
        {
            _viewTimer.Stop();
            _viewTimer.Start();
        }

        /// <summary>
        /// The preview's bars.  A job whose runs are mostly closer together than <see cref="DenseGapPixels"/> is drawn as bands
        /// rather than a bar per run - a week of a job that runs every 10 seconds is 60,000 runs, tens to a pixel - and they
        /// come back as the view zooms in.  Around the view (<see cref="BarsWindow"/>) that's judged at the zoom; outside it,
        /// at the zoom of the whole range, so the overview and a pan show the runs there too while the view settles.
        /// </summary>
        private JobTimelineModel BuildPreviewModel()
        {
            var (included, runs, overlaps, from, to, _) = _shown;
            var window = BarsWindow();
            _barsFor = window;
            var wholeRange = (to - from).TotalSeconds / Math.Max(200, _preview.ChartPixelWidth);
            var previewRuns = new List<PreviewRun>();
            var bars = new List<TimelineBar>();
            _denseSpans.Clear();
            for (var i = 0; i < included.Count; i++)
            {
                var (first, last) = SchedulePlan.RunsBetween(runs[i], window.From, window.To);
                AddBars(bars, previewRuns, included[i], runs[i], overlaps, i, 0, first, wholeRange, exact: false);
                AddBars(bars, previewRuns, included[i], runs[i], overlaps, i, first, last, window.SecondsPerPixel, exact: true);
                AddBars(bars, previewRuns, included[i], runs[i], overlaps, i, last, runs[i].Count, wholeRange, exact: false);
            }
            var denseGap = TimeSpan.FromSeconds(window.SecondsPerPixel * DenseGapPixels);
            _previewRuns = previewRuns;
            var model = JobTimelineModel.FromBars(bars, _instanceId);
            // Moved jobs say so beside their name, and by stripes on their runs
            foreach (var row in model.Roots)
            {
                var job = included.FirstOrDefault(j => j.JobId == row.JobId);
                if (job == null) continue;
                // Where the runs happened, before its schedules changed to what's shown - those close together as one outline
                if (PreviewActual)
                {
                    foreach (var r in _plan.ReplacedRuns(job, window.From, window.To))
                    {
                        var outline = (Start: Display(r.Start), End: Display(r.End), Status: (TimelineRow.RunStatus)(r.Status ?? (int)TimelineRow.RunStatus.Succeeded));
                        if (row.Outlines.Count > 0 && row.Outlines[^1] is var previous && previous.Status == outline.Status && outline.Start - previous.End < denseGap)
                        {
                            row.Outlines[^1] = (previous.Start, outline.End, previous.Status);
                        }
                        else
                        {
                            row.Outlines.Add(outline);
                        }
                    }
                }
                if (_plan.IsMoved(job) || _plan.OwnSchedule.Contains(job.JobId)) row.Label = $"{row.Label}  {_plan.DescribeShift(job)}" + (_plan.OwnSchedule.Contains(job.JobId) ? " (own schedule)" : "");
            }
            return model;
        }

        /// <summary>Bars for runs <paramref name="first"/> up to <paramref name="last"/> of job <paramref name="i"/>, as bands where they're too close together at <paramref name="secondsPerPixel"/></summary>
        /// <param name="exact">
        /// Pick out just when a run's own bar overlaps, as <see cref="PlanOverlaps.Detail"/> works out - around the view.
        /// Otherwise the whole of a run that overlaps, which is all that shows at the zoom of the whole range.  Bands are
        /// always picked out by run.
        /// </param>
        private void AddBars(List<TimelineBar> bars, List<PreviewRun> previewRuns, JobScheduleInput job, IReadOnlyList<JobRun> jobRuns, PlanOverlaps overlaps, int i, int first, int last, double secondsPerPixel, bool exact)
        {
            if (first >= last) return;
            var denseGap = TimeSpan.FromSeconds(secondsPerPixel * DenseGapPixels);
            var mergeGap = TimeSpan.FromSeconds(secondsPerPixel);
            var closeRuns = 0;
            for (var k = first; k + 1 < last; k++)
            {
                if (jobRuns[k + 1].Start - jobRuns[k].Start < denseGap) closeRuns++;
            }
            var dense = last - first >= 20 && closeRuns * 2 > last - first;
            var detail = exact && !dense ? overlaps.Detail(i, first, last) : default;
            var flags = overlaps.Flags[i];
            for (var k = first; k < last;)
            {
                var run = jobRuns[k];
                var movedBy = run.MovedBySec;
                var status = ShownStatus(run);
                var end = k + 1;
                // A band runs on while the runs are close, end the same way and moved the same - a failure among them is its own
                // short band, so it shows.  When dragging a run moves only the schedule that started it, a band keeps to one
                // schedule too: it's dragged as its first run.
                if (dense)
                {
                    var bySchedule = !MovesWholeJob(job);
                    while (end < last && jobRuns[end].Start - jobRuns[end - 1].End < denseGap && ShownStatus(jobRuns[end]) == status && jobRuns[end].MovedBySec == movedBy
                           && (!bySchedule || ReferenceEquals(jobRuns[end].Schedule, run.Schedule))) end++;
                }
                var index = previewRuns.Count;
                previewRuns.Add(new PreviewRun(job, run, i, k));
                if (end - k > 1)
                {
                    bars.Add(DenseBar(job, jobRuns, overlaps, i, k, end, status, index, mergeGap));
                }
                else
                {
                    bars.Add(new TimelineBar
                    {
                        InstanceId = _instanceId,
                        JobId = job.JobId,
                        JobName = job.Name,
                        StepName = "Outcome",
                        Start = Display(run.Start),
                        End = Display(run.End),
                        DurationSec = (int)run.Seconds,
                        // Coloured by how the run ended, as on the timeline - or as a prediction, for one the schedules predict
                        Status = status,
                        Executions = 1,
                        SourceIndex = index,
                        // The overlapping parts picked out - faintly where no move could avoid them
                        Highlights = detail.Segments != null ? detail.Segments[k - first]?.Select(s => (Display(s.Start), Display(s.End))).ToList()
                            : (flags[k] & PlanOverlaps.Avoidable) != 0 ? new[] { (Display(run.Start), Display(run.End)) } : null,
                        FaintHighlights = detail.Unavoidable != null ? detail.Unavoidable[k - first]?.Select(s => (Display(s.Start), Display(s.End))).ToList()
                            : (flags[k] & PlanOverlaps.Unavoidable) != 0 ? new[] { (Display(run.Start), Display(run.End)) } : null,
                        // Only the runs that moved get an outline where they were: one started by hand or at agent start stays put
                        GhostStart = movedBy == 0 ? null : Display(run.Start.AddSeconds(-movedBy)),
                        InBusyHours = RunInBusy(i, k)
                    });
                }
                k = end;
            }
        }

        /// <summary>A band for runs <paramref name="first"/> up to <paramref name="end"/> of job <paramref name="i"/>, the runs that overlap picked out - merged where they're less than a pixel apart</summary>
        private TimelineBar DenseBar(JobScheduleInput job, IReadOnlyList<JobRun> runs, PlanOverlaps overlaps, int i, int first, int end, TimelineRow.RunStatus status, int index, TimeSpan mergeGap)
        {
            var count = end - first;
            double busy = 0;
            var highlights = new List<(DateTime Start, DateTime End)>();
            var faint = new List<(DateTime Start, DateTime End)>();
            var flags = overlaps.Flags[i];
            for (var k = first; k < end; k++)
            {
                busy += runs[k].Seconds;
                if ((flags[k] & PlanOverlaps.Avoidable) != 0) Merge(highlights, runs[k], mergeGap);
                if ((flags[k] & PlanOverlaps.Unavoidable) != 0) Merge(faint, runs[k], mergeGap);
            }
            var start = runs[first].Start;
            var finish = runs[end - 1].End;
            var every = count > 1 ? (runs[end - 1].Start - start).TotalSeconds / (count - 1) : 0;
            var movedBy = runs[first].MovedBySec;
            _denseSpans[index] = new DenseSpan(count, start, finish, busy, every, i, first, end);
            var each = JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(busy / count));
            return new TimelineBar
            {
                InstanceId = _instanceId,
                JobId = job.JobId,
                JobName = job.Name,
                StepName = "Outcome",
                Start = Display(start),
                End = Display(finish),
                DurationSec = (int)Math.Round(busy / count),
                Status = status,
                Executions = count,
                SourceIndex = index,
                DenseLabel = $"every {JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(Math.Round(every)))}, {each} each",
                Highlights = highlights.Count == 0 ? null : highlights.Select(s => (Display(s.Start), Display(s.End))).ToList(),
                FaintHighlights = faint.Count == 0 ? null : faint.Select(s => (Display(s.Start), Display(s.End))).ToList(),
                GhostStart = movedBy == 0 ? null : Display(start.AddSeconds(-movedBy)),
                InBusyHours = AnyRunInBusy(i, first, end)
            };
        }

        /// <summary>Adds <paramref name="run"/> to <paramref name="into"/>, joined to the last if it's less than <paramref name="gap"/> after it</summary>
        private static void Merge(List<(DateTime Start, DateTime End)> into, JobRun run, TimeSpan gap)
        {
            if (into.Count > 0 && run.Start - into[^1].End < gap)
            {
                if (run.End > into[^1].End) into[^1] = (into[^1].Start, run.End);
            }
            else
            {
                into.Add((run.Start, run.End));
            }
        }

        /// <summary>
        /// The summary under the toolbar: what's previewed and how many runs overlap, what the fixed cycles allow, then a
        /// warning, the last suggestion, or how to use the plan
        /// </summary>
        private string SummaryText(List<JobScheduleInput> included, List<JobScheduleInput> shifted, List<JobCycle?> cycles, DateTime from, DateTime to, int overlaps, int before, bool anyMoves, int unavoidable = 0)
        {
            var (clear, collide, cannot, sharedPairs) = (0, 0, 0, 0);
            for (var i = 0; i < shifted.Count; i++)
            {
                for (var j = i + 1; j < shifted.Count; j++)
                {
                    // Jobs sharing a schedule start together whatever its start time
                    if (JobOverlapAnalyzer.SharedSchedule(shifted[i], shifted[j]) != null)
                    {
                        sharedPairs++;
                        continue;
                    }
                    if (cycles[i] is not { } a || cycles[j] is not { } b) continue;
                    if (shifted[i].ExpectedDurationSec is not { } da || shifted[j].ExpectedDurationSec is not { } db) continue;
                    switch (JobOverlapAnalyzer.CompareCycles(a, da, b, db, out _))
                    {
                        case CycleVerdict.Clear: clear++; break;
                        case CycleVerdict.Collides: collide++; break;
                        case CycleVerdict.CannotSeparate or CycleVerdict.AlwaysRunning: cannot++; break;
                    }
                }
            }

            var text = new StringBuilder();
            if (PeriodNote != null) text.Append(PeriodNote).Append("  ");
            var range = PreviewActual
                ? $"{Display(from).ToString("g", CultureInfo.CurrentCulture)} to {Display(to).ToString("g", CultureInfo.CurrentCulture)} {DisplayTimeName}, the runs that happened"
                : $"from {Display(from).ToString("g", CultureInfo.CurrentCulture)} {DisplayTimeName}, as the schedules predict";
            text.Append(included.Count == 0
                ? "Tick jobs below to preview their runs.  "
                : $"{included.Count} jobs, {range}: {overlaps:N0} overlapping pairs of runs" +
                  (anyMoves ? $" (was {before:N0} before the moves)" : "") +
                  (unavoidable > 0 ? $", {unavoidable:N0} of them between jobs no start times can keep apart (shown faintly).  " : ".  ") +
                  BusySummary(included, from, to, anyMoves));
            if (clear + collide + cannot > 0)
            {
                text.Append($"Of the pairs on fixed cycles, the start times keep {clear} apart, {collide} collide but could be moved apart and {cannot} can't be kept apart by start times alone.");
            }
            text.AppendLine();
            text.Append(_whatIfWarning ?? _suggestionText ?? (sharedPairs > 0
                ? $"{sharedPairs} {(sharedPairs == 1 ? "pair" : "pairs")} of previewed jobs share a schedule, so they start together.  " +
                  (_splitShared
                      ? "Moving one gives it its own copy of the schedule, so the others stay put (Moving Shared Jobs: Move All to move them all).  Hold Shift while dragging to move by the second."
                      : "Moving one moves them all (Moving Shared Jobs: Give Own Copy to move one on its own).  Hold Shift while dragging to move by the second.")
                : "Purple marks overlaps, and dotted outlines where moved runs were.  Drag a job's runs, or type in Move By, to try other start times - Ctrl+Z undoes.  Nothing changes on the server."));
            return text.ToString();
        }

        /// <summary>One line on what needs a second look, the jobs it's about marked ⚠ in their Note</summary>
        private void ShowCautions()
        {
            var cautions = new List<string>();
            var copies = _plan.Jobs.Count(j => _plan.OwnSchedule.Contains(j.JobId));
            if (copies > 0) cautions.Add($"{(copies == 1 ? "1 job gets its own copy" : $"{copies} jobs get their own copies")} of a shared schedule");
            if (_clashes.Count > 0) cautions.Add($"{(_clashes.Count == 1 ? "1 job has" : $"{_clashes.Count} jobs have")} schedules moved so they clash - Agent skips a start while the job is running");
            var changed = PreviewActual ? _plan.IncludedJobs.Count(ScheduleChanged) : 0;
            if (changed > 0) cautions.Add($"schedules changed during the range for {(changed == 1 ? "1 job" : $"{changed} jobs")} - before then its runs are predicted");
            if (_movedIntoBusy.Count > 0) cautions.Add($"{(_movedIntoBusy.Count == 1 ? "1 job that could avoid busy hours is" : $"{_movedIntoBusy.Count} jobs that could avoid busy hours are")} moved into them");
            if (_clockChange is { } clock)
            {
                // Until isn't corrected, so Display takes nothing off it
                cautions.Add($"the server's clocks changed {(clock.Until == DateTime.MaxValue ? "since this period" : $"at about {Display(clock.Until).ToString("g", CultureInfo.CurrentCulture)}")}, " +
                             "so the runs before then are matched to their schedules by the server's clock at the time, an hour " +
                             (clock.Minutes > 0 ? "later" : "earlier") + " than the UTC offset now says");
            }
            _whatIfCaution.Visible = cautions.Count > 0;
            // Set each time: the theme resets label colours
            _whatIfCaution.ForeColor = DashColors.Warning;
            if (cautions.Count == 0) return;
            var text = string.Join("; ", cautions);
            _whatIfCaution.Text = "⚠ " + char.ToUpper(text[0], CultureInfo.CurrentCulture) + text[1..] + (copies + _clashes.Count + changed + _movedIntoBusy.Count > 0 ? ".  See Note." : ".");
            SizeToText(_whatIfCaution);
        }

        /// <summary>
        /// The job's runs to preview, in start order: those it actually ran in the selected range, moved as planned - or
        /// those its schedules predict.  <paramref name="moved"/> false for where they are now.
        /// </summary>
        // The overlaps before the moves don't change as jobs are moved, and finding them took a second on a busy week - so
        // they're found again only when the jobs previewed, the range, what's previewed or the data change
        private (string Key, int Count)? _overlapsBefore;

        private int OverlapsBeforeMoves(List<JobScheduleInput> included, List<string> names, DateTime from, DateTime to)
        {
            var key = $"{PreviewActual}|{from.Ticks}|{to.Ticks}|{string.Join(",", included.Select(j => j.JobId))}";
            if (_overlapsBefore is { } cached && cached.Key == key) return cached.Count;
            var count = SchedulePlan.FindOverlaps(included.Select(j => PreviewRunsOf(j, moved: false, from, to)).ToList(), names).Count;
            _overlapsBefore = (key, count);
            return count;
        }

        private IReadOnlyList<JobRun> PreviewRunsOf(JobScheduleInput job, bool moved, DateTime from, DateTime to) =>
            PreviewActual ? _plan.ActualRuns(job, moved, from, to) : _plan.PredictedRuns(job, moved, from, to);

        /// <summary>
        /// The pairs that overlap, with the moves or before them, beside the jobs: what the moves change first - new overlaps,
        /// worse ones, cleared, reduced - then the pairs they leave as they were.  The table is kept and refilled, so the grid keeps its columns and sort.
        /// </summary>
        private void FillPairs(List<PlanPairOverlap> pairs, bool anyMoves, List<JobScheduleInput> included, List<JobScheduleInput> shifted, List<JobCycle?> cycles)
        {
            if (_pairsTable == null)
            {
                _pairsTable = new DataTable();
                _pairsTable.Columns.Add("ChangeOrder", typeof(int));
                _pairsTable.Columns.Add("Change", typeof(string));
                _pairsTable.Columns.Add("Job A", typeof(string));
                _pairsTable.Columns.Add("Job B", typeof(string));
                _pairsTable.Columns.Add("Overlaps", typeof(int));
                _pairsTable.Columns.Add("OverlapSec", typeof(double));
                _pairsTable.Columns.Add("StartTimes", typeof(string));
                _pairsTable.Columns.Add("Note", typeof(string));
                _pairsTable.Columns.Add("OverlapChangeSec", typeof(double));
                _pairsTable.Columns.Add("OverlapDetail", typeof(string));
                _pairsGrid.DataSource = new DataView(_pairsTable) { Sort = "ChangeOrder, OverlapSec DESC" };
                SetHeader(_pairsGrid, "Change", "Change", "How the moves change the pair's overlap: New, More, Less or Cleared");
                SetHeader(_pairsGrid, "Overlaps", "Overlaps", "Pairs of runs that overlap over the selected range, as the schedules predict, moved as planned - or in the runs that happened, when a job has no expected duration");
                SetHeader(_pairsGrid, "OverlapSec", "Overlap", "Time the two jobs overlap over the selected range, as the schedules predict, moved as planned, and how the moves change it.  " +
                                                             "Judged by the runs that happened when a job has no expected duration.  Hover for both measures.");
                SetHeader(_pairsGrid, "StartTimes", "Start Times", "For two jobs that repeat at fixed intervals, moved as planned: whether their start times keep them apart, could if moved, or can't - " +
                                                                  "their start times line up again every so often (the greatest common divisor of the intervals), and both runs have to fit in that.  Jobs on the same schedule start together.");
                // Narrow enough to leave the Note in view: it says why a pair can't be kept apart
                _pairsGrid.Columns["Change"].Width = LogicalToDeviceUnits(60);
                _pairsGrid.Columns["Job A"].Width = LogicalToDeviceUnits(110);
                _pairsGrid.Columns["Job B"].Width = LogicalToDeviceUnits(110);
                _pairsGrid.Columns["Overlaps"].Width = LogicalToDeviceUnits(60);
                _pairsGrid.Columns["OverlapSec"].Width = LogicalToDeviceUnits(110);
                _pairsGrid.Columns["StartTimes"].Width = LogicalToDeviceUnits(170);
                _pairsGrid.Columns["Note"].Width = LogicalToDeviceUnits(400);
            }
            var index = included.Select((j, i) => (j.JobId, i)).ToDictionary(x => x.JobId, x => x.i);
            _pairsTable.BeginLoadData();
            _pairsTable.Rows.Clear();
            foreach (var p in pairs)
            {
                var change = p.Change;
                var lead = Lead(p);
                var (a, b) = (index[p.A.JobId], index[p.B.JobId]);
                var (startTimes, note) = StartTimes(p, shifted[a], cycles[a], shifted[b], cycles[b]);
                _pairsTable.Rows.Add(change switch { PairChange.New => 0, PairChange.More => 1, PairChange.Cleared => 2, PairChange.Less => 3, _ => 4 },
                    change == PairChange.Same ? null : change.ToString(),
                    p.A.Name, p.B.Name, lead.Count, lead.After, startTimes, note, anyMoves ? lead.After - lead.Before : 0,
                    OverlapDetail(p.Predicted?.Seconds ?? 0, p.PredictedBefore?.Seconds ?? 0, lead.Predicted, p.Actual.Seconds, p.ActualBefore.Seconds, anyMoves));
            }
            _pairsTable.EndLoadData();
        }

        /// <summary>
        /// Whether two jobs' start times, as moved, keep them apart - for two on fixed cycles, or on the same schedule - and
        /// what to know about it: why they can't be, and the interval that would let them, or that they overlapped when their
        /// schedules don't predict it
        /// </summary>
        private (string StartTimes, string Note) StartTimes(PlanPairOverlap p, JobScheduleInput a, JobCycle? cycleA, JobScheduleInput b, JobCycle? cycleB)
        {
            string startTimes = null;
            var notes = new List<string>();
            if (JobOverlapAnalyzer.SharedSchedule(a, b) is { } shared)
            {
                startTimes = "Same schedule - start together";
                notes.Add($"Both use schedule {shared}, so moving it moves both.  Give one its own copy (Own Schedule) to move it apart");
            }
            else if (cycleA is { } ca && cycleB is { } cb && a.ExpectedDurationSec is { } da && b.ExpectedDurationSec is { } db)
            {
                var verdict = JobOverlapAnalyzer.CompareCycles(ca, da, cb, db, out _);
                startTimes = verdict switch
                {
                    CycleVerdict.Clear => "Keep them apart",
                    CycleVerdict.Collides => "Collide - could be moved apart",
                    CycleVerdict.CannotSeparate => "Can't be kept apart",
                    CycleVerdict.AlwaysRunning => "One is always running",
                    _ => null
                };
                if (verdict == CycleVerdict.CannotSeparate)
                {
                    var why = JobScheduleOptimizer.WhyNotApart(a.Name, ca.PeriodSec, Math.Max(1, da), b.Name, cb.PeriodSec, Math.Max(1, db));
                    notes.Add(char.ToUpper(why[0], CultureInfo.CurrentCulture) + why[1..]);
                }
                if ((!ca.AllDay || !cb.AllDay) && verdict is CycleVerdict.CannotSeparate or CycleVerdict.Collides) notes.Add("Runs part of the day, so this is approximate");
            }
            if (p.Actual.Any && p.Predicted is { Any: false } && startTimes is null or "Keep them apart")
            {
                notes.Add("Overlapped though the schedules don't predict it - runs longer than expected, manual starts or schedule changes");
            }
            return (startTimes, notes.Count == 0 ? null : string.Join(".  ", notes));
        }

        private Font _pairsBold;

        private void PairsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Change") return;
            e.CellStyle.ForeColor = (e.Value as string) switch
            {
                "New" or "More" => DashColors.Fail,
                "Cleared" or "Less" => DashColors.Success,
                _ => e.CellStyle.ForeColor
            };
            if (e.Value is "New" or "Cleared") e.CellStyle.Font = _pairsBold ??= new Font(grid.Font, FontStyle.Bold);
        }

        private IEnumerable<(string, string)> PreviewDetails(TimelineBar bar)
        {
            // Only the plan's own bars: while a plan loads, the chart can still be showing the timeline's, whose indexes mean
            // something else.  Nothing here leaves the chart to describe the bar itself.
            if (RunOf(bar) is not { } p) yield break;
            if (bar.IsDense && _denseSpans.TryGetValue(bar.SourceIndex, out var d))
            {
                yield return ("Runs", $"{d.Runs:N0} - too close together to show apart at this zoom.  Zoom in to see each one.");
                yield return ("From", d.Start.ToString("G", CultureInfo.CurrentCulture) + " (server)");
                yield return ("To", d.End.ToString("G", CultureInfo.CurrentCulture) + " (server)");
                if (d.Runs > 1) yield return ("Every", "About " + JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(Math.Round(d.EverySec)), true));
                yield return ("Duration", JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(d.BusySec / d.Runs), true) + " on average");
                var spanSec = Math.Max(1, (d.End - d.Start).TotalSeconds);
                yield return ("Running", $"{d.BusySec / spanSec:P0} of the time");
                var (with, overlapSec, unavoidableSec) = d.Overlaps(_shown.Overlaps);
                yield return ("Overlaps", with.Count == 0 ? "None" : string.Join(", ", with.OrderBy(n => n)));
                if (overlapSec > 0) yield return ("Overlap", JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(overlapSec), true) + " that moving could change");
                if (unavoidableSec > 0) yield return ("Unavoidable", JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(unavoidableSec), true) + " with jobs no start times can keep it apart from (shown faintly)");
                if (p.Run.MovedBySec != 0) yield return ("Moved by", ScheduleMath.Shift(p.Run.MovedBySec));
                if (AnyRunInBusy(d.JobIndex, d.First, d.Last)) yield return ("In busy hours", "Some of these runs - the job could run outside busy hours");
                yield break;
            }
            yield return ("Start", p.Run.Start.ToString("G", CultureInfo.CurrentCulture) + " (server)");
            if (ShowAppTimeZone) yield return ("Your time", Display(p.Run.Start).ToString("G", CultureInfo.CurrentCulture));
            yield return ("End", p.Run.End.ToString("G", CultureInfo.CurrentCulture) + " (server)");
            var predicted = !PreviewActual || p.Run.Predicted;
            yield return ("Duration", !predicted ? JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(p.Run.Seconds), true)
                : p.Job.ExpectedDurationSec.HasValue ? JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(p.Run.Seconds), true) + " expected" : "Unknown");
            var runOverlaps = _shown.Overlaps.Detail(p.JobIndex, p.RunIndex, p.RunIndex + 1);
            var overlapsWith = runOverlaps.With[0]?.Distinct().ToList();
            yield return ("Overlaps", overlapsWith == null ? "None" : string.Join(", ", overlapsWith));
            if (runOverlaps.Unavoidable[0] != null) yield return ("Unavoidable", "Some of it is with jobs no start times can keep it apart from - shown faintly");
            if (RunInBusy(p.JobIndex, p.RunIndex)) yield return ("In busy hours", "Yes - the job could run outside them (outlined in amber)");
            var shift = p.Run.MovedBySec;
            if (p.Run.Predicted && PreviewActual) yield return ("Predicted", "Its schedules have changed since the runs then - this is when they'd run it now.  The dotted outlines are the runs that happened.");
            if (!PreviewActual || p.Run.Predicted)
            {
                // Which schedule a drag of it moves
                if (p.Run.Schedule != null && p.Job.Schedules.Count(s => s.Enabled) > 1) yield return ("Schedule", p.Run.Schedule.Name);
            }
            else
            {
                yield return ("Started by", SchedulePlan.DescribeStart(p.Run) ?? "Not known - not matched to a schedule");
            }
            if (shift != 0) yield return ("Was", p.Run.Start.AddSeconds(-shift).ToString("G", CultureInfo.CurrentCulture) + " (server)");
            if (shift != 0) yield return ("Moved by", ScheduleMath.Shift(shift));
            // Said whether or not the job has moved yet, so a run that can't move is known before it's dragged
            else if (PreviewActual && SchedulePlan.NotMovedReason(p.Job, p.Run) is { } reason) yield return ("Doesn't move", reason);
        }

        /// <summary>
        /// What the moves do overall, as one line above the details: overlap before and after, the pairs that get better and
        /// worse, and the jobs worse off.  Green when the moves reduce overlap, red when they add to it.  Each pair counts by
        /// its <see cref="Lead"/> measure, so no pair is left out of the verdict or of the jobs worse off; the other measure
        /// is in the tooltip.
        /// </summary>
        private void ShowNetEffect(List<JobScheduleInput> included, List<PlanPairOverlap> pairs, bool anyMoves)
        {
            _netEffect.Visible = anyMoves && included.Count > 0;
            if (!_netEffect.Visible) return;
            static double Change(PlanPairOverlap p) => Lead(p).After - Lead(p).Before;

            static string Amount(double before, double after)
            {
                var change = after - before;
                var percent = before > 0 && Math.Abs(change) >= 0.5 ? $", {Math.Abs(change) / before:P0} {(change < 0 ? "less" : "more")}" : "";
                return $"{Format(before)} → {Format(after)}" + (Math.Abs(change) < 0.5 ? " (no change)" : $" ({FormatChange(change)}{percent})");
            }

            var parts = new List<string> { $"overlap {Amount(pairs.Sum(p => Lead(p).Before), pairs.Sum(p => Lead(p).After))}" };
            var counts = pairs.Where(p => p.Change != PairChange.Same).GroupBy(p => p.Change).ToDictionary(g => g.Key, g => g.Count());
            var pairText = new[] { (PairChange.Cleared, "cleared"), (PairChange.Less, "less"), (PairChange.More, "more"), (PairChange.New, "new") }
                .Where(c => counts.ContainsKey(c.Item1))
                .Select(c => $"{counts[c.Item1]} {c.Item2}")
                .ToList();
            if (pairText.Count > 0) parts.Add("pairs of jobs: " + string.Join(", ", pairText));
            // The jobs the moves make worse, by how much - what's given up for the overall gain
            var worse = included
                .Select(j => (Job: j, Change: pairs.Where(p => p.A == j || p.B == j).Sum(Change)))
                .Where(x => x.Change >= 0.5)
                .OrderByDescending(x => x.Change)
                .ToList();
            if (worse.Count > 0) parts.Add("worse for " + string.Join(", ", worse.Take(4).Select(x => $"{x.Job.Name} ({FormatChange(x.Change)})")) + (worse.Count > 4 ? $" and {worse.Count - 4} more" : ""));

            var net = pairs.Sum(Change);
            var verdict = net < -0.5 ? "Better overall" : net > 0.5 ? "Worse overall" : "No better overall";
            _netEffect.Text = $"{verdict}: {string.Join("  ·  ", parts)}.";
            var predicted = pairs.Where(p => p.Predicted != null).ToList();
            var tip = new StringBuilder("Overlap as the schedules predict it over the selected range");
            if (predicted.Count < pairs.Count) tip.Append($" - or, for {pairs.Count - predicted.Count} {(pairs.Count - predicted.Count == 1 ? "pair" : "pairs")} with a job that has no expected duration, in the runs that happened");
            tip.AppendLine(".");
            if (predicted.Count > 0) tip.AppendLine($"Predicted: {Amount(predicted.Sum(p => p.PredictedBefore?.Seconds ?? 0), predicted.Sum(p => p.Predicted?.Seconds ?? 0))}");
            var changed = included.Any(ScheduleChanged);
            tip.Append($"In the runs that happened{(changed ? " (predicted before schedules changed)" : "")}: {Amount(pairs.Sum(p => p.ActualBefore.Seconds), pairs.Sum(p => p.Actual.Seconds))}");
            _tips.SetToolTip(_netEffect, tip.ToString());
            // Set each time: the theme resets label colours
            _netEffect.ForeColor = net < -0.5 ? DashColors.Success : net > 0.5 ? DashColors.Fail : ForeColor;
            SizeNetEffect();
        }

        /// <summary>What a previewed run is coloured as: a prediction (the predicted preview, or before a schedule changed), or how it ended</summary>
        private TimelineRow.RunStatus ShownStatus(JobRun run) =>
            !PreviewActual || run.Predicted ? TimelineRow.RunStatus.Predicted : (TimelineRow.RunStatus)(run.Status ?? (int)TimelineRow.RunStatus.Succeeded);

        /// <summary>When a job's schedules changed and how, for its note</summary>
        private string DescribeChange(JobScheduleChange change)
        {
            var when = change.Until == DateTime.MaxValue ? "since these runs"
                : $"{DisplayRan(change.Until).ToString("g", CultureInfo.CurrentCulture)}{(ShowAppTimeZone ? "" : " (server time)")}";
            var text = change.Inferred
                ? $"Some runs don't fit its schedules as they are now - they've changed ({(change.Until == DateTime.MaxValue ? "since these runs" : "before " + when)})"
                : $"Its schedules changed {(change.Until == DateTime.MaxValue ? when : "around " + when)}: " +
                  string.Join("; ", change.Schedules.Select(d =>
                      d.Was == null ? $"{d.Now.Name} added ({d.Now.Describe()})"
                      : d.Now == null ? $"{d.Was.Name} removed or disabled (was {d.Was.Describe()})"
                      : $"{d.Now.Name} {d.Was.Describe()} → {d.Now.Describe()}"));
            return text + ".  Before then, the runs its schedules predict now are shown, with dotted outlines where the runs happened";
        }

        private static readonly Regex ShiftPart = new(@"(\d+)\s*([hms])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Reads a move: 2m, -30s, +1h 15m, 00:02:30, -0:30 or a number of minutes.  Empty is no move.</summary>
        internal static bool TryParseShift(string text, out int seconds)
        {
            seconds = 0;
            text = text?.Trim() ?? string.Empty;
            if (text.Length == 0 || text == "0") return true;
            var sign = 1;
            if (text[0] is '+' or '-')
            {
                sign = text[0] == '-' ? -1 : 1;
                text = text[1..].Trim();
            }
            long total;
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
            {
                total = minutes * 60L;
            }
            else if (text.Contains(':'))
            {
                // hh:mm:ss, or mm:ss with two parts
                var parts = text.Split(':');
                if (parts.Length is < 2 or > 3 || parts.Any(p => !int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _))) return false;
                var values = parts.Select(p => long.Parse(p, CultureInfo.InvariantCulture)).ToArray();
                total = values.Length == 3 ? values[0] * 3600 + values[1] * 60 + values[2] : values[0] * 60 + values[1];
            }
            else
            {
                var matches = ShiftPart.Matches(text);
                if (matches.Count == 0 || ShiftPart.Replace(text, "").Trim().Length > 0) return false;
                total = 0;
                foreach (Match m in matches)
                {
                    var value = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    total += char.ToLowerInvariant(m.Groups[2].Value[0]) switch { 'h' => value * 3600, 'm' => value * 60, _ => value };
                }
            }
            // Within a week either way
            if (total > 7 * 86400) return false;
            seconds = (int)(sign * total);
            return true;
        }
    }
}
