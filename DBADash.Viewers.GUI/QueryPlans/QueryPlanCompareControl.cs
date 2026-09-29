using DBADash.QueryPlan.Compare;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using DiffPlex.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// One statement's plan, from one of the plans open in the window, as a side of a comparison.
    /// </summary>
    public sealed class PlanCompareSide
    {
        public PlanCompareSide(string title, ExecutionPlan plan, PlanStatement statement)
        {
            Title = title;
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Statement = statement ?? throw new ArgumentNullException(nameof(statement));
            Ordinal = plan.Statements.ToList().IndexOf(statement) + 1;
        }

        /// <summary>The plan's tab title.</summary>
        public string Title { get; }

        public ExecutionPlan Plan { get; }

        public PlanStatement Statement { get; }

        /// <summary>Position of the statement in its plan, from 1.</summary>
        public int Ordinal { get; }

        /// <summary>The tab's title, and which statement when the plan has more than one.</summary>
        public string Caption =>
            Plan.Statements.Count > 1 ? Title + "  -  " + Ordinal.ToString(CultureInfo.InvariantCulture) + ". " + Statement.Caption : Title;

        public bool IsSameAs(PlanCompareSide other) =>
            other is not null && ReferenceEquals(Plan, other.Plan) && ReferenceEquals(Statement, other.Statement);

        public override string ToString() => Caption;
    }

    /// <summary>
    /// Two plans set against each other, on a tab of <see cref="DBADashGUI.Viewers.ViewerForm"/> like a
    /// plan of its own.
    ///
    /// The summary comes first because it is the answer to the question comparisons are opened to
    /// ask - did it get faster, and what did it cost - and the tabs after it are the why: the plans
    /// side by side, the operators and access methods that came and went, and the waits,
    /// parameters, warnings and query text that differ.  Either side can be changed to any statement
    /// of any plan open in the window, without going back to the plans themselves.
    ///
    /// The figures are worked out by <see cref="PlanComparison"/>; this only shows them.
    /// </summary>
    public sealed class QueryPlanCompareControl : UserControl
    {
        /// <summary>Every statement of every plan open in the window, asked for each time a side is chosen, since tabs come and go.</summary>
        private readonly Func<IReadOnlyList<PlanCompareSide>> _available;

        private PlanCompareSide _before;
        private PlanCompareSide _after;
        private PlanComparison _comparison;

        private readonly ToolStripComboBox _beforeCombo = NewSideCombo();
        private readonly ToolStripComboBox _afterCombo = NewSideCombo();

        /// <summary>Set while a combo is being filled, so it doesn't take its own change as the reader choosing.</summary>
        private bool _fillingCombos;

        private readonly ThemedTabControl _tabs = new() { Dock = DockStyle.Fill, ShowToolTips = true };

        private readonly DBADashDataGridView _highlightsGrid = NewGrid();
        private readonly DBADashDataGridView _metricsGrid = NewGrid();
        private readonly DBADashDataGridView _operatorsGrid = NewGrid();
        private readonly DBADashDataGridView _objectsGrid = NewGrid();
        private readonly DBADashDataGridView _waitsGrid = NewGrid();
        private readonly DBADashDataGridView _parametersGrid = NewGrid();
        private readonly DBADashDataGridView _warningsGrid = NewGrid();
        private readonly DBADashDataGridView _missingIndexGrid = NewGrid();

        private readonly SplitContainer _summarySplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, FixedPanel = FixedPanel.Panel1 };
        private readonly SplitContainer _warningsSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

        // The plans themselves, side by side - or one above the other, for the wide plans that fit
        // better that way.
        // A wide, coloured splitter and a minimum pane size: the canvases fill their panes edge to
        // edge in the same background, so a plain splitter was invisible, and dragging it looked like
        // one plan sliding under the other until it was gone.
        private readonly SplitContainer _plansSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6
        };

        /// <summary>
        /// The least either plan can be squeezed to.  Set only once the split has room for both -
        /// SplitContainer throws for a minimum its current size can't hold, and at construction, or on
        /// a hidden tab, it has only its default size.
        /// </summary>
        private const int PlanPaneMinSize = 120;
        private readonly QueryPlanGraphControl _beforeGraph = NewGraph();
        private readonly QueryPlanGraphControl _afterGraph = NewGraph();
        private readonly Label _beforeHeader = NewGraphHeader();
        private readonly Label _afterHeader = NewGraphHeader();

        /// <summary>Set while one graph's selection is being mirrored in the other, so the other doesn't mirror it back.</summary>
        private bool _syncingSelection;

        private readonly DiffViewer _queryDiff = new();

        private readonly TabPage _summaryTab;
        private readonly TabPage _plansTab;
        private readonly TabPage _operatorsTab;
        private readonly TabPage _objectsTab;
        private readonly TabPage _waitsTab;
        private readonly TabPage _parametersTab;
        private readonly TabPage _warningsTab;

        private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        // Hidden columns carrying what a row is about, for colouring and for double clicking.
        private const string ChangeColumn = "Change";
        private const string StatusColumn = "Status";

        public QueryPlanCompareControl(PlanCompareSide before, PlanCompareSide after, Func<IReadOnlyList<PlanCompareSide>> available)
        {
            _before = before ?? throw new ArgumentNullException(nameof(before));
            _after = after ?? throw new ArgumentNullException(nameof(after));
            _available = available ?? (() => []);

            _summarySplit.Panel1.Controls.Add(_highlightsGrid);
            _summarySplit.Panel2.Controls.Add(_metricsGrid);

            _warningsSplit.Panel1.Controls.Add(_warningsGrid);
            _warningsSplit.Panel2.Controls.Add(_missingIndexGrid);

            _plansSplit.Panel1.Controls.Add(_beforeGraph);
            _plansSplit.Panel1.Controls.Add(_beforeHeader);
            _plansSplit.Panel2.Controls.Add(_afterGraph);
            _plansSplit.Panel2.Controls.Add(_afterHeader);

            _summaryTab = NewPage("Summary", _summarySplit);
            _plansTab = NewPage("Plans", _plansSplit);
            _operatorsTab = NewPage("Operators", _operatorsGrid);
            _objectsTab = NewPage("Objects", _objectsGrid);
            _waitsTab = NewPage("Waits", _waitsGrid);
            _parametersTab = NewPage("Parameters", _parametersGrid);
            _warningsTab = NewPage("Warnings", _warningsSplit);

            _tabs.TabPages.Add(_summaryTab);
            _tabs.TabPages.Add(_plansTab);
            _tabs.TabPages.Add(_operatorsTab);
            _tabs.TabPages.Add(_objectsTab);
            _tabs.TabPages.Add(_waitsTab);
            _tabs.TabPages.Add(_parametersTab);
            _tabs.TabPages.Add(_warningsTab);
            _tabs.TabPages.Add(NewPage("Query", new ElementHost { Dock = DockStyle.Fill, Child = _queryDiff }));

            _tabs.SelectedIndexChanged += (_, _) =>
            {
                // Laid out on first sight: a graph fitted while its tab is hidden is fitted to no size at all.
                if (_tabs.SelectedTab == _plansTab || _tabs.SelectedTab == _warningsTab) PlaceSplitters();
            };

            Controls.Add(_tabs);
            Controls.Add(BuildToolbar());
            Controls.Add(BuildStatusBar());

            _metricsGrid.CellFormatting += ChangeCell_Formatting;
            _highlightsGrid.CellFormatting += ChangeCell_Formatting;
            _operatorsGrid.CellFormatting += StatusCell_Formatting;
            _objectsGrid.CellFormatting += StatusCell_Formatting;
            _missingIndexGrid.CellFormatting += StatusCell_Formatting;
            _waitsGrid.CellFormatting += ChangeCell_Formatting;
            _warningsGrid.CellFormatting += ChangeCell_Formatting;
            _parametersGrid.CellFormatting += ParametersGrid_CellFormatting;
            foreach (var grid in AllGrids) grid.VisibleChanged += Grid_VisibleChanged;
            _operatorsGrid.CellDoubleClick += (_, e) => ShowGroupInPlans(_operatorsGrid, e.RowIndex);
            _objectsGrid.CellDoubleClick += (_, e) => ShowGroupInPlans(_objectsGrid, e.RowIndex);

            _beforeGraph.SelectionChanged += (_, node) => MirrorSelection(node, _afterGraph);
            _afterGraph.SelectionChanged += (_, node) => MirrorSelection(node, _beforeGraph);

            Load += (_, _) =>
            {
                ShowComparison();
                BeginInvoke(() =>
                {
                    PlaceSplitters();
                    ClearSelections();
                });
            };
        }

        /// <summary>What the comparison's tab is called: both plans' titles.</summary>
        public string Title => Shorten(_before.Title, 20) + " vs " + Shorten(_after.Title, 20);

        public string TabToolTip => "Before: " + _before.Caption + Environment.NewLine + "After: " + _after.Caption;

        /// <summary>Raised when a side changes, so the window can rename the tab.</summary>
        public event EventHandler TitleChanged;

        private static string Shorten(string text, int length) => text.Length <= length ? text : text[..(length - 1)] + "…";

        // ---------------------------------------------------------------- chrome

        private ToolStrip BuildToolbar()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            toolbar.Items.Add(new ToolStripLabel("Before:"));
            toolbar.Items.Add(_beforeCombo);
            _beforeCombo.ToolTipText = "The plan compared against.  Any statement of any plan open in this window.";

            toolbar.Items.Add(new ToolStripButton("Swap", null, (_, _) => Swap())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = "⇄ Swap",
                ToolTipText = "Swap the before and after plans.  Every difference is the after plan relative to the before plan."
            });

            toolbar.Items.Add(new ToolStripLabel("After:"));
            toolbar.Items.Add(_afterCombo);
            _afterCombo.ToolTipText = "The plan compared.  Any statement of any plan open in this window.";

            _beforeCombo.DropDown += (_, _) => FillCombo(_beforeCombo, _before);
            _afterCombo.DropDown += (_, _) => FillCombo(_afterCombo, _after);
            _beforeCombo.SelectedIndexChanged += (_, _) => SideChosen(_beforeCombo, isBefore: true);
            _afterCombo.SelectedIndexChanged += (_, _) => SideChosen(_afterCombo, isBefore: false);

            toolbar.Items.Add(new ToolStripSeparator());

            var stacked = new ToolStripButton("Stacked")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                CheckOnClick = true,
                ToolTipText = "Show the plans one above the other rather than side by side - better for wide plans."
            };
            stacked.CheckedChanged += (_, _) =>
            {
                // Cleared first: a minimum that suits the width may not fit the height.
                _plansSplit.Panel1MinSize = 0;
                _plansSplit.Panel2MinSize = 0;
                _plansSplit.Orientation = stacked.Checked ? Orientation.Horizontal : Orientation.Vertical;
                PlaceSplitters(force: true);
            };
            toolbar.Items.Add(stacked);

            toolbar.Items.Add(new ToolStripButton("Fit", Resources.ZoomToFit, (_, _) =>
            {
                _beforeGraph.ZoomToFit();
                _afterGraph.ZoomToFit();
            })
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Fit both plans to their panes."
            });

            toolbar.Items.Add(new ToolStripSeparator());

            var copy = new ToolStripDropDownButton("Copy", Resources.ASX_Copy_blue_16x)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Copy the comparison."
            };
            copy.DropDownItems.Add(new ToolStripMenuItem("Copy Summary", null, (_, _) => CopyText(_comparison?.ToText()))
            {
                ToolTipText = "The key differences and every figure, as text - for a ticket or a chat."
            });
            toolbar.Items.Add(copy);

            return toolbar;
        }

        private StatusStrip BuildStatusBar()
        {
            var status = new StatusStrip { SizingGrip = false };
            status.Items.Add(_status);
            return status;
        }

        private static ToolStripComboBox NewSideCombo() => new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoSize = false,
            Width = 320,
            DropDownWidth = 600,
            FlatStyle = FlatStyle.Flat
        };

        /// <summary>
        /// Lists every statement of every plan open, keeping the one chosen even when its tab has been
        /// closed since - the comparison still holds it, and dropping it from the list would leave the
        /// combo showing nothing.
        /// </summary>
        private void FillCombo(ToolStripComboBox combo, PlanCompareSide current)
        {
            _fillingCombos = true;
            try
            {
                combo.Items.Clear();

                var sides = _available().ToList();
                var selected = sides.FirstOrDefault(s => s.IsSameAs(current));
                if (selected is null)
                {
                    sides.Insert(0, current);
                    selected = current;
                }

                foreach (var side in sides) combo.Items.Add(side);
                combo.SelectedItem = selected;
            }
            finally
            {
                _fillingCombos = false;
            }
        }

        private void SideChosen(ToolStripComboBox combo, bool isBefore)
        {
            if (_fillingCombos || combo.SelectedItem is not PlanCompareSide side) return;

            var current = isBefore ? _before : _after;
            if (side.IsSameAs(current)) return;

            if (isBefore) _before = side;
            else _after = side;

            ShowComparison();
        }

        private void Swap()
        {
            (_before, _after) = (_after, _before);
            ShowComparison();
        }

        private static void CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                // Another application holding the clipboard open is not worth an error dialog.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        // ---------------------------------------------------------------- building

        private static DBADashDataGridView NewGrid() => new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        private static QueryPlanGraphControl NewGraph() => new()
        {
            Dock = DockStyle.Fill,
            EdgeWidthMetric = QueryPlanViewerControl.LoadEdgeWidth(),
            EdgeWidthBasis = QueryPlanViewerControl.LoadEdgeWidthBasis(),
            OperatorTimeMode = QueryPlanViewerControl.LoadTimeMode(),
            NodeWidth = QueryPlanViewerControl.LoadNodeWidth(),
            ColumnSpacing = QueryPlanViewerControl.LoadColumnSpacing(),
            VerticalLayout = QueryPlanViewerControl.LoadVerticalLayout(),
            ShowNodeIds = DBADashGUI.Viewers.ViewerSettings.QueryPlanShowNodeIds,
            // Fitted whole: side by side, the shapes are what is being compared, and each pane has
            // half the room a plan normally gets.
            MinAutoFitZoom = 0
        };

        private static Label NewGraphHeader() => new()
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        private static TabPage NewPage(string text, Control content)
        {
            var page = new TabPage(text);
            page.Controls.Add(content);
            return page;
        }

        /// <summary>
        /// Halves the plans between their panes and sizes the key differences list to its rows, once
        /// the control has a real size - a SplitContainer places its splitter in pixels.
        /// </summary>
        private void PlaceSplitters() => PlaceSplitters(force: false);

        private bool _plansPlaced;

        private void PlaceSplitters(bool force)
        {
            var span = _plansSplit.Orientation == Orientation.Vertical ? _plansSplit.Width : _plansSplit.Height;
            if ((force || !_plansPlaced) && span > 0 && _tabs.SelectedTab == _plansTab)
            {
                SetSplitterDistance(_plansSplit, span / 2);
                if (span >= PlanPaneMinSize * 2 + _plansSplit.SplitterWidth)
                {
                    _plansSplit.Panel1MinSize = PlanPaneMinSize;
                    _plansSplit.Panel2MinSize = PlanPaneMinSize;
                }
                _plansPlaced = true;
                _beforeGraph.ZoomToFit();
                _afterGraph.ZoomToFit();
            }

            SizeHighlights();

            if (!_warningsPlaced && !_warningsSplit.Panel2Collapsed && _tabs.SelectedTab == _warningsTab)
            {
                SetSplitterDistance(_warningsSplit, _warningsSplit.Height / 2);
                _warningsPlaced = true;
            }
        }

        private bool _warningsPlaced;

        /// <summary>
        /// Places a splitter, clamped to where the container allows it - and not at all while it has too
        /// little room for both panels, which is the size of a container on a tab not shown yet.  The
        /// setter throws for anything outside the range rather than clamping.
        /// </summary>
        private static void SetSplitterDistance(SplitContainer split, int distance)
        {
            var span = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
            var max = span - split.Panel2MinSize - split.SplitterWidth;
            if (max < split.Panel1MinSize) return;

            split.SplitterDistance = Math.Clamp(distance, split.Panel1MinSize, max);
        }

        /// <summary>The key differences get the height of their rows, up to half the tab; the figures get the rest.</summary>
        private void SizeHighlights()
        {
            if (_summarySplit.Height <= 0) return;

            var rows = _highlightsGrid.Rows.Cast<DataGridViewRow>().Sum(r => r.Height);
            var wanted = _highlightsGrid.ColumnHeadersHeight + rows + 4;
            SetSplitterDistance(_summarySplit, Math.Min(wanted, _summarySplit.Height / 2));
        }

        private void ShowComparison()
        {
            _comparison = PlanComparison.Compare(_before.Statement, _after.Statement, _before.Plan.Build, _after.Plan.Build);

            FillCombo(_beforeCombo, _before);
            FillCombo(_afterCombo, _after);

            ShowHighlights();
            ShowMetrics();
            ShowPlans();
            ShowGroups(_operatorsGrid, _comparison.Operators, byObject: false);
            ShowGroups(_objectsGrid, _comparison.Objects, byObject: true);
            ShowWaits();
            ShowParameters();
            ShowWarnings();
            ShowQueryText();
            ClearSelections();

            _operatorsTab.Text = Counted("Operators", _comparison.Operators.Count(o => o.Status != PlanPresence.Both), "changed");
            _objectsTab.Text = Counted("Objects", _comparison.Objects.Count(o => o.Status != PlanPresence.Both), "changed");
            _waitsTab.Text = Counted("Waits", _comparison.Waits.Count, null);
            _parametersTab.Text = Counted("Parameters", _comparison.Parameters.Count(p => p.CompiledValueDiffers || p.RuntimeValueDiffers), "differ");
            _warningsTab.Text = Counted("Warnings", _comparison.Warnings.Count(w => w.Change is PlanComparisonChange.Better or PlanComparisonChange.Worse), "changed");

            _status.Text = StatusText();

            SizeHighlights();
            TitleChanged?.Invoke(this, EventArgs.Empty);
            this.ApplyTheme();
            ShowPlansDivider();
            _queryDiff.ApplyTheme(ThemeExtensions.CurrentTheme);
        }

        /// <summary>
        /// Sizes each grid's columns to what is in them, once, and leaves no row selected to begin
        /// with - a grid selects its first row as it is shown, and the selection colour hides the change
        /// colour of the row that is usually the most important.
        ///
        /// Sized once rather than left in an auto size mode, which would stop the reader dragging a
        /// column wider or narrower.
        /// </summary>
        private void ClearSelections()
        {
            _unshownGrids.Clear();
            foreach (var grid in AllGrids)
            {
                // A grid on a tab not yet shown has no width to size against, and selects its first row
                // when it is shown, after this has run - so both wait until it is.
                if (grid.Visible) FitGrid(grid);
                else _unshownGrids.Add(grid);
            }
        }

        private static void FitGrid(DataGridView grid)
        {
            grid.AutoResizeColumnsWithMaxColumnWidth();
            grid.ClearSelection();
        }

        private IEnumerable<DataGridView> AllGrids =>
            [_highlightsGrid, _metricsGrid, _operatorsGrid, _objectsGrid, _waitsGrid, _parametersGrid, _warningsGrid, _missingIndexGrid];

        /// <summary>Grids refilled while their tab was hidden, sized and cleared when they are first shown.</summary>
        private readonly HashSet<DataGridView> _unshownGrids = new();

        private void Grid_VisibleChanged(object sender, EventArgs e)
        {
            if (sender is not DataGridView { Visible: true } grid || !_unshownGrids.Remove(grid)) return;

            BeginInvoke(() => FitGrid(grid));
        }

        /// <summary>
        /// The splitter between the plans shows the split container's own background, which theming
        /// sets to the same colour as the canvases either side - so it is set to the header colour
        /// afterwards, with the panels kept in the background colour.
        /// </summary>
        private void ShowPlansDivider()
        {
            var theme = ThemeExtensions.CurrentTheme;
            _plansSplit.BackColor = theme.ColumnHeaderBackColor;
            _plansSplit.Panel1.BackColor = theme.BackgroundColor;
            _plansSplit.Panel2.BackColor = theme.BackgroundColor;
        }

        private static string Counted(string name, int count, string what) =>
            count == 0 ? name : name + " (" + count.ToString(CultureInfo.InvariantCulture) + (what is null ? "" : " " + what) + ")";

        private string StatusText()
        {
            var parts = new List<string>
            {
                _comparison.SamePlanShape switch
                {
                    true => "Same plan shape",
                    false => "Different plan shape",
                    _ => "Plan shape unknown"
                }
            };

            if (_comparison.SameQuery == false) parts.Add("different query hash");

            var better = _comparison.Metrics.Count(m => m.Change == PlanComparisonChange.Better && !m.IsEstimate);
            var worse = _comparison.Metrics.Count(m => m.Change == PlanComparisonChange.Worse && !m.IsEstimate);
            parts.Add(better.ToString(CultureInfo.InvariantCulture) + (better == 1 ? " figure" : " figures") + " better, " +
                      worse.ToString(CultureInfo.InvariantCulture) + " worse");

            return string.Join("  ·  ", parts);
        }

        // ---------------------------------------------------------------- summary

        private void ShowHighlights()
        {
            ResetGrid(_highlightsGrid);
            _highlightsGrid.ColumnHeadersVisible = true;
            _highlightsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = ChangeColumn, Visible = false });
            _highlightsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Marker",
                HeaderText = "",
                Width = 90,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _highlightsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Text",
                HeaderText = "Key Differences",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            foreach (var highlight in _comparison.Highlights)
            {
                var index = _highlightsGrid.Rows.Add(highlight.Change, ChangeName(highlight.Change, highlight.Metric), highlight.Text);
                _highlightsGrid.Rows[index].Tag = highlight.Metric;
            }
        }

        private void ShowMetrics()
        {
            ResetGrid(_metricsGrid);

            _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = ChangeColumn, Visible = false });
            AddTextColumn(_metricsGrid, "Group", "Group", sortable: false);
            AddTextColumn(_metricsGrid, "Metric", "Metric", sortable: false);
            AddTextColumn(_metricsGrid, "Before", "Before", sortable: false, right: true);
            AddTextColumn(_metricsGrid, "After", "After", sortable: false, right: true);
            AddTextColumn(_metricsGrid, "Marker", "Change", sortable: false);
            AddTextColumn(_metricsGrid, "Difference", "Difference", sortable: false, right: true);

            string lastGroup = null;
            foreach (var metric in _comparison.Metrics)
            {
                // The group named once, at its first row, so the list reads in sections.
                var group = metric.Group == lastGroup ? string.Empty : metric.Group;
                lastGroup = metric.Group;

                var index = _metricsGrid.Rows.Add(
                    metric.Change,
                    group,
                    metric.Name,
                    metric.BeforeText ?? "-",
                    metric.AfterText ?? "-",
                    ChangeName(metric.Change, metric),
                    metric.DifferenceText);

                var row = _metricsGrid.Rows[index];
                row.Tag = metric;
                row.Cells["Metric"].ToolTipText = metric.Description;
            }

            _metricsGrid.Columns["Group"].DefaultCellStyle.Font = GroupFont;
        }

        /// <summary>The metric groups' names, in bold - one font for every refill rather than one per row.</summary>
        private Font GroupFont => _groupFont ??= new Font(_metricsGrid.Font, FontStyle.Bold);

        private Font _groupFont;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _groupFont?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// How a change is written in the lists.  Better and worse are for what happened; a figure the
        /// optimiser expected - a cost, memory sized from its estimates - is only higher or lower, since
        /// more accurate estimates can raise it and still give the faster plan.  The same for a number
        /// with no better direction.  The arrow is always the way the figure moved, so a share of the
        /// grant used that went up is "▲ Better".  Similar is left blank: it is the same, as far as
        /// anybody should care.
        /// </summary>
        private static string ChangeName(PlanComparisonChange change, PlanComparisonMetric metric = null)
        {
            var arrow = metric?.Trend switch
            {
                PlanComparisonTrend.Higher => "▲ ",
                PlanComparisonTrend.Lower => "▼ ",
                _ => change == PlanComparisonChange.Worse ? "▲ " : change == PlanComparisonChange.Better ? "▼ " : string.Empty
            };

            if (metric is { Trend: not PlanComparisonTrend.None } &&
                (metric.IsEstimate || change == PlanComparisonChange.Changed))
            {
                return arrow + (metric.Trend == PlanComparisonTrend.Higher ? "Higher" : "Lower");
            }

            return change switch
            {
                PlanComparisonChange.Better => arrow + "Better",
                PlanComparisonChange.Worse => arrow + "Worse",
                PlanComparisonChange.Changed => "Changed",
                PlanComparisonChange.NotComparable => "n/a",
                _ => string.Empty
            };
        }

        /// <summary>
        /// The change cell in the theme's success and critical colours - the same status colours the
        /// rest of DBA Dash uses, so green and red mean what they mean everywhere else.  An estimate's
        /// change is in the pale green and red: still good or bad news, but a weaker signal than a
        /// measured one.
        /// </summary>
        private void ChangeCell_Formatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;
            var column = grid.Columns[e.ColumnIndex].Name;
            if (column is not ("Marker" or "Difference" or "DifferenceMs")) return;

            var row = grid.Rows[e.RowIndex];
            var change = row.Cells[ChangeColumn].Value is PlanComparisonChange c ? c : PlanComparisonChange.Same;
            ApplyChangeStyle(e.CellStyle, change, row.Tag is PlanComparisonMetric { IsEstimate: true });
        }

        private static void ApplyChangeStyle(DataGridViewCellStyle style, PlanComparisonChange change, bool estimate = false)
        {
            var theme = ThemeExtensions.CurrentTheme;
            switch (change)
            {
                // Pale fills with dark text are the same in either theme, as the insight cards' are.
                case PlanComparisonChange.Better when estimate:
                    style.BackColor = DashColors.GreenPale;
                    style.ForeColor = DashColors.GreenDark;
                    break;
                case PlanComparisonChange.Worse when estimate:
                    style.BackColor = DashColors.RedPale;
                    style.ForeColor = DashColors.RedDark;
                    break;
                case PlanComparisonChange.Better:
                    style.BackColor = theme.SuccessBackColor;
                    style.ForeColor = theme.SuccessForeColor;
                    break;
                case PlanComparisonChange.Worse:
                    style.BackColor = theme.CriticalBackColor;
                    style.ForeColor = theme.CriticalForeColor;
                    break;
                case PlanComparisonChange.Changed:
                    style.BackColor = theme.WarningLowBackColor;
                    style.ForeColor = theme.WarningLowForeColor;
                    break;
            }

            style.SelectionBackColor = style.BackColor.IsEmpty ? style.SelectionBackColor : ControlPaint.Dark(style.BackColor, 0.1f);
            style.SelectionForeColor = style.ForeColor.IsEmpty ? style.SelectionForeColor : style.ForeColor;
        }

        // ---------------------------------------------------------------- plans

        private void ShowPlans()
        {
            _beforeHeader.Text = "Before:  " + _before.Caption;
            _afterHeader.Text = "After:  " + _after.Caption;

            ShowPlan(_beforeGraph, _before.Statement);
            ShowPlan(_afterGraph, _after.Statement);

            // Actual plans are compared by what happened, so the bars show time where both graphs have
            // it - otherwise cost on both, so the two sides are always measured the same way.
            var graphs = new[] { _beforeGraph, _afterGraph }.Where(g => g.Statement is not null).ToList();
            var metric = _comparison.BothActual && graphs.Count == 2 && graphs.All(g => g.Supports(PlanHeatMetric.Elapsed))
                ? PlanHeatMetric.Elapsed
                : PlanHeatMetric.OperatorCost;
            foreach (var graph in graphs.Where(g => g.Supports(metric)))
            {
                graph.HeatMetric = metric;
            }
        }

        private static void ShowPlan(QueryPlanGraphControl graph, PlanStatement statement)
        {
            graph.Visible = statement.HasPlan;
            if (statement.HasPlan) graph.LoadStatement(statement);
        }

        /// <summary>
        /// Selecting an operator in one plan selects its counterpart in the other, where there is one.
        /// The same shape means the same node ids; otherwise the first operator of the same kind on
        /// the same object and index - right far more often than not, and harmless when it isn't,
        /// since it only moves a selection.
        /// </summary>
        private void MirrorSelection(PlanNode node, QueryPlanGraphControl other)
        {
            if (_syncingSelection || node?.Operator is not { } op || other.Statement is null) return;

            var match = Counterpart(op, other.Statement);
            if (match is null) return;

            _syncingSelection = true;
            try
            {
                other.SelectOperator(match);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private PlanOperator Counterpart(PlanOperator op, PlanStatement other)
        {
            var candidates = other.Operators.ToList();

            if (_comparison.SamePlanShape == true)
            {
                var byId = candidates.FirstOrDefault(c => c.NodeId == op.NodeId);
                if (byId is not null) return byId;
            }

            var name = PlanComparison.OperatorName(op);
            var target = op.PrimaryObject;

            return candidates.FirstOrDefault(c =>
                string.Equals(PlanComparison.OperatorName(c), name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.PrimaryObject?.QualifiedTableName, target?.QualifiedTableName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.PrimaryObject?.Index, target?.Index, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Double clicking a group shows its operators on the Plans tab, on whichever sides have them.</summary>
        private void ShowGroupInPlans(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || grid.Rows[rowIndex].Tag is not PlanOperatorGroupComparison group) return;

            _tabs.SelectedTab = _plansTab;

            _syncingSelection = true;
            try
            {
                if (group.Before?.Operators.FirstOrDefault() is { } before) _beforeGraph.SelectOperator(before);
                if (group.After?.Operators.FirstOrDefault() is { } after) _afterGraph.SelectOperator(after);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        // ---------------------------------------------------------------- operators and objects

        private static void ShowGroups(DataGridView grid, IReadOnlyList<PlanOperatorGroupComparison> groups, bool byObject)
        {
            ResetGrid(grid);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = StatusColumn, Visible = false });
            AddTextColumn(grid, "Presence", "In");
            if (byObject)
            {
                AddTextColumn(grid, "Object", "Object");
                AddTextColumn(grid, "Index", "Index");
            }

            AddTextColumn(grid, "Operator", "Operator");
            AddNumberColumn(grid, "BeforeCount", "Count Before", typeof(int), "N0");
            AddNumberColumn(grid, "AfterCount", "Count After", typeof(int), "N0");
            AddNumberColumn(grid, "BeforeCost", "Cost % Before", typeof(double), "P1");
            AddNumberColumn(grid, "AfterCost", "Cost % After", typeof(double), "P1");
            AddNumberColumn(grid, "BeforeRows", "Rows Before", typeof(double), "N0");
            AddNumberColumn(grid, "AfterRows", "Rows After", typeof(double), "N0");
            if (byObject)
            {
                AddNumberColumn(grid, "BeforeRowsRead", "Rows Read Before", typeof(long), "N0");
                AddNumberColumn(grid, "AfterRowsRead", "Rows Read After", typeof(long), "N0");
                AddNumberColumn(grid, "BeforeReads", "Logical Reads Before", typeof(long), "N0");
                AddNumberColumn(grid, "AfterReads", "Logical Reads After", typeof(long), "N0");
            }

            AddNumberColumn(grid, "BeforeElapsed", "Own Elapsed Before (ms)", typeof(long), "N0");
            AddNumberColumn(grid, "AfterElapsed", "Own Elapsed After (ms)", typeof(long), "N0");
            AddNumberColumn(grid, "BeforeCpu", "Own CPU Before (ms)", typeof(long), "N0");
            AddNumberColumn(grid, "AfterCpu", "Own CPU After (ms)", typeof(long), "N0");

            foreach (var group in groups)
            {
                var values = new List<object>
                {
                    group.Status,
                    PresenceName(group.Status)
                };

                if (byObject)
                {
                    values.Add(group.Object);
                    values.Add(group.Index);
                }

                values.Add(group.Operator);
                values.Add(group.Before?.Count ?? 0);
                values.Add(group.After?.Count ?? 0);
                values.Add(group.Before?.CostShare);
                values.Add(group.After?.CostShare);
                values.Add(group.Before?.Rows);
                values.Add(group.After?.Rows);
                if (byObject)
                {
                    values.Add(group.Before?.RowsRead);
                    values.Add(group.After?.RowsRead);
                    values.Add(group.Before?.LogicalReads);
                    values.Add(group.After?.LogicalReads);
                }

                values.Add(group.Before?.OwnElapsedMs);
                values.Add(group.After?.OwnElapsedMs);
                values.Add(group.Before?.OwnCpuMs);
                values.Add(group.After?.OwnCpuMs);

                var index = grid.Rows.Add(values.ToArray());
                grid.Rows[index].Tag = group;
                grid.Rows[index].Cells["Operator"].ToolTipText = "Double click to show on the Plans tab.";
            }

            HideEmptyColumns(grid);
        }

        private static string PresenceName(PlanPresence presence) => presence switch
        {
            PlanPresence.BeforeOnly => "Before only",
            PlanPresence.AfterOnly => "After only",
            _ => "Both"
        };

        /// <summary>Before-only rows are removed operators and after-only rows added ones, coloured so they stand out from the rows both plans share.</summary>
        private void StatusCell_Formatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid || grid.Columns[e.ColumnIndex].Name != "Presence") return;
            if (grid.Rows[e.RowIndex].Cells[StatusColumn].Value is not PlanPresence presence || presence == PlanPresence.Both) return;

            var theme = ThemeExtensions.CurrentTheme;
            e.CellStyle.BackColor = presence == PlanPresence.AfterOnly ? theme.InformationBackColor : theme.WarningLowBackColor;
            e.CellStyle.ForeColor = presence == PlanPresence.AfterOnly ? theme.InformationForeColor : theme.WarningLowForeColor;
        }

        // ---------------------------------------------------------------- waits, parameters, warnings, text

        private void ShowWaits()
        {
            ResetGrid(_waitsGrid);

            _waitsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = ChangeColumn, Visible = false });
            AddTextColumn(_waitsGrid, "WaitType", "Wait Type");
            AddNumberColumn(_waitsGrid, "BeforeMs", "Wait Time Before (ms)", typeof(long), "N0");
            AddNumberColumn(_waitsGrid, "AfterMs", "Wait Time After (ms)", typeof(long), "N0");
            AddNumberColumn(_waitsGrid, "DifferenceMs", "Difference (ms)", typeof(long), "+#,##0;-#,##0;0");
            AddNumberColumn(_waitsGrid, "BeforeCount", "Wait Count Before", typeof(long), "N0");
            AddNumberColumn(_waitsGrid, "AfterCount", "Wait Count After", typeof(long), "N0");

            foreach (var wait in _comparison.Waits)
            {
                _waitsGrid.Rows.Add(wait.Change, wait.WaitType, wait.BeforeMs, wait.AfterMs, wait.DifferenceMs, wait.BeforeCount, wait.AfterCount);
            }
        }

        private void ShowParameters()
        {
            ResetGrid(_parametersGrid);

            AddTextColumn(_parametersGrid, "Name", "Parameter");
            AddTextColumn(_parametersGrid, "DataType", "Data Type");
            AddTextColumn(_parametersGrid, "CompiledBefore", "Compiled Before");
            AddTextColumn(_parametersGrid, "CompiledAfter", "Compiled After");
            AddTextColumn(_parametersGrid, "RuntimeBefore", "Runtime Before");
            AddTextColumn(_parametersGrid, "RuntimeAfter", "Runtime After");

            foreach (var parameter in _comparison.Parameters)
            {
                var index = _parametersGrid.Rows.Add(
                    parameter.Name,
                    parameter.DataType,
                    parameter.Before?.CompiledValue,
                    parameter.After?.CompiledValue,
                    parameter.Before?.RuntimeValue,
                    parameter.After?.RuntimeValue);
                _parametersGrid.Rows[index].Tag = parameter;
            }

            HideEmptyColumns(_parametersGrid);
        }

        /// <summary>The value pairs that differ, marked - a plan compiled for a different value is the first thing to check.</summary>
        private void ParametersGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _parametersGrid.Rows[e.RowIndex].Tag is not PlanParameterComparison parameter) return;

            var differs = _parametersGrid.Columns[e.ColumnIndex].Name switch
            {
                "CompiledBefore" or "CompiledAfter" => parameter.CompiledValueDiffers,
                "RuntimeBefore" or "RuntimeAfter" => parameter.RuntimeValueDiffers,
                _ => false
            };

            if (differs) ApplyChangeStyle(e.CellStyle, PlanComparisonChange.Changed);
        }

        private void ShowWarnings()
        {
            ResetGrid(_warningsGrid);

            _warningsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = ChangeColumn, Visible = false });
            AddTextColumn(_warningsGrid, "Warning", "Warning");
            AddTextColumn(_warningsGrid, "Severity", "Severity");
            AddNumberColumn(_warningsGrid, "BeforeCount", "Before", typeof(int), "N0");
            AddNumberColumn(_warningsGrid, "AfterCount", "After", typeof(int), "N0");
            AddTextColumn(_warningsGrid, "Marker", "Change");

            foreach (var warning in _comparison.Warnings)
            {
                var index = _warningsGrid.Rows.Add(warning.Change, warning.Title, warning.Severity.ToString(), warning.BeforeCount, warning.AfterCount, ChangeName(warning.Change));
                if (warning.Change == PlanComparisonChange.NotComparable)
                {
                    _warningsGrid.Rows[index].Cells["Marker"].ToolTipText = "Only a run can raise this warning, and one of the plans is estimated.";
                }
            }

            ResetGrid(_missingIndexGrid);
            _missingIndexGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = StatusColumn, Visible = false });
            AddTextColumn(_missingIndexGrid, "Presence", "In");
            AddNumberColumn(_missingIndexGrid, "BeforeImpact", "Impact Before (%)", typeof(double), "N1");
            AddNumberColumn(_missingIndexGrid, "AfterImpact", "Impact After (%)", typeof(double), "N1");
            AddTextColumn(_missingIndexGrid, "Index", "Missing Index");

            foreach (var index in _comparison.MissingIndexes)
            {
                _missingIndexGrid.Rows.Add(index.Status, PresenceName(index.Status), index.Before?.Impact, index.After?.Impact, index.Index.CreateStatementOneLine);
            }

            _warningsSplit.Panel2Collapsed = _comparison.MissingIndexes.Count == 0;
        }

        private void ShowQueryText()
        {
            _queryDiff.OldTextHeader = "Before: " + _before.Title;
            _queryDiff.NewTextHeader = "After: " + _after.Title;
            _queryDiff.OldText = _before.Statement.StatementText ?? string.Empty;
            _queryDiff.NewText = _after.Statement.StatementText ?? string.Empty;
            _queryDiff.ShowSideBySide();
        }

        // ---------------------------------------------------------------- grid helpers

        private static void ResetGrid(DataGridView grid)
        {
            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
        }

        private static void AddTextColumn(DataGridView grid, string name, string header, bool sortable = true, bool right = false)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                SortMode = sortable ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = right ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft }
            });
        }

        private static void AddNumberColumn(DataGridView grid, string name, string header, Type valueType, string format)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                ValueType = valueType,
                MinimumWidth = 80,
                DefaultCellStyle = { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
            });
        }

        /// <summary>
        /// Hides the columns with nothing in them - the run time figures between two estimated plans,
        /// the data types of a plan from before SQL Server 2017 - so the ones left are the ones to read.
        /// </summary>
        private static void HideEmptyColumns(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible || column.Name is StatusColumn or ChangeColumn) continue;

                column.Visible = grid.Rows.Cast<DataGridViewRow>().Any(row => row.Cells[column.Index].Value is not null and not "");
            }
        }
    }
}
