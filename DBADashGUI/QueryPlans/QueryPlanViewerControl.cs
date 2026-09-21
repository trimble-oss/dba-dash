using DBADash.QueryPlan;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.ShellIntegration;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The query plan viewer for one plan: the graph, the properties of whatever is selected, and
    /// the lists that carry what there is no room for on the picture.  Each plan open is one of these
    /// on a tab of <see cref="QueryPlanViewerForm"/>, with its own toolbar and status bar, since
    /// every figure on them is about its own plan.
    ///
    /// The graph is the part people ask for, but a plan is diagnosed from the detail - which
    /// estimate was wrong, which index was wanted, what it waited on - so the warnings and missing
    /// index lists are first class tabs rather than an afterthought, and each of them points back at
    /// the operator it is about.
    /// </summary>
    public sealed class QueryPlanViewerControl : UserControl
    {
        private readonly ExecutionPlan _plan;
        private readonly string _sourceXml;
        private readonly string _fileName;

        /// <summary>
        /// Where the plan came from, where the caller knew.  Null for a plan opened from a file, and for
        /// the call sites that have no context to hand - the viewer works the same either way; an
        /// AI analysis is simply recorded without an instance against it.
        /// </summary>
        private readonly DBADashContext _context;

        private readonly QueryPlanGraphControl _graphControl = new() { Dock = DockStyle.Fill };
        private readonly QueryPlanPropertiesControl _properties = new() { Dock = DockStyle.Fill };
        private readonly DBADashDataGridView _warningsGrid = NewGrid();
        private readonly DBADashDataGridView _missingIndexGrid = NewGrid();
        private readonly DBADashDataGridView _expressionsGrid = NewGrid();
        private readonly DBADashDataGridView _parametersGrid = NewGrid();
        private readonly DBADashDataGridView _waitsGrid = NewGrid();

        /// <summary>
        /// The graph and the properties panel side by side.  Properties are docked rather than in a
        /// separate window: reading a plan is a loop of clicking an operator and looking at its
        /// figures, and a window that has to be reopened breaks the loop every time.
        ///
        /// The panel is open while an operator is selected, and with nothing selected while the
        /// statement has an overview to show - its warnings, missing indexes and the rest.  Otherwise
        /// it is closed: empty, it was a blank area beside the plan that read as part of it - as
        /// though the plan carried on underneath - and it took width from the plan for nothing.
        /// </summary>
        private readonly SplitContainer _graphSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel2,
            Panel2Collapsed = true
        };

        /// <summary>
        /// How wide the properties panel opens.  Remembered when the reader drags it, so closing and
        /// reopening it does not undo their choice.
        /// </summary>
        private int _propertiesWidth = 340;

        /// <summary>
        /// Set while the panel is being opened, so the splitter being placed is not mistaken for the
        /// reader dragging it - which on a narrow window would remember a squeezed width.
        /// </summary>
        private bool _openingProperties;

        // Tooltips on, for the tabs that summarise what they hold - a parameter that ran with a
        // different value, the total time spent waiting.
        private readonly ThemedTabControl _tabs = new() { Dock = DockStyle.Fill, ShowToolTips = true };
        private readonly TabPage _warningsTab;
        private readonly TabPage _missingIndexTab;
        private readonly TabPage _expressionsTab;
        private readonly TabPage _parametersTab;
        private readonly TabPage _waitsTab;

        private readonly CodeEditor _queryText;
        private readonly ElementHost _queryHost = new() { Dock = DockStyle.Fill };
        private readonly CodeEditor _xmlText;
        private readonly ElementHost _xmlHost = new() { Dock = DockStyle.Fill };

        // The T-SQL for the missing indexes and the parameters, under their grids: the list says what
        // is there, and the script is what anybody does with it - see GridAndScript.
        private readonly CodeEditor _missingIndexScript = NewScriptEditor();
        private readonly CodeEditor _parameterScript = NewScriptEditor();
        private readonly SplitContainer _missingIndexScriptSplit = new();
        private readonly SplitContainer _parameterScriptSplit = new();

        private readonly QueryPlanStatementsControl _statements = new() { Dock = DockStyle.Fill };

        private readonly QueryPlanAiControl _ai = new() { Dock = DockStyle.Fill };

        /// <summary>
        /// The statement list above the plan.  Above rather than on a tab of its own, because moving
        /// between statements is a loop of choosing one and looking at its plan - and a list that
        /// has to be switched to and back from breaks the loop on every step.  Collapsed away
        /// entirely for a single statement, where there is nothing to choose between.
        /// </summary>
        private readonly SplitContainer _statementSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel1
        };

        private readonly ToolStripComboBox _metricSelector = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoSize = false,
            Width = 130
        };

        private readonly ToolStripTextBox _findBox = new()
        {
            Width = 140,
            ToolTipText = "Find an operator, table, index or predicate - or a node id, for the nodes the cards and lists name"
        };

        /// <summary>Own or reported operator times - see <see cref="BuildTimeMenu"/>.</summary>
        private readonly ToolStripDropDownButton _timeMenu = new();

        /// <summary>What the arrows measure - see <see cref="BuildLineWidthMenu"/>.</summary>
        private readonly ToolStripDropDownButton _lineWidthMenu = new();

        private readonly Dictionary<PlanEdgeWidthMetric, ToolStripMenuItem> _lineWidthMetricItems = new();
        private readonly Dictionary<PlanEdgeWidthBasis, ToolStripMenuItem> _lineWidthBasisItems = new();

        /// <summary>The toolbar toggle, kept so the right click menu can work it and show its state.</summary>
        private ToolStripButton _dataPathButton;

        /// <summary>
        /// Kept so it can be greyed out while there is nothing collapsed to put back - which is most
        /// of the time, and is what the right click menu has always done by leaving the item out.
        /// </summary>
        private ToolStripButton _expandAllButton;

        /// <summary>Settings > Show Operator Descriptions, for the same reason.</summary>
        private ToolStripMenuItem _descriptionsItem;

        /// <summary>Settings > Show Node IDs, so the right click menu can work it and show its state.</summary>
        private ToolStripMenuItem _nodeIdsItem;

        /// <summary>Settings > Operator Width - see <see cref="BuildOperatorWidthMenu"/>.</summary>
        private readonly ToolStripMenuItem _operatorWidthMenu = new("Operator Width");

        /// <summary>Settings > Column Spacing - see <see cref="BuildColumnSpacingMenu"/>.</summary>
        private readonly ToolStripMenuItem _columnSpacingMenu = new("Column Spacing");

        /// <summary>Settings > Plan Shape - see <see cref="BuildVerticalLayoutMenu"/>.</summary>
        private readonly ToolStripMenuItem _verticalLayoutMenu = new("Plan Shape");

        /// <summary>Settings > Zoom on Open - see <see cref="BuildOpeningZoomMenu"/>.</summary>
        private readonly ToolStripMenuItem _openingZoomMenu = new("Zoom on Open");

        /// <summary>Its items are rebuilt each time it opens - see <see cref="BuildOpenWithMenu"/>.</summary>
        private ToolStripDropDownButton _openWith;
        private readonly ToolStripStatusLabel _summary = new();
        private readonly ToolStripStatusLabel _findStatus = new();

        // The status bar's sliders - see BuildStatusBar - and the labels saying where each one is.
        private readonly TrackBar _zoomSlider = NewSlider(120, ZoomSliderSteps);
        private readonly TrackBar _widthSlider = NewSlider(90, OperatorWidths.Length - 1);
        private readonly TrackBar _spacingSlider = NewSlider(70, ColumnSpacings.Length - 1);

        private readonly ToolStripStatusLabel _zoomLabel = new() { AutoSize = false, Width = 40, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStripStatusLabel _widthLabel = new() { AutoSize = false, Width = 90, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStripStatusLabel _spacingLabel = new() { AutoSize = false, Width = 50, TextAlign = ContentAlignment.MiddleLeft };

        /// <summary>Set while a slider is being put where the view is, so it does not act on its own change.</summary>
        private bool _updatingSliders;

        /// <summary>The statements offered in the selector, in the order they appear in it.</summary>
        private readonly List<PlanStatement> _selectable;

        private PlanStatement _current;

        /// <summary>Set once the statement list has been given a starting height, so a drag is not undone.</summary>
        private bool _splitterPlaced;

        // The hidden column carrying which operator a list row is about, so a double click can point
        // the graph at it.  The visible columns do not identify an operator uniquely - two scans of
        // one table differ only by node id - and the grids are sortable, so a row index is no use.
        private const string NodeIdColumn = "NodeId";

        public QueryPlanViewerControl(ExecutionPlan plan, string sourceXml, string fileName = null, DBADashContext context = null)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (plan.Statements.Count == 0) throw new ArgumentException("No statements to show.", nameof(plan));

            _sourceXml = sourceXml;
            _fileName = fileName;
            _context = context;

            // Statements with no plan still appear: the text is often the only reason the file was
            // opened, and dropping them silently would make a batch look shorter than it is.  Set
            // before the toolbar is built, which decides from it whether a selector is worth showing.
            _selectable = _plan.Statements.ToList();

            _queryText = new CodeEditor
            {
                Mode = CodeEditor.CodeEditorModes.SQL,
                IsReadOnly = true,
                WordWrap = false
            };
            _queryHost.Child = _queryText;

            _xmlText = new CodeEditor
            {
                Mode = CodeEditor.CodeEditorModes.XML,
                IsReadOnly = true,
                WordWrap = false
            };
            _xmlHost.Child = _xmlText;

            // Set before the toolbar is built, so the menu opens with the right option ticked, and
            // before the first statement is shown, so its arrows are routed that way from the start.
            _graphControl.EdgeWidthMetric = LoadEdgeWidth();
            _graphControl.EdgeWidthBasis = LoadEdgeWidthBasis();
            _graphControl.OperatorTimeMode = LoadTimeMode();
            _graphControl.NodeWidth = LoadNodeWidth();
            _graphControl.UniformColumnWidths = Properties.Settings.Default.QueryPlanUniformColumnWidths;
            _graphControl.WrapObjectNames = Properties.Settings.Default.QueryPlanWrapObjectNames;
            _graphControl.ShowNodeIds = Properties.Settings.Default.QueryPlanShowNodeIds;
            _graphControl.ColumnSpacing = LoadColumnSpacing();
            _graphControl.VerticalLayout = LoadVerticalLayout();
            _graphControl.MinAutoFitZoom = LoadMinFitZoom();
            ShowOperatorDescriptions(Properties.Settings.Default.QueryPlanShowOperatorDescriptions);

            _graphSplit.Panel1.Controls.Add(_graphControl);
            _graphSplit.Panel2.Controls.Add(_properties);

            _statementSplit.Panel1.Controls.Add(_statements);
            _statementSplit.Panel2.Controls.Add(_graphSplit);
            _statementSplit.Panel1Collapsed = !HasStatementChoice;

            _warningsTab = NewPage("Warnings", _warningsGrid);
            _missingIndexTab = NewPage("Missing Indexes", GridAndScript(_missingIndexGrid, _missingIndexScript, _missingIndexScriptSplit));
            _expressionsTab = NewPage("Expressions", _expressionsGrid);
            _parametersTab = NewPage("Parameters", GridAndScript(_parametersGrid, _parameterScript, _parameterScriptSplit));
            _waitsTab = NewPage("Waits", _waitsGrid);

            _tabs.TabPages.Add(NewPage("Plan", _statementSplit));
            _tabs.TabPages.Add(_warningsTab);
            _tabs.TabPages.Add(_missingIndexTab);
            _tabs.TabPages.Add(_expressionsTab);
            _tabs.TabPages.Add(_parametersTab);
            _tabs.TabPages.Add(_waitsTab);
            _tabs.TabPages.Add(NewPage("Query", _queryHost));
            _tabs.TabPages.Add(NewPage("AI Analysis", _ai));
            _tabs.TabPages.Add(NewPage("XML", _xmlHost));

            Controls.Add(_tabs);
            Controls.Add(BuildToolbar());
            Controls.Add(BuildStatusBar());

            _graphControl.SelectionChanged += (_, node) => ShowSelection(node);
            _graphSplit.SplitterMoved += (_, _) =>
            {
                if (!_openingProperties && !_graphSplit.Panel2Collapsed && _graphSplit.Panel2.Width > 0)
                {
                    _propertiesWidth = _graphSplit.Panel2.Width;
                }
            };
            _graphControl.NodeActivated += GraphControl_NodeActivated;
            _graphControl.ContextMenuBuilding += GraphControl_ContextMenuBuilding;
            _properties.OperatorRequested += (_, op) => _graphControl.SelectOperator(op);
            _warningsGrid.CellDoubleClick += (_, e) => SelectFromGrid(_warningsGrid, e.RowIndex);
            _missingIndexGrid.CellDoubleClick += MissingIndexGrid_CellDoubleClick;
            _expressionsGrid.CellDoubleClick += ExpressionsGrid_CellDoubleClick;
            _expressionsGrid.CellContentClick += ExpressionsGrid_CellContentClick;
            _waitsGrid.CellContentClick += WaitsGrid_CellContentClick;

            // Top of the Parameters list's right click menus - on a row, and on the empty space below the
            // rows, which is most of the list for a statement with one or two parameters.
            foreach (var menu in new[] { _parametersGrid.CellContextMenu, _parametersGrid.ColumnContextMenu })
            {
                var (runtime, compiled) = AddParameterCopyItems(menu.Items, 0, "Copy as DECLARE");
                menu.Items.Insert(2, new ToolStripSeparator());
                menu.Opening += (_, _) => EnableParameterCopyItems(runtime, compiled);
            }
            _statements.StatementSelected += (_, statement) => Show(statement);

            _statements.Show(PlanStatementSummary.For(_plan));

            // Shown once the control has its real size, which is when the splitters can be placed:
            // on Load it has been laid out in its tab, and the rest waits for that layout to settle
            // the way a form's Shown event does.
            Load += (_, _) =>
            {
                Show(_plan.PrimaryStatement ?? _plan.Statements[0]);
                BeginInvoke(() =>
                {
                    PlaceSplitter();
                    if (_graphControl.SelectedNode is null) SetPropertiesOpen(HasOverview(_current), keepView: false);
                });
            };
        }

        /// <summary>The plan XML exactly as it was opened, so the same plan opened twice is recognised.</summary>
        public string SourceXml => _sourceXml;

        /// <summary>
        /// What the plan's tab is called: the file name when it came from one, otherwise the start of
        /// its most expensive statement - the one it opens on, and the likeliest to tell two apart.
        /// </summary>
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(_fileName)) return _fileName;

                var statement = _plan.PrimaryStatement ?? _plan.Statements[0];
                var text = string.Join(" ", (statement.StatementText ?? statement.StatementType ?? "Query Plan")
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

                return text.Length <= 32 ? text : text[..31] + "…";
            }
        }

        /// <summary>The tab's tooltip: the whole file name, or the statement's text as far as it is sensible to show.</summary>
        public string TabToolTip =>
            !string.IsNullOrEmpty(_fileName)
                ? _fileName
                : ((_plan.PrimaryStatement ?? _plan.Statements[0]).StatementText is { } text && text.Length > 500
                    ? text[..500] + "…"
                    : (_plan.PrimaryStatement ?? _plan.Statements[0]).StatementText ?? string.Empty);

        /// <summary>
        /// More than one statement to choose between.  A .sqlplan of one statement is the common
        /// case, and a list with one row in it is furniture.
        /// </summary>
        private bool HasStatementChoice => _selectable.Count > 1;

        private static DBADashDataGridView NewGrid() => new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        private static TabPage NewPage(string text, Control content)
        {
            var page = new TabPage(text);
            page.Controls.Add(content);
            return page;
        }

        private static CodeEditor NewScriptEditor() => new()
        {
            Mode = CodeEditor.CodeEditorModes.SQL,
            IsReadOnly = true,
            WordWrap = false
        };

        /// <summary>
        /// A grid with a script below it.  The script starts with three fifths of the height once the
        /// tab has a real size - a SplitContainer places its splitter in pixels, and at construction
        /// it has only the default size to take a share of.  The grid rarely has more than a few rows;
        /// the script is the part that is read.
        /// </summary>
        private static SplitContainer GridAndScript(Control grid, CodeEditor script, SplitContainer split)
        {
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.Panel1.Controls.Add(grid);
            split.Panel2.Controls.Add(new ElementHost { Dock = DockStyle.Fill, Child = script });

            var placed = false;
            split.SizeChanged += (_, _) =>
            {
                if (placed || split.Height <= 0) return;
                split.SplitterDistance = Math.Max(split.Panel1MinSize, split.Height * 2 / 5);
                placed = true;
            };

            return split;
        }

        // ---------------------------------------------------------------- chrome

        private ToolStrip BuildToolbar()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            // Only offered when there is a choice to make.  Hiding the list gives its height back to
            // the plan once the statement worth reading has been found.
            if (HasStatementChoice)
            {
                var statements = new ToolStripButton(
                    "Statements (" + _selectable.Count.ToString(CultureInfo.InvariantCulture) + ")")
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    CheckOnClick = true,
                    Checked = true,
                    ToolTipText = "Show or hide the list of statements in this plan."
                };

                statements.CheckedChanged += (_, _) => _statementSplit.Panel1Collapsed = !statements.Checked;
                toolbar.Items.Add(statements);
                toolbar.Items.Add(new ToolStripSeparator());
            }

            // No zoom buttons here: the zoom is the slider on the status bar, with Fit beside it, and
            // the wheel and the keyboard as before.  Two sets of zoom controls only raise the question
            // of how they differ.

            // The metric the bars measure.  Cost is the optimiser's guess; on an actual plan the
            // measured figures are the ones worth ranking by, and a plan where the dearest operator
            // by cost and the slowest by time are different nodes is exactly the interesting case.
            toolbar.Items.Add(new ToolStripLabel("Show:"));
            toolbar.Items.Add(_metricSelector);
            _metricSelector.SelectedIndexChanged += MetricSelector_SelectedIndexChanged;

            toolbar.Items.Add(BuildLineWidthMenu());

            BuildTimeMenu();
            toolbar.Items.Add(_timeMenu);

            toolbar.Items.Add(new ToolStripSeparator());
            _dataPathButton = new ToolStripButton("Data Path", Properties.Resources.NavigationPathLeft_16x, (_, _) => ToggleDataPath())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                CheckOnClick = true,
                ToolTipText = "Fade everything off the selected operator's path back to the root."
            };
            toolbar.Items.Add(_dataPathButton);

            _expandAllButton = new ToolStripButton("Expand All", null, (_, _) => _graphControl.ExpandAll())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Show every input hidden by collapsing.",

                // Nothing is collapsed until the reader collapses something.
                Enabled = false
            };
            toolbar.Items.Add(_expandAllButton);

            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_findBox);
            _findBox.TextChanged += (_, _) => RunFind();
            _findBox.KeyDown += FindBox_KeyDown;
            toolbar.Items.Add(new ToolStripButton("Find Next", null, (_, _) => NextMatch(1))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = "Next",
                ToolTipText = "Go to the next match (F3)"
            });

            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(BuildSettingsMenu());
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripButton("Open...", Properties.Resources.FolderOpened_16x, (_, _) => Common.OpenQueryPlanFile(FindForm()))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open query plans (.sqlplan) from disk, each on a tab of its own."
            });
            toolbar.Items.Add(BuildCopyMenu());
            toolbar.Items.Add(new ToolStripButton("Save As...", Properties.Resources.Save_16x, (_, _) => SaveAs())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Save the plan XML or a picture of the plan."
            });

            // Keeps its caption where the rest of the toolbar is icons only: the icon can say the
            // plan leaves for another application, but not which one.
            _openWith = new ToolStripDropDownButton("Open With", Properties.Resources.Open_16x)
            {
                ToolTipText = "Open the plan in another application registered for .sqlplan files (e.g. SSMS)",
                Alignment = ToolStripItemAlignment.Right
            };

            // A placeholder so the drop down arrow works - the real items are listed as it opens.
            _openWith.DropDownItems.Add(new ToolStripMenuItem("Loading..."));
            _openWith.DropDownOpening += (_, _) => BuildOpenWithMenu();
            toolbar.Items.Add(_openWith);

            return toolbar;
        }

        /// <summary>
        /// The line width picker, two choices in one menu: what the arrows measure - rows or data
        /// size - and whether by what happened or by what the optimiser expected.
        ///
        /// Rows or data size because the two answer different questions - how many rows moved, and
        /// how much memory and I/O moving them cost.  Actual or estimated because switching between
        /// them is the quickest way to see where the estimates went wrong: those are the arrows that
        /// change width.  Both are remembered for next time.
        /// </summary>
        private ToolStripDropDownButton BuildLineWidthMenu()
        {
            _lineWidthMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _lineWidthMenu.ToolTipText = "What the thickness of the arrows measures.";

            AddLineWidthOption(PlanEdgeWidthMetric.Rows, "Rows",
                "Arrow thickness by the number of rows.");
            AddLineWidthOption(PlanEdgeWidthMetric.DataSize, "Data Size",
                "Arrow thickness by rows times the estimated row size - what memory grants and spills are sized by.");

            _lineWidthMenu.DropDownItems.Add(new ToolStripSeparator());

            AddLineWidthBasis(PlanEdgeWidthBasis.Actual, "Actual",
                "By what the query actually did.  Only an actual plan has these; an estimated plan is drawn by its estimates.");
            AddLineWidthBasis(PlanEdgeWidthBasis.Estimated, "Estimated",
                "By what the optimiser expected, over all the executions it expected.  Both are drawn on the same scale, so the arrows that change width are where the estimates were wrong.");
            AddLineWidthBasis(PlanEdgeWidthBasis.Both, "Actual vs Estimated",
                "Actual rows, with the estimate drawn over them as a dashed outline: green within 10x, amber from 10x, red from 100x out.  Hover an arrow for both figures.");

            ShowCurrentLineWidth();
            return _lineWidthMenu;
        }

        private void AddLineWidthOption(PlanEdgeWidthMetric metric, string text, string tip)
        {
            var item = new ToolStripMenuItem(text) { ToolTipText = tip };

            item.Click += (_, _) =>
            {
                _graphControl.EdgeWidthMetric = metric;
                ShowCurrentLineWidth();
                SaveSetting(() => Properties.Settings.Default.QueryPlanEdgeWidth = metric.ToString());
            };

            _lineWidthMetricItems[metric] = item;
            _lineWidthMenu.DropDownItems.Add(item);
        }

        private void AddLineWidthBasis(PlanEdgeWidthBasis basis, string text, string tip)
        {
            var item = new ToolStripMenuItem(text) { ToolTipText = tip };

            item.Click += (_, _) =>
            {
                _graphControl.EdgeWidthBasis = basis;
                ShowCurrentLineWidth();
                SaveSetting(() => Properties.Settings.Default.QueryPlanEdgeWidthBasis = basis.ToString());
            };

            _lineWidthBasisItems[basis] = item;
            _lineWidthMenu.DropDownItems.Add(item);
        }

        /// <summary>
        /// Ticks the options in force and names them on the button, so what the arrows measure is
        /// visible without opening the menu.
        ///
        /// Shows what the arrows are actually drawn by rather than what was chosen: on an estimated
        /// plan that is the estimates whatever the choice, and ticking Actual there would be ticking
        /// something the picture is not showing.  The choice itself is kept, for the next actual plan.
        /// </summary>
        private void ShowCurrentLineWidth()
        {
            var metric = _graphControl.EdgeWidthMetric;
            var basis = _graphControl.EffectiveEdgeWidthBasis;

            foreach (var (value, item) in _lineWidthMetricItems) item.Checked = value == metric;
            foreach (var (value, item) in _lineWidthBasisItems) item.Checked = value == basis;

            // Both need actual rows to show; an estimated plan has only its estimates.
            _lineWidthBasisItems[PlanEdgeWidthBasis.Actual].Enabled = _graphControl.HasActualRows;
            _lineWidthBasisItems[PlanEdgeWidthBasis.Both].Enabled = _graphControl.HasActualRows;

            var measure = metric == PlanEdgeWidthMetric.DataSize ? "Data Size" : "Rows";

            _lineWidthMenu.Text = "Line Width: " + basis switch
            {
                PlanEdgeWidthBasis.Actual => "Actual " + measure,
                PlanEdgeWidthBasis.Both => measure + ", Actual vs Estimated",
                _ => "Estimated " + measure
            };
        }

        /// <summary>
        /// The line width choices are per user preferences held locally: the viewer opens plans from
        /// a file as well as from the repository, so it cannot depend on being connected to one.
        /// </summary>
        private static PlanEdgeWidthMetric LoadEdgeWidth() =>
            Enum.TryParse<PlanEdgeWidthMetric>(Properties.Settings.Default.QueryPlanEdgeWidth, out var metric)
                ? metric
                : PlanEdgeWidthMetric.Rows;

        private static PlanEdgeWidthBasis LoadEdgeWidthBasis() =>
            Enum.TryParse<PlanEdgeWidthBasis>(Properties.Settings.Default.QueryPlanEdgeWidthBasis, out var basis)
                ? basis
                : PlanEdgeWidthBasis.Actual;

        private static void SaveSetting(Action set)
        {
            try
            {
                set();
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // A preference that cannot be saved is not worth interrupting anyone over.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// The operator time picker: each operator's own time, or the times as SQL Server reported
        /// them.  A choice because SSMS shows the reported figures and plenty of readers know plans by
        /// those - but on a row mode plan they include every input, so the operators nearest the root
        /// always look the slowest, and own time is what finds the one that took it.  Remembered for
        /// next time.
        /// </summary>
        private void BuildTimeMenu()
        {
            _timeMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;

            AddTimeOption(OperatorTimeMode.Own, "Per Node",
                "Each operator's own elapsed and CPU time, with the time of the operators feeding it taken out.  Finds the operator that took the time.");
            AddTimeOption(OperatorTimeMode.AsReported, "Cumulative (SSMS)",
                "The times exactly as SQL Server reported them, which is what SSMS shows.  A row mode operator's time includes the operators feeding it; a batch mode operator's does not.");

            ShowCurrentTimeMode();
        }

        private void AddTimeOption(OperatorTimeMode mode, string text, string tip)
        {
            var item = new ToolStripMenuItem(text) { ToolTipText = tip, Tag = mode };

            item.Click += (_, _) =>
            {
                _graphControl.OperatorTimeMode = mode;
                ShowCurrentTimeMode();
                SaveSetting(() => Properties.Settings.Default.QueryPlanOperatorTime = mode.ToString());
            };

            _timeMenu.DropDownItems.Add(item);
        }

        /// <summary>Ticks the chosen option and names it on the button, like the line width menu.</summary>
        private void ShowCurrentTimeMode()
        {
            var mode = _graphControl.OperatorTimeMode;

            foreach (var item in _timeMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (OperatorTimeMode)item.Tag! == mode;
            }

            _timeMenu.Text = mode == OperatorTimeMode.AsReported ? "Times: Cumulative" : "Times: Per Node";
        }

        /// <summary>
        /// Only offered when the statement has operator times to show, and explained when the choice
        /// makes no difference: SQL Server 2022 can report every operator's own time itself, and then
        /// the two options show the same figures.
        /// </summary>
        private void UpdateTimeMenu(PlanStatement statement)
        {
            _timeMenu.Enabled = statement.Operators.Any(o => o.Runtime?.ActualElapsedMs is not null || o.Runtime?.ActualCpuMs is not null);

            _timeMenu.ToolTipText = statement.ExclusiveProfileTimeActive == true
                ? "What the operator times show.  This plan already reports each operator's own time, so both options show the same figures."
                : "What the operator times show: each operator's own, or as reported, where a row mode operator's time includes its inputs.";
        }

        private static OperatorTimeMode LoadTimeMode() =>
            Enum.TryParse<OperatorTimeMode>(Properties.Settings.Default.QueryPlanOperatorTime, out var mode)
                ? mode
                : OperatorTimeMode.Own;

        /// <summary>
        /// The operator widths on offer, narrowest first, as the menu's wording and the short name the
        /// slider shows.  One list so the menu and the slider cannot drift apart, and in the order the
        /// slider runs.
        /// </summary>
        private static readonly (PlanNodeWidth Width, string Text, string Short, string Tip)[] OperatorWidths =
        [
            (PlanNodeWidth.Stacked, "Stacked (Icon Above Text)", "Stacked",
                "The narrowest: the icon above the operator's name, as SSMS draws it, with long names and figures wrapping onto more lines.  Operators are taller but take far less width."),
            (PlanNodeWidth.SuperCompact, "Super Compact", "Super Compact",
                "As narrow as an operator can usefully be: its icon and the start of its name.  For seeing the shape of a very deep plan; hover an operator for the rest."),
            (PlanNodeWidth.Compact, "Compact", "Compact",
                "Narrow operators, so a deep plan fits the window.  Long names are cut short sooner."),
            (PlanNodeWidth.Normal, "Normal", "Normal",
                "Wide enough for most operator and object names."),
            (PlanNodeWidth.Wide, "Wide", "Wide",
                "Room for longer object and index names."),
            (PlanNodeWidth.Widest, "Widest", "Widest",
                "Wide enough that names are rarely cut short, at the cost of a much wider plan.")
        ];

        /// <summary>The column spacings on offer, tightest first - see <see cref="OperatorWidths"/>.</summary>
        private static readonly (PlanColumnSpacing Spacing, string Text, string Tip)[] ColumnSpacings =
        [
            (PlanColumnSpacing.Tight, "Tight",
                "Just room for the arrows, so a deep plan fits.  Row counts may crowd the arrows' corners."),
            (PlanColumnSpacing.Normal, "Normal",
                "Room for a row count and the corner of a thick arrow."),
            (PlanColumnSpacing.Wide, "Wide",
                "More room, so the arrows and their row counts stand apart."),
            (PlanColumnSpacing.Widest, "Widest",
                "Plenty of room between columns, for reading the arrows rather than fitting the plan.")
        ];

        /// <summary>
        /// How wide the operators are drawn: narrow so a deep plan fits, or wide so long object and
        /// index names are not cut short - and whether a column's operators share one width.  Both
        /// lay the plan out again, keeping the selection, and are remembered for next time.
        /// </summary>
        private ToolStripMenuItem BuildOperatorWidthMenu()
        {
            _operatorWidthMenu.ToolTipText = "How wide the operators in the plan are drawn.";

            foreach (var (width, text, _, tip) in OperatorWidths) AddOperatorWidthOption(width, text, tip);

            _operatorWidthMenu.DropDownItems.Add(new ToolStripSeparator());

            var uniform = new ToolStripMenuItem("Same Width in Each Column")
            {
                CheckOnClick = true,
                Checked = _graphControl.UniformColumnWidths,
                ToolTipText = "Give every operator in a column the width of the widest, so the plan reads as a sequence of stages.  Off sizes each operator to its own text."
            };

            uniform.CheckedChanged += (_, _) =>
            {
                _graphControl.UniformColumnWidths = uniform.Checked;
                SaveSetting(() => Properties.Settings.Default.QueryPlanUniformColumnWidths = uniform.Checked);
            };

            _operatorWidthMenu.DropDownItems.Add(uniform);

            var wrapNames = new ToolStripMenuItem("Wrap Object Names")
            {
                CheckOnClick = true,
                Checked = _graphControl.WrapObjectNames,
                ToolTipText = "Wrap a table or index name too long for the operator onto more lines, rather than cutting it short.  Operators with long names get taller."
            };

            wrapNames.CheckedChanged += (_, _) =>
            {
                _graphControl.WrapObjectNames = wrapNames.Checked;
                SaveSetting(() => Properties.Settings.Default.QueryPlanWrapObjectNames = wrapNames.Checked);
            };

            _operatorWidthMenu.DropDownItems.Add(wrapNames);

            ShowCurrentOperatorWidth();
            return _operatorWidthMenu;
        }

        private void AddOperatorWidthOption(PlanNodeWidth width, string text, string tip)
        {
            var item = new ToolStripMenuItem(text) { ToolTipText = tip, Tag = width };

            item.Click += (_, _) =>
            {
                _graphControl.NodeWidth = width;
                ShowCurrentOperatorWidth();
                SaveSetting(() => Properties.Settings.Default.QueryPlanNodeWidth = width.ToString());
            };

            _operatorWidthMenu.DropDownItems.Add(item);
        }

        /// <summary>Ticks the width in force on the menu and puts the slider and its name on it.</summary>
        private void ShowCurrentOperatorWidth()
        {
            var width = _graphControl.NodeWidth;

            foreach (var item in _operatorWidthMenu.DropDownItems.OfType<ToolStripMenuItem>().Where(i => i.Tag is PlanNodeWidth))
            {
                item.Checked = (PlanNodeWidth)item.Tag == width;
            }

            var position = Array.FindIndex(OperatorWidths, w => w.Width == width);
            if (position < 0) return;

            _widthLabel.Text = OperatorWidths[position].Short;
            SetSlider(_widthSlider, position);
        }

        /// <summary>Moves a slider without its handler acting on the change it did not come from.</summary>
        private void SetSlider(TrackBar slider, int value)
        {
            if (slider.Value == value) return;

            _updatingSliders = true;
            try
            {
                slider.Value = value;
            }
            finally
            {
                _updatingSliders = false;
            }
        }

        private static PlanNodeWidth LoadNodeWidth() =>
            Enum.TryParse<PlanNodeWidth>(Properties.Settings.Default.QueryPlanNodeWidth, out var width)
                ? width
                : PlanNodeWidth.Normal;

        /// <summary>
        /// How much room is left between the columns of operators.  Paid once per column, so on a
        /// deep plan it matters as much as the operator width.  Remembered for next time.
        /// </summary>
        private ToolStripMenuItem BuildColumnSpacingMenu()
        {
            _columnSpacingMenu.ToolTipText = "How much room is left between the columns of operators.";

            foreach (var (spacing, text, tip) in ColumnSpacings) AddColumnSpacingOption(spacing, text, tip);

            ShowCurrentColumnSpacing();
            return _columnSpacingMenu;
        }

        private void AddColumnSpacingOption(PlanColumnSpacing spacing, string text, string tip)
        {
            var item = new ToolStripMenuItem(text) { ToolTipText = tip, Tag = spacing };

            item.Click += (_, _) =>
            {
                _graphControl.ColumnSpacing = spacing;
                ShowCurrentColumnSpacing();
                SaveSetting(() => Properties.Settings.Default.QueryPlanColumnSpacing = spacing.ToString());
            };

            _columnSpacingMenu.DropDownItems.Add(item);
        }

        private void ShowCurrentColumnSpacing()
        {
            var spacing = _graphControl.ColumnSpacing;

            foreach (var item in _columnSpacingMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (PlanColumnSpacing)item.Tag! == spacing;
            }

            var position = Array.FindIndex(ColumnSpacings, s => s.Spacing == spacing);
            if (position < 0) return;

            _spacingLabel.Text = ColumnSpacings[position].Text;
            SetSlider(_spacingSlider, position);
        }

        /// <summary>The plan shapes on offer - see <see cref="OperatorWidths"/>.</summary>
        private static readonly (PlanVerticalLayout Layout, string Text, string Tip)[] VerticalLayouts =
        [
            (PlanVerticalLayout.Centred, "Centred on Inputs",
                "Put each operator level with the middle of what feeds it.  The easier shape to follow a join on, and the taller one - every branch pushes the rest of the plan further down."),
            (PlanVerticalLayout.FirstChildAligned, "Aligned to First Input (SSMS)",
                "Put each operator level with its first input and drop the rest below, as SSMS draws a plan.  Much shorter on a branching plan, at the cost of an operator no longer sitting between its inputs.")
        ];

        /// <summary>
        /// Whether an operator sits between its inputs or level with the first of them.  The second
        /// is how SSMS draws a plan, and is far shorter on a plan with many branches - which is what
        /// gets a big plan on screen once the width has been squeezed as far as it will go.  Lays the
        /// plan out again, keeping the selection, and is remembered for next time.
        /// </summary>
        private ToolStripMenuItem BuildVerticalLayoutMenu()
        {
            _verticalLayoutMenu.ToolTipText = "Where an operator sits against the inputs that feed it.";

            foreach (var (layout, text, tip) in VerticalLayouts)
            {
                var item = new ToolStripMenuItem(text) { ToolTipText = tip, Tag = layout };

                item.Click += (_, _) =>
                {
                    _graphControl.VerticalLayout = layout;
                    ShowCurrentVerticalLayout();
                    SaveSetting(() => Properties.Settings.Default.QueryPlanVerticalLayout = layout.ToString());
                };

                _verticalLayoutMenu.DropDownItems.Add(item);
            }

            ShowCurrentVerticalLayout();
            return _verticalLayoutMenu;
        }

        private void ShowCurrentVerticalLayout()
        {
            var layout = _graphControl.VerticalLayout;

            foreach (var item in _verticalLayoutMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (PlanVerticalLayout)item.Tag! == layout;
            }
        }

        private static PlanVerticalLayout LoadVerticalLayout() =>
            Enum.TryParse<PlanVerticalLayout>(Properties.Settings.Default.QueryPlanVerticalLayout, out var layout)
                ? layout
                : PlanVerticalLayout.FirstChildAligned;

        /// <summary>
        /// How far out a plan may be zoomed when it is opened, as a percentage - zero meaning fit it
        /// however far out that takes.  Actual size by default: a big plan shrunk to fit is a picture
        /// of a plan rather than a plan anyone can read, so it opens full size and is scrolled.
        /// Whatever this says, the Fit button still fits the whole plan.
        /// </summary>
        private static readonly (int Percent, string Text, string Tip)[] OpeningZooms =
        [
            (0, "Fit to Window",
                "Zoom out as far as it takes to show the whole plan, however small that makes it."),
            (50, "No Smaller Than 50%",
                "Fit the plan, but never zoom out past half size - a plan smaller than that opens at 50% and is scrolled."),
            (75, "No Smaller Than 75%",
                "Fit the plan, but never zoom out past three quarters."),
            (100, "Actual Size (100%)",
                "Open at full size, zoomed in only when there is room to spare.  A plan bigger than the window is scrolled, or fitted with the Fit button.")
        ];

        /// <summary>The zoom a plan opens at - see <see cref="OpeningZooms"/>.  Remembered for next time.</summary>
        private ToolStripMenuItem BuildOpeningZoomMenu()
        {
            _openingZoomMenu.ToolTipText =
                "How far out a plan may be zoomed when it is opened or the window resized.  The Fit button always fits the whole plan.";

            foreach (var (percent, text, tip) in OpeningZooms)
            {
                var item = new ToolStripMenuItem(text) { ToolTipText = tip, Tag = percent };

                item.Click += (_, _) =>
                {
                    _graphControl.MinAutoFitZoom = percent / 100.0;
                    ShowCurrentOpeningZoom();
                    SaveSetting(() => Properties.Settings.Default.QueryPlanMinFitZoom = percent);
                };

                _openingZoomMenu.DropDownItems.Add(item);
            }

            ShowCurrentOpeningZoom();
            return _openingZoomMenu;
        }

        private void ShowCurrentOpeningZoom()
        {
            var percent = (int)Math.Round(_graphControl.MinAutoFitZoom * 100);

            foreach (var item in _openingZoomMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (int)item.Tag! == percent;
            }
        }

        private static double LoadMinFitZoom()
        {
            var percent = Properties.Settings.Default.QueryPlanMinFitZoom;
            return OpeningZooms.Any(z => z.Percent == percent) ? percent / 100.0 : 1.0;
        }

        private static PlanColumnSpacing LoadColumnSpacing() =>
            Enum.TryParse<PlanColumnSpacing>(Properties.Settings.Default.QueryPlanColumnSpacing, out var spacing)
                ? spacing
                : PlanColumnSpacing.Normal;

        /// <summary>
        /// Copy: the picture, or the plan itself.  A menu rather than one button because which of
        /// the two is wanted depends entirely on where it is going - a ticket wants the picture, a
        /// colleague wants the plan they can open.
        /// </summary>
        private ToolStripDropDownButton BuildCopyMenu()
        {
            var menu = new ToolStripDropDownButton("Copy", Properties.Resources.ASX_Copy_blue_16x)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Copy the plan, a picture of it, or T-SQL from it."
            };

            menu.DropDownItems.Add(new ToolStripMenuItem("Copy Whole Plan as Image", null, (_, _) => CopyImage(whole: true))
            {
                ToolTipText = "The entire plan at full size, not just the part that fits the window."
            });
            menu.DropDownItems.Add(new ToolStripMenuItem("Copy Current View as Image", null, (_, _) => CopyImage(whole: false)));
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(new ToolStripMenuItem("Copy Plan XML", null, (_, _) => CopyText(_sourceXml)));
            menu.DropDownItems.Add(new ToolStripMenuItem("Copy Query Text", null, (_, _) => CopyText(_current?.StatementText)));
            menu.DropDownItems.Add(new ToolStripMenuItem("Copy Properties", null, (_, _) => CopyText(_properties.ToText())));
            menu.DropDownItems.Add(new ToolStripSeparator());

            var indexes = new ToolStripMenuItem("Copy Missing Index T-SQL", null, (_, _) => CopyText(PlanScripts.MissingIndexes(_current)))
            {
                ToolTipText = "CREATE INDEX statements for the statement's missing indexes, with the optimizer's estimates."
            };
            menu.DropDownItems.Add(indexes);
            var (runtime, compiled) = AddParameterCopyItems(menu.DropDownItems, int.MaxValue, "Copy Parameters as DECLARE");

            // Only offered when the statement shown has something to script.
            menu.DropDownOpening += (_, _) =>
            {
                indexes.Enabled = _current?.MissingIndexes.Count > 0;
                EnableParameterCopyItems(runtime, compiled);
            };

            return menu;
        }

        /// <summary>The tooltip and the properties panel both, so they never disagree.</summary>
        private void ShowOperatorDescriptions(bool show)
        {
            _graphControl.ShowOperatorDescriptions = show;
            _properties.ShowDescriptions = show;
        }

        /// <summary>
        /// The viewer's settings, and the Windows integration: offering DBA Dash for .sqlplan files,
        /// and making it the default.
        ///
        /// The integration is offered from the viewer rather than done at startup, because putting an
        /// application in Explorer's Open with menu is a change to the machine and not ours to make
        /// uninvited.
        /// </summary>
        private ToolStripDropDownButton BuildSettingsMenu()
        {
            var menu = new ToolStripDropDownButton("Settings") { ToolTipText = "Viewer and file association settings." };

            // For readers still learning the operators; those who know them can have the figures alone.
            var descriptions = _descriptionsItem = new ToolStripMenuItem("Show Operator Descriptions")
            {
                CheckOnClick = true,
                Checked = _graphControl.ShowOperatorDescriptions,
                ToolTipText = "Say what each operator does, on its tooltip and above its properties."
            };

            descriptions.CheckedChanged += (_, _) =>
            {
                ShowOperatorDescriptions(descriptions.Checked);
                SaveSetting(() => Properties.Settings.Default.QueryPlanShowOperatorDescriptions = descriptions.Checked);
            };

            // Beside the descriptions: both are about what the operators say rather than how the plan
            // is laid out.  Off by default - see PlanLayoutOptions.ShowNodeIds.
            var nodeIds = _nodeIdsItem = new ToolStripMenuItem("Show Node IDs")
            {
                CheckOnClick = true,
                Checked = _graphControl.ShowNodeIds,
                ToolTipText = "Put each operator's node id on it, so the nodes the cards, the warnings list and the properties panel name can be found in the plan."
            };

            nodeIds.CheckedChanged += (_, _) =>
            {
                _graphControl.ShowNodeIds = nodeIds.Checked;
                SaveSetting(() => Properties.Settings.Default.QueryPlanShowNodeIds = nodeIds.Checked);
            };

            menu.DropDownItems.Add(descriptions);
            menu.DropDownItems.Add(nodeIds);
            menu.DropDownItems.Add(BuildOperatorWidthMenu());
            menu.DropDownItems.Add(BuildColumnSpacingMenu());
            menu.DropDownItems.Add(BuildVerticalLayoutMenu());
            menu.DropDownItems.Add(BuildOpeningZoomMenu());
            menu.DropDownItems.Add(new ToolStripSeparator());

            var offer = new ToolStripMenuItem("Open .sqlplan Files with DBA Dash", null, (_, _) => ToggleFileAssociation());
            var makeDefault = new ToolStripMenuItem("Make DBA Dash the Default...", null, (_, _) => MakeDefault())
            {
                ToolTipText = "Opens Settings > Default apps.  Windows only lets you choose the default yourself."
            };

            menu.DropDownItems.Add(offer);
            menu.DropDownItems.Add(makeDefault);

            // Read as the menu opens: the registration can be changed from another copy of DBA Dash,
            // or by the user in Default Apps, while this window is sitting open.
            menu.DropDownOpening += (_, _) =>
            {
                try
                {
                    var association = FileAssociation.QueryPlan;
                    var registered = association.RegisteredExePath;

                    offer.Checked = association.IsRegisteredToThisCopy;
                    offer.ToolTipText = offer.Checked || registered == null
                        ? "Offer DBA Dash in Explorer's Open with menu for execution plan (.sqlplan) files.  They open in this viewer without starting the full GUI."
                        : $"Currently registered to another copy of DBA Dash:\n{registered}\n\nClick to use this copy instead.";

                    makeDefault.Enabled = !association.IsDefaultHandler;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            };

            return menu;
        }

        private void ToggleFileAssociation()
        {
            try
            {
                var association = FileAssociation.QueryPlan;

                if (association.IsRegisteredToThisCopy)
                {
                    association.Unregister();
                }
                else
                {
                    association.Register();
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error updating the .sqlplan file association");
            }
        }

        private void MakeDefault()
        {
            try
            {
                FileAssociation.QueryPlan.OpenDefaultAppsSettings();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Could not open Default apps settings");
            }
        }

        /// <summary>
        /// The status bar: what the statement adds up to, then the three settings worth changing while
        /// looking at the plan rather than from a menu - how far in it is zoomed, how wide the
        /// operators are, and how much room is between the columns.
        ///
        /// Sliders because all three are a scale with an order to it, and the answer is found by
        /// trying it: dragging one and watching the plan reflow beats opening a menu, choosing, and
        /// opening it again.  They stay in step with the same menus, which is where the wording
        /// explaining each choice lives.
        /// </summary>
        private StatusStrip BuildStatusBar()
        {
            var status = new StatusStrip();

            _zoomSlider.ValueChanged += (_, _) =>
            {
                if (_updatingSliders) return;

                // Zooming raises the view's own change, which would put the slider back where it
                // thinks the zoom is - a pixel out, mid drag.  The thumb is already where the user
                // put it, so only the percentage needs saying.
                _updatingSliders = true;
                try
                {
                    _graphControl.SetZoom(ZoomFor(_zoomSlider.Value));
                }
                finally
                {
                    _updatingSliders = false;
                }

                ShowCurrentZoom();
            };

            _widthSlider.ValueChanged += (_, _) =>
            {
                if (_updatingSliders) return;

                var width = OperatorWidths[_widthSlider.Value].Width;
                _graphControl.NodeWidth = width;
                ShowCurrentOperatorWidth();
                SaveSetting(() => Properties.Settings.Default.QueryPlanNodeWidth = width.ToString());
            };

            _spacingSlider.ValueChanged += (_, _) =>
            {
                if (_updatingSliders) return;

                var spacing = ColumnSpacings[_spacingSlider.Value].Spacing;
                _graphControl.ColumnSpacing = spacing;
                ShowCurrentColumnSpacing();
                SaveSetting(() => Properties.Settings.Default.QueryPlanColumnSpacing = spacing.ToString());
            };

            // First, at the left.  A status bar drops the items it has no room for off its right hand
            // end, so anything after the summary is the first thing to disappear as the window
            // narrows - and these are controls, which are worth more than the line of figures beside
            // them.  The summary is on the statement list too, and in full on the tooltip.
            AddSlider(status, "Zoom", _zoomSlider, _zoomLabel, false,
                "How far the plan is zoomed in.  The mouse wheel and the zoom buttons do the same.");

            // Beside the slider, where the zoom is being set: fitting the whole plan is the one zoom
            // nobody wants to find by dragging.
            status.Items.Add(new ToolStripButton("Fit", Properties.Resources.ZoomToFit, (_, _) => _graphControl.ZoomToFit())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Fit the whole plan in the window (0)"
            });
            AddSlider(status, "Operators", _widthSlider, _widthLabel, true,
                "How wide the operators are drawn - the same choices as Settings > Operator Width.");
            AddSlider(status, "Columns", _spacingSlider, _spacingLabel, true,
                "How much room is left between the columns of operators.");

            // Last, taking whatever width is left over, and giving it up first.
            status.Items.Add(new ToolStripSeparator());
            _summary.Spring = true;
            _summary.TextAlign = ContentAlignment.MiddleLeft;
            status.Items.Add(_summary);
            status.Items.Add(_findStatus);

            // The wheel and the keyboard zoom too, so the slider follows the view rather than only
            // driving it.
            _graphControl.ViewChanged += (_, _) =>
            {
                ShowCurrentZoom();
                ShowCollapsedState();
            };

            ShowCurrentZoom();
            ShowCurrentOperatorWidth();
            ShowCurrentColumnSpacing();

            return status;
        }

        /// <summary>
        /// What the statement adds up to, on the status bar and in full on its tooltip - the label
        /// gives up its width to the sliders first, so what it shows is often the start of it.
        /// </summary>
        private void SetSummary(string text)
        {
            _summary.Text = text;
            _summary.ToolTipText = text;
        }

        private static void AddSlider(
            StatusStrip status, string caption, TrackBar slider, ToolStripStatusLabel value, bool separator, string tip)
        {
            if (separator) status.Items.Add(new ToolStripSeparator());
            status.Items.Add(new ToolStripStatusLabel(caption + ":") { ToolTipText = tip });
            status.Items.Add(new ToolStripControlHost(slider)
            {
                AutoSize = false,
                Width = slider.Width,
                ToolTipText = tip,
                AutoToolTip = true
            });

            value.ToolTipText = tip;
            status.Items.Add(value);
        }

        /// <summary>
        /// Positions on the zoom slider.  Enough that a drag is smooth rather than stepped, since the
        /// zoom it sets is continuous - unlike the other two, which have a position per choice.
        /// </summary>
        private const int ZoomSliderSteps = 100;

        /// <summary>A slider for the status bar: no ticks, and short enough to leave the summary room.</summary>
        private static TrackBar NewSlider(int width, int max) => new()
        {
            Minimum = 0,
            Maximum = max,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Width = width,
            Height = 20
        };

        /// <summary>
        /// The zoom a slider position means.  Geometric rather than linear: the useful zooms run from
        /// a tenth to eight times, and on a linear scale everything under actual size would be squeezed
        /// into the first eighth of the slider.
        /// </summary>
        private double ZoomFor(int value)
        {
            var (min, max) = _graphControl.ZoomRange;
            return min * Math.Pow(max / min, (double)value / _zoomSlider.Maximum);
        }

        private int SliderFor(double zoom)
        {
            var (min, max) = _graphControl.ZoomRange;
            var fraction = Math.Log(Math.Clamp(zoom, min, max) / min) / Math.Log(max / min);

            return (int)Math.Round(fraction * _zoomSlider.Maximum);
        }

        /// <summary>
        /// Grey out Expand All while there is nothing hidden to put back, which is how the right
        /// click menu has always treated it - it leaves the item out entirely.  A button that is
        /// always available for an action that usually does nothing teaches the reader to doubt it.
        ///
        /// Driven off the view's change event, so it follows a collapse made from the node's own
        /// control, from Space, or from the right click menu, none of which the toolbar hears about
        /// directly.
        /// </summary>
        private void ShowCollapsedState()
        {
            if (_expandAllButton is null) return;

            // Only on a change: that event is raised on every pan and every hover, and assigning
            // Enabled invalidates the toolbar whether or not the value actually moved.
            var expandable = _graphControl.HasCollapsedNodes;
            if (_expandAllButton.Enabled != expandable) _expandAllButton.Enabled = expandable;
        }

        /// <summary>
        /// Put the zoom slider and its percentage where the view actually is - after a drag of the
        /// slider itself, but also after a wheel zoom, a Fit, or a statement being opened.
        /// </summary>
        private void ShowCurrentZoom()
        {
            var zoom = _graphControl.Zoom;
            _zoomLabel.Text = (zoom * 100).ToString("0", CultureInfo.CurrentCulture) + "%";

            if (!_updatingSliders) SetSlider(_zoomSlider, SliderFor(zoom));
        }

        /// <summary>
        /// Give the statement list its starting height once the viewer has a real size.
        ///
        /// SplitterDistance is validated against the size the container has now, which at
        /// construction is the default, so setting it early either throws or is silently wrong.
        /// </summary>
        private void PlaceSplitter()
        {
            if (_splitterPlaced) return;

            // The statement list gets room for its first few rows as they are actually sized - short
            // statements make short rows - but never more than two fifths of the tab: it is there to
            // find a statement with, and the plan is what is read once it is found.
            if (HasStatementChoice && _statementSplit.Height > 0)
            {
                var wanted = _statements.PreferredHeight(5);
                _statementSplit.SplitterDistance = Math.Max(60, Math.Min(wanted, _statementSplit.Height * 2 / 5));

                // Once the resize has been laid out, so the grid knows how much room it now has.
                BeginInvoke(_statements.RevealShown);
            }

            _splitterPlaced = true;
        }

        // ---------------------------------------------------------------- showing a statement

        private void Show(PlanStatement statement)
        {
            // Choosing the row that is already showing - a click on the selected row - re-lays the
            // plan out for nothing and throws away the reader's zoom and selection.
            if (ReferenceEquals(_current, statement)) return;

            _current = statement;

            // The list follows when the statement was changed from elsewhere, such as the initial
            // choice of the most expensive statement.
            _statements.Select(statement);

            // A new statement starts with nothing selected, which shows its overview when it has
            // anything worth reading.  Opened or closed before the new plan is laid out, so it is
            // fitted to the width it will actually have.  Before the viewer is shown the split has no
            // real width to place the panel by, so the first statement's is opened once it has.
            _properties.Show(null, statement);
            SetPropertiesOpen(_splitterPlaced && HasOverview(statement), keepView: false);

            _graphControl.LoadStatement(statement);

            PopulateMetricSelector();
            ShowCurrentLineWidth();

            // Explicitly: the new statement is laid out with nothing collapsed, but moving to it
            // leaves the canvas the same size, so the view raises no change of its own to follow.
            ShowCollapsedState();
            UpdateTimeMenu(statement);
            ShowWarnings(statement);
            ShowMissingIndexes(statement);
            ShowExpressions(statement);
            ShowParameters(statement);
            ShowWaits(statement);

            _queryText.Text = statement.StatementText ?? string.Empty;
            _xmlText.Text = _sourceXml ?? string.Empty;

            // The AI tab is about one statement, so it follows the selector.  It contacts nothing until
            // the reader presses its own button - moving between statements costs nothing.
            _ai.Show(_plan, statement, _fileName, _context);

            SetSummary(Summarise(statement, _graphControl.PlanLayout));

            RunFind();
            this.ApplyTheme();
        }

        /// <summary>
        /// What the status bar says about the statement: the shape of the plan, and the two or three
        /// figures that say whether it is worth looking at.
        /// </summary>
        private static string Summarise(PlanStatement statement, PlanLayout layout)
        {
            var parts = new List<string>();

            var operators = layout.Nodes.Count - 1;
            parts.Add(operators.ToString(CultureInfo.InvariantCulture) +
                      (operators == 1 ? " operator" : " operators"));

            parts.Add(statement.IsActualPlan ? "actual plan" : "estimated plan");
            parts.Add("cost " + statement.StatementSubTreeCost.ToString("0.###", CultureInfo.InvariantCulture));

            if (statement.DegreeOfParallelism is > 1)
            {
                parts.Add("DOP " + statement.DegreeOfParallelism.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (statement.QueryTimeStats?.ElapsedMs is { } elapsed)
            {
                parts.Add(elapsed.ToString("N0", CultureInfo.InvariantCulture) + " ms elapsed");
            }

            if (statement.MemoryGrant?.GrantedMemoryKb is { } granted)
            {
                parts.Add("grant " + granted.ToString("N0", CultureInfo.InvariantCulture) + " KB");
            }

            return string.Join("  ·  ", parts);
        }

        // ---------------------------------------------------------------- the metric picker

        /// <summary>
        /// Offer only the metrics this plan can actually support: an estimated plan has no CPU to
        /// rank by, and offering the choice would be offering an empty picture.
        /// </summary>
        private void PopulateMetricSelector()
        {
            var selected = _graphControl.HeatMetric;

            _metricSelector.SelectedIndexChanged -= MetricSelector_SelectedIndexChanged;
            _metricSelector.Items.Clear();

            foreach (var metric in Enum.GetValues<PlanHeatMetric>())
            {
                if (!_graphControl.Supports(metric)) continue;
                _metricSelector.Items.Add(new MetricChoice(metric));
            }

            var index = _metricSelector.Items
                .Cast<MetricChoice>()
                .ToList()
                .FindIndex(c => c.Metric == selected);

            // A statement with no plan supports no metric at all, and selecting into an empty list
            // throws - so the picker is left empty and disabled rather than taking the viewer down.
            _metricSelector.Enabled = _metricSelector.Items.Count > 0;
            if (_metricSelector.Items.Count > 0) _metricSelector.SelectedIndex = index >= 0 ? index : 0;

            _metricSelector.SelectedIndexChanged += MetricSelector_SelectedIndexChanged;
        }

        private void MetricSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_metricSelector.SelectedItem is MetricChoice choice) _graphControl.HeatMetric = choice.Metric;
        }

        /// <summary>A metric in the dropdown, so the list carries the value rather than parsing its text.</summary>
        private sealed class MetricChoice
        {
            public MetricChoice(PlanHeatMetric metric) => Metric = metric;

            public PlanHeatMetric Metric { get; }

            public override string ToString() => PlanLayoutMetrics.NameOf(Metric);
        }

        private void ToggleDataPath()
        {
            _graphControl.FollowDataPath = !_graphControl.FollowDataPath;
        }

        /// <summary>
        /// The plan's view options on the right click menu as well as the toolbar, where they are to
        /// hand while looking at the plan rather than across the window from it.  Each item works the
        /// toolbar control it mirrors, so there is one place an option is changed and saved, and the
        /// toolbar always shows what the menu chose.
        /// </summary>
        private void GraphControl_ContextMenuBuilding(object sender, QueryPlanMenuEventArgs e)
        {
            if (e.Items.Count > 0) e.Items.Add(new ToolStripSeparator());

            var show = new ToolStripMenuItem("Show") { ToolTipText = "What the bars and heat colouring measure." };
            foreach (var choice in _metricSelector.Items.OfType<MetricChoice>())
            {
                show.DropDownItems.Add(new ToolStripMenuItem(choice.ToString(), null, (_, _) => _metricSelector.SelectedItem = choice)
                {
                    Checked = ReferenceEquals(_metricSelector.SelectedItem, choice)
                });
            }

            show.Enabled = _metricSelector.Enabled && show.DropDownItems.Count > 0;
            e.Items.Add(show);

            e.Items.Add(Mirror("Line Width", _lineWidthMenu));
            e.Items.Add(Mirror("Operator Times", _timeMenu));
            e.Items.Add(Mirror("Operator Width", _operatorWidthMenu));
            e.Items.Add(Mirror("Column Spacing", _columnSpacingMenu));
            e.Items.Add(Mirror("Plan Shape", _verticalLayoutMenu));

            e.Items.Add(new ToolStripMenuItem("Follow Data Path", Properties.Resources.NavigationPathLeft_16x, (_, _) => _dataPathButton.PerformClick())
            {
                Checked = _dataPathButton.Checked,
                ToolTipText = _dataPathButton.ToolTipText
            });

            e.Items.Add(new ToolStripMenuItem("Show Operator Descriptions", null, (_, _) => _descriptionsItem.Checked = !_descriptionsItem.Checked)
            {
                Checked = _descriptionsItem.Checked,
                ToolTipText = _descriptionsItem.ToolTipText
            });

            // Beside the descriptions, as on the Settings menu: this is the menu open in front of the
            // reader who has just been told a warning is on node 9 and is looking for node 9.
            e.Items.Add(new ToolStripMenuItem("Show Node IDs", null, (_, _) => _nodeIdsItem.Checked = !_nodeIdsItem.Checked)
            {
                Checked = _nodeIdsItem.Checked,
                ToolTipText = _nodeIdsItem.ToolTipText
            });
        }

        /// <summary>
        /// A copy of a toolbar drop down for the right click menu: the same items, ticked and enabled
        /// as they are now, each clicking the original.
        /// </summary>
        private static ToolStripMenuItem Mirror(string text, ToolStripDropDownItem source)
        {
            var menu = new ToolStripMenuItem(text) { Enabled = source.Enabled, ToolTipText = source.ToolTipText };

            foreach (ToolStripItem item in source.DropDownItems)
            {
                switch (item)
                {
                    case ToolStripSeparator:
                        menu.DropDownItems.Add(new ToolStripSeparator());
                        break;

                    case ToolStripMenuItem original:
                        menu.DropDownItems.Add(new ToolStripMenuItem(original.Text, original.Image, (_, _) => original.PerformClick())
                        {
                            Checked = original.Checked,
                            Enabled = original.Enabled,
                            ToolTipText = original.ToolTipText
                        });
                        break;
                }
            }

            return menu;
        }

        // ---------------------------------------------------------------- find

        private void RunFind()
        {
            var text = _findBox.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                _graphControl.Find(string.Empty);
                _findStatus.Text = string.Empty;
                return;
            }

            var count = _graphControl.Find(text);
            _findStatus.Text = count == 0
                ? "No match"
                : count.ToString(CultureInfo.InvariantCulture) + (count == 1 ? " match" : " matches");
            _findStatus.ForeColor = count == 0 ? DashColors.Warning : DashColors.Information;

            // Go to the first match as it is typed, the way find works everywhere else.  Highlighting
            // a match and leaving the view where it was says "no match" to anyone who cannot see the
            // operator it found - which, on a plan bigger than the window, is most of them.
            if (count > 0) NextMatch(1);
        }

        private void NextMatch(int step)
        {
            if (!_graphControl.NextMatch(step)) return;

            // The match is selected on the graph, so the properties panel has to follow it - the
            // selection changing without the panel changing reads as the panel being stuck.
            _tabs.SelectedIndex = 0;
        }

        private void FindBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is not (Keys.Enter or Keys.F3)) return;

            NextMatch(e.Shift ? -1 : 1);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        // ---------------------------------------------------------------- the lists

        /// <summary>
        /// Fill the properties panel for a selection, opening it if it was closed, or close it when
        /// the selection is cleared.
        /// </summary>
        /// <summary>
        /// The selected operator's properties - or, with nothing selected, the statement's overview:
        /// its warnings, missing indexes and the rest worth knowing.  The panel is open while there
        /// is either to show, and closed for a statement with nothing to say and nothing selected.
        /// </summary>
        private void ShowSelection(PlanNode node)
        {
            _properties.Show(node, _current);

            // Opening or closing the panel changes the plan's width, and the plan keeps its zoom
            // through that rather than rescaling under the pointer - see ResizeKeepingView.
            SetPropertiesOpen(node is not null || HasOverview(_current), keepView: true);
        }

        /// <summary>True when a statement has something for the overview to show.</summary>
        private static bool HasOverview(PlanStatement statement) =>
            statement is { HasPlan: true } && PlanInsights.ForStatement(statement).Count > 0;

        /// <summary>
        /// Open or close the properties panel.  <paramref name="keepView"/> keeps the plan's zoom as
        /// its width changes; without it, a plan the reader has not zoomed or panned is fitted to
        /// its new width, which is what a statement that has just been shown wants.
        /// </summary>
        private void SetPropertiesOpen(bool open, bool keepView)
        {
            if (_graphSplit.Panel2Collapsed != open) return;

            _openingProperties = true;
            try
            {
                void Resize()
                {
                    _graphSplit.Panel2Collapsed = !open;
                    if (open) _graphSplit.SplitterDistance = PropertiesSplitterDistance();
                }

                if (keepView) _graphControl.ResizeKeepingView(Resize);
                else Resize();
            }
            finally
            {
                _openingProperties = false;
            }
        }

        /// <summary>
        /// Where the splitter goes for the panel to open at its remembered width, within what the
        /// container allows - on a narrow window, the plan keeps at least a little room.
        /// </summary>
        private int PropertiesSplitterDistance()
        {
            var max = _graphSplit.Width - _graphSplit.Panel2MinSize - _graphSplit.SplitterWidth;
            var min = Math.Min(max, Math.Max(_graphSplit.Panel1MinSize, 200));
            var wanted = _graphSplit.Width - _propertiesWidth - _graphSplit.SplitterWidth;

            return Math.Max(min, Math.Min(max, wanted));
        }

        private void GraphControl_NodeActivated(object sender, PlanNode node)
        {
            // Double clicking an operator opens the statement text, which is the question a double
            // click is usually asking: what part of the query is this?
            if (node is null) return;
            _tabs.SelectedTab = _tabs.TabPages.Cast<TabPage>().First(p => p.Text == "Query");
        }

        private void ShowWarnings(PlanStatement statement)
        {
            _warningsGrid.DataSource = null;
            _warningsGrid.Rows.Clear();
            _warningsGrid.Columns.Clear();

            _warningsGrid.Columns.Add("Severity", "Severity");
            _warningsGrid.Columns.Add("Operator", "Operator");
            _warningsGrid.Columns.Add("Warning", "Warning");
            _warningsGrid.Columns.Add("Detail", "Detail");
            _warningsGrid.Columns.Add("Expanded", "In Full");
            _warningsGrid.Columns.Add(NodeIdColumn, NodeIdColumn);
            _warningsGrid.Columns[NodeIdColumn].Visible = false;

            var count = 0;

            // Plan level warnings first, then the operators' own, worst first within each - the same
            // order AllWarnings uses, so the list and the badges agree about what matters.
            foreach (var warning in statement.Warnings.OrderByDescending(w => w.Severity))
            {
                // A conversion showplan reported against the statement happens in one operator, and
                // the reader wants that one - so the row names it and double clicking goes there, the
                // same as a warning the plan put on an operator itself.
                AddWarningRow(
                    warning.Severity.ToString(),
                    warning.Operators.Count == 0 ? "Plan" : warning.OperatorsDescription,
                    warning.Title,
                    warning.Detail,
                    warning.Operators.Count == 0 ? null : warning.Operators[0].NodeId);

                count++;
            }

            foreach (var op in statement.Operators)
            {
                foreach (var warning in op.Warnings.OrderByDescending(w => w.Severity))
                {
                    AddWarningRow(warning.Severity.ToString(), op.ToString(), warning.Title, warning.Detail, op.NodeId);
                    count++;
                }
            }

            _warningsTab.Text = count == 0 ? "Warnings" : "Warnings (" + count.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private void AddWarningRow(string severity, string source, string title, string detail, int? nodeId)
        {
            // The values the plan works out that the detail names, written out in place: a wrong
            // estimate blamed on [Expr1011] is unreadable until something says what [Expr1011] is.
            // Empty where the detail names none, so a filled cell is itself the sign it refers to one.
            var expanded = PlanExpressions.ExpandedReferencesIn(_current, detail);

            var index = _warningsGrid.Rows.Add(severity, source, title, detail ?? string.Empty, expanded,
                nodeId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

            // The severity colour is the point of the column: a spill and a note about a missing
            // statistic in the same list need telling apart without reading them.
            _warningsGrid.Rows[index].Cells[0].Style.ForeColor = severity switch
            {
                nameof(PlanWarningSeverity.Critical) => DashColors.Fail,
                nameof(PlanWarningSeverity.Warning) => DashColors.Warning,
                _ => DashColors.Information
            };
        }

        private void ShowMissingIndexes(PlanStatement statement)
        {
            _missingIndexGrid.DataSource = null;
            _missingIndexGrid.Rows.Clear();
            _missingIndexGrid.Columns.Clear();

            _missingIndexGrid.Columns.Add("Impact", "Impact %");
            _missingIndexGrid.Columns.Add("Table", "Table");
            _missingIndexGrid.Columns.Add("Equality", "Equality");
            _missingIndexGrid.Columns.Add("Inequality", "Inequality");
            _missingIndexGrid.Columns.Add("Include", "Include");
            _missingIndexGrid.Columns.Add("Script", "T-SQL");

            foreach (var missing in statement.MissingIndexes)
            {
                // On one line in the cell, where Ctrl+C copies it ready to run; the row keeps the
                // laid out statement for the code viewer a double click opens.
                var index = _missingIndexGrid.Rows.Add(
                    missing.Impact.ToString("0.#", CultureInfo.InvariantCulture),
                    missing.QualifiedTableName,
                    string.Join(", ", missing.EqualityColumns),
                    string.Join(", ", missing.InequalityColumns),
                    string.Join(", ", missing.IncludedColumns),
                    missing.CreateStatementOneLine);

                _missingIndexGrid.Rows[index].Tag = PlanScripts.MissingIndex(statement, missing);
                _missingIndexGrid.Rows[index].Cells["Script"].ToolTipText =
                    "Double click to open the CREATE INDEX statement with the optimizer's notes.";
            }

            _missingIndexTab.Text = statement.MissingIndexes.Count == 0
                ? "Missing Indexes"
                : "Missing Indexes (" + statement.MissingIndexes.Count.ToString(CultureInfo.InvariantCulture) + ")";

            ShowScript(_missingIndexScript, _missingIndexScriptSplit, PlanScripts.MissingIndexes(statement));
        }

        // The column naming the operator that works an expression out, which a double click on it
        // goes to - as against a double click anywhere else on the row, which opens the expression.
        private const string ExpressionOperatorColumn = "DefinedBy";

        /// <summary>How wide an expression column opens, before the reader drags it.</summary>
        private const int ExpressionColumnWidth = 420;

        /// <summary>
        /// The values the plan works out for itself - Expr1011 and the rest - which nothing else in
        /// the viewer puts in one place.
        ///
        /// Showplan writes the definition on the operator that computes it and the bare name on every
        /// operator that uses it, which on a wide plan are nowhere near each other: a predicate that
        /// reads Expr1011 &gt; Expr1013 says nothing at all until both are looked up, and looking them
        /// up means clicking through the plan hunting for the Compute Scalars that define them.
        /// </summary>
        private void ShowExpressions(PlanStatement statement)
        {
            ResetGrid(_expressionsGrid);

            // The expression name and the operator that works it out are links, so it is obvious they
            // do something: clicking the name opens the value in full, clicking the operator goes to it
            // in the plan.  See ExpressionsGrid_CellContentClick.
            _expressionsGrid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "Name",
                HeaderText = "Expression",
                UseColumnTextForLinkValue = false,
                TrackVisitedState = false
            });
            _expressionsGrid.Columns.Add("Definition", "Definition");
            _expressionsGrid.Columns.Add("Expanded", "In Full");
            _expressionsGrid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = ExpressionOperatorColumn,
                HeaderText = "Worked Out By",
                UseColumnTextForLinkValue = false,
                TrackVisitedState = false
            });
            _expressionsGrid.Columns.Add("UsedBy", "Used By");
            _expressionsGrid.Columns.Add(NodeIdColumn, NodeIdColumn);
            _expressionsGrid.Columns[NodeIdColumn].Visible = false;

            // The two expression columns hold hundreds of characters, and sized to their content they
            // push the columns saying where the value comes from and goes off the right of the window.
            // Wide enough to read the start of an expression, resizable, and the whole of it is a
            // double click away.
            foreach (var column in new[] { "Definition", "Expanded" })
            {
                _expressionsGrid.Columns[column].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                _expressionsGrid.Columns[column].Width = ExpressionColumnWidth;
            }

            foreach (var expression in statement.Expressions)
            {
                var index = _expressionsGrid.Rows.Add(
                    expression.DisplayName,
                    expression.DefinitionOneLine,

                    // Empty where the expansion says nothing the definition did not, rather than the
                    // same text twice: a filled cell in this column is itself the sign that the
                    // expression is built on others.
                    expression.ExpandedOneLine,
                    expression.DefinedByDescription,
                    expression.UsedByDescription,
                    expression.DefinedBy?.NodeId.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

                var row = _expressionsGrid.Rows[index];
                row.Tag = expression;
                row.Cells["Name"].ToolTipText = "Click to see it in full.";
                row.Cells["Definition"].ToolTipText = "Double click to see it in full.";
                row.Cells[ExpressionOperatorColumn].ToolTipText = "Click to go to this operator in the plan.";
            }

            _expressionsTab.Text = statement.Expressions.Count == 0
                ? "Expressions"
                : "Expressions (" + statement.Expressions.Count.ToString(CultureInfo.InvariantCulture) + ")";

            var nested = statement.Expressions.Count(e => e.IsNested);
            _expressionsTab.ToolTipText = statement.Expressions.Count == 0
                ? string.Empty
                : "The values the plan works out for itself." +
                  (nested == 0
                      ? string.Empty
                      : " " + nested.ToString(CultureInfo.InvariantCulture) + " of them are built from others.");
        }

        /// <summary>
        /// Double clicking an expression opens it with everything it is built from written out, which
        /// is the whole reason for the list; double clicking the operator that works it out goes
        /// there instead, the way the other lists point back at the plan.  The Expression and Worked
        /// Out By columns are links, so a single click does the same - see
        /// <see cref="ExpressionsGrid_CellContentClick"/>.
        /// </summary>
        private void ExpressionsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex >= 0 && _expressionsGrid.Columns[e.ColumnIndex].Name == ExpressionOperatorColumn)
            {
                SelectFromGrid(_expressionsGrid, e.RowIndex);
                return;
            }

            OpenExpression(e.RowIndex);
        }

        /// <summary>
        /// Clicking the Expression link opens the value in full; clicking the Worked Out By link goes
        /// to the operator that works it out.  Only these two columns are links, so a click anywhere
        /// else does nothing here.
        /// </summary>
        private void ExpressionsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var column = _expressionsGrid.Columns[e.ColumnIndex].Name;
            if (column == ExpressionOperatorColumn)
            {
                SelectFromGrid(_expressionsGrid, e.RowIndex);
            }
            else if (column == "Name")
            {
                OpenExpression(e.RowIndex);
            }
        }

        /// <summary>Opens the expression on <paramref name="rowIndex"/> in the code viewer, written out in full.</summary>
        private void OpenExpression(int rowIndex)
        {
            if (_expressionsGrid.Rows[rowIndex].Tag is not PlanExpression expression) return;

            CommonShared.ShowCodeViewer(
                PlanScripts.Expression(expression),
                "Expression " + expression.DisplayName,
                CodeEditor.CodeEditorModes.SQL);
        }

        /// <summary>
        /// The two ways to copy the parameters as a DECLARE script - with the values this execution
        /// ran with, or those the plan was compiled for - added to <paramref name="items"/> at
        /// <paramref name="position"/>, or at the end when that is past it.  On the Copy menu and on
        /// the Parameters list's right click menu.
        /// </summary>
        private (ToolStripMenuItem Runtime, ToolStripMenuItem Compiled) AddParameterCopyItems(
            ToolStripItemCollection items, int position, string prefix)
        {
            var runtime = new ToolStripMenuItem(prefix + " (Runtime Values)", null,
                (_, _) => CopyText(PlanScripts.DeclareParameters(_current, PlanParameterValues.Runtime)))
            {
                ToolTipText = "The parameters as variables holding the values this execution ran with. Only an actual plan records these."
            };

            var compiled = new ToolStripMenuItem(prefix + " (Compiled Values)", null,
                (_, _) => CopyText(PlanScripts.DeclareParameters(_current, PlanParameterValues.Compiled)))
            {
                ToolTipText = "The parameters as variables holding the values the plan was compiled for - the ones its estimates were made for."
            };

            if (position >= items.Count)
            {
                items.Add(runtime);
                items.Add(compiled);
            }
            else
            {
                items.Insert(position, runtime);
                items.Insert(position + 1, compiled);
            }

            return (runtime, compiled);
        }

        /// <summary>Runtime values only exist on an actual plan; either needs parameters to script.</summary>
        private void EnableParameterCopyItems(ToolStripMenuItem runtime, ToolStripMenuItem compiled)
        {
            var hasParameters = _current?.Parameters.Count > 0;
            runtime.Enabled = hasParameters && PlanScripts.HasRuntimeValues(_current);
            compiled.Enabled = hasParameters;
        }

        /// <summary>The script under a grid, or the grid alone when there is nothing to script.</summary>
        private static void ShowScript(CodeEditor editor, SplitContainer split, string script)
        {
            editor.Text = script;
            split.Panel2Collapsed = script.Length == 0;
        }

        /// <summary>
        /// The parameters with the value the plan was compiled for beside the value it ran with.
        ///
        /// The pair is the point: a plan built for one value and run with a very different one is
        /// parameter sniffing, and it is invisible unless the two sit side by side.  The runtime
        /// value is only on an actual plan.
        /// </summary>
        private void ShowParameters(PlanStatement statement)
        {
            ResetGrid(_parametersGrid);

            _parametersGrid.Columns.Add("Parameter", "Parameter");
            _parametersGrid.Columns.Add("DataType", "Data Type");
            _parametersGrid.Columns.Add("CompiledValue", "Compiled Value");
            _parametersGrid.Columns.Add("RuntimeValue", "Runtime Value");
            _parametersGrid.Columns.Add("Differs", "Runtime Differs");

            var differing = 0;

            foreach (var parameter in statement.Parameters)
            {
                var index = _parametersGrid.Rows.Add(
                    parameter.Name,
                    parameter.DataType ?? string.Empty,
                    parameter.CompiledValue ?? string.Empty,
                    parameter.RuntimeValue ?? string.Empty,
                    parameter.CompiledValueDiffers ? "Yes" : string.Empty);

                if (!parameter.CompiledValueDiffers) continue;

                differing++;

                // Coloured as well as flagged in its own column, so the rows worth reading stand out
                // in a procedure with twenty parameters without sorting first.
                var runtime = _parametersGrid.Rows[index].Cells["RuntimeValue"];
                runtime.Style.ForeColor = DashColors.Warning;
                runtime.ToolTipText = "The plan was compiled for " + (parameter.CompiledValue ?? "another value") +
                                      ", and this execution used " + parameter.RuntimeValue + ".";
            }

            _parametersTab.Text = statement.Parameters.Count == 0
                ? "Parameters"
                : "Parameters (" + statement.Parameters.Count.ToString(CultureInfo.InvariantCulture) + ")";

            _parametersTab.ToolTipText = differing == 0
                ? string.Empty
                : differing.ToString(CultureInfo.InvariantCulture) + " ran with a different value than the plan was compiled for.";

            // As variables, to run the statement again with the values this execution had.
            ShowScript(_parameterScript, _parameterScriptSplit, PlanScripts.DeclareParameters(statement));
        }

        // The link column on the waits list, which opens the SQLskills page for the wait type - the
        // same help link the rest of DBA Dash offers wherever it lists waits.
        private const string WaitHelpColumn = "Help";

        /// <summary>
        /// What the query waited on, from an actual plan: all of it, where the root node's tooltip
        /// has room for the top three.  Numbers are held as numbers so the columns sort by value.
        /// </summary>
        private void ShowWaits(PlanStatement statement)
        {
            ResetGrid(_waitsGrid);

            _waitsGrid.Columns.Add("WaitType", "Wait Type");
            AddNumberColumn(_waitsGrid, "WaitTimeMs", "Wait Time (ms)", typeof(long), "N0");
            AddNumberColumn(_waitsGrid, "WaitCount", "Wait Count", typeof(long), "N0");
            AddNumberColumn(_waitsGrid, "AverageWaitMs", "Avg Wait (ms)", typeof(double), "N1");

            // Of the time spent waiting rather than of elapsed time: waits are counted per thread, so
            // on a parallel plan they can add up to more than the query took.
            AddNumberColumn(_waitsGrid, "Share", "% of Wait Time", typeof(double), "P1");

            _waitsGrid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = WaitHelpColumn,
                HeaderText = "Help",
                Text = "Help",
                UseColumnTextForLinkValue = true,
                ToolTipText = "What this wait type means (SQLskills)"
            });

            var total = statement.WaitStats.Sum(w => w.WaitTimeMs);

            foreach (var wait in statement.WaitStats)
            {
                _waitsGrid.Rows.Add(
                    wait.WaitType,
                    wait.WaitTimeMs,
                    wait.WaitCount,
                    wait.AverageWaitMs,
                    total > 0 ? (object)(wait.WaitTimeMs / (double)total) : null);
            }

            _waitsTab.Text = statement.WaitStats.Count == 0
                ? "Waits"
                : "Waits (" + statement.WaitStats.Count.ToString(CultureInfo.InvariantCulture) + ")";

            _waitsTab.ToolTipText = total == 0
                ? string.Empty
                : total.ToString("N0", CultureInfo.InvariantCulture) + " ms waiting in total.";
        }

        private void WaitsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _waitsGrid.Columns[e.ColumnIndex].Name != WaitHelpColumn) return;

            var wait = Convert.ToString(_waitsGrid.Rows[e.RowIndex].Cells["WaitType"].Value);
            if (string.IsNullOrEmpty(wait)) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://www.sqlskills.com/help/waits/" + wait.ToLowerInvariant() + "/")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Could not open the help page");
            }
        }

        private static void ResetGrid(DataGridView grid)
        {
            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
        }

        private static void AddNumberColumn(DataGridView grid, string name, string header, Type valueType, string format)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                ValueType = valueType,
                DefaultCellStyle = { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
            });
        }

        /// <summary>
        /// Double clicking a warning selects the operator it is about.
        ///
        /// A list that names an operator without being able to point at it leaves the reader to find
        /// "Sort" among the four sorts in the picture, which on a wide plan is most of the work.
        /// </summary>
        private void SelectFromGrid(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || _current is null) return;

            var value = Convert.ToString(grid.Rows[rowIndex].Cells[NodeIdColumn].Value);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeId)) return;

            var op = _current.Operators.FirstOrDefault(o => o.NodeId == nodeId);
            if (op is null) return;

            _tabs.SelectedIndex = 0;
            _graphControl.SelectOperator(op);
        }

        /// <summary>
        /// Double clicking a missing index shows the CREATE statement, which is the one thing anybody
        /// does with a recommendation.
        /// </summary>
        private void MissingIndexGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var script = _missingIndexGrid.Rows[e.RowIndex].Tag as string;
            if (string.IsNullOrEmpty(script)) return;

            CommonShared.ShowCodeViewer(script, "Missing Index", CodeEditor.CodeEditorModes.SQL);
        }

        // ---------------------------------------------------------------- copy, save, hand off

        private void CopyImage(bool whole)
        {
            using var bitmap = whole ? _graphControl.RenderWholePlanToBitmap() : _graphControl.RenderToBitmap();
            if (bitmap is null) return;

            try
            {
                Clipboard.SetImage(bitmap);
                SetSummary(whole ? "Whole plan copied to the clipboard." : "View copied to the clipboard.");
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Copy failed");
            }
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
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void SaveAs()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Query plan (*.sqlplan)|*.sqlplan|PNG image (*.png)|*.png",
                FileName = Path.GetFileNameWithoutExtension(_fileName) is { Length: > 0 } name
                    ? name
                    : "QueryPlan",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                if (dialog.FilterIndex == 2)
                {
                    using var bitmap = _graphControl.RenderWholePlanToBitmap();
                    bitmap?.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    // A .sqlplan is for the tools that read one, so a plan that arrived inside an
                    // extended events envelope is saved without it - see
                    // <see cref="Common.WriteQueryPlanTempFile"/>.  Whatever cannot be lifted out is
                    // saved as it stands rather than refused, since the user asked for this file.
                    var xml = PlanParser.TryExtractShowPlanXml(_sourceXml, out var showPlanXml)
                        ? showPlanXml
                        : _sourceXml ?? string.Empty;

                    File.WriteAllText(dialog.FileName, xml, Encoding.Unicode);
                }

                SetSummary("Saved " + dialog.FileName);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Save failed");
            }
        }

        /// <summary>
        /// Lists the applications registered for .sqlplan - the same list as Explorer's Open with -
        /// built each time the menu opens so an application installed meanwhile shows up.
        ///
        /// The built-in viewer is the default now, but SSMS and Plan Explorer can both do things this
        /// one cannot, and a viewer that traps the plan is worse than one that does less.  DBA Dash
        /// itself is left out: it is quite likely registered, possibly as the default, and would
        /// only open the plan in another one of these windows.
        /// </summary>
        private void BuildOpenWithMenu()
        {
            DisposeOpenWithItems();

            var extension = FileAssociation.QueryPlan.Extension;

            try
            {
                foreach (var handler in ShellFileHandlers.Get(extension)
                             .Where(h => !FileAssociation.IsThisCopy(h.Name)))
                {
                    _openWith.DropDownItems.Add(new ToolStripMenuItem(handler.DisplayName, handler.Image,
                        (_, _) => OpenExternal(path => ShellFileHandlers.Open(extension, handler.Name, path)))
                    {
                        ToolTipText = handler.Name
                    });
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Unable to list applications for .sqlplan files");
            }

            if (_openWith.DropDownItems.Count > 0) _openWith.DropDownItems.Add(new ToolStripSeparator());
            _openWith.DropDownItems.Add(new ToolStripMenuItem("Choose Another App...", null,
                (_, _) => OpenExternal(path => ShellFileHandlers.ShowOpenWithDialog(path, FindForm()?.Handle ?? Handle))));
        }

        /// <summary>
        /// Dispose rather than just clear - each rebuild loads new icons, which the items do not
        /// dispose themselves.
        /// </summary>
        private void DisposeOpenWithItems()
        {
            if (_openWith == null) return;

            foreach (var item in _openWith.DropDownItems.Cast<ToolStripItem>().ToList())
            {
                item.Image?.Dispose();
                item.Dispose();
            }
        }

        /// <summary>
        /// Write the plan to a temp .sqlplan and hand it to <paramref name="open"/>.  Every statement
        /// of the source document, not just the selected one, so what opens elsewhere matches what
        /// was handed to this viewer - minus any envelope it arrived in, which the application being
        /// handed the file would not read past.
        /// </summary>
        private void OpenExternal(Action<string> open)
        {
            try
            {
                open(Common.WriteQueryPlanTempFile(_sourceXml, _fileName));
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Could not open the plan in another application");
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Disposing the items does not dispose their images, and the handler icons are loaded
            // for this menu alone.
            if (disposing) DisposeOpenWithItems();
            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+F from anywhere in the viewer, which is where every reader's hand goes first.
            if (keyData == (Keys.Control | Keys.F))
            {
                _findBox.Focus();
                _findBox.SelectAll();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
