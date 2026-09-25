using DBADash.QueryPlan.Model;
using DBADashGUI.CustomReports;
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
    /// Every statement in the plan, with the figures worth comparing them by, as the way to move
    /// between statements.
    ///
    /// A batch or a procedure can hold dozens of statements and the question is nearly always which
    /// one matters - so the list is sortable, the cost, time and row columns carry bars, and the
    /// statement text wraps over several lines so a row says what the statement is rather than
    /// only how it starts.  Choosing a row shows that statement's plan.
    /// </summary>
    public sealed class QueryPlanStatementsControl : UserControl, IThemedControl
    {
        /// <summary>
        /// The most lines of statement text a row grows to.  Rows are sized to their text up to this,
        /// so SET NOCOUNT ON takes one line and a long SELECT takes four - a fixed height would
        /// either cut the long ones short or pad the short ones with blank space.
        /// </summary>
        private const int MaxTextLines = 4;

        private const string OrdinalColumn = "Ordinal";
        private const string StatementColumn = "Statement";
        private const string CostPercentColumn = "CostPercent";
        private const string ElapsedColumn = "Elapsed";
        private const string WarningsColumn = "Warnings";
        private const string EstimateErrorColumn = "EstimateError";

        // Hidden: the bar lengths, carried on the row so they survive sorting.
        private const string CostShareColumn = "CostShare";
        private const string ElapsedShareColumn = "ElapsedShare";
        private const string CriticalColumn = "Critical";

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
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };

        private IReadOnlyList<PlanStatementSummary> _summaries = [];

        /// <summary>
        /// The statement the host is showing.  The list is kept pointing at it through anything that
        /// moves the grid's own selection - binding, sorting - so only a deliberate choice by the
        /// reader ever changes the plan on screen.
        /// </summary>
        private PlanStatement _shown;

        // Set while the list is moving its own selection, so doing so is not mistaken for the reader
        // choosing a statement.
        private bool _syncing;

        public QueryPlanStatementsControl()
        {
            AddColumns();

            _grid.SelectionChanged += Grid_SelectionChanged;
            _grid.DataBindingComplete += (_, _) => AfterRowsChanged();
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            _grid.ColumnWidthChanged += (_, e) =>
            {
                if (e.Column.Name == StatementColumn) QueueFitRowHeights();
            };
            _grid.SizeChanged += (_, _) => QueueFitRowHeights();
            _grid.CellPainting += Grid_CellPainting;
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;

            Controls.Add(_grid);
        }

        /// <summary>Raised when the reader picks a statement.</summary>
        public event EventHandler<PlanStatement> StatementSelected;

        /// <summary>How many statements are listed.</summary>
        public int Count => _summaries.Count;

        /// <summary>
        /// The height that shows the header and the first <paramref name="rows"/> rows as they are
        /// currently sized, so the host can size its splitter to the list rather than guess.
        /// </summary>
        public int PreferredHeight(int rows)
        {
            // Fitted first rather than trusting a queued fit to have run: this is asked as the form
            // is first shown, which can be before the queue is reached.
            FitRowHeights();

            var shown = _grid.Rows.Cast<DataGridViewRow>().Take(rows).Sum(r => r.Height);
            if (shown == 0) shown = rows * MaxRowHeight;

            return _grid.ColumnHeadersHeight + shown + 4;
        }

        public void Show(IReadOnlyList<PlanStatementSummary> summaries)
        {
            _summaries = summaries ?? throw new ArgumentNullException(nameof(summaries));

            _syncing = true;
            try
            {
                BuildScales(summaries);
                _grid.DataSource = BuildTable(summaries);
                HideEmptyColumns(summaries);
                FitHeaderWidths();
            }
            finally
            {
                _syncing = false;
            }
        }

        /// <summary>
        /// Widen any figure column whose header would otherwise be cut short, measured with the font
        /// the header is actually drawn in.  Fixed widths guessed at one font size clip at another -
        /// "Missing Indexes" came out as "Missing" - and the grid's own font is set by the theme,
        /// not here.
        /// </summary>
        private void FitHeaderWidths()
        {
            var font = _grid.ColumnHeadersDefaultCellStyle.Font ?? _grid.Font;

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (!column.Visible || column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill) continue;

                // Room for the sort arrow as well as the text, or sorting a column clips its header.
                var needed = TextRenderer.MeasureText(column.HeaderText, font).Width + 28;
                if (column.Width < needed) column.Width = needed;
            }
        }

        /// <summary>
        /// Point the list at the statement the host is showing, without raising
        /// <see cref="StatementSelected"/>.  Remembered as well as applied, because the rows may not
        /// exist yet - a grid binds its rows when it is first shown, not when it is given data.
        /// </summary>
        public void Select(PlanStatement statement)
        {
            _shown = statement;
            SelectShownRow();
        }

        /// <summary>
        /// Scroll so the shown statement is visible with as much of the list above it as fits -
        /// from the very top when it fits there.
        ///
        /// Needed after the list is resized: the grid scrolls a selected row into view when it is
        /// selected, and if that happened while the list was still small it is left scrolled with
        /// the row at the top and empty space below, hiding the statements before it for no reason.
        /// </summary>
        public void RevealShown()
        {
            if (_grid.CurrentRow is not { } current || _grid.Rows.Count == 0) return;

            var available = _grid.DisplayRectangle.Height - _grid.ColumnHeadersHeight;
            var first = current.Index;
            var used = current.Height;

            // Walk upwards from the shown row while the rows above still fit.
            while (first > 0 && used + _grid.Rows[first - 1].Height <= available)
            {
                first--;
                used += _grid.Rows[first].Height;
            }

            if (_grid.FirstDisplayedScrollingRowIndex != first) _grid.FirstDisplayedScrollingRowIndex = first;
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            BackColor = theme.BackgroundColor;
            _grid.ApplyTheme(theme);
        }

        // ---------------------------------------------------------------- columns

        private void AddColumns()
        {
            AddColumn(OrdinalColumn, "#", 40, "N0");
            AddColumn("Type", "Type", 135, null, DataGridViewContentAlignment.TopLeft);

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = StatementColumn,
                DataPropertyName = StatementColumn,
                HeaderText = "Statement",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 250,
                FillWeight = 100,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True,
                    Alignment = DataGridViewContentAlignment.TopLeft
                }
            });

            AddColumn(CostPercentColumn, "Cost %", 80, "0.0");
            AddColumn("EstimatedCost", "Est. Cost", 85, "#,##0.###");
            AddColumn("EstimatedRows", "Est. Rows", 90, "N0");
            AddColumn("ActualRows", "Actual Rows", 95, "N0");
            AddColumn(ElapsedColumn, "Elapsed (ms)", 100, "N0");
            AddColumn("Cpu", "CPU (ms)", 85, "N0");
            AddColumn(EstimateErrorColumn, "Worst Estimate", 110, null);
            AddColumn("Granted", "Grant (KB)", 90, "N0");
            AddColumn("Used", "Used (KB)", 85, "N0");
            AddColumn("Dop", "DOP", 50, "N0");
            AddColumn(WarningsColumn, "Warnings", 80, "N0");
            AddColumn("MissingIndexes", "Missing Indexes", 115, "N0");
            AddColumn("Operators", "Operators", 80, "N0");

            foreach (var hidden in new[] { CostShareColumn, ElapsedShareColumn, CriticalColumn })
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = hidden,
                    DataPropertyName = hidden,
                    Visible = false
                });
            }
        }

        private void AddColumn(
            string name,
            string header,
            int width,
            string format,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.TopRight)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                Width = width,

                // Sorted by this control rather than by the grid - see Grid_ColumnHeaderMouseClick.
                SortMode = DataGridViewColumnSortMode.Programmatic,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = alignment }
            });
        }

        /// <summary>
        /// Hide the columns no statement has a value for: an estimated plan has no timings, and a
        /// column of blanks is width taken from the statement text for nothing.
        /// </summary>
        private void HideEmptyColumns(IReadOnlyList<PlanStatementSummary> summaries)
        {
            var actual = summaries.Any(s => s.IsActual);

            _grid.Columns["ActualRows"]!.Visible = actual;
            _grid.Columns[ElapsedColumn]!.Visible = summaries.Any(s => s.ElapsedMs is not null);
            _grid.Columns["Cpu"]!.Visible = summaries.Any(s => s.CpuMs is not null);
            _grid.Columns[EstimateErrorColumn]!.Visible = summaries.Any(s => s.WorstEstimateError is not null);
            _grid.Columns["Granted"]!.Visible = summaries.Any(s => s.GrantedMemoryKb is > 0);
            _grid.Columns["Used"]!.Visible = summaries.Any(s => s.UsedMemoryKb is not null);
            _grid.Columns["Dop"]!.Visible = summaries.Any(s => s.DegreeOfParallelism is > 1);
        }

        /// <summary>
        /// The rows, as a table rather than a bound list of summaries: a DataTable sorts on any
        /// column without further work, and nullable figures come through as blanks rather than
        /// zeroes - an estimated plan's elapsed time is unknown, not nothing.
        /// </summary>
        private static DataTable BuildTable(IReadOnlyList<PlanStatementSummary> summaries)
        {
            var table = new DataTable();
            table.Columns.Add(OrdinalColumn, typeof(int));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add(StatementColumn, typeof(string));
            table.Columns.Add(CostPercentColumn, typeof(double));
            table.Columns.Add("EstimatedCost", typeof(double));
            table.Columns.Add("EstimatedRows", typeof(double));
            table.Columns.Add("ActualRows", typeof(long));
            table.Columns.Add(ElapsedColumn, typeof(long));
            table.Columns.Add("Cpu", typeof(long));
            table.Columns.Add(EstimateErrorColumn, typeof(double));
            table.Columns.Add("Granted", typeof(long));
            table.Columns.Add("Used", typeof(long));
            table.Columns.Add("Dop", typeof(int));
            table.Columns.Add(WarningsColumn, typeof(int));
            table.Columns.Add("MissingIndexes", typeof(int));
            table.Columns.Add("Operators", typeof(int));
            table.Columns.Add(CostShareColumn, typeof(double));
            table.Columns.Add(ElapsedShareColumn, typeof(double));
            table.Columns.Add(CriticalColumn, typeof(bool));

            foreach (var summary in summaries)
            {
                var statement = summary.Statement;

                // Indented by nesting, so a statement inside a conditional branch reads as being
                // inside it rather than as another statement of the batch.
                var type = new string(' ', statement.NestingLevel * 3) +
                           (string.IsNullOrWhiteSpace(statement.StatementType) ? "Statement" : statement.StatementType);

                table.Rows.Add(
                    summary.Ordinal,
                    type,
                    summary.Text,
                    summary.CostShare * 100,
                    summary.EstimatedCost,
                    Value(summary.EstimatedRows),
                    Value(summary.ActualRows),
                    Value(summary.ElapsedMs),
                    Value(summary.CpuMs),
                    Value(summary.WorstEstimateError),
                    Value(summary.GrantedMemoryKb),
                    Value(summary.UsedMemoryKb),
                    Value(summary.DegreeOfParallelism),
                    summary.WarningCount,
                    summary.MissingIndexCount,
                    summary.OperatorCount,
                    summary.CostShare,
                    summary.ElapsedShare,
                    summary.HasCriticalWarning);
            }

            return table;
        }

        private static object Value<T>(T? value) where T : struct => value is { } v ? v : DBNull.Value;

        // ---------------------------------------------------------------- selection

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            // Only a choice the reader made changes the plan: a click or a key press in the list,
            // which is when it has focus.  The grid also moves its selection by itself - it selects
            // the first row when it binds - and treating that as a choice is how the viewer came to
            // open on statement one rather than on the statement it had chosen to show.
            if (_syncing || !_grid.ContainsFocus || _grid.CurrentRow is null) return;

            var statement = StatementAt(_grid.CurrentRow);
            if (statement is null || ReferenceEquals(statement, _shown)) return;

            _shown = statement;
            StatementSelected?.Invoke(this, statement);
        }

        private PlanStatement StatementAt(DataGridViewRow row)
        {
            var ordinal = Convert.ToInt32(row.Cells[OrdinalColumn].Value);
            return _summaries.FirstOrDefault(s => s.Ordinal == ordinal)?.Statement;
        }

        private void SelectShownRow()
        {
            if (_shown is null) return;

            var row = _grid.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(r => ReferenceEquals(StatementAt(r), _shown));

            if (row is null) return;

            _syncing = true;
            try
            {
                _grid.CurrentCell = row.Cells[StatementColumn];
                row.Selected = true;
            }
            finally
            {
                _syncing = false;
            }
        }

        /// <summary>Rows were created or reordered: point at the shown statement and size them.</summary>
        private void AfterRowsChanged()
        {
            SelectShownRow();
            QueueFitRowHeights();
        }

        /// <summary>
        /// Sort by the clicked column, keeping the shown statement selected.
        ///
        /// Done here rather than left to the grid, because the grid's own sort moves the current row
        /// as it goes, which would read as the reader choosing whatever statement landed there.
        /// Numbers sort largest first on the first click - ranking the statements by cost or by time
        /// is what anyone sorting this list is doing - and text sorts A to Z.
        /// </summary>
        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0) return;

            var column = _grid.Columns[e.ColumnIndex];
            var numeric = column.Name is not ("Type" or StatementColumn or OrdinalColumn);

            var direction = column.HeaderCell.SortGlyphDirection switch
            {
                SortOrder.Ascending => ListSortDirection.Descending,
                SortOrder.Descending => ListSortDirection.Ascending,
                _ => numeric ? ListSortDirection.Descending : ListSortDirection.Ascending
            };

            _syncing = true;
            try
            {
                _grid.Sort(column, direction);

                foreach (DataGridViewColumn other in _grid.Columns)
                {
                    other.HeaderCell.SortGlyphDirection = SortOrder.None;
                }

                column.HeaderCell.SortGlyphDirection =
                    direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
            }
            finally
            {
                _syncing = false;
            }

            AfterRowsChanged();
        }

        // ---------------------------------------------------------------- row heights

        private int LineHeight => _grid.Font.Height;

        private int MaxRowHeight => (LineHeight * MaxTextLines) + 8;

        // Set while a fit is queued, so a resize drag - dozens of size changes - fits once.
        private bool _fitQueued;

        /// <summary>
        /// Fit the row heights once the grid has finished laying itself out.
        ///
        /// Deferred rather than done on the spot, because the events that call for it arrive while
        /// the statement column - the auto-filled one - is part way through being resized, and the
        /// grid refuses any change to a row's height at that point.
        /// </summary>
        private void QueueFitRowHeights()
        {
            if (_fitQueued || !IsHandleCreated) return;

            _fitQueued = true;
            BeginInvoke(() =>
            {
                _fitQueued = false;
                FitRowHeights();
            });
        }

        /// <summary>
        /// Size each row to its statement text, between one line and <see cref="MaxTextLines"/>.
        /// Recalculated whenever the statement column changes width, because that is what decides
        /// how many lines the text wraps onto.
        /// </summary>
        private void FitRowHeights()
        {
            if (_grid.Rows.Count == 0 || !_grid.IsHandleCreated) return;

            var minimum = LineHeight + 8;
            var maximum = MaxRowHeight;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                var preferred = row.GetPreferredHeight(row.Index, DataGridViewAutoSizeRowMode.AllCells, true);
                var height = Math.Clamp(preferred, minimum, maximum);

                if (row.Height != height) row.Height = height;
            }
        }

        // ---------------------------------------------------------------- painting

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var name = _grid.Columns[e.ColumnIndex].Name;

            // The worst estimate as the multiple a reader would say out loud.
            if (name == EstimateErrorColumn && e.Value is double error)
            {
                e.Value = error.ToString(error >= 10 ? "0" : "0.#", CultureInfo.InvariantCulture) + "x";
                e.FormattingApplied = true;

                // Out by an order of magnitude or more is what made a bad plan choice.
                if (error >= 10) e.CellStyle.ForeColor = DashColors.Warning;
                return;
            }

            // Warnings on a statement with a spill or a cross join are the answer, not a note.
            if (name == WarningsColumn && e.Value is int count && count > 0)
            {
                var critical = _grid.Rows[e.RowIndex].Cells[CriticalColumn].Value is true;
                e.CellStyle.ForeColor = critical ? DashColors.Fail : DashColors.Warning;
            }
        }

        /// <summary>
        /// The cost, time and row cells carry a bar under the number, so the expensive or slow statement
        /// stands out in a long list without reading every figure.  Cost % and Elapsed are drawn to
        /// their hidden share columns, the others to the largest in the list.
        /// </summary>
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var name = _grid.Columns[e.ColumnIndex].Name;
            var shareColumn = name switch
            {
                CostPercentColumn => CostShareColumn,
                ElapsedColumn => ElapsedShareColumn,
                _ => null
            };

            if (shareColumn is not null)
            {
                var share = _grid.Rows[e.RowIndex].Cells[shareColumn].Value as double?;
                PlanGridBars.Paint(e, share);
                return;
            }

            // The figure columns, against the largest in the list.
            if (_scales.TryGetValue(name, out var scale))
            {
                var share = PlanGridBars.Share(_grid.Rows[e.RowIndex].Cells[name].Value, scale.Max);
                PlanGridBars.Paint(e, share, scale.Neutral ? DashColors.BlueLight : null);
            }
        }

        /// <summary>
        /// What the bar in a figure column is measured against, and whether it is coloured by size.
        /// Estimated and actual rows share a scale - the pair is compared, and bars of different
        /// lengths for the same number of rows would say something that is not true.  Rows are a
        /// volume rather than a cost, so they stay one colour; cost and CPU run from blue to red.
        /// </summary>
        private readonly Dictionary<string, (double Max, bool Neutral)> _scales = [];

        private void BuildScales(IReadOnlyList<PlanStatementSummary> summaries)
        {
            _scales.Clear();
            if (summaries.Count == 0) return;

            _scales["EstimatedCost"] = (summaries.Max(s => s.EstimatedCost), false);
            _scales["Cpu"] = (summaries.Max(s => (double)(s.CpuMs ?? 0)), false);

            var rows = summaries.Max(s => Math.Max(s.EstimatedRows ?? 0, s.ActualRows ?? 0));
            _scales["EstimatedRows"] = (rows, true);
            _scales["ActualRows"] = (rows, true);
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != StatementColumn) return;

            // The text a row cannot fit.  Already collapsed onto one line and capped, so the tooltip
            // cannot grow past the screen.
            e.ToolTipText = Convert.ToString(_grid.Rows[e.RowIndex].Cells[StatementColumn].Value);
        }
    }
}
