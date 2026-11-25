using DBADash.Deadlock;
using DBADash.QueryPlan;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
    ///
    /// A text column whose every value is a JSON object or array - nested JSON in a JSON file's records, or JSON held
    /// as text - is a link too, opening in a code window the same way.  Right-clicking a cell of JSON offers it as a
    /// grid of its own, on a new tab, so nested records can be filtered and sorted like the rest - see
    /// <see cref="NestedJson"/>.
    ///
    /// Data bars added to the grid are remembered for the next result set with the same columns - see
    /// <see cref="GridDataBarMemory"/>.
    /// </summary>
    public sealed class GridViewerControl : UserControl
    {
        private readonly DBADashDataGridView _grid;
        private readonly DataTable _table;
        private readonly ToolStripStatusLabel _rowCount = new() { Alignment = ToolStripItemAlignment.Right };
        private bool _columnsConfigured;

        /// <summary>Checks the file a grid was read from, when Watch is on - see <see cref="FileWatch"/>.</summary>
        private readonly FileWatch _watch;
        private ToolStripButton _reloadButton;
        private ToolStripButton _watchButton;
        private readonly ToolStripStatusLabel _watchStatus = new();
        private readonly ToolStripStatusLabel _pendingStatus = new();
        private DateTime? _lastRead;
        private ToolStripButton _fileTextButton;

        /// <summary>The columns shown as JSON links: a json column from SSMS, or one whose values are all JSON.</summary>
        private readonly HashSet<string> _jsonColumns = new(StringComparer.OrdinalIgnoreCase);

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
            _grid.ColumnStateChanged += (_, e) =>
            {
                if (_columnsConfigured && e.StateChanged == DataGridViewElementStates.Visible) ShowRowCount();
            };
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.CellContentClick += Grid_CellContentClick;
            AddJsonMenuItems();
            _watch = new FileWatch(CheckForNewData);

            Controls.Add(_grid);
            // Docked after the toolbar, so below it: what went wrong reading the file, where something did.  Always
            // there, so a later read of a growing file can show or clear it without rebuilding the grid.
            Controls.Add(_notice);
            UpdateReadState();

            Controls.Add(BuildToolbar());
            Controls.Add(BuildStatusBar());

            // A DataView rather than the table, as that's what the grid's filtering works through.
            _grid.DataSource = new DataView(table);

            this.ApplyTheme();
        }

        /// <summary>The warning shown above the grid, or null - see <see cref="JsonRecords.LoadWarningProperty"/>.</summary>
        public string Notice => _notice.Visible ? _notice.Text : null;

        private readonly NoticeLabel _notice = new() { Dock = DockStyle.Top, Visible = false };

        /// <summary>Incomplete records at the end of the file - see <see cref="JsonRecordsGrowth.UnreadableCounts"/>.</summary>
        public int PendingRecordCount => JsonRecordsGrowth.UnreadableCounts(_table).Pending;

        /// <summary>
        /// Sorts out what the read says.  A record at the end that isn't complete yet is what a file being written looks
        /// like, so it goes on the status bar.  Only an unreadable part before the end - a problem in the file itself -
        /// is shown above the grid.
        /// </summary>
        private void UpdateReadState()
        {
            var (pending, unreadable) = JsonRecordsGrowth.UnreadableCounts(_table);
            var problemText = JsonRecordsGrowth.ProblemText(unreadable - pending);
            _notice.Text = problemText ?? string.Empty;
            _notice.Visible = problemText is not null;
            _pendingStatus.Text = JsonRecordsGrowth.PendingText(pending) ?? string.Empty;
        }

        /// <summary>A one-line warning across the top of the grid, in the theme's warning colours.</summary>
        private sealed class NoticeLabel : Label, IThemedControl
        {
            public NoticeLabel()
            {
                AutoSize = false;
                AutoEllipsis = true;
                Padding = new Padding(6, 0, 6, 0);
                Height = TextRenderer.MeasureText("X", Font).Height + 8;
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }

            public void ApplyTheme(BaseTheme theme)
            {
                BackColor = theme.WarningLowBackColor;
                ForeColor = theme.WarningLowForeColor;
            }
        }

        /// <summary>
        /// The file the grid was read from, once a host has said so with <see cref="SetSource"/> - it's then the grid can
        /// be read again, and watched for new data.  Null for a grid from anywhere else.
        /// </summary>
        public string SourcePath => _watch.Path;

        /// <summary>
        /// Raised when the grid's file should be read again - by the Reload button (<c>manual</c> is true), or when the
        /// Watch button is on and the file has changed (false).
        /// </summary>
        public event Action<GridViewerControl, bool> ReloadRequested;

        /// <summary>Whether the file is checked every few seconds, and read again when it changes - the Watch button.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool WatchForNewData
        {
            get => _watchButton?.Checked ?? false;
            set
            {
                if (_watchButton is not null) _watchButton.Checked = value;
            }
        }

        /// <summary>The filter in force on the grid, so a grid read again from the file can show the same rows.</summary>
        public string RowFilter => _grid.RowFilter;

        /// <summary>Applies a filter from <see cref="RowFilter"/> - ignored if it doesn't apply to the grid's columns.</summary>
        public void ApplyRowFilter(string filter)
        {
            if (!string.IsNullOrEmpty(filter)) _grid.TrySetFilter(filter);
        }

        /// <summary>
        /// Records the file the grid was read from, and when it was last changed, so Reload and Watch can tell a change
        /// since.  Taken before the file is read - a change while it's read is then caught by the next check.
        /// </summary>
        public void SetSource(string path, (DateTime WriteUtc, long Length) stamp)
        {
            _watch.Start(path, stamp);
            _reloadButton.Enabled = true;
            _watchButton.Enabled = true;
            _fileTextButton.Enabled = true;
            _lastRead = DateTime.Now;
            ShowWatchStatus();
        }

        /// <summary>Says on the status bar whether the file is being watched, and when it was last read.</summary>
        private void ShowWatchStatus()
        {
            _watchStatus.Text = WatchForNewData && SourcePath is not null
                ? $"Watching {Path.GetFileName(SourcePath)} - last read {_lastRead:HH:mm:ss}"
                : string.Empty;
        }

        /// <summary>The file's last write time and length, or null when it isn't there - see <see cref="FileWatch.Stamp"/>.</summary>
        public static (DateTime WriteUtc, long Length)? FileStamp(string path) => FileWatch.Stamp(path);

        /// <summary>
        /// Brings the grid up to date with a fresh read of the same file, by adding the rows the file has gained since -
        /// the rows already shown are left as they are, so the scroll position, sort, filter and column layout all stay.
        /// Returns false, changing nothing, when the read isn't the file grown; the grid is then replaced instead.
        /// See <see cref="JsonRecordsGrowth.TryAppend"/>.
        /// </summary>
        public bool TryAppendRows(DataTable fresh)
        {
            if (!JsonRecordsGrowth.TryAppend(_table, fresh, () => JsonRecordsGrowth.AddUnreadableGridColumn(_grid), out _, out _)) return false;

            UpdateReadState();
            ShowRowCount();
            return true;
        }

        /// <summary>
        /// Set by the host while the file is being read again.  One read at a time, and the watch waits for it: a change
        /// made during the read is then seen once it's done.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ReloadInProgress { get; set; }

        private void CheckForNewData()
        {
            if (!ReloadInProgress) ReloadRequested?.Invoke(this, false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _watch.Dispose();
            base.Dispose(disposing);
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

                // A number format the table asks for - the statistics tables' counts, say, with thousands separators.
                if (_table.Columns[column.DataPropertyName]?.ExtendedProperties["Format"] is string format)
                {
                    column.DefaultCellStyle.Format = format;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                // The column's SQL type, where the SSMS extension recorded it - the grid only knows the .NET one.
                var sqlType = SqlType(column);
                var isJson = sqlType?.Equals("json", StringComparison.OrdinalIgnoreCase) == true ||
                             (!IsLinkType(sqlType) && _table.Columns[column.DataPropertyName] is { } dataColumn &&
                              NestedJson.IsJsonColumn(_table, dataColumn));
                if (isJson) _jsonColumns.Add(column.DataPropertyName);
                if (sqlType is null && !isJson) continue;

                var configured = isJson || IsLinkType(sqlType)
                    ? ReplaceColumn(column, new DataGridViewLinkColumn { TrackVisitedState = false })
                    : column;
                configured.ToolTipText = $"{configured.HeaderText} ({sqlType ?? "JSON"})" +
                                         (isJson ? " - Ctrl+click a value to view it as a grid" : string.Empty);
            }

            // Link columns are added after the grid was themed, so they need their link colours now.
            _grid.ApplyTheme();

            // The bars the table starts with, unless others - or none - were chosen the last time a result set of this
            // shape was open.  And from here on, the ones chosen now: an empty set too, where removing every bar would
            // otherwise bring the table's own back next time.
            var defaults = DefaultDataBars(_table);
            var remember = _table.ExtendedProperties[RememberDataBarsProperty] is not false;
            _grid.SetDataBars(remember && GridDataBarMemory.TryRecall(_table, out var remembered) ? remembered : defaults);
            // Reset Data Bars puts them back - and forgets what was chosen, so the next of this shape starts with them too.
            _grid.DefaultDataBars = defaults;
            if (remember)
            {
                _grid.DataBarChanged += (_, _) => GridDataBarMemory.Remember(_table, _grid.DataBars, keepEmpty: defaults.Count > 0);
                _grid.DataBarsReset += (_, _) => GridDataBarMemory.Forget(_table);
            }
        }

        /// <summary>
        /// A column extended property holding the <see cref="DataBarSettings"/> it starts with - the statistics tables'
        /// time and I/O, say.  The user can change or remove them as any other.
        /// </summary>
        public const string DataBarProperty = "DataBar";

        /// <summary>
        /// A table extended property: false where bars chosen on it shouldn't come back for the next table of the same
        /// columns - a comparison, whose columns are numbered for runs that won't be the same next time.
        /// </summary>
        public const string RememberDataBarsProperty = "RememberDataBars";

        /// <summary>The bars <paramref name="table"/>'s columns start with - see <see cref="DataBarProperty"/>.</summary>
        public static Dictionary<string, DataBarSettings> DefaultDataBars(DataTable table) =>
            table.Columns.Cast<DataColumn>()
                .Where(c => c.ExtendedProperties[DataBarProperty] is DataBarSettings)
                .ToDictionary(c => c.ColumnName, c => ((DataBarSettings)c.ExtendedProperties[DataBarProperty]).Clone(),
                    StringComparer.OrdinalIgnoreCase);

        private string SqlType(DataGridViewColumn column) =>
            _table.Columns[column.DataPropertyName]?.ExtendedProperties["SqlType"] as string;

        /// <summary>
        /// The types SSMS shows as a link that opens in a window of its own: QEResultSet.IsXMLColumn and
        /// IsJsonColumn compare the server's type name the same way.
        /// </summary>
        private static bool IsLinkType(string sqlType) =>
            sqlType is not null &&
            (sqlType.Equals("xml", StringComparison.OrdinalIgnoreCase) ||
             sqlType.Equals("json", StringComparison.OrdinalIgnoreCase));

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
            // Only for a grid read from a file: a log still being written is read again to get what's new.
            _reloadButton = new ToolStripButton(null, Resources.Refresh_16x, (_, _) => ReloadRequested?.Invoke(this, true))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false,
                ToolTipText = "Reload - read the file again, to show what has been added to it"
            };
            _watchButton = new ToolStripButton(Resources.Watch_16x)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false,
                CheckOnClick = true,
                ToolTipText = "Watch - read the file again whenever it changes"
            };
            _watchButton.CheckedChanged += (_, _) =>
            {
                _watch.Enabled = _watchButton.Checked;
                ShowWatchStatus();
            };
            toolbar.Items.Add(_reloadButton);
            toolbar.Items.Add(_watchButton);
            // The file itself, as text - also only for a grid read from one.
            _fileTextButton = new ToolStripButton(null, Resources.Code_16x, (_, _) => FileTextViewer.Show(FindForm(), SourcePath))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false,
                ToolTipText = "View File Text - the file as it is, in the code viewer"
            };
            toolbar.Items.Add(_fileTextButton);
            toolbar.Items.Add(new ToolStripSeparator());
            // The grid's own copy, export, transpose, Group By and filter actions - the same as its context menus.
            var items = _grid.CreateToolbarItems();
            _exportMenu = items.OfType<ToolStripDropDownButton>().FirstOrDefault(i => i.Text == "Export");
            toolbar.Items.AddRange(items);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(BuildSettingsMenu());

            return toolbar;
        }

        // The toolbar's Export drop-down, for a host to add its own ways of saving to.
        private ToolStripDropDownButton _exportMenu;

        /// <summary>
        /// Adds <paramref name="item"/> to the toolbar's Export menu, after a separator - e.g. saving the grid with the
        /// query's messages, where it's a query's results.
        /// </summary>
        internal void AddExportItem(ToolStripItem item)
        {
            if (_exportMenu == null) return;
            _exportMenu.DropDownItems.Add(new ToolStripSeparator());
            _exportMenu.DropDownItems.Add(item);
        }

        /// <summary>The text of the Export menu's items, separators aside.</summary>
        public string[] ExportItemNames => _exportMenu?.DropDownItems.Cast<ToolStripItem>()
            .Where(i => i is not ToolStripSeparator).Select(i => i.Text).ToArray() ?? [];

        /// <summary>
        /// The bottom bar: rows and the file's watch state, and what the read says about records still being written.
        /// Its own line, so messages coming and going don't move the grid.
        /// </summary>
        private ToolStrip BuildStatusBar()
        {
            var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Bottom, RenderMode = ToolStripRenderMode.System };
            bar.Items.Add(_rowCount);
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(_watchStatus);
            bar.Items.Add(_pendingStatus);
            return bar;
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
                ? $"{shown:N0} {(shown == 1 ? "row" : "rows")}"
                : $"{shown:N0} of {_table.Rows.Count:N0} rows";
            // So a hidden column isn't taken for one that isn't there
            var hidden = _grid.Columns.Cast<DataGridViewColumn>().Count(c => !c.Visible);
            var hiddenText = $"{hidden:N0} {(hidden == 1 ? "column" : "columns")} hidden";
            _rowCount.ToolTipText = hidden == 0 ? null : $"{hiddenText} - right-click a column header, Columns, to show them";
            if (hidden > 0) _rowCount.Text += $", {hiddenText}";
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
                var isJson = _jsonColumns.Contains(column.DataPropertyName);
                if (!isJson && TryOpenInViewer(text, e.RowIndex)) return;

                // Ctrl+click opens it as a grid instead - View as Grid without the menu.
                if (isJson && (ModifierKeys & Keys.Control) == Keys.Control) OpenJsonAsGrid(e.RowIndex, e.ColumnIndex);
                else if (isJson) ShowJson(text, column, e.RowIndex);
                // The code editor indents XML itself.
                else CommonShared.ShowCodeViewer(text, CellCaption(column, e.RowIndex), CodeEditor.CodeEditorModes.XML);
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

        /// <summary>Which cell something was opened from: its column and row.</summary>
        private static string CellCaption(DataGridViewColumn column, int rowIndex) => $"{column.HeaderText} - {RowCaption(rowIndex)}";

        /// <summary>JSON in the code viewer, indented: the editor shows JSON as it is.</summary>
        private static void ShowJson(string json, DataGridViewColumn column, int rowIndex) =>
            CommonShared.ShowCodeViewer(FormatJson(json), CellCaption(column, rowIndex), CodeEditor.CodeEditorModes.Json);

        /// <summary>
        /// View JSON and View as Grid at the top of a cell's menu, where the cell holds a JSON object or array - in a JSON
        /// link column or not, as a column can mix JSON with other values.
        /// </summary>
        private void AddJsonMenuItems()
        {
            var cell = (Row: -1, Column: -1);
            var viewJson = new ToolStripMenuItem("View JSON", Resources.Code_16x, (_, _) =>
            {
                if (JsonCellText(cell.Row, cell.Column) is { } json) ShowJson(json, _grid.Columns[cell.Column], cell.Row);
            })
            {
                ToolTipText = "Show the cell's JSON in the code viewer"
            };
            var viewAsGrid = new ToolStripMenuItem("View as Grid", Resources.DataTable_16x, (_, _) => OpenJsonAsGrid(cell.Row, cell.Column))
            {
                ToolTipText = "Show the cell's JSON as a grid of its own, on a new tab",
                // A hint only - Ctrl+click on a JSON link does it, which a menu shortcut can't express.
                ShortcutKeyDisplayString = "Ctrl+Click"
            };
            var separator = new ToolStripSeparator();

            var menu = _grid.CellContextMenu.Items;
            menu.Insert(0, separator);
            menu.Insert(0, viewAsGrid);
            menu.Insert(0, viewJson);

            _grid.CellContextMenuOpening += (_, e) =>
            {
                cell = (e.RowIndex, e.ColumnIndex);
                var isJson = JsonCellText(e.RowIndex, e.ColumnIndex) is not null;
                viewJson.Visible = viewAsGrid.Visible = separator.Visible = isJson;
            };
        }

        /// <summary>The cell's text, if it looks like a JSON object or array, else null.</summary>
        private string JsonCellText(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || columnIndex < 0 || rowIndex >= _grid.Rows.Count || columnIndex >= _grid.Columns.Count) return null;
            return _grid.Rows[rowIndex].Cells[columnIndex].Value is string text && NestedJson.LooksLikeJson(text) ? text : null;
        }

        /// <summary>
        /// Opens a cell's JSON as a grid on a new tab of the window - nested records, filtered and sorted as any other
        /// grid, whose own JSON can be opened the same way.
        /// </summary>
        private void OpenJsonAsGrid(int rowIndex, int columnIndex)
        {
            if (JsonCellText(rowIndex, columnIndex) is not { } json) return;

            var column = _grid.Columns[columnIndex];
            var title = $"{Title} - {column.HeaderText} ({RowCaption(rowIndex)})";
            try
            {
                var dataSet = NestedJson.ToDataSet(json);
                if (dataSet.Tables.Count == 0)
                {
                    MessageBox.Show(FindForm(), "There is nothing in the JSON to show as a grid.", "View as Grid",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ViewerForm.OpenGrid(dataSet, title, FindForm());
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error opening cell", text: "The cell's JSON could not be shown as a grid.");
            }
        }

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
