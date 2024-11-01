using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public class DBADashDataGridView : DataGridView
    {
        public ContextMenuStrip CellContextMenu;
        public ContextMenuStrip ColumnContextMenu;
        public EventHandler<DataGridViewCellEventArgs> CellContextMenuOpening;
        public EventHandler<DataGridViewCellEventArgs> ColumnContextMenuOpening;
        public EventHandler GridFilterChanged;

        public int ResultSetID { get; set; }

        public string ResultSetName { get; set; }

        private ToolStripMenuItem GetCopyGridMenuItem() =>
            new("Copy Grid", Properties.Resources.ASX_Copy_blue_16x, (_, _) => CopyGrid());

        private ToolStripMenuItem GetExportToExcelMenuItem() => new("Export Excel", Properties.Resources.excel16x16,
            (_, _) => ExportToExcel());

        private ToolStripMenuItem GetClearFilterMenuItem() => new("Clear Filters", Properties.Resources.Eraser_16x,
            (_, _) => SetFilter(string.Empty));

        private ToolStripMenuItem GetColumnsMenuItem() => new("Columns", Properties.Resources.Column_16x,
            (_, _) => this.PromptColumnSelection());

        private ToolStripMenuItem GetEditFilterMenuItem() => new("Edit Filter", Properties.Resources.EditFilter_16x,
            (_, _) => PromptFilter());

        public int ClickedColumnIndex { get; private set; } = -1;
        public int ClickedRowIndex { get; private set; } = -1;

        public DBADashDataGridView()
        {
            this.CellFormatting += DBADashDataGridView_CellFormatting;
            this.MouseUp += Dgv_MouseUp;
            this.DataSourceChanged += Dgv_DataSourceChanged;
            EnableDoubleBuffering();
            AddCellContextMenuItems();
            AddColumnContextMenuItems();
            this.ApplyTheme();
        }

        private void Dgv_DataSourceChanged(object sender, EventArgs e)
        {
            FormatFilteredColumns();
        }

        private void AddColumnContextMenuItems()
        {
            ColumnContextMenu = new();
            var clearFilter = GetClearFilterMenuItem();
            var editFilter = GetEditFilterMenuItem();
            ColumnContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    GetExportToExcelMenuItem(),
                    new ToolStripSeparator(),
                    GetColumnsMenuItem(),
                    new ToolStripSeparator(),
                    editFilter,
                    clearFilter,
                    new ToolStripSeparator(),
                }
            );
            ColumnContextMenuOpening += (sender, e) =>
            {
                var filterSupported = DataSource is DataView;
                clearFilter.Enabled = !string.IsNullOrEmpty((DataSource as DataView)?.RowFilter);
                clearFilter.Visible = filterSupported;
                editFilter.Visible = filterSupported;
            };
        }

        private void AddCellContextMenuItems()
        {
            CellContextMenu = new();
            var cellClearFilterMenuItem = GetClearFilterMenuItem();
            var filterByValue =
                new ToolStripMenuItem("Filter By Value", Properties.Resources.Filter_16x, FilterByValue_Click)
                { Tag = "=" };
            var excludeValue =
                new ToolStripMenuItem("Exclude Value", Properties.Resources.StopFilter_16x, FilterByValue_Click)
                { Tag = "<>" };
            var inFilter = new ToolStripMenuItem("IN", Properties.Resources.Filter_16x, (_, _) => InFilter());
            var notInFilter = new ToolStripMenuItem("NOT IN", Properties.Resources.StopFilter_16x, (_, _) => NotInFilter());
            var copyCell = new ToolStripMenuItem("Copy Cell", Properties.Resources.ASX_Copy_grey_16x, CopyCell);
            var editFilter = GetEditFilterMenuItem();
            var filterSeparator = new ToolStripSeparator();

            var allFilters = new ToolStripMenuItem("All Filters", Properties.Resources.FilterDropdown_16x);
            var filterLike = new ToolStripMenuItem("Like", null, (_, _) => FilterLike());
            var filterNotLike = new ToolStripMenuItem("Not Like", null, (_, _) => FilterNotLike());
            var greaterThan = new ToolStripMenuItem(">", null, (_, _) => FilterByValueWithPrompt(">"));
            var lessThan = new ToolStripMenuItem("<", null, (_, _) => FilterByValueWithPrompt("<"));
            var greaterThanEqual = new ToolStripMenuItem(">=", null, (_, _) => FilterByValueWithPrompt(">="));
            var lessThanEqual = new ToolStripMenuItem("<=", null, (_, _) => FilterByValueWithPrompt("<="));
            var equal = new ToolStripMenuItem("=", null, (_, _) => FilterByValueWithPrompt("="));
            var notEqual = new ToolStripMenuItem("<>", null, (_, _) => FilterByValueWithPrompt("<>"));

            allFilters.DropDownItems.AddRange(new ToolStripItem[]
                { filterLike, filterNotLike, equal, notEqual, greaterThan, lessThan, greaterThanEqual, lessThanEqual });

            CellContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    copyCell,
                    GetExportToExcelMenuItem(),
                    new ToolStripSeparator(),
                    GetColumnsMenuItem(),
                    new ToolStripSeparator(),
                    inFilter,
                    notInFilter,
                    filterByValue,
                    excludeValue,
                    allFilters,
                    editFilter,
                    cellClearFilterMenuItem,
                    filterSeparator,
                }
            );

            CellContextMenuOpening += (sender, e) =>
            {
                var dgv = (DataGridView)sender;
                if (dgv == null) return;
                var columnType = dgv.Columns[ClickedColumnIndex].ValueType;
                var filterSupported = DataSource is DataView;
                var columnFilterSupported = filterSupported &&
                                            columnType != typeof(byte[]) &&
                                            columnType != typeof(object) &&
                                            !string.IsNullOrEmpty(dgv.Columns[ClickedColumnIndex].DataPropertyName);
                var inFilterSupported = AreMultipleCellsSelectedFromTheSameColumnOnlyAndMatchClickedColumnIndex() &&
                                        columnFilterSupported;
                var likeFilterSupported = columnFilterSupported && columnType == typeof(string);
                allFilters.Visible = columnFilterSupported;
                inFilter.Visible = inFilterSupported;
                notInFilter.Visible = inFilterSupported;
                filterLike.Visible = likeFilterSupported;
                filterNotLike.Visible = likeFilterSupported;
                cellClearFilterMenuItem.Enabled = HasFilter;
                filterByValue.Visible = columnFilterSupported;
                excludeValue.Visible = columnFilterSupported;
                editFilter.Visible = filterSupported;
                cellClearFilterMenuItem.Visible = filterSupported;
                filterSeparator.Visible = filterSupported;
            };
        }

        public bool HasFilter => !string.IsNullOrEmpty(RowFilter);

        private bool AreMultipleCellsSelectedFromTheSameColumnOnlyAndMatchClickedColumnIndex()
        {
            return SelectedCells.Count > 1 && (SelectedCells.Cast<DataGridViewCell>()
                .Select(selectedCell => selectedCell.ColumnIndex).Distinct().Count() == 1) && SelectedCells[0].ColumnIndex == ClickedColumnIndex;
        }

        private void InFilter(bool isNotIn = false)
        {
            var colName = EscapeColumnName(SelectedColumnName);
            var list = string.Join(',',
                SelectedCells.Cast<DataGridViewCell>().Where(cell => cell.Value != DBNull.Value)
                    .Select(cell => GetFormattedValue(cell.Value)).Distinct());
            var hasNull = SelectedCells.Cast<DataGridViewCell>().Any(cell => cell.Value == DBNull.Value);
            var operatorSymbol = isNotIn ? "NOT IN" : "IN";
            var filter = string.IsNullOrEmpty(RowFilter) ? RowFilter : RowFilter + Environment.NewLine + " AND ";

            if ((hasNull && !isNotIn) || (isNotIn && !hasNull)) // Include NULL if null value is in the list for IN filter or include NULL if it's not in the list for NOT IN filter.
                filter += $"({colName} IS NULL OR {colName} {operatorSymbol}({list}))";
            else
                filter += $"{colName} {operatorSymbol}({list})";

            SetFilter(filter);
        }

        private void NotInFilter() => InFilter(true);

        public void RegisterClearFilter(ToolStripItem item)
        {
            this.GridFilterChanged += (_, _) => { UpdateClearFilter(item); };
            this.DataSourceChanged += (_, _) => { UpdateClearFilter(item); };
            item.Click += (_, _) => ClearFilter();
        }

        private void UpdateClearFilter(ToolStripItem item)
        {
            item.Enabled = !string.IsNullOrEmpty(RowFilter);
            item.Font = new System.Drawing.Font(item.Font, item.Enabled ? FontStyle.Bold : FontStyle.Regular);
            item.ToolTipText = item.Enabled ? RowFilter : "No Filter Applied";
        }

        private void PromptFilter()
        {
            var filter = RowFilter;
            if (CommonShared.ShowInputDialog(ref filter, "Edit Filter") == DialogResult.OK)
            {
                SetFilter(filter);
            }
        }

        private void CopyCell(object sender, EventArgs e)
        {
            Clipboard.SetText(
                Rows[ClickedRowIndex].Cells[ClickedColumnIndex].FormattedValue?.ToString() ?? string.Empty);
        }

        public void ExportToExcel()
        {
            Common.PromptSaveDataGridView(this);
        }

        public void CopyGrid()
        {
            Common.CopyDataGridViewToClipboard(this);
        }

        private bool IsColumnFiltered(int columnIndex)
        {
            if (DataSource is not DataView dv) return false;
            var filter = dv.RowFilter;
            if (string.IsNullOrEmpty(filter)) return false;
            var colName = "[" + Columns[columnIndex].DataPropertyName + "]";
            return filter.Contains(colName);
        }

        private void DBADashDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.Columns[e.ColumnIndex].ValueType == typeof(byte[]) && e.Value != null && e.Value != DBNull.Value)
            {
                byte[] bytes = (byte[])e.Value;
                // Convert the byte array to a hexadecimal string
                e.Value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                e.FormattingApplied = true; // Indicate that formatting was applied
            }
        }

        private void EnableDoubleBuffering()
        {
            // Set DoubleBuffered to true using reflection
            var dgvType = this.GetType().BaseType; // Get the type of the base DataGridView class
            var pi = dgvType?.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi?.SetValue(this, true, null);
        }

        /// <summary>
        /// Used to display context menu when user right-clicks column headers.  Selected column index is stored in clickedColumnIndex
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dgv_MouseUp(object sender, MouseEventArgs e)
        {
            var dgv = (DBADashDataGridView)sender;
            if (e.Button != MouseButtons.Right) return;

            // Perform a hit test to determine where the click occurred
            var hitTestInfo = dgv.HitTest(e.X, e.Y);
            ClickedColumnIndex = hitTestInfo.ColumnIndex;
            ClickedRowIndex = hitTestInfo.RowIndex;
            switch (hitTestInfo.Type)
            {
                case DataGridViewHitTestType.Cell:
                    if (CellContextMenu.Items.Count == 0) return;
                    CellContextMenuOpening?.Invoke(dgv,
                        new DataGridViewCellEventArgs(ClickedColumnIndex, ClickedRowIndex));
                    CellContextMenu.Show(dgv, e.Location);
                    return;

                case DataGridViewHitTestType.ColumnHeader:
                    {
                        if (ColumnContextMenu.Items.Count == 0) return;
                        ColumnContextMenuOpening?.Invoke(dgv,
                            new DataGridViewCellEventArgs(ClickedColumnIndex, ClickedRowIndex));
                        ColumnContextMenu.Show(dgv, e.Location);
                        break;
                    }
            }
        }

        public string RowFilter => DataSource is not DataView dv ? string.Empty : dv.RowFilter;
        public string SortString => DataSource is not DataView dv ? string.Empty : dv.Sort;

        public void SetFilter(string filter)
        {
            if (DataSource is not DataView dv) return;
            var previousFilter = dv.RowFilter;
            try
            {
                dv.RowFilter = filter;
                FormatFilteredColumns();
                GridFilterChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                dv.RowFilter = previousFilter;
                MessageBox.Show("Error setting row filter: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void ClearFilter() => SetFilter(string.Empty);

        private void FormatFilteredColumns()
        {
            foreach (DataGridViewColumn col in Columns)
            {
                col.HeaderCell.Style.Font = new Font(col.HeaderCell.Style.Font ?? Font,
                    IsColumnFiltered(col.Index) ? FontStyle.Bold | FontStyle.Italic : FontStyle.Regular);
            }
        }

        private string SelectedColumnName => Columns[ClickedColumnIndex].DataPropertyName;
        private object SelectedValue => Rows[ClickedRowIndex].Cells[ClickedColumnIndex].Value.DBNullToNull();

        private void FilterLike(bool IsNotLike = false)
        {
            var value = SelectedValue?.ToString();
            if (CommonShared.ShowInputDialog(ref value, "Enter value to filter by:", default,
                    "Use % or * as wildcards") == DialogResult.Cancel) return;
            if (string.IsNullOrEmpty(value)) return;
            AppendFilter(value, SelectedColumnName, IsNotLike ? "NOT LIKE" : "LIKE");
        }

        private void AppendFilter(object value, string colName, string operatorSymbol)
        {
            var filter = string.IsNullOrEmpty(RowFilter) ? RowFilter : RowFilter + Environment.NewLine + " AND ";
            filter += FormatFilterExpression(value, colName, operatorSymbol);
            SetFilter(filter);
        }

        private void FilterNotLike() => FilterLike(true);

        private void FilterByValueWithPrompt(string operatorSymbol)
        {
            var valueString = SelectedValue?.ToString();
            if (CommonShared.ShowInputDialog(ref valueString, $"Enter value to filter by {operatorSymbol}:") ==
                DialogResult.Cancel) return;
            if (string.IsNullOrEmpty(valueString)) return;
            var value = Convert.ChangeType(valueString,
                SelectedValue?.GetType() ?? typeof(string)); // convert back to original type
            AppendFilter(value, SelectedColumnName, operatorSymbol);
        }

        private void FilterByValue_Click(object sender, EventArgs e)
        {
            if (DataSource is not DataView dv) return;
            var operatorSymbol = ((ToolStripMenuItem)sender).Tag!.ToString();

            AppendFilter(SelectedValue, SelectedColumnName, operatorSymbol);
        }

        private static string EscapeColumnName(string columnName)
        {
            return "[" + columnName.Replace("]", "]]") + "]";
        }

        private static string EscapeValue(string value) => "'" + value?.Replace("'", "''") + "'";

        private static string FormatFilterExpression(object value, string colName, string operatorSymbol)
        {
            colName = EscapeColumnName(colName);
            // Validation
            var validOperators = new[] { "=", "<>", ">", "<", ">=", "<=", "LIKE", "NOT LIKE" };
            if (!validOperators.Contains(operatorSymbol))
            {
                throw new ArgumentException("Invalid operator symbol.", nameof(operatorSymbol));
            }

            // Handle NULL value
            if (value.DBNullToNull() is null)
            {
                return operatorSymbol switch
                {
                    "=" => $"{colName} IS NULL",
                    "<>" => $"{colName} IS NOT NULL",
                    _ => throw new ArgumentException("Invalid operator for NULL value.", nameof(operatorSymbol))
                };
            }

            // Format value for the expression based on its type
            var formattedValue = GetFormattedValue(value);

            return operatorSymbol switch
            {
                "<>" or "NOT LIKE" => $"({colName} {operatorSymbol} {formattedValue} OR {colName} IS NULL)",
                _ => $"{colName} {operatorSymbol} {formattedValue}"
            };
        }

        private static string GetFormattedValue(object value)
        {
            return value switch
            {
                _ when value.GetType().IsNumericType() => value.ToString(),
                DateTime dateTimeValue => $"#{dateTimeValue:yyyy-MM-dd HH:mm:ss.fff}#",
                bool boolValue => boolValue.ToString(),
                _ => EscapeValue(value.ToString())
            };
        }
    }
}