using DBADash;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;
using System.Globalization;

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

        private ToolStripMenuItem GetSaveTableMenuItem()
        {
            var tsSave = new ToolStripMenuItem("Save Table", Properties.Resources.SaveTable_16x) { ToolTipText = "Save to table in SQL Server database" };
            tsSave.DropDownItems.AddRange(new[]{
                new ToolStripMenuItem("From Data Table", Properties.Resources.DataTable_16x,
                    (_, _) => SaveTable(false)) {ToolTipText = "Save underlying DataTable to table in SQL Server database"},
                new ToolStripMenuItem("From Grid", Properties.Resources.Table_16x,
                (_, _) => SaveTable(true)){ ToolTipText = "Save grid to table in SQL Server database" },
                new ToolStripMenuItem("Script Data Table", Properties.Resources.SQLScript_16x,
                    (_, _) => ScriptTable(false)) {ToolTipText = "Script underlying DataTable AS INSERT"},
                new ToolStripMenuItem("Script Grid", Properties.Resources.TableScript_16x,
                    (_, _) => ScriptTable(true)){ ToolTipText = "Script grid as INSERT" }
            });
            return tsSave;
        }

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
            this.ColumnAdded += (_, args) => args.Column.ApplyTheme();
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
            var saveTable = GetSaveTableMenuItem();
            ColumnContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    GetExportToExcelMenuItem(),
                    saveTable,
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
                saveTable.DropDownItems[0].Enabled = DataSource is DataTable or DataView;
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
            var notInFilter =
                new ToolStripMenuItem("NOT IN", Properties.Resources.StopFilter_16x, (_, _) => NotInFilter());
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
            var saveTable = GetSaveTableMenuItem();

            CellContextMenu.Items.AddRange(
                new ToolStripItem[]
                {
                    GetCopyGridMenuItem(),
                    copyCell,
                    GetExportToExcelMenuItem(),
                    saveTable,
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
                saveTable.DropDownItems[0].Enabled = DataSource is DataTable or DataView;
            };
        }

        public bool HasFilter => !string.IsNullOrEmpty(RowFilter);

        private bool AreMultipleCellsSelectedFromTheSameColumnOnlyAndMatchClickedColumnIndex()
        {
            return SelectedCells.Count > 1 && (SelectedCells.Cast<DataGridViewCell>()
                       .Select(selectedCell => selectedCell.ColumnIndex).Distinct().Count() == 1) &&
                   SelectedCells[0].ColumnIndex == ClickedColumnIndex;
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

            if ((hasNull && !isNotIn) ||
                (isNotIn && !hasNull)) // Include NULL if null value is in the list for IN filter or include NULL if it's not in the list for NOT IN filter.
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
            if (Rows[ClickedRowIndex].Cells[ClickedColumnIndex].ValueType ==
                typeof(string)) // Formatted value could be truncated
            {
                Clipboard.SetText(
                    Rows[ClickedRowIndex].Cells[ClickedColumnIndex].Value?.ToString() ?? string.Empty);
            }
            else
            {
                Clipboard.SetText(
                    Rows[ClickedRowIndex].Cells[ClickedColumnIndex].FormattedValue?.ToString() ?? string.Empty);
            }
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

        private const int CellTruncateLength = 43679; // Cell will not display text beyond this length

        private void DBADashDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.Columns[e.ColumnIndex].ValueType == typeof(byte[]) && e.Value != null && e.Value != DBNull.Value)
            {
                var bytes = (byte[])e.Value;
                // Convert the byte array to a hexadecimal string
                e.Value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                e.FormattingApplied = true; // Indicate that formatting was applied
            }
            else if (this.Columns[e.ColumnIndex].ValueType == typeof(string) && e.Value != null &&
                     e.Value != DBNull.Value && ((string)e.Value).Length > CellTruncateLength)
            {
                e.Value = ((string)e.Value)[..(CellTruncateLength - 3)] + "...";
                e.FormattingApplied = true;
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
            try
            {
                var value = Convert.ChangeType(valueString,
                    SelectedValue?.GetType() ?? typeof(string)); // convert back to original type
                AppendFilter(value, SelectedColumnName, operatorSymbol);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting value: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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

        /// <summary>
        /// Returns a DataTable containing the data to be exported.
        /// </summary>
        /// <param name="fromGrid">If true, exports the DataGridView.  If false, exports the underlying DataTable.</param>
        private DataTable GetDataTableForExport(bool fromGrid)
        {
            if (fromGrid)
            {
                return DataGridViewToDataTable(this);
            }
            return DataSource switch
            {
                DataView dv => dv.ToTable(),
                DataTable dt => dt,
                _ => DataGridViewToDataTable(this)
            };
        }

        /// <summary>
        /// Generates a SQL CREATE TABLE command based on the provided DataTable schema.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the schema to generate the CREATE TABLE command.</param>
        /// <param name="tableName">The name of the table to be created.</param>
        /// <returns>A string containing the SQL CREATE TABLE command.</returns>
        private static string GenerateCreateTableCommand(DataTable dataTable, string tableName, bool quoteTableName = true)
        {
            var quotedTableName = quoteTableName ? tableName.SqlQuoteName() : tableName;
            var commandText = new StringBuilder($"CREATE TABLE {quotedTableName} (\n\t");

            for (var i = 0; i < dataTable.Columns.Count; i++)
            {
                var column = dataTable.Columns[i];
                var sqlType = ConvertToSqlType(column);

                commandText.Append($"{column.ColumnName.SqlQuoteName()} {sqlType}");

                if (column.AutoIncrement)
                {
                    commandText.Append(" IDENTITY(1,1)");
                }

                if (!column.AllowDBNull)
                {
                    commandText.Append(" NOT NULL");
                }

                if (i < dataTable.Columns.Count - 1)
                {
                    commandText.Append(",\n\t");
                }
            }

            commandText.Append("\n);");

            return commandText.ToString();
        }

        /// <summary>
        /// Converts a DataColumn to its corresponding SQL data type as a string.
        /// </summary>
        /// <param name="column">The DataColumn to convert.</param>
        /// <returns>A string representing the SQL data type.</returns>
        private static string ConvertToSqlType(DataColumn column)
        {
            var columnSize = column.MaxLength; // Note: Not set using DataAdapter Fill method, so types will end up being MAX
            const int numericPrecision = 28;
            const int numericScale = 9;

            return column.DataType switch
            {
                { } t when t == typeof(byte) => "TINYINT",
                { } t when t == typeof(short) => "SMALLINT",
                { } t when t == typeof(int) => "INT",
                { } t when t == typeof(long) => "BIGINT",
                { } t when t == typeof(float) => "REAL",
                { } t when t == typeof(double) => "FLOAT",
                { } t when t == typeof(decimal) || t == typeof(decimal?) =>
                    $"DECIMAL({numericPrecision}, {numericScale})",
                { } t when t == typeof(bool) => "BIT",
                { } t when t == typeof(DateTime) => "DATETIME2",
                { } t when t == typeof(TimeSpan) => "TIME",
                { } t when t == typeof(char) || (t == typeof(string) && columnSize == 1) => "CHAR(1)",
                { } t when t == typeof(string) => columnSize is > 0 and <= 4000
                    ? $"NVARCHAR({columnSize})"
                    : "NVARCHAR(MAX)",
                { } t when t == typeof(byte[]) => columnSize is > 0 and <= 8000
                    ? $"VARBINARY({columnSize})"
                    : "VARBINARY(MAX)",
                { } t when t == typeof(Guid) => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(MAX)"
            };
        }

        /// <summary>
        /// Saves the provided DataTable to a SQL Server table.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the data to be saved.</param>
        /// <param name="tableName">The name of the table to be created or inserted into.</param>
        /// <param name="connectionString">The connection string to the SQL Server database.</param>
        private static bool SaveDataTableToSql(DataTable dataTable, string tableName, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            var createSQL = GenerateCreateTableCommand(dataTable, tableName);
            cn.Open();

            var cmd = new SqlCommand(createSQL, cn);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 2714) // Table already exists, provide the option to append
            {
                if (MessageBox.Show("The table already exists.  Import into existing table?", "WARNING",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
            }

            using var bulkCopy = new SqlBulkCopy(cn);

            bulkCopy.DestinationTableName = tableName;

            foreach (DataColumn col in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            bulkCopy.WriteToServer(dataTable);
            return true;
        }

        public static string LastConnectionStringForExport = null;

        // / <summary>
        // / Saves the DataGridView to a SQL Server table.
        // / </summary>
        // / <param name="fromGrid">If true, saves the DataGridView to a table.  If false, saves the underlying DataTable to a table.</param>
        private void SaveTable(bool fromGrid)
        {
            var frm = new DBConnection() { InitialCatalogRequired = true };
            if (!string.IsNullOrEmpty(LastConnectionStringForExport))
            {
                frm.ConnectionString = LastConnectionStringForExport;
            }
            if (frm.ShowDialog() != DialogResult.OK) return;
            LastConnectionStringForExport = frm.ConnectionString;
            var tableName = DateTime.Now.ToString("yyyyMMddHHmmss");
            CommonShared.ShowInputDialog(ref tableName, "Enter table name:");
            try
            {
                if (SaveDataTableToSql(GetDataTableForExport(fromGrid), tableName, frm.ConnectionString))
                {
                    MessageBox.Show("Table saved successfully.", "Success", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving table: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Converts a DataGridView to a DataTable.
        /// </summary>
        /// <param name="dgv">The DataGridView to convert.</param>
        public DataTable DataGridViewToDataTable(DataGridView dgv)
        {
            var dt = new DataTable();
            Dictionary<int, DataColumn> columnMapping = new();
            var columnNames = new HashSet<string>();
            // Create table columns based on DataGridView columns
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (!column.Visible) continue; // Add only visible columns to DataTable
                var columnName = column.HeaderText;
                // Ensure unique column names for the DataTable
                var duplicateCount = 0;
                while (columnNames.Contains(columnName))
                {
                    duplicateCount++;
                    columnName = $"{column.HeaderText}[{duplicateCount}]";
                }
                columnNames.Add(columnName);

                var dc = new DataColumn(columnName);
                dc.DataType = column.InferColumnType();

                columnMapping.Add(column.Index, dc);
                dt.Columns.Add(dc);
            }

            // Populate the DataTable with rows from the DataGridView
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue; // Skip the new row in DataGridView
                var dr = dt.NewRow();
                foreach (var col in columnMapping)
                {
                    if (row.Cells[col.Key].Value != null)
                        dr[col.Value] = row.Cells[col.Key].Value;
                    else
                        dr[col.Value] = DBNull.Value; // Handle null values
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }

        // / <summary>
        // / Script DataGridView AS INSERT statements
        // / </summary>
        // / <param name="fromGrid">If true, saves the DataGridView to a table.  If false, saves the underlying DataTable to a table.</param>
        private void ScriptTable(bool fromGrid)
        {
            const string tableName = "#DBADashGrid";
            try
            {
                var dt = GetDataTableForExport(fromGrid);
                var tableScript = GenerateCreateTableCommand(dt, tableName, false);
                var insertStatements = GenerateInsertStatementsWithBatching(dt, tableName, false);
                var header = @$"/*********************************************************
----------------------------------------------------------
|   ____   ____     _      ____               _          |
|  |  _ \ | __ )   / \    |  _ \   __ _  ___ | |__       |
|  | | | ||  _ \  / _ \   | | | | / _` |/ __|| '_ \      |
|  | |_| || |_) |/ ___ \  | |_| || (_| |\__ \| | | |     |
|  |____/ |____//_/   \_\ |____/  \__,_||___/|_| |_|     |
|                                                        |
|	    SQL Server Monitoring by David Wiseman			 |
|       Copyright 2022 Trimble, Inc.					 |
|		https://dbadash.com								 |
|	    https://github.com/trimble-oss/dba-dash			 |
|       Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}					 |
----------------------------------------------------------
*********************************************************/

IF OBJECT_ID('tempdb..{tableName}') IS NOT NULL
BEGIN
    DROP TABLE {tableName}
END
GO
";
                var footer = $"\n\nSELECT {GetColumnList(dt)}\nFROM {tableName}\n\n--DROP TABLE {tableName}";
                var insertScript = header + tableScript + "\n" + string.Join("\n", insertStatements) + footer;
                var frm = new CodeViewer() { Code = insertScript, Language = CodeEditor.CodeEditorModes.SQL };
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating script: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public static List<string> GenerateInsertStatementsWithBatching(DataTable dataTable, string tableName, bool quoteTableName = true)
        {
            const int batchSize = 1000;
            var insertStatements = new List<string>();
            var totalRows = dataTable.Rows.Count;
            var batchCount = (int)Math.Ceiling(totalRows / (double)batchSize);
            var quotedTableName = quoteTableName ? tableName.SqlQuoteName() : tableName;
            for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                var batchRows = dataTable.AsEnumerable().Skip(batchIndex * batchSize).Take(batchSize);
                var valuesList = batchRows.Select(row => string.Join(", ", row.ItemArray.Select((value, index) => FormatSqlValue(value, dataTable.Columns[index])))).Select(rowValues => $"({rowValues})").ToList();

                var columnNames = GetColumnList(dataTable);
                var valuesClause = string.Join(",\n", valuesList);
                var insertStatement = $"INSERT INTO {quotedTableName} (\n\t{columnNames})\nVALUES {valuesClause};";
                insertStatements.Add(insertStatement);
            }

            return insertStatements;
        }

        private static string GetColumnList(DataTable dataTable) => string.Join(", \n\t", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName.SqlQuoteName()));

        /// <summary>
        /// Formats a value for use in a SQL statement.
        /// </summary>
        private static string FormatSqlValue(object value, DataColumn column)
        {
            if (value == DBNull.Value)
            {
                return "NULL";
            }
            else if (column.DataType == typeof(string))
            {
                return value.ToString().SqlSingleQuoteWithEncapsulation();
            }
            else if (column.DataType == typeof(DateTime))
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff").SqlSingleQuoteWithEncapsulation();
            }
            else if (column.DataType == typeof(bool))
            {
                return (bool)value ? "1" : "0";
            }
            else if (column.DataType == typeof(byte[]))
            {
                // Convert byte array to hexadecimal string
                var byteArray = (byte[])value;
                var hexString = "0x" + BitConverter.ToString(byteArray).Replace("-", "");
                return hexString;
            }
            else if (column.DataType == typeof(int) || column.DataType == typeof(long) || column.DataType == typeof(short) || column.DataType == typeof(byte) || column.DataType == typeof(decimal) || column.DataType == typeof(float) || column.DataType == typeof(double))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            else
            {
                return value.ToString().SqlSingleQuoteWithEncapsulation();
            }
        }
    }
}