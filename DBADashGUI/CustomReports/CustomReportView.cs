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
using System.Windows.Navigation;
using DBADash;
using DBADashGUI.Interface;
using DBADash.Messaging;
using DBADashGUI.CommunityTools;
using DBADashGUI.Messaging;

namespace DBADashGUI.CustomReports
{
    public partial class CustomReportView : UserControl, ISetContext, IRefreshData, ISetStatus
    {
        public event EventHandler ReportNameChanged;

        private ContextMenuStrip columnContextMenu;
        private ContextMenuStrip cellContextMenu;
        private readonly ToolStripMenuItem convertLocalMenuItem = new("Convert to local timezone") { Checked = true, CheckOnClick = true };
        private readonly ToolStripMenuItem filterLike = new("LIKE", Properties.Resources.FilteredTextBox_16x);
        private int clickedColumnIndex = -1;
        private int clickedRowIndex = -1;
        protected DataSet reportDS;
        private int selectedTableIndex;
        private bool doAutoSize = true;
        private bool suppressCboResultsIndexChanged;
        public DataGridView Grid => dgv;
        public ToolStripStatusLabel StatusLabel => lblDescription;
        public StatusStrip StatusStrip => statusStrip1;
        private DBADashContext context;
        private CustomReport report;
        private List<CustomSqlParameter> customParams = new();
        private CancellationTokenSource cancellationTokenSource;
        private Guid CurrentMessageGroup;

        private bool AutoLoad => report is not DirectExecutionReport;

        public CustomReportView()
        {
            InitializeComponent();
            ShowParamPrompt(false);
            dgv.AutoGenerateColumns = false;
            this.ApplyTheme();
        }

        private void InitializeContextMenu()
        {
            columnContextMenu = new ContextMenuStrip();
            if (report.CanEditReport)
            {
                var renameColumnMenuItem = new ToolStripMenuItem("Rename Column", Properties.Resources.Rename_16x);
                var setFormatStringMenuItem =
                    new ToolStripMenuItem("Set Format String", Properties.Resources.Percentage_16x);
                var addLink = new ToolStripMenuItem("Add Link", Properties.Resources.WebURL_16x);
                var rules = new ToolStripMenuItem("Highlighting Rules", Properties.Resources.HighlightHS);
                renameColumnMenuItem.Click += RenameColumnMenuItem_Click;
                convertLocalMenuItem.Click += ConvertLocalMenuItem_Click;
                setFormatStringMenuItem.Click += SetFormatStringMenuItem_Click;
                addLink.Click += AddLink_Click;
                rules.Click += SetCellHighlightingRules;
                columnContextMenu.Items.Add(renameColumnMenuItem);
                columnContextMenu.Items.Add(convertLocalMenuItem);
                columnContextMenu.Items.Add(setFormatStringMenuItem);
                columnContextMenu.Items.Add(addLink);
                columnContextMenu.Items.Add(rules);
                dgv.MouseUp += Dgv_MouseUp;
            }

            var highlight = new ToolStripMenuItem("Highlight", Properties.Resources.HighlightHS);
            var filterByValue = new ToolStripMenuItem("Filter By Value", Properties.Resources.Filter_16x) { Tag = false };
            var excludeValue = new ToolStripMenuItem("Exclude Value", Properties.Resources.StopFilter_16x) { Tag = true };
            cellContextMenu = new ContextMenuStrip();
            cellContextMenu.Items.Add(filterByValue);
            cellContextMenu.Items.Add(excludeValue);
            cellContextMenu.Items.Add(filterLike);
            if (report.CanEditReport)
            {
                cellContextMenu.Items.Add(highlight);
            }
            highlight.Click += SetCellHighlightingRules;
            excludeValue.Click += FilterByValue_Click;
            filterByValue.Click += FilterByValue_Click;
            filterLike.Click += FilterLike_Click;
            dgv.MouseUp += Dgv_MouseUp;
        }

        private void FilterLike_Click(object sender, EventArgs e)
        {
            var value = dgv.Rows[clickedRowIndex].Cells[clickedColumnIndex].Value.DBNullToNull()?.ToString();
            var colName = dgv.Columns[clickedColumnIndex].DataPropertyName;

            if (CommonShared.ShowInputDialog(ref value, "Enter value to filter by:", default, "Use % or * as wildcards") == DialogResult.Cancel) return;
            if (string.IsNullOrEmpty(value)) return;
            if (dgv.DataSource is not DataView dv) return;
            var filter = dv.RowFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                filter += Environment.NewLine + " AND ";
            }
            value = EscapeValue(value);
            filter += $"{colName} LIKE {value}";
            SetFilter(filter);
        }

        private void SetFilter(string filter)
        {
            if (dgv.DataSource is not DataView dv) return;
            var previousFilter = dv.RowFilter;
            try
            {
                dv.RowFilter = filter;
                tsClearFilter.Enabled = true;
                tsClearFilter.Font = new Font(tsClearFilter.Font, FontStyle.Bold);
                tsClearFilter.ToolTipText = dv.RowFilter;
            }
            catch (Exception ex)
            {
                dv.RowFilter = previousFilter;
                MessageBox.Show("Error setting row filter: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FilterByValue_Click(object sender, EventArgs e)
        {
            var exclude = (bool)((ToolStripMenuItem)sender).Tag;
            var value = dgv.Rows[clickedRowIndex].Cells[clickedColumnIndex].Value;
            var colName = dgv.Columns[clickedColumnIndex].DataPropertyName;
            if (value.GetType() == typeof(byte[]))
            {
                MessageBox.Show("Filter by value is not supported for binary data", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            colName = EscapeColumnName(colName);
            var filterValue = FormatFilterValue(value, exclude);

            if (dgv.DataSource is not DataView dv) return;
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
            else
            {
                return $"{compare} " + EscapeValue(value.ToString());
            }
        }

        private void SetCellHighlightingRules(object sender, EventArgs e)
        {
            try
            {
                var customReportResult = report.CustomReportResults[selectedTableIndex];
                var columnName = dgv.Columns[clickedColumnIndex].DataPropertyName;
                customReportResult.CellHighlightingRules.TryGetValue(dgv.Columns[clickedColumnIndex].DataPropertyName,
                    out var ruleSet);

                var frm = new CellHighlightingRulesConfig()
                {
                    ColumnList = dgv.Columns,
                    CellHighlightingRules = new KeyValuePair<string, CellHighlightingRuleSet>(columnName,
                        ruleSet.DeepCopy() ?? new CellHighlightingRuleSet() { TargetColumn = columnName }),
                    CellValue = clickedRowIndex >= 0 ? dgv.Rows[clickedRowIndex].Cells[clickedColumnIndex].Value : null,
                    CellValueIsNull = clickedRowIndex >= 0 &&
                                      dgv.Rows[clickedRowIndex].Cells[clickedColumnIndex].Value.DBNullToNull() == null
                };

                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;
                report.CustomReportResults[selectedTableIndex].CellHighlightingRules.Remove(columnName);
                if (frm.CellHighlightingRules.Value is { HasRules: true })
                {
                    report.CustomReportResults[selectedTableIndex].CellHighlightingRules
                        .Add(columnName, frm.CellHighlightingRules.Value);
                }

                report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting highlighting rules: " + ex.Message, "Error", MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
            }
        }

        private void AddLink_Click(object sender, EventArgs e)
        {
            try
            {
                var customReportResult = report.CustomReportResults[selectedTableIndex];
                var col = dgv.Columns[clickedColumnIndex].DataPropertyName;
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

                dgv.Columns.Clear();
                doAutoSize = true;
                report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding link: " + ex.Message, "Error", MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
            }
        }

        private void SetFormatStringMenuItem_Click(object sender, EventArgs e)
        {
            if (clickedColumnIndex < 0) return;
            var formatString = dgv.Columns[clickedColumnIndex].DefaultCellStyle.Format;
            var key = dgv.Columns[clickedColumnIndex].Name;
            if (CommonShared.ShowInputDialog(ref formatString, "Enter format string (e.g. N1, P1, yyyy-MM-dd)") !=
                DialogResult.OK) return;
            dgv.Columns[clickedColumnIndex].DefaultCellStyle.Format = formatString;
            report.CustomReportResults[selectedTableIndex].CellFormatString.Remove(key);

            if (!string.IsNullOrEmpty(formatString))
            {
                report.CustomReportResults[selectedTableIndex].CellFormatString.Add(key, formatString);
            }

            report.Update();
        }

        private void ConvertLocalMenuItem_Click(object sender, EventArgs e)
        {
            if (clickedColumnIndex < 0) return;
            var name = dgv.Columns[clickedColumnIndex].DataPropertyName;
            switch (convertLocalMenuItem.Checked)
            {
                case true when report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(name):
                    report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Remove(name);
                    break;

                case false when !report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(name):
                    report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Add(name);
                    break;
            }

            try
            {
                report.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving time zone preference: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            RefreshData();
        }

        /// <summary>
        /// Used to display context menu when user right-clicks column headers.  Selected column index is stored in clickedColumnIndex
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dgv_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            // Perform a hit test to determine where the click occurred
            var hitTestInfo = dgv.HitTest(e.X, e.Y);
            clickedColumnIndex = hitTestInfo.ColumnIndex;
            clickedRowIndex = hitTestInfo.RowIndex;
            switch (hitTestInfo.Type)
            {
                case DataGridViewHitTestType.Cell:
                    filterLike.Visible = dgv.Columns[clickedColumnIndex].ValueType == typeof(string);
                    cellContextMenu.Show(dgv, e.Location);
                    return;

                case DataGridViewHitTestType.ColumnHeader:
                    {
                        if (dgv.Columns[clickedColumnIndex].ValueType ==
                            typeof(DateTime)) // Show option for timezone conversion if column is DateTime
                        {
                            convertLocalMenuItem.Checked =
                                !report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone
                                    .Contains(dgv.Columns[clickedColumnIndex].DataPropertyName);
                            convertLocalMenuItem.Visible = true;
                        }
                        else
                        {
                            convertLocalMenuItem.Visible = false;
                        }

                        // Show the context menu at the mouse position
                        columnContextMenu.Show(dgv, e.Location);
                        break;
                    }
            }
        }

        private void RenameColumnMenuItem_Click(object sender, EventArgs e)
        {
            if (clickedColumnIndex < 0) return;
            // Show a dialog for renaming
            var newName = dgv.Columns[clickedColumnIndex].HeaderText;
            if (CommonShared.ShowInputDialog(ref newName, "Enter new column name:") != DialogResult.OK) return;
            if (string.IsNullOrEmpty(newName))
            {
                newName = dgv.Columns[clickedColumnIndex].DataPropertyName;
            }
            dgv.Columns[clickedColumnIndex].HeaderText = newName;
            try
            {
                report.CustomReportResults[selectedTableIndex].ColumnAlias = GetColumnMapping();
                report.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving alias: " + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, string> GetColumnMapping() => dgv.Columns.Cast<DataGridViewColumn>()
                .Where(column => column.DataPropertyName != column.HeaderText)
                .ToDictionary(column => column.DataPropertyName, column => column.HeaderText);

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
        /// <param name="dt"></param>
        private void ConvertDateTimeColsToLocalTimeZone(DataTable dt)
        {
            var convertCols = dt.Columns.Cast<DataColumn>()
                .Where(column => column.DataType == typeof(DateTime) && !report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(column.ColumnName))
                .Select(column => column.ColumnName).ToList();
            if (convertCols.Count > 0)
            {
                DateHelper.ConvertUTCToAppTimeZone(ref dt, convertCols);
            }
        }

        private void SetDataSource(DataTable dt)
        {
            if (dgv.Columns.Count == 0)
            {
                var customReportResults = report.CustomReportResults[selectedTableIndex];
                dgv.AddColumns(dt, customReportResults);
                dgv.ApplyTheme();
            }
            dgv.DataSource = null;
            dgv.DataSource = new DataView(dt);
            tsClearFilter.Enabled = false;
            tsClearFilter.Font = new Font(tsClearFilter.Font, FontStyle.Regular);
            tsClearFilter.ToolTipText = string.Empty;
        }

        /// <summary>
        /// Results combobox allows user to select which result set to display if the report returns multiple result sets
        /// </summary>
        protected void LoadResultsCombo()
        {
            suppressCboResultsIndexChanged = true;
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                if (report.CustomReportResults.TryGetValue(i, out var result))
                {
                    result.ResultName ??= "Result" + i; // ensure we have a name
                }
                else
                {
                    report.CustomReportResults.Add(i, new CustomReportResult() { ResultName = "Result" + i });
                }
            }

            cboResults.Items.Clear();
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                cboResults.Items.Add(report.CustomReportResults[i].ResultName);
            }
            renameResultSetToolStripMenuItem.Visible = cboResults.Items.Count > 1;
            cboResults.Visible = cboResults.Items.Count > 1;
            lblSelectResults.Visible = cboResults.Items.Count > 1;
            cboResults.SelectedIndex = selectedTableIndex < cboResults.Items.Count ? selectedTableIndex : 0;
            suppressCboResultsIndexChanged = false;
        }

        public void RefreshData()
        {
            if (!report.HasAccess())
            {
                MessageBox.Show("You do not have access to this report", "Access Denied", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            if (report is DirectExecutionReport)
            {
                RefreshDataMessage();
            }
            else
            {
                StartTimer();
                cancellationTokenSource = new CancellationTokenSource();
                IsMessageInProgress = true;
                Task.Run(() =>
                {
                    _ = RefreshDataRepository(cancellationTokenSource.Token);
                });
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
                CommandName = Enum.Parse<ProcedureExecutionMessage.CommandNames>(report.ProcedureName),
                Parameters = customParams,
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = Config.DefaultCommandTimeout
            };
            doAutoSize = true;

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
                        this.Invoke(() =>
                        {
                            ShowTable(true);
                        });
                        SetStatus("Completed", "Success", DashColors.Success);
                    }
                    else
                    {
                        SetStatus("Report didn't return a result set", "Warning", System.Drawing.Color.Orange);
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

        protected void ShowTable(bool reset = false)
        {
            if (reset)
            {
                dgv.Columns.Clear();
            }
            if (reportDS.Tables.Count == 0) return;
            if (selectedTableIndex >= reportDS.Tables.Count)
            {
                suppressCboResultsIndexChanged = true;
                selectedTableIndex = 0;
                cboResults.SelectedIndex = 0;
                dgv.Columns.Clear();
                suppressCboResultsIndexChanged = false;
            }
            var dt = reportDS.Tables[selectedTableIndex];
            ConvertDateTimeColsToLocalTimeZone(dt);

            SetDataSource(dt);

            SetColumnLayout();
        }

        private void SetColumnLayout()
        {
            const int maxWidth = 400;
            if (report.CustomReportResults[selectedTableIndex].ColumnLayout.Count > 0)
            {
                dgv.LoadColumnLayout(report.CustomReportResults[selectedTableIndex].ColumnLayout);
            }
            else if (doAutoSize)
            {
                dgv.AutoResizeColumns();
                // Ensure column size is not excessive
                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    if (column.Width <= maxWidth) continue;
                    column.Width = maxWidth;
                }
            }

            if (dgv.Rows.Count > 0)
            {
                doAutoSize = false;
            }
        }

        /// <summary>
        /// Run the query to get the data for the user custom report
        /// </summary>
        /// <returns></returns>
        protected async Task<DataSet> GetReportDataAsync(CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand(report.QualifiedProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
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

            // Add user supplied parameters
            foreach (var p in customParams.Where(p => !p.UseDefaultValue || CustomReport.SystemParamNames.Contains(p.Param.ParameterName, StringComparer.OrdinalIgnoreCase)))
            {
                cmd.Parameters.Add(p.Param.Clone());
            }
            await using var registration = cancellationToken.Register(() =>
            {
                cmd.Cancel();
            });
            var ds = new DataSet();
            try
            {
                da.Fill(ds);
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
            IsMessageInProgress = false;
            CurrentMessageGroup = Guid.Empty;
            doAutoSize = true;
            selectedTableIndex = 0;
            dgv.Columns.Clear();
            this.context = _context;
            report = _context.Report;
            customParams = sqlParams ?? report.GetCustomSqlParameters();
            tsParams.Enabled = customParams.Count > 0;
            tsConfigure.Visible = report.CanEditReport;
            SetStatus(report.Description, report.Description, DBADashUser.SelectedTheme.ForegroundColor);
            lblDescription.Visible = !string.IsNullOrEmpty(report.Description);
            if (report.DeserializationException != null)
            {
                MessageBox.Show(
                    "An error occurred deserializing the report. Preferences have been reset.\n" +
                    report.DeserializationException.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                report.DeserializationException = null;// Display the message once
            }
            editPickersToolStripMenuItem.Enabled = report.UserParams.Any();
            tsRefresh.Visible = report is not DirectExecutionReport;
            tsExecute.Visible = report is DirectExecutionReport;
            lblURL.Text = report.URL;
            lblURL.Visible = !string.IsNullOrEmpty(report.URL);
            InitializeContextMenu();
            AddPickers();
            SetTriggerCollectionVisibility();
            if (AutoLoad)
            {
                RefreshData();
            }
        }

        public void SetTriggerCollectionVisibility() => tsTrigger.Visible = report.TriggerCollectionTypes.Count > 0 && context.CanMessage;

        private void AddPickers()
        {
            tsParams.DropDownItems.Clear();
            tsParams.Click -= TsParameters_Click;
            if (report.Pickers == null)
            {
                tsParams.Click += TsParameters_Click;

                return;
            }

            var pickers = report.Pickers;
            if (customParams.Any(p => p.Param.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase) && p.Param.SqlDbType == SqlDbType.Int) && !pickers.Any(p => p.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase)))
            {
                pickers.Add(Picker.CreateTopPicker());
            }

            foreach (var picker in report.Pickers.OrderBy(p => p.Name))
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

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(dgv);
        }

        private void SetTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var title = report.ReportName;
            if (CommonShared.ShowInputDialog(ref title, "Update Title") != DialogResult.OK) return;
            try
            {
                report.ReportName = title;
                report.Update();
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

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgv.PromptColumnSelection();
        }

        private void SaveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save Layout (column visibility, order and size)?", "Save", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            report.CustomReportResults[selectedTableIndex].ColumnLayout = dgv.GetColumnLayout();
            report.Update();
        }

        private void ResetLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset Layout (column visibility, order and size)?", "Reset", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            report.CustomReportResults[selectedTableIndex].ColumnLayout = new();
            report.Update();
            dgv.Columns.Clear();
            doAutoSize = true;
            RefreshData();
        }

        private void CboResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressCboResultsIndexChanged) return;
            selectedTableIndex = cboResults.SelectedIndex;
            dgv.Columns.Clear();
            doAutoSize = true;
            ShowTable();
        }

        private void RenameResultSetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var name = cboResults.SelectedItem.ToString();
            if (CommonShared.ShowInputDialog(ref name, "Enter name") == DialogResult.OK)
            {
                report.CustomReportResults[cboResults.SelectedIndex].ResultName = name;
                suppressCboResultsIndexChanged = true;
                cboResults.Items[selectedTableIndex] = name;
                suppressCboResultsIndexChanged = false;
                report.Update();
            }
        }

        private void SetDescriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var description = report.Description;
            if (CommonShared.ShowInputDialog(ref description, "Enter description") != DialogResult.OK) return;
            report.Description = description;
            report.Update();
            lblDescription.Text = report.Description;
            lblDescription.Visible = !string.IsNullOrEmpty(report.Description);
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

            var proc = db.StoredProcedures[report.ProcedureName, report.SchemaName];

            if (proc != null)
            {
                var options = new ScriptingOptions() { ScriptForCreateOrAlter = true, ScriptBatchTerminator = true, EnforceScriptingOptions = true }; /* EnforceScriptingOptions = true is required to generate CREATE OR ALTER */

                var parts = proc.Script(options);
                var sb = new StringBuilder();
                sb.AppendFormat("/*\n\t{0}\n\t{1}\n\n\tCustom report for DBA Dash.\n\thttp://dbadash.com\n\tGenerated: {2:yyyy-MM-dd HH:mm:ss} \n*/\n\n",
                    report.ReportName.Replace("*/", ""), report.Description?.Replace("*/", ""), DateTime.Now);
                foreach (var part in parts)
                {
                    sb.AppendLine(part);
                    sb.AppendLine("GO");
                }

                try
                {
                    foreach (var picker in report.Pickers?.OfType<DBPicker>() ?? Enumerable.Empty<DBPicker>())
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

                var meta = report.Serialize();
                sb.AppendLine();
                sb.AppendLine("/* Report customizations in GUI */");
                sb.AppendFormat("DELETE dbo.CustomReport\nWHERE SchemaName = '{0}'\nAND ProcedureName = '{1}'\n\n", report.SchemaName.SqlSingleQuote(), report.ProcedureName.SqlSingleQuote());
                sb.AppendLine("INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData)");
                sb.AppendFormat("VALUES('{0}','{1}','{2}')", report.SchemaName.SqlSingleQuote(),
                    report.ProcedureName.SqlSingleQuote(), meta.SqlSingleQuote());

                var frm = new CodeViewer() { Code = sb.ToString() };
                frm.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Unable to find procedure {report.QualifiedProcedureName}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = dgv.Columns[e.ColumnIndex].DataPropertyName;
            LinkColumnInfo linkColumnInfo = null;
            report.CustomReportResults[selectedTableIndex].LinkColumns?.TryGetValue(colName, out linkColumnInfo);
            try
            {
                linkColumnInfo?.Navigate(context, dgv.Rows[e.RowIndex], selectedTableIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error navigating to link: " + ex.Message, "Error", MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            report.CustomReportResults[selectedTableIndex].CellHighlightingRules.FormatRowsAdded(sender as DataGridView, e);
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            if (dgv.DataSource is not DataView dv) return;
            dv.RowFilter = string.Empty;
            tsClearFilter.Enabled = false;
            tsClearFilter.Font = new Font(tsClearFilter.Font, FontStyle.Regular);
            tsClearFilter.ToolTipText = string.Empty;
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            if (context.CollectAgentID == null || context.ImportAgentID == null) return;
            await Messaging.CollectionMessaging.TriggerCollection(context.ConnectionID, report.TriggerCollectionTypes, context.CollectAgentID.Value, context.ImportAgentID.Value, this, null);
        }

        private void AssociateCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var collectionTypes = string.Join(',', report.TriggerCollectionTypes);
            if (CommonShared.ShowInputDialog(ref collectionTypes, "Enter collection types to associate with this report", default, "Enter name of collection to be associated with this report.\nThis will allow the collection to be triggered directly from this report.\nMultiple collections can be specified comma-separated.\ne.g.\nUserData.MyCustomCollection") != DialogResult.OK) return;

            report.TriggerCollectionTypes = collectionTypes.Split(',').Select(c => c.Trim()).ToList();
            report.Update();
            SetTriggerCollectionVisibility();
        }

        private void EditPickersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var pickers = new Pickers() { Report = report };
            pickers.ShowDialog();
            if (pickers.DialogResult != DialogResult.OK) return;
            AddPickers();
        }

        private void URL_Click(object sender, EventArgs e)
        {
            CommonShared.OpenURL(report.URL);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].ValueType == typeof(byte[]) && e.Value != null && e.Value != DBNull.Value)
            {
                byte[] bytes = (byte[])e.Value;
                // Convert the byte array to a hexadecimal string
                e.Value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                e.FormattingApplied = true; // Indicate that formatting was applied
            }
        }

        private DateTime TimerStart;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            lblTimer.Text = (DateTime.Now - TimerStart).ToString(@"hh\:mm\:ss");
        }

        private async void TsCancel_Click(object sender, EventArgs e)
        {
            await CancelProcessing();
        }

        private async Task CancelProcessing()
        {
            if (report is DirectExecutionReport)
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
                cancellationTokenSource.Cancel();
            }
        }
    }
}