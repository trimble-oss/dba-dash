using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Every operator in the statement as a row: what it is, what it reads, and its cost, rows, time
    /// and I/O, sortable and filterable.
    ///
    /// The graph shows how the operators connect; this answers the questions the graph is slow at on
    /// a big plan - which operator read the most pages, every scan of one table, all the lookups -
    /// by sorting a column or ticking a filter rather than panning around.  Each row links back to
    /// its operator on the graph.
    /// </summary>
    public sealed class QueryPlanOperatorsControl : UserControl
    {
        private const string NodeIdColumn = "NodeId";
        private const string ShowColumn = "Show";
        private const string OperatorColumn = "Operator";
        private const string CostPercentColumn = "CostPercent";
        private const string EstimateErrorColumn = "EstimateError";
        private const string ElapsedColumn = "Elapsed";
        private const string CpuColumn = "Cpu";
        private const string InsightsColumn = "Insights";
        private const string RowsInColumn = "RowsIn";
        private const string SeekPredicateColumn = "SeekPredicate";
        private const string PredicateColumn = "Predicate";
        private const string RowsOutColumn = "RowsOut";
        private const string RowsDiffColumn = "RowsDiff";

        // Hidden: the worst severity among a row's insights, for the colour of its count.
        private const string SeverityColumn = "WorstSeverity";

        /// <summary>Longest predicate shown in a cell.  The whole of it is on the properties panel.</summary>
        private const int MaxPredicateLength = 300;

        private readonly DBADashDataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };

        private readonly ToolStrip _toolbar = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripDropDownButton _groupMenu = new() { DisplayStyle = ToolStripItemDisplayStyle.Text };
        private readonly ToolStripMenuItem _allGroupsItem = new("All Operators");
        private readonly Dictionary<PlanOperatorGroup, ToolStripMenuItem> _groupItems = new();

        private readonly ToolStripComboBox _operatorBox = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoSize = false,
            Width = 170,
            ToolTipText = "Show one kind of operator only."
        };

        private readonly ToolStripButton _insightsButton = new("With Insights")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            CheckOnClick = true,
            ToolTipText = "Show only the operators with insights: warnings, missing indexes, or DBA Dash's own findings."
        };

        private readonly ToolStripTextBox _findBox = new()
        {
            Width = 160,
            ToolTipText = "Find text in an operator's name, table, index or predicates - or a node id."
        };

        private readonly ToolStripLabel _countLabel = new();

        private const string AnyOperator = "(Any Operator)";

        private readonly DataTable _table = NewTable();
        private readonly DataView _view;

        /// <summary>
        /// Every operator's row values, in plan order.  The toolbar's filter decides which of them are
        /// in the table, which leaves the view's RowFilter to the grid's own filter commands, so the
        /// two filter independently and clearing one leaves the other alone.
        /// </summary>
        private readonly List<(int NodeId, object[] Values)> _rows = new();

        private PlanStatement _statement;
        private OperatorTimeMode _timeMode = OperatorTimeMode.Own;

        /// <summary>The operator the graph has selected, kept so the list can follow it through a rebuild.</summary>
        private PlanOperator _selected;

        // Set while the list moves its own selection, so following the graph is not taken for a request.
        private bool _syncing;

        public QueryPlanOperatorsControl()
        {
            _view = new DataView(_table);

            AddColumns();
            BuildToolbar();

            _grid.RowTemplate.Height = _grid.Font.Height + 14;
            _grid.DataSource = _view;
            _grid.CellContentClick += Grid_CellContentClick;
            // Not on a link cell, where the double click is two clicks on the link.
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex] is DataGridViewLinkColumn) return;
                RequestRow(e.RowIndex);
            };
            _grid.KeyDown += Grid_KeyDown;
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            _grid.DataBindingComplete += (_, _) => SelectRowOf(_selected);
            _grid.GridFilterChanged += (_, _) => ShowCount();

            // Bars under the figures that rank operators against each other.  Cost % is against the
            // whole statement, so it is drawn on a fixed 0 to 100 rather than against the largest row.
            var costShare = DataBarSettings.MoreIsWorse();
            costShare.Minimum = 0;
            costShare.Maximum = 100;
            _grid.SetDataBar(CostPercentColumn, costShare);
            foreach (var column in new[] { ElapsedColumn, CpuColumn, "LogicalReads", "PhysicalReads" })
            {
                _grid.SetDataBar(column, DataBarSettings.MoreIsWorse());
            }

            Controls.Add(_grid);
            Controls.Add(_toolbar);
        }

        /// <summary>Raised when the reader asks to see an operator on the graph.</summary>
        public event EventHandler<PlanOperator> OperatorRequested;

        /// <summary>
        /// Own time or time as reported - the toolbar choice that applies to the graph, applied here
        /// too so the two never disagree about an operator's time.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OperatorTimeMode OperatorTimeMode
        {
            get => _timeMode;
            set
            {
                if (_timeMode == value) return;
                _timeMode = value;
                if (_statement is not null) Show(_statement);
            }
        }

        /// <summary>How many operators the statement has, for the tab's caption.</summary>
        public int OperatorCount => _rows.Count;

        public void Show(PlanStatement statement)
        {
            var changed = !ReferenceEquals(statement, _statement);
            _statement = statement ?? throw new ArgumentNullException(nameof(statement));
            if (changed) _selected = null;

            BuildRows(statement);
            FillOperatorBox(statement, keepChoice: !changed);
            HideEmptyColumns(statement);
            ApplyFilter();
            FitColumns();
        }

        /// <summary>
        /// Point the list at the operator selected on the graph, without raising
        /// <see cref="OperatorRequested"/>.  Nothing happens when a filter hides it.
        /// </summary>
        public void Select(PlanOperator op)
        {
            _selected = op;
            SelectRowOf(op);
        }

        // ---------------------------------------------------------------- toolbar and filters

        private void BuildToolbar()
        {
            _allGroupsItem.ToolTipText = "Clear the filter and show every operator.";
            _allGroupsItem.Click += (_, _) =>
            {
                foreach (var item in _groupItems.Values) item.Checked = false;
                GroupsChanged();
            };
            _groupMenu.DropDownItems.Add(_allGroupsItem);
            _groupMenu.DropDownItems.Add(new ToolStripSeparator());

            foreach (var group in PlanOperatorGroup.All)
            {
                var item = new ToolStripMenuItem(group.Name) { CheckOnClick = true, ToolTipText = group.Description, Tag = group };
                item.CheckedChanged += (_, _) => GroupsChanged();
                _groupItems.Add(group, item);
                _groupMenu.DropDownItems.Add(item);

                // Separate the overlapping questions from the families: Reading Data and the three
                // kinds of read under it, then the rest.
                if (group == PlanOperatorGroup.Lookups || group == PlanOperatorGroup.Computing)
                {
                    _groupMenu.DropDownItems.Add(new ToolStripSeparator());
                }
            }

            // Kept open while groups are ticked, since ticking two or three is the usual thing to do.
            _groupMenu.DropDown.Closing += (_, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true;
            };
            _groupMenu.ToolTipText = "Show the operators in one or more groups.  Ticking several shows the operators in any of them.";
            ShowGroupMenuText();

            _operatorBox.SelectedIndexChanged += (_, _) => ApplyFilter();
            _insightsButton.CheckedChanged += (_, _) => ApplyFilter();
            _findBox.TextChanged += (_, _) => ApplyFilter();

            var clear = new ToolStripButton("Clear")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Clear every filter."
            };
            clear.Click += (_, _) => ClearFilters();

            _toolbar.Items.Add(new ToolStripLabel("Show:"));
            _toolbar.Items.Add(_groupMenu);
            _toolbar.Items.Add(_operatorBox);
            _toolbar.Items.Add(_insightsButton);
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripLabel("Find:"));
            _toolbar.Items.Add(_findBox);
            _toolbar.Items.Add(clear);
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(_countLabel);
        }

        private void ClearFilters()
        {
            foreach (var item in _groupItems.Values) item.Checked = false;
            if (_operatorBox.Items.Count > 0) _operatorBox.SelectedIndex = 0;
            _insightsButton.Checked = false;
            _findBox.Text = string.Empty;
            GroupsChanged();
            // Every filter, the ones made from the grid's menus included.
            if (_grid.HasFilter) _grid.ClearFilter();
        }

        private void GroupsChanged()
        {
            ShowGroupMenuText();
            ApplyFilter();
        }

        private IReadOnlyCollection<PlanOperatorGroup> CheckedGroups =>
            _groupItems.Where(pair => pair.Value.Checked).Select(pair => pair.Key).ToList();

        private void ShowGroupMenuText()
        {
            var groups = CheckedGroups;
            _allGroupsItem.Checked = groups.Count == 0;
            _groupMenu.Text = groups.Count switch
            {
                0 => "All Operators",
                1 => groups.First().Name,
                _ => groups.Count.ToString(CultureInfo.InvariantCulture) + " Groups"
            };
        }

        /// <summary>
        /// The operator names this statement has, so the list offers only ones that find something.
        /// The reader's choice is kept when the same statement is refreshed - a change of time mode -
        /// and dropped for a new statement, which may not have that operator at all.
        /// </summary>
        private void FillOperatorBox(PlanStatement statement, bool keepChoice)
        {
            var chosen = keepChoice ? _operatorBox.SelectedItem as string : null;

            _operatorBox.Items.Clear();
            _operatorBox.Items.Add(AnyOperator);
            foreach (var name in statement.Operators.Select(op => op.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
            {
                _operatorBox.Items.Add(name);
            }

            var index = chosen is null ? 0 : _operatorBox.Items.IndexOf(chosen);
            _operatorBox.SelectedIndex = Math.Max(0, index);
        }

        private PlanOperatorFilter CurrentFilter => new()
        {
            Groups = CheckedGroups,
            OperatorName = _operatorBox.SelectedItem is string name && name != AnyOperator ? name : null,
            InsightsOnly = _insightsButton.Checked,
            Text = _findBox.Text
        };

        /// <summary>
        /// Fill the table with the operators the filter lets through.  Worked out by the model's
        /// <see cref="PlanOperatorFilter"/>, so the rules live in one tested place rather than in a
        /// row filter expression - and kept out of the view's RowFilter, which belongs to the grid's
        /// own filter commands.
        /// </summary>
        private void ApplyFilter()
        {
            if (_statement is null) return;

            var filter = CurrentFilter;
            var shown = filter.IsEmpty
                ? null
                : filter.Apply(_statement).Select(op => op.NodeId).ToHashSet();

            _table.BeginLoadData();
            _table.Rows.Clear();
            foreach (var (nodeId, values) in _rows)
            {
                if (shown is null || shown.Contains(nodeId)) _table.Rows.Add(values);
            }
            _table.EndLoadData();

            ShowCount();
            SelectRowOf(_selected);
        }

        /// <summary>What is showing, out of every operator, through the toolbar's filter and the grid's together.</summary>
        private void ShowCount()
        {
            var total = _rows.Count;
            var filtered = !CurrentFilter.IsEmpty || _grid.HasFilter;
            _countLabel.Text = filtered
                ? _view.Count.ToString("N0", CultureInfo.InvariantCulture) + " of " + total.ToString("N0", CultureInfo.InvariantCulture) + " operators"
                : total.ToString("N0", CultureInfo.InvariantCulture) + (total == 1 ? " operator" : " operators");
        }

        // ---------------------------------------------------------------- columns and rows

        private static DataTable NewTable()
        {
            var table = new DataTable();
            table.Columns.Add(NodeIdColumn, typeof(int));
            table.Columns.Add(OperatorColumn, typeof(string));
            table.Columns.Add("LogicalOp", typeof(string));
            table.Columns.Add("Category", typeof(string));
            table.Columns.Add("Object", typeof(string));
            table.Columns.Add("Index", typeof(string));
            table.Columns.Add(CostPercentColumn, typeof(double));
            table.Columns.Add("OperatorCost", typeof(double));
            table.Columns.Add("SubtreeCost", typeof(double));
            table.Columns.Add("EstimatedRows", typeof(double));
            table.Columns.Add("EstimatedExecutions", typeof(double));
            table.Columns.Add("EstimatedRowsAll", typeof(double));
            table.Columns.Add("ActualRows", typeof(double));
            table.Columns.Add("ActualExecutions", typeof(long));
            table.Columns.Add("RowsRead", typeof(long));
            table.Columns.Add(RowsInColumn, typeof(double));
            table.Columns.Add(RowsOutColumn, typeof(double));
            table.Columns.Add(RowsDiffColumn, typeof(double));
            table.Columns.Add(EstimateErrorColumn, typeof(double));
            table.Columns.Add(ElapsedColumn, typeof(long));
            table.Columns.Add(CpuColumn, typeof(long));
            table.Columns.Add("LogicalReads", typeof(long));
            table.Columns.Add("PhysicalReads", typeof(long));
            table.Columns.Add("MemoryUsed", typeof(long));
            table.Columns.Add("Mode", typeof(string));
            table.Columns.Add("Parallel", typeof(string));
            table.Columns.Add(InsightsColumn, typeof(int));
            table.Columns.Add("SeekPredicate", typeof(string));
            table.Columns.Add("Predicate", typeof(string));
            table.Columns.Add(SeverityColumn, typeof(int));
            return table;
        }

        private void AddColumns()
        {
            AddColumn(NodeIdColumn, "Node", "N0", "Showplan's node id for the operator.");

            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = ShowColumn,
                HeaderText = "",
                Text = "Show",
                UseColumnTextForLinkValue = true,
                Width = 50,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ToolTipText = "Select this operator on the plan.  Double clicking a row does the same."
            });

            AddTextColumn(OperatorColumn, "Operator", null);
            AddTextColumn("LogicalOp", "Logical Operation", null);
            AddTextColumn("Category", "Category", "The colour family the operator is drawn in on the plan.");
            AddTextColumn("Object", "Object", "The table, view or function the operator reads or writes.");
            AddTextColumn("Index", "Index", null);
            AddColumn(CostPercentColumn, "Cost %", "0.0", "The operator's own estimated cost as a share of the statement's - the figure SSMS shows on the node.");
            AddColumn("OperatorCost", "Est. Cost", "#,##0.####", "The operator's own estimated cost: its subtree cost less its inputs'.");
            AddColumn("SubtreeCost", "Est. Subtree Cost", "#,##0.####", "The estimated cost of the operator and everything feeding it.");
            AddColumn("EstimatedRows", "Est. Rows", "#,##0.##", "Rows the optimizer expected per execution.");
            AddColumn("EstimatedExecutions", "Est. Executions", "#,##0.##", "How many times the optimizer expected the operator to run.");
            AddColumn("EstimatedRowsAll", "Est. Rows (All)", "#,##0.##", "Rows the optimizer expected across all the executions it expected.");
            AddColumn("ActualRows", "Actual Rows", "N0", "Rows the operator returned, across every execution.");
            AddColumn("ActualExecutions", "Actual Executions", "N0", null);
            AddColumn("RowsRead", "Rows Read", "N0", "Rows read before the residual predicate was applied.  Many more than Actual Rows means rows read and thrown away.");
            AddColumn(RowsInColumn, "Rows In", "N0", "Rows the operator's inputs handed it - zero for an operator with no inputs, such as a scan or seek.  Actual on an actual plan, estimated for all executions on an estimated one.");
            AddColumn(RowsOutColumn, "Rows Out", "N0", "Rows the operator handed on, counted the same way as Rows In.");
            AddColumn(RowsDiffColumn, "Rows Diff", "+#,##0;-#,##0;0", "Rows Out less Rows In: positive where the operator introduced rows - a scan or seek reading them, or a join multiplying them - and negative where it removed them.  Sort largest first to find where the plan's rows come from.");
            AddColumn(EstimateErrorColumn, "Estimate Error", null, "How far the row estimate was out, as a multiple either way, comparing actual rows with the estimate for the executions that happened.");
            AddColumn(ElapsedColumn, "Elapsed (ms)", "N0", null);
            AddColumn(CpuColumn, "CPU (ms)", "N0", null);
            AddColumn("LogicalReads", "Logical Reads", "N0", null);
            AddColumn("PhysicalReads", "Physical Reads", "N0", null);
            AddColumn("MemoryUsed", "Memory Used (KB)", "N0", "Memory grant the operator used.");
            AddTextColumn("Mode", "Mode", "Row or batch mode: as it ran on an actual plan, as the optimizer expected on an estimated one.");
            AddTextColumn("Parallel", "Parallel", null);
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = InsightsColumn,
                DataPropertyName = InsightsColumn,
                HeaderText = "Insights",
                ValueType = typeof(int),
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                ToolTipText = "Warnings, missing indexes and DBA Dash's own findings about the operator - what the properties panel shows for it.  Click to go to the operator on the plan, where the panel shows them in full.",
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.TopRight }
            });
            AddLinkColumn(SeekPredicateColumn, "Seek Predicate", "The part of the predicate that navigated the index.  Click to open it in full.");
            AddLinkColumn(PredicateColumn, "Predicate", "The residual predicate, tested against every row the operator reads.  Click to open it in full.");

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = SeverityColumn, DataPropertyName = SeverityColumn, Visible = false });
        }

        private void AddColumn(string name, string header, string format, string toolTip)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                ToolTipText = toolTip ?? string.Empty,
                ValueType = _table.Columns[name]!.DataType,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.TopRight }
            });
        }

        private void AddTextColumn(string name, string header, string toolTip)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                ToolTipText = toolTip ?? string.Empty,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.TopLeft }
            });
        }

        /// <summary>
        /// One row per operator, in the plan's own order - parents first, each input after the
        /// operator it feeds - which is the order the reader meets them reading the plan from the left.
        /// </summary>
        private void BuildRows(PlanStatement statement)
        {
            _rows.Clear();

            var includeDatabase = statement.Operators
                .SelectMany(op => op.Objects)
                .Select(o => o.Database)
                .Where(database => !string.IsNullOrEmpty(database))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            foreach (var op in statement.Operators)
            {
                var runtime = op.Runtime;
                var insights = PlanInsights.ForOperator(op, statement);

                _rows.Add((op.NodeId, new object[]
                {
                    op.NodeId,
                    op.DisplayName,
                    op.LogicalOp,
                    PlanOperatorClassifier.CategoryName(op.Category),
                    ObjectNames(op, includeDatabase),
                    IndexNames(op),
                    op.CostPercent * 100,
                    op.OperatorCost,
                    op.EstimatedTotalSubtreeCost,
                    op.EstimateRows,
                    op.EstimatedExecutions,
                    op.EstimatedTotalRows,
                    Value(op.ActualRows),
                    runtime is null ? DBNull.Value : runtime.ActualExecutions,
                    Value(runtime?.ActualRowsRead),
                    op.RowsIn,
                    op.RowsForDisplay,
                    op.RowsDiff,
                    Value(op.RowEstimateError),
                    Value(op.ElapsedMs(_timeMode)),
                    Value(op.CpuMs(_timeMode)),
                    Value(runtime?.ActualLogicalReads),
                    Value(runtime?.ActualPhysicalReads),
                    Value(runtime?.UsedMemoryGrantKb),
                    runtime?.ActualExecutionMode ?? op.EstimatedExecutionMode,
                    op.IsParallel ? "Yes" : string.Empty,
                    insights.Count,
                    Predicate(op.SeekPredicate),
                    Predicate(op.Predicate),
                    insights.Count == 0 ? DBNull.Value : (int)insights.Max(i => i.Severity)
                }));
            }

            var timeNote = _timeMode == OperatorTimeMode.Own
                ? "The operator's own time, with the time of its inputs taken out."
                : "As SQL Server reported it: a row mode operator's time includes its inputs'.";
            _grid.Columns[ElapsedColumn]!.ToolTipText = timeNote;
            _grid.Columns[CpuColumn]!.ToolTipText = timeNote;
        }

        // A Clustered Update names every index it maintains; the table is usually the same for all.
        // The database is left off when every object is in the same one, where it is the same words on
        // every row and pushes the table name out of the column.
        private static string ObjectNames(PlanOperator op, bool includeDatabase) =>
            string.Join(", ", op.Objects.Select(o =>
                {
                    var name = includeDatabase
                        ? o.QualifiedTableName
                        : string.Join(".", new[] { o.Schema, o.Table }.Where(part => !string.IsNullOrEmpty(part)));
                    return string.IsNullOrEmpty(o.Alias) || o.Alias == o.Table || name.Length == 0 ? name : name + " AS " + o.Alias;
                })
                .Where(name => name.Length > 0)
                .Distinct());

        private static string IndexNames(PlanOperator op) =>
            string.Join(", ", op.Objects.Select(o => o.Index).Where(index => !string.IsNullOrEmpty(index)).Distinct());

        private static string Predicate(string predicate) =>
            string.IsNullOrWhiteSpace(predicate) ? string.Empty : SingleLine(predicate);

        // Collapsed onto one line and cut short, so a predicate laid out over many lines fits a row.
        private static string SingleLine(string value)
        {
            var collapsed = string.Join(' ', value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            return collapsed.Length <= MaxPredicateLength ? collapsed : collapsed[..MaxPredicateLength] + "…";
        }

        private static object Value<T>(T? value) where T : struct => value is { } v ? v : DBNull.Value;

        /// <summary>
        /// Hide the columns no operator has a value for - an estimated plan has no actuals, and most
        /// plans have no memory consumers or parallelism - so the width goes to the columns that say something.
        /// </summary>
        private void HideEmptyColumns(PlanStatement statement)
        {
            var ops = statement.Operators.ToList();
            var actual = ops.Any(op => op.HasRuntime);

            foreach (var name in new[] { "ActualRows", "ActualExecutions", EstimateErrorColumn })
            {
                _grid.Columns[name]!.Visible = actual;
            }

            _grid.Columns["Object"]!.Visible = ops.Any(op => op.Objects.Count > 0);
            _grid.Columns["Index"]!.Visible = ops.Any(op => op.Objects.Any(o => !string.IsNullOrEmpty(o.Index)));
            _grid.Columns["RowsRead"]!.Visible = ops.Any(op => op.Runtime?.ActualRowsRead is not null);
            _grid.Columns[ElapsedColumn]!.Visible = ops.Any(op => op.ElapsedMs(_timeMode) is not null);
            _grid.Columns[CpuColumn]!.Visible = ops.Any(op => op.CpuMs(_timeMode) is not null);
            _grid.Columns["LogicalReads"]!.Visible = ops.Any(op => op.Runtime?.ActualLogicalReads is not null);
            _grid.Columns["PhysicalReads"]!.Visible = ops.Any(op => op.Runtime?.ActualPhysicalReads is not null);
            _grid.Columns["MemoryUsed"]!.Visible = ops.Any(op => op.Runtime?.UsedMemoryGrantKb is > 0);
            _grid.Columns["Parallel"]!.Visible = ops.Any(op => op.IsParallel);
            _grid.Columns["SeekPredicate"]!.Visible = ops.Any(op => !string.IsNullOrWhiteSpace(op.SeekPredicate));
            _grid.Columns["Predicate"]!.Visible = ops.Any(op => !string.IsNullOrWhiteSpace(op.Predicate));
        }

        // Sized once per statement, so a column the reader drags keeps its width until the next one.
        private bool _fitPending;

        private void FitColumns()
        {
            if (!_grid.IsHandleCreated || !_grid.Visible)
            {
                _fitPending = true;
                return;
            }

            _fitPending = false;
            _grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // Predicates can be long; they are clipped to a readable width and the cell's tooltip has the rest.
            foreach (var name in new[] { "SeekPredicate", "Predicate", "Object" })
            {
                if (_grid.Columns[name]!.Width > 400) _grid.Columns[name]!.Width = 400;
            }

            // Room for the sort arrow beside each header.
            var font = _grid.ColumnHeadersDefaultCellStyle.Font ?? _grid.Font;
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (!column.Visible || column.Name == ShowColumn) continue;
                var needed = TextRenderer.MeasureText(column.HeaderText, font).Width + 24;
                if (column.Width < needed) column.Width = needed;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && _fitPending) BeginInvoke(FitColumns);
        }

        // ---------------------------------------------------------------- selection and links

        private void SelectRowOf(PlanOperator op)
        {
            if (op is null) return;

            var row = _grid.Rows.Cast<DataGridViewRow>()
                .FirstOrDefault(r => r.Cells[NodeIdColumn].Value is int id && id == op.NodeId);
            if (row is null) return;

            // The grid's Hide Column command can hide the Operator column, and a hidden cell cannot be current.
            var column = _grid.Columns[OperatorColumn] is { Visible: true } operatorColumn
                ? operatorColumn
                : _grid.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
            if (column is null) return;

            _syncing = true;
            try
            {
                _grid.CurrentCell = row.Cells[column.Index];
                row.Selected = true;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case ShowColumn:
                    RequestRow(e.RowIndex);
                    break;
                // The properties panel shows the operator's insights as cards, with their links, so
                // going to the operator is the way to read them in full.
                case InsightsColumn when _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is int and > 0:
                    RequestRow(e.RowIndex);
                    break;
                case SeekPredicateColumn:
                    OpenPredicate(e.RowIndex, "Seek Predicate", op => op.SeekPredicate);
                    break;
                case PredicateColumn:
                    OpenPredicate(e.RowIndex, "Predicate", op => op.Predicate);
                    break;
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || _grid.CurrentRow is null) return;

            RequestRow(_grid.CurrentRow.Index);
            e.Handled = true;
        }

        private void RequestRow(int rowIndex)
        {
            if (_syncing || OperatorAt(rowIndex) is not { } op) return;

            _selected = op;
            OperatorRequested?.Invoke(this, op);
        }

        private PlanOperator OperatorAt(int rowIndex)
        {
            if (rowIndex < 0 || _statement is null) return null;
            if (_grid.Rows[rowIndex].Cells[NodeIdColumn].Value is not int nodeId) return null;

            return _statement.Operators.FirstOrDefault(o => o.NodeId == nodeId);
        }

        /// <summary>
        /// Open a predicate in the code viewer, in full and laid out for reading, with the plan's own
        /// values it names written out underneath - the cell only has room for the start of it.
        /// </summary>
        private void OpenPredicate(int rowIndex, string title, Func<PlanOperator, string> predicate)
        {
            if (OperatorAt(rowIndex) is not { } op) return;

            var script = PlanScripts.Predicate(_statement, predicate(op));
            if (script.Length == 0) return;

            CommonShared.ShowCodeViewer(script, title + " - " + op, CodeEditor.CodeEditorModes.SQL);
        }

        /// <summary>A column of text that opens in full when clicked, sorted as text.</summary>
        private void AddLinkColumn(string name, string header, string toolTip)
        {
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                ToolTipText = toolTip,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                TrackVisitedState = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.TopLeft }
            });
        }

        /// <summary>
        /// Sort by the clicked column.  Figures sort largest first on the first click - ranking the
        /// operators by cost, time or rows introduced is what anyone sorting them is doing - and text
        /// sorts A to Z.  A second click reverses it.
        /// </summary>
        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0) return;

            var column = _grid.Columns[e.ColumnIndex];
            if (column.SortMode != DataGridViewColumnSortMode.Programmatic) return;

            var numeric = column.ValueType is { } type && type != typeof(string);
            var direction = column.HeaderCell.SortGlyphDirection switch
            {
                SortOrder.Descending => ListSortDirection.Ascending,
                SortOrder.Ascending => ListSortDirection.Descending,
                _ => numeric ? ListSortDirection.Descending : ListSortDirection.Ascending
            };

            _grid.Sort(column, direction);
            SelectRowOf(_selected);
        }

        /// <summary>The longest an insight runs to in the Insights tooltip, which is a list to scan, not to read.</summary>
        private const int MaxInsightTipLength = 160;

        /// <summary>
        /// The Insights cell's tooltip lists what they are, worst first, so a reader can see what the
        /// count stands for without leaving the list.  Built when asked, as there is nothing to keep.
        /// </summary>
        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != InsightsColumn) return;
            if (OperatorAt(e.RowIndex) is not { } op) return;

            e.ToolTipText = InsightsToolTip(op, _statement);
        }

        /// <summary>One line per insight on <paramref name="op"/>, worst first, or empty for none.</summary>
        internal static string InsightsToolTip(PlanOperator op, PlanStatement statement)
        {
            var insights = PlanInsights.ForOperator(op, statement);
            if (insights.Count == 0) return string.Empty;

            var lines = insights.Select(insight =>
            {
                var text = insight.Title is { } title ? title + ": " + insight.Text : insight.Text;
                text = string.Join(' ', text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                if (text.Length > MaxInsightTipLength) text = text[..MaxInsightTipLength] + "…";
                return insight.Severity + " - " + text;
            });

            return string.Join(Environment.NewLine, lines) + Environment.NewLine + Environment.NewLine +
                            "Click to go to the operator on the plan and read them in full.";
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case EstimateErrorColumn when e.Value is double error:
                    e.Value = error.ToString(error >= 10 ? "N0" : "0.#", CultureInfo.InvariantCulture) + "x";
                    e.FormattingApplied = true;
                    if (error >= 10) e.CellStyle.ForeColor = error >= 100 ? DashColors.Fail : DashColors.Warning;
                    break;

                case RowsDiffColumn when e.Value is double diff && diff != 0:
                    e.CellStyle.ForeColor = diff < 0 ? DashColors.Information : DashColors.Warning;
                    break;

                case InsightsColumn when e.Value is int count:
                    // Blank rather than a 0 link, so the operators with something to read stand out.
                    if (count == 0)
                    {
                        e.Value = string.Empty;
                        e.FormattingApplied = true;
                        break;
                    }

                    // Coloured as the cards are, by the worst of them.
                    if (_grid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewLinkCell link &&
                        _grid.Rows[e.RowIndex].Cells[SeverityColumn].Value is int severity)
                    {
                        var colour = (PlanWarningSeverity)severity switch
                        {
                            PlanWarningSeverity.Critical => DashColors.Fail,
                            PlanWarningSeverity.Warning => DashColors.Warning,
                            _ => DashColors.Information
                        };
                        if (link.LinkColor != colour) link.LinkColor = link.ActiveLinkColor = colour;
                    }
                    break;
            }
        }
    }
}
