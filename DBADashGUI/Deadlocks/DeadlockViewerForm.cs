using DBADash;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using DBADash.Deadlock.Model;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// The deadlock viewer: the graph, plus the grids that carry the detail there is no room for on
    /// it, plus the original XML.
    ///
    /// The graph is the part people ask for, but the process and resource grids are where most of
    /// the triage actually happens - which login, which application, which statement - so they are
    /// first class tabs rather than an afterthought.
    /// </summary>
    public sealed class DeadlockViewerForm : Form
    {
        private readonly IReadOnlyList<DeadlockGraph> _graphs;
        private readonly string _sourceXml;
        private readonly string _fileName;

        /// <summary>
        /// The instance the graph came from, when the caller knew it.  Null for a graph opened from a file or
        /// from a context that isn't instance-scoped, in which case the lookups that go back to the source
        /// instance aren't offered.
        /// </summary>
        private readonly DBADashContext _context;

        /// <summary>
        /// Evaluated once: <see cref="DBADashContext.CanMessage"/> hits the repository the first time it is
        /// asked, and the answer can't change while the viewer is open.
        /// </summary>
        private readonly bool _canLookup;

        private readonly DeadlockGraphControl _graphControl = new() { Dock = DockStyle.Fill };
        private readonly DBADashDataGridView _processGrid = NewGrid();
        private readonly DBADashDataGridView _resourceGrid = NewGrid();

        /// <summary>
        /// The process grid over the cached plans for the selected statement.  The lower panel stays
        /// collapsed until a Plans link is clicked, so the tab opens as the plain process list it was.
        /// </summary>
        private readonly SplitContainer _processSplit = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel2Collapsed = true

            // No Panel1MinSize / Panel2MinSize here: they are validated against the height the container
            // has *now*, which at construction is the default 100px, and anything larger throws.  The
            // splitter position is set once the panel is first opened, when the height is real.
        };

        /// <summary>Null when the source instance can't be reached - there is nothing to load.</summary>
        private readonly DeadlockPlansControl _plansControl;

        private readonly DeadlockFindingsControl _findings = new() { Dock = DockStyle.Fill };

        private readonly DeadlockAiControl _ai = new() { Dock = DockStyle.Fill };

        /// <summary>Kept so the caption can carry the count, which is what makes the tab worth opening.</summary>
        private readonly TabPage _findingsTab;

        // Kept so an action on the graph can bring the matching grid to the front.
        private readonly TabPage _processesTab;

        private readonly TabPage _resourcesTab;

        private readonly CodeEditor _xmlText;
        private readonly ElementHost _xmlHost = new() { Dock = DockStyle.Fill };
        private readonly ThemedTabControl _tabs = new() { Dock = DockStyle.Fill };
        private readonly ToolStripComboBox _deadlockSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ToolStripStatusLabel _summary = new();

        // What the status bar says with nothing selected.  Held so selecting a node can describe the
        // node instead, and clearing the selection can put the graph summary back.
        private string _graphSummary = string.Empty;

        private DeadlockGraph _current;

        /// <summary>Set once the plans panel has been given a starting height, so a drag isn't undone.</summary>
        private bool _splitterPlaced;

        // The two link columns on the process grid, and the hidden columns feeding them.
        private const string PlansColumn = "Plans";

        private const string ProcedureColumn = "Procedure";
        private const string SqlHandleColumn = "SqlHandle";
        private const string StatementStartColumn = "StatementStart";
        private const string ModuleObjectColumn = "ModuleObject";
        private const string PlanDatabaseColumn = "PlanDatabase";

        private static readonly string[] HiddenProcessColumns =
        {
            SqlHandleColumn, StatementStartColumn, ModuleObjectColumn, PlanDatabaseColumn
        };

        /// <summary>
        /// The resource's position in the graph, carried on the row so a resource node can be matched
        /// to its row exactly.  The visible columns don't identify a resource uniquely - two keylocks
        /// on one index differ only by which processes are on them - and the grid is sortable, so a
        /// row index is no use either.
        /// </summary>
        private const string ResourceIndexColumn = "ResourceIndex";

        private const string PlansText = "Plans";

        public DeadlockViewerForm(IReadOnlyList<DeadlockGraph> graphs, string sourceXml, string fileName = null,
            DBADashContext context = null)
        {
            _graphs = graphs ?? throw new ArgumentNullException(nameof(graphs));
            if (graphs.Count == 0) throw new ArgumentException("No deadlocks to show.", nameof(graphs));

            _sourceXml = sourceXml;
            _fileName = fileName;
            _context = context;
            _canLookup = DeadlockPlansControl.CanShow(context);

            Text = "Deadlock";
            Icon = Properties.Resources.DeadlockIcon;
            Width = 1100;
            Height = 780;
            StartPosition = FormStartPosition.CenterParent;

            _xmlText = new CodeEditor
            {
                Mode = CodeEditor.CodeEditorModes.XML,
                IsReadOnly = true,
                WordWrap = false
            };
            _xmlHost.Child = _xmlText;

            // Set before the toolbar is built, so the menu opens with the right option ticked, and
            // before the first graph is shown, so it is laid out that way from the start.
            _graphControl.LayoutStyle = LoadLayoutStyle();

            _processSplit.Panel1.Controls.Add(_processGrid);
            if (_canLookup)
            {
                _plansControl = new DeadlockPlansControl(_context, _summary) { Dock = DockStyle.Fill };
                _plansControl.CloseRequested += (_, _) => _processSplit.Panel2Collapsed = true;
                _processSplit.Panel2.Controls.Add(_plansControl);
            }

            _findingsTab = NewPage("Findings", _findings);
            _processesTab = NewPage("Processes", _processSplit);
            _resourcesTab = NewPage("Resources", _resourceGrid);

            _tabs.TabPages.Add(NewPage("Graph", _graphControl));
            _tabs.TabPages.Add(_findingsTab);
            _tabs.TabPages.Add(_processesTab);
            _tabs.TabPages.Add(_resourcesTab);
            _tabs.TabPages.Add(NewPage("AI Analysis", _ai));
            _tabs.TabPages.Add(NewPage("XML", _xmlHost));

            Controls.Add(_tabs);
            Controls.Add(BuildToolbar());
            Controls.Add(BuildStatusBar());

            _processGrid.CellDoubleClick += ProcessGrid_CellDoubleClick;
            _processGrid.CellContentClick += ProcessGrid_CellContentClick;
            _processGrid.DataBindingComplete += (_, _) => ConfigureProcessGridColumns();
            _resourceGrid.DataBindingComplete += (_, _) => ConfigureResourceGridColumns();
            _graphControl.SelectionChanged += GraphControl_SelectionChanged;
            _graphControl.StatementActivated += GraphControl_StatementActivated;
            _graphControl.ContextMenuBuilding += GraphControl_ContextMenuBuilding;
            _graphControl.NodeActivated += GraphControl_NodeActivated;
            _findings.ProcessActivated += (_, process) => SelectProcess(process);

            PopulateDeadlockSelector();
            Load += (_, _) => Show(_graphs[0]);
        }

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

        // ---------------------------------------------------------------- chrome

        private ToolStrip BuildToolbar()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            toolbar.Items.Add(BuildLayoutMenu());
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripButton("Fit", Properties.Resources.ZoomToFit, (_, _) => _graphControl.ZoomToFit()) { DisplayStyle = ToolStripItemDisplayStyle.Image });
            toolbar.Items.Add(new ToolStripButton("Zoom In", Properties.Resources.ZoomIn_16x, (_, _) => _graphControl.ZoomIn()) { DisplayStyle = ToolStripItemDisplayStyle.Image });
            toolbar.Items.Add(new ToolStripButton("Zoom Out", Properties.Resources.ZoomOut_16x, (_, _) => _graphControl.ZoomOut()) { DisplayStyle = ToolStripItemDisplayStyle.Image });
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripButton("Open...", Properties.Resources.FolderOpened_16x, (_, _) => Common.OpenDeadlockGraphFile(this))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open a deadlock graph (.xdl) from disk in a new window."
            });
            toolbar.Items.Add(new ToolStripButton("Copy Image", Properties.Resources.ASX_Copy_blue_16x, (_, _) => CopyImage()) { DisplayStyle = ToolStripItemDisplayStyle.Image });
            toolbar.Items.Add(new ToolStripButton("Save As...", Properties.Resources.Save_16x, (_, _) => SaveAs()) { DisplayStyle = ToolStripItemDisplayStyle.Image });
            // Keeps its caption where the rest of the toolbar is icons only: the icon can say the
            // graph leaves for another application, but not which one.
            toolbar.Items.Add(new ToolStripButton("Open in SSMS", Properties.Resources.Open_16x, (_, _) => OpenExternal())
            {
                ToolTipText = "Open the graph in whatever handles .xdl files (e.g. SSMS)",
                Alignment = ToolStripItemAlignment.Right
            });

            // Only worth showing when the source actually held more than one deadlock, which is
            // common for a .xdl saved from the system_health session.
            if (_graphs.Count > 1)
            {
                toolbar.Items.Add(new ToolStripSeparator());
                toolbar.Items.Add(new ToolStripLabel("Deadlock:"));
                toolbar.Items.Add(_deadlockSelector);
                _deadlockSelector.SelectedIndexChanged += (_, _) =>
                {
                    if (_deadlockSelector.SelectedIndex >= 0) Show(_graphs[_deadlockSelector.SelectedIndex]);
                };
            }

            return toolbar;
        }

        /// <summary>
        /// The layout picker.  Which arrangement reads better depends on the deadlock - the ring makes
        /// a two or three way cycle recognisable at a glance, the columns cope better with a graph
        /// carrying processes around the cycle - so it is a choice rather than a default, remembered
        /// for next time.
        /// </summary>
        private ToolStripDropDownButton BuildLayoutMenu()
        {
            // Image and text: the icon alone would not say which of the two is currently in force,
            // and the button carries the current one so the choice is visible without opening it.
            var menu = new ToolStripDropDownButton("Layout")
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "How the graph is arranged."
            };

            AddLayoutOption(menu, DeadlockLayoutStyle.Ring, "Ring",
                "The cycle as a ring, with anything outside it beside the node it hangs off.");
            AddLayoutOption(menu, DeadlockLayoutStyle.Layered, "Columns",
                "Left to right from the victim, the way SSMS draws a deadlock.");

            ShowCurrentLayout(menu, _graphControl.LayoutStyle);
            return menu;
        }

        private static Image LayoutImage(DeadlockLayoutStyle style) =>
            style == DeadlockLayoutStyle.Layered
                ? Properties.Resources.DeadlockLayoutColumns_16x
                : Properties.Resources.DeadlockLayoutRing_16x;

        private void AddLayoutOption(ToolStripDropDownButton menu, DeadlockLayoutStyle style, string text, string tip)
        {
            var item = new ToolStripMenuItem(text, LayoutImage(style))
            {
                Checked = _graphControl.LayoutStyle == style,
                ToolTipText = tip,
                Tag = style
            };

            item.Click += (_, _) =>
            {
                _graphControl.LayoutStyle = style;
                ShowCurrentLayout(menu, style);
                SaveLayoutStyle(style);
            };

            menu.DropDownItems.Add(item);
        }

        /// <summary>Ticks the chosen option and puts its icon on the button.</summary>
        private static void ShowCurrentLayout(ToolStripDropDownButton menu, DeadlockLayoutStyle style)
        {
            foreach (var item in menu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (DeadlockLayoutStyle)item.Tag! == style;
            }

            menu.Image = LayoutImage(style);
        }

        /// <summary>
        /// The layout choice is a per user preference held locally: the viewer opens graphs from a
        /// file as well as from the repository, so it can't depend on being connected to one.
        /// </summary>
        private static DeadlockLayoutStyle LoadLayoutStyle() =>
            Enum.TryParse<DeadlockLayoutStyle>(Properties.Settings.Default.DeadlockLayoutStyle, out var style)
                ? style
                : DeadlockLayoutStyle.Ring;

        private static void SaveLayoutStyle(DeadlockLayoutStyle style)
        {
            try
            {
                Properties.Settings.Default.DeadlockLayoutStyle = style.ToString();
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // A preference that cannot be saved is not worth interrupting anyone over.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private StatusStrip BuildStatusBar()
        {
            var status = new StatusStrip();
            status.Items.Add(_summary);
            return status;
        }

        private void PopulateDeadlockSelector()
        {
            for (var i = 0; i < _graphs.Count; i++)
            {
                var victim = _graphs[i].Victims.FirstOrDefault();
                _deadlockSelector.Items.Add(
                    victim is null ? $"{i + 1}" : $"{i + 1} - victim {victim.DisplayName}");
            }

            if (_deadlockSelector.Items.Count > 0) _deadlockSelector.SelectedIndex = 0;
        }

        // ---------------------------------------------------------------- content

        private void Show(DeadlockGraph graph)
        {
            _current = graph;

            _graphControl.LoadGraph(graph);

            // The plans panel belongs to a statement in the deadlock being replaced, so it goes away with it.
            _processSplit.Panel2Collapsed = true;

            _processGrid.DataSource = BuildProcessTable(graph, _canLookup);
            _resourceGrid.DataSource = BuildResourceTable(graph);
            _xmlText.Text = graph.Xml;

            // The count belongs on the tab: a viewer that has found three things to say about this
            // deadlock should say so where it can be seen without opening the tab.
            _findings.Show(graph);
            _findingsTab.Text = _findings.Count == 0 ? "Findings" : $"Findings ({_findings.Count})";

            // Builds the payload and shows it.  Nothing is contacted, and nothing is sent, until the
            // user asks for it on that tab.
            _ai.Show(graph, _context?.InstanceName, _context);

            HighlightVictimRows();
            _graphSummary = Summarise(graph, _graphControl.GraphLayout);
            _summary.Text = _graphSummary;

            this.ApplyTheme();
        }

        private static string Summarise(DeadlockGraph graph, DeadlockLayout layout)
        {
            var parts = new List<string>
            {
                $"{graph.Processes.Count} process{(graph.Processes.Count == 1 ? string.Empty : "es")}",
                $"{graph.Resources.Count} resource{(graph.Resources.Count == 1 ? string.Empty : "s")}"
            };

            parts.Add(graph.Victims.Count switch
            {
                0 => "no victim named",
                1 => $"victim {graph.Victims[0].DisplayName}",
                _ => $"{graph.Victims.Count} victims"
            });

            if (graph.IsParallel) parts.Add("parallel (intra-query) deadlock");

            // A graph with no traceable cycle is usually a truncated capture, which is worth saying
            // rather than leaving the reader to wonder why the picture does not close.
            parts.Add(layout.Cycle.Count > 0
                ? $"cycle of {layout.Cycle.Count}"
                : "no complete cycle - the capture may be truncated");

            return string.Join("   |   ", parts);
        }

        /// <param name="canLookup">
        /// Whether the source instance can be reached.  The Plans cells are left empty when it can't - a link
        /// that can only ever report "no messaging" is worse than no link.
        /// </param>
        private static DataTable BuildProcessTable(DeadlockGraph graph, bool canLookup)
        {
            var table = new DataTable();
            table.Columns.Add("Victim", typeof(bool));
            table.Columns.Add("SPID", typeof(int));
            table.Columns.Add("Ecid", typeof(int));
            table.Columns.Add(PlansColumn, typeof(string));
            table.Columns.Add(ProcedureColumn, typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Login", typeof(string));
            table.Columns.Add("Host", typeof(string));
            table.Columns.Add("Application", typeof(string));
            table.Columns.Add("Database", typeof(string));
            table.Columns.Add("Isolation Level", typeof(string));
            table.Columns.Add("Lock Mode", typeof(string));
            table.Columns.Add("Wait Resource", typeof(string));
            table.Columns.Add("Wait (ms)", typeof(long));
            table.Columns.Add("Tran Count", typeof(int));
            table.Columns.Add("Log Used", typeof(long));
            table.Columns.Add("Transaction", typeof(string));
            table.Columns.Add("Statement", typeof(string));

            // Hidden columns carrying what the two link actions need.  They live on the row rather than in a
            // side table because the grid is sortable, so a row index is not a stable key.
            table.Columns.Add(SqlHandleColumn, typeof(string));
            table.Columns.Add(StatementStartColumn, typeof(int));
            table.Columns.Add(ModuleObjectColumn, typeof(string));
            table.Columns.Add(PlanDatabaseColumn, typeof(string));

            foreach (var p in graph.Processes)
            {
                var row = table.NewRow();
                row["Victim"] = p.IsVictim;
                row["SPID"] = (object)p.Spid ?? DBNull.Value;
                row["Ecid"] = (object)p.Ecid ?? DBNull.Value;
                row["Status"] = (object)p.Status ?? DBNull.Value;
                row["Login"] = (object)p.LoginName ?? DBNull.Value;
                row["Host"] = (object)p.HostName ?? DBNull.Value;
                row["Application"] = (object)p.ClientApp ?? DBNull.Value;
                row["Database"] = (object)p.CurrentDatabaseName ?? DBNull.Value;
                row["Isolation Level"] = (object)p.IsolationLevel ?? DBNull.Value;
                row["Lock Mode"] = (object)p.LockMode ?? DBNull.Value;
                row["Wait Resource"] = (object)p.WaitResource ?? DBNull.Value;
                row["Wait (ms)"] = p.WaitTime is { } w ? (long)w.TotalMilliseconds : (object)DBNull.Value;
                row["Tran Count"] = (object)p.TransactionCount ?? DBNull.Value;
                row["Log Used"] = (object)p.LogUsed ?? DBNull.Value;
                row["Transaction"] = (object)p.TransactionName ?? DBNull.Value;
                row["Statement"] = (object)p.PrimaryStatement ?? DBNull.Value;

                var frame = p.PrimaryFrame;

                // A module's own database beats the process's current database: an EXEC across databases
                // leaves currentdbname on the caller, but Query Store lives with the module.
                var database = frame?.ModuleDatabaseName ?? p.CurrentDatabaseName;

                row[SqlHandleColumn] = (object)frame?.SqlHandle ?? DBNull.Value;
                row[StatementStartColumn] = frame?.StatementStart ?? 0;
                row[ModuleObjectColumn] = (object)frame?.ModuleObjectName ?? DBNull.Value;
                row[PlanDatabaseColumn] = (object)database ?? DBNull.Value;
                row[PlansColumn] = canLookup && !string.IsNullOrWhiteSpace(frame?.SqlHandle)
                    ? PlansText
                    : string.Empty;

                // Only a real module goes in the Procedure column: it is a Query Store link, and the
                // "adhoc" / "unknown" placeholders have nothing to look up.  That the statement was ad-hoc
                // is already clear from the statement text.
                row[ProcedureColumn] =
                    frame is { IsModule: true } ? frame.ProcedureName : (object)DBNull.Value;

                table.Rows.Add(row);
            }

            return table;
        }

        private static DataTable BuildResourceTable(DeadlockGraph graph)
        {
            var table = new DataTable();
            table.Columns.Add("Type", typeof(string));

            // The database gets its own column rather than sitting in front of every object name,
            // where a long one leaves the table name off the end of the visible width.
            table.Columns.Add("Database", typeof(string));
            table.Columns.Add("Object", typeof(string));
            table.Columns.Add("Index", typeof(string));
            table.Columns.Add("Mode", typeof(string));

            // A parallel query deadlocking over one table gives every row the same type, object,
            // index, database and hobt id - the page is the only column that tells them apart, and
            // the lock id is what matches a row back to the raw XML.
            table.Columns.Add("Page", typeof(string));
            table.Columns.Add("Database ID", typeof(int));
            table.Columns.Add("Page ID", typeof(long));
            table.Columns.Add("HoBt ID", typeof(long));

            // Next to the hobt id because the pair is read together: they match on an ordinary table,
            // and differ where the lock is on a partition or a separate allocation unit - which is
            // the case where the graph is confusing without it.
            table.Columns.Add("Associated Object ID", typeof(long));
            table.Columns.Add("Lock ID", typeof(string));
            table.Columns.Add("Owners", typeof(string));
            table.Columns.Add("Waiters", typeof(string));
            table.Columns.Add(ResourceIndexColumn, typeof(int));

            for (var i = 0; i < graph.Resources.Count; i++)
            {
                var r = graph.Resources[i];
                var row = table.NewRow();
                row[ResourceIndexColumn] = i;
                row["Type"] = r.TypeName;
                row["Database"] = (object)r.DatabaseName ?? DBNull.Value;
                row["Object"] = (object)r.SchemaQualifiedName ?? DBNull.Value;
                row["Index"] = (object)r.IndexName ?? DBNull.Value;
                row["Mode"] = (object)r.Mode ?? DBNull.Value;
                row["Page"] = (object)r.PageKey ?? DBNull.Value;
                row["Database ID"] = (object)r.DatabaseId ?? DBNull.Value;
                row["Page ID"] = (object)r.PageId ?? DBNull.Value;
                row["HoBt ID"] = (object)r.HobtId ?? DBNull.Value;
                row["Associated Object ID"] = (object)r.AssociatedObjectId ?? DBNull.Value;
                row["Lock ID"] = (object)r.Id ?? DBNull.Value;
                row["Owners"] = Participants(r.Owners);
                row["Waiters"] = Participants(r.Waiters);
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// An unresolved process id means the process is missing from the process-list, which happens
        /// with a truncated capture.  Showing the raw id beats showing a blank.
        /// </summary>
        private static string Participants(IReadOnlyList<DeadlockResourceParticipant> participants) =>
            string.Join(", ", participants.Select(p =>
            {
                var name = p.Process?.DisplayName ?? p.ProcessId;
                return string.IsNullOrWhiteSpace(p.Mode) ? name : $"{name} ({p.Mode})";
            }));

        /// <summary>
        /// Turns the two action columns into link columns and hides the columns that only carry data for
        /// them.  Runs on every bind because the grid regenerates its columns when the deadlock is switched.
        /// </summary>
        private void ConfigureProcessGridColumns()
        {
            foreach (var name in HiddenProcessColumns)
            {
                var hidden = _processGrid.Columns[name];
                if (hidden != null) hidden.Visible = false;
            }

            var plans = _processGrid.Columns[PlansColumn];
            if (plans != null)
            {
                // Without messaging the cells are empty in every row, so the column would be dead weight.
                // The procedure name stays either way - it is worth reading even when it isn't a link.
                plans.Visible = _canLookup;
                plans.ToolTipText = "Plans cached on the instance for this statement, with their stats.";
            }

            var procedure = _processGrid.Columns[ProcedureColumn];
            if (procedure != null)
            {
                procedure.ToolTipText = _canLookup
                    ? "Find this module in Query Store."
                    : "The module the deadlocking statement ran in.";
            }

            // Next to the process identity rather than out past the statement text, which is wide enough to
            // push anything after it off screen.
            if (_canLookup) LinkifyColumn(PlansColumn);
            if (_canLookup) LinkifyColumn(ProcedureColumn);

            SetDisplayIndex(PlansColumn, 3);
            SetDisplayIndex(ProcedureColumn, 4);
        }

        /// <summary>Hides the column that only exists to match a resource node to its row.</summary>
        private void ConfigureResourceGridColumns()
        {
            var index = _resourceGrid.Columns[ResourceIndexColumn];
            if (index != null) index.Visible = false;
        }

        private void SetDisplayIndex(string name, int displayIndex)
        {
            var col = _processGrid.Columns[name];
            if (col != null) col.DisplayIndex = displayIndex;
        }

        private void LinkifyColumn(string name)
        {
            var col = _processGrid.Columns[name];
            if (col is null or DataGridViewLinkColumn) return;

            var index = col.Index;
            var link = new DataGridViewLinkColumn
            {
                Name = col.Name,
                HeaderText = col.HeaderText,
                DataPropertyName = col.DataPropertyName,
                ToolTipText = col.ToolTipText,
                Visible = col.Visible,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                TrackVisitedState = false,
                LinkColor = DashColors.LinkColor,
                ActiveLinkColor = DashColors.LinkColor,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            };
            _processGrid.Columns.RemoveAt(index);
            _processGrid.Columns.Insert(index, link);
        }

        private void HighlightVictimRows()
        {
            var theme = ThemeExtensions.CurrentTheme;

            foreach (DataGridViewRow row in _processGrid.Rows)
            {
                if (row.Cells["Victim"].Value is not true) continue;
                row.DefaultCellStyle.BackColor = theme.CriticalBackColor;
                row.DefaultCellStyle.ForeColor = theme.CriticalForeColor;
            }
        }

        // ---------------------------------------------------------------- actions

        private async void ProcessGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_processGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView row) return;

            var column = _processGrid.Columns[e.ColumnIndex].Name;
            try
            {
                switch (column)
                {
                    case PlansColumn:
                        await ShowPlansAsync(row.Row);
                        break;

                    case ProcedureColumn:
                        ShowQueryStore(row.Row);
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, $"Error running the {column} action");
            }
        }

        /// <summary>
        /// Lists what the instance has cached for the row's statement - every plan under the sql handle, with
        /// its stats - in the panel below the process grid.  Fetching plan XML is left to the row the user
        /// picks there.
        /// </summary>
        private async Task ShowPlansAsync(DataRow row)
        {
            if (_plansControl == null || row[PlansColumn] as string != PlansText) return;

            var spid = row["SPID"] == DBNull.Value ? string.Empty : $"SPID {row["SPID"]}";
            var module = row[ProcedureColumn] as string;
            var caption = string.IsNullOrEmpty(module) ? spid : $"{spid} - {module}";

            ExpandPlansPanel();

            await _plansControl.ShowStatementAsync(
                row[SqlHandleColumn] as string,
                row[StatementStartColumn] is int start ? start : 0,
                row[PlanDatabaseColumn] as string,
                caption);
        }

        /// <summary>
        /// Opens the plans panel, giving it the lower third on first use and leaving the splitter alone
        /// afterwards so a position the user has dragged survives the next click.
        /// </summary>
        private void ExpandPlansPanel()
        {
            if (!_processSplit.Panel2Collapsed) return;

            _processSplit.Panel2Collapsed = false;
            if (_splitterPlaced) return;

            // SplitterDistance throws rather than clamping when it doesn't fit, and the height here depends
            // on the window, so this only moves the splitter when there is genuinely room for both panels.
            var distance = _processSplit.Height * 2 / 3;
            var maximum = _processSplit.Height - _processSplit.Panel2MinSize - _processSplit.SplitterWidth;
            if (distance >= _processSplit.Panel1MinSize && distance <= maximum)
            {
                _processSplit.SplitterDistance = distance;
                _splitterPlaced = true;
            }
        }

        /// <summary>
        /// Opens the Query Store viewer for the module the deadlocking statement ran in.  An ad-hoc statement
        /// has no object to look up - it is found by query hash from the plans list instead.
        /// </summary>
        private void ShowQueryStore(DataRow row)
        {
            var objectName = row[ModuleObjectColumn] as string;
            if (string.IsNullOrEmpty(objectName)) return;

            var context = _context.DeepCopy();
            context.DatabaseName = row[PlanDatabaseColumn] as string;
            context.ObjectName = objectName;
            context.Type = SQLTreeItem.TreeType.StoredProcedure;

            var frm = new QueryStoreViewer { Context = context };
            frm.ShowSingleInstance();
        }

        private void ProcessGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_processGrid.Columns[e.ColumnIndex].Name != "Statement") return;

            var sql = _processGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
            if (string.IsNullOrWhiteSpace(sql)) return;

            using var frm = new CodeEditorForm { Code = sql, Syntax = CodeEditor.CodeEditorModes.SQL };
            frm.EditEnabled = false;
            frm.ShowDialog();
        }

        /// <summary>
        /// Points the rest of the viewer at a process: the graph node and the grid row.  Used when a
        /// finding is chosen, so "SPID 61 holds..." can be followed to SPID 61 without hunting.
        /// </summary>
        private void SelectProcess(DeadlockProcess process)
        {
            if (process is null) return;

            _graphControl.SelectProcess(process);
            SelectGridRow(_processGrid, FindProcessRow(process));
        }

        /// <summary>Selecting a process on the graph selects the matching grid row, and vice versa.</summary>
        private void GraphControl_SelectionChanged(object sender, DeadlockNode node)
        {
            if (node is DeadlockProcessNode processNode)
            {
                SelectGridRow(_processGrid, FindProcessRow(processNode.Process));
            }
            else if (node is DeadlockResourceNode resourceNode)
            {
                SelectGridRow(_resourceGrid, FindResourceRow(resourceNode.Resource));
            }

            _summary.Text = DescribeSelection(node) ?? _graphSummary;
        }

        /// <summary>
        /// What the graph is now emphasising, in words: the colours say which boxes are held and which
        /// are waited on, and this says how many of each without counting arrows.  Null when nothing
        /// is selected, so the caller can fall back to the graph summary.
        /// </summary>
        private string DescribeSelection(DeadlockNode node)
        {
            if (node is null) return null;

            var highlight = _graphControl.Highlight;
            var owned = highlight.OwnershipRelated.Count;
            var wanted = highlight.WaitRelated.Count;

            var parts = new List<string> { node.Title };

            parts.Add(node is DeadlockProcessNode
                ? $"holds {owned} resource{(owned == 1 ? string.Empty : "s")}"
                : $"held by {owned} process{(owned == 1 ? string.Empty : "es")}");

            parts.Add(node is DeadlockProcessNode
                ? $"waiting on {wanted} resource{(wanted == 1 ? string.Empty : "s")}"
                : $"wanted by {wanted} process{(wanted == 1 ? string.Empty : "es")}");

            return string.Join("   |   ", parts);
        }

        /// <summary>
        /// The grid row for a process.  Matched on spid and ecid rather than row order: the grid is
        /// sortable, and the pair identifies a process within one deadlock (a parallel query
        /// contributes several rows sharing a spid, told apart by ecid).
        /// </summary>
        private DataGridViewRow FindProcessRow(DeadlockProcess process)
        {
            if (process is null) return null;

            foreach (DataGridViewRow row in _processGrid.Rows)
            {
                if (row.Cells["SPID"].Value as int? == process.Spid &&
                    row.Cells["Ecid"].Value as int? == process.Ecid) return row;
            }

            return null;
        }

        private DataGridViewRow FindResourceRow(DeadlockResource resource)
        {
            if (resource is null) return null;

            var index = -1;
            for (var i = 0; i < _current.Resources.Count; i++)
            {
                if (!ReferenceEquals(_current.Resources[i], resource)) continue;
                index = i;
                break;
            }

            if (index < 0) return null;

            foreach (DataGridViewRow row in _resourceGrid.Rows)
            {
                if (row.Cells[ResourceIndexColumn].Value as int? == index) return row;
            }

            return null;
        }

        /// <summary>
        /// Selects a row and scrolls it into view.  Selecting a row that is off screen would leave
        /// the reader looking at an unchanged grid, which reads as the action having done nothing.
        /// </summary>
        private static void SelectGridRow(DataGridView grid, DataGridViewRow row)
        {
            // A row whose grid is null was detached by a rebind after it was found.  Setting Selected
            // on one throws, and there is nothing useful to select, so leave the grid as it is.
            if (row?.DataGridView is null) return;

            grid.ClearSelection();
            row.Selected = true;

            try
            {
                grid.FirstDisplayedScrollingRowIndex = row.Index;
            }
            catch (InvalidOperationException)
            {
                // Thrown when the grid has no displayed area yet - it is on a tab that has never been
                // shown.  The row is selected either way, and it will be in view when the tab opens.
            }
        }

        private static DataRow DataRowOf(DataGridViewRow row) => (row?.DataBoundItem as DataRowView)?.Row;

        /// <summary>
        /// Adds the graph's right click actions that need the rest of the viewer: the two lookups the
        /// process grid carries as links, a way into each grid, and the picture itself.
        ///
        /// The graph is where the reading happens, so the actions have to be reachable from it - up
        /// to now the only thing a node could do was open its statement, and everything else meant
        /// finding the same process again on another tab.
        /// </summary>
        private void GraphControl_ContextMenuBuilding(object sender, DeadlockGraphMenuEventArgs e)
        {
            var start = e.Items.Count;

            switch (e.Node)
            {
                case DeadlockProcessNode processNode:
                    AddProcessMenuItems(e.Items, processNode.Process);
                    break;

                case DeadlockResourceNode resourceNode:
                    var resource = resourceNode.Resource;
                    e.Items.Add(new ToolStripMenuItem("Show in Resources Grid", Properties.Resources.DataTable_16x,
                        (_, _) => ShowInGrid(_resourcesTab, _resourceGrid, () => FindResourceRow(resource))));
                    break;
            }

            // Separators are put in around what was actually added, so a node offering none of these
            // actions - or a click on empty canvas - does not end up with a menu of dividers.
            if (start > 0 && e.Items.Count > start) e.Items.Insert(start, new ToolStripSeparator());
            if (e.Items.Count > 0) e.Items.Add(new ToolStripSeparator());

            e.Items.Add(new ToolStripMenuItem("Copy Image", Properties.Resources.ASX_Copy_blue_16x,
                (_, _) => CopyImage()));
            e.Items.Add(new ToolStripMenuItem("Zoom to Fit", Properties.Resources.ZoomToFit,
                (_, _) => _graphControl.ZoomToFit()));
        }

        private void AddProcessMenuItems(ToolStripItemCollection items, DeadlockProcess process)
        {
            var row = DataRowOf(FindProcessRow(process));
            if (row is null) return;

            // The same two conditions the grid's link cells use: a statement to look plans up by, and
            // a real module to look up in Query Store.
            if (_canLookup && row[PlansColumn] as string == PlansText)
            {
                items.Add(new ToolStripMenuItem("Plans", Properties.Resources.query_plan,
                    async (_, _) => await RunActionAsync("Plans", () => ShowPlansForAsync(process, row))));
            }

            if (_canLookup && !string.IsNullOrEmpty(row[ModuleObjectColumn] as string))
            {
                items.Add(new ToolStripMenuItem("Query Store", Properties.Resources.history,
                    (_, _) => RunAction("Query Store", () => ShowQueryStore(row))));
            }

            items.Add(new ToolStripMenuItem("Show in Processes Grid", Properties.Resources.DataTable_16x,
                (_, _) => ShowInGrid(_processesTab, _processGrid, () => FindProcessRow(process))));
        }

        /// <summary>
        /// Double clicking a node takes the reader to its row, which is where the detail the node has
        /// no room for lives.  The control has raised this since it was written with nothing listening
        /// - the graph now has a menu naming the same action, so the double click means it too.
        /// </summary>
        private void GraphControl_NodeActivated(object sender, DeadlockNode node)
        {
            switch (node)
            {
                case DeadlockProcessNode processNode:
                    ShowInGrid(_processesTab, _processGrid, () => FindProcessRow(processNode.Process));
                    break;

                case DeadlockResourceNode resourceNode:
                    ShowInGrid(_resourcesTab, _resourceGrid, () => FindResourceRow(resourceNode.Resource));
                    break;
            }
        }

        /// <summary>
        /// Brings a grid to the front with the row for the node already selected.
        ///
        /// The row is looked up <em>after</em> the tab is shown, not before, which is why this takes a
        /// lookup rather than a row: selecting a tab that has never been opened creates its grid and
        /// binds it, and that replaces every DataGridViewRow the grid had handed out.  A row found
        /// beforehand is detached by the time we get here, and setting Selected on a detached row
        /// throws.
        /// </summary>
        private void ShowInGrid(TabPage tab, DataGridView grid, Func<DataGridViewRow> findRow)
        {
            _tabs.SelectedTab = tab;
            SelectGridRow(grid, findRow());
        }

        /// <summary>
        /// The plans panel, reached from the graph rather than from the grid's link.  The grid comes
        /// to the front first: the panel it fills lives under that grid, so opening it on a hidden tab
        /// would look like nothing happened.
        /// </summary>
        private async Task ShowPlansForAsync(DeadlockProcess process, DataRow row)
        {
            ShowInGrid(_processesTab, _processGrid, () => FindProcessRow(process));
            await ShowPlansAsync(row);
        }

        // A menu item handler is on its own for errors - there is no cell-click wrapper above it -
        // so these mirror what ProcessGrid_CellContentClick does for the links.
        private void RunAction(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, $"Error running the {name} action");
            }
        }

        private async Task RunActionAsync(string name, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, $"Error running the {name} action");
            }
        }

        /// <summary>Clicking a process node's statement link opens the full statement.</summary>
        private void GraphControl_StatementActivated(object sender, DeadlockStatementActivatedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Statement)) return;

            using var frm = new CodeEditorForm { Code = e.Statement, Syntax = CodeEditor.CodeEditorModes.SQL };
            frm.EditEnabled = false;
            frm.ShowDialog();
        }

        private void CopyImage()
        {
            if (_graphControl.CopyImageToClipboard())
            {
                _summary.Text = "Graph copied to the clipboard.";
            }
        }

        private void SaveAs()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Deadlock graph (*.xdl)|*.xdl|XML (*.xml)|*.xml|All files (*.*)|*.*",
                FileName = string.IsNullOrEmpty(_fileName)
                    ? "deadlock.xdl"
                    : System.IO.Path.ChangeExtension(_fileName, ".xdl")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                System.IO.File.WriteAllText(dialog.FileName, _current.Xml);
                _summary.Text = $"Saved to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving deadlock graph");
            }
        }

        private void OpenExternal()
        {
            try
            {
                // The whole source document, not just the selected deadlock, so what opens externally
                // matches what was handed to the viewer.
                Common.ShowDeadlockGraphExternal(_sourceXml ?? _current.Xml, _fileName);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(
                    ex,
                    "Error opening deadlock graph",
                    text:
                    "Opening a .xdl externally needs an application registered for the extension, such as SSMS.");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Dispose();
        }
    }
}