using DBADash;
using DBADash.Messaging;
using DBADashGUI.CommunityTools;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;
using DataTable = System.Data.DataTable;

namespace DBADashGUI.CustomReports
{
    public partial class CustomReportView : UserControl, ISetContext, IRefreshData, ISetStatus
    {
        public event EventHandler ReportNameChanged;

        protected DataSet reportDS;
        private bool suppressCboResultsIndexChanged;
        public static DataGridView Grid => new(); // dgv;
        public ToolStripStatusLabel StatusLabel => lblDescription;
        private DBADashContext context;
        public CustomReport Report { get; set; }
        private List<CustomSqlParameter> customParams = new();
        private CancellationTokenSource cancellationTokenSource;
        private Guid CurrentMessageGroup;
        public EventHandler PostGridRefresh;

        private bool AutoLoad => Report is not DirectExecutionReport;

        public CustomReportView()
        {
            Grids = new();
            InitializeComponent();
            ShowParamPrompt(false);
            this.ApplyTheme();
            scriptDataTablesToolStripMenuItem.Click += (_, _) => ScriptDataTables(false);
            scriptGridsToolStripMenuItem.Click += (_, _) => ScriptDataTables(true);
        }

        private void ScriptDataTables(bool fromGrid)
        {
            try
            {
                var i = 0;
                StringBuilder sb = new();
                foreach (var grid in Grids)
                {
                    if (i > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("/*********************************************************");
                        sb.AppendLine($" Result Set {i + 1}");
                        sb.AppendLine("*********************************************************/");
                        sb.AppendLine();
                    }
                    var tableName = $"#DBADashGrid{i + 1}";
                    sb.Append(grid.ScriptTable(fromGrid, i == 0, tableName));
                    sb.AppendLine();
                    i += 1;
                }

                var frm = new CodeViewer() { Code = sb.ToString(), Language = CodeEditor.CodeEditorModes.SQL };
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error scripting tables: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetCellHighlightingRules(DBADashDataGridView dgv)
        {
            if (dgv == null) return;
            try
            {
                var customReportResult = Report.CustomReportResults[dgv.ResultSetID];
                var columnName = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
                customReportResult.CellHighlightingRules.TryGetValue(
                    dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName,
                    out var ruleSet);

                var frm = new CellHighlightingRulesConfig()
                {
                    ColumnList = dgv.Columns,
                    CellHighlightingRules = new KeyValuePair<string, CellHighlightingRuleSet>(columnName,
                        ruleSet.DeepCopy() ?? new CellHighlightingRuleSet() { TargetColumn = columnName }),
                    CellValue = dgv.ClickedRowIndex >= 0
                        ? dgv.Rows[dgv.ClickedRowIndex].Cells[dgv.ClickedColumnIndex].Value
                        : null,
                    CellValueIsNull = dgv.ClickedRowIndex >= 0 &&
                                      dgv.Rows[dgv.ClickedRowIndex].Cells[dgv.ClickedColumnIndex].Value
                                          .DBNullToNull() == null
                };

                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;
                Report.CustomReportResults[dgv.ResultSetID].CellHighlightingRules.Remove(columnName);
                if (frm.CellHighlightingRules.Value is { HasRules: true })
                {
                    Report.CustomReportResults[dgv.ResultSetID].CellHighlightingRules
                        .Add(columnName, frm.CellHighlightingRules.Value);
                }

                Report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting highlighting rules: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AddLink_Click(DBADashDataGridView dgv)
        {
            var tableIndex = dgv.ResultSetID;
            try
            {
                var customReportResult = Report.CustomReportResults[tableIndex];
                var col = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
                var linkColumnInfo = customReportResult.LinkColumns?.TryGetValue(col, out var column) is true
                    ? column
                    : null;
                var colList = dgv.Columns.Cast<DataGridViewColumn>().Select(c => c.DataPropertyName).ToList();
                var frm = new LinkColumnTypeSelector()
                { LinkColumnInfo = linkColumnInfo, ColumnList = colList, Context = context, LinkColumn = col };
                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;
                customReportResult.LinkColumns ??= new();
                if (frm.LinkColumnInfo == null)
                {
                    customReportResult.LinkColumns.Remove(col);
                }
                else if (customReportResult.LinkColumns.ContainsKey(col))
                {
                    customReportResult.LinkColumns[col] = frm.LinkColumnInfo;
                }
                else
                {
                    customReportResult.LinkColumns.Add(col, frm.LinkColumnInfo);
                }

                previousSchema = null; // Force grids to be re-generated
                dgv.Columns.Clear();
                Report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding link: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetFormatStringMenuItem_Click(DBADashDataGridView dgv)
        {
            var formatString = dgv.Columns[dgv.ClickedColumnIndex].DefaultCellStyle.Format;
            var key = dgv.Columns[dgv.ClickedColumnIndex].Name;
            if (CommonShared.ShowInputDialog(ref formatString, "Enter format string (e.g. N1, P1, yyyy-MM-dd)") !=
                DialogResult.OK) return;
            dgv.Columns[dgv.ClickedColumnIndex].DefaultCellStyle.Format = formatString;
            Report.CustomReportResults[dgv.ResultSetID].CellFormatString.Remove(key);

            if (!string.IsNullOrEmpty(formatString))
            {
                Report.CustomReportResults[dgv.ResultSetID].CellFormatString.Add(key, formatString);
            }

            Report.Update();
        }

        private void ConvertLocalMenuItem_Click(object sender, DBADashDataGridView dgv)
        {
            var convertLocalMenuItem = (ToolStripMenuItem)sender;
            if (dgv.ClickedColumnIndex < 0) return;
            var name = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
            switch (convertLocalMenuItem.Checked)
            {
                case true when Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(name):
                    if (MessageBox.Show("Convert column from UTC to local time?", "Convert?", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.No) return;
                    Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Remove(name);
                    break;

                case false when !Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(name):
                    if (MessageBox.Show("Remove UTC to local time zone conversion for this column?", "Convert?",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                    Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Add(name);
                    break;
            }

            try
            {
                Report.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving time zone preference: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            RefreshData();
        }

        private void RenameColumnMenuItem_Click(DBADashDataGridView dgv)
        {
            if (dgv.ClickedColumnIndex < 0) return;
            // Show a dialog for renaming
            var newName = dgv.Columns[dgv.ClickedColumnIndex].HeaderText;
            if (CommonShared.ShowInputDialog(ref newName, "Enter new column name:") != DialogResult.OK) return;
            if (string.IsNullOrEmpty(newName))
            {
                newName = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
            }

            dgv.Columns[dgv.ClickedColumnIndex].HeaderText = newName;
            try
            {
                var alias = dgv.Columns.Cast<DataGridViewColumn>()
                    .Where(column => column.DataPropertyName != column.HeaderText)
                    .ToDictionary(column => column.DataPropertyName, column => column.HeaderText);
                Report.CustomReportResults[dgv.ResultSetID].ColumnAlias = alias;
                Report.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving alias: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Param prompt is shown instead of grid if we have a 201 error indicating that parameter values are required.
        /// </summary>
        /// <param name="show">True = parameter prompt is visible.  False = Grid is visible</param>
        private void ShowParamPrompt(bool show)
        {
            splitContainer1.Panel2Collapsed = !show;
            splitContainer1.Panel1Collapsed = show;
        }

        /// <summary>
        /// Convert DateTime columns to local timezone unless user has specified that conversion should be excluded for the column
        /// </summary>
        /// <param name="ds"></param>
        private void ConvertDateTimeColsToLocalTimeZone(DataSet ds)
        {
            for (var i = 0; i < ds.Tables.Count; i++)
            {
                if (!Report.CustomReportResults.TryGetValue(i, out var result)) continue;
                var dt = ds.Tables[i];
                var convertCols = dt.Columns.Cast<DataColumn>()
                    .Where(column =>
                        column.DataType == typeof(DateTime) &&
                        !result.DoNotConvertToLocalTimeZone.Contains(column.ColumnName))
                    .Select(column => column.ColumnName).ToList();
                if (convertCols.Count > 0)
                {
                    DateHelper.ConvertUTCToAppTimeZone(ref dt, convertCols);
                }

                i += 1;
            }
        }

        /// <summary>
        /// Results combobox allows user to select which result set to display if the report returns multiple result sets
        /// </summary>
        protected void LoadResultsCombo()
        {
            suppressCboResultsIndexChanged = true;
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                if (Report.CustomReportResults.TryGetValue(i, out var result))
                {
                    result.ResultName ??= "Result" + (i + 1); // ensure we have a name
                }
                else
                {
                    Report.CustomReportResults.Add(i, new CustomReportResult() { ResultName = "Result" + (i + 1) });
                }
            }

            cboResults.Items.Clear();
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                cboResults.Items.Add(Report.CustomReportResults[i].ResultName);
            }

            cboResults.Items.Add("ALL");

            cboResults.Visible = cboResults.Items.Count > 2;
            lblSelectResults.Visible = cboResults.Items.Count > 2;
            cboResults.SelectedIndex = cboResults.Items.Count - 1;
            suppressCboResultsIndexChanged = false;
        }

        private bool ShowAllResults => cboResults.SelectedIndex == cboResults.Items.Count - 1;

        public void RefreshData()
        {
            if (!Report.HasAccess())
            {
                MessageBox.Show("You do not have access to this report", "Access Denied", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (Report is DirectExecutionReport)
            {
                RefreshDataMessage();
            }
            else
            {
                StartTimer();
                cancellationTokenSource = new CancellationTokenSource();
                IsMessageInProgress = true;
                Task.Run(() => { _ = RefreshDataRepository(cancellationTokenSource.Token); });
            }
        }

        private void StartTimer()
        {
            ResetTimer();
            timer1.Start();
        }

        private void StopTimer()
        {
            timer1.Stop();
            UpdateTimer();
        }

        private void ResetTimer()
        {
            timer1.Stop();
            TimerStart = DateTime.Now;
            lblTimer.Text = "00:00:00";
        }

        public async Task RefreshDataRepository(CancellationToken token)
        {
            try
            {
                SetStatus("Running report", string.Empty, DashColors.Information);
                reportDS = await GetReportDataAsync(token);
                this.Invoke(() =>
                {
                    ShowParamPrompt(false); // Show grid
                    LoadResultsCombo();
                    if (reportDS.Tables.Count > 0)
                    {
                        ShowTable();
                    }
                    else
                    {
                        MessageBox.Show("Report didn't return a result set", "Warning", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                });
                SetStatus("Completed", "Success", DashColors.Success);
            }
            catch (SqlException ex) when (ex.Number == 201) // Parameter required
            {
                ShowParamPrompt(
                    true); // Show parameter prompt instead of grid as we have required parameters that were not supplied
            }
            catch (Exception ex)
            {
                SetStatus("Error running report:" + ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                IsMessageInProgress = false;
                StopTimer();
            }
        }

        public void ShowData(DataSet ds)
        {
            reportDS = ds;
            ShowTable();
            LoadResultsCombo();
        }

        private bool IsMessageInProgress
        {
            get => tsCancel.Enabled;
            set
            {
                tsCancel.Enabled = value;
                tsRefresh.Enabled = !tsCancel.Enabled;
                tsExecute.Enabled = !tsCancel.Enabled;
            }
        }

        public void RefreshDataMessage()
        {
            if (IsMessageInProgress)
            {
                SetStatus("A message is already in progress.  Please wait for the current message to complete before running another.", "Warning", DashColors.Warning);
                return;
            }
            var msg = new ProcedureExecutionMessage
            {
                CommandName = Enum.Parse<ProcedureExecutionMessage.CommandNames>(Report.ProcedureName),
                Parameters = customParams,
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = Config.DefaultCommandTimeout
            };

            SetStatus("Message sent", "Running", DashColors.Information);
            IsMessageInProgress = true;
            CurrentMessageGroup = Guid.NewGuid();
            StartTimer();
            Task.Run(() => MessagingHelper.SendMessageAndProcessReply(msg, context, SetStatus, ProcessCompletedMessage,
                CurrentMessageGroup));
        }

        private Task ProcessCompletedMessage(ResponseMessage reply, Guid messageGroup)
        {
            if (CurrentMessageGroup != messageGroup) // Context has changed.  Ignore
            {
                return Task.CompletedTask;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Success && reply.Data is { Tables.Count: > 0 })
            {
                StopTimer();
                IsMessageInProgress = false;
                try
                {
                    reportDS = reply.Data;
                    this.Invoke(LoadResultsCombo);

                    if (reportDS.Tables.Count > 0)
                    {
                        this.Invoke(ShowTable);
                        SetStatus("Completed", "Success", DashColors.Success);
                    }
                    else
                    {
                        SetStatus("Report didn't return a result set", "Warning", Color.Orange);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error running report:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Failure)
            {
                StopTimer();
                SetStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                IsMessageInProgress = false;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                StopTimer();
                SetStatus(reply.Message, null, DashColors.Success);
                IsMessageInProgress = false;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Progress)
            {
                SetStatus(reply.Message, null, DashColors.Information);
            }

            return Task.CompletedTask;
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            this.Invoke(() =>
            {
                lblDescription.Text = message;
                lblDescription.ForeColor = color;
                lblDescription.ToolTipText = tooltip;
                lblDescription.Visible = true;
            });
        }

        public List<DBADashDataGridView> Grids { get; }
        private string previousSchema;

        private void ClearResults()
        {
            splitContainer1.Panel1.Controls.Clear();
            Grids.Clear();
            previousSchema = string.Empty;
            UpdateClearFilter();
        }

        private void LoadResultsIntoExistingGrids()
        {
            foreach (var grid in Grids)
            {
                grid.AutoGenerateColumns = false;
                var table = reportDS.Tables[grid.ResultSetID];
                grid.DataSource = new DataView(table, grid.RowFilter, grid.SortString, DataViewRowState.CurrentRows);
            }
        }

        protected void ShowTable()
        {
            if (reportDS.Tables.Count == 0) return;
            var currentSchema = reportDS.GetXmlSchema();
            if (currentSchema == previousSchema)
            {
                LoadResultsIntoExistingGrids();
                return;
            }

            const int minDataGridViewHeight = 100; // Minimum height for a DataGridView
            var maxDataGridViewHeight = Math.Max(300, this.Height / Math.Min(3, reportDS.Tables.Count)); // Allow table to take up to 1/3 (or half if there are 2 tables) of the form height with minimum size of 300.
            var parentPanel = splitContainer1.Panel1;
            ClearResults();
            List<Panel> panels = new();
            var i = ShowAllResults ? 0 : cboResults.SelectedIndex;
            var tables = ShowAllResults ? reportDS.Tables.Cast<DataTable>().ToArray() : new[] { reportDS.Tables[cboResults.SelectedIndex] };
            foreach (var table in tables)
            {
                var pnl = new Panel()
                {
                    Dock = tables.Length == 1 ? DockStyle.Fill : DockStyle.Top, // Dock to the top of the panel
                    Tag = i,
                    Padding = new Padding(0, 0, 0, 5),
                };
                var dgv = new DBADashDataGridView()
                {
                    DataSource = new DataView(table),
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 0, 0, 30),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToOrderColumns = true,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    RowHeadersVisible = false,
                    ResultSetID = i,
                    ResultSetName = Report.CustomReportResults[i].ResultName,
                };
                dgv.RowsAdded += Dgv_RowsAdded;
                dgv.CellContentClick += Dgv_CellContentClick;
                if (tables.Length > 1)
                {
                    pnl.MouseDown += DataGridPanel_MouseDown;
                    pnl.MouseMove += DataGridPanel_MouseMove;
                    pnl.MouseUp += DataGridPanel_MouseUp;
                    pnl.MouseLeave += DataGridPanel_MouseLeave;
                }
                var customReportResults = Report.CustomReportResults[i];
                dgv.AddColumns(table, customReportResults);
                dgv.DataBindingComplete += Dgv_DataBindingComplete;

                if (Report.CanEditReport) // Add context menu items for editing
                {
                    AddContextMenuItemsForEditing(dgv);
                }
                dgv.ApplyTheme();
                // Adjust height based on row count, with a maximum limit.  Allow for column height being adjusted from default.
                var rowsHeight = ((dgv.RowTemplate.Height) * table.Rows.Count) + dgv.Padding.Top + dgv.Padding.Bottom + (dgv.ColumnHeadersHeight * 3) + pnl.Padding.Top + pnl.Padding.Bottom;
                pnl.Height = Math.Max(Math.Min(rowsHeight, maxDataGridViewHeight), minDataGridViewHeight);

                Grids.Add(dgv);
                panels.Add(pnl);

                if (tables.Length > 1)
                {
                    var ts = new ToolStrip() { Dock = DockStyle.Top };
                    ts.Items.Add(new ToolStripButton("+", null, Maximize_Click)
                    {
                        Alignment = ToolStripItemAlignment.Right,
                        DisplayStyle = ToolStripItemDisplayStyle.Text,
                        Tag = i
                    });
                    ts.Items.Add(new ToolStripLabel(Report.CustomReportResults[i].ResultName)
                    {
                        Alignment = ToolStripItemAlignment.Right,
                        Font = new Font(this.Font, FontStyle.Bold)
                    });
                    ts.Items.Add(new ToolStripButton("Columns", Properties.Resources.Column_16x, Columns_Click)
                    {
                        Alignment = ToolStripItemAlignment.Left,
                        DisplayStyle = ToolStripItemDisplayStyle.Image,
                        Tag = i
                    });
                    ts.Items.Add(new ToolStripButton("Copy", Properties.Resources.ASX_Copy_blue_16x, Copy_Click)
                    {
                        Alignment = ToolStripItemAlignment.Left,
                        DisplayStyle = ToolStripItemDisplayStyle.Image,
                        Tag = i
                    });
                    var clearFilterMenuItem =
                        new ToolStripButton("Clear Filter", Properties.Resources.Eraser_16x, ClearFilter_Click)
                        {
                            Alignment = ToolStripItemAlignment.Left,
                            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                            Enabled = false,
                            Tag = i
                        };
                    ts.Items.Add(clearFilterMenuItem);
                    if (Report.CanEditReport)
                        ts.Items.Add(
                            new ToolStripButton("Rename", Properties.Resources.Rename_16x, RenameResultSet_Click)
                            {
                                Alignment = ToolStripItemAlignment.Right,
                                DisplayStyle = ToolStripItemDisplayStyle.Image,
                                Tag = i
                            });
                    ts.ApplyTheme();
                    pnl.Controls.AddRange(new Control[] { dgv, ts });
                    dgv.GridFilterChanged += (sender, e) =>
                    {
                        clearFilterMenuItem.Enabled = !string.IsNullOrEmpty(dgv.RowFilter);
                        clearFilterMenuItem.Font = new Font(clearFilterMenuItem.Font, clearFilterMenuItem.Enabled ? FontStyle.Bold : FontStyle.Regular);
                        UpdateClearFilter();
                    };
                }
                else
                {
                    pnl.Controls.Add(dgv);
                    dgv.GridFilterChanged += (sender, e) =>
                    {
                        UpdateClearFilter();
                    };
                }

                i += 1;
            }
            parentPanel.Controls.AddRange(panels.OrderByDescending(p => (int)p.Tag!).Cast<Control>().ToArray());
            OnPostGridRefresh();
            previousSchema = currentSchema;
        }

        private void UpdateClearFilter()
        {
            tsClearFilter.Enabled = Grids.Any(g => (g.DataSource as DataView)?.RowFilter != string.Empty);
            tsClearFilter.Font = new Font(tsClearFilter.Font, tsClearFilter.Enabled ? FontStyle.Bold : FontStyle.Regular);
        }

        private void ClearFilter_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            dgv.SetFilter(string.Empty);
        }

        private void AddContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            AddCellContextMenuItemsForEditing(dgv);
            AddColumnContextMenuItemsForEditing(dgv);
        }

        private void AddCellContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            var editReport = new ToolStripMenuItem("Edit Report", Properties.Resources.VBReport_16x);
            var highlight = new ToolStripMenuItem("Highlight Cell Value", Properties.Resources.HighlightHS);
            highlight.Click += (sender, e) => SetCellHighlightingRules(dgv);
            editReport.DropDownItems.Add(highlight);
            dgv.CellContextMenu.Items.Add(editReport);
        }

        private void AddColumnContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            var editReport = new ToolStripMenuItem("Edit Report", Properties.Resources.VBReport_16x) { Name = "EditReport" };

            var renameColumnMenuItem = new ToolStripMenuItem("Rename Column", Properties.Resources.Rename_16x);
            var setFormatStringMenuItem =
                new ToolStripMenuItem("Set Format String", Properties.Resources.Percentage_16x);
            var addLink = new ToolStripMenuItem("Add Link", Properties.Resources.WebURL_16x);
            var rules = new ToolStripMenuItem("Highlighting Rules", Properties.Resources.HighlightHS);
            var convertLocalMenuItem = new ToolStripMenuItem("Convert to local timezone") { Checked = true, CheckOnClick = true, Name = "ConvertLocal" };
            renameColumnMenuItem.Click += (sender, e) => RenameColumnMenuItem_Click(dgv);
            convertLocalMenuItem.Click += (sender, e) => ConvertLocalMenuItem_Click(sender, dgv);
            setFormatStringMenuItem.Click += (sender, e) => SetFormatStringMenuItem_Click(dgv);
            addLink.Click += (sender, e) => AddLink_Click(dgv);
            rules.Click += (sender, e) => SetCellHighlightingRules(dgv);

            editReport.DropDownItems.AddRange(new ToolStripItem[]
            {
                renameColumnMenuItem,
                convertLocalMenuItem,
                setFormatStringMenuItem,
                addLink,
                rules
            });

            dgv.ColumnContextMenu.Items.Add(editReport);
            dgv.ColumnContextMenuOpening += (sender, args) =>
            {
                convertLocalMenuItem.Checked = dgv.Columns[args.ColumnIndex].ValueType == typeof(DateTime) &&
                                       !Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(dgv.Columns[args.ColumnIndex].DataPropertyName);
                convertLocalMenuItem.Visible = dgv.Columns[args.ColumnIndex].ValueType == typeof(DateTime);
            };
        }

        private void Maximize_Click(object sender, EventArgs e)
        {
            foreach (var panel in splitContainer1.Panel1.Controls.OfType<Panel>())
            {
                if (panel.Controls[1] is ToolStrip ts && ts.Items[0] is ToolStripButton tsb && tsb == sender)
                {
                    panel.Dock = DockStyle.Fill;
                    ts.Items[0].Text = "-";
                    ts.Items[0].Click -= Maximize_Click;
                    ts.Items[0].Click += Minimize_Click;
                }
                else
                {
                    panel.Visible = false;
                }
            }
        }

        private void Minimize_Click(object sender, EventArgs e)
        {
            foreach (var panel in splitContainer1.Panel1.Controls.OfType<Panel>())
            {
                if (panel.Controls[1] is ToolStrip ts && ts.Items[0] is ToolStripButton tsb && tsb == sender)
                {
                    panel.Dock = DockStyle.Top;
                    ts.Items[0].Text = "+";
                    ts.Items[0].Click -= Minimize_Click;
                    ts.Items[0].Click += Maximize_Click;
                }
                else
                {
                    panel.Visible = true;
                }
            }
        }

        protected virtual void OnPostGridRefresh()
        {
            UpdateClearFilter();
            PostGridRefresh?.Invoke(this, EventArgs.Empty);
        }

        private void RenameResultSet_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            if (dgv == null) return;
            var name = dgv.ResultSetName;
            if (CommonShared.ShowInputDialog(ref name, "Enter name") != DialogResult.OK) return;
            Report.CustomReportResults[dgv.ResultSetID].ResultName = name;
            suppressCboResultsIndexChanged = true;
            cboResults.Items[dgv.ResultSetID] = name;
            dgv.ResultSetName = name;
            suppressCboResultsIndexChanged = false;
            Report.Update();
            ShowTable();
        }

        private DBADashDataGridView GetAssociatedGrid(object sender)
        {
            switch (Grids.Count)
            {
                case 0:
                    return null;

                case 1:
                    return Grids[0];
            }

            if (sender is not ToolStripButton tsb || tsb.Tag == null) return Grids[0];
            var resultSetID = (int)tsb.Tag;
            return Grids.FirstOrDefault(d => d.ResultSetID == resultSetID);
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void Columns_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            dgv?.PromptColumnSelection();
        }

        #region DataGridPanelResizing

        private bool isResizing = false;
        private int initialResizeY;
        private Control resizingControl;

        private void DataGridPanel_MouseLeave(object sender, EventArgs e)
        {
            if (!isResizing) // Only reset the cursor if we're not currently resizing
            {
                ((Control)sender).Cursor = Cursors.Default;
            }
        }

        private void DataGridPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Start resizing if the mouse is near the edge of the panel
            if (e.Y < ((Control)sender).Height - 10) return;
            isResizing = true;
            initialResizeY = e.Y;
            resizingControl = (Control)sender;
            resizingControl.Cursor = Cursors.SizeNS; // Change cursor to resizing cursor
        }

        private void DataGridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && resizingControl != null)
            {
                // Calculate the new height
                var newHeight = resizingControl.Height + (e.Y - initialResizeY);
                if (newHeight <= 100) return; // Minimum height constraint
                resizingControl.Height = newHeight;
                initialResizeY = e.Y;
            }
            else if (e.Y >= ((Control)sender).Height - 10)
            {
                // Change cursor to indicate resizable area
                ((Control)sender).Cursor = Cursors.SizeNS;
            }
            else
            {
                // Change cursor back to default
                ((Control)sender).Cursor = Cursors.Default;
            }
        }

        private void DataGridPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isResizing) return;
            isResizing = false;
            resizingControl.Cursor = Cursors.Default; // Reset cursor
            resizingControl = null;
        }

        #endregion DataGridPanelResizing

        private void SetColumnLayout(DBADashDataGridView dgv)
        {
            if (!Report.CustomReportResults.TryGetValue(dgv.ResultSetID, out var value)) return;
            const int maxWidth = 400;
            if (value.ColumnLayout.Count > 0)
            {
                dgv.LoadColumnLayout(value.ColumnLayout);
            }
            else
            {
                dgv.AutoResizeColumns();
                // Ensure column size is not excessive
                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    if (column.Width <= maxWidth) continue;
                    column.Width = maxWidth;
                }
            }
        }

        private void Dgv_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not DBADashDataGridView dgv) return;
            SetColumnLayout(dgv);
            dgv.DataBindingComplete -= Dgv_DataBindingComplete;
        }

        /// <summary>
        /// Run the query to get the data for the user custom report
        /// </summary>
        /// <returns></returns>
        protected async Task<DataSet> GetReportDataAsync(CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand(Report.QualifiedProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);

            // Add system parameters unless they are overridden by user supplied parameters for drill down reports
            var pInstanceIDs = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@InstanceIDs", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pInstanceIDs != null)
            {
                pInstanceIDs.Param.Value = context.InstanceIDs.AsDataTable();
            }
            var pInstanceID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@InstanceID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pInstanceID != null)
            {
                pInstanceID.Param.Value = context.InstanceID > 0 ? context.InstanceID : DBNull.Value;
            }
            var pDatabaseID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@DatabaseID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pDatabaseID != null)
            {
                pDatabaseID.Param.Value = context.DatabaseID > 0 ? context.DatabaseID : DBNull.Value;
            }
            var pFromDate = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@FromDate", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pFromDate != null)
            {
                pFromDate.Param.Value = DateRange.FromUTC;
            }

            var pToDate = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ToDate", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pToDate != null)
            {
                pToDate.Param.Value = DateRange.ToUTC;
            }

            var pObjectID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ObjectID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pObjectID != null)
            {
                pObjectID.Param.Value = context.ObjectID > 0 ? context.ObjectID : DBNull.Value;
            }
            var pObjectName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ObjectName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pObjectName != null && !string.IsNullOrEmpty(context.ObjectName))
            {
                pObjectName.Param.Value = context.ObjectName;
                pObjectName.UseDefaultValue = false;
            }
            var pTableName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@TableName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pTableName != null && !string.IsNullOrEmpty(context.ObjectName) && context.Type == SQLTreeItem.TreeType.Table)
            {
                pTableName.Param.Value = context.ObjectName;
                pTableName.UseDefaultValue = false;
            }
            var pSchemaName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@SchemaName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pSchemaName != null && !string.IsNullOrEmpty(context.SchemaName))
            {
                pSchemaName.Param.Value = context.SchemaName;
                pSchemaName.UseDefaultValue = false;
            }

            // Add user supplied parameters
            foreach (var p in customParams.Where(p => !p.UseDefaultValue || CustomReport.SystemParamNames.Contains(p.Param.ParameterName, StringComparer.OrdinalIgnoreCase)))
            {
                cmd.Parameters.Add(p.Param.Clone());
            }
            await using var registration = cancellationToken.Register(() =>
            {
                // ReSharper disable once AccessToDisposedClosure
                cmd.Cancel();
            });
            var ds = new DataSet();
            try
            {
                da.Fill(ds);
                ConvertDateTimeColsToLocalTimeZone(ds);
            }
            finally
            {
                registration.Unregister();
            }
            return ds;
        }

        public void SetContext(DBADashContext _context)
        {
            _ = SetContext(_context, null);
        }

        public async Task SetContext(DBADashContext _context, List<CustomSqlParameter> sqlParams)
        {
            if (_context == this.context) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => _ = SetContext(_context, sqlParams)));
                return;
            }

            cboResults.Visible = false;
            lblSelectResults.Visible = false;
            ResetTimer();
            if (IsMessageInProgress)
            {
                await CancelProcessing();
            }
            ClearResults();
            IsMessageInProgress = false;
            CurrentMessageGroup = Guid.Empty;
            this.context = _context;
            Report = _context.Report ?? Report;
            customParams = sqlParams ?? Report.GetCustomSqlParameters();
            SetContextParametersForDirectExecutionReport();
            tsParams.Enabled = customParams.Count > 0;
            tsConfigure.Visible = Report.CanEditReport;
            SetStatus(Report.Description, Report.Description, DBADashUser.SelectedTheme.ForegroundColor);
            lblDescription.Visible = !string.IsNullOrEmpty(Report.Description);
            if (Report.DeserializationException != null)
            {
                MessageBox.Show(
                    "An error occurred deserializing the report. Preferences have been reset.\n" +
                    Report.DeserializationException.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Report.DeserializationException = null;// Display the message once
            }
            editPickersToolStripMenuItem.Enabled = Report.UserParams.Any();
            tsRefresh.Visible = Report is not DirectExecutionReport;
            tsExecute.Visible = Report is DirectExecutionReport;
            tsExecute.Enabled = Report is DirectExecutionReport && _context.IsScriptAllowed(Report.ProcedureName);
            lblURL.Text = Report.URL;
            lblURL.Visible = !string.IsNullOrEmpty(Report.URL);
            AddPickers();
            SetTriggerCollectionVisibility();
            if (AutoLoad)
            {
                RefreshData();
            }
        }

        private void SetContextParametersForDirectExecutionReport() // Set DatabaseName
        {
            if (Report is not DirectExecutionReport dxReport) return;
            if (string.IsNullOrEmpty(context.DatabaseName)) return;
            foreach (var p in customParams.Where(p =>
                         p.Param.ParameterName.Equals(dxReport.DatabaseNameParameter,
                             StringComparison.InvariantCultureIgnoreCase)))
            {
                p.Param.Value = context.DatabaseName;
                p.UseDefaultValue = false;
            }
            var pObjectName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ObjectName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pObjectName != null && !string.IsNullOrEmpty(context.ObjectName))
            {
                pObjectName.Param.Value = context.ObjectName;
                pObjectName.UseDefaultValue = false;
            }
            var pTableName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@TableName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pTableName != null && !string.IsNullOrEmpty(context.ObjectName) && context.Type == SQLTreeItem.TreeType.Table)
            {
                pTableName.Param.Value = context.ObjectName;
                pTableName.UseDefaultValue = false;
            }
            var pSchemaName = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@SchemaName", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pSchemaName != null && !string.IsNullOrEmpty(context.ObjectName))
            {
                pSchemaName.Param.Value = context.SchemaName;
                pSchemaName.UseDefaultValue = false;
            }
            // Some reports have a parameter to get all databases that we need to turn off
            foreach (var p in customParams.Where(p =>
                         string.Equals(p.Param.ParameterName, "@GetAllDatabases") ||
                         string.Equals(p.Param.ParameterName, "@get_all_databases",
                             StringComparison.OrdinalIgnoreCase)))
            {
                p.Param.Value = false;
                p.UseDefaultValue = false;
            }
        }

        public void SetTriggerCollectionVisibility() => tsTrigger.Visible = Report.TriggerCollectionTypes.Count > 0 && context.CanMessage;

        private void AddPickers()
        {
            tsParams.DropDownItems.Clear();
            tsParams.Click -= TsParameters_Click;
            if (Report.Pickers == null)
            {
                tsParams.Click += TsParameters_Click;

                return;
            }

            var pickers = Report.Pickers;
            if (customParams.Any(p => p.Param.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase) && p.Param.SqlDbType == SqlDbType.Int) && !pickers.Any(p => p.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase)))
            {
                pickers.Add(Picker.CreateTopPicker());
            }

            foreach (var picker in Report.Pickers.OrderBy(p => p.Name))
            {
                var param = customParams.FirstOrDefault(p =>
                        p.Param.ParameterName.TrimStart('@').Equals(picker.ParameterName.TrimStart('@'),
                            StringComparison.InvariantCultureIgnoreCase));
                if (param == null) continue;
                if (param.UseDefaultValue && !string.IsNullOrEmpty(picker.DefaultValue?.ToString()))
                {
                    param.Param.Value = picker.DefaultValue;
                    param.UseDefaultValue = false;
                }
                var pickerMenu = new ToolStripMenuItem(picker.Name);
                foreach (var itm in picker.PickerItems)
                {
                    var item = new ToolStripMenuItem(itm.Value)
                    {
                        Tag = itm.Key,
                        Checked = (param.UseDefaultValue && string.IsNullOrEmpty(itm.Key.ToString())) || (!param.UseDefaultValue && param.Param.Value != null && param.Param.Value.Equals(itm.Key))
                    };
                    item.Click += (sender, e) => PickerItem_Click(sender, itm, picker.ParameterName);
                    pickerMenu.DropDownItems.Add(item);
                }

                tsParams.DropDownItems.Add(pickerMenu);
            }

            tsParams.DropDownItems.Add(new ToolStripSeparator());

            var tsParameters = new ToolStripMenuItem("Parameters");
            tsParameters.Click += TsParameters_Click;
            tsParams.DropDownItems.Add(tsParameters);
        }

        private void PickerItem_Click(object sender, KeyValuePair<object, string> itm, string paramName)
        {
            var menu = (ToolStripMenuItem)sender;
            var param = customParams.First(p => p.Param.ParameterName.TrimStart('@').Equals(paramName.TrimStart('@'), StringComparison.InvariantCultureIgnoreCase));
            if (itm.Key == null || string.IsNullOrEmpty(itm.Key.ToString()))
            {
                param.UseDefaultValue = true;
            }
            else
            {
                param.Param.Value = itm.Key;
                param.UseDefaultValue = false;
            }

            if (menu.Owner != null)
            {
                foreach (var item in menu.Owner.Items.Cast<ToolStripMenuItem>())
                {
                    item.Checked = item == menu;
                }
            }
            if (AutoLoad)
            {
                RefreshData();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(Grids.Cast<DataGridView>().ToArray());
        }

        private void SetTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var title = Report.ReportName;
            if (CommonShared.ShowInputDialog(ref title, "Update Title") != DialogResult.OK) return;
            try
            {
                Report.ReportName = title;
                Report.Update();
                OnReportNameChanged(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving title: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected virtual void OnReportNameChanged(EventArgs e)
        {
            ReportNameChanged?.Invoke(this, e);
        }

        private void TsParameters_Click(object sender, EventArgs e)
        {
            PromptParams();
        }

        private void LnkParams_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PromptParams();
        }

        private void PromptParams()
        {
            var frm = new ReportParams() { Params = customParams };
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;
            customParams = frm.Params;
            AddPickers();// Update checks on picker items
            if (AutoLoad)
                RefreshData();
        }

        private void SaveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save Layout (column visibility, order and size)?", "Save", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var dgv in Grids)
            {
                Report.CustomReportResults[dgv.ResultSetID].ColumnLayout = dgv.GetColumnLayout();
            }
            Report.Update();
        }

        private void ResetLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset Layout (column visibility, order and size)?", "Reset", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var dgv in Grids)
            {
                Report.CustomReportResults[dgv.ResultSetID].ColumnLayout = new();
            }
            Report.Update();
            RefreshData();
        }

        private void CboResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressCboResultsIndexChanged) return;
            previousSchema = string.Empty; // Force re-generation of grids
            ShowTable();
        }

        private void SetDescriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var description = Report.Description;
            if (CommonShared.ShowInputDialog(ref description, "Enter description") != DialogResult.OK) return;
            Report.Description = description;
            Report.Update();
            lblDescription.Text = Report.Description;
            lblDescription.Visible = !string.IsNullOrEmpty(Report.Description);
        }

        private void ScriptReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ScriptReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error scripting report:" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ScriptReport()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            var serverCn = new ServerConnection(cn);
            var server = new Server(serverCn);
            var db = server.Databases[cn.Database];

            var proc = db.StoredProcedures[Report.ProcedureName, Report.SchemaName];

            if (proc != null)
            {
                var options = new ScriptingOptions() { ScriptForCreateOrAlter = true, ScriptBatchTerminator = true, EnforceScriptingOptions = true }; /* EnforceScriptingOptions = true is required to generate CREATE OR ALTER */

                var parts = proc.Script(options);
                var sb = new StringBuilder();
                sb.AppendFormat("/*\n\t{0}\n\t{1}\n\n\tCustom report for DBA Dash.\n\thttp://dbadash.com\n\tGenerated: {2:yyyy-MM-dd HH:mm:ss} \n*/\n\n",
                    Report.ReportName.Replace("*/", ""), Report.Description?.Replace("*/", ""), DateTime.Now);
                foreach (var part in parts)
                {
                    sb.AppendLine(part);
                    sb.AppendLine("GO");
                }

                try
                {
                    foreach (var picker in Report.Pickers?.OfType<DBPicker>() ?? Enumerable.Empty<DBPicker>())
                    {
                        sb.AppendLine($"/* Script picker {picker.Name.Replace("*", "")} */");

                        var (ObjectId, SchemaName, ObjectName) = CommonData.GetDBObject(picker.StoredProcedureName);
                        if (ObjectName == null || SchemaName == null)
                        {
                            sb.AppendLine($"/* Unable to find object {picker.StoredProcedureName.Replace("*", "")} */");
                            continue;
                        }

                        proc = db.StoredProcedures[ObjectName, SchemaName];
                        parts = proc.Script(options);
                        foreach (var part in parts)
                        {
                            sb.AppendLine(part);
                            sb.AppendLine("GO");
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"/* Error scripting pickers {ex.Message.Replace("*", "")} */");
                }

                var meta = Report.Serialize();
                sb.AppendLine();
                sb.AppendLine("/* Report customizations in GUI */");
                sb.AppendFormat("DELETE dbo.CustomReport\nWHERE SchemaName = '{0}'\nAND ProcedureName = '{1}'\n\n", Report.SchemaName.SqlSingleQuote(), Report.ProcedureName.SqlSingleQuote());
                sb.AppendLine("INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData)");
                sb.AppendFormat("VALUES('{0}','{1}','{2}')", Report.SchemaName.SqlSingleQuote(),
                    Report.ProcedureName.SqlSingleQuote(), meta.SqlSingleQuote());

                var frm = new CodeViewer() { Code = sb.ToString() };
                frm.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Unable to find procedure {Report.QualifiedProcedureName}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DBADashDataGridView)sender;
            if (e.RowIndex < 0) return;
            var colName = dgv.Columns[e.ColumnIndex].DataPropertyName;
            LinkColumnInfo linkColumnInfo = null;
            Report.CustomReportResults[dgv.ResultSetID].LinkColumns?.TryGetValue(colName, out linkColumnInfo);
            try
            {
                linkColumnInfo?.Navigate(context, dgv.Rows[e.RowIndex], dgv.ResultSetID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error navigating to link: " + ex.Message, "Error", MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DBADashDataGridView dgv) return;
            if (!Report.CustomReportResults.TryGetValue(dgv.ResultSetID, out CustomReportResult value)) return;
            value.CellHighlightingRules.FormatRowsAdded(dgv, e);
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            foreach (var dgv in Grids)
            {
                dgv.SetFilter(string.Empty);
            }
            tsClearFilter.Enabled = false;
            tsClearFilter.Font = new Font(tsClearFilter.Font, FontStyle.Regular);
            tsClearFilter.ToolTipText = string.Empty;
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            if (context.CollectAgentID == null || context.ImportAgentID == null) return;
            await CollectionMessaging.TriggerCollection(context.ConnectionID, Report.TriggerCollectionTypes, context.CollectAgentID.Value, context.ImportAgentID.Value, this);
        }

        private void AssociateCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var collectionTypes = string.Join(',', Report.TriggerCollectionTypes);
            if (CommonShared.ShowInputDialog(ref collectionTypes, "Enter collection types to associate with this report", default, "Enter name of collection to be associated with this report.\nThis will allow the collection to be triggered directly from this report.\nMultiple collections can be specified comma-separated.\ne.g.\nUserData.MyCustomCollection") != DialogResult.OK) return;

            Report.TriggerCollectionTypes = collectionTypes.Split(',').Select(c => c.Trim()).ToList();
            Report.Update();
            SetTriggerCollectionVisibility();
        }

        private void EditPickersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var pickers = new Pickers() { Report = Report };
            pickers.ShowDialog();
            if (pickers.DialogResult != DialogResult.OK) return;
            AddPickers();
        }

        private void URL_Click(object sender, EventArgs e)
        {
            CommonShared.OpenURL(Report.URL);
        }

        private DateTime TimerStart;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(UpdateTimer);
                return;
            }
            lblTimer.Text = (DateTime.Now - TimerStart).ToString(@"hh\:mm\:ss");
        }

        private async void TsCancel_Click(object sender, EventArgs e)
        {
            await CancelProcessing();
        }

        private async Task CancelProcessing()
        {
            if (!string.IsNullOrEmpty(Report.CancellationMessageWarning)
                && MessageBox.Show(Report.CancellationMessageWarning + "\nDo you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            if (Report is DirectExecutionReport)
            {
                if (CurrentMessageGroup != Guid.Empty)
                {
                    var msg = new CancellationMessage()
                    {
                        CollectAgent = context.CollectAgent,
                        ImportAgent = context.ImportAgent,
                        Lifetime = Config.DefaultCommandTimeout,
                        CancelMessageId = CurrentMessageGroup
                    };
                    SetStatus("Cancellation requested", "", DashColors.Warning);
                    await MessagingHelper.SendMessageAndProcessReply(msg, context, SetStatus, ProcessCompletedMessage,
                        CurrentMessageGroup);
                }
                else
                {
                    SetStatus("Nothing to cancel", "", DashColors.Information);
                    IsMessageInProgress = false;
                }
            }
            else
            {
                await cancellationTokenSource.CancelAsync();
            }
        }

        private void TsNewWindow_Click(object sender, EventArgs e)
        {
            var frm = new CustomReportViewer();

            var ctx = context.DeepCopy();
            ctx.Report = Report;
            frm.Context = ctx;
            frm.DataSet = reportDS;
            frm.CustomParams = customParams;
            frm.Show();
        }
    }
}