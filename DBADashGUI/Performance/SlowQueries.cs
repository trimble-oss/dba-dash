using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class SlowQueries : UserControl, INavigation, ISetContext, IRefreshData
    {
        public SlowQueries()
        {
            InitializeComponent();
            dgvSummary.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
        }

        private DBADashContext CurrentContext;

        private HashSet<int> InstanceIDs => CurrentContext.InstanceIDs;
        private string groupBy = "InstanceDisplayName";
        private bool savedLayoutLoaded;

        public string DBName => CurrentContext.DatabaseName;
        private bool IsDBLevel => !string.IsNullOrEmpty(CurrentContext.DatabaseName);

        private string Metric => tsMetric.Text == "CPU" ? "cpu_time" : "Duration";

        private bool IsCPU => tsMetric.Text == "CPU";

        public bool CanNavigateBack => tsRunningBack.Visible && tsRunning.Enabled;

        private long CPUFilterFrom => long.TryParse(txtCPUFrom.Text, out var value) ? value : -1;
        private long CPUFilterTo => long.TryParse(txtCPUTo.Text, out var value) ? value : long.MaxValue;
        private long DurationFilterFrom => long.TryParse(txtDurationFrom.Text, out var value) ? value : -1;
        private long DurationFilterTo => long.TryParse(txtDurationTo.Text, out var value) ? value : long.MaxValue;

        private DateTime FromLocal = DateTime.MinValue;
        private DateTime ToLocal = DateTime.MaxValue;

        private DateTime From => DateRange.FromUTC > FromLocal ? DateRange.FromUTC : FromLocal;
        private DateTime To => DateRange.ToUTC < ToLocal ? DateRange.ToUTC : ToLocal;

        private bool IsFiltered() => txtText.Text.Length > 0 ||
                                     txtClient.Text.Length > 0 ||
                                     txtDatabase.Text.Length > 0 ||
                                     txtInstance.Text.Length > 0 ||
                                     txtObject.Text.Length > 0 ||
                                     txtUser.Text.Length > 0 ||
                                     txtApp.Text.Length > 0 ||
                                     txtResult.Text.Length > 0 ||
                                     txtSessionID.Text.Length > 0 ||
                                     txtExcludeText.Text.Length > 0 ||
                                     txtExcludeClient.Text.Length > 0 ||
                                     txtExcludeDatabase.Text.Length > 0 ||
                                     txtExcludeInstance.Text.Length > 0 ||
                                     txtExcludeObject.Text.Length > 0 ||
                                     txtExcludeUser.Text.Length > 0 ||
                                     txtExcludeApp.Text.Length > 0 ||
                                     txtExcludeResult.Text.Length > 0 ||
                                     txtExcludeSessionID.Text.Length > 0 ||
                                     txtDurationFrom.Text.Length > 0 ||
                                     txtDurationTo.Text.Length > 0 ||
                                     txtPhysicalReadsFrom.Text.Length > 0 ||
                                     txtPhysicalReadsTo.Text.Length > 0 ||
                                     txtLogicalReadsFrom.Text.Length > 0 ||
                                     txtLogicalReadsTo.Text.Length > 0 ||
                                     txtWritesFrom.Text.Length > 0 ||
                                     txtWritesTo.Text.Length > 0 ||
                                     txtCPUFrom.Text.Length > 0 ||
                                     txtCPUTo.Text.Length > 0 ||
                                     FromLocal != DateTime.MinValue ||
                                     ToLocal != DateTime.MaxValue ||
                                     sqlbatchcompletedToolStripMenuItem.Checked !=
                                     rpccompletedToolStripMenuItem.Checked;

        public void SetFilterFormatting()
        {
            var boldFont = new Font(tsFilter.Font, FontStyle.Bold);
            var regularFont = new Font(tsFilter.Font, FontStyle.Regular);
            tsFilter.Font = IsFiltered() ? boldFont : regularFont;
            SetFilterFormatting(lblApp, lblIncludeApp, lblExcludeApp, txtApp, txtExcludeApp);
            SetFilterFormatting(lblText, lblIncludeText, lblExcludeText, txtText, txtExcludeText);
            SetFilterFormatting(lblClient, lblIncludeClient, lblExcludeClient, txtClient, txtExcludeClient);
            SetFilterFormatting(lblDatabase, lblIncludeDatabase, lblExcludeDatabase, txtDatabase, txtExcludeDatabase);
            SetFilterFormatting(lblInstance, lblIncludeInstance, lblExcludeInstance, txtInstance, txtExcludeInstance);
            SetFilterFormatting(lblObject, lblIncludeObject, lblExcludeObject, txtObject, txtExcludeObject);
            SetFilterFormatting(lblResult, lblIncludeResult, lblExcludeResult, txtResult, txtExcludeResult);
            SetFilterFormatting(lblUser, lblIncludeUser, lblExcludeUser, txtUser, txtExcludeUser);
            SetFilterFormatting(lblSessionID, lblIncludeSessionID, lblExcludeSessionID, txtSessionID,
                txtExcludeSessionID);
            SetFilterFormatting(lblDuration, lblDurationFrom, lblDurationTo, txtDurationFrom, txtDurationTo);
            SetFilterFormatting(lblCPU, lblCPUFrom, lblCPUTo, txtCPUFrom, txtCPUTo);
            SetFilterFormatting(lblLogicalReads, lblLogicalReadsFrom, lblLogicalReadsTo, txtLogicalReadsFrom,
                txtLogicalReadsTo);
            SetFilterFormatting(lblPhysicalReads, lblPhysicalReadsFrom, lblPhysicalReadsTo, txtPhysicalReadsFrom,
                txtPhysicalReadsTo);
            SetFilterFormatting(lblWrites, lblWritesFrom, lblWritesTo, txtWritesFrom, txtWritesTo);
            SetFilterFormatting(lblContextInfo, lblIncludeContextInfo, lblExcludeContextInfo, txtContextInfo, txtExcludeContextInfo);
            lblEventType.Font = sqlbatchcompletedToolStripMenuItem.Checked == rpccompletedToolStripMenuItem.Checked
                ? regularFont
                : boldFont;
            sqlbatchcompletedToolStripMenuItem.Font = regularFont;
            rpccompletedToolStripMenuItem.Font = regularFont;
        }

        private void SetFilterFormatting(ToolStripMenuItem rootMnu, ToolStripMenuItem includeMnu,
            ToolStripMenuItem excludeMnu, ToolStripTextBox includeTxt, ToolStripTextBox excludeTxt)
        {
            var boldFont = new Font(tsFilter.Font, FontStyle.Bold);
            var regularFont = new Font(tsFilter.Font, FontStyle.Regular);
            rootMnu.Font = includeTxt.Text.Length > 0 || excludeTxt.Text.Length > 0 ? boldFont : regularFont;
            includeMnu.Font = includeTxt.Text.Length > 0 ? boldFont : regularFont;
            excludeMnu.Font = excludeTxt.Text.Length > 0 ? boldFont : regularFont;
        }

        public void ResetFilters()
        {
            txtText.Text = "";
            txtClient.Text = "";
            txtDatabase.Text = "";
            txtInstance.Text = "";
            txtObject.Text = "";
            txtText.Text = "";
            txtUser.Text = "";
            txtApp.Text = "";
            txtResult.Text = "";
            txtSessionID.Text = "";
            txtExcludeText.Text = "";
            txtExcludeClient.Text = "";
            txtExcludeDatabase.Text = "";
            txtExcludeInstance.Text = "";
            txtExcludeObject.Text = "";
            txtExcludeText.Text = "";
            txtExcludeUser.Text = "";
            txtExcludeApp.Text = "";
            txtExcludeResult.Text = "";
            txtExcludeSessionID.Text = "";
            txtDurationFrom.Text = "";
            txtDurationTo.Text = "";
            txtCPUFrom.Text = "";
            txtCPUTo.Text = "";
            txtPhysicalReadsFrom.Text = "";
            txtPhysicalReadsTo.Text = "";
            txtLogicalReadsFrom.Text = "";
            txtLogicalReadsTo.Text = "";
            txtWritesFrom.Text = "";
            txtWritesTo.Text = "";
            txtContextInfo.Text = "";
            txtExcludeContextInfo.Text = "";
            rpccompletedToolStripMenuItem.Checked = false;
            sqlbatchcompletedToolStripMenuItem.Checked = false;
            ResetTime();
            if (IsDBLevel)
            {
                groupBy = "object_name";
            }
            else if (InstanceIDs.Count == 1)
            {
                groupBy = "DatabaseName";
            }
            else
            {
                groupBy = "InstanceDisplayName";
            }

            SelectGroupBy();
        }

        private Task<DataTable> GetSlowQueriesSummary()
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("dbo.SlowQueriesSummary_Get", cn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = Config.DefaultCommandTimeout
                };
                using var da = new SqlDataAdapter(cmd);
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("FromDate", From);
                cmd.Parameters.AddWithValue("ToDate", To);
                cmd.Parameters.AddWithValue("GroupBy", groupBy.Replace("{UTCOffset}", DateHelper.UtcOffset.ToString()));
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientHostName", txtClient.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", txtInstance.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientAppName", txtApp.Text);
                var db = DBName.Length > 0 ? DBName : txtDatabase.Text;
                cmd.Parameters.AddStringIfNotNullOrEmpty("DatabaseName", db);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ObjectName", txtObject.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("UserName", txtUser.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Text", txtText.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Result", txtResult.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("SessionID", txtSessionID.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeClientAppName", txtExcludeApp.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeClientHostName", txtExcludeClient.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeDatabaseName", txtExcludeDatabase.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeInstanceDisplayName", txtExcludeInstance.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeObjectName", txtExcludeObject.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeSessionID", txtExcludeSessionID.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeText", txtExcludeText.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeUserName", txtExcludeUser.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeResult", txtExcludeResult.Text);
                cmd.Parameters.AddWithValue("Metric", Metric);
                if (txtDurationFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DurationFromMs", Convert.ToInt64(txtDurationFrom.Text));
                }

                if (txtDurationTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DurationToMs", Convert.ToInt64(txtDurationTo.Text));
                }

                if (txtCPUFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("CPUFromMs", Convert.ToInt64(txtCPUFrom.Text));
                }

                if (txtCPUTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("CPUToMs", Convert.ToInt64(txtCPUTo.Text));
                }

                if (txtPhysicalReadsFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("PhysicalReadsFrom", Convert.ToInt64(txtPhysicalReadsFrom.Text));
                }

                if (txtPhysicalReadsTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("PhysicalReadsTo", Convert.ToInt64(txtPhysicalReadsTo.Text));
                }

                if (txtLogicalReadsFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("LogicalReadsFrom", Convert.ToInt64(txtLogicalReadsFrom.Text));
                }

                if (txtLogicalReadsTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("LogicalReadsTo", Convert.ToInt64(txtLogicalReadsTo.Text));
                }

                if (txtWritesFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WritesFrom", Convert.ToInt64(txtWritesFrom.Text));
                }

                if (txtWritesTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WritesTo", Convert.ToInt64(txtWritesTo.Text));
                }

                if (sqlbatchcompletedToolStripMenuItem.Checked && !rpccompletedToolStripMenuItem.Checked)
                {
                    cmd.Parameters.AddWithValue("EventType", "sql_batch_completed");
                }
                else if (rpccompletedToolStripMenuItem.Checked && !sqlbatchcompletedToolStripMenuItem.Checked)
                {
                    cmd.Parameters.AddWithValue("EventType", "rpc_completed");
                }
                if (txtContextInfo.Text.Length > 0)
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("ContextInfo",
                            Convert.FromHexString(txtContextInfo.Text.Trim().RemoveHexPrefix()));
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, "Invalid Context Info");
                    }
                }
                if (txtExcludeContextInfo.Text.Length > 0)
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("ExcludeContextInfo",
                            Convert.FromHexString(txtExcludeContextInfo.Text.Trim().RemoveHexPrefix()));
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, "Invalid Exclude Context Info");
                    }
                }

                var top = Convert.ToInt32(tsTop.Tag);
                cmd.Parameters.AddWithValue("Top", top);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            });
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context == CurrentContext) return;
            CurrentContext = _context;
            lblDatabase.Visible = !IsDBLevel;
            lblInstance.Visible = !IsDBLevel;
            instanceToolStripMenuItem.Visible = !IsDBLevel;
            databaseNameToolStripMenuItem.Visible = !IsDBLevel;
            ResetFilters();
            RefreshData();
        }

        public void RefreshData()
        {
            if (!savedLayoutLoaded)
            {
                LoadSavedLayout();
                savedLayoutLoaded = true;
            }

            SetFilterFormatting();
            dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvSlow.DataSource = null;
            lblPageSize.Visible = false;
            ToggleSummary(true);

            dgvSummary.Visible = false;
            refresh1.ShowRefresh();
            GetSlowQueriesSummary().ContinueWith(task =>
            {
                try
                {
                    var dt = task.Result;
                    FormatTop(dt.Rows.Count);
                    refresh1.Invoke(() => refresh1.HideRefresh());
                    dgvSummary.Invoke(() =>
                    {
                        dgvSummary.AutoGenerateColumns = false;
                        dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                        if (dgvSummary.Columns[0].Width > dgvSummary.Width)
                        {
                            // If column has expanded to > width, reset to half width
                            // scrollbars are missing in this situation so this is a workaround to resolve it
                            dgvSummary.Columns[0].Width = dgvSummary.Width / 2;
                        }

                        dgvSummary.DataSource = new DataView(dt);
                        dgvSummary.ApplyTheme(DBADashUser.SelectedTheme);
                        dgvSummary.Visible = true;
                    });
                }
                catch (Exception ex)
                {
                    refresh1.Invoke(() => refresh1.SetFailed(ex.Message));
                }
            });
        }

        private void FormatTop(int RowCount)
        {
            var boldFont = new Font(tsTop.Font, FontStyle.Bold);
            var regularFont = new Font(tsTop.Font, FontStyle.Regular);
            tsSummary.Invoke(() =>
                {
                    tsTop.Font = Convert.ToInt32(tsTop.Tag) == RowCount ? boldFont : regularFont;
                    foreach (ToolStripMenuItem mnu in tsTop.DropDownItems)
                    {
                        mnu.Font = tsTop.Tag == mnu.Tag ? boldFont : regularFont;
                    }
                }
            );
        }

        private void GroupBy_Click(object sender, EventArgs e)
        {
            var selected = (ToolStripMenuItem)sender;
            groupBy = (string)selected.Tag;
            SelectGroupBy();
            RefreshData();
        }

        private void TimeCustomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var top = Convert.ToInt32(tsTop.Tag);
            var grouping = (int)(DateRange.TimeSpan.TotalMinutes / top);
            grouping = grouping switch
            {
                < 5 => 5,
                < 10 => 10,
                < 20 => 20,
                < 30 => 30,
                < 60 => 60,
                < 120 => 120,
                < 240 => 240,
                < 720 => 720,
                _ => 1440,
            };
            var groupingInput = grouping.ToString(CultureInfo.InvariantCulture);
            if (CommonShared.ShowInputDialog(ref groupingInput, "Enter a grouping value in minutes") == DialogResult.OK)
            {
                if (int.TryParse(groupingInput, out grouping))
                {
                    groupBy = "timestamp." + grouping + "." + DateHelper.UtcOffset;
                    timeCustomToolStripMenuItem.Tag = groupBy;
                    SelectGroupBy();
                    RefreshData();
                }
                else
                {
                    MessageBox.Show($"Invalid input '{groupingInput}'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SelectGroupBy()
        {
            foreach (ToolStripMenuItem mnu in tsGroup.DropDownItems)
            {
                mnu.Checked = (string)mnu.Tag == groupBy;
                if (mnu.Checked)
                {
                    Grp.HeaderText = mnu.Text;
                }
            }
        }

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
            selectedGroupValue = row["Grp"] == DBNull.Value ? "" : Convert.ToString(row["Grp"]) ?? string.Empty;
            if (dgvSummary.Columns[e.ColumnIndex] == Grp)
            {
                switch (groupBy)
                {
                    case "InstanceDisplayName":
                        txtInstance.Text = selectedGroupValue;
                        break;

                    case "client_hostname":
                        txtClient.Text = selectedGroupValue;
                        break;

                    case "client_app_name":
                        txtApp.Text = selectedGroupValue;
                        break;

                    case "DatabaseName":
                        txtDatabase.Text = selectedGroupValue;
                        break;

                    case "object_name":
                        txtObject.Text = selectedGroupValue;
                        break;

                    case "username":
                        txtUser.Text = selectedGroupValue;
                        break;

                    case "Result":
                        txtResult.Text = selectedGroupValue;
                        break;

                    case "text":
                        txtText.Text = selectedGroupValue;
                        break;

                    case "session_id":
                        txtSessionID.Text = selectedGroupValue;
                        break;

                    case "EventType":
                        sqlbatchcompletedToolStripMenuItem.Checked = selectedGroupValue == "sql_batch_completed";
                        rpccompletedToolStripMenuItem.Checked = selectedGroupValue == "rpc_completed";
                        break;

                    default:
                        {
                            if (groupBy.StartsWith("timestamp"))
                            {
                                var mins = int.Parse(groupBy.Split(".")[1]);

                                FromLocal = Convert.ToDateTime(row["Grp"]).AddMinutes(-DateHelper.UtcOffset);
                                ToLocal = FromLocal.AddMinutes(mins);
                                timeToolStripMenuItem.Text = $"Time {FromLocal.ToAppTimeZone()} to {ToLocal.ToAppTimeZone()}";
                                timeToolStripMenuItem.Enabled = true;
                            }
                            else if (groupBy == "context_info")
                            {
                                txtContextInfo.Text = selectedGroupValue;
                            }
                            else
                            {
                                throw new Exception($"Invalid group by: {groupBy}");
                            }

                            break;
                        }
                }

                if (txtInstance.Text.Length == 0 && !IsDBLevel)
                {
                    groupBy = "InstanceDisplayName";
                }
                else if (txtDatabase.Text.Length == 0 && !IsDBLevel)
                {
                    groupBy = "DatabaseName";
                }
                else if (txtApp.Text.Length == 0)
                {
                    groupBy = "client_app_name";
                }
                else if (txtClient.Text.Length == 0)
                {
                    groupBy = "client_hostname";
                }
                else if (txtObject.Text.Length == 0)
                {
                    groupBy = "object_name";
                }
                else if (txtUser.Text.Length == 0)
                {
                    groupBy = "username";
                }
                else if (sqlbatchcompletedToolStripMenuItem.Checked == rpccompletedToolStripMenuItem.Checked)
                {
                    groupBy = "EventType";
                }
                else
                {
                    groupBy = "Result";
                }

                SelectGroupBy();
                RefreshData();
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == Total)
            {
                LoadSlowQueriesDetail();
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == Failed)
            {
                LoadSlowQueriesDetail(default, default, true);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _1hrPlus)
            {
                LoadSlowQueriesDetail(3600);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _30to60min)
            {
                LoadSlowQueriesDetail(1800, 3600);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _10to30min)
            {
                LoadSlowQueriesDetail(600, 1800);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _5to10min)
            {
                LoadSlowQueriesDetail(300, 600);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _1to5min)
            {
                LoadSlowQueriesDetail(60, 300);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _30to60)
            {
                LoadSlowQueriesDetail(30, 60);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _20to30)
            {
                LoadSlowQueriesDetail(20, 30);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _10to20)
            {
                LoadSlowQueriesDetail(10, 20);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _5to10)
            {
                LoadSlowQueriesDetail(5, 10);
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == _lt5)
            {
                LoadSlowQueriesDetail(0, 5);
            }
        }

        private string selectedGroupValue;

        private void SlowQueries_Load(object sender, EventArgs e)
        {
            dgvSummary.ApplyTheme();
            dgvSlow.ApplyTheme();
            SelectGroupBy();
            Common.AddContextInfoDisplayAsMenu(dgvSlow, "colContextInfo");
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetFilters();
            RefreshData();
        }

        private DataTable GetSlowQueriesDetail(long durationFrom = -1, long durationTo = -1, long cpuFrom = -1,
            long cpuTo = long.MaxValue, bool failed = false)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SlowQueriesDetail_Get", cn)
            { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("Top", Config.SlowQueriesDrillDownMaxRows);
                if (failed)
                {
                    cmd.Parameters.AddWithValue("ResultFailed", true);
                }

                var from = From;
                var to = To;
                var displayName = txtInstance.Text;
                var client = txtClient.Text;
                var user = txtUser.Text;
                var db = DBName.Length > 0 ? DBName : txtDatabase.Text;
                var objectName = txtObject.Text;
                var app = txtApp.Text;
                var result = txtResult.Text;
                var text = txtText.Text;
                var sessionId = txtSessionID.Text;
                var eventType = "";
                var contextInfo = txtContextInfo.Text;
                if (sqlbatchcompletedToolStripMenuItem.Checked && !rpccompletedToolStripMenuItem.Checked)
                {
                    eventType = "sql_batch_completed";
                }
                else if (rpccompletedToolStripMenuItem.Checked && !sqlbatchcompletedToolStripMenuItem.Checked)
                {
                    eventType = "rpc_completed";
                }

                switch (groupBy)
                {
                    case "InstanceDisplayName":
                        displayName = selectedGroupValue;
                        break;

                    case "client_hostname":
                        client = selectedGroupValue;
                        break;

                    case "client_app_name":
                        app = selectedGroupValue;
                        break;

                    case "DatabaseName":
                        db = selectedGroupValue;
                        break;

                    case "object_name":
                        objectName = selectedGroupValue;
                        break;

                    case "username":
                        user = selectedGroupValue;
                        break;

                    case "Result":
                        result = selectedGroupValue;
                        break;

                    case "text":
                        text = selectedGroupValue;
                        break;

                    case "session_id":
                        sessionId = selectedGroupValue;
                        break;

                    case "EventType":
                        eventType = selectedGroupValue;
                        break;

                    default:
                        {
                            if (groupBy.StartsWith("timestamp"))
                            {
                                var mins = int.Parse(groupBy.Split(".")[1]);

                                var dateGroupFrom = Convert.ToDateTime(selectedGroupValue).AddMinutes(-DateHelper.UtcOffset);
                                var dateGroupTo = dateGroupFrom.AddMinutes(mins);
                                from = dateGroupFrom > From ? dateGroupFrom : From;
                                to = dateGroupTo < To ? dateGroupTo : To;
                            }
                            else if (groupBy == "context_info")
                            {
                                contextInfo = selectedGroupValue;
                            }
                            else
                            {
                                throw new Exception($"Invalid group by: {groupBy}");
                            }

                            break;
                        }
                }

                var cpuFromMs = Math.Max(cpuFrom, CPUFilterFrom);
                var cpuToMs = Math.Min(cpuTo, CPUFilterTo);
                var durationFromMs = Math.Max(durationFrom, DurationFilterFrom);
                var durationToMs = Math.Min(durationTo, DurationFilterTo);

                cmd.Parameters.AddWithValue("FromDate", from);
                cmd.Parameters.AddWithValue("ToDate", to);
                cmd.Parameters.AddIfGreaterThanZero("CPUFromMs", cpuFromMs);
                cmd.Parameters.AddIfLessThanMaxValue("CPUToMs", cpuToMs);
                cmd.Parameters.AddIfGreaterThanZero("DurationFromMs", durationFromMs);
                cmd.Parameters.AddIfLessThanMaxValue("DurationToMs", durationToMs);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientHostName", client);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", displayName);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientAppName", app);
                cmd.Parameters.AddStringIfNotNullOrEmpty("DatabaseName", db);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ObjectName", objectName);
                cmd.Parameters.AddStringIfNotNullOrEmpty("UserName", user);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Text", text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Result", result);
                cmd.Parameters.AddStringIfNotNullOrEmpty("SessionID", sessionId);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeClientAppName", txtExcludeApp.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeClientHostName", txtExcludeClient.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeDatabaseName", txtExcludeDatabase.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeInstanceDisplayName", txtExcludeInstance.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeObjectName", txtExcludeObject.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeSessionID", txtExcludeSessionID.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeText", txtExcludeText.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeUserName", txtExcludeUser.Text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ExcludeResult", txtExcludeResult.Text);

                if (txtPhysicalReadsFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("PhysicalReadsFrom", Convert.ToInt64(txtPhysicalReadsFrom.Text));
                }

                if (txtPhysicalReadsTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("PhysicalReadsTo", Convert.ToInt64(txtPhysicalReadsTo.Text));
                }

                if (txtLogicalReadsFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("LogicalReadsFrom", Convert.ToInt64(txtLogicalReadsFrom.Text));
                }

                if (txtLogicalReadsTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("LogicalReadsTo", Convert.ToInt64(txtLogicalReadsTo.Text));
                }

                if (txtWritesFrom.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WritesFrom", Convert.ToInt64(txtWritesFrom.Text));
                }

                if (txtWritesTo.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WritesTo", Convert.ToInt64(txtWritesTo.Text));
                }

                if (contextInfo.Length > 0)
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("ContextInfo",
                            Convert.FromHexString(contextInfo.Trim().RemoveHexPrefix()));
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, $"Invalid Context Info '{contextInfo}'");
                    }
                }
                if (txtExcludeContextInfo.Text.Length > 0)
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("ExcludeContextInfo",
                            Convert.FromHexString(txtExcludeContextInfo.Text.Trim().RemoveHexPrefix()));
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, $"Invalid Exclude Context Info '{txtExcludeContextInfo.Text}'");
                    }
                }

                cmd.Parameters.AddStringIfNotNullOrEmpty("EventType", eventType);
                var dt = new DataTable();
                da.Fill(dt);
                Common.ReplaceBinaryContextInfoColumn(ref dt);
                return dt;
            }
        }

        private void LoadSlowQueriesDetail(int metricFrom = -1, int metricTo = -1, bool failed = false)
        {
            var durationFrom = !IsCPU && metricFrom > 0 ? Convert.ToInt64(metricFrom) * 1000 : -1;
            var durationTo = !IsCPU && metricTo > 0 ? Convert.ToInt64(metricTo) * 1000 : long.MaxValue;
            var cpuFrom = IsCPU && metricFrom > 0 ? Convert.ToInt64(metricFrom) * 1000 : -1;
            var cpuTo = IsCPU && metricTo > 0 ? Convert.ToInt64(metricTo) * 1000 : long.MaxValue;
            var dt = GetSlowQueriesDetail(durationFrom, durationTo, cpuFrom, cpuTo, failed);
            dt.Columns.Add("text_trunc", typeof(string));
            foreach (DataRow r in dt.Rows)
            {
                if (r["text"] != DBNull.Value)
                {
                    var txt = ((string)r["text"]);
                    r["text_trunc"] = txt.Length > 10000 ? txt[..10000] : txt;
                }
            }

            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            if (dt.Rows.Count == Config.SlowQueriesDrillDownMaxRows)
            {
                lblPageSize.Text = $"Top {Config.SlowQueriesDrillDownMaxRows} rows";
                lblPageSize.ForeColor = DashColors.Fail;
                lblPageSize.Visible = true;
            }
            else
            {
                lblPageSize.Visible = false;
            }

            dgvSlow.AutoGenerateColumns = false;
            dgvSlow.Columns["colContextInfo"].Visible = dt.Rows.Cast<DataRow>().Any(row => row["context_info"] != DBNull.Value && (row["context_info"] as string is not "0x" or "" or null));

            dgvSlow.DataSource = new DataView(dt);
            if (autoSizeColumnsToolStripMenuItem.Checked)
            {
                dgvSlow.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells, 0.25f);
            }
            dgvSlow.ApplyTheme(DBADashUser.SelectedTheme);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DgvSlow_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvSlow.Rows[e.RowIndex].DataBoundItem;
            var sessionID = Convert.ToInt32(row["session_id"]);
            var timestamp = Convert.ToDateTime(row["timestamp"]);
            if (dgvSlow.Columns[e.ColumnIndex] == colText)
            {
                var title = "SPID: " + sessionID + ", " + timestamp.ToString(CultureInfo.CurrentCulture);
                Common.ShowCodeViewer((string)row["Text"], title);
            }
            else if (dgvSlow.Columns[e.ColumnIndex] == colSessionID)
            {
                var toDate = timestamp.AppTimeZoneToUtc();
                var fromDate = Convert.ToDateTime(row["start_time"]).AppTimeZoneToUtc();
                var instanceID = Convert.ToInt32(row["InstanceID"]);
                ToggleSummary(false);
                runningQueries1.SessionID = sessionID;
                runningQueries1.SnapshotDateFrom = fromDate;
                runningQueries1.SnapshotDateTo = toDate;
                runningQueries1.InstanceID = instanceID;
                runningQueries1.RefreshData();
            }
        }

        private void Filter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshData();
            }
        }

        private void TsCopySummary_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSummary);
        }

        private void TsCopyDetail_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSlow);
        }

        private void TsTop_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            if (ts == tsTopAll)
            {
                tsTop.Text = "Top *";
            }
            else
            {
                tsTop.Text = "Top " + ts.Tag;
            }

            tsTop.Tag = ts.Tag;
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvSummary.ExportToExcel();
        }

        private void TsExcelDetail_Click(object sender, EventArgs e)
        {
            dgvSlow.ExportToExcel();
        }

        private void ToggleSummary(bool isSummary)
        {
            tsRunning.Visible = !isSummary;
            tsSummary.Visible = isSummary;
            dgvSummary.Visible = isSummary;
            runningQueries1.Visible = !isSummary;
        }

        private void TsRunningBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                ToggleSummary(true);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Filter_TextChanged(object sender, EventArgs e)
        {
            SetFilterFormatting();
        }

        private void EventType_CheckedChanged(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            if (mnu.Checked)
            {
                rpccompletedToolStripMenuItem.Checked = rpccompletedToolStripMenuItem == mnu;
                sqlbatchcompletedToolStripMenuItem.Checked = sqlbatchcompletedToolStripMenuItem == mnu;
            }
        }

        private void EventType_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Filter_Numeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshData();
            }
            else if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void Filter_NumericPlusComma_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshData();
            }
            else if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != ',')
            {
                e.Handled = true;
            }
        }

        private void DgvSlow_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgvSlow.Rows[idx];
                var status = Convert.ToString(r.Cells["Result"].Value) == "0 - OK"
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical;
                r.Cells["Result"].SetStatusColor(status);
            }
        }

        private void LoadSavedLayout()
        {
            resetLayoutToolStripMenuItem.Enabled = false;
            loadSavedToolStripMenuItem.Enabled = false;
            try
            {
                var saved = SlowQueryDetailSavedView.GetDefaultSavedView();

                if (saved != null)
                {
                    resetLayoutToolStripMenuItem.Enabled = true;
                    loadSavedToolStripMenuItem.Enabled = true;
                    autoSizeColumnsToolStripMenuItem.Checked = saved.AutoSizeColumns;
                    dgvSlow.LoadColumnLayout(saved.ColumnLayout);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading saved view");
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SlowQueryDetailSavedView saved = new()
            {
                ColumnLayout = dgvSlow.GetColumnLayout(),
                Name = "Default",
                AutoSizeColumns = autoSizeColumnsToolStripMenuItem.Checked
            };
            try
            {
                saved.Save();
                resetLayoutToolStripMenuItem.Enabled = true;
                loadSavedToolStripMenuItem.Enabled = true;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving grid layout");
            }
        }

        private void AutoSizeColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autoSizeColumnsToolStripMenuItem.Checked)
            {
                dgvSlow.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells, 0.25f);
            }
        }

        private void LoadSavedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadSavedLayout();
        }

        private void ResetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                var saved = SlowQueryDetailSavedView.GetDefaultSavedView();
                if (saved != null)
                {
                    if (MessageBox.Show("Remove saved layout?", "Reset", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        saved.Delete();
                        resetLayoutToolStripMenuItem.Enabled = false;
                        loadSavedToolStripMenuItem.Enabled = false;
                        MessageBox.Show(
                            "The application defaults will be used the next time the application is started", "Reset",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error deleting saved view");
            }
        }

        private void Metric_Selected(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            tsMetric.CheckSingleItem(item);
            tsMetric.Text = item.Text;
            RefreshData();
        }

        private void ResetTime()
        {
            FromLocal = DateTime.MinValue;
            ToLocal = DateTime.MaxValue;
            timeToolStripMenuItem.Text = "Time";
            timeToolStripMenuItem.Enabled = false;
        }

        private void TimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetTime();
            RefreshData();
        }
    }
}