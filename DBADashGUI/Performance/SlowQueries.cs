using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
        }

        private List<Int32> InstanceIDs;
        private string groupBy = "InstanceDisplayName";
        private string _db = "";
        private bool savedLayoutLoaded = false;

        public string DBName
        {
            get => _db;
            set
            {
                _db = value;

                lblDatabase.Visible = _db.Length == 0;
                lblInstance.Visible = _db.Length == 0;
                instanceToolStripMenuItem.Visible = _db.Length == 0;
                databaseNameToolStripMenuItem.Visible = _db.Length == 0;
            }
        }

        private string Metric => tsMetric.Text == "CPU" ? "cpu_time" : "Duration";

        private bool IsCPU => tsMetric.Text == "CPU";

        public bool CanNavigateBack => tsRunningBack.Visible && tsRunning.Enabled;

        private long CPUFilterFrom => long.TryParse(txtCPUFrom.Text, out long value) ? value : -1;
        private long CPUFilterTo => long.TryParse(txtCPUTo.Text, out long value) ? value : long.MaxValue;
        private long DurationFilterFrom => long.TryParse(txtDurationFrom.Text, out long value) ? value : -1;
        private long DurationFilterTo => long.TryParse(txtDurationTo.Text, out long value) ? value : long.MaxValue;

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
                  sqlbatchcompletedToolStripMenuItem.Checked != rpccompletedToolStripMenuItem.Checked
                  ;

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
            SetFilterFormatting(lblSessionID, lblIncludeSessionID, lblExcludeSessionID, txtSessionID, txtExcludeSessionID);
            SetFilterFormatting(lblDuration, lblDurationFrom, lblDurationTo, txtDurationFrom, txtDurationTo);
            SetFilterFormatting(lblCPU, lblCPUFrom, lblCPUTo, txtCPUFrom, txtCPUTo);
            SetFilterFormatting(lblLogicalReads, lblLogicalReadsFrom, lblLogicalReadsTo, txtLogicalReadsFrom, txtLogicalReadsTo);
            SetFilterFormatting(lblPhysicalReads, lblPhysicalReadsFrom, lblPhysicalReadsTo, txtPhysicalReadsFrom, txtPhysicalReadsTo);
            SetFilterFormatting(lblWrites, lblWritesFrom, lblWritesTo, txtWritesFrom, txtWritesTo);
            lblEventType.Font = sqlbatchcompletedToolStripMenuItem.Checked == rpccompletedToolStripMenuItem.Checked ? regularFont : boldFont;
            sqlbatchcompletedToolStripMenuItem.Font = regularFont;
            rpccompletedToolStripMenuItem.Font = regularFont;
        }

        private void SetFilterFormatting(ToolStripMenuItem rootMnu, ToolStripMenuItem includeMnu, ToolStripMenuItem excludeMnu, ToolStripTextBox includeTxt, ToolStripTextBox excludeTxt)
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
            rpccompletedToolStripMenuItem.Checked = false;
            sqlbatchcompletedToolStripMenuItem.Checked = false;
            if (_db.Length > 0)
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
                using (var cn = new SqlConnection(Common.ConnectionString))
                using (var cmd = new SqlCommand("dbo.SlowQueriesSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
                using (var da = new SqlDataAdapter(cmd))
                {
                    cn.Open();

                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.Parameters.AddWithValue("GroupBy", groupBy);
                    cmd.Parameters.AddStringIfNotNullOrEmpty("ClientHostName", txtClient.Text);
                    cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", txtInstance.Text);
                    cmd.Parameters.AddStringIfNotNullOrEmpty("ClientAppName", txtApp.Text);
                    string db = DBName.Length > 0 ? DBName : txtDatabase.Text;
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
                        cmd.Parameters.AddWithValue("Eventtype", "sql_batch_completed");
                    }
                    else if (rpccompletedToolStripMenuItem.Checked && !sqlbatchcompletedToolStripMenuItem.Checked)
                    {
                        cmd.Parameters.AddWithValue("EventType", "rpc_completed");
                    }
                    int top = Convert.ToInt32(tsTop.Tag);
                    cmd.Parameters.AddWithValue("Top", top);
                    cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            });
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DBName = context.DatabaseName;
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
                        if (dgvSummary.Columns[0].Width > dgvSummary.Width)
                        {
                            // If column has expanded to > width, reset to half width
                            // scrollbars are missing in this situation so this is a workaround to resolve it
                            dgvSummary.Columns[0].Width = dgvSummary.Width / 2;
                        }
                        dgvSummary.DataSource = dt;

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

        public Int32 pageSize = 1000;

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                selectedGroupValue = row["Grp"] == DBNull.Value ? "" : Convert.ToString(row["Grp"]);
                if (dgvSummary.Columns[e.ColumnIndex] == Grp)
                {
                    if (groupBy == "InstanceDisplayName")
                    {
                        txtInstance.Text = selectedGroupValue;
                    }
                    else if (groupBy == "client_hostname")
                    {
                        txtClient.Text = selectedGroupValue;
                    }
                    else if (groupBy == "client_app_name")
                    {
                        txtApp.Text = selectedGroupValue;
                    }
                    else if (groupBy == "DatabaseName")
                    {
                        txtDatabase.Text = selectedGroupValue;
                    }
                    else if (groupBy == "object_name")
                    {
                        txtObject.Text = selectedGroupValue;
                    }
                    else if (groupBy == "username")
                    {
                        txtUser.Text = selectedGroupValue;
                    }
                    else if (groupBy == "Result")
                    {
                        txtResult.Text = selectedGroupValue;
                    }
                    else if (groupBy == "text")
                    {
                        txtText.Text = selectedGroupValue;
                    }
                    else if (groupBy == "session_id")
                    {
                        txtSessionID.Text = selectedGroupValue;
                    }
                    else if (groupBy == "EventType")
                    {
                        sqlbatchcompletedToolStripMenuItem.Checked = selectedGroupValue == "sql_batch_completed";
                        rpccompletedToolStripMenuItem.Checked = selectedGroupValue == "rpc_completed";
                    }
                    else
                    {
                        throw new Exception($"Invalid group by: {groupBy}");
                    }

                    if (txtInstance.Text.Length == 0 && _db.Length == 0)
                    {
                        groupBy = "InstanceDisplayName";
                    }
                    else if (txtDatabase.Text.Length == 0 && _db.Length == 0)
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
                    LoadSlowQueriesDetail(3600, -1);
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
        }

        private string selectedGroupValue;

        private void SlowQueries_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgvSummary);
            Common.StyleGrid(ref dgvSlow);
            SelectGroupBy();
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetFilters();
            RefreshData();
        }

        private DataTable GetSlowQueriesDetail(long durationFrom = -1, long durationTo = -1, long cpuFrom = -1, long cpuTo = long.MaxValue, bool failed = false)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SlowQueriesDetail_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("Top", pageSize);
                if (failed)
                {
                    cmd.Parameters.AddWithValue("ResultFailed", true);
                }

                string displayName = txtInstance.Text;
                string client = txtClient.Text;
                string user = txtUser.Text;
                string db = DBName.Length > 0 ? DBName : txtDatabase.Text;
                string objectname = txtObject.Text;
                string app = txtApp.Text;
                string result = txtResult.Text;
                string text = txtText.Text;
                string sessionid = txtSessionID.Text;
                string eventType = "";
                if (sqlbatchcompletedToolStripMenuItem.Checked && !rpccompletedToolStripMenuItem.Checked)
                {
                    eventType = "sql_batch_completed";
                }
                else if (rpccompletedToolStripMenuItem.Checked && !sqlbatchcompletedToolStripMenuItem.Checked)
                {
                    eventType = "rpc_completed";
                }
                if (groupBy == "InstanceDisplayName")
                {
                    displayName = selectedGroupValue;
                }
                else if (groupBy == "client_hostname")
                {
                    client = selectedGroupValue;
                }
                else if (groupBy == "client_app_name")
                {
                    app = selectedGroupValue;
                }
                else if (groupBy == "DatabaseName")
                {
                    db = selectedGroupValue;
                }
                else if (groupBy == "object_name")
                {
                    objectname = selectedGroupValue;
                }
                else if (groupBy == "username")
                {
                    user = selectedGroupValue;
                }
                else if (groupBy == "Result")
                {
                    result = selectedGroupValue;
                }
                else if (groupBy == "text")
                {
                    text = selectedGroupValue;
                }
                else if (groupBy == "session_id")
                {
                    sessionid = selectedGroupValue;
                }
                else if (groupBy == "EventType")
                {
                    eventType = selectedGroupValue;
                }
                else
                {
                    throw new Exception($"Invalid group by: {groupBy}");
                }
                long cpuFromMs = Math.Max(cpuFrom, CPUFilterFrom);
                long cpuToMs = Math.Min(cpuTo, CPUFilterTo);
                long durationFromMs = Math.Max(durationFrom, DurationFilterFrom);
                long durationToMs = Math.Min(durationTo, DurationFilterTo);

                cmd.Parameters.AddIfGreaterThanZero("CPUFromMs", cpuFromMs);
                cmd.Parameters.AddIfLessThanMaxValue("CPUToMs", cpuToMs);
                cmd.Parameters.AddIfGreaterThanZero("DurationFromMs", durationFromMs);
                cmd.Parameters.AddIfLessThanMaxValue("DurationToMs", durationToMs);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientHostName", client);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", displayName);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ClientAppName", app);
                cmd.Parameters.AddStringIfNotNullOrEmpty("DatabaseName", db);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ObjectName", objectname);
                cmd.Parameters.AddStringIfNotNullOrEmpty("UserName", user);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Text", text);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Result", result);
                cmd.Parameters.AddStringIfNotNullOrEmpty("SessionID", sessionid);
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
                cmd.Parameters.AddStringIfNotNullOrEmpty("EventType", eventType);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void LoadSlowQueriesDetail(Int32 metricFrom = -1, Int32 metricTo = -1, bool failed = false)
        {
            long durationFrom = !IsCPU && metricFrom > 0 ? Convert.ToInt64(metricFrom) * 1000 : -1;
            long durationTo = !IsCPU && metricTo > 0 ? Convert.ToInt64(metricTo) * 1000 : long.MaxValue;
            long cpuFrom = IsCPU && metricFrom > 0 ? Convert.ToInt64(metricFrom) * 1000 : -1;
            long cpuTo = IsCPU && metricTo > 0 ? Convert.ToInt64(metricTo) * 1000 : long.MaxValue;
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
            if (dt.Rows.Count == pageSize)
            {
                lblPageSize.Text = string.Format("Top {0} rows", pageSize);
                lblPageSize.Visible = true;
            }
            else
            {
                lblPageSize.Visible = false;
            }
            dgvSlow.AutoGenerateColumns = false;

            dgvSlow.DataSource = dt;
            if (autoSizeColumnsToolStripMenuItem.Checked)
            {
                AutoSizeDetailGridColumns();
            }
        }

        private void AutoSizeDetailGridColumns()
        {
            colText.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvSlow.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            colText.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvSlow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DgvSlow_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvSlow.Rows[e.RowIndex].DataBoundItem;
                int sessionID = Convert.ToInt32(row["session_id"]);
                DateTime timestamp = Convert.ToDateTime(row["timestamp"]);
                if (dgvSlow.Columns[e.ColumnIndex] == colText)
                {
                    string title = "SPID: " + sessionID + ", " + timestamp.ToString();
                    Common.ShowCodeViewer((string)row["Text"], title);
                }
                else if (dgvSlow.Columns[e.ColumnIndex] == colSessionID)
                {
                    DateTime toDate = timestamp.AppTimeZoneToUtc();
                    DateTime fromDate = Convert.ToDateTime(row["start_time"]).AppTimeZoneToUtc();
                    int instanceID = Convert.ToInt32(row["InstanceID"]);
                    ToggleSummary(false);
                    runningQueries1.SessionID = sessionID;
                    runningQueries1.SnapshotDateFrom = fromDate;
                    runningQueries1.SnapshotDateTo = toDate;
                    runningQueries1.InstanceID = instanceID;
                    runningQueries1.RefreshData();
                }
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
                tsTop.Text = "Top " + ts.Tag.ToString();
            }
            tsTop.Tag = ts.Tag;
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSummary);
        }

        private void TsExcelDetail_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSlow);
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
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsControl(e.KeyChar))
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
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsControl(e.KeyChar) && !(e.KeyChar == ','))
            {
                e.Handled = true;
            }
        }

        private void DgvSlow_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgvSlow.Rows[idx];
                var status = Convert.ToString(r.Cells["Result"].Value) == "0 - OK" ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Critical;
                r.Cells["Result"].SetStatusColor(status);
            }
        }

        private void LoadSavedLayout()
        {
            resetLayoutToolStripMenuItem.Enabled = false;
            loadSavedToolStripMenuItem.Enabled = false;
            try
            {
                SlowQueryDetailSavedView saved = SlowQueryDetailSavedView.GetDefaultSavedView();

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
                MessageBox.Show("Error loading saved view\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Error saving grid layout\n", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AutoSizeColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autoSizeColumnsToolStripMenuItem.Checked)
            {
                AutoSizeDetailGridColumns();
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
                SlowQueryDetailSavedView saved = SlowQueryDetailSavedView.GetDefaultSavedView();
                if (saved != null)
                {
                    if (MessageBox.Show("Remove saved layout?", "Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        saved.Delete();
                        resetLayoutToolStripMenuItem.Enabled = false;
                        loadSavedToolStripMenuItem.Enabled = false;
                        MessageBox.Show("The application defaults will be used the next time the application is started", "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting saved view\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Metric_Selected(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            tsMetric.CheckSingleItem(item);
            tsMetric.Text = item.Text;
            RefreshData();
        }
    }
}