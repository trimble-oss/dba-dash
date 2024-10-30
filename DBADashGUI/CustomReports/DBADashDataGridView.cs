using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
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

        private readonly ToolStripMenuItem filterLike = new("LIKE", Properties.Resources.FilteredTextBox_16x);

        private ToolStripMenuItem GetCopyGridMenuItem() => new("Copy Grid", Properties.Resources.ASX_Copy_blue_16x, (_, _) => CopyGrid());

        private ToolStripMenuItem GetExportToExcelMenuItem() => new("Export Excel", Properties.Resources.excel16x16, (_, _) => ExportToExcel());

        private ToolStripMenuItem GetClearFilterMenuItem() => new("Clear Filters", Properties.Resources.Eraser_16x, (_, _) => SetFilter(string.Empty));

        private ToolStripMenuItem GetColumnsMenuItem() => new("Columns", Properties.Resources.Column_16x, (_, _) => this.PromptColumnSelection());

        private ToolStripMenuItem GetEditFilterMenuItem() => new("Edit Filter", Properties.Resources.FilteredTextBox_16x, (_, _) => PromptFilter());

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
            ColumnContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    GetExportToExcelMenuItem(),
                    new ToolStripSeparator(),
                    GetColumnsMenuItem(),
                    new ToolStripSeparator(),
                    GetEditFilterMenuItem(),
                    clearFilter,
                    new ToolStripSeparator(),
                }
            );
            ColumnContextMenuOpening += (sender, e) =>
            {
                clearFilter.Enabled = !string.IsNullOrEmpty((DataSource as DataView)?.RowFilter);
            };
        }

        private void AddCellContextMenuItems()
        {
            CellContextMenu = new();
            var cellClearFilterMenuItem = GetClearFilterMenuItem();
            var filterByValue = new ToolStripMenuItem("Filter By Value", Properties.Resources.Filter_16x, FilterByValue_Click) { Tag = false };
            var excludeValue = new ToolStripMenuItem("Exclude Value", Properties.Resources.StopFilter_16x, FilterByValue_Click) { Tag = true };
            var copyCell = new ToolStripMenuItem("Copy Cell", Properties.Resources.ASX_Copy_grey_16x, CopyCell);

            filterLike.Click += FilterLike_Click;

            CellContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    copyCell,
                    GetExportToExcelMenuItem(),
                    new ToolStripSeparator(),
                    GetColumnsMenuItem(),
                    new ToolStripSeparator(),
                    filterByValue,
                    excludeValue,
                    filterLike,
                    GetEditFilterMenuItem(),
                    cellClearFilterMenuItem,
                    new ToolStripSeparator(),
                }
            );

            CellContextMenuOpening += (sender, e) =>
            {
                var dgv = (DataGridView)sender;
                if (dgv == null) return;
                var columnType = dgv.Columns[ClickedColumnIndex].ValueType;
                var filterSupported = columnType != typeof(byte[]) &&
                                      !string.IsNullOrEmpty(dgv.Columns[ClickedColumnIndex].DataPropertyName);

                filterLike.Visible = filterSupported && columnType == typeof(string);
                cellClearFilterMenuItem.Enabled = !string.IsNullOrEmpty((DataSource as DataView)?.RowFilter);
                filterByValue.Visible = filterSupported;
                excludeValue.Visible = filterSupported;
            };
        }

        public void RegisterClearFilter(ToolStripItem item)
        {
            this.GridFilterChanged += (_, _) =>
            {
                UpdateClearFilter(item);
            };
            this.DataSourceChanged += (_, _) =>
            {
                UpdateClearFilter(item);
            };
            item.Click += (_, _) => ClearFilter();
        }

        private void UpdateClearFilter(ToolStripItem item)
        {
            item.Enabled = !string.IsNullOrEmpty(RowFilter);
            item.Font = new System.Drawing.Font(item.Font, item.Enabled ? FontStyle.Bold : FontStyle.Regular);
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
            Clipboard.SetText(Rows[ClickedRowIndex].Cells[ClickedColumnIndex].FormattedValue?.ToString() ?? string.Empty);
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

        private void FilterLike_Click(object sender, EventArgs e)
        {
            var dgv = (DataGridView)CellContextMenu.SourceControl;
            var value = dgv.Rows[ClickedRowIndex].Cells[ClickedColumnIndex].Value.DBNullToNull()?.ToString();
            var colName = dgv.Columns[ClickedColumnIndex].DataPropertyName;

            if (CommonShared.ShowInputDialog(ref value, "Enter value to filter by:", default, "Use % or * as wildcards") == DialogResult.Cancel) return;
            if (string.IsNullOrEmpty(value)) return;
            if (dgv.DataSource is not DataView dv) return;
            var filter = dv.RowFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                filter += Environment.NewLine + " AND ";
            }
            value = EscapeValue(value);
            filter += $"[{colName}] LIKE {value}";
            SetFilter(filter);
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
                    filterLike.Visible = dgv.Columns[ClickedColumnIndex].ValueType == typeof(string);
                    CellContextMenuOpening?.Invoke(dgv, new DataGridViewCellEventArgs(ClickedColumnIndex, ClickedRowIndex));
                    CellContextMenu.Show(dgv, e.Location);
                    return;

                case DataGridViewHitTestType.ColumnHeader:
                    {
                        if (ColumnContextMenu.Items.Count == 0) return;
                        ColumnContextMenuOpening?.Invoke(dgv, new DataGridViewCellEventArgs(ClickedColumnIndex, ClickedRowIndex));
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
                col.HeaderCell.Style.Font = new Font(col.HeaderCell.Style.Font ?? Font, IsColumnFiltered(col.Index) ? FontStyle.Bold | FontStyle.Italic : FontStyle.Regular);
            }
        }

        private void FilterByValue_Click(object sender, EventArgs e)
        {
            var exclude = (bool)((ToolStripMenuItem)sender).Tag;
            var value = Rows[ClickedRowIndex].Cells[ClickedColumnIndex].Value;
            var colName = Columns[ClickedColumnIndex].DataPropertyName;

            if (value.GetType() == typeof(byte[]))
            {
                MessageBox.Show("Filter by value is not supported for binary data", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            colName = EscapeColumnName(colName);
            var filterValue = FormatFilterValue(value, exclude);

            if (DataSource is not DataView dv) return;
            var filter = dv.RowFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                filter += Environment.NewLine + " AND ";
            }

            filter += $"{colName} {filterValue}";
            SetFilter(filter);
        }

        private static string EscapeColumnName(string columnName)
        {
            return "[" + columnName.Replace("]", "]]") + "]";
        }

        private static string EscapeValue(string value) => "'" + value?.Replace("'", "''") + "'";

        private static string FormatFilterValue(object value, bool exclude)
        {
            var compare = (exclude ? "<>" : "=");
            if (value.DBNullToNull() is null)
            {
                return exclude ? "IS NOT NULL" : "IS NULL";
            }
            else if (value.GetType().IsNumericType())
            {
                return compare + value;
            }
            else if (value is DateTime)
            {
                return $"{compare} #{value:yyyy-MM-dd HH:mm:ss.fff}#";
            }
            else
            {
                return $"{compare} " + EscapeValue(value.ToString());
            }
        }
    }
}