using DBADash.Deadlock;
using DBADash.QueryPlan;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using System;
using System.Data;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// A result set on a tab of the viewer window, in DBA Dash's own grid rather than SSMS's: filtering, Group By,
    /// column stats, and export to Excel, JSON, Markdown and the rest all come with <see cref="DBADashDataGridView"/>.
    ///
    /// The SSMS extension opens a results grid here, and it opens the grid files DBA Dash itself exports too.
    /// xml and json columns are links, as they are in SSMS: a plan or deadlock graph - sp_BlitzCache's query_plan,
    /// sp_BlitzLock's deadlock_graph - opens in its viewer on a tab of the same window, anything else in a code
    /// window.  A plan or graph in a column of any other type opens in its viewer when double-clicked.
    /// </summary>
    public sealed class GridViewerControl : UserControl
    {
        private readonly DBADashDataGridView _grid;
        private readonly DataTable _table;
        private readonly ToolStripLabel _rowCount = new() { Alignment = ToolStripItemAlignment.Right };
        private bool _columnsConfigured;

        public GridViewerControl(DataTable table, string title)
        {
            _table = table;
            Title = string.IsNullOrWhiteSpace(title) ? "Results" : title;

            _grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                // Room for the data; the grid's context menus can bring them back, numbered or not.
                RowHeadersVisible = false,
                // Not AllCells: a result set can be a lot of rows to measure.
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ResultSetName = Title
            };
            _grid.DataBindingComplete += (_, _) =>
            {
                // Columns only exist once the grid has bound, which waits for it to be on a form.
                if (!_columnsConfigured) ConfigureColumns();
                ShowRowCount();
            };
            _grid.GridFilterChanged += (_, _) => ShowRowCount();
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.CellContentClick += Grid_CellContentClick;

            Controls.Add(_grid);
            Controls.Add(BuildToolbar());

            // A DataView rather than the table, as that's what the grid's filtering works through.
            _grid.DataSource = new DataView(table);

            this.ApplyTheme();
        }

        /// <summary>What the tab is called: the name the result set was opened with.</summary>
        public string Title { get; }

        /// <summary>The tab's tooltip: the title, and the result set's size.</summary>
        public string TabToolTip => $"{Title}\n{_table.Rows.Count:N0} rows, {_table.Columns.Count:N0} columns";

        private void ConfigureColumns()
        {
            _columnsConfigured = true;

            foreach (var column in _grid.Columns.Cast<DataGridViewColumn>().ToList())
            {
                // Binding makes an image column for binary data - the plan and sql handles throughout what the DBA
                // Dash service collects.  As text, the grid shows it as 0x... hex, as SSMS does.
                if (column is DataGridViewImageColumn && column.ValueType == typeof(byte[]))
                {
                    ReplaceColumn(column, new DataGridViewTextBoxColumn());
                    continue;
                }

                // The column's SQL type, where the SSMS extension recorded it - the grid only knows the .NET one.
                if (SqlType(column) is not { } sqlType) continue;

                var configured = IsLinkType(sqlType)
                    ? ReplaceColumn(column, new DataGridViewLinkColumn { TrackVisitedState = false })
                    : column;
                configured.ToolTipText = $"{configured.HeaderText} ({sqlType})";
            }

            // Link columns are added after the grid was themed, so they need their link colours now.
            _grid.ApplyTheme();
        }

        private string SqlType(DataGridViewColumn column) =>
            _table.Columns[column.DataPropertyName]?.ExtendedProperties["SqlType"] as string;

        /// <summary>
        /// The types SSMS shows as a link that opens in a window of its own: QEResultSet.IsXMLColumn and
        /// IsJsonColumn compare the server's type name the same way.
        /// </summary>
        private static bool IsLinkType(string sqlType) =>
            sqlType.Equals("xml", StringComparison.OrdinalIgnoreCase) ||
            sqlType.Equals("json", StringComparison.OrdinalIgnoreCase);

        /// <summary>Swaps a bound column for another kind of column over the same data, in the same place.</summary>
        private T ReplaceColumn<T>(DataGridViewColumn column, T replacement) where T : DataGridViewColumn
        {
            replacement.Name = column.Name;
            replacement.HeaderText = column.HeaderText;
            replacement.DataPropertyName = column.DataPropertyName;
            replacement.ValueType = column.ValueType;
            replacement.SortMode = DataGridViewColumnSortMode.Automatic;

            var index = column.Index;
            var displayIndex = column.DisplayIndex;
            _grid.Columns.Remove(column);
            _grid.Columns.Insert(index, replacement);
            replacement.DisplayIndex = displayIndex;
            return replacement;
        }

        /// <summary>
        /// Sizes the columns to what's on screen - the grid's [Smart] Displayed.  Waits for the tab to be shown and
        /// laid out: measured any earlier there are no displayed cells to size to, and every column is left at its
        /// minimum width.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BeginInvoke(() =>
            {
                if (_grid.Columns.Count > 0)
                {
                    _grid.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            });
        }

        private ToolStrip BuildToolbar()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };

            toolbar.Items.Add(new ToolStripButton("Open...", Resources.FolderOpened_16x, (_, _) => OpenFiles())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open a query plan, deadlock graph or grid file"
            });
            toolbar.Items.Add(new ToolStripSeparator());
            // The grid's own copy, export, transpose, Group By and filter actions - the same as its context menus.
            toolbar.Items.AddRange(_grid.CreateToolbarItems());
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(BuildSettingsMenu());
            toolbar.Items.Add(_rowCount);

            return toolbar;
        }

        private static ToolStripDropDownButton BuildSettingsMenu()
        {
            var settings = new ToolStripDropDownButton("Settings", Resources.SettingsOutline_16x)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Settings"
            };

            ViewerApp.AddFileAssociationMenuItems(settings);
            ViewerApp.AddSettingsMenuItems(settings.DropDownItems);

            return settings;
        }

        private void ShowRowCount()
        {
            var shown = _grid.Rows.Count;
            _rowCount.Text = shown == _table.Rows.Count
                ? $"{shown:N0} rows"
                : $"{shown:N0} of {_table.Rows.Count:N0} rows";
        }

        private void OpenFiles()
        {
            var files = ViewerApp.PromptForFiles(FindForm());
            if (files.Count > 0) ViewerApp.OpenFiles(files);
        }

        /// <summary>
        /// Opens an xml or json link cell: a plan or deadlock graph in its viewer, anything else in a code window.
        /// </summary>
        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex] is not DataGridViewLinkColumn column) return;
            if (_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not string text) return;

            try
            {
                var isJson = SqlType(column)?.Equals("json", StringComparison.OrdinalIgnoreCase) == true;
                if (!isJson && TryOpenInViewer(text, e.RowIndex)) return;

                // The code editor indents XML itself; JSON it shows as it is, so that's indented here.
                CommonShared.ShowCodeViewer(isJson ? FormatJson(text) : text,
                    $"{column.HeaderText} - {RowCaption(e.RowIndex)}",
                    isJson ? CodeEditor.CodeEditorModes.Json : CodeEditor.CodeEditorModes.XML);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error opening cell");
            }
        }

        /// <summary>Opens a plan or deadlock graph cell in its viewer. Anything else is left to the grid.</summary>
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            // A link column already opens on a single click.
            if (_grid.Columns[e.ColumnIndex] is DataGridViewLinkColumn) return;
            if (_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not string xml) return;

            try
            {
                TryOpenInViewer(xml, e.RowIndex);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error opening cell");
            }
        }

        /// <summary>Opens <paramref name="xml"/> in the plan or deadlock viewer if it's one of the two.</summary>
        private bool TryOpenInViewer(string xml, int rowIndex)
        {
            // The same cheap checks the viewers' own file open uses to tell the two apart - a deadlock graph never
            // contains ShowPlanXML.  Must start as XML, so a query's text that merely mentions either isn't taken
            // for one.
            if (!xml.TrimStart().StartsWith('<')) return false;

            if (PlanParser.LooksLikeExecutionPlan(xml))
            {
                ViewerLauncher.ShowQueryPlan(xml, $"{Title} - Plan ({RowCaption(rowIndex)})");
                return true;
            }

            if (DeadlockParser.IsDeadlockXml(xml))
            {
                ViewerLauncher.ShowDeadlockGraph(xml, $"{Title} - Deadlock ({RowCaption(rowIndex)})");
                return true;
            }

            return false;
        }

        /// <summary>Which row something was opened from, so what's opened from several can be told apart.</summary>
        private static string RowCaption(int rowIndex) => $"row {rowIndex + 1:N0}";

        private static string FormatJson(string json)
        {
            try
            {
                return JsonNode.Parse(json)?.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }) ?? json;
            }
            catch (JsonException)
            {
                return json;
            }
        }
    }
}
